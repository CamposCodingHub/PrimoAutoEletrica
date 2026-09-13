# PRIMOX-COMMERCIAL-09.5 — Visual Audit

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

**Fase:** COMMERCIAL-09.5 Overnight  
**Decisão visual:** **PASS with known limitations** (sem P0/P1 de layout que impeçam operação)

## 1. Método

- ExhaustiveUi FullSimulation: **8 rounds** Light/Dark × 4 resoluções  
- Tema smoke: Light/Dark **2/2**  
- Sidebar smoke: PASS  
- I18n07: PT/EN/ES operacional  
- Screenshots / árvore: `TestResults/UiSmoke/ExhaustiveUi/` + AppData smoke `Logs/` (gitignored quando aplicável)  
- **Sem** inventar ferramenta nova de visual diff

## 2. Design system (PRIMOX)

| Token | Papel | Regressão overnight |
|-------|-------|---------------------|
| Orange | ação / brand | Sem excesso neon / gamer observado nos smokes |
| Navy | estrutura | Shell/sidebar coerente |
| Blue | técnico/info | Sem regressão reportada |
| Green / Amber / Red | sucesso / atenção / perigo | Login erro usa `DangerBrush` (tema) |
| Gold | **somente Sidebar hover** | Mantido restrito (Sidebar PASS) |

Não redesignado. Sem glassmorphism / gradientes agressivos introduzidos nesta fase.

## 3. Light mode

| Área | Resultado |
|------|-----------|
| Dashboard → Relatórios (módulos DeepQa) | Navegação OvernightQa PASS |
| Contraste texto/fundo | Sem FAIL Exhaustive / Tema |
| Botões / DataGrid / Dialog | Exhaustive PASS 1880 |

## 4. Dark mode

| Área | Resultado |
|------|-----------|
| Shell / modules | Exhaustive Dark rounds PASS |
| Inputs / Combo / Dialog | Sem FAIL |
| Calendar cabeçalho Dark | **KNOWN LIMITATION** WPF — **não** alterado nesta fase |

## 5. Layout / clipping

Resoluções exercitadas no Exhaustive:

| Resolução | Light | Dark |
|-----------|-------|------|
| 1366×768 | PASS | PASS |
| 1600×900 | PASS | PASS |
| 1920×1080 | PASS | PASS |
| 2560×1440 | PASS | PASS |

Resize intermediário / DPI: ver MATRIX (DPI BLOCKED).

## 6. Severidade visual

| Sev | Count | Notas |
|-----|-------|-------|
| P0 | 0 | — |
| P1 | 0 | — |
| P2 | 0 novos | Calendar Dark = histórico conhecido |
| P3 | 0 novos registrados | — |

## 7. Evidência

Não versionar screenshots gerados automaticamente se cobertos por `.gitignore`.  
Relatórios sumário: `exhaustive-summary-latest.md`, `exhaustive-tree-latest.md`.
