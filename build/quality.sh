#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIRECTORY="$(
    cd "$(dirname "${BASH_SOURCE[0]}")" || exit 1
    pwd -P
)"
REPOSITORY_ROOT="$(cd "$SCRIPT_DIRECTORY/.." && pwd -P)"

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
    echo "Usage: bash build/quality.sh"
    echo
    echo "Runs the same restore, build, test, pack, CLI, validation, and documentation checks as the PR quality workflow."
    exit 0
fi

if (( $# > 0 )); then
    echo "quality.sh does not accept arguments. Use --help for usage." >&2
    exit 2
fi

for requirement in dotnet python; do
    if ! command -v "$requirement" >/dev/null 2>&1; then
        echo "Required command '$requirement' was not found on PATH." >&2
        exit 1
    fi
done

if command -v pwsh >/dev/null 2>&1; then
    POWERSHELL="pwsh"
elif command -v powershell.exe >/dev/null 2>&1; then
    POWERSHELL="powershell.exe"
else
    echo "Required command 'pwsh' or 'powershell.exe' was not found on PATH." >&2
    exit 1
fi

QUALITY_TEMPORARY="$(mktemp -d "${TMPDIR:-/tmp}/paradigm-quality.XXXXXX")"
ARTIFACTS_DIRECTORY="$QUALITY_TEMPORARY/artifacts"
TOOLS_DIRECTORY="$QUALITY_TEMPORARY/tools"

cleanup() {
    if [[ -n "${QUALITY_TEMPORARY:-}" && -d "$QUALITY_TEMPORARY" ]]; then
        rm -rf -- "$QUALITY_TEMPORARY"
    fi
}
trap cleanup EXIT

step() {
    printf '\n==> %s\n' "$1"
}

cd "$REPOSITORY_ROOT"

step "Restore"
dotnet tool restore
dotnet restore src/Paradigm.Enterprise.slnx

step "Build and test source"
dotnet build src/Paradigm.Enterprise.slnx --configuration Release --no-restore
dotnet test src/Paradigm.Enterprise.slnx --configuration Release --no-build --filter "TestCategory!=Integration"

step "Pack current release and build example"
dotnet pack src/Paradigm.Enterprise.slnx --configuration Release --no-build --output "$ARTIFACTS_DIRECTORY"
dotnet restore example/ExampleApp.sln --property:RestoreAdditionalProjectSources="$ARTIFACTS_DIRECTORY"
dotnet build example/ExampleApp.sln --configuration Release --no-restore
dotnet test example/ExampleApp.sln --configuration Release --no-build

step "Install and test the packed Paradigm CLI"
VERSION="$("$POWERSHELL" -NoProfile -NonInteractive -Command '([xml](Get-Content -Raw "build/Paradigm.Version.props")).Project.PropertyGroup.ParadigmEnterpriseVersion')"
VERSION="${VERSION//$'\r'/}"
dotnet tool install --tool-path "$TOOLS_DIRECTORY" --add-source "$ARTIFACTS_DIRECTORY" --no-cache Paradigm.Enterprise.Cli --version "$VERSION"

PARADIGM="$TOOLS_DIRECTORY/paradigm"
if [[ -f "${PARADIGM}.exe" ]]; then
    PARADIGM="${PARADIGM}.exe"
fi

export PARADIGM_CLI_INTEGRATION_PACKAGE="$ARTIFACTS_DIRECTORY/Paradigm.Enterprise.Cli.$VERSION.nupkg"
export PARADIGM_CLI_INTEGRATION_EXECUTABLE="$PARADIGM"
dotnet test src/Paradigm.Enterprise.Cli.Tests/Paradigm.Enterprise.Cli.Tests.csproj --configuration Release --no-build --filter "TestCategory=Integration"

step "Deterministic quality checks"
"$PARADIGM" packages check --project src/Paradigm.Enterprise.slnx
"$PARADIGM" packages audit --project src/Paradigm.Enterprise.slnx
"$PARADIGM" packages check --project example/ExampleApp.sln
"$PARADIGM" packages audit --project example/ExampleApp.sln
"$PARADIGM" checks run --project src/Paradigm.Enterprise.Cli/Paradigm.Enterprise.Cli.csproj
"$PARADIGM" checks run --project src/Paradigm.Enterprise.Cli.Tests/Paradigm.Enterprise.Cli.Tests.csproj
"$PARADIGM" checks run --project src/Paradigm.Enterprise.Checks.CSharp/Paradigm.Enterprise.Checks.CSharp.csproj
"$PARADIGM" checks run --project src/Paradigm.Enterprise.CodeGenerator/Paradigm.Enterprise.CodeGenerator.csproj
"$PARADIGM" validate --project example/ExampleApp.sln
"$PARADIGM" checks run --project example/ExampleApp.sln
"$POWERSHELL" -NoProfile -NonInteractive -File build/verify-version.ps1

step "Validate skills and plugin"
python .github/scripts/validate-skills.py
python .github/scripts/validate-plugin.py

step "Build documentation"
dotnet docfx docs/docfx.json --warningsAsErrors

printf '\nPR quality checks completed successfully.\n'
