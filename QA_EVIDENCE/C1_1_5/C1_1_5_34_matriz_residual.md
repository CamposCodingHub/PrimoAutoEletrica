# C1.1.5-34 — Matriz residual Owner + regressao bugs

Data: 2026-09-26 America/Sao_Paulo
Artefato: C:\Users\campo\AppData\Local\PrimoAutoEletrica\App\PrimoAutoEletrica.exe
EXE_SHA: 05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B
DLL_SHA: 9E74F733F33D7A3A9017E4F1ACB1669BD8DB5B1D1A3DBC12E0D7C461B49DB683

| Item | Check / Prova | Resultado | Evidencia |
|------|---------------|-----------|-----------|
| Residual C1.1.4 | Interacao:Controle:BaseConhecimentoControl | PASS (3973 ms) | ui-smoke-2026-09-26-18-28-50 |
| Collateral | Interacao:Controle:FerramentasControl | PASS (7121 ms); 0 Owner unhandled | smoke_app + residual_check |
| Race harness | Clientes → BaseConhecimento → Ferramentas focused | 3/3 PASS; 0 Owner errs | residual_check_report.txt |
| BUG-001..003 | C1BugFixRegressionTests | PASS (unit) | c115_bug_regression.trx |
| BUG-005 | AutoEletrica:HistoricoDefeitosRecorrentes | PASS (34 ms) | smoke_key_checks |
| BUG-006 | Financeiro:GraficosAlertasDivergencia | PASS (344 ms) | smoke_key_checks |
| BUG-007 | Interacao:Modulo:ClientesControl | PASS (7168 ms) | smoke_key_checks |
| Agendamentos/Veiculos NULL | Agendamentos:* + Veiculos:* + Interacao:Modulo | PASS | smoke_key_checks |

Suite real App: **Total=206 Sucesso=206 Falhas=0** | owner_on_closed_errors=0
