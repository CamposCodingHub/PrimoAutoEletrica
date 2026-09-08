# PRIMOX COMMERCIAL PACKAGING & DEPLOYMENT — Fase 15B

**Produto:** PRIMOX Workshop  
**Versão:** 1.0.0  
**Tag:** `v1.0.0` → `a4ad6fe` (**não movida**)  
**HEAD no fechamento desta fase:** ver Git após commits de packaging  
**Branch:** `main`  
**Data:** 08/09/2026  

**Decisão:** **GO** (cadeia comercial reproduzível), com limitações conhecidas documentadas abaixo.

---

## 1. Caminho oficial

```
Código → Build Release → Publish win-x64 self-contained → Inno Setup 6 → PRIMOX-Workshop-Setup-1.0.0.exe → Install → PRIMOX Workshop
```

| Classificação | Item |
|---------------|------|
| **OFFICIAL** | `Scripts/Build-PrimoXCommercialRelease.ps1` + `Installer/PrimoAutoEletrica.iss` |
| LEGACY WRAPPER | `Installer/build-installer.ps1` → chama o oficial |
| DEVELOPMENT | `Scripts/Deploy-ToInstalledApp.ps1`, `Scripts/Atualizar-PrimoAuto.bat` |
| LEGACY / auxiliar | `Scripts/New-WindowsInstallerPackage.ps1`, ZIP no Actions |
| CI | `.github/workflows/release.yml` — SDK 6.0.x; **não** cria GitHub Release automático nesta fase |

---

## 2. Build / Publish / Installer

| Item | Resultado | Evidência |
|------|-----------|-----------|
| TFM | **PASS** | `net6.0-windows` (csproj = fonte de verdade); scripts/ISS alinhados |
| SDK local | .NET SDK 10 com `DOTNET_ROLL_FORWARD=LatestMajor` | log commercial-release |
| Publish | **PASS** | `artifacts/publish/win-x64`, self-contained, **sem** SingleFile/Trim |
| Runtime | win-x64 self-contained | ~59 MB Setup |
| Inno | **PASS** | Inno Setup **6.7.3** / ISCC em `%LOCALAPPDATA%\Programs\Inno Setup 6\` |
| Setup | `artifacts/installer/PRIMOX-Workshop-Setup-1.0.0.exe` | gerado 08/09/2026 |
| SHA256 | `A62388AE547B6863363825DA984A7532061F5EF7548CCB66BEB5615D35735EB3` | `artifacts/checksums/…sha256.txt` |
| EXE metadata (publish) | ProductVersion **1.0.0** / FileVersion **1.0.0.0** | log 09:08:18 |
| Branding instalador | **PASS** | AppName PRIMOX Workshop; Publisher CamposCodingHub; dir `{commonpf}\PRIMOX\Workshop` |
| AssemblyCompany | **KNOWN LIMITATION** | ainda `Primo Auto Elétrica` (identidade de oficina); Product/Title = PRIMOX Workshop |
| SIGNING | **NOT CONFIGURED** | Authenticode não comprado/configurado |
| Artefato no Git | **não** versionado | `artifacts/` ignorado |

Comando:

```powershell
$env:DOTNET_ROLL_FORWARD='LatestMajor'
.\Scripts\Build-PrimoXCommercialRelease.ps1 -Version 1.0.0
```

---

## 3. Install / Startup / Uninstall

Ambiente: instalação silenciosa em `%LOCALAPPDATA%\PRIMOX-Workshop-InstallTest-15B` (isolado; não Program Files de produção).

| Teste | Resultado | Evidência |
|-------|-----------|-----------|
| Setup exists | **PASS** | Exit 0 |
| Silent install | **PASS** | EXE + `unins000.exe` presentes |
| Versão instalada | **PASS** | FV 1.0.0.0 / PV 1.0.0 / Product PRIMOX Workshop |
| Startup (sem smoke) | **PASS** | processo vivo, Responding=True, ~139 MB WS (~12 s) |
| Login interativo | **NOT TESTABLE** | sem Sandbox/UI assistida nesta sessão |
| CRUD primeira execução | **NOT TESTABLE** | mesma restrição |
| Smoke `--smoke-test` no EXE publicado | **BLOCKED** | processo não conclui / não cria DB sob filtro em 90 s (não usado como FAIL de packaging) |
| Uninstall silencioso | **PASS** | Exit 0; EXE removido; pasta de teste removida |
| Dados AppData preservados | **PASS** | marker + `primoauto.db` em `%LOCALAPPDATA%\PrimoAutoEletrica` permaneceram |
| Atalhos (ISS) | **PASS** (definição) | Desktop + Start Menu → `{app}\PrimoAutoEletrica.exe`; WorkingDir `{app}` |
| Sandbox Windows | **NOT TESTABLE** | não executada nesta sessão |
| Dual install legado PF `0.0.0.0` | **KNOWN LIMITATION** | **não** apagado automaticamente; AppId novo `PRIMOX.Workshop.1` |

**Política uninstall:** remove programa/DLLs/atalhos; **não** remove `%LOCALAPPDATA%\PrimoAutoEletrica` (banco/backups/config/mídia).

---

## 4. Database / Migrations

| Item | Valor |
|------|-------|
| SQLite path (runtime 1.0.0) | `%LOCALAPPDATA%\PrimoAutoEletrica\primoauto.db` (**não migrado** nesta fase) |
| Integrity (cópia LocalAppData) | **ok** |
| Foreign keys | **0** violações |
| Smoke DB (QaEngine) | 27 migrations; integrity ok |
| Código | **27** `ApplyMigration` (até `202609060001`) |
| LocalAppData SchemaMigrations | **32** |

### Diferença Banco − Código (5 extras)

| ID | Descrição (no banco) | Classificação | Notas |
|----|----------------------|---------------|-------|
| `202609060002` | Indices extras para listagens e filtros | **B** histórica aplicada, removida do código | SQL espelho em `Migrations/012_AddListagensIndices.sql` (histórico) |
| `202609060003` | 2FA TOTP em funcionarios | **B** | Colunas `TwoFactorEnabled`, `TotpSecret` presentes no prod LocalAppData |
| `202609070001` | Tabela Filiais para multi-filial local | **B** | Tabela `Filiais` existe; `FilialService` atual usa dados simulados |
| `202609070002` | CPF criptografado com blind index | **B** | aplicada 2026-09-07 |
| `202609070003` | Backfill CPF criptografado | **B** | aplicada 2026-09-07 |

**Código − Banco:** vazio (todo ID do código está no banco prod).

**Schema:** **SCHEMA DIVERGENT / KNOWN LIMITATION** — extras legítimas no banco de desenvolvimento/produção local; **não** apagar `SchemaMigrations`; **não** recriar banco. Fresh install (smoke) = 27 apenas. Core CRUD 1.0.0 validado no schema 27 (QaEngine).

---

## 5. Backup / Restore

| Item | Resultado |
|------|-----------|
| Serviço | `DatabaseBackupService` — pasta `%LOCALAPPDATA%\PrimoAutoEletrica\Backups` |
| Manual / automático / pre_migration | implementados no app (não recriados) |
| Restore E2E (arquivo, cópia smoke isolada) | **PASS** — backup file copy → mutate → restore → `probeTable` sumiu; integrity ok; tabelas Clientes/Veiculos/OS/Produtos/ContasReceber presentes |
| Restore via UI interativa | **NOT TESTABLE** nesta sessão |
| Backup embutido no Inno | **NÃO** (limitação honesta) |

---

## 6. Update

| Item | Estado |
|------|--------|
| UpdateService | parcial / existente |
| Auto-update comercial completo | **NOT IMPLEMENTED** |
| Upgrade E2E A→B | **NOT TESTABLE** (sem Versão B de packaging distinta) |

---

## 7. QA regressão (após packaging)

| Suite | Resultado | Evidência |
|-------|-----------|-----------|
| QaEngine | **37/37 PASS** | `TestResults/UiSmoke/2026-09-08_09-22-19` |
| Deep QA | **6/6 PASS** | `TestResults/UiSmoke/2026-09-08_09-28-50` |
| Long Run | **PASS** (incluído nas suites) | DeepQa / QaEngine |
| Regressões | **0** atribuíveis ao packaging | flaky concorrente anterior não reproduzido após isolamento |

---

## 8. CI

- SDK Actions: **6.0.x** (alinhado)  
- Publish self-contained win-x64; upload artifact ZIP auxiliar  
- **Não** publica GitHub Release automaticamente nesta fase  
- Inno no CI: ainda não obrigatório (Setup gerado localmente com ISCC 6.7.3)

---

## 9. Documentação

| Arquivo | Estado |
|---------|--------|
| `Installer/README_INSTALADOR.md` | atualizado |
| `INSTALLATION.md` | instruções usuário |
| `PROJECT_STATUS.md` | Fase 15B |
| `README.md` | seção deploy oficial |
| Este relatório | obrigatório |

---

## 10. Riscos restantes

1. Instalação legada `C:\Program Files\Primo Auto Elétrica` (EXE **0.0.0.0**) ainda pode coexistir — orientar remoção manual após validação  
2. Deploy LocalAppData\App (dev) vs Program Files oficial — educar atalho correto  
3. Schema 32 vs 27 em bancos antigos — não “limpar”; monitorar  
4. Logs ainda podem ir para `BaseDirectory\Logs` — KNOWN; sem refatoração ampla  
5. Sem Authenticode — SmartScreen  
6. Login/CRUD pós-install interativo sem evidência Sandbox nesta sessão  
7. AssemblyCompany legado vs Publisher CamposCodingHub  

---

## 11. Critérios §43 (resumo)

PASS: TFM, Publish, Inno, pipeline, Setup 1.0.0, EXE 1.0.0, branding instalador, atalho (ISS), install silencioso, startup processo, integrity/FK, migrations explicadas, backup serviço, restore arquivo E2E, uninstall+retenção, SHA256, CI alinhado, QaEngine/DeepQa, docs.  

PARTIAL/KNOWN: branding AssemblyCompany; dual install legado; schema divergente histórico.  

NOT TESTABLE / BLOCKED: Sandbox; login/CRUD interativo; smoke no pacote instalado; upgrade A→B; signing.  

**Nenhum BLOCKED convertido em PASS.**
