# -*- coding: utf-8 -*-
"""Apply low-risk mechanical edits to a DOCX copy.

Safe defaults:
- original file is never modified;
- foreign terms are italicized only before the References section;
- explicit replacements come from JSON and are logged;
- manual line breaks are normalized only in reference-like paragraphs.
"""

from __future__ import annotations

import argparse
import json
import re
import shutil
import zipfile
from pathlib import Path
from xml.etree import ElementTree as ET

from docx_common import (
    NS,
    clone_run_with_text,
    compact_text,
    ensure_docx,
    load_terms,
    normalize_key,
    paragraph_text,
    run_is_plain_text,
    run_text,
    wtag,
    xml_attr,
)


def load_replacements(path: str | Path | None) -> list[dict]:
    if not path:
        return []
    payload = json.loads(Path(path).read_text(encoding="utf-8"))
    if isinstance(payload, dict):
        payload = payload.get("replacements", [])
    result = []
    for item in payload:
        if not item.get("enabled", True):
            continue
        if "find" not in item or "replace" not in item:
            continue
        result.append(
            {
                "find": str(item["find"]),
                "replace": str(item["replace"]),
                "case_sensitive": bool(item.get("case_sensitive", True)),
                "note": item.get("note", ""),
            }
        )
    return result


def reference_started(text: str) -> bool:
    return normalize_key(text) in {"REFERENCIAS", "REFERENCIAS BIBLIOGRAFICAS", "REFERENCIA"}


def paragraph_is_reference_like(text: str) -> bool:
    normalized = normalize_key(text)
    return (
        "DISPONIVEL EM" in normalized
        or "ACESSO EM" in normalized
        or "HTTP" in normalized
        or "WWW" in normalized
    )


def apply_text_replacements(root: ET.Element, replacements: list[dict]) -> list[dict]:
    log: list[dict] = []
    if not replacements:
        return log
    ordinal = 0
    for paragraph in root.iter(wtag("p")):
        para_text = compact_text(paragraph_text(paragraph))
        if para_text:
            ordinal += 1
        for text_node in paragraph.iter(wtag("t")):
            value = text_node.text or ""
            if not value:
                continue
            new_value = value
            for item in replacements:
                find = item["find"]
                replace = item["replace"]
                if item["case_sensitive"]:
                    count = new_value.count(find)
                    if count:
                        new_value = new_value.replace(find, replace)
                else:
                    pattern = re.compile(re.escape(find), re.IGNORECASE)
                    count = len(pattern.findall(new_value))
                    if count:
                        new_value = pattern.sub(replace, new_value)
                if count:
                    log.append(
                        {
                            "type": "replacement",
                            "paragraph": ordinal,
                            "find": find,
                            "replace": replace,
                            "count": count,
                            "note": item.get("note", ""),
                            "context": para_text[:260],
                        }
                    )
            if new_value != value:
                text_node.text = new_value
                if new_value[:1].isspace() or new_value[-1:].isspace() or "  " in new_value:
                    text_node.attrib[xml_attr("space")] = "preserve"
    return log


def apply_italic_terms(root: ET.Element, terms: list[str]) -> list[dict]:
    log: list[dict] = []
    if not terms:
        return log
    pattern = re.compile(
        r"(?<!\w)(" + "|".join(re.escape(term) for term in sorted(terms, key=len, reverse=True)) + r")(?!\w)",
        re.IGNORECASE,
    )

    references_seen = False
    ordinal = 0
    for paragraph in list(root.iter(wtag("p"))):
        para_text = compact_text(paragraph_text(paragraph))
        if para_text:
            ordinal += 1
        if reference_started(para_text):
            references_seen = True
        if references_seen:
            continue

        parent_map = {child: parent for parent in paragraph.iter() for child in list(parent)}
        for run in list(paragraph.iter(wtag("r"))):
            if not run_is_plain_text(run):
                continue
            text = run_text(run)
            if not text:
                continue
            if run.find("./w:rPr/w:i", NS) is not None:
                continue
            matches = list(pattern.finditer(text))
            if not matches:
                continue
            parent = parent_map.get(run)
            if parent is None:
                continue
            new_runs = []
            cursor = 0
            for match in matches:
                if match.start() > cursor:
                    new_runs.append(clone_run_with_text(run, text[cursor : match.start()], italic=False))
                new_runs.append(clone_run_with_text(run, match.group(0), italic=True))
                log.append(
                    {
                        "type": "italic",
                        "paragraph": ordinal,
                        "term": match.group(0),
                        "context": para_text[:260],
                    }
                )
                cursor = match.end()
            if cursor < len(text):
                new_runs.append(clone_run_with_text(run, text[cursor:], italic=False))
            index = list(parent).index(run)
            parent.remove(run)
            for offset, new_run in enumerate(new_runs):
                parent.insert(index + offset, new_run)
    return log


def normalize_reference_breaks(root: ET.Element) -> list[dict]:
    log: list[dict] = []
    references_seen = False
    ordinal = 0
    for paragraph in root.iter(wtag("p")):
        para_text = compact_text(paragraph_text(paragraph))
        if para_text:
            ordinal += 1
        if reference_started(para_text):
            references_seen = True
        if not references_seen or not paragraph_is_reference_like(para_text):
            continue

        parent_map = {child: parent for parent in paragraph.iter() for child in list(parent)}
        breaks = [node for node in paragraph.iter(wtag("br"))]
        if not breaks:
            continue
        for br in breaks:
            parent = parent_map.get(br)
            if parent is None:
                continue
            index = list(parent).index(br)
            parent.remove(br)
            replacement = ET.Element(wtag("t"))
            replacement.attrib[xml_attr("space")] = "preserve"
            replacement.text = " "
            parent.insert(index, replacement)
        log.append(
            {
                "type": "normalize-reference-breaks",
                "paragraph": ordinal,
                "count": len(breaks),
                "context": para_text[:260],
            }
        )
    return log


def write_modified_docx(source: Path, destination: Path, document_xml: bytes) -> None:
    destination.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(source, "r") as zin, zipfile.ZipFile(destination, "w", zipfile.ZIP_DEFLATED) as zout:
        for item in zin.infolist():
            if item.filename == "word/document.xml":
                zout.writestr(item, document_xml)
            else:
                zout.writestr(item, zin.read(item.filename))


def write_logs(out_dir: Path, edited_docx: Path, log: list[dict]) -> None:
    out_dir.mkdir(parents=True, exist_ok=True)
    (out_dir / "alteracoes-aplicadas.json").write_text(json.dumps(log, ensure_ascii=False, indent=2), encoding="utf-8")
    lines = [
        "# Alterações aplicadas na cópia",
        "",
        f"Arquivo editado: `{edited_docx}`",
        "",
        "## Resumo",
        "",
    ]
    by_type: dict[str, int] = {}
    for item in log:
        by_type[item["type"]] = by_type.get(item["type"], 0) + int(item.get("count", 1))
    if by_type:
        for key, value in sorted(by_type.items()):
            lines.append(f"- {key}: {value}")
    else:
        lines.append("- Nenhuma alteração aplicada.")
    lines.extend(["", "## Detalhe", ""])
    for item in log:
        if item["type"] == "italic":
            lines.append(f"- P{item['paragraph']:04d}: itálico em `{item['term']}`. Contexto: {item['context']}")
        elif item["type"] == "replacement":
            lines.append(
                f"- P{item['paragraph']:04d}: `{item['find']}` -> `{item['replace']}` ({item['count']}x). Contexto: {item['context']}"
            )
        elif item["type"] == "normalize-reference-breaks":
            lines.append(f"- P{item['paragraph']:04d}: {item['count']} quebra(s) interna(s) normalizada(s). Contexto: {item['context']}")
    (out_dir / "alteracoes-aplicadas.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


def apply_safe_formatting(
    source_docx: str | Path,
    output_docx: str | Path,
    terms_file: str | Path | None = None,
    replacements_file: str | Path | None = None,
    normalize_breaks: bool = True,
    log_dir: str | Path | None = None,
) -> list[dict]:
    source = ensure_docx(source_docx)
    output = Path(output_docx)
    with zipfile.ZipFile(source, "r") as archive:
        root = ET.fromstring(archive.read("word/document.xml"))

    log: list[dict] = []
    log.extend(apply_text_replacements(root, load_replacements(replacements_file)))
    log.extend(apply_italic_terms(root, load_terms(terms_file)))
    if normalize_breaks:
        log.extend(normalize_reference_breaks(root))

    xml_bytes = ET.tostring(root, encoding="utf-8", xml_declaration=True)
    if source.resolve() == output.resolve():
        raise ValueError("Refusing to overwrite the original DOCX. Choose a different output path.")
    if not log:
        shutil.copy2(source, output)
    else:
        write_modified_docx(source, output, xml_bytes)
    if log_dir:
        write_logs(Path(log_dir), output, log)
    return log


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Aplica formatação mecânica segura em uma cópia DOCX.")
    parser.add_argument("docx", help="Arquivo .docx original")
    parser.add_argument("--out", required=True, help="Arquivo .docx de saída")
    parser.add_argument("--terms", default=None, help="Arquivo de estrangeirismos/termos técnicos")
    parser.add_argument("--replacements", default=None, help="JSON com substituições explícitas")
    parser.add_argument("--no-normalize-breaks", action="store_true", help="Não normaliza quebras internas em referências/URLs")
    parser.add_argument("--log-dir", default=None, help="Pasta para log Markdown/JSON")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    log = apply_safe_formatting(
        args.docx,
        args.out,
        terms_file=args.terms,
        replacements_file=args.replacements,
        normalize_breaks=not args.no_normalize_breaks,
        log_dir=args.log_dir,
    )
    print(f"Cópia gerada: {args.out}")
    print(f"Alterações aplicadas: {len(log)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

