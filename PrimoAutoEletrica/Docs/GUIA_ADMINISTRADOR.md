# Guia do Administrador - Primo Auto Elétrica

## Visão Geral

Este guia destina-se a administradores do sistema Primo Auto Elétrica, cobrindo configurações avançadas, gerenciamento de usuários, manutenção e solução de problemas.

## Índice

1. [Configurações Iniciais](#configurações-iniciais)
2. [Gerenciamento de Usuários](#gerenciamento-de-usuários)
3. [Permissões e Acessos](#permissões-e-acessos)
4. [Configurações de Banco de Dados](#configurações-de-banco-de-dados)
5. [Configurações de Impressão](#configurações-de-impressão)
6. [Manutenção do Sistema](#manutenção-do-sistema)
7. [Atualizações](#atualizações)
8. [Backup e Restauração](#backup-e-restauração)
9. [Logs e Auditoria](#logs-e-auditoria)
10. [Solução de Problemas](#solução-de-problemas)

## Configurações Iniciais

### Primeiro Acesso

1. Faça login com o usuário administrador padrão
2. Altere imediatamente a senha do administrador
3. Configure os dados da empresa
4. Configure as impressoras
5. Configure o backup automático

### Dados da Empresa

Acesse **Configurações** > **Dados da Empresa**:

- Nome fantasia
- Razão social
- CNPJ
- Inscrição Estadual
- Endereço completo
- Telefone
- E-mail
- Site

### Configurações de Sistema

Acesse **Configurações** > **Sistema**:

- Moeda (BRL)
- Formato de data
- Formato de hora
- Fuso horário
- Idioma

## Gerenciamento de Usuários

### Criar Novo Usuário

1. Acesse **Configurações** > **Usuários**
2. Clique em **Novo Usuário**
3. Preencha os dados:
   - Nome completo
   - E-mail
   - Senha temporária
   - Cargo
4. Configure as permissões
5. Clique em **Salvar**

### Editar Usuário

1. Selecione o usuário
2. Clique em **Editar**
3. Altere os dados necessários
4. Clique em **Salvar**

### Desativar Usuário

1. Selecione o usuário
2. Clique em **Desativar**
3. Confirme a operação

### Redefinir Senha

1. Selecione o usuário
2. Clique em **Redefinir Senha**
3. Digite a nova senha
4. Confirme
5. Clique em **Salvar**

## Permissões e Acessos

### Níveis de Acesso

**Administrador:**
- Acesso total ao sistema
- Gerenciamento de usuários
- Configurações avançadas

**Gerente:**
- Acesso a todos os módulos exceto configurações
- Relatórios gerenciais
- Gerenciamento de estoque

**Operador:**
- Acesso a PDV, estoque (visualização)
- Criação de orçamentos
- Acompanhamento de ordens de serviço

**Técnico:**
- Acesso a ordens de serviço
- Atualização de status
- Visualização de histórico

### Configurar Permissões

1. Acesse **Configurações** > **Permissões**
2. Selecione o usuário
3. Marque os módulos permitidos
4. Configure ações específicas (criar, editar, excluir)
5. Clique em **Salvar**

## Configurações de Banco de Dados

### SQLite (Padrão)

O sistema usa SQLite por padrão, ideal para uso local.

**Localização:**
```
%LOCALAPPDATA%\PrimoAutoEletrica\Data\primoauto.db
```

**Configuração:**
```json
{
  "provider": "sqlite",
  "connectionString": "Data Source=%LOCALAPPDATA%\\PrimoAutoEletrica\\Data\\primoauto.db"
}
```

### SQL Server (Multi-usuário)

Para ambientes multi-usuário, configure SQL Server.

**Pré-requisitos:**
- SQL Server 2017 ou superior
- Usuário com permissões de CREATE DATABASE

**Configuração:**
```json
{
  "provider": "sqlserver",
  "connectionString": "Server=localhost;Database=PrimoAutoEletrica;Trusted_Connection=True;"
}
```

**Migração:**
1. Faça backup completo do banco SQLite
2. Configure a conexão SQL Server
3. Use o utilitário de migração
4. Valide os dados migrados

## Configurações de Impressão

### Configurar Impressoras

1. Acesse **Configurações** > **Impressão**
2. Selecione o tipo de documento:
   - Orçamentos
   - Ordens de serviço
   - Notas fiscais
   - Relatórios
3. Selecione a impressora padrão
4. Configure o formato de papel
5. Teste a impressão

### Layout de Impressão

O sistema suporta layouts personalizados. Consulte a documentação técnica para detalhes.

## Manutenção do Sistema

### Limpeza de Logs

1. Acesse **Configurações** > **Manutenção**
2. Clique em **Limpar Logs**
3. Selecione o período
4. Confirme

### Compactação de Banco de Dados

1. Acesse **Configurações** > **Manutenção**
2. Clique em **Compactar Banco**
3. Aguarde a conclusão

### Validação de Integridade

1. Acesse **Configurações** > **Manutenção**
2. Clique em **Validar Integridade**
3. Revise o relatório
4. Corrija problemas se necessário

## Atualizações

### Verificar Atualizações

1. Acesse **Configurações** > **Atualizações**
2. Clique em **Verificar Atualizações**
3. Se houver atualização disponível, siga as instruções

### Atualização Automática

Configure atualização automática:
1. Acesse **Configurações** > **Atualizações**
2. Marque **Verificar automaticamente**
3. Defina a frequência
4. Configure download automático (opcional)

### Rollback

Se uma atualização causar problemas:
1. Acesse **Configurações** > **Atualizações**
2. Clique em **Histórico de Atualizações**
3. Selecione a versão anterior
4. Clique em **Restaurar**

## Backup e Restauração

### Configurar Backup Automático

1. Acesse **Configurações** > **Backup**
2. Marque **Backup automático**
3. Defina a frequência (diário recomendado)
4. Defina o horário
5. Configure destino externo (opcional)
6. Clique em **Salvar**

### Backup Manual

1. Acesse **Configurações** > **Backup**
2. Clique em **Criar Backup Manual**
3. Aguarde a conclusão
4. O backup será salvo no diretório configurado

### Restaurar Backup

1. Acesse **Configurações** > **Backup**
2. Selecione o backup desejado
3. Clique em **Restaurar**
4. Confirme a operação
5. Aguarde a conclusão

### Backup Externo

Configure backup externo para maior segurança:
1. Acesse **Configurações** > **Backup**
2. Marque **Backup externo**
3. Selecione o destino (disco externo, rede, nuvem)
4. Configure sincronização automática
5. Clique em **Salvar**

## Logs e Auditoria

### Visualizar Logs

1. Acesse **Configurações** > **Logs**
2. Selecione o tipo de log:
   - Sistema
   - Erros
   - Auditoria
3. Defina o período
4. Clique em **Filtrar**

### Auditoria de Ações

O sistema registra automaticamente:
- Login/logout
- Criação/edição/exclusão de registros
- Alterações de configurações
- Operações críticas

### Exportar Logs

1. Selecione os logs desejados
2. Clique em **Exportar**
3. Escolha o formato (CSV, TXT)
4. Salve o arquivo

## Solução de Problemas

### Sistema Não Inicia

**Possíveis causas:**
- Banco de dados corrompido
- Permissões insuficientes
- Arquivos de sistema faltando

**Solução:**
1. Verifique os logs de erro
2. Restaure o backup mais recente
3. Reinstale o sistema se necessário

### Erro de Conexão com Banco

**Possíveis causas:**
- Arquivo de banco bloqueado
- Permissões insuficientes
- Caminho incorreto

**Solução:**
1. Verifique se há outras instâncias rodando
2. Verifique permissões da pasta de dados
3. Valide o caminho de conexão

### Impressão Não Funciona

**Possíveis causas:**
- Impressora não configurada
- Driver ausente
- Papel acabou

**Solução:**
1. Configure a impressora em Configurações
2. Instale o driver da impressora
3. Abasteça papel

### Backup Falha

**Possíveis causas:**
- Espaço em disco insuficiente
- Permissões insuficientes
- Destino inacessível

**Solução:**
1. Libere espaço em disco
2. Verifique permissões
3. Verifique conexão com destino externo

### Performance Lenta

**Possíveis causas:**
- Banco de dados grande
- Muitos registros
- Hardware insuficiente

**Solução:**
1. Compacte o banco de dados
2. Limpe logs antigos
3. Considere upgrade de hardware

## Boas Práticas

### Segurança

- Altere senhas regularmente
- Use senhas fortes
- Limite permissões de usuários
- Mantenha backup externo
- Monitore logs regularmente

### Manutenção

- Faça backup diariamente
- Valide integridade semanalmente
- Limpe logs mensalmente
- Atualize o sistema regularmente
- Monitore espaço em disco

### Documentação

- Mantenha documentação atualizada
- Registre configurações customizadas
- Documente procedimentos especiais
- Mantenha registro de problemas

## Suporte Técnico

### Quando Contatar

- Erros não documentados
- Problemas de performance
- Dúvidas sobre configurações
- Necessidade de treinamento

### Informações Necessárias

- Versão do sistema
- Mensagem de erro completa
- Logs relevantes
- Passos para reproduzir o problema

### Contato

- E-mail: suporte@primoautoeletrica.com
- Telefone: (XX) XXXX-XXXX
- Horário: Seg-Sex 08:00-18:00

---

**Versão:** 1.0.0
**Última atualização:** 2026-06-17
