# Processo de Release - Primo Auto Elétrica

## Visão Geral

Este documento define o processo de versionamento e release do sistema Primo Auto Elétrica, garantindo estabilidade e rastreabilidade das versões entregues.

## Estrutura de Versionamento

### Diretórios

- **PrimoAutoEletrica_STABLE**: Versão estável aprovada para produção. Nunca recebe alterações diretas.
- **PrimoAutoEletrica_DEV**: Versão de desenvolvimento onde todas as melhorias são implementadas.
- **Releases**: Pacotes de release gerados (ZIP, instalador, etc.).
- **Backups**: Backups de versões anteriores para rollback.
- **Docs**: Documentação do projeto.
- **LogsValidacao**: Logs de validação de builds e testes.

### Regra de Ouro

**A versão STABLE nunca deve receber alteração direta.**

Toda melhoria deve ser feita na DEV. Só promover para STABLE quando passar por todas as validações obrigatórias.

## Ciclo de Release

### 1. Desenvolvimento (DEV)

Todas as melhorias, correções e novos recursos são implementados em `PrimoAutoEletrica_DEV`.

### 2. Validação Obrigatória

Antes de promover para STABLE, a versão DEV deve passar por:

```bash
# Limpeza
dotnet clean

# Restauração de dependências
dotnet restore

# Build
dotnet build

# Testes unitários
dotnet test

# Testes automatizados
dotnet run -- --smoke-test
dotnet run -- --workflow-test
dotnet run -- --theme-test
dotnet run -- --permission-test
dotnet run -- --database-test

# Simulação de instalação limpa
dotnet run -- --clean-install-simulation
```

### 3. Promoção para STABLE

Se todas as validações passarem:

1. Criar backup da versão STABLE atual em `Backups/`
2. Copiar todo o conteúdo de `PrimoAutoEletrica_DEV` para `PrimoAutoEletrica_STABLE`
3. Registrar a versão em `Docs/CHANGELOG.md`
4. Gerar pacote de release em `Releases/`
5. Atualizar número de versão

### 4. Geração de Release

#### Nomeação de Pacotes

Formato: `PrimoAutoEletrica_v{MAJOR}.{MINOR}.{PATCH}_{YYYYMMDD}.zip`

Exemplo: `PrimoAutoEletrica_v1.0.0_20260617.zip`

#### Componentes do Release

- Executável compilado
- Banco de dados inicial (vazio)
- Arquivos de configuração
- Documentação
- Manual do usuário
- Termos de uso e privacidade
- Script de instalação (quando aplicável)

#### Hash SHA256

Gerar hash SHA256 do pacote para integridade:

```bash
certutil -hashfile PrimoAutoEletrica_v1.0.0_20260617.zip SHA256
```

Registrar o hash no `Docs/RELEASE_NOTES.md`.

## Registro de Mudanças

### CHANGELOG.md

Manter registro de todas as mudanças por versão:

```markdown
## [1.0.0] - 2026-06-17

### Adicionado
- Novo recurso X
- Melhoria Y

### Alterado
- Refatoração do módulo Z

### Corrigido
- Bug relacionado a W

### Removido
- Legado V
```

### RELEASE_NOTES.md

Documentar detalhes específicos do release:

- Versão
- Data
- Hash SHA256
- Lista de arquivos
- Instruções de instalação
- Requisitos de sistema
- Notas importantes

## Rollback

### Quando Fazer Rollback

- Bug crítico descoberto após release
- Problema de compatibilidade
- Falha de segurança
- Problema de performance severo

### Processo de Rollback

1. Identificar a versão anterior estável em `Backups/`
2. Restaurar backup para `PrimoAutoEletrica_STABLE`
3. Registrar rollback em `Docs/ROLLBACK_LOG.md`
4. Notificar usuários (se aplicável)
5. Gerar novo release com versão incrementada (PATCH)

### Exemplo de Rollback

```
ROLLBACK - 2026-06-17
De: v1.1.0
Para: v1.0.0
Motivo: Bug crítico no módulo de financeiro
Responsável: [Nome]
```

## Boas Práticas

### Antes de Commit

- Executar testes locais
- Verificar compilação
- Revisar código
- Atualizar documentação

### Antes de Release

- Backup completo do banco de dados
- Testar em ambiente de homologação
- Validar instalação limpa
- Revisar CHANGELOG
- Gerar documentação atualizada

### Após Release

- Monitorar logs de erro
- Coletar feedback de usuários
- Documentar problemas encontrados
- Planejar correções necessárias

## Versionamento Semântico

### MAJOR (X.0.0)

- Mudanças incompatíveis na API
- Alterações significativas na arquitetura
- Remoção de funcionalidades

### MINOR (0.X.0)

- Adição de funcionalidades compatíveis
- Melhorias significativas
- Novos módulos

### PATCH (0.0.X)

- Correções de bugs
- Pequenas melhorias
- Atualizações de documentação

## Validação de Qualidade

### Checklist de Release

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

## Contingência

### Se Build Falhar

1. Revisar logs de erro
2. Verificar dependências
3. Reverter última alteração
4. Tentar build novamente
5. Documentar problema

### Se Testes Falharem

1. Identificar teste que falhou
2. Reproduzir problema localmente
3. Corrigir ou documentar comportamento esperado
4. Reexecutar testes
5. Não prosseguir com release

### Se Instalação Falhar

1. Verificar pré-requisitos
2. Testar em ambiente limpo
3. Revisar script de instalação
4. Corrigir problema
5. Revalidar instalação

## Suporte

### Contato

Para dúvidas sobre o processo de release:
- Documentação técnica: `Docs/ManualTecnico/`
- Logs de validação: `LogsValidacao/`
- Histórico de releases: `Docs/RELEASE_NOTES.md`

## Aprovação

Todo release deve ser aprovado por:
- Desenvolvedor responsável
- Validador de qualidade
- (Opcional) Gerente de projeto

---

**Última atualização:** 2026-06-17
**Versão do documento:** 1.0
