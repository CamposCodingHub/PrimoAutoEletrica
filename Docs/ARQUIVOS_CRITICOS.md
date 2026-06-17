# ARQUIVOS CRÍTICOS - PRIMO AUTO ELÉTRICA

**Data:** 2026-06-10  
**Projeto:** PrimoAutoEletrica  
**Versão:** 1.0

---

## 1. VISÃO GERAL

Este documento lista os arquivos críticos do sistema PrimoAutoEletrica que requerem atenção especial, seja por tamanho, complexidade ou responsabilidade crítica.

---

## 2. ARQUIVOS POR TAMAHO (> 500 linhas)

### 2.1 App.xaml.cs
**Linhas:** ~582  
**Responsabilidade:** Inicialização da aplicação, tratamento de exceções globais  
**Risco:** Alto - falha aqui impede inicialização  
**Ação:** Manter, revisar periodicamente

### 2.2 UiSmokeTestService.cs
**Linhas:** > 500 (estimado)  
**Responsabilidade:** Teste de smoke de UI  
**Risco:** Médio - usado apenas em testes  
**Ação:** Considerar dividir em partial classes

### 2.3 OperationalWorkflowTestService.cs
**Linhas:** > 500 (estimado)  
**Responsabilidade:** Teste de workflow  
**Risco:** Médio - usado apenas em testes  
**Ação:** Considerar dividir em partial classes

### 2.4 DatabaseService.cs
**Linhas:** > 500 (estimado)  
**Responsabilidade:** Acesso ao banco de dados  
**Risco:** Alto - falha aqui impede acesso a dados  
**Ação:** Revisar, considerar partial classes

### 2.5 AgendamentosViewModel.cs
**Linhas:** > 500 (estimado)  
**Responsabilidade:** ViewModel de agendamentos  
**Risco:** Médio - falha afeta apenas módulo de agendamentos  
**Ação:** Revisar, simplificar se possível

### 2.6 RelatoriosViewModel.cs
**Linhas:** > 500 (estimado)  
**Responsabilidade:** ViewModel de relatórios  
**Risco:** Médio - falha afeta apenas módulo de relatórios  
**Ação:** Revisar, simplificar se possível

### 2.7 FinanceiroViewModel.cs
**Linhas:** > 500 (estimado)  
**Responsabilidade:** ViewModel financeiro  
**Risco:** Médio - falha afeta apenas módulo financeiro  
**Ação:** Revisar, simplificar se possível

---

## 3. ARQUIVOS POR RESPONSABILIDADE CRÍTICA

### 3.1 App.xaml.cs
**Responsabilidade:** Inicialização da aplicação  
**Por que é crítico:**
- Inicializa todos os serviços
- Configura tratamento de exceções
- Define o fluxo de inicialização

**Riscos:**
- Falha de inicialização
- Stack Overflow
- Recursão infinita

**Mitigações:**
- Proteção contra recursão (_isHandlingGlobalException)
- Fallback de logging
- Try-catch em pontos críticos

---

### 3.2 DatabaseService.cs
**Responsabilidade:** Acesso ao banco de dados  
**Por que é crítico:**
- Único ponto de acesso ao banco
- Gerencia conexões
- Aplica migrations

**Riscos:**
- Falha de conexão
- Corrupção de dados
- Migration falhando

**Mitigações:**
- Lock para inicialização
- Backup antes de migrations
- Tratamento de erros

---

### 3.3 AuditLogService.cs
**Responsabilidade:** Auditoria de ações  
**Por que é crítico:**
- Registro de ações sensíveis
- Compliance
- Rastreabilidade

**Riscos:**
- Recursão com DatabaseService
- Falha ao registrar auditoria
- Perda de trilha de auditoria

**Mitigações:**
- Try-catch ao registrar
- Fallback de logging
- Não bloquear operações críticas

---

### 3.4 LoggerService.cs
**Responsabilidade:** Logging  
**Por que é crítico:**
- Registro de erros
- Diagnóstico de problemas
- Tracing

**Riscos:**
- Falha ao logar
- Arquivo de log corrompido
- Espaço em disco

**Mitigações:**
- Fallback de arquivo
- Rotação de logs
- Não bloquear operações críticas

---

### 3.5 DatabaseBackupService.cs
**Responsabilidade:** Backup do banco  
**Por que é crítico:**
- Proteção contra perda de dados
- Recuperação de desastres
- Rollback de migrations

**Riscos:**
- Falha ao criar backup
- Backup corrompido
- Espaço insuficiente

**Mitigações:**
- Verificação de integridade
- Backup em múltiplos locais
- Compressão de backups

---

## 4. ARQUIVOS POR COMPLEXIDADE

### 4.1 NavigationService.cs
**Complexidade:** Alta  
**Responsabilidade:** Navegação entre módulos  
**Desafios:**
- Verificação de permissões
- Cache de controles
- Histórico de navegação

**Ação:** Revisar periodicamente

---

### 4.2 PermissionService.cs
**Complexidade:** Alta  
**Responsabilidade:** Verificação de permissões  
**Desafios:**
- Matriz de permissões
- Bloqueio dinâmico
- Herança de permissões

**Ação:** Revisar periodicamente

---

### 4.3 ThemeService.cs
**Complexidade:** Média  
**Responsabilidade:** Gestão de temas  
**Desafios:**
- Troca dinâmica de tema
- Persistência de tema
- ResourceDictionary

**Ação:** Revisar se necessário

---

## 5. ARQUIVOS POR MANUTENÇÃO

### 5.1 Arquivos que requerem revisão periódica
- App.xaml.cs
- DatabaseService.cs
- AuditLogService.cs
- LoggerService.cs
- NavigationService.cs
- PermissionService.cs

### 5.2 Arquivos que requerem refatoração futura
- UiSmokeTestService.cs (dividir em partial classes)
- OperationalWorkflowTestService.cs (dividir em partial classes)
- DatabaseService.cs (considerar partial classes)
- AgendamentosViewModel.cs (simplificar)
- RelatoriosViewModel.cs (simplificar)
- FinanceiroViewModel.cs (simplificar)

---

## 6. ARQUIVOS POR RISCO DE REGRESSÃO

### 6.1 Alto risco de regressão
- App.xaml.cs (inicialização)
- DatabaseService.cs (acesso a dados)
- AuditLogService.cs (auditoria)

### 6.2 Médio risco de regressão
- NavigationService.cs (navegação)
- PermissionService.cs (permissões)
- ThemeService.cs (temas)

### 6.3 Baixo risco de regressão
- ViewModels (isolados por módulo)
- Repositories (isolados por entidade)
- UserControls (isolados por funcionalidade)

---

## 7. RECOMENDAÇÕES

### 7.1 Antes de alterar arquivos críticos
1. Criar backup do banco
2. Criar branch no Git
3. Executar testes completos
4. Testar manualmente o fluxo afetado
5. Commit com mensagem descritiva

### 7.2 Durante alteração
1. Alterar pequenas partes por vez
2. Testar após cada alteração
3. Não alterar múltiplos arquivos críticos ao mesmo tempo
4. Manter logs detalhados

### 7.3 Após alteração
1. Executar testes completos
2. Testar manualmente o fluxo afetado
3. Verificar logs de erro
4. Documentar a alteração

---

## 8. MONITORAMENTO

### 8.1 Logs a monitorar
- Logs/CriticalErrors.log
- Logs/DatabaseService.log
- Logs/AuditLogService.log
- Logs/LoggerService.log

### 8.2 Métricas a monitorar
- Tempo de inicialização
- Tempo de resposta do banco
- Tempo de resposta de navegação
- Número de erros críticos

---

**Última atualização:** 2026-06-10
