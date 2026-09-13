# PRIMOX NET10-22 — FINAL HARDENING + PROMOTION PREPARATION

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

**Timestamp:** 2026-09-13  
**Branch:** `migration/net10`  
**HEAD início:** `ec4b8d8`  
**Evidence:** `TestResults/Net10-Overnight/20260913/NET10-22-Hardening/`  
**Comparação:** `Docs/qa/PRIMOX-NET10-COMPARISON.md`

> Zero-trust · sem merge · sem push · sem mover tags · refs protegidas intactas.

---

## F00 — Git

| Ref | Valor | Status |
|---|---|---|
| Branch | `migration/net10` | OK |
| HEAD início | `ec4b8d8` | OK |
| `main` | `29b19b1` | **PASS** |
| `v1.0.0` peeled | `a4ad6fe` | **PASS** |
| `primox-net6-final` peeled | `29b19b1` | **PASS** |

---

## F01 — Baseline CURRENT (pré-alteração)

| Gate | Resultado |
|---|---|
| Build Debug/Release | **0 erros** |
| Unit | **173/173** |
| QaEngine | **43/43 APROVADO** |
| DeepQa | **6/6 APROVADO** |

---

## F02–F04 — NU1701

### Forensic

| Campo | Valor |
|---|---|
| Origem | `LiveChartsCore.SkiaSharpView.WPF 2.0.0-rc5.4` (direto no csproj) |
| Transitivos NU1701 | OpenTK 3.3.1 · OpenTK.GLWpfControl 3.3.0 · SkiaSharp.Views.WPF 3.116.1 |
| Uso em código/XAML | **ZERO** (`LiveChartsCore` / `lvc:` / `CartesianChart` / `SkiaSharpView` — sem matches) |
| UI de gráficos | `Financeiro` / `Relatorios` via **ItemsControl** custom (AUDITORIA: reestruturado sem LiveCharts.Wpf) |
| Classificação uso | **UNUSED DEPENDENCY** (direto órfão) |

### Decisão: **A — REMOVE**

Pacote removido do `PrimoAutoEletrica.csproj`.

### Reteste

| Check | Resultado |
|---|---|
| `dotnet restore` NU1701 count | **0** |
| Build Debug/Release | **0 erros** · NU1701_RELEASE=0 |
| Unit | **173/173** |
| Relatorios / Financeiro / Dashboard | **APROVADO** |
| QaEngine / DeepQa pós-fix | **43/43** · **6/6** |
| Publish LiveCharts/OpenTK files | **0 / 0** |

**NU1701 CLOSED**

---

## F05–F07 — Calendar Dark

### Reprodução

PNG Dark pré-fix: header “setembro de 2026” + setas baixo contraste  
(`05-CalendarBefore/`).

### Correção segura

`Helpers/CalendarContrastHealer.cs`:

- **Não** substitui ControlTemplate de `CalendarItem`
- Aplica `PrimaryTextBrush` em `PART_HeaderButton` / prev / next + TextBlocks de título
- Paths de seta **somente** se glifo pequeno (≤14px) — evita blocos sólidos
- Hook: class `Loaded` + `ThemeService.ApplyTheme` → `HealAllOpen`
- Tentativa agressiva (Fill em todos Paths) **reprovada visualmente** → refinada

### Reteste

| Check | Resultado |
|---|---|
| Calendar smoke | **APROVADO** |
| PNG Dark após | header **branco legível** · setas visíveis (`05-CalendarAfter2/`) |
| PNG Light após | header escuro legível · setas escuras preservadas |
| Tema / Agenda / Components | **APROVADO** |

**CALENDAR DARK CLOSED**

---

## F08–F09 — Visual / Accessibility

| Item | Resultado |
|---|---|
| DeepQa (multi-res Light/Dark) | **6/6 APROVADO** (baseline + pós) |
| Pixel-perfect compare | **NOT EXECUTED** |
| Accessibility (Components/Tema/Calendar nav) | **PASS** harness — **não** WCAG 100% |

---

## F10 — Residual .NET 6

| Escopo | Resultado |
|---|---|
| ACTIVE `*.csproj/*.props/*.targets/*.pubxml` | **0** hits `net6.0` |
| `git grep net6.0` geral | hits em Docs / Logs / histórico — **DOCUMENTATION / HISTORICAL** |
| Configuração ativa involuntária | **nenhuma** |

---

## F11 — Comparação NET6 × NET10

Ver `Docs/qa/PRIMOX-NET10-COMPARISON.md`.

Resumo: paridade funcional; NET10 fecha NU1701 e Calendar Dark; externos iguais.

---

## F12 — Performance

| | Resultado |
|---|---|
| NET6 | **NOT EXECUTED** neste host (sem SDK/EXE net6) |
| NET10 CURRENT ×10 | fails=0 · avg **1871.5** ms · min 1851 · max 1923 |
| NET10-20 hist (ref) | avg 1942.6 ms |

Não tratar diferença de ambiente como regressão.

---

## F13 — Publish

| Item | Valor |
|---|---|
| Output | `artifacts/publish/net10-22-hardening-win-x64/` |
| Files | **472** · ~177.0 MB · PDB=0 · DB=0 |
| LiveCharts/OpenTK | **0** arquivos |
| Published Clientes | exit **0** |
| EXE SHA256 (apphost) | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` |

---

## F14 — Installer

| Item | Resultado |
|---|---|
| Comercial 1.0.0 | SHA `9A08494D…A9C5` · 61686497 · **INTACTO** |
| Preview 1.1.0-net10 | SHA `3E361DB2…8729` presente |
| E2E cycles esta sessão | **NOT EXECUTED** (PackagingE2E setup do preview ausente; comercial não regenerado) |
| NET10-20 E2E | HISTORICAL fails=0 |

---

## F15–F17 — Externos

| Item | Status |
|---|---|
| Fiscal LIVE (`PRIMOX_FOCUS_HOMOLOG_TOKEN`) | **BLOCKED_EXTERNAL** |
| Code Signing (SignTool OK · thumbprint comercial ausente) | **BLOCKED_EXTERNAL** |
| .NET6 SxS (SDK6 False) | **BLOCKED_EXTERNAL** |

---

## F18–F19 — Regressão / Security

| Gate | Resultado |
|---|---|
| Unit pós-fix | **173/173** |
| QaEngine pós-fix | **43/43** |
| DeepQa pós-fix | **6/6** |
| Login / Dashboard / Agenda / Relatorios / Financeiro | **APROVADO** |
| A12Security | **3/3** |
| A13Database | **1/1** |
| Critical | **0** |

---

## F20 — Limitation matrix

| LIMITAÇÃO | RESULTADO | EVIDÊNCIA | STATUS |
|---|---|---|---|
| NU1701 | removido LiveCharts unused | NU1701=0 · publish sem OpenTK | **CLOSED** |
| Calendar Dark | ContrastHealer | PNG Before/After2 | **CLOSED** |
| Gallery pixel | DeepQa + PNGs | 18-DeepQa | **PASS** (não pixel-perfect) |
| Accessibility | Components/Tema/Calendar | smokes | **PASS** (não WCAG 100%) |
| Fiscal LIVE | token ausente | 15-fiscal.txt | **BLOCKED_EXTERNAL** |
| Code Signing | sem cert comercial | 16-signing.txt | **BLOCKED_EXTERNAL** |
| .NET6 SxS | SDK6 ausente | 17-net6.txt | **BLOCKED_EXTERNAL** |
| Installer E2E (esta sessão) | PackagingE2E preview ausente | 14-installer-status.txt | **NOT EXECUTED** |

---

## F21 — Critério READY

Limitações técnicas internas conhecidas desta trilha: **fechadas**.  
P0/P1: **nenhum**.  
Externos: permanecem **EXTERNAL DEPENDENCY** (não defeito).  
Installer E2E desta sessão: **NOT EXECUTED** ⇒ não declarar READY/100% absoluto.

**FINAL VERDICT: APPROVED WITH LIMITATIONS**  
(pronto para preparação de promoção; bloqueios restantes são externos / E2E instalador não reexecutado aqui)

---

## Alterações de código

1. Remoção `LiveChartsCore.SkiaSharpView.WPF` do csproj  
2. `CalendarContrastHealer` + registro em `App` / `ThemeService`  
3. Smoke Calendar chama Heal antes do PNG  
4. Matrix Finalization A11Y/THEME Calendar → PASS  
5. Docs NET10-22 + COMPARISON + PROJECT_STATUS
