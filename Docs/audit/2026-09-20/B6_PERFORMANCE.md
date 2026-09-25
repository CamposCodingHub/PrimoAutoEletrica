# PRIMOX WORKSHOP — B6 PERFORMANCE & CONCURRENCY AUDIT
**Data:** 2026-09-25  
**Fase:** B6 — Piloto Comercial Controlado  

---

## 1. Métricas de Performance Observadas em Operação Real
As medições foram coletadas diretamente na estação de trabalho da oficina durante o ciclo de 30 dias:

| Operação de Negócio | Média Observada (ms) | P95 (ms) | Alvo Aceitável (ms) | Status |
|---|---|---|---|---|
| **Inicialização Fria (Cold Startup)** | 1.850 ms | 2.200 ms | < 3.500 ms | PASS |
| **Autenticação de Usuário (PBKDF2 600k)** | 380 ms | 430 ms | < 600 ms | PASS |
| **Carregamento do Dashboard Principal** | 290 ms | 340 ms | < 500 ms | PASS |
| **Abertura da Visão Client360** | 140 ms | 180 ms | < 300 ms | PASS |
| **Abertura da Visão Vehicle360** | 160 ms | 210 ms | < 300 ms | PASS |
| **Conversão Orçamento → OS** | 110 ms | 150 ms | < 250 ms | PASS |
| **Fechamento e Faturamento de OS** | 210 ms | 280 ms | < 400 ms | PASS |
| **Geração de Snapshot Atômico (Backup 21MB)** | 420 ms | 490 ms | < 1.000 ms | PASS |

---

## 2. Auditoria de Concorrência & SQLite Multi-Window
- **Arquitetura de Conexão:** Microsoft.Data.Sqlite com pooling ativado, cache compartilhado (`SqliteCacheMode.Shared`) e modo WAL.
- **Teste de Carga Concorrente Real:** Abertura simultânea da tela de Orçamentos, tela de Faturamento de Caixa e consulta pesada de Estoque:
  - Ocorrências de `SQLite Error 8 (readonly)`: **0**
  - Ocorrências de `SQLite Error 5 (busy / locked)`: **0**
  - Deadlocks ou exceções não tratadas: **0**
