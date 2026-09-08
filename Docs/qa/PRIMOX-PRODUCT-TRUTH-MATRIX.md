# PRIMOX Workshop — Product Truth Matrix 1.0

**Produto:** PRIMOX Workshop 1.0.0  
**HEAD auditoria:** `1f3af7e`  
**Tag protegida:** `v1.0.0` → `a4ad6fe` (annotated tag object ≠ commit SHA; commit resolvido = `a4ad6fe`)  
**Data:** 2026-09-08  
**Método:** código → testes → schema → runtime → docs (docs não são prova)

Legenda Status: `REAL` | `REAL+TESTADO` | `REAL+PARCIAL` | `PARCIAL` | `SCAFFOLD` | `PLACEHOLDER` | `NÃO IMPLEMENTADO` | `NÃO TESTÁVEL` | `ORPHAN` | `FORA DO ESCOPO` | `DOC INCORRETA` | `DOC DESATUALIZADA`

| ID | Área | Funcionalidade | UI | Service | Repo/DB | API | Teste | Status real | Limitação |
|----|------|----------------|----|---------|---------|-----|-------|-------------|-----------|
| PT-001 | Login | Autenticação PBKDF2 + lockout | Sim | PasswordHasher + LoginTentativas | Funcionarios | n/a | QaEngine/unit | REAL+TESTADO | — |
| PT-002 | Login | 2FA TOTP no login | Setup window existe | TwoFactorService (OtpNet) | Colunas Totp no schema | n/a | unit parcial | PARCIAL | LoginWindow **sem** desafio 2FA; setup sem opener claro |
| PT-003 | Login | Seleção multi-filial | SelecaoFilialWindow | FilialService mock | Sem persistência real | n/a | UI smoke | SCAFFOLD | Filiais hardcoded SP/RJ |
| PT-004 | Dashboard | KPIs (OS, clientes, faturamento) | DashboardControl | DashboardViewModel SQL | OrdensServico/Clientes/Vendas | n/a | Exhaustive nav | REAL+TESTADO | — |
| PT-005 | Clientes | CRUD + pesquisa | ClientesControl + windows | VM/repo | Clientes | n/a | Exhaustive + smoke | REAL+TESTADO | Soft delete / LGPD campos |
| PT-006 | Clientes | LGPD anonimizar lote | parcial UI | SoftDeleteService | IsDeleted | n/a | parcial | REAL+PARCIAL | Fluxo completo DPO não é produto SaaS |
| PT-007 | Veículos | CRUD + vínculo cliente | Sim | VM/repo | Veiculos | n/a | Exhaustive | REAL+TESTADO | — |
| PT-008 | OS | Criação/edição/status/itens | OrdemServicoWindow | domínio OS | OrdensServico | **não mapeado** | Exhaustive | REAL+TESTADO | API OS ausente |
| PT-009 | OS | Kanban transitions | Kanban UI | status domain | Status campos | n/a | Exhaustive | REAL+PARCIAL | Validar regras de transição por status |
| PT-010 | Orçamentos | CRUD + conversão OS | Sim | VM/repo | Orcamentos | listagem ObterTodos | Exhaustive | REAL+TESTADO | API sem paginação |
| PT-011 | Agenda | CRUD + check-in | Agendamentos | VM | Agendamentos | n/a | Exhaustive | REAL+PARCIAL | Conflito de agenda não comprovado como regra forte |
| PT-012 | Estoque | CRUD produtos + saldo | EstoqueControl | EstoqueViewModel | Produtos | ObterTodos | Exhaustive | REAL+TESTADO | PagingHelper **CLIENT_SIDE** |
| PT-013 | Estoque | Baixa OS/PDV | fluxos | serviços estoque | movimentos | n/a | smoke/workflows | REAL+TESTADO | — |
| PT-014 | Financeiro | Contas / origens OS-PDV | FinanceiroControl | serviços | Financeiro | endpoints financeiros | Exhaustive | REAL+PARCIAL | DRE/conciliação/metas: profundidade limitada |
| PT-015 | PDV | Venda + caixa abrir/fechar | PDV | caixa/venda | Vendas/Caixa | n/a | Exhaustive | REAL+TESTADO | Impressão nativa NOT_TESTABLE |
| PT-016 | Funcionários | CRUD + permissões perfil | FuncionariosControl code-behind | PermissionService | Funcionarios | n/a | DeepQa Funcionarios | REAL+TESTADO | FuncionariosViewModel **ORPHAN** |
| PT-017 | Funcionários | Salário zero bug | UI | — | — | — | DeepQa/15E | REAL+TESTADO | Bug histórico tratado em QA |
| PT-018 | Fornecedores | CRUD | Sim | repo | Fornecedores | n/a | Exhaustive | REAL+TESTADO | — |
| PT-019 | Relatórios | PDF/CSV export | RelatoriosControl + RelatoriosViewModel | RelatorioDatabaseService | SQL LIMIT em partes | n/a | Exhaustive export | REAL+PARCIAL | RelatoriosModernoViewModel ORPHAN; truncamento em memória em fluxos |
| PT-020 | Catálogo | Import/export peças | CatalogoPecas | serviços | Catalogo | n/a | Exhaustive | REAL+TESTADO | File picker NOT_TESTABLE |
| PT-021 | Auto Elétrica | Diagnóstico técnico | AutoEletricaTecnica | VM | — | n/a | Exhaustive | REAL+PARCIAL | a11y chrome P15E-015 |
| PT-022 | Help | Ajuda in-app | HelpControl | — | — | n/a | — | PARCIAL / WIP | HelpTopicsCatalog WIP não commitado |
| PT-023 | NF-e | Importação XML → estoque/financeiro | ImportarNotaWindow | NFeService | Importações | n/a | Exhaustive | REAL+TESTADO | — |
| PT-024 | NF-e | Emissão SEFAZ / DANFE / cancel | — | NFeEmissaoService **vazio** | — | n/a | NOT TESTABLE | NÃO IMPLEMENTADO | Sem Transmitir/Autorizar |
| PT-025 | Notificações | Twilio/SMS API | — | NotificationService Delay+TODO | — | n/a | — | PLACEHOLDER | — |
| PT-026 | Notificações | WhatsApp wa.me share | botões agenda/orçamento | URL scheme | — | n/a | Exhaustive | REAL | Não é API Cloud |
| PT-027 | Multi-filial | Persistência + sync | UI seleção | FilialService mock | — | n/a | — | SCAFFOLD | — |
| PT-028 | Offline sync | Fila remota | — | ProcessarFilaOfflineAsync **ausente** | — | n/a | — | NÃO IMPLEMENTADO | — |
| PT-029 | API | Health + orcamentos/estoque/financeiro | n/a | Minimal API net9 | SQLite | Program.cs | ApiIntegrationTests PARCIAL | REAL+PARCIAL | Sem JWT wired; sem OS routes; docs exageram |
| PT-030 | API | Auth JWT / Keycloak / policies AdminOnly | n/a | packages no csproj | appsettings.Keycloak vazio | UseAuthorization sem policies | — | SCAFFOLD | NOT_USED |
| PT-031 | RBAC WPF | PermissionService módulos | MainWindow gates | PermissionService | Perfis | n/a | DeepQa | REAL+TESTADO | — |
| PT-032 | Paginação | SQL ObterPaginado | — | **não existe** | — | — | — | NÃO IMPLEMENTADO | — |
| PT-033 | Paginação | Estoque PagingHelper | Sim | in-memory Skip/Take | ObterTodos | — | — | REAL+PARCIAL | CLIENT_SIDE |
| PT-034 | Backup | Manual + histórico | Configurações | DatabaseBackupService | AppData | n/a | 15C E2E | REAL+TESTADO | — |
| PT-035 | Restore | Restore UI | Configurações | DatabaseBackupService | File.Copy | n/a | 15C | REAL+TESTADO | — |
| PT-036 | Update | Auto-update comercial | AtualizacaoWindow órfã | UpdateService | — | — | NOT TESTABLE | REAL+PARCIAL | UI não ligada; update comercial NOT IMPLEMENTED no Gate |
| PT-037 | Logs | AppData Logs | — | LoggerService | %LOCALAPPDATA%\PrimoAutoEletrica\Logs | — | runtime | REAL | — |
| PT-038 | DB | Migrations código | — | 27 ApplyMigration IDs | SchemaVersion live ~32 rows | — | integrity OK | REAL+PARCIAL | CODE_MIGRATIONS=27 vs HISTORICAL_DB≈32 |
| PT-039 | Segurança | Soft delete LGPD | parcial | SoftDeleteService | IsDeleted | — | migrations | REAL+TESTADO | — |
| PT-040 | Impressão/PDF | Relatórios/OS | botões | export services | arquivos | — | Exhaustive | REAL+PARCIAL | PrintDialog nativo NOT_TESTABLE |
| PT-041 | Packaging | Installer Inno + E2E | — | Scripts 15B/15C | — | — | 15C PASS | REAL+TESTADO | Signing NOT CONFIGURED |
| PT-042 | SaaS/Cloud | Multi-tenant / website | — | — | — | — | — | FORA DO ESCOPO | Explicitamente não iniciado |

## Contagem objetiva (matriz)

| Status | Qtd |
|--------|----:|
| REAL / REAL+TESTADO | 22 |
| REAL+PARCIAL / PARCIAL | 12 |
| SCAFFOLD / PLACEHOLDER | 5 |
| NÃO IMPLEMENTADO | 3 |
| ORPHAN / WIP / FORA DO ESCOPO | 3+ (Help WIP, orphans, SaaS) |

*Itens podem cruzar categorias; totais são da tabela acima (~42 linhas).*
