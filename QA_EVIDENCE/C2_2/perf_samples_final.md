# C2.2 performance samples (real)

## Desktop UiSmoke Search (installed EXE path via --app-data sandbox)
Source: QA_EVIDENCE/C2_2/smoke_appdata/Logs/smoke-tests/ui-smoke-2026-09-26-19-40-55-135-p23176.txt

| Scenario | Wall (ms) |
|----------|-----------|
| Search:Empty | 455 |
| Search:NoResults (XYZ_NAO_EXISTE_PRIMOX_999) | 170 |
| Search:D01 | 143 |
| Search:D17 | 130 |
| Search:Knowledge (queda de tensão) | 185 |
| Search:DiagnosticCase (partida) | 162 |
| Search:OpenSource | 152 |
| Search:Clear | 140 |
| Search:BaseConhecimentoHost | 675 |

## Unit suite wall clocks (Debug filter)
- D01_to_D17_IndividualSearches_17of17: ~1000 ms (17 sequential searches)
- Performance_SampleQueries_RecordRealMs: ~487 ms (D01,D17,queda,alternador,24V combined)
- LargeSynthetic_DatasetTimings_Recorded: ~658 ms (build 1k + search + grow to 10k + search)

Service DurationMs is also asserted >= 0 inside unit tests (Stopwatch in KnowledgeSearchService).
