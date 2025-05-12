#!/bin/bash
# shellcheck disable=SC2059
# shellcheck disable=SC2046
CURRENT_PATH=$(
    cd "$(dirname "${BASH_SOURCE[0]}")" || exit
    pwd -P
)

clear

# define color variables.
C_DARK_GRAY='\033[1;30m'
C_LIGHT_RED='\033[1;31m'
C_LIGHT_GREEN='\033[1;32m'
C_YELLOW='\033[1;33m'
C_WHITE='\033[1;37m'
C_RESET='\e[0m'


# define helper methods
function newline() {
    printf "${C_RESET}\\n"
}

function header() {
    newline
    echo -e "${C_WHITE}${1}"
    echo -e "${C_LIGHT_GREEN}---------------------------------------------------------------------------------------"
}

function footer() {
    newline
    echo -e "${C_LIGHT_GREEN}---------------------------------------------------------------------------------------"
    echo -e "${C_RESET}${1}"
    newline
}

function message() {
    echo -e "${C_DARK_GRAY}[$(date +"%d/%m/%Y %H:%M:%S")] ${C_RESET}${1}"
}

function error() {
    echo -e "${C_DARK_GRAY}[$(date +"%d/%m/%Y %H:%M:%S")] ${C_LIGHT_RED}ERROR: ${1}${C_RESET}"
}

function optional() {
    printf "${C_RESET}[${1}${C_RESET}] "
}

function argument() {
    printf "${C_LIGHT_GREEN}${1} ${C_LIGHT_GREEN}${2}${C_RESET}${3}"
}

function check_command() {
    if [ -z "$(command -v "$1")" ]; then
        error " - The command $1 is not installed in the system."
        exit 1
    fi

    message " - The command ${C_LIGHT_GREEN}$1${C_RESET} is installed in this system."
}

function check_dotnet_tool() {
    RESULT=$(dotnet tool list "$1" --global)
    if [[ ! $RESULT == *"$1"* ]]; then
        message "${C_YELLOW}The command ${C_LIGHT_GREEN}$1${C_YELLOW} is not installed in the system.${C_RESET}"
        message " - Trying to install ${C_LIGHT_GREEN}$1${C_RESET}..."
        dotnet tool install --global "$1"
    fi

    message " - The command ${C_LIGHT_GREEN}$1${C_RESET} is installed in this system."
}

################################################################################################
# CHECK PREREQUISITES
################################################################################################
newline
header "Checking required software:"
check_command "dotnet"

newline
header "Loading files from: ${DOCKER_COMPOSE_FILE}"

################################################################################################
# import the environment variables
################################################################################################
if [ -f .env ]; then
    header "Loading the enviroment variables.."
    set -o allexport
    source .env set
    set +o allexport
    message " - Variables loaded"
fi

pushd "$CURRENT_PATH/src/" || error "Failed to pushd to ./src/Paradigm.Enterprise.Api"
dotnet test || error "Failed to run tests"
popd || error "Failed to popd from ./src/Paradigm.Enterprise.Api"


################################################################################################
# FINISH THE PROCESS
################################################################################################
footer "Operation finished."
