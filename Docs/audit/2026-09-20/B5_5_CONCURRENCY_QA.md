# PRIMOX Workshop — Fase B5.5: Auditoria de Concorrência e Multi-Janela

**Data:** 2026-09-24  
**Escopo:** Bloqueios Otimistas, Tabelas `RecordLocks`, Múltiplas Instâncias de Janelas

---

## 1. Resultados de Concorrência

- **Controle de Bloqueio Otimista:** Tabela `RecordLocks` e coluna `RowVersion` ativas para evitar sobrescrita cega.
- **Multi-Janelas Operacionais:** Capacidade de manter janelas de Orçamento, OS, Veículo e Estoque abertas em paralelo.
- **Z-Order e Modais:** Modais filhos associados corretamente ao `Owner` sem perda de foco ou travamento em segundo plano.
