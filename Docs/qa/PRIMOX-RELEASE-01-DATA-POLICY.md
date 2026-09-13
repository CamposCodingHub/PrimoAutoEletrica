# PRIMOX RELEASE-01 — DATA POLICY

> **RELATÓRIO DE AVANÇO / FASE — 2026-09-13**
>
> Este arquivo registra **melhorias e evidências da fase em que foi escrito**.
> **Não** é inventário operacional atual.
>
> Verdade atual: `Docs/CURRENT-TRUTH.md` · Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md` · Índice: `Docs/DOCUMENTATION-INDEX.md`
> HEAD pós-NET10-26: `1372e11` · TFM `net10.0-windows`

---

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
