# PRIMOX RELEASE-01 — DATA POLICY

**Data:** 12/09/2026

| Área | Localização | Mutável? |
|---|---|---|
| PROGRAM FILES / `{commonpf}\PRIMOX\Workshop` | Binários da aplicação (EXE/DLL/runtime) | Não (somente leitura em operação normal) |
| LOCALAPPDATA `\PrimoAutoEletrica` | Banco, config, logs, backups, mídia, imports | Sim |
| CONFIG | `%LOCALAPPDATA%\PrimoAutoEletrica\*.json` | Sim |
| LOG | `%LOCALAPPDATA%\PrimoAutoEletrica\Logs\` | Sim |
| BACKUP | `%LOCALAPPDATA%\PrimoAutoEletrica\Backups\` | Sim |
| TEMP | `%TEMP%` (+ PDFs/temp sob política SecureProcessLauncher) | Sim |
| MyDocuments `\PrimoAutoEletrica` | PDFs exportados (quando aplicável) | Sim |

## Uninstall policy

- Uninstall remove binários / atalhos / entradas do instalador.  
- **Não** remove AppData (preservar banco/backups/config/mídia) — documentado no ISS.

## Permissions

- Installer: `PrivilegesRequired=admin`  
- Runtime: dados mutáveis em LocalAppData — não dependem de escrita em Program Files.
