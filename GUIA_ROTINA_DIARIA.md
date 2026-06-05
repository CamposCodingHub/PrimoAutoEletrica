# Guia de Rotina Diária Operacional - Primo Auto Eletrica

## Objetivo
Definir a rotina diária operacional para operação do Primo Auto Eletrica em ambiente real de oficina.

## 1. Abertura do Sistema

### 1.1 Abertura do Servidor

**Horário Recomendado:** 07:30

**Passo 1: Ligar Servidor**
- Ligar servidor
- Aguardar inicialização do Windows
- Verificar conexão de rede

**Passo 2: Validar SQL Server**
- Abrir SQL Server Management Studio
- Conectar ao servidor
- Verificar se serviço está rodando
- Verificar se banco PrimoAutoEletrica está online

**Passo 3: Validar Backup**
- Abrir pasta de backup
- Verificar se backup automático diário foi criado
- Verificar integridade do último backup
- Verificar espaço em disco

**Passo 4: Abrir Sistema**
- Abrir Primo Auto Eletrica no servidor
- Fazer login como administrador
- Verificar painel de saúde do sistema
- Verificar usuários online

**Passo 5: Validar Logs**
- Abrir pasta de logs
- Verificar logs de erros
- Verificar logs de warnings
- Verificar logs de performance

**Registro:**
- Data: ___/___/___
- Hora: ___:___
- SQL Server: [ ] OK [ ] Falha
- Backup: [ ] OK [ ] Falha
- Sistema: [ ] OK [ ] Falha
- Logs: [ ] OK [ ] Falha
- Observações: ___________________

### 1.2 Abertura das Estações

**Horário Recomendado:** 07:45

**CAIXA-01:**
- Ligar computador
- Abrir Primo Auto Eletrica
- Fazer login como caixa
- Verificar caixa aberto
- Verificar estoque crítico
- Verificar produtos em promoção

**ADM-01:**
- Ligar computador
- Abrir Primo Auto Eletrica
- Fazer login como gerente
- Verificar relatórios do dia anterior
- Verificar contas a pagar
- Verificar contas a receber

**ATEND-01:**
- Ligar computador
- Abrir Primo Auto Eletrica
- Fazer login como atendente
- Verificar agendamentos do dia
- Verificar orçamentos pendentes

**ESTOQUE-01:**
- Ligar computador
- Abrir Primo Auto Eletrica
- Fazer login como estoque
- Verificar estoque crítico
- Verificar pedidos pendentes

**OFICINA-01:**
- Ligar computador
- Abrir Primo Auto Eletrica
- Fazer login como mecânico
- Verificar OS em andamento
- Verificar agendamentos do dia

**Registro:**
- CAIXA-01: [ ] OK [ ] Falha
- ADM-01: [ ] OK [ ] Falha
- ATEND-01: [ ] OK [ ] Falha
- ESTOQUE-01: [ ] OK [ ] Falha
- OFICINA-01: [ ] OK [ ] Falha
- Observações: ___________________

## 2. Durante o Dia

### 2.1 Monitoramento de Sessões

**Frequência:** A cada hora

**Ações:**
- Verificar usuários online
- Verificar sessões expiradas
- Verificar sessões travadas
- Limpar sessões expiradas

**Registro:**
- Horário: ___:___
- Usuários online: ___
- Sessões expiradas: ___
- Ação tomada: ___________________

### 2.2 Monitoramento de Estoque

**Frequência:** A cada 2 horas

**Ações:**
- Verificar estoque crítico
- Verificar produtos em promoção
- Verificar pedidos pendentes
- Verificar movimentações recentes

**Registro:**
- Horário: ___:___
- Estoque crítico: ___ produtos
- Pedidos pendentes: ___
- Ação tomada: ___________________

### 2.3 Monitoramento de Logs

**Frequência:** A cada 2 horas

**Ações:**
- Verificar logs de erros
- Verificar logs de warnings
- Verificar logs de performance
- Verificar logs de segurança

**Registro:**
- Horário: ___:___
- Erros: ___
- Warnings: ___
- Ação tomada: ___________________

### 2.4 Monitoramento de Backup

**Frequência:** A cada 4 horas

**Ações:**
- Verificar se backup automático foi criado
- Verificar integridade do backup
- Verificar espaço em disco
- Verificar pasta de rede

**Registro:**
- Horário: ___:___
- Backup: [ ] OK [ ] Falha
- Espaço disco: ___ GB
- Ação tomada: ___________________

### 2.5 Monitoramento de Performance

**Frequência:** A cada 4 horas

**Ações:**
- Verificar tempo de resposta
- Verificar uso de CPU
- Verificar uso de memória
- Verificar gargalos

**Registro:**
- Horário: ___:___
- Tempo resposta: ___ ms
- CPU: ___ %
- Memória: ___ %
- Ação tomada: ___________________

## 3. Operações Específicas

### 3.1 Operações de Caixa

**Abertura de Caixa:**
- Horário: 08:00
- Ação: Abrir caixa
- Valor inicial: R$ ___
- Registro: ___________________

**Fechamento de Caixa:**
- Horário: 18:00
- Ação: Fechar caixa
- Valor final: R$ ___
- Diferença: R$ ___
- Registro: ___________________

**Vendas:**
- Total de vendas: R$ ___
- Quantidade de vendas: ___
- Ticket médio: R$ ___
- Registro: ___________________

**Cancelamentos:**
- Quantidade: ___
- Valor total: R$ ___
- Motivo: ___________________

### 3.2 Operações de Estoque

**Entradas:**
- Quantidade: ___
- Valor total: R$ ___
- Fornecedor: ___________________

**Saídas:**
- Quantidade: ___
- Valor total: R$ ___
- Motivo: ___________________

**Ajustes:**
- Quantidade: ___
- Motivo: ___________________

**Pedidos:**
- Quantidade: ___
- Fornecedor: ___________________

### 3.3 Operações de Oficina

**OS Abertas:**
- Quantidade: ___
- Prioridade Alta: ___
- Prioridade Média: ___
- Prioridade Baixa: ___

**OS Concluídas:**
- Quantidade: ___
- Valor total: R$ ___

**OS em Andamento:**
- Quantidade: ___
- Tempo médio: ___ horas

**Agendamentos:**
- Quantidade: ___
- Confirmados: ___
- Cancelados: ___

### 3.4 Operações Financeiras

**Contas a Pagar:**
- Quantidade: ___
- Valor total: R$ ___
- Vencidas: ___
- A vencer: ___

**Contas a Receber:**
- Quantidade: ___
- Valor total: R$ ___
- Vencidas: ___
- A vencer: ___

**Pagamentos:**
- Quantidade: ___
- Valor total: R$ ___

**Recebimentos:**
- Quantidade: ___
- Valor total: R$ ___

## 4. Fechamento do Sistema

### 4.1 Fechamento das Estações

**Horário Recomendado:** 18:00

**CAIXA-01:**
- Fechar caixa
- Verificar diferença
- Imprimir relatório de fechamento
- Fazer logout
- Fechar sistema
- Desligar computador

**ADM-01:**
- Verificar relatórios do dia
- Verificar financeiro
- Fazer logout
- Fechar sistema
- Desligar computador

**ATEND-01:**
- Verificar orçamentos pendentes
- Verificar agendamentos do dia seguinte
- Fazer logout
- Fechar sistema
- Desligar computador

**ESTOQUE-01:**
- Verificar estoque crítico
- Verificar pedidos pendentes
- Fazer logout
- Fechar sistema
- Desligar computador

**OFICINA-01:**
- Verificar OS em andamento
- Verificar agendamentos do dia seguinte
- Fazer logout
- Fechar sistema
- Desligar computador

**Registro:**
- CAIXA-01: [ ] OK [ ] Falha
- ADM-01: [ ] OK [ ] Falha
- ATEND-01: [ ] OK [ ] Falha
- ESTOQUE-01: [ ] OK [ ] Falha
- OFICINA-01: [ ] OK [ ] Falha
- Observações: ___________________

### 4.2 Fechamento do Servidor

**Horário Recomendado:** 18:30

**Passo 1: Validar Sessões**
- Verificar usuários online
- Verificar se todas as estações fecharam
- Forçar logout se necessário

**Passo 2: Validar Financeiro**
- Verificar fechamento de caixa
- Verificar contas a pagar
- Verificar contas a receber
- Gerar relatório financeiro

**Passo 3: Validar Estoque**
- Verificar estoque crítico
- Verificar movimentações do dia
- Verificar pedidos pendentes

**Passo 4: Validar Oficina**
- Verificar OS em andamento
- Verificar agendamentos do dia seguinte
- Verificar OS concluídas

**Passo 5: Criar Backup**
- Criar backup manual
- Verificar integridade do backup
- Verificar espaço em disco
- Copiar backup para pasta de rede

**Passo 6: Validar Backup**
- Verificar se backup foi criado
- Verificar integridade do backup
- Verificar tamanho do backup
- Verificar localização do backup

**Passo 7: Validar Logs**
- Verificar logs de erros
- Verificar logs de warnings
- Verificar logs de performance
- Verificar logs de segurança

**Passo 8: Validar Integridade**
- Verificar integridade do banco
- Verificar integridade dos arquivos
- Verificar integridade do sistema

**Passo 9: Encerrar Sistema**
- Fazer logout
- Fechar sistema
- Desligar servidor

**Registro:**
- Data: ___/___/___
- Hora: ___:___
- Sessões: [ ] OK [ ] Falha
- Financeiro: [ ] OK [ ] Falha
- Estoque: [ ] OK [ ] Falha
- Oficina: [ ] OK [ ] Falha
- Backup: [ ] OK [ ] Falha
- Logs: [ ] OK [ ] Falha
- Integridade: [ ] OK [ ] Falha
- Observações: ___________________

## 5. Relatórios Diários

### 5.1 Relatório de Vendas

**Dados:**
- Total de vendas: R$ ___
- Quantidade de vendas: ___
- Ticket médio: R$ ___
- Vendas por forma de pagamento
- Vendas por categoria
- Vendas por horário

**Registro:**
- Data: ___/___/___
- Responsável: ___________________

### 5.2 Relatório de Estoque

**Dados:**
- Movimentações do dia
- Estoque crítico
- Produtos em promoção
- Pedidos pendentes
- Ajustes realizados

**Registro:**
- Data: ___/___/___
- Responsável: ___________________

### 5.3 Relatório de Oficina

**Dados:**
- OS abertas
- OS concluídas
- OS em andamento
- Agendamentos
- Tempo médio de atendimento

**Registro:**
- Data: ___/___/___
- Responsável: ___________________

### 5.4 Relatório Financeiro

**Dados:**
- Contas a pagar
- Contas a receber
- Pagamentos realizados
- Recebimentos realizados
- Fluxo de caixa

**Registro:**
- Data: ___/___/___
- Responsável: ___________________

## 6. Procedimentos de Emergência

### 6.1 Queda de Servidor

**Ações:**
1. Verificar energia
2. Verificar conexão de rede
3. Reiniciar servidor
4. Verificar SQL Server
5. Verificar backups
6. Comunicar equipe

**Registro:**
- Data: ___/___/___
- Hora: ___:___
- Causa: ___________________
- Ação tomada: ___________________
- Resultado: ___________________

### 6.2 Queda de SQL Server

**Ações:**
1. Reiniciar serviço SQL Server
2. Verificar logs do SQL Server
3. Verificar espaço em disco
4. Verificar conexões ativas
5. Reiniciar servidor se necessário
6. Comunicar equipe

**Registro:**
- Data: ___/___/___
- Hora: ___:___
- Causa: ___________________
- Ação tomada: ___________________
- Resultado: ___________________

### 6.3 Falha de Backup

**Ações:**
1. Verificar espaço em disco
2. Verificar permissões
3. Verificar pasta de destino
4. Verificar integridade do banco
5. Criar backup manual
6. Comunicar equipe

**Registro:**
- Data: ___/___/___
- Hora: ___:___
- Causa: ___________________
- Ação tomada: ___________________
- Resultado: ___________________

### 6.4 Falha de Rede

**Ações:**
1. Verificar conexão de rede
2. Verificar roteador
3. Verificar switch
4. Verificar cabos
5. Reiniciar equipamentos
6. Comunicar equipe

**Registro:**
- Data: ___/___/___
- Hora: ___:___
- Causa: ___________________
- Ação tomada: ___________________
- Resultado: ___________________

## 7. Checklist Diário

### 7.1 Checklist de Abertura

**Servidor:**
- [ ] Ligar servidor
- [ ] Validar SQL Server
- [ ] Validar backup
- [ ] Abrir sistema
- [ ] Validar logs

**Estações:**
- [ ] CAIXA-01 ligado e conectado
- [ ] ADM-01 ligado e conectado
- [ ] ATEND-01 ligado e conectado
- [ ] ESTOQUE-01 ligado e conectado
- [ ] OFICINA-01 ligado e conectado

**Sistema:**
- [ ] Login funcionando
- [ ] Usuários online visíveis
- [ ] Estoque crítico verificado
- [ ] Agendamentos verificados
- [ ] Relatórios verificados

### 7.2 Checklist Durante o Dia

**Monitoramento:**
- [ ] Sessões monitoradas
- [ ] Estoque monitorado
- [ ] Logs monitorados
- [ ] Backup monitorado
- [ ] Performance monitorada

**Operações:**
- [ ] Vendas registradas
- [ ] Estoque atualizado
- [ ] OS atualizadas
- [ ] Financeiro atualizado
- [ ] Agendamentos atualizados

### 7.3 Checklist de Fechamento

**Estações:**
- [ ] CAIXA-01 fechado
- [ ] ADM-01 fechado
- [ ] ATEND-01 fechado
- [ ] ESTOQUE-01 fechado
- [ ] OFICINA-01 fechado

**Servidor:**
- [ ] Sessões validadas
- [ ] Financeiro validado
- [ ] Estoque validado
- [ ] Oficina validado
- [ ] Backup criado
- [ ] Backup validado
- [ ] Logs validados
- [ ] Integridade validada
- [ ] Sistema encerrado

## 8. Treinamento

### 8.1 Treinamento por Função

**Caixa:**
- Abertura de caixa
- Realização de vendas
- Cancelamento de vendas
- Fechamento de caixa
- Relatórios de caixa

**Atendente:**
- Cadastro de clientes
- Criação de orçamentos
- Agendamentos
- Consulta de histórico

**Estoque:**
- Entrada de produtos
- Saída de produtos
- Ajustes de estoque
- Pedidos de compra
- Relatórios de estoque

**Mecânico:**
- Abertura de OS
- Atualização de OS
- Consulta de histórico
- Agendamentos
- Relatórios de OS

**Financeiro:**
- Contas a pagar
- Contas a receber
- Pagamentos
- Recebimentos
- Relatórios financeiros

**Gerente:**
- Relatórios gerenciais
- Configurações
- Usuários
- Permissões
- Auditoria

### 8.2 Treinamento de Rotina

**Abertura:**
- Procedimento de abertura
- Validações necessárias
- Registro de dados

**Durante o Dia:**
- Monitoramento
- Operações específicas
- Procedimentos de emergência

**Fechamento:**
- Procedimento de fechamento
- Validações necessárias
- Registro de dados

## 9. Suporte

### 9.1 Suporte Interno

**Contato:**
- Administrador do sistema
- TI responsável
- Gerente da oficina

**Horário:**
- Segunda a Sexta: 08:00 - 18:00
- Sábado: 08:00 - 12:00

### 9.2 Suporte Externo

**Contato:**
- Desenvolvedor do sistema
- Suporte SQL Server (Microsoft)

**Horário:**
- Conforme contrato de suporte

## 10. Melhoria Contínua

### 10.1 Revisão Semanal

**Itens a Revisar:**
- Performance do sistema
- Erros e warnings
- Feedback da equipe
- Sugestões de melhoria
- Atualizações disponíveis

**Registro:**
- Data: ___/___/___
- Participantes: ___________________
- Itens discutidos: ___________________
- Ações definidas: ___________________

### 10.2 Revisão Mensal

**Itens a Revisar:**
- Relatórios mensais
- Backup e restauração
- Configurações
- Permissões
- Documentação

**Registro:**
- Data: ___/___/___
- Participantes: ___________________
- Itens discutidos: ___________________
- Ações definidas: ___________________
