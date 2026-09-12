# PRIMOX FULL ASSURANCE-13 — PERFORMANCE

**Data:** 11/09/2026

## Startup (10 ciclos, `--smoke-filter=MainWindow`)

Evidência: `TestResults/UiSmoke/a13-startup-profile/startup-profile.json`

| Métrica | ms |
|---|---:|
| MIN | 2030 |
| MAX | 2114 |
| AVG | 2062.7 |
| MEDIAN | 2056 |
| Failures | 0 |

## Navigation / recovery sample (`A13Performance`)

Evidência: `…/A13Performance/appdata/Logs/a13-performance/sample.txt`

| Contador | Before | After (5× Dashboard + GC) |
|---|---:|---:|
| WorkingSet (bytes) | 74_375_168 | 187_367_424 |
| Handles | 366 | 734 |
| CPU total (ms) | 891 | 5141 |

## Bulk

Bulk QA13 concluiu ~9–14 s sem OOM; saldo estoque consistente. Pico durante bulk **não** instrumentado processo-a-processo além do smoke host (harness in-process).

## Classificação

| Área | Resultado | Notas |
|---|---|---|
| MEMORY | **EXPECTED GROWTH** / **REVIEW** | Crescimento WPF após abrir Dashboard é esperado; sem prova de leak persistente multi-sessão |
| CPU | **STABLE** | Startup estável; smoke counters sem anomalia |
| HANDLES | **REVIEW** | +368 handles após 5 hosts; abaixo do limiar de falha do harness (+5000) |

**PERFORMANCE FINAL = YELLOW** — medido; sem leak reproduzido; sem campanha de 20 ciclos de navegação multi-módulo com séries completas além do sample A13.
