# Status do runtime SQL Server

Data: 2026-06-15

## Veredito tecnico

O schema SQL Server foi corrigido e validado em `MSSQLLocalDB`, mas o runtime operacional completo do aplicativo ainda nao pode ser declarado concluido sem uma refatoracao ampla da camada de persistencia.

Motivo objetivo:

- 39 arquivos ainda possuem dependencia direta de `Microsoft.Data.Sqlite`, `SqliteConnection`, `SqliteTransaction`, `SqliteCommand`, `SqliteDataReader` ou dialeto SQLite.
- Foram encontradas aproximadamente 750 ocorrencias relacionadas a SQLite/dialeto SQLite no projeto principal.
- Existem aproximadamente 494 referencias a infraestrutura global (`App.Database`, `App.Repositories`, `DatabaseService(...)`), o que dificulta alternancia real de provider.
- O schema SQL Server sobe, mas os repositorios e servicos operacionais ainda esperam tipos concretos SQLite e comandos com `PRAGMA`, `sqlite_master`, `LIMIT`, `INSERT OR`, `last_insert_rowid()` e outras diferencas de dialeto.

## O que foi concluido

- `SqlServerSchema.sql` executa com sucesso em `MSSQLLocalDB`.
- O script agora configura `SET ANSI_NULLS ON` e `SET QUOTED_IDENTIFIER ON`, exigidos para indice filtrado.
- `Veiculos.ClienteId` foi corrigido para `INT`, compativel com `Clientes.Id`.
- `UX_RecordLocks_Entity_Active` foi validado no SQL Server.
- A FK `Veiculos -> Clientes` foi validada no SQL Server.
- Criado `Scripts/Test-SqlServerSchema.ps1` para validar schema SQL Server de forma repetivel.
- Configuracao do aplicativo bloqueia ativacao SQL Server sem fallback explicito, evitando falsa operacao enterprise.

Evidencia local:

- `TestResults/SqlServerSchema/2026-06-15_21-25-42/sqlserver-schema-summary.json`

## O que falta para runtime SQL Server completo

1. Extrair interfaces provider-agnostic para conexao, comandos, transacoes e dialect SQL.
2. Migrar repositories principais: Produto, Cliente, Fornecedor, Funcionario, OrdemServico, Venda e Importacao.
3. Migrar services operacionais: Caixa, Financeiro, Orcamento, Agendamento, Estoque, Auditoria, Sessao, Backup/Restore e Relatorios.
4. Substituir comandos SQLite por dialeto duplo ou comandos SQL Server dedicados.
5. Criar seed/migrations SQL Server equivalentes ao `DatabaseService` SQLite atual.
6. Rodar workflow e smoke contra SQL Server real, nao apenas schema.
7. Remover fallback SQLite somente depois dos passos acima passarem.

## Decisao de seguranca

Nao foi feito um "fake runtime" que apontaria a UI para SQL Server enquanto as operacoes reais continuam SQLite.
O sistema agora falha rapido se SQL Server for configurado sem fallback explicito, protegendo producao contra falsa homologacao.
