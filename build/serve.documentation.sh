#!/usr/bin/env bash
set -eo pipefail

SCRIPT_DIRECTORY="$(
    cd "$(dirname "${BASH_SOURCE[0]}")" || exit 1
    pwd -P
)"
REPOSITORY_ROOT="$(cd "$SCRIPT_DIRECTORY/.." && pwd -P)"

. "$SCRIPT_DIRECTORY/_include.sh"

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
    echo "Usage: bash build/serve.documentation.sh [port]"
    echo
    echo "Builds the libraries and documentation, then serves the site."
    echo "The default address is http://localhost:8080."
    echo "Set DOCS_HOST to bind to a different hostname."
    exit 0
fi

DOCS_PORT="${1:-8080}"
DOCS_HOST="${DOCS_HOST:-localhost}"

if [[ ! "$DOCS_PORT" =~ ^[0-9]+$ ]] || ((10#$DOCS_PORT < 1 || 10#$DOCS_PORT > 65535)); then
    error "Port must be a number between 1 and 65535."
    exit 1
fi

clear
block "Build and Serve Documentation"

cd "$REPOSITORY_ROOT"

block "Restore Docfx"
dotnet tool restore

block "Restore Libraries"
dotnet restore src/Paradigm.Enterprise.slnx

block "Build Libraries and XML Documentation"
dotnet build \
    src/Paradigm.Enterprise.slnx \
    --configuration Release \
    --no-restore \
    --property:GenerateDocumentationFile=true \
    --property:NoWarn=1591%3B1572%3B1573%3B1574

block "Serve Documentation at http://${DOCS_HOST}:${DOCS_PORT}"
exec dotnet docfx \
    docs/docfx.json \
    --warningsAsErrors \
    --serve \
    --hostname "$DOCS_HOST" \
    --port "$DOCS_PORT"
