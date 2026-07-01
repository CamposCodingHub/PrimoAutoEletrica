#!/usr/bin/env bash
set -euo pipefail

# Creates Installer/primoauto-1.0.0.tar.gz from repository root, excluding build artifacts
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TARBALL="$REPO_ROOT/Installer/primoauto-1.0.0.tar.gz"

rm -f "$TARBALL"
TMPDIR=$(mktemp -d)
mkdir -p "$TMPDIR/primoauto-1.0.0"

pushd "$REPO_ROOT" >/dev/null
shopt -s dotglob
for f in *; do
  case "$f" in
    publish|.git|TestResults|obj|bin|Installer/primoauto-1.0.0.tar.gz)
      continue
      ;;
  esac
  cp -a "$f" "$TMPDIR/primoauto-1.0.0/"
done
popd >/dev/null

tar -czf "$TARBALL" -C "$TMPDIR" primoauto-1.0.0
rm -rf "$TMPDIR"
echo "Created $TARBALL"
