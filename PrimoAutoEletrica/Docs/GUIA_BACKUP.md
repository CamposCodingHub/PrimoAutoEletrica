# Guia de Backup - Primo Auto Elétrica

## Por que Fazer Backup?

O backup é a única garantia de que seus dados estarão seguros em caso de:
- Falha de hardware
- Erro humano
- Ataque de malware/ransomware
- Roubo do equipamento
- Desastre natural

## Tipos de Backup

### Backup Automático

Configurado para rodar automaticamente em horários definidos.

**Recomendação:** Diário às 02:00

### Backup Manual

Criado sob demanda pelo usuário.

**Quando usar:**
- Antes de alterações importantes
- Antes de limpeza de dados
- Antes de migrações
- A pedido do administrador

### Backup Externo

Cópia do backup armazenada fora do computador principal.

**Opções:**
- Disco externo (USB, HD externo)
- Unidade de rede
- Serviços de nuvem (OneDrive, Google Drive, Dropbox)

## Configurar Backup Automático

1. Acesse **Configurações** > **Backup**
2. Marque **Backup automático**
3. Defina a frequência:
   - **Diário** (recomendado)
   - Semanal
   - Mensal
4. Defina o horário (ex: 02:00)
5. Configure retenção:
   - Manter últimos 30 backups diários
   - Manter últimos 12 backups semanais
   - Manter últimos 6 backups mensais
6. Clique em **Salvar**

## Configurar Backup Externo

1. Acesse **Configurações** > **Backup**
2. Marque **Backup externo**
3. Clique em **...** para selecionar o destino
4. Escolha uma das opções:
   - Disco externo conectado
   - Pasta de rede compartilhada
   - Pasta sincronizada com nuvem
5. Marque **Sincronizar com nuvem** se aplicável
6. Clique em **Salvar**

## Fazer Backup Manual

1. Acesse **Configurações** > **Backup**
2. Clique em **Criar Backup Manual**
3. Opcionalmente, adicione uma descrição
4. Clique em **OK**
5. Aguarde a conclusão
6. O backup será salvo com timestamp

## Restaurar Backup

### Antes de Restaurar

**Importante:** A restauração substituirá todos os dados atuais.

1. Faça um backup do estado atual
2. Notifique todos os usuários
3. Feche o sistema em todos os computadores
4. Valide o backup a ser restaurado

### Processo de Restauração

1. Acesse **Configurações** > **Backup**
2. Selecione o backup desejado na lista
3. Clique em **Validar** para verificar integridade
4. Clique em **Restaurar**
5. Confirme a operação
6. Aguarde a conclusão
7. Reinicie o sistema

## Validar Backup

1. Acesse **Configurações** > **Backup**
2. Selecione o backup
3. Clique em **Validar**
4. O sistema verificará:
   - Hash SHA256
   - Integridade do banco
   - Estrutura de arquivos
5. Revise o resultado

## Política de Retenção

### Configuração Padrão

- **Backups diários:** 30 dias
- **Backups semanais:** 12 semanas
- **Backups mensais:** 6 meses
- **Backups de atualização:** 10 últimos
- **Backups manuais:** Mantidos até exclusão manual

### Limpeza Automática

O sistema remove automaticamente backups antigos:
- Diariamente às 03:00
- Quando espaço em disco < 10%
- Antes de criar novo backup (se necessário)

### Configurar Retenção

1. Acesse **Configurações** > **Backup**
2. Vá para **Política de Retenção**
3. Ajuste os valores conforme necessário
4. Marque **Limpeza automática**
5. Clique em **Salvar**

## Backup em Nuvem

### OneDrive

1. Instale o aplicativo OneDrive
2. Faça login com sua conta Microsoft
3. Configure a pasta de sincronização
4. No Primo Auto Elétrica, configure o destino externo para a pasta do OneDrive
5. O backup será sincronizado automaticamente

### Google Drive

1. Instale o aplicativo Google Drive
2. Faça login com sua conta Google
3. Configure a pasta de sincronização
4. No Primo Auto Elétrica, configure o destino externo para a pasta do Google Drive
5. O backup será sincronizado automaticamente

### Dropbox

1. Instale o aplicativo Dropbox
2. Faça login com sua conta Dropbox
3. Configure a pasta de sincronização
4. No Primo Auto Elétrica, configure o destino externo para a pasta do Dropbox
5. O backup será sincronizado automaticamente

## Solução de Problemas

### Backup Falha

**Espaço insuficiente:**
- Libere espaço em disco
- Configure retenção mais agressiva
- Use destino externo

**Permissões insuficientes:**
- Execute como administrador
- Verifique permissões da pasta de destino
- Verifique permissões de rede (se aplicável)

**Destino inacessível:**
- Verifique conexão de rede
- Verifique se disco externo está conectado
- Verifique credenciais de rede

### Restauração Falha

**Backup corrompido:**
- Tente backup anterior
- Valide integridade antes de restaurar
- Use backup externo se disponível

**Banco em uso:**
- Feche o sistema em todos os computadores
- Verifique se há processos bloqueando o arquivo
- Reinicie o computador se necessário

**Versão incompatível:**
- Verifique compatibilidade de versão
- Atualize o sistema se necessário
- Entre em contato com suporte

## Boas Práticas

### Frequência

- **Backup diário:** Essencial para uso diário
- **Backup semanal:** Mínimo aceitável
- **Backup externo:** Semanal ou mensal

### Armazenamento

- Mantenha pelo menos 2 cópias do backup
- Uma cópia local, uma externa
- Considere nuvem para terceira cópia

### Validação

- Valide backups regularmente
- Teste restauração mensalmente
- Verifique integridade após cada backup

### Documentação

- Documente procedimentos de backup
- Mantenha registro de backups externos
- Documente procedimentos de restauração

## Checklist Diário

- [ ] Verificar se backup automático foi executado
- [ ] Validar último backup
- [ ] Verificar espaço em disco
- [ ] Verificar backup externo (se configurado)
- [ ] Documentar qualquer problema

## Checklist Semanal

- [ ] Validar integridade de todos os backups
- [ ] Testar restauração de backup recente
- [ ] Verificar política de retenção
- [ ] Limpar backups antigos manualmente (se necessário)
- [ ] Atualizar documentação

## Checklist Mensal

- [ ] Testar restauração completa
- [ ] Verificar backup externo
- [ ] Revisar política de retenção
- [ ] Atualizar configurações se necessário
- [ ] Treinar equipe em procedimentos de backup

## Emergência

### Perda de Dados

1. Não entre em pânico
2. Não tente recuperar dados manualmente
3. Use o backup mais recente
4. Se backup falhar, tente backup anterior
5. Entre em contato com suporte se necessário

### Backup Não Existe

1. Verifique backup externo
2. Verifique nuvem (se configurada)
3. Verificar histórico de backups
4. Entre em contato com suporte
5. Considere recuperação profissional

---

**Versão:** 1.0.0
**Última atualização:** 2026-06-17
