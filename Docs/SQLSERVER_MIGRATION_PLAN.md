# Plano de Migração: SQLite -> SQL Server

Objetivo: definir passos, verificações e ferramentas para migrar dados da base SQLite atual para SQL Server de forma segura e reversível.

Passos recomendados:

1. Preparação
   - Fazer backup completo da pasta de dados e do arquivo SQLite.
   - Verificar consistência do banco SQLite com `PRAGMA integrity_check;`.
   - Configurar ambiente de testes com instância SQL Server (local ou container).

2. Gerar esquema SQL compatível
   - Revisar `PrimoAutoEletrica/Services/DatabaseProviders/SqlServerSchema.sql`.
   - Ajustar tipos e constraints específicas conforme necessidade do ambiente alvo.

3. Exportar dados do SQLite
   - Exportar por tabela usando `sqlite3` para gerar arquivos CSV ou INSERTs.
   - Para dados grandes, preferir CSV e `BULK INSERT` no SQL Server.

4. Importar para SQL Server
   - Aplicar schema no SQL Server (`Tools/DbConfigurator create-schema "<cs>"`).
   - Importar dados via `BULK INSERT` ou `bcp` e validar contagens por tabela.

5. Validação
   - Validar contagens e somas chave (ex.: total de vendas, saldos de estoque).
   - Rodar suíte de testes automatizados em ambiente conectado ao SQL Server.

6. Rollback e verificação final
   - Manter backup do SQLite e do dump do SQL Server antes da troca final.
   - Planejar janela de migração e aviso aos usuários.

Ferramentas úteis:
- `sqlite3` (export CSV)
- `bcp` / `BULK INSERT`
- `Tools/DbConfigurator` (aplicar `SqlServerSchema.sql`)

Observação: a migração exige coordenação operacional e backups; automatizar com scripts é recomendado após um teste manual bem-sucedido.
