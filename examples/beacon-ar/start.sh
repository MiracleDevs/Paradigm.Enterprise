#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
PARADIGM_CLI_VERSION="1.1.0"
SQLCMD_MAJOR_VERSION="18"
APPHOST_PROJECT="${ROOT}/src/BeaconAr.AppHost/BeaconAr.AppHost.csproj"
LOCAL_PACKAGES="$(cd "${ROOT}/../.." && pwd -P)/artifacts"

usage() {
    echo "Usage: ./start.sh [start|stop|doctor|help]"
    echo "  start   Start the Aspire development environment (default)"
    echo "  stop    Stop Aspire-managed resources"
    echo "  doctor  Check Paradigm, Aspire, and Docker prerequisites"
}

fail() {
    echo "error: $*" >&2
    exit 1
}

ensure_dotnet() {
    command -v dotnet >/dev/null 2>&1 || fail ".NET SDK is required. Install .NET 10 and retry."
    dotnet --list-sdks | awk '{print $1}' | grep -Eq '^10\.' || fail ".NET 10 SDK is required. Install it and retry."
}

require_apphost() {
    [[ -f "${APPHOST_PROJECT}" ]] || fail "Aspire AppHost was not found at ${APPHOST_PROJECT}."
}

ensure_manifest() {
    [[ -f "${ROOT}/.config/dotnet-tools.json" ]] || fail "The pinned repository-local tool manifest is missing."
}

restore_tools() {
    if [[ -d "${LOCAL_PACKAGES}" ]]; then
        dotnet tool restore --add-source "${LOCAL_PACKAGES}" >/dev/null
    else
        dotnet tool restore >/dev/null
    fi
}

ensure_tools() {
    ensure_dotnet
    ensure_manifest
    restore_tools
    dotnet tool run paradigm --version | grep -Fq "${PARADIGM_CLI_VERSION}" || fail "Paradigm CLI ${PARADIGM_CLI_VERSION} is required."
    dotnet tool run aspire --version >/dev/null
    dotnet tool run sqlpackage /Version >/dev/null
    sqlcmd_path="${Database__SqlCmdPath:-sqlcmd}"
    if ! command -v "${sqlcmd_path}" >/dev/null 2>&1; then
        [[ -x /opt/mssql-tools18/bin/sqlcmd ]] || fail "Microsoft SQLCMD ${SQLCMD_MAJOR_VERSION} is required. Install mssql-tools18 and retry."
        sqlcmd_path=/opt/mssql-tools18/bin/sqlcmd
    fi
    sqlcmd_help="$("${sqlcmd_path}" -? 2>&1 || true)"
    grep -Eq "^Version ${SQLCMD_MAJOR_VERSION}\." <<<"${sqlcmd_help}" || fail "Microsoft SQLCMD major version ${SQLCMD_MAJOR_VERSION} is required."
    export Database__SqlCmdPath="${sqlcmd_path}"
    export Database__SqlCmdMajorVersion="${SQLCMD_MAJOR_VERSION}"
}

wait_for_docker() {
    while ! command -v docker >/dev/null 2>&1 || ! docker info >/dev/null 2>&1; do
        echo "Docker is missing or its daemon is not running."
        if [[ ! -t 0 ]]; then
            fail "Start/install Docker and rerun this command from an interactive terminal."
        fi
        read -r -p "Install or start Docker, then press Enter to retry (q to quit): " answer
        [[ "${answer}" != "q" && "${answer}" != "Q" ]] || exit 1
    done
}

cd "${ROOT}"
action="${1:-start}"
case "${action}" in
    -h|--help|help)
        usage
        ;;
    start)
        ensure_tools
        wait_for_docker
        require_apphost
        dotnet tool run aspire -- run --apphost "${APPHOST_PROJECT}" --non-interactive --nologo
        ;;
    stop)
        ensure_tools
        require_apphost
        dotnet tool run aspire -- stop --apphost "${APPHOST_PROJECT}" --non-interactive --nologo
        ;;
    doctor)
        ensure_tools
        wait_for_docker
        dotnet tool run paradigm doctor --project src/BeaconAr.sln
        dotnet tool run aspire -- doctor --non-interactive
        ;;
    *)
        usage
        fail "Unknown action '${action}'."
        ;;
esac
