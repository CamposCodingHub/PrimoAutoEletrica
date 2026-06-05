# Guia de Contingência - Primo Auto Eletrica

## Objetivo
Definir procedimentos de contingência para situações de emergência no Primo Auto Eletrica.

## 1. Cenários de Falha

### 1.1 Queda de Servidor

**Sintomas:**
- Servidor não responde a ping
- Servidor não acessível pela rede
- Servidor desligado
- Servidor reiniciando constantemente

**Impacto:**
- Sistema inoperável em todas as estações
- Perda de acesso ao banco de dados
- Perda de acesso a backups
- Paralização total das operações

**Prioridade:** CRÍTICA

### 1.2 Queda do SQL Server

**Sintomas:**
- SQL Server não responde
- SQL Server parado
- Erro de conexão SQL Server
- Timeout de conexão

**Impacto:**
- Sistema inoperável em todas as estações
- Perda de acesso ao banco de dados
- Perda de dados não salvos
- Paralização total das operações

**Prioridade:** CRÍTICA

### 1.3 Falha de Backup

**Sintomas:**
- Backup automático não é criado
- Erro ao criar backup
- Backup corrompido
- Espaço insuficiente

**Impacto:**
- Perda de dados em caso de falha
- Impossibilidade de restauração
- Risco de perda de dados

**Prioridade:** ALTA

### 1.4 Falha de Rede

**Sintomas:**
- Estações não acessam servidor
- Latência excessiva
- Perda de pacotes
- Conexão intermitente

**Impacto:**
- Sistema inoperável em estações
- Perda de dados não salvos
- Operações parciais
- Inconsistência de dados

**Prioridade:** ALTA

### 1.5 Corrupção do Banco de Dados

**Sintomas:**
- Erro ao abrir banco
- Erro de integridade
- Dados inconsistentes
- Registros duplicados

**Impacto:**
- Sistema inoperável
- Perda de dados
- Inconsistência de dados
- Necessidade de restauração

**Prioridade:** CRÍTICA

### 1.6 Falha de Disco

**Sintomas:**
- Disco cheio
- Disco com erros
- Disco falhando
- Disco inacessível

**Impacto:**
- Sistema inoperável
- Perda de dados
- Impossibilidade de backup
- Necessidade de substituição

**Prioridade:** CRÍTICA

## 2. Procedimentos de Contingência

### 2.1 Queda de Servidor

**Passo 1: Verificar Energia**
- Verificar se servidor está ligado
- Verificar cabo de energia
- Verificar nobreak (se aplicável)
- Verificar energia elétrica

**Passo 2: Verificar Conexão de Rede**
- Verificar cabo de rede
- Verificar switch
- Verificar roteador
- Testar ping do servidor

**Passo 3: Reiniciar Servidor**
- Reiniciar servidor
- Aguardar inicialização
- Verificar se Windows inicia
- Verificar conexão de rede

**Passo 4: Verificar SQL Server**
- Verificar se serviço SQL Server está rodando
- Iniciar serviço SQL Server se necessário
- Verificar logs do SQL Server
- Testar conexão com SQL Server

**Passo 5: Verificar Backups**
- Verificar último backup
- Verificar integridade do backup
- Verificar espaço em disco
- Verificar pasta de rede

**Passo 6: Comunicar Equipe**
- Informar equipe sobre falha
- Estimar tempo de recuperação
- Definir procedimentos alternativos
- Documentar incidente

**Tempo Estimado de Recuperação:** 15-30 minutos

**Registro:**
- Data: ___/___/___
- Hora: ___:___
- Causa: ___________________
- Ação tomada: ___________________
- Tempo de recuperação: ___ minutos
- Resultado: ___________________

### 2.2 Queda do SQL Server

**Passo 1: Reiniciar Serviço SQL Server**
- Abrir Services.msc
- Localizar SQL Server (SQLEXPRESS)
- Reiniciar serviço
- Aguardar inicialização

**Passo 2: Verificar Logs do SQL Server**
- Abrir SQL Server Management Studio
- Verificar logs do SQL Server
- Identificar causa da falha
- Documentar erro

**Passo 3: Verificar Espaço em Disco**
- Verificar espaço em disco do servidor
- Verificar espaço em disco do banco
- Liberar espaço se necessário
- Limpar logs antigos

**Passo 4: Verificar Conexões Ativas**
- Verificar conexões ativas
- Matar conexões travadas se necessário
- Verificar locks
- Liberar recursos

**Passo 5: Reiniciar Servidor se Necessário**
- Reiniciar servidor
- Aguardar inicialização
- Verificar SQL Server
- Testar conexão

**Passo 6: Comunicar Equipe**
- Informar equipe sobre falha
- Estimar tempo de recuperação
- Definir procedimentos alternativos
- Documentar incidente

**Tempo Estimado de Recuperação:** 10-20 minutos

**Registro:**
- Data: ___/___/___
- Hora: ___:___
- Causa: ___________________
- Ação tomada: ___________________
- Tempo de recuperação: ___ minutos
- Resultado: ___________________

### 2.3 Falha de Backup

**Passo 1: Verificar Espaço em Disco**
- Verificar espaço em disco do servidor
- Verificar espaço em disco da pasta de backup
- Liberar espaço se necessário
- Limpar backups antigos

**Passo 2: Verificar Permissões**
- Verificar permissões da pasta de backup
- Verificar permissões do usuário do SQL Server
- Corrigir permissões se necessário
- Testar acesso

**Passo 3: Verificar Pasta de Destino**
- Verificar se pasta de destino existe
- Verificar se pasta de destino está acessível
- Criar pasta se necessário
- Testar acesso

**Passo 4: Verificar Integridade do Banco**
- Verificar integridade do banco
- Executar DBCC CHECKDB
- Corrigir erros se necessário
- Documentar erros

**Passo 5: Criar Backup Manual**
- Criar backup manual
- Verificar integridade do backup
- Verificar tamanho do backup
- Verificar localização do backup

**Passo 6: Comunicar Equipe**
- Informar equipe sobre falha
- Estimar tempo de recuperação
- Definir procedimentos alternativos
- Documentar incidente

**Tempo Estimado de Recuperação:** 15-30 minutos

**Registro:**
- Data: ___/___/___
- Hora: ___:___
- Causa: ___________________
- Ação tomada: ___________________
- Tempo de recuperação: ___ minutos
- Resultado: ___________________

### 2.4 Falha de Rede

**Passo 1: Verificar Conexão de Rede**
- Verificar cabo de rede
- Verificar switch
- Verificar roteador
- Testar ping do servidor

**Passo 2: Reiniciar Equipamentos**
- Reiniciar switch
- Reiniciar roteador
- Aguardar inicialização
- Testar conexão

**Passo 3: Verificar Configurações de Rede**
- Verificar IP do servidor
- Verificar IP das estações
- Verificar gateway
- Verificar DNS

**Passo 4: Verificar Firewall**
- Verificar firewall do servidor
- Verificar firewall das estações
- Verificar regras de firewall
- Ajustar regras se necessário

**Passo 5: Verificar Cabos**
- Verificar cabos de rede
- Substituir cabos danificados
- Verificar conectores
- Testar conexão

**Passo 6: Comunicar Equipe**
- Informar equipe sobre falha
- Estimar tempo de recuperação
- Definir procedimentos alternativos
- Documentar incidente

**Tempo Estimado de Recuperação:** 10-30 minutos

**Registro:**
- Data: ___/___/___
- Hora: ___:___
- Causa: ___________________
- Ação tomada: ___________________
- Tempo de recuperação: ___ minutos
- Resultado: ___________________

### 2.5 Corrupção do Banco de Dados

**Passo 1: Parar Sistema em Todas as Estações**
- Comunicar equipe
- Parar sistema em todas as estações
- Verificar se todas as estações pararam
- Forçar logout se necessário

**Passo 2: Identificar Último Backup Válido**
- Verificar backups disponíveis
- Verificar integridade dos backups
- Identificar último backup válido
- Documentar backup selecionado

**Passo 3: Restaurar Backup**
- Restaurar último backup válido
- Verificar integridade após restauração
- Verificar dados restaurados
- Documentar restauração

**Passo 4: Verificar Integridade**
- Executar DBCC CHECKDB
- Verificar erros de integridade
- Corrigir erros se necessário
- Documentar erros

**Passo 5: Reiniciar Sistema**
- Reiniciar servidor
- Iniciar SQL Server
- Iniciar sistema nas estações
- Testar funcionamento

**Passo 6: Verificar Dados**
- Verificar dados críticos
- Verificar movimentações do dia
- Verificar inconsistências
- Documentar perdas

**Passo 7: Comunicar Equipe**
- Informar equipe sobre falha
- Documentar perdas de dados
- Definir procedimentos de recuperação
- Documentar incidente

**Tempo Estimado de Recuperação:** 30-60 minutos

**Registro:**
- Data: ___/___/___
- Hora: ___:___
- Causa: ___________________
- Ação tomada: ___________________
- Tempo de recuperação: ___ minutos
- Dados perdidos: ___________________
- Resultado: ___________________

### 2.6 Falha de Disco

**Passo 1: Verificar Disco**
- Verificar espaço em disco
- Verificar erros de disco
- Verificar saúde do disco
- Documentar problemas

**Passo 2: Liberar Espaço se Necessário**
- Limpar backups antigos
- Limpar logs antigos
- Limpar arquivos temporários
- Verificar espaço liberado

**Passo 3: Substituir Disco se Necessário**
- Comunicar equipe
- Planejar substituição
- Backup de dados
- Substituir disco

**Passo 4: Restaurar Dados**
- Restaurar backup
- Verificar integridade
- Verificar dados
- Documentar restauração

**Passo 5: Reiniciar Sistema**
- Reiniciar servidor
- Iniciar SQL Server
- Iniciar sistema nas estações
- Testar funcionamento

**Passo 6: Comunicar Equipe**
- Informar equipe sobre falha
- Documentar substituição
- Documentar restauração
- Documentar incidente

**Tempo Estimado de Recuperação:** 2-4 horas

**Registro:**
- Data: ___/___/___
- Hora: ___:___
- Causa: ___________________
- Ação tomada: ___________________
- Tempo de recuperação: ___ minutos
- Resultado: ___________________

## 3. Procedimentos de Recuperação

### 3.1 Restauração de Backup

**Passo 1: Identificar Backup**
- Verificar backups disponíveis
- Verificar integridade dos backups
- Identificar backup a ser restaurado
- Documentar backup selecionado

**Passo 2: Criar Backup de Segurança**
- Criar backup do banco atual
- Verificar integridade do backup
- Documentar backup de segurança
- Guardar backup de segurança

**Passo 3: Restaurar Backup**
- Parar sistema em todas as estações
- Restaurar backup selecionado
- Verificar integridade após restauração
- Documentar restauração

**Passo 4: Verificar Dados**
- Verificar dados críticos
- Verificar movimentações
- Verificar inconsistências
- Documentar perdas

**Passo 5: Reiniciar Sistema**
- Iniciar sistema nas estações
- Testar funcionamento
- Verificar usuários online
- Documentar teste

**Tempo Estimado de Recuperação:** 30-60 minutos

**Registro:**
- Data: ___/___/___
- Hora: ___:___
- Backup restaurado: ___________________
- Dados perdidos: ___________________
- Tempo de recuperação: ___ minutos
- Resultado: ___________________

### 3.2 Recuperação de Dados Parciais

**Passo 1: Identificar Dados Perdidos**
- Verificar dados críticos
- Identificar dados perdidos
- Documentar dados perdidos
- Priorizar recuperação

**Passo 2: Verificar Logs**
- Verificar logs de auditoria
- Verificar logs de sistema
- Identificar movimentações
- Documentar movimentações

**Passo 3: Recuperar Dados**
- Recuperar dados de logs
- Recuperar dados de backups
- Recuperar dados de outras fontes
- Documentar recuperação

**Passo 4: Verificar Integridade**
- Verificar integridade dos dados
- Verificar consistência dos dados
- Corrigir inconsistências
- Documentar correções

**Passo 5: Comunicar Equipe**
- Informar equipe sobre recuperação
- Documentar dados recuperados
- Documentar dados perdidos
- Documentar incidente

**Tempo Estimado de Recuperação:** 1-2 horas

**Registro:**
- Data: ___/___/___
- Hora: ___:___
- Dados recuperados: ___________________
- Dados perdidos: ___________________
- Tempo de recuperação: ___ minutos
- Resultado: ___________________

## 4. Procedimentos de Comunicação

### 4.1 Comunicação Interna

**Quem Comunicar:**
- Administrador do sistema
- TI responsável
- Gerente da oficina
- Equipe afetada

**O Que Comunicar:**
- Tipo de falha
- Impacto nas operações
- Tempo estimado de recuperação
- Procedimentos alternativos

**Como Comunicar:**
- E-mail
- Telefone
- WhatsApp
- Reunião presencial

**Registro:**
- Data: ___/___/___
- Hora: ___:___
- Tipo de falha: ___________________
- Pessoas comunicadas: ___________________
- Tempo estimado: ___ minutos
- Procedimentos alternativos: ___________________

### 4.2 Comunicação Externa

**Quem Comunicar:**
- Suporte externo
- Desenvolvedor do sistema
- Fornecedor de hardware
- Fornecedor de software

**O Que Comunicar:**
- Tipo de falha
- Impacto nas operações
- Tempo estimado de recuperação
- Informações técnicas

**Como Comunicar:**
- E-mail
- Telefone
- Sistema de suporte

**Registro:**
- Data: ___/___/___
- Hora: ___:___
- Tipo de falha: ___________________
- Pessoas comunicadas: ___________________
- Tempo estimado: ___ minutos
- Informações técnicas: ___________________

## 5. Documentação de Incidentes

### 5.1 Registro de Incidentes

**Informações Necessárias:**
- Data e hora do incidente
- Tipo de falha
- Causa da falha
- Impacto nas operações
- Ações tomadas
- Tempo de recuperação
- Dados perdidos
- Pessoas envolvidas
- Lições aprendidas

**Template de Registro:**

```
INCIDENTE #___

Data: ___/___/___
Hora: ___:___

Tipo de Falha:
[ ] Queda de Servidor
[ ] Queda do SQL Server
[ ] Falha de Backup
[ ] Falha de Rede
[ ] Corrupção do Banco de Dados
[ ] Falha de Disco
[ ] Outro: ___________________

Causa: ___________________

Impacto:
[ ] Sistema inoperável
[ ] Paralização parcial
[ ] Perda de dados
[ ] Perda de operações
[ ] Outro: ___________________

Ações Tomadas:
1. ___________________
2. ___________________
3. ___________________
4. ___________________
5. ___________________

Tempo de Recuperação: ___ minutos

Dados Perdidos: ___________________

Pessoas Envolvidas: ___________________

Lições Aprendidas: ___________________

Ações Preventivas: ___________________

Responsável: ___________________

Status: [ ] Aberto [ ] Em Andamento [ ] Concluído
```

### 5.2 Análise de Incidentes

**Frequência:** Mensal

**Itens a Analisar:**
- Incidentes ocorridos
- Causas dos incidentes
- Tempo de recuperação
- Impacto nas operações
- Lições aprendidas
- Ações preventivas

**Registro:**
- Data: ___/___/___
- Participantes: ___________________
- Incidentes analisados: ___
- Causas identificadas: ___________________
- Ações preventivas definidas: ___________________

## 6. Prevenção

### 6.1 Manutenção Preventiva

**Diária:**
- Verificar logs de erros
- Verificar backup automático
- Verificar espaço em disco
- Verificar usuários online

**Semanal:**
- Verificar integridade do banco
- Limpar logs antigos
- Limpar backups antigos
- Verificar performance

**Mensal:**
- Atualizar sistema
- Revisar configurações
- Testar restauração
- Revisar permissões

**Trimestral:**
- Revisar hardware
- Revisar rede
- Revisar segurança
- Revisar documentação

**Anual:**
- Revisar plano de contingência
- Revisar procedimentos
- Treinar equipe
- Atualizar documentação

### 6.2 Monitoramento

**Monitoramento Contínuo:**
- Usuários online
- Conexões ativas
- Espaço em disco
- Performance do sistema

**Alertas:**
- Espaço em disco < 10%
- Backup falhou
- SQL Server parado
- Erros críticos
- Performance degradada

**Registro:**
- Data: ___/___/___
- Hora: ___:___
- Alerta: ___________________
- Ação tomada: ___________________
- Resultado: ___________________

### 6.3 Testes de Recuperação

**Frequência:** Mensal

**Testes a Realizar:**
- Teste de restauração de backup
- Teste de falha de rede
- Teste de falha de SQL Server
- Teste de falha de servidor

**Registro:**
- Data: ___/___/___
- Teste realizado: ___________________
- Resultado: [ ] Sucesso [ ] Falha
- Tempo de recuperação: ___ minutos
- Observações: ___________________

## 7. Treinamento

### 7.1 Treinamento de Contingência

**Frequência:** Trimestral

**Conteúdo:**
- Procedimentos de contingência
- Procedimentos de recuperação
- Procedimentos de comunicação
- Registro de incidentes

**Participantes:**
- Administrador do sistema
- TI responsável
- Gerente da oficina
- Equipe chave

**Registro:**
- Data: ___/___/___
- Participantes: ___________________
- Conteúdo: ___________________
- Avaliação: [ ] Aprovado [ ] Reprovado
- Observações: ___________________

### 7.1 Simulações de Falha

**Frequência:** Semestral

**Simulações a Realizar:**
- Simulação de queda de servidor
- Simulação de queda de SQL Server
- Simulação de falha de rede
- Simulação de corrupção de banco

**Registro:**
- Data: ___/___/___
- Simulação: ___________________
- Resultado: [ ] Sucesso [ ] Falha
- Tempo de recuperação: ___ minutos
- Observações: ___________________

## 8. Suporte

### 8.1 Suporte Interno

**Contato:**
- Administrador do sistema
- TI responsável
- Gerente da oficina

**Horário:**
- Segunda a Sexta: 08:00 - 18:00
- Sábado: 08:00 - 12:00

**Fora do Horário:**
- Telefone de emergência
- E-mail de emergência

### 8.2 Suporte Externo

**Contato:**
- Desenvolvedor do sistema
- Suporte SQL Server (Microsoft)
- Fornecedor de hardware
- Fornecedor de software

**Horário:**
- Conforme contrato de suporte

**Fora do Horário:**
- Telefone de emergência
- Sistema de suporte 24/7

## 9. Revisão

### 9.1 Revisão do Plano de Contingência

**Frequência:** Anual

**Itens a Revisar:**
- Procedimentos de contingência
- Procedimentos de recuperação
- Procedimentos de comunicação
- Documentação de incidentes
- Ações preventivas
- Plano de treinamento

**Registro:**
- Data: ___/___/___
- Participantes: ___________________
- Itens revisados: ___________________
- Ações definidas: ___________________
- Próxima revisão: ___/___/___

### 9.2 Atualização do Plano de Contingência

**Quando Atualizar:**
- Após incidente crítico
- Após mudança no sistema
- Após mudança no ambiente
- Após mudança na equipe
- Anualmente

**Registro:**
- Data: ___/___/___
- Motivo: ___________________
- Alterações: ___________________
- Responsável: ___________________
- Aprovado por: ___________________
