# PRIMOX-COMMERCIAL-09.5 — Visual Audit

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
