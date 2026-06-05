# Guia de Teste de Rede Local - Primo Auto Eletrica

## Objetivo
Validar o funcionamento real do sistema em rede local usando múltiplos computadores conectados ao mesmo banco central.

## Cenário de Teste

```
PC Servidor (192.168.1.100)
├── SQL Server instalado
├── Banco central: PrimoAutoEletrica
├── Backup configurado
├── Firewall configurado
└── Pasta compartilhada: \\SERVIDOR\BackupsPrimoAutoEletrica

PC Caixa (192.168.1.101)
├── Sistema instalado
├── Conectado ao servidor
└── Tipo: Caixa

PC Administrativo (192.168.1.102)
├── Sistema instalado
├── Conectado ao servidor
└── Tipo: Administrativo

PC Estoque/Vendas (192.168.1.103)
├── Sistema instalado
├── Conectado ao servidor
└── Tipo: Estoque
```

## 1. Preparação do Servidor

### 1.1 Instalar SQL Server

```powershell
# Baixar SQL Server Express
# https://www.microsoft.com/pt-br/sql-server/sql-server-downloads

# Instalar com configurações:
# - Instância: SQLEXPRESS
# - Autenticação: Windows + SQL
# - Porta: 1433
# - TCP/IP habilitado
```

### 1.2 Configurar Firewall

```powershell
# Permitir conexões SQL Server
New-NetFirewallRule -DisplayName "SQL Server" -Direction Inbound -Protocol TCP -LocalPort 1433 -Action Allow

# Permitir conexões SQL Browser
New-NetFirewallRule -DisplayName "SQL Browser" -Direction Inbound -Protocol UDP -LocalPort 1434 -Action Allow

# Permitir compartilhamento de arquivos (SMB)
New-NetFirewallRule -DisplayName "SMB" -Direction Inbound -Protocol TCP -LocalPort 445 -Action Allow
```

### 1.3 Criar Banco de Dados

```sql
-- No SQL Server Management Studio
CREATE DATABASE PrimoAutoEletrica;
GO

-- Executar script SqlServerSchema.sql
USE PrimoAutoEletrica;
GO

-- Executar o script completo do schema
```

### 1.4 Configurar Pasta Compartilhada de Backup

```powershell
# Criar pasta
New-Item -Path "C:\BackupsPrimoAutoEletrica" -ItemType Directory -Force

# Compartilhar pasta
New-SmbShare -Name "BackupsPrimoAutoEletrica" -Path "C:\BackupsPrimoAutoEletrica" -FullAccess "Todos"

# Configurar permissões NTFS
$acl = Get-Acl "C:\BackupsPrimoAutoEletrica"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("Todos","FullControl","ContainerInherit,ObjectInherit","None","Allow")
$acl.SetAccessRule($accessRule)
Set-Acl "C:\BackupsPrimoAutoEletrica" $acl
```

### 1.5 Configurar IP Fixo

```powershell
# Configurar IP fixo no servidor
# Exemplo: 192.168.1.100
# Máscara: 255.255.255.0
# Gateway: 192.168.1.1
# DNS: 192.168.1.1
```

### 1.6 Criar Usuário SQL Server (opcional)

```sql
-- Se não usar autenticação Windows
CREATE LOGIN primo_user WITH PASSWORD = 'SenhaForte123!';
GO

USE PrimoAutoEletrica;
GO

CREATE USER primo_user FOR LOGIN primo_user;
GO

ALTER ROLE db_owner ADD MEMBER primo_user;
GO
```

## 2. Preparação dos Clientes

### 2.1 Instalar Sistema em Cada PC

```powershell
# Copiar pasta do sistema para cada PC
# Instalar .NET 9.0 Runtime
# Configurar atalho na área de trabalho
```

### 2.2 Configurar Conexão no PC Caixa

1. Abrir sistema
2. Ir em Configurações > Banco de Dados
3. Selecionar SQL Server
4. Configurar:
   - Servidor: 192.168.1.100
   - Instância: SQLEXPRESS
   - Banco: PrimoAutoEletrica
   - Autenticação: Windows
5. Testar conexão
6. Salvar

### 2.3 Configurar Estação no PC Caixa

1. Ir em Configurações > Multiusuário / Rede
2. Configurar:
   - Nome da máquina: (automático)
   - Nome da estação: Caixa 01
   - Tipo de estação: Caixa
   - Descrição: Caixa principal
3. Salvar

### 2.4 Repetir para Outros PCs

**PC Administrativo:**
- Nome da estação: Administrativo
- Tipo de estação: Administrativo

**PC Estoque:**
- Nome da estação: Estoque 01
- Tipo de estação: Estoque

## 3. Checklist de Validação de Conectividade

### 3.1 Testar Ping

```powershell
# No PC Caixa
ping 192.168.1.100

# Esperado: Resposta < 10ms
```

### 3.2 Testar Acesso SQL Server

```powershell
# No PC Caixa
# Usar SQL Server Management Studio ou telnet
telnet 192.168.1.100 1433

# Esperado: Conexão estabelecida
```

### 3.3 Testar Acesso Pasta Compartilhada

```powershell
# No PC Caixa
# Abrir Windows Explorer
\\192.168.1.100\BackupsPrimoAutoEletrica

# Esperado: Pasta acessível
```

### 3.4 Registrar Dados

```
PC Caixa:
- IP: 192.168.1.101
- Nome máquina: CAIXA01
- Tempo resposta ping: ___ ms
- Tempo resposta SQL: ___ ms
- Acesso backup: [ ] Sim [ ] Não

PC Administrativo:
- IP: 192.168.1.102
- Nome máquina: ADM01
- Tempo resposta ping: ___ ms
- Tempo resposta SQL: ___ ms
- Acesso backup: [ ] Sim [ ] Não

PC Estoque:
- IP: 192.168.1.103
- Nome máquina: ESTOQUE01
- Tempo resposta ping: ___ ms
- Tempo resposta SQL: ___ ms
- Acesso backup: [ ] Sim [ ] Não
```

## 4. Procedimento de Teste de Login Multiusuário

### 4.1 Preparação

Criar usuários de teste:
- caixa@primoauto.com (Perfil: Caixa)
- gerente@primoauto.com (Perfil: Administrador)
- vendedor@primoauto.com (Perfil: Vendedor)

### 4.2 Executar Teste

**Passo 1: Login PC Caixa**
1. Abrir sistema no PC Caixa
2. Login: caixa@primoauto.com
3. Verificar: Login bem-sucedido
4. Registrar: Sessão criada

**Passo 2: Login PC Administrativo**
1. Abrir sistema no PC Administrativo
2. Login: gerente@primoauto.com
3. Verificar: Login bem-sucedido
4. Registrar: Sessão criada

**Passo 3: Login PC Estoque**
1. Abrir sistema no PC Estoque
2. Login: vendedor@primoauto.com
3. Verificar: Login bem-sucedido
4. Registrar: Sessão criada

**Passo 4: Verificar Sessões Online**
1. No PC Administrativo
2. Ir em Configurações > Multiusuário / Rede
3. Abrir tela "Usuários Online"
4. Verificar: 3 sessões ativas
5. Registrar: Todas as sessões visíveis

**Passo 5: Testar Logout**
1. Fazer logout no PC Caixa
2. Verificar: Sessão removida da lista
3. Registrar: Logout funcionou

**Passo 6: Testar Expiração**
1. Aguardar 60 minutos sem atividade
2. Verificar: Sessão expirada automaticamente
3. Registrar: Expiração funcionou

### 4.3 Resultado Esperado

- [ ] Todos os logins funcionam
- [ ] Sessões aparecem na lista de usuários online
- [ ] Heartbeat funciona
- [ ] Logout funciona
- [ ] Expiração funciona

## 5. Procedimento de Teste de Concorrência

### 5.1 Teste de Cliente

**Passo 1: PC1 Abre Cliente**
1. No PC Caixa
2. Abrir cliente existente
3. Clicar em "Editar"
4. Verificar: Janela de edição abriu
5. Registrar: Cliente bloqueado

**Passo 2: PC2 Tenta Editar Mesmo Cliente**
1. No PC Administrativo
2. Abrir mesmo cliente
3. Clicar em "Editar"
4. Verificar: Mensagem de bloqueio
5. Mensagem esperada: "Este registro está sendo editado por [usuário] em [máquina]"
6. Registrar: Bloqueio detectado

**Passo 3: PC1 Salva e Fecha**
1. No PC Caixa
2. Salvar alterações
3. Fechar janela
4. Registrar: Bloqueio liberado

**Passo 4: PC2 Tenta Novamente**
1. No PC Administrativo
2. Clicar em "Editar"
3. Verificar: Janela de edição abriu
4. Registrar: Bloqueio liberado

### 5.2 Repetir para Outras Entidades

**Produto:**
- [ ] Bloqueio funciona
- [ ] Mensagem amigável
- [ ] Liberação funciona

**Ordem de Serviço:**
- [ ] Bloqueio funciona
- [ ] Mensagem amigável
- [ ] Liberação funciona

**Orçamento:**
- [ ] Bloqueio funciona
- [ ] Mensagem amigável
- [ ] Liberação funciona

**Financeiro:**
- [ ] Bloqueio funciona
- [ ] Mensagem amigável
- [ ] Liberação funciona

**Agendamento:**
- [ ] Bloqueio funciona
- [ ] Mensagem amigável
- [ ] Liberação funciona

## 6. Procedimento de Teste de Estoque

### 6.1 Preparação

Criar produto de teste:
- Nome: "Produto Teste Rede"
- Estoque inicial: 100 unidades

### 6.2 Executar Teste

**Passo 1: PC1 Realiza Venda**
1. No PC Caixa
2. Criar venda com 10 unidades do produto
3. Finalizar venda
4. Verificar: Estoque atualizado para 90
5. Registrar: Venda realizada

**Passo 2: PC2 Consulta Estoque**
1. No PC Estoque
2. Abrir tela de estoque
3. Consultar produto "Produto Teste Rede"
4. Verificar: Estoque mostra 90 unidades
5. Registrar: Estoque sincronizado

**Passo 3: PC1 Realiza Outra Venda**
1. No PC Caixa
2. Criar venda com 5 unidades
3. Finalizar venda
4. Verificar: Estoque atualizado para 85
5. Registrar: Segunda venda realizada

**Passo 4: PC2 Consulta Novamente**
1. No PC Estoque
2. Atualizar tela de estoque
3. Consultar produto
4. Verificar: Estoque mostra 85 unidades
5. Registrar: Sincronização em tempo real

### 6.3 Resultado Esperado

- [ ] Estoque atualizado imediatamente após venda
- [ ] Sem duplicidade de registros
- [ ] Sem atraso excessivo (< 2 segundos)
- [ ] Todos os PCs veem o mesmo valor

## 7. Procedimento de Teste de Queda de Rede

### 7.1 Simular Queda de Servidor

**Passo 1: Operação Normal**
1. Todos os PCs conectados
2. Realizar uma venda
3. Verificar: Funciona normalmente
4. Registrar: Operação normal

**Passo 2: Desconectar Servidor**
1. Desconectar cabo de rede do servidor
2. Aguardar 10 segundos
3. Registrar: Servidor desconectado

**Passo 3: Tentar Operação no PC Caixa**
1. Tentar criar venda
2. Verificar: Mensagem de erro amigável
3. Mensagem esperada: "Não foi possível conectar ao banco de dados"
4. Verificar: Nenhuma corrupção de dados
5. Registrar: Erro tratado corretamente

**Passo 4: Reconectar Servidor**
1. Reconectar cabo de rede
2. Aguardar 10 segundos
3. Registrar: Servidor reconectado

**Passo 5: Tentar Operação Novamente**
1. Tentar criar venda
2. Verificar: Funciona normalmente
3. Registrar: Reconexão funcionou

### 7.2 Simular Queda do SQL Server

**Passo 1: Parar SQL Server**
1. No servidor
2. Parar serviço SQL Server
3. Registrar: SQL Server parado

**Passo 2: Tentar Operação no PC Administrativo**
1. Tentar abrir clientes
2. Verificar: Mensagem de erro amigável
3. Verificar: Nenhuma corrupção
4. Registrar: Erro tratado

**Passo 3: Reiniciar SQL Server**
1. No servidor
2. Iniciar serviço SQL Server
3. Registrar: SQL Server reiniciado

**Passo 4: Tentar Operação Novamente**
1. Tentar abrir clientes
2. Verificar: Funciona normalmente
3. Registrar: Reconexão funcionou

### 7.3 Resultado Esperado

- [ ] Alerta amigável em caso de queda
- [ ] Nenhuma corrupção de dados
- [ ] Reconexão possível automaticamente
- [ ] Sistema continua estável após reconexão

## 8. Procedimento de Teste de Performance

### 8.1 Medir Tempos de Abertura de Telas

**PC Caixa:**
- Abrir clientes: ___ ms
- Abrir estoque: ___ ms
- Abrir vendas: ___ ms
- Abrir OS: ___ ms

**PC Administrativo:**
- Abrir clientes: ___ ms
- Abrir estoque: ___ ms
- Abrir vendas: ___ ms
- Abrir OS: ___ ms

**PC Estoque:**
- Abrir clientes: ___ ms
- Abrir estoque: ___ ms
- Abrir vendas: ___ ms
- Abrir OS: ___ ms

### 8.2 Medir Tempos de Consultas

**Consulta de clientes (1000 registros):**
- PC Caixa: ___ ms
- PC Administrativo: ___ ms
- PC Estoque: ___ ms

**Consulta de produtos (500 registros):**
- PC Caixa: ___ ms
- PC Administrativo: ___ ms
- PC Estoque: ___ ms

**Consulta de OS (200 registros):**
- PC Caixa: ___ ms
- PC Administrativo: ___ ms
- PC Estoque: ___ ms

### 8.3 Medir Tempos de Operações

**Criar venda:**
- PC Caixa: ___ ms
- PC Administrativo: ___ ms
- PC Estoque: ___ ms

**Criar OS:**
- PC Caixa: ___ ms
- PC Administrativo: ___ ms
- PC Estoque: ___ ms

**Gerar relatório:**
- PC Caixa: ___ ms
- PC Administrativo: ___ ms
- PC Estoque: ___ ms

### 8.4 Resultado Esperado

- [ ] Tempo médio de abertura < 500ms
- [ ] Tempo médio de consulta < 200ms
- [ ] Tempo médio de operação < 1000ms
- [ ] Sem gargalos identificados

## 9. Checklist Final de Validação

### 9.1 Conectividade

- [ ] Todos os PCs podem pingar o servidor
- [ ] Todos os PCs podem acessar SQL Server
- [ ] Todos os PCs podem acessar pasta de backup
- [ ] Latência < 10ms em todos os PCs
- [ ] Sem falhas de conexão

### 9.2 Login Multiusuário

- [ ] Login funciona em todos os PCs
- [ ] Sessões aparecem na lista de usuários online
- [ ] Heartbeat funciona
- [ ] Logout funciona
- [ ] Expiração funciona

### 9.3 Concorrência

- [ ] Bloqueio de cliente funciona
- [ ] Bloqueio de produto funciona
- [ ] Bloqueio de OS funciona
- [ ] Bloqueio de orçamento funciona
- [ ] Bloqueio de financeiro funciona
- [ ] Bloqueio de agendamento funciona
- [ ] Mensagens amigáveis
- [ ] Liberação funciona

### 9.4 Estoque

- [ ] Estoque atualizado imediatamente
- [ ] Sem duplicidade
- [ ] Sem atraso excessivo
- [ ] Todos os PCs sincronizados

### 9.5 Queda de Rede

- [ ] Alerta amigável
- [ ] Nenhuma corrupção
- [ ] Reconexão possível
- [ ] Sistema estável após reconexão

### 9.6 Performance

- [ ] Abertura de telas < 500ms
- [ ] Consultas < 200ms
- [ ] Operações < 1000ms
- [ ] Sem gargalos

## 10. Resultado Final

Sistema funcionando em múltiplos PCs:

- [ ] Sincronizado
- [ ] Estável
- [ ] Sem perda
- [ ] Sem corrupção
- [ ] Sem conflitos

**Status Final:** [ ] APROVADO [ ] REPROVADO

**Observações:**
___________________________________________________________
___________________________________________________________
___________________________________________________________
