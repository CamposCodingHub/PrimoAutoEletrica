# PRIMOX QA Engine — Relatorio de Cobertura

Gerado: 2026-09-08 07:21:36

## Resumo
- Modulos: 17
- Windows: 51
- UserControls: 28
- Botoes (runtime): 277
- Acoes descobertas: 419
- Testes executados: 37
- PASS: 36
- FAIL: 0
- BLOCKED: 1
- NOT TESTABLE: 15

## Coberturas
- Descoberta (itens inventariados): 419
- Execucao (PASS/total): 97,3%
- Validacao: exigida em checks QaEngine:* (re-leitura DB/repo apos UI)

## Observacoes
- Inventario estatico via reflection de Windows/UserControls/_Click handlers.
- Deep QA Fase 11 permanece ativo e e expandido por este engine.
- Destrutivo Funcionarios: botao Excluir nao disponivel/habilitado — marcado BLOCKED (seguro).
- OS emitida OS-2026-0002 validada (create/update/repeat).
- Finalizacao inventario: modulos=17, windows=51, userControls=28, clickHandlers=392.
- Hardcoded colors audit: hex≈345, FromRgb/FromArgb≈71 (print/chips/converters incluidos).
- Versao assembly=1.0.0.0; informational=1.0.0-rc.1. Recomendacao RC: 1.0.0-rc.1 (PROJECT_STATUS ainda listava 1.3.0 legado).
- Matriz de cobertura: C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\bin\Debug\net6.0-windows\Logs\qa-engine\primox-coverage-matrix-20260908-072031.md
- Deep QA da Fase 11 foi preservado e expandido.
- O QaEngine foi expandido para cobertura funcional profunda dos modulos prioritarios.
- Ambiente: banco isolado ui-smoke-test (nunca producao).
- Baseline: Fase 12=14; Fase 13=29; Fase 14 finalizacao=37 checks QaEngine.

## Inventario (amostra)
| Modulo | Tela | Funcao | Testavel | Resultado |
| ------ | ---- | ------ | -------- | --------- |
| Agendamentos | Agendamentos | Navegar | True | DISCOVERED |
| AutoEletricaTecnica | AutoEletricaTecnica | Navegar | True | DISCOVERED |
| CatalogoPecas | CatalogoPecas | Navegar | True | DISCOVERED |
| Clientes | Clientes | Navegar | True | DISCOVERED |
| Dashboard | Dashboard | Navegar | True | DISCOVERED |
| Estoque | Estoque | Navegar | True | DISCOVERED |
| Financeiro | Financeiro | Navegar | True | DISCOVERED |
| Fornecedores | Fornecedores | Navegar | True | DISCOVERED |
| Funcionarios | Funcionarios | Navegar | True | DISCOVERED |
| Help | Help | Navegar | True | DISCOVERED |
| ImportarNFe | ImportarNFe | Navegar | True | DISCOVERED |
| OficinaKanban | OficinaKanban | Navegar | True | DISCOVERED |
| Orcamentos | Orcamentos | Navegar | True | DISCOVERED |
| OrdensServico | OrdensServico | Navegar | True | DISCOVERED |
| PDV | PDV | Navegar | True | DISCOVERED |
| Relatorios | Relatorios | Navegar | True | DISCOVERED |
| Veiculos | Veiculos | Navegar | True | DISCOVERED |
| Fornecedores | AdicionarFornecedorDialog | SimButton_Click | True | DISCOVERED |
| Fornecedores | AdicionarFornecedorDialog | NaoButton_Click | True | DISCOVERED |
| Estoque | AjusteEstoqueWindow | SalvarButton_Click | True | DISCOVERED |
| Estoque | AjusteEstoqueWindow | SalvarAjusteUnitario | True | DISCOVERED |
| Estoque | AjusteEstoqueWindow | SalvarAjusteLote | True | DISCOVERED |
| Estoque | AjusteEstoqueWindow | SalvarAjustePreco | True | DISCOVERED |
| Estoque | AjusteEstoqueWindow | CancelarButton_Click | True | DISCOVERED |
| Geral | AssinaturaDigitalWindow | LimparButton_Click | True | DISCOVERED |
| Geral | AssinaturaDigitalWindow | CancelarButton_Click | True | DISCOVERED |
| Geral | AssinaturaDigitalWindow | SalvarButton_Click | True | DISCOVERED |
| Geral | AssinaturaDigitalWindow | SalvarImagemAssinatura | True | DISCOVERED |
| OrdensServico | AtalhosTecladoWindow | Fechar_Click | True | DISCOVERED |
| Geral | AtualizacaoWindow | CheckUpdateButton_Click | True | DISCOVERED |
| Geral | AtualizacaoWindow | InstallUpdateButton_Click | True | DISCOVERED |
| Geral | AtualizacaoWindow | CancelButton_Click | True | DISCOVERED |
| Geral | BackupSettingsWindow | SaveButton_Click | True | DISCOVERED |
| Geral | BackupSettingsWindow | CancelButton_Click | True | DISCOVERED |
| Geral | BackupSettingsWindow | BrowseButton_Click | True | DISCOVERED |
| Geral | BackupSettingsWindow | TestBackupButton_Click | True | DISCOVERED |
| Configuracoes | ConfiguracoesSistemaWindow | SalvarButton_Click | True | DISCOVERED |
| Configuracoes | ConfiguracoesSistemaWindow | FecharButton_Click | True | DISCOVERED |
| Configuracoes | ConfiguracoesSistemaWindow | GerenciarPerfisButton_Click | True | DISCOVERED |
| Configuracoes | ConfiguracoesSistemaWindow | ConfigurarPermissoesButton_Click | True | DISCOVERED |
| Configuracoes | ConfiguracoesSistemaWindow | SalvarConfiguracoesComerciaisButton_Click | True | DISCOVERED |
| Configuracoes | ConfiguracoesSistemaWindow | ValidarLogoButton_Click | True | DISCOVERED |
| Configuracoes | ConfiguracoesSistemaWindow | GerarPreviaComprovanteButton_Click | True | DISCOVERED |
| Configuracoes | ConfiguracoesSistemaWindow | CriarBackupManualButton_Click | True | DISCOVERED |
| Configuracoes | ConfiguracoesSistemaWindow | ValidarBackupRestauracaoButton_Click | True | DISCOVERED |
| Configuracoes | ConfiguracoesSistemaWindow | RestaurarBackupButton_Click | True | DISCOVERED |
| Configuracoes | ConfiguracoesSistemaWindow | AbrirPastaButton_Click | True | DISCOVERED |
| Configuracoes | ConfiguracoesSistemaWindow | SalvarConfiguracoesOperacionaisButton_Click | True | DISCOVERED |
| Configuracoes | ConfiguracoesSistemaWindow | TestarPastaRedeButton_Click | True | DISCOVERED |
| Configuracoes | ConfiguracoesSistemaWindow | CarregarInformacoesBackupButton_Click | True | DISCOVERED |
| Configuracoes | ConfiguracoesSistemaWindow | TestarConexaoSqlServerButton_Click | True | DISCOVERED |
| Configuracoes | ConfiguracoesSistemaWindow | AtualizarInformacoesBancoButton_Click | True | DISCOVERED |
| Configuracoes | ConfiguracoesSistemaWindow | SalvarConfiguracoesMultiusuario | True | DISCOVERED |
| Configuracoes | ConfiguracoesSistemaWindow | AtualizarImpressorasButton_Click | True | DISCOVERED |
| Configuracoes | ConfiguracoesSistemaWindow | SalvarConfiguracoesEstacaoButton_Click | True | DISCOVERED |
| Configuracoes | ConfigurarPermissoesWindow | NovaPermissaoButton_Click | True | DISCOVERED |
| Configuracoes | ConfigurarPermissoesWindow | EditarPermissaoButton_Click | True | DISCOVERED |
| Configuracoes | ConfigurarPermissoesWindow | ExcluirPermissaoButton_Click | False | NOT TESTABLE (destrutivo — so dialog) |
| Configuracoes | ConfigurarPermissoesWindow | SalvarPermissaoButton_Click | True | DISCOVERED |
| Configuracoes | ConfigurarPermissoesWindow | CancelarPermissaoButton_Click | True | DISCOVERED |
| Configuracoes | ConfigurarPermissoesWindow | ExportarPermissoesButton_Click | True | DISCOVERED |
| Configuracoes | ConfigurarPermissoesWindow | FecharButton_Click | True | DISCOVERED |
| Geral | ConfirmacaoCriticaWindow | ConfirmarButton_Click | True | DISCOVERED |
| Geral | ConfirmacaoCriticaWindow | CancelarButton_Click | True | DISCOVERED |
| Clientes | EditarClienteWindow | FecharButton_Click | True | DISCOVERED |
| Clientes | EditarClienteWindow | WhatsAppButton_Click | True | DISCOVERED |
| Clientes | EditarClienteWindow | HistoricoButton_Click | True | DISCOVERED |
| Clientes | EditarClienteWindow | NovaOsButton_Click | True | DISCOVERED |
| Clientes | EditarClienteWindow | AdicionarVeiculoButton_Click | True | DISCOVERED |
| Clientes | EditarClienteWindow | SelecionarFotoButton_Click | True | DISCOVERED |
| Clientes | EditarClienteWindow | RemoverFotoButton_Click | False | NOT TESTABLE (destrutivo — so dialog) |
| Clientes | EditarClienteWindow | SelecionarDocumentoButton_Click | True | DISCOVERED |
| Clientes | EditarClienteWindow | RegistrarAssinaturaButton_Click | True | DISCOVERED |
| Clientes | EditarClienteWindow | AbrirDocumentoButton_Click | True | DISCOVERED |
| Clientes | EditarClienteWindow | AbrirAssinaturaButton_Click | True | DISCOVERED |
| Clientes | EditarClienteWindow | SalvarButton_Click | True | DISCOVERED |
| Fornecedores | EditarFornecedorWindow | FecharButton_Click | True | DISCOVERED |
| Fornecedores | EditarFornecedorWindow | CancelarButton_Click | True | DISCOVERED |
| Fornecedores | EditarFornecedorWindow | SalvarButton_Click | True | DISCOVERED |
| Funcionarios | EditarFuncionarioWindow | SelecionarFotoButton_Click | True | DISCOVERED |
| Funcionarios | EditarFuncionarioWindow | RemoverFotoButton_Click | False | NOT TESTABLE (destrutivo — so dialog) |
| Funcionarios | EditarFuncionarioWindow | SalvarButton_Click | True | DISCOVERED |
| Funcionarios | EditarFuncionarioWindow | CancelarButton_Click | True | DISCOVERED |
| Estoque | EditarProdutoWindow | FecharButton_Click | True | DISCOVERED |
| Estoque | EditarProdutoWindow | SelecionarFotoButton_Click | True | DISCOVERED |
| Estoque | EditarProdutoWindow | RemoverFotoButton_Click | False | NOT TESTABLE (destrutivo — so dialog) |
| Estoque | EditarProdutoWindow | SelecionarAnexosButton_Click | True | DISCOVERED |
| Estoque | EditarProdutoWindow | AbrirAnexoButton_Click | True | DISCOVERED |
| Estoque | EditarProdutoWindow | RemoverAnexoButton_Click | False | NOT TESTABLE (destrutivo — so dialog) |
| Estoque | EditarProdutoWindow | GerarCodigoButton_Click | True | DISCOVERED |
| Estoque | EditarProdutoWindow | SalvarButton_Click | True | DISCOVERED |
| Geral | FriendlyErrorWindow | CopyErrorButton_Click | True | DISCOVERED |
| Geral | FriendlyErrorWindow | CloseButton_Click | True | DISCOVERED |
| Geral | GerenciarPerfisWindow | NovoPerfilButton_Click | True | DISCOVERED |
| Geral | GerenciarPerfisWindow | EditarPerfilButton_Click | True | DISCOVERED |
| Geral | GerenciarPerfisWindow | ExcluirPerfilButton_Click | False | NOT TESTABLE (destrutivo — so dialog) |
| Geral | GerenciarPerfisWindow | SalvarPerfilButton_Click | True | DISCOVERED |
| Geral | GerenciarPerfisWindow | CancelarPerfilButton_Click | True | DISCOVERED |
| Geral | GerenciarPerfisWindow | FecharButton_Click | True | DISCOVERED |
| Clientes | HistoricoClienteWindow | FecharButton_Click | True | DISCOVERED |
| Estoque | HistoricoEstoqueWindow | Fechar_Click | True | DISCOVERED |
| CatalogoPecas | ImportarCatalogoPecasWindow | SelecionarArquivoButton_Click | True | DISCOVERED |
| CatalogoPecas | ImportarCatalogoPecasWindow | GerarPreviaButton_Click | True | DISCOVERED |
| CatalogoPecas | ImportarCatalogoPecasWindow | ConfirmarImportacaoButton_Click | True | DISCOVERED |
| CatalogoPecas | ImportarCatalogoPecasWindow | CancelarButton_Click | True | DISCOVERED |
| Geral | ImportarNotaWindow | SelecionarXmlButton_Click | True | DISCOVERED |
| Geral | ImportarNotaWindow | ImportarButton_Click | True | DISCOVERED |
| Geral | ImportarNotaWindow | CancelarButton_Click | True | DISCOVERED |
| Geral | ImportarNotaWindow | AplicarSugestoesButton_Click | True | DISCOVERED |
| Geral | ImportarNotaWindow | SelecionarTodosButton_Click | True | DISCOVERED |
| Geral | ImportarNotaWindow | IgnorarSelecionadosButton_Click | True | DISCOVERED |
| Geral | LicenseActivationWindow | ActivateButton_Click | True | DISCOVERED |
| Geral | LicenseActivationWindow | CancelButton_Click | True | DISCOVERED |
| Geral | LoginWindow | MostrarSenhaButton_Click | True | DISCOVERED |
| Geral | LoginWindow | LoginButton_Click | True | DISCOVERED |
| Geral | LoginWindow | EsqueciSenhaButton_Click | True | DISCOVERED |
| Geral | LoginWindow | CloseButton_Click | True | DISCOVERED |
| Geral | MainWindow | SidebarToggleButton_Click | True | DISCOVERED |
| Geral | MainWindow | CommandCenterButton_Click | True | DISCOVERED |
| Geral | MainWindow | MenuHelp_Click | True | DISCOVERED |
| ... | ... | (+416 itens) | ... | ... |
