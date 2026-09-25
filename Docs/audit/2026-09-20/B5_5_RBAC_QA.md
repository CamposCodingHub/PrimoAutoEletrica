# PRIMOX Workshop — Fase B5.5: Auditoria de Segurança e RBAC

**Data:** 2026-09-24  
**Escopo:** Validação dos 10 Perfis de Acesso e Política Fail-Closed

---

## 1. Matriz de Perfis Homologados

| Perfil | Acesso Permitido (ALLOW) | Acesso Negado (DENY) | Comportamento Fail-Closed |
| :--- | :--- | :--- | :---: |
| **Administrador** | Acesso irrestrito (todas as 82 permissões) | N/A | **PASS** |
| **Gerente** | Operação, Aprovações, Relatórios, Descontos | Configurações Críticas de Sistema | **PASS** |
| **Recepcao** | Entrada de Clientes, Veículos, Agendamentos | Financeiro Avançado, Exclusões | **PASS** |
| **Tecnico** | Execução de OS, Diagnósticos, Laudos | Operações de Caixa, Financeiro | **PASS** |
| **Eletricista** | Roteiros D01-D06, Medições 12V/24V | Gestão de Usuários, Fiscal | **PASS** |
| **Mecanico** | Manutenção Física, OS, Checklists | Alteração de Preços de Peças | **PASS** |
| **Caixa** | Abertura/Fechamento Caixa, Recebimentos | Configuração de Banco, Perfis | **PASS** |
| **Estoquista** | Entrada de Peças, Ajustes de Saldo | Fechamento Financeiro | **PASS** |
| **Auxiliar** | Visualização Básica de Serviços | Ações Críticas e Destrutivas | **PASS** |
| **Auditor** | Consulta a Logs, Auditoria, Rastreamento | Modificação de Dados | **PASS** |

- **Resultado:** Zero vazamento de privilégios. Fail-Closed ativado em todos os componentes.
