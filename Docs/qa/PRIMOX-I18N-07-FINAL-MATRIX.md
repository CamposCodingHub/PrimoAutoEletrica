# PRIMOX-I18N-07 — FINAL MATRIX (user-visible)

Status legend: PASS · PARTIAL · FAIL · LEGITIMATE · TECHNICAL · FISCAL · DATA · INTERNAL

| Módulo | Tela | Elemento | PT | EN | ES | Categoria | Prioridade | Status | Observação |
|--------|------|----------|----|----|----|-----------|------------|--------|------------|
| Shell | Main | Operations label | Operações | Operations | Operaciones | UI | P0 | PASS | Binding Operations |
| Clientes | Grid | Primary vehicle header | Veículo principal | Primary vehicle | Vehículo principal | UI | P1 | PASS | PrimaryVehicle |
| Veículos | Lista | Contagem | 1 veículo… | 1 vehicle found | 1 vehículo… | UI | P1 | PASS | Gate |
| Veículos | Insight | Histórico OS/Agenda | Histórico com… | History with… | Historial con… | UI | P1 | PASS | Gate |
| Orçamentos | Status | Draft/Rascunho display | Rascunho | Draft | Borrador | UI | P0 | PASS | QuoteStatusLocalizer |
| Orçamentos | MessageBox | Duplicate/convert/delete | PT | EN | ES | UI | P0 | PASS | UiText Gate/Interaction |
| Orçamentos | Painel | Last 10 / summary | PT | EN | ES | UI | P1 | PASS | Gate bindings |
| PDV | Fluxo | Ações/atalhos | PT | EN | ES | UI | P0 | PASS | I18N-06 + cognatos ES |
| Estoque | Empty | Min / expiry | PT | EN | ES | UI | P1 | PASS | Gate + UiText |
| Estoque | Ficha | Card/search hints | PT | EN | ES | UI | P1 | PASS | |
| Financeiro | Módulo | Chrome | PT | EN | ES | UI | P0 | PASS | |
| Agenda | Subtitle + Vehicle btn | PT | EN | ES | UI | P1 | PASS | |
| Fornecedores | Delete/Search | PT | EN | ES | UI | P1 | PASS | |
| Relatórios | Empty states | PT | EN | ES | UI | P1 | PASS | Gate |
| Relatórios | Audit filters Sucesso/Falha | PT=EN codes | — | — | TECHNICAL | — | LEGITIMATE | Valores alinhados a filtro/auditoria |
| Help | Core | Primeiros passos | PT | EN | ES | UI | P3 | PASS | Help Core usable |
| Help | Extended | Guias longos | PT | PARTIAL | PARTIAL | UI | P3 | PARTIAL | Exceção documentada |
| Login/Config | Idioma | Persistência | — | — | — | UI | P0 | PASS | Probe I18n07 |
| Calendar | Dark header | Native WPF | — | — | — | TECHNICAL | — | LEGITIMATE | Limitação conhecida |
| Dados | Nomes/placas | Demo/user | — | — | — | DATA | — | LEGITIMATE | Não traduzir |

## USER_VISIBLE_STRICT (smoke I18n07)

| Idioma | Strict |
|--------|--------|
| PT | 100% |
| EN | **93,3%** |
| ES | **93,3%** |

## CRITICAL_FLOW_ZERO_RESIDUAL

| Fluxo | PT | EN | ES |
|-------|----|----|----|
| Clientes | PASS | PASS | PASS |
| Veículos | PASS | PASS | PASS |
| OS | PASS | PASS | PASS |
| Orçamentos | PASS | PASS | PASS |
| PDV | PASS | PASS | PASS |
| Estoque | PASS | PASS | PASS |
| Financeiro | PASS | PASS | PASS |
| Agenda | PASS | PASS | PASS |
| Relatórios | PASS | PASS | PASS |
| Login | PASS* | PASS* | PASS* |
| Configurações | PASS* | PASS* | PASS* |

\*Persistência/fallback/runtime switch cobertos pelo audit I18n07 (não módulo sidebar).
