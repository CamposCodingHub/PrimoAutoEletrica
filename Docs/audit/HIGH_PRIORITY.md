# PRIMOX Workshop — HIGH PRIORITY (P1)

---

## P1.01 — DatabaseService God Object

| Campo | Detalhe |
|---|---|
| **ID** | P1.01 |
| **PRIORIDADE** | P1 |
| **CATEGORIA** | Arquitetura |
| **ARQUIVO** | `DatabaseService.cs` + 8 partials |
| **PROBLEMA** | DatabaseService (~340KB total) concentra: conexão, inicialização, schema, migrations, clientes, funcionários, permissões, estoque, OS, financeiro, segurança, auditoria. |
| **RISCO** | Manutenabilidade degradada, testabilidade prejudicada, risco de conflitos em equipe. |
| **CORREÇÃO** | Evolução gradual para: PRIMOX.Domain, PRIMOX.Application, PRIMOX.Infrastructure, PRIMOX.Persistence. Migrar por domínio, não reescrever tudo. |
| **STATUS** | ⚠️ PLANEJADO |

## P1.02 — Instanciação Direta (new XxxService())

| Campo | Detalhe |
|---|---|
| **ID** | P1.02 |
| **PRIORIDADE** | P1 |
| **CATEGORIA** | Arquitetura / Testabilidade |
| **ARQUIVO** | Múltiplos (HistoricoClienteWindow, OrdemServicoWindow, Primox360Service, etc.) |
| **PROBLEMA** | `new OrcamentoDatabaseService()`, `new FinanceiroDatabaseService()`, `new VendaRepository()` espalhados por views e services. |
| **RISCO** | Acoplamento alto, impossibilidade de mock em testes unitários. |
| **CORREÇÃO** | Injeção de dependência via construtor. Primox360Service já usa Func<> factories como interim. |
| **STATUS** | ⚠️ PARCIAL — DI container existe (App.Services), uso inconsistente. |

## P1.03 — Multi-Filial Scaffold

| Campo | Detalhe |
|---|---|
| **ID** | P1.03 |
| **PRIORIDADE** | P1 |
| **CATEGORIA** | Produto / Escala |
| **PROBLEMA** | `FilialService` neutralizado (correto). Sem isolamento real persistente por filial. |
| **CORREÇÃO** | Planejar quando cloud architecture estiver pronta: Tenant → Filial → Caixa → Estoque → Usuários → OS → Fiscal. |
| **STATUS** | ⚠️ PLANEJADO |

## P1.04 — Sincronização Não Real

| Campo | Detalhe |
|---|---|
| **ID** | P1.04 |
| **PRIORIDADE** | P1 |
| **CATEGORIA** | Infraestrutura |
| **PROBLEMA** | `LocalSyncService` (LAN UDP), `SynchronizationService` existem mas não há sync cloud robusta. |
| **CORREÇÃO** | Futuro: Outbox → Sync Engine → Retry → Idempotency → Conflict Resolution → Cloud. Offline-first strategy. |
| **STATUS** | ⚠️ PLANEJADO |

## P1.05 — Funcionalidades de Produto Ausentes

| Campo | Detalhe |
|---|---|
| **ID** | P1.05 |
| **PRIORIDADE** | P1 |
| **CATEGORIA** | Produto |
| **PROBLEMA** | Funcionalidades necessárias para competir comercialmente: inspeção digital completa, portal do cliente, interface mobile técnico, motor de revisão/garantia/retorno, gestão de frotas avançada, BI operacional, DRE, PIX dinâmico, WhatsApp Cloud API. |
| **CORREÇÃO** | Ver MARKET_PRODUCT_GAPS.md e PRODUCT_ROADMAP.md. |
| **STATUS** | ⚠️ PLANEJADO |

## P1.06 — Fiscal Foundation vs Produção

| Campo | Detalhe |
|---|---|
| **ID** | P1.06 |
| **PRIORIDADE** | P1 |
| **CATEGORIA** | Fiscal |
| **PROBLEMA** | Base fiscal relevante existe (IFiscalProvider, FocusNfeProvider, idempotência, FiscalOperationId, validator, mapper). Não homologada em produção real. |
| **EVIDÊNCIA** | Testes fiscais existem: `FiscalFoundationTests`, `FiscalOperationsTests`, `NFeHomologationTests`, `FiscalMegaStressTests`. |
| **CORREÇÃO** | Completar: NF-e, NFC-e, NFS-e, A1, cancelamento, inutilização, eventos, DANFE, contingência, reprocessamento. |
| **STATUS** | ⚠️ FUNDAÇÃO EXISTENTE — produção pendente. |
