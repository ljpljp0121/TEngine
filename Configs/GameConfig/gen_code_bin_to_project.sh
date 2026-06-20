#!/bin/bash

cd "$(dirname "$0")"
echo "当前目录: $(pwd)"

export WORKSPACE="$(realpath ../../)"
export LUBAN_DLL="${WORKSPACE}/Configs/Luban/Luban.dll"
export CONF_ROOT="$(pwd)"
export DATA_OUTPATH="${WORKSPACE}/UnityProject/Assets/AssetRaw/Configs/bytes/"
export CODE_OUTPATH="${WORKSPACE}/UnityProject/Assets/GameScripts/HotFix/GameProto/LubanConfig/"

dotnet "${LUBAN_DLL}" \
    -t client \
    -c cs-bin \
    -d bin \
    --conf "${CONF_ROOT}/luban.conf" \
    -x code.lineEnding=crlf \
    -x outputCodeDir="${CODE_OUTPATH}" \
    -x outputDataDir="${DATA_OUTPATH}"
