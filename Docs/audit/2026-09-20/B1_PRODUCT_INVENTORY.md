# PRIMOX WORKSHOP — INVENTÁRIO DO PRODUTO (FASE B1)
**Data de Auditoria:** 2026-09-20 / 2026-09-23  
**Branch de Auditoria:** `audit/product-discovery-2026-09`  
**Regra Fundamental:** Toda informação baseada em código real, XAML, C#, banco SQLite, testes e evidências concretas. Proibido inventar componentes ou mascarar lacunas.

---

## 1. Resumo Quantitativo do Inventário
* **Telas / Janelas (Views):** 53 arquivos XAML em `PrimoAutoEletrica/Views/`
* **Controles Modulares (UserControls):** 28 controles XAML em `PrimoAutoEletrica/UserControls/`
* **ViewModels:** 22 classes em `PrimoAutoEletrica/ViewModels/`
* **Serviços de Domínio/Infra:** 203 classes em `PrimoAutoEletrica/Services/`
* **Repositórios de Dados:** 13 arquivos em `PrimoAutoEletrica/Repositories/`
* **Tabelas no Banco SQLite Real:** 58 tabelas mapeadas
* **Endpoints REST (Minimal API):** 12 rotas em `PrimoAutoEletrica.Api/Program.cs`
* **Testes Automatizados de Regressão:** 383 testes PASS (0 FAIL, 0 SKIP) em `PrimoAutoEletrica.Tests`
* **Banco Real de Produção:** `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db` (SHA-256: `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B`, `user_version = 0`, estritamente intacto e protegido como Read-Only).

---

## 2. Inventário Detalhado por Módulo

### Módulo 1: DASHBOARD
* **Nome:** Dashboard Operacional e Executivo
* **Tela:** `UserControls/DashboardControl.xaml`, `Views/OrcamentosDashboardControl.xaml`
* **ViewModel:** `DashboardViewModel`
* **Service:** `DatabaseService`, `RelatorioService`
* **Repository:** `NOT FOUND` (acesso direto via DatabaseService)
* **Tabela(s):** `Orcamentos`, `OrdensServico`, `MovimentacoesFinanceiras`, `Clientes`, `Produtos`
* **Persistência:** REAL (agregação em tempo de execução via SQLite)
* **Testes:** PASS (`PrimoAutoEletrica.Tests`)
* **UI Test:** PASS (verificado em smoke tests de UI)
* **Integração:** REAL (integra métricas de ordens de serviço, orçamentos pendentes, fluxo diário e alertas)
* **Light:** PASS (suporte completo via tokens dinâmicos)
* **Dark:** PASS (suporte completo via `Themes/Colors.Dark.xaml`)
* **Permissões:** `DASHBOARD_VER` (código RBAC)
* **Status:** `CORE`

---

### Módulo 2: CLIENTES
* **Nome:** Gestão de Clientes e Contatos
* **Tela:** `UserControls/ClientesControl.xaml`, `Views/Clientes/NovoClienteWindow.xaml`, `Views/Clientes/EditarClienteWindow.xaml`, `Views/Clientes/VisualizarClienteWindow.xaml`
* **ViewModel:** `ClientesViewModel`, `ClienteListItemViewModel`
* **Service:** `CustomerService`, `ClienteMediaService`
* **Repository:** `ClienteRepository` (`IClienteRepository`)
* **Tabela(s):** `Clientes` (36 colunas)
* **Persistência:** REAL (`Clientes` table, SQLite INTEGER Id autoincrement)
* **Testes:** PASS (`ClienteRepositoryTests`, `CustomerServiceTests`, 383 testes verdes)
* **UI Test:** PASS (`CrudTests.Cliente_Criar_Editar_Excluir`)
* **Integração:** REAL (vinculado a Veículos, Orçamentos, OS, Contas a Receber e Vendas)
* **Light:** PASS
* **Dark:** PASS
* **Permissões:** `CLIENTES_VER`, `CLIENTES_CRIAR`, `CLIENTES_EDITAR`, `CLIENTES_EXCLUIR`
* **Status:** `CORE`

---

### Módulo 3: VEÍCULOS
* **Nome:** Gestão de Veículos da Oficina
* **Tela:** `UserControls/VeiculosControl.xaml`, `Views/NovoVeiculoWindow.xaml`, `Views/VisualizarVeiculoWindow.xaml`
* **ViewModel:** `VeiculosViewModel`, `VeiculoViewModel`
* **Service:** `VeiculoProfileService`, `VeiculoMediaService`, `AutoEletricaTecnicaService`
* **Repository:** `ClienteRepository` (`ObterTodosVeiculos`, `ObterVeiculosPorClienteId`)
* **Tabela(s):** `Veiculos` (49 colunas, incluindo telemetria elétrica)
* **Persistência:** REAL (`Veiculos` table, chave `Id`, FK `ClienteId`)
* **Testes:** PASS
* **UI Test:** PASS
* **Integração:** REAL (vinculado ao Proprietário/Cliente, Ordens de Serviço, Orçamentos e Agendamentos)
* **Light:** PASS
* **Dark:** PASS
* **Permissões:** `VEICULOS_VER`, `VEICULOS_CRIAR`, `VEICULOS_EDITAR`, `VEICULOS_EXCLUIR`
* **Status:** `CORE`

---

### Módulo 4: CLIENTE 360
* **Nome:** Visão 360° do Cliente
* **Tela:** `Views/HistoricoClienteWindow.xaml`
* **ViewModel:** `ClientesViewModel` (orquestrado via codebehind de `HistoricoClienteWindow`)
* **Service:** `Primox360Service` (`IPrimox360Service`), `CommercialDocumentActions`
* **Repository:** `ClienteRepository`, `OrdemServicoRepository`, `VendaRepository`
* **Tabela(s):** `Clientes`, `Veiculos`, `Orcamentos`, `OrdensServico`, `ContasReceber`, `Vendas`
* **Persistência:** REAL (agregação puramente por chaves primárias `ClienteId`, sem TEXT_MATCH frágil para financeiro)
* **Testes:** PASS (`Primox360ServiceTests`, `Primox360IdFinancialJoinTests`)
* **UI Test:** PASS
* **Integração:** REAL (exibe frota do cliente, histórico de orçamentos, ordens de serviço, notas de venda, contas a receber pendentes/pagas e botão para WhatsApp direto)
* **Light:** PASS
* **Dark:** PASS
* **Permissões:** `CLIENTES_HISTORICO`, `FINANCEIRO_VER`
* **Status:** `CORE`

---

### Módulo 5: VEHICLE 360 (VEÍCULO 360)
* **Nome:** Visão 360° do Veículo
* **Tela:** `Views/VisualizarVeiculoWindow.xaml`
* **ViewModel:** `VeiculosViewModel`
* **Service:** `Primox360Service`, `VeiculoProfileService`, `AutoEletricaTecnicaService`
* **Repository:** `ClienteRepository`, `OrdemServicoRepository`
* **Tabela(s):** `Veiculos`, `OrdensServico`, `Orcamentos`, `Clientes`
* **Persistência:** REAL (agregação por `VeiculoId` Guid)
* **Testes:** PASS (`Primox360ServiceTests`)
* **UI Test:** PASS
* **Integração:** REAL (apresenta dados do proprietário, histórico de OSs, orçamentos, prontuário técnico de bateria/alternador/motor de partida e galeria de fotos geridas)
* **Light:** PASS
* **Dark:** PASS
* **Permissões:** `VEICULOS_VER`, `ORDENS_VER`
* **Status:** `CORE`

---

### Módulo 6: ORÇAMENTOS
* **Nome:** Orçamentos Comerciais & Técnicos
* **Tela:** `UserControls/OrcamentosControl.xaml`, `Views/NovoOrcamentoWindow.xaml`, `Views/OrcamentosView.xaml`, `Views/SelecionarOrcamentoWindow.xaml`
* **ViewModel:** `OrcamentosViewModel`
* **Service:** `OrcamentoDatabaseService`, `OrcamentoAprovacaoService`, `OrcamentoPdfService`
* **Repository:** `NOT FOUND` (encapsulado em `OrcamentoDatabaseService`)
* **Tabela(s):** `Orcamentos` (29 colunas), `OrcamentoItens` (18 colunas)
* **Persistência:** REAL (`Orcamentos` e `OrcamentoItens` em SQLite)
* **Testes:** PASS (`OrcamentoDatabaseServiceTests`, `OrcamentoStatusNormalizerTests`)
* **UI Test:** PASS
* **Integração:** REAL (vinculado a ClienteId, VeiculoId, produtos do estoque; gera PDF oficial e link wa.me; converte com 1 clique para OS)
* **Light:** PASS
* **Dark:** PASS
* **Permissões:** `ORCAMENTO_VER`, `ORCAMENTO_CRIAR`, `ORCAMENTO_EDITAR`, `ORCAMENTO_EXCLUIR`, `ORCAMENTO_APROVAR`
* **Status:** `CORE`

---

### Módulo 7: DVI (DIGITAL VEHICLE INSPECTION)
* **Nome:** Inspeção Veicular Digital / Checklist de Entrada e Saída
* **Tela:** `Views/DviOrcamentoWindow.xaml`, aba de inspeção em `Views/OrdemServicoWindow.xaml`
* **ViewModel:** `NOT FOUND` (controlado diretamente por `DviChecklistService` e codebehind)
* **Service:** `DviChecklistService`, `OficinaProfissionalService`
* **Repository:** `NOT FOUND`
* **Tabela(s):** `NOT FOUND` (não utiliza tabela relacional no SQLite; persiste como arquivos JSON estruturados `os-{id}.json` e `orc-{id}.json` em `%LOCALAPPDATA%\PrimoAutoEletrica\Dvi\`)
* **Persistência:** PARTIAL (persiste em arquivos locais JSON; mantém histórico de fotos e conformidade, mas não em tabela relacional)
* **Testes:** PASS (`UiSmokeTestService.Dvi.cs`)
* **UI Test:** PASS
* **Integração:** REAL (espelha checklist entre Orçamento e Ordem de Serviço na conversão; anexa fotos reais de avarias e bateria)
* **Light:** PASS
* **Dark:** PASS
* **Permissões:** `ORDENS_EDITAR`
* **Status:** `PARTIAL` (completo localmente em JSON, sem portal web remoto de aprovação do cliente)

---

### Módulo 8: ORDENS DE SERVIÇO (OS)
* **Nome:** Gestão de Ordens de Serviço da Oficina
* **Tela:** `UserControls/OrdensServicoControl.xaml`, `Views/OrdemServicoWindow.xaml`, `UserControls/OficinaKanbanControl.xaml`
* **ViewModel:** `OrdensServicoViewModel`, `OrdemServicoPainelItemViewModel`
* **Service:** `DatabaseService`, `FinanceiroDatabaseService`, `OrdemServicoMediaService`
* **Repository:** `OrdemServicoRepository` (`IOrdemServicoRepository`)
* **Tabela(s):** `OrdensServico` (47 colunas), `OrdemServicoItens` (11 colunas), `OrdemServicoEventos` (7 colunas)
* **Persistência:** REAL (`OrdensServico`, `OrdemServicoItens`, `OrdemServicoEventos` em SQLite)
* **Testes:** PASS (`OrdemServicoRepositoryTests`, `OperationalWorkflowTestService`)
* **UI Test:** PASS
* **Integração:** REAL (baixa automática de estoque na finalização, geração automática de Conta a Receber no Financeiro, log de eventos com usuário logado e timeline de execução)
* **Light:** PASS
* **Dark:** PASS
* **Permissões:** `ORDENS_VER`, `ORDENS_CRIAR`, `ORDENS_EDITAR`, `ORDENS_CANCELAR`, `ORDENS_FINALIZAR`
* **Status:** `CORE`

---

### Módulo 9: AUTOELÉTRICA TÉCNICA
* **Nome:** Prontuário Elétrico e Diagnóstico Especializado
* **Tela:** `UserControls/AutoEletricaTecnicaControl.xaml`, `Views/VisualizarVeiculoWindow.xaml`
* **ViewModel:** `AutoEletricaTecnicaViewModel`
* **Service:** `AutoEletricaTecnicaService`, `AutoEletricaRoteiroPersistService`, `SintomaCausaHistoricoService`
* **Repository:** `ClienteRepository` (para leitura de dados de `Veiculos`)
* **Tabela(s):** `Veiculos` (campos: `SistemaEletrico`, `BateriaPrincipal`, `BateriaMarca`, `BateriaAmperagem`, `BateriaDataInstalacao`, `TesteTensaoRepouso`, `TesteTensaoPartida`, `TesteCargaAlternador`, `CorrenteFuga`, `EstadoAterramentos`, `ChicotesReparados`, `FusiveisSubstituidos`, `RelesSubstituidos`, `LampadasSubstituidas`, `AcessoriosInstalados`, `ObservacoesTecnicasEletricas`, `HistoricoTecnico`)
* **Persistência:** PARTIAL (os dados elétricos do veículo são persistidos na tabela `Veiculos`; os roteiros de diagnóstico guiado são mantidos em catálogo em memória e seus resultados salvos em arquivo JSON global `roteiros-resultados.json` sem chave de OS/Veículo)
* **Testes:** PASS (`UiSmokeTestService.AutoEletrica.cs`)
* **UI Test:** PASS
* **Integração:** PARTIAL (lê veículos da base real, mas a execução de roteiros ainda não vincula o resultado gravado à chave primária da OS em execução)
* **Light:** PASS
* **Dark:** PASS
* **Permissões:** `TECNICA_VER`, `TECNICA_EDITAR`
* **Status:** `PARTIAL`

---

### Módulo 10: HISTÓRICO TÉCNICO
* **Nome:** Histórico Estruturado de Defeitos e Serviços por Veículo
* **Tela:** `UserControls/AutoEletricaTecnicaControl.xaml`, `Views/VisualizarVeiculoWindow.xaml`
* **ViewModel:** `AutoEletricaTecnicaViewModel`, `VeiculosViewModel`
* **Service:** `SintomaCausaHistoricoService`, `Primox360Service`
* **Repository:** `OrdemServicoRepository`
* **Tabela(s):** `OrdensServico`, `OrdemServicoItens`
* **Persistência:** REAL (rastreável através das OSs executadas pelo veículo, itens trocados, problemas relatados e diagnósticos finais)
* **Testes:** PASS
* **UI Test:** PASS
* **Integração:** REAL (agrega automaticamente todas as intervenções anteriores do veículo)
* **Light:** PASS
* **Dark:** PASS
* **Permissões:** `VEICULOS_VER`, `ORDENS_VER`
* **Status:** `CORE`

---

### Módulo 11: ESTOQUE E PRODUTOS
* **Nome:** Gestão de Produtos, Peças e Movimentação de Estoque
* **Tela:** `UserControls/EstoqueControl.xaml`, `Views/NovoProdutoWindow.xaml`, `Views/EditarProdutoWindow.xaml`, `Views/AjusteEstoqueWindow.xaml`, `Views/HistoricoEstoqueWindow.xaml`, `Views/Produto360Window.xaml`
* **ViewModel:** `NOT FOUND` (orquestrado via `EstoqueOperationalService` e codebehinds das janelas)
* **Service:** `EstoqueOperationalService`, `ProdutoEtiquetaService`
* **Repository:** `ProdutoRepository` (`IProdutoRepository`)
* **Tabela(s):** `Produtos` (53 colunas, 55 registros na base de homologação)
* **Persistência:** REAL (`Produtos` table em SQLite)
* **Testes:** PASS (`ProdutoRepositoryTests`, `EstoqueOperationalServiceTests`)
* **UI Test:** PASS
* **Integração:** REAL (baixa por venda no balcão PDV, baixa na finalização de OS, ajuste com justificativa e histórico)
* **Light:** PASS
* **Dark:** PASS
* **Permissões:** `ESTOQUE_VER`, `ESTOQUE_CRIAR`, `ESTOQUE_EDITAR`, `ESTOQUE_AJUSTAR`, `ESTOQUE_EXCLUIR`
* **Status:** `CORE`

---

### Módulo 12: CATÁLOGO MASTER DE PEÇAS & APLICAÇÕES
* **Nome:** Catálogo Técnico de Peças Automotivas & Fabricantes
* **Tela:** `UserControls/CatalogoPecasControl.xaml`, `Views/RevisarCatalogoPecaWindow.xaml`, `Views/ImportarCatalogoPecasWindow.xaml`
* **ViewModel:** `CatalogoPecasViewModel`
* **Service:** `CatalogoPecasService`, `CatalogoImportacaoService`
* **Repository:** `NOT FOUND` (acesso direto via SQLite commands em `CatalogoPecasService`)
* **Tabela(s):** `CatalogoPecas` (4.287 linhas), `CatalogoPecaVeiculos` (51.119 linhas de relação peça-veículo), `CatalogoVeiculos` (171 veículos base)
* **Persistência:** REAL (`CatalogoPecas`, `CatalogoPecaVeiculos`, `CatalogoVeiculos`)
* **Testes:** PASS (`CatalogoImportacaoServiceTests`)
* **UI Test:** PASS
* **Integração:** REAL (pesquisa peças por código original/fabricante/DNI/IKRO, busca por montadora/modelo/ano, exporta ou converte peça do catálogo para produto do estoque com 1 clique)
* **Light:** PASS
* **Dark:** PASS
* **Permissões:** `ESTOQUE_VER`, `ESTOQUE_CRIAR`
* **Status:** `CORE`

---

### Módulo 13: FINANCEIRO (CONTAS A PAGAR, RECEBER E FLUXO)
* **Nome:** Módulo Financeiro Geral
* **Tela:** `UserControls/FinanceiroControl.xaml`
* **ViewModel:** `FinanceiroViewModel`
* **Service:** `FinanceiroDatabaseService`, `RelatorioService`
* **Repository:** `NOT FOUND` (encapsulado em `FinanceiroDatabaseService`)
* **Tabela(s):** `ContasPagar` (12 cols), `ContasReceber` (13 cols), `MovimentacoesFinanceiras` (12 cols)
* **Persistência:** REAL (SQLite)
* **Testes:** PASS (`FinanceiroDatabaseServiceTests`, `MoneyRepositoryIoTests`, `MoneyPreProductionGateTests`)
* **UI Test:** PASS
* **Integração:** REAL (recebe receitas automáticas de OS e Vendas; permite lançamento manual de despesas, conciliação e baixa)
* **Light:** PASS
* **Dark:** PASS
* **Permissões:** `FINANCEIRO_VER`, `FINANCEIRO_CRIAR`, `FINANCEIRO_EDITAR`, `FINANCEIRO_BAIXAR`
* **Status:** `CORE`

---

### Módulo 14: CAIXA / FRENTE DE CAIXA
* **Nome:** Controle de Sessões e Movimentações de Caixa
* **Tela:** `Views/OperacaoCaixaWindow.xaml`
* **ViewModel:** `PDVViewModel`
* **Service:** `CaixaService`
* **Repository:** `NOT FOUND` (encapsulado em `CaixaService`)
* **Tabela(s):** `CaixaSessoes` (18 cols), `MovimentacoesCaixa` (14 cols)
* **Persistência:** REAL (schema SQLite operacional)
* **Testes:** PASS (`CaixaServiceTests`)
* **UI Test:** PASS
* **Integração:** REAL (suporta abertura de caixa com saldo inicial, sangria, suprimento, fechamento cego e conferência de valores por forma de pagamento)
* **Light:** PASS
* **Dark:** PASS
* **Permissões:** `CAIXA_OPERAR`, `CAIXA_SANGRIA`, `CAIXA_FECHAR`
* **Status:** `CORE`

---

### Módulo 15: VENDAS / BALCÃO (PDV)
* **Nome:** Ponto de Venda (PDV Balcão)
* **Tela:** `UserControls/PDVControl.xaml`, `Views/PagamentoMistoWindow.xaml`, `Views/SelecionarClientePDVWindow.xaml`, `Views/SelecionarProdutoPDVWindow.xaml`, `Views/SelecionarVendaWindow.xaml`
* **ViewModel:** `PDVViewModel`
* **Service:** `VendaRepository`, `CaixaService`, `PrinterDiagnosticsService`
* **Repository:** `VendaRepository`
* **Tabela(s):** `Vendas` (14 cols), `VendaItens` (11 cols)
* **Persistência:** REAL (`Vendas` e `VendaItens` em SQLite)
* **Testes:** PASS (`VendaRepositoryTests`)
* **UI Test:** PASS
* **Integração:** REAL (baixa produtos do estoque em tempo real, gera movimentação no caixa atual, emite comprovante/recibo não fiscal)
* **Light:** PASS
* **Dark:** PASS
* **Permissões:** `VENDAS_OPERAR`
* **Status:** `CORE`

---

### Módulo 16: FISCAL (NF-E, NFC-E, NFS-E, XML, DANFE)
* **Nome:** Operações Fiscais e Emissão Eletrônica
* **Tela:** `UserControls/FiscalOperationsControl.xaml`, `UserControls/ImportarNFeControl.xaml`, `Views/ImportarNotaWindow.xaml`
* **ViewModel:** `NOT FOUND` (controlado por `FiscalOperationsCenterService` e `NFeImportacaoService`)
* **Service:** `FiscalOperationsCenterService`, `NFeImportacaoService`, `FiscalApplicationService`, `NFeHomologationService`, `DanfeInformationalPdfGenerator`, `FocusNfeProvider`, `PlugNotasProvider`
* **Repository:** `FiscalOperationStore`, `FiscalEmpresaStore`
* **Tabela(s):** `FiscalDocuments`, `FiscalEmpresas`, `FiscalEvents`, `FiscalOperations`, `ImportacoesNFe`, `ImportacoesItens`
* **Persistência:** REAL (`FiscalDocuments`, `ImportacoesNFe`, etc.)
* **Testes:** PASS (`FiscalSecurityAbstractions`, `DanfePdfTests`, `FiscalValidationTests`)
* **UI Test:** PASS
* **Integração:**
  - *Importação de XML de entrada:* REAL (`CORE`). Lê XML do fornecedor, cadastra/atualiza fornecedor, reconcilia produtos e dá entrada no estoque.
  - *Geração de DANFE informativo:* REAL (`CORE`).
  - *Emissão SEFAZ em Homologação:* PARTIAL (adaptadores Focus/PlugNotas presentes e testados com fake/sandbox).
  - *Emissão SEFAZ em Produção:* EXTERNAL_DEPENDENCY (bloqueada via `FiscalProductionGuard.cs` até liberação contratual).
* **Light:** PASS
* **Dark:** PASS
* **Permissões:** `FISCAL_VER`, `FISCAL_EMITIR`, `FISCAL_CONFIGURAR`
* **Status:** `PARTIAL` (Importação XML e DANFE são CORE; emissão SEFAZ Produção é EXTERNAL_DEPENDENCY)

---

### Módulo 17: AGENDA / OFICINA TIMELINE
* **Nome:** Agenda de Serviços e Agendamentos Premium
* **Tela:** `UserControls/AgendamentosControl.xaml`, `Views/NovoAgendamentoPremiumWindow.xaml`
* **ViewModel:** `AgendamentosViewModel`, `NovoAgendamentoPremiumViewModel`
* **Service:** `AgendamentoDatabaseService`, `ShellNotificationService`
* **Repository:** `NOT FOUND` (encapsulado em `AgendamentoDatabaseService`)
* **Tabela(s):** `Agendamentos` (74 colunas), `AgendamentoProdutos`, `AgendamentoServicos`, `AgendamentoTimeline`
* **Persistência:** REAL (SQLite)
* **Testes:** PASS (`AgendamentoDatabaseServiceTests`, `OperationalWorkflowTestService`)
* **UI Test:** PASS
* **Integração:** REAL (reserva de peças no estoque, timeline com detecção de conflitos mecânico/box, conversão direta de Agendamento em OS sem redigitação)
* **Light:** PASS
* **Dark:** PASS
* **Permissões:** `AGENDA_VER`, `AGENDA_CRIAR`, `AGENDA_EDITAR`, `AGENDA_CANCELAR`
* **Status:** `CORE`

---

### Módulo 18: FUNCIONÁRIOS / RH & SEGURANÇA 2FA
* **Nome:** Gestão de Usuários, Funcionários e Autenticação Forte
* **Tela:** `UserControls/FuncionariosControl.xaml`, `Views/NovoFuncionarioWindow.xaml`, `Views/EditarFuncionarioWindow.xaml`, `Views/TwoFactorSetupWindow.xaml`, `Views/TrocarSenhaObrigatoriaWindow.xaml`
* **ViewModel:** `NOT FOUND` (orquestrado via codebehind)
* **Service:** `TwoFactorService`, `TwoFactorStoreService`
* **Repository:** `FuncionarioRepository` (`IFuncionarioRepository`)
* **Tabela(s):** `Funcionarios` (25 colunas), `UserSessions`, `LoginTentativasSeguranca`
* **Persistência:** REAL (SQLite com hash seguro de senha)
* **Testes:** PASS (`FuncionarioRepositoryTests`, `TwoFactorServiceTests`)
* **UI Test:** PASS
* **Integração:** REAL (suporta TOTP padrão Google Authenticator/Authy, bloqueio de tentativas sucessivas e troca obrigatória no primeiro login)
* **Light:** PASS
* **Dark:** PASS
* **Permissões:** `SISTEMA_USUARIOS`
* **Status:** `CORE`

---

### Módulo 19: CONFIGURAÇÕES DO SISTEMA
* **Nome:** Painel de Configurações Administrativas
* **Tela:** `Views/ConfiguracoesSistemaWindow.xaml`, `Views/BackupSettingsWindow.xaml`, `Views/Configuracoes/ResetSistemaWindow.xaml`
* **ViewModel:** `NOT FOUND`
* **Service:** `ConfiguracoesSistemaService`, `DatabaseBackupService`
* **Repository:** `NOT FOUND`
* **Tabela(s):** `ConfiguracoesSistema` (25 parâmetros gravados)
* **Persistência:** REAL (`ConfiguracoesSistema` em SQLite)
* **Testes:** PASS
* **UI Test:** PASS
* **Integração:** REAL (configuração de dados da oficina, logotipo, chave de emissão, diretórios de backup e alternância de temas)
* **Light:** PASS
* **Dark:** PASS
* **Permissões:** `SISTEMA_CONFIGURAR`
* **Status:** `CORE`

---

### Módulo 20: BACKUP & RESTORE
* **Nome:** Motor de Backup, Validação de Integridade e Restauração Segura
* **Tela:** `Views/BackupSettingsWindow.xaml`, `Views/ConfiguracoesSistemaWindow.xaml`
* **ViewModel:** `NOT FOUND`
* **Service:** `DatabaseBackupService`, `ExternalBackupService`
* **Repository:** `NOT FOUND`
* **Tabela(s):** `DatabaseBackups` (38 registros)
* **Persistência:** REAL (arquivos `.db` físicos com checksum SHA-256 e tabela de auditoria `DatabaseBackups`)
* **Testes:** PASS (`DatabaseBackupServiceTests`, `ExternalBackupEncryptionHonestyTests`)
* **UI Test:** PASS
* **Integração:** REAL (criação automática a cada 24h, criação manual, proteção de jail de caminhos `EnforceRestorePathJail`, criação obrigatória de backup pré-restauração `pre_restore`, limpeza de pools e verificação de integridade)
* **Light:** PASS
* **Dark:** PASS
* **Permissões:** `SISTEMA_CONFIGURAR` (exigência estrita de autorização em `RestaurarBackupAuthorized`)
* **Status:** `CORE`

---

### Módulo 21: AUDITORIA / LOGS
* **Nome:** Trilha de Auditoria Comercial & Logs Técnicos
* **Tela:** Aba em `Views/ConfiguracoesSistemaWindow.xaml`
* **ViewModel:** `NOT FOUND`
* **Service:** `AuditLogService` (`App.Audit`), `LoggerService` (`App.Logger`)
* **Repository:** `AuditoriaRepository`
* **Tabela(s):** `AuditLogs` (17 colunas, 1.592 registros ativos)
* **Persistência:** REAL (`AuditLogs` em SQLite + arquivos de texto em `%LOCALAPPDATA%\PrimoAutoEletrica\Logs\`)
* **Testes:** PASS (`AuditLogServiceTests`)
* **UI Test:** PASS
* **Integração:** REAL (registra logins, acessos a menus, exclusões, alterações de preços, criação de backups e operações financeiras)
* **Light:** PASS
* **Dark:** PASS
* **Permissões:** `SISTEMA_AUDITORIA`
* **Status:** `CORE`

---

### Módulo 22: RBAC (PERFIS E PERMISSÕES)
* **Nome:** Controle de Acesso Baseado em Perfis
* **Tela:** `Views/ConfigurarPermissoesWindow.xaml`, `Views/GerenciarPerfisWindow.xaml`, `Views/NovoPerfilWindow.xaml`
* **ViewModel:** `NOT FOUND`
* **Service:** `PermissionService`, `PermissionProfileTestService`
* **Repository:** `NOT FOUND` (queries diretas em SQLite)
* **Tabela(s):** `PerfisAcesso` (10 perfis), `Permissoes` (82 permissões atômicas), `PerfilPermissoes` (276 associações)
* **Persistência:** REAL (SQLite)
* **Testes:** PASS (`PermissionProfileTestService`, `SecurityTests`)
* **UI Test:** PASS
* **Integração:** REAL (validação em botões de ação crítica `ValidarPermissaoModulo`, menus e interceptação de ações sensíveis)
* **Light:** PASS
* **Dark:** PASS
* **Permissões:** `SISTEMA_CONFIGURAR`
* **Status:** `CORE` (Nota: existe duplicidade cadastral de encoding no perfil "Mecanico" / "Mecânico" a sanear)

---

### Módulo 23: LICENCIAMENTO
* **Nome:** Sistema de Licenciamento do Software
* **Tela:** `Views/LicenseActivationWindow.xaml`
* **ViewModel:** `NOT FOUND`
* **Service:** `LicenseService`
* **Repository:** `NOT FOUND`
* **Tabela(s):** `NOT FOUND` (armazena em arquivo JSON local com chave de ativação)
* **Persistência:** MOCK (`LicenseService.IsCommercialScaffoldOnly = true`; licença local com validação simplificada por SHA-256 de MachineName/UserName)
* **Testes:** PASS (`LicenseServiceTests`)
* **UI Test:** PASS
* **Integração:** MOCK (não existe servidor de licença remoto, nem criptografia assimétrica RSA/ECDSA com chaves públicas, nem validação de expiração contra servidor SaaS)
* **Light:** PASS
* **Dark:** PASS
* **Permissões:** `NOT FOUND`
* **Status:** `MOCK` (Scaffold local demonstrativo; licenciamento comercial em servidor é `NOT_IMPLEMENTED`)

---

### Módulo 24: API REST
* **Nome:** API REST Minimal ASP.NET Core
* **Tela:** `NOT_APPLICABLE` (Swagger UI disponível em ambiente Development em `/swagger`)
* **ViewModel:** `NOT_APPLICABLE`
* **Service:** `PrimoAutoEletrica.Api/Program.cs`
* **Repository:** `ProdutoRepository`
* **Tabela(s):** `Orcamentos`, `Produtos`, `MovimentacoesFinanceiras`
* **Persistência:** REAL (opera sobre as tabelas existentes via serviços singleton)
* **Testes:** PASS (compila e executa; autenticação JWT com validação estrita de SigningKey >= 32 chars)
* **UI Test:** `NOT_APPLICABLE`
* **Integração:** REAL (fornece endpoints protegidos para orçamentos, estoque e resumo financeiro com rate limiter de 120 req/min)
* **Light:** `NOT_APPLICABLE`
* **Dark:** `NOT_APPLICABLE`
* **Permissões:** Claims JWT ("perm": "ORCAMENTO_LER", "ESTOQUE_LER", etc.)
* **Status:** `CORE` (para as rotas implementadas; endpoints de OS, Clientes e Veículos via API ainda são `NOT_IMPLEMENTED`)

---

### Módulo 25: INTEGRAÇÕES EXTERNAS
* **Nome:** Conectores com Serviços Externos
* **Composição:**
  1. *WhatsApp:* `PARTIAL` (Deep-link direto via protocolo `wa.me` com higienização de telefone e termo LGPD; não há WhatsApp Business Cloud API direta).
  2. *Fiscal SEFAZ:* `PARTIAL` / `EXTERNAL_DEPENDENCY` (Homologação operacional via Focus/PlugNotas; produção bloqueada preventivamente).
  3. *TEF / Maquininhas de Cartão:* `NOT_IMPLEMENTED` (pagamento com cartão registrado internamente, sem protocolo de TEF dedicado).
  4. *Gateways Pagamento (Pix / Boleto online):* `NOT_IMPLEMENTED` (Pix registrado como movimentação local; sem webhook de confirmação bancária automática).
  5. *Email / SMTP:* `NOT_IMPLEMENTED` (botão existe visualmente em `OrcamentosView.xaml`, mas sem handler de envio SMTP).
  6. *SMS:* `NOT_IMPLEMENTED`.
  7. *Nuvem / Portal Web da Oficina / Sync Multi-loja:* `NOT_IMPLEMENTED` (aplicação opera 100% desktop local).
* **Status Consolidado:** `PARTIAL`
