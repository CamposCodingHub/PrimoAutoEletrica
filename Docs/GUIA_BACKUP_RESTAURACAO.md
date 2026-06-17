# GUIA DE BACKUP E RESTAURAÇÃO - PRIMO AUTO ELÉTRICA

**Data:** 2026-06-10  
**Projeto:** PrimoAutoEletrica  
**Versão:** 1.0

---

## 1. VISÃO GERAL

Este guia explica como criar backups do projeto PrimoAutoEletrica e como restaurá-los em caso de problemas.

---

## 2. TIPOS DE BACKUP

### 2.1 Backup Completo do Projeto

Copia todos os arquivos do projeto (código, documentação, scripts) e opcionalmente o banco de dados.

**Quando usar:**
- Antes de grandes alterações
- Antes de migrations
- Antes de atualizações
- Diariamente (recomendado)

**Como criar:**
```powershell
cd C:\Projetos\PrimoAutoEletrica
.\Scripts\Create-ProjectBackup.ps1
```

**O que é copiado:**
- Todo o código fonte
- Documentação
- Scripts
- Banco de dados (opcional)
- Arquivos de configuração

**O que NÃO é copiado:**
- bin/ (binários compilados)
- obj/ (objetos de compilação)
- .vs/ (configurações do Visual Studio)
- Logs/ (logs temporários)
- TestResults/ (resultados de testes)

### 2.2 Backup do Banco de Dados

Copia apenas o arquivo do banco de dados SQLite.

**Localização do banco:**
```
%LocalApplicationData%\PrimoAutoEletrica\database.db
```

**Como criar manualmente:**
```powershell
$LocalAppData = [Environment]::GetFolderPath("LocalApplicationData")
$DatabasePath = Join-Path $LocalAppData "PrimoAutoEletrica\database.db"
$BackupPath = ".\Backups\database_backup_$(Get-Date -Format 'yyyy-MM-dd_HH-mm-ss').db"

Copy-Item -Path $DatabasePath -Destination $BackupPath -Force
```

### 2.3 Backup via Git

O projeto usa Git para controle de versão.

**Como fazer commit:**
```powershell
cd C:\Projetos\PrimoAutoEletrica

git add .
git commit -m "Descrição das alterações"
git push
```

**Como criar branch:**
```powershell
git checkout -b nome-do-branch
```

**Como voltar para commit anterior:**
```powershell
git log --oneline
git checkout <hash-do-commit>
```

---

## 3. RESTAURAÇÃO

### 3.1 Restaurar Backup Completo

**Passos:**
1. Navegue até a pasta do backup: `.\Backups\PrimoAutoEletrica_Backup_DATA_HORA`
2. Copie todo o conteúdo para a pasta do projeto: `C:\Projetos\PrimoAutoEletrica`
3. Se o backup incluiu o banco de dados, copie para: `%LocalApplicationData%\PrimoAutoEletrica\`
4. Execute `dotnet restore` para restaurar pacotes NuGet
5. Execute `dotnet build` para compilar
6. Execute `dotnet run` para testar

### 3.2 Restaurar Apenas Banco de Dados

**Passos:**
1. Pare a aplicação se estiver rodando
2. Navegue até: `%LocalApplicationData%\PrimoAutoEletrica\`
3. Renomeie `database.db` para `database_backup_old.db`
4. Copie o backup do banco para: `%LocalApplicationData%\PrimoAutoEletrica\database.db`
5. Execute a aplicação para testar

### 3.3 Restaurar via Git

**Para voltar para commit anterior:**
```powershell
git log --oneline
git checkout <hash-do-commit>
```

**Para descartar alterações não commitadas:**
```powershell
git restore .
```

**Para voltar para branch anterior:**
```powershell
git checkout nome-do-branch
```

---

## 4. PACOTE LIMPO

### 4.1 O que é Pacote Limpo?

Pacote limpo é uma cópia do projeto sem arquivos temporários (bin, obj, .vs, logs).

**Quando usar:**
- Para compartilhar o projeto
- Para criar instalador
- Para testar em ambiente limpo

**Como criar:**
```powershell
cd C:\Projetos\PrimoAutoEletrica
.\Scripts\Clean-PackageProject.ps1
```

**Onde fica:**
```
.\PackageClean\PrimoAutoEletrica_PackageClean_DATA_HORA\
```

---

## 5. ROTINAS RECOMENDADAS

### 5.1 Antes de Qualquer Alteração Importante

1. Criar backup completo
2. Criar branch no Git
3. Fazer as alterações
4. Testar
5. Commit no Git

### 5.2 Diariamente

1. Commit no Git
2. Backup do banco de dados

### 5.3 Semanalmente

1. Backup completo do projeto
2. Testar restauração

---

## 6. SOLUÇÃO DE PROBLEMAS

### 6.1 Backup Falhou

**Possíveis causas:**
- Arquivos em uso
- Permissões insuficientes
- Espaço em disco

**Soluções:**
- Feche o Visual Studio
- Execute como administrador
- Verifique espaço em disco

### 6.2 Restauração Falhou

**Possíveis causas:**
- Caminho incorreto
- Arquivos em uso
- Versão incompatível

**Soluções:**
- Verifique o caminho
- Feche a aplicação
- Verifique a versão do backup

---

## 7. VERIFICAÇÃO DE INTEGRIDADE

### 7.1 Verificar Backup

**Verifique se:**
- O arquivo BACKUP_METADATA.txt existe
- O número de arquivos está correto
- O tamanho do backup é razoável

### 7.2 Testar Restauração

**Periodicamente:**
1. Restaure o backup em pasta temporária
2. Execute `dotnet build`
3. Execute `dotnet run`
4. Verifique se tudo funciona

---

## 8. AUTOMATIZAÇÃO FUTURA

### 8.1 Backup Automático

Futuramente pode-se criar:
- Agendamento de backup diário
- Backup automático antes de migrations
- Backup automático antes de atualizações
- Notificação de backup falhado

### 8.2 Backup em Nuvem

Futuramente pode-se adicionar:
- Upload para Google Drive
- Upload para Dropbox
- Upload para OneDrive
- Backup criptografado

---

## 9. CONTATO

Em caso de dúvidas ou problemas com backup/restauração, consulte:
- Documentação técnica: Docs/
- Logs: Logs/
- Relatórios: Reports/

---

**Última atualização:** 2026-06-10
