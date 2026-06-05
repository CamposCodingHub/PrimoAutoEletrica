# Checklist Executavel - Primo Auto Eletrica

Atualizado em `2026-05-25`.

Base desta checklist:

- `STATUS_LISTA_OFICIAL.md`
- `LISTA_OFICIAL_EXECUCAO.md`
- `PrimoAutoEletrica/AUDITORIA_ESTABILIZACAO.md`
- validacao automatizada atual: `build` ok, smoke test UI `128/128`, workflow test `38/38`

Legenda:

- `feito`: confirmado no codigo e/ou nos testes atuais
- `parcial`: existe base real, mas ainda nao esta pronto para uso final
- `falta`: ainda nao foi entregue no nivel necessario

## Resumo rapido

- etapas `1` a `6` e `8` a `13` agora estao fechadas com evidencia automatizada real
- a etapa `7` ficou fechada no software, ganhou diagnostico operacional dedicado de impressoras, e depende apenas de validacao em impressora fisica real fora deste ambiente
- as pendencias restantes ficam concentradas em validacao final de uso real, rede e implantacao

## Notas recentes do agente

- `2026-06-04`: Implementado `VendaRepository` e refatorado `VendaService` para utilizar o repositório.
- `2026-06-04`: Adicionado projeto de testes `PrimoAutoEletrica.Tests` com teste básico para `VendaRepository` (executado localmente com sucesso).
- `2026-06-04`: Endurecido `UiSmokeTestService` com retry ao preparar janelas e timeout crítico para checks longos.
- `2026-06-04`: Tornado `LocalSyncMessageHandler` idempotente — checa eventos recentes antes de registrar venda no `SynchronizationService`.


## Bases ja confirmadas

- `[x]` Build estavel com automacao basica de UI e fluxo operacional
- `[x]` Hash de senha com `PBKDF2`
- `[x]` Backup manual, verificacao, restauracao e backup automatico diario
- `[x]` Bloqueio de registro por sessao
- `[x]` Auditoria estruturada
- `[x]` Base de configuracao futura para `SQL Server`
- `[x]` Design system global base carregado pela aplicacao

## Checklist por prioridade pratica

### 1. Estabilizacao geral

Status: `feito`

Ja confirmado:

- `[x]` `build` sem erros e sem avisos
- `[x]` smoke test cobre abertura de modulos e janelas principais
- `[x]` workflow test cobre fluxo operacional sintetico principal
- `[x]` smoke test cobre notificacoes acionaveis do shell e navegacao cruzada partindo de telas internas
- `[x]` workflow test valida cenarios reais de falha operacional em seguranca administrativa
- `[x]` cobertura automatizada reforcada para garantir que acoes internas relevantes nao derrubem o sistema

### 2. Navegacao e MainWindow

Status: `feito`

Ja confirmado:

- `[x]` navegacao sequencial
- `[x]` voltar historico
- `[x]` refresh do modulo atual
- `[x]` busca global base
- `[x]` busca global com carga sob demanda
- `[x]` permissoes basicas no menu
- `[x]` versao, build, ambiente e banco conectado exibidos no shell principal
- `[x]` abertura da tela de configuracoes pelo shell principal e pela busca global
- `[x]` navegacao cruzada entre modulos validada a partir de telas internas
- `[x]` notificacoes uteis e acionaveis padronizadas no shell
- `[x]` carga inicial da `MainWindow` reduzida com indice da busca global sob demanda
- `[x]` logs de navegacao fechados nos caminhos importantes do shell

### 3. Logs e auditoria

Status: `feito`

Ja confirmado:

- `[x]` tabela `AuditLogs`
- `[x]` auditoria de login, logout e eventos estruturados
- `[x]` leitura de auditoria dentro de `Relatorios`
- `[x]` logs separados por area operacional em `Logs/Areas`
- `[x]` auditoria dedicada de desconto no `PDV` e de abertura, fechamento, sangria e suprimento no caixa operacional
- `[x]` auditoria estruturada de ajuste manual de estoque e reajuste de preco, com `antes/depois` e `CorrelationId` para lotes
- `[x]` auditoria estruturada de criacao, alteracao e exclusao de perfis/permissoes administrativas
- `[x]` medicao de duracao das consultas pesadas por area em `Relatorios`, `Auditoria` e `Financeiro`
- `[x]` fechar auditoria de desconto, sangria, suprimento e fechamento de caixa
- `[x]` criar uma experiencia operacional de consulta de auditoria mais dedicada
- `[x]` cobrir a consulta operacional dedicada na validacao automatizada

### 4. Permissoes por acao

Status: `feito`

Ja confirmado:

- `[x]` perfis e permissoes persistidos
- `[x]` fallback por perfil
- `[x]` tela real para configurar permissoes
- `[x]` codigos granulares de permissao adicionados para exclusao, PDV, OS, relatorios e funcionarios
- `[x]` validacao por acao aplicada nas exclusoes de clientes, veiculos, fornecedores e funcionarios
- `[x]` validacao por acao aplicada em desconto/finalizacao/cancelamento do `PDV`, em acoes de `OrdensServico` e em exportacao/impressao de `Relatorios`
- `[x]` validacao por acao expandida para exportacao/impressao em `Financeiro`, exportacao/impressao/conversao em `Orcamentos` e cancelamento/geracao de OS/exportacao/impressao em `Agendamentos`
- `[x]` validacao por acao expandida para `Estoque` em cadastro, edicao, exclusao e ajuste
- `[x]` validacao por acao expandida para abertura, fechamento, sangria, suprimento, reimpressao e cancelamento completo de venda no `PDV`
- `[x]` validacao por acao expandida para criacao, edicao, duplicacao, reagendamento, check-in, check-out e compartilhamento em `Agendamentos`
- `[x]` validacao por acao expandida para inventario no `Estoque`
- `[x]` confirmacao critica digitada aplicada em finalizacao de venda no `PDV`, cancelamento de `NF-e` em processamento, aceite digital de cliente e administracao de perfis/permissoes
- `[x]` workflow test cobre a malha granular com `Seguranca:PermissoesGranularesOperacionais`

### 5. Seguranca

Status: `parcial`

Ja confirmado:

- `[x]` senha protegida com hash
- `[x]` log de falha de login
- `[x]` bloqueio temporario por tentativas erradas de login
- `[x]` expiracao de sessao por inatividade
- `[x]` logout automatico por inatividade
- `[x]` confirmacao critica padronizada para exclusoes, cancelamentos e overrides sensiveis
- `[x]` protecao reforcada contra exclusoes acidentais nos modulos operacionais e administrativos principais

Falta para fechar:

- `[x]` tornar o timeout de inatividade configuravel pela tela de configuracoes

### 6. Validacoes

Status: `parcial`

Ja confirmado:

- `[x]` validacoes pontuais de obrigatoriedade
- `[x]` verificacoes de email duplicado de funcionario
- `[x]` verificacao de placa duplicada
- `[x]` validacao de quantidade, preco e estoque no cadastro/edicao de produto
- `[x]` validacao de duplicidade basica de produto por codigo/nome no repositorio
- `[x]` validacao centralizada de CPF/CNPJ em clientes, fornecedores e funcionarios
- `[x]` validacao centralizada de telefone, e-mail e placa nos cadastros principais e fluxos de contato
- `[x]` validacao centralizada de datas em funcionario e orcamento
- `[x]` duplicidade de cliente e fornecedor com regra clara no repositorio
- `[x]` CPF de funcionario agora e persistido e reaproveitado nas telas de cadastro/edicao
- `[x]` validacoes comerciais centrais para desconto, subtotal, total, datas e estoque em `PDV`, `OrdensServico`, `Orcamentos` e `Financeiro`

Falta para fechar:

- `[ ]` ampliar validacao de estoque e valores monetarios para todos os modulos
- `[ ]` padronizar mensagens amigaveis

### 7. Performance inicial

Status: `feito`

Ja confirmado:

- `[x]` virtualizacao no `PDVView`
- `[x]` cache curto em `OrcamentosViewModel`
- `[x]` reducao de recargas em `AgendamentosViewModel`
- `[x]` medir consultas pesadas restantes com log de duracao por area
- `[x]` aplicar paginacao real onde faltar
- `[x]` ampliar carregamento assincrono
- `[x]` aplicar mais filtros no banco
- `[x]` adicionar loading visual nas telas mais pesadas

### 8. Arquitetura

Status: `parcial`

Ja confirmado:

- `[x]` repositorios dedicados para clientes, fornecedores, funcionarios, produtos e ordens de servico
- `[x]` base de separacao entre `Services`, `Repositories`, `ViewModels`, `Models` e `Helpers`

Falta para fechar:

- `[ ]` reduzir mais o papel central do `DatabaseService`
- `[ ]` criar `VendaRepository`
- `[ ]` criar `FinanceiroRepository`
- `[ ]` criar `AgendamentoRepository`
- `[ ]` criar `OrcamentoRepository`
- `[ ]` criar `AuditoriaRepository`
- `[ ]` diminuir code-behind onde o fluxo ainda esta pesado

### 9. PDV profissional

Status: `parcial`

Ja confirmado:

- `[x]` venda real persistida no workflow test
- `[x]` baixa automatica de estoque no fluxo validado
- `[x]` integracao financeira base
- `[x]` abertura de caixa
- `[x]` fechamento de caixa
- `[x]` sangria
- `[x]` suprimento
- `[x]` cancelamento de venda completo
- `[x]` historico operacional de vendas recentes para reimpressao e cancelamento completo
- `[x]` reimpressao por comprovante reutilizavel
- `[x]` bloquear venda com caixa fechado
- `[x]` validar auditoria completa do caixa
- `[x]` validar historico operacional do PDV no workflow test

Falta para fechar:

- `[ ]` validar a reimpressao em impressora fisica real fora do ambiente automatizado

### 10. Estoque profissional

Status: `feito`

Ja confirmado:

- `[x]` baixa por venda validada
- `[x]` baixa por OS ponta a ponta validada
- `[x]` ajuste operacional visual e funcional de estoque
- `[x]` busca e filtro operacionais na grade principal do estoque
- `[x]` permissoes por acao aplicadas em cadastro, edicao, exclusao e ajuste de estoque
- `[x]` bloqueio central de estoque negativo sem permissao gerencial, com override auditado
- `[x]` confirmacao critica digitada para override de estoque negativo nos ajustes manuais
- `[x]` validacao de duplicidade basica de produto por codigo/nome
- `[x]` reserva de peca no ciclo de agendamento e OS
- `[x]` inventario operacional com reconciliacao de saldo
- `[x]` historico de movimentacao operacional por produto
- `[x]` grade principal com visao de estoque reservado e disponivel
- `[x]` estoque minimo validado pela disponibilidade operacional

Falta para fechar:

- `[ ]` alerta de reposicao
- `[ ]` curva ABC
- `[ ]` custo medio
- `[ ]` margem

### 11. Financeiro integrado

Status: `feito`

Ja confirmado:

- `[x]` conta a receber e movimentacao no workflow test
- `[x]` resumo financeiro funcional
- `[x]` exportacoes e PDFs financeiros basicos
- `[x]` fluxo de caixa operacional real do `PDV` com abertura, venda, sangria, suprimento, fechamento e estorno refletidos no financeiro
- `[x]` fechar fluxo de caixa operacional real
- `[x]` fechar DRE
- `[x]` baixas automaticas de ponta a ponta
- `[x]` integrar completamente `PDV`, `OS`, `Orcamentos` convertidos e `Agendamentos` finalizados
- `[x]` relatorios por forma de pagamento no panorama financeiro atual
- `[x]` workflow test cobre `Financeiro:BaixasEDre` e `Orcamentos:ConverterEmVendaFinanceiro`

### 12. Ordens de servico

Status: `feito`

Ja confirmado:

- `[x]` persistencia real de OS com itens e eventos
- `[x]` repositorio dedicado de OS
- `[x]` reflexos completos em estoque
- `[x]` reflexos completos em financeiro
- `[x]` emissao operacional completa
- `[x]` finalizacao robusta
- `[x]` checklist funcional
- `[x]` garantia
- `[x]` prazo
- `[x]` reflexos completos em relatorios
- `[x]` workflow test cobre `OS:Criar` e `OS:ChecklistGarantiaPainel`

### 13. Agendamentos

Status: `feito`

Ja confirmado:

- `[x]` agenda base
- `[x]` checkout base
- `[x]` integracoes principais base
- `[x]` melhorias de performance no carregamento
- `[x]` consolidar conversao operacional para OS
- `[x]` reserva operacional de pecas em check-in e conversao para OS
- `[x]` liberacao/consumo da reserva nas saidas operacionais principais
- `[x]` validar timeline operacional real
- `[x]` validar todos os status principais da agenda no uso diario
- `[x]` validar alertas em uso diario
- `[x]` workflow test cobre `Agendamentos:TimelineEAlertas`

### 14. Orcamentos

Status: `feito`

Ja confirmado:

- `[x]` fluxo vivo de orcamentos
- `[x]` salvamento correto de cliente, status e itens
- `[x]` base de PDF
- `[x]` aprovacao completa
- `[x]` historico de negociacao mais robusto
- `[x]` PDF padronizado final
- `[x]` conversao completa em venda
- `[x]` conversao completa em OS
- `[x]` revisar status finais oficiais
- `[x]` workflow test cobre `Orcamentos:AprovarEPdf`, `Orcamentos:ConverterEmVendaFinanceiro` e `Orcamentos:ConverterEmOS`

### 15. Clientes / CRM

Status: `parcial`

Ja confirmado:

- `[x]` base de clientes e veiculos vinculados
- `[x]` historicos visuais iniciais em janelas de cliente

Falta para fechar:

- `[ ]` consolidar historico de compras
- `[ ]` consolidar historico de OS
- `[ ]` consolidar historico de orcamentos
- `[ ]` consolidar valor total gasto
- `[ ]` consolidar ultima visita
- `[ ]` status VIP confiavel
- `[ ]` inadimplencia no CRM
- `[ ]` observacoes importantes
- `[ ]` retorno recomendado

### 16. Relatorios profissionais

Status: `feito`

Ja confirmado:

- `[x]` `RelatoriosView` e `RelatoriosControl` operando
- `[x]` exportacao PDF e Excel base
- `[x]` leitura de auditoria e dados financeiros
- `[x]` revisar consistencia dos dados por modulo
- `[x]` fechar relatorios profissionais da lista oficial
- `[x]` revisar exportacoes finais
- `[x]` smoke test cobre `Relatorios:ConsistenciaOperacional`
- `[x]` workflow test cobre `Relatorios:ConsistenciaEExportacoes`

### 17. Backup profissional

Status: `parcial`

Ja confirmado:

- `[x]` backup manual
- `[x]` backup diario automatico
- `[x]` backup ao fechar sistema
- `[x]` verificacao de integridade
- `[x]` restauracao com backup de seguranca
- `[x]` historico de backups

Falta para fechar:

- `[ ]` backup antes de atualizacao
- `[ ]` backup antes de migracao guiado pelo fluxo final
- `[ ]` alerta operacional quando backup falhar
- `[ ]` fluxo amigavel para salvar em rede e midia externa

### 18. Banco profissional / SQL Server

Status: `parcial`

Ja confirmado:

- `[x]` classe de configuracao de conexao
- `[x]` montagem de connection string SQL Server
- `[x]` plano de provider e pendencias registradas
- `[x]` timeout tecnico do banco persistido em configuracoes e reaproveitado no runtime SQLite atual

Falta para fechar:

- `[ ]` provider SQL Server real
- `[ ]` schema SQL Server real
- `[ ]` tela `Configuracoes > Banco de Dados`
- `[ ]` teste de conexao
- `[ ]` salvar configuracao por interface
- `[ ]` fallback seguro operacional
- `[ ]` logs de conexao e troca de provider

Notas recentes do agente (ações realizadas):

- Foi criado um utilitário console `Tools/DbConfigurator` que permite testar conexão e aplicar `SqlServerSchema.sql` localmente (`dotnet run -- test "<connectionString>"` ou `dotnet run -- create-schema "<connectionString>"`).
- Adicionado workflow de CI (`.github/workflows/ci.yml`) para build e execução de testes automáticos.
- Adicionado documentação de validação de reimpressão de PDV em `Docs/PDV_REIMPRESSION_VALIDATION.md`.

Observação: itens relacionados a validação física (impressoras) e migração real de banco demandam ambiente externo (SQL Server, impressoras físicas). Foram providenciadas ferramentas e instruções para facilitar a conclusão manual.

### 19. Migracao SQLite para SQL Server

Status: `falta`

Falta para fechar:

- `[ ]` ferramenta de migracao
- `[ ]` validacao antes da migracao
- `[ ]` backup antes da migracao
- `[ ]` relatorio final
- `[ ]` rollback seguro
- `[ ]` migracao de clientes, veiculos, produtos, estoque, vendas, financeiro, funcionarios, permissoes, agendamentos, orcamentos, OS, fornecedores e logs

### 20. Multiusuario real

Status: `parcial`

Ja confirmado:

- `[x]` sessao de usuario base
- `[x]` bloqueio de registro entre duas sessoes sinteticas

Falta para fechar:

- `[ ]` usuarios online
- `[ ]` sincronizacao real entre estacoes
- `[ ]` concorrencia real em rede
- `[ ]` prevencao de conflito com aviso de sobrescrita
- `[ ]` atualizacao automatica de dados entre postos

### 21. Atualizacao entre computadores

Status: `parcial`

Ja confirmado:

- `[x]` refresh manual do modulo atual
- `[x]` auto-refresh pontual em `Agendamentos`

Falta para fechar:

- `[ ]` atualizar ao abrir cada tela critica
- `[ ]` atualizar apos venda
- `[ ]` atualizar apos estoque
- `[ ]` alerta quando os dados mudarem em outro computador
- `[ ]` evitar tela com dado antigo em ambiente multiestacao

### 22. Design system global

Status: `feito`

Ja confirmado:

- `[x]` `Colors.xaml`
- `[x]` `Buttons.xaml`
- `[x]` `Cards.xaml`
- `[x]` `Inputs.xaml`
- `[x]` `Header.xaml`
- `[x]` `Sidebar.xaml`
- `[x]` `Tables.xaml`
- `[x]` `Typography.xaml`
- `[x]` `GlobalStyles.xaml`
- `[x]` aplicar o padrao nas janelas restantes
- `[x]` revisar consistencia visual das telas administrativas restantes
- `[x]` fechar a uniformidade visual completa antes do polimento final desta etapa

### 23. Configuracoes do sistema

Status: `parcial`

Ja confirmado:

- `[x]` tela base de configuracoes
- `[x]` timeout de sessao por interface
- `[x]` atalhos administrativos para perfis, permissoes e backup manual
- `[x]` visao tecnica com versao, build, ambiente, banco e pastas operacionais

Falta para fechar:

- `[ ]` empresa
- `[ ]` dados da oficina
- `[ ]` banco de dados
- `[ ]` usuarios
- `[ ]` permissoes
- `[ ]` backup
- `[ ]` impressora
- `[ ]` tema visual
- `[ ]` parametros PDV
- `[ ]` parametros estoque
- `[ ]` parametros financeiro

### 24. Impressao

Status: `parcial`

Ja confirmado:

- `[x]` impressao/PDF em partes de agendamentos, orcamentos, OS, financeiro e relatorios
- `[x]` impressao de venda por comprovante reutilizavel no `PDV`

Falta para fechar:

- `[ ]` padronizar modelo visual de impressao
- `[ ]` validar impressao de venda em impressora fisica real
- `[ ]` centralizar configuracao de impressora
- `[ ]` revisar consistencia entre os modelos impressos

### 25. Controle de versao interno

Status: `parcial`

Ja confirmado:

- `[x]` exibir versao atual no shell principal
- `[x]` exibir data do build no shell principal
- `[x]` exibir ambiente no shell principal
- `[x]` exibir usuario atual no shell principal
- `[x]` exibir banco conectado no shell principal

Falta para fechar:

- `[x]` colocar isso tambem em `Sobre` ou `Configuracoes`

### 26. Rede local real

Status: `falta`

Falta para fechar:

- `[ ]` montar cenario com servidor central
- `[ ]` validar caixa
- `[ ]` validar administrativo
- `[ ]` validar vendas/atendimento
- `[ ]` validar estoque
- `[ ]` testar reflexo de dados entre computadores reais

### 27. Implantacao e versao de uso real

Status: `falta`

Falta para fechar:

- `[ ]` instalador
- `[ ]` configuracao inicial
- `[ ]` banco inicial
- `[ ]` usuario administrador inicial
- `[ ]` pasta de dados
- `[ ]` pasta de logs
- `[ ]` pasta de relatorios
- `[ ]` rotina de operacao diaria pronta

### 28. Fluxo completo como usuario real

Status: `falta`

Falta para fechar:

- `[ ]` cadastrar cliente
- `[ ]` cadastrar veiculo
- `[ ]` criar orcamento
- `[ ]` converter orcamento em OS
- `[ ]` reservar peca
- `[ ]` finalizar OS
- `[ ]` gerar financeiro
- `[ ]` vender produto no PDV
- `[ ]` baixar estoque
- `[ ]` fechar caixa
- `[ ]` gerar relatorio
- `[ ]` consultar auditoria
- `[ ]` restaurar backup
- `[ ]` acessar com outro usuario
- `[ ]` simular dois computadores ao mesmo tempo

## Fechamento pratico das proximas rodadas

Sequencia recomendada a partir de agora:

1. fechar estabilizacao interna das telas criticas
2. fechar permissoes por acao e seguranca basica restante
3. fechar PDV, estoque, financeiro e OS no fluxo real
4. fechar relatorios e backup no nivel operacional
5. implementar SQL Server real e migracao
6. validar multiusuario e rede local reais
7. preparar implantacao e versao de uso real
