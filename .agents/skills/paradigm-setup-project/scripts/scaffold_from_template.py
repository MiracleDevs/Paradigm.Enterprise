#!/usr/bin/env python3
"""Scaffold a Paradigm solution from the reviewed Web API template."""

from __future__ import annotations

import argparse
import json
import re
import shutil
import sys
import uuid
from pathlib import Path


TEMPLATE_NAME = "Paradigm.Web.ApiTemplate"
TEXT_SUFFIXES = {
    "",
    ".config",
    ".cs",
    ".csproj",
    ".css",
    ".editorconfig",
    ".gitignore",
    ".html",
    ".json",
    ".md",
    ".props",
    ".ps1",
    ".resx",
    ".sh",
    ".sln",
    ".slnx",
    ".t4",
    ".targets",
    ".txt",
    ".vstemplate",
    ".xml",
}
EXCLUDED_PARTS = {".git", ".vs", "bin", "obj"}
NAME_PATTERN = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*$")
GUID_PATTERN = re.compile(
    r"\{[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\}"
)
PARADIGM_PACKAGE_PATTERN = re.compile(
    r'(<PackageReference\s+Include="Paradigm\.Enterprise\.[^"]+"\s+Version=")[^"]+("\s*/?>)',
    re.IGNORECASE,
)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--template-root", required=True, type=Path)
    parser.add_argument("--name", required=True)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--paradigm-version", required=True)
    parser.add_argument("--dry-run", action="store_true")
    return parser.parse_args()


def resolve_source(template_root: Path) -> Path:
    root = template_root.resolve()
    source = root / "src" if (root / "src").is_dir() else root
    if not source.is_dir() or not any(source.glob("*.sln*")):
        raise ValueError(f"Template source does not contain a solution: {source}")
    return source


def validate_destination(source: Path, output: Path, dry_run: bool) -> Path:
    destination = output.resolve()
    if destination == source or destination.is_relative_to(source):
        raise ValueError("Output must not be the template source or one of its descendants.")
    if destination.exists():
        if not destination.is_dir():
            raise ValueError(f"Output is not a directory: {destination}")
        if any(destination.iterdir()):
            raise ValueError(f"Output directory must be empty: {destination}")
    elif not dry_run and not destination.parent.exists():
        destination.parent.mkdir(parents=True, exist_ok=True)
    return destination


def replace_name(value: str, solution_name: str) -> str:
    return (
        value.replace(TEMPLATE_NAME, solution_name)
        .replace("$ext_safeprojectname$", solution_name)
        .replace("$safeprojectname$", solution_name)
    )


def transform_text(
    text: str,
    solution_name: str,
    paradigm_version: str,
    guid_map: dict[str, str],
) -> str:
    transformed = replace_name(text, solution_name)
    transformed = PARADIGM_PACKAGE_PATTERN.sub(
        rf"\g<1>{paradigm_version}\g<2>", transformed
    )

    def replace_guid(match: re.Match[str]) -> str:
        key = match.group(0).lower()
        if key not in guid_map:
            guid_map[key] = "{" + str(uuid.uuid4()).upper() + "}"
        return guid_map[key]

    return GUID_PATTERN.sub(replace_guid, transformed)


def collect_files(source: Path, solution_name: str) -> list[tuple[Path, Path]]:
    files: list[tuple[Path, Path]] = []
    for item in sorted(source.rglob("*"), key=lambda path: str(path).lower()):
        relative = item.relative_to(source)
        if any(part.lower() in EXCLUDED_PARTS for part in relative.parts):
            continue
        if item.is_file():
            target = Path(*(replace_name(part, solution_name) for part in relative.parts))
            files.append((item, target))
    return files


def scaffold(
    template_root: Path,
    solution_name: str,
    output: Path,
    paradigm_version: str,
    dry_run: bool,
) -> list[str]:
    if not NAME_PATTERN.fullmatch(solution_name):
        raise ValueError("Name must be a valid dot-separated C# namespace.")
    if not re.fullmatch(r"[0-9A-Za-z][0-9A-Za-z.+-]*", paradigm_version):
        raise ValueError("Paradigm version contains unsupported characters.")

    source = resolve_source(template_root)
    destination = validate_destination(source, output, dry_run)
    planned = collect_files(source, solution_name)
    if not planned:
        raise ValueError("Template source contains no files.")

    guid_map: dict[str, str] = {}
    targets: list[str] = []
    if not dry_run:
        destination.mkdir(parents=True, exist_ok=True)

    for source_file, relative_target in planned:
        target = destination / "src" / relative_target
        targets.append(str(target))
        if dry_run:
            if source_file.suffix.lower() in TEXT_SUFFIXES:
                transform_text(
                    source_file.read_text(encoding="utf-8-sig"),
                    solution_name,
                    paradigm_version,
                    guid_map,
                )
            continue

        target.parent.mkdir(parents=True, exist_ok=True)
        if source_file.suffix.lower() in TEXT_SUFFIXES:
            text = source_file.read_text(encoding="utf-8-sig")
            transformed = transform_text(text, solution_name, paradigm_version, guid_map)
            target.write_bytes(transformed.encode("utf-8"))
        else:
            shutil.copy2(source_file, target)

    return targets


def main() -> int:
    args = parse_args()
    try:
        targets = scaffold(
            args.template_root,
            args.name,
            args.output,
            args.paradigm_version,
            args.dry_run,
        )
    except (OSError, UnicodeError, ValueError) as error:
        print(f"error: {error}", file=sys.stderr)
        return 1

    print(
        json.dumps(
            {
                "dryRun": args.dry_run,
                "fileCount": len(targets),
                "output": str(args.output.resolve()),
                "files": targets,
            },
            indent=2,
        )
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
