# C1.1.4-34 — Matriz de prova dos 3 bugs (UiSmoke App EXE real)

Data: 2026-09-26 America/Sao_Paulo
Artefato: C:\Users\campo\AppData\Local\PrimoAutoEletrica\App\PrimoAutoEletrica.exe
EXE_SHA: 05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B
DLL_SHA: 886AB11F815C691A5E9A02A08D673BB168E121C9BA6D21CA329932B7D95E4764
Sandbox: C:\Users\campo\AppData\Local\Temp\primox-c114-smoke-20260926_162941

| Bug | Check UiSmoke | Resultado | Tempo | Evidencia |
|-----|---------------|-----------|-------|-----------|
| BUG-005 Historico recorrente | AutoEletrica:HistoricoDefeitosRecorrentes | PASS | 34 ms | smoke_log / ui-smoke report |
| BUG-006 Lucro por Servico | Financeiro:GraficosAlertasDivergencia | PASS | 358 ms | smoke_log / ui-smoke report |
| BUG-007 Clientes timeout | Interacao:Modulo:ClientesControl | PASS | 7473 ms | smoke_log / ui-smoke report |

Suite real App: Total=206 Sucesso=205 Falhas=1
Falha residual (fora do escopo 005/006/007): Interacao:Controle:BaseConhecimentoControl — Owner em janela fechada.