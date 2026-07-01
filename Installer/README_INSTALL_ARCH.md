# Primo Auto Eletrica — Instalação rápida (Arch Linux)

Resumo rápido — três modos:
- Execução local (usa dotnet instalado em `~/.dotnet` pelo instalador local que criei).
- Pacote AUR (use `Installer/PKGBUILD` ou `Installer/PKGBUILD.selfcontained`).
- Instalação system-wide (copiar `publish/*` para `/opt/primoauto`, criar service).

Comandos essenciais (copiar/colar):

1) Garantir dotnet local (se necessário):

```bash
# já instalado em ~/.dotnet pelo processo automatizado; caso precise reinstalar:
curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
bash /tmp/dotnet-install.sh --channel 10.0 --install-dir "$HOME/.dotnet" --architecture x64
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$HOME/.local/bin:$PATH"
```

2) Rodar a aplicação (sessão gráfica):

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$HOME/.local/bin:$PATH"
~/.local/bin/primoauto
```

3) Habilitar como serviço do usuário (systemd user):

```bash
systemctl --user daemon-reload
systemctl --user enable --now primoauto.service
```

4) Construir pacote Arch (AUR) a partir do tarball (opção profissional):

```bash
sudo pacman -S --needed base-devel
cd Installer
makepkg -si
```

Notas importantes:
- `Installer/PKGBUILD` produz pacote *framework-dependent* (exige `dotnet-runtime`).
- `Installer/PKGBUILD.selfcontained` produz pacote *self-contained* (não precisa de `dotnet-runtime` no target).
- Se ver `XOpenDisplay failed` ao testar, execute o binário em uma sessão gráfica (local ou via X/Wayland forwarding).
- O `global.json` em `PrimoAutoEletrica/global.json` fixa SDK 9; eu utilizei SDK 10 para o projeto Avalonia. Caso tenha problemas de SDK, execute os builds dentro do diretório `PrimoAutoEletricaAvalonia/PrimoAutoEletricaAvalonia` ou atualize/remova `global.json` conforme desejar.

Remoção rápida (se instalado localmente):

```bash
rm -rf ~/.local/share/primoauto ~/.local/bin/primoauto ~/.local/share/applications/primoauto.desktop ~/.config/systemd/user/primoauto.service
systemctl --user daemon-reload
```
