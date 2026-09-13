# PRIMOX NET10 — Desktop Deploy

> **RELATÓRIO DE AVANÇO / FASE — 2026-09-13**
>
> Este arquivo registra **melhorias e evidências da fase em que foi escrito**.
> **Não** é inventário operacional atual.
>
> Verdade atual: `Docs/CURRENT-TRUTH.md` · Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md` · Índice: `Docs/DOCUMENTATION-INDEX.md`
> HEAD pós-NET10-26: `1372e11` · TFM `net10.0-windows`

---

**Data/hora:** 2026-09-13 13:00–13:09 (America/Sao_Paulo)  
**Branch:** `migration/net10`  
**HEAD:** `0f71fe198c5d1a08cda94d3aea7ab21ae5ffd45d`  
**Resultado:** **PASS**

---

## Objetivo

Atualizar a instalação do PRIMOX Workshop apontada pelo atalho da área de trabalho com o build NET10 aprovado em `migration/net10` (HEAD `0f71fe1`), sem usar `main`, `v1.0.0` nem o installer comercial NET6 `1.0.0`.

---

## Instalação anterior (pré-deploy)

| Campo | Valor |
|------|--------|
| Atalho | `%USERPROFILE%\OneDrive\Desktop\PRIMOX Workshop.lnk` |
| Destino físico | `C:\Program Files\PRIMOX\Workshop` |
| EXE | `C:\Program Files\PRIMOX\Workshop\PrimoAutoEletrica.exe` |
| EXE SHA256 (pré) | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` |
| TFM runtimeconfig | `net10.0` (já NET10 da sessão anterior) |
| Tamanho EXE | 203776 |

---

## Mecanismo oficial

Scripts lidos e usados:

1. `Scripts/Atualizar-PrimoAuto.bat` → wrapper de `Deploy-ToInstalledApp.ps1`
2. `Scripts/Deploy-ToInstalledApp.ps1` — publish self-contained a partir do TFM do csproj (`net10.0-windows`), backup em `%LOCALAPPDATA%\PrimoAutoEletrica\Backups\BeforeDeploy`, preserva pastas Data/Logs/Config/…, **não** apaga AppData do usuário, **não** executa uninstall.

**Installer comercial NET6 `PRIMOX-Workshop-Setup-1.0.0.exe`:** **não utilizado**.

---

## Artefato NET10 (origem)

| Campo | Valor |
|------|--------|
| Origem | `dotnet publish` Release `win-x64` self-contained @ HEAD `0f71fe1` |
| Publish dir | `PrimoAutoEletrica\bin\Release\net10.0-windows\win-x64\publish` |
| Snapshot evidência | `TestResults\Net10-Overnight\20260913\NET10-Desktop-Deploy\publish-artifact\` |
| **Installer** (mecanismo) | N/A — deploy por publish (`Deploy-ToInstalledApp` / cópia elevada equivalente) |
| **Installer SHA256** | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` *(SHA do EXE publish; não há Inno nesta via)* |
| **EXE** | `PrimoAutoEletrica.exe` |
| **EXE SHA256** | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` |
| Tamanho | 203776 |
| LiveCharts/OpenTK | 0 |
| TFM | `net10.0-windows` / runtime `net10.0` |

Manifesto: `TestResults\...\NET10-Desktop-Deploy\artifact-manifest.json`

---

## Backup / segurança

| Item | Resultado |
|------|-----------|
| AppData pré | Presente — inventário em `pre-appdata-inventory.json` |
| Banco pré | `%LOCALAPPDATA%\PrimoAutoEletrica\primoauto.db` · 3 411 968 bytes · SHA `3BE5387D…E66E` |
| Backup não destrutivo AppData | Cópia em `pre-deploy-user-backup\` |
| Backup instalação (Program Files) | `%LOCALAPPDATA%\PrimoAutoEletrica\Backups\BeforeDeploy\install-20260913_130131` (475 arquivos) |

---

## Instalação REAL

1. `dotnet publish` Exit=0 (artefato SHA acima).
2. Elevação UAC: cópia elevada para `C:\Program Files\PRIMOX\Workshop` com a mesma política de preservação do `Deploy-ToInstalledApp.ps1` (backup → preserve Data/Logs/Config/… → copiar publish). Log: `deploy-elevated.log` (`ROBO_START` … `INSTALLED_SHA=05D103…` … `ROBO_END`).
3. Segunda tentativa do wrapper oficial `Deploy-ToInstalledApp.ps1 -SkipBuild` via UAC retornou `-196608` (elevação cancelada/recusada pelo host); o conteúdo já estava atualizado pela etapa 2 com SHA idêntico ao artefato.

| Campo pós-install | Valor |
|-------------------|--------|
| EXE instalado | `C:\Program Files\PRIMOX\Workshop\PrimoAutoEletrica.exe` |
| Existe | Sim |
| Timestamp pasta backup | 2026-09-13 13:01:32 |
| SHA256 instalado | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` |
| SHA == artefato | **Sim** |
| Atalho | Continua → Program Files\PRIMOX\Workshop |
| CalendarContrastHealer no DLL | Sim |
| LiveCharts | 0 |

---

## Startup

| Passo | Resultado |
|-------|-----------|
| Início processo | `startup_alive=True` |
| Fechamento | `CloseMainWindow` / graceful · exit `0` |
| Evidência | `startup-final.log` |

---

## Smoke (EXE instalado)

Executado no **EXE de Program Files** com `--app-data` **isolado** sob `TestResults\...\smoke-run-*` (não usa o AppData do usuário como alvo do smoke).

| Módulo | Filtro | Exit | Checks | Fails |
|--------|--------|------|--------|-------|
| LOGIN | LoginSessao | 0 | 1/1 | 0 |
| Nav (core + Configurações) | PreCheck | 0 | 1/1 | 0 |
| DASHBOARD | Dashboard | 0 | 1/1 | 0 |
| CLIENTES | Clientes | 0 | 3/3 | 0 |
| VEÍCULOS | Veiculos | 0 | 3/3 | 0 |
| OS | OrdensServico | 0 | 2/2 | 0 |
| AGENDA | Agendamentos | 0 | 2/2 | 0 |
| ESTOQUE | Estoque | 0 | 2/2 | 0 |
| FINANCEIRO | Financeiro | 0 | 3/3 | 0 |
| RELATÓRIOS | Relatorios | 0 | 6/6 | 0 |
| CONFIGURAÇÕES | Configuracoes | 0 | 2/2 | 0 |
| Temas Light/Dark | Tema | 0 | 2/2 | 0 |

**Totais:** 28 checks · **fails = 0** · **BAD_EXIT_COUNT = 0**  
JSON: `smoke-installed-results.json`

---

## Dados do usuário

| Verificação | Resultado |
|-------------|-----------|
| AppData presente | **PASS** |
| Banco presente | **PASS** |
| Configs (`theme_settings`, `language_settings`, `business-config`, `database-settings`, `station-config`, `sidebar_settings`) | **PASS** (presentes) |
| Contagens PRE vs POST (Python/sqlite) | Clientes 13=13 · Veículos 18=18 · OS 16=16 · Produtos 19=19 · Funcionários 7=7 · ContasPagar 13=13 · ContasReceber 24=24 · Agendamentos 10=10 · tabelas 55=55 |
| SHA do `.db` bit-a-bit | Alterado após startup normal (auto-backup/WAL/schema) — **não** apagado pelo deploy; cópia pré em `pre-deploy-user-backup\` |

**APPDATA:** PASS  
**DATABASE:** PASS (dados de negócio preservados; SHA não bit-idêntico após startup)

---

## Git (protegido)

```
branch = migration/net10
HEAD   = 0f71fe198c5d1a08cda94d3aea7ab21ae5ffd45d
main   = 29b19b16d0e6e3413bdba20c505e20c992596c24
v1.0.0 = a4ad6fe810a96914275891d294e9e585c7867e9e
primox-net6-final = 29b19b16d0e6e3413bdba20c505e20c992596c24
```

MERGE=NO · PUSH=NO · TAGS=UNCHANGED

---

## Resultado final

| Campo | Valor |
|-------|--------|
| DEPLOY | **PASS** |
| SOURCE | `migration/net10` @ `0f71fe1` publish net10 self-contained |
| TARGET | `C:\Program Files\PRIMOX\Workshop` |
| OLD EXE SHA | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` |
| NEW EXE SHA | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` |
| INSTALLER SHA | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` (EXE publish) |
| STARTUP | PASS |
| SMOKE | PASS (fails=0) |
| APPDATA | PASS |
| DATABASE | PASS |
| NET10 VERIFIED | **YES** (`net10.0` / `net10.0-windows`) |
| GIT PROTECTED | **YES** |
| FINAL | **PASS** |

Evidência: `TestResults\Net10-Overnight\20260913\NET10-Desktop-Deploy\`
