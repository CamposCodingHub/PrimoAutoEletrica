# PRIMOX-COMMERCIAL-09.5 — Regression

**HEAD baseline:** `1966d2b`  
**Tag `v1.0.0`:** `72d85fa` intacta  

## 1. Critérios

Após qualquer P0/P1 real: Build + QaEngine + CompleteUi + DeepQa + LongRun + Exhaustive + DB + Security.  
Nesta fase: falhas iniciais = harness → correção assert/timeout → reexecução LoginSessao + OvernightQa + QaEngine.

## 2. Matriz de regressão

| Camada | Overnight | Pós-fix | Verdict |
|--------|-----------|----------|---------|
| Build Debug/Release | PASS | — | PASS |
| Localization + Fiscal units | via UnitTests **162/162** | — | PASS |
| QaEngine (Complete UI) | 43/43 | 43/43 | PASS |
| DeepQa | 6/6 (+ repeats) | — | PASS |
| LongRun | PASS | — | PASS |
| ExhaustiveUi | PASS 1880/0/0 | — | PASS |
| DB integrity / FK / migration check | PASS | — | PASS |
| Security / secret scan | PASS | — | PASS |
| Fiscal (sem emissão real) | regressão via suites | — | PASS |
| I18N-07 | PASS | — | PASS (frente encerrada) |
| Installer | SKIPPED → C08 | — | PASS (baseline) |
| Code signing | fora de escopo | — | BLOCKED externo |

## 3. Comparação com baseline C08/C09

| Indicador | C08/C09 | C09.5 |
|-----------|---------|-------|
| QaEngine | 43/43 | 43/43 |
| DeepQa | 6/6 | 6/6 |
| Exhaustive | PASS | PASS (1880) |
| Units | 162/162 | 162/162 |
| Startup stress | n/a | **50/50** novo |
| OvernightQa harness | n/a | **3/3** pós-fix |

**Sem regressão funcional** vs baseline comercial recente.

## 4. Fiscal / I18N / Installer / Signing

| Área | Ação nesta fase |
|------|-----------------|
| `IFiscalProvider` / Focus / NFeEmissao / FiscalConfiguration | **não** alterados |
| I18N architecture | **não** alterada; I18N-07 permanece fechada |
| Installer architecture | **não** alterada; regressão via C08 |
| Authenticode | **não** alterado; continua BLOCKED BY EXTERNAL CERTIFICATE |

## 5. Conclusão regressão

**PASS** com limitações de ambiente documentadas (DPI, installer skip-in-session, signing).
