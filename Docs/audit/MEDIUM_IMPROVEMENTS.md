# PRIMOX Workshop — MELHORIAS DE PRIORIDADE MÉDIA (P2)

---

## P2.01 — Empty Catches (~13 ocorrências em Services)

| Campo | Detalhe |
|---|---|
| **PRIORIDADE** | P2 |
| **CATEGORIA** | Qualidade de Código |
| **ARQUIVOS** | AppCacheService.cs:68,87; CommercialDocumentActions.cs:102; FinanceiroDatabaseService.cs:576; LocalSyncService.cs:60,63; VendaService.cs:84,153; UiSmokeTestService*.cs (vários) |
| **PROBLEMA** | `catch { }` silencia erros sem logar. Pode mascarar falhas em auditoria LGPD, sync, financeiro, vendas. |
| **RISCO** | Falhas silenciosas em operações críticas. |
| **RECOMENDAÇÃO** | Substituir por `catch (Exception ex) { logger.LogWarning(ex, "contexto"); }` ou, no mínimo, `catch (Exception) { /* intencional: [justificativa] */ }`. |
| **STATUS** | NOT STARTED |

---

## P2.02 — DateTime.Now em 100+ Arquivos

| Campo | Detalhe |
|---|---|
| **PRIORIDADE** | P2 |
| **CATEGORIA** | Integridade de Dados |
| **PROBLEMA** | Mistura de `DateTime.Now` (local) e `DateTime.UtcNow`. Inconsistente para relatórios, sincronização, multi-timezone. |
| **RECOMENDAÇÃO** | Backend: sempre UTC. Apresentação: converter para timezone configurada. API: UTC com ISO 8601. |
| **STATUS** | NOT STARTED |

---

## P2.03 — ViewModels Potencialmente Órfãs

| Campo | Detalhe |
|---|---|
| **PRIORIDADE** | P2 |
| **CATEGORIA** | Manutenabilidade |
| **PROBLEMA** | ViewModels criadas mas sem referência ativa em Views. Possível dead code. |
| **RECOMENDAÇÃO** | Auditar referências. Remover ViewModels sem uso. |
| **STATUS** | NOT STARTED |

---

## P2.04 — Divergência de Migrations

| Campo | Detalhe |
|---|---|
| **PRIORIDADE** | P2 |
| **CATEGORIA** | Banco de Dados |
| **PROBLEMA** | DatabaseService.Migrations.cs contém ~27 migrações. Sem documentação de versão por release. |
| **RECOMENDAÇÃO** | Documentar schema por release. Criar teste de cadeia: v0 → vN. MigrationSchemaTests.cs existe (expandir). |
| **STATUS** | PARTIAL |

---

## P2.05 — Task.Run() com Potencial Thread Issues

| Campo | Detalhe |
|---|---|
| **PRIORIDADE** | P2 |
| **CATEGORIA** | Concorrência |
| **PROBLEMA** | `Task.Run()` em vários serviços sem proteção contra race conditions. `RecordLockService` existe mas cobertura desconhecida. |
| **RECOMENDAÇÃO** | Auditar usos de Task.Run(). Verificar acesso concorrente a SQLite (single writer). |
| **STATUS** | NOT STARTED |

---

## P2.06 — UX/UI Temas Light/Dark

| Campo | Detalhe |
|---|---|
| **PRIORIDADE** | P2 |
| **CATEGORIA** | UX/UI |
| **PROBLEMA** | Campos brancos em tema escuro. Contraste insuficiente em alguns controles. |
| **RECOMENDAÇÃO** | Auditar contraste WCAG AA. UiSmokeTest DVI já verifica existência de controles. |
| **STATUS** | PARTIAL |

---

## P2.07 — Design System (Cores Hardcoded)

| Campo | Detalhe |
|---|---|
| **PRIORIDADE** | P2 |
| **CATEGORIA** | UX/UI |
| **PROBLEMA** | Cores definidas diretamente em XAML de Views individuais em vez de referências ao tema. |
| **RECOMENDAÇÃO** | Migrar para ResourceDictionary centralizado. Themes/ já existe como base. |
| **STATUS** | NOT STARTED |

---

## P2.08 — Responsividade Desktop (1366x768 vs 4K)

| Campo | Detalhe |
|---|---|
| **PRIORIDADE** | P2 |
| **CATEGORIA** | UX/UI |
| **PROBLEMA** | Layout pode quebrar em resoluções extremas. |
| **RECOMENDAÇÃO** | Testar em 1366x768 (mínimo para oficinas) e 4K. |
| **STATUS** | NOT STARTED |

---

## P2.09 — Estoque (Paginação, Busca, Filtros)

| Campo | Detalhe |
|---|---|
| **PRIORIDADE** | P2 |
| **CATEGORIA** | Produto |
| **PROBLEMA** | Listas de estoque sem paginação server-side para volumes grandes. |
| **RECOMENDAÇÃO** | Implementar virtualização/paginação quando volume > 1000 itens. |
| **STATUS** | NOT STARTED |

---

## P2.10 — Dashboard/BI (Indicadores Operacionais)

| Campo | Detalhe |
|---|---|
| **PRIORIDADE** | P2 |
| **CATEGORIA** | Produto |
| **PROBLEMA** | Dashboard básico. Sem indicadores operacionais avançados (DRE, margem por serviço, custo por técnico). |
| **RECOMENDAÇÃO** | Ver PRODUCT_ROADMAP.md Fase 5. |
| **STATUS** | NOT STARTED |

---

## P2.11 — Backup/Recovery (Testar RESTORE)

| Campo | Detalhe |
|---|---|
| **PRIORIDADE** | P2 |
| **CATEGORIA** | Infraestrutura |
| **PROBLEMA** | Backup criação existe e funciona. Restore automatizado não é testado. |
| **RECOMENDAÇÃO** | Criar teste que: cria backup → destrói DB → restaura → verifica integridade. |
| **STATUS** | NOT STARTED |

---

## P2.12 — Observabilidade (CorrelationId, Structured Logs)

| Campo | Detalhe |
|---|---|
| **PRIORIDADE** | P2 |
| **CATEGORIA** | Infraestrutura |
| **PROBLEMA** | Logs existem mas sem correlationId para rastreamento de operações. |
| **RECOMENDAÇÃO** | Adicionar correlationId em cada request/operação. Structured logging (Serilog já pode ser integrado). |
| **STATUS** | NOT STARTED |
