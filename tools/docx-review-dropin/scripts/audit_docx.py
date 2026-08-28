# -*- coding: utf-8 -*-
"""Audit a DOCX for ABNT-style review support.

This script does not edit the document.  It extracts text, detects citations
that look orphaned, reports references that may be unused, lists image/caption
signals, and writes Markdown/JSON files that a human reviewer can inspect.
"""

from __future__ import annotations

import argparse
from datetime import datetime
from pathlib import Path

from docx_common import (
    count_images_and_links,
    ensure_docx,
    extract_citations,
    extract_reference_keys,
    find_bold_candidates,
    find_caption_issues,
    find_foreign_term_occurrences,
    find_missing_references,
    find_section_ordinals,
    find_unused_references,
    iter_paragraph_records,
    load_terms,
    markdown_table_row,
    write_json,
)


def build_general_report(docx_path: Path, out_dir: Path, analysis: dict) -> str:
    missing = analysis["missing_references"]
    unused = analysis["unused_references"]
    image_info = analysis["image_info"]
    section_info = analysis["sections"]
    lines = [
        "# Relatório geral da auditoria DOCX",
        "",
        f"Arquivo auditado: `{docx_path}`",
        f"Gerado em: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}",
        "",
        "## Resumo técnico",
        "",
        f"- Parágrafos com texto: {analysis['paragraph_count']}",
        f"- Citações detectadas: {analysis['citation_count']}",
        f"- Possíveis citações sem referência: {len(missing)}",
        f"- Possíveis referências não citadas: {len(unused)}",
        f"- Arquivos de mídia no DOCX: {image_info['media_files']}",
        f"- Elementos gráficos no XML: {image_info['drawing_elements']}",
        f"- Hiperlinks no XML: {image_info['hyperlink_elements']}",
        "",
        "## Seções detectadas",
        "",
        f"- Referências: parágrafo {section_info.get('references') or 'não detectado'}",
        f"- Ludografia: parágrafo {section_info.get('ludography') or 'não detectado'}",
        f"- Apêndice/anexo: parágrafo {section_info.get('appendix') or 'não detectado'}",
        "",
        "## Leituras recomendadas",
        "",
        "- Revisar manualmente qualquer citação listada como órfã antes da entrega.",
        "- Não criar referência por aproximação: fonte não confirmada deve ser substituída ou removida.",
        "- Conferir imagens com fonte `Elaborado pelo autor` quando elas usam screenshots, artbooks ou sites oficiais.",
        "- Renderizar para PDF e fazer inspeção visual antes de enviar.",
        "",
        "Arquivos gerados nesta auditoria:",
        "",
        "- `paragraph-index.txt`",
        "- `document-audit.json`",
        "- `citacoes-e-referencias.md`",
        "- `figuras-e-layout.md`",
        "- `formatacao-candidatos.md`",
    ]
    return "\n".join(lines) + "\n"


def build_citation_report(analysis: dict) -> str:
    lines = [
        "# Citações e referências",
        "",
        "## Possíveis citações sem referência",
        "",
    ]
    missing = analysis["missing_references"]
    if not missing:
        lines.append("Nenhuma ocorrência forte detectada.")
    else:
        lines.extend(
            [
                markdown_table_row(["Parágrafo", "Chamada", "Ano", "Tipo", "Trecho"]),
                markdown_table_row(["---", "---", "---", "---", "---"]),
            ]
        )
        for item in missing:
            lines.append(
                markdown_table_row(
                    [
                        item["paragraph"],
                        item["author"],
                        item["year"],
                        item["kind"],
                        item["text"][:260],
                    ]
                )
            )
    lines.extend(["", "## Possíveis referências não citadas", ""])
    unused = analysis["unused_references"]
    if not unused:
        lines.append("Nenhuma ocorrência forte detectada.")
    else:
        lines.extend(
            [
                markdown_table_row(["Parágrafo", "Autor-chave", "Ano", "Entrada"]),
                markdown_table_row(["---", "---", "---", "---"]),
            ]
        )
        for item in unused:
            lines.append(markdown_table_row([item["paragraph"], ", ".join(item["keys"]), item["year"], item["text"][:280]]))
    lines.extend(
        [
            "",
            "## Observação",
            "",
            "A detecção é heurística. Ela ajuda a encontrar problemas, mas não substitui a conferência humana das referências finais.",
        ]
    )
    return "\n".join(lines) + "\n"


def build_layout_report(analysis: dict) -> str:
    lines = [
        "# Figuras, legendas e layout",
        "",
        "## Imagens e links",
        "",
    ]
    image_info = analysis["image_info"]
    for key, value in image_info.items():
        lines.append(f"- {key}: {value}")
    lines.extend(["", "## Problemas potenciais de legenda/fonte", ""])
    issues = analysis["caption_issues"]
    if not issues:
        lines.append("Nenhum problema forte detectado em legendas de Figura/Tabela/Quadro.")
    else:
        lines.extend(
            [
                markdown_table_row(["Parágrafo", "Tipo", "Trecho", "Sugestão"]),
                markdown_table_row(["---", "---", "---", "---"]),
            ]
        )
        for item in issues:
            lines.append(markdown_table_row([item["paragraph"], item["kind"], item["text"][:240], item["suggestion"]]))
    lines.extend(
        [
            "",
            "## Checagens manuais recomendadas",
            "",
            "- Sumário e lista de figuras atualizados no Word.",
            "- Numeração progressiva coerente.",
            "- Títulos sem ficar sozinhos no fim da página.",
            "- Figuras com fonte rastreável quando não forem 100% autorais.",
            "- Quebras de seção/página justificadas.",
        ]
    )
    return "\n".join(lines) + "\n"


def build_format_report(analysis: dict) -> str:
    lines = [
        "# Formatação: candidatos para revisão",
        "",
        "## Estrangeirismos/termos técnicos encontrados",
        "",
    ]
    terms = analysis["foreign_terms"]
    if not terms:
        lines.append("Nenhum termo listado foi detectado, ou nenhum arquivo de termos foi informado.")
    else:
        lines.extend(
            [
                markdown_table_row(["Parágrafo", "Termo", "Contexto"]),
                markdown_table_row(["---", "---", "---"]),
            ]
        )
        for item in terms:
            lines.append(markdown_table_row([item["paragraph"], item["term"], item["context"]]))
    lines.extend(["", "## Negritos em parágrafos comuns", ""])
    bold = analysis["bold_candidates"]
    if not bold:
        lines.append("Nenhum candidato forte detectado.")
    else:
        lines.extend(
            [
                markdown_table_row(["Parágrafo", "Estilo", "Trechos em negrito", "Contexto"]),
                markdown_table_row(["---", "---", "---", "---"]),
            ]
        )
        for item in bold:
            lines.append(
                markdown_table_row(
                    [
                        item["paragraph"],
                        item["style"],
                        "; ".join(item["bold_runs"]),
                        item["text"],
                    ]
                )
            )
    lines.extend(
        [
            "",
            "Observação: negrito não deve ser removido automaticamente sem inspeção visual, pois pode indicar título, rótulo, pergunta ou estrutura do template.",
        ]
    )
    return "\n".join(lines) + "\n"


def run_audit(docx_path: str | Path, out_dir: str | Path, terms_file: str | Path | None = None) -> dict:
    docx_path = ensure_docx(docx_path)
    out_dir = Path(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    terms = load_terms(terms_file)
    paragraphs = iter_paragraph_records(docx_path)
    analysis = {
        "docx": str(docx_path),
        "paragraph_count": len(paragraphs),
        "sections": find_section_ordinals(paragraphs),
        "citation_count": len(extract_citations(paragraphs)),
        "reference_keys": {year: sorted(keys) for year, keys in extract_reference_keys(paragraphs).items()},
        "missing_references": find_missing_references(paragraphs),
        "unused_references": find_unused_references(paragraphs),
        "image_info": count_images_and_links(docx_path),
        "caption_issues": find_caption_issues(paragraphs),
        "foreign_terms": find_foreign_term_occurrences(paragraphs, terms),
        "bold_candidates": find_bold_candidates(docx_path),
    }
    (out_dir / "paragraph-index.txt").write_text(
        "\n".join(f"P{item['ordinal']:04d} | {item['style']} | {item['text']}" for item in paragraphs) + "\n",
        encoding="utf-8",
    )
    write_json(out_dir / "document-audit.json", analysis)
    (out_dir / "relatorio-geral-revisao.md").write_text(build_general_report(docx_path, out_dir, analysis), encoding="utf-8")
    (out_dir / "citacoes-e-referencias.md").write_text(build_citation_report(analysis), encoding="utf-8")
    (out_dir / "figuras-e-layout.md").write_text(build_layout_report(analysis), encoding="utf-8")
    (out_dir / "formatacao-candidatos.md").write_text(build_format_report(analysis), encoding="utf-8")
    return analysis


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Audita um arquivo DOCX sem modificar o original.")
    parser.add_argument("docx", help="Caminho do arquivo .docx")
    parser.add_argument("--out", default="revisao-docx/audit", help="Pasta de saída da auditoria")
    parser.add_argument("--terms", default=None, help="Arquivo com estrangeirismos/termos técnicos, um por linha")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    analysis = run_audit(args.docx, args.out, args.terms)
    print(f"Auditoria concluída: {args.out}")
    print(f"Possíveis citações sem referência: {len(analysis['missing_references'])}")
    print(f"Possíveis referências não citadas: {len(analysis['unused_references'])}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

