#!/usr/bin/env bash
# ==============================================================================
# PRIMOX Workshop 2.0 — Empacotador Multiplataforma (Linux .tar.gz + Windows .zip)
# ==============================================================================
set -e

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="2.0.0"
PUBLISH_DIR="$REPO_ROOT/PrimoAutoEletrica/bin/Release/net6.0-windows/win-x64/publish"
ARTIFACTS_DIR="$REPO_ROOT/artifacts"
INSTALLER_DIR="$ARTIFACTS_DIR/installer"
CHECKSUMS_DIR="$ARTIFACTS_DIR/checksums"

echo "=== Empacotando Distribuições do PRIMOX Workshop $VERSION ==="

mkdir -p "$INSTALLER_DIR"
mkdir -p "$CHECKSUMS_DIR"

# 1. Garantir que os binários estão publicados
if [ ! -f "$PUBLISH_DIR/PrimoAutoEletrica.exe" ]; then
    echo "Publicando binários de release win-x64..."
    dotnet publish "$REPO_ROOT/PrimoAutoEletrica/PrimoAutoEletrica.csproj" \
        -c Release \
        -r win-x64 \
        -p:EnableWindowsTargeting=true \
        --self-contained false
fi

# 2. Empacotar Distribuição Linux (.tar.gz)
echo "▶ Criando pacote de distribuição para Linux..."
LINUX_STAGING="$(mktemp -d)"
mkdir -p "$LINUX_STAGING/PRIMOX-Workshop-Linux-$VERSION/app"

cp -r "$PUBLISH_DIR/"* "$LINUX_STAGING/PRIMOX-Workshop-Linux-$VERSION/app/"
cp "$REPO_ROOT/Installer/linux/install.sh" "$LINUX_STAGING/PRIMOX-Workshop-Linux-$VERSION/install.sh"
cp "$REPO_ROOT/Installer/linux/primox-launcher.sh" "$LINUX_STAGING/PRIMOX-Workshop-Linux-$VERSION/primox-launcher.sh"
cp "$REPO_ROOT/PrimoAutoEletrica/icon.png" "$LINUX_STAGING/PRIMOX-Workshop-Linux-$VERSION/icon.png"
chmod +x "$LINUX_STAGING/PRIMOX-Workshop-Linux-$VERSION/install.sh"
chmod +x "$LINUX_STAGING/PRIMOX-Workshop-Linux-$VERSION/primox-launcher.sh"

LINUX_TAR="$INSTALLER_DIR/PRIMOX-Workshop-Linux-$VERSION.tar.gz"
tar -czf "$LINUX_TAR" -C "$LINUX_STAGING" "PRIMOX-Workshop-Linux-$VERSION"
rm -rf "$LINUX_STAGING"

echo "✓ Pacote Linux gerado: $LINUX_TAR"

# 3. Empacotar Distribuição Portable Windows (.zip)
echo "▶ Criando pacote Portable para Windows (.zip)..."
WIN_ZIP="$INSTALLER_DIR/PRIMOX-Workshop-Portable-win-x64-$VERSION.zip"
rm -f "$WIN_ZIP"
if command -v 7z >/dev/null 2>&1; then
    (cd "$PUBLISH_DIR" && 7z a -tzip -bso0 "$WIN_ZIP" .)
elif command -v python3 >/dev/null 2>&1; then
    python3 -c "import zipfile, os, sys;
z = zipfile.ZipFile('$WIN_ZIP', 'w', zipfile.ZIP_DEFLATED)
for root, dirs, files in os.walk('$PUBLISH_DIR'):
    for f in files:
        p = os.path.join(root, f)
        z.write(p, os.path.relpath(p, '$PUBLISH_DIR'))
z.close()"
fi
echo "✓ Pacote Windows Portable gerado: $WIN_ZIP"

# 4. Gerar Checksums SHA256
echo "▶ Gerando assinaturas de integridade SHA-256..."
cd "$INSTALLER_DIR"
sha256sum "PRIMOX-Workshop-Linux-$VERSION.tar.gz" > "$CHECKSUMS_DIR/PRIMOX-Workshop-Linux-$VERSION.sha256.txt"
sha256sum "PRIMOX-Workshop-Portable-win-x64-$VERSION.zip" > "$CHECKSUMS_DIR/PRIMOX-Workshop-Portable-win-x64-$VERSION.sha256.txt"

echo "=========================================================="
echo "   Distribuições 2.0 criadas com sucesso em $INSTALLER_DIR:"
echo "   1. Linux: $(basename "$LINUX_TAR") ($(du -h "$LINUX_TAR" | cut -f1))"
echo "   2. Windows: $(basename "$WIN_ZIP") ($(du -h "$WIN_ZIP" | cut -f1))"
echo "   3. Checksums salvos em: $CHECKSUMS_DIR"
echo "=========================================================="
