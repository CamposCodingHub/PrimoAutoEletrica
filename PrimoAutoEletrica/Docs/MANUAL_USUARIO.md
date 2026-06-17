# Manual do Usuário - Primo Auto Elétrica

## Bem-vindo ao Primo Auto Elétrica

O Primo Auto Elétrica é um sistema ERP completo para oficinas elétricas automotivas, desenvolvido para facilitar o gerenciamento diário do seu negócio.

## Índice

1. [Instalação](#instalação)
2. [Primeiro Acesso](#primeiro-acesso)
3. [Interface do Sistema](#interface-do-sistema)
4. [Cadastro de Clientes](#cadastro-de-clientes)
5. [Cadastro de Veículos](#cadastro-de-veículos)
6. [Orçamentos](#orçamentos)
7. [Ordens de Serviço](#ordens-de-serviço)
8. [PDV - Ponto de Venda](#pdv---ponto-de-venda)
9. [Estoque](#estoque)
10. [Financeiro](#financeiro)
11. [Agendamentos](#agendamentos)
12. [Relatórios](#relatórios)
13. [Backup](#backup)
14. [Configurações](#configurações)
15. [Suporte](#suporte)

## Instalação

### Requisitos do Sistema

- Windows 7 SP1 (x64) ou superior
- 4 GB de RAM (recomendado 8 GB)
- 500 MB de espaço em disco
- .NET 9.0 Runtime (incluído no instalador)

### Passo a Passo

1. Execute o instalador `PrimoAutoEletrica-Setup.exe` como administrador
2. Siga as instruções do assistente de instalação
3. Selecione o diretório de instalação (padrão: `C:\Program Files\PrimoAutoEletrica`)
4. Aguarde a conclusão da instalação
5. Execute o sistema pelo atalho na área de trabalho

## Primeiro Acesso

### Login Inicial

Ao executar o sistema pela primeira vez, você será solicitado a fazer login com o usuário administrador padrão:

- **Email:** admin@primoautoeletrica.com
- **Senha:** admin123

**Importante:** Altere a senha do administrador imediatamente após o primeiro acesso.

### Configuração Inicial

1. **Dados da Empresa**
   - Nome da oficina
   - CNPJ
   - Endereço
   - Telefone
   - E-mail

2. **Configurações de Impressão**
   - Selecione a impressora padrão
   - Configure o formato de papel
   - Teste a impressão

3. **Backup**
   - Configure o backup automático
   - Defina a frequência (diário recomendado)
   - Configure destino externo (opcional)

## Interface do Sistema

### Tela Principal

A tela principal é dividida em módulos:

- **Dashboard** - Visão geral do sistema
- **Clientes** - Gerenciamento de clientes
- **Veículos** - Gerenciamento de veículos
- **Orçamentos** - Criação e gestão de orçamentos
- **Ordens de Serviço** - Acompanhamento de serviços
- **PDV** - Ponto de venda
- **Estoque** - Controle de produtos
- **Financeiro** - Gestão financeira
- **Agendamentos** - Agenda de serviços
- **Relatórios** - Relatórios gerenciais

### Navegação

Use o menu lateral para navegar entre os módulos. Cada módulo possui suas próprias funcionalidades específicas.

## Cadastro de Clientes

### Adicionar Novo Cliente

1. Acesse o módulo **Clientes**
2. Clique no botão **Novo Cliente**
3. Preencha os dados:
   - Nome completo
   - CPF/CNPJ
   - Telefone
   - E-mail
   - Endereço completo
4. Clique em **Salvar**

### Editar Cliente

1. Selecione o cliente na lista
2. Clique no botão **Editar**
3. Altere os dados necessários
4. Clique em **Salvar**

### Buscar Cliente

Use o campo de busca para filtrar clientes por:
- Nome
- CPF/CNPJ
- Telefone
- E-mail

## Cadastro de Veículos

### Adicionar Novo Veículo

1. Acesse o módulo **Veículos**
2. Clique no botão **Novo Veículo**
3. Preencha os dados:
   - Cliente (selecione na lista)
   - Placa
   - Marca/Modelo
   - Ano
   - Chassi
   - Cor
4. Clique em **Salvar**

### Histórico do Veículo

Ao selecionar um veículo, você pode visualizar:
- Orçamentos relacionados
- Ordens de serviço
- Compras realizadas

## Orçamentos

### Criar Orçamento

1. Acesse o módulo **Orçamentos**
2. Clique no botão **Novo Orçamento**
3. Selecione o cliente e veículo
4. Adicione itens:
   - Produtos do estoque
   - Serviços
   - Peças
5. Defina preços e quantidades
6. Clique em **Salvar**

### Aprovar Orçamento

1. Selecione o orçamento
2. Clique em **Aprovar**
3. O orçamento será convertido em ordem de serviço

### Imprimir Orçamento

1. Selecione o orçamento
2. Clique no botão **Imprimir**
3. Selecione a impressora
4. Clique em **OK**

## Ordens de Serviço

### Criar Ordem de Serviço

A partir de um orçamento aprovado:
1. O sistema cria automaticamente a ordem de serviço
2. Atribua um técnico
3. Defina a data prevista
4. Inicie o serviço

### Acompanhar Serviço

1. Acesse o módulo **Ordens de Serviço**
2. Selecione a ordem desejada
3. Visualize o status atual
4. Atualize o progresso conforme necessário

### Finalizar Serviço

1. Selecione a ordem de serviço
2. Clique em **Finalizar**
3. Confirme os dados
4. Gere a nota fiscal (se configurado)

## PDV - Ponto de Venda

### Realizar Venda

1. Acesse o módulo **PDV**
2. Selecione ou cadastre o cliente
3. Adicione produtos ao carrinho:
   - Busque pelo código ou nome
   - Defina a quantidade
   - Aplique descontos (se necessário)
4. Selecione a forma de pagamento
5. Clique em **Finalizar Venda**

### Fechamento de Caixa

1. Acesse o módulo **Financeiro**
2. Selecione **Fechamento de Caixa**
3. Confirme os valores
4. Imprima o relatório

## Estoque

### Adicionar Produto

1. Acesse o módulo **Estoque**
2. Clique no botão **Novo Produto**
3. Preencha os dados:
   - Código
   - Nome
   - Descrição
   - Preço de compra
   - Preço de venda
   - Quantidade em estoque
   - Unidade de medida
4. Clique em **Salvar**

### Atualizar Estoque

1. Selecione o produto
2. Clique em **Editar**
3. Altere a quantidade
4. Clique em **Salvar**

### Importação de NF-e

1. Acesse o módulo **Estoque**
2. Clique em **Importar NF-e**
3. Selecione o arquivo XML
4. Revise os produtos importados
5. Confirme a importação

## Financeiro

### Contas a Pagar

1. Acesse o módulo **Financeiro**
2. Selecione **Contas a Pagar**
3. Clique em **Nova Conta**
4. Preencha os dados:
   - Fornecedor
   - Valor
   - Data de vencimento
   - Categoria
5. Clique em **Salvar**

### Contas a Receber

1. Acesse o módulo **Financeiro**
2. Selecione **Contas a Receber**
3. Visualize as contas pendentes
4. Registre os pagamentos

### Fluxo de Caixa

Visualize o fluxo de caixa através de gráficos e relatórios detalhados.

## Agendamentos

### Agendar Serviço

1. Acesse o módulo **Agendamentos**
2. Clique no botão **Novo Agendamento**
3. Preencha os dados:
   - Cliente
   - Veículo
   - Tipo de serviço
   - Data e hora
   - Técnico responsável
4. Clique em **Salvar**

### Visualizar Agenda

A agenda exibe os agendamentos em formato de calendário, facilitando a visualização e organização.

## Relatórios

### Relatórios Disponíveis

- **Vendas** - Resumo de vendas por período
- **Estoque** - Situação atual do estoque
- **Financeiro** - Balanço financeiro
- **Serviços** - Estatísticas de serviços realizados
- **Clientes** - Lista de clientes ativos

### Gerar Relatório

1. Acesse o módulo **Relatórios**
2. Selecione o tipo de relatório
3. Defina o período
4. Clique em **Gerar**
5. Visualize ou imprima o relatório

## Backup

### Backup Automático

O sistema realiza backup automático diariamente (configurável). Verifique as configurações para ajustar a frequência.

### Backup Manual

1. Acesse **Configurações** > **Backup**
2. Clique em **Criar Backup Manual**
3. Aguarde a conclusão
4. O backup será salvo no diretório configurado

### Restaurar Backup

1. Acesse **Configurações** > **Backup**
2. Selecione o backup desejado
3. Clique em **Restaurar**
4. Confirme a operação

## Configurações

### Configurações da Empresa

Atualize os dados da sua oficina:
- Nome fantasia
- Razão social
- CNPJ
- Endereço
- Contato

### Configurações de Impressão

Configure as impressoras para:
- Orçamentos
- Ordens de serviço
- Notas fiscais
- Relatórios

### Configurações de Backup

Configure o backup automático:
- Frequência
- Horário
- Destino externo
- Compactação

## Suporte

### Obter Ajuda

- Consulte o **Guia Rápido** para instruções rápidas
- Consulte o **Guia do Administrador** para configurações avançadas
- Entre em contato com o suporte técnico

### Horário de Suporte

- Segunda a Sexta: 08:00 - 18:00
- Sábado: 08:00 - 12:00

### Contato

- E-mail: suporte@primoautoeletrica.com
- Telefone: (XX) XXXX-XXXX

---

**Versão:** 1.0.0
**Última atualização:** 2026-06-17
