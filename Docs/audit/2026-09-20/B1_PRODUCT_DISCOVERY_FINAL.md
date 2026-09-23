# PRIMOX WORKSHOP — RELATÓRIO FINAL DE DISCOVERY (FASE B1)
**Data de Emissão:** 2026-09-20 / 2026-09-23  
**Branch:** `audit/product-discovery-2026-09`  
**Escopo:** Auditoria Real, Cirúrgica e Baseada em Evidências do Produto PRIMOX Workshop  
**Status do Gate B1:** **PASS** (100% dos requisitos de auditoria atendidos)

---

```text
================================================================================
PRIMOX PRODUCT DISCOVERY — FOTOGRAFIA OBJETIVA DO PRODUTO REAL
================================================================================

TOTAL DE FUNCIONALIDADES AUDITADAS NA MATRIZ: 58

CORE:                 39  (67.2%)
PARTIAL:               7  (12.1%)
UI_ONLY:               3  ( 5.2%)
MOCK:                  1  ( 1.7%)
NOT_IMPLEMENTED:       7  (12.1%)
EXTERNAL_DEPENDENCY:   1  ( 1.7%)

360 FLOW (14 ETAPAS OPERACIONAIS):
CORE:                 12  (85.7%)
PARTIAL:               2  (14.3%)
GAP:                   0  ( 0.0%)

BANCO DE DADOS DE PRODUÇÃO:
Status:               100% INTACTO E PROTEGIDO COMO READ-ONLY
SHA-256 Oficial:      C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B
User Version:         0 (Nenhuma migration, alter table ou update executado)
================================================================================
```

---

## 1. Executive Summary

A presente auditoria B1 foi realizada com o objetivo de responder com máxima precisão: **"O que o PRIMOX Workshop realmente tem hoje?"**, superando quaisquer suposições teóricas.

A auditoria cobriu toda a base de código C# (.NET 10 WPF), 53 janelas (Views), 28 controles reutilizáveis (UserControls), 22 ViewModels, 203 classes de serviço, 13 arquivos de repositório, 58 tabelas no SQLite, 12 endpoints da Minimal API REST, 383 testes automatizados de regressão e o banco de dados oficial de homologação/produção.

**Principais Conclusões:**
1. **O Núcleo da Oficina é Real e Forte (`CORE` = 67.2%):** A gestão de clientes, veículos, orçamentos, ordens de serviço, catálogo massivo de autopeças (4.287 peças e 51.119 aplicações), baixa automática de estoque, conciliação financeira, sessões de caixa, emissão de comprovantes e trilha de auditoria crítica estão plenamente implementadas, persistentes e testadas.
2. **O Fluxo 360 é Fluido e Livre de Redigitação (85.7% `CORE`):** A conversão de Cliente → Veículo → Orçamento → OS → Peças → Execução → Pagamento → Histórico ocorre sem nenhuma redigitação de dados.
3. **Persistência de Diagnósticos Especiais:** O Prontuário Elétrico Veicular armazena 17 parâmetros técnicos estruturados no banco. No entanto, os resultados dos testes guiados de diagnóstico (bateria, alternador, fuga de corrente) são gravados em arquivo JSON global sem chave primária de OS/Veículo (`PARTIAL`).
4. **Fundação Fiscal e Guard de Produção:** O módulo fiscal importa notas XML de fornecedores e emite DANFE informativo com precisão. A emissão direta contra a SEFAZ está operacional em Homologação, mas propositalmente bloqueada para Produção por meio da classe `FiscalProductionGuard.cs` (`EXTERNAL_DEPENDENCY`).
5. **Licenciamento Local:** O licenciamento é um scaffold local simplificado (`IsCommercialScaffoldOnly = true` em `LicenseService.cs`), sem servidor de validação SaaS nem criptografia assimétrica (`MOCK`).
6. **Segurança do Banco de Dados Real:** O banco oficial em `%LOCALAPPDATA%\PrimoAutoEletrica\primoauto.db` permaneceu estritamente inviolado durante toda a fase, apresentando o hash SHA-256 canônico `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` e protegido como Read-Only.

---

## 2. Product Inventory

O inventário completo e detalhado de todos os 25 módulos do sistema encontra-se catalogado no artefato complementar:
👉 [`Docs/audit/2026-09-20/B1_PRODUCT_INVENTORY.md`](file:///c:/Projetos/PrimoAutoEletrica/Docs/audit/2026-09-20/B1_PRODUCT_INVENTORY.md)

* **Dashboard:** CORE (agrega KPIs de vendas, OS e estoque).
* **Clientes:** CORE (cadastro, consulta, histórico e LGPD).
* **Veículos:** CORE (frota, placa, modelo, telemetria elétrica).
* **Cliente 360:** CORE (painel unificado por ID).
* **Vehicle 360:** CORE (painel unificado por ID de veículo).
* **Orçamentos:** CORE (cálculo, margens, desconto, conversão em OS).
* **DVI:** PARTIAL (checklist completo em JSON, sem portal web remoto).
* **Ordens de Serviço:** CORE (quadro Kanban, baixa de estoque, eventos).
* **Autoelétrica Técnica:** PARTIAL (prontuário no banco; roteiros em JSON global).
* **Histórico Técnico:** CORE (rastreabilidade por veículo).
* **Estoque / Produtos:** CORE (gestão de produtos, movimentação, histórico).
* **Catálogo de Peças:** CORE (4.287 peças, 51.119 vínculos).
* **Financeiro:** CORE (contas a pagar, receber, movimentações, saldo).
* **Caixa:** CORE (sessões, abertura, sangria, suprimento, fechamento cego).
* **Vendas (PDV):** CORE (balcão, múltiplos pagamentos, baixa de estoque).
* **Relatórios:** CORE (exportação CSV, impressão PDF, DRE).
* **Fiscal:** PARTIAL (Importação XML e DANFE são CORE; SEFAZ Produção é EXTERNAL_DEPENDENCY).
* **Agenda:** CORE (timeline, conflitos, conversão em OS).
* **Funcionários:** CORE (cargos, 2FA TOTP, login, auditoria).
* **Configurações:** CORE (parâmetros gerais da oficina).
* **Backup / Restore:** CORE (jail enforcement, verificação SHA-256, pré-restore seguro).
* **Auditoria:** CORE (tabela `AuditLogs` com 1.592 eventos reais).
* **RBAC:** CORE (10 perfis, 82 permissões, validação em botões e menus).
* **Licenciamento:** MOCK (scaffold local sem servidor).
* **API REST:** CORE (Minimal API com JWT para rotas existentes).
* **Integrações Externas:** PARTIAL (WhatsApp link; sem TEF nem gateways automáticos).

---

## 3. 360 Workflow

O mapeamento minucioso de cada transição, chave primária e nível de integração encontra-se em:
👉 [`Docs/audit/2026-09-20/B1_360_WORKFLOW_MAP.md`](file:///c:/Projetos/PrimoAutoEletrica/Docs/audit/2026-09-20/B1_360_WORKFLOW_MAP.md)

* **Aderência do Fluxo:** 85.7% CORE (12 de 14 etapas 100% integradas).
* **Redigitação:** ZERO em todas as transições entre módulos.
* **Integridade Referencial:** Vínculos garantidos por `ClienteId`, `VeiculoId`, `OrcamentoId` e `ProdutoId`.

---

## 4. Customer 360 (Cliente 360)

* **Tela:** `Views/HistoricoClienteWindow.xaml`
* **Serviço Central:** `Primox360Service.cs` (`ObterCliente360(Guid clienteId)`)
* **Dados Agregados em Tempo Real:**
  - Identidade do cliente, contatos, CPF/CNPJ, termos de consentimento LGPD;
  - Frota vinculada de veículos (`ClienteRepository.ObterVeiculosPorClienteId`);
  - Histórico de orçamentos (aprovados, pendentes e recusados);
  - Histórico de ordens de serviço executadas e em andamento;
  - Histórico financeiro com lista de contas a receber vinculadas (`ObterContasReceberVinculadasAoCliente`);
  - Vendas de balcão vinculadas;
  - Botão de acionamento WhatsApp com validação prévia de consentimento LGPD (`ConsentimentoLGPD` e `AutorizaContatoWhatsApp`).
* **Design System:** Totalmente compatível com Light e Dark themes via recursos dinâmicos.
* **Status:** `CORE`

---

## 5. Vehicle 360 (Veículo 360)

* **Tela:** `Views/VisualizarVeiculoWindow.xaml`
* **Serviço Central:** `Primox360Service.cs` (`ObterVeiculo360(Guid veiculoId)`)
* **Dados Agregados em Tempo Real:**
  - Dados cadastrais do veículo (marca, modelo, ano, placa, renavam, chassi, quilometragem);
  - Ficha do proprietário atual;
  - Todas as OSs executadas no veículo ao longo da sua vida útil na oficina;
  - Peças já substituídas e serviços já prestados;
  - Prontuário elétrico específico do veículo (bateria, alternador, motor de partida, testes de tensão e fuga);
  - Galeria de anexos e fotos técnicas gerenciadas em disco.
* **Status:** `CORE`

---

## 6. DVI (Digital Vehicle Inspection)

* **Telas:** `Views/DviOrcamentoWindow.xaml`, aba de checklist em `Views/OrdemServicoWindow.xaml`
* **Serviço:** `DviChecklistService.cs`
* **Funcionamento:**
  - Permite marcar itens de entrada e saída como OK ou PENDENTE;
  - Registro de observações textuais por item inspecionado;
  - Anexação de fotos de avarias, quilometragem no painel e teste de bateria;
  - Espelhamento de `orc-{id}.json` para `os-{id}.json` quando o orçamento é aprovado e convertido em OS.
* **Lacuna Técnica:** O DVI não utiliza tabela relacional no SQLite (persiste como arquivo JSON estruturado em `AppData/Dvi/`). Não há portal web para que o cliente aprove o checklist pelo smartphone via link externo.
* **Status:** `PARTIAL`

---

## 7. Quotes (Orçamentos)

* **Telas:** `UserControls/OrcamentosControl.xaml`, `Views/NovoOrcamentoWindow.xaml`, `Views/OrcamentosView.xaml`
* **Serviço:** `OrcamentoDatabaseService.cs`, `OrcamentoAprovacaoService.cs`, `OrcamentoPdfService.cs`
* **Persistência:** Tabelas `Orcamentos` (25 registros) e `OrcamentoItens` (49 registros).
* **Cálculo Monetário:** Integrado à blindagem MoneyIO/CentsV1, cálculo de descontos em valor fixo ou percentual, margem de contribuição e subtotal por tipo de item (peça vs serviço).
* **Conversão em OS:** 100% automatizada através de `OrcamentosViewModel.ConverterEmOrdemServico`, sem redigitação de itens, preservando descontos, dados do cliente e veículo.
* **Lacuna Identificada:** Os botões `<Button Content="Email".../>` e `<Button Content="Converter em venda".../>` na janela `OrcamentosView.xaml` são puramente visuais (`UI_ONLY`).
* **Status Geral do Módulo:** `CORE`

---

## 8. Work Orders (Ordens de Serviço)

* **Telas:** `UserControls/OrdensServicoControl.xaml`, `Views/OrdemServicoWindow.xaml`, `UserControls/OficinaKanbanControl.xaml`
* **Serviços / Repositório:** `OrdemServicoRepository.cs`, `FinanceiroDatabaseService.cs`
* **Persistência:** Tabelas `OrdensServico` (9 linhas), `OrdemServicoItens` (16 linhas), `OrdemServicoEventos` (62 eventos gravados).
* **Integrações de Ciclo Fechado:**
  - *Estoque:* O status `Finalizada` ou `Entregue` debita automaticamente os produtos do estoque no banco SQLite em transação ACID.
  - *Financeiro:* O botão `Gerar financeiro` ou a finalização da OS gera o contas a receber correspondente com identificador da OS em `ReferenciaExterna`.
  - *Rastreabilidade:* A tabela `OrdemServicoEventos` audita cada avanço de status, inclusão de serviço ou cancelamento com carimbo de data/hora e operador autenticado.
* **Status:** `CORE`

---

## 9. Auto Electrical Technical (Autoelétrica Técnica)

* **Tela:** `UserControls/AutoEletricaTecnicaControl.xaml`
* **Serviços:** `AutoEletricaTecnicaService.cs`, `AutoEletricaRoteiroPersistService.cs`
* **Estrutura Existente:**
  - *Prontuário Elétrico Veicular:* REAL. A tabela `Veiculos` conta com 17 campos específicos para autoeletricidade (`BateriaPrincipal`, `BateriaMarca`, `BateriaAmperagem`, `BateriaDataInstalacao`, `TesteTensaoRepouso`, `TesteTensaoPartida`, `TesteCargaAlternador`, `CorrenteFuga`, `EstadoAterramentos`, `ChicotesReparados`, `FusiveisSubstituidos`, `RelesSubstituidos`, `LampadasSubstituidas`, etc.).
  - *Roteiros Guiados de Diagnóstico:* PARTIAL. Procedimentos técnicos padronizados (D01: Não dá partida, D02: Bateria descarregando, D03: Alternador não carrega, etc.) codificados em memória em C#.
  - *Persistência de Resultados:* PARTIAL. O arquivo `AppData/AutoEletrica/roteiros-resultados.json` armazena o resultado da medição indexado apenas pelo código do roteiro, sem vinculação com a chave primária da OS em execução.
* **Status:** `PARTIAL`

---

## 10. Technical History (Histórico Técnico)

* **Serviços:** `SintomaCausaHistoricoService.cs`, `Primox360Service.cs`
* **Funcionamento:** Cruza problemas relatados na abertura da OS, causas diagnosticadas pelo eletricista, medições elétricas, peças substituídas e laudo final de entrega por placa/VeiculoId.
* **Status:** `CORE`

---

## 11. Inventory (Estoque & Catálogo)

* **Telas:** `UserControls/EstoqueControl.xaml`, `UserControls/CatalogoPecasControl.xaml`, janelas de novo/ajuste/revisão.
* **Repositório:** `ProdutoRepository.cs`, `CatalogoPecasService.cs`
* **Base de Dados Existente:**
  - `Produtos`: 55 registros com estoque, preço de custo, preço de venda, código de barras, localização física e fornecedor principal.
  - `CatalogoPecas`: 4.287 autopeças catalogadas com referências originais e de fabricantes renomados do setor elétrico (DNI, IKRO, Bosch, etc.).
  - `CatalogoPecaVeiculos`: 51.119 relacionamentos estruturados de compatibilidade peça-veículo.
  - `CatalogoVeiculos`: 171 modelos de veículos base.
* **Status:** `CORE`

---

## 12. Finance (Financeiro & Caixa)

* **Telas:** `UserControls/FinanceiroControl.xaml`, `Views/OperacaoCaixaWindow.xaml`
* **Serviços:** `FinanceiroDatabaseService.cs`, `CaixaService.cs`
* **Tabelas:** `ContasPagar`, `ContasReceber`, `MovimentacoesFinanceiras`, `CaixaSessoes`, `MovimentacoesCaixa`.
* **Capacidade Operacional:**
  - Fluxo de caixa com segregação de receitas e despesas por categoria;
  - Contas a receber integrado a OS e Orçamentos com coluna `ClienteId`;
  - Abertura de caixa com suprimento inicial, sangria autorizada e fechamento cego;
  - Movimentações financeiras blindadas pela camada MoneyIO/CentsV1.
* **Risco Mapeado:** `ContasPagar` ainda não possui a coluna `FornecedorId` (associa por texto do nome do fornecedor).
* **Status:** `CORE`

---

## 13. Fiscal (Operações Fiscais)

* **Serviços:** `FiscalOperationsCenterService.cs`, `NFeImportacaoService.cs`, `DanfeInformationalPdfGenerator.cs`, `FiscalProductionGuard.cs`
* **Tabelas:** `FiscalDocuments`, `FiscalOperations`, `FiscalEvents`, `ImportacoesNFe`, `ImportacoesItens`.
* **Classificação por Sub-recurso:**
  - *Importação de XML de Fornecedor (Entrada):* `CORE` (converte itens da NFe em estoque, cadastra fornecedor).
  - *DANFE Informativo PDF:* `CORE` (geração local em PDF padrão).
  - *SEFAZ Homologação:* `PARTIAL` (adaptadores Focus/PlugNotas testados em sandbox).
  - *SEFAZ Produção:* `EXTERNAL_DEPENDENCY` (bloqueada via `FiscalProductionGuard` até contratação e liberação deliberada).
* **Status Consolidado:** `PARTIAL`

---

## 14. Scheduling (Agenda & Agendamentos)

* **Telas:** `UserControls/AgendamentosControl.xaml`, `Views/NovoAgendamentoPremiumWindow.xaml`
* **Serviço:** `AgendamentoDatabaseService.cs`
* **Tabelas:** `Agendamentos` (74 colunas), `AgendamentoProdutos`, `AgendamentoServicos`, `AgendamentoTimeline`.
* **Funcionalidades:** Grade de horários com verificação de conflitos de box/mecânico, reserva preventiva de peças e conversão de agendamento em Ordem de Serviço em 1 clique.
* **Status:** `CORE`

---

## 15. RBAC (Perfis e Permissões)

* **Telas:** `Views/ConfigurarPermissoesWindow.xaml`, `Views/GerenciarPerfisWindow.xaml`
* **Serviço:** `PermissionService.cs`
* **Tabelas no Banco:**
  - `PerfisAcesso`: 10 perfis cadastrados (`Administrador`, `Gerente`, `Mecanico`, `Mecânico`, `Vendedor`, `Caixa`, `Estoquista`, `Almoxarife`, `Financeiro`, `Tecnico`).
  - `Permissoes`: 82 permissões atômicas cadastradas cobrindo todos os módulos do sistema.
  - `PerfilPermissoes`: 276 vínculos ativos entre perfis e permissões.
* **Observação:** Duplicidade cadastral identificada entre o perfil ID 3 (`Mecanico`) e o perfil ID 4 (`Mecânico` com diacrítico) a ser unificada na Trilha B.
* **Status:** `CORE`

---

## 16. Backup / Restore

* **Telas:** `Views/BackupSettingsWindow.xaml`, `Views/ConfiguracoesSistemaWindow.xaml`
* **Serviço:** `DatabaseBackupService.cs`
* **Recursos de Segurança:**
  - Backup automático diário (timer de 24h);
  - Backup manual com gravação de histórico na tabela `DatabaseBackups`;
  - Verificação rigorosa de integridade e checksum SHA-256;
  - Proteção contra path traversal (`EnforceRestorePathJail`);
  - Criação mandatória de backup de segurança (`pre_restore`) antes de qualquer substituição de arquivo;
  - Limpeza forçada de pool SQLite (`SqliteConnection.ClearAllPools()`) e expurgo de arquivos temporários WAL e SHM.
* **Status:** `CORE`

---

## 17. Licensing (Licenciamento)

* **Tela:** `Views/LicenseActivationWindow.xaml`
* **Serviço:** `LicenseService.cs`
* **Realidade do Código:** A classe declara publicamente: `public const bool IsCommercialScaffoldOnly = true;`. A validação é baseada em arquivo JSON local e hash SHA-256 de identificadores do hardware (`MachineName`, `UserName`). Não existe infraestrutura de chaves assimétricas públicas/privadas, nem comunicação com servidor SaaS online para revogação remota.
* **Status:** `MOCK` (Licenciamento comercial remoto é `NOT_IMPLEMENTED`).

---

## 18. Audit (Trilha de Auditoria & Logs)

* **Serviço:** `AuditLogService.cs` (`App.Audit`), `LoggerService.cs` (`App.Logger`)
* **Tabela:** `AuditLogs` com 17 colunas e 1.592 registros gravados na base auditada.
* **Cobertura:** Logins com sucesso/falha, tentativas suspeitas, abertura de menus, exclusões de registros, geração de financeiros, alterações de preços e backups restaurados.
* **Status:** `CORE`

---

## 19. API REST

* **Projeto:** `PrimoAutoEletrica.Api` (Minimal API ASP.NET Core)
* **Segurança:** Autenticação JWT Bearer com SigningKey >= 32 chars, validação estrita de tokens em ambiente de produção, Rate Limiting de 120 req/min e políticas baseadas em Claims.
* **Endpoints Existentes:**
  - `GET /api/health` (público com rate limit)
  - `POST /api/auth/token` (geração de JWT)
  - `GET /api/orcamentos`, `GET /api/orcamentos/{id}`, `POST /api/orcamentos`, `PUT /api/orcamentos/{id}`, `DELETE /api/orcamentos/{id}`, `GET /api/orcamentos/{id}/itens`
  - `GET /api/estoque/produtos`, `GET /api/estoque/produtos/{id}`
  - `GET /api/financeiro/resumo/{inicio}/{fim}`, `POST /api/financeiro/orcamento`
* **Status:** `CORE` para o escopo coberto (rotas de OS, Clientes e Veículos via REST são `NOT_IMPLEMENTED`).

---

## 20. External Integrations (Integrações Externas)

* **WhatsApp:** `PARTIAL` (disparo via deep-link `wa.me` com higienização de telefone e verificação de consentimento LGPD; sem WhatsApp Business Cloud API direta).
* **Fiscal SEFAZ:** `PARTIAL` / `EXTERNAL_DEPENDENCY` (Homologação ativa via Focus/PlugNotas; produção com trava de segurança).
* **TEF Maquininhas de Cartão:** `NOT_IMPLEMENTED`.
* **Gateways de Pagamento (Pix / Boleto online automático):** `NOT_IMPLEMENTED`.
* **Email / SMTP:** `NOT_IMPLEMENTED`.
* **SMS Transacional:** `NOT_IMPLEMENTED`.
* **Sincronização em Nuvem / Portal da Oficina:** `NOT_IMPLEMENTED`.
* **Status Consolidado:** `PARTIAL`

---

## 21. UI / Design System

* **Arquitetura de Temas:** 25 arquivos de tema e dicionários de recursos em `PrimoAutoEletrica/Themes/`.
* **Temas Suportados:** Modo Claro (`Themes/Colors.Light.xaml`) e Modo Escuro (`Themes/Colors.Dark.xaml`).
* **Qualidade Visual:**
  - 53 Views utilizam quase em sua totalidade referências dinâmicas (`DynamicResource`), garantindo contraste apropriado e eliminação de bordas brancas no modo escuro;
  - Tipografia moderna padronizada (`Themes/Typography.xaml`);
  - Botões, caixas de texto, cartões, cabeçalhos e modais com estilos unificados (`ModalAccentButton`, `ModalSecondaryButton`, `DataGridCellStyle`, etc.);
  - Apenas 15 ocorrências de cores hexadecimais hardcoded foram encontradas em 5 janelas utilitárias secundárias (`LoginWindow`, `PrimeiraExecucaoWindow`, `ConfigurarPermissoesWindow`, `AssinaturaDigitalWindow`, `AjusteEstoqueWindow`).
* **Status:** `CORE`

---

## 22. Testing (Inventário de Testes)

* **Projeto Principal:** `Tests/PrimoAutoEletrica.Tests`
* **Resultados da Última Execução:**
  - Total de testes executados: **383**
  - **Aprovados (PASS): 383 (100%)**
  - **Com falha (FAIL): 0**
  - **Ignorados (SKIP): 0**
  - Tempo de execução: ~17 segundos
* **Áreas Cobertas:**
  - Repositórios e Serviços: Clientes, Fornecedores, Funcionários, Produtos, Ordem de Serviço, Vendas, Agendamentos, Auditoria;
  - Domínio Financeiro & MoneyIO: Integridade de centavos INTEGER, arredondamentos bancários, I/O hardening, compatibilidade com CentsV1 e Legado;
  - Workflow Operacional: Conversão de orçamento para OS, baixa de estoque, geração de contas a receber;
  - Segurança e Autenticação: 2FA TOTP, RBAC, hash de senha, validação de tokens JWT;
  - Fiscal: Validação de NFe, gerador de DANFE informativo e adapters.
* **Testes de UI (`PrimoAutoEletrica.UiTests`):** 7 falhas pontuais ocasionadas por incompatibilidade de path (`UiTestBase.cs` buscando o antigo build `net6.0`).
* **Status:** `CORE` para suíte de testes funcionais/unitários/integração.

---

## 23. Mock/Fake Audit (Auditoria de Código Simulado)

Foram identificadas 54 ocorrências de termos como `fake`, `TODO`, `placeholder` ou `NotImplementedException`:
1. **Fiscal Fake Provider:** `Testing/FakeFiscalProvider.cs` e `FiscalSecurityAbstractions.cs` — utilizados exclusivamente pela suíte de testes unitários para simular respostas da SEFAZ sem chamadas de rede externas.
2. **TODOs Documentais:**
   - `ContabilExportService.cs:120`: lembrete para implementação de formato SPED completo;
   - `FinanceiroDatabaseService.cs:1785`: aviso explícito para não utilizar buscas por nome para métricas 360;
   - `AccessibilityChromeHealer.cs:157`: traduções adicionais de acessibilidade.
3. **Placeholders:** `UpdateService.cs:233`: `await Task.Delay(100)`.
4. **NotImplementedException:** `LocalizationConverter.cs:27` no método `ConvertBack`, padrão comum em WPF para bindings one-way.
5. **Nenhum mock ou fake está mascarando operações financeiras ou de estoque na aplicação real.**

---

## 24. Persistence (Auditoria de Banco & Integridade)

* **Motor:** SQLite 3 (com suporte estruturado para SQL Server via `DatabaseService`).
* **Total de Tabelas Mapeadas:** 58 tabelas.
* **Principais Tabelas com Dados Ativos:**
  - `CatalogoPecas` (4.287 linhas), `CatalogoPecaVeiculos` (51.119 linhas), `CatalogoVeiculos` (171 linhas);
  - `AuditLogs` (1.592 registros críticos);
  - `Produtos` (55 registros);
  - `OrcamentoItens` (49 registros), `Orcamentos` (25 registros);
  - `OrdensServico` (9 registros), `OrdemServicoItens` (16 registros), `OrdemServicoEventos` (62 registros);
  - `Veiculos` (7 registros), `Clientes` (1 registro), `Funcionarios` (1 registro);
  - `DatabaseBackups` (38 registros);
  - `PerfilPermissoes` (276 registros), `Permissoes` (82 registros), `PerfisAcesso` (10 registros).
* **Tabelas Residuais Identificadas:**
  - `Auditoria` (0 linhas) — substituída por `AuditLogs`;
  - `RegistroBloqueios` (0 linhas) — substituída por `RecordLocks`.
* **Status da Proteção:** O banco oficial em `%LOCALAPPDATA%\PrimoAutoEletrica\primoauto.db` permaneceu intocado, sem nenhuma migration ou alter table aplicada.

---

## 25. Documentation (Conformidade Documental)

A documentação prévia da pasta `Docs/` foi confrontada diretamente com os fontes em C# e o banco SQLite:
- As documentações das Fases 2, 2.1, 2.2, 2.3 e 2.4 descrevem com precisão a blindagem da camada de dinheiro (MoneyIO / CentsV1).
- As divergências encontradas entre documentação legada e o código atual (ex: existência de licenciamento comercial e integração completa de TEF) foram devidamente reclassificadas como `MOCK` e `NOT_IMPLEMENTED`.

---

## 26. Top Product Gaps

As 20 principais lacunas técnicas e funcionais identificadas encontram-se descritas e priorizadas em:
👉 [`Docs/audit/2026-09-20/B1_TOP20_PRODUCT_GAPS.md`](file:///c:/Projetos/PrimoAutoEletrica/Docs/audit/2026-09-20/B1_TOP20_PRODUCT_GAPS.md)

---

## 27. Recommended Next Implementation (Recomendação Pós-B1)

Com base nas evidências consolidadas na Matriz de Discovery e no Mapa 360, a próxima etapa da Trilha B (**B2**) NÃO deve ser escolhida de forma aleatória.

**Recomenda-se para a Fase B2 focar em:**
1. **Autoelétrica Técnica (GAP 03 e 04):** Conectar os roteiros de diagnóstico elétrico (bateria, alternador, motor de partida e fuga de corrente) diretamente à `OrdemServicoId` e `VeiculoId`, transformando o módulo estratégico em `CORE` completo e pericial.
2. **Harmonização do DVI (GAP 05):** Estruturar a persistência do DVI no banco de dados SQLite para unificar com o ecossistema relacional da OS.
3. **Higienização de IDs e Cadastros (GAP 06 e 07):** Incluir `FornecedorId` em `ContasPagar` e unificar a duplicidade de perfil de acesso (`Mecanico` vs `Mecânico`).

---

## 28. Evidence (Evidências Auditadas)

1. **Compilação e Testes:**
   - `dotnet build --configuration Release`: 0 Erros, 80 Avisos benignos de plataforma Windows.
   - `dotnet test`: 383 Aprovados, 0 Falhas, 0 Ignorados em `PrimoAutoEletrica.Tests`.
2. **Hash Criptográfico do Banco de Dados Real:**
   - Caminho: `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db`
   - Algoritmo: SHA-256
   - Valor: `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B`
   - Atributo: `IsReadOnly = True`
3. **Arquivos Entregues:**
   - [`Docs/audit/2026-09-20/B1_PRODUCT_INVENTORY.md`](file:///c:/Projetos/PrimoAutoEletrica/Docs/audit/2026-09-20/B1_PRODUCT_INVENTORY.md)
   - [`Docs/audit/2026-09-20/B1_PRODUCT_DISCOVERY_MATRIX.csv`](file:///c:/Projetos/PrimoAutoEletrica/Docs/audit/2026-09-20/B1_PRODUCT_DISCOVERY_MATRIX.csv)
   - [`Docs/audit/2026-09-20/B1_360_WORKFLOW_MAP.md`](file:///c:/Projetos/PrimoAutoEletrica/Docs/audit/2026-09-20/B1_360_WORKFLOW_MAP.md)
   - [`Docs/audit/2026-09-20/B1_TOP20_PRODUCT_GAPS.md`](file:///c:/Projetos/PrimoAutoEletrica/Docs/audit/2026-09-20/B1_TOP20_PRODUCT_GAPS.md)
   - [`Docs/audit/2026-09-20/B1_PRODUCT_DISCOVERY_FINAL.md`](file:///c:/Projetos/PrimoAutoEletrica/Docs/audit/2026-09-20/B1_PRODUCT_DISCOVERY_FINAL.md)

---
*Fim do Relatório de Auditoria e Discovery B1 — PRIMOX Workshop.*
