#!/bin/bash
# Install the locally built native .NET template package.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
PACKAGE_PROJECT="$ROOT_DIR/Paradigm.Enterprise.Templates.WebApi.csproj"

if [[ "${1:-}" == "--help" ]]; then
    echo "Usage: $0"
    echo "Install the native dotnet new template package built for the current framework version."
    exit 0
fi
if [[ $# -ne 0 ]]; then
    echo "Unexpected argument: $1" >&2
    exit 2
fi

PACKAGE_VERSION="$(dotnet msbuild "$PACKAGE_PROJECT" -nologo -getProperty:PackageVersion)"
PACKAGE_FILE="$ROOT_DIR/artifacts/packages/Paradigm.Enterprise.Templates.WebApi.$PACKAGE_VERSION.nupkg"
if [[ ! -f "$PACKAGE_FILE" ]]; then
    echo "Template package not found: $PACKAGE_FILE" >&2
    echo "Run bash templates/web-api/build/build-template.sh --no-install from the repository root first." >&2
    exit 1
fi

dotnet new install "$PACKAGE_FILE" --force
