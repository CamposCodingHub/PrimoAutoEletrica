# PRIMOX Workshop — TEST GAPS

> O que os testes atuais NÃO comprovam.

---

## 1. Testes que Existem e Funcionam ✅

| Área | Arquivo | Testes | Tipo |
|---|---|---|---|
| JWT Auth | ApiJwtHttpTests.cs | 10 | Real HTTP contra API |
| JWT Bootstrap | ApiJwtProductionBootstrapTests.cs | 4 | Config validation |
| Auth Gate | ApiAuthenticationGateTests.cs | ~3 | Auth gate |
| Permission Fail-Closed | PermissionServiceFailClosedTests.cs | 10 | Real componente |
| Password Hasher | PasswordHasherRealTests.cs | 14 | Real componente |
| MoneyCents | MoneyCentsTests.cs + Expanded | 25+ | Aritmética |
| ID Isolation | Primox360IdFinancialJoinTests.cs | 2 | Source proof |
| Financial Isolation | HistoricoClienteFinancialIsolationTests.cs | ~3 | Source proof |
| Primox360 | Primox360ServiceTests.cs | ~20+ | Real componente |
| Permission Profiles | PermissionTests.cs | ~10 | Real componente |
| DB Persistence | DatabasePersistenceTests.cs | ~15 | Real DB |
| DB Provider | DatabaseProviderRuntimeTests.cs | ~5 | Runtime |
| Migration Schema | MigrationSchemaTests.cs | ~5 | Schema validation |
| Fiscal Foundation | FiscalFoundationTests.cs | ~10 | Foundation |
| Fiscal Operations | FiscalOperationsTests.cs | ~10 | Operations |
| NF-e Homologation | NFeHomologationTests.cs | ~15 | Homologation |
| Localization | LocalizationServiceTests.cs | ~15 | i18n |
| Path Security | PathSecurityHelperTests.cs | ~5 | Security |
| Secure XML | SecureXmlLoaderTests.cs | ~3 | Security |
| Orçamento Status | OrcamentoStatusNormalizerTests.cs | ~3 | Normalizer |
| Orçamento DB | OrcamentoDatabaseServiceTests.cs | ~5 | Real DB |
| License Honesty | LicenseScaffoldHonestyTests.cs | 1 | Honesty |
| Backup Encryption | ExternalBackupEncryptionHonestyTests.cs | ~3 | Honesty |
| DVI Checklist | DviChecklistServiceTests.cs | ~5 | Service |
| DVI→OS Inheritance | DviOrcamentoOsInheritanceTests.cs | ~5 | Workflow |
| Lembrete Revisão | LembreteRevisaoServiceTests.cs | ~5 | Service |

## 2. Gaps Críticos — Testes que NÃO Existem 🔴

### 2.1 Broken Access Control (IDOR)
- Usuário A tenta acessar dados de Usuário B via API
- Nenhum teste verifica isolamento de dados por usuário/tenant

### 2.2 Privilege Escalation
- Usuário tenta alterar próprio perfil para Administrador
- Nenhum teste verifica proteção contra escalação

### 2.3 Tenant/Branch Isolation
- Quando multi-tenant for implementado, testes de isolamento são obrigatórios
- Atualmente não aplicável (single-tenant)

### 2.4 Session Expiration
- `SessionInactivityService` existe mas sem teste automatizado de expiração real

### 2.5 2FA (Two-Factor)
- `TwoFactorService` e `TwoFactorStoreService` existem mas sem testes

### 2.6 Concurrency / Threading
- `Task.Run()` usado em vários locais sem testes de race condition
- `RecordLockService` existe mas cobertura de testes desconhecida

### 2.7 Backup RESTORE
- Backup criação testada, mas RESTORE não é testado automaticamente
- Backup não é "funcionando" apenas porque o arquivo foi criado

### 2.8 Financial Precision (Runtime)
- MoneyCents struct testada, mas cálculos financeiros reais no DB usam REAL
- Nenhum teste verifica que operações financeiras no banco produzem resultados corretos
- Risco: teste de MoneyCents passa mas DB perde precisão

### 2.9 Workflow End-to-End
- Nenhum teste integrado: Lead → Agendamento → Check-in → Diagnóstico → Orçamento → Aprovação → OS → Peças → Execução → Teste → Entrega → Pagamento → Garantia → Pós-venda

### 2.10 UI/UX Regression
- UiSmokeTestService (500KB+) valida existência de controles
- Não valida: layout correto, contraste, temas, responsividade real

### 2.11 API Contract Tests
- Testes existentes não validam formato de resposta (schema)
- Sem teste de backward compatibility de API

### 2.12 Empty Catches
- 13+ `catch { }` que mascaram falhas — nenhum teste verifica que erros são logados
