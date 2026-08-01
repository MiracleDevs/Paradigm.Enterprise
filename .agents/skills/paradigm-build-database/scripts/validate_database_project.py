#!/usr/bin/env python3
"""Read-only validation for Paradigm SQL Server and PostgreSQL projects."""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
import xml.etree.ElementTree as ET
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Any


@dataclass(frozen=True)
class Diagnostic:
    code: str
    severity: str
    path: str
    message: str


class Validator:
    def __init__(self, project: Path, solution: Path | None, strict: bool) -> None:
        self.project = project.resolve()
        self.root = self.project.parent
        self.solution = solution.resolve() if solution else None
        self.strict = strict
        self.diagnostics: list[Diagnostic] = []

    def add(self, code: str, severity: str, path: Path, message: str) -> None:
        try:
            display_path = str(path.resolve().relative_to(self.root))
        except ValueError:
            display_path = str(path.resolve())
        self.diagnostics.append(Diagnostic(code, severity, display_path, message))

    def policy(self, code: str, path: Path, message: str) -> None:
        self.add(code, "error" if self.strict else "warning", path, message)

    def validate(self) -> list[Diagnostic]:
        if not self.project.is_file():
            self.add("PEDB001", "error", self.project, "Database project does not exist.")
        elif self.project.suffix.lower() == ".sqlproj":
            self.validate_sql_server()
        elif self.project.suffix.lower() in {".json", ".jsonc"}:
            self.validate_postgresql()
        else:
            self.add(
                "PEDB001",
                "error",
                self.project,
                "Expected a .sqlproj or DbPublisher .json/.jsonc project.",
            )
        return sorted(
            self.diagnostics,
            key=lambda item: (item.code, item.path.lower(), item.message, item.severity),
        )

    def validate_solution_membership(self) -> None:
        if self.solution is None:
            return
        if not self.solution.is_file():
            self.add("PEDB005", "error", self.solution, "Solution file does not exist.")
            return
        text = self.solution.read_text(encoding="utf-8-sig", errors="replace")
        if self.project.name.lower() not in text.lower():
            self.policy(
                "PEDB005",
                self.solution,
                f"Solution does not reference {self.project.name}.",
            )

    def validate_layout(self) -> None:
        required = (
            "tables",
            "views",
            "functions",
            "routines",
            "types",
            "scripts/predeployment",
            "scripts/postdeployment",
            "scripts/maintenance",
        )
        for relative in required:
            path = self.root / relative
            if not path.is_dir():
                self.policy("PEDB004", path, f"Canonical database folder is missing: {relative}.")

    def validate_baseline(self) -> None:
        bootstrap = self.root / "bootstrap"
        baselines = sorted(bootstrap.glob("*.bacpac")) if bootstrap.is_dir() else []
        if len(baselines) > 1:
            self.add(
                "PEDB107",
                "error",
                bootstrap,
                "Keep at most one optional BACPAC baseline under bootstrap.",
            )
        for baseline in self.root.rglob("*.bacpac"):
            if baseline.parent != bootstrap:
                self.policy(
                    "PEDB107",
                    baseline,
                    "BACPAC baseline must be directly under the database bootstrap folder.",
                )

    def validate_sql_server(self) -> None:
        raw = self.project.read_text(encoding="utf-8-sig", errors="replace")
        try:
            xml_root = ET.fromstring(raw)
        except ET.ParseError as error:
            self.add("PEDB002", "error", self.project, f"Invalid SQL project XML: {error}.")
            return
        if "Microsoft.Build.Sql" not in raw:
            self.add(
                "PEDB002",
                "error",
                self.project,
                "Use the SDK-style Microsoft.Build.Sql project format.",
            )
        if "preview" in raw.lower():
            self.policy(
                "PEDB002",
                self.project,
                "New projects must not pin a preview Microsoft.Build.Sql SDK.",
            )

        self.validate_layout()
        self.validate_solution_membership()
        self.validate_baseline()
        self.validate_literal_secrets()
        self.validate_publish_profiles()
        self.validate_sql_includes(xml_root)
        self.validate_sql_objects(sql_server=True)
        self.validate_sql_deployment(raw)
        self.validate_adjacent_publish()

    def validate_sql_includes(self, xml_root: ET.Element) -> None:
        seen: set[str] = set()
        for element in xml_root.iter():
            kind = element.tag.rsplit("}", 1)[-1]
            if kind not in {"Build", "None", "PreDeploy", "PostDeploy"}:
                continue
            include = element.attrib.get("Include")
            if not include or "$(" in include:
                continue
            normalized = include.replace("\\", "/")
            key = normalized.lower()
            if key in seen:
                self.policy("PEDB105", self.project, f"Duplicate SQL project include: {include}.")
                continue
            seen.add(key)
            if any(character in normalized for character in "*?"):
                continue
            path = (self.root / normalized).resolve()
            if not path.is_file():
                self.policy("PEDB105", self.project, f"SQL project includes missing file: {include}.")

    def validate_publish_profiles(self) -> None:
        unsafe = (
            ("BlockOnPossibleDataLoss", False),
            ("DropObjectsNotInSource", True),
            ("AllowDropBlockingAssemblies", True),
        )
        profiles = sorted(self.root.rglob("*.publish.xml")) + sorted(self.root.rglob("*.pubxml"))
        for path in profiles:
            text = path.read_text(encoding="utf-8-sig", errors="replace")
            for property_name, unsafe_value in unsafe:
                match = re.search(
                    rf"<{property_name}>\s*(true|false)\s*</{property_name}>",
                    text,
                    re.IGNORECASE,
                )
                if match and (match.group(1).lower() == "true") == unsafe_value:
                    self.policy(
                        "PEDB108",
                        path,
                        f"Publish profile sets destructive {property_name}={match.group(1)}; require explicit reviewed use and do not make it an automatic default.",
                    )

    def validate_postgresql(self) -> None:
        raw = self.project.read_text(encoding="utf-8-sig", errors="replace")
        try:
            configuration = json.loads(strip_json_comments(raw))
        except (json.JSONDecodeError, ValueError) as error:
            self.add("PEDB201", "error", self.project, f"Invalid DbPublisher JSONC: {error}.")
            return
        if str(configuration.get("databaseType", "")).lower() != "postgresql":
            self.add(
                "PEDB201",
                "error",
                self.project,
                'DbPublisher databaseType must be "PostgreSql".',
            )
        connection_string = str(configuration.get("connectionString") or "").strip()
        if connection_string:
            self.add(
                "PEDB003",
                "error",
                self.project,
                "Remove connectionString and supply Paradigm_ORM_ConnectionString securely.",
            )

        generate_script = configuration.get("generateScript")
        execute_script = configuration.get("executeScript")
        if "executeScript" in configuration and not isinstance(execute_script, bool):
            self.add("PEDB201", "error", self.project, "DbPublisher executeScript must be Boolean.")
        output_file_name = str(configuration.get("outputFileName") or "").strip()
        if generate_script is not True:
            self.policy(
                "PEDB201",
                self.project,
                "Enable generateScript so the aggregate publication can be reviewed.",
            )
        if generate_script is True and not output_file_name:
            self.add("PEDB201", "error", self.project, "Set outputFileName when generateScript is enabled.")
        elif output_file_name:
            output_path = (self.root / output_file_name).resolve()
            if not output_path.is_relative_to(self.root):
                self.policy("PEDB201", self.project, "Keep generated publish output inside the database project.")

        files = configuration.get("files", [])
        if not isinstance(files, list) or not all(isinstance(item, str) for item in files):
            self.add("PEDB202", "error", self.project, "DbPublisher files must be a string array.")
            files = []
        paths = configuration.get("paths", [])
        if not isinstance(paths, list) or not all(isinstance(item, str) for item in paths):
            self.add("PEDB202", "error", self.project, "DbPublisher paths must be a string array.")
            paths = []
        top_directory_only = configuration.get("topDirectoryOnly", False)
        if not isinstance(top_directory_only, bool):
            self.add("PEDB201", "error", self.project, "DbPublisher topDirectoryOnly must be Boolean.")
            top_directory_only = False

        normalized: list[str] = []
        seen: set[str] = set()
        for configured in files:
            item = configured.replace("\\", "/")
            path = (self.root / item).resolve()
            if not path.is_file() or not path.is_relative_to(self.root):
                self.add("PEDB202", "error", self.project, f"Missing or escaping DbPublisher file: {item}.")
                continue
            self.add_postgresql_file(item, path, normalized, seen)

        for configured in paths:
            item = configured.replace("\\", "/")
            directory = (self.root / item).resolve()
            if not directory.is_dir() or not directory.is_relative_to(self.root):
                self.add("PEDB202", "error", self.project, f"Missing or escaping DbPublisher path: {item}.")
                continue
            candidates = directory.glob("*.sql") if top_directory_only else directory.rglob("*.sql")
            for path in sorted(candidates, key=lambda candidate: str(candidate).lower()):
                relative = path.relative_to(self.root).as_posix()
                self.add_postgresql_file(relative, path, normalized, seen)

        categories = [postgres_category(item) for item in normalized]
        relevant = [value for value in categories if value is not None]
        if relevant != sorted(relevant):
            self.add(
                "PEDB203",
                "error",
                self.project,
                "Order DbPublisher files as predeployment, tables, upgrades, views, routines/functions, postdeployment.",
            )

        self.validate_layout()
        self.validate_solution_membership()
        self.validate_baseline()
        self.validate_literal_secrets({self.project} if connection_string else None)
        self.validate_sql_objects(sql_server=False)

    def add_postgresql_file(
        self,
        item: str,
        path: Path,
        normalized: list[str],
        seen: set[str],
    ) -> None:
        key = str(path).lower()
        if key in seen:
            self.add("PEDB202", "error", self.project, f"Duplicate DbPublisher file: {item}.")
            return
        seen.add(key)
        normalized.append(item)

    def validate_literal_secrets(self, excluded: set[Path] | None = None) -> None:
        excluded = excluded or set()
        candidate_suffixes = {".json", ".jsonc", ".xml", ".config", ".pubxml"}
        secret_pattern = re.compile(
            r"[\"']?(?:password|pwd)[\"']?\s*[=:]\s*[\"']?(?!\$\(|\$\{|\[change\]|<|\*{3,})([^;\s\"']+)",
            re.IGNORECASE,
        )
        for path in sorted(self.root.rglob("*")):
            if path in excluded or not path.is_file() or path.suffix.lower() not in candidate_suffixes:
                continue
            text = path.read_text(encoding="utf-8-sig", errors="replace")
            if secret_pattern.search(text):
                self.add(
                    "PEDB003",
                    "error",
                    path,
                    "Configuration appears to contain a literal database password.",
                )

    def validate_sql_objects(self, sql_server: bool) -> None:
        self.validate_object_files(sql_server)
        table_root = self.root / "tables"
        for path in sorted(table_root.rglob("*.sql")) if table_root.is_dir() else []:
            text = path.read_text(encoding="utf-8-sig", errors="replace")
            names = extract_constraint_names(text)
            for name in names:
                prefix = name.split("_", 1)[0].upper()
                if prefix == "UX":
                    self.policy(
                        "PEDB101",
                        path,
                        f"Preserve legacy {name}, but use UQ_ for new unique constraints.",
                    )
                elif prefix not in {"PK", "FK", "UQ", "IX", "DF", "CK"}:
                    self.policy(
                        "PEDB101",
                        path,
                        f"Constraint {name} does not use a canonical prefix.",
                    )

            for keyword in ("PRIMARY KEY", "FOREIGN KEY"):
                total = len(re.findall(rf"\b{keyword}\b", text, re.IGNORECASE))
                named = len(
                    re.findall(
                        rf"\bCONSTRAINT\s+(?:\[[^]]+\]|\"[^\"]+\"|[A-Za-z_][\w$]*)\s+{keyword}\b",
                        text,
                        re.IGNORECASE,
                    )
                )
                if total > named:
                    self.policy(
                        "PEDB102",
                        path,
                        f"Name every {keyword.lower()} constraint explicitly.",
                    )

            if sql_server:
                default_total = len(re.findall(r"\bDEFAULT\b", text, re.IGNORECASE))
                named_defaults = len(
                    re.findall(
                        r"\bCONSTRAINT\s+(?:\[[^]]+\]|\"[^\"]+\"|[A-Za-z_][\w$]*)\s+DEFAULT\b",
                        text,
                        re.IGNORECASE,
                    )
                )
                if default_total > named_defaults:
                    self.policy("PEDB102", path, "Name every default constraint explicitly.")

            audit = ("CreatedByUserId", "CreationDate", "ModifiedByUserId", "ModificationDate")
            present = {column for column in audit if identifier_present(text, column)}
            if present and len(present) != len(audit):
                self.policy(
                    "PEDB103",
                    path,
                    "Use the complete canonical audit-field set or document why the table is not auditable.",
                )
            for column in present:
                if re.search(
                    rf"(?:\[{re.escape(column)}\]|\"{re.escape(column)}\"|\b{re.escape(column)}\b)[^,\r\n]*\bDEFAULT\b",
                    text,
                    re.IGNORECASE,
                ):
                    self.policy(
                        "PEDB103",
                        path,
                        f"Audit column {column} has a database default; verify that the database, not the Paradigm application, deliberately owns this value.",
                    )
            if identifier_present(text, "IsDeleted"):
                self.policy(
                    "PEDB103",
                    path,
                    "Use IsActive for new Paradigm logical lifecycle behavior; preserve IsDeleted only for compatibility.",
                )

    def validate_object_files(self, sql_server: bool) -> None:
        roots = ("tables", "views", "functions", "routines", "types")
        pattern = re.compile(
            r"\bCREATE\s+(?:OR\s+(?:ALTER|REPLACE)\s+)?(?:TABLE|VIEW|TYPE|FUNCTION|PROCEDURE|PROC)\s+"
            r"(?:IF\s+NOT\s+EXISTS\s+)?((?:\[[^]]+\]|\"[^\"]+\"|[A-Za-z_][\w$]*)(?:\."
            r"(?:\[[^]]+\]|\"[^\"]+\"|[A-Za-z_][\w$]*))?)",
            re.IGNORECASE,
        )
        for folder in roots:
            root = self.root / folder
            for path in sorted(root.rglob("*.sql")) if root.is_dir() else []:
                text = path.read_text(encoding="utf-8-sig", errors="replace")
                objects = pattern.findall(text)
                if len(objects) > 1:
                    self.policy("PEDB100", path, "Keep one semantic database object per file.")
                for qualified_name in objects:
                    parts = qualified_name.split(".")
                    if sql_server and len(parts) < 2:
                        self.policy("PEDB100", path, "SQL Server objects must use an explicit owned schema.")
                    object_name = parts[-1].strip('[]"')
                    if object_name.lower() != path.stem.lower():
                        self.policy(
                            "PEDB100",
                            path,
                            f"File name must match object {object_name}.",
                        )
        if sql_server:
            view_root = self.root / "views"
            for path in sorted(view_root.rglob("*.sql")) if view_root.is_dir() else []:
                text = path.read_text(encoding="utf-8-sig", errors="replace")
                if "SCHEMABINDING" not in text.upper():
                    self.policy(
                        "PEDB104",
                        path,
                        "SQL Server views should use SCHEMABINDING unless a documented dependency prevents it.",
                    )

        post_root = self.root / "scripts" / "postdeployment"
        for path in sorted(post_root.rglob("*.sql")) if post_root.is_dir() else []:
            if path.name.lower() == "postdeployment.sql":
                continue
            text = path.read_text(encoding="utf-8-sig", errors="replace")
            idempotent = re.search(
                r"\bMERGE\b|\bIF\s+NOT\s+EXISTS\b|\bON\s+CONFLICT\b|\bWHERE\s+NOT\s+EXISTS\b",
                text,
                re.IGNORECASE,
            )
            if not idempotent:
                self.add(
                    "PEDB106",
                    "warning",
                    path,
                    "Post-deployment data script has no recognized idempotency signal; review manually.",
                )
            if re.search(r"WHEN\s+NOT\s+MATCHED\s+BY\s+SOURCE\s+THEN\s+DELETE", text, re.IGNORECASE):
                self.add(
                    "PEDB106",
                    "warning",
                    path,
                    "Source-deleting MERGE requires an explicitly authoritative closed catalog.",
                )
            if re.search(r"\bON\s+CONFLICT\b[\s\S]*?\bDO\s+UPDATE\b", text, re.IGNORECASE) and not re.search(
                r"\bEXCLUDED\s*\.", text, re.IGNORECASE
            ):
                self.add(
                    "PEDB106",
                    "warning",
                    path,
                    "ON CONFLICT DO UPDATE does not reference EXCLUDED; verify that the update is not a no-op.",
                )

    def validate_sql_deployment(self, project_text: str) -> None:
        pre = self.root / "scripts" / "predeployment" / "PreDeployment.sql"
        post = self.root / "scripts" / "postdeployment" / "PostDeployment.sql"
        if pre.is_file() and not re.search(r"<PreDeploy\s+Include=", project_text, re.IGNORECASE):
            self.policy("PEDB105", self.project, "Register PreDeployment.sql as PreDeploy.")
        if post.is_file() and not re.search(r"<PostDeploy\s+Include=", project_text, re.IGNORECASE):
            self.policy("PEDB105", self.project, "Register PostDeployment.sql as PostDeploy.")

        subordinate = []
        post_root = post.parent
        if post_root.is_dir():
            subordinate = [
                path for path in sorted(post_root.rglob("*.sql")) if path.name.lower() != "postdeployment.sql"
            ]
        post_text = post.read_text(encoding="utf-8-sig", errors="replace") if post.is_file() else ""
        build_remove_post = bool(
            re.search(r'<Build\s+Remove="[^"]*postdeployment[^"]*"', project_text, re.IGNORECASE)
        )
        for path in subordinate:
            if path.name.lower() not in post_text.lower():
                self.policy("PEDB105", path, "PostDeployment.sql does not include this data script with :r.")
            if not build_remove_post:
                self.policy("PEDB105", path, "Exclude subordinate post-deployment scripts from model build.")

        maintenance = self.root / "scripts" / "maintenance"
        if maintenance.is_dir() and any(maintenance.rglob("*.sql")):
            if not re.search(r'<Build\s+Remove="[^"]*maintenance[^"]*"', project_text, re.IGNORECASE):
                self.policy("PEDB105", self.project, "Exclude maintenance scripts from model build.")

        scripts_root = self.root / "scripts"
        canonical = {"predeployment", "postdeployment", "maintenance"}
        if scripts_root.is_dir():
            for child in sorted(scripts_root.iterdir()):
                if child.is_dir() and child.name.lower() not in canonical and any(child.rglob("*.sql")):
                    self.policy(
                        "PEDB105",
                        child,
                        "Noncanonical deployment-script folder needs an explicit idempotent executor and verified ordering; DACPAC PreDeploy may be too late for pre-plan work.",
                    )

    def validate_adjacent_publish(self) -> None:
        if self.solution is None or not self.solution.is_file():
            return
        common = Path(os.path.commonpath((self.root, self.solution.parent)))
        if len(common.parts) < 4 or not self.root.is_relative_to(common):
            return
        patterns = (
            (
                re.compile(r"BlockOnPossibleDataLoss\s*[=:]\s*(?:False|false|0)"),
                "Automatic database publication must not disable possible-data-loss blocking.",
            ),
            (
                re.compile(r"tool\s+install[^\r\n]*(?:--global|-g)[^\r\n]*sqlpackage", re.IGNORECASE),
                "Do not install SqlPackage globally from a running bootstrap; use the repository-local tool manifest.",
            ),
        )
        ignored = {"bin", "obj", ".git", ".vs", "node_modules", "dist", "artifacts"}
        candidates: list[Path] = []
        for directory, directories, files in os.walk(common):
            directories[:] = [item for item in directories if item.lower() not in ignored]
            folder = Path(directory)
            relative_parts = folder.relative_to(common).parts
            if not any(
                token in part.lower()
                for part in relative_parts
                for token in ("bootstrap", "apphost", "publish")
            ):
                continue
            for file_name in files:
                path = folder / file_name
                if path.suffix.lower() in {".cs", ".ps1", ".json", ".jsonc"}:
                    candidates.append(path)
        for path in sorted(candidates):
            text = path.read_text(encoding="utf-8-sig", errors="replace")
            for pattern, message in patterns:
                if pattern.search(text):
                    self.add("PEDB108", "error", path, message)


def strip_json_comments(text: str) -> str:
    result: list[str] = []
    index = 0
    in_string = False
    escaped = False
    while index < len(text):
        char = text[index]
        next_char = text[index + 1] if index + 1 < len(text) else ""
        if in_string:
            result.append(char)
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                in_string = False
            index += 1
            continue
        if char == '"':
            in_string = True
            result.append(char)
            index += 1
        elif char == "/" and next_char == "/":
            index += 2
            while index < len(text) and text[index] not in "\r\n":
                index += 1
        elif char == "/" and next_char == "*":
            end = text.find("*/", index + 2)
            if end < 0:
                raise ValueError("unterminated block comment")
            index = end + 2
        else:
            result.append(char)
            index += 1
    return "".join(result)


def postgres_category(path: str) -> int | None:
    normalized = path.lower().replace("\\", "/")
    if "/predeployment/" in f"/{normalized}":
        return 0
    if normalized.startswith("tables/"):
        return 1
    if normalized.startswith("upgrades/") or "/upgrades/" in f"/{normalized}":
        return 2
    if normalized.startswith("views/"):
        return 3
    if normalized.startswith("functions/") or normalized.startswith("routines/"):
        return 4
    if "/postdeployment/" in f"/{normalized}":
        return 5
    return None


def extract_constraint_names(text: str) -> list[str]:
    pattern = re.compile(
        r"\bCONSTRAINT\s+(?:\[([^]]+)\]|\"([^\"]+)\"|([A-Za-z_][\w$]*))",
        re.IGNORECASE,
    )
    return [next(group for group in match.groups() if group) for match in pattern.finditer(text)]


def identifier_present(text: str, identifier: str) -> bool:
    return bool(
        re.search(
            rf"(?:\[{re.escape(identifier)}\]|\"{re.escape(identifier)}\"|\b{re.escape(identifier)}\b)",
            text,
            re.IGNORECASE,
        )
    )


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", required=True, type=Path)
    parser.add_argument("--solution", type=Path)
    parser.add_argument("--strict", action="store_true")
    parser.add_argument("--format", choices=("text", "json"), default="text")
    return parser.parse_args()


def render_text(diagnostics: list[Diagnostic]) -> str:
    if not diagnostics:
        return "No database project diagnostics."
    return "\n".join(
        f"{item.severity.upper()} {item.code} {item.path}: {item.message}"
        for item in diagnostics
    )


def main() -> int:
    args = parse_args()
    diagnostics = Validator(args.project, args.solution, args.strict).validate()
    if args.format == "json":
        print(json.dumps([asdict(item) for item in diagnostics], indent=2))
    else:
        print(render_text(diagnostics))
    return 1 if any(item.severity == "error" for item in diagnostics) else 0


if __name__ == "__main__":
    raise SystemExit(main())
