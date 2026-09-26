# PRIMOX C1.1.3 FINAL

**Data:** 2026-09-26 (America/Sao_Paulo)  
**Branch:** `cycle-c1/operational-intelligence`  
**Status oficial:** **FIX_REQUIRED**

Motivo do status: o bug C1.1.3 (NULL ordinal 6 em Agendamentos/Veículos) foi corrigido e validado no EXE real; permanecem 3 falhas de UiSmoke laterais, suíte smoke incompleta vs. o piso 206 do C1.1.2, e Light/Dark/resoluções/matriz manual P01–P25 sem certificação real completa. **C2 não iniciado. `main` intacta.**

---

## 1. Baseline (C1.1.3-00)

- Pré-fix tip: `8156813` (C1.1.2)
- Atalho `PRIMOX Workshop.lnk` → `C:\Users\campo\AppData\Local\PrimoAutoEletrica\App\PrimoAutoEletrica.exe` (OK)
- Protected DB SHA (antes/depois): `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` (OK)
- Evidência: `QA_EVIDENCE/C1_1_3/runtime/baseline.txt`

## 2. Erros originais

1. **Agendamentos:** `The data is NULL at ordinal 6` ao navegar.
2. **Veículos:** mesma mensagem ao carregar (mensagem de UI diferente; mesma causa).

## 3–7. Agendamentos — query, ordinal, causa, correção

| Item | Valor |
|------|--------|
| Query | `SELECT * FROM Agendamentos` |
| Ordinal 6 | **`DuracaoEstimada`** (TEXT, nullable) |
| Mapper | `AgendamentoDatabaseService.LerAgendamento` |
| Causa | `TimeSpan.Parse(reader.GetString(6))` sem `IsDBNull` |
| Dados | 10/10 linhas no operacional com `DuracaoEstimada` **e** `DuracaoReal` NULL (+ várias colunas opcionais NULL) |
| Classificação | **B — opcional** |
| Correção | Helpers tipados (`ReadOptionalTimeSpan` → `TimeSpan.Zero` se NULL; strings → `""`; money → `MoneyIO.LerMoedaOpcionalOuZero`; Ids/datas obrigatórios **fail-closed**) |
| Arquivo | `PrimoAutoEletrica/Services/AgendamentoDatabaseService.cs` |

Política: `QA_EVIDENCE/C1_1_3/P2_NULL_DURATION_POLICY.md`  
Probe: `QA_EVIDENCE/C1_1_3/before_fix/`

## 8–12. Veículos — mesma raiz

| Item | Valor |
|------|--------|
| Sintoma | `Erro ao carregar veículos` + NULL ordinal 6 |
| Causa real | `VeiculosViewModel.LoadVeiculos()` chama `ObterTodosAgendamentos()` (mesmo mapper) |
| Lista de veículos | `MaterializarVeiculo` já usava leitura segura; placa NULL count = 0 |
| Correção | A mesma do mapper de Agendamentos (não mascarar Placa) |

## 13. Auditoria global NULL

- Heurística: ~335 SAFE / ~133 RISK (`QA_EVIDENCE/C1_1_3/audit/`)
- Relatório: `C1_1_3_GLOBAL_NULL_AUDIT.md`
- Residual RISK em Financeiro/Fiscal/AccessControl **não** drive-by fixed (fora do escopo sem evidência do crash)

## 14–15. Testes

- Novos: `AgendamentoNullDurationMaterializationTests` (NULL + valor preenchido) — **2/2 PASS**
- `PrimoAutoEletrica.Tests` Release: **489/489 PASS** (era 487; +2)
- UiTests legado Debug path: falha pré-existente — **fora** do caminho de certificação Desktop

## 16–20. Build / publish / Desktop

- Release App: 0 erros
- Deploy: `Scripts\Deploy-ToInstalledApp.ps1 -Configuration Release -ForceStop`
- **EXE SHA:** `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` (stub)
- **DLL SHA:** `CBD6ED2526022ECE778B824E23D3E5BFE963F3C1E56D8D61F25A4A9D7320187A` (era `A6D4C01D…`)
- Atalho confirma EXE do App
- Markers na DLL: `ReadOptionalTimeSpan`, `LerMoedaOpcionalOuZero`, `ModernTabControl`

## 21–22. Teste real Agendamentos / Veículos

UiSmoke no EXE real (`--smoke-test`, sandbox com cópia do operacional):

- **Agendamentos:** Visualizações/Filtros, CheckIn/Out, Cruzada→OS, Módulo, NovoAgendamento — **PASS**
- **Veículos:** Cadastro, Alertas/Mídia, Export CSV, Módulo, Interacao:VeiculosControl — **PASS**
- Hits `NULL at ordinal` / erros de navegação Agendamentos/Veículos: **0**

## 23–27. Page smoke / Light / Dark / resoluções / E2E

| Item | Resultado |
|------|-----------|
| UiSmoke real App | **120 aprovado / 3 falhou** (suíte não atingiu o piso 206 do C1.1.2; processo encerrou após fase de interação) |
| Falhas | `AutoEletrica:HistoricoDefeitosRecorrentes` (consolidação); `Financeiro:GraficosAlertasDivergencia` (lucro por serviço); `Interacao:Modulo:ClientesControl` (timeout 120s) — **não** são o bug NULL ordinal 6 |
| Light / Dark | **NOT_TESTED** (manual) |
| 1280 / 1366 / 1920 | **NOT_TESTED** (manual) |
| E2E manual completo | **NOT_TESTED** (parcial via smoke) |
| Matriz | `QA_EVIDENCE/C1_1_3/page_matrix.md` |

## 28–29. Banco

- Operacional: `PRAGMA integrity_check=ok`; `foreign_key_check=0`; `user_version=1`
- Protected SHA: **inalterado** `C7420D18…A7CE0B`

## 30. Git

- Commit desta fase (após testes): mensagem `C1.1.3: fix nullable data readers and certify pages`
- Push somente `cycle-c1/operational-intelligence`
- `main` / `origin/main` **não** alterados neste ciclo

## 31. Conclusão

| Critério C1.1.3 | Estado |
|-----------------|--------|
| Reproduzir + mapear ordinal 6 | FEITO |
| Correção tipada sem `?? 0` indiscriminado | FEITO |
| Auditoria global | FEITO (residual RISK documentado) |
| Unit 489/489 | FEITO |
| Deploy EXE real + Agendamentos/Veículos sem popup NULL | FEITO |
| Smoke 100% + temas + resoluções + matriz P01–P25 | **NÃO** |
| Status | **FIX_REQUIRED** |

**STOP.** Não iniciar C2. Aguardar auditoria deste relatório.
