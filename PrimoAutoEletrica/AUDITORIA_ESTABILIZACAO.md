# Auditoria de Estabilizacao do ERP

## Estado atual

- Build principal da solucao: compila com sucesso.
- Warnings atuais de build: nenhum.
- Estrutura atual relevante:
  - `58` UserControls
  - `55` Views
  - `22` Services
  - `24` Models
  - `14` temas antes desta rodada

## Correcoes aplicadas nesta rodada

- Corrigido o binding do painel de detalhes de `Agendamentos`:
  - `SelectedAgendamento` -> `AgendamentoSelecionado`
- Corrigido o ciclo de vida de telas cacheadas:
  - `AgendamentosControl`
  - `FinanceiroControl`
  - `PDVControl`
- Corrigidos problemas de cache e historico no `NavigationService`:
  - evitava duplicacao de historico
  - `NavigateBack` deixa de reempilhar o modulo anterior
  - limpeza e remocao de cache agora atualizam a ordem LRU
- Endurecida a inicializacao global da aplicacao:
  - tratamento de excecoes nao tratadas
  - logs de startup e falhas criticas
- Reforcada a seguranca do login:
  - senha lembrada localmente passa a usar `ProtectedData` do Windows
  - falhas de leitura e escrita local deixam de ser silenciosas
- Reduzido o custo estrutural do acesso ao banco:
  - `DatabaseService` deixa de reexecutar inicializacao completa do schema a cada nova instancia
  - inicializacao do SQLite passa a ocorrer uma vez por processo e por base
  - erros de leitura e importacao passam a ser registrados no `LoggerService`
- Centralizado o acesso ao banco na aplicacao:
  - modulos ativos deixam de instanciar `new DatabaseService()` de forma dispersa
  - `App.Database` passa a ser a referencia compartilhada do ERP
- Saneado o pipeline de importacao de NF-e:
  - `ProdutoImportacaoService` deixa de depender de logs em console
  - `XmlProdutoParser` foi regravado em formato limpo, com logging centralizado e sem trechos corrompidos por encoding
  - nao restam `Console.WriteLine` em `Services`, `Views` e `UserControls`
- Contexto global de sessao introduzido:
  - usuario autenticado deixa de ser hardcoded em modulos principais
  - `Agendamentos`, `Relatorios`, `Financeiro`, `Orcamentos`, `PDV` e `Importar NF-e` passam a consumir sessao real
  - logout encerra explicitamente a sessao atual
- Profissionalizada a shell principal:
  - sidebar premium
  - cabecalho operacional
  - destaque visual do modulo ativo
- Criada a base inicial do design system faltante:
  - `Themes/Dashboard.xaml`
  - `Themes/Tables.xaml`
  - `Themes/Modal.xaml`
- Eliminada a dependencia legada de graficos:
  - `Financeiro` e `Relatorios` foram reestruturados sem `LiveCharts.Wpf`
  - build deixa de carregar pacote incompativel com `net9.0-windows`
- Fortalecida a rastreabilidade de erros:
  - nao restam `catch` silenciosos em `Services`, `ViewModels`, `Views` e `UserControls`
- Fechada a persistencia operacional do PDV:
  - criadas tabelas `Vendas` e `VendaItens`
  - `VendaService` agora registra venda com transacao
  - baixa de estoque e historico de venda passam a ocorrer no fechamento do PDV
- Fechada a integracao financeira de `Agendamentos`:
  - finalizacao de servico gera conta a receber e movimentacao de entrada sem duplicidade
  - `FinanceiroDatabaseService` ganhou chaves de integracao e indices unicos para deduplicacao
  - migracao de banco legado passa a copiar colunas em comum sem quebrar por diferenca de schema
  - dashboard financeiro deixa de operar sobre apenas `50` movimentacoes recentes
- Profissionalizado o fluxo vivo de `Orcamentos`:
  - paines `Cliente`, `Produtos` e `Carrinho` deixam de usar bindings quebrados e placeholders
  - carrinho passa a editar, remover e duplicar itens com fluxo real
  - cadastro rapido de cliente pode vincular o cliente novo ao orcamento atual
  - conversao de orcamento em venda passa a registrar venda real e conta a receber
- Acoes operacionais de comunicacao em `Agendamentos` deixam de ser ficticias:
  - WhatsApp abre conversa real com mensagem contextual
  - lembrete por e-mail passa a abrir `mailto:` com assunto e corpo prontos
- Busca global conectada ao shell principal:
  - `GlobalSearchControl` deixa de abrir mensagem placeholder
  - selecao de resultado aciona navegacao real na `MainWindow`
  - indice inicial inclui modulos, clientes, veiculos, produtos, OS e agendamentos
- Fechadas as integracoes restantes do checkout de `Agendamentos`:
  - baixa de estoque passa a ser transacional
  - PDV registra venda derivada do agendamento sem baixar estoque duas vezes
  - integracoes de estoque, PDV e orcamentos passam a ter controle de idempotencia por agendamento
  - `VendaService` suporta registro de venda sem movimentar estoque quando o estoque ja foi tratado pelo fluxo operacional
- Persistido o historico operacional de importacao NF-e:
  - `ImportacaoRepository` passa a usar o banco compartilhado da aplicacao
  - importacoes, itens e duplicidade por chave de acesso sao gravados com transacao
  - falhas de importacao tambem geram historico rastreavel quando houver dados suficientes
- Alimentado o historico financeiro do cliente:
  - `HistoricoClienteWindow` deixa de exibir pagamentos vazios por padrao
  - contas a receber do financeiro sao filtradas pelo cliente e exibidas no historico
  - pagamentos quitados entram tambem na timeline do cliente
  - corrigido binding silencioso de `PagamentoCliente.Status`
- Removidos placeholders ativos em cadastro de cliente e novo agendamento:
  - `NovoAgendamentoPremiumWindow` passa a aceitar `NovoAgendamentoPremiumViewModel` injetado
  - assinatura digital de cliente gera termo rastreavel com hash SHA-256
  - caminho da assinatura e documento anexado sao persistidos no cadastro do cliente
  - erros nesses fluxos passam a ser registrados no `LoggerService`
- Profissionalizada a base de perfis e permissoes:
  - banco principal passa a inicializar `Permissoes`, `PerfisAcesso` e `PerfilPermissoes`
  - perfis e permissoes padrao sao semeados de forma idempotente
  - `PermissionService` consulta permissoes persistidas antes de usar fallback
  - `ConfigurarPermissoesWindow` carrega, salva, exclui e exporta permissoes reais
  - `GerenciarPerfisWindow` carrega perfis reais e persiste vinculos de permissoes por perfil
  - cadastro e edicao de funcionarios passam a listar perfis ativos do banco
- Otimizado o PDF de `OrdensServico`:
  - remocao do XPS temporario intermediario
  - geracao direta em PDF com PdfSharpCore
  - quebra de texto e paginacao basica para reduzir cortes em OS maiores
- Implementada auditoria persistida enterprise:
  - tabela `AuditLogs` com indices por data, categoria/acao e entidade
  - `AuditLogService` tolerante a falhas para nao derrubar o ERP se a auditoria falhar
  - registros para startup, excecoes globais, login, falha de login, logout e navegacao
  - registros para abertura/importacao/cancelamento/falha de NF-e
  - registros para vendas PDV, cancelamento de venda, financeiro, estoque/produtos e exclusao de OS
  - registros para cancelamento, remocao, reagendamento, check-in e check-out de agendamentos
- Reforcada a seguranca de senhas de funcionarios:
  - senhas novas passam a ser gravadas com PBKDF2/SHA-256, salt aleatorio e comparacao em tempo fixo
  - senhas legadas em texto simples continuam funcionando e sao migradas para hash no primeiro login valido
  - tela de recuperacao deixa de expor senhas padrao e registra tentativa em auditoria
- Adicionada rotina de backup do banco local:
  - `DatabaseBackupService` cria backup automatico diario no startup
  - executa checkpoint WAL antes da copia
  - aplica retencao simples dos backups automaticos
  - sucesso e falha de backup entram na auditoria
- Aplicada otimizacao global de listas:
  - `PremiumDataGrid` passa a ativar virtualizacao de linhas e colunas
  - `PremiumListView` passa a usar `VirtualizingStackPanel` por padrao
  - `CanContentScroll` e reciclagem de containers reduzem custo em telas com muitos registros
- Integrada a auditoria real aos relatorios:
  - grade `Auditoria completa` passa a priorizar a tabela enterprise `AuditLogs`
  - timeline operacional passa a receber eventos reais de auditoria
  - tabela legada `Auditoria` segue como fallback para preservar historico antigo
- Fechados placeholders operacionais ativos:
  - `Financeiro` passa a imprimir relatorio mensal real com resumo, contas a pagar e contas a receber
  - exclusao de funcionario passa a ser inativacao segura, com bloqueio do proprio usuario e do ultimo administrador ativo
  - inativacao de funcionario passa a gerar evento na auditoria
- Corrigida estrutura de `RelatoriosView`:
  - criado code-behind ausente para o `x:Class`
  - removido cabecalho estatico com dados ficticios
  - janela passa a hospedar diretamente o `RelatoriosControl` real
- Reduzido SQL direto em tela:
  - `FuncionariosControl` deixa de montar consulta manual no code-behind
  - carregamento de funcionarios passa a usar `DatabaseService.ObterTodosFuncionarios()`
- Centralizada a persistencia de funcionarios:
  - cadastro passa a usar `DatabaseService.SalvarFuncionario()`
  - edicao passa a usar `DatabaseService.AtualizarFuncionario()`
  - validacao de e-mail duplicado passa para `DatabaseService.EmailFuncionarioExiste()`
  - edicao sem nova senha preserva a senha existente em vez de gravar senha vazia
- Centralizada a criacao de perfis:
  - `NovoPerfilWindow` deixa de executar SQL direto
  - criacao de perfil e vinculos de permissoes passam por `DatabaseService.CriarPerfilAcesso()`
  - validacao de nome duplicado passa para `DatabaseService.NomePerfilExiste()`
  - criacao de perfil passa a ser transacional e auditada
- Centralizada a baixa de estoque de agendamentos:
  - `AgendamentosViewModel` deixa de executar SQL direto no checkout
  - baixa de produtos passa por `DatabaseService.BaixarProdutosDoEstoquePorAgendamento()`
  - operacao segue transacional e agora tambem gera auditoria de estoque
- Implantado versionamento profissional do banco:
  - criada tabela `SchemaMigrations`
  - migrations idempotentes passam a registrar versao, maquina e data de aplicacao
  - adicionados indices operacionais para produtos, clientes, funcionarios, fornecedores, veiculos, vendas, itens e importacoes
  - adicionadas colunas `RowVersion` e `DataUltimaAlteracao` em entidades criticas para preparar concorrencia
- Preparada base de multiusuario no banco:
  - criada tabela `RegistroBloqueios`
  - criado `RegistroBloqueioService` para bloqueio temporario de registros por sessao
  - expiracao automatica evita registros presos indefinidamente
- Melhorada auditoria de alteracoes:
  - `AuditLogs` passa a suportar `ValorAnterior`, `ValorNovo` e `CorrelationId`
  - relatorios de auditoria passam a exibir valores detalhados quando disponiveis
- Evoluida configuracao de conexao:
  - criado `database-settings.json` em `%LOCALAPPDATA%\PrimoAutoEletrica`
  - `DatabaseConnectionSettingsService` centraliza configuracoes atuais SQLite e campos futuros SQL Server
- Evoluido backup/restauracao:
  - criada tabela `DatabaseBackups`
  - backup passa a registrar historico, tamanho e hash SHA-256
  - adicionada verificacao de integridade do backup
  - adicionada rotina de restauracao com backup de seguranca previo
- Adicionada verificacao de saude do banco:
  - `DatabaseHealthService` executa `PRAGMA integrity_check`
  - startup registra quantidade de migrations aplicadas e avisos estruturais
- Corrigida compatibilidade de bancos antigos de permissoes:
  - indices unicos de `Permissoes`, `PerfisAcesso` e `PerfilPermissoes` sao garantidos antes do seed
  - duplicidades legadas sao saneadas para evitar falha em `ON CONFLICT`
- Iniciada separacao real do `DatabaseService` em repositorios:
  - criado `RepositoryRegistry`
  - criado contrato `IProdutoRepository`
  - criado `ProdutoRepository` para CRUD de produtos, importacao em massa e baixa de estoque por agendamento
  - telas, viewmodels e servicos ativos deixam de chamar diretamente os metodos antigos de produto no `DatabaseService`
  - metodos antigos de produto no `DatabaseService` ficam como ponte de compatibilidade apontando para o repositorio
- Avancada separacao de clientes e veiculos:
  - criado contrato `IClienteRepository`
  - criado `ClienteRepository` para CRUD de clientes, veiculos do cliente, veiculos avulsos, resumo de cliente e auditoria basica
  - telas, viewmodels e controles ativos passam a usar `App.Repositories.Clientes`
  - metodos antigos de clientes/veiculos no `DatabaseService` ficam como ponte de compatibilidade
  - historico do cliente permanece reaproveitando o fluxo existente de ordens de servico para reduzir risco nesta etapa
- Corrigidos bloqueios de acesso pos-login e navegacao:
  - `Header.xaml` e `Sidebar.xaml` deixam de depender de recursos que podiam resolver como `DependencyProperty.UnsetValue`
  - menu ativo da `MainWindow` passa a aplicar brushes explicitos em vez de limpar propriedades visuais criticas
  - `RelatorioDatabaseService` passa a aceitar schema atual de `Vendas` (`Total`, `QuantidadeItens`) e schema legado (`ValorTotal`, `ItensQuantidade`)
  - smoke tests validaram abertura da `MainWindow`, construcao de `RelatoriosControl` e navegacao para `Relatorios`
- Consolidada validacao de navegacao dos modulos principais:
  - corrigido `OrcamentosDashboardControl` para herdar o `DataContext` do controle pai em vez de acessar `App.Current.MainWindow`
  - `NavigationService` passa a desempacotar `TargetInvocationException`, exibindo/logando a causa raiz real
  - smoke test de construcao/layout aprovado para Dashboard, Clientes, Veiculos, Orcamentos, OrdensServico, PDV, Estoque, Financeiro, Fornecedores, Funcionarios, Agendamentos e Relatorios
  - build validado novamente com `0` erros e `0` avisos
- Preparada evolucao para SQL Server:
  - `DatabaseConnectionSettings` passa a montar connection string SQL Server
  - adicionados campos de criptografia, certificado e timeout
  - criado `DatabaseProviderPlanService` para avaliar pendencias antes da troca de provider
  - runtime atual segue em SQLite com aviso claro caso o provider seja alterado antes da migracao completa
- Consolidada estabilizacao visual dos dicionarios globais:
  - `Buttons.xaml`, `Cards.xaml`, `DataGrid.xaml`, `Inputs.xaml`, `Typography.xaml`, `Dashboard.xaml`, `Calendar.xaml`, `ListView.xaml`, `Tables.xaml`, `Modal.xaml`, `Sidebar.xaml` e `Shadows.xaml` deixam de depender de brushes que podiam resolver como `UnsetValue` em setters criticos
  - criado alias `PremiumTextBox` para corrigir o `SearchBox` global
  - `MainWindow.xaml` tambem passa a usar cores explicitas em pontos criticos do shell
  - varredura de `StaticResource` nao encontrou recursos ausentes
- Endurecidos converters e tratamento de excecoes:
  - removidos `NotImplementedException` em `ConvertBack`, substituindo por `Binding.DoNothing` quando a conversao reversa nao se aplica
  - removido `catch` silencioso da configuracao de banco, agora com log e preservacao do arquivo invalido
  - repositórios de clientes/produtos passam a registrar contexto antes de repropagar falhas transacionais
- Avancada separacao de fornecedores e funcionarios:
  - criado contrato `IFornecedorRepository`
  - criado `FornecedorRepository` para CRUD, contatos, auditoria e `RowVersion`
  - telas e importacao de NF-e passam a usar `App.Repositories.Fornecedores`
  - metodos antigos de fornecedor no `DatabaseService` ficam como ponte de compatibilidade
  - criado contrato `IFuncionarioRepository`
  - criado `FuncionarioRepository` para listagem, validacao de e-mail, cadastro, atualizacao e inativacao segura
  - telas de funcionarios, agendamentos e OS passam a usar `App.Repositories.Funcionarios` para listagem/CRUD
  - metodos antigos de funcionario no `DatabaseService` ficam como ponte de compatibilidade
- Fortalecida camada de permissoes:
  - fallback de perfis passa a normalizar acentos, espacos e grafias legadas
  - perfis `Tecnico`, `Técnico`, `Estoque`, `Estoquista`, `Caixa`, `Financeiro`, `Gerente` e `Administrador` foram validados
- Removida criacao direta de `DatabaseService` em janelas ativas:
  - `EditarClienteWindow` passa a usar `App.Database`
  - varredura nao encontrou mais `new DatabaseService()` fora da instancia central
- Corrigida camada visual contra textos corrompidos por encoding:
  - criado `UiTextSanitizer` para recuperar mojibake comum em textos de UI
  - criado `TextSanitizerBehavior` aplicado globalmente a `Window` e `UserControl`
  - cards de `Agendamentos`, `Financeiro`, `Orcamentos` e `Relatorios` passam a limpar titulo, comparacao, tendencia, valor textual e icones quebrados
  - filtros e indicadores financeiros passam a comparar `Saida`, `Debito`, `Credito` e formas de pagamento por chave normalizada
  - filtros de agenda e visualizacao passam a normalizar status e nomes exibidos
- Avancada separacao de Ordens de Servico:
  - criado contrato `IOrdemServicoRepository`
  - criado `OrdemServicoRepository` para numeracao, listagem, busca, insercao, atualizacao, exclusao, itens, eventos, baixa/restauracao de estoque e resumo do cliente
  - `MainWindow`, `OrdensServicoControl`, `OrdemServicoWindow` e `VisualizarVeiculoWindow` passam a usar `App.Repositories.OrdensServico`
  - metodos antigos de OS no `DatabaseService` ficam como ponte de compatibilidade para reduzir risco de regressao
- Validacao geral desta rodada:
  - build validado com `0` erros e `0` avisos
  - smoke test aprovado para Dashboard, Clientes, Veiculos, Orcamentos, OrdensServico, PDV, Estoque, Financeiro, Fornecedores, Funcionarios, Agendamentos e Relatorios

## Consolidacao mais recente

- Formalizada a referencia persistente da execucao em `LISTA_OFICIAL_EXECUCAO.md`
- Adicionado `UiSmokeTestService` para validacao automatizada da etapa `1` da lista
- `App.xaml.cs` passa a aceitar `--smoke-test` para rodar a validacao sem abrir a tela de login
- `MainWindow` suprime dialogos de erro durante smoke test para nao travar a automacao
- Primeira execucao validada em `2026-05-22` com `14` checks aprovados e `0` falhas:
  - `MainWindow`
  - `Dashboard`
  - `Clientes`
  - `Veiculos`
  - `Orcamentos`
  - `OrdensServico`
  - `PDV`
  - `Estoque`
  - `Financeiro`
  - `Fornecedores`
  - `Funcionarios`
  - `Agendamentos`
  - `Relatorios`
  - `ImportarNotaWindow`
- Smoke test expandido validado em `2026-05-22` com `58` checks aprovados e `0` falhas:
  - modulos centrais via `NavigationService`
  - janelas com construtor padrao: login, cadastro, visualizacao, orcamentos, PDV, relatorios e apoio operacional
  - janelas parametrizadas criticas: permissoes, perfis, funcionarios, clientes, produtos, fornecedores, historico, veiculos, OS e selecao de orcamentos
  - controles auxiliares de orcamentos, agenda, pesquisa global e paines operacionais
- Comando atual de validacao automatizada:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
- Infraestrutura de runtime automatizado isolado adicionada:
  - `App` passa a inicializar servicos em modo lazy
  - modos `--smoke-test` e `--workflow-test` usam base SQLite isolada em `LocalAppData\PrimoAutoEletrica\AutomatedTests`
  - backup e configuracao de banco passam a respeitar o runtime isolado
- Workflow test operacional implementado e validado em `2026-05-22` com `14` checks aprovados e `0` falhas:
  - ambiente isolado
  - seed de usuarios sinteticos
  - cadastro de cliente
  - cadastro de veiculo
  - cadastro de produto
  - bloqueio multiusuario por registro
  - criacao de orcamento
  - criacao de ordem de servico
  - registro de venda no PDV
  - baixa de estoque apos venda
  - geracao financeira basica
  - validacao de auditoria
  - criacao e verificacao de backup
  - resumo final do banco com entidades esperadas
- Bugs reais corrigidos durante a validacao operacional:
  - `OrcamentoDatabaseService` deixa de quebrar ao persistir campos opcionais nulos em SQLite
  - `FinanceiroDatabaseService.ObterResumoFinanceiro()` deixa de reutilizar comando com `reader` aberto
- Comando operacional atual:
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
  - smoke test aprovado para 27 janelas/modais principais
  - smoke test aprovado para `NovoClienteWindow` e `EditarClienteWindow`
  - smoke test aprovado para `MainWindow` completa com busca global, permissoes e navegacao inicial

## Achados prioritarios ainda abertos

### Alta prioridade

- `PrimoAutoEletrica/Services/DatabaseService.cs`
  - arquivo ainda monolitico, mas Produtos/Estoque/Clientes/Veiculos/Fornecedores/Funcionarios/OrdensServico ja foram direcionados para repositorios dedicados
- `PrimoAutoEletrica/Services/DatabaseService.OrdensServico.cs`
  - extensao grande mantida como compatibilidade; fluxo ativo ja passa por `OrdemServicoRepository`
- `PrimoAutoEletrica/ViewModels/AgendamentosViewModel.cs`
  - viewmodel grande demais, com regras de negocio, mensagens de UI e integracoes operacionais
- `PrimoAutoEletrica/UserControls/OrdensServicoControl.xaml.cs`
  - code-behind excessivo
- `PrimoAutoEletrica/UserControls/PDVControl.xaml.cs`
  - ainda concentra fluxo operacional e regras de tela em code-behind
- `PrimoAutoEletrica/ViewModels/OrcamentosViewModel.cs`
  - melhorou bastante, mas ainda concentra conversao comercial, selecao de contexto e regras de UI

### Inconsistencias estruturais

- O arquivo duplicado fora do projeto foi removido do pacote-fonte em 04/06/2026:
  - `Services/DatabaseService.cs`
- Os services legados de agendamento que estavam excluidos da compilacao foram removidos do pacote-fonte em 04/06/2026:
  - `Services/AgendamentoIntegrationService.cs`
  - `Services/AgendamentoPerformanceService.cs`
  - `Services/AgendamentoTestService.cs`
- O controle antigo com forte indicio de orfandade foi removido em 04/06/2026:
  - `UserControls/AgendamentoControl.xaml`
- Stubs visuais antigos sem referencia ativa tambem foram removidos em 04/06/2026:
  - `UserControls/AlertasInteligentesControl.xaml`
  - `UserControls/ControleTecnicosControl.xaml`
  - `UserControls/ControleVeiculosControl.xaml`
  - `UserControls/PainelClientesControl.xaml`
  - `UserControls/PainelServicosControl.xaml`
  - `UserControls/StatusServicosControl.xaml`
  - `Views/PDV/PDVView.xaml`
  - `Views/Relatorios/RelatoriosView.xaml`

### Funcionalidades com placeholder ou persistencia incompleta

- Os comentarios de placeholder que restavam nos services legados excluidos da compilacao foram eliminados pela remocao dos arquivos mortos em 04/06/2026.
- O seed inicial de funcionarios deixou de conter senhas padrao fixas no fonte; banco novo gera senha temporaria aleatoria para o administrador e persiste apenas hash PBKDF2.
- Nenhum placeholder ativo conhecido foi encontrado em arquivos compilados nesta varredura apos a correcao de `Financeiro`, `Funcionarios`, a limpeza dos services legados de agendamento e a remocao do duplicado fora do projeto.

## Proxima sequencia recomendada

1. Quebrar `DatabaseService` em repositorios por modulo.
2. Migrar `OrdensServico`, `PDV` e `Agendamentos` para menos code-behind e mais comandos e viewmodels.
3. Padronizar as telas principais usando o design system e o visual de `Agendamentos`.
4. Fechar bloqueios administrativos finos por acao critica.
5. Iniciar auditoria de bindings e telas incompletas por fluxo operacional, nao por arquivo isolado.

## Atualizacao 2026-05-22 - Navegacao operacional

- `NavigationService` reforcado para navegacao operacional:
  - novo evento `NavigationStateChanged` para expor modulo atual, historico e possibilidade de retorno
  - novo metodo `RefreshCurrent()` para recarregar o modulo atual sem reaproveitar instancia de cache
- `MainWindow` passa a tratar navegacao como shell operacional:
  - adicionados botoes `Voltar` e `Atualizar` no header
  - estado de navegacao visivel no header com modulo atual e profundidade do historico
  - refresh manual do modulo registra auditoria em `Navegacao/AtualizarModulo`
  - logout em automacao deixa de travar execucoes por dialogo modal
  - helpers de automacao adicionados para validar modulo atual, retorno, refresh e destaque de menu
- `UiSmokeTestService` ampliado com regressao de navegacao real:
  - `MainWindow:NavegacaoSequencial`
  - `MainWindow:VoltarHistorico`
  - `MainWindow:AtualizarModulo`
  - `MainWindow:PermissoesMenuVendedor`
  - `NavigationService:ModuloInexistente`
  - `NavigationService:RefreshSemModulo`
- Validacao objetiva desta rodada em `2026-05-22`:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
  - resultado UI: `66 checks aprovados`, `0 falhas`
  - relatorio UI: `Logs/smoke-tests/ui-smoke-2026-05-22-06-43-23.txt`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
  - resultado operacional: `14 checks aprovados`, `0 falhas`
  - relatorio operacional: `Logs/workflow-tests/workflow-test-2026-05-22-06-43-31.txt`

## Atualizacao 2026-05-22 - Base visual de modais

- `Themes/Modal.xaml` ampliado com estilos globais para:
  - header de modal
  - cards de secao e barra lateral
  - footer padrao
  - botoes primario, secundario e fechar
  - titulos e labels de campo
- Padronizacao inicial aplicada em janelas de cadastro:
  - `Views/NovoProdutoWindow.xaml`
  - `Views/NovoFornecedorWindow.xaml`
- Segunda leva de padronizacao aplicada em modais administrativos:
  - `Views/NovoFuncionarioWindow.xaml`
  - `Views/EditarFuncionarioWindow.xaml`
  - `Views/NovoPerfilWindow.xaml`
- Textos quebrados removidos do fluxo de cadastro de funcionario:
  - `Views/NovoFuncionarioWindow.xaml.cs`
  - mensagens de validacao e sucesso passaram a usar texto limpo e consistente
- As duas janelas passam a usar:
  - `BackgroundBrush` global
  - `PremiumTextBox` e `PremiumComboBox`
  - superficies e botoes vindos do design system
- Achado real corrigido durante a padronizacao:
  - estilos de modal deixaram de depender de lookup externo em tempo de parse, evitando `UnsetValue` e falhas de `BorderBrush` no smoke test
- Validacao final desta frente:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
  - resultado UI final: `66 checks aprovados`, `0 falhas`
  - relatorio UI atualizado: `Logs/smoke-tests/ui-smoke-2026-05-22-06-52-13.txt`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
  - resultado operacional mantido: `14 checks aprovados`, `0 falhas`
  - relatorio operacional atualizado: `Logs/workflow-tests/workflow-test-2026-05-22-06-52-09.txt`

## Atualizacao 2026-05-22 - Janelas administrativas padronizadas

- Padronizacao visual aplicada nas janelas administrativas de perfis e permissoes:
  - `Views/GerenciarPerfisWindow.xaml`
  - `Views/ConfigurarPermissoesWindow.xaml`
- As duas telas agora usam:
  - shell visual de modal administrativo com header, cards laterais e footer padrao
  - grids com visual padronizado, virtualizacao e acoes por linha mais legiveis
  - campos e botoes alinhados ao design system global
- Ajustes funcionais feitos junto com a padronizacao:
  - botoes de editar e excluir passam a atuar sobre a linha clicada, nao apenas sobre a selecao anterior
  - detalhes de perfil entram em modo controlado de edicao, com permissoes bloqueadas fora do fluxo de editar
  - detalhes de permissao voltam corretamente ao estado anterior ao cancelar
  - criacao de nova permissao limpa selecao anterior antes da edicao
- Achado tecnico real corrigido durante a rodada:
  - o encadeamento `PremiumTable -> PremiumDataGrid` falhava no parse dessas janelas dentro do smoke test
  - as duas telas receberam estilos locais equivalentes para manter o padrao visual sem depender desse lookup quebrado
- Validacao objetiva desta rodada:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
  - resultado UI: `66 checks aprovados`, `0 falhas`
  - relatorio UI: `Logs/smoke-tests/ui-smoke-2026-05-22-07-02-24.txt`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
  - resultado operacional: `14 checks aprovados`, `0 falhas`
  - relatorio operacional: `Logs/workflow-tests/workflow-test-2026-05-22-07-02-33.txt`

## Atualizacao 2026-05-22 - Leva de edicao de cadastros

- Padronizacao visual aplicada nas janelas de edicao:
  - `Views/EditarProdutoWindow.xaml`
  - `Views/EditarFornecedorWindow.xaml`
- As telas agora seguem o mesmo bloco visual usado nos modais recentes:
  - header padrao com contexto da acao
  - secoes organizadas por dominio de informacao
  - campos com `PremiumTextBox`, `PremiumComboBox` e `MultiLineTextBox`
  - footer com acoes consistentes de cancelar e salvar
- O objetivo desta leva foi manter o comportamento atual e reduzir divergencia visual entre cadastros novos e telas de edicao.
- Validacao objetiva desta rodada:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
  - resultado UI: `66 checks aprovados`, `0 falhas`
  - relatorio UI: `Logs/smoke-tests/ui-smoke-2026-05-22-07-05-42.txt`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
  - resultado operacional: `14 checks aprovados`, `0 falhas`
  - relatorio operacional: `Logs/workflow-tests/workflow-test-2026-05-22-07-05-49.txt`

## Atualizacao 2026-05-22 - Bloco CRM padronizado

- Padronizacao visual aplicada nas telas do bloco de CRM:
  - `Views/Clientes/NovoClienteWindow.xaml`
  - `Views/NovoVeiculoWindow.xaml`
  - `Views/HistoricoClienteWindow.xaml`
  - `Views/Clientes/VisualizarClienteWindow.xaml`
- O bloco agora segue o mesmo design system dos modais e paines administrativos:
  - `ModalSurface`, `ModalHeaderSurface` e `ModalFooterSurface`
  - `PremiumTextBox`, `PremiumComboBox` e `MultiLineTextBox`
  - cards laterais e areas de resumo mais consistentes
  - tabelas com visual alinhado ao restante do sistema sem depender do lookup quebrado de estilos globais de grid
- Ajuste funcional incluido nesta frente:
  - `Views/NovoVeiculoWindow.xaml.cs` foi reescrito em ASCII limpo
  - pre-selecao de cliente ficou mais confiavel ao abrir o cadastro de veiculo a partir do cliente
  - mensagens com texto corrompido foram normalizadas no fluxo de validacao e salvamento de veiculo
- Validacao objetiva desta rodada:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
  - resultado UI: `66 checks aprovados`, `0 falhas`
  - relatorio UI: `Logs/smoke-tests/ui-smoke-2026-05-22-07-17-10.txt`
- `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
- resultado operacional: `14 checks aprovados`, `0 falhas`
- relatorio operacional: `Logs/workflow-tests/workflow-test-2026-05-22-07-17-16.txt`

## Atualizacao 2026-05-22 - Bloco de orcamentos padronizado e corrigido

- Padronizacao visual aplicada nas janelas operacionais do bloco de orcamentos:
  - `Views/OrdemServicoWindow.xaml`
  - `Views/SelecionarOrcamentoWindow.xaml`
  - `Views/NovoOrcamentoWindow.xaml`
- O conjunto agora segue o mesmo design system dos modais recentes:
  - `ModalSurface`, `ModalHeaderSurface` e `ModalFooterSurface`
  - `PremiumTextBox`, `PremiumComboBox`, `PremiumDatePicker` e `MultiLineTextBox`
  - grids com visual local padronizado para evitar o lookup quebrado de estilos globais
  - indicadores laterais, cards de resumo e acoes consistentes entre OS e orcamentos
- Correcao funcional aplicada em `Views/NovoOrcamentoWindow.xaml.cs`:
  - o cliente selecionado passa a ser persistido com `Guid` valido no salvamento
  - o status comercial selecionado deixa de ser ignorado no salvar definitivo
  - os itens do carrinho foram sincronizados com `OrcamentosViewModel.ItensCarrinho`, evitando perda de itens no save
  - o subtotal por item passou a ser recalculado em edicoes de grade, considerando quantidade, preco e desconto do item
  - o seletor de produtos usava binding incorreto para estoque e agora aponta para `QuantidadeEstoque`
  - os dialogs auxiliares de selecionar produto e informar quantidade receberam visual consistente com o design system
- Validacao objetiva desta rodada:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
  - resultado UI: `66 checks aprovados`, `0 falhas`
  - relatorio UI: `Logs/smoke-tests/ui-smoke-2026-05-22-07-32-13.txt`
- `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
- resultado operacional: `14 checks aprovados`, `0 falhas`
- relatorio operacional: `Logs/workflow-tests/workflow-test-2026-05-22-07-32-20.txt`

## Atualizacao 2026-05-22 - Telas de consulta padronizadas

- Padronizacao visual aplicada nas telas de consulta rapida:
  - `Views/VisualizarFornecedorWindow.xaml`
  - `Views/VisualizarVeiculoWindow.xaml`
- As duas telas agora seguem a mesma linguagem dos modais recentes:
  - `ModalSurface`, `ModalHeaderSurface` e `ModalFooterSurface`
  - cards de secao e barra lateral consistentes com o restante do sistema
  - textos e titulos sem caracteres corrompidos
  - acoes finais alinhadas ao padrao global de botoes
- O objetivo desta frente foi reduzir a divergencia visual em telas muito usadas no atendimento e manter o smoke test cobrindo consultas sem regressao.
- Validacao objetiva desta rodada:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
  - resultado UI: `66 checks aprovados`, `0 falhas`
  - relatorio UI: `Logs/smoke-tests/ui-smoke-2026-05-22-07-35-46.txt`
- `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
- resultado operacional: `14 checks aprovados`, `0 falhas`
- relatorio operacional: `Logs/workflow-tests/workflow-test-2026-05-22-07-35-54.txt`

## Atualizacao 2026-05-22 - Bloco administrativo e operacional complementar

- Padronizacao visual aplicada em janelas que ainda estavam fora do design system:
  - `Views/Clientes/EditarClienteWindow.xaml`
  - `Views/ImportarNotaWindow.xaml`
  - `Views/AjusteEstoqueWindow.xaml`
- O objetivo desta frente foi fechar um trecho importante da etapa 3 em telas usadas no atendimento, estoque e importacao fiscal:
  - `EditarCliente` agora segue o mesmo shell visual de modais administrativos, com cards, sidebar e grade de veiculos padronizada
  - `ImportarNota` recebeu header, cards de resumo, area de drag and drop, grid de produtos e bloco de logs alinhados ao sistema
  - `AjusteEstoque` ganhou layout consistente para ajuste unitario, lote e preco, com filtros e grids no mesmo padrao
- Ajuste funcional incluido na rodada:
  - `Views/AjusteEstoqueWindow.xaml.cs` passou a usar o filtro `Estoque Critico` de forma coerente com a interface nova, evitando divergencia silenciosa na filtragem
- Validacao objetiva desta rodada:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
  - resultado UI: `66 checks aprovados`, `0 falhas`
  - relatorio UI: `Logs/smoke-tests/ui-smoke-2026-05-22-07-48-02.txt`
- `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
- resultado operacional: `14 checks aprovados`, `0 falhas`
- relatorio operacional: `Logs/workflow-tests/workflow-test-2026-05-22-07-48-09.txt`

## Atualizacao 2026-05-22 - Acesso inicial e dialogos auxiliares

- Padronizacao visual aplicada em componentes que aparecem logo no fluxo de acesso e importacao:
  - `Views/LoginWindow.xaml`
  - `Views/AdicionarFornecedorDialog.xaml`
- O bloco agora fica mais alinhado ao restante do sistema:
  - login com card central, campos premium e acoes consistentes com o design system
  - dialogo de adicionar fornecedor refeito como modal simples, sem textos corrompidos
- A tela de login preservou comportamento existente:
  - nomes dos controles de autenticacao foram mantidos
  - alternancia de mostrar/ocultar senha continua compativel com o code-behind
  - tratamento de erro visual continua funcional
- Validacao objetiva desta rodada:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
  - resultado UI: `66 checks aprovados`, `0 falhas`
  - relatorio UI: `Logs/smoke-tests/ui-smoke-2026-05-22-07-50-39.txt`
- `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
- resultado operacional: `14 checks aprovados`, `0 falhas`
- relatorio operacional: `Logs/workflow-tests/workflow-test-2026-05-22-07-50-45.txt`

## Atualizacao 2026-05-22 - Performance inicial no PDV

- Frente pequena da etapa 4 aplicada em `Views/PDV/PDVView.xaml`:
  - `ProdutosListBox` agora usa `VirtualizingStackPanel` com `Recycling`
  - `ClientesListBox` agora usa `VirtualizingStackPanel` com `Recycling`
  - `CarrinhoListView` passou a explicitar `ScrollViewer.CanContentScroll`
- O objetivo foi reduzir custo de renderizacao e scroll em listas longas sem alterar o fluxo funcional do PDV.
- Validacao objetiva desta rodada:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
  - resultado UI: `66 checks aprovados`, `0 falhas`
  - relatorio UI: `Logs/smoke-tests/ui-smoke-2026-05-22-07-52-21.txt`
- `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
- resultado operacional: `14 checks aprovados`, `0 falhas`
- relatorio operacional: `Logs/workflow-tests/workflow-test-2026-05-22-07-52-28.txt`

## Atualizacao 2026-05-23 - Modulos principais padronizados

- Padronizacao visual aplicada nos wrappers dos modulos principais:
  - `Views/PDV/PDVView.xaml`
  - `Views/OrcamentosView.xaml`
  - `Views/Relatorios/RelatoriosView.xaml`
- Objetivo desta frente:
  - alinhar os modulos centrais ao design system global
  - remover cabecalhos e containers visuais antigos que destoavam do restante do sistema
  - manter os nomes de controles e bindings existentes para evitar regressao funcional
- Ajustes relevantes:
  - `PDVView` agora usa shell visual consistente, cards padronizados e estilos locais de listas mantendo a virtualizacao ja aplicada
  - `OrcamentosView` ganhou cabecalho, acoes e area de contexto coerentes com o dashboard comercial existente
  - `RelatoriosView` passou a encapsular `RelatoriosControl` dentro da mesma moldura visual usada nos demais modulos
- Validacao objetiva desta rodada:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
  - resultado UI: `66 checks aprovados`, `0 falhas`
  - relatorio UI: `Logs/smoke-tests/ui-smoke-2026-05-23-15-27-33.txt`
- `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
- resultado operacional: `14 checks aprovados`, `0 falhas`
- relatorio operacional: `Logs/workflow-tests/workflow-test-2026-05-23-15-27-39.txt`

## Atualizacao 2026-05-23 - Modais restantes alinhados ao design system

- Padronizacao visual concluida em janelas que ainda dependiam de base antiga:
  - `Views/NovoAgendamentoPremiumWindow.xaml`
  - `Views/NovoFornecedorWindow.xaml`
- Ajuste tecnico importante:
  - `NovoAgendamentoPremiumWindow` deixou de depender do uso direto de `PrimaryButton` e `SecondaryButton` no footer e passou a usar os estilos de modal, reduzindo risco de lookup quebrado em tempo de execucao
- O bloco tambem eliminou textos corrompidos na estrutura principal do agendamento, mantendo os mesmos bindings e comandos.
- Validacao objetiva desta rodada:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
  - resultado UI: `66 checks aprovados`, `0 falhas`
  - relatorio UI: `Logs/smoke-tests/ui-smoke-2026-05-23-15-30-51.txt`
- `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
- resultado operacional: `14 checks aprovados`, `0 falhas`
- relatorio operacional: `Logs/workflow-tests/workflow-test-2026-05-23-15-30-57.txt`

## Atualizacao 2026-05-23 - Otimizacao pontual em Orcamentos

- Frente de performance aplicada em `ViewModels/OrcamentosViewModel.cs`:
  - produtos ativos agora usam cache curto em memoria para evitar recarga completa a cada inicializacao do fluxo
  - `AtualizarDashboard()` passou a materializar a lista uma unica vez antes dos agregados, reduzindo reenumeracoes desnecessarias
- O objetivo desta rodada foi atacar custo repetitivo de carregamento sem alterar comportamento funcional do modulo.
- Validacao objetiva desta rodada:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
  - resultado UI: `66 checks aprovados`, `0 falhas`
  - relatorio UI: `Logs/smoke-tests/ui-smoke-2026-05-23-15-33-34.txt`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
  - resultado operacional: `14 checks aprovados`, `0 falhas`
  - relatorio operacional: `Logs/workflow-tests/workflow-test-2026-05-23-15-33-41.txt`

## Atualizacao 2026-05-23 - Reducao de recargas em Agendamentos

- Frente de performance aplicada em `ViewModels/AgendamentosViewModel.cs`:
  - a tela deixou de carregar todos os agendamentos duas vezes na inicializacao antes de montar as colecoes visuais
  - `CarregarDadosIniciais()` agora reaproveita um snapshot sanitizado unico para agenda do dia, tecnicos, veiculos, clientes, alertas e dashboard
  - `CarregarTecnicos()` parou de consultar todos os agendamentos uma vez por tecnico e passou a usar agrupamento em memoria
  - `CarregarClientes()` parou de consultar agendamentos uma vez por cliente e passou a montar os indicadores a partir de snapshots compartilhados
  - o dashboard passou a ser calculado depois do carregamento dos tecnicos, evitando card inicial com total de tecnicos ativos desatualizado
- O objetivo desta rodada foi reduzir custo de leitura repetitiva em um dos modulos mais pesados da inicializacao sem alterar o fluxo operacional.
- Validacao objetiva desta rodada:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
  - resultado UI: `66 checks aprovados`, `0 falhas`
  - relatorio UI: `Logs/smoke-tests/ui-smoke-2026-05-23-21-00-18.txt`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
  - resultado operacional: `14 checks aprovados`, `0 falhas`
  - relatorio operacional: `Logs/workflow-tests/workflow-test-2026-05-23-21-00-14.txt`

## Atualizacao 2026-05-23 - Permissoes por acao nas rotas criticas

- Frente de seguranca e controle operacional aplicada em:
  - `Services/PermissionService.cs`
  - `Services/DatabaseService.AccessControl.cs`
  - `UserControls/ClientesControl.xaml.cs`
  - `UserControls/VeiculosControl.xaml.cs`
  - `UserControls/FornecedoresControl.xaml.cs`
  - `UserControls/FuncionariosControl.xaml.cs`
  - `UserControls/PDVControl.xaml.cs`
  - `UserControls/OrdensServicoControl.xaml.cs`
  - `UserControls/RelatoriosControl.xaml.cs`
- Ajustes relevantes desta rodada:
  - o servico de permissao passou a consultar permissao persistida por codigo e a usar fallback compativel por perfil quando nao houver regra explicita
  - o schema de acesso ganhou seeds de permissoes granulares para exclusao, funcionarios, `PDV`, `OrdensServico` e `Relatorios`
  - o vinculo padrao dos perfis deixou de conceder automaticamente novas permissoes granulares por modulo, preservando o comportamento atual e evitando acesso acidental
  - negacoes de permissao agora registram warning e auditoria de seguranca
  - acoes criticas de exclusao, desconto, finalizacao/cancelamento de venda, avancos operacionais de OS e exportacao/impressao de relatorios passaram a validar permissao por codigo
- Objetivo desta rodada:
  - sair do modelo de bloqueio apenas por menu e passar a validar a acao real nos pontos de maior risco operacional
- Validacao objetiva desta rodada:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
  - resultado UI: `66 checks aprovados`, `0 falhas`
  - relatorio UI: `Logs/smoke-tests/ui-smoke-2026-05-23-21-31-30.txt`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
  - resultado operacional: `14 checks aprovados`, `0 falhas`
  - relatorio operacional: `Logs/workflow-tests/workflow-test-2026-05-23-21-31-26.txt`

## Atualizacao 2026-05-23 - Bloqueio temporario apos falhas de login

- Frente de seguranca aplicada em:
  - `Services/DatabaseService.cs`
  - `Services/DatabaseService.Migrations.cs`
  - `Services/LoginAuthenticationResult.cs`
  - `Views/LoginWindow.xaml.cs`
- Ajustes relevantes desta rodada:
  - o banco ganhou uma tabela dedicada para tentativas de login e bloqueio temporario
  - o fluxo de autenticacao passou a distinguir credencial invalida de conta bloqueada
  - apos `5` falhas consecutivas, a conta fica bloqueada por `15 minutos`
  - a tela de login passou a mostrar mensagens amigaveis com tentativas restantes e com horario de desbloqueio
  - credenciais locais so sao salvas depois de autenticacao bem-sucedida
- Objetivo desta rodada:
  - reduzir risco de tentativa repetitiva de senha e fechar mais uma exigencia direta da trilha de seguranca
- Validacao objetiva desta rodada:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
  - resultado UI: `66 checks aprovados`, `0 falhas`
  - relatorio UI: `Logs/smoke-tests/ui-smoke-2026-05-23-21-36-16.txt`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
  - resultado operacional: `14 checks aprovados`, `0 falhas`
  - relatorio operacional: `Logs/workflow-tests/workflow-test-2026-05-23-21-36-11.txt`
  - probe isolado de seguranca validou `4` falhas com contagem regressiva, `5a` falha com bloqueio e credencial correta negada durante a janela de bloqueio

## Atualizacao 2026-05-23 - Expiracao de sessao por inatividade

- Frente de seguranca aplicada em:
  - `Services/AppSessionService.cs`
  - `Services/SessionInactivityService.cs`
  - `MainWindow.xaml.cs`
- Ajustes relevantes desta rodada:
  - a sessao agora registra a ultima atividade do usuario
  - um monitor global de inatividade foi adicionado na interface principal
  - apos `30 minutos` sem interacao de teclado/mouse/stylus, a sessao e encerrada automaticamente
  - o fluxo de logout por inatividade reutiliza o retorno seguro para a tela de login e registra auditoria especifica `LogoutInatividade`
  - o monitor fica desativado em modos automatizados para nao contaminar smoke test e workflow test
- Objetivo desta rodada:
  - cumprir a exigencia de expirar sessao parada sem impactar a navegacao normal e sem gerar fechamento abrupto da aplicacao
- Validacao objetiva desta rodada:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
  - resultado UI: `66 checks aprovados`, `0 falhas`
  - relatorio UI: `Logs/smoke-tests/ui-smoke-2026-05-23-21-44-16.txt`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
  - resultado operacional: `14 checks aprovados`, `0 falhas`
  - relatorio operacional: `Logs/workflow-tests/workflow-test-2026-05-23-21-44-12.txt`

## Atualizacao 2026-05-23 - Permissoes expandidas para Financeiro, Orcamentos e Agendamentos

- Frente de seguranca e controle operacional aplicada em:
  - `Services/DatabaseService.AccessControl.cs`
  - `Services/PermissionService.cs`
  - `UserControls/FinanceiroControl.xaml.cs`
  - `UserControls/OrcamentosControl.xaml.cs`
  - `ViewModels/AgendamentosViewModel.cs`
- Ajustes relevantes desta rodada:
  - novos codigos granulares foram adicionados para exportacao/impressao em `Financeiro`
  - `Orcamentos` passou a validar exportacao, impressao e conversao em venda
  - `Agendamentos` passou a validar cancelamento, geracao de OS, exportacao e impressao diretamente no `ViewModel`
  - os perfis de fallback foram expandidos para refletir essas novas acoes sem quebrar o comportamento atual
- Objetivo desta rodada:
  - reduzir mais uma faixa de operacoes sensiveis que ainda dependiam apenas de acesso ao modulo
- Validacao objetiva desta rodada:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
  - resultado UI: `66 checks aprovados`, `0 falhas`
  - relatorio UI: `Logs/smoke-tests/ui-smoke-2026-05-23-21-49-54.txt`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
  - resultado operacional: `14 checks aprovados`, `0 falhas`
  - relatorio operacional: `Logs/workflow-tests/workflow-test-2026-05-23-21-49-50.txt`

## Atualizacao 2026-05-23 - Estoque endurecido, backup no fechamento e shell com versao

- Frente aplicada em:
  - `Repositories/ProdutoRepository.cs`
  - `UserControls/EstoqueControl.xaml.cs`
  - `Views/NovoProdutoWindow.xaml.cs`
  - `Views/EditarProdutoWindow.xaml.cs`
  - `Views/AjusteEstoqueWindow.xaml.cs`
  - `Services/PermissionService.cs`
  - `Services/DatabaseService.AccessControl.cs`
  - `Services/DatabaseBackupService.cs`
  - `App.xaml.cs`
  - `MainWindow.xaml`
  - `MainWindow.xaml.cs`
- Ajustes relevantes desta rodada:
  - o modulo `Estoque` passou a validar permissao por acao em cadastro, edicao, exclusao e ajuste
  - o repositorio de produtos agora valida regras centrais antes de persistir:
    - estoque negativo so passa com permissao `ESTOQUE_PERMITIR_NEGATIVO`
    - override gerencial de estoque negativo gera auditoria dedicada
    - duplicidade basica por codigo e nome passou a ser barrada no repositorio
    - cadastro e edicao validam quantidade, minimo/maximo e precos de forma mais consistente
  - `NovoProduto`, `EditarProduto` e `AjusteEstoque` agora respeitam a mesma malha de permissao da camada de persistencia
  - a grade principal do `Estoque` passou a aplicar busca textual e filtro por categoria/estoque baixo de forma funcional
  - o app passou a criar backup automatico ao encerrar fora dos modos automatizados, registrando o evento em auditoria quando aplicavel
  - a `MainWindow` passou a exibir versao, data de build, ambiente e banco conectado no cabecalho principal
- Objetivo desta rodada:
  - fechar uma faixa operacional critica do estoque, fortalecer backup de rotina e melhorar rastreabilidade da versao em campo
- Validacao objetiva desta rodada:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
  - resultado UI: `66 checks aprovados`, `0 falhas`
  - relatorio UI: `Logs/smoke-tests/ui-smoke-2026-05-23-22-08-41.txt`
  - `dotnet run --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
  - resultado operacional: `14 checks aprovados`, `0 falhas`
  - relatorio operacional: `Logs/workflow-tests/workflow-test-2026-05-23-22-08-50.txt`

## Atualizacao 2026-05-24 - Validacao centralizada e duplicidade endurecida

- Frente aplicada em:
  - `Helpers/CadastroValidationHelper.cs`
  - `Repositories/ClienteRepository.cs`
  - `Repositories/FornecedorRepository.cs`
  - `Repositories/FuncionarioRepository.cs`
  - `Services/DatabaseService.cs`
  - `Services/DatabaseService.Migrations.cs`
  - `Services/OperationalWorkflowTestService.cs`
  - `Views/Clientes/NovoClienteWindow.xaml.cs`
  - `Views/Clientes/EditarClienteWindow.xaml.cs`
  - `Views/NovoFornecedorWindow.xaml.cs`
  - `Views/EditarFornecedorWindow.xaml.cs`
  - `Views/NovoFuncionarioWindow.xaml.cs`
  - `Views/EditarFuncionarioWindow.xaml`
  - `Views/EditarFuncionarioWindow.xaml.cs`
  - `Views/NovoVeiculoWindow.xaml.cs`
  - `Views/NovoOrcamentoWindow.xaml.cs`
  - `ViewModels/AgendamentosViewModel.cs`
  - `UserControls/OrcamentosControl.xaml.cs`
  - `UserControls/OrdensServicoControl.xaml.cs`
  - `Views/Clientes/VisualizarClienteWindow.xaml.cs`
- Ajustes relevantes desta rodada:
  - criada a base compartilhada `CadastroValidationHelper` para normalizar e validar `CPF/CNPJ`, telefone, e-mail, placa, datas e valores numericos
  - clientes, fornecedores, funcionarios e veiculos passaram a validar os mesmos campos tanto na interface quanto na camada de persistencia
  - `Funcionario` ganhou persistencia real de `CPF`, incluindo schema novo e migracao para bancos existentes
  - repositos de clientes e fornecedores agora barram duplicidade com regra operacional clara:
    - cliente: mesmo `CPF` ou mesmo nome com contato principal coincidente
    - fornecedor: mesmo `CNPJ` ou mesmo nome fantasia com contato principal coincidente
  - `Veiculo` passou a validar placa em padrao brasileiro tambem no repositorio
  - fluxos de WhatsApp e e-mail em `Clientes`, `Agendamentos`, `Orcamentos` e `OrdensServico` passaram a reaproveitar a mesma normalizacao de contato
  - o workflow sintetico foi alinhado aos novos criterios para cobrir salario, e-mail, `CPF` e placa validos sem gerar falso negativo
- Objetivo desta rodada:
  - sair de validacoes isoladas por tela e fechar uma malha consistente de dados cadastrais antes dos proximos blocos operacionais
- Validacao objetiva desta rodada:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
  - resultado UI: `66 checks aprovados`, `0 falhas`
  - relatorio UI: `Logs/smoke-tests/ui-smoke-2026-05-24-04-04-01.txt`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
  - resultado operacional: `14 checks aprovados`, `0 falhas`
  - relatorio operacional: `Logs/workflow-tests/workflow-test-2026-05-24-04-05-34.txt`

## Atualizacao 2026-05-24 - Validacao comercial e configuracoes do sistema

- Frente aplicada em:
  - `Helpers/ComercialValidationHelper.cs`
  - `Services/VendaService.cs`
  - `ViewModels/PDVViewModel.cs`
  - `UserControls/PDVControl.xaml.cs`
  - `Services/OrcamentoDatabaseService.cs`
  - `Services/FinanceiroDatabaseService.cs`
  - `Repositories/OrdemServicoRepository.cs`
  - `Views/OrdemServicoWindow.xaml.cs`
  - `Services/DatabaseConnectionSettingsService.cs`
  - `Services/DatabaseService.cs`
  - `Services/DatabaseBackupService.cs`
  - `App.xaml.cs`
  - `MainWindow.xaml`
  - `MainWindow.xaml.cs`
  - `Views/ConfiguracoesSistemaWindow.xaml`
  - `Views/ConfiguracoesSistemaWindow.xaml.cs`
- Ajustes relevantes desta rodada:
  - criada a base `ComercialValidationHelper` para consolidar textos obrigatorios, quantidades, descontos, subtotais, totais e intervalo de datas comerciais
  - `VendaService`, `PDV`, `Orcamentos`, `Financeiro` e `OrdensServico` passaram a validar estoque, desconto, subtotal, total e cronologia antes de gravar
  - o shell principal ganhou entrada de `Configuracoes` com permissao `SISTEMA_CONFIGURAR` e acesso adicional pela busca global
  - a nova `ConfiguracoesSistemaWindow` passou a:
    - persistir timeout de inatividade por interface
    - persistir timeout tecnico do banco
    - reaplicar o monitor de inatividade na sessao atual
    - exibir versao, build, ambiente, banco e pastas operacionais
    - abrir atalhos para perfis, permissoes e backup manual com verificacao imediata
  - `DatabaseService` passou a reaproveitar o timeout tecnico configurado ao montar a conexao SQLite
  - a atualizacao dessas configuracoes agora gera log e auditoria dedicados
- Objetivo desta rodada:
  - fechar mais uma faixa operacional da lista com foco em consistencia comercial e iniciar uma tela real de configuracoes sem depender de edicao manual de arquivo
- Validacao objetiva desta rodada:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
  - resultado UI: `66 checks aprovados`, `0 falhas`
  - relatorio UI: `Logs/smoke-tests/ui-smoke-2026-05-24-09-37-47.txt`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
  - resultado operacional: `14 checks aprovados`, `0 falhas`
  - relatorio operacional: `Logs/workflow-tests/workflow-test-2026-05-24-09-37-42.txt`

## Atualizacao 2026-05-24 - Confirmacao critica e blindagem contra exclusoes acidentais

- Frente aplicada em:
  - `Services/CriticalActionDialogService.cs`
  - `Views/ConfirmacaoCriticaWindow.xaml`
  - `Views/ConfirmacaoCriticaWindow.xaml.cs`
  - `UserControls/ClientesControl.xaml.cs`
  - `UserControls/FornecedoresControl.xaml.cs`
  - `UserControls/FuncionariosControl.xaml.cs`
  - `UserControls/EstoqueControl.xaml.cs`
  - `UserControls/VeiculosControl.xaml.cs`
  - `UserControls/OrdensServicoControl.xaml.cs`
  - `UserControls/PDVControl.xaml.cs`
  - `ViewModels/AgendamentosViewModel.cs`
  - `Views/NovoProdutoWindow.xaml.cs`
  - `Views/EditarProdutoWindow.xaml.cs`
  - `Views/GerenciarPerfisWindow.xaml.cs`
  - `Views/ConfigurarPermissoesWindow.xaml.cs`
- Ajustes relevantes desta rodada:
  - criada a janela reutilizavel `ConfirmacaoCriticaWindow` com digitacao obrigatoria da palavra-chave antes de concluir a acao
  - exclusoes de clientes, fornecedores, funcionarios, produtos, veiculos, ordens de servico, perfis e permissoes passaram a usar o mesmo fluxo de confirmacao critica
  - o `PDV` agora exige confirmacao critica ao cancelar a venda atual
  - `Agendamentos` passou a exigir permissao e confirmacao critica para remocao/cancelamento, alem de confirmacao manual para conversao em `OS` e `check-out` com integracoes
  - override de estoque negativo em cadastro e edicao de produto passou a usar o mesmo padrao de revisao manual
  - `PDV` ganhou auditoria dedicada para aplicacao de desconto, reduzindo uma lacuna da trilha comercial
- Objetivo desta rodada:
  - padronizar as acoes destrutivas e operacionais sensiveis com uma experiencia mais segura e menos sujeita a clique acidental
- Validacao objetiva desta rodada:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
  - resultado UI: `66 checks aprovados`, `0 falhas`
  - relatorio UI: `Logs/smoke-tests/ui-smoke-2026-05-24-09-48-25.txt`
- `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
- resultado operacional: `14 checks aprovados`, `0 falhas`
- relatorio operacional: `Logs/workflow-tests/workflow-test-2026-05-24-09-48-21.txt`

## Atualizacao 2026-05-24 - Caixa operacional real e fechamento profissional do PDV

- Frente aplicada em:
  - `Models/CaixaOperacional.cs`
  - `Models/Venda.cs`
  - `Services/CaixaService.cs`
  - `Services/VendaService.cs`
  - `Services/DatabaseService.Migrations.cs`
  - `Services/DatabaseService.AccessControl.cs`
  - `Services/PermissionService.cs`
  - `Services/FinanceiroDatabaseService.cs`
  - `ViewModels/PDVViewModel.cs`
  - `UserControls/PDVControl.xaml`
  - `UserControls/PDVControl.xaml.cs`
  - `Views/OperacaoCaixaWindow.xaml`
  - `Views/OperacaoCaixaWindow.xaml.cs`
  - `Services/OperationalWorkflowTestService.cs`
- Ajustes relevantes desta rodada:
  - criada a estrutura persistida de `CaixaSessoes` e `MovimentacoesCaixa`, com migration real e indices para sessao aberta, consulta por operador e historico operacional
  - `Venda` passou a carregar `Status`, `CaixaSessaoId`, dados de cancelamento e o `VendaService` agora grava a venda no mesmo ciclo transacional do caixa
  - o `PDV` ganhou operacao real de caixa com abertura, fechamento, sangria e suprimento pela interface, incluindo saldo esperado, contagem de vendas na sessao e bloqueio de pagamento quando o caixa estiver fechado
  - o fluxo de venda passou a registrar automaticamente movimentacao financeira integrada por forma de pagamento e movimentacao dedicada do caixa operacional
  - o `PDV` agora permite reimprimir a ultima venda concluida e cancelar a ultima venda com estorno completo de estoque, financeiro e caixa da sessao aberta
  - novas permissoes granulares foram adicionadas para abrir/fechar caixa, sangria, suprimento, reimpressao e cancelamento completo de venda
  - o workflow test foi expandido para validar abertura de caixa, venda vinculada ao caixa, reflexo financeiro, trilha de movimentacoes e fechamento da sessao
- Objetivo desta rodada:
  - fechar o bloco mais critico do `PDV` com operacao de caixa real, reduzir lacunas entre venda e financeiro e transformar a trilha do caixa em evidencia funcional objetiva
- Validacao objetiva desta rodada:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
  - resultado UI: `66 checks aprovados`, `0 falhas`
  - relatorio UI: `Logs/smoke-tests/ui-smoke-2026-05-24-10-13-30.txt`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
  - resultado operacional: `17 checks aprovados`, `0 falhas`
  - relatorio operacional: `Logs/workflow-tests/workflow-test-2026-05-24-10-13-25.txt`

## Atualizacao 2026-05-24 - Conversao real de agendamento em OS e reflexo financeiro da OS

- Frente aplicada em:
  - `Services/AgendamentoDatabaseService.cs`
  - `ViewModels/AgendamentosViewModel.cs`
  - `Repositories/OrdemServicoRepository.cs`
  - `Services/FinanceiroDatabaseService.cs`
  - `Services/OperationalWorkflowTestService.cs`
- Ajustes relevantes desta rodada:
  - `Agendamento` passou a persistir corretamente campos operacionais que estavam ficando so em memoria, incluindo `FormaPagamento`, `DuracaoReal`, vinculo de `OS`, datas de `OS`, fotos e marcadores de reagendamento/cancelamento
  - o servico de agendamentos agora converte de fato um atendimento em `OS` real, com numero persistido, snapshots de cliente/veiculo, itens de servico e pecas e evento operacional de conversao
  - o `check-out` de agendamentos com `OS` vinculada passou a finalizar a ordem real, entregando a `OS` pelo repositório, disparando baixa de estoque e evitando duplicidade de integracao entre agenda e ordem
  - `OrdensServico` entregues agora geram conta a receber e movimentacao financeira idempotentes no mesmo fluxo transacional do repositório
  - a auditoria da integracao financeira da `OS` foi movida para depois do `commit`, eliminando a espera artificial observada no fechamento automatizado do checkout
  - a leitura de agendamentos foi enriquecida para trazer produtos e servicos relacionados, reduzindo lacunas entre o que a tela mostra e o que o banco realmente possui
  - o workflow test foi expandido para validar conversao `Agendamento -> OS`, finalizacao da `OS` pelo checkout, baixa de estoque por `OS` e reflexo financeiro integrado
- Objetivo desta rodada:
  - fechar o trecho mais fraco entre agenda, execucao operacional, estoque e financeiro com evidencia automatizada ponta a ponta
- Validacao objetiva desta rodada:
  - `dotnet build PrimoAutoEletrica.sln`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --smoke-test`
  - resultado UI: `66 checks aprovados`, `0 falhas`
  - relatorio UI: `Logs/smoke-tests/ui-smoke-2026-05-24-10-41-43.txt`
  - `dotnet run --no-build --project PrimoAutoEletrica\PrimoAutoEletrica.csproj -- --workflow-test`
  - resultado operacional: `21 checks aprovados`, `0 falhas`
  - relatorio operacional: `Logs/workflow-tests/workflow-test-2026-05-24-10-41-32.txt`

## Atualizacao 2026-06-04 - Limpeza de controles e wrappers orfaos

- Frente aplicada em:
  - `UserControls/AlertasInteligentesControl.xaml`
  - `UserControls/AlertasInteligentesControl.xaml.cs`
  - `UserControls/ControleTecnicosControl.xaml`
  - `UserControls/ControleTecnicosControl.xaml.cs`
  - `UserControls/ControleVeiculosControl.xaml`
  - `UserControls/ControleVeiculosControl.xaml.cs`
  - `UserControls/PainelClientesControl.xaml`
  - `UserControls/PainelClientesControl.xaml.cs`
  - `UserControls/PainelServicosControl.xaml`
  - `UserControls/PainelServicosControl.xaml.cs`
  - `UserControls/StatusServicosControl.xaml`
  - `UserControls/StatusServicosControl.xaml.cs`
  - `Views/PDV/PDVView.xaml`
  - `Views/PDV/PDVView.xaml.cs`
  - `Views/Relatorios/RelatoriosView.xaml`
  - `Views/Relatorios/RelatoriosView.xaml.cs`
- Ajustes relevantes desta rodada:
  - removidos controles standalone sem rota ativa na sidebar, no `NavigationService` ou nos smoke tests
  - removidos wrappers antigos de `PDV` e `Relatorios`; os modulos reais seguem em `UserControls/PDVControl.xaml` e `UserControls/RelatoriosControl.xaml`
  - preservada a janela operacional de selecao de cliente do PDV, que ja foi blindada apenas para `App.IsAutomatedTestMode`
- Objetivo desta rodada:
  - reduzir ruido estrutural, evitar manutencao em telas que o usuario nao acessa e manter a entrega fonte alinhada aos modulos reais
- Validacao objetiva desta rodada:
  - `dotnet build .\PrimoAutoEletrica.csproj --no-restore`: `0` erros e `0` avisos
  - varredura de referencias ativas aos controles removidos: nenhuma ocorrencia em `.cs`, `.xaml` ou `.csproj` fora de `bin/obj/Docs`
  - pacote `PrimoAutoEletrica_Source_20260604_QA.zip`: 319 arquivos e 0 entradas proibidas

## Atualizacao 2026-06-04 - Snapshot seguro para rollback de NF-e atualizada

- Frente aplicada em:
  - `Models/ProdutoImportacaoSnapshot.cs`
  - `Models/ProdutoImportado.cs`
  - `Models/ImportacaoRollbackResult.cs`
  - `Services/ProdutoImportacaoService.cs`
  - `Data/Repositories/ImportacaoRepository.cs`
  - `Services/DatabaseService.cs`
  - `Services/DatabaseService.Migrations.cs`
  - `UserControls/ImportarNFeControl.xaml.cs`
  - `Services/AppRuntimeConfiguration.cs`
  - `Services/UiSmokeTestService.cs`
- Ajustes relevantes desta rodada:
  - produtos existentes atualizados por NF-e passam a gravar snapshot anterior e posterior no item do historico
  - o rollback fiscal restaura produtos atualizados apenas se o estado atual ainda bater com o snapshot posterior
  - alteracoes posteriores, snapshot ausente ou produto ja revertido continuam bloqueando a reversao automatica
  - o resultado de rollback passou a contabilizar `AtualizacoesRevertidas`
  - o smoke UI ganhou filtro `--smoke-filter=...` para validar checks especificos sem executar a suite completa
- Validacao objetiva desta rodada:
  - `dotnet build .\PrimoAutoEletrica.csproj --no-restore`: `0` erros e `0` avisos
  - `dotnet .\bin\Debug\net9.0-windows\PrimoAutoEletrica.dll --smoke-test --smoke-filter=ImportarNFe:RollbackAtualizacaoComSnapshot`: `1/1` check aprovado
  - relatorio: `Logs/smoke-tests/ui-smoke-2026-06-04-18-18-54.txt`

## Atualizacao 2026-06-04 - Pre-check automatizado contra modais presas

- Frente aplicada em:
  - `Services/UiSmokeTestService.cs`
  - `MainWindow.xaml.cs`
- Ajustes relevantes desta rodada:
  - criado o smoke filtravel `PreCheck:SemModaisPresasNavegacao`
  - a automacao navega pelos modulos centrais, `ImportarNFe` e `Configuracoes`, falhando se alguma janela transiente ficar visivel fora da shell principal
  - `Configuracoes` ganhou abertura segura para automacao, sem manter `ShowDialog` preso
- Validacao objetiva desta rodada:
  - `dotnet build .\PrimoAutoEletrica.csproj --no-restore`: `0` erros e `0` avisos
  - `dotnet .\bin\Debug\net9.0-windows\PrimoAutoEletrica.dll --smoke-test --smoke-filter=PreCheck:SemModaisPresasNavegacao`: `1/1` check aprovado
  - relatorio: `Logs/smoke-tests/ui-smoke-2026-06-04-18-29-43.txt`

## Atualizacao 2026-06-04 - Textos comerciais do comprovante validados

- Frente aplicada em:
  - `Services/UiSmokeTestService.cs`
- Ajustes relevantes desta rodada:
  - o smoke filtrado de `Configuracoes` inicializa a base sintetica quando executado isoladamente
  - `Configuracoes:ComercialBackupRestauracao` validou persistencia de cabecalho/rodape e reflexo no comprovante gerado
- Validacao objetiva desta rodada:
  - `dotnet build .\PrimoAutoEletrica.csproj --no-restore`: `0` erros e `0` avisos
  - `dotnet .\bin\Debug\net9.0-windows\PrimoAutoEletrica.dll --smoke-test --smoke-filter=Configuracoes:ComercialBackupRestauracao`: `1/1` check aprovado
  - relatorio: `Logs/smoke-tests/ui-smoke-2026-06-04-18-34-59.txt`
