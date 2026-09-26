# C1.1.3 Global NULL Reader Audit

Date: 2026-09-26 BRT
Branch: cycle-c1/operational-intelligence

## Heuristic
Lines with `reader.GetXxx(` where `IsDBNull` does not appear on the same line or the two previous lines.

Summary (PrimoAutoEletrica project sources):
- SAFE≈335
- RISK≈133 (heuristic; many are required PK columns or false negatives/positives)

Evidence CSV: `QA_EVIDENCE/C1_1_3/audit/getxxx_risk.csv`

## Confirmed Desktop crash (fixed)
| Module | Query | Ordinal 6 | Column | SQLite | Nullable | Mapper | Before | After |
|--------|-------|-----------|--------|--------|----------|--------|--------|-------|
| Agendamentos | SELECT * FROM Agendamentos | 6 | DuracaoEstimada | TEXT | yes | LerAgendamento | GetString(6) crash | ReadOptionalTimeSpan → TimeSpan.Zero |
| Veículos UI | LoadVeiculos → ObterTodosAgendamentos() | same | same | same | same | same | same popup | same fix |

Veículos MaterializarVeiculo already used ReadString (null→""). Placa NULL count in operacional=0. The "Erro ao carregar veículos" was the Agendamentos loader inside VeiculosViewModel.

## Semantic policy applied in LerAgendamento
- Optional TimeSpan (DuracaoEstimada/DuracaoReal): NULL → TimeSpan.Zero (matches existing DuracaoReal + "Zero means unset" fallback in DeterminarDataPrevisaoOrdemServico)
- Optional strings: NULL → ""
- Optional bool/int: NULL → false/0
- Optional money snapshots: LerMoedaOpcionalOuZero
- Optional TecnicoId: NULL → Guid.Empty
- Required Id/ClienteId/VeiculoId/DataCriacao/DataAgendamento: fail-closed with clear InvalidOperationException

## Remaining RISK (not this crash; tracked)
Top files: FinanceiroDatabaseService, DatabaseService.AccessControl, VendaRepository, FiscalOperationStore, CatalogoVeiculoService.
Agendamento residual GetString on produto/timeline Ids (required FKs in related tables) — not ordinal-6 list path.

## Classification of DuracaoEstimada
**B — optional** (schema notnull=0; 10/10 operacional rows NULL; UI/domain treat Zero as unset).
