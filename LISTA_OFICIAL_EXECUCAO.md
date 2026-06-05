# Lista Oficial de Execucao - Primo Auto Eletrica

Esta e a lista oficial de referencia do projeto nesta conversa.
Quando o usuario disser apenas "a lista", esta deve ser a base usada para decidir prioridade, verificacao e validacao.

## Objetivo central

Transformar o sistema Primo Auto Eletrica em um ERP automotivo profissional para operacao real em oficina multiusuario, com foco em:

- estabilidade
- seguranca
- banco central
- rede local
- permissoes
- auditoria
- backup
- performance
- integracao real entre modulos

## Ordem oficial de execucao

1. corrigir erros e telas fechando
2. estabilizar navegacao
3. padronizar visual global
4. corrigir performance inicial
5. melhorar logs
6. melhorar permissoes
7. corrigir PDV
8. corrigir estoque
9. corrigir financeiro
10. corrigir OS
11. corrigir agendamentos
12. corrigir orcamentos
13. corrigir relatorios
14. implementar backup
15. preparar SQL Server
16. preparar multiusuario
17. testar rede local
18. testar fluxo completo
19. polir visual final
20. preparar versao de uso real

## Escopo detalhado da lista

### 1. Estabilizacao geral

- nenhuma tela pode fechar o sistema
- corrigir navegacao quebrada, erros silenciosos, bindings invalidos, botoes sem acao e telas pesadas
- validar Dashboard, Clientes, Veiculos, Orcamentos, Ordens de Servico, PDV, Estoque, Importar NF-e, Financeiro, Fornecedores, Funcionarios, Agendamentos e Relatorios

### 2. Banco profissional

- preparar suporte a SQL Server Express
- conexao em rede local e servidor central
- connection string configuravel
- tela Configuracoes > Banco de Dados com teste e persistencia

### 3. Migracao SQLite para SQL Server

- migrar clientes, veiculos, produtos, estoque, vendas, financeiro, funcionarios, permissoes, agendamentos, orcamentos, OS, fornecedores e logs
- executar backup, validacao, relatorio final e rollback seguro

### 4. Multiusuario real

- sessoes de usuario
- usuarios online
- logs por usuario
- bloqueio de registro em edicao
- controle de concorrencia
- atualizacao automatica e prevencao de conflito

### 5. Perfis e permissoes

- perfis: Administrador, Gerente, Caixa, Vendedor, Estoquista, Mecanico/Tecnico, Financeiro
- validar permissoes por acao critica, nao apenas por menu

### 6. Auditoria completa

- registrar login, logout, venda, cancelamento, desconto, exclusao, alteracao de preco, estoque, pagamento, sangria, suprimento, fechamento de caixa, importacao NF-e, alteracao financeira e alteracao de permissao
- incluir usuario, data, hora, modulo, acao, valor anterior, valor novo e computador

### 7. Backup profissional

- backup manual
- backup diario automatico
- backup ao fechar o sistema
- backup antes de migracao e atualizacao
- restauracao, historico e alerta de falha

### 8. Seguranca

- senha criptografada
- bloqueio por tentativas erradas
- expiracao e logout por inatividade
- confirmacao de acoes criticas
- logs de falha de login

### 9. PDV profissional

- abertura e fechamento de caixa
- sangria e suprimento
- venda, cancelamento, reimpressao
- varias formas de pagamento
- troco automatico
- desconto por permissao
- baixa automatica de estoque
- integracao financeira e auditoria

### 10. Financeiro integrado

- integrar PDV, OS, orcamentos convertidos, agendamentos finalizados, contas a pagar e receber, sangrias e suprimentos
- entregar fluxo de caixa, DRE, centro de custo, faturamento e indicadores

### 11. Estoque profissional

- entrada e saida manual
- baixa por venda e OS
- reserva de peca
- estoque minimo
- curva ABC
- custo medio
- margem
- inventario
- vinculo com fornecedor
- bloquear estoque negativo sem permissao elevada

### 12. Ordens de servico

- cliente, veiculo, tecnico, servicos, pecas, mao de obra, checklist, status, garantia, prazo e valor total
- integrar estoque, financeiro e relatorios

### 13. Agendamentos

- corrigir scroll, responsividade, modal e painel lateral
- timeline operacional, status por cor, tecnico, veiculo, cliente, servico e prioridade
- integrar com OS

### 14. Orcamentos

- validade, status, cliente, veiculo, produtos, servicos, desconto, aprovacao
- converter em venda e OS
- PDF e historico de negociacao

### 15. Clientes / CRM

- historico de compras, OS e orcamentos
- veiculos vinculados
- total gasto
- ultima visita
- status VIP
- inadimplencia

### 16. Relatorios profissionais

- vendas, lucro, produtos mais vendidos, produtos parados, estoque baixo, clientes recorrentes, inadimplencia, OS por tecnico, formas de pagamento, fluxo de caixa, DRE, cancelamentos, descontos, auditoria, produtividade e conversao
- exportar PDF, Excel e impressao

### 17. Design system global

- padronizar Colors, Buttons, Cards, Inputs, Header, Sidebar, Tables, Typography e GlobalStyles
- todas as paginas devem seguir o mesmo sistema visual

### 18. MainWindow

- sidebar profissional
- menu ativo
- scroll no menu
- header global
- busca global otimizada
- usuario logado
- cargo
- notificacoes
- botao sair
- logs de navegacao

### 19. Performance

- paginacao
- carregamento assincrono
- virtualizacao
- cache inteligente
- lazy loading
- filtros no banco
- loading visual

### 20. Arquitetura

- separar Services, Repositories, ViewModels, Models, Helpers e Validators
- reduzir o papel centralizador do DatabaseService

### 21. Validacoes

- CPF/CNPJ
- telefone
- e-mail
- placa
- valor monetario
- quantidade
- estoque
- datas
- obrigatoriedade
- duplicidade

### 22. Tratamento de erros

- registrar tudo em log
- informar modulo e acao
- nunca fechar o sistema por erro de operacao
- manter Logs/erros, Logs/auditoria e Logs/navegacao

### 23. Atualizacao entre computadores

- botao atualizar
- atualizacao ao abrir tela
- atualizacao apos venda
- atualizacao apos estoque
- alerta quando dados mudarem

### 24. Controle de versao do sistema

- exibir versao atual
- data de build
- usuario
- ambiente
- banco conectado

### 25. Configuracoes do sistema

- empresa
- dados da oficina
- banco de dados
- usuarios
- permissoes
- backup
- impressora
- tema visual
- parametros PDV
- parametros estoque
- parametros financeiro

### 26. Impressao

- imprimir orcamento, OS, venda e relatorios
- configuracao de impressora
- modelo visual padrao

### 27. Implantacao na oficina

- instalador
- configuracao inicial
- banco inicial
- usuario administrador
- backup
- pasta dados
- pasta logs
- pasta relatorios

### 28. Teste como usuario real

1. cadastrar cliente
2. cadastrar veiculo
3. criar orcamento
4. converter orcamento em OS
5. reservar peca
6. finalizar OS
7. gerar financeiro
8. vender produto no PDV
9. baixar estoque
10. fechar caixa
11. gerar relatorio
12. consultar auditoria
13. restaurar backup
14. acessar com outro usuario
15. simular dois computadores ao mesmo tempo

### 29. Meta de validacao

- seguir a ordem oficial acima
- verificar, testar, criar e modificar o que for necessario
- concluir com foco em operacao real, nao apenas interface bonita

### 30. Objetivo final

O sistema deve operar como ERP automotivo real para oficina multiusuario, sendo:

- estavel
- seguro
- rapido
- bonito
- padronizado
- auditavel
- preparado para backup
- preparado para SQL Server
- preparado para uso diario
