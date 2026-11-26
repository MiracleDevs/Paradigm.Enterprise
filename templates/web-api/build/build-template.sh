#!/bin/bash
# Build the native .NET template package and optionally install it.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
PACKAGE_PROJECT="$ROOT_DIR/Paradigm.Enterprise.Templates.WebApi.csproj"
PACKAGE_OUTPUT="$ROOT_DIR/artifacts/packages"

usage() {
    echo "Usage: $0 [--no-install | --install-only | --help]"
    echo ""
    echo "  --no-install    Pack the template without installing it"
    echo "  --install-only  Install the package built for the current framework version"
    echo "  --help          Show this help message"
    echo ""
    echo "Without options, builds and installs the native dotnet new template."
}

NO_INSTALL=false
INSTALL_ONLY=false
for argument in "$@"; do
    case "$argument" in
        --no-install) NO_INSTALL=true ;;
        --install-only) INSTALL_ONLY=true ;;
        --help) usage; exit 0 ;;
        *) echo "Unknown option: $argument" >&2; usage >&2; exit 2 ;;
    esac
done

if [[ "$NO_INSTALL" == true && "$INSTALL_ONLY" == true ]]; then
    echo "--no-install and --install-only cannot be combined." >&2
    exit 2
fi

if [[ "$INSTALL_ONLY" != true ]]; then
    dotnet pack "$PACKAGE_PROJECT" --configuration Release \
        --output "$PACKAGE_OUTPUT" --disable-build-servers
fi

if [[ "$NO_INSTALL" != true ]]; then
    bash "$SCRIPT_DIR/install-template.sh"
fi
