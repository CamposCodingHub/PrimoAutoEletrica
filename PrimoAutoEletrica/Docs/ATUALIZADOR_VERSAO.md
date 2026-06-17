# Atualizador de Versão - Primo Auto Elétrica

## Visão Geral

Este documento descreve a arquitetura e funcionamento do sistema de atualização automática do Primo Auto Elétrica.

## Objetivos

- Permitir atualizações automáticas do sistema
- Garantir integridade dos pacotes de atualização
- Preservar dados do usuário durante atualizações
- Permitir rollback em caso de falha
- Oferecer atualização local (sem dependência de internet)
- Suportar atualização remota (opcional)

## Arquitetura

### Componentes

1. **UpdateService.cs** - Serviço principal de atualização
2. **UpdateManifest.cs** - Modelo de manifesto de atualização
3. **UpdatePackageInfo.cs** - Informações do pacote de atualização
4. **AtualizacaoWindow.xaml** - Interface de atualização
5. **manifest.json** - Arquivo de manifesto local

### Fluxo de Atualização

```
1. Verificar versão atual
2. Consultar manifesto (local ou remoto)
3. Comparar versões
4. Baixar pacote (se remoto)
5. Validar hash SHA256
6. Criar backup automático
7. Verificar se sistema está fechado
8. Aplicar atualização
9. Validar atualização
10. Registrar log
```

## Modos de Atualização

### Modo Local (Padrão)

O sistema verifica um arquivo `manifest.json` local para atualizações disponíveis.

**Vantagens:**
- Não requer conexão com internet
- Controle total sobre quando atualizar
- Ideal para ambientes offline
- Mais seguro (sem download externo)

**Implementação:**
```json
{
  "version": "1.1.0",
  "releaseDate": "2026-06-20",
  "packagePath": "Updates/PrimoAutoEletrica-1.1.0.zip",
  "sha256": "abc123...",
  "size": 15728640,
  "mandatory": false,
  "changes": [
    "Correção de bugs",
    "Melhoria de performance"
  ]
}
```

### Modo Remoto (Opcional)

O sistema consulta um servidor remoto para atualizações disponíveis.

**Vantagens:**
- Atualizações automáticas
- Notificação de novas versões
- Download automático

**Implementação Futura:**
- Endpoint HTTPS seguro
- Autenticação via API key
- Validação de certificado SSL

## Segurança

### Validação de Hash

Todo pacote de atualização deve ter seu hash SHA256 validado antes da instalação.

```csharp
public bool ValidatePackage(string packagePath, string expectedHash)
{
    var actualHash = ComputeSHA256(packagePath);
    return actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
}
```

### Assinatura Digital (Futuro)

- Assinar pacotes com chave privada
- Verificar assinatura com chave pública
- Prevenir adulteração de pacotes

## Backup Automático

### Quando Criar Backup

Antes de qualquer atualização:
- Backup do banco de dados
- Backup de configurações
- Backup de logs
- Backup de arquivos de mídia

### Localização

```
%LOCALAPPDATA%\PrimoAutoEletrica\Backups\PreUpdate_YYYYMMDD_HHMMSS\
```

### Retenção

- Manter últimos 10 backups
- Remover backups antigos automaticamente
- Permitir limpeza manual

## Verificação de Sistema Fechado

### Detectar Instâncias Ativas

```csharp
public bool IsApplicationRunning()
{
    var processes = Process.GetProcessesByName("PrimoAutoEletrica");
    return processes.Length > 0;
}
```

### Bloquear Atualização

Se o sistema estiver aberto:
- Exibir mensagem ao usuário
- Oferecer opção de fechar automaticamente
- Impedir atualização até sistema fechado

## Rollback

### Quando Executar Rollback

- Falha na validação de hash
- Falha na aplicação da atualização
- Erro crítico após atualização
- Solicitação explícita do usuário

### Processo de Rollback

1. Detectar falha
2. Restaurar backup mais recente
3. Reverter versão no registro
4. Registrar log de rollback
5. Notificar usuário

### Interface de Rollback

Janela dedicada com:
- Lista de backups disponíveis
- Data e hora de cada backup
- Opção de restaurar
- Validação de integridade

## Manifesto de Atualização

### Estrutura

```json
{
  "currentVersion": "1.0.0",
  "latestVersion": "1.1.0",
  "updateAvailable": true,
  "updateMandatory": false,
  "releaseDate": "2026-06-20T00:00:00Z",
  "package": {
    "url": "https://releases.primoautoeletrica.com/v1.1.0.zip",
    "sha256": "abc123def456...",
    "size": 15728640,
    "minVersion": "1.0.0"
  },
  "changes": [
    {
      "type": "feature",
      "description": "Nova funcionalidade de relatórios"
    },
    {
      "type": "fix",
      "description": "Correção de bug no módulo de estoque"
    }
  ],
  "compatibility": {
    "minWindowsVersion": "6.1",
    "requiredDiskSpace": 500000000,
    "requiredRAM": 2147483648
  }
}
```

### Campos

- **currentVersion**: Versão atual instalada
- **latestVersion**: Versão mais recente disponível
- **updateAvailable**: Se há atualização disponível
- **updateMandatory**: Se atualização é obrigatória
- **releaseDate**: Data de lançamento da atualização
- **package**: Informações do pacote de atualização
- **changes**: Lista de mudanças na nova versão
- **compatibility**: Requisitos de compatibilidade

## Interface de Atualização

### Janela Principal

Componentes:
- Indicador de progresso
- Log de operações
- Botão de cancelar
- Informações da versão
- Lista de mudanças

### Estados

1. **Verificando atualizações**
2. **Baixando pacote** (se remoto)
3. **Validando pacote**
4. **Criando backup**
5. **Aplicando atualização**
6. **Validando atualização**
7. **Concluído com sucesso**
8. **Falha na atualização**

## Logs

### Registro de Eventos

Todas as operações de atualização são registradas:

```
[2026-06-20 10:30:00] Verificando atualizações...
[2026-06-20 10:30:05] Atualização disponível: 1.1.0
[2026-06-20 10:30:10] Baixando pacote...
[2026-06-20 10:31:00] Pacote baixado: 15.0 MB
[2026-06-20 10:31:05] Validando hash SHA256...
[2026-06-20 10:31:10] Hash validado com sucesso
[2026-06-20 10:31:15] Criando backup...
[2026-06-20 10:31:30] Backup criado: PreUpdate_20260620_103130
[2026-06-20 10:31:35] Aplicando atualização...
[2026-06-20 10:32:00] Atualização aplicada com sucesso
[2026-06-20 10:32:05] Validando atualização...
[2026-06-20 10:32:10] Atualização validada com sucesso
[2026-06-20 10:32:15] Atualização concluída
```

### Localização

```
%LOCALAPPDATA%\PrimoAutoEletrica\Logs\update-YYYYMMDD-HHMMSS.log
```

## Configurações

### Opções do Usuário

- Verificar atualizações automaticamente (sim/não)
- Frequência de verificação (diária/semanal/mensal)
- Baixar automaticamente (sim/não)
- Instalar automaticamente (sim/não)
- Notificar sobre atualizações (sim/não)

### Arquivo de Configuração

```json
{
  "autoCheck": true,
  "checkInterval": "daily",
  "autoDownload": false,
  "autoInstall": false,
  "notifyUpdates": true,
  "lastCheck": "2026-06-20T10:30:00Z"
}
```

## Testes

### Testes Obrigatórios

1. **Atualização local bem-sucedida**
2. **Atualização remota bem-sucedida**
3. **Validação de hash**
4. **Backup automático**
5. **Rollback após falha**
6. **Detecção de sistema aberto**
7. **Atualização obrigatória**
8. **Atualização opcional**
9. **Preservação de dados**
10. **Registro de logs**

### Testes de Integração

- Atualização de versão anterior
- Atualização para versão futura
- Atualização com conexão intermitente
- Atualização com espaço insuficiente
- Atualização com permissões insuficientes

## Solução de Problemas

### Atualização Falha

1. Verificar logs em `%LOCALAPPDATA%\PrimoAutoEletrica\Logs\`
2. Validar hash do pacote
3. Verificar espaço em disco
4. Verificar permissões
5. Executar rollback

### Backup Falha

1. Verificar espaço em disco
2. Verificar permissões de escrita
3. Verificar integridade do banco de dados
4. Tentar backup manual

### Rollback Falha

1. Verificar se backup existe
2. Validar integridade do backup
3. Verificar permissões
4. Reinstalar versão anterior manualmente

## Boas Práticas

### Para Desenvolvedores

- Sempre testar atualização em ambiente limpo
- Validar hash SHA256 antes de release
- Incluir changelog detalhado
- Testar rollback
- Manter compatibilidade com versões anteriores

### Para Usuários

- Fazer backup manual antes de atualizações importantes
- Fechar o sistema antes de atualizar
- Verificar espaço em disco
- Ler changelog antes de atualizar
- Manter backup em local seguro

## Roadmap

### Versão 1.0 (Atual)
- Atualização local via manifest.json
- Validação de hash SHA256
- Backup automático
- Rollback básico
- Interface de atualização

### Versão 1.1 (Planejado)
- Atualização remota via HTTPS
- Assinatura digital de pacotes
- Atualização incremental (delta)
- Agendamento de atualizações
- Notificações push

### Versão 2.0 (Futuro)
- Atualização em segundo plano
- Atualização silenciosa
- Rollback automático
- A/B testing de atualizações
- Telemetria de atualizações

---

**Versão do documento:** 1.0
**Última atualização:** 2026-06-17
