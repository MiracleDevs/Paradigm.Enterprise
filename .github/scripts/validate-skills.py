from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[2]
errors = []
canonical = root / ".agents" / "skills"
redirects = root / "skills"
good_practices = root / ".agents" / "references" / "good-coding-practices.md"
good_practices_reference = "../../references/good-coding-practices.md"
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
        "localStorage",
        "explicit permission",
        "ActivitySource",
        "AddHealthChecks",
    ]
    for practice in required_practices:
        if practice not in good_practices_text:
            errors.append(f"Good Coding Practices is missing required guidance: {practice}")
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
if errors:
    print("\n".join(errors), file=sys.stderr)
    raise SystemExit(1)
print(f"Validated {len(list(canonical.iterdir()))} Paradigm skills.")
