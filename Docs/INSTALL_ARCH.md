# Instalação e build no Arch Linux

Resumo: o projeto `PrimoAutoEletricaAvalonia` mira em `net10.0`. Para compilar você precisa do .NET 10 SDK; para executar, do `dotnet-runtime` compatível.

Passos rápidos (recomendado):

1) Atualizar sistema e instalar runtime/sdk (escolha SDK se for compilar):

```bash
sudo pacman -Syu
# SDK (compilar + executar)
sudo pacman -S dotnet-sdk
# Ou apenas runtime (executar apenas)
sudo pacman -S dotnet-runtime
# dependências nativas
sudo pacman -S gtk3 libglvnd mesa harfbuzz fontconfig
```

2) Compilar/publicar (a partir da raiz do repositório):

```bash
./scripts/publish-linux.sh
# saída em publish/linux
```

3) Instalar manualmente no sistema (exemplo):

```bash
sudo mkdir -p /opt/primoauto
sudo cp -r publish/linux/* /opt/primoauto/
sudo ln -sf /opt/primoauto/PrimoAutoEletricaAvalonia /usr/local/bin/primoauto
```

4) Instalar service e atalho gráfico (opcionais):

```bash
sudo cp Installer/primoauto.service /etc/systemd/system/primoauto.service
sudo systemctl daemon-reload
sudo systemctl enable --now primoauto.service

sudo cp Installer/primoauto.desktop /usr/share/applications/primoauto.desktop
# atualize o ícone em /opt/primoauto/icons/app-icon.png se desejar
```

Observações importantes:
- O repositório contém um `global.json` em `PrimoAutoEletrica/global.json` fixando SDK `9.0.314`. Se ocorrer erro dizendo que o SDK 9 é requerido, remova ou atualize esse `global.json`, ou execute os comandos de compilação a partir do diretório `PrimoAutoEletricaAvalonia/PrimoAutoEletricaAvalonia` e garanta que o SDK 10 esteja instalado.
 - Se preferir criar um pacote Arch (PKGBUILD/AUR), há um `PKGBUILD` pronto em `Installer/PKGBUILD` e um tarball fonte `Installer/primoauto-1.0.0.tar.gz`.

Para construir o pacote localmente:

```bash
# instale as ferramentas necessárias
sudo pacman -S --needed base-devel
# entre em um diretório de build (fora do repositório é recomendado)
cd Installer
makepkg -si
```

O `PKGBUILD` referencia `primoauto-1.0.0.tar.gz` já incluído em `Installer/` e contém o `sha256sums` para verificação.

Habilitar o serviço do usuário (systemd user):

```bash
# recarregar unidades do usuário e habilitar
systemctl --user daemon-reload
systemctl --user enable --now primoauto.service
```

Se você quiser que eu gere um `PKGBUILD` alternativo para um `--self-contained` publish (distribuição sem depender de `dotnet-runtime`), eu já posso criar isso também.

Há um PKGBUILD alternativo para distribuição self-contained em `Installer/PKGBUILD.selfcontained`. Ele compila com `--self-contained true` e produz um pacote que não depende do `dotnet-runtime` no target.

O build self-contained foi gerado aqui no repositório em `publish/linux-selfcontained/`.
