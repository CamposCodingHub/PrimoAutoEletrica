# F — ARCHITECTURE_AUDIT

| Tema | Achado | Severidade |
|------|--------|------------|
| TFM | Desktop+API `net10.0-windows` neste branch | Info (corrige mito net6) |
| API | Minimal APIs no Program.cs; sem auth layer | P0 |
| Desktop | WPF + service locator `App.Services` | P1 |
| Dados | SQLite runtime; SQL Server “configurado” mas fallback SQLite | P1 |
| Permissões | RBAC DB + fallback perfil; Unavailable agora fail-closed | P0 mitigado |
| 360 | Primox360 por ID (financeiro) | Bom |
| Testes | Duas árvores de teste; quality uneven | P1 |

Não reescrever DatabaseService nesta Phase 0.
