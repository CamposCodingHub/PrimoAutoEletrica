# PRIMOX Workshop — Fase B5.3
## Baseline Oficial: Money Migration Rehearsal + Final Proof

**Data:** 2026-09-24 19:30:00-03:00  
**Branch:** `audit/product-discovery-2026-09`  
**HEAD:** `e258b1f`  
**Branch main:** `29b19b1` (Intacta / 100% protegida)  
**Executor:** Antigravity Autonomous Engine  

---

### 1. Status das Fases Anteriores
- **Fase B5.0 (Arquitetura Comercial):** PASS
- **Fase B5.1 (Checklist Multiponto + Pós-Venda):** PASS
- **Fase B5.2 (Instalador Comercial Inno Setup + Update/Recovery):** PASS
- **Suíte de Testes (xUnit):** 432/432 PASS (0 falhas, 0 ignorados)
- **UI Smoke Test Real (Release):** 200/200 APROVADO
- **Desktop Startups:** 3/3 PASS via atalho oficial, zero SQLite Error 8
- **Light / Dark Mode:** PASS
- **Resoluções:** 1280x720 PASS, 1366x768 PASS, 1920x1080 PASS

---

### 2. Integridade dos Bancos de Dados
- **Banco de Produção Oficial:**
  - Caminho: `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db`
  - SHA-256: `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B`
  - Status ReadOnly: `True` (Estritamente protegido contra escrita)
  - Regra de Ouro: Proibida qualquer operação DDL/DML ou migração neste banco.
- **Banco Operacional:**
  - Caminho: `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto_operacional.db`
  - PRAGMA integrity_check: `ok`
  - PRAGMA foreign_key_check: `0` inconsistências
  - PRAGMA user_version: `0` (LegacyReal)

---

### 3. Escopo e Objetivos da Fase B5.3
A Fase B5.3 tem por finalidade única e exclusiva executar o **ensaio completo e exaustivo da migração física** de valores monetários:
`SQLite REAL` → `INTEGER CENTS` → `MoneyIO` → `decimal` → `UI / Services / Reports`.

**Garantias:**
1. A migração ocorrerá em ambiente isolado (`TestResults/Homologacao_B5_3/`), utilizando cópia do banco operacional.
2. O banco de produção NÃO será migrado nem tocado nesta fase.
3. Classificação dos 67 campos monetários e preservação integral dos campos não-monetários (quantidades, percentuais, coordenadas, telemetria).
4. Prova row-by-row, prova de agregados (diferença = 0), prova de rollback físico e lógico.
5. Emissão do relatório de prontidão futura (`READY_FOR_FUTURE_WINDOW`).
