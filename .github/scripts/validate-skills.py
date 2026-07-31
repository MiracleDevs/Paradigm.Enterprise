from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[2]
errors = []
canonical = root / ".agents" / "skills"
redirects = root / "skills"
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
if errors:
    print("\n".join(errors), file=sys.stderr)
    raise SystemExit(1)
print(f"Validated {len(list(canonical.iterdir()))} Paradigm skills.")
