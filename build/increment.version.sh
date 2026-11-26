#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIRECTORY="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [[ $# -ne 2 ]]; then
    echo "Usage: bash build/increment.version.sh <old-version> <new-version>" >&2
    echo "Add the new release's CHANGELOG.md entry before running this command." >&2
    exit 2
fi
if ! command -v pwsh >/dev/null 2>&1; then
    echo "PowerShell 7 (pwsh) is required to update release metadata and templates." >&2
    exit 1
fi

pwsh -NoProfile -NonInteractive -File "$SCRIPT_DIRECTORY/set-version.ps1" \
    -ExpectedVersion "$1" -Version "$2"
