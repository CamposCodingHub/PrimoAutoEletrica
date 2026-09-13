# PRIMOX NET10 — PROMOTION READINESS AUDIT

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
**Objective:** Determinar se `migration/net10` está pronta para promoção controlada a `main` / candidato **1.1.0**.  
**Branch:** `migration/net10`  
**HEAD auditado:** `19587e7`  
**Evidence root:** `TestResults/Net10-Overnight/20260913/NET10-Promotion-Readiness/`  
**MERGE:** NÃO · **PUSH:** NÃO · **TAGS:** inalteradas

> Zero-trust: PASS somente com execução nesta sessão.

---

## 1–3. Baseline / Git protection

| Ref | Valor | Status |
|---|---|---|
| Branch | `migration/net10` | OK |
| HEAD | `19587e7` | OK |
| `main` | `29b19b1` | **INTACT** |
| `v1.0.0` peeled | `a4ad6fe` | **INTACT** |
| `primox-net6-final` peeled | `29b19b1` | **INTACT** |

Commit audit: `Docs/qa/PRIMOX-NET10-PROMOTION-COMMIT-AUDIT.md`  
- `main..HEAD` = **27** commits · `HEAD..main` = **0**  
- Ancestralidade: NET10 é descendente linear de `main`.

---

## 4–5. Diff audit (`main...HEAD`)

| Métrica | Valor |
|---|---|
| Files | 60 |
| +/− | +2209 / −46 |

| Classe | Exemplos |
|---|---|
| A NET10 migration | TFM `net10.0-windows`, package bumps, scripts TFM |
| B bug fix | ClipboardHelper, smoke tab i18n |
| C security | SecurityRedTeam path harden |
| D UI/UX | CalendarContrastHealer, Calendar.xaml |
| E installer | Build/PackagingE2E scripts TFM-aware |
| F tests | Tests TFM net10 |
| G QA infra | Run-UiSmoke TFM from csproj |
| H documentation | Docs/qa NET10-00…23 |
| I unrelated | nenhum bloqueante |
| J suspicious | nenhum (LiveCharts remove = unused dependency) |

---

## 6. TFM audit

| Item | Resultado |
|---|---|
| Produto ACTIVE | `PrimoAutoEletrica.csproj` → **`net10.0-windows`** |
| Unit ACTIVE | `Tests/...Tests.csproj` → **`net10.0-windows`** |
| ACTIVE `net6.0-windows` csproj | **0** |
| Api/Simulation/Tools net9 | TOOL / non-product |

---

## 7. Dependency audit (CURRENT)

| Check | Comando | Exit | Resultado |
|---|---|---:|---|
| Restore | `dotnet restore` | 0 | OK |
| NU1701 | count in restore log | — | **0** |
| NU1605 | count | — | **0** |
| OpenTK / LiveCharts | transitive list | — | **ausentes** |
| Sqlite | Microsoft.Data.Sqlite **9.0.9** · SQLitePCLRaw **3.0.5** | — | OK |

---

## 8. Build (CURRENT)

| Config | Exit | Errors | Evidência |
|---|---:|---|---|
| Debug | **0** | **0** | `05-build-debug.txt` · ~11:50 |
| Release | **0** | **0** | `05-build-release.txt` |

Warnings: CA1416 Windows-only / nullable pré-existentes — classificados não-bloqueantes.

---

## 9–12. Unit / QA / Deep QA / Journey (CURRENT)

| Gate | Exit | Resultado | Timestamp / evidência |
|---|---:|---|---|
| Unit | 0 | **173/173** | `06-unit.txt` |
| QaEngine | 0 | **43/43 APROVADO** | `06-QaEngine/` · 11:56 |
| DeepQa | 0 | **6/6 APROVADO** | `06-DeepQa/` · 11:58 |
| Journey | 0 | **fails=0** (12 módulos) | `06-journey-matrix.txt` |

Journey modules: Login · Dashboard · Clientes · Veículos · OS · Estoque · Financeiro · Agenda · Relatórios · Config · Tema · Calendar — todos APROVADO.

---

## 13. Database (CURRENT)

| Check | Resultado | Evidência |
|---|---|---|
| A13Database | **APROVADO** 1/1 | `07-Database/` · IntegrityOrphanDuplicate PASS |
| Banco | isolado em appdata de smoke | não toca DB comercial usuário |
| Installer seed integrity | **ok** (ciclos E2E) | `10-commercial08-report.md` |

---

## 14. Security (CURRENT)

| Check | Resultado |
|---|---|
| A12Security | **3/3 APROVADO** |
| A13Security | **1/1 APROVADO** |
| LoginSessao | **APROVADO** |
| RedTeam | **YELLOW** · Critical=**0** · High=**0** · Info=6 | `08-RedTeam/security-redteam.json` |

---

## 15. Publish (CURRENT)

| Item | Valor |
|---|---|
| Dir | `artifacts/publish/net10-promotion-readiness-win-x64/` |
| Exit | **0** |
| Files | **472** · ~177.0 MB · PDB=**0** |
| OpenTK / LiveCharts | **0 / 0** |
| EXE SHA256 | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` |
| Published Clientes | exit **0** |

---

## 16–17. Installer / Reinstall (CURRENT)

| Item | Valor |
|---|---|
| Setup | `PRIMOX-Workshop-Setup-1.1.0-PackagingE2E.exe` |
| Size / SHA256 | 54195878 · `9E03914B709E0F0E22330E9BF65DC26B7B80A38145CA6E4BC448C84DAD2C8537` |
| E2E script exit | **0** · FailCount=**0** |
| Cycle 1/2/3 | **PASS / PASS / PASS** |
| Final reinstall + data | **PASS** · integrity=ok |
| Comercial 1.0.0 BEFORE/AFTER | `9A08494D…A9C5` · 61686497 · **INTACTO** |

---

## 18. Regression vs NET6 commercial (capability)

| Item | Classificação |
|---|---|
| Módulos comerciais (Clientes…Config) | PARITY — journeys PASS |
| LiveCharts package removed | **EXPECTED** (unused; UI charts = ItemsControl) |
| TFM net6 → net10 | **EXPECTED** migration |
| AppData policy (preserve on uninstall) | PARITY — confirmed in E2E |
| NET6 EXE side-by-side | **BLOCKED_EXTERNAL** (SDK6/EXE ausentes) — não regressão de produto |

**NET10 não removeu capacidade comercial demonstrável nesta sessão.**

---

## 19. Visual (CURRENT)

| Check | Resultado |
|---|---|
| Tema / Calendar / módulos journey | APROVADO |
| Calendar Dark PNG | header branco legível (`13-CalendarPng/`) |

---

## 20. Performance (CURRENT)

| | Valor |
|---|---|
| Cycles | 10 |
| Min / Max / Avg | 1943 / 2055 / **1968.7** ms |
| Failures | **0** |
| NET6 compare | **BLOCKED_EXTERNAL** (SDK6 ausente) |

---

## 21. Fiscal (CURRENT)

| Check | Resultado |
|---|---|
| Fake/Homolog unit filter | **46/46 PASS** · `16-fiscal-unit.txt` |
| LIVE (`PRIMOX_FOCUS_HOMOLOG_TOKEN`) | **BLOCKED_EXTERNAL** |

---

## 22. Signing (CURRENT)

| Check | Resultado |
|---|---|
| SignTool | presente |
| Thumbprint comercial | **ausente** |
| Status | **BLOCKED_EXTERNAL** |

---

## 23–24. Limitations / blockers

### Technical limitations
Nenhuma relevante nesta sessão (NU1701=0 · Calendar Dark PASS · installer E2E PASS).

### External limitations
- Fiscal LIVE  
- Code Signing comercial  
- .NET6 Side-by-Side  

### Critical blockers
**Nenhum P0/P1** nesta sessão.

---

## 25. Final verdict

**APPROVED WITH EXTERNAL LIMITATIONS**

O produto em `migration/net10` está tecnicamente pronto para preparação de promoção manual a candidato **1.1.0**, segundo gates executados nesta sessão.  
Promoção comercial completa continua dependente de recursos externos (token Focus, certificado, ambiente net6 SxS).

**NÃO autorizado nesta fase:** merge · push · alteração de tags · alteração de `main` / `v1.0.0` / `primox-net6-final`.

---

## 26–29. Exact refs / timestamp

| Item | Valor |
|---|---|
| HEAD auditado | `19587e7` |
| main | `29b19b1` |
| v1.0.0 | `a4ad6fe` |
| primox-net6-final | `29b19b1` |
| Timestamp | 2026-09-13 |
| Commit desta auditoria | `04e8d91` |
