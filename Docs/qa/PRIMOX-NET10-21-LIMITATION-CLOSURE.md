# PRIMOX NET10-21 — LIMITATION CLOSURE

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
**HEAD início:** `505435b`  
**Evidence root:** `TestResults/Net10-Overnight/20260913/NET10-21-Closure/`

> Zero-trust: PASS somente com execução nesta sessão. Histórico NET10-20 ≠ CURRENT.  
> Sem merge / push / tag / alteração de `main`.

---

## 21.01 — Git forensic

| Ref | Valor | Status |
|---|---|---|
| Branch | `migration/net10` | OK |
| HEAD início | `505435b` | OK |
| `main` | `29b19b1` | **PASS** intacta |
| `v1.0.0` peeled | `a4ad6fe` | **PASS** intacta |
| `primox-net6-final` peeled | `29b19b1` | **PASS** intacta |
| TFM produto | `net10.0-windows` | **PASS** |

---

## 21.02 / 21.03 — NU1701 (OpenTK / SkiaSharp)

| Campo | Valor |
|---|---|
| Packages | `OpenTK 3.3.1` · `OpenTK.GLWpfControl 3.3.0` · `SkiaSharp.Views.WPF 3.116.1` |
| Source | **transitiva** via `LiveChartsCore.SkiaSharpView.WPF 2.0.0-rc5.4` |
| Chain | LiveCharts WPF → SkiaSharp.Views.WPF → OpenTK* |
| Warning | NU1701 (restaurados como .NETFramework 4.x vs `net10.0-windows7.0`) |
| Probe | LiveCharts **2.0.5** → ainda **3× NU1701** → **revertido** para `2.0.0-rc5.4` |
| Runtime | Relatorios smoke **6/6 APROVADO** (usa charts) |
| Classificação | **B — aceitável** |
| Decisão | **NU1701 RETAINED — JUSTIFIED** |

Não forçar upgrade: não remove o warning e arrisca regressão de charts.

---

## 21.04 — Calendar Dark

| Check | Resultado | Evidência |
|---|---|---|
| Smoke Calendar | **4/4 APROVADO** (antes e após ajuste) | `smoke-Calendar/` · `21-04-retest-Calendar/` |
| Interação Dark/Light | PASS funcional (mês/ano/seleção/nav) | Calendar smoke |
| Tentativa de correção | `PremiumCalendarItemStyle` **somente setters** (Foreground/Background) — **sem** ControlTemplate | `Themes/Calendar.xaml` |
| Reteste pós-ajuste | Calendar/Tema/Components/Agendamentos **APROVADO** | `21-04-retest-matrix.txt` |
| Inspeção visual PNG Dark | Header “setembro de 2026” + setas **baixo contraste** | `21-04-CalendarAfter/agendamentos-calendar-dark.png` |
| Decisão | **KNOWN LIMITATION** (header Dark WPF) · funcional **PASS** | ControlTemplate CalendarItem permanece bloqueado (histórico 0 dias) |

**Não** declarar CALENDAR DARK CLOSED.

---

## 21.05 — Visual Gallery

| Item | Valor |
|---|---|
| Infra | DeepQa + capturas `Logs/qa-visual` (existente) |
| DeepQa | **6/6 APROVADO** |
| PNGs coletados | **58** em `21-05-Gallery/` |
| Por resolução | calendar=2 · 1366x768=26 · 1600x900=10 · 1920x1080=10 · 2560x1440=10 |
| Temas | Light + Dark |
| Telas | Dashboard, OS, Clientes, Agenda, Estoque, Financeiro, Relatórios, PDV, Help, etc. |
| Pixel-perfect compare | **NOT EXECUTED** (não confundir inspeção com pixel-perfect) |
| Classificação | **PASS** (gallery dedicada executada; não pixel-perfect) |

---

## 21.06 — Accessibility

| Antes (NET10-20) | Nesta sessão | Depois |
|---|---|---|
| PASS parcial | Components **1/1** · Tema · Calendar nav PART_* · journeys Config/Login | **PASS** (smoke/foco/teclado via harness) |

**Não** afirmar WCAG 100% — sem suite WCAG dedicada / ferramenta automatizada completa.

---

## 21.07 — .NET 6 side-by-side

| Check | Resultado |
|---|---|
| `dotnet --list-sdks` | SDK 6 **ausente** (`NET6_SDK=False`) |
| EXE net6 v1.0.0 no host | não disponível para SxS |
| Decisão | **BLOCKED_EXTERNAL** |

> Side-by-side net6 não foi executado porque o artefato/runtime necessário não está disponível neste host.

---

## 21.08 — Code signing

| Check | Resultado |
|---|---|
| SignTool | **encontrado** (Windows Kits 10.0.26100.0 x64) |
| Thumbprint env | **False** |
| Certificado comercial CurrentUser\My | ausente / sem match |
| Decisão | **BLOCKED_EXTERNAL** |

Não criar certificado fake / localhost.

---

## 21.09 — Fiscal LIVE

| Check | Resultado |
|---|---|
| `PRIMOX_FOCUS_HOMOLOG_TOKEN` | **False** (valor nunca impresso) |
| Homologação LIVE | **BLOCKED_EXTERNAL** |
| Fake/unit fiscal | **HISTÓRICAL** NET10-20 46/46 — **não** reexecutado como LIVE |

Fake ≠ LIVE HOMOLOGATION.

---

## 21.10 — Residual .NET 6

| Escopo | Resultado |
|---|---|
| Produto + testes unit | `net10.0-windows` |
| ACTIVE csproj residual (início) | `PrimoAutoEletrica.UiTests` · `Commercial08DataSeed` em `net6.0` |
| Correção | ambos → `net10.0`; seed Sqlite → 9.0.9 + SQLitePCLRaw 3.0.5 (fecha NU1903 no tool) |
| ACTIVE após | **0** hits `net6.0` em `*.csproj/*.props/*.targets/*.pubxml` |
| HISTORICAL / Docs / Logs | referências net6 **preservadas** (não apagar histórico) |
| Scripts | `Build-PrimoXCommercialRelease.ps1` aceita `netN.0-windows` do csproj |

---

## 21.11 — Publish forensic

| Item | Valor |
|---|---|
| Command | `dotnet publish` Release self-contained win-x64 · SingleFile=false · DebugSymbols=false |
| Output | `artifacts/publish/net10-21-closure-win-x64/` |
| Files | **484** · ~195.7 MB · **PDB=0** · **DB=0** |
| EXE SHA256 | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` |
| Published smoke Clientes | exit **0** |
| Comercial 1.0.0 | SHA `9A08494D…A9C5` · 61686497 bytes · **INTACTO** |
| Preview 1.1.0-net10 | SHA `3E361DB2…8729` · 65877816 bytes |

---

## 21.12 — Critical regression (CURRENT)

| Gate | Resultado | Evidência |
|---|---|---|
| Build Debug | **0 erros** | `21-12-build-debug.txt` |
| Build Release | **0 erros** | `21-12-build.txt` / `21-04-build-after-calendar.txt` |
| Unit | **173/173** | `21-12-unit.txt` · `21-12-unit-after.txt` |
| QaEngine | **43/43 APROVADO** | `21-12-QaEngine/` |
| DeepQa | **6/6 APROVADO** | `DeepQa/` |
| Journeys críticas | **journeyFails=0** | `21-12-journey-matrix.txt` |
| Módulos | Login · Dashboard · OS · Clientes · Veículos · Agenda · Estoque · Financeiro · Relatórios · Config | APROVADO cada um |

---

## 21.13 — Database safety

| Check | Resultado |
|---|---|
| A13Database | **APROVADO** (`IntegrityOrphanDuplicate` PASS) |
| DB isolado | `21-13-Database/appdata/primoauto.db` (não banco do usuário) |
| sqlite3 CLI | ausente no host — integrity via smoke A13 |
| Esperado | integrity ok / FK 0 (coberto por A13) |

---

## 21.14 — Security regression

| Check | Resultado |
|---|---|
| A12Security | **APROVADO** |
| LoginSessao (retest) | **APROVADO** |
| Critical | **0** nesta sessão |

---

## 21.15 — Limitation matrix

| LIMITAÇÃO | STATUS | EVIDÊNCIA | AÇÃO |
|---|---|---|---|
| NU1701 OpenTK/SkiaSharp | **RETAINED — JUSTIFIED** | matrix + Relatorios PASS | manter LiveCharts; não forçar |
| Calendar Dark header | **KNOWN LIMITATION** | PNG Dark pós-fix | sem ControlTemplate CalendarItem |
| Gallery pixel | **PASS** | 58 PNG · DeepQa 6/6 | não pixel-perfect |
| Accessibility | **PASS** | Components/Tema/journeys | sem WCAG 100% |
| .NET6 SxS | **BLOCKED_EXTERNAL** | SDK6 ausente | host com net6/EXE |
| Fiscal LIVE | **BLOCKED_EXTERNAL** | token ausente | token homolog |
| Code Signing | **BLOCKED_EXTERNAL** | sem thumbprint comercial | cert comercial |
| Residual TFM net6 ativo | **CLOSED** | ACTIVE_AFTER=0 | tools migrados |

---

## 21.16 — Critério 100% / READY

Checklist honesto:

- [x] main / v1.0.0 / primox-net6-final intactas  
- [x] .NET10 ativo  
- [x] Build Debug/Release PASS  
- [x] Unit / QaEngine / DeepQa / journeys PASS  
- [x] DB / Security PASS (smoke)  
- [x] Visual gallery PASS (não pixel-perfect)  
- [x] Accessibility PASS (harness; não WCAG 100%)  
- [x] publish PASS  
- [x] installer comercial intacto (hash)  
- [ ] Fiscal LIVE  
- [ ] Code Signing comercial  
- [ ] .NET6 SxS  

Limitações externas restantes ⇒ **não** READY / 100%.

---

## FINAL VERDICT

**APPROVED WITH LIMITATIONS**

Produto tecnicamente sólido em `net10.0-windows` nesta branch.  
Blockers externos legítimos (token Focus, certificado comercial, runtime/EXE net6) impedem declaração 100%.  
NU1701 e Calendar Dark header permanecem limitações técnicas justificadas / conhecidas, não P0.
