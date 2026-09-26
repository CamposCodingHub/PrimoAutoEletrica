# C1.1 Truth Matrix

| Área | Antes | Correção | Depois | Evidência |
|------|-------|----------|--------|-----------|
| BUG-001 ModernTab | FAIL XamlParse | Tabs.xaml + GlobalStyles merge | FIXED | C1BugFix tests + UiSmoke BaseConhecimento/NecessidadesCompra |
| BUG-002 RBAC | FAIL Admin ESTOQUE_CRIAR | TemPermissaoCodigo | FIXED fail-closed | C1BugFix Bug002_* |
| BUG-003 Ferramentas NRE | FAIL init | _isInitialized + Loaded | FIXED | Bug003 STA + UiSmoke Ferramentas |
| Build Debug/Release | — | — | 0 errors | QA_EVIDENCE/C1_1/build_*.txt |
| xUnit | 474 baseline | +C1 tests | 483/483 | xunit_main.txt |
| UI Smoke | 199/206 | fixes | 206/206 | ui_smoke_206_PASS_pre_reconfirm.txt |
| E2E | 42/42 | — | 42/42 | workflow_e2e_report.txt |
| RBAC | bug no call site | API correta | PASS | Bug002 tests |
| Banco protegido | SHA C7420D… | não tocado | SHA idêntico | Get-FileHash + PRAGMA |
| Main | 29b19b1 | não tocada | 29b19b1 | git rev-parse main |
| Desktop smoke | — | EXE --smoke-test | PASS | UiSmoke runner |
| C2 | — | — | NÃO INICIADO | — |