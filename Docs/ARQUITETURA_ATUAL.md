# Arquitetura Atual - PrimoAutoEletrica

Gerado em: 2026-06-11
Fonte: inventario local do repositorio apos validacao UI Smoke 158/158 e Workflow 42/42.

## Pastas Existentes

Pastas principais do projeto WPF:

- `PrimoAutoEletrica/Converters`: converters XAML e auxiliares de binding.
- `PrimoAutoEletrica/Data`: arquivos estaticos, XML de teste e repositorios auxiliares de importacao.
- `PrimoAutoEletrica/Helpers`: validadores, sanitizacao de texto e helpers de janela.
- `PrimoAutoEletrica/Models`: entidades de dominio como Cliente, Veiculo, Produto, Orcamento, OrdemServico, Venda e Perfil.
- `PrimoAutoEletrica/Repositories`: repositorios de dados de dominio.
- `PrimoAutoEletrica/Services`: servicos de negocio, banco, validacao, backup, auditoria, tema, navegacao e automacao.
- `PrimoAutoEletrica/Services/Catalogo`: importacao e normalizacao de catalogos de pecas.
- `PrimoAutoEletrica/Services/DatabaseProviders`: plano de provider, SQLite atual e esqueleto SQL Server.
- `PrimoAutoEletrica/Themes`: dicionarios XAML de cores, botoes, cards, inputs, tabelas, tipografia e sombras.
- `PrimoAutoEletrica/UserControls`: modulos principais carregados dentro da shell.
- `PrimoAutoEletrica/ViewModels`: estado e comandos de telas mais complexas.
- `PrimoAutoEletrica/Views`: janelas, dialogs e telas auxiliares.
- `Scripts`: automacao de validacao, empacotamento e diagnostico.
- `Tests`: testes automatizados de unidade, banco, permissao e tema.
- `Docs`: documentacao tecnica e operacional.

Pastas de artefato que nao devem ser versionadas ou empacotadas como fonte:

- `bin/`
- `obj/`
- `.vs/`
- `Artifacts/`
- `PackageClean/`
- `TestResults/`
- `Logs/`
- `Backups/`

## Principais Telas

Shell e entrada:

- `App.xaml.cs`: bootstrap, modo normal, modo smoke, modo workflow, infraestrutura global e tratamento de excecoes.
- `MainWindow.xaml/cs`: shell principal, menu lateral, busca global, notificacoes, permissoes e navegacao.
- `Views/LoginWindow.xaml/cs`: autenticacao inicial.
- `Views/PrimeiraExecucaoWindow.xaml/cs`: configuracao inicial.

Modulos carregados como `UserControl`:

- `DashboardControl`: indicadores e visao executiva.
- `ClientesControl`: cadastro e consulta de clientes.
- `VeiculosControl`: cadastro e consulta de veiculos.
- `OrcamentosControl`: propostas, carrinho e conversao.
- `OrdensServicoControl`: fluxo operacional de OS.
- `PDVControl`: venda rapida, carrinho, cliente e pagamento.
- `EstoqueControl`: produtos, movimentos e ajustes.
- `CatalogoPecasControl`: importacao/revisao de catalogos.
- `ImportarNFeControl`: importacao fiscal XML e historico.
- `FinanceiroControl`: caixa, receitas, despesas e indicadores.
- `FornecedoresControl`: fornecedores e compras.
- `FuncionariosControl`: equipe, perfis e permissoes.
- `AgendamentosControl`: agenda da oficina.
- `RelatoriosControl`: relatorios gerenciais.

Janelas auxiliares relevantes:

- Clientes: `NovoClienteWindow`, `EditarClienteWindow`, `VisualizarClienteWindow`, `HistoricoClienteWindow`.
- Veiculos: `NovoVeiculoWindow`, `VisualizarVeiculoWindow`.
- Orcamentos: `NovoOrcamentoWindow`, `SelecionarOrcamentoWindow`, `OrcamentosDashboardControl`, `OrcamentosView`.
- OS: `OrdemServicoWindow`.
- PDV: `SelecionarClientePDVWindow`, `SelecionarProdutoPDVWindow`, `SelecionarVendaWindow`, `PagamentoMistoWindow`, `OperacaoCaixaWindow`.
- Estoque/catalogo: `NovoProdutoWindow`, `EditarProdutoWindow`, `AjusteEstoqueWindow`, `HistoricoEstoqueWindow`, `ImportarCatalogoPecasWindow`, `RevisarCatalogoPecaWindow`.
- Fornecedores: `NovoFornecedorWindow`, `EditarFornecedorWindow`, `VisualizarFornecedorWindow`, `AdicionarFornecedorDialog`.
- Funcionarios/permissoes: `NovoFuncionarioWindow`, `EditarFuncionarioWindow`, `GerenciarPerfisWindow`, `NovoPerfilWindow`, `ConfigurarPermissoesWindow`, `UsuariosOnlineWindow`.
- Agendamentos/configuracao: `NovoAgendamentoPremiumWindow`, `ConfiguracoesSistemaWindow`, `ConfirmacaoCriticaWindow`.

## Principais Servicos

Infraestrutura:

- `AppRuntimeConfiguration`: interpreta argumentos e caminhos de runtime.
- `LoggerService`: logs operacionais.
- `AppSessionService`, `UserSessionService`, `SessionInactivityService`: sessao, usuario ativo e timeout.
- `AuditLogService`: auditoria de eventos e erros.
- `ShellNotificationService`: notificacoes da shell.

Navegacao e seguranca:

- `NavigationService`: mapeia nomes de modulos para `UserControl`, aplica permissao e cache LRU.
- `PermissionService`: regras de perfil e permissao.
- `PasswordHasherService`: hash/senha.
- `CriticalActionDialogService`: confirmacao de acoes sensiveis.

Banco e dados:

- `DatabaseService` e partials `AccessControl`, `Audit`, `Estoque`, `Fornecedores`, `Funcionarios`, `Migrations`, `OrdensServico`.
- `DatabaseBackupService`, `DatabaseHealthService`, `DatabaseConnectionSettingsService`, `DatabaseProviderPlanService`.
- `AgendamentoDatabaseService`, `FinanceiroDatabaseService`, `OrcamentoDatabaseService`, `RelatorioDatabaseService`.
- Repositories: `ClienteRepository`, `ProdutoRepository`, `OrdemServicoRepository`, `VendaRepository`, entre outros.

Operacao e modulos:

- `CaixaService`, `VendaService`, `EstoqueOperationalService`, `FornecedorOperationalService`, `FuncionarioOperationalService`.
- `ProdutoImportacaoService`, `NFeService`, `XmlProdutoParser`.
- `OrcamentoPdfService`, `VendaComprovanteService`, `ProdutoEtiquetaService`, `RelatorioExportService`.
- `ClienteMediaService`, `ProdutoMediaService`, `VeiculoMediaService`, `OrdemServicoMediaService`.

Validacao e automacao:

- `UiSmokeTestService`: automacao de tela ponta a ponta.
- `OperationalWorkflowTestService`: validacao de workflow operacional sem UI pesada.
- `ThemeResourceValidationService`, `PermissionProfileTestService`, `DatabasePersistenceTestService`.
- `QualityReportService`, `ScreenshotCaptureService`.

## Fluxo de Navegacao

- `App.OnStartup` aplica tema, inicializa infraestrutura e decide entre modo normal, `--smoke-test` e `--workflow-test`.
- No modo normal, `LoginWindow` autentica e abre `MainWindow`.
- `MainWindow` cria `PermissionService`, `NavigationService`, `ThemeService` e `UserSessionService`.
- `NavigationService` resolve o nome do modulo para um `UserControl`, valida permissao por `PermissionService`, usa cache LRU e emite eventos.
- `MainWindow` reage aos eventos para trocar `MainContent`, atualizar botao ativo, historico e estado visual.
- A busca global de `MainWindow` permite abrir modulos ou registros principais com base nas permissoes do usuario.

## Fluxo de Banco

- `App.Database` cria um `DatabaseService` unico por infraestrutura.
- `DatabaseService` resolve pasta de dados via `AppRuntimeConfiguration` e `DatabaseConnectionSettingsService`.
- O runtime atual usa SQLite com `WAL`, `synchronous=NORMAL`, pool e timeout configurado.
- `EnsureDatabaseInitialized` protege a criacao por lock estatico e evita reinicializar o mesmo arquivo.
- O schema base ainda esta parcialmente concentrado em `DatabaseService.cs`, com dominios ja separados em partials.
- Repositories e services de dominio usam `DatabaseService.GetConnection()` ou servicos especializados para CRUD e consultas.
- Backups automaticos rodam no startup diario e no encerramento, exceto em modo automatizado.

## Fluxo de Login e Permissao

- `LoginWindow` autentica funcionario usando dados persistidos e hash de senha.
- `TrocarSenhaObrigatoriaWindow` bloqueia avanço quando o usuário tem `ExigirTrocaSenha = 1`.
- `AppSessionService` guarda contexto da sessao atual.
- `MainWindow` recebe `Funcionario` autenticado e monta `PermissionService`.
- Menus sao habilitados/ocultados de acordo com modulos permitidos e codigos especificos como `SISTEMA_CONFIGURAR`.
- `NavigationService` reforca permissao em cada navegacao para evitar acesso via chamada direta.
- `UserSessionService` registra sessao, encerramento e timeout de inatividade.
- A matriz oficial de perfis, acoes e eventos de auditoria fica em `Docs/MATRIZ_PERMISSOES_SEGURANCA.md`.

## Fluxo de Tema

- `ThemeService` aplica o tema salvo antes da abertura de janelas e tambem dentro da `MainWindow`.
- `App.xaml` agrega os dicionarios em `Themes/`.
- `Colors.Light.xaml` e `Colors.Dark.xaml` devem manter paridade de chaves.
- `Buttons.xaml`, `Cards.xaml`, `Inputs.xaml`, `Tables.xaml`, `Typography.xaml`, `Sidebar.xaml` e `Shadows.xaml` concentram estilos reutilizaveis.
- `ThemeXamlTests` valida XML, recursos resolvidos, duplicidade de chaves e paridade claro/escuro.

## Testes Existentes

Scripts:

- `Scripts/Run-FullValidation.ps1`: restore, build, testes, smoke, workflow e relatorio consolidado.
- `Scripts/Run-UiSmoke.ps1`: executa app real com `--smoke-test`.
- `Scripts/Run-WorkflowTest.ps1`: executa app real com `--workflow-test`.
- `Scripts/Get-TestReport.ps1`: resume a ultima validacao.
- `Scripts/Create-CleanPackage.ps1`: gera ZIP limpo e opcionalmente compila apos extrair.

Suites:

- `Tests/PrimoAutoEletrica.Tests/ThemeXamlTests.cs`: validacao de temas.
- `Tests/PrimoAutoEletrica.Tests/PermissionTests.cs`: perfis e permissoes.
- `Tests/PrimoAutoEletrica.Tests/DatabasePersistenceTests.cs`: persistencia e integridade basica.
- Testes de repositorios e servicos em `Tests/PrimoAutoEletrica/Tests`.

Validacao estavel atual:

- UI Smoke: 158/158 checks.
- Workflow: 42/42 checks.
- Pacote limpo extraido: build aprovado.

## Arquivos Grandes Demais

Arquivos que devem ser alterados com cuidado e preferencialmente em etapas:

- `PrimoAutoEletrica/Services/UiSmokeTestService.cs`: cerca de 7.278 linhas; deve ser dividido em partials por dominio.
- `PrimoAutoEletrica/Services/DatabaseService.cs`: cerca de 2.223 linhas; schema base ainda grande, embora ja existam partials por dominio.
- `PrimoAutoEletrica/ViewModels/AgendamentosViewModel.cs`: cerca de 2.116 linhas.
- `PrimoAutoEletrica/Services/RelatorioDatabaseService.cs`: cerca de 1.997 linhas.
- `PrimoAutoEletrica/Services/OperationalWorkflowTestService.cs`: cerca de 1.716 linhas.
- `PrimoAutoEletrica/Services/DatabaseService.OrdensServico.cs`: cerca de 1.629 linhas.
- `PrimoAutoEletrica/UserControls/PDVControl.xaml.cs`: cerca de 1.607 linhas.
- `PrimoAutoEletrica/ViewModels/RelatoriosViewModel.cs`: cerca de 1.580 linhas.
- `PrimoAutoEletrica/Services/FinanceiroDatabaseService.cs`: cerca de 1.514 linhas.
- `PrimoAutoEletrica/UserControls/OrdensServicoControl.xaml.cs`: cerca de 1.342 linhas.

## Arquivos Perigosos de Alterar

- `App.xaml.cs`: afeta startup, modo teste, infraestrutura global, backup e tratamento de erro.
- `App.xaml` e `Themes/*.xaml`: qualquer chave removida pode quebrar varias telas em runtime.
- `MainWindow.xaml/cs`: shell, permissoes, navegacao, busca e notificacoes.
- `NavigationService.cs`: ponte entre permissoes e carregamento de modulos.
- `PermissionService.cs`: regra de acesso de todo o sistema.
- `DatabaseService.cs` e partials: schema, migracoes e conexoes SQLite.
- `ImportarNFeControl.xaml.cs` e `ImportacaoRepository.cs`: fluxo fiscal e historico de importacao.
- `PDVControl.xaml.cs`: venda, cliente, carrinho e fechamento.
- `UiSmokeTestService.cs`: validacao ponta a ponta; refatorar sem mudar asserts e fixtures.
- `OperationalWorkflowTestService.cs`: validacao operacional usada no gate completo.

## Direcao de Refatoracao Segura

- Dividir primeiro arquivos gigantes em `partial class` sem alterar comportamento.
- Depois mover regras de negocio pesadas de code-behind para services ou ViewModels, uma tela por vez.
- Preservar API publica usada pelas telas ate que todos os pontos sejam atualizados.
- Rodar `dotnet build`, testes dedicados e smoke/workflow apos cada bloco de refatoracao.
- Evitar refatoracao simultanea de UI, banco e testes na mesma alteracao.
