#!/usr/bin/env bash

set -euo pipefail

if ! [ -d ".certs" ]; then
    echo "error: please run ./setup.sh first" >&2
    exit 1
fi

echo "preparing distribution directory... "
rm -rf dist
mkdir -p dist
echo "*" > dist/.gitignore

echo "building server... "
dotnet publish --sc server/NeuralNonsense.Server.csproj -o dist -c Release
rm dist/appsettings.Development.json

echo "building client bundle... "
pnpm -C client run build --outDir ../dist/wwwroot  --minify -m production

echo "distribution packaging complete."