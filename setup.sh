#!/usr/bin/env bash

set -euo pipefail
echo -n "checking installed software... "

if ! command -v tmux >/dev/null 2>&1; then
    echo "error: tmux is not installed" >&2
    exit 1
fi

if ! command -v pnpm >/dev/null 2>&1; then
    echo "error: pnpm is not installed" >&2
    exit 1
fi

if ! command -v node >/dev/null 2>&1; then
    echo "error: node is not installed" >&2
    exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
    echo "error: dotnet is not installed" >&2
    exit 1
fi

DOTNET_RUNTIMES=$(dotnet --list-runtimes)
if ! [[ $DOTNET_RUNTIMES == *"Microsoft.NETCore.App"* ]]; then
    echo "error: no asp.net runtime found" >&2
    exit 1
fi
if ! [[ $DOTNET_RUNTIMES == *"Microsoft.AspNetCore.App"* ]]; then
    echo "error: no asp.net runtime found" >&2
    exit 1
fi

echo -en "\nverifying .net runtimes... "
BK_IFS=$IFS
IFS=$'\n'
ASP_OK=1
NET_OK=1
for line in $DOTNET_RUNTIMES; do
    RT_PATH=$(echo $line | sed 's|.*\[||g' | sed 's|\]||g')
    if [ "$(basename $RT_PATH)" == "Microsoft.AspNetCore.App" ]; then
        for ver in $(ls $RT_PATH); do
            if [ "$(echo $ver | sed 's/\..*//g')" -ge "10" ]; then
                ASP_OK=0
            fi
        done
    fi
    if [ "$(basename $RT_PATH)" == "Microsoft.NETCore.App" ]; then
        for ver in $(ls $RT_PATH); do
            if [ "$(echo $ver | sed 's/\..*//g')" -ge "10" ]; then
                NET_OK=0
            fi
        done
    fi
done
IFS=$BK_IFS
if [ "$ASP_OK" == "1" ] || [ "$NET_OK" == "1" ]; then
    echo "error: missing suitable .net and asp.net runtimes" >&2
    exit 1
fi

echo -e "\ninstalling npm packages..."
cd client
pnpm i

echo "generating certificates (you may be asked to authenticate)..."
pnpm dev &
until curl https://localhost:5173 >/dev/null 2>/dev/null; do
    sleep 1
done
pkill -INT -f vite

echo "setup complete."
echo "note: if you see an error above related to virtual module imports, this is not a real error and may be disregarded."