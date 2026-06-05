# Guia de Ambiente Produtivo - Primo Auto Eletrica

## Objetivo
Definir o ambiente produtivo para operação do Primo Auto Eletrica em ambiente real de oficina.

## 1. Servidor

### 1.1 Função do Servidor
- Hospedar SQL Server
- Hospedar banco central
- Hospedar backups
- Hospedar arquivos compartilhados
- Centralizar dados do sistema

### 1.2 Configuração de Hardware Mínimo

**Requisitos Mínimos:**
- CPU: Intel Core i5 ou equivalente (4 núcleos)
- RAM: 8 GB
- Disco: 500 GB SSD
- Rede: Gigabit Ethernet
- Sistema: Windows 10/11 Pro ou Windows Server 2019/2022

**Requisitos Recomendados:**
- CPU: Intel Core i7 ou equivalente (8 núcleos)
- RAM: 16 GB
- Disco: 1 TB SSD
- Rede: Gigabit Ethernet
- Sistema: Windows Server 2019/2022

### 1.3 Configuração de Software

**SQL Server:**
- SQL Server Express (gratuito) ou SQL Server Standard
- Instância: SQLEXPRESS (padrão)
- Porta: 1433
- Autenticação: Windows + SQL Server
- TCP/IP: Habilitado
- SQL Browser: Habilitado

**Windows:**
- Atualizações automáticas: Configuradas
- Firewall: Configurado
- Antivírus: Configurado com exceções para SQL Server
- Backup do Windows: Configurado

### 1.4 Configuração de Rede

**IP Fixo:**
- Exemplo: 192.168.0.10
- Máscara: 255.255.255.0
- Gateway: 192.168.0.1
- DNS: 192.168.0.1

**Nome da Máquina:**
- Exemplo: PRIMO-SERVER
- Grupo de trabalho: WORKGROUP ou domínio

### 1.5 Estrutura de Pastas no Servidor

```
C:\PrimoAutoEletrica\
├── Backups\
├── Database\
├── Logs\
├── Temp\
├── Reports\
├── Config\
├── Updates\
├── Exports\
└── Imports\
```

### 1.6 Compartilhamento de Pastas

**Pasta de Backup:**
- Caminho: C:\PrimoAutoEletrica\Backups
- Compartilhamento: \\PRIMO-SERVER\BackupsPrimoAutoEletrica
- Permissões: Leitura/Escrita para todos os usuários do sistema

### 1.7 Configuração de Firewall

```powershell
# Permitir conexões SQL Server
New-NetFirewallRule -DisplayName "SQL Server" -Direction Inbound -Protocol TCP -LocalPort 1433 -Action Allow

# Permitir conexões SQL Browser
New-NetFirewallRule -DisplayName "SQL Browser" -Direction Inbound -Protocol UDP -LocalPort 1434 -Action Allow

# Permitir compartilhamento de arquivos (SMB)
New-NetFirewallRule -DisplayName "SMB" -Direction Inbound -Protocol TCP -LocalPort 445 -Action Allow
```

## 2. Estações de Trabalho

### 2.1 Tipos de Estações

**CAIXA-01**
- Função: PDV (Ponto de Venda)
- Local: Frente da loja
- Hardware: PC com leitor de código de barras, impressora fiscal
- Tipo: Caixa

**ADM-01**
- Função: Financeiro e administrativo
- Local: Escritório
- Hardware: PC padrão
- Tipo: Administrativo

**ATEND-01**
- Função: Clientes e orçamentos
- Local: Atendimento
- Hardware: PC padrão
- Tipo: Vendas

**ESTOQUE-01**
- Função: Controle de estoque
- Local: Depósito
- Hardware: PC com leitor de código de barras
- Tipo: Estoque

**OFICINA-01**
- Função: Ordens de serviço e agendamento
- Local: Oficina
- Hardware: PC padrão
- Tipo: Oficina

### 2.2 Configuração de Hardware Mínimo (Estações)

**Requisitos Mínimos:**
- CPU: Intel Core i3 ou equivalente (2 núcleos)
- RAM: 4 GB
- Disco: 256 GB SSD
- Rede: Gigabit Ethernet ou Wi-Fi
- Sistema: Windows 10/11

**Requisitos Recomendados:**
- CPU: Intel Core i5 ou equivalente (4 núcleos)
- RAM: 8 GB
- Disco: 512 GB SSD
- Rede: Gigabit Ethernet
- Sistema: Windows 10/11

### 2.3 Configuração de Software (Estações)

**Sistema Operacional:**
- Windows 10/11 Pro
- Atualizações automáticas: Configuradas
- Firewall: Configurado
- Antivírus: Configurado

**Primo Auto Eletrica:**
- Instalado em: C:\Program Files\PrimoAutoEletrica\
- Atalho na área de trabalho
- Inicialização automática: Opcional

### 2.4 Configuração de Rede (Estações)

**IP Dinâmico (DHCP) ou IP Fixo:**
- Exemplo: 192.168.0.101 (CAIXA-01)
- Exemplo: 192.168.0.102 (ADM-01)
- Exemplo: 192.168.0.103 (ATEND-01)
- Exemplo: 192.168.0.104 (ESTOQUE-01)
- Exemplo: 192.168.0.105 (OFICINA-01)

**Nome da Máquina:**
- CAIXA-01
- ADM-01
- ATEND-01
- ESTOQUE-01
- OFICINA-01

### 2.5 Configuração da Estação no Sistema

Cada estação deve ser configurada no sistema:

**CAIXA-01:**
- StationName: Caixa 01
- StationType: Caixa
- MachineName: CAIXA-01 (automático)
- Description: Caixa principal

**ADM-01:**
- StationName: Administrativo
- StationType: Administrativo
- MachineName: ADM-01 (automático)
- Description: Financeiro e administrativo

**ATEND-01:**
- StationName: Atendimento
- StationType: Vendas
- MachineName: ATEND-01 (automático)
- Description: Clientes e orçamentos

**ESTOQUE-01:**
- StationName: Estoque 01
- StationType: Estoque
- MachineName: ESTOQUE-01 (automático)
- Description: Controle de estoque

**OFICINA-01:**
- StationName: Oficina
- StationType: Oficina
- MachineName: OFICINA-01 (automático)
- Description: Ordens de serviço e agendamento

## 3. Instalação do Sistema

### 3.1 Instalação no Servidor

**Passo 1: Instalar SQL Server**
- Baixar SQL Server Express
- Executar instalador
- Configurar instância SQLEXPRESS
- Habilitar TCP/IP
- Configurar autenticação Windows + SQL Server

**Passo 2: Criar Banco de Dados**
- Abrir SQL Server Management Studio
- Criar banco PrimoAutoEletrica
- Executar script SqlServerSchema.sql

**Passo 3: Instalar Primo Auto Eletrica**
- Copiar arquivos do sistema
- Executar instalador
- Configurar conexão SQL Server
- Testar conexão
- Criar usuário administrador

**Passo 4: Configurar Backup**
- Configurar pasta de backup
- Configurar pasta compartilhada
- Testar backup manual
- Configurar backup automático

**Passo 5: Configurar Firewall**
- Permitir conexões SQL Server
- Permitir compartilhamento de arquivos
- Testar acesso de outras estações

### 3.2 Instalação nas Estações

**Passo 1: Instalar Primo Auto Eletrica**
- Copiar arquivos do sistema
- Executar instalador
- Configurar conexão SQL Server (192.168.0.10)
- Testar conexão
- Configurar estação (StationName, StationType)

**Passo 2: Testar Conectividade**
- Ping servidor (192.168.0.10)
- Testar acesso SQL Server
- Testar acesso pasta compartilhada
- Testar login no sistema

**Passo 3: Configurar Periféricos**
- Configurar impressora
- Configurar leitor de código de barras
- Configurar balança (se aplicável)
- Testar funcionamento

## 4. Configuração Inicial do Sistema

### 4.1 Configuração Geral

**Dados da Oficina:**
- Nome: Primo Auto Eletrica
- Telefone: (XX) XXXX-XXXX
- Endereço: Rua XXX, Número XXX
- Bairro: XXX
- Cidade: XXX
- Estado: XX
- CEP: XXXXX-XXX
- CNPJ: XX.XXX.XXX/XXXX-XX
- Logotipo: (upload)

### 4.2 Configuração de Banco

**Provider:**
- SQLite: Para uso local/teste
- SQL Server: Para produção/multiusuário

**Conexão SQL Server:**
- Servidor: 192.168.0.10
- Instância: SQLEXPRESS
- Banco: PrimoAutoEletrica
- Autenticação: Windows
- Timeout: 30 segundos

### 4.3 Configuração de Backup

**Pasta Local:**
- C:\PrimoAutoEletrica\Backups

**Pasta de Rede:**
- \\PRIMO-SERVER\BackupsPrimoAutoEletrica

**Frequência:**
- Backup automático diário: 18:00
- Backup ao encerrar: Sim
- Backup ao atualizar: Sim

**Retenção:**
- Manter backups por: 30 dias

### 4.4 Configuração Multiusuário

**Tempo de Expiração de Sessão:**
- 60 minutos

**Tempo de Expiração de Bloqueio:**
- 30 minutos

**Fallback para SQLite:**
- Não (em ambiente multiusuário)

### 4.5 Configuração PDV

**Caixa Padrão:**
- Caixa 01

**Impressora:**
- Configurar impressora fiscal
- Configurar impressora de não fiscal

### 4.6 Configuração da Estação

**Nome da Estação:**
- Configurado conforme tipo (Caixa 01, Administrativo, etc.)

**Tipo de Estação:**
- Configurado conforme função (Caixa, Administrativo, Vendas, Estoque, Oficina)

## 5. Gerenciamento de Usuários

### 5.1 Perfis de Usuário

**Administrador:**
- Acesso total ao sistema
- Permissões: Todas
- Telas: Todas
- Ações: Todas

**Gerente:**
- Acesso a relatórios e configurações
- Permissões: Gerenciais
- Telas: Relatórios, Financeiro, Clientes
- Ações: Visualizar, Editar, Aprovar

**Caixa:**
- Acesso a PDV
- Permissões: Vendas, Caixa
- Telas: PDV, Clientes (visualizar)
- Ações: Vender, Cancelar, Fechar caixa

**Mecânico:**
- Acesso a OS
- Permissões: OS, Agendamento
- Telas: OS, Agendamento
- Ações: Visualizar, Editar, Atualizar status

**Estoque:**
- Acesso a estoque
- Permissões: Estoque, Produtos
- Telas: Estoque, Produtos
- Ações: Visualizar, Editar, Entrada, Saída

**Atendimento:**
- Acesso a clientes e orçamentos
- Permissões: Clientes, Orçamentos
- Telas: Clientes, Orçamentos
- Ações: Visualizar, Editar, Criar

**Financeiro:**
- Acesso a financeiro
- Permissões: Financeiro, Contas
- Telas: Financeiro, Contas
- Ações: Visualizar, Editar, Pagar, Receber

### 5.2 Criação de Usuários

**Usuário Administrador Inicial:**
- Email: admin@primoauto.com
- Senha: (definida na instalação)
- Perfil: Administrador

**Usuários Adicionais:**
- Criar conforme necessidade
- Definir perfil adequado
- Definir permissões específicas

## 6. Importação de Dados

### 6.1 Importação de Clientes

**Formato:**
- CSV ou Excel
- Colunas: Nome, CPF/CNPJ, Email, Telefone, Endereço, etc.

**Processo:**
1. Preparar arquivo de importação
2. Pré-visualizar dados
3. Validar dados
4. Importar
5. Verificar resultado

### 6.2 Importação de Produtos

**Formato:**
- CSV ou Excel
- Colunas: Código, Nome, Descrição, Categoria, Preço, Estoque, etc.

**Processo:**
1. Preparar arquivo de importação
2. Pré-visualizar dados
3. Validar dados
4. Importar
5. Verificar resultado

### 6.3 Importação de Fornecedores

**Formato:**
- CSV ou Excel
- Colunas: Nome, CNPJ, Email, Telefone, Endereço, etc.

**Processo:**
1. Preparar arquivo de importação
2. Pré-visualizar dados
3. Validar dados
4. Importar
5. Verificar resultado

## 7. Validação do Ambiente

### 7.1 Checklist de Validação

**Servidor:**
- [ ] SQL Server instalado e funcionando
- [ ] Banco de dados criado
- [ ] Firewall configurado
- [ ] Pasta compartilhada acessível
- [ ] Backup configurado
- [ ] IP fixo configurado

**Estações:**
- [ ] Sistema instalado
- [ ] Conexão SQL Server funcionando
- [ ] Estação configurada
- [ ] Login funcionando
- [ ] Periféricos configurados

**Rede:**
- [ ] Ping servidor funcionando
- [ ] Latência < 10ms
- [ ] Sem falhas de conexão
- [ ] Acesso pasta compartilhada funcionando

**Sistema:**
- [ ] Usuários criados
- [ ] Permissões configuradas
- [ ] Dados importados
- [ ] Backup testado
- [ ] Restauração testada

## 8. Manutenção Preventiva

### 8.1 Diária

- Verificar logs de erros
- Verificar backup automático
- Verificar espaço em disco
- Verificar usuários online

### 8.2 Semanal

- Verificar integridade do banco
- Limpar logs antigos
- Limpar backups antigos
- Verificar performance

### 8.3 Mensal

- Atualizar sistema
- Revisar configurações
- Testar restauração
- Revisar permissões

## 9. Suporte e Contingência

### 9.1 Plano de Falha

**Se Servidor Parar:**
1. Verificar energia
2. Verificar conexão de rede
3. Reiniciar servidor
4. Verificar SQL Server
5. Verificar backups

**Se SQL Server Parar:**
1. Reiniciar serviço SQL Server
2. Verificar logs do SQL Server
3. Verificar espaço em disco
4. Verificar conexões ativas
5. Reiniciar servidor se necessário

**Se Backup Falhar:**
1. Verificar espaço em disco
2. Verificar permissões
3. Verificar pasta de destino
4. Verificar integridade do banco
5. Criar backup manual

**Se Banco Corromper:**
1. Parar sistema em todas as estações
2. Restaurar último backup válido
3. Verificar integridade
4. Reiniciar sistema
5. Verificar dados

### 9.2 Contato de Suporte

**Suporte Interno:**
- Administrador do sistema
- TI responsável

**Suporte Externo:**
- Desenvolvedor do sistema
- Suporte SQL Server (Microsoft)

## 10. Documentação

### 10.1 Documentação Necessária

- Guia de instalação
- Guia de configuração
- Guia de uso por perfil
- Guia de backup e restauração
- Guia de contingência
- Guia de atualização
- Guia de troubleshooting

### 10.2 Manutenção da Documentação

- Atualizar documentação a cada mudança
- Manter documentação acessível
- Treinar equipe na documentação
- Revisar documentação periodicamente
