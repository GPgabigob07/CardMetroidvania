# -*- coding: utf-8 -*-
"""Small OOXML helpers for conservative DOCX review tools.

The helpers intentionally use only Python's standard library.  A DOCX is a ZIP
of XML files; reading the XML directly preserves images, fields, hyperlinks and
styles better than rebuilding the document from scratch.
"""

from __future__ import annotations

import json
import re
import unicodedata
import zipfile
from collections import defaultdict
from copy import deepcopy
from pathlib import Path
from typing import Iterable
from xml.etree import ElementTree as ET


NS = {
    "w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main",
    "r": "http://schemas.openxmlformats.org/officeDocument/2006/relationships",
    "a": "http://schemas.openxmlformats.org/drawingml/2006/main",
    "wp": "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing",
    "pic": "http://schemas.openxmlformats.org/drawingml/2006/picture",
    "xml": "http://www.w3.org/XML/1998/namespace",
}

for prefix, uri in NS.items():
    if prefix != "xml":
        ET.register_namespace(prefix, uri)


def wtag(name: str) -> str:
    return f"{{{NS['w']}}}{name}"


def atag(name: str) -> str:
    return f"{{{NS['a']}}}{name}"


def rtag(name: str) -> str:
    return f"{{{NS['r']}}}{name}"


def xml_attr(name: str) -> str:
    return f"{{{NS['xml']}}}{name}"


def wval(element: ET.Element | None) -> str | None:
    if element is None:
        return None
    return element.attrib.get(wtag("val"))


def normalize_key(value: str) -> str:
    """Uppercase, accent-free key used for tolerant ABNT matching."""
    decomposed = unicodedata.normalize("NFD", value or "")
    no_accents = "".join(ch for ch in decomposed if unicodedata.category(ch) != "Mn")
    cleaned = re.sub(r"[^A-Za-z0-9]+", " ", no_accents).strip().upper()
    return re.sub(r"\s+", " ", cleaned)


def ensure_docx(path: str | Path) -> Path:
    candidate = Path(path)
    if not candidate.exists():
        raise FileNotFoundError(candidate)
    if candidate.suffix.lower() != ".docx":
        raise ValueError(f"Expected a .docx file, got: {candidate}")
    return candidate


def read_document_root(docx_path: str | Path) -> ET.Element:
    docx_path = ensure_docx(docx_path)
    with zipfile.ZipFile(docx_path) as archive:
        return ET.fromstring(archive.read("word/document.xml"))


def paragraph_text(paragraph: ET.Element) -> str:
    parts: list[str] = []
    for node in paragraph.iter():
        if node.tag == wtag("t"):
            parts.append(node.text or "")
        elif node.tag == wtag("tab"):
            parts.append("\t")
        elif node.tag == wtag("br"):
            parts.append("\n")
    return "".join(parts)


def compact_text(value: str) -> str:
    return re.sub(r"\s+", " ", value or "").strip()


def paragraph_style_id(paragraph: ET.Element) -> str:
    p_style = paragraph.find("./w:pPr/w:pStyle", NS)
    return wval(p_style) or ""


def property_is_on(run: ET.Element, prop_name: str) -> bool:
    prop = run.find(f"./w:rPr/w:{prop_name}", NS)
    if prop is None:
        return False
    value = prop.attrib.get(wtag("val"))
    return value not in {"0", "false", "False", "off"}


def run_text(run: ET.Element) -> str:
    return "".join((node.text or "") for node in run.findall("./w:t", NS))


def run_is_plain_text(run: ET.Element) -> bool:
    allowed = {wtag("rPr"), wtag("t")}
    return all(child.tag in allowed for child in list(run))


def iter_paragraph_records(docx_path: str | Path) -> list[dict]:
    root = read_document_root(docx_path)
    records: list[dict] = []
    ordinal = 0
    for xml_index, paragraph in enumerate(root.iter(wtag("p")), start=1):
        text = compact_text(paragraph_text(paragraph))
        if not text:
            continue
        ordinal += 1
        records.append(
            {
                "ordinal": ordinal,
                "xml_index": xml_index,
                "style": paragraph_style_id(paragraph),
                "text": text,
            }
        )
    return records


def find_section_ordinals(paragraphs: list[dict]) -> dict[str, int | None]:
    aliases = {
        "references": {
            "REFERENCIAS",
            "REFERENCIAS BIBLIOGRAFICAS",
            "REFERENCIA",
            "REFERENCIAS BIBLIOGRAFICAS",
        },
        "ludography": {"LUDOGRAFIA"},
        "appendix": {"APENDICE", "APENDICES", "ANEXO", "ANEXOS"},
    }
    result: dict[str, int | None] = {key: None for key in aliases}
    for record in paragraphs:
        key = normalize_key(record["text"])
        for section, names in aliases.items():
            if result[section] is None and key in names:
                result[section] = record["ordinal"]
    return result


def year_in_text(value: str) -> str | None:
    match = re.search(r"\b((?:18|19|20)\d{2}[a-z]?)\b", value)
    return match.group(1) if match else None


def split_author_field(value: str) -> list[str]:
    """Extract possible ABNT author keys from a reference entry prefix."""
    first_year = re.search(r"\b(?:18|19|20)\d{2}[a-z]?\b", value)
    prefix = value[: first_year.start()] if first_year else value[:120]
    # ABNT entries often put all authors before the title.  Split on semicolons
    # first, then keep the surname before the first comma.
    candidates: list[str] = []
    for chunk in prefix.split(";"):
        chunk = chunk.strip()
        if not chunk:
            continue
        if "," in chunk:
            candidates.append(chunk.split(",", 1)[0])
        elif "." in chunk:
            candidates.append(chunk.split(".", 1)[0])
        else:
            candidates.append(chunk)
    return [normalize_key(item) for item in candidates if normalize_key(item)]


def extract_reference_keys(paragraphs: list[dict]) -> dict[str, set[str]]:
    sections = find_section_ordinals(paragraphs)
    references_start = sections["references"]
    if references_start is None:
        return {}
    ludography_start = sections["ludography"] or len(paragraphs) + 1
    keys: dict[str, set[str]] = defaultdict(set)
    for record in paragraphs:
        ordinal = record["ordinal"]
        if ordinal <= references_start or ordinal >= ludography_start:
            continue
        text = record["text"]
        year = year_in_text(text)
        if not year:
            continue
        for key in split_author_field(text):
            keys[year[:4]].add(key)
    return dict(keys)


CONNECTIVE_PREFIXES = (
    "SEGUNDO",
    "CONFORME",
    "DE ACORDO COM",
    "PARA",
    "COMO APONTA",
    "COMO DESTACA",
    "COMO OBSERVA",
    "EM",
)


def citation_author_variants(author_text: str) -> set[str]:
    text = normalize_key(author_text)
    for prefix in CONNECTIVE_PREFIXES:
        if text.startswith(prefix + " "):
            text = text[len(prefix) + 1 :]
    text = re.sub(r"\bET AL\b\.?", "", text).strip()
    text = re.sub(r"\b(APUD|P|PP)\b.*$", "", text).strip()
    if not text:
        return set()
    pieces = re.split(r"\s+E\s+|\s*&\s*|\s*,\s*", text)
    variants: set[str] = set()
    particles = {"DE", "DA", "DO", "DAS", "DOS", "VAN", "VON", "DEL", "DI"}
    for piece in pieces:
        words = [w for w in piece.split() if w and w not in particles]
        if not words:
            continue
        variants.add(" ".join(words))
        variants.add(words[-1])
    return {variant for variant in variants if variant}


def extract_citations(paragraphs: list[dict]) -> list[dict]:
    sections = find_section_ordinals(paragraphs)
    references_start = sections["references"] or len(paragraphs) + 1
    citations: list[dict] = []
    seen: set[tuple[int, str, str, str]] = set()

    for record in paragraphs:
        if record["ordinal"] >= references_start:
            continue
        text = record["text"]

        for match in re.finditer(r"\(([^()]{2,180}?\b(?:18|19|20)\d{2}[a-z]?[^()]*)\)", text):
            content = match.group(1)
            for part in re.split(r"\s*;\s*", content):
                year_match = re.search(r"\b((?:18|19|20)\d{2})[a-z]?\b", part)
                if not year_match:
                    continue
                author = part[: year_match.start()].strip(" ,")
                if not author or normalize_key(author) in {"FIGURA", "TABELA", "QUADRO"}:
                    continue
                key = (record["ordinal"], author, year_match.group(1), "parenthetical")
                if key not in seen:
                    seen.add(key)
                    citations.append(
                        {
                            "paragraph": record["ordinal"],
                            "author": author,
                            "year": year_match.group(1),
                            "kind": "parenthetical",
                            "text": text,
                        }
                    )

        for match in re.finditer(r"\(((?:18|19|20)\d{2})[a-z]?\)", text):
            left = text[: match.start()].strip()
            tail = left[-100:]
            author_match = re.search(
                r"((?:[A-ZÁÀÂÃÉÈÊÍÏÓÔÕÖÚÜÇÑ][A-Za-zÁÀÂÃÉÈÊÍÏÓÔÕÖÚÜÇÑáàâãéèêíïóôõöúüçñ'-]+|et al\.|e|de|da|do|das|dos)\s*){1,10}$",
                tail,
            )
            if not author_match:
                continue
            author = author_match.group(0).strip()
            author_key = normalize_key(author)
            if author_key in {"FIGURA", "TABELA", "QUADRO"}:
                continue
            key = (record["ordinal"], author, match.group(1), "narrative")
            if key not in seen:
                seen.add(key)
                citations.append(
                    {
                        "paragraph": record["ordinal"],
                        "author": author,
                        "year": match.group(1),
                        "kind": "narrative",
                        "text": text,
                    }
                )
    return citations


def find_missing_references(paragraphs: list[dict]) -> list[dict]:
    reference_keys = extract_reference_keys(paragraphs)
    missing: list[dict] = []
    for citation in extract_citations(paragraphs):
        variants = citation_author_variants(citation["author"])
        available = reference_keys.get(citation["year"], set())
        matched = any(
            variant in available
            or any(variant in ref_key or ref_key in variant for ref_key in available)
            for variant in variants
        )
        if not matched:
            item = dict(citation)
            item["variants_checked"] = sorted(variants)
            missing.append(item)
    return missing


def find_unused_references(paragraphs: list[dict]) -> list[dict]:
    sections = find_section_ordinals(paragraphs)
    references_start = sections["references"]
    if references_start is None:
        return []
    ludography_start = sections["ludography"] or len(paragraphs) + 1
    cited_pairs = set()
    for citation in extract_citations(paragraphs):
        for variant in citation_author_variants(citation["author"]):
            cited_pairs.add((variant, citation["year"]))
    unused: list[dict] = []
    for record in paragraphs:
        ordinal = record["ordinal"]
        if ordinal <= references_start or ordinal >= ludography_start:
            continue
        year = year_in_text(record["text"])
        if not year:
            continue
        keys = split_author_field(record["text"])
        if keys and not any((key, year[:4]) in cited_pairs for key in keys):
            unused.append({"paragraph": ordinal, "keys": keys, "year": year[:4], "text": record["text"]})
    return unused


def count_images_and_links(docx_path: str | Path) -> dict:
    docx_path = ensure_docx(docx_path)
    with zipfile.ZipFile(docx_path) as archive:
        names = archive.namelist()
        media_files = [name for name in names if name.startswith("word/media/")]
        document_xml = archive.read("word/document.xml").decode("utf-8", errors="ignore")
    return {
        "media_files": len(media_files),
        "drawing_elements": document_xml.count("<w:drawing"),
        "blip_elements": document_xml.count("<a:blip"),
        "hyperlink_elements": document_xml.count("<w:hyperlink"),
    }


def find_caption_issues(paragraphs: list[dict]) -> list[dict]:
    issues: list[dict] = []
    caption_re = re.compile(r"^(Figura|Tabela|Quadro)\s+\d+\s+[–-]\s+\S", re.IGNORECASE)
    for index, record in enumerate(paragraphs):
        text = record["text"]
        if not re.match(r"^(Figura|Tabela|Quadro)\s+\d+", text, re.IGNORECASE):
            continue
        if not caption_re.match(text):
            issues.append(
                {
                    "paragraph": record["ordinal"],
                    "kind": "caption-format",
                    "text": text,
                    "suggestion": "Padronizar como 'Figura N – Título' / 'Tabela N – Título'.",
                }
            )
        next_text = paragraphs[index + 1]["text"] if index + 1 < len(paragraphs) else ""
        if not normalize_key(next_text).startswith("FONTE"):
            issues.append(
                {
                    "paragraph": record["ordinal"],
                    "kind": "missing-source-near-caption",
                    "text": text,
                    "suggestion": "Conferir se ha fonte logo abaixo da legenda.",
                }
            )
    return issues


def load_terms(path: str | Path | None) -> list[str]:
    if not path:
        return []
    term_path = Path(path)
    if not term_path.exists():
        raise FileNotFoundError(term_path)
    terms = []
    for line in term_path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        terms.append(line)
    return sorted(set(terms), key=lambda item: (-len(item), item.lower()))


def find_foreign_term_occurrences(paragraphs: list[dict], terms: Iterable[str]) -> list[dict]:
    occurrences: list[dict] = []
    if not terms:
        return occurrences
    pattern = re.compile(
        r"(?<!\w)(" + "|".join(re.escape(term) for term in sorted(terms, key=len, reverse=True)) + r")(?!\w)",
        re.IGNORECASE,
    )
    for record in paragraphs:
        for match in pattern.finditer(record["text"]):
            occurrences.append(
                {
                    "paragraph": record["ordinal"],
                    "term": match.group(0),
                    "context": record["text"][:260],
                }
            )
    return occurrences


def find_bold_candidates(docx_path: str | Path) -> list[dict]:
    root = read_document_root(docx_path)
    candidates: list[dict] = []
    ordinal = 0
    for paragraph in root.iter(wtag("p")):
        text = compact_text(paragraph_text(paragraph))
        if not text:
            continue
        ordinal += 1
        style = paragraph_style_id(paragraph)
        if normalize_key(style).startswith("HEADING") or normalize_key(style).startswith("TITULO"):
            continue
        bold_runs = []
        for run in paragraph.iter(wtag("r")):
            run_value = compact_text(run_text(run))
            if run_value and property_is_on(run, "b"):
                bold_runs.append(run_value)
        if bold_runs:
            candidates.append(
                {
                    "paragraph": ordinal,
                    "style": style,
                    "bold_runs": bold_runs[:12],
                    "text": text[:320],
                }
            )
    return candidates


def write_json(path: str | Path, payload: object) -> None:
    Path(path).write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")


def markdown_table_row(values: Iterable[object]) -> str:
    safe = [str(value).replace("|", "\\|").replace("\n", " ") for value in values]
    return "| " + " | ".join(safe) + " |"


def clone_run_with_text(run: ET.Element, text: str, italic: bool = False) -> ET.Element:
    new_run = ET.Element(wtag("r"))
    rpr = run.find("./w:rPr", NS)
    if rpr is not None:
        new_run.append(deepcopy(rpr))
    if italic:
        rpr = new_run.find("./w:rPr", NS)
        if rpr is None:
            rpr = ET.Element(wtag("rPr"))
            new_run.insert(0, rpr)
        if rpr.find("./w:i", NS) is None:
            rpr.append(ET.Element(wtag("i")))
    text_node = ET.Element(wtag("t"))
    if text[:1].isspace() or text[-1:].isspace() or "  " in text:
        text_node.attrib[xml_attr("space")] = "preserve"
    text_node.text = text
    new_run.append(text_node)
    return new_run

