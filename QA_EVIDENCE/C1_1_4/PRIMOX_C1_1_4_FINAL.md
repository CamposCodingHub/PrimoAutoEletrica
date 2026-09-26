# PRIMOX C1.1.4 FINAL — Historico Auto Eletrica, Lucro Servico, Timeout Clientes

**Data:** 2026-09-26 (America/Sao_Paulo)  
**Branch:** `cycle-c1/operational-intelligence`  
**Commit:** `ea3a1d62ac257e206a1a353a1a088a8a15c1ef06`
**Decisao:** **PASS** (objetivos BUG-005/006/007 certificados no App EXE real)  
**Residual:** FIX_REQUIRED fora do escopo — `Interacao:Controle:BaseConhecimentoControl` (Owner em Window fechada). **Nao iniciar C2.**

## Root causes (one-liners)

- **BUG-005:** painel `Take(5)` afogava recorrencia recente/smoke no historico operacional ruidoso; "Mesmo defeito" aceitava `Count>=1`.
- **BUG-006:** ViewModel carregava so top-8 `LucroPorServico`, entao servico sintetico do smoke sumia entre centenas de OS; nao era bug CentsV1.
- **BUG-007:** `ExerciseInteractionSurface` generico clicava todos os botoes de Clientes (incl. Abrir360/Historico por recreacao) → `TimeoutException` >120s; fix = harness dedicado + roteamento (nao so subir timeout).

## Fixes aplicados

1. `AutoEletricaTecnicaService.ConsolidarDefeitosRecorrentes` publico/static: `Take(25)`, `Count>=2`, `ThenByDescending(DataAbertura)`.
2. `FinanceiroViewModel` limit 8→50; `FinanceiroDatabaseService.ObterLucroPorTipoItem` filtro `LIKE 'servi%'` apos normalize `char(231)/char(199)`; Referencia de Servico prefere `DescricaoItem`.
3. `UiSmokeTestService.Helpers.ExerciseHostedElementButtons` roteia `ClientesControl` → `ExerciseClientesControlButtons()` (espera tipada + fecha transientes).

## Testes unitarios

- Suite: **506 PASS / 0 FAIL** (`Tests\PrimoAutoEletrica.Tests`, Debug).
- Novos: `DefeitosRecorrentesConsolidationTests`, `LucroPorServicoMathTests` (centavos exatos + integracao limit 50), `ClientesHarnessRoutingRegressionTests`.

## Artefato Desktop (C1.1.4-21)

| Campo | Valor |
|-------|-------|
| EXE | `C:\Users\campo\AppData\Local\PrimoAutoEletrica\App\PrimoAutoEletrica.exe` |
| EXE SHA256 | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` |
| DLL SHA256 | `886AB11F815C691A5E9A02A08D673BB168E121C9BA6D21CA329932B7D95E4764` |
| Publish mode | self-contained win-x64 Release |
| Framework | net10.0-windows (runtime 10.0.10) |
| Identity match publish | **True** |
| Shortcut | `C:\Users\campo\OneDrive\Desktop\PRIMOX Workshop.lnk` → App EXE / WorkingDirectory App |

## UiSmoke App EXE real

- Preflight: `QA_EVIDENCE/C1_1_4/after_publish/uismoke_preflight.txt`
- Resultado: **205 PASS / 1 FAIL / Total 206**
- BUG-005/006/007: **PASS / PASS / PASS**
- Unica falha: `Interacao:Controle:BaseConhecimentoControl` — `InvalidOperationException: Nao e possivel definir a propriedade Owner para uma Janela fechada` (harness generico + controle; fora do escopo C1.1.4).

## DB

- Protected `primoauto.db` SHA: **INTACTO** `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B`
- Operacional: `integrity_check=ok`, `foreign_key_check` vazio, `user_version=1`

## Auditorias globais

Ver `QA_EVIDENCE/C1_1_4/audits/global_audits.txt` — Take(N), MoneyIO/GetDouble, `.Result/.Wait` em Clientes/360/AutoEletrica/Financeiro documentados; **sem mudanca sem evidencia**.

## Evidencias

- `QA_EVIDENCE/C1_1_4/` (baseline, patches, builds, unit_tests, audits, after_publish)
- Matrizes: `C1_1_4_34_matriz_bugs.md`, `C1_1_4_35_matriz_build_artifact.md`
- Copia docs: `Docs/audit/2026-09-20/PRIMOX_C1_1_4_FINAL.md`

## Git

- Commit (apos prova): `C1.1.4: fix AutoElectrical history, service profit and client timeout`
- Push **somente** `cycle-c1/operational-intelligence`
- `main` nao alterado

## Proximo

**STOP.** Nao iniciar C2. Residual BaseConhecimento/Ferramentas Owner-on-closed-window pode entrar em ciclo futuro com evidencia propria.