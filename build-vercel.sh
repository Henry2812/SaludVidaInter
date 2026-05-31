#!/usr/bin/env bash
set -euo pipefail

curl -sSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
bash dotnet-install.sh --channel 9.0 --install-dir ./.dotnet

export PATH="$PWD/.dotnet:$PATH"

dotnet publish SaludVidaPwa.csproj -c Release -o publish

rm -rf deploy
mkdir -p deploy
cp -R publish/wwwroot/. deploy/
