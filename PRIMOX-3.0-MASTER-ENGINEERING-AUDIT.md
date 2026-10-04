# PRIMOX 3.0 — MASTER ENGINEERING AUDIT
**Data:** 04 de Outubro de 2026  
**Versão:** 3.0.0-AUDIT  
**Autor:** Principal Software Architect & Lead Automotive Systems Engineer  
**Repositório:** [CamposCodingHub/PrimoAutoEletrica](https://github.com/CamposCodingHub/PrimoAutoEletrica)  
**Status do Documento:** OFICIAL / FONTE DE VERDADE TÉCNICA (Truth Over Marketing)

---

## SUMÁRIO EXECUTIVO E AVALIAÇÃO HONESTA

Esta auditoria de engenharia estabelece a realidade técnica incontestável do ecossistema **PRIMOX Workshop / PrimoAutoEletrica**, pautando-se estritamente na inspeção do código-fonte compilável, arquivos de projeto, esquemas de persistência, testes automatizados e esteiras de integração contínua.

### Avaliação Dimensional Honesta (Escala 1 a 10 — Sem Marketing)

| Dimensão | Nota | Classificação | Justificativa Objetiva |
| :--- | :---: | :--- | :--- |
| **Arquitetura** | **5.5** | Parcial / Monolítica | DI configurado e camadas conceituais existentes, porém tudo acoplado num único executável WPF (.NET 6.0 EOL); Web API referencia projeto WPF; day-trading alienígena presente. |
| **Código** | **5.0** | Dívida Técnica Alta | Classes com mais de 2.800 linhas; 15.000 linhas de smoke-tests embutidas no assembly principal; 686 ocorrências de `DateTime.Now`; exceções com `catch { }`. |
| **Banco de Dados** | **6.0** | Robusto Local / Monotenant | 38 migrações sequenciais ativas no SQLite/SQL Server; ausência de PostgreSQL; 90 ocorrências de `SELECT *` e 160 de `ObterTodos` sem paginação de servidor. |
| **Segurança** | **5.0** | Básica / Incompleta | PBKDF2 e lockout funcionais; serviço 2FA/TOTP isolado NUNCA acionado no fluxo real de login; Web API sem autenticação JWT ativa; auditoria superficial em strings. |
| **Performance** | **4.5** | Não Medida / Risco Alto | Rápido em bases pequenas de demonstração; risco iminente de exaustão de memória em bases de 10.000+ OS/itens por ausência de paginação; sem benchmarks formais. |
| **UX / UI** | **7.5** | Avançada / Consistente | Design system Dark/Light consistente; Command Palette, paleta automotiva e DVI semafórico de alta qualidade; falta virtualização e modo chão-de-fábrica/tablet. |
| **Gestão / ERP** | **7.0** | Funcional / Maduro | Ciclo de clientes, veículos, ordens de serviço, orçamentos, estoque e compras plenamente operacional em oficina física mono-loja. |
| **Financeiro** | **6.5** | Operacional Básico | Frente de caixa (PDV), fluxo diário e DRE gerencial simplificado; ausência de plano de contas formal, centro de custos, conciliação bancária e pricing dinâmico. |
| **Diagnóstico Técnico** | **6.5** | Especializado / Empírico | 8 centrais catalogadas com precisão de fusíveis/relés (HB20 cofre/painel); esquemas visuais com trace wire; falta motor universal de evidências e camada de segurança. |
| **IA / Copilot** | **6.0** | Híbrido / Não Estruturado | Roteamento em cascata (Gemini -> Ollama -> Determinístico); RAG e fallback monolíticos baseados em strings estáticas de 2.800 linhas; sem AI Router semântico. |
| **Gestão de Frotas** | **5.5** | Estrutural Inicial | Tabelas de contratos B2B e planos preventivos persistidas; falta cálculo de Fleet Risk Score, telemetria e custo automatizado por KM/horímetro. |
| **Mobile** | **1.0** | Scaffold Vazio | Arquivo `.csproj` e `.cs` com 0 bytes (`PrimoAutoEletrica.Maui`); diretório `PrimoAutoEletrica.Mobile` inexistente. Funcionalidade inexistente. |
| **API Web** | **3.5** | Piloto Embrionário | Minimal API com 4 endpoints básicos; sem JWT configurado; compilação atrelada ao Windows Desktop; sem rate limit, versionamento ou multi-tenancy. |
| **SaaS / Cloud** | **2.5** | Não Implementado | Zero ocorrências de `TenantId` no domínio; multi-filial restrita a seletor visual e tabela isolada sem escopo nas entidades reais (OS, Peças, Clientes). |
| **Internacionalização** | **4.0** | Parcial (UI Text Only) | Dicionário de termos em pt/en/es; regras de negócio, moedas (R$), tributos (NFe), unidades e formatação numérica permanentemente travadas no padrão Brasil. |
| **QA / Confiabilidade** | **6.0** | Parcial / Divergente | 114 testes automatizados funcionais; porém TFM Windows Desktop impede execução nativa em Linux; CI ignora falhas de CodeQL com `continue-on-error`. |

---

## A. ESTADO ATUAL DO REPOSITÓRIO

O PRIMOX Workshop encontra-se hoje como uma **solução desktop rica e robusta para uso local e mono-oficina**, com especialização profunda em auto elétrica e eletroeletrônica veicular. No entanto, possui divergências arquiteturais severas entre o que foi projetado/documentado em relatórios de marketing e o que está efetivamente implementado em nível de infraestrutura empresarial.

### Fatos Comprovados no Código-Fonte:
1. **Target Framework Principal:** O aplicativo desktop (`PrimoAutoEletrica.csproj`) roda em `net6.0-windows` (versão EOL sem suporte oficial de segurança da Microsoft). O SDK instalado na máquina é .NET 10.0.112.
2. **Projetos Satélites Desalinhados:** A API web roda em `net9.0-windows`; o projeto de testes roda em `net10.0-windows`; o Mobile possui 0 bytes. Não há centralização de propriedades via `Directory.Build.props` ou pacotes via `Directory.Packages.props`.
3. **Persistência de Produção:** SQLite local (`primoauto.db`) gerenciado por ADO.NET direto e Dapper pontual, com 38 scripts de migração aplicados sequencialmente. O suporte a SQL Server existe via `SqlServerDatabaseProvider.cs`, mas não há suporte a PostgreSQL.

---

## B. ARQUITETURA ATUAL

```
[Camada de Apresentação - WPF]
  MainWindow, Views (*.xaml), UserControls (*.xaml)
  ├── ViewModels (MVVM parcial com CommunityToolkit)
  └── Services Visuais (ThemeService, NavigationService, Accessibility)
            │
            ▼
[Camada de Negócio e Serviços Monolítica - Services/]
  ├── Services Operacionais (OrdemServico, Cliente, Produto, Estoque, Caixa)
  ├── Services de Especialidade Técnica (DVI, SureTrack, Biblioteca, Calculadora, Trace Wire)
  ├── Services de IA (GeminiAIService, LocalOllamaAIService, DeterministicFallback, RAG)
  └── Services de Infraestrutura (DatabaseService, LoggerService, MigrationService)
            │
            ▼
[Acesso a Dados Híbrido]
  ├── Repositories (IClienteRepository, IProdutoRepository, IOrdemServicoRepository...)
  ├── DatabaseService Partials (DatabaseService.OrdensServico.cs com 2.008 linhas de SQL)
  └── DatabaseProviders (SQLiteDatabaseProvider, SqlServerDatabaseProvider)
```

### Principais Desvios Arquiteturais Identificados:
1. **Acoplamento Bidirecional UI-Serviço-Banco:** Telas e controles XAML frequentemente instanciam ou invocam métodos do `DatabaseService` diretamente em seus code-behinds, desviando dos Repositories.
2. **Dependência Cíclica da Web API:** `PrimoAutoEletrica.Api` referencia o executável WPF `PrimoAutoEletrica.csproj` para reaproveitar modelos e serviços, forçando a API a compilar com `-windows` e impedindo deploy em contêineres Linux leves.
3. **Inexistência de Camada de Domínio Pura:** Os modelos de domínio (`Models/`) contêm anotações, métodos de serialização JSON e lógica de apresentação misturados.

---

## C. DÍVIDA TÉCNICA CRÍTICA

### 1. Classes Monolíticas que Violam o SRP (Single Responsibility Principle)
* **`AutomotiveDiagnosticRAGService.cs` (2.823 linhas):** Contém mais de 2.000 linhas de inicializadores C# hardcoded com tabelas de DTCs, sintomas e procedimentos que deveriam residir em repositório de dados.
* **`DeterministicFallbackAIService.cs` (2.197 linhas):** Regras gigânticas de expressões regulares e interpolação de strings para simular respostas técnicas.
* **`FinanceiroDatabaseService.cs` (2.571 linhas):** Mistura conciliação de caixa, geração de parcelas, emissão de relatórios e consultas agregadas.
* **`DatabaseService.OrdensServico.cs` (2.008 linhas):** Mapeamento e queries manuais de OS espalhadas em partial class, duplicando responsabilidade do `OrdemServicoRepository.cs`.
* **`AiDiagnosticCenterControl.xaml.cs` (2.141 linhas):** Code-behind gigantesco orquestrando chamadas de IA, carregamento de esquemas, SureTrack e DVI.
* **`UiSmokeTestService*.cs` (15.000+ linhas):** Código de testes e QA compilado diretamente dentro do binário de produção.

### 2. Resíduos e Código Alienígena
* **`PrimoAutoEletrica/Simulation/TradeSimulationService.cs`:** Código de negociação financeira de mini-contratos ("4 contratos entram, alvo fixo e breakeven"), completamente estranho ao escopo automotivo.
* **Projetos Duplicados em Disco:**
  * `PrimoAutoEletrica.Api` vs `PrimoAutoEletrica/Api`
  * `Tests/PrimoAutoEletrica.Tests` vs `PrimoAutoEletrica.Tests`
  * `PrimoAutoEletrica.Simulation` vs `PrimoAutoEletrica/Simulation`

### 3. Falta de Tratamento de Tempo e Fuso
* **686 ocorrências de `DateTime.Now`:** Risco de inconsistência em registros fiscais, auditoria e sincronização multi-fuso. Inexistência de abstração de relógio (`ITimeProvider` / `DateTimeOffset`).

### 4. Consultas Descontroladas
* **90 ocorrências de `SELECT *`:** Tráfego excessivo e fragilidade a alterações de schema.
* **160 métodos do tipo `ObterTodos`:** Listagens completas de clientes, produtos e ordens carregadas em memória sem `LIMIT`/`OFFSET` no banco.

---

## D. FUNCIONALIDADES REAIS E TESTADAS

As seguintes funcionalidades foram comprovadas no código executável e validadas:

1. **Gestão Operacional de Auto Elétrica (REAL + TESTADA):**
   * Cadastro completo de Clientes (Física/Jurídica, validação CPF/CNPJ, LGPD soft-delete).
   * Cadastro de Veículos com histórico de serviços vinculados por placa.
   * Abertura, tramitação, faturamento e encerramento de Ordens de Serviço com cálculo de mão de obra e peças.
   * Controle de Estoque com movimentações de entrada/saída, reserva e alerta de estoque mínimo.
   * Frente de Caixa (PDV) com múltiplos meios de pagamento (Dinheiro, PIX, Cartão).
2. **Inspeção Digital Veicular - DVI 2.0 (REAL + TESTADA):**
   * Checklist semafórico (Verde, Amarelo, Vermelho).
   * Captura de evidências fotográficas com compressão ImageSharp.
   * Conversão automática de itens pendentes em orçamento prévio (`DviInspectionTests` com 14/14 aprovados).
3. **Mapeamento Cirúrgico de Fusíveis e Relés (REAL + TESTADA):**
   * 8 centrais elétricas pré-populadas no banco SQLite com diferenciação estrita de localização:
     * Hyundai HB20 (2012-2019): Painel Interno (BCM) vs Cofre do Motor.
     * VW Gol G5 / Voyage EA111.
     * Fiat Palio / Strada Fire & Fire EVO.
     * GM Onix / Prisma SPE/4.
     * Toyota Corolla Dual VVT-i.
     * Scania Série R (24V).
     * Mercedes-Benz Atego (24V).
   * Roteamento determinístico testado com 114 casos e 100% de precisão.
4. **Calculadora de Queda de Tensão Automotiva (REAL + TESTADA):**
   * Implementação da norma técnica DIN 72551 para circuitos de 12V e 24V.
   * Cálculo de perda percentual e recomendação de bitola em cobre estanhado.
5. **Esquemas Elétricos Visuais e Trace Wire (REAL + TESTADA):**
   * Grafo de circuitos com nós de alimentação (Linha 30, Linha 15, Linha 31, Linha 50).
   * 6 diagramas técnicos em alta resolução empacotados e destacados na interface.
6. **Ponte IA → Ordem de Serviço (1-Click Bridge) (REAL + TESTADA):**
   * Extração de diagnósticos da IA e proposição estruturada de peças e tempo padrão de mão de obra com inserção direta na O.S. ativa.

---

## E. FUNCIONALIDADES PARCIAIS

1. **SureTrack / Base de Casos:** Funciona na UI e no banco, porém os dados iniciais são sintéticos/curados e falsamente identificados como "RedeHomologada". Deve evoluir para **PRIMOX Repair Intelligence**.
2. **Módulo Fiscal (Focus NFe):** Adapter implementado e testado em ambiente de homologação. O ambiente de produção é bloqueado por software (`FiscalProductionGuard`), impedindo emissão real com valor jurídico.
3. **Multi-Filial:** Existe a tela `SelecaoFilialWindow` e tabela `Filiais`, mas as entidades centrais (`OrdemServico`, `Cliente`, `Produto`, `CaixaOperacional`) não possuem a coluna `FilialId`. O isolamento de dados não existe.
4. **Internacionalização (i18n):** O seletor de idiomas altera apenas textos estáticos de telas. A moeda é invariavelmente `R$`, unidades são fixas em métrico e regras fiscais são exclusivas do Brasil.

---

## F. SCAFFOLDS, MOCKS E PLACEHOLDERS

1. **`PrimoAutoEletrica.Maui`:** Projeto de 0 bytes; nenhum código mobile funcional.
2. **`TwoFactorSetupWindow.xaml` / 2FA no Login:** A janela e o serviço TOTP existem, mas o fluxo de login em `LoginWindow.xaml.cs` nunca exige autenticação em duas etapas.
3. **`PrimoAutoEletrica.Api`:** Apenas uma casca de Minimal API com métodos `ObterTodos` e sem camada de autenticação real via JWT.

---

## G. DADOS SINTÉTICOS: REGISTRO DE VERDADE

* **Casos Curados no `SureTrackService.cs`:** Casos como Onix com bobina trincada (P0300) e Gol com sensor ECT (P0118) foram redigidos internamente e marcados artificialmente com `OrigemCaso = "RedeHomologada"` e `OcorrenciasConfirmadas = 14`.
* **Classificação Obrigatória a partir de agora:** Devem ser categorizados como `CuratedTechnical` ou `Synthetic`, e nunca apresentados ao técnico como estatísticas de uma rede inexistente de oficinas credenciadas.

---

## H. DIAGNÓSTICO DE SEGURANÇA

1. **Web API Desprotegida:** Endpoints expostos sem middleware de autenticação JWT ativo.
2. **Secrets em Texto:** Arquivos `appsettings.json` possuem estruturas para chaves de terceiros sem cofre de credenciais nativo.
3. **Auditoria Superficial:** A tabela `AuditLogs` não armazena snapshots de valores anteriores e novos (`Diff`), dificultando perícia em caso de fraude financeira ou manipulação de estoque.
4. **GitHub Actions Security Gate Permissivo:** `.github/workflows/ci.yml` contém `continue-on-error: true` na análise de segurança do CodeQL.

---

## I. DIAGNÓSTICO DE PERFORMANCE

1. **Falta de Paginação no Banco:**
   * `ObterTodosClientes()` carrega toda a tabela para listas em memória.
   * `ObterTodosProdutos()` carrega fotos e anexos sem projeções resumidas.
2. **Consultas Não Indexadas em Faturamento:** Agrupamentos de relatórios financeiros executados em memória via LINQ em vez de queries SQL agregadas (`SUM`, `GROUP BY`).
3. **UI Thread Block:** Operações síncronas de I/O em leitura de arquivos de diagramas podem causar micro-travamentos na UI se o disco estiver sob carga.

---

## J. DIAGNÓSTICO DE UX / UI

1. **Prós:**
   * Contraste excelente em Dark Mode; navegação rápida por atalhos (`Ctrl+K`, `F1-F12`).
   * Componentes claros para visualização de relés e fusíveis com identificação por cor (Amperagem DIN).
2. **Contras:**
   * Complexidade elevada para uso direto em tablets sob o capô do veículo.
   * Ausência de botões gigantes de toque para confirmação rápida de medições (ex: "[ PASS - 12.5V ]", "[ FAIL - 0V ]").

---

## K. DIAGNÓSTICO DO BANCO DE DADOS

1. **Motor Atual:** SQLite é excelente para operação autônoma sem servidor. No entanto, para redes de oficinas ou SaaS centralizado, sofre com concorrência de escrita (`database is locked`).
2. **Falta de Índices Compostos:** Faltam índices nas buscas por `(Placa, Ano)` e `(Modelo, CodigoDTC)`.
3. **Persistência de Imagens:** Fotos do DVI são salvas em disco local e o caminho é gravado no banco; caso o caminho absoluto mude de máquina, os links são rompidos se não houver normalização relativa.

---

## L. DIAGNÓSTICO DE IA E DIAGNÓSTICO GUIADO

1. **Inexistência do Motor de Evidências:** A IA atual é conversacional e reativa. Falta a estrutura que armazena a sessão de diagnóstico como uma árvore lógica formal (`DiagnosticSession` com `Measurement`, `Hypothesis`, `TestResult`).
2. **Inexistência da Camada de Segurança:** Não há barreira de validação que impeça a IA de sugerir testes perigosos (ex: jumper direto em atuadores de alta corrente, alimentação direta de pinos de ECU com 12V em linha de sensor 5V, manipulação de rede CAN sob chave ligada).
3. **Acoplamento Extremo do Fallback:** Quase 5.000 linhas de código C# dedicadas unicamente a regras determinísticas de regex que deveriam ser tabeladas em grafo de decisão.

---

## M. DIAGNÓSTICO DE INTERNACIONALIZAÇÃO

* O sistema opera com regras contábeis, tributárias e monetárias 100% brasileiras:
  * Moeda: `R$` hardcoded em máscaras de exibição.
  * Identificação: CPF/CNPJ e formato Mercosul de placas.
  * Métricas: Quilômetros (km), Bar e Graus Celsius (°C).
* Uma eventual expansão para América do Norte (EUA/Canadá) exige desacoplar a formatação numérica da UI e introduzir o conceito de **Localidade Empresarial**.

---

## N. DIAGNÓSTICO DO MÓDULO FISCAL

* **Status Oficial:** **HOMOLOGAÇÃO ATIVA / PRODUÇÃO BLOQUEADA**.
* A camada fiscal possui excelente arquitetura de contratos (`IFiscalProvider`, `FiscalApplicationService`, `VendaFiscalNFeMapper`).
* No entanto, por segurança legal estrita, a emissão em produção está e deve permanecer desativada até que haja credenciamento oficial, certificado digital A1 em nuvem e homologação comprovada com SEFAZ.

---

## O. MATRIZ DE DECISÃO TÉCNICA (KEEP / REFACTOR / REMOVE / FUTURE)

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│                                MATRIZ PRIMOX 3.0                                │
├─────────────────────┬────────────────────────────────────────────────────────────┤
│ AÇÃO                │ MÓDULOS E COMPONENTES                                      │
├─────────────────────┼────────────────────────────────────────────────────────────┤
│ KEEP (Manter)       │ - Fluxo de Ordem de Serviço, Clientes, Veículos e Estoque  │
│                     │ - DVI 2.0 (Checklist, Semáforo, Laudo e Fotos)             │
│                     │ - Banco de Centrais Elétricas (8 centrais de fusíveis)     │
│                     │ - Calculadora de Queda de Tensão (DIN 72551)               │
│                     │ - Mapeamento e Visualização de Esquemas Elétricos          │
│                     │ - Ponte 1-Click Bridge IA -> O.S.                          │
│                     │ - Mecanismo de Migrações do Banco de Dados                 │
│                     │ - Design System (Cores Dark/Light, Brushes e Tipografia)   │
├─────────────────────┼────────────────────────────────────────────────────────────┤
│ REFACTOR (Refatorar)│ - RAG e Fallback IA (extrair strings para Knowledge Repo)  │
│                     │ - FinanceiroDatabaseService (separar Caixa, DRE e Títulos) │
│                     │ - DatabaseService.OrdensServico (unificar no Repository)   │
│                     │ - Eliminar 686 DateTime.Now substituindo por ITimeProvider │
│                     │ - Substituir SELECT * e ObterTodos por Queries Paginadas   │
│                     │ - Mover 15.000 linhas de Smoke Tests para projeto de testes│
│                     │ - Migrar solução para .NET 10 LTS unificado                │
├─────────────────────┼────────────────────────────────────────────────────────────┤
│ SUBSTITUTE (Mudar)  │ - SureTrack -> PRIMOX Repair Intelligence (identidade nova)│
│                     │ - "RedeHomologada" -> "CuratedTechnical" / "Synthetic"     │
│                     │ - 2FA Mock -> Desafio Real de Segundo Fator no Login       │
│                     │ - Web API Piloto -> API Modular com Autenticação JWT Real  │
├─────────────────────┼────────────────────────────────────────────────────────────┤
│ REMOVE (Remover)    │ - PrimoAutoEletrica/Simulation/TradeSimulationService.cs   │
│                     │ - Diretórios duplicados (Simulation, Api e Tests redundantes)│
│                     │ - continue-on-error no CodeQL do GitHub Actions            │
├─────────────────────┼────────────────────────────────────────────────────────────┤
│ FUTURE (Planejar)   │ - Diagnostic Evidence Engine (Sessão, Medição, Hipótese)   │
│                     │ - Diagnostic Safety Layer (Travas de segurança mecânica)   │
│                     │ - Digital Bay (Visão em tempo real de boxes de oficina)    │
│                     │ - Multi-Tenant real (TenantId no banco e na API)           │
│                     │ - App Técnico Mobile/Tablet (MAUI ou Web PWA)              │
│                     │ - Suporte central a PostgreSQL / Cloud Database            │
│                     │ - Integração física com Scanner / Osciloscópio             │
└─────────────────────┴────────────────────────────────────────────────────────────┘
```

---

## P. ARQUITETURA ALVO (PRIMOX 3.0 ENTERPRISE MODULAR)

```
src/
  ├── PRIMOX.Domain/              <- Entidades puras, Enums, Value Objects, Regras
  ├── PRIMOX.Application/         <- Casos de uso, DTOs, Interfaces, Comandos
  ├── PRIMOX.Infrastructure/      <- SQLite, SQL Server, PostgreSQL, Criptografia
  ├── PRIMOX.Diagnostics/         <- Evidence Engine, Safety Layer, Medições
  ├── PRIMOX.Knowledge/           <- Esquemas, Centrais, Pinagens, Procedimentos
  ├── PRIMOX.RepairIntelligence/  <- Estatísticas reais, Casos confirmados
  ├── PRIMOX.AI/                  <- AI Router, Gemini, Ollama, Fallback Determinístico
  ├── PRIMOX.Fiscal/              <- NF-e, NFC-e, NFS-e, Validadores e Provedores
  ├── PRIMOX.Api/                 <- ASP.NET Core Web API, JWT, Rate Limiting
  └── PRIMOX.Desktop/             <- WPF, XAML, ViewModels, Design System

tests/
  ├── PRIMOX.UnitTests/           <- Testes de unidade puros (Domain + Application)
  ├── PRIMOX.IntegrationTests/    <- Testes de banco e repositórios
  ├── PRIMOX.DiagnosticsTests/    <- Benchmark automotivo e validação de segurança
  └── PRIMOX.UiSmokeTests/        <- Smoke tests isolados do assembly de produção
```

---

## Q. ROADMAP DE EXECUÇÃO EM FASES ESTRITAS

### Fase 0: A Verdade do Produto e Limpeza de Resíduos (Imediata)
* Remoção de código alienígena (`TradeSimulationService`).
* Eliminação de diretórios e projetos duplicados na árvore do repositório.
* Substituição formal da nomenclatura "SureTrack" para **PRIMOX Repair Intelligence**.
* Identificação explícita de casos sintéticos/curados como `CuratedTechnical`.

### Fase 1: Padronização .NET 10 LTS e Centralização de Dependências
* Criação de `Directory.Build.props` e `Directory.Packages.props` na raiz.
* Elevação do projeto principal e projetos satélites para .NET 10 LTS.
* Resolução de referências de dependência cíclicas entre API e WPF.

### Fase 2: Banco de Dados, Paginação e Desacoplamento Temporal
* Criação da abstração `ITimeProvider` / `DateTimeOffset` substituindo `DateTime.Now`.
* Eliminação de `SELECT *` e implementação de paginação server-side nos repositórios.
* Preparação do schema de banco com migração segura.

### Fase 3: Diagnostic Evidence Engine e Safety Layer
* Modelagem de `DiagnosticSession`, `DiagnosticMeasurement`, `DiagnosticHypothesis`.
* Criação do `DiagnosticSafetyLayer` avaliando risco térmico, elétrico e curto-circuito antes de qualquer instrução de teste.
* Interface para o técnico registrar medições objetivas (Tensão, Resistência, Queda).

### Fase 4: Refatoração do RAG e Arquitetura de Conhecimento
* Extração do monólito `AutomotiveDiagnosticRAGService` para repositório particionado.
* Criação do `DiagnosticRankingService` e `DiagnosticApplicabilityService`.
* Roteador de IA semântico (`AIRouter`) direcionando consultas entre cálculo, estoque e diagnóstico.

### Fase 5: Fortalecimento da O.S. como Núcleo (OS 360) e Digital Bay
* Integração total: DVI -> Evidências -> Orçamento -> Execução -> Histórico -> Inteligência.
* Visão de pátio por boxes ("Digital Bay") para gestão visual em tempo real.

---

## R. PRIORIDADES ABSOLUTAS DE AÇÃO (P0 a P4)

* **P0 (Imediato - Higiene e Fundamentos):**
  1. Remover `TradeSimulationService` e resíduos de simulação de trading.
  2. Limpar projetos duplicados e consolidar a solução `PrimoAutoEletrica.sln`.
  3. Criar `Directory.Build.props` e `Directory.Packages.props` padronizando versões.
  4. Renomear e reclassificar dados de "SureTrack" para **PRIMOX Repair Intelligence** com flags de proveniência (`CuratedTechnical`).
  5. Desacoplar a Web API do projeto WPF.
* **P1 (Fundação de Engenharia e Diagnóstico):**
  1. Implementar o `DiagnosticSafetyLayer` para impedir sugestões perigosas.
  2. Implementar o `DiagnosticEvidenceEngine` estruturado.
  3. Eliminar `DateTime.Now` e introduzir paginação real em estoque e clientes.
* **P2 (Inteligência e Gestão Avançada):**
  1. Implementar o AI Router para desmembrar o fallback monolítico.
  2. Implementar a visão de pátio Digital Bay.
  3. Implementar Fleet Risk Score para gestão de frotas.
* **P3 (SaaS, Nuvem e Mobile):**
  1. Adicionar `TenantId` no modelo e arquitetar isolamento multi-tenant.
  2. Suporte oficial a PostgreSQL.
  3. Desenvolver client mobile/tablet dedicado para chão de fábrica.
* **P4 (Internacionalização Real):**
  1. Localização por país (moeda, impostos, unidades e normas técnicas internacionais).

---
*Relatório auditado e validado em 04/10/2026. Aprovado para execução técnica da Fase 0.*
