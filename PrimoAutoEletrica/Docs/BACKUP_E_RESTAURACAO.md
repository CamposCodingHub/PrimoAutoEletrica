# Backup e Restauração - Primo Auto Elétrica

## Visão Geral

Este documento descreve o sistema completo de backup e restauração do Primo Auto Elétrica, incluindo backup local, backup externo e integração com serviços de nuvem.

## Objetivos

- Garantir integridade dos dados do usuário
- Permitir recuperação rápida em caso de falha
- Oferecer múltiplas opções de backup
- Suportar backup automático e manual
- Integrar com serviços de nuvem (OneDrive, Google Drive, Dropbox)
- Validar integridade dos backups
- Facilitar restauração seletiva

## Tipos de Backup

### 1. Backup Automático

**Frequência:**
- Diário (configurável)
- Semanal (configurável)
- Antes de atualizações
- Antes de migrações
- Antes de operações críticas

**O que é backup:**
- Banco de dados SQLite
- Arquivos de configuração
- Logs do sistema
- Arquivos de mídia (produtos, clientes)

**Configuração:**
```json
{
  "autoBackup": {
    "enabled": true,
    "frequency": "daily",
    "time": "02:00",
    "retentionDays": 30,
    "compress": true,
    "includeMedia": true
  }
}
```

### 2. Backup Manual

**Quando usar:**
- Antes de alterações importantes
- Antes de limpeza de dados
- Antes de importação em massa
- A pedido do usuário

**Opções:**
- Backup completo
- Backup parcial (apenas banco, apenas configurações)
- Backup seletivo (tabelas específicas)

### 3. Backup Externo

**Destinos:**
- Unidade de rede
- Disco externo (USB, HD externo)
- Servidor FTP/SFTP
- Serviços de nuvem (OneDrive, Google Drive, Dropbox)

**Configuração:**
```json
{
  "externalBackup": {
    "enabled": true,
    "destination": "D:\\Backups\\PrimoAutoEletrica",
    "syncWithCloud": true,
    "cloudProvider": "onedrive",
    "compress": true,
    "encrypt": false
  }
}
```

## Estrutura de Backup

### Diretório Local

```
%LOCALAPPDATA%\PrimoAutoEletrica\Backups\
├── Auto\
│   ├── Daily_20260617\
│   │   ├── primoauto.db
│   │   ├── database-settings.json
│   │   ├── business-config.json
│   │   └── Media\
│   ├── Daily_20260616\
│   └── Weekly_20260613\
├── PreUpdate_20260617_103130\
├── PreMigration_20260617_110000\
└── Manual_20260617_120000\
```

### Diretório Externo

```
{ExternalPath}\PrimoAutoEletrica\Backups\
├── {YYYYMMDD}_PrimoAutoEletrica_Backup.zip
├── {YYYYMMDD}_PrimoAutoEletrica_Backup.sha256
└── manifest.json
```

## Processo de Backup

### Passo 1: Preparação

**Verificações:**
- Espaço em disco disponível
- Permissões de escrita
- Integridade do banco de dados
- Conexão com destino externo (se aplicável)

### Passo 2: Criação do Backup

**Backup Completo:**
1. Fechar conexões ativas
2. Copiar banco de dados
3. Copiar configurações
4. Copiar arquivos de mídia
5. Compactar (se configurado)
6. Calcular hash SHA256
7. Salvar manifesto

**Backup Parcial:**
1. Fechar conexões ativas
2. Copiar apenas itens selecionados
3. Compactar (se configurado)
4. Calcular hash SHA256
5. Salvar manifesto

### Passo 3: Validação

**Verificações:**
- Hash SHA256
- Integridade do banco
- Tamanho do arquivo
- Estrutura de diretórios

### Passo 4: Armazenamento

**Local:**
- Salvar em diretório de backup
- Atualizar índice de backups
- Aplicar política de retenção

**Externo:**
- Copiar para destino externo
- Validar transferência
- Registrar log

## Processo de Restauração

### Passo 1: Seleção do Backup

**Interface:**
- Lista de backups disponíveis
- Filtros (data, tipo, tamanho)
- Preview de conteúdo
- Validação de integridade

### Passo 2: Validação

**Verificações:**
- Hash SHA256
- Integridade do banco
- Compatibilidade de versão
- Espaço em disco disponível

### Passo 3: Preparação

**Ações:**
- Fechar aplicação
- Fechar conexões de banco
- Criar backup do estado atual (snapshot)
- Notificar usuários conectados

### Passo 4: Restauração

**Opções:**

**Restauração Completa:**
- Restaurar banco de dados
- Restaurar configurações
- Restaurar arquivos de mídia
- Validar sistema

**Restauração Parcial:**
- Apenas banco de dados
- Apenas configurações
- Apenas arquivos específicos

### Passo 5: Validação Pós-Restauração

**Verificações:**
- Aplicação inicia corretamente
- Banco de dados acessível
- Configurações válidas
- Dados intactos
- Funcionalidades operacionais

## Compactação

### Formato ZIP

**Vantagens:**
- Redução de tamanho (50-70%)
- Compatibilidade universal
- Suporte a criptografia
- Compressão incremental

**Configuração:**
```json
{
  "compression": {
    "enabled": true,
    "format": "zip",
    "level": "normal",
    "password": null
  }
}
```

### Criptografia (Opcional)

**Suporte:**
- Criptografia AES-256
- Proteção por senha
- Compatível com ZIP padrão

## Integração com Nuvem

### OneDrive

**Configuração:**
- Pasta sincronizada do OneDrive
- Backup automático na nuvem
- Histórico de versões
- Acesso de qualquer dispositivo

### Google Drive

**Configuração:**
- Pasta do Google Drive
- API do Google Drive (futuro)
- Sincronização automática

### Dropbox

**Configuração:**
- Pasta do Dropbox
- API do Dropbox (futuro)
- Sincronização automática

## Validação de Integridade

### Hash SHA256

Todo backup deve ter seu hash calculado e validado:

```csharp
public string ComputeSHA256(string filePath)
{
    using var sha256 = SHA256.Create();
    using var stream = File.OpenRead(filePath);
    var bytes = sha256.ComputeHash(stream);
    return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
}
```

### Integridade do Banco

Verificar integridade do banco SQLite:

```sql
PRAGMA integrity_check;
```

## Política de Retenção

### Configuração Padrão

- Manter últimos 30 backups diários
- Manter últimos 12 backups semanais
- Manter últimos 6 backups mensais
- Manter backups manuais até exclusão manual
- Manter backups de atualização (últimos 10)

### Limpeza Automática

Remover backups antigos automaticamente:
- Diariamente às 03:00
- Antes de criar novo backup
- Quando espaço em disco < 10%

## Logs

### Formato

```
[2026-06-17 02:00:00] Iniciando backup automático...
[2026-06-17 02:00:05] Fechando conexões...
[2026-06-17 02:00:10] Copiando banco de dados...
[2026-06-17 02:00:30] Banco copiado: 15.2 MB
[2026-06-17 02:00:35] Copiando configurações...
[2026-06-17 02:00:40] Configurações copiadas
[2026-06-17 02:00:45] Copiando mídia...
[2026-06-17 02:01:00] Mídia copiada: 250.5 MB
[2026-06-17 02:01:05] Compactando...
[2026-06-17 02:01:30] Compactação concluída: 85.3 MB
[2026-06-17 02:01:35] Calculando hash SHA256...
[2026-06-17 02:01:40] Hash: abc123...
[2026-06-17 02:01:45] Backup concluído: Daily_20260617
```

### Localização

```
%LOCALAPPDATA%\PrimoAutoEletrica\Logs\backup-YYYYMMDD-HHMMSS.log
```

## Interface de Backup

### Janela Principal

**Componentes:**
- Lista de backups disponíveis
- Filtros (data, tipo, tamanho)
- Botão de criar backup manual
- Botão de restaurar
- Botão de validar
- Botão de excluir
- Configurações de backup

### Configurações

**Opções:**
- Habilitar backup automático
- Frequência de backup
- Retenção de backups
- Compactação
- Destino externo
- Sincronização com nuvem
- Notificações

## Solução de Problemas

### Backup Falha

1. Verificar espaço em disco
2. Verificar permissões
3. Verificar integridade do banco
4. Verificar conexão externa (se aplicável)
5. Tentar backup manual

### Restauração Falha

1. Validar backup novamente
2. Verificar compatibilidade de versão
3. Verificar espaço em disco
4. Tentar backup anterior
5. Reinstalar do zero

### Backup Externo Falha

1. Verificar conexão de rede
2. Verificar permissões de rede
3. Verificar espaço no destino
4. Verificar credenciais (se aplicável)
5. Tentar backup local

## Boas Práticas

### Para Usuários

- Fazer backup manual antes de alterações importantes
- Manter backup externo regularmente
- Validar backups periodicamente
- Testar restauração em ambiente seguro
- Manter backup offsite (fora da empresa)

### Para Administradores

- Configurar backup automático
- Monitorar logs de backup
- Validar integridade regularmente
- Testar restauração mensalmente
- Manter política de retenção clara

## Roadmap

### Versão 1.0 (Atual)
- Backup automático local
- Backup manual
- Compactação ZIP
- Validação de integridade
- Interface de backup/restauração

### Versão 1.1 (Planejado)
- Backup externo configurável
- Integração OneDrive
- Integração Google Drive
- Integração Dropbox
- Criptografia de backup

### Versão 2.0 (Futuro)
- Backup incremental
- Backup em tempo real
- Replicação multi-sítio
- Backup na nuvem dedicado
- Disaster Recovery

---

**Versão do documento:** 1.0
**Última atualização:** 2026-06-17
