# PRIMOX 3.0 — ARCHITECTURE BLUEPRINT
**Data:** 04 de Outubro de 2026  
**Versão:** 3.0.0-BLUEPRINT  
**Status:** ESPECIFICAÇÃO TÉCNICA OFICIAL E VINCULANTE  
**Objetivo:** Estabelecer a topologia de camadas, regras estritas de dependência e desacoplamento do ecossistema PRIMOX.

---

## 1. VISÃO GERAL DAS CAMADAS

Para transformar o PRIMOX em uma plataforma sustentável e escalável, a estrutura monolítica atual do projeto desktop será gradualmente decomposta em camadas funcionais com limites bem definidos:

```
                          ┌───────────────────────┐
                          │     PRIMOX.Domain     │ (Entidades puras, Enums, Value Objects)
                          └───────────▲───────────┘
                                      │
                          ┌───────────┴───────────┐
                          │  PRIMOX.Application   │ (Casos de Uso, DTOs, Contratos, Portas)
                          └───────────▲───────────┘
                                      │
        ┌─────────────────────────────┼─────────────────────────────┐
        │                             │                             │
┌───────┴──────────────┐   ┌──────────┴───────────┐   ┌─────────────┴──────────┐
│  PRIMOX.Diagnostics  │   │   PRIMOX.Knowledge   │   │       PRIMOX.AI        │
│ (Evidence & Safety)  │   │(Diagramas, Centrais) │   │ (Router, Models, RAG)  │
└───────▲──────────────┘   └──────────▲───────────┘   └─────────────▲──────────┘
        │                             │                             │
        └─────────────────────────────┼─────────────────────────────┘
                                      │
                    ┌─────────────────┴─────────────────┐
                    │      PRIMOX.Infrastructure        │ (SQLite, SQL Server, Postgre, IO)
                    └─────────────────▲─────────────────┘
                                      │
           ┌──────────────────────────┼──────────────────────────┐
           │                          │                          │
┌──────────┴──────────┐   ┌───────────┴──────────┐   ┌───────────┴──────────┐
│   PRIMOX.Desktop    │   │      PRIMOX.Api      │   │    PRIMOX.Mobile     │
│   (WPF / Windows)   │   │(ASP.NET Core Web API)│   │ (PWA / Field Tablet) │
└─────────────────────┘   └──────────────────────┘   └──────────────────────┘
```

---

## 2. MATRIZ DE DEPENDÊNCIAS PERMITIDAS E PROIBIDAS

### 2.1 Dependências Permitidas

| Projeto / Camada | Pode Depender De | Justificativa |
| :--- | :--- | :--- |
| **`PRIMOX.Domain`** | *Nenhum* (Apenas BCL .NET básico) | Núcleo de domínio puro sem dependência de framework, banco ou UI. |
| **`PRIMOX.Application`** | `PRIMOX.Domain` | Orquestra regras de negócio através de interfaces e DTOs. |
| **`PRIMOX.Diagnostics`** | `PRIMOX.Domain`, `PRIMOX.Application` | Especialização de evidência técnica, testes e regras de segurança elétrica. |
| **`PRIMOX.Knowledge`** | `PRIMOX.Domain`, `PRIMOX.Application` | Repositório de centrais elétricas, pinagens, diagramas e dados OEM. |
| **`PRIMOX.AI`** | `PRIMOX.Diagnostics`, `PRIMOX.Knowledge`, `PRIMOX.Application` | Roteador e adaptadores de IA consultando evidências e conhecimentos. |
| **`PRIMOX.Infrastructure`** | `PRIMOX.Application`, `PRIMOX.Domain` | Implementa repositórios, persistência (SQLite/SQL Server) e serviços externos. |
| **`PRIMOX.Desktop`** | `PRIMOX.Application`, `PRIMOX.Infrastructure`, `PRIMOX.Diagnostics`, `PRIMOX.AI` | Apresentação rica em WPF / XAML. |
| **`PRIMOX.Api`** | `PRIMOX.Application`, `PRIMOX.Infrastructure` | Exposição HTTP headless sem acoplamento à UI. |
| **`PRIMOX.Mobile`** | `PRIMOX.Application`, `PRIMOX.Api` | Interface leve para o mecânico no box ou no pátio. |

### 2.2 Dependências Estritamente Proibidas (Regras de Ouro)

```text
API          → WPF          = PROIBIDO (Impede deploy em Docker / Linux headless)
Domain       → WPF          = PROIBIDO (Poluição de domínio com UI)
Domain       → SQLite / SQL = PROIBIDO (Domínio agnóstico a banco)
Domain       → HTTP / Net   = PROIBIDO (Sem I/O de rede no núcleo)
Application  → WPF          = PROIBIDO (Casos de uso independentes da tela)
Diagnostics  → WPF          = PROIBIDO (Mecanismo de medição roda em qualquer runtime)
AI           → WPF          = PROIBIDO (Roteamento de IA é serviço de aplicação)
```

---

## 3. RESPONSABILIDADE DETALHADA POR CAMADA

### 3.1 `PRIMOX.Domain`
* **Entidades:** `OrdemServico`, `Veiculo`, `Cliente`, `ItemOS`, `Produto`, `MovimentacaoEstoque`, `TransacaoFinanceira`.
* **Value Objects:** `Money`, `Currency`, `BitolaCabo`, `MedicaoTensao`, `PlacaVeiculo`, `VIN`.
* **Enums e Estados:** `StatusOS`, `SeveridadeDvi`, `NivelRiscoDiagnostico`, `NivelEvidenciaTecnica`.
* **Regras Invariantes:** Cálculo de totais de OS, validação de transição de status, integridade de estoque.

### 3.2 `PRIMOX.Application`
* **Portas (Interfaces):** `IOrdemServicoRepository`, `IProdutoRepository`, `ITimeProvider`, `IDiagnosticEvidenceService`.
* **Comandos e Consultas:** DTOs de entrada e saída, paginação (`PagedResult<T>`), orquestração de transações.
* **Segurança de Aplicação:** `IAuthorizationPolicy`, contextos de execução (`IExecutionContext`).

### 3.3 `PRIMOX.Diagnostics` (Motor de Evidências e Camada de Segurança)
* **`DiagnosticEvidenceEngine`:** Gerenciamento do ciclo Sintoma -> DTC -> Teste -> Medição -> Hipótese -> Causa Raiz.
* **`DiagnosticSafetyLayer`:** Barreira de contenção que valida riscos elétricos, de alta corrente e curto antes de autorizar qualquer instrução técnica ao mecânico.
* **Calculadoras Técnicas:** Implementação matemática de normas DIN (queda de tensão, dimensionamento).

### 3.4 `PRIMOX.Knowledge`
* **Bases Estruturadas:** Mapeamento de fusíveis e relés (8 centrais), diagramas de injeção, pinagens de ECU/BCM.
* **Catalogação de Fontes:** Associação de cada dado com `KnowledgeSource` (OEM, Curated, Synthetic).

### 3.5 `PRIMOX.AI`
* **`AIRouter`:** Classificador de intenções (Diagnóstico, Estoque, Navegação, Conversa, Cálculo).
* **Adaptadores:** `GeminiAIService` (Nuvem), `LocalOllamaAIService` (Local), `DeterministicFallbackAIService` (Motor Determinístico).
* **Tools:** Registro de ferramentas de consulta idempotentes (`AIToolRegistry`).

### 3.6 `PRIMOX.Infrastructure`
* **Provedores de Dados:** `SQLiteDatabaseProvider`, `SqlServerDatabaseProvider` e futuro `PostgreSqlDatabaseProvider`.
* **Repositórios Concretos:** Mapeamento Dapper/ADO.NET com consultas parametrizadas e paginação de banco.
* **Relógio do Sistema:** Implementação de `ITimeProvider` baseada em `DateTimeOffset.UtcNow`.

---

## 4. ESTRATÉGIAS OPERACIONAIS E TECNOLÓGICAS

### 4.1 Estratégia Desktop (WPF)
* O desktop continua sendo a principal estação de trabalho de alta densidade da oficina (frente de caixa, administrativo, diagnóstico em bancada).
* Utiliza o design system consolidado (Dark/Light Mode), CommunityToolkit.Mvvm e injeção de dependência centralizada.
* Comunica-se diretamente com a camada `Application` e `Infrastructure` local em modo embarcado.

### 4.2 Estratégia Web API
* Arquitetura ASP.NET Core minimalista e desacoplada do Windows Desktop.
* Foco em interoperabilidade: tablets, aplicativos clientes e futuras integrações com scanners/rastreadores.
* Autenticação padronizada com JWT Bearer tokens contendo claims de identidade, filial e perfis.

### 4.3 Estratégia Mobile / Tablet
* **Decisão Arquitetural:** Em vez de manter scaffolds vazios de MAUI sem manutenção, o mobile para técnicos em chão de fábrica priorizará **Web PWA Responsivo / Tablet Client** consumindo a Web API.
* Foco em ergonomia de chão de fábrica: botões de toque de alto contraste, captura direta de fotos de DVI e seleção de medições em 1 toque.

### 4.4 Estratégia Offline-First
* A oficina física **não pode parar se a internet cair**.
* Operação local prioritária com SQLite. Em instalações de rede local, suporte a SQL Server local.
* Fila de sincronização (Outbox Pattern) para upload assíncrono de relatórios fiscais, backups e dados de IA em nuvem.

### 4.5 Estratégia Multi-Filial e Futuro Multi-Tenant
* Inclusão gradual de `FilialId` e `TenantId` em todas as entidades mestres.
* As consultas em `Application` exigirão contexto de filial ativa (`IBranchContext`), evitando vazamento acidental de dados entre unidades.

### 4.6 Estratégia de Diagnóstico e Evidências
* Substituição gradual de chatbots puramente textuais pelo **Diagnostic Evidence Engine**.
* A IA atua como co-piloto para preencher o formulário de evidência e sugerir o próximo passo lógico baseado em medições reais, e não como um oráculo alucinador.

### 4.7 Estratégia Financeira (Finance Intelligence)
* Separação estrita entre:
  1. **Caixa Diário Operacional:** Abertura, fechamento, sangria e suprimento.
  2. **Contas a Pagar / Receber:** Títulos, datas de vencimento, liquidações.
  3. **DRE e Margem Gerencial:** Custo de mercadoria vendida (CMV), mão de obra e lucro real por OS.
  4. **Pricing Dinâmico:** Recomendações de margem mínima aprovadas pelo gestor.
