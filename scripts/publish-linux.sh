#!/usr/bin/env bash
set -euo pipefail

PROJECT_DIR="PrimoAutoEletricaAvalonia/PrimoAutoEletricaAvalonia"
OUT_DIR="publish/linux"

pushd "$PROJECT_DIR" >/dev/null
dotnet restore
dotnet publish -c Release -r linux-x64 -o "../../$OUT_DIR" --self-contained false
popd >/dev/null

echo "Published to $OUT_DIR"
