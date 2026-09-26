# PRIMOX Workshop — Full Functional QA
**Auditoria Funcional Completa de Telas, Funções, Botões, Modais e Janelas**

---

## 1. Identificação

| Item | Informação |
|---|---|
| **Data da Auditoria** | 25 de Setembro de 2026 |
| **Sistema Operacional** | Windows 11 Pro 64-bit (10.0.26200 Build 26200) |
| **Arquitetura** | x64 (AMD64) |
| **Branch Git Atual** | `cycle-c1/operational-intelligence` |
| **Commit Atual** | `8a397fe6e28ab3591848e59713cb077b02e2070a` |
| **Commit da Branch `main`** | `29b19b16d0e6e3413bdba20c505e20c992596c24` (Intacta, preservada) |
| **Versão do .NET Runtime** | .NET 10.0.302 (Self-contained win-x64) |
| **Caminho do EXE Instalado** | `C:\Users\campo\AppData\Local\PrimoAutoEletrica\App\PrimoAutoEletrica.exe` |
| **Tamanho do EXE** | 203.776 bytes |
| **Hash SHA256 do EXE** | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` |
| **Banco Protegido** | `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db` |
| **Status ReadOnly do Banco Protegido** | `True` (Atributo de Somente Leitura ativado) |
| **Hash SHA256 Banco Protegido** | `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` (Inalterado) |
| **Banco Operacional Desktop** | `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto_operacional.db` |
| **Hash SHA256 Banco Operacional** | `9E6B8587418BB0282F4EA9B641E114BB4E3F4646E88A3D20E6B4D8B603B39D97` (Inalterado) |
| **Diretório de Logs** | `C:\Users\campo\AppData\Local\PrimoAutoEletrica\App\Logs` |
| **Sandbox de Testes Automatizados** | `C:\Users\campo\AppData\Local\Temp\PrimoAuto_Automated\AutomatedTests` |

---

## 2. Objetivo

Executar uma auditoria funcional COMPLETA do PRIMOX Workshop instalado no Desktop, em modo **ESTRITAMENTE DIAGNÓSTICO**, atendendo aos seguintes princípios:
1. Abrir o programa real e interagir com o ambiente ativo no Windows.
2. Mapear todas as telas acessíveis, janelas, modais e UserControls.
3. Testar toda a malha de navegação e transição de módulos.
4. Testar todos os botões interativos e seus gatilhos de comando.
5. Catalogar formulários, filtros, pesquisas, seleções e comportamentos visuais.
6. Detectar qualquer popup de erro ou falha de binding/XAML/código.
7. Capturar mensagens exatas, InnerExceptions e stack traces sem paráfrases.
8. Não mascarar falhas, não transformar erro em PASS e não iniciar o Ciclo C2.
9. Manter a branch `main` e os bancos protegido e operacional intactos.

---

## 3. Proteções

Todas as proteções mandatórias estabelecidas foram cumpridas com rigor absoluto:
- **Proteção do Banco Protegido (`primoauto.db`)**:
  - SHA256 Inicial: `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B`
  - SHA256 Final: `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B`
  - Estado: **100% IDÊNTICO**. Nenhuma gravação efetuada. `IsReadOnly = True`.
  - Integridade SQLite: `ok` | Foreign Key Check: `0 erros`.
- **Proteção do Banco Operacional (`primoauto_operacional.db`)**:
  - SHA256 Inicial: `9E6B8587418BB0282F4EA9B641E114BB4E3F4646E88A3D20E6B4D8B603B39D97`
  - SHA256 Final: `9E6B8587418BB0282F4EA9B641E114BB4E3F4646E88A3D20E6B4D8B603B39D97`
  - Estado: **100% IDÊNTICO**. Nenhuma gravação residual de teste efetuada.
- **Isolamento de Testes**:
  - Todos os testes de UI, smoke tests interativos e workflow tests foram direcionados para o ambiente sandbox descartável (`%LOCALAPPDATA%\Temp\PrimoAuto_Automated\AutomatedTests`).
- **Código Fonte e Repositório Git**:
  - Nenhuma alteração corretiva ("fix") foi implementada durante o diagnóstico.
  - Branch `main` permaneceu intacta.

---

## 4. Inventário de Telas (UserControls & Módulos Canônicos)

Catalogação exata a partir da solução em `PrimoAutoEletrica/UserControls` e do `NavigationService.cs`:

| ID | Nome do UserControl | Módulo de Navegação | Arquivo XAML | Finalidade Operacional |
|---|---|---|---|---|
| UC-01 | `DashboardControl` | `Dashboard` | `UserControls/DashboardControl.xaml` | Centro de comando, atalhos rápidos e KPIs |
| UC-02 | `ProdutosControl` | `Estoque` | `UserControls/ProdutosControl.xaml` | Listagem de produtos, estoque mínimo e etiquetas |
| UC-03 | `EstoqueControl` | `Estoque` | `UserControls/EstoqueControl.xaml` | Movimentação, entradas, saídas e ajustes |
| UC-04 | `ClientesControl` | `Clientes` | `UserControls/ClientesControl.xaml` | Gestão de clientes, LGPD e atalhos rápidos |
| UC-05 | `VeiculosControl` | `Veiculos` | `UserControls/VeiculosControl.xaml` | Cadastro técnico de veículos e prontuário |
| UC-06 | `OrcamentosControl` | `Orcamentos` | `UserControls/OrcamentosControl.xaml` | Gestão da carteira comercial de orçamentos |
| UC-07 | `OrdensServicoControl` | `OrdensServico` | `UserControls/OrdensServicoControl.xaml` | Ordens de serviço em andamento e histórico |
| UC-08 | `OficinaKanbanControl` | `OficinaKanban` | `UserControls/OficinaKanbanControl.xaml` | Quadro Kanban operacional de bancada e pátio |
| UC-09 | `AgendamentosControl` | `Agendamentos` | `UserControls/AgendamentosControl.xaml` | Agenda integrada, timeline e agendamento |
| UC-10 | `PdvControl` | `PDV` | `UserControls/PdvControl.xaml` | Ponto de venda rápido, caixa e comprovantes |
| UC-11 | `FinanceiroControl` | `Financeiro` | `UserControls/FinanceiroControl.xaml` | Contas a pagar/receber, conciliação e DRE |
| UC-12 | `RelatoriosControl` | `Relatorios` | `UserControls/RelatoriosControl.xaml` | Relatórios gerenciais, exportações PDF/Excel |
| UC-13 | `FornecedoresControl` | `Fornecedores` | `UserControls/FornecedoresControl.xaml` | Gestão de fornecedores e compras vinculadas |
| UC-14 | `FuncionariosControl` | `Funcionarios` | `UserControls/FuncionariosControl.xaml` | Gestão de colaboradores e produtividade |
| UC-15 | `AuditoriaControl` | `Auditoria` | `UserControls/AuditoriaControl.xaml` | Trilha de auditoria persistente e eventos |
| UC-16 | `ImportarNFeControl` | `ImportarNFe` | `UserControls/ImportarNFeControl.xaml` | Importação de XML de NF-e e conciliação |
| UC-17 | `CatalogoPecasControl` | `CatalogoPecas` | `UserControls/CatalogoPecasControl.xaml` | Consulta a catálogo e integração com estoque |
| UC-18 | `FiscalOperationsControl` | `FiscalOperacoes` | `UserControls/FiscalOperationsControl.xaml` | Painel de operações fiscais e documentos |
| UC-19 | `AutoEletricaControl` | `AutoEletrica` | `UserControls/AutoEletricaControl.xaml` | Prontuário técnico elétrico (D01-D17) |
| UC-20 | `BaseConhecimentoControl` | `BaseConhecimento` | `UserControls/BaseConhecimentoControl.xaml` | Boletins técnicos e base de bancada |
| UC-21 | `FerramentasControl` | `Ferramentas` | `UserControls/FerramentasControl.xaml` | Controle de ferramentas, retiradas e calibração |
| UC-22 | `NecessidadesCompraControl` | `NecessidadesCompra` | `UserControls/NecessidadesCompraControl.xaml` | Sugestões de reposição e requisições |
| UC-23 | `ConfiguracoesControl` | `Configuracoes` | `UserControls/ConfiguracoesControl.xaml` | Parâmetros do sistema, marca e dados da oficina |
| UC-24 | `HelpControl` | `Ajuda` | `UserControls/HelpControl.xaml` | Central de ajuda ao usuário e documentação |
| UC-25 | `GlobalSearchControl` | — | `UserControls/GlobalSearchControl.xaml` | Busca global no shell |
| UC-26 | `PrimoxAssistControl` | — | `UserControls/PrimoxAssistControl.xaml` | Assistente técnico grounded fail-closed |
| UC-27 | `OrcamentoAlertasControl` | — | `UserControls/OrcamentoAlertasControl.xaml` | Componente de alertas de orçamento |
| UC-28 | `OrcamentoBuscaInteligenteControl` | — | `UserControls/OrcamentoBuscaInteligenteControl.xaml` | Busca de peças para montagem de orçamento |
| UC-29 | `OrcamentoCarrinhoControl` | — | `UserControls/OrcamentoCarrinhoControl.xaml` | Carrinho de itens de orçamento |
| UC-30 | `OrcamentoClientePanelControl` | — | `UserControls/OrcamentoClientePanelControl.xaml` | Painel de cliente no orçamento |
| UC-31 | `OrcamentoHistoricoNegociacaoControl` | — | `UserControls/OrcamentoHistoricoNegociacaoControl.xaml` | Timeline de negociação comercial |

---

## 5. Inventário de Janelas (Windows Secundárias)

Catalogação de todas as 60 `Window` declaradas na aplicação:

1. `MainWindow` (Shell principal da aplicação)
2. `LoginWindow` (Autenticação e sessão de usuário)
3. `PrimeiraExecucaoWindow` (Assistente de inicialização e onboarding)
4. `SelecaoFilialWindow` (Seleção de unidade/filial operacional)
5. `NovoClienteWindow` (Cadastro completo de cliente com LGPD e anexos)
6. `EditarClienteWindow` (Edição com concorrência e bloqueio otimista)
7. `VisualizarClienteWindow` (Ficha resumida do cliente)
8. `HistoricoClienteWindow` (Cliente 360 com histórico multi-entidade)
9. `NovoVeiculoWindow` (Cadastro técnico de veículo e sistema elétrico)
10. `VisualizarVeiculoWindow` (Vehicle 360 com histórico técnico completo)
11. `NovoProdutoWindow` (Cadastro de peça com NCM, tributação, peso e fotos)
12. `EditarProdutoWindow` (Atualização de dados e estoques)
13. `HistoricoEstoqueWindow` (Trilha de movimentação de saldo e reservas)
14. `TransferirEstoqueWindow` (Transferência de itens entre locais/almoxarifados)
15. `AjusteEstoqueWindow` (Ajuste de inventário físico com justificativa)
16. `NovoFornecedorWindow` (Cadastro de fornecedor comercial)
17. `EditarFornecedorWindow` (Edição de prazos médios e contatos)
18. `VisualizarFornecedorWindow` (Ficha de compras e estatísticas de fornecedor)
19. `NovoFuncionarioWindow` (Cadastro de colaborador com perfil RBAC)
20. `EditarFuncionarioWindow` (Edição e redefinição de credenciais)
21. `GerenciarPerfisWindow` (Listagem e permissões de perfis de acesso)
22. `NovoPerfilWindow` (Criação de novos perfis granulares)
23. `ConfigurarPermissoesWindow` (Matriz visual de permissões granulares)
24. `NovoOrcamentoWindow` (Montagem completa de orçamento comercial)
25. `OrcamentosView` (Visualização dedicada da carteira de propostas)
26. `OrcamentoRapidoWindow` (Emissão expressa de orçamento balcão)
27. `SelecionarOrcamentoWindow` (Seletor modal de orçamento para conversão)
28. `ReenviarOrcamentosWindow` (Disparo em lote via WhatsApp / e-mail)
29. `OrdemServicoWindow` (Ficha operacional de OS com checklist e mídias)
30. `ChecklistTecnicoWindow` (Checklist de entrada, execução e entrega)
31. `PosVendaWindow` (Follow-up pós-serviço e satisfação)
32. `NovoAgendamentoPremiumWindow` (Agendamento de bancada com previsão de peças)
33. `LembretesRevisaoWindow` (Central de retornos preventivos e revisões)
34. `GarantiaRetornosWindow` (Gestão de garantias ativas e retornos)
35. `OperacaoCaixaWindow` (Abertura, fechamento, sangria e suprimento)
36. `RegistrarPagamentoWindow` (Liquidação financeira com formas de pagamento)
37. `PagamentoMistoWindow` (Composição de múltiplos meios de pagamento no PDV)
38. `ImportarNotaWindow` (Processamento de XML de NF-e)
39. `VisualizarXmlWindow` (Visualização estruturada do XML importado)
40. `ImportarCatalogoPecasWindow` (Carga massiva de catálogo de fabricantes)
41. `RevisarCatalogoPecaWindow` (Ajuste de item de catálogo importado)
42. `AssociarCatalogoPecaWindow` (Vínculo de catálogo com produto do estoque)
43. `CompatibilidadeVeiculosWindow` (Tabela de aplicações peça-veículo)
44. `ComissaoSettlementWindow` (Fechamento e quitação de comissões de técnicos)
45. `ConfiguracoesSistemaWindow` (Configurações avançadas de sistema e BD)
46. `BackupRestauracaoWindow` (Criação, integridade e restore de cópias)
47. `AuditoriaAvancadaWindow` (Filtros forenses na trilha de auditoria)
48. `UsuariosOnlineWindow` (Monitor de sessões concorrentes ativas)
49. `AtalhosTecladoWindow` (Guia de teclas de atalho do operador)
50. `SobreWindow` (Créditos, compilação e licença)
51. `RelatorioPreviaWindow` (Visualizador nativo de prévia de impressão)
52. `Tool360Window` (Ficha 360 da ferramenta com QR e histórico de uso)
53. `NovaFerramentaDialog` (Cadastro de nova ferramenta no acervo)
54. `ToolCheckoutDialog` (Retirada de ferramenta por técnico/OS)
55. `ToolReturnDialog` (Devolução com inspeção visual de avarias)
56. `ToolMaintenanceDialog` (Registro de calibração ou manutenção corretiva)
57. `NovaRequisicaoCompraDialog` (Requisição formal de compra de suprimentos)
58. `AprovacaoRequisicaoDialog` (Aprovação gerencial de compra)
59. `CasoTecnicoDialog` (Registro de caso técnico de diagnóstico na base)
60. `AssistWindow` (Janela autônoma do assistente grounded PRIMOX Assist)

---

## 6. Inventário de Modais

Os diálogos modais bloqueantes catalogados com validação de cancelamento e fechamento determinístico:
- `AdicionarFornecedorDialog`
- `NovaFerramentaDialog`
- `ToolCheckoutDialog`
- `ToolReturnDialog`
- `ToolMaintenanceDialog`
- `NovaRequisicaoCompraDialog`
- `AprovacaoRequisicaoDialog`
- `CasoTecnicoDialog`
- `SelecionarOrcamentoWindow`
- `SelecionarVendaWindow`
- `OperacaoCaixaWindow`
- `PagamentoMistoWindow`

---

## 7. Inventário de Botões e Ações Interativas

A suíte executou a varredura dinâmica de todos os botões instanciados nas superfícies visuais:
- **Botões Padrão Mapeados e Exercitados**:
  - Salvar, Cancelar, Fechar, Atualizar, Novo, Editar, Excluir, Limpar Filtros, Pesquisar, Aplicar, Voltar, Avançar, Exportar CSV, Exportar PDF, Imprimir, Gerar Comprovante, Finalizar Venda, Desconto, Adicionar Item, Remover Item, Sangria, Suprimento, Fechar Caixa, Abrir Caixa, Enviar WhatsApp, Assinatura Digital, Desfazer Rollback, Gerar Prévia, Bloquear Usuário, Reativar Usuário.
- **Botões de Módulo Testados**: 206 verificações automatizadas de botões e controles.

---

## 8. Matriz de Testes Executados

| ID | Módulo / Superfície | Ação / Verificação | Resultado Esperado | Resultado Real | Status | Bug Vinculado |
|---|---|---|---|---|---|---|
| MT-001 | MainWindow | Inicialização do Shell | Carregar janela principal e menus | Inicializado com sucesso | **PASS** | — |
| MT-002 | Dados | Seed sintético isolado | Inicializar base isolada de homologação | Base isolada inicializada | **PASS** | — |
| MT-003 | Produtos | Campos, anexos e fotos | Gravar e recuperar mídias | Anexos e mídias persistidos | **PASS** | — |
| MT-004 | Produtos | Cadastro completo pela tela | Inserir produto com SKU, NCM, estoque | Salvo com sucesso | **PASS** | — |
| MT-005 | Produtos | Etiqueta PDF | Gerar PDF de etiquetas | Arquivo PDF gerado | **PASS** | — |
| MT-006 | Estoque | Entrada / Saída / Histórico | Atualizar saldos e auditoria | Movimentações registradas | **PASS** | — |
| MT-007 | Estoque | Filtros operacionais | Filtrar Curva ABC, saldo mínimo | Filtros aplicados | **PASS** | — |
| MT-008 | Fornecedores | Ficha, compras e prazos | Calcular ticket médio e compras | Cálculos corretos | **PASS** | — |
| MT-009 | Fornecedores | Ações sem botão excluir em grid | Botão fora da linha da tabela | Template em conformidade | **PASS** | — |
| MT-010 | Fornecedores | Excluir somente selecionado | Preservar outros registros | Apenas o selecionado removido | **PASS** | — |
| MT-011 | Fornecedores | Editar prazo/categoria/contato | Refletir alterações no banco | Persistido com sucesso | **PASS** | — |
| MT-012 | Clientes | Cadastro completo e LGPD | Termo LGPD e contato WhatsApp | Salvo com sucesso | **PASS** | — |
| MT-013 | Clientes | Anexos e assinatura digital | Persistência com hash SHA256 | Assinatura e doc vinculados | **PASS** | — |
| MT-014 | Clientes | Atalhos operacionais | Atalho para OS e orçamento | Auditoria e janelas acionadas | **PASS** | — |
| MT-015 | Dashboard | Indicadores reais e atalhos | Exibir métricas da base isolada | Indicadores exibidos | **PASS** | — |
| MT-016 | Veículos | Cadastro técnico | Sistema 12V/24V, motor, alternador | Salvo com sucesso | **PASS** | — |
| MT-017 | Veículos | Alertas de retorno e revisão | Exibir status preventivo | Alertas calculados | **PASS** | — |
| MT-018 | Veículos | Exportação CSV | Gerar arquivo CSV | CSV exportado | **PASS** | — |
| MT-019 | Auto Elétrica | Catálogos técnicos | Acessar esquemas D01-D06 | Catálogos carregados | **PASS** | — |
| MT-020 | Auto Elétrica | Prontuário persistente | Histórico técnico do veículo | Dados persistidos | **PASS** | — |
| MT-021 | Auto Elétrica | Diagnóstico gera orçamento | Converter diagnóstico em orçamento | Orçamento criado | **PASS** | — |
| MT-022 | Auto Elétrica | Defeitos recorrentes | Identificar falhas históricas | Padrão identificado | **PASS** | — |
| MT-023 | Ordens de Serviço | Cadastro completo | Peças, serviços, técnicos, fotos | OS emitida com sucesso | **PASS** | — |
| MT-024 | Ordens de Serviço | Mídias, checklist e financeiro | Concluir OS e gerar a receber | Conta a receber integrada | **PASS** | — |
| MT-025 | Oficina Kanban | Quadro operacional | Cards em colunas, timeline | Quadro funcional | **PASS** | — |
| MT-026 | Orçamentos | Conversão em OS e Venda | Converter sem duplicidade | Convertido perfeitamente | **PASS** | — |
| MT-027 | DVI | Design Visual Light/Dark 1280x720 | Contraste e ausência de corte | Design system validado | **PASS** | — |
| MT-028 | Agendamentos | Visões diária/semanal e check-in | Check-in/check-out operacionais | Agendamento integrado | **PASS** | — |
| MT-029 | Financeiro | CentsV1, baixas e conciliação | Valores inteiros em centavos | Cálculos exatos | **PASS** | — |
| MT-030 | Importar NF-e | Rollback com snapshot | Desfazer nota sem desintegrar | Auditoria e reversão ok | **PASS** | — |
| MT-031 | Funcionários | Cadastro, perfil RBAC e lockout | Bloqueio após falhas de senha | Segurança validada | **PASS** | — |
| MT-032 | Base Conhecimento | Carga de `BaseConhecimentoControl` | Exibir artigos e casos técnicos | Falha de StaticResource | **FAIL** | BUG-001 |
| MT-033 | Compras | Carga de `NecessidadesCompraControl` | Exibir sugestões de reposição | Falha de StaticResource | **FAIL** | BUG-001 |
| MT-034 | Ferramentas | Carga de `FerramentasControl` | Exibir acervo e status | Falha NullReference | **FAIL** | BUG-003 |
| MT-035 | Catálogo Peças | Botão Criar Produto | Habilitado para Administrador | Permissão negada indevida | **FAIL** | BUG-002 |
| MT-036 | Temas | Alternância Claro/Escuro nos módulos | Mudar tema sem quebrar módulos | Falha ao carregar BaseConhecimento | **FAIL** | BUG-001 |
| MT-037 | Janelas 60x | Abertura e fechamento das 60 Windows | Instanciar e exercer botões | 60 Janelas aprovadas | **PASS** | — |
| MT-038 | Workflow E2E | Fluxo Operacional 42 etapas | Cliente -> Veículo -> OS -> Venda -> Caixa | 42/42 Passos aprovados | **PASS** | — |

---

## 9. Resumo Geral de Bugs Encontrados

| Total de Bugs | P0 (Bloqueador) | P1 (Crítico) | P2 (Alto) | P3 (Médio) | P4 (Baixo) |
|---|---|---|---|---|---|
| **3** | **0** | **2** | **1** | **0** | **0** |

---

## 10. Bugs P0 (Bloqueadores)
**Nenhum P0 encontrado.**
A aplicação inicia com sucesso, apresenta tela de login, autentica usuários, navega pelos módulos centrais (Dashboard, Clientes, Veículos, Estoque, Orçamentos, OS, Kanban, PDV, Financeiro, Fiscal, Relatórios) sem travamento fatal do executável.

---

## 11. Bugs P1 (Críticos)

### BUG-001 — `XamlParseException` em `BaseConhecimentoControl` e `NecessidadesCompraControl`
- **Módulos Afetados**: Base de Conhecimento e Compras/Reposição.
- **Causa Raiz**: O XAML faz referência estática a `ModernTabControl` e `ModernTabItem` que não existem nos dicionários de recursos de tema (`Themes/*.xaml`).
- **Impacto**: Usuário é impedido de acessar esses dois módulos pelo menu principal; suíte de teste de alternância de temas falha.

### BUG-003 — `NullReferenceException` ao instanciar `FerramentasControl`
- **Módulo Afetado**: Ferramentas & Equipamentos.
- **Causa Raiz**: Atributo `SelectedIndex="0"` e handler `SelectionChanged="FiltroCombo_SelectionChanged"` no XAML disparam durante `InitializeComponent()`, invocando `AplicarFiltros()` antes de `FerramentasDataGrid` ser instanciado.
- **Impacto**: O módulo de Ferramentas quebra ao ser clicado no menu principal, gerando popup de erro e impedindo seu uso.

---

## 12. Bugs P2 (Altos)

### BUG-002 — Rejeição de Permissão Indevida em `CatalogoPecasViewModel`
- **Módulo Afetado**: Estoque / Catálogo de Peças.
- **Causa Raiz**: Invocação de `_permissionService.TemPermissao("ESTOQUE_CRIAR")` em vez de `TemPermissaoCodigo("ESTOQUE_CRIAR")`.
- **Impacto**: Usuários autenticados como Administrador não conseguem utilizar a funcionalidade de criação direta de produtos a partir de itens do catálogo.

---

## 13. Bugs P3 (Médios)
*Nenhum bug P3 classificado nesta fase.*

---

## 14. Bugs P4 (Baixos)
*Nenhum bug visual menor isolado nesta fase.*

---

## 15. Stack Traces Relevantes

### Stack Trace BUG-001 (BaseConhecimento / NecessidadesCompra):
```
System.Windows.Markup.XamlParseException: O valor fornecido em 'System.Windows.StaticResourceExtension' iniciou uma exceção.
 ---> System.Windows.ResourceReferenceKeyNotFoundException: Não foi possível localizar o recurso 'ModernTabControl'.
   at System.Windows.StaticResourceExtension.ProvideValue(IServiceProvider serviceProvider)
   at MS.Internal.Xaml.Runtime.ClrObjectRuntime.SetConnectionId(Object root, Int32 connectionId, Object instance)
   --- End of inner exception stack trace ---
   at System.Windows.Markup.WpfXamlLoader.Load(XamlReader xamlReader, IXamlObjectWriterFactory writerFactory, Boolean skipJournaledProperties, Object rootObject, XamlObjectWriterSettings settings, Uri baseUri)
   at System.Windows.Markup.XamlReader.LoadBaml(Stream stream, ParserContext parserContext, Object parent, Boolean closeStream)
   at System.Windows.Application.LoadComponent(Object component, Uri resourceLocator)
   at PrimoAutoEletrica.UserControls.BaseConhecimentoControl.InitializeComponent() in C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\UserControls\BaseConhecimentoControl.xaml:line 1
   at PrimoAutoEletrica.UserControls.BaseConhecimentoControl..ctor()
   at PrimoAutoEletrica.Services.NavigationService.GetOrCreateControl(String moduleName) in C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\Services\NavigationService.cs:line 241
```

### Stack Trace BUG-003 (Ferramentas):
```
System.NullReferenceException: Object reference not set to an instance of an object.
   at PrimoAutoEletrica.UserControls.FerramentasControl.AplicarFiltros() in C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\UserControls\FerramentasControl.xaml.cs:line 87
   at PrimoAutoEletrica.UserControls.FerramentasControl.FiltroCombo_SelectionChanged(Object sender, SelectionChangedEventArgs e) in C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\UserControls\FerramentasControl.xaml.cs:line 206
   at System.Windows.Controls.ComboBox.OnSelectionChanged(SelectionChangedEventArgs e)
   at PrimoAutoEletrica.UserControls.FerramentasControl.InitializeComponent() in C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\UserControls\FerramentasControl.xaml:line 1
   at PrimoAutoEletrica.UserControls.FerramentasControl..ctor() in C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\UserControls\FerramentasControl.xaml.cs:line 20
   at PrimoAutoEletrica.Services.NavigationService.GetOrCreateControl(String moduleName) in C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\Services\NavigationService.cs:line 241
```

---

## 16. Problemas de Banco de Dados
- **Integridade**: `PRAGMA integrity_check` retornou `ok` em ambos os bancos.
- **Chaves Estrangeiras**: `PRAGMA foreign_key_check` retornou 0 violações.
- **Bloqueios e Concorrência**: O mecanismo de `RecordLocks` funcionou com precisão (testado na suíte: criação e liberação de lock por usuário/máquina em 30 minutos).

---

## 17. Problemas de Navegação
- `NavigationService`:
  - Módulos inexistentes: Rejeitados com segurança (`NavigationService:ModuloInexistente` = PASS).
  - Refresh sem módulo ativo: Ignorado de forma segura (`NavigationService:RefreshSemModulo` = PASS).
  - Acesso negado a módulos restritos: Bloqueado com auditoria em `AuditLogs` (`NavigationService:PermissaoNegada` = PASS).
  - Bloqueios detectados: Falha ao carregar `BaseConhecimento`, `NecessidadesCompra` e `Ferramentas` decorrentes de BUG-001 e BUG-003.

---

## 18. Problemas de DI / Services / Repositories
- Todos os repositórios em `RepositoryRegistry` inicializam corretamente.
- Injeção de dependência do `CatalogoPecasViewModel`, `PermissionService`, `DatabaseService`, `LoggerService` e `AuditService` sem falhas de resolução de contêiner.

---

## 19. Problemas de UI
- Diálogos suprimidos com segurança em modo de automação para testes de botões.
- Todos os UserControls principais renderizam de forma fluida e responsiva.

---

## 20. Problemas Light / Dark
- A alternância global de temas via `ThemeService` funciona perfeitamente nos módulos centrais:
  - Dashboard, PDV, Estoque, Orçamentos, OS, Relatórios.
  - Teste `Dvi:OrcamentoOsFluxoLight1280`: **PASS**
  - Teste `Dvi:OrcamentoOsFluxoDark1280`: **PASS**
- Ponto de atenção: `Tema:ClaroEscuroModulosPrincipais` falhou unicamente porque tentou validar a transição no controle `BaseConhecimentoControl`, que não carregou por causa do BUG-001.

---

## 21. Problemas de Resolução
- Testes executados nas resoluções 1280x720, 1366x768 e 1920x1080.
- Não foram observados botões cortados, sobreposições impeditivas ou grids quebrados nas telas principais.

---

## 22. Problemas de RBAC
- Testado perfil `Administrador` vs `Vendedor` vs `Caixa`.
- Bloqueio de acesso a módulos sensíveis (ex.: Vendedor tentando abrir Financeiro): **PASS** (auditoria gravada com severidade Warning).
- Bloqueio por senha errada (lockout temporário): **PASS**.
- BUG-002 reportado como inconsistência na validação de permissão granular em `CatalogoPecas`.

---

## 23. Problemas Financeiros / CentsV1
- Varredura em `primoauto_operacional.db` confirmou que todas as colunas monetárias estão no padrão CentsV1 (inteiro representando centavos):
  - `Produtos.PrecoCompra`: `1500` -> R$ 15,00
  - `Produtos.PrecoVenda`: `3200` -> R$ 32,00
  - `ContasPagar.Valor`: `541275` -> R$ 5.412,75
  - `ContasReceber.Valor`: `38020` -> R$ 380,20
  - `Orcamentos.Total`: `12000` -> R$ 120,00
- Nenhum valor de R$ 10,00 apareceu como R$ 1000,00 ou vice-versa. Cálculos de troco, sangria, suprimento e DRE 100% íntegros.

---

## 24. Problemas de Estoque
- Criação de produtos com SKU, NCM, fotos e anexos: **PASS**.
- Entrada e saída de estoque com atualização em tempo real do saldo disponível (`Disponível = Estoque - Reservado`): **PASS**.
- Filtros de estoque baixo e Curva ABC: **PASS**.

---

## 25. Problemas de Compras
- Módulo `NecessidadesCompraControl` impedido de abrir devido ao BUG-001 (`ModernTabControl` ausente no XAML).
- A tabela `PurchaseRequests` e `PurchaseRequestItems` está estruturada e pronta no banco.

---

## 26. Problemas de Ferramentas
- Módulo `FerramentasControl` impedido de abrir devido ao BUG-003 (`NullReferenceException` em `AplicarFiltros` durante o `InitializeComponent`).
- As janelas filhas (`Tool360Window`, `ToolCheckoutDialog`, etc.) foram instanciadas com sucesso na descoberta de janelas.

---

## 27. Problemas Cliente 360
- `HistoricoClienteWindow` testou com sucesso:
  - Vínculo com Veículo 360: **PASS**
  - Follow-up de pós-venda: **PASS**
  - Acionamento de WhatsApp sem janela externa: **PASS**
  - Criação de nova OS / orçamento a partir do cliente: **PASS**

---

## 28. Problemas Vehicle 360
- `VisualizarVeiculoWindow` testou com sucesso todas as abas e histórico técnico em 40 segundos: **PASS**.

---

## 29. Problemas Auto Elétrica Técnica
- Prontuário técnico elétrico de veículos: **PASS**.
- Diagnósticos D01 a D06 gerando orçamentos automaticamente: **PASS**.
- Cenários 12V e 24V validados: **PASS**.

---

## 30. Problemas Base de Conhecimento
- Módulo `BaseConhecimentoControl` impedido de carregar devido ao BUG-001 (`ModernTabControl` e `ModernTabItem` ausentes no XAML).
- Tabela `TechnicalKnowledgeEntries` no banco contém registros válidos preservados.

---

## 31. Problemas PRIMOX Assist
- `GroundedLocalRuleAssistantProvider` e `AssistantService` respeitam contexto e regras de segurança fail-closed sem alucinação.
- Janela `AssistWindow` instanciada e aprovada na suíte.

---

## 32. Problemas de Backup / Restore
- Rotina de backup automático antes de migração e atualização: **PASS** (hashes de integridade validados).
- Banco protegido mantido intocado.

---

## 33. Testes Bloqueados
1. Teste interativo das abas internas de `BaseConhecimentoControl` (bloqueado por BUG-001).
2. Teste interativo das abas internas de `NecessidadesCompraControl` (bloqueado por BUG-001).
3. Teste interativo da grade principal de `FerramentasControl` (bloqueado por BUG-003).

---

## 34. Funcionalidades Não Testadas
- Emissão real de NF-e contra a SEFAZ de produção (ambiente restrito a testes locais/contingência simulada).
- Envio real de mensagens externas do WhatsApp Web (suprimido propositalmente pela automação para não disparar mensagens a telefones reais).

---

## 35. Regressões Encontradas
- O núcleo comercial e operacional histórico (Clientes, Veículos, Estoque, OS, Orçamentos, Caixa, PDV, Relatórios) encontra-se **100% ESTÁVEL**, com 0 regressões.
- Os 3 bugs encontrados estão concentrados exclusivamente nos módulos de extensão operacional recentemente integrados no ciclo C1 (`BaseConhecimento`, `NecessidadesCompra`, `Ferramentas` e comando de conversão de catálogo).

---

## 36. Evidências

Todas as evidências completas foram organizadas nos diretórios:
- `QA_EVIDENCE/BUG-001/BUG-001_EVIDENCE.md`
- `QA_EVIDENCE/BUG-002/BUG-002_EVIDENCE.md`
- `QA_EVIDENCE/BUG-003/BUG-003_EVIDENCE.md`
- Relatório de smoke test executável: `Logs/smoke-tests/ui-smoke-2026-09-25-20-43-55-616-p24088.txt`
- Relatório de workflow test executável: `Logs/workflow-tests/workflow-test-2026-09-25-20-44-55-208-p2568.txt`
- Log mestre da aplicação: `Logs/app-2026-09-25.log`

---

## 37. Conclusão

- **Total de Verificações de UI Realizadas**: 206
- **Aprovações**: 199 (96,6%)
- **Falhas Detectadas**: 7 (todas rastreadas aos bugs BUG-001, BUG-002 e BUG-003)
- **Total de Verificações de Fluxos Operacionais (Workflow E2E)**: 42
- **Aprovações**: 42 (100%)
- **Integridade dos Bancos de Dados**: 100% preservada.
- **Status Final Mandatório**: **`QA_WITH_FINDINGS`**

*A auditoria foi finalizada com êxito sem aplicação de correções no código, aguardando análise técnica para a próxima etapa.*
