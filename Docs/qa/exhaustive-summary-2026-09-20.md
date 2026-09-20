# PRIMOX Exhaustive UI — Summary
Generated: 2026-09-20 00:22:37
Rounds planned: Light-1366x768, Dark-1366x768, Light-1600x900, Dark-1600x900, Light-1920x1080, Dark-1920x1080, Light-2560x1440, Dark-2560x1440
Rounds executed: Light-1366x768, Dark-1366x768, Light-1600x900, Dark-1600x900, Light-1920x1080, Dark-1920x1080, Light-2560x1440, Dark-2560x1440
Pages: 18
Windows: 43
Buttons discovered: 4495
Buttons executable (tested+blocked): 2424
Buttons tested: 2424
PASS: 2424
FAIL: 0
SKIPPED/EXPECTED_DISABLED/N/A: 2071
BLOCKED: 0
Coverage tested/discovered: 53,93%
Coverage tested/executable: 100,00%
Functional PASS rate (PASS/tested): 100,00%
Popups dismissed: 584
Note: CalendarDayButton days excluded by audit rule; native file/print = SKIPPED/NOT_TESTABLE.

## Failures

## Popups (text captured)
- 23:54:50.170|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Recuperacao de Senha | Solicite a redefinicao de senha a um administrador do sistema. A tentativa foi registrada para auditoria.
- 23:54:55.086|Light-1366x768|bg|action=native-scan+dismiss kind=expected-business|title=Ordem de Servico | Este agendamento nao possui OS vinculada.
- 23:54:55.582|Light-1366x768|bg|action=native-scan+dismiss kind=expected-business|title=Veiculo | Este agendamento nao possui VeiculoId valido para abrir o prontuario.
- 23:54:56.035|Light-1366x768|bg|action=native-scan+dismiss kind=expected-business|title=Cliente | Este agendamento nao possui ClienteId valido para abrir o perfil.
- 23:55:02.985|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Laudo eletrico | Laudo gerado:
C:\Projetos\PrimoAutoEletrica\TestResults\UiSmoke\2026-09-19_23-54-45\appdata\DocumentosComerciais\laudo-D01-20260919235502.pdf
- 23:55:05.151|Light-1366x768|bg|action=modal-explore-then-close|win=ImportarCatalogoPecasWindow | title=Importar Catalogo de Pecas | Confirmar importacao | Gerar previa | Cancelar | Previa | Erros e alertas | Validacao | Status | Pagina | Categoria | Descricao | Nome | Marca
- 23:55:07.621|Light-1366x768|bg|action=native-scan+dismiss kind=expected-business|title=Catalogo | Nao ha itens filtrados para exportar.
- 23:55:08.163|Light-1366x768|bg|action=native-scan+dismiss kind=expected-business|title=Historico | Nenhuma importacao de catalogo foi registrada ainda.
- 23:55:08.640|Light-1366x768|bg|action=modal-explore-then-close|win=ImportarCatalogoPecasWindow | title=Importar Catalogo de Pecas | Confirmar importacao | Gerar previa | Cancelar | Previa | Erros e alertas | Validacao | Status | Pagina | Categoria | Descricao | Nome | Marca
- 23:55:11.192|Light-1366x768|bg|action=native-scan+dismiss kind=confirm|title=Aviso | Selecione um cliente para excluir.
- 23:55:12.280|Light-1366x768|bg|action=modal-explore-then-close|win=EditarClienteWindow | title=Editar cliente | Salvar alterações | Cancelar | A criacao de nova OS abre a emissao ja vinculada ao cliente selecionado. | Use esta tela para manter cadastro, contato e frota sempre alinhados com as ordens de servico. | Nenhuma assinatura registrada. | Nenhum documento anexado. | Abrir assinatura | Abrir documento | Registrar assinatura | Substituir documento | Anexos e Resumo | Observações
- 23:55:13.272|Light-1366x768|bg|action=modal-explore-then-close|win=NovoClienteWindow | title=Novo cliente | Salvar cliente | Cancelar | Clientes com cadastro completo e frota vinculada aceleram atendimento, historico e a abertura de OS. | Observações | Pontos de fidelidade | LGPD pendente: registre o aceite antes de campanhas ou contatos ativos. | Autoriza contato por WhatsApp | Consentimento LGPD registrado | Cliente VIP | Relacionamento | Nenhuma assinatura registrada. | Nenhum documento anexado.
- 23:55:19.115|Light-1366x768|bg|action=native-scan+dismiss kind=confirm|title=Confirmar ExclusÃ£o | Deseja realmente excluir o produto 'Produto Smoke 427219358663'?

CÃ³digo: SMK-427219358663
Estoque atual: 20
Fornecedor: Fornecedor Smoke 93427295331517

Esta aÃ§Ã£o nÃ£o pode ser desfeita.
- 23:55:24.991|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Compras / Pedido | Sugestoes: 0
Pedido fornecedor: 0
CSV: C:\Projetos\PrimoAutoEletrica\TestResults\UiSmoke\2026-09-19_23-54-45\appdata\Comercial\pedido-fornecedor-20260919235524.csv
Impressao: C:\Projetos\PrimoAutoEletrica\TestResults\UiSmoke\2026-09-19_23-54-45\appdata\Comercial\pedido-fornecedor-20260919235524.txt
- 23:55:32.647|Light-1366x768|bg|action=modal-explore-then-close|win=ComissaoSettlementWindow | title=Fechamento de comissao - tecnicos | Status | Comissao | Percentual | BaseCalculo | TotalPecas | TotalServicos | QuantidadeOs | Tecnico | Salvar status | Exportar CSV | Calcular
- 23:55:32.745|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Comissao | Status de comissao salvo.
- 23:55:33.274|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=CSV | C:\Projetos\PrimoAutoEletrica\TestResults\UiSmoke\2026-09-19_23-54-45\appdata\Comercial\comissao-fechamento-20260820-20260919.csv
- 23:55:37.530|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Operacoes Fiscais | Configuração do emitente salva. Ambiente permanece em Homologação. Produção bloqueada.
- 23:55:39.667|Light-1366x768|bg|action=modal-explore-then-close|win=NovoFornecedorWindow | title=Novo fornecedor | Salvar fornecedor | Cancelar | Fornecedores inativos ficam sinalizados no ranking e nos alertas operacionais. | Fornecedor ativo | Status | Observações | Nota (1-5) | Avaliacao | Categoria preferencial | Prazo medio de pagamento (dias) | Prazo medio de entrega (dias)
- 23:55:42.060|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Fornecedores | Exportacao concluida em:
C:\Projetos\PrimoAutoEletrica\TestResults\UiSmoke\2026-09-19_23-54-45\appdata\Exports\fornecedores_20260919_235541.csv
- 23:55:44.602|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Senha temporaria | Senha temporaria gerada para Administrador:

Primo152C1!

O colaborador devera trocar essa senha no proximo login.
- 23:55:45.686|Light-1366x768|bg|action=modal-explore-then-close|win=ConfigurarPermissoesWindow | title=Configurar Permissoes - PRIMOX | Fechar | Exportar permissoes | Permissoes essenciais devem ser mantidas com cuidado, pois impactam login, financeiro, estoque e operacoes criticas do sistema. | Selecione uma permissao para revisar ou criar um novo cadastro. | Descricao | Nome | Codigo | Detalhes da permissao | 15 | Permissoes essenciais | Permissoes inativas
- 23:55:46.320|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Salvar como | Controle da Árvore de Namespace
- 23:55:49.998|Light-1366x768|bg|action=modal-explore-then-close|win=GerenciarPerfisWindow | title=Gerenciar Perfis - PRIMOX | Fechar | Revise permissoes criticas como financeiro, exclusoes e alteracoes de estoque antes de liberar novos perfis. | Perfis essenciais devem permanecer bloqueados para evitar perda de acesso administrativo. | Boas praticas | Selecione um perfil para revisar os detalhes e permissoes. | Permissoes | Descricao | Nome | Detalhes do perfil | Ativo
- 23:55:51.260|Light-1366x768|bg|action=modal-explore-then-close|win=NovoPerfilWindow | title=Novo Perfil - PRIMOX | Criar perfil | Cancelar | Relatorios | Financeiro | Importar NF-e | Catalogo de Pecas | Estoque | PDV | Ordens de Servico | Orcamentos | Veiculos
- 23:55:54.049|Light-1366x768|bg|action=modal-explore-then-close|win=TwoFactorSetupWindow | title=Autenticação de Dois Fatores (2FA) | ✅ Verificar e Ativar | Cancelar | ⚠️ Guarde sua chave secreta em local seguro. Sem ela, você não poderá recuperar o acesso à sua conta caso perca o celular. | Digite o código exibido no seu app autenticador: | Passo 4: Confirme o código de 6 dígitos | otpauth://totp/PrimoAutoEletrica:admin%40primoauto.com?secret=UWEPLREH5IOMKDFCA74SXM6TUX2XQQDI&issuer=PrimoAutoEletrica&digits=6&period=30 | Passo 3: Ou use o link direto | 📋 Copiar | UWEP LREH 5IOM KDFC A74S XM6T UX2X QQDI | Copie a chave abaixo e adicione manualmente no seu app autenticador: | Passo 2: Adicione esta chave ao aplicativo
- 23:55:55.795|Light-1366x768|bg|action=modal-explore-then-close|win=NovoFuncionarioWindow | title=Novo Funcionario - PRIMOX | Salvar funcionario | Cancelar | Administrador | Perfil de acesso | Confirmar senha | Senha | Dados de acesso | Observações | Ativo | Status inicial | Data de admissao
- 23:55:57.963|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Selecionar foto do funcionario | Controle da Árvore de Namespace | &Nome: | &Tipo:
- 23:56:02.172|Light-1366x768|bg|action=modal-explore-then-close|win=ImportarNotaWindow | title=Importar NF-e | Fechar | Trocar XML | Aguardando selecao de arquivo XML... | Selecione um historico para detalhes. | Logs da conferencia | Historico recente | Vinculo | Categoria | Acao | Custo | Qtd | Produto
- 23:56:05.755|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Importacao NF-e | Pasta de XMLs preparada em:
C:\Projetos\PrimoAutoEletrica\TestResults\UiSmoke\2026-09-19_23-54-45\appdata\Imports
- 23:56:06.786|Light-1366x768|bg|action=modal-explore-then-close|win=ImportarNotaWindow | title=Importar NF-e | Fechar | Trocar XML | Aguardando selecao de arquivo XML... | Selecione um historico para detalhes. | Logs da conferencia | Historico recente | Vinculo | Categoria | Acao | Custo | Qtd | Produto
- 23:56:12.545|Light-1366x768|bg|action=selecao-cancel|win=SelecionarOrcamentoWindow | title=Carteira completa de orcamentos | Abrir orcamento | Cancelar | $25.00 | Rascunho | 26/09/2026 | 19/09/2026 23:54 | Cliente Smoke 83427259715481 | ORC-20260919-0001 | $180.00
- 23:56:13.599|Light-1366x768|bg|action=modal-explore-then-close|win=HistoricoClienteWindow | title=Historico do Cliente - PRIMOX | 19/09/2026 00:00 | PIX · R$ 180,00 | Pagamento (vínculo ID) | 19/09/2026 23:54 | Cliente Smoke 83427259715481 | Cliente cadastrado | Cliente | Rascunho · R$ 25,00 | Orçamento ORC-20260919-0001 | ORC
- 23:56:17.358|Light-1366x768|bg|action=modal-explore-then-close|win=HistoricoClienteWindow | title=Historico do Cliente - PRIMOX | 19/09/2026 00:00 | PIX · R$ 180,00 | Pagamento (vínculo ID) | 19/09/2026 23:54 | Cliente Smoke 83427259715481 | Cliente cadastrado | Cliente | Rascunho · R$ 25,00 | Orçamento ORC-20260919-0001 | ORC
- 23:56:20.990|Light-1366x768|bg|action=selecao-cancel|win=SelecionarOrcamentoWindow | title=Carteira completa de orcamentos | Abrir orcamento | Cancelar | $25.00 | Rascunho | 26/09/2026 | 19/09/2026 23:54 | Cliente Smoke 83427259715481 | ORC-20260919-0001 | $180.00
- 23:56:21.577|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Orçamentos | Rascunho salvo com sucesso.
- 23:56:22.200|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Orçamentos | Orçamento convertido em OS com sucesso!
- 23:56:22.767|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Orçamentos | Orçamento convertido em venda com sucesso!
- 23:56:23.220|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Orçamentos | Este orçamento já está aprovado ou convertido.
- 23:56:24.793|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Orçamentos | Orçamento duplicado com sucesso.
- 23:56:25.323|Light-1366x768|bg|action=modal-explore-then-close|win=NovoOrcamentoWindow | title=Novo Orcamento | Salvar orcamento | Salvar rascunho | Historico | Recusar token | Validar token | Token aprov. | WhatsApp | Cancelar | Os totais sao recalculados automaticamente a cada alteracao de item ou desconto. | Salvar definitivo respeita o status comercial selecionado na tela. | Salvar rascunho preserva o orcamento em aberto para ajuste posterior. | Regras do fluxo
- 23:56:25.418|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Validação | Selecione um cliente.
- 23:56:25.963|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Validação | Selecione um cliente.
- 23:56:26.432|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Historico | Nenhum token registrado ainda.
- 23:56:26.917|Light-1366x768|bg|action=modal-explore-then-close|win=Window | title=Recusar aprovacao | Cancelar | OK | Cole o token para registrar RECUSA:
- 23:56:27.868|Light-1366x768|bg|action=modal-explore-then-close|win=Window | title=Validar aprovacao | Cancelar | OK | Cole o token recebido do cliente:
- 23:56:28.806|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Token de aprovacao | Token: 185CB060B0567828
Expira em: 22/09/2026 23:56
(Copiado para a area de transferencia)
- 23:56:29.247|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Validação | Selecione um cliente.
- 23:56:30.559|Light-1366x768|bg|action=modal-explore-then-close|win=SelecionarProdutoWindow | title=Selecionar Produto | Cancelar | Adicionar | 20 | $25.00 | Eletrica | Produto Smoke 427219358663 | SMK-427219358663 | Estoque | Preco | Categoria | Nome | Codigo
- 23:56:35.404|Light-1366x768|bg|action=modal-explore-then-close|win=VisualizarVeiculoWindow | title=Prontuário Técnico — Veículo | Editar | Fechar | Agendar | Orçamento | Nova OS | Rascunho | R$ 25,00 | VeiculoId | ORC-20260919-0001 | Convertido em Venda | R$ 180,00 | VeiculoId | ORC-20260919-0002 | Rascunho | R$ 180,00 | VeiculoId | ORC-20260919-0003 | Orcamentos relacionados
- 23:56:35.595|Light-1366x768|bg|action=modal-explore-then-close|win=NovoVeiculoWindow | title=Novo Veiculo | Salvar veiculo | Cancelar | Retorno, garantia e revisao ajudam a equipe a antecipar contato e garantia ativa. | Alertas | Placa, tipo, sistema eletrico, conjunto de baterias, alternador e historico recorrente. | Campos criticos | Equipe de recepcao e tecnico passam a registrar o estado eletrico do veiculo na mesma ficha. | Uso ideal | Resumo rapido | Remover | Selecionar documento | Anexe CRLV ou documento visual para atendimento.
- 23:56:41.265|Light-1366x768|bg|action=modal-explore-then-close|win=HistoricoClienteWindow | title=Historico do Cliente - PRIMOX | 19/09/2026 00:00 | PIX · R$ 180,00 | Pagamento (vínculo ID) | 19/09/2026 23:54 | Cliente Smoke 83427259715481 | Cliente cadastrado | Cliente | Rascunho · R$ 25,00 | Orçamento ORC-20260919-0001 | ORC
- 23:56:42.811|Light-1366x768|bg|action=modal-explore-then-close|win=HistoricoClienteWindow | title=Historico do Cliente - PRIMOX | 19/09/2026 00:00 | PIX · R$ 180,00 | Pagamento (vínculo ID) | 19/09/2026 23:54 | Cliente Smoke 83427259715481 | Cliente cadastrado | Cliente | Rascunho · R$ 25,00 | Orçamento ORC-20260919-0001 | ORC
- 23:56:45.063|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Operacao nao concluida | Falha ao avancar status:
Esta OS nao possui proximo status automatico.
- 23:56:46.519|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Excluído | OS OS-2026-0001 excluida com sucesso.
- 23:56:47.218|Light-1366x768|bg|action=modal-explore-then-close|win=OrdemServicoWindow | title=Dossiê Técnico — Ordem de Serviço | Emitir OS | Salvar rascunho | Lembretes revisao | Retornos garantia | WhatsApp docs | Termo garantia | Checklist PDF | Cancelar | Baixa de estoque e integracao financeira continuam acontecendo ao chegar em Entregue. | Emitir leva a OS para aprovacao ou atendimento conforme o status atual. | Salvar cria ou atualiza a OS sem forcar emissao. | Regras do fluxo
- 23:56:48.079|Light-1366x768|bg|action=modal-explore-then-close|win=LembretesRevisaoWindow | title=Lembretes de revisao (local) | Obs local | Status | Motivo | Telefone | OS | Veiculo | Cliente | Data | Total na vista: 0 · Pendentes vencidos/hoje: 0 · Acao 'Marcar tratado' NAO envia WhatsApp. | Sem WhatsApp Cloud — so lista local. | Reabrir pendente | Marcar tratado (local)
- 23:56:48.185|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Lembretes | Selecione um lembrete.
- 23:56:48.657|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Lembretes | Selecione um lembrete.
- 23:56:50.046|Light-1366x768|bg|action=modal-explore-then-close|win=GarantiaRetornosWindow | title=Retornos em garantia | Nao | Aguardando aprovacao | OS criada a partir do orcamento ORC-20260919-0002. | 12/18/2026 6:00:00 PM | YCY6Y89 | Volkswagen Gol 2019 | Cliente Smoke 83427259715481 | OS-2026-0002 | Reincidencia | Status | Problema | Validade
- 23:56:50.149|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Garantia | Selecione uma OS.
- 23:56:54.534|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Foto DVI - Objetos deixados no veiculo | Controle da Árvore de Namespace | &Nome: | &Tipo:
- 23:56:55.356|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Foto DVI - Avarias | Controle da Árvore de Namespace | &Nome: | &Tipo:
- 23:57:03.563|Light-1366x768|bg|action=selecao-cancel|win=SelecionarVendaWindow | title=Selecionar venda | Emitir NF-e Homologacao | Cancelar | $180.00 | Concluida | A combinar | Cliente Smoke 83427259715481 | 19/09/2026 23:56 | Total | Itens | Status | Forma
- 23:57:04.092|Light-1366x768|bg|action=selecao-cancel|win=SelecionarVendaWindow | title=Selecionar venda | Cancelar venda | Cancelar | $180.00 | Concluida | A combinar | Cliente Smoke 83427259715481 | 19/09/2026 23:56 | Total | Itens | Status | Forma
- 23:57:05.755|Light-1366x768|bg|action=operacao-caixa-cancel|win=OperacaoCaixaWindow | title=Abrir caixa | Abrir caixa | Voltar | 3. A confirmacao grava dados operacionais para consulta posterior. | 2. Use observacoes claras quando a operacao alterar caixa ou rastreabilidade. | 1. Revise o valor informado antes de confirmar. | Checklist rapido | Os dados informados entram na trilha operacional e na auditoria do sistema. | Observacoes | Valor de abertura | Dados da operacao | Informe o valor inicial que entra no caixa nesta sessao operacional.
- 23:57:11.480|Light-1366x768|bg|action=modal-explore-then-close|win=SelecionarProdutoPDVWindow | title=Selecionar produto | Cancelar | Adicionar e fechar | Adicionar e continuar | Enter adiciona o produto selecionado. Esc fecha a janela. | Adicionar | 20 | 427219358663 | SKU-427219358663 | Smoke | Eletrica | Produto Smoke 427219358663 | SMK-427219358663
- 23:57:11.621|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Selecionar produto | Selecione um produto para adicionar ao carrinho.
- 23:57:12.165|Light-1366x768|bg|action=native-scan+dismiss kind=info|title=Selecionar produto | Selecione um produto para adicionar ao carrinho.
- 23:58:05.675|Light-1366x768|bg|action=native-scan+dismiss kind=confirm|title=Confirmar Exclusão | Deseja realmente excluir o veículo 'Volkswagen Gol'?

Placa: YCY6Y89
Cliente: Cliente Smoke 83427259715481

Esta ação não pode ser desfeita.
- 23:58:06.266|Light-1366x768|bg|action=modal-explore-then-close|win=HistoricoClienteWindow | title=Historico do Cliente - PRIMOX | 19/09/2026 00:00 | PIX · R$ 180,00 | Pagamento (vínculo ID) | 19/09/2026 23:54 | Cliente Smoke 83427259715481 | Cliente cadastrado | Cliente | Rascunho · R$ 25,00 | Orçamento ORC-20260919-0001 | ORC
- 23:58:07.393|Light-1366x768|bg|action=modal-explore-then-close|win=NovoVeiculoWindow | title=Novo Veiculo | Salvar veiculo | Cancelar | Retorno, garantia e revisao ajudam a equipe a antecipar contato e garantia ativa. | Alertas | Placa, tipo, sistema eletrico, conjunto de baterias, alternador e historico recorrente. | Campos criticos | Equipe de recepcao e tecnico passam a registrar o estado eletrico do veiculo na mesma ficha. | Uso ideal | Resumo rapido | Remover | Selecionar documento | Anexe CRLV ou documento visual para atendimento.
- 23:58:10.853|Light-1366x768|bg|action=modal-explore-then-close|win=NovoVeiculoWindow | title=Novo Veiculo | Salvar veiculo | Cancelar | Retorno, garantia e revisao ajudam a equipe a antecipar contato e garantia ativa. | Alertas | Placa, tipo, sistema eletrico, conjunto de baterias, alternador e historico recorrente. | Campos criticos | Equipe de recepcao e tecnico passam a registrar o estado eletrico do veiculo na mesma ficha. | Uso ideal | Resumo rapido | Remover | Selecionar documento | Anexe CRLV ou documento visual para atendimento.
- 23:58:14.780|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Recuperacao de Senha | Solicite a redefinicao de senha a um administrador do sistema. A tentativa foi registrada para auditoria.
- 23:58:19.412|Dark-1366x768|bg|action=native-scan+dismiss kind=expected-business|title=Ordem de Servico | Este agendamento nao possui OS vinculada.
- 23:58:19.916|Dark-1366x768|bg|action=native-scan+dismiss kind=expected-business|title=Veiculo | Este agendamento nao possui VeiculoId valido para abrir o prontuario.
- 23:58:20.367|Dark-1366x768|bg|action=native-scan+dismiss kind=expected-business|title=Cliente | Este agendamento nao possui ClienteId valido para abrir o perfil.
- 23:58:27.367|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Laudo eletrico | Laudo gerado:
C:\Projetos\PrimoAutoEletrica\TestResults\UiSmoke\2026-09-19_23-54-45\appdata\DocumentosComerciais\laudo-D01-20260919235827.pdf
- 23:58:29.780|Dark-1366x768|bg|action=modal-explore-then-close|win=ImportarCatalogoPecasWindow | title=Importar Catalogo de Pecas | Confirmar importacao | Gerar previa | Cancelar | Previa | Erros e alertas | Validacao | Status | Pagina | Categoria | Descricao | Nome | Marca
- 23:58:32.291|Dark-1366x768|bg|action=native-scan+dismiss kind=expected-business|title=Catalogo | Nao ha itens filtrados para exportar.
- 23:58:32.836|Dark-1366x768|bg|action=native-scan+dismiss kind=expected-business|title=Historico | Nenhuma importacao de catalogo foi registrada ainda.
- 23:58:33.343|Dark-1366x768|bg|action=modal-explore-then-close|win=ImportarCatalogoPecasWindow | title=Importar Catalogo de Pecas | Confirmar importacao | Gerar previa | Cancelar | Previa | Erros e alertas | Validacao | Status | Pagina | Categoria | Descricao | Nome | Marca
- 23:58:35.969|Dark-1366x768|bg|action=native-scan+dismiss kind=confirm|title=Aviso | Selecione um cliente para excluir.
- 23:58:37.010|Dark-1366x768|bg|action=modal-explore-then-close|win=EditarClienteWindow | title=Editar cliente | Salvar alterações | Cancelar | A criacao de nova OS abre a emissao ja vinculada ao cliente selecionado. | Use esta tela para manter cadastro, contato e frota sempre alinhados com as ordens de servico. | Nenhuma assinatura registrada. | Nenhum documento anexado. | Abrir assinatura | Abrir documento | Registrar assinatura | Substituir documento | Anexos e Resumo | Observações
- 23:58:38.055|Dark-1366x768|bg|action=modal-explore-then-close|win=NovoClienteWindow | title=Novo cliente | Salvar cliente | Cancelar | Clientes com cadastro completo e frota vinculada aceleram atendimento, historico e a abertura de OS. | Observações | Pontos de fidelidade | LGPD pendente: registre o aceite antes de campanhas ou contatos ativos. | Autoriza contato por WhatsApp | Consentimento LGPD registrado | Cliente VIP | Relacionamento | Nenhuma assinatura registrada. | Nenhum documento anexado.
- 23:58:44.119|Dark-1366x768|bg|action=native-scan+dismiss kind=confirm|title=Confirmar ExclusÃ£o | Deseja realmente excluir o produto 'Produto Smoke 427219358663'?

CÃ³digo: SMK-427219358663
Estoque atual: 20
Fornecedor: Fornecedor Smoke 93427295331517

Esta aÃ§Ã£o nÃ£o pode ser desfeita.
- 23:58:49.950|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Compras / Pedido | Sugestoes: 0
Pedido fornecedor: 0
CSV: C:\Projetos\PrimoAutoEletrica\TestResults\UiSmoke\2026-09-19_23-54-45\appdata\Comercial\pedido-fornecedor-20260919235849.csv
Impressao: C:\Projetos\PrimoAutoEletrica\TestResults\UiSmoke\2026-09-19_23-54-45\appdata\Comercial\pedido-fornecedor-20260919235849.txt
- 23:58:58.126|Dark-1366x768|bg|action=modal-explore-then-close|win=ComissaoSettlementWindow | title=Fechamento de comissao - tecnicos | Status | Comissao | Percentual | BaseCalculo | TotalPecas | TotalServicos | QuantidadeOs | Tecnico | Salvar status | Exportar CSV | Calcular
- 23:58:58.226|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Comissao | Status de comissao salvo.
- 23:58:58.771|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=CSV | C:\Projetos\PrimoAutoEletrica\TestResults\UiSmoke\2026-09-19_23-54-45\appdata\Comercial\comissao-fechamento-20260820-20260919.csv
- 23:59:02.768|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Operacoes Fiscais | Configuração do emitente salva. Ambiente permanece em Homologação. Produção bloqueada.
- 23:59:04.858|Dark-1366x768|bg|action=modal-explore-then-close|win=NovoFornecedorWindow | title=Novo fornecedor | Salvar fornecedor | Cancelar | Fornecedores inativos ficam sinalizados no ranking e nos alertas operacionais. | Fornecedor ativo | Status | Observações | Nota (1-5) | Avaliacao | Categoria preferencial | Prazo medio de pagamento (dias) | Prazo medio de entrega (dias)
- 23:59:07.163|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Fornecedores | Exportacao concluida em:
C:\Projetos\PrimoAutoEletrica\TestResults\UiSmoke\2026-09-19_23-54-45\appdata\Exports\fornecedores_20260919_235907.csv
- 23:59:09.669|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Senha temporaria | Senha temporaria gerada para Administrador:

Primo18C36!

O colaborador devera trocar essa senha no proximo login.
- 23:59:10.758|Dark-1366x768|bg|action=modal-explore-then-close|win=ConfigurarPermissoesWindow | title=Configurar Permissoes - PRIMOX | Fechar | Exportar permissoes | Permissoes essenciais devem ser mantidas com cuidado, pois impactam login, financeiro, estoque e operacoes criticas do sistema. | Selecione uma permissao para revisar ou criar um novo cadastro. | Descricao | Nome | Codigo | Detalhes da permissao | 15 | Permissoes essenciais | Permissoes inativas
- 23:59:11.353|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Salvar como | Controle da Árvore de Namespace
- 23:59:15.040|Dark-1366x768|bg|action=modal-explore-then-close|win=GerenciarPerfisWindow | title=Gerenciar Perfis - PRIMOX | Fechar | Revise permissoes criticas como financeiro, exclusoes e alteracoes de estoque antes de liberar novos perfis. | Perfis essenciais devem permanecer bloqueados para evitar perda de acesso administrativo. | Boas praticas | Selecione um perfil para revisar os detalhes e permissoes. | Permissoes | Descricao | Nome | Detalhes do perfil | Ativo
- 23:59:16.317|Dark-1366x768|bg|action=modal-explore-then-close|win=NovoPerfilWindow | title=Novo Perfil - PRIMOX | Criar perfil | Cancelar | Relatorios | Financeiro | Importar NF-e | Catalogo de Pecas | Estoque | PDV | Ordens de Servico | Orcamentos | Veiculos
- 23:59:19.072|Dark-1366x768|bg|action=modal-explore-then-close|win=TwoFactorSetupWindow | title=Autenticação de Dois Fatores (2FA) | ✅ Verificar e Ativar | Cancelar | ⚠️ Guarde sua chave secreta em local seguro. Sem ela, você não poderá recuperar o acesso à sua conta caso perca o celular. | Digite o código exibido no seu app autenticador: | Passo 4: Confirme o código de 6 dígitos | otpauth://totp/PrimoAutoEletrica:admin%40primoauto.com?secret=ZBZ37QAAB7PDB4FROARXSGHVMO2ENHJ7&issuer=PrimoAutoEletrica&digits=6&period=30 | Passo 3: Ou use o link direto | 📋 Copiar | ZBZ3 7QAA B7PD B4FR OARX SGHV MO2E NHJ7 | Copie a chave abaixo e adicione manualmente no seu app autenticador: | Passo 2: Adicione esta chave ao aplicativo
- 23:59:20.942|Dark-1366x768|bg|action=modal-explore-then-close|win=NovoFuncionarioWindow | title=Novo Funcionario - PRIMOX | Salvar funcionario | Cancelar | Administrador | Perfil de acesso | Confirmar senha | Senha | Dados de acesso | Observações | Ativo | Status inicial | Data de admissao
- 23:59:23.145|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Selecionar foto do funcionario | Controle da Árvore de Namespace | &Nome: | &Tipo:
- 23:59:27.421|Dark-1366x768|bg|action=modal-explore-then-close|win=ImportarNotaWindow | title=Importar NF-e | Fechar | Trocar XML | Aguardando selecao de arquivo XML... | Selecione um historico para detalhes. | Logs da conferencia | Historico recente | Vinculo | Categoria | Acao | Custo | Qtd | Produto
- 23:59:31.084|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Importacao NF-e | Pasta de XMLs preparada em:
C:\Projetos\PrimoAutoEletrica\TestResults\UiSmoke\2026-09-19_23-54-45\appdata\Imports
- 23:59:32.078|Dark-1366x768|bg|action=modal-explore-then-close|win=ImportarNotaWindow | title=Importar NF-e | Fechar | Trocar XML | Aguardando selecao de arquivo XML... | Selecione um historico para detalhes. | Logs da conferencia | Historico recente | Vinculo | Categoria | Acao | Custo | Qtd | Produto
- 23:59:37.977|Dark-1366x768|bg|action=selecao-cancel|win=SelecionarOrcamentoWindow | title=Carteira completa de orcamentos | Abrir orcamento | Cancelar | $25.00 | Rascunho | 26/09/2026 | 19/09/2026 23:54 | Cliente Smoke 83427259715481 | ORC-20260919-0001 | $180.00 | Convertido em Venda
- 23:59:39.091|Dark-1366x768|bg|action=modal-explore-then-close|win=HistoricoClienteWindow | title=Historico do Cliente - PRIMOX | 19/09/2026 00:00 | PIX · R$ 180,00 | Pagamento (vínculo ID) | 19/09/2026 23:54 | Cliente Smoke 83427259715481 | Cliente cadastrado | Cliente
- 23:59:42.938|Dark-1366x768|bg|action=modal-explore-then-close|win=HistoricoClienteWindow | title=Historico do Cliente - PRIMOX | 19/09/2026 00:00 | PIX · R$ 180,00 | Pagamento (vínculo ID) | 19/09/2026 23:54 | Cliente Smoke 83427259715481 | Cliente cadastrado | Cliente
- 23:59:46.611|Dark-1366x768|bg|action=selecao-cancel|win=SelecionarOrcamentoWindow | title=Carteira completa de orcamentos | Abrir orcamento | Cancelar | $25.00 | Rascunho | 26/09/2026 | 19/09/2026 23:54 | Cliente Smoke 83427259715481 | ORC-20260919-0001 | $180.00 | Convertido em Venda
- 23:59:47.205|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Orçamentos | Rascunho salvo com sucesso.
- 23:59:47.770|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Orçamentos | Orçamento convertido em OS com sucesso!
- 23:59:48.340|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Orçamentos | Orçamento convertido em venda com sucesso!
- 23:59:48.916|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Orçamentos | Este orçamento já está aprovado ou convertido.
- 23:59:50.461|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Orçamentos | Orçamento duplicado com sucesso.
- 23:59:51.031|Dark-1366x768|bg|action=modal-explore-then-close|win=NovoOrcamentoWindow | title=Novo Orcamento | Salvar orcamento | Salvar rascunho | Historico | Recusar token | Validar token | Token aprov. | WhatsApp | Cancelar | Os totais sao recalculados automaticamente a cada alteracao de item ou desconto. | Salvar definitivo respeita o status comercial selecionado na tela. | Salvar rascunho preserva o orcamento em aberto para ajuste posterior. | Regras do fluxo
- 23:59:51.130|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Validação | Selecione um cliente.
- 23:59:51.689|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Validação | Selecione um cliente.
- 23:59:52.235|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Historico | Nenhum token registrado ainda.
- 23:59:52.739|Dark-1366x768|bg|action=modal-explore-then-close|win=Window | title=Recusar aprovacao | Cancelar | OK | Cole o token para registrar RECUSA:
- 23:59:53.687|Dark-1366x768|bg|action=modal-explore-then-close|win=Window | title=Validar aprovacao | Cancelar | OK | Cole o token recebido do cliente:
- 23:59:54.711|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Token de aprovacao | Token: FAFAF492CF436B21
Expira em: 22/09/2026 23:59
(Copiado para a area de transferencia)
- 23:59:55.257|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Validação | Selecione um cliente.
- 23:59:56.559|Dark-1366x768|bg|action=modal-explore-then-close|win=SelecionarProdutoWindow | title=Selecionar Produto | Cancelar | Adicionar | 20 | $25.00 | Eletrica | Produto Smoke 427219358663 | SMK-427219358663 | Estoque | Preco | Categoria | Nome | Codigo
- 00:00:01.512|Dark-1366x768|bg|action=modal-explore-then-close|win=VisualizarVeiculoWindow | title=Prontuário Técnico — Veículo | Editar | Fechar | Agendar | Orçamento | Nova OS | Rascunho | R$ 25,00 | VeiculoId | ORC-20260919-0001 | Convertido em Venda | R$ 180,00 | VeiculoId | ORC-20260919-0002 | Rascunho | R$ 180,00 | VeiculoId | ORC-20260919-0003
- 00:00:01.666|Dark-1366x768|bg|action=modal-explore-then-close|win=NovoVeiculoWindow | title=Novo Veiculo | Salvar veiculo | Cancelar | Retorno, garantia e revisao ajudam a equipe a antecipar contato e garantia ativa. | Alertas | Placa, tipo, sistema eletrico, conjunto de baterias, alternador e historico recorrente. | Campos criticos | Equipe de recepcao e tecnico passam a registrar o estado eletrico do veiculo na mesma ficha. | Uso ideal | Resumo rapido | Remover | Selecionar documento | Anexe CRLV ou documento visual para atendimento.
- 00:00:07.448|Dark-1366x768|bg|action=modal-explore-then-close|win=HistoricoClienteWindow | title=Historico do Cliente - PRIMOX | 19/09/2026 00:00 | PIX · R$ 180,00 | Pagamento (vínculo ID) | 19/09/2026 23:54 | Cliente Smoke 83427259715481 | Cliente cadastrado | Cliente
- 00:00:09.260|Dark-1366x768|bg|action=modal-explore-then-close|win=HistoricoClienteWindow | title=Historico do Cliente - PRIMOX | 19/09/2026 00:00 | PIX · R$ 180,00 | Pagamento (vínculo ID) | 19/09/2026 23:54 | Cliente Smoke 83427259715481 | Cliente cadastrado | Cliente
- 00:00:11.511|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Operacao nao concluida | Falha ao avancar status:
Esta OS nao possui proximo status automatico.
- 00:00:12.968|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Excluído | OS OS-2026-0003 excluida com sucesso.
- 00:00:13.679|Dark-1366x768|bg|action=modal-explore-then-close|win=OrdemServicoWindow | title=Dossiê Técnico — Ordem de Serviço | Emitir OS | Salvar rascunho | Lembretes revisao | Retornos garantia | WhatsApp docs | Termo garantia | Checklist PDF | Cancelar | Baixa de estoque e integracao financeira continuam acontecendo ao chegar em Entregue. | Emitir leva a OS para aprovacao ou atendimento conforme o status atual. | Salvar cria ou atualiza a OS sem forcar emissao. | Regras do fluxo
- 00:00:14.541|Dark-1366x768|bg|action=modal-explore-then-close|win=LembretesRevisaoWindow | title=Lembretes de revisao (local) | Obs local | Status | Motivo | Telefone | OS | Veiculo | Cliente | Data | Total na vista: 0 · Pendentes vencidos/hoje: 0 · Acao 'Marcar tratado' NAO envia WhatsApp. | Sem WhatsApp Cloud — so lista local. | Reabrir pendente | Marcar tratado (local)
- 00:00:14.651|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Lembretes | Selecione um lembrete.
- 00:00:15.197|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Lembretes | Selecione um lembrete.
- 00:00:16.577|Dark-1366x768|bg|action=modal-explore-then-close|win=GarantiaRetornosWindow | title=Retornos em garantia | Nao | Agendado | OS criada a partir do orcamento ORC-20260919-0002. | 12/18/2026 6:00:00 PM | YCY6Y89 | Volkswagen Gol 2019 | Cliente Smoke 83427259715481 | OS-2026-0002 | Reincidencia | Status | Problema | Validade
- 00:00:16.690|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Garantia | Selecione uma OS.
- 00:00:20.968|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Foto DVI - Objetos deixados no veiculo | Controle da Árvore de Namespace | &Nome: | &Tipo:
- 00:00:21.911|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Foto DVI - Avarias | Controle da Árvore de Namespace | &Nome: | &Tipo:
- 00:00:30.077|Dark-1366x768|bg|action=selecao-cancel|win=SelecionarVendaWindow | title=Selecionar venda | Emitir NF-e Homologacao | Cancelar | $180.00 | Concluida | A combinar | Cliente Smoke 83427259715481 | 19/09/2026 23:56
- 00:00:30.608|Dark-1366x768|bg|action=selecao-cancel|win=SelecionarVendaWindow | title=Selecionar venda | Cancelar venda | Cancelar | $180.00 | Concluida | A combinar | Cliente Smoke 83427259715481 | 19/09/2026 23:56
- 00:00:32.290|Dark-1366x768|bg|action=operacao-caixa-cancel|win=OperacaoCaixaWindow | title=Abrir caixa | Abrir caixa | Voltar | 3. A confirmacao grava dados operacionais para consulta posterior. | 2. Use observacoes claras quando a operacao alterar caixa ou rastreabilidade. | 1. Revise o valor informado antes de confirmar. | Checklist rapido | Os dados informados entram na trilha operacional e na auditoria do sistema. | Observacoes | Valor de abertura | Dados da operacao | Informe o valor inicial que entra no caixa nesta sessao operacional.
- 00:00:37.910|Dark-1366x768|bg|action=modal-explore-then-close|win=SelecionarProdutoPDVWindow | title=Selecionar produto | Cancelar | Adicionar e fechar | Adicionar e continuar | Enter adiciona o produto selecionado. Esc fecha a janela. | Adicionar | 20 | 427219358663 | SKU-427219358663 | Smoke | Eletrica | Produto Smoke 427219358663 | SMK-427219358663
- 00:00:37.970|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Selecionar produto | Selecione um produto para adicionar ao carrinho.
- 00:00:38.423|Dark-1366x768|bg|action=native-scan+dismiss kind=info|title=Selecionar produto | Selecione um produto para adicionar ao carrinho.
- 00:00:54.501|Dark-1366x768|bg|action=native-scan+dismiss kind=confirm|title=Confirmar Exclusão | Deseja realmente excluir o veículo 'Volkswagen Gol'?

Placa: YCY6Y89
Cliente: Cliente Smoke 83427259715481

Esta ação não pode ser desfeita.
- 00:00:55.115|Dark-1366x768|bg|action=modal-explore-then-close|win=HistoricoClienteWindow | title=Historico do Cliente - PRIMOX | 19/09/2026 00:00 | PIX · R$ 180,00 | Pagamento (vínculo ID) | 19/09/2026 23:54 | Cliente Smoke 83427259715481 | Cliente cadastrado | Cliente
- 00:00:56.259|Dark-1366x768|bg|action=modal-explore-then-close|win=NovoVeiculoWindow | title=Novo Veiculo | Salvar veiculo | Cancelar | Retorno, garantia e revisao ajudam a equipe a antecipar contato e garantia ativa. | Alertas | Placa, tipo, sistema eletrico, conjunto de baterias, alternador e historico recorrente. | Campos criticos | Equipe de recepcao e tecnico passam a registrar o estado eletrico do veiculo na mesma ficha. | Uso ideal | Resumo rapido | Remover | Selecionar documento | Anexe CRLV ou documento visual para atendimento.
- 00:00:59.633|Dark-1366x768|bg|action=modal-explore-then-close|win=NovoVeiculoWindow | title=Novo Veiculo | Salvar veiculo | Cancelar | Retorno, garantia e revisao ajudam a equipe a antecipar contato e garantia ativa. | Alertas | Placa, tipo, sistema eletrico, conjunto de baterias, alternador e historico recorrente. | Campos criticos | Equipe de recepcao e tecnico passam a registrar o estado eletrico do veiculo na mesma ficha. | Uso ideal | Resumo rapido | Remover | Selecionar documento | Anexe CRLV ou documento visual para atendimento.
- 00:01:03.681|Light-1600x900|bg|action=native-scan+dismiss kind=info|title=Recuperacao de Senha | Solicite a redefinicao de senha a um administrador do sistema. A tentativa foi registrada para auditoria.
- 00:01:08.293|Light-1600x900|bg|action=native-scan+dismiss kind=expected-business|title=Ordem de Servico | Este agendamento nao possui OS vinculada.
- 00:01:08.810|Light-1600x900|bg|action=native-scan+dismiss kind=expected-business|title=Veiculo | Este agendamento nao possui VeiculoId valido para abrir o prontuario.
- 00:01:09.262|Light-1600x900|bg|action=native-scan+dismiss kind=expected-business|title=Cliente | Este agendamento nao possui ClienteId valido para abrir o perfil.
- 00:01:16.154|Light-1600x900|bg|action=native-scan+dismiss kind=info|title=Laudo eletrico | Laudo gerado:
C:\Projetos\PrimoAutoEletrica\TestResults\UiSmoke\2026-09-19_23-54-45\appdata\DocumentosComerciais\laudo-D01-20260920000116.pdf
- 00:01:18.548|Light-1600x900|bg|action=modal-explore-then-close|win=ImportarCatalogoPecasWindow | title=Importar Catalogo de Pecas | Confirmar importacao | Gerar previa | Cancelar | Previa | Erros e alertas | Validacao | Status | Pagina | Categoria | Descricao | Nome | Marca
- 00:01:21.068|Light-1600x900|bg|action=native-scan+dismiss kind=expected-business|title=Catalogo | Nao ha itens filtrados para exportar.
- 00:01:21.614|Light-1600x900|bg|action=native-scan+dismiss kind=expected-business|title=Historico | Nenhuma importacao de catalogo foi registrada ainda.
- 00:01:22.122|Light-1600x900|bg|action=modal-explore-then-close|win=ImportarCatalogoPecasWindow | title=Importar Catalogo de Pecas | Confirmar importacao | Gerar previa | Cancelar | Previa | Erros e alertas | Validacao | Status | Pagina | Categoria | Descricao | Nome | Marca
- 00:01:24.658|Light-1600x900|bg|action=native-scan+dismiss kind=confirm|title=Aviso | Selecione um cliente para excluir.
- 00:01:25.685|Light-1600x900|bg|action=modal-explore-then-close|win=EditarClienteWindow | title=Editar cliente | Salvar alterações | Cancelar | A criacao de nova OS abre a emissao ja vinculada ao cliente selecionado. | Use esta tela para manter cadastro, contato e frota sempre alinhados com as ordens de servico. | Nenhuma assinatura registrada. | Nenhum documento anexado. | Abrir assinatura | Abrir documento | Registrar assinatura | Substituir documento | Anexos e Resumo | Observações
- 00:01:26.726|Light-1600x900|bg|action=modal-explore-then-close|win=NovoClienteWindow | title=Novo cliente | Salvar cliente | Cancelar | Clientes com cadastro completo e frota vinculada aceleram atendimento, historico e a abertura de OS. | Observações | Pontos de fidelidade | LGPD pendente: registre o aceite antes de campanhas ou contatos ativos. | Autoriza contato por WhatsApp | Consentimento LGPD registrado | Cliente VIP | Relacionamento | Nenhuma assinatura registrada. | Nenhum documento anexado.
- 00:01:32.832|Light-1600x900|bg|action=native-scan+dismiss kind=confirm|title=Confirmar ExclusÃ£o | Deseja realmente excluir o produto 'Produto Smoke 427219358663'?

CÃ³digo: SMK-427219358663
Estoque atual: 20
Fornecedor: Fornecedor Smoke 93427295331517

Esta aÃ§Ã£o nÃ£o pode ser desfeita.
- 00:01:38.668|Light-1600x900|bg|action=native-scan+dismiss kind=info|title=Compras / Pedido | Sugestoes: 0
Pedido fornecedor: 0
CSV: C:\Projetos\PrimoAutoEletrica\TestResults\UiSmoke\2026-09-19_23-54-45\appdata\Comercial\pedido-fornecedor-20260920000138.csv
Impressao: C:\Projetos\PrimoAutoEletrica\TestResults\UiSmoke\2026-09-19_23-54-45\appdata\Comercial\pedido-fornecedor-20260920000138.txt
- 00:01:46.614|Light-1600x900|bg|action=modal-explore-then-close|win=ComissaoSettlementWindow | title=Fechamento de comissao - tecnicos | Status | Comissao | Percentual | BaseCalculo | TotalPecas | TotalServicos | QuantidadeOs | Tecnico | Salvar status | Exportar CSV | Calcular
- 00:01:46.712|Light-1600x900|bg|action=native-scan+dismiss kind=info|title=Comissao | Status de comissao salvo.
- 00:01:47.258|Light-1600x900|bg|action=native-scan+dismiss kind=info|title=CSV | C:\Projetos\PrimoAutoEletrica\TestResults\UiSmoke\2026-09-19_23-54-45\appdata\Comercial\comissao-fechamento-20260821-20260920.csv
- 00:01:51.229|Light-1600x900|bg|action=native-scan+dismiss kind=info|title=Operacoes Fiscais | Configuração do emitente salva. Ambiente permanece em Homologação. Produção bloqueada.
- 00:01:53.352|Light-1600x900|bg|action=modal-explore-then-close|win=NovoFornecedorWindow | title=Novo fornecedor | Salvar fornecedor | Cancelar | Fornecedores inativos ficam sinalizados no ranking e nos alertas operacionais. | Fornecedor ativo | Status | Observações | Nota (1-5) | Avaliacao | Categoria preferencial | Prazo medio de pagamento (dias) | Prazo medio de entrega (dias)
- 00:01:55.841|Light-1600x900|bg|action=native-scan+dismiss kind=info|title=Fornecedores | Exportacao concluida em:
C:\Projetos\PrimoAutoEletrica\TestResults\UiSmoke\2026-09-19_23-54-45\appdata\Exports\fornecedores_20260920_000155.csv
- 00:01:58.385|Light-1600x900|bg|action=native-scan+dismiss kind=info|title=Senha temporaria | Senha temporaria gerada para Administrador:

Primo18565!

O colaborador devera trocar essa senha no proximo login.
- 00:01:59.445|Light-1600x900|bg|action=modal-explore-then-close|win=ConfigurarPermissoesWindow | title=Configurar Permissoes - PRIMOX | Fechar | Exportar permissoes | Permissoes essenciais devem ser mantidas com cuidado, pois impactam login, financeiro, estoque e operacoes criticas do sistema. | Selecione uma permissao para revisar ou criar um novo cadastro. | Descricao | Nome | Codigo | Detalhes da permissao | 15 | Permissoes essenciais | Permissoes inativas
- 00:01:59.985|Light-1600x900|bg|action=native-scan+dismiss kind=info|title=Salvar como | Controle da Árvore de Namespace
- 00:02:03.605|Light-1600x900|bg|action=modal-explore-then-close|win=GerenciarPerfisWindow | title=Gerenciar Perfis - PRIMOX | Fechar | Revise permissoes criticas como financeiro, exclusoes e alteracoes de estoque antes de liberar novos perfis. | Perfis essenciais devem permanecer bloqueados para evitar perda de acesso administrativo. | Boas praticas | Selecione um perfil para revisar os detalhes e permissoes. | Permissoes | Descricao | Nome | Detalhes do perfil | Ativo
- 00:02:04.895|Light-1600x900|bg|action=modal-explore-then-close|win=NovoPerfilWindow | title=Novo Perfil - PRIMOX | Criar perfil | Cancelar | Relatorios | Financeiro | Importar NF-e | Catalogo de Pecas | Estoque | PDV | Ordens de Servico | Orcamentos | Veiculos
- 00:02:07.675|Light-1600x900|bg|action=modal-explore-then-close|win=TwoFactorSetupWindow | title=Autenticação de Dois Fatores (2FA) | ✅ Verificar e Ativar | Cancelar | ⚠️ Guarde sua chave secreta em local seguro. Sem ela, você não poderá recuperar o acesso à sua conta caso perca o celular. | Digite o código exibido no seu app autenticador: | Passo 4: Confirme o código de 6 dígitos | otpauth://totp/PrimoAutoEletrica:admin%40primoauto.com?secret=T3PUUZYQHKBED6KUALJJUOHM3KL5L5AE&issuer=PrimoAutoEletrica&digits=6&period=30 | Passo 3: Ou use o link direto | 📋 Copiar | T3PU UZYQ HKBE D6KU ALJJ UOHM 3KL5 L5AE | Copie a chave abaixo e adicione manualmente no seu app autenticador: | Passo 2: Adicione esta chave ao aplicativo
- 00:02:09.609|Light-1600x900|bg|action=modal-explore-then-close|win=NovoFuncionarioWindow | title=Novo Funcionario - PRIMOX | Salvar funcionario | Cancelar | Administrador | Perfil de acesso | Confirmar senha | Senha | Dados de acesso | Observações | Ativo | Status inicial | Data de admissao
- 00:02:11.987|Light-1600x900|bg|action=native-scan+dismiss kind=info|title=Selecionar foto do funcionario | Controle da Árvore de Namespace | &Nome: | &Tipo:
- 00:02:16.220|Light-1600x900|bg|action=modal-explore-then-close|win=ImportarNotaWindow | title=Importar NF-e | Fechar | Trocar XML | Aguardando selecao de arquivo XML... | Selecione um historico para detalhes. | Logs da conferencia | Historico recente | Vinculo | Categoria | Acao | Custo | Qtd | Produto
- 00:02:19.877|Light-1600x900|bg|action=native-scan+dismiss kind=info|title=Importacao NF-e | Pasta de XMLs preparada em:
C:\Projetos\PrimoAutoEletrica\TestResults\UiSmoke\2026-09-19_23-54-45\appdata\Imports
- 00:02:20.846|Light-1600x900|bg|action=modal-explore-then-close|win=ImportarNotaWindow | title=Importar NF-e | Fechar | Trocar XML | Aguardando selecao de arquivo XML... | Selecione um historico para detalhes. | Logs da conferencia | Historico recente | Vinculo | Categoria | Acao | Custo | Qtd | Produto
- 00:02:26.905|Light-1600x900|bg|action=selecao-cancel|win=SelecionarOrcamentoWindow | title=Carteira completa de orcamentos | Abrir orcamento | Cancelar | $25.00 | Rascunho | 26/09/2026 | 19/09/2026 23:54 | Cliente Smoke 83427259715481 | ORC-20260919-0001 | $180.00 | Convertido em Venda
- 00:02:28.069|Light-1600x900|bg|action=modal-explore-then-close|win=HistoricoClienteWindow | title=Historico do Cliente - PRIMOX | 19/09/2026 00:00 | PIX · R$ 180,00 | Pagamento (vínculo ID) | 19/09/2026 23:54 | Cliente Smoke 83427259715481 | Cliente cadastrado | Cliente
- 00:02:31.926|Light-1600x900|bg|action=modal-explore-then-close|win=HistoricoClienteWindow | title=Historico do Cliente - PRIMOX | 19/09/2026 00:00 | PIX · R$ 180,00 | Pagamento (vínculo ID) | 19/09/2026 23:54 | Cliente Smoke 83427259715481 | Cliente cadastrado | Cliente
- 00:02:35.682|Light-1600x900|bg|action=selecao-cancel|win=SelecionarOrcamentoWindow | title=Carteira completa de orcamentos | Abrir orcamento | Cancelar | $25.00 | Rascunho | 26/09/2026 | 19/09/2026 23:54 | Cliente Smoke 83427259715481 | ORC-20260919-0001 | $180.00 | Convertido em Venda
- 00:02:36.380|Light-1600x900|bg|action=native-scan+dismiss kind=info|title=Orçamentos | Rascunho salvo com sucesso.
- 00:02:37.063|Light-1600x900|bg|action=native-scan+dismiss kind=info|title=Orçamentos | Orçamento convertido em OS com sucesso!
- 00:02:37.645|Light-1600x900|bg|action=native-scan+dismiss kind=info|title=Orçamentos | Orçamento convertido em venda com sucesso!
- 00:02:38.092|Light-1600x900|bg|action=native-scan+dismiss kind=info|title=Orçamentos | Este orçamento já está aprovado ou convertido.
- 00:02:39.675|Light-1600x900|bg|action=native-scan+dismiss kind=info|title=Orçamentos | Orçamento duplicado com sucesso.
- 00:02:40.223|Light-1600x900|bg|action=modal-explore-then-close|win=NovoOrcamentoWindow | title=Novo Orcamento | Salvar orcamento | Salvar rascunho | Historico | Recusar token | Validar token | Token aprov. | WhatsApp | Cancelar | Os totais sao recalculados automaticamente a cada alteracao de item ou desconto. | Salvar definitivo respeita o status comercial selecionado na tela. | Salvar rascunho preserva o orcamento em aberto para ajuste posterior. | Regras do fluxo
- 00:02:40.329|Light-1600x900|bg|action=native-scan+dismiss kind=info|title=Validação | Selecione um cliente.
- 00:02:40.887|Light-1600x900|bg|action=native-scan+dismiss kind=info|title=Validação | Selecione um cliente.
- 00:02:41.447|Light-1600x900|bg|action=native-scan+dismiss kind=info|title=Historico | Nenhum token registrado ainda.
- 00:02:41.934|Light-1600x900|bg|action=modal-explore-then-close|win=Window | title=Recusar aprovacao | Cancelar | OK | Cole o token para registrar RECUSA:
- 00:02:42.886|Light-1600x900|bg|action=modal-explore-then-close|win=Window | title=Validar aprovacao | Cancelar | OK | Cole o token recebido do cliente:
- 00:02:43.903|Light-1600x900|bg|action=native-scan+dismiss kind=info|title=Token de aprovacao | Token: D8369B04D1E65244
Expira em: 23/09/2026 00:02
(Copiado para a area de transferencia)
- 00:02:44.463|Light-1600x900|bg|action=native-scan+dismiss kind=info|title=Validação | Selecione um cliente.
- 00:02:45.763|Light-1600x900|bg|action=modal-explore-then-close|win=SelecionarProdutoWindow | title=Selecionar Produto | Cancelar | Adicionar | 20 | $25.00 | Eletrica | Produto Smoke 427219358663 | SMK-427219358663 | Estoque | Preco | Categoria | Nome | Codigo
- 00:02:50.703|Light-1600x900|bg|action=modal-explore-then-close|win=VisualizarVeiculoWindow | title=Prontuário Técnico — Veículo | Editar | Fechar | Agendar | Orçamento | Nova OS | Rascunho | R$ 180,00 | VeiculoId | ORC-20260919-0003 | Convertido em Venda | R$ 180,00 | VeiculoId | ORC-20260919-0004 | ORC-20260919-0005
- 00:02:50.855|Light-1600x900|bg|action=modal-explore-then-close|win=NovoVeiculoWindow | title=Novo Veiculo | Salvar veiculo | Cancelar | Retorno, garantia e revisao ajudam a equipe a antecipar contato e garantia ativa. | Alertas | Placa, tipo, sistema eletrico, conjunto de baterias, alternador e historico recorrente. | Campos criticos | Equipe de recepcao e tecnico passam a registrar o estado eletrico do veiculo na mesma ficha. | Uso ideal | Resumo rapido | Remover | Selecionar documento | Anexe CRLV ou documento visual para atendimento.
- 00:02:56.728|Light-1600x900|bg|action=modal-explore-then-close|win=HistoricoClienteWindow | title=Historico do Cliente - PRIMOX | 19/09/2026 00:00 | PIX · R$ 180,00 | Pagamento (vínculo ID) | 19/09/2026 23:54 | Cliente Smoke 83427259715481 | Cliente cadastrado | Cliente
- 00:02:58.319|Light-1600x900|bg|action=modal-explore-then-close|win=HistoricoClienteWindow | title=Historico do Cliente - PRIMOX | 19/09/2026 00:00 | PIX · R$ 180,00 | Pagamento (vínculo ID) | 19/09/2026 23:54 | Cliente Smoke 83427259715481 | Cliente cadastrado | Cliente
- 00:03:00.690|Light-1600x900|bg|action=native-scan+dismiss kind=info|title=Operacao nao concluida | Falha ao avancar status:
Esta OS nao possui proximo status automatico.
