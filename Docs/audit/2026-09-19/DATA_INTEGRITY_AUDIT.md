# G — DATA_INTEGRITY_AUDIT

| ID | Achado | Status |
|----|--------|--------|
| DI-01 | Colunas monetárias SQLite `REAL` | ABERTO — sem migration destrutiva |
| DI-02 | Histórico financeiro por nome | MITIGADO — ClienteId only |
| DI-03 | ContasReceber podem ter snapshot de nome sem FK rígida | ABERTO (schema) |
| DI-04 | Provider SQL Server anunciado com runtime SQLite | Risco operacional |

**Regra Phase 0:** não apagar migrations; não migration money destrutiva.
