# PRIMOX — Matriz de Cobertura (Fase 14)

Gerado: 2026-09-08 07:20:31

Legenda: PASS | FAIL | BLOCKED | NOT TESTABLE | CONDITIONAL | KNOWN LIMITATION | DESTRUCTIVE — DIALOG VERIFIED | DISCOVERED | ORPHAN CANDIDATE — RETAINED | NOT FOUND

| ID | Modulo | Janela | Controle | Acao | Tipo | Testavel | Resultado | Evidencia |
| -- | ------ | ------ | -------- | ---- | ---- | -------- | --------- | --------- |
| NAV-001 | Agendamentos | MainWindow | Navigate | Abrir modulo | Navigation | Sim | PASS | GetCanonicalModuleNames |
| NAV-002 | AutoEletricaTecnica | MainWindow | Navigate | Abrir modulo | Navigation | Sim | PASS | GetCanonicalModuleNames |
| NAV-003 | CatalogoPecas | MainWindow | Navigate | Abrir modulo | Navigation | Sim | PASS | GetCanonicalModuleNames |
| NAV-004 | Clientes | MainWindow | Navigate | Abrir modulo | Navigation | Sim | PASS | GetCanonicalModuleNames |
| NAV-005 | Dashboard | MainWindow | Navigate | Abrir modulo | Navigation | Sim | PASS | GetCanonicalModuleNames |
| NAV-006 | Estoque | MainWindow | Navigate | Abrir modulo | Navigation | Sim | PASS | GetCanonicalModuleNames |
| NAV-007 | Financeiro | MainWindow | Navigate | Abrir modulo | Navigation | Sim | PASS | GetCanonicalModuleNames |
| NAV-008 | Fornecedores | MainWindow | Navigate | Abrir modulo | Navigation | Sim | PASS | GetCanonicalModuleNames |
| NAV-009 | Funcionarios | MainWindow | Navigate | Abrir modulo | Navigation | Sim | PASS | GetCanonicalModuleNames |
| NAV-010 | Help | MainWindow | Navigate | Abrir modulo | Navigation | Sim | PASS | GetCanonicalModuleNames |
| NAV-011 | ImportarNFe | MainWindow | Navigate | Abrir modulo | Navigation | Sim | PASS | GetCanonicalModuleNames |
| NAV-012 | OficinaKanban | MainWindow | Navigate | Abrir modulo | Navigation | Sim | PASS | GetCanonicalModuleNames |
| NAV-013 | Orcamentos | MainWindow | Navigate | Abrir modulo | Navigation | Sim | PASS | GetCanonicalModuleNames |
| NAV-014 | OrdensServico | MainWindow | Navigate | Abrir modulo | Navigation | Sim | PASS | GetCanonicalModuleNames |
| NAV-015 | PDV | MainWindow | Navigate | Abrir modulo | Navigation | Sim | PASS | GetCanonicalModuleNames |
| NAV-016 | Relatorios | MainWindow | Navigate | Abrir modulo | Navigation | Sim | PASS | GetCanonicalModuleNames |
| NAV-017 | Veiculos | MainWindow | Navigate | Abrir modulo | Navigation | Sim | PASS | GetCanonicalModuleNames |
| CLK-018 | Fornecedores | AdicionarFornecedorDialog | AdicionarFornecedorDialog | NaoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-019 | Fornecedores | AdicionarFornecedorDialog | AdicionarFornecedorDialog | SimButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-020 | OrdensServico | AgendamentosControl | AgendamentosControl | AbrirClienteAgendaButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-021 | OrdensServico | AgendamentosControl | AgendamentosControl | AbrirOsAgendaButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-022 | OrdensServico | AgendamentosControl | AgendamentosControl | AbrirVeiculoAgendaButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-023 | OrdensServico | AgendamentosControl | AgendamentosControl | RetryAgendaButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-024 | Estoque | AjusteEstoqueWindow | AjusteEstoqueWindow | CancelarButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-025 | Estoque | AjusteEstoqueWindow | AjusteEstoqueWindow | SalvarButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-026 | Geral | AssinaturaDigitalWindow | AssinaturaDigitalWindow | CancelarButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-027 | Geral | AssinaturaDigitalWindow | AssinaturaDigitalWindow | LimparButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-028 | Geral | AssinaturaDigitalWindow | AssinaturaDigitalWindow | SalvarButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-029 | OrdensServico | AtalhosTecladoWindow | AtalhosTecladoWindow | Fechar_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-030 | Geral | AtualizacaoWindow | AtualizacaoWindow | CancelButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-031 | Geral | AtualizacaoWindow | AtualizacaoWindow | CheckUpdateButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-032 | Geral | AtualizacaoWindow | AtualizacaoWindow | InstallUpdateButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-033 | Geral | AutoEletricaTecnicaControl | AutoEletricaTecnicaControl | AtualizarTecnicaButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-034 | Geral | AutoEletricaTecnicaControl | AutoEletricaTecnicaControl | GerarOrcamentoDiagnosticoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-035 | Geral | BackupSettingsWindow | BackupSettingsWindow | BrowseButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-036 | Geral | BackupSettingsWindow | BackupSettingsWindow | CancelButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-037 | Geral | BackupSettingsWindow | BackupSettingsWindow | SaveButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-038 | Geral | BackupSettingsWindow | BackupSettingsWindow | TestBackupButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-039 | CatalogoPecas | CatalogoPecasControl | CatalogoPecasControl | AtualizarButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-040 | CatalogoPecas | CatalogoPecasControl | CatalogoPecasControl | CriarProdutoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-041 | CatalogoPecas | CatalogoPecasControl | CatalogoPecasControl | CriarProdutoLinhaButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-042 | CatalogoPecas | CatalogoPecasControl | CatalogoPecasControl | ExportarButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-043 | CatalogoPecas | CatalogoPecasControl | CatalogoPecasControl | HistoricoImportacoesButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-044 | CatalogoPecas | CatalogoPecasControl | CatalogoPecasControl | IgnorarButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-045 | CatalogoPecas | CatalogoPecasControl | CatalogoPecasControl | ImportarCatalogoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-046 | CatalogoPecas | CatalogoPecasControl | CatalogoPecasControl | LimparFiltrosButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-047 | CatalogoPecas | CatalogoPecasControl | CatalogoPecasControl | RevisarButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-048 | CatalogoPecas | CatalogoPecasControl | CatalogoPecasControl | VerButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-049 | Clientes | ClientesControl | ClientesControl | EditarClienteButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-050 | Clientes | ClientesControl | ClientesControl | ExcluirClienteButton_Click | Destructive | Dialog only | DESTRUCTIVE — DIALOG VERIFIED (seguro) | reflection |
| CLK-051 | Clientes | ClientesControl | ClientesControl | ExportarClientesButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-052 | Clientes | ClientesControl | ClientesControl | HistoricoClienteButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-053 | Clientes | ClientesControl | ClientesControl | LimparFiltrosClientesButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-054 | Clientes | ClientesControl | ClientesControl | NovaOsClienteButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-055 | Clientes | ClientesControl | ClientesControl | NovoClienteButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-056 | Clientes | ClientesControl | ClientesControl | NovoOrcamentoClienteButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-057 | Clientes | ClientesControl | ClientesControl | RestaurarClienteButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-058 | Clientes | ClientesControl | ClientesControl | RetryClientesButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-059 | Clientes | ClientesControl | ClientesControl | VisualizarClienteButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-060 | Clientes | ClientesControl | ClientesControl | WhatsAppClienteButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-061 | Configuracoes | ConfiguracoesSistemaWindow | ConfiguracoesSistemaWindow | AbrirPastaButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-062 | Configuracoes | ConfiguracoesSistemaWindow | ConfiguracoesSistemaWindow | AtualizarImpressorasButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-063 | Configuracoes | ConfiguracoesSistemaWindow | ConfiguracoesSistemaWindow | AtualizarInformacoesBancoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-064 | Configuracoes | ConfiguracoesSistemaWindow | ConfiguracoesSistemaWindow | CarregarInformacoesBackupButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-065 | Configuracoes | ConfiguracoesSistemaWindow | ConfiguracoesSistemaWindow | ConfigurarPermissoesButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-066 | Configuracoes | ConfiguracoesSistemaWindow | ConfiguracoesSistemaWindow | CriarBackupManualButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-067 | Configuracoes | ConfiguracoesSistemaWindow | ConfiguracoesSistemaWindow | FecharButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-068 | Configuracoes | ConfiguracoesSistemaWindow | ConfiguracoesSistemaWindow | GerarPreviaComprovanteButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-069 | Configuracoes | ConfiguracoesSistemaWindow | ConfiguracoesSistemaWindow | GerenciarPerfisButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-070 | Configuracoes | ConfiguracoesSistemaWindow | ConfiguracoesSistemaWindow | RestaurarBackupButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-071 | Configuracoes | ConfiguracoesSistemaWindow | ConfiguracoesSistemaWindow | SalvarButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-072 | Configuracoes | ConfiguracoesSistemaWindow | ConfiguracoesSistemaWindow | SalvarConfiguracoesComerciaisButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-073 | Configuracoes | ConfiguracoesSistemaWindow | ConfiguracoesSistemaWindow | SalvarConfiguracoesEstacaoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-074 | Configuracoes | ConfiguracoesSistemaWindow | ConfiguracoesSistemaWindow | SalvarConfiguracoesOperacionaisButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-075 | Configuracoes | ConfiguracoesSistemaWindow | ConfiguracoesSistemaWindow | TestarConexaoSqlServerButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-076 | Configuracoes | ConfiguracoesSistemaWindow | ConfiguracoesSistemaWindow | TestarPastaRedeButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-077 | Configuracoes | ConfiguracoesSistemaWindow | ConfiguracoesSistemaWindow | ValidarBackupRestauracaoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-078 | Configuracoes | ConfiguracoesSistemaWindow | ConfiguracoesSistemaWindow | ValidarLogoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-079 | Configuracoes | ConfigurarPermissoesWindow | ConfigurarPermissoesWindow | CancelarPermissaoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-080 | Configuracoes | ConfigurarPermissoesWindow | ConfigurarPermissoesWindow | EditarPermissaoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-081 | Configuracoes | ConfigurarPermissoesWindow | ConfigurarPermissoesWindow | ExcluirPermissaoButton_Click | Destructive | Dialog only | DESTRUCTIVE — DIALOG VERIFIED (seguro) | reflection |
| CLK-082 | Configuracoes | ConfigurarPermissoesWindow | ConfigurarPermissoesWindow | ExportarPermissoesButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-083 | Configuracoes | ConfigurarPermissoesWindow | ConfigurarPermissoesWindow | FecharButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-084 | Configuracoes | ConfigurarPermissoesWindow | ConfigurarPermissoesWindow | NovaPermissaoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-085 | Configuracoes | ConfigurarPermissoesWindow | ConfigurarPermissoesWindow | SalvarPermissaoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-086 | Geral | ConfirmacaoCriticaWindow | ConfirmacaoCriticaWindow | CancelarButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-087 | Geral | ConfirmacaoCriticaWindow | ConfirmacaoCriticaWindow | ConfirmarButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-088 | Dashboard | DashboardControl | DashboardControl | AtalhoModuloButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-089 | Dashboard | DashboardControl | DashboardControl | AttentionItem_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-090 | Dashboard | DashboardControl | DashboardControl | AtualizarDashboardButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-091 | Clientes | EditarClienteWindow | EditarClienteWindow | AbrirAssinaturaButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-092 | Clientes | EditarClienteWindow | EditarClienteWindow | AbrirDocumentoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-093 | Clientes | EditarClienteWindow | EditarClienteWindow | AdicionarVeiculoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-094 | Clientes | EditarClienteWindow | EditarClienteWindow | FecharButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-095 | Clientes | EditarClienteWindow | EditarClienteWindow | HistoricoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-096 | Clientes | EditarClienteWindow | EditarClienteWindow | NovaOsButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-097 | Clientes | EditarClienteWindow | EditarClienteWindow | RegistrarAssinaturaButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-098 | Clientes | EditarClienteWindow | EditarClienteWindow | RemoverFotoButton_Click | Destructive | Dialog only | DESTRUCTIVE — DIALOG VERIFIED (seguro) | reflection |
| CLK-099 | Clientes | EditarClienteWindow | EditarClienteWindow | SalvarButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-100 | Clientes | EditarClienteWindow | EditarClienteWindow | SelecionarDocumentoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-101 | Clientes | EditarClienteWindow | EditarClienteWindow | SelecionarFotoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-102 | Clientes | EditarClienteWindow | EditarClienteWindow | WhatsAppButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-103 | Fornecedores | EditarFornecedorWindow | EditarFornecedorWindow | CancelarButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-104 | Fornecedores | EditarFornecedorWindow | EditarFornecedorWindow | FecharButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-105 | Fornecedores | EditarFornecedorWindow | EditarFornecedorWindow | SalvarButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-106 | Funcionarios | EditarFuncionarioWindow | EditarFuncionarioWindow | CancelarButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-107 | Funcionarios | EditarFuncionarioWindow | EditarFuncionarioWindow | RemoverFotoButton_Click | Destructive | Dialog only | DESTRUCTIVE — DIALOG VERIFIED (seguro) | reflection |
| CLK-108 | Funcionarios | EditarFuncionarioWindow | EditarFuncionarioWindow | SalvarButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-109 | Funcionarios | EditarFuncionarioWindow | EditarFuncionarioWindow | SelecionarFotoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-110 | Estoque | EditarProdutoWindow | EditarProdutoWindow | AbrirAnexoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-111 | Estoque | EditarProdutoWindow | EditarProdutoWindow | FecharButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-112 | Estoque | EditarProdutoWindow | EditarProdutoWindow | GerarCodigoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-113 | Estoque | EditarProdutoWindow | EditarProdutoWindow | RemoverAnexoButton_Click | Destructive | Dialog only | DESTRUCTIVE — DIALOG VERIFIED (seguro) | reflection |
| CLK-114 | Estoque | EditarProdutoWindow | EditarProdutoWindow | RemoverFotoButton_Click | Destructive | Dialog only | DESTRUCTIVE — DIALOG VERIFIED (seguro) | reflection |
| CLK-115 | Estoque | EditarProdutoWindow | EditarProdutoWindow | SalvarButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-116 | Estoque | EditarProdutoWindow | EditarProdutoWindow | SelecionarAnexosButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-117 | Estoque | EditarProdutoWindow | EditarProdutoWindow | SelecionarFotoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-118 | Estoque | EstoqueControl | EstoqueControl | AjustarEstoqueButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-119 | Estoque | EstoqueControl | EstoqueControl | EditarProdutoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-120 | Estoque | EstoqueControl | EstoqueControl | EditarProdutoSelecionadoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-121 | Estoque | EstoqueControl | EstoqueControl | EntradaEstoqueButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-122 | Estoque | EstoqueControl | EstoqueControl | EtiquetaProdutoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-123 | Estoque | EstoqueControl | EstoqueControl | ExcluirProdutoButton_Click | Destructive | Dialog only | DESTRUCTIVE — DIALOG VERIFIED (seguro) | reflection |
| CLK-124 | Estoque | EstoqueControl | EstoqueControl | HistoricoEstoqueButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-125 | Estoque | EstoqueControl | EstoqueControl | InventariarProdutoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-126 | Estoque | EstoqueControl | EstoqueControl | NovoProdutoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-127 | Estoque | EstoqueControl | EstoqueControl | RetryEstoqueButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-128 | Estoque | EstoqueControl | EstoqueControl | SaidaEstoqueButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-129 | Estoque | EstoqueControl | EstoqueControl | VisualizarProdutoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-130 | Financeiro | FinanceiroControl | FinanceiroControl | AtualizarDadosButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-131 | Financeiro | FinanceiroControl | FinanceiroControl | BaixarContaPagarSelecionada_Click | Destructive | Dialog only | DESTRUCTIVE — DIALOG VERIFIED (seguro) | reflection |
| CLK-132 | Financeiro | FinanceiroControl | FinanceiroControl | BaixarContaReceberSelecionada_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-133 | Financeiro | FinanceiroControl | FinanceiroControl | ExportarRelatorioButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-134 | Financeiro | FinanceiroControl | FinanceiroControl | FiltroContasPagarHoje_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-135 | Financeiro | FinanceiroControl | FinanceiroControl | FiltroContasPagarSemana_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-136 | Financeiro | FinanceiroControl | FinanceiroControl | FiltroContasPagarTodas_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-137 | Financeiro | FinanceiroControl | FinanceiroControl | FiltroContasPagarVencidas_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-138 | Financeiro | FinanceiroControl | FinanceiroControl | FiltroContasReceberHoje_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-139 | Financeiro | FinanceiroControl | FinanceiroControl | FiltroContasReceberSemana_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-140 | Financeiro | FinanceiroControl | FinanceiroControl | FiltroContasReceberTodas_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-141 | Financeiro | FinanceiroControl | FinanceiroControl | FiltroContasReceberVencidas_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-142 | Financeiro | FinanceiroControl | FinanceiroControl | FiltrosAvancadosButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-143 | Financeiro | FinanceiroControl | FinanceiroControl | GerarPDFButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-144 | Financeiro | FinanceiroControl | FinanceiroControl | ImprimirButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-145 | Financeiro | FinanceiroControl | FinanceiroControl | RetryFinanceiroButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-146 | Fornecedores | FornecedoresControl | FornecedoresControl | AtualizarButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-147 | Fornecedores | FornecedoresControl | FornecedoresControl | EditarButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-148 | Fornecedores | FornecedoresControl | FornecedoresControl | ExcluirFornecedorSelecionadoButton_Click | Destructive | Dialog only | DESTRUCTIVE — DIALOG VERIFIED (seguro) | reflection |
| CLK-149 | Fornecedores | FornecedoresControl | FornecedoresControl | LimparFiltrosButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-150 | Fornecedores | FornecedoresControl | FornecedoresControl | NovoFornecedorButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-151 | Fornecedores | FornecedoresControl | FornecedoresControl | VisualizarButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-152 | Geral | FriendlyErrorWindow | FriendlyErrorWindow | CloseButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-153 | Geral | FriendlyErrorWindow | FriendlyErrorWindow | CopyErrorButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-154 | Funcionarios | FuncionariosControl | FuncionariosControl | AtualizarButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-155 | Funcionarios | FuncionariosControl | FuncionariosControl | BloquearFuncionarioButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-156 | Funcionarios | FuncionariosControl | FuncionariosControl | ConfigurarPermissoesButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-157 | Funcionarios | FuncionariosControl | FuncionariosControl | EditarFuncionarioButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-158 | Funcionarios | FuncionariosControl | FuncionariosControl | ExcluirFuncionarioButton_Click | Destructive | Dialog only | DESTRUCTIVE — DIALOG VERIFIED (seguro) | reflection |
| CLK-159 | Funcionarios | FuncionariosControl | FuncionariosControl | GerenciarPerfisButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-160 | Funcionarios | FuncionariosControl | FuncionariosControl | LimparFiltrosButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-161 | Funcionarios | FuncionariosControl | FuncionariosControl | NovoFuncionarioButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-162 | Funcionarios | FuncionariosControl | FuncionariosControl | ReativarFuncionarioButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-163 | Funcionarios | FuncionariosControl | FuncionariosControl | RedefinirSenhaButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-164 | Funcionarios | FuncionariosControl | FuncionariosControl | VisualizarFuncionarioButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-165 | Geral | GerenciarPerfisWindow | GerenciarPerfisWindow | CancelarPerfilButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-166 | Geral | GerenciarPerfisWindow | GerenciarPerfisWindow | EditarPerfilButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-167 | Geral | GerenciarPerfisWindow | GerenciarPerfisWindow | ExcluirPerfilButton_Click | Destructive | Dialog only | DESTRUCTIVE — DIALOG VERIFIED (seguro) | reflection |
| CLK-168 | Geral | GerenciarPerfisWindow | GerenciarPerfisWindow | FecharButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-169 | Geral | GerenciarPerfisWindow | GerenciarPerfisWindow | NovoPerfilButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-170 | Geral | GerenciarPerfisWindow | GerenciarPerfisWindow | SalvarPerfilButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-171 | Geral | GlobalSearchControl | GlobalSearchControl | ClearButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-172 | Clientes | HistoricoClienteWindow | HistoricoClienteWindow | FecharButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-173 | Estoque | HistoricoEstoqueWindow | HistoricoEstoqueWindow | Fechar_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-174 | CatalogoPecas | ImportarCatalogoPecasWindow | ImportarCatalogoPecasWindow | CancelarButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-175 | CatalogoPecas | ImportarCatalogoPecasWindow | ImportarCatalogoPecasWindow | ConfirmarImportacaoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-176 | CatalogoPecas | ImportarCatalogoPecasWindow | ImportarCatalogoPecasWindow | GerarPreviaButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-177 | CatalogoPecas | ImportarCatalogoPecasWindow | ImportarCatalogoPecasWindow | SelecionarArquivoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-178 | ImportarNFe | ImportarNFeControl | ImportarNFeControl | AbrirPastaXmlsButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-179 | ImportarNFe | ImportarNFeControl | ImportarNFeControl | AtualizarHistoricoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-180 | ImportarNFe | ImportarNFeControl | ImportarNFeControl | CancelarImportacaoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-181 | ImportarNFe | ImportarNFeControl | ImportarNFeControl | DesfazerProdutosImportacaoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-182 | ImportarNFe | ImportarNFeControl | ImportarNFeControl | ExcluirImportacaoSelecionadaButton_Click | Destructive | Dialog only | DESTRUCTIVE — DIALOG VERIFIED (seguro) | reflection |
| CLK-183 | ImportarNFe | ImportarNFeControl | ImportarNFeControl | LimparFiltrosButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-184 | ImportarNFe | ImportarNFeControl | ImportarNFeControl | NovaImportacaoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-185 | ImportarNFe | ImportarNFeControl | ImportarNFeControl | ProdutosImportacaoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-186 | ImportarNFe | ImportarNFeControl | ImportarNFeControl | ReprocessarImportacaoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-187 | ImportarNFe | ImportarNFeControl | ImportarNFeControl | VisualizarImportacaoButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-188 | ImportarNFe | ImportarNotaWindow | ImportarNotaWindow | AplicarSugestoesButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-189 | ImportarNFe | ImportarNotaWindow | ImportarNotaWindow | CancelarButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-190 | ImportarNFe | ImportarNotaWindow | ImportarNotaWindow | IgnorarSelecionadosButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-191 | ImportarNFe | ImportarNotaWindow | ImportarNotaWindow | ImportarButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-192 | ImportarNFe | ImportarNotaWindow | ImportarNotaWindow | SelecionarTodosButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-193 | ImportarNFe | ImportarNotaWindow | ImportarNotaWindow | SelecionarXmlButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-194 | Geral | LicenseActivationWindow | LicenseActivationWindow | ActivateButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-195 | Geral | LicenseActivationWindow | LicenseActivationWindow | CancelButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-196 | Login | LoginWindow | LoginWindow | CloseButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-197 | Login | LoginWindow | LoginWindow | EsqueciSenhaButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-198 | Login | LoginWindow | LoginWindow | LoginButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-199 | Login | LoginWindow | LoginWindow | MostrarSenhaButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-200 | Geral | MainWindow | MainWindow | BackButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-201 | Geral | MainWindow | MainWindow | CommandCenterButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-202 | Geral | MainWindow | MainWindow | DensityToggleButton_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-203 | Geral | MainWindow | MainWindow | MenuAgendamento_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-204 | Geral | MainWindow | MainWindow | MenuAutoEletricaTecnica_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-205 | Geral | MainWindow | MainWindow | MenuCatalogoPecas_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-206 | Geral | MainWindow | MainWindow | MenuClientes_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-207 | Geral | MainWindow | MainWindow | MenuConfiguracoes_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-208 | Geral | MainWindow | MainWindow | MenuDashboard_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-209 | Geral | MainWindow | MainWindow | MenuEstoque_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-210 | Geral | MainWindow | MainWindow | MenuFinanceiro_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-211 | Geral | MainWindow | MainWindow | MenuFornecedores_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-212 | Geral | MainWindow | MainWindow | MenuFuncionarios_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-213 | Geral | MainWindow | MainWindow | MenuHelp_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-214 | Geral | MainWindow | MainWindow | MenuImportarNFe_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-215 | Geral | MainWindow | MainWindow | MenuOS_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-216 | Geral | MainWindow | MainWindow | MenuOficinaKanban_Click | Click | Condicional | DISCOVERED | reflection |
| CLK-217 | Geral | MainWindow | MainWindow | MenuOrcamentos_Click | Click | Condicional | DISCOVERED | reflection |
| WIN-218 | Estoque | AjusteEstoqueWindow | AjusteEstoqueWindow | OpenClose | Window | Sim | PASS | PrepareWindow |
| WIN-219 | OrdensServico | AtalhosTecladoWindow | AtalhosTecladoWindow | OpenClose | Window | Sim | PASS | PrepareWindow |
| WIN-220 | CatalogoPecas | ImportarCatalogoPecasWindow | ImportarCatalogoPecasWindow | OpenClose | Window | Sim | PASS | PrepareWindow |
| WIN-221 | Login | LoginWindow | LoginWindow | OpenClose | Window | Sim | PASS | PrepareWindow |
| WIN-222 | Agendamentos | NovoAgendamentoPremiumWindow | NovoAgendamentoPremiumWindow | OpenClose | Window | Sim | PASS | PrepareWindow |
| WIN-223 | Clientes | NovoClienteWindow | NovoClienteWindow | OpenClose | Window | Sim | PASS | PrepareWindow |
| WIN-224 | Fornecedores | NovoFornecedorWindow | NovoFornecedorWindow | OpenClose | Window | Sim | PASS | PrepareWindow |
| WIN-225 | Estoque | NovoProdutoWindow | NovoProdutoWindow | OpenClose | Window | Sim | PASS | PrepareWindow |
| WIN-226 | OrdensServico | OrcamentosView | OrcamentosView | OpenClose | Window | Sim | PASS | PrepareWindow |
| WIN-227 | Geral | PrimeiraExecucaoWindow | PrimeiraExecucaoWindow | OpenClose | Window | Sim | PASS | PrepareWindow |
| WIN-228 | Geral | SelecaoFilialWindow | SelecaoFilialWindow | OpenClose | Window | Sim | PASS | PrepareWindow |
| WIN-229 | OrdensServico | UsuariosOnlineWindow | UsuariosOnlineWindow | OpenClose | Window | Sim | PASS | PrepareWindow |
| WIN-230 | Clientes | VisualizarClienteWindow | VisualizarClienteWindow | OpenClose | Window | Sim | PASS | PrepareWindow |
| WIN-231 | Geral | MainWindow | MainWindow | Open | Window | Parametros/seguro | CONDITIONAL | requer fixture/parametros |
| WIN-232 | ImportarNFe | ImportarNotaWindow | ImportarNotaWindow | Open | Window | Parametros/seguro | CONDITIONAL | requer fixture/parametros |
| WIN-233 | Configuracoes | ConfigurarPermissoesWindow | ConfigurarPermissoesWindow | Open | Window | Parametros/seguro | CONDITIONAL | requer fixture/parametros |
| WIN-234 | Geral | GerenciarPerfisWindow | GerenciarPerfisWindow | Open | Window | Parametros/seguro | CONDITIONAL | requer fixture/parametros |
| WIN-235 | Fornecedores | EditarFornecedorWindow | EditarFornecedorWindow | Open | Window | Parametros/seguro | CONDITIONAL | requer fixture/parametros |
| WIN-236 | Funcionarios | EditarFuncionarioWindow | EditarFuncionarioWindow | Open | Window | Parametros/seguro | CONDITIONAL | requer fixture/parametros |
| WIN-237 | Clientes | EditarClienteWindow | EditarClienteWindow | Open | Window | Parametros/seguro | CONDITIONAL | requer fixture/parametros |
| WIN-238 | Estoque | EditarProdutoWindow | EditarProdutoWindow | Open | Window | Parametros/seguro | CONDITIONAL | requer fixture/parametros |
| WIN-239 | Clientes | HistoricoClienteWindow | HistoricoClienteWindow | Open | Window | Parametros/seguro | CONDITIONAL | requer fixture/parametros |
| WIN-240 | Funcionarios | NovoFuncionarioWindow | NovoFuncionarioWindow | Open | Window | Parametros/seguro | CONDITIONAL | requer fixture/parametros |
| WIN-241 | Orcamentos | NovoOrcamentoWindow | NovoOrcamentoWindow | Open | Window | Parametros/seguro | CONDITIONAL | requer fixture/parametros |
| WIN-242 | Geral | NovoPerfilWindow | NovoPerfilWindow | Open | Window | Parametros/seguro | CONDITIONAL | requer fixture/parametros |
| WIN-243 | Veiculos | NovoVeiculoWindow | NovoVeiculoWindow | Open | Window | Parametros/seguro | CONDITIONAL | requer fixture/parametros |
| WIN-244 | OrdensServico | OrdemServicoWindow | OrdemServicoWindow | Open | Window | Parametros/seguro | CONDITIONAL | requer fixture/parametros |
| WIN-245 | Orcamentos | SelecionarOrcamentoWindow | SelecionarOrcamentoWindow | Open | Window | Parametros/seguro | CONDITIONAL | requer fixture/parametros |
| WIN-246 | Fornecedores | VisualizarFornecedorWindow | VisualizarFornecedorWindow | Open | Window | Parametros/seguro | CONDITIONAL | requer fixture/parametros |
| WIN-247 | Veiculos | VisualizarVeiculoWindow | VisualizarVeiculoWindow | Open | Window | Parametros/seguro | CONDITIONAL | requer fixture/parametros |
| WIN-248 | Fornecedores | AdicionarFornecedorDialog | AdicionarFornecedorDialog | Open | Window | Parametros/seguro | CONDITIONAL | requer fixture/parametros |
| BTN-249 | Agendamentos | Agendamentos | (sem Name) | Novo agendamento + | Button | Enumerado | PASS | descoberta+identidade |
| BTN-250 | Agendamentos | Agendamentos | (sem Name) | Encaixe rápido ! | Button | Enumerado | PASS | descoberta+identidade |
| BTN-251 | Agendamentos | Agendamentos | (sem Name) | Atualizar agenda R | Button | Sim | PASS | click seguro |
| BTN-252 | Agendamentos | Agendamentos | (sem Name) | Imprimir PR | Button | Enumerado | PASS | descoberta+identidade |
| BTN-253 | Agendamentos | Agendamentos | (sem Name) | Exportar PDF | Button | Enumerado | PASS | descoberta+identidade |
| BTN-254 | Agendamentos | Agendamentos | (sem Name) | Filtros FL | Button | Enumerado | PASS | descoberta+identidade |
| BTN-255 | Agendamentos | Agendamentos | (sem Name) | Modo oficina OF | Button | Enumerado | PASS | descoberta+identidade |
| BTN-256 | Agendamentos | Agendamentos | PART_PreviousButton | Botão Anterior | Button | Enumerado | PASS | descoberta+identidade |
| BTN-257 | Agendamentos | Agendamentos | PART_HeaderButton | setembro de 2026 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-258 | Agendamentos | Agendamentos | PART_NextButton | Botão Próximo | Button | Enumerado | PASS | descoberta+identidade |
| BTN-259 | Agendamentos | Agendamentos | (sem Name) | 31 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-260 | Agendamentos | Agendamentos | (sem Name) | 1 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-261 | Agendamentos | Agendamentos | (sem Name) | 2 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-262 | Agendamentos | Agendamentos | (sem Name) | 3 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-263 | Agendamentos | Agendamentos | (sem Name) | 4 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-264 | Agendamentos | Agendamentos | (sem Name) | 5 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-265 | Agendamentos | Agendamentos | (sem Name) | 6 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-266 | Agendamentos | Agendamentos | (sem Name) | 7 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-267 | Agendamentos | Agendamentos | (sem Name) | 8 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-268 | Agendamentos | Agendamentos | (sem Name) | 9 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-269 | Agendamentos | Agendamentos | (sem Name) | 10 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-270 | Agendamentos | Agendamentos | (sem Name) | 11 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-271 | Agendamentos | Agendamentos | (sem Name) | 12 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-272 | Agendamentos | Agendamentos | (sem Name) | 13 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-273 | Agendamentos | Agendamentos | (sem Name) | 14 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-274 | Agendamentos | Agendamentos | (sem Name) | 15 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-275 | Agendamentos | Agendamentos | (sem Name) | 16 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-276 | Agendamentos | Agendamentos | (sem Name) | 17 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-277 | Agendamentos | Agendamentos | (sem Name) | 18 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-278 | Agendamentos | Agendamentos | (sem Name) | 19 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-279 | Agendamentos | Agendamentos | (sem Name) | 20 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-280 | Agendamentos | Agendamentos | (sem Name) | 21 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-281 | Agendamentos | Agendamentos | (sem Name) | 22 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-282 | Agendamentos | Agendamentos | (sem Name) | 23 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-283 | Agendamentos | Agendamentos | (sem Name) | 24 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-284 | Agendamentos | Agendamentos | (sem Name) | 25 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-285 | Agendamentos | Agendamentos | (sem Name) | 26 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-286 | Agendamentos | Agendamentos | (sem Name) | 27 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-287 | Agendamentos | Agendamentos | (sem Name) | 28 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-288 | Agendamentos | Agendamentos | (sem Name) | 29 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-289 | Agendamentos | Agendamentos | (sem Name) | 30 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-290 | Agendamentos | Agendamentos | (sem Name) | 1 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-291 | Agendamentos | Agendamentos | (sem Name) | 2 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-292 | Agendamentos | Agendamentos | (sem Name) | 3 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-293 | Agendamentos | Agendamentos | (sem Name) | 4 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-294 | Agendamentos | Agendamentos | (sem Name) | 5 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-295 | Agendamentos | Agendamentos | (sem Name) | 6 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-296 | Agendamentos | Agendamentos | (sem Name) | 7 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-297 | Agendamentos | Agendamentos | (sem Name) | 8 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-298 | Agendamentos | Agendamentos | (sem Name) | 9 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-299 | Agendamentos | Agendamentos | (sem Name) | 10 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-300 | Agendamentos | Agendamentos | (sem Name) | 11 | Button | Enumerado | PASS | descoberta+identidade |
| BTN-301 | Agendamentos | Agendamentos | (sem Name) | Limpar filtros | Button | Sim | PASS | click seguro |
| BTN-302 | Agendamentos | Agendamentos | CheckInAgendamentoButton | Entrada | Button | Disabled | CONDITIONAL | disabled |
| BTN-303 | Agendamentos | Agendamentos | CheckOutAgendamentoButton | Saída | Button | Disabled | CONDITIONAL | disabled |
| BTN-304 | Agendamentos | Agendamentos | (sem Name) | Novo + | Button | Enumerado | PASS | descoberta+identidade |
| BTN-305 | Agendamentos | Agendamentos | (sem Name) | Editar ED | Button | Disabled | CONDITIONAL | disabled |
| BTN-306 | Agendamentos | Agendamentos | ReagendarAgendamentoButton | Reagendar RE | Button | Disabled | CONDITIONAL | disabled |
| BTN-307 | Agendamentos | Agendamentos | ConfirmarAgendamentoButton | Confirmar OK | Button | Disabled | CONDITIONAL | disabled |
| BTN-308 | Agendamentos | Agendamentos | (sem Name) | Duplicar CP | Button | Disabled | CONDITIONAL | disabled |
| BTN-309 | Agendamentos | Agendamentos | GerarOsAgendamentoButton | Gerar OS OS | Button | Disabled | CONDITIONAL | disabled |
| BTN-310 | Agendamentos | Agendamentos | (sem Name) | Cancelar X | Button | Disabled | CONDITIONAL | disabled |
| BTN-311 | Agendamentos | Agendamentos | (sem Name) | Cliente | Button | Enumerado | PASS | descoberta+identidade |
| BTN-312 | Agendamentos | Agendamentos | (sem Name) | Veículo | Button | Enumerado | PASS | descoberta+identidade |
| BTN-313 | Agendamentos | Agendamentos | (sem Name) | Abrir OS | Button | Enumerado | PASS | descoberta+identidade |
| BTN-314 | Agendamentos | Agendamentos | (sem Name) | Iniciar IN | Button | Disabled | CONDITIONAL | disabled |
| BTN-315 | Agendamentos | Agendamentos | (sem Name) | Finalizar ✓ | Button | Disabled | CONDITIONAL | disabled |
| BTN-316 | Agendamentos | Agendamentos | (sem Name) | Reagendar 📅 | Button | Disabled | CONDITIONAL | disabled |
| BTN-317 | Agendamentos | Agendamentos | (sem Name) | WhatsApp 💬 | Button | Disabled | CONDITIONAL | disabled |
| BTN-318 | Agendamentos | Agendamentos | (sem Name) | Gerar OS OS | Button | Disabled | CONDITIONAL | disabled |
| BTN-319 | Agendamentos | Agendamentos | (sem Name) | Editar ✏️ | Button | Disabled | CONDITIONAL | disabled |
| BTN-320 | AutoEletricaTecnica | AutoEletricaTecnica | AtualizarTecnicaButton | Atualizar | Button | Sim | PASS | click seguro |
| BTN-321 | AutoEletricaTecnica | AutoEletricaTecnica | GerarOrcamentoDiagnosticoButton | Gerar orcamento | Button | Enumerado | PASS | descoberta+identidade |
| BTN-322 | AutoEletricaTecnica | AutoEletricaTecnica | (sem Name) |  | Button | Enumerado | PASS | descoberta+identidade |
| BTN-323 | AutoEletricaTecnica | AutoEletricaTecnica | (sem Name) |  | Button | Enumerado | PASS | descoberta+identidade |
| BTN-324 | CatalogoPecas | CatalogoPecas | ImportarCatalogoButton | Importar catalogo | Button | Enumerado | PASS | descoberta+identidade |
| BTN-325 | CatalogoPecas | CatalogoPecas | HistoricoImportacoesButton | Historico de importacoes | Button | Enumerado | PASS | descoberta+identidade |
| BTN-326 | CatalogoPecas | CatalogoPecas | ExportarButton | Exportar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-327 | CatalogoPecas | CatalogoPecas | AtualizarButton | Atualizar | Button | Sim | PASS | click seguro |
| BTN-328 | CatalogoPecas | CatalogoPecas | CriarProdutoButton | Criar produto no estoque | Button | Disabled | CONDITIONAL | disabled |
| BTN-329 | CatalogoPecas | CatalogoPecas | LimparFiltrosButton | Limpar filtros | Button | Sim | PASS | click seguro |
| BTN-330 | Clientes | Clientes | LimparFiltrosClientesButton | Limpar filtros | Button | Sim | PASS | click seguro |
| BTN-331 | Clientes | Clientes | ExportarClientesButton | Exportar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-332 | Clientes | Clientes | WhatsAppClienteButton | WhatsApp | Button | Disabled | CONDITIONAL | disabled |
| BTN-333 | Clientes | Clientes | NovaOsClienteButton | Nova OS | Button | Disabled | CONDITIONAL | disabled |
| BTN-334 | Clientes | Clientes | NovoOrcamentoClienteButton | Novo orcamento | Button | Disabled | CONDITIONAL | disabled |
| BTN-335 | Clientes | Clientes | NovoClienteButton | Novo cliente | Button | Enumerado | PASS | descoberta+identidade |
| BTN-336 | Clientes | Clientes | (sem Name) | Ver | Button | Enumerado | PASS | descoberta+identidade |
| BTN-337 | Clientes | Clientes | (sem Name) | Editar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-338 | Clientes | Clientes | (sem Name) | Historico | Button | Enumerado | PASS | descoberta+identidade |
| BTN-339 | Clientes | Clientes | (sem Name) | Excluir | Destructive | Dialog only | DESTRUCTIVE — DIALOG VERIFIED | nao executado real |
| BTN-340 | Clientes | Clientes | (sem Name) | Ver | Button | Enumerado | PASS | descoberta+identidade |
| BTN-341 | Clientes | Clientes | (sem Name) | Editar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-342 | Clientes | Clientes | (sem Name) | Historico | Button | Enumerado | PASS | descoberta+identidade |
| BTN-343 | Clientes | Clientes | (sem Name) | Excluir | Destructive | Dialog only | DESTRUCTIVE — DIALOG VERIFIED | nao executado real |
| BTN-344 | Dashboard | Dashboard | AtualizarDashboardButton | Atualizar | Button | Sim | PASS | click seguro |
| BTN-345 | Dashboard | Dashboard | (sem Name) | Warning 2 orçamento(s) aguardando decisão Orçamentos | Button | Enumerado | PASS | descoberta+identidade |
| BTN-346 | Dashboard | Dashboard | AtalhoOrcamentosDashboardButton | Novo orçamento | Button | Enumerado | PASS | descoberta+identidade |
| BTN-347 | Dashboard | Dashboard | AtalhoOrdensServicoDashboardButton | Nova OS | Button | Enumerado | PASS | descoberta+identidade |
| BTN-348 | Dashboard | Dashboard | AtalhoAgendamentosDashboardButton | Agenda | Button | Enumerado | PASS | descoberta+identidade |
| BTN-349 | Dashboard | Dashboard | AtalhoPdvDashboardButton | PDV | Button | Enumerado | PASS | descoberta+identidade |
| BTN-350 | Dashboard | Dashboard | (sem Name) | Clientes | Button | Enumerado | PASS | descoberta+identidade |
| BTN-351 | Dashboard | Dashboard | (sem Name) | Veículos | Button | Enumerado | PASS | descoberta+identidade |
| BTN-352 | Dashboard | Dashboard | AtalhoEstoqueDashboardButton | Estoque | Button | Enumerado | PASS | descoberta+identidade |
| BTN-353 | Dashboard | Dashboard | AtalhoFinanceiroDashboardButton | Financeiro | Button | Enumerado | PASS | descoberta+identidade |
| BTN-354 | Dashboard | Dashboard | (sem Name) | Kanban | Button | Enumerado | PASS | descoberta+identidade |
| BTN-355 | Estoque | Estoque | NovoProdutoButton | Novo produto | Button | Enumerado | PASS | descoberta+identidade |
| BTN-356 | Estoque | Estoque | EditarProdutoSelecionadoButton | Editar produto | Button | Enumerado | PASS | descoberta+identidade |
| BTN-357 | Estoque | Estoque | AjustarEstoqueButton | Ajuste | Button | Enumerado | PASS | descoberta+identidade |
| BTN-358 | Estoque | Estoque | EntradaEstoqueButton | Entrada | Button | Enumerado | PASS | descoberta+identidade |
| BTN-359 | Estoque | Estoque | SaidaEstoqueButton | Saida | Button | Enumerado | PASS | descoberta+identidade |
| BTN-360 | Estoque | Estoque | InventarioEstoqueButton | Inventario | Button | Enumerado | PASS | descoberta+identidade |
| BTN-361 | Estoque | Estoque | EtiquetaProdutoButton | Etiqueta | Button | Enumerado | PASS | descoberta+identidade |
| BTN-362 | Estoque | Estoque | HistoricoEstoqueHeaderButton | Historico | Button | Enumerado | PASS | descoberta+identidade |
| BTN-363 | Estoque | Estoque | (sem Name) | Ver | Button | Enumerado | PASS | descoberta+identidade |
| BTN-364 | Estoque | Estoque | (sem Name) | Editar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-365 | Estoque | Estoque | (sem Name) | Inventario | Button | Enumerado | PASS | descoberta+identidade |
| BTN-366 | Estoque | Estoque | (sem Name) | Historico | Button | Enumerado | PASS | descoberta+identidade |
| BTN-367 | Estoque | Estoque | (sem Name) | Excluir | Destructive | Dialog only | DESTRUCTIVE — DIALOG VERIFIED | nao executado real |
| BTN-368 | Estoque | Estoque | (sem Name) | Ver | Button | Enumerado | PASS | descoberta+identidade |
| BTN-369 | Estoque | Estoque | (sem Name) | Editar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-370 | Estoque | Estoque | (sem Name) | Inventario | Button | Enumerado | PASS | descoberta+identidade |
| BTN-371 | Estoque | Estoque | (sem Name) | Historico | Button | Enumerado | PASS | descoberta+identidade |
| BTN-372 | Estoque | Estoque | (sem Name) | Excluir | Destructive | Dialog only | DESTRUCTIVE — DIALOG VERIFIED | nao executado real |
| BTN-373 | Financeiro | Financeiro | AtualizarDadosButton | Atualizar | Button | Sim | PASS | click seguro |
| BTN-374 | Financeiro | Financeiro | ExportarRelatorioButton | Exportar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-375 | Financeiro | Financeiro | (sem Name) | PDF | Button | Enumerado | PASS | descoberta+identidade |
| BTN-376 | Financeiro | Financeiro | (sem Name) | Imprimir | Button | Enumerado | PASS | descoberta+identidade |
| BTN-377 | Financeiro | Financeiro | (sem Name) | Filtros | Button | Enumerado | PASS | descoberta+identidade |
| BTN-378 | Financeiro | Financeiro | FiltroContasPagarTodasButton | Todas | Button | Enumerado | PASS | descoberta+identidade |
| BTN-379 | Financeiro | Financeiro | FiltroContasPagarVencidasButton | Vencidas | Button | Enumerado | PASS | descoberta+identidade |
| BTN-380 | Financeiro | Financeiro | FiltroContasPagarHojeButton | Hoje | Button | Enumerado | PASS | descoberta+identidade |
| BTN-381 | Financeiro | Financeiro | FiltroContasPagarSemanaButton | Semana | Button | Enumerado | PASS | descoberta+identidade |
| BTN-382 | Financeiro | Financeiro | BaixarContaPagarSelecionadaButton | Baixar selecionada | Button | Enumerado | PASS | descoberta+identidade |
| BTN-383 | Financeiro | Financeiro | FiltroContasReceberTodasButton | Todas | Button | Enumerado | PASS | descoberta+identidade |
| BTN-384 | Financeiro | Financeiro | FiltroContasReceberVencidasButton | Vencidas | Button | Enumerado | PASS | descoberta+identidade |
| BTN-385 | Financeiro | Financeiro | FiltroContasReceberHojeButton | Hoje | Button | Enumerado | PASS | descoberta+identidade |
| BTN-386 | Financeiro | Financeiro | FiltroContasReceberSemanaButton | Semana | Button | Enumerado | PASS | descoberta+identidade |
| BTN-387 | Financeiro | Financeiro | BaixarContaReceberSelecionadaButton | Receber selecionada | Button | Enumerado | PASS | descoberta+identidade |
| BTN-388 | Fornecedores | Fornecedores | (sem Name) | Atualizar 🔄 | Button | Sim | PASS | click seguro |
| BTN-389 | Fornecedores | Fornecedores | (sem Name) | Novo Fornecedor ➕ | Button | Enumerado | PASS | descoberta+identidade |
| BTN-390 | Fornecedores | Fornecedores | ExcluirFornecedorSelecionadoButton | Excluir fornecedor selecionado | Button | Disabled | CONDITIONAL | disabled |
| BTN-391 | Fornecedores | Fornecedores | (sem Name) | Limpar Filtros | Button | Sim | PASS | click seguro |
| BTN-392 | Fornecedores | Fornecedores | (sem Name) | Ver | Button | Enumerado | PASS | descoberta+identidade |
| BTN-393 | Fornecedores | Fornecedores | (sem Name) | Editar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-394 | Fornecedores | Fornecedores | (sem Name) | Ver | Button | Enumerado | PASS | descoberta+identidade |
| BTN-395 | Fornecedores | Fornecedores | (sem Name) | Editar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-396 | Funcionarios | Funcionarios | AtualizarButton | Atualizar | Button | Sim | PASS | click seguro |
| BTN-397 | Funcionarios | Funcionarios | NovoFuncionarioButton | Novo funcionario | Button | Enumerado | PASS | descoberta+identidade |
| BTN-398 | Funcionarios | Funcionarios | GerenciarPerfisButton | Gerenciar perfis | Button | Enumerado | PASS | descoberta+identidade |
| BTN-399 | Funcionarios | Funcionarios | ConfigurarPermissoesButton | Configurar permissoes | Button | Enumerado | PASS | descoberta+identidade |
| BTN-400 | Funcionarios | Funcionarios | (sem Name) | Limpar busca | Button | Sim | PASS | click seguro |
| BTN-401 | Funcionarios | Funcionarios | (sem Name) | Ver | Button | Enumerado | PASS | descoberta+identidade |
| BTN-402 | Funcionarios | Funcionarios | (sem Name) | Editar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-403 | Funcionarios | Funcionarios | (sem Name) | Inativar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-404 | Funcionarios | Funcionarios | (sem Name) | Ver | Button | Enumerado | PASS | descoberta+identidade |
| BTN-405 | Funcionarios | Funcionarios | (sem Name) | Editar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-406 | Funcionarios | Funcionarios | (sem Name) | Inativar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-407 | Funcionarios | Funcionarios | (sem Name) | Ver | Button | Enumerado | PASS | descoberta+identidade |
| BTN-408 | Funcionarios | Funcionarios | (sem Name) | Editar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-409 | Funcionarios | Funcionarios | (sem Name) | Inativar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-410 | Funcionarios | Funcionarios | (sem Name) | Ver | Button | Enumerado | PASS | descoberta+identidade |
| BTN-411 | Funcionarios | Funcionarios | (sem Name) | Editar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-412 | Funcionarios | Funcionarios | (sem Name) | Inativar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-413 | Funcionarios | Funcionarios | (sem Name) | Ver | Button | Enumerado | PASS | descoberta+identidade |
| BTN-414 | Funcionarios | Funcionarios | (sem Name) | Editar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-415 | Funcionarios | Funcionarios | (sem Name) | Inativar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-416 | Funcionarios | Funcionarios | (sem Name) | Ver | Button | Enumerado | PASS | descoberta+identidade |
| BTN-417 | Funcionarios | Funcionarios | (sem Name) | Editar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-418 | Funcionarios | Funcionarios | (sem Name) | Inativar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-419 | Funcionarios | Funcionarios | RedefinirSenhaButton | Redefinir senha | Button | Enumerado | PASS | descoberta+identidade |
| BTN-420 | Funcionarios | Funcionarios | BloquearFuncionarioButton | Bloquear | Button | Enumerado | PASS | descoberta+identidade |
| BTN-421 | ImportarNFe | ImportarNFe | NovaImportacaoButton | Nova importacao XML | Button | Enumerado | PASS | descoberta+identidade |
| BTN-422 | ImportarNFe | ImportarNFe | AtualizarHistoricoButton | Atualizar historico | Button | Sim | PASS | click seguro |
| BTN-423 | ImportarNFe | ImportarNFe | DesfazerProdutosImportacaoButton | Desfazer produtos | Button | Enumerado | PASS | descoberta+identidade |
| BTN-424 | ImportarNFe | ImportarNFe | ExcluirImportacaoSelecionadaButton | Excluir XML selecionado | Destructive | Dialog only | DESTRUCTIVE — DIALOG VERIFIED | nao executado real |
| BTN-425 | ImportarNFe | ImportarNFe | AbrirPastaXmlsButton | Abrir pasta de XMLs | Button | Enumerado | PASS | descoberta+identidade |
| BTN-426 | ImportarNFe | ImportarNFe | PART_Button | Mostrar Calendário | Button | Enumerado | PASS | descoberta+identidade |
| BTN-427 | ImportarNFe | ImportarNFe | PART_Button | Mostrar Calendário | Button | Enumerado | PASS | descoberta+identidade |
| BTN-428 | ImportarNFe | ImportarNFe | LimparFiltrosButton | Limpar filtros | Button | Sim | PASS | click seguro |
| BTN-429 | ImportarNFe | ImportarNFe | (sem Name) | Ver | Button | Enumerado | PASS | descoberta+identidade |
| BTN-430 | ImportarNFe | ImportarNFe | (sem Name) | Produtos | Button | Enumerado | PASS | descoberta+identidade |
| BTN-431 | ImportarNFe | ImportarNFe | (sem Name) | Reprocessar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-432 | OficinaKanban | OficinaKanban | AtualizarKanbanButton | Atualizar | Button | Sim | PASS | click seguro |
| BTN-433 | OficinaKanban | OficinaKanban | AbrirOsSelecionadaButton | Abrir OS | Button | Enumerado | PASS | descoberta+identidade |
| BTN-434 | OficinaKanban | OficinaKanban | (sem Name) | Selecionar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-435 | OficinaKanban | OficinaKanban | (sem Name) | Avancar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-436 | OficinaKanban | OficinaKanban | (sem Name) | WhatsApp | Button | Enumerado | PASS | descoberta+identidade |
| BTN-437 | OficinaKanban | OficinaKanban | (sem Name) | Selecionar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-438 | OficinaKanban | OficinaKanban | (sem Name) | Avancar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-439 | OficinaKanban | OficinaKanban | (sem Name) | WhatsApp | Button | Enumerado | PASS | descoberta+identidade |
| BTN-440 | OficinaKanban | OficinaKanban | (sem Name) | Selecionar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-441 | OficinaKanban | OficinaKanban | (sem Name) | Avancar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-442 | OficinaKanban | OficinaKanban | (sem Name) | WhatsApp | Button | Enumerado | PASS | descoberta+identidade |
| BTN-443 | OficinaKanban | OficinaKanban | (sem Name) | Alterar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-444 | OficinaKanban | OficinaKanban | (sem Name) | Copiar mensagem sugerida | Button | Enumerado | PASS | descoberta+identidade |
| BTN-445 | Orcamentos | Orcamentos | (sem Name) | Novo orcamento | Button | Enumerado | PASS | descoberta+identidade |
| BTN-446 | Orcamentos | Orcamentos | (sem Name) | Duplicar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-447 | Orcamentos | Orcamentos | ExportarPdfOrcamentoButton | Exportar PDF | Button | Enumerado | PASS | descoberta+identidade |
| BTN-448 | Orcamentos | Orcamentos | (sem Name) | Imprimir | Button | Enumerado | PASS | descoberta+identidade |
| BTN-449 | Orcamentos | Orcamentos | WhatsAppOrcamentoButton | WhatsApp | Button | Enumerado | PASS | descoberta+identidade |
| BTN-450 | Orcamentos | Orcamentos | (sem Name) | E-mail | Button | Enumerado | PASS | descoberta+identidade |
| BTN-451 | Orcamentos | Orcamentos | (sem Name) | Aprovar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-452 | Orcamentos | Orcamentos | (sem Name) | Converter venda | Button | Enumerado | PASS | descoberta+identidade |
| BTN-453 | Orcamentos | Orcamentos | (sem Name) | Converter OS | Button | Enumerado | PASS | descoberta+identidade |
| BTN-454 | Orcamentos | Orcamentos | (sem Name) | Salvar rascunho | Button | Enumerado | PASS | descoberta+identidade |
| BTN-455 | Orcamentos | Orcamentos | (sem Name) | Excluir | Destructive | Dialog only | DESTRUCTIVE — DIALOG VERIFIED | nao executado real |
| BTN-456 | Orcamentos | Orcamentos | (sem Name) | Carteira completa | Button | Enumerado | PASS | descoberta+identidade |
| BTN-457 | Orcamentos | Orcamentos | (sem Name) | Ficha do cliente | Button | Enumerado | PASS | descoberta+identidade |
| BTN-458 | Orcamentos | Orcamentos | (sem Name) | Historico | Button | Enumerado | PASS | descoberta+identidade |
| BTN-459 | Orcamentos | Orcamentos | (sem Name) | Ver todos | Button | Enumerado | PASS | descoberta+identidade |
| BTN-460 | OrdensServico | OrdensServico | (sem Name) | Nova OS | Button | Enumerado | PASS | descoberta+identidade |
| BTN-461 | OrdensServico | OrdensServico | (sem Name) | Editar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-462 | OrdensServico | OrdensServico | ExcluirOsButton | Excluir | Destructive | Dialog only | DESTRUCTIVE — DIALOG VERIFIED | nao executado real |
| BTN-463 | OrdensServico | OrdensServico | (sem Name) | Aprovar cliente | Button | Enumerado | PASS | descoberta+identidade |
| BTN-464 | OrdensServico | OrdensServico | (sem Name) | Aguardar peca | Button | Enumerado | PASS | descoberta+identidade |
| BTN-465 | OrdensServico | OrdensServico | AvancarStatusButton | Avancar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-466 | OrdensServico | OrdensServico | GerarFinanceiroButton | Gerar financeiro | Button | Enumerado | PASS | descoberta+identidade |
| BTN-467 | OrdensServico | OrdensServico | EnviarClienteButton | Enviar cliente | Button | Enumerado | PASS | descoberta+identidade |
| BTN-468 | OrdensServico | OrdensServico | ImprimirOsButton | Imprimir | Button | Enumerado | PASS | descoberta+identidade |
| BTN-469 | OrdensServico | OrdensServico | (sem Name) | Atualizar | Button | Sim | PASS | click seguro |
| BTN-470 | OrdensServico | OrdensServico | (sem Name) | Copiar resumo | Button | Enumerado | PASS | descoberta+identidade |
| BTN-471 | OrdensServico | OrdensServico | (sem Name) | Histórico do cliente | Button | Enumerado | PASS | descoberta+identidade |
| BTN-472 | PDV | PDV | BuscarProdutoButton | Buscar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-473 | PDV | PDV | AbrirSelecionarProdutoButton | Produtos | Button | Enumerado | PASS | descoberta+identidade |
| BTN-474 | PDV | PDV | (sem Name) | ... | Button | Enumerado | PASS | descoberta+identidade |
| BTN-475 | PDV | PDV | AbrirSelecionarClienteButton | Selecionar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-476 | PDV | PDV | AplicarDescontoButton | Aplicar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-477 | PDV | PDV | PagamentoButton | F6 FINALIZAR VENDA | Button | Enumerado | PASS | descoberta+identidade |
| BTN-478 | PDV | PDV | (sem Name) | F7 Cancelar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-479 | PDV | PDV | SuspenderVendaButton | F9 Suspender | Button | Enumerado | PASS | descoberta+identidade |
| BTN-480 | PDV | PDV | RetomarVendaSuspensaButton | F10 Retomar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-481 | PDV | PDV | DinheiroButton | Dinheiro | Button | Enumerado | PASS | descoberta+identidade |
| BTN-482 | PDV | PDV | PixButton | PIX | Button | Enumerado | PASS | descoberta+identidade |
| BTN-483 | PDV | PDV | DebitoButton | Débito | Button | Enumerado | PASS | descoberta+identidade |
| BTN-484 | PDV | PDV | CreditoButton | Crédito | Button | Enumerado | PASS | descoberta+identidade |
| BTN-485 | PDV | PDV | MistoButton | Misto | Button | Enumerado | PASS | descoberta+identidade |
| BTN-486 | PDV | PDV | (sem Name) | Abrir caixa | Button | Enumerado | PASS | descoberta+identidade |
| BTN-487 | PDV | PDV | (sem Name) | Suprimento | Button | Enumerado | PASS | descoberta+identidade |
| BTN-488 | PDV | PDV | (sem Name) | Sangria | Button | Enumerado | PASS | descoberta+identidade |
| BTN-489 | PDV | PDV | (sem Name) | Fechar caixa | Button | Enumerado | PASS | descoberta+identidade |
| BTN-490 | PDV | PDV | ReimprimirUltimaVendaButton | Reimprimir | Button | Enumerado | PASS | descoberta+identidade |
| BTN-491 | PDV | PDV | CancelarUltimaVendaConcluidaButton | Cancelar concluída | Button | Enumerado | PASS | descoberta+identidade |
| BTN-492 | Relatorios | Relatorios | (sem Name) | Atualizar | Button | Sim | PASS | click seguro |
| BTN-493 | Relatorios | Relatorios | (sem Name) | PDF | Button | Enumerado | PASS | descoberta+identidade |
| BTN-494 | Relatorios | Relatorios | (sem Name) | Excel | Button | Enumerado | PASS | descoberta+identidade |
| BTN-495 | Relatorios | Relatorios | (sem Name) | Evidencias | Button | Enumerado | PASS | descoberta+identidade |
| BTN-496 | Relatorios | Relatorios | (sem Name) | Imprimir | Button | Enumerado | PASS | descoberta+identidade |
| BTN-497 | Relatorios | Relatorios | (sem Name) | Favoritos | Button | Enumerado | PASS | descoberta+identidade |
| BTN-498 | Relatorios | Relatorios | (sem Name) | Salvar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-499 | Relatorios | Relatorios | (sem Name) | Executivo | Button | Enumerado | PASS | descoberta+identidade |
| BTN-500 | Relatorios | Relatorios | (sem Name) | Tela cheia | Button | Enumerado | PASS | descoberta+identidade |
| BTN-501 | Relatorios | Relatorios | (sem Name) | Hoje | Button | Enumerado | PASS | descoberta+identidade |
| BTN-502 | Relatorios | Relatorios | (sem Name) | Ontem | Button | Enumerado | PASS | descoberta+identidade |
| BTN-503 | Relatorios | Relatorios | (sem Name) | 7 dias | Button | Enumerado | PASS | descoberta+identidade |
| BTN-504 | Relatorios | Relatorios | (sem Name) | 30 dias | Button | Enumerado | PASS | descoberta+identidade |
| BTN-505 | Relatorios | Relatorios | (sem Name) | Mês atual | Button | Enumerado | PASS | descoberta+identidade |
| BTN-506 | Relatorios | Relatorios | (sem Name) | Mês anterior | Button | Enumerado | PASS | descoberta+identidade |
| BTN-507 | Relatorios | Relatorios | PART_Button | Mostrar Calendário | Button | Enumerado | PASS | descoberta+identidade |
| BTN-508 | Relatorios | Relatorios | PART_Button | Mostrar Calendário | Button | Enumerado | PASS | descoberta+identidade |
| BTN-509 | Relatorios | Relatorios | (sem Name) | Aplicar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-510 | Relatorios | Relatorios | (sem Name) | Limpar | Button | Sim | PASS | click seguro |
| BTN-511 | Relatorios | Relatorios | (sem Name) | Consultar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-512 | Relatorios | Relatorios | (sem Name) | Anterior | Button | Disabled | CONDITIONAL | disabled |
| BTN-513 | Relatorios | Relatorios | (sem Name) | Próxima | Button | Enumerado | PASS | descoberta+identidade |
| BTN-514 | Veiculos | Veiculos | NovoVeiculoButton | Novo veiculo | Button | Enumerado | PASS | descoberta+identidade |
| BTN-515 | Veiculos | Veiculos | ExportarVeiculosButton | Exportar | Button | Enumerado | PASS | descoberta+identidade |
| BTN-516 | Veiculos | Veiculos | LimparFiltrosVeiculosButton | Limpar filtros | Button | Sim | PASS | click seguro |
| BTN-517 | Veiculos | Veiculos | (sem Name) | Ver | Button | Enumerado | PASS | descoberta+identidade |
| BTN-518 | Veiculos | Veiculos | (sem Name) | Edit | Button | Enumerado | PASS | descoberta+identidade |
| BTN-519 | Veiculos | Veiculos | (sem Name) | Del | Button | Enumerado | PASS | descoberta+identidade |
| BTN-520 | Veiculos | Veiculos | (sem Name) | Ver | Button | Enumerado | PASS | descoberta+identidade |
| BTN-521 | Veiculos | Veiculos | (sem Name) | Edit | Button | Enumerado | PASS | descoberta+identidade |
| BTN-522 | Veiculos | Veiculos | (sem Name) | Del | Button | Enumerado | PASS | descoberta+identidade |
| BTN-523 | Veiculos | Veiculos | (sem Name) | Ver | Button | Enumerado | PASS | descoberta+identidade |
| BTN-524 | Veiculos | Veiculos | (sem Name) | Edit | Button | Enumerado | PASS | descoberta+identidade |
| BTN-525 | Veiculos | Veiculos | (sem Name) | Del | Button | Enumerado | PASS | descoberta+identidade |
| A11Y-526 | Funcionarios | Funcionarios | SearchTextBox | Focus+Tab | Accessibility | Sim | PASS | teclado |
| A11Y-527 | PDV | PDV | IconButtons | ToolTip/AutomationName | Accessibility | Sim | PASS | identidade |
| A11Y-528 | Calendar | CalendarItem | Header Dark | Native template | Accessibility | Nao seguro | KNOWN LIMITATION | CalendarItem bloqueado |
| THEME-529 | Geral | XAML | Hex | Hardcoded colors | Audit | N/A | KNOWN LIMITATION | hex=345 |
| THEME-530 | Geral | C# | FromRgb/FromArgb | Hardcoded colors | Audit | N/A | KNOWN LIMITATION | rgb=71 |
| THEME-531 | Calendar | CalendarItem | Dark header | Native | Audit | Nao | KNOWN LIMITATION | template bloqueado |
| ORPHAN-532 | Funcionarios | FuncionariosViewModel | DI+Tests | Retain | Code | N/A | ORPHAN CANDIDATE — RETAINED | ServiceExtensions+UITests |
| REL-533 | Shell | MainWindow | HeaderSystemInfo | Version display | Release | Sim | PASS | 1.0.0-rc.1 |
| REL-534 | Branding | icon.ico | ApplicationIcon | App icon | Release | Sim | PASS | csproj ApplicationIcon |
| REL-535 | Phase | Indicador de fase | N/A | Phase badge | Release | Nao | NOT FOUND | mecanismo inexistente no codigo |
| REL-536 | NFe | Emissao real | SEFAZ | Transmitir | Fiscal | Nao | NOT TESTABLE — requires production integration | nunca emitir real |
| PERF-537 | Shell | MainWindow | LongRun | 5 ciclos | Performance | Sim | PASS | 110,1s / 75 nav |

## Metricas
- Linhas na matriz: 537
- Modulos canonicos: 17
- Cobertura 100% de TODOS os botoes com execucao real: **NAO reivindicada**
- Enumeracao/auditoria de botoes nos modulos navegaveis: **SIM**
