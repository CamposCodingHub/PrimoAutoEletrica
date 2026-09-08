# PRIMOX Workshop — Instalador comercial (Inno Setup)

**Classificação:** OFFICIAL COMMERCIAL DISTRIBUTION  
**Produto:** PRIMOX Workshop **1.0.0**  
**TFM:** `net6.0-windows` (fonte: `PrimoAutoEletrica.csproj`)  
**Runtime publish:** `win-x64` self-contained  

## Pipeline oficial

```powershell
# Pré-requisito: Inno Setup 6 (ISCC.exe)
winget install JRSoftware.InnoSetup

$env:DOTNET_ROLL_FORWARD='LatestMajor'
.\Scripts\Build-PrimoXCommercialRelease.ps1 -Version 1.0.0
```

Saídas (pasta `artifacts/` — ignorada pelo Git):

| Artefato | Caminho |
|----------|---------|
| Publish | `artifacts/publish/win-x64/` |
| Setup | `artifacts/installer/PRIMOX-Workshop-Setup-1.0.0.exe` |
| SHA256 | `artifacts/checksums/PRIMOX-Workshop-Setup-1.0.0.sha256.txt` |
| Log | `artifacts/logs/commercial-release-*.log` |

## O que o instalador faz

- Instala o **programa** em `C:\Program Files\PRIMOX\Workshop`
- Cria atalhos **PRIMOX Workshop** (Menu Iniciar + Desktop opcional)
- Registra desinstalação (Add/Remove Programs)
- **Não** embute `primoauto.db` nem dados reais
- **Não** remove `%LOCALAPPDATA%\PrimoAutoEletrica` no uninstall (preserva banco/backups/config/mídia)

## Dados do usuário (runtime)

O aplicativo continua usando (compatibilidade 1.0.0):

`%LOCALAPPDATA%\PrimoAutoEletrica\primoauto.db`

Não alterar esse caminho sem migração segura.

## Scripts auxiliares (NÃO oficiais)

| Script | Classificação |
|--------|---------------|
| `Scripts/Build-PrimoXCommercialRelease.ps1` | **OFFICIAL** |
| `Installer/build-installer.ps1` | LEGACY WRAPPER → oficial |
| `Scripts/Deploy-ToInstalledApp.ps1` | DEVELOPMENT (LocalAppData\App) |
| `Scripts/Atualizar-PrimoAuto.bat` | DEVELOPMENT |
| `Scripts/New-WindowsInstallerPackage.ps1` | LEGACY / ZIP auxiliar |

## Assinatura

**SIGNING: NOT CONFIGURED** — Authenticode preparado para fase futura (não versionar certificados).

## Limitações honestas

- Auto-update comercial completo: **NOT IMPLEMENTED**
- Backup/restore: no **aplicativo** (`DatabaseBackupService`), não no script Inno
- Instalação Parallel File (legado `Primo Auto Elétrica` 0.0.0.0): documentada; não apagar automaticamente
