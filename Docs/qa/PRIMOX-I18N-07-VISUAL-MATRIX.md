# PRIMOX-I18N-07 — VISUAL MATRIX

**Pasta:** `Logs/qa-visual/i18n-07/{pt,en,es}/`  
**Resolução smoke:** 1600×900 · tema runtime padrão  
**Audit:** `i18n07-audit-20260910-193000.md`

| Tela | Idioma | Tema | Resolução | Resultado | Clipping | Overflow | Observação |
|------|--------|------|-----------|-----------|----------|----------|------------|
| shell-main | pt/en/es | default | 1600×900 | PASS | n/a | n/a | Captura shell |
| Dashboard | pt/en/es | default | 1600×900 | PASS | none critical | none | |
| Clientes | pt/en/es | default | 1600×900 | PASS | none | none | |
| Veículos | pt/en/es | default | 1600×900 | PASS | none | none | |
| OS | pt/en/es | default | 1600×900 | PASS | none | none | |
| Orçamentos | pt/en/es | default | 1600×900 | PASS | none | none | Status localizado |
| PDV | pt/en/es | default | 1600×900 | PASS | none | none | |
| Estoque | pt/en/es | default | 1600×900 | PASS | none | none | |
| Financeiro | pt/en/es | default | 1600×900 | PASS | none | none | |
| Agenda | pt/en/es | default | 1600×900 | PASS | none | none | |
| Relatórios | pt/en/es | default | 1600×900 | PASS | none | none | |
| Help | pt/en/es | default | 1600×900 | PARTIAL | n/a | n/a | Extended PT |
| Config/Login | — | — | — | PASS* | — | — | Via persistence probe |

Light/Dark e 1366/1920/2560: cobertos por DeepQa LongRun tema + evidência I18N-04/06 prévia; sem regressão visual crítica reportada nesta fase.

## Accessibility

- Tooltips/ações críticos alinhados ao idioma visual nos fluxos corrigidos
- Allowlist TECHNICAL não altera AutomationProperties existentes
