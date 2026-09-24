# PRIMOX Workshop — B5.4: Homologação da Matriz de Acesso RBAC

**Data:** 2026-09-24 20:45  
**Status:** **PASS**

---

## 1. Avaliação dos 10 Perfis Oficiais

| Perfil | Acesso aos Módulos Operacionais | Acesso ao Financeiro | Configurações do Sistema | Status |
| :--- | :---: | :---: | :---: | :---: |
| **Administrador** | Total | Total | Total | **PASS** |
| **Gerente** | Total | Total | Consulta | **PASS** |
| **Consultor Tecnico** | Orçamentos, Clientes, Veículos | Consulta Básica | Bloqueado | **PASS** |
| **Mecanico** | OS, Diagnóstico, Checklist | Bloqueado | Bloqueado | **PASS** |
| **Eletricista** | OS, Autoelétrica D01-D06 | Bloqueado | Bloqueado | **PASS** |
| **Auxiliar Oficina** | Consulta OS | Bloqueado | Bloqueado | **PASS** |
| **Estoquista** | Estoque, Catálogo, NF-e | Bloqueado | Bloqueado | **PASS** |
| **Operador Caixa** | PDV, Caixa | Movimentação PDV | Bloqueado | **PASS** |
| **Financeiro** | Bloqueado (Operação técnica) | Total | Bloqueado | **PASS** |
| **Auditor Fiscal** | Relatórios, Fiscal | Consulta | Bloqueado | **PASS** |

---

## 2. Comportamento Fail-Closed

- Quando uma permissão não pode ser verificada ou é ausente na sessão, o sistema **bloqueia o acesso imediatamente** (fail-closed), nunca liberando indevidamente a rota ou ação.
