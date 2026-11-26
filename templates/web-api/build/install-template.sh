#!/bin/bash
# Install the locally built native .NET template package.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
PACKAGE_PROJECT="$ROOT_DIR/Paradigm.Enterprise.Templates.WebApi.csproj"
PACKAGE_ID="Paradigm.Enterprise.Templates.WebApi"
TEMPLATE_ARGUMENTS=()

while [[ $# -gt 0 ]]; do
    case "$1" in
        --hive)
            if [[ $# -lt 2 || -z "$2" ]]; then
                echo "--hive requires an isolated template cache directory." >&2
                exit 2
            fi
            TEMPLATE_ARGUMENTS=(--debug:custom-hive "$2")
            shift 2
            ;;
        --help)
            echo "Usage: $0 [--hive <directory>]"
            echo "Replace the installed template package with the local build for the current framework version."
            echo "Use --hive for isolated installation tests."
            exit 0
            ;;
        *) echo "Unexpected argument: $1" >&2; exit 2 ;;
    esac
done

PACKAGE_VERSION="$(dotnet msbuild "$PACKAGE_PROJECT" -nologo -getProperty:PackageVersion)"
PACKAGE_VERSION="${PACKAGE_VERSION//$'\r'/}"
PACKAGE_FILE="$ROOT_DIR/artifacts/packages/$PACKAGE_ID.$PACKAGE_VERSION.nupkg"
if [[ ! -f "$PACKAGE_FILE" ]]; then
    echo "Template package not found: $PACKAGE_FILE" >&2
    echo "Run bash templates/web-api/build/build-template.sh --no-install from the repository root first." >&2
    exit 1
fi

# Replacing a local package with --force can leave duplicate registrations in the
# template engine. Explicit removal also picks up rebuilt content at the same version.
INSTALLED_PACKAGES="$(dotnet new "${TEMPLATE_ARGUMENTS[@]}" uninstall)"
while IFS= read -r line; do
    line="${line//$'\r'/}"
    if [[ "$line" =~ ^[[:space:]]*Paradigm\.Enterprise\.Templates\.WebApi[[:space:]]*$ ]]; then
        dotnet new "${TEMPLATE_ARGUMENTS[@]}" uninstall "$PACKAGE_ID"
        break
    fi
done <<< "$INSTALLED_PACKAGES"

dotnet new "${TEMPLATE_ARGUMENTS[@]}" install "$PACKAGE_FILE"
