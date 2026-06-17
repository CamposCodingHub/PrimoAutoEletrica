# Teste de SQL Server em Rede - Primo Auto Elétrica

## Visão Geral

Este documento descreve como testar a configuração do SQL Server em rede para uso multi-usuário do Primo Auto Elétrica.

## Pré-requisitos

### Servidor SQL Server

- SQL Server 2017 ou superior
- Windows Server 2016 ou superior (ou Windows 10 Pro/Enterprise)
- 8 GB de RAM mínimo (16 GB recomendado)
- 50 GB de espaço em disco
- Porta 1433 aberta no firewall

### Cliente

- Windows 7 SP1 ou superior
- Conexão de rede estável
- Permissões de acesso ao servidor

## Configuração do Servidor

### 1. Habilitar Protocolos TCP/IP

1. Abra **SQL Server Configuration Manager**
2. Navegue para **SQL Server Network Configuration** > **Protocols for SQLEXPRESS**
3. Habilite **TCP/IP**
4. Clique com botão direito > **Properties**
5. Vá para a aba **IP Addresses**
6. Configure IPAll:
   - TCP Port: 1433
   - TCP Dynamic Ports: (deixe vazio)
7. Reinicie o serviço SQL Server

### 2. Configurar Autenticação

1. Abra **SQL Server Management Studio**
2. Conecte ao servidor
3. Clique com botão direito no servidor > **Properties**
4. Vá para **Security**
5. Selecione **SQL Server and Windows Authentication mode**
6. Clique em **OK**
7. Reinicie o serviço SQL Server

### 3. Criar Usuário

1. No SQL Server Management Studio
2. Navegue para **Security** > **Logins**
3. Clique com botão direito > **New Login**
4. Crie usuário com permissões adequadas
5. Atribua role `db_owner` para o banco PrimoAutoEletrica

### 4. Configurar Firewall

```powershell
# Permitir porta 1433
New-NetFirewallRule -DisplayName "SQL Server" -Direction Inbound -Protocol TCP -LocalPort 1433 -Action Allow
```

## Configuração do Cliente

### 1. Testar Conexão

Use o script PowerShell:

```powershell
.\Test-SqlServerNetwork.ps1 -ServerName "SERVIDOR\SQLEXPRESS" -Database "PrimoAutoEletrica"
```

### 2. Configurar Sistema

1. Abra o Primo Auto Elétrica
2. Acesse **Configurações** > **Banco de Dados**
3. Selecione **SQL Server**
4. Digite a string de conexão:
   ```
   Server=SERVIDOR\SQLEXPRESS;Database=PrimoAutoEletrica;User ID=usuario;Password=senha;
   ```
5. Clique em **Testar Conexão**
6. Se bem-sucedido, clique em **Salvar**

## Testes

### Teste 1: Ping

```powershell
ping SERVIDOR
```

**Resultado esperado:** Resposta do servidor

### Teste 2: Porta TCP

```powershell
Test-NetConnection -ComputerName SERVIDOR -Port 1433
```

**Resultado esperado:** TcpTestSucceeded: True

### Teste 3: Conexão SQL

```powershell
.\Test-SqlServerNetwork.ps1 -ServerName "SERVIDOR\SQLEXPRESS"
```

**Resultado esperado:** Conexão bem-sucedida

### Teste 4: Multi-usuário

1. Abra o sistema no computador 1
2. Abra o sistema no computador 2
3. Crie ordem de serviço no computador 1
4. Atualize status no computador 2
5. Verifique sincronização

**Resultado esperado:** Dados sincronizados

## Solução de Problemas

### Não consigo conectar ao servidor

**Verificações:**
- Servidor está ligado?
- SQL Server está rodando?
- Firewall está configurado?
- Porta 1433 está aberta?
- Usuário e senha estão corretos?

### Conexão lenta

**Soluções:**
- Verifique latência de rede
- Otimize consultas SQL
- Adicione índices
- Aumente largura de banda

### Erro de permissão

**Soluções:**
- Verifique permissões do usuário
- Atribua role db_owner
- Verifique autenticação SQL Server habilitada

## Script de Teste Automatizado

O script `Test-SqlServerNetwork.ps1` realiza todos os testes automaticamente.

### Uso

```powershell
.\Test-SqlServerNetwork.ps1 -ServerName "SERVIDOR\SQLEXPRESS" -Database "PrimoAutoEletrica" -Username "sa" -Password "senha"
```

### Relatório

O script gera um relatório detalhado com:
- Status de cada teste
- Tempo de resposta
- Recomendações

## Documentação Adicional

Consulte `Scripts/Setup-SqlServerLocalNetwork.md` para instruções detalhadas de configuração.

---

**Versão:** 1.0
**Última atualização:** 2026-06-17
