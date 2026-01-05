#!/bin/bash
. _include.sh

#------------------------------------------------
# INIT VARS
#------------------------------------------------
index=$1
noclsArg=$2

if [[ "$noclsArg" == "" ]]; then clear; fi
if [[ "$index"    == "" ]]; then index="1"; fi

block "$index - Build Solution"

#------------------------------------------------
# BUILD SOLUTION
#------------------------------------------------
execute "dotnet restore ../src/Paradigm.Enterprise.slnx -v q"
execute "dotnet build ../src/Paradigm.Enterprise.slnx -c Release -v q"

buildSuccessfully
