# Setup SQL Server em Rede Local - Primo Auto Elétrica

## Visão Geral

Este documento fornece instruções detalhadas para configurar o SQL Server em rede local para uso multi-usuário do Primo Auto Elétrica.

## Requisitos

### Servidor

- Windows Server 2016+ ou Windows 10 Pro/Enterprise
- SQL Server 2017+ (Express ou Standard)
- 8 GB RAM mínimo
- 50 GB disco
- Conexão de rede estável

### Clientes

- Windows 7 SP1+ ou superior
- Conexão de rede estável
- Primo Auto Elétrica instalado

## Instalação do SQL Server

### 1. Download

Baixe o SQL Server Express em:
https://www.microsoft.com/pt-br/sql-server/sql-server-downloads

### 2. Instalação

1. Execute o instalador
2. Selecione **Custom**
3. Aceite os termos
4. Selecione **SQL Server Express**
5. Configure **Instance**:
   - Instance ID: SQLEXPRESS
   - Instance root directory: C:\Program Files\Microsoft SQL Server
6. Configure **Server Configuration**:
   - Service Account: NT AUTHORITY\SYSTEM
   - Startup Type: Automatic
7. Configure **Authentication Mode**:
   - Selecione **Mixed Mode**
   - Defina senha para sa
8. Configure **Data Directories**:
   - Data root directory: C:\Program Files\Microsoft SQL Server\MSSQL15.SQLEXPRESS\MSSQL\Data
9. Conclua a instalação

## Configuração de Rede

### 1. Habilitar TCP/IP

1. Abra **SQL Server Configuration Manager**
2. Navegue para **SQL Server Network Configuration** > **Protocols for SQLEXPRESS**
3. Habilite **TCP/IP**
4. Clique com botão direito > **Properties**
5. Vá para aba **IP Addresses**
6. Em IPAll:
   - TCP Port: 1433
   - TCP Dynamic Ports: (deixe vazio)
7. Clique em **OK**
8. Reinicie o serviço SQL Server

### 2. Configurar Firewall

#### Windows Server

```powershell
# Permitir porta 1433
New-NetFirewallRule -DisplayName "SQL Server" -Direction Inbound -Protocol TCP -LocalPort 1433 -Action Allow

# Permitir SQL Browser (opcional)
New-NetFirewallRule -DisplayName "SQL Browser" -Direction Inbound -Protocol UDP -LocalPort 1434 -Action Allow
```

#### Windows 10

1. Abra **Windows Defender Firewall**
2. Clique em **Allow an app or feature through Windows Defender Firewall**
3. Clique em **Change settings**
4. Encontre **SQL Server** e marque todas as caixas
5. Clique em **OK**

### 3. Verificar Porta

```powershell
Test-NetConnection -ComputerName localhost -Port 1433
```

## Criação do Banco de Dados

### 1. Conectar ao SQL Server

Abra **SQL Server Management Studio**:
- Server name: .\SQLEXPRESS
- Authentication: SQL Server Authentication
- Login: sa
- Password: [senha definida na instalação]

### 2. Criar Banco

```sql
CREATE DATABASE PrimoAutoEletrica;
GO

USE PrimoAutoEletrica;
GO
```

### 3. Criar Usuário

```sql
CREATE LOGIN primo_user WITH PASSWORD = 'SuaSenhaSegura123!';
GO

USE PrimoAutoEletrica;
GO

CREATE USER primo_user FOR LOGIN primo_user;
GO

ALTER ROLE db_owner ADD MEMBER primo_user;
GO
```

## Configuração do Cliente

### 1. Testar Conexão

```powershell
.\Test-SqlServerNetwork.ps1 -ServerName "SERVIDOR\SQLEXPRESS" -Database "PrimoAutoEletrica" -Username "primo_user" -Password "SuaSenhaSegura123!"
```

### 2. Configurar Primo Auto Elétrica

1. Abra o sistema
2. Acesse **Configurações** > **Banco de Dados**
3. Selecione **SQL Server**
4. String de conexão:
   ```
   Server=SERVIDOR\SQLEXPRESS;Database=PrimoAutoEletrica;User Id=primo_user;Password=SuaSenhaSegura123!;
   ```
5. Clique em **Testar Conexão**
6. Se bem-sucedido, clique em **Salvar**

## Migração de SQLite para SQL Server

### 1. Backup SQLite

Antes de migrar, faça backup do banco SQLite:
1. Acesse **Configurações** > **Backup**
2. Clique em **Criar Backup Manual**
3. Aguarde conclusão

### 2. Exportar Dados

Use ferramenta de migração ou script SQL para exportar dados do SQLite.

### 3. Importar Dados

Importe os dados para o SQL Server usando o mesmo script ou ferramenta.

### 4. Validar Migração

1. Verifique contagem de registros
2. Valide integridade referencial
3. Teste funcionalidades principais

## Manutenção

### Backup Automático

Configure backup automático no SQL Server:

```sql
-- Criar backup job
USE msdb;
GO

EXEC sp_add_job
    @job_name = 'Backup PrimoAutoEletrica';

EXEC sp_add_jobstep
    @job_name = 'Backup PrimoAutoEletrica',
    @step_name = 'Backup Database',
    @subsystem = 'TSQL',
    @command = 'BACKUP DATABASE PrimoAutoEletrica TO DISK = ''C:\Backups\PrimoAutoEletrica.bak'' WITH INIT';

EXEC sp_add_schedule
    @schedule_name = 'Daily Backup',
    @freq_type = 4, -- Daily
    @freq_interval = 1,
    @active_start_time = 020000; -- 02:00

EXEC sp_attach_schedule
    @job_name = 'Backup PrimoAutoEletrica',
    @schedule_name = 'Daily Backup';

EXEC sp_add_jobserver
    @job_name = 'Backup PrimoAutoEletrica';
```

### Manutenção de Índices

```sql
-- Rebuild índices
USE PrimoAutoEletrica;
GO

ALTER INDEX ALL ON Customers REBUILD;
ALTER INDEX ALL ON Vehicles REBUILD;
ALTER INDEX ALL ON ServiceOrders REBUILD;
```

## Solução de Problemas

### Não consigo conectar

**Verificações:**
1. SQL Server está rodando?
2. TCP/IP está habilitado?
3. Firewall está configurado?
4. Porta 1433 está aberta?
5. Usuário existe e tem permissões?

### Performance lenta

**Soluções:**
1. Adicione índices
2. Otimize consultas
3. Aumente RAM do servidor
4. Use SQL Server Standard (melhor performance que Express)

### Erro de login

**Soluções:**
1. Verifique autenticação mista habilitada
2. Verifique usuário existe
3. Verifique senha correta
4. Verifique permissões db_owner

## Segurança

### Boas Práticas

- Use senhas fortes
- Limite acesso de rede
- Use autenticação Windows quando possível
- Criptografe conexão SSL
- Faça backup regularmente
- Monitore logs de segurança

### Auditoria

Habilite auditoria para rastrear atividades:

```sql
-- Habilitar auditoria
USE master;
GO

CREATE SERVER AUDIT PrimoAutoEletrica_Audit
TO FILE (FILEPATH = 'C:\Audit\');

ALTER SERVER AUDIT PrimoAutoEletrica_Audit WITH (STATE = ON);
```

---

**Versão:** 1.0
**Última atualização:** 2026-06-17
