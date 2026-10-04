# PRIMOX 3.0 — MAPA DE MIGRAÇÃO: LEGADO → NOVA ARQUITETURA
**Data:** 04 de Outubro de 2026  
**Fase de Implementação:** Fase 1 — Vertical Slice da Ordem de Serviço  
**Diretriz:** `VERDADE > MARKETING > QUANTIDADE DE FUNCIONALIDADES`

---

## 1. INTRODUÇÃO E POLÍTICA DE COEXISTÊNCIA

Durante a evolução do PRIMOX 3.0, o sistema opera de forma intencional em regime híbrido:
1. **Nova Arquitetura (Clean Architecture / Domain-Driven):** `PRIMOX.Domain`, `PRIMOX.Application`, `PRIMOX.Infrastructure`, `PrimoAutoEletrica.Api` e testes cross-platform em .NET 10.
2. **Legado Estabilizado:** Módulos do aplicativo WPF que continuam funcionando sobre `DatabaseService` e repositórios ADO.NET existentes até que sua fatia vertical seja migrada.

Nenhum código novo pode depender de regras de negócio legadas. Toda transição deve preservar comportamento funcional comprovado e invariantes operacionais.

---

## 2. MATRIZ DE MIGRAÇÃO DE COMPONENTES

| Componente | Status Atual | Arquitetura Atual | Arquitetura Alvo | Dependências Principais | Nível de Risco | Prioridade | Critério de Conclusão |
| :--- | :---: | :--- | :--- | :--- | :---: | :---: | :--- |
| **Ordem de Serviço (Vertical Slice)** | **MIGRADO (Fase 1)** | ViewModel → Service → SQL direto | `WPF/API → Application UseCases → Domain Aggregate → Repository` | `PRIMOX.Domain`, `PRIMOX.Application`, `PRIMOX.Infrastructure` | Baixo (validado) | **P0 (Concluída)** | Casos de uso `AbrirOS`, `ObterOS`, `AdicionarItens` e `AlterarStatus` executados por Desktop e API sem duplicação de regras. |
| **Desacoplamento da API Web** | **MIGRADO (Fase 1)** | TFM `net9.0-windows` acoplada ao WPF | TFM `net10.0` cross-platform headless | `PRIMOX.Application`, `PRIMOX.Infrastructure` | Baixo (validado) | **P0 (Concluída)** | API compila e executa sem assemblies de WPF/Windows (`0` referências a PresentationFramework). |
| **Value Object Money & Tempo Determinístico** | **MIGRADO (Fase 1)** | `decimal` disperso e `DateTime.Now` | `Money` Value Object + `ITimeProvider` | BCL .NET 10 | Baixo (validado) | **P0 (Concluída)** | 24 testes unitários de Money e FakeTimeProvider aprovados. |
| **Clientes & Veículos** | **LEGADO** | `ClientesViewModel`, `VeiculosViewModel` acoplados a SQLite | Entidades em `PRIMOX.Domain`, UseCases em `PRIMOX.Application` | `DatabaseService`, Repositórios legados | Médio | **P1 (Fase 2)** | Cadastros de Cliente e Veículo consumidos como agregados pela Ordem de Serviço. |
| **Estoque & Catálogo de Peças** | **LEGADO** | `EstoqueOperationalService` monolítico | `PRIMOX.Domain.Entities.ItemEstoque` com reserva e baixa atômica | `DatabaseService.cs`, `Dapper` | Alto | **P1 (Fase 2)** | Movimentação e reserva de estoque integradas ao status `Aprovada` / `EmExecucao` da OS. |
| **Inspeção Digital Veicular (DVI 2.0)** | **LEGADO ESTÁVEL** | `DviInspectionService` embutido no WPF | Caso de uso `RealizarInspecaoDvi` em Application | SQLite, ImageSharp | Médio | **P2 (Fase 2.5)** | Checklists fotográficos vinculados diretamente ao ciclo `EmTriagemDvi` da OS. |
| **Módulo Financeiro & Fluxo de Caixa** | **LEGADO** | `FinanceiroDatabaseService` (2.571 linhas) | Casos de uso de Faturamento e Lançamento de Contas | SQLite, Relatórios | Alto | **P2 (Fase 3)** | Faturamento automático disparado pela transição da OS para `Finalizada`. |
| **PRIMOX Repair Intelligence (Base Curada)** | **ISOLADO** | `SureTrackService` com 10 casos curados | `PRIMOX.Knowledge` | `CuratedTechnical` data | Médio | **P2 (Fase 3)** | Integração desacoplada de dados técnicos sem simulação de rede externa. |
| **Módulo Fiscal (Focus NFe)** | **LEGADO ESTÁVEL** | `FocusNfeProvider` (Homologação ativa) | `PRIMOX.Integrations.Fiscal` | `FocusNfeHttpClient` | Médio | **P3 (Fase 3.5)** | Emissão de NFC-e/NF-e disparada por evento de conclusão da OS. |
| **Smoke Tests de Interface (15k linhas)** | **LEGADO** | `UiSmokeTestService*.cs` no binário principal | Projeto separado `PrimoAutoEletrica.Qa` | FlaUI, Windows Desktop | Baixo | **P3 (Fase 3.5)** | Extração das classes de teste para assembly exclusivo sem poluir produção. |
| **Multi-Tenancy Físico SaaS** | **FUTURO** | Banco single-tenant local | `TenantId` em todas as tabelas e queries EF Core | PostgreSQL / SQLite multi-banco | Alto | **P4 (Fase 4)** | Isolamento lógico total de dados para ambiente cloud corporativo. |

---

## 3. CHECKPOINTS E CRITÉRIOS DE NÃO-REGRESSÃO

1. **Compilação Contínua:** Nenhuma etapa de migração pode introduzir erros de build no Release da solução (`0 Erros`).
2. **Execução de Testes Cross-Platform:** Todos os testes de `PRIMOX.Domain`, `PRIMOX.Application` e `PRIMOX.Architecture` devem rodar no Linux e no CI sem dependência de Windows.
3. **Invariantes do Banco:** Nenhuma migração pode recriar tabelas duplicadas (`OrdensServico2`). O esquema existente deve ser evoluído de maneira compatível e idempotente.
