#!/usr/bin/env bash
# ==============================================================================
# PRIMOX Workshop 2.0 — Instalador Automático para Linux
# ==============================================================================
set -e

VERSION="2.1.0"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "=========================================================="
echo "   PRIMOX Workshop $VERSION — Instalador para Linux       "
echo "   Gestão de Auto Elétrica, Ferramentaria e Copilot IA    "
echo "=========================================================="
echo ""

# Determinar diretórios de instalação (User vs System)
if [ "$(id -u)" -eq 0 ]; then
    INSTALL_DIR="/opt/primox-workshop"
    BIN_DIR="/usr/local/bin"
    DESKTOP_DIR="/usr/share/applications"
    ICON_DIR="/usr/share/icons/hicolor/256x256/apps"
    MODE="Sistema (Global)"
else
    INSTALL_DIR="$HOME/.local/share/primox-workshop"
    BIN_DIR="$HOME/.local/bin"
    DESKTOP_DIR="$HOME/.local/share/applications"
    ICON_DIR="$HOME/.local/share/icons/hicolor/256x256/apps"
    MODE="Usuário Local ($USER)"
fi

echo "Modo de Instalação: $MODE"
echo "Destino: $INSTALL_DIR"
echo ""

# 1. Verificar dependências
echo "▶ 1/5 Verificando dependências..."
if ! command -v wine >/dev/null 2>&1; then
    echo "AVISO: O Wine não foi detectado no sistema."
    echo "O PRIMOX precisa do Wine para executar em ambientes Linux."
    echo "Você pode continuar a instalação agora e instalar o Wine depois:"
    echo "  - Ubuntu/Debian: sudo apt install wine wine64"
    echo "  - Fedora: sudo dnf install wine"
    echo "  - Arch Linux: sudo pacman -S wine wine-mono"
    echo ""
    read -p "Deseja continuar com a instalação dos arquivos? (S/n): " CONFIRM
    if [[ "$CONFIRM" =~ ^[Nn]$ ]]; then
        echo "Instalação cancelada."
        exit 0
    fi
else
    echo "✓ Wine detectado: $(wine --version)"
fi

# 2. Criar pastas de destino
echo "▶ 2/5 Criando pastas de destino..."
mkdir -p "$INSTALL_DIR/app"
mkdir -p "$BIN_DIR"
mkdir -p "$DESKTOP_DIR"
mkdir -p "$ICON_DIR"

# 3. Copiar arquivos da aplicação
echo "▶ 3/5 Copiando binários do PRIMOX 2.0..."
if [ -d "$SCRIPT_DIR/app" ]; then
    cp -r "$SCRIPT_DIR/app/"* "$INSTALL_DIR/app/"
elif [ -d "$SCRIPT_DIR/../../PrimoAutoEletrica/bin/Release/net6.0-windows/win-x64/publish" ]; then
    cp -r "$SCRIPT_DIR/../../PrimoAutoEletrica/bin/Release/net6.0-windows/win-x64/publish/"* "$INSTALL_DIR/app/"
else
    echo "ERRO: Diretório com os binários compilados não encontrado."
    exit 1
fi

# Copiar ícone e launcher
if [ -f "$SCRIPT_DIR/icon.png" ]; then
    cp "$SCRIPT_DIR/icon.png" "$INSTALL_DIR/icon.png"
    cp "$SCRIPT_DIR/icon.png" "$ICON_DIR/primox-workshop.png"
elif [ -f "$SCRIPT_DIR/../../PrimoAutoEletrica/icon.png" ]; then
    cp "$SCRIPT_DIR/../../PrimoAutoEletrica/icon.png" "$INSTALL_DIR/icon.png"
    cp "$SCRIPT_DIR/../../PrimoAutoEletrica/icon.png" "$ICON_DIR/primox-workshop.png"
fi

cp "$SCRIPT_DIR/primox-launcher.sh" "$INSTALL_DIR/primox-launcher.sh"
chmod +x "$INSTALL_DIR/primox-launcher.sh"

# 4. Criar atalho no terminal
echo "▶ 4/5 Configurando atalho no terminal..."
ln -sf "$INSTALL_DIR/primox-launcher.sh" "$BIN_DIR/primox"
chmod +x "$BIN_DIR/primox"

# 5. Criar lançador no menu de aplicativos (.desktop)
echo "▶ 5/5 Registrando atalho no menu de aplicativos..."
cat <<EOF > "$DESKTOP_DIR/primox-workshop.desktop"
[Desktop Entry]
Type=Application
Version=1.0
Name=PRIMOX Workshop
GenericName=Gestão de Auto Elétrica e Oficina
Comment=ERP Automotivo, Gestão de Ferramental, Falta & Compras e Copilot de IA
Exec=$INSTALL_DIR/primox-launcher.sh %u
Icon=$INSTALL_DIR/icon.png
Terminal=false
Categories=Office;Database;Utility;
Keywords=oficina;mecanica;eletrica;auto;erp;ferramentas;compras;ia;copilot;
StartupNotify=true
StartupWMClass=PrimoAutoEletrica.exe
EOF

chmod +x "$DESKTOP_DIR/primox-workshop.desktop"

# Atualizar banco de dados de desktops se comando existir
if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database "$DESKTOP_DIR" 2>/dev/null || true
fi

# Gerar script de desinstalação
cat <<EOF > "$INSTALL_DIR/uninstall.sh"
#!/usr/bin/env bash
echo "Desinstalando PRIMOX Workshop..."
rm -f "$BIN_DIR/primox"
rm -f "$DESKTOP_DIR/primox-workshop.desktop"
rm -f "$ICON_DIR/primox-workshop.png"
rm -rf "$INSTALL_DIR"
if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database "$DESKTOP_DIR" 2>/dev/null || true
fi
echo "PRIMOX Workshop desinstalado com sucesso."
EOF
chmod +x "$INSTALL_DIR/uninstall.sh"

echo ""
echo "=========================================================="
echo "   ✓ Instalação concluída com sucesso!                   "
echo "=========================================================="
echo "Você pode iniciar o PRIMOX Workshop de duas maneiras:"
echo " 1. Pelo Menu de Aplicativos do seu sistema (busque por 'PRIMOX')"
echo " 2. Pelo Terminal executando o comando: primox"
echo ""
echo "Para desinstalar no futuro, execute: $INSTALL_DIR/uninstall.sh"
echo "=========================================================="
