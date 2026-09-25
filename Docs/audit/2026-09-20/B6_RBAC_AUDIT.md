# PRIMOX WORKSHOP — B6 RBAC & PERMISSIONS AUDIT
**Data:** 2026-09-25  
**Fase:** B6 — Piloto Comercial Controlado  

---

## 1. Matriz de Perfis e Política de Segurança
O PRIMOX Workshop implementa uma política estrita **fail-closed**: na ausência de permissão explícita, o acesso é sumariamente negado (`DENY`), sem concessão por fallback.

---

## 2. Auditoria dos 10 Perfis Operacionais

| Perfil | Acesso a Clientes / Veículos | Orçamentos / OS | Módulo Financeiro | Estoque / Peças | Diagnóstico / Checklist | Configurações / Usuários | Política Fail-Closed |
|---|---|---|---|---|---|---|---|
| **1. Administrador** | Total (CRUD) | Total (CRUD) | Total (CRUD + DRE) | Total (CRUD) | Total (CRUD) | Total (CRUD) | PASS |
| **2. Gerente** | Total (CRUD) | Total (CRUD) | Operacional + Relat. | Total (CRUD) | Consulta / Laudo | Somente Leitura | PASS |
| **3. Atendente / Recepção** | Criar / Editar / Ler | Criar / Aprovar OS | Apenas Caixa Balcão | Apenas Consulta | Consulta / Checklist | DENY | PASS |
| **4. Técnico Mecânico** | Consulta | Editar OS atribuída | DENY | Requisitar Peça | Preencher Checklist | DENY | PASS |
| **5. Eletricista 12V/24V** | Consulta | Editar OS atribuída | DENY | Requisitar Peça | Total D01 a D06 | DENY | PASS |
| **6. Auxiliar Técnico** | Consulta | Visualizar OS | DENY | DENY | Checklist supervisionado | DENY | PASS |
| **7. Operador de Caixa** | Consulta Básica | Visualizar OS | Abertura/Fech. Caixa | DENY | DENY | DENY | PASS |
| **8. Estoquista** | DENY | Visualizar Peças OS | DENY | Total (Entrada/Saída) | DENY | DENY | PASS |
| **9. Auditor / Fiscal** | Somente Leitura | Somente Leitura | Somente Leitura | Somente Leitura | Somente Leitura | DENY | PASS |
| **10. Trainee / Estagiário** | Consulta | Visualizar OS | DENY | Consulta | Checklist básico | DENY | PASS |

---

## 3. Teste de Tentativas de Acesso Proibido
- Foram simuladas tentativas forçadas de abrir a tela de configurações do sistema e relatórios de DRE logado como Atendente e Mecânico:
  - O sistema bloqueou a navegação exibindo aviso de permissão insuficiente e registrou o evento de segurança no log de auditoria.
  - Zero bypass ou elevação indevida de privilégio.
