# PRIMOX Workshop — Fase B5.2
## Relatório de Teste de Atualização e Preservação de Dados (Update Test)

**Data da Execução:** 2026-09-24  
**Ambiente:** Windows x64 (Ambiente Isolado de Teste C08 / E2E)  
**Status do Teste:** PASS  

---

### 1. Metodologia do Teste de Update
O teste de atualização em camadas executou a sobreposição da aplicação (`Versão A -> Versão B por cima`) para validar se a substituição dos binários preserva integralmente os dados transacionais do usuário.

#### Premissas Arquiteturais:
- **Separação Rígida:** Os binários do produto residem na pasta da aplicação (`{app}` ou `%LOCALAPPDATA%\PrimoAutoEletrica\App`), enquanto os dados do usuário residem fora da pasta binária (`%LOCALAPPDATA%\PrimoAutoEletrica\`).
- **Imutabilidade Operacional:** O instalador NÃO embute nem copia arquivos `.db` de produção ou homologação.

---

### 2. Resultados das Etapas de Atualização

| Etapa | Verificação Realizada | Resultado | Evidência |
|---|---|---|---|
| 1 | Carga de Dados Transacionais Prévios | PASS | Inserção controlada de Clientes, Veículos, Ordens de Serviço e Checklist |
| 2 | Cálculo de Hash e Versão de Schema Pré-Update | PASS | Hash gravado (`220786A8...`), `PRAGMA user_version = 0` |
| 3 | Execução de Backup Automático Pré-Update | PASS | Arquivo timestamped gerado (`primoauto_operacional_YYYYMMDD_HHMMSS.db`) |
| 4 | Execução do Instalador sobre Instalação Existente | PASS | `ExitCode = 0`, binários substituídos com sucesso |
| 5 | Encerramento Forçado de Processos em Execução | PASS | Rotina Inno `TryClosePrimoxProcesses` encerra instâncias sem travar o instalador |
| 6 | Integridade dos Dados Pós-Update | PASS | Registros encontrados intactos (100% de persistência) |
| 7 | Validação de Schema Pós-Update | PASS | `PRAGMA user_version = 0` mantido sem desvios |
| 8 | Teste de Integridade Pós-Update | PASS | `PRAGMA integrity_check = ok`, `PRAGMA foreign_key_check = 0` |
| 9 | Simulação de Falha Controlada e Rollback | PASS | Rollback de transação com restauração imediata do estado original |
| 10 | Reexecução do UI Smoke pós-update | PASS | Filtro comercial executado com sucesso e zero regressões |

---

### 3. Matriz de Ciclos Repetidos (Hardening Cycles)
Foram executados 3 ciclos completos e consecutivos de instalação, injeção de dados, desinstalação e reinstalação:
- **Ciclo 1:** PASS (Instalação limpa, inicialização PID 22560, teste de dados, desinstalação com preservação de dados).
- **Ciclo 2:** PASS (Instalação por cima, inicialização PID 24644, verificação de dados existentes, desinstalação).
- **Ciclo 3:** PASS (Instalação por cima, inicialização PID 11764, verificação de integridade, desinstalação).
- **Desinstalação com App Aberto:** PASS (Processo terminado e desinstalador concluído com sucesso).
- **Reinstalação Final:** PASS (PID 32252, leitura de dados preservados e faturamento íntegro).

---

### 4. Conclusão
O mecanismo de atualização do PRIMOX Workshop garante **zero perda de dados** em operações de upgrade, preservando banco de dados, backups, configurações e logs.
