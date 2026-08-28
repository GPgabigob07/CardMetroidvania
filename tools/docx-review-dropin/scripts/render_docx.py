# -*- coding: utf-8 -*-
"""Render DOCX to PDF, optionally PNG, using local desktop tools.

Priority:
1. Microsoft Word via PowerShell COM automation on Windows.
2. LibreOffice/soffice if available in PATH.
3. PNG pages only when pdftoppm is available after PDF export.
"""

from __future__ import annotations

import argparse
import shutil
import subprocess
from datetime import datetime
from pathlib import Path

from docx_common import ensure_docx


def run(command: list[str], timeout: int = 180) -> subprocess.CompletedProcess:
    return subprocess.run(command, text=True, capture_output=True, timeout=timeout)


def ps_quote(path: Path) -> str:
    return "'" + str(path).replace("'", "''") + "'"


def render_with_word(docx: Path, out_dir: Path) -> tuple[bool, str, Path | None]:
    if not shutil.which("powershell"):
        return False, "PowerShell não encontrado.", None
    pdf = out_dir / f"{docx.stem}.pdf"
    script = f"""
$ErrorActionPreference = 'Stop'
$docx = {ps_quote(docx.resolve())}
$pdf = {ps_quote(pdf.resolve())}
$word = New-Object -ComObject Word.Application
$word.Visible = $false
try {{
  $doc = $word.Documents.Open($docx, $false, $false)
  try {{
    foreach ($field in $doc.Fields) {{ $field.Update() | Out-Null }}
    foreach ($toc in $doc.TablesOfContents) {{ $toc.Update() | Out-Null }}
    foreach ($tof in $doc.TablesOfFigures) {{ $tof.Update() | Out-Null }}
  }} catch {{
    Write-Host "Aviso: não foi possível atualizar todos os campos: $($_.Exception.Message)"
  }}
  $doc.ExportAsFixedFormat($pdf, 17)
  $doc.Close($false)
}} finally {{
  $word.Quit()
}}
"""
    ps1 = out_dir / "_render_word.ps1"
    ps1.write_text(script, encoding="utf-8")
    result = run(["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", str(ps1)], timeout=240)
    ok = result.returncode == 0 and pdf.exists() and pdf.stat().st_size > 0
    message = (result.stdout + "\n" + result.stderr).strip()
    return ok, message or "Renderização via Word concluída.", pdf if ok else None


def render_with_libreoffice(docx: Path, out_dir: Path) -> tuple[bool, str, Path | None]:
    executable = shutil.which("soffice") or shutil.which("libreoffice")
    if not executable:
        return False, "LibreOffice/soffice não encontrado.", None
    result = run(
        [
            executable,
            "--headless",
            "--convert-to",
            "pdf",
            "--outdir",
            str(out_dir),
            str(docx),
        ],
        timeout=240,
    )
    pdf = out_dir / f"{docx.stem}.pdf"
    ok = result.returncode == 0 and pdf.exists() and pdf.stat().st_size > 0
    message = (result.stdout + "\n" + result.stderr).strip()
    return ok, message or "Renderização via LibreOffice concluída.", pdf if ok else None


def render_png_pages(pdf: Path, out_dir: Path) -> tuple[bool, str]:
    pdftoppm = shutil.which("pdftoppm")
    if not pdftoppm:
        return False, "pdftoppm não encontrado; PNGs não foram gerados."
    pages_dir = out_dir / "pages"
    pages_dir.mkdir(parents=True, exist_ok=True)
    prefix = pages_dir / "page"
    result = run([pdftoppm, "-png", "-r", "150", str(pdf), str(prefix)], timeout=240)
    ok = result.returncode == 0 and any(pages_dir.glob("page-*.png"))
    message = (result.stdout + "\n" + result.stderr).strip()
    return ok, message or "PNGs gerados com pdftoppm."


def render_docx(docx_path: str | Path, out_dir: str | Path, png: bool = False, prefer: str = "word") -> dict:
    docx = ensure_docx(docx_path)
    out_dir = Path(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    attempts: list[dict] = []
    pdf: Path | None = None

    renderers = ["word", "libreoffice"] if prefer == "word" else ["libreoffice", "word"]
    for renderer in renderers:
        if renderer == "word":
            ok, message, candidate = render_with_word(docx, out_dir)
        else:
            ok, message, candidate = render_with_libreoffice(docx, out_dir)
        attempts.append({"renderer": renderer, "ok": ok, "message": message})
        if ok:
            pdf = candidate
            break

    png_result = None
    if pdf and png:
        ok, message = render_png_pages(pdf, out_dir)
        png_result = {"ok": ok, "message": message}

    log = {
        "docx": str(docx),
        "out_dir": str(out_dir),
        "generated_at": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        "pdf": str(pdf) if pdf else None,
        "attempts": attempts,
        "png": png_result,
    }
    lines = [
        "# Renderização DOCX",
        "",
        f"Arquivo: `{docx}`",
        f"Gerado em: {log['generated_at']}",
        f"PDF: `{pdf}`" if pdf else "PDF: não gerado",
        "",
        "## Tentativas",
        "",
    ]
    for attempt in attempts:
        lines.append(f"- {attempt['renderer']}: {'ok' if attempt['ok'] else 'falhou'} - {attempt['message']}")
    if png_result:
        lines.extend(["", f"PNG: {'ok' if png_result['ok'] else 'falhou'} - {png_result['message']}"])
    (out_dir / "render-log.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    return log


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Renderiza DOCX para PDF e, se possível, PNG.")
    parser.add_argument("docx", help="Arquivo .docx")
    parser.add_argument("--out", default="revisao-docx/render", help="Pasta de saída")
    parser.add_argument("--png", action="store_true", help="Tenta gerar PNGs se pdftoppm estiver disponível")
    parser.add_argument("--prefer", choices=["word", "libreoffice"], default="word", help="Renderizador preferido")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    result = render_docx(args.docx, args.out, png=args.png, prefer=args.prefer)
    print(result["pdf"] or "Renderização falhou; veja render-log.md")
    return 0 if result["pdf"] else 1


if __name__ == "__main__":
    raise SystemExit(main())

