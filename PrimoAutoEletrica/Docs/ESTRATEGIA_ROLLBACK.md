# Estratégia de Rollback - Primo Auto Elétrica

## Visão Geral

Este documento define a estratégia completa de rollback para o sistema Primo Auto Elétrica, garantindo que seja possível reverter alterações em caso de falhas.

## Objetivos

- Permitir rollback seguro de atualizações
- Preservar dados do usuário durante rollback
- Minimizar tempo de inatividade
- Registrar todas as operações de rollback
- Oferecer múltiplas opções de rollback

## Tipos de Rollback

### 1. Rollback de Atualização

**Quando usar:**
- Atualização falhou
- Sistema instável após atualização
- Bugs críticos descobertos após atualização
- Usuário solicita reversão

**Processo:**
1. Detectar versão instalada
2. Identificar backup anterior
3. Validar integridade do backup
4. Fechar aplicação
5. Restaurar arquivos do backup
6. Reverter versão no registro
7. Validar sistema restaurado
8. Registrar log de rollback

### 2. Rollback de Banco de Dados

**Quando usar:**
- Corrupção do banco de dados
- Migração falhou
- Dados inconsistentes
- Erro crítico em operação de banco

**Processo:**
1. Identificar backup do banco mais recente
2. Validar integridade do backup
3. Fechar conexões ativas
4. Restaurar banco de dados
5. Validar integridade pós-restauração
6. Registrar log de rollback

### 3. Rollback de Configuração

**Quando usar:**
- Configuração incorreta
- Configuração corrompida
- Configuração causando problemas

**Processo:**
1. Identificar backup de configuração
2. Restaurar arquivo de configuração
3. Reiniciar aplicação
4. Validar configuração
5. Registrar log de rollback

## Estratégia de Backup

### Quando Criar Backup

**Automático:**
- Antes de qualquer atualização
- Antes de migração de banco
- Antes de operações críticas
- Diariamente (configurável)
- Semanalmente (configurável)

**Manual:**
- Antes de alterações importantes
- Antes de limpeza de dados
- Antes de importação em massa
- A pedido do usuário

### O Que Backup

**Sempre:**
- Banco de dados SQLite
- Arquivos de configuração
- Logs do sistema
- Arquivos de mídia (produtos, clientes)

**Opcional:**
- Logs antigos (configurável)
- Backups antigos (configurável)
- Arquivos temporários

### Localização

```
%LOCALAPPDATA%\PrimoAutoEletrica\Backups\
├── PreUpdate_YYYYMMDD_HHMMSS\
│   ├── primoauto.db
│   ├── database-settings.json
│   ├── business-config.json
│   └── Media\
├── Daily_YYYYMMDD\
└── Manual_YYYYMMDD_HHMMSS\
```

### Retenção

**Política Padrão:**
- Manter últimos 10 backups de atualização
- Manter últimos 7 backups diários
- Manter últimos 4 backups semanais
- Manter backups manuais até exclusão manual

**Configuração:**
```json
{
  "retentionPolicy": {
    "preUpdate": 10,
    "daily": 7,
    "weekly": 4,
    "manual": -1
  }
}
```

## Processo de Rollback Detalhado

### Passo 1: Detecção do Problema

**Sinais de Falha:**
- Aplicação não inicia
- Erros críticos no log
- Dados corrompidos
- Performance degradada
- Funcionalidades quebradas

**Diagnóstico:**
1. Verificar logs de erro
2. Verificar integridade do banco
3. Verificar configurações
4. Comparar com backup anterior

### Passo 2: Seleção do Backup

**Critérios:**
- Data do backup
- Tipo de backup (atualização, diário, manual)
- Integridade do backup
- Tamanho do backup

**Interface:**
- Lista de backups disponíveis
- Filtros por data e tipo
- Preview de conteúdo
- Validação de integridade

### Passo 3: Validação do Backup

**Verificações:**
- Hash SHA256
- Integridade do banco de dados
- Tamanho esperado
- Estrutura de diretórios

**Comando:**
```csharp
public bool ValidateBackup(string backupPath)
{
    // Validar hash
    // Validar integridade do banco
    // Validar estrutura
    // Retornar resultado
}
```

### Passo 4: Preparação para Rollback

**Ações:**
- Fechar aplicação
- Fechar conexões de banco
- Notificar usuários conectados
- Criar backup do estado atual (snapshot)

### Passo 5: Execução do Rollback

**Opções:**

**Rollback Completo:**
- Restaurar banco de dados
- Restaurar configurações
- Restaurar arquivos de mídia
- Reverter versão do aplicativo

**Rollback Parcial:**
- Apenas banco de dados
- Apenas configurações
- Apenas arquivos específicos

### Passo 6: Validação Pós-Rollback

**Verificações:**
- Aplicação inicia corretamente
- Banco de dados acessível
- Configurações válidas
- Dados intactos
- Funcionalidades operacionais

### Passo 7: Registro de Rollback

**Informações:**
- Data e hora
- Versão de/para
- Tipo de rollback
- Backup utilizado
- Motivo
- Responsável
- Resultado

## Interface de Rollback

### Janela Principal

**Componentes:**
- Lista de backups disponíveis
- Filtros (data, tipo, tamanho)
- Preview de conteúdo
- Botão de validar
- Botão de restaurar
- Log de operações

### Estados

1. **Selecionando backup**
2. **Validando backup**
3. **Preparando rollback**
4. **Executando rollback**
5. **Validando pós-rollback**
6. **Concluído com sucesso**
7. **Falha no rollback**

## Logs de Rollback

### Formato

```
[2026-06-20 14:30:00] Iniciando rollback...
[2026-06-20 14:30:05] Backup selecionado: PreUpdate_20260620_103130
[2026-06-20 14:30:10] Validando backup...
[2026-06-20 14:30:15] Backup validado com sucesso
[2026-06-20 14:30:20] Fechando aplicação...
[2026-06-20 14:30:25] Aplicação fechada
[2026-06-20 14:30:30] Restaurando banco de dados...
[2026-06-20 14:31:00] Banco de dados restaurado
[2026-06-20 14:31:05] Restaurando configurações...
[2026-06-20 14:31:10] Configurações restauradas
[2026-06-20 14:31:15] Revertendo versão: 1.1.0 -> 1.0.0
[2026-06-20 14:31:20] Versão revertida
[2026-06-20 14:31:25] Validando sistema...
[2026-06-20 14:31:30] Sistema validado com sucesso
[2026-06-20 14:31:35] Rollback concluído com sucesso
```

### Localização

```
%LOCALAPPDATA%\PrimoAutoEletrica\Logs\rollback-YYYYMMDD-HHMMSS.log
```

## Segurança

### Permissões

- Apenas administradores podem executar rollback
- Requer confirmação antes de rollback
- Log de auditoria obrigatório
- Notificação de rollback para todos os usuários

### Validação

- Hash SHA256 obrigatório
- Integridade do banco obrigatória
- Backup do estado atual antes de rollback
- Validação pós-rollback obrigatória

## Testes

### Testes Obrigatórios

1. **Rollback de atualização bem-sucedido**
2. **Rollback de banco de dados bem-sucedido**
3. **Rollback de configuração bem-sucedido**
4. **Rollback com backup corrompido**
5. **Rollback sem backup disponível**
6. **Rollback com aplicação aberta**
7. **Rollback parcial**
8. **Rollback completo**
9. **Validação de backup**
10. **Registro de logs**

### Testes de Integração

- Rollback após atualização falha
- Rollback após migração falha
- Rollback múltiplo consecutivo
- Rollback com dados grandes
- Rollback com múltiplos usuários

## Solução de Problemas

### Rollback Falha

1. Verificar logs de erro
2. Validar backup novamente
3. Tentar backup anterior
4. Reinstalar versão anterior manualmente
5. Contatar suporte técnico

### Backup Corrompido

1. Tentar backup anterior
2. Verificar integridade do sistema
3. Tentar reparação do banco
4. Reinstalar do zero
5. Restaurar de backup externo

### Sistema Não Inicia Após Rollback

1. Verificar versão do aplicativo
2. Verificar configurações
3. Verificar banco de dados
4. Reinstalar aplicativo
5. Contatar suporte técnico

## Boas Práticas

### Para Desenvolvedores

- Sempre criar backup antes de alterações
- Testar rollback em ambiente de teste
- Validar backups regularmente
- Manter política de retenção clara
- Documentar procedimentos de rollback

### Para Usuários

- Fazer backup manual antes de alterações importantes
- Testar rollback em ambiente seguro
- Manter backup externo
- Documentar procedimentos customizados
- Treinar equipe em rollback

## Roadmap

### Versão 1.0 (Atual)
- Rollback manual via interface
- Validação de backup
- Registro de logs
- Interface de seleção de backup

### Versão 1.1 (Planejado)
- Rollback automático em caso de falha
- Rollback programado
- Rollback remoto
- Notificação de rollback

### Versão 2.0 (Futuro)
- Rollback instantâneo (snapshot)
- Rollback seletivo (por tabela/registro)
- Rollback em tempo real
- Rollback multi-sistema

---

**Versão do documento:** 1.0
**Última atualização:** 2026-06-17
