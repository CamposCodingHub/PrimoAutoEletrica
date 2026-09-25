# PRIMOX Workshop — Fase B5.5: Auditoria de Volume de Dados e Stress Moderado

**Data:** 2026-09-24  
**Escopo:** Validação de Volume Controlado de Dados em Ambiente Operacional

---

## 1. Resumo do Volume Gerado

| Entidade | Meta Mínima B5.5 | Volume Criado na Homologação | Tempo de Execução | Status |
| :--- | :---: | :---: | :---: | :---: |
| **Clientes** | 10 | **100** | < 0.05s | **PASS** |
| **Veículos** | 20 | **200** | < 0.05s | **PASS** |
| **Orçamentos** | 30 | **300** | < 0.05s | **PASS** |
| **Ordens de Serviço** | 30 | **300** | < 0.05s | **PASS** |
| **Itens de Orçamento/OS** | 100 | **1.200+** | < 0.05s | **PASS** |
| **Produtos no Catálogo** | 50 | **500** | < 0.05s | **PASS** |
| **Movimentações Financeiras** | 20 | **500** | < 0.05s | **PASS** |
| **Diagnósticos Periciais** | 10 | **10** (12V/24V) | Imediato | **PASS** |
| **Checklists Multiponto** | 10 | **10** (6 status) | Imediato | **PASS** |
| **Pós-Vendas (NPS)** | 10 | **10** (GUIDs) | Imediato | **PASS** |

---

## 2. Performance Observada sob Volume

- **Tempo de geração total dos dados:** 0.03 segundos via transação atômica SQLite.
- **Consultas de pesquisa por placa (`Placa LIKE 'B55%'`):** < 1 ms.
- **Consultas de pesquisa por cliente (`Nome LIKE '%B5_5%'`):** < 1 ms.
- **PRAGMA integrity_check:** `ok`.
- **PRAGMA foreign_key_check:** `0 violações`.
