# Guia de Testes - Primo Auto Elétrica

## Visão Geral

Este guia descreve os testes automatizados e manuais que devem ser executados para garantir a qualidade e estabilidade do sistema Primo Auto Elétrica.

## Testes Automatizados

### Build Test

Valida se o projeto compila sem erros.

```bash
dotnet build
```

**Critério de Sucesso:** Build concluído sem erros ou warnings

### Unit Test

Valida a lógica de negócio através de testes unitários.

```bash
dotnet test
```

**Critério de Sucesso:** Todos os testes passam

### UI Smoke Test

Valida se a interface principal carrega corretamente.

```bash
dotnet run -- --smoke-test
```

**Validações:**
- Janela de login exibida
- Campos de login funcionais
- Autenticação bem-sucedida
- Dashboard carregado
- Todos os módulos acessíveis

### Workflow Test

Valida os fluxos principais do sistema.

```bash
dotnet run -- --workflow-test
```

**Validações:**
- Cadastro de cliente
- Cadastro de veículo
- Criação de orçamento
- Aprovação de orçamento
- Criação de ordem de serviço
- Finalização de serviço
- Venda no PDV
- Fechamento de caixa

### Theme Test

Valida a consistência visual dos temas.

```bash
dotnet run -- --theme-test
```

**Validações:**
- Tema claro carregado corretamente
- Tema escuro carregado corretamente
- Cores consistentes
- Contraste adequado
- Elementos visíveis

### Permission Test

Valida o sistema de permissões.

```bash
dotnet run -- --permission-test
```

**Validações:**
- Usuário sem permissão não acessa módulo
- Usuário com permissão acessa módulo
- Ações restritas bloqueadas
- Ações permitidas funcionais

### Database Test

Valida a integridade e operações do banco de dados.

```bash
dotnet run -- --database-test
```

**Validações:**
- Conexão com banco estabelecida
- Operações CRUD funcionais
- Transações funcionais
- Integridade referencial mantida
- Performance aceitável

### Clean Install Simulation

Simula uma instalação limpa do sistema.

```bash
dotnet run -- --clean-install-simulation
```

**Validações:**
- Banco criado corretamente
- Configurações padrão aplicadas
- Usuário administrador criado
- Sistema funcional

## Testes Manuais

### Teste de Instalação

1. Execute o instalador em máquina limpa
2. Siga o assistente de instalação
3. Verifique se todos os arquivos foram copiados
4. Execute o sistema
5. Valide funcionalidades básicas

### Teste de Atualização

1. Instale versão anterior
2. Execute o instalador da nova versão
3. Verifique se dados foram preservados
4. Valide funcionalidades após atualização
5. Teste rollback se necessário

### Teste de Backup/Restauração

1. Crie backup manual
2. Valide integridade do backup
3. Faça alterações no sistema
4. Restaure o backup
5. Verifique se dados foram restaurados corretamente

### Teste de Impressão

1. Configure impressora
2. Imprima orçamento
3. Imprima ordem de serviço
4. Imprima relatório
5. Verifique layout e conteúdo

### Teste de Performance

1. Cadastre 100 clientes
2. Cadastre 50 veículos
3. Crie 30 orçamentos
4. Crie 20 ordens de serviço
5. Gere relatórios
6. Meça tempo de resposta

### Teste de Multi-usuário (SQL Server)

1. Configure SQL Server
2. Abra sistema em 2 computadores
3. Crie ordem de serviço no computador 1
4. Atualize status no computador 2
5. Verifique sincronização

## Checklist de Release

### Antes de Release

- [ ] Build sem erros
- [ ] Testes unitários passando
- [ ] UI Smoke Test aprovado
- [ ] Workflow Test aprovado
- [ ] Theme Test aprovado
- [ ] Permission Test aprovado
- [ ] Database Test aprovado
- [ ] Clean Install Simulation aprovado
- [ ] Documentação atualizada
- [ ] CHANGELOG atualizado
- [ ] Backup criado
- [ ] Hash SHA256 gerado
- [ ] Release notes preparadas

### Após Release

- [ ] Instalação limpa testada
- [ ] Atualização testada
- [ ] Backup/restauração testado
- [ ] Impressão testada
- [ ] Performance validada
- [ ] Logs monitorados
- [ ] Feedback coletado

## Relatório de Qualidade

O sistema gera automaticamente um relatório de qualidade executando todos os testes automatizados.

### Gerar Relatório

```bash
dotnet run -- --quality-report
```

O relatório será salvo em `LogsValidacao/QualityReport_YYYYMMDD_HHMMSS.md`

### Conteúdo do Relatório

- Resumo dos testes
- Detalhes de cada teste
- Tempo de execução
- Status (passou/falhou)
- Recomendações

## Solução de Problemas

### Teste Falha

**Build falha:**
- Verifique erros de compilação
- Verifique dependências
- Limpe e reconstrua

**Teste unitário falha:**
- Verifique lógica do teste
- Verifique dependências do teste
- Revise código testado

**UI Smoke Test falha:**
- Verifique se aplicação inicia
- Verifique logs de erro
- Verifique configurações

**Workflow Test falha:**
- Verifique fluxo específico
- Verifique validações
- Verifique banco de dados

### Performance Insuficiente

**Identificar gargalo:**
- Use profiler
- Verifique consultas de banco
- Verifique uso de memória
- Otimize código crítico

**Soluções:**
- Adicionar índices no banco
- Otimizar consultas
- Implementar cache
- Revisar algoritmos

## Boas Práticas

### Antes de Desenvolver

- Escreva testes primeiro (TDD)
- Defina critérios de aceitação
- Documente comportamento esperado

### Durante Desenvolvimento

- Execute testes frequentemente
- Mantenha testes atualizados
- Refatore com segurança (testes protegem)

### Antes de Commit

- Execute todos os testes
- Valide build
- Revise código
- Documentar mudanças

### Antes de Release

- Execute testes completos
- Teste em ambiente similar ao produção
- Valide performance
- Documentar release

## Ferramentas

### dotnet CLI

```bash
dotnet build          # Build
dotnet test           # Testes unitários
dotnet clean          # Limpeza
dotnet restore        # Restauração
```

### PowerShell Scripts

```powershell
.\run-tests.ps1       # Executar todos os testes
.\quality-report.ps1  # Gerar relatório de qualidade
```

## Contato

Para dúvidas sobre testes ou problemas não documentados:
- Consulte o Manual Técnico
- Entre em contato com suporte técnico

---

**Versão:** 1.0.0
**Última atualização:** 2026-06-17
