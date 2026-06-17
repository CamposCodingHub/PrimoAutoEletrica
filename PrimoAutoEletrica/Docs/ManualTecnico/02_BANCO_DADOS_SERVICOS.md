# Banco de Dados e Servicos Principais

## Banco de dados

O sistema usa SQLite como banco local padrao. A estrutura e criada e migrada pela camada de banco da aplicacao, com historico de schema e rotinas de compatibilidade.

Documentos relacionados:

- `Docs/BANCO_DE_DADOS.md`
- `Docs/SQLSERVER_MIGRATION_PLAN.md`
- `Docs/GUIA_BACKUP_RESTAURACAO.md`

## Migracoes

As migracoes ficam centralizadas em `DatabaseService.Migrations.cs` e arquivos relacionados. Cada mudanca estrutural deve:

- Criar ou alterar tabelas de forma idempotente.
- Preservar dados existentes.
- Registrar versao de schema quando aplicavel.
- Ser validada por build e smoke ou teste operacional.

## Servicos principais

- `DatabaseService`: inicializacao, schema, migracoes e operacoes historicas ainda centralizadas.
- `SystemConfigurationService`: configuracoes persistidas do sistema.
- `BusinessConfigurationService`: dados comerciais legados e compatibilidade.
- `DatabaseBackupService`: backup e restauracao.
- `PermissionService`: validacao de permissoes por usuario.
- `AuditLogService`: trilha de auditoria.
- `LoggerService`: logs texto e logs estruturados.
- `ErrorHandlingService`: mensagens amigaveis e detalhes tecnicos copiaveis.
- `DocumentoPdfService`: documentos oficiais em PDF.
- `UiSmokeTestService`: validacao automatizada de telas e fluxos.
- `OperationalWorkflowTestService`: fluxo operacional de ponta a ponta.

## Cuidados tecnicos

- Nao remover coluna ou tabela sem estrategia de migracao.
- Nao acessar caminhos fixos fora dos servicos de runtime/configuracao.
- Nao registrar senha ou dado sensivel em log.
- Antes de restaurar banco, gerar backup do estado atual.
