#!/usr/bin/env bash
set -eo pipefail

SCRIPT_DIRECTORY="$(
    cd "$(dirname "${BASH_SOURCE[0]}")" || exit 1
    pwd -P
)"
REPOSITORY_ROOT="$(cd "$SCRIPT_DIRECTORY/.." && pwd -P)"

. "$SCRIPT_DIRECTORY/_include.sh"

clear
block "Run Tests"

cd "$REPOSITORY_ROOT"
dotnet test src/Paradigm.Enterprise.slnx --filter "TestCategory!=Integration"

buildSuccessfully
