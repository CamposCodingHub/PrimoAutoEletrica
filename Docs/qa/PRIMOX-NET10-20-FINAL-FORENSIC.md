# PRIMOX NET10-20 — FINAL FORENSIC + PROMOTION GATE

> **DOCUMENTO REESCRITO EM CAMADAS — 2026-09-13**
>
> | Camada | Uso |
> |--------|-----|
> | **Estado atual** | Fonte operacional hoje · ver também `Docs/CURRENT-TRUTH.md` e NET10-26 |
> | **Avanços desta fase (histórico)** | Registro do que esta execução entregou — **não** sobrescrever mentalmente o estado atual |
>
> HEAD de referência pós-NET10-26: `1372e11` · TFM `net10.0-windows` · Branch `migration/net10`
> Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md`

## Estado atual (pós NET10-26 · 2026-09-13)

| Item | Valor |
|------|-------|
| Branch | `migration/net10` |
| HEAD fiscal foundation | `1372e11` |
| TFM | `net10.0-windows` |
| Unit | **194/194** |
| QaEngine | **43/43** |
| DeepQa | **6/6** (baseline NET10-26) |
| Fiscal LIVE / WhatsApp API / Code signing | **BLOCKED_EXTERNAL** |
| Calendar Dark | Mitigado (`CalendarContrastHealer`) — não citar KNOWN LIMITATION antigo como atual |
| NF-e | PARTIAL + TESTED (Focus path + Fake) |
| NFC-e / NFS-e | SCAFFOLD + FAKE_ONLY |
| DANFE | PDF informativo (≠ SEFAZ oficial) |
| Multiempresa fiscal | IMPLEMENTED + TESTED (DB) |

**Claims abaixo sobre net6, “emissão NÃO IMPLEMENTADO”, Unit 173, DANFE/cancel NI, Calendar Dark KNOWN LIMITATION, etc. pertencem ao registro histórico da fase.**

---

## Avanços desta fase (registro histórico — preservar)

**Timestamp:** 2026-09-13 (sessão CURRENT)  
**Branch:** `migration/net10`  
**Evidence root:** `TestResults/Net10-Overnight/20260913/NET10-20-Forensic/`

## 1–7. Git / TFM

| Item | Valor | Status |
|---|---|---|
| HEAD início | `88dee6b` | — |
| main | `29b19b1` | **PASS** intacta |
| v1.0.0 peeled | `a4ad6fe` | **PASS** intacta |
| primox-net6-final peeled | `29b19b1` | **PASS** intacta |
| TFM produto | `net10.0-windows` (csproj) | **PASS** |
| TFM testes ativos | `Tests/.../PrimoAutoEletrica.Tests.csproj` → net10.0-windows | **PASS** |
| net6 em scripts (defaults) | corrigidos → TFM do csproj | **PASS** (fix nesta sessão) |
| Directory.Build.* / global.json | ausentes | N/A |
| Pacotes vulneráveis (dotnet list) | nenhum | **PASS** |
| NU1701 OpenTK/SkiaSharp.Views.WPF | warnings classificados | LIMITAÇÃO não-bloqueante |

## 8–9. Build / Unit (CURRENT)

| Gate | COMMAND | EXIT | RESULT | EVIDENCE |
|---|---|---:|---|---|
| Restore | `dotnet restore` | 0 | OK | `03-restore.txt` |
| Debug build | `dotnet build -c Debug` | 0 | 0 erros · ~138 warnings | `04-build-debug*` |
| Release build | `dotnet build -c Release` | 0 | 0 erros · ~138 warnings | `04-build-release*` |
| Unit | `dotnet test` Release | 0 | **173/173** pass · 0 fail · 0 skip · ~4s | `05-unit.txt` |

## 10–13. QA / Deep / Exhaustive / Journey (CURRENT)

| Gate | EXIT | RESULT | EVIDENCE |
|---|---:|---|---|
| QaEngine | 0 | **43/43 APROVADO** | `QaEngine-summary.json` · 08:13:36 |
| DeepQa | 0 | **6/6 APROVADO** | `DeepQa-summary.json` · 08:16:15 |
| ExhaustiveUi #1 | 2 | **FAIL** · 1/1942 tested · CLIPBRD_E_CANT_OPEN (Kanban copy) sob carga paralela | `ExhaustiveUi/` |
| Correção | — | `ClipboardHelper.SetTextWithRetry` + scripts TFM | código |
| ExhaustiveUi #2 (sozinho) | 0 | **APROVADO** FullSimulation ~1247s | `ExhaustiveUi-retry1/` · 09:06:47 |
| Journeys A/B/C/D | 0 | **journeyFails=0** | `09-journey*.json` |

## 14. Database (CURRENT)

| Check | RESULT | EVIDENCE |
|---|---|---|
| A13Database | APROVADO 1/1 | `10-Database/A13Database/` |
| Clientes CRUD isolado | APROVADO | `10-Database/CRUD/` |
| integrity_check | **ok** | `10-Database/integrity.txt` |
| FK | foreign_keys=1 · fk_violations=**0** · migrations=**28** | same |
| tx rollback | after_rollback=0 | `tx-rollback.txt` |
| backup/restore/reopen | hashMatch=True · exit=0 | `backup-restore.txt` |

## 15. Security (CURRENT)

| Check | RESULT | EVIDENCE |
|---|---|---|
| A12Security | 3/3 APROVADO | `11-Security/` |
| A13Security | 1/1 APROVADO | same |
| A13Concurrency | 1/1 APROVADO | same |
| LoginSessao | 1/1 APROVADO | same |
| SecurityRedTeam | YELLOW · 0 Critical/High VULNERABLE | `RedTeam/security-redteam.json` |
| FOCUS token env | **False** | `focus-token-env.txt` |

## 16. Performance (CURRENT)

| Check | RESULT | EVIDENCE |
|---|---|---|
| Startup ×10 MainWindow | fails=0 · avg **1942.6** ms · min 1901 · max 2075 | `12-Performance/startup/startup-profile.json` |

## 17. Bulk (CURRENT)

| Check | RESULT | EVIDENCE |
|---|---|---|
| BulkDataQa13 | APROVADO 1/1 | `13-Bulk/BulkDataQa13/` |

## 18. Fiscal (CURRENT)

| Check | RESULT | EVIDENCE |
|---|---|---|
| Fake/Homolog unit filter | **46/46 PASS** | `14-Fiscal/fiscal-unit-rerun.txt` |
| Homologação LIVE Focus | **BLOCKED_EXTERNAL** | `PRIMOX_FOCUS_HOMOLOG_TOKEN` ausente |

## 19. Publish (CURRENT)

| Check | RESULT | EVIDENCE |
|---|---|---|
| `dotnet publish` win-x64 self-contained Trim=false SingleFile=false | EXIT 0 | `15-publish.txt` |
| Audit | 484 files · ~186.6 MB · PDB=0 · DB=0 | `15-publish-audit.txt` |
| EXE publicado Clientes+MainWindow | exit **0/0** | `15-published-smoke-exit.txt` |

## 20. Installer (CURRENT)

| Check | RESULT | EVIDENCE |
|---|---|---|
| Setup preview exists | True · SHA `3E361DB2…8729` · 65877816 bytes | `16-installer-precheck.txt` |
| 3× install/smoke/uninstall (PackagingE2E-net10) | **installerFails=0** | `16-installer-3cycles.json` |
| Comercial 1.0.0 SHA | `9A08494D…A9C5` intacto | `16-commercial-hash-after.txt` |
| Code signing | **BLOCKED_EXTERNAL** (sem thumbprint) | build log histórico + ambiente |

## 21. Side-by-side (CURRENT)

| Check | RESULT | EVIDENCE |
|---|---|---|
| Dual AppData net10 (bin + publish) | PASS · hashes distintos · exit 0/0 | `17-sxs-result.txt` |
| net6 EXE ↔ net10 no mesmo host | **BLOCKED_EXTERNAL** | `NET6_SDK_PRESENT=False` · `17-net6-presence.txt` |

## 22–23. Visual / Accessibility / I18N (CURRENT)

| Check | RESULT | EVIDENCE |
|---|---|---|
| Tema/I18n06/I18n07/OvernightQa/Sidebar/CommandCenter/Components/Calendar + módulos | **visualFails=0** | `18-visual-i18n.json` |
| Resoluções pixel-gallery dedicada | **NOT EXECUTED** | — |
| Calendar Dark header | KNOWN LIMITATION (WPF) | documentado |
| Accessibility dedicada | parcial (Components/Tema/focus via smokes) | LIMITAÇÃO |

## 24. Blockers / Limitations

### CRITICAL BLOCKERS (produto)
Nenhum P0/P1 reproduzido nesta sessão após correção clipboard.

### EXTERNAL BLOCKERS (impedem READY/100%)
1. **Fiscal LIVE** — token homologação ausente  
2. **Side-by-side vs .NET 6 binário** — SDK/EXE net6 ausente neste host  
3. **Code signing** — certificado/thumbprint comercial ausente  

### KNOWN LIMITATIONS
- NU1701 (OpenTK / SkiaSharp.Views.WPF netfx)  
- Gallery visual pixel / DPI matrix dedicada NOT EXECUTED  
- Accessibility suite não exhaustiva  
- ExhaustiveUi mensagem de PASS não reimprimiu contagens discovered/tested (OK sem fail)

## 25. Correções nesta sessão

1. Clipboard retry (`ClipboardHelper`) — Kanban/OS/2FA/FriendlyError  
2. Scripts Assurance/Commercial/Installer/E2E — default Framework vazio → TFM do csproj  

## 26. Final verdict

**APPROVED WITH LIMITATIONS**

**NÃO** READY / 100% — ver EXTERNAL BLOCKERS.  
**NÃO** merge · **NÃO** push · **NÃO** tag RC · main/tags intactas.
