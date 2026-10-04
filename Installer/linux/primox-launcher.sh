#!/usr/bin/env bash
# ==============================================================================
# PRIMOX Workshop 2.0 — Linux Desktop Launcher
# ==============================================================================
set -e

REAL_SCRIPT="$(readlink -f "${BASH_SOURCE[0]}")"
SCRIPT_DIR="$(cd "$(dirname "$REAL_SCRIPT")" && pwd)"
APP_DIR="$SCRIPT_DIR/app"

export WINEPREFIX="${WINEPREFIX:-$HOME/.primox/wineprefix}"
export WINEDEBUG="-all"
export LANG="${LANG:-pt_BR.UTF-8}"
export LC_ALL="${LC_ALL:-pt_BR.UTF-8}"

# Verificar Wine
if ! command -v wine >/dev/null 2>&1; then
    echo "ERRO: O Wine não foi encontrado. Por favor, instale o Wine:"
    echo "  - Ubuntu/Debian: sudo apt install wine"
    echo "  - Fedora: sudo dnf install wine"
    echo "  - Arch Linux: sudo pacman -S wine"
    exit 1
fi

# Inicializar prefixo Wine se não existir
if [ ! -d "$WINEPREFIX" ]; then
    echo "Inicializando ambiente Wine isolado para PRIMOX em $WINEPREFIX..."
    mkdir -p "$WINEPREFIX"
    wineboot -u
fi

if [ ! -f "$APP_DIR/PrimoAutoEletrica.exe" ]; then
    echo "ERRO: Executável PrimoAutoEletrica.exe não encontrado em $APP_DIR"
    exit 1
fi

cd "$APP_DIR"
exec wine PrimoAutoEletrica.exe "$@"
