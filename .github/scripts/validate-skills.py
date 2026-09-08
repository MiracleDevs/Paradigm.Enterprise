from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[2]
errors = []
canonical = root / ".agents" / "skills"
redirects = root / "skills"
good_practices = root / ".agents" / "references" / "good-coding-practices.md"
database_practices = root / ".agents" / "references" / "database-practices.md"
solution_layout = root / ".agents" / "references" / "solution-layout.md"
good_practices_reference = "../../references/good-coding-practices.md"
solution_layout_reference = "../../references/solution-layout.md"
if not good_practices.exists():
    errors.append("missing canonical Paradigm Good Coding Practices reference")
else:
    good_practices_text = good_practices.read_text(encoding="utf-8")
    required_practices = [
        "one top-level semantic type per file",
        "AccountingDbContext",
        "least visibility",
        "#region Nested Types",
        "#region Static Constructors",
        "#region Event Handlers",
        "exactly one empty line between adjacent regions",
        "localStorage",
        "explicit permission",
        "ActivitySource",
        "AddHealthChecks",
    ]
    for practice in required_practices:
        if practice not in good_practices_text:
            errors.append(f"Good Coding Practices is missing required guidance: {practice}")
if not database_practices.exists():
    errors.append("missing canonical Paradigm Database Practices reference")
else:
    database_practices_text = database_practices.read_text(encoding="utf-8")
    required_database_practices = [
        "auto-incrementing `Id`",
        "stable, explicitly assigned identifiers",
        "service-side .NET enum",
        "<Entity>StatusHistory",
        "scripts/prepredeployment/PrePreDeployment.sql",
    ]
    for practice in required_database_practices:
        if practice not in database_practices_text:
            errors.append(f"Database Practices is missing required guidance: {practice}")
if not solution_layout.exists():
    errors.append("missing canonical Paradigm Solution Layout reference")
else:
    solution_layout_text = solution_layout.read_text(encoding="utf-8")
    required_solution_layout = [
        "00.SolutionItems",
        "01.Shared",
        "02.Modules",
        "03.Hosts",
        "04.Tools",
        "05.Tests",
        "database schema project is a module/data asset",
        "Solution folders are navigation and ownership metadata",
        "one canonical application solution",
        "listed `.csproj` projects as managed assemblies",
        "`.sqlproj` output or DACPAC",
        "database validate",
    ]
    for requirement in required_solution_layout:
        if requirement not in solution_layout_text:
            errors.append(f"Solution Layout is missing required guidance: {requirement}")
for skill_name in ["paradigm-setup-project", "paradigm-review-change", "paradigm-evolve-guidance"]:
    skill_text = (canonical / skill_name / "SKILL.md").read_text(encoding="utf-8")
    if solution_layout_reference not in skill_text:
        errors.append(f"{skill_name}: does not reference Paradigm Solution Layout")
retired_skill_helpers = [
    root / ".agents" / "skills" / "paradigm-setup-project" / "scripts" / "scaffold_from_template.py",
    root / ".agents" / "skills" / "paradigm-build-database" / "scripts" / "validate_database_project.py",
    root / ".github" / "scripts" / "test-skill-tools.py",
]
for helper in retired_skill_helpers:
    if helper.exists():
        errors.append(f"retired Python helper still exists; use Paradigm.Enterprise.Cli: {helper.relative_to(root)}")
for skill in sorted(canonical.iterdir()):
    source = skill / "SKILL.md"
    ui = skill / "agents" / "openai.yaml"
    redirect = redirects / skill.name / "SKILL.md"
    if not source.exists() or not ui.exists() or not redirect.exists():
        errors.append(f"{skill.name}: missing SKILL.md, agents/openai.yaml, or packaging redirect")
        continue
    text = source.read_text(encoding="utf-8")
    if not re.match(r"^---\nname: [a-z0-9-]+\ndescription: .+\n---\n", text):
        errors.append(f"{skill.name}: invalid frontmatter")
    if len(text.splitlines()) > 500:
        errors.append(f"{skill.name}: SKILL.md exceeds 500 lines")
    if f"name: {skill.name}" not in text:
        errors.append(f"{skill.name}: folder and skill name differ")
    if good_practices_reference not in text:
        errors.append(f"{skill.name}: does not reference Paradigm Good Coding Practices")
    redirect_text = redirect.read_text(encoding="utf-8")
    expected_redirect = f"../../.agents/skills/{skill.name}/SKILL.md"
    if expected_redirect not in redirect_text:
        errors.append(f"{skill.name}: packaging redirect does not target the canonical skill")
aspire_assets = canonical / "paradigm-setup-aspire" / "assets" / "sql-server-bootstrap"
dockerfile = aspire_assets / "Dockerfile"
dockerignore = aspire_assets / "Dockerfile.dockerignore"
if not dockerfile.exists() or not dockerignore.exists():
    errors.append("paradigm-setup-aspire: missing governed SQL Server bootstrap container assets")
else:
    dockerfile_text = dockerfile.read_text(encoding="utf-8")
    for required in [
        "DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0.302-noble",
        "DOTNET_RUNTIME_IMAGE=mcr.microsoft.com/dotnet/runtime:10.0.10-noble",
        "SQLPACKAGE_VERSION=170.4.83",
        "Microsoft.SqlPackage",
        "mssql-tools18",
        "dotnet build \"${DATABASE_PROJECT}\"",
        "USER ${APP_UID}",
    ]:
        if required not in dockerfile_text:
            errors.append(f"paradigm-setup-aspire: SQL Server bootstrap Dockerfile is missing {required}")
    dockerignore_text = dockerignore.read_text(encoding="utf-8")
    for required in [".git/", ".env", "**/artifacts/", "**/bin/", "**/obj/"]:
        if required not in dockerignore_text:
            errors.append(f"paradigm-setup-aspire: SQL Server bootstrap Dockerfile.dockerignore is missing {required}")
if errors:
    print("\n".join(errors), file=sys.stderr)
    raise SystemExit(1)
print(f"Validated {len(list(canonical.iterdir()))} Paradigm skills.")
