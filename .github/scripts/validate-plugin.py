from pathlib import Path
import json
import re

root = Path(__file__).resolve().parents[2]
plugin = json.loads((root / ".codex-plugin" / "plugin.json").read_text(encoding="utf-8"))
props = (root / "build" / "Paradigm.Version.props").read_text(encoding="utf-8")
version = re.search(r"<ParadigmEnterpriseVersion>([^<]+)", props).group(1)
if plugin.get("version") != version:
    raise SystemExit(f"plugin version {plugin.get('version')} != {version}")
if plugin.get("skills") != "./.agents/skills/":
    raise SystemExit("plugin skills path must be ./.agents/skills/")
print(f"Validated plugin metadata at {version}.")
