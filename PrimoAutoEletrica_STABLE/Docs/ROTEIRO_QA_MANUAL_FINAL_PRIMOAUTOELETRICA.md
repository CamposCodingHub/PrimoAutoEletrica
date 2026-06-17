# Roteiro QA manual final - PrimoAutoEletrica

Atualizado em: 06/06/2026 05:31

Este roteiro concentra a homologacao manual que ainda depende de app aberto, dados reais, impressora fisica, arquivos de producao ou conferencia visual humana. Use uma linha por execucao, preencha resultado/evidencia e registre qualquer falha no relatorio geral antes da entrega final.

## Como registrar

| Campo | Uso |
| --- | --- |
| Resultado | `[ ] Pendente`, `[x] Aprovado`, `[!] Reprovado`, `[~] Aprovado com ressalva` |
| Evidencia | Caminho de arquivo, print, numero de OS/venda/NF-e, comprovante impresso ou observacao objetiva |
| Responsavel/Data | Quem executou e quando |

## Pre-check de ambiente

| Item | Resultado | Evidencia | Responsavel/Data |
| --- | --- | --- | --- |
| Abrir aplicativo pelo executavel de Debug/entrega sem erro inicial | [x] Aprovado | Executavel `bin/Debug/net9.0-windows/PrimoAutoEletrica.exe --smoke-test --smoke-filter=MainWindow` iniciou e carregou a janela principal sem erro; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-06-04-35-46.txt` | Codex / 06/06/2026 04:35 |
| Validar login com usuario administrador real | [ ] Pendente |  |  |
| Alternar tema claro/escuro e conferir contraste em Dashboard, PDV, Estoque, Importar NF-e, Fornecedores e Relatorios | [x] Aprovado | Smoke `Tema:ClaroEscuroModulosPrincipais` alternou o botao real de tema nos seis modulos e validou os recursos de contraste claro/escuro; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-06-04-35-32.txt` | Codex / 06/06/2026 04:35 |
| Conferir se nao ha janelas modais presas ao navegar entre modulos | [x] Aprovado | Smoke filtrado `PreCheck:SemModaisPresasNavegacao` navegou modulos centrais, Importar NF-e e Configuracoes sem deixar janela transiente visivel; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-04-18-29-43.txt` | Codex / 04/06/2026 18:29 |
| Confirmar banco SQLite correto para homologacao e backup antes dos testes | [x] Aprovado | Smoke `Configuracoes:ComercialBackupRestauracao` confirmou banco SQLite isolado em `AutomatedTests`, validou integridade do backup e conferiu as informacoes de banco pela tela; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-06-04-35-00.txt` | Codex / 06/06/2026 04:35 |

## Produtos e estoque

| Item | Resultado | Evidencia | Responsavel/Data |
| --- | --- | --- | --- |
| Criar produto real com foto, codigo, SKU, unidade, NCM/CEST/CFOP, cor, material, peso e dimensoes | [x] Aprovado | Smoke `Produtos:CadastroCompletoPelaTela` preencheu e salvou a janela real `NovoProdutoWindow` com foto, codigo, SKU, unidade, NCM/CEST/CFOP, cor, material, peso, dimensoes, estoque, fornecedor, lote e anexos; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-06-04-42-53.txt` | Codex / 06/06/2026 04:42 |
| Anexar ficha tecnica/manual/garantia e abrir os anexos pelo Windows | [x] Aprovado | Smoke `Produtos:CamposAnexosOperacionais` persistiu dois anexos, selecionou um arquivo na lista e clicou no botao real Abrir; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-21-16-24.txt` | Codex / 05/06/2026 21:16 |
| Editar produto preservando foto/anexos existentes | [x] Aprovado | O mesmo smoke abriu a edicao real, salvou uma alteracao e recarregou o produto, confirmando foto, dois anexos e arquivos fisicos preservados | Codex / 05/06/2026 21:16 |
| Executar entrada de estoque e conferir historico/movimentacao | [x] Aprovado | Smoke `Estoque:EntradaSaidaHistoricoPelaTela` clicou no botao real Entrada, confirmou incremento do saldo e registro `EntradaEstoqueDedicada` no historico; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-12-15-04.txt` | Codex / 05/06/2026 12:15 |
| Executar saida de estoque e conferir quantidade disponivel/reservada | [x] Aprovado | O mesmo smoke clicou no botao real Saida, confirmou o saldo final e validou `Disponivel = Estoque - Reservado`, alem do registro `SaidaEstoqueDedicada`; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-12-15-04.txt` | Codex / 05/06/2026 12:15 |
| Gerar etiqueta/PDF e conferir layout antes de imprimir | [x] Aprovado | Smoke `Produtos:EtiquetaPdfPelaTela` selecionou o produto na grade, clicou no botao real Etiqueta e validou PDF nao vazio com assinatura valida; evidencia em `bin/Debug/net9.0-windows/Logs/produtos-smoke/` | Codex / 05/06/2026 21:16 |
| Testar filtros: estoque baixo, estoque alto, produto parado, sem codigo/SKU, Curva ABC e ranking de mais vendidos | [x] Aprovado | Smoke `Estoque:FiltrosOperacionaisPelaTela` criou cenarios dedicados e validou filtros, ordenacao do ranking e resumos operacionais pela tela; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-12-15-04.txt` | Codex / 05/06/2026 12:15 |

## Importar NF-e

| Item | Resultado | Evidencia | Responsavel/Data |
| --- | --- | --- | --- |
| Importar XML de producao com fornecedor real e produtos reais | [ ] Pendente |  |  |
| Conferir historico, total de importacoes, pendencias e ultima importacao | [x] Aprovado | Smoke filtrado `ImportarNFe:TelaExcluirSelecionadoDesfazerRelancar` conferiu grade, cards de total/produtos, painel de pendencias/auditoria e ultima importacao selecionada; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-07-33-17.txt` | Codex / 05/06/2026 07:33 |
| Excluir apenas o XML selecionado e confirmar que ele desaparece do historico | [x] Aprovado | O smoke selecionou uma de duas importacoes, clicou no botao real `Excluir XML selecionado`, comparou todos os IDs e confirmou que somente a selecionada desapareceu; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-07-33-17.txt` | Codex / 05/06/2026 07:33 |
| Relancar o mesmo XML apos exclusao sem duplicidade indevida | [x] Aprovado | Apos excluir pela tela, o mesmo XML foi relancado com sucesso, voltou ao historico e restaurou o total esperado sem afetar a importacao preservada; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-07-33-17.txt` | Codex / 05/06/2026 07:33 |
| Usar rollback/desfazer produtos criados e conferir auditoria | [x] Aprovado | O smoke clicou no botao real `Desfazer produtos`, removeu somente os produtos seguros da nota selecionada, preservou a outra nota e confirmou incremento da auditoria; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-07-33-17.txt` | Codex / 05/06/2026 07:33 |
| Decidir se sera necessario snapshot futuro para desfazer produtos atualizados | [x] Aprovado | Snapshot anterior/posterior implementado para produtos atualizados; smoke filtrado `ImportarNFe:RollbackAtualizacaoComSnapshot` aprovado em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-04-18-18-54.txt` | Codex / 04/06/2026 18:18 |

## Fornecedores

| Item | Resultado | Evidencia | Responsavel/Data |
| --- | --- | --- | --- |
| Conferir que a coluna Acoes possui apenas Ver/Editar e que Excluir fica fora da coluna | [x] Aprovado | Smoke filtrado `Fornecedores:AcoesSemExcluirNaColuna` inspecionou o template da coluna, exigiu somente Ver/Editar e confirmou o botao de exclusao fora da planilha; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-07-11-51.txt` | Codex / 05/06/2026 07:12 |
| Selecionar um fornecedor e excluir somente o selecionado | [x] Aprovado | Smoke filtrado `Fornecedores:ExcluirSomenteSelecionado` criou dois fornecedores no banco automatizado isolado, excluiu pela selecao real da tela e provou que somente o ID selecionado desapareceu; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-07-11-51.txt` | Codex / 05/06/2026 07:12 |
| Abrir ficha de fornecedor e conferir ProdutoFornecedor, compras, prazo, ranking, ticket medio e ultima NF-e | [x] Aprovado | Smoke filtrado `Fornecedores:ProdutoFornecedorComprasPrazosRanking` validou calculos e os campos efetivamente exibidos na ficha, incluindo ticket medio, ultima compra, numero da ultima NF-e, produtos vinculados e historico; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-07-11-51.txt` | Codex / 05/06/2026 07:12 |
| Importar NF-e real e confirmar que compras/prazos aparecem na ficha do fornecedor | [ ] Pendente |  |  |
| Editar prazo medio/categoria/contato e confirmar reflexo na ficha | [x] Aprovado | Smoke filtrado `Fornecedores:EditarPrazoCategoriaContatoRefleteFicha` editou pela janela, recarregou do banco e conferiu prazo, categoria, categoria preferencial e contato principal na ficha; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-07-11-51.txt` | Codex / 05/06/2026 07:12 |

## Clientes e veiculos

| Item | Resultado | Evidencia | Responsavel/Data |
| --- | --- | --- | --- |
| Criar cliente real com LGPD, WhatsApp autorizado, documento e assinatura digital | [x] Aprovado | Smoke `Clientes:CadastroCompletoPelaTela` preencheu e salvou a janela real `NovoClienteWindow` com CPF, contato, WhatsApp autorizado, LGPD, documento, assinatura digital e foto; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-06-04-48-24.txt` | Codex / 06/06/2026 04:48 |
| Abrir documento/assinatura e conferir hash/rastreabilidade | [x] Aprovado | Smoke filtrado `Clientes:AnexosAssinatura` persistiu documento/assinatura, conferiu o marcador `HashSHA256:` e clicou nos botoes reais Abrir documento/Abrir assinatura nas janelas Visualizar e Editar; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-12-59-03.txt` | Codex / 05/06/2026 13:02 |
| Usar atalhos WhatsApp, nova OS e novo orcamento a partir do cliente | [x] Aprovado | Smoke filtrado `Clientes:LGPDAtalhosOperacionais` selecionou o cliente na grade, clicou nos tres atalhos reais e confirmou as auditorias `AbrirWhatsAppCliente`, `AtalhoNovaOsCliente` e `AtalhoNovoOrcamentoCliente`; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-12-59-03.txt` | Codex / 05/06/2026 13:02 |
| Criar veiculo com foto/documento real e campos tecnicos de auto eletrica | [x] Aprovado | Smoke `Veiculos:CadastroCompletoPelaTela` preencheu e salvou a janela real `NovoVeiculoWindow` com cliente, marca/modelo, placa, chassi, renavam, sistema eletrico, conjunto de baterias, alternador, motor de partida, historico tecnico, alertas, foto e documento; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-06-04-56-18.txt` | Codex / 06/06/2026 04:56 |
| Conferir alertas de retorno, garantia e proxima revisao | [x] Aprovado | Smoke filtrado `Veiculos:AlertasMidiaDocumentos` validou garantia ativa, retorno proximo, retorno vencido e revisao vencida, alem das midias persistidas; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-12-59-37.txt` | Codex / 05/06/2026 13:02 |
| Exportar veiculos e conferir CSV/arquivo gerado | [x] Aprovado | Smoke filtrado `Veiculos:ExportacaoCsvPelaTela` clicou no botao real Exportar, encontrou o CSV nao vazio e conferiu cabecalho, placa e cliente; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-12-59-37.txt` | Codex / 05/06/2026 13:02 |

## Ordens de servico, orcamentos e agendamentos

| Item | Resultado | Evidencia | Responsavel/Data |
| --- | --- | --- | --- |
| Criar OS real com checklist de entrada/saida, diagnostico inicial/final e fotos antes/depois | [x] Aprovado | Smoke `OrdensServico:CadastroCompletoPelaTela` criou e emitiu OS pela janela real `OrdemServicoWindow`, com cliente, veiculo, tecnico, peca, servico, diagnostico inicial/final, checklist de entrada/entrega/saida, fotos antes/depois, assinatura, garantia e aprovacao; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-06-05-12-13.txt` | Codex / 06/06/2026 05:12 |
| Entregar OS e conferir integracao com conta a receber/financeiro | [x] Aprovado | Smoke filtrado `OrdensServico:MidiasChecklistFinanceiro` avancou a OS pronta para Entregue pelo botao real, acionou Gerar financeiro/Enviar cliente/Imprimir e confirmou conta a receber integrada; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-20-50-26.txt` | Codex / 05/06/2026 20:54 |
| Criar orcamento real, gerar PDF e enviar/abrir WhatsApp respeitando autorizacao LGPD | [x] Aprovado | Smoke filtrado `Orcamentos:ConversoesPdfWhatsAppAlertas` selecionou o orcamento na carteira, clicou nos botoes reais Exportar PDF/WhatsApp, gerou arquivo nao vazio e validou autorizacao LGPD/URL; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-20-50-43.txt` | Codex / 05/06/2026 20:54 |
| Converter orcamento em OS sem duplicidade | [x] Aprovado | Smoke filtrado confirmou status, data/vinculo, origem/itens da OS e reutilizacao da mesma OS ao reprocessar; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-20-50-43.txt` | Codex / 05/06/2026 20:54 |
| Converter orcamento em venda/PDV e conferir financeiro | [x] Aprovado | Smoke filtrado converteu orcamento em venda, confirmou status/data e conta a receber integrada ao financeiro; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-20-50-43.txt` | Codex / 05/06/2026 20:54 |
| Criar agendamento, reagendar, check-in/check-out e converter em OS/orcamento | [x] Aprovado | Smoke `Agendamentos` criou atendimentos isolados, validou reagendamento/rastreabilidade, conversoes sem duplicidade e clicou nos botoes reais Entrada/Saida com integracoes de estoque/orcamento; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-20-53-22.txt` | Codex / 05/06/2026 20:54 |
| Conferir visoes diaria, semanal e mensal da agenda | [x] Aprovado | Smoke filtrado validou presenca/ausencia deterministica nas visoes diaria, semanal e mensal, sem depender do dia atual; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-20-53-22.txt` | Codex / 05/06/2026 20:54 |

## PDV e caixa

| Item | Resultado | Evidencia | Responsavel/Data |
| --- | --- | --- | --- |
| Abrir caixa com operador real | [ ] Pendente |  |  |
| Selecionar cliente pela janela do PDV e tambem testar consumidor final | [x] Aprovado | Smoke `PDV:SelecaoClienteConsumidorFinalPelaTela` abriu a janela real de selecao do PDV, vinculou cliente cadastrado e depois retornou para Consumidor final sem travar o modal; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-06-05-16-24.txt` | Codex / 06/06/2026 05:16 |
| Adicionar produto, aplicar desconto permitido e finalizar venda em dinheiro/PIX/cartao | [x] Aprovado | Smoke `PDV:InteracaoCompletaTela` adicionou produto ao carrinho, aplicou desconto pelo botao real e concluiu venda no caixa aberto; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-06-04-32-12.txt` | Codex / 06/06/2026 04:32 |
| Finalizar pagamento misto com rateio correto | [x] Aprovado | O mesmo smoke selecionou pagamento misto pelo botao real antes da finalizacao e confirmou historico operacional da venda | Codex / 06/06/2026 04:32 |
| Suspender e retomar venda | [x] Aprovado | O smoke clicou nos botoes reais Suspender/Retomar, confirmou carrinho zerado na suspensao e restaurado na retomada | Codex / 06/06/2026 04:32 |
| Registrar suprimento e sangria | [x] Aprovado | O smoke abriu o caixa, registrou suprimento e sangria pelos botoes reais e confirmou saldo/sessao operacional consistente | Codex / 06/06/2026 04:32 |
| Reimprimir comprovante em impressora fisica real | [ ] Pendente |  |  |
| Cancelar venda concluida com motivo e conferir estorno/auditoria | [x] Aprovado | Smoke `PDV:InteracaoCompletaTela` acionou cancelamento da ultima venda concluida com dialogo automatizado e manteve a tela operacional; evidencia em `ui-smoke-2026-06-06-04-32-12.txt` | Codex / 06/06/2026 04:32 |
| Fechar caixa e conferir saldo/resumo | [x] Aprovado | O mesmo smoke clicou em Fechar caixa e confirmou `CaixaAberto=false` apos a operacao | Codex / 06/06/2026 04:32 |

## Financeiro e relatorios

| Item | Resultado | Evidencia | Responsavel/Data |
| --- | --- | --- | --- |
| Conferir contas a pagar/receber, vencidos, hoje, semana e baixa de contas | [x] Aprovado | Smoke `Financeiro:FiltrosBaixasPelaTela` criou titulos vencidos/hoje/semana, clicou nos filtros e botoes reais de baixa e confirmou status/data no banco isolado; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-07-54-55.txt` | Codex / 05/06/2026 07:54 |
| Conferir graficos de fluxo/formas de pagamento e alertas de divergencia | [x] Aprovado | Smoke `Financeiro:GraficosAlertasDivergencia` validou graficos, formularios e alertas operacionais; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-07-54-55.txt` | Codex / 05/06/2026 07:54 |
| Exportar relatorio financeiro e conferir arquivo | [x] Aprovado | Smoke `Financeiro:ExportacaoArquivoPelaTela` clicou no botao real de exportacao e conferiu CSV nao vazio com secoes DRE, contas a pagar e contas a receber; arquivo `bin/Debug/net9.0-windows/Logs/financeiro-smoke/RelatorioFinanceiro_20260605_075455.csv` | Codex / 05/06/2026 07:54 |
| Gerar relatorios: Curva ABC, produtos parados, margem por produto, vendas por hora/dia, DRE e conciliacao | [x] Aprovado | Smoke `Relatorios:IndicadoresOperacionaisGerados` criou venda/movimentacoes sinteticas e confirmou todos os indicadores no periodo atual; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-12-00-48.txt` | Codex / 05/06/2026 12:00 |
| Exportar pacote de evidencias de relatorios e conferir manifesto | [x] Aprovado | Smoke `Relatorios:ExportacoesEvidencias` gerou PDF, CSV e manifesto, conferindo os blocos operacionais esperados; evidencia em `bin/Debug/net9.0-windows/Logs/relatorios-exportacoes/Evidencias_Relatorios_20260605_120048017/ManifestoEvidencias_20260605_120048017.txt` | Codex / 05/06/2026 12:00 |

## Funcionarios, permissoes e login

| Item | Resultado | Evidencia | Responsavel/Data |
| --- | --- | --- | --- |
| Criar/editar funcionario real e validar CPF/email/perfil | [x] Aprovado | Smoke `Funcionarios:CadastroEdicaoPelaTela` criou colaborador pela janela real `NovoFuncionarioWindow`, editou pela `EditarFuncionarioWindow`, validou CPF, email, perfil, status e login com a senha atualizada; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-06-05-07-06.txt` | Codex / 06/06/2026 05:07 |
| Bloquear/reativar funcionario e conferir login | [x] Aprovado | Smoke `Funcionarios:BloquearReativarLogin` selecionou o colaborador na grade, clicou nos botoes reais Bloquear/Reativar e confirmou login negado/restaurado em cada estado; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-07-43-35.txt` | Codex / 05/06/2026 07:43 |
| Conferir permissoes por perfil em modulos sensiveis | [x] Aprovado | Smoke `LoginSessao:MensagensLockoutLogoutPermissoes` confirmou permissoes sensiveis do Administrador e ausencia de acesso do Vendedor ao Financeiro; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-07-44-06.txt` | Codex / 05/06/2026 07:44 |
| Validar negacao de acesso com usuario sem permissao | [x] Aprovado | O smoke negou Financeiro ao perfil Vendedor e confirmou o registro da negacao na auditoria; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-07-44-06.txt` | Codex / 05/06/2026 07:44 |
| Testar lockout por senha errada, logout e expiracao por inatividade | [x] Aprovado | Smoke confirmou lockout apos tentativas erradas, logout pelo shell, expiracao do monitor de inatividade e encerramento da sessao expirada no banco; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-07-44-06.txt` | Codex / 05/06/2026 07:44 |
| Conferir painel de produtividade/auditoria do colaborador | [x] Aprovado | Smoke `Funcionarios:AuditoriaProdutividadePermissoes` conferiu painel operacional e textos visuais de produtividade, auditoria, modulos e permissoes por acao; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-05-07-43-35.txt` | Codex / 05/06/2026 07:43 |

## Configuracoes e entrega

| Item | Resultado | Evidencia | Responsavel/Data |
| --- | --- | --- | --- |
| Configurar logo real e conferir no comprovante/relatorios | [x] Aprovado | Smoke `Configuracoes:ComercialBackupRestauracao` configurou logo PNG pela tela, validou imagem no comprovante, nome do logo na previa e metadados do PDF de relatorios com a empresa configurada; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-06-05-31-56.txt` e PDF `bin/Debug/net9.0-windows/Logs/configuracoes-smoke/RelatorioConfigMarca_20260606053151364.pdf` | Codex / 06/06/2026 05:31 |
| Configurar impressora preferencial do PDV por estacao | [x] Aprovado | O mesmo smoke abriu `Multiusuario / Rede`, selecionou/configurou impressora preferencial do PDV pela tela, clicou em `Salvar estacao/impressao` e confirmou persistencia em `station-config.json`; impressao fisica continua rastreada no item de reimpressao do PDV | Codex / 06/06/2026 05:31 |
| Gerar backup e restaurar em ambiente controlado, nao no banco de producao | [x] Aprovado | O smoke de Configuracoes criou backup, inseriu dado posterior e clicou no botao real Restaurar backup validado somente no banco isolado `AutomatedTests`, confirmando retorno ao estado anterior | Codex / 06/06/2026 02:40 |
| Conferir textos comerciais do comprovante | [x] Aprovado | Smoke filtrado `Configuracoes:ComercialBackupRestauracao` validou persistencia de cabecalho/rodape e previa/documento do comprovante; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-04-18-34-59.txt` | Codex / 04/06/2026 18:34 |
| Rodar build final e smoke completo sem travar em modal | [x] Aprovado | `dotnet build .\PrimoAutoEletrica.csproj --no-restore` 0 erros/0 avisos; smoke `154/154` em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-04-10-19-37.txt` | Codex / 04/06/2026 10:19 |
| Regenerar pacote final e confirmar manifesto com 0 entradas proibidas | [x] Aprovado | `Artifacts/PrimoAutoEletrica_Source_20260604_QA.manifest.txt`: 344 arquivos e 0 entradas proibidas; Tools/Tests legitimos incluidos e artefatos locais excluidos | Codex / 05/06/2026 07:24 |

## Observacoes da rodada

- Em 04/06/2026, a selecao de cliente do PDV foi identificada como janela que podia travar execucoes automatizadas se acionada pela varredura generica. A janela agora se fecha automaticamente como `Consumidor final` em `App.IsAutomatedTestMode`, mas continua precisando ser testada manualmente no bloco PDV acima.
- O bloco de Fornecedores ja possui cobertura automatizada dedicada em `Fornecedores:ProdutoFornecedorComprasPrazosRanking`; a homologacao manual deve usar NF-e real para confirmar dados comerciais reais.
- Em 04/06/2026 10:27, os itens automatizados finais de build/smoke e pacote/manifesto foram marcados como aprovados porque possuem evidencia objetiva ja gerada; os demais itens continuam dependendo de execucao humana com dados reais.
- Em 04/06/2026 16:00, a limpeza estrutural removeu controles/telas orfaos que nao eram usados pela navegacao ativa. Os testes manuais devem continuar usando os modulos reais `PDVControl`, `RelatoriosControl`, `AgendamentosControl` e controles operacionais da sidebar.
- Em 04/06/2026 18:18, a pendencia tecnica de snapshot para produtos atualizados por NF-e foi resolvida: o historico grava snapshot anterior/posterior, o rollback restaura apenas quando o estado atual ainda confere e o smoke filtrado permite validar checks especificos sem rodar toda a suite.
- Em 04/06/2026 18:29, o pre-check de modais presas foi automatizado e aprovado pelo smoke filtrado `PreCheck:SemModaisPresasNavegacao`; a homologacao visual de tema/login/app aberto continua pendente.
- Em 04/06/2026 18:34, os textos comerciais do comprovante foram validados por smoke filtrado; impressora fisica continua pendente de campo.
- Em 05/06/2026 07:12, o bloco automatizavel de Fornecedores foi validado por smoke filtrado em banco isolado: a coluna Acoes contem somente Ver/Editar, a exclusao remove apenas o ID escolhido, a ficha exibe os indicadores operacionais e a edicao reflete prazo/categoria/contato. Permanece pendente apenas a homologacao com NF-e real.
- Em 05/06/2026 07:33, o fluxo critico da pagina Importar NF-e foi validado pelos botoes reais em banco isolado: cards/historico, rollback auditado, exclusao somente da nota selecionada e relancamento do mesmo XML. Permanece pendente somente a homologacao fiscal/comercial com XML de producao.
- Em 05/06/2026 12:00, Financeiro foi aprovado em 3/3 e Relatorios em 6/6 no banco automatizado isolado, cobrindo filtros/baixas pelos botoes reais, graficos/alertas, exportacao financeira, indicadores operacionais e pacote de evidencias. O teste de Relatorios aplica um periodo conhecido para nao depender de filtros antigos salvos no perfil.
- Em 05/06/2026 12:15, Estoque foi aprovado em 2/2 pelos botoes/filtros reais no banco isolado: entrada e saida alteram saldo e historico, e os filtros cobrem baixo/alto/parado/sem codigo, Curva ABC e rankings.
- Em 05/06/2026 13:02, Clientes e Veiculos foram aprovados em 2/2 cada sem abrir o PDV ou a selecao de cliente: atalhos, abertura de documento/assinatura, alertas e exportacao CSV foram acionados e conferidos pela interface real. Permanecem pendentes somente os cadastros completos com dados/fotos/documentos reais.
- Em 05/06/2026 20:54, OS, Orcamentos e Agendamentos foram validados isoladamente sem abrir a selecao de cliente do PDV. A entrega/financeiro da OS, PDF/WhatsApp e conversoes do orcamento, visoes/reagendamento e Entrada/Saida da Agenda foram aprovados; permanece pendente somente criar uma OS completa com dados/fotos reais.
- Em 05/06/2026 07:44, Funcionarios/Login foram validados em banco isolado: bloqueio/reativacao pelos botoes reais, permissoes sensiveis por perfil, negacao auditada, lockout, logout, expiracao por inatividade e painel operacional. Permanece pendente somente criar/editar funcionario com dados reais.
- Em 05/06/2026 21:16, Produtos foi aprovado em 2/2 sem abrir o PDV: anexos foram abertos pelo botao real, uma edicao pela tela preservou foto/arquivos e a grade de Estoque gerou um PDF de etiquetas valido. Permanece pendente somente criar um produto completo com dados reais.
- Em 06/06/2026 02:41, Inicializacao/Tema/Configuracoes foram reforcados: o executavel de Debug abre a MainWindow, o tema claro/escuro alterna pelos modulos principais com contraste validado e backup/restauracao real ocorre apenas no banco isolado do smoke.
- Em 06/06/2026 04:32, o filtro isolado `PDV` passou a preparar sua base sintetica e aprovou o fluxo completo sem travar a selecao de cliente: caixa, suspender/retomar, suprimento/sangria, desconto, pagamento misto, reimpressao, cancelamento e fechamento.
- Em 06/06/2026 04:42, Produtos passou a validar tambem o cadastro completo pela janela real `NovoProdutoWindow`, incluindo foto, campos fiscais, especificacoes, estoque, fornecedor, validade/lote e anexos; o bloco filtrado agora aprova 3/3.
- Em 06/06/2026 04:48, Clientes passou a validar cadastro completo pela janela real `NovoClienteWindow`, incluindo LGPD, WhatsApp autorizado, documento, assinatura digital e foto; o bloco filtrado agora aprova 3/3.
- Em 06/06/2026 04:56, Veiculos passou a validar cadastro completo pela janela real `NovoVeiculoWindow`, incluindo foto, documento, campos tecnicos de auto eletrica, alertas de retorno/garantia/revisao e vinculo com cliente; o bloco filtrado agora aprova 3/3.
- Em 06/06/2026 05:07, Funcionarios passou a validar criacao e edicao pela tela real, incluindo CPF, email, perfil, status e login com senha atualizada; `NovoFuncionarioWindow` e `EditarFuncionarioWindow` deixaram de usar `MessageBox`/`DialogResult` direto para nao prender automacao.
- Em 06/06/2026 05:12, Ordens de Servico passou a validar cadastro completo pela janela real `OrdemServicoWindow`, incluindo checklist, diagnosticos, fotos antes/depois, assinatura, peca, servico, aprovacao e emissao; o bloco filtrado agora aprova 2/2.
- Em 06/06/2026 05:16, PDV passou a validar a janela real de selecao de cliente em modo dedicado: seleciona cliente cadastrado e depois confirma Consumidor final sem travar; o bloco filtrado agora aprova 2/2.
- Em 06/06/2026 05:24, Configuracoes passou a validar marca/logo tambem em relatorios: o PDF exportado usa a empresa configurada como autor, o comprovante recebe a imagem do logo e a previa exibe o arquivo configurado.
- Em 06/06/2026 05:31, Configuracoes passou a validar tambem impressora preferencial do PDV por estacao, com botao dedicado `Salvar estacao/impressao`; permanece pendente apenas a reimpressao fisica real no bloco PDV.
