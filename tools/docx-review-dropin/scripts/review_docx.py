# -*- coding: utf-8 -*-
"""One-command drop-in workflow for conservative DOCX review."""

from __future__ import annotations

import argparse
import shutil
from datetime import datetime
from pathlib import Path

from apply_safe_formatting import apply_safe_formatting
from audit_docx import run_audit
from docx_common import ensure_docx
from render_docx import render_docx


PACKAGE_ROOT = Path(__file__).resolve().parents[1]


def timestamp() -> str:
    return datetime.now().strftime("%Y%m%d-%H%M")


def default_out_dir(docx: Path) -> Path:
    return docx.parent / f"REVISAO_{docx.stem}_{timestamp()}"


def build_readme(out_dir: Path, source: Path, edited: Path | None, render_result: dict | None) -> None:
    lines = [
        "# Pacote de revisão gerado",
        "",
        f"Documento original: `{source}`",
        "",
        "## Arquivos principais",
        "",
        f"- Snapshot do original: `{out_dir / (source.stem + '_ORIGINAL_SNAPSHOT.docx')}`",
    ]
    if edited:
        lines.append(f"- Cópia editada: `{edited}`")
    if render_result and render_result.get("pdf"):
        lines.append(f"- PDF renderizado: `{render_result['pdf']}`")
    lines.extend(
        [
            "",
            "## Relatórios",
            "",
            "- `audit-original/relatorio-geral-revisao.md`",
            "- `audit-original/citacoes-e-referencias.md`",
            "- `audit-original/figuras-e-layout.md`",
            "- `audit-original/formatacao-candidatos.md`",
        ]
    )
    if edited:
        lines.extend(
            [
                "- `audit-editado/relatorio-geral-revisao.md`",
                "- `alteracoes/alteracoes-aplicadas.md`",
            ]
        )
    lines.extend(
        [
            "",
            "## Regra de ouro",
            "",
            "A cópia editada recebe apenas alterações mecânicas. Mudanças de argumento, estrutura, citação ou referência devem ser decididas manualmente.",
        ]
    )
    (out_dir / "LEIA-ME-RESULTADO.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def run_review(
    docx_path: str | Path,
    out_dir: str | Path | None = None,
    terms_file: str | Path | None = None,
    replacements_file: str | Path | None = None,
    audit_only: bool = False,
    no_render: bool = False,
    png: bool = False,
    make_zip: bool = False,
) -> dict:
    source = ensure_docx(docx_path)
    out_dir = Path(out_dir) if out_dir else default_out_dir(source)
    out_dir.mkdir(parents=True, exist_ok=True)

    terms_file = Path(terms_file) if terms_file else PACKAGE_ROOT / "foreign_terms.txt"
    replacements_file = Path(replacements_file) if replacements_file else PACKAGE_ROOT / "safe_replacements.json"

    snapshot = out_dir / f"{source.stem}_ORIGINAL_SNAPSHOT.docx"
    shutil.copy2(source, snapshot)

    audit_original = run_audit(source, out_dir / "audit-original", terms_file)

    edited: Path | None = None
    edit_log = None
    audit_edited = None
    render_result = None
    if not audit_only:
        edited = out_dir / f"{source.stem}_CODEX_EDITADO.docx"
        edit_log = apply_safe_formatting(
            source,
            edited,
            terms_file=terms_file,
            replacements_file=replacements_file,
            normalize_breaks=True,
            log_dir=out_dir / "alteracoes",
        )
        audit_edited = run_audit(edited, out_dir / "audit-editado", terms_file)
        if not no_render:
            render_result = render_docx(edited, out_dir / "render-editado", png=png)

    build_readme(out_dir, source, edited, render_result)

    zip_path = None
    if make_zip:
        archive_base = out_dir.with_suffix("")
        zip_path = shutil.make_archive(str(archive_base), "zip", root_dir=out_dir)

    return {
        "out_dir": str(out_dir),
        "snapshot": str(snapshot),
        "edited": str(edited) if edited else None,
        "audit_original_missing": len(audit_original["missing_references"]),
        "audit_edited_missing": len(audit_edited["missing_references"]) if audit_edited else None,
        "edit_count": len(edit_log) if edit_log is not None else None,
        "pdf": render_result.get("pdf") if render_result else None,
        "zip": zip_path,
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Fluxo completo de revisão DOCX conservadora.")
    parser.add_argument("docx", help="Arquivo .docx a revisar")
    parser.add_argument("--out", default=None, help="Pasta de saída. Padrão: pasta ao lado do DOCX.")
    parser.add_argument("--terms", default=None, help="Arquivo de estrangeirismos. Padrão: foreign_terms.txt do pacote.")
    parser.add_argument("--replacements", default=None, help="JSON de substituições. Padrão: safe_replacements.json do pacote.")
    parser.add_argument("--audit-only", action="store_true", help="Só audita; não cria cópia editada.")
    parser.add_argument("--no-render", action="store_true", help="Não tenta exportar PDF.")
    parser.add_argument("--png", action="store_true", help="Tenta gerar PNGs quando pdftoppm estiver disponível.")
    parser.add_argument("--zip", action="store_true", help="Compacta a pasta de resultado ao final.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    result = run_review(
        args.docx,
        out_dir=args.out,
        terms_file=args.terms,
        replacements_file=args.replacements,
        audit_only=args.audit_only,
        no_render=args.no_render,
        png=args.png,
        make_zip=args.zip,
    )
    print(f"Pasta de saída: {result['out_dir']}")
    print(f"Cópia editada: {result['edited'] or 'não gerada'}")
    print(f"Alterações aplicadas: {result['edit_count'] if result['edit_count'] is not None else 'n/a'}")
    print(f"PDF: {result['pdf'] or 'não gerado'}")
    print(f"ZIP: {result['zip'] or 'não gerado'}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

