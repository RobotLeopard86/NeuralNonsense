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
dotnet publish --sc server/NeuralNonsense.Server.csproj -o dist/app -c Release
rm dist/app/appsettings.Development.json
echo -en "#!/usr/bin/env bash\n\nset -euo pipefail\ncd \$(dirname "\$0")/app\nASPNETCORE_ENVIRONMENT=Production ./NeuralNonsense.Server" > dist/run.sh
chmod +x dist/run.sh

echo "building client bundle... "
pnpm -C client run build --outDir ../dist/app/wwwroot  --minify -m production

echo "copying certificates... "
mkdir -p dist/.certs
cp .certs/cert.pem dist/.certs
cp .certs/dev.pem dist/.certs

echo "distribution packaging complete."