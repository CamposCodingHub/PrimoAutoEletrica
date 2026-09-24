# PRIMOX Workshop — Fase B5.3
## Relatório de Performance e Métricas Físicas da Migração Money

**Data:** 2026-09-24  
**Ambiente:** Windows x64 (Homologação Isolada B5.3)  
**Status:** PASS  

---

### 1. Métricas de Tempo de Execução

| Etapa | Duração (s) | Descrição |
|---|---|---|
| Cópia e Isolamento de Origem | 0.059 s | Cópia binária do banco operacional para ambiente de ensaio |
| Execução da Migração (7 Estágios) | 4.618 s | Rebuild atômico de shadow tables com conversão AwayFromZero |
| Validação de Integridade e FKs | 0.082 s | Checagem de integridade física e relacional completa |
| Prova de Rollback Físico e Lógico | 0.268 s | Restauração de backup e verificação byte-a-byte via SHA-256 |
| **Tempo Total do Ensaio** | **5.378 s** | **Ensaio completo com geração de 6 relatórios e evidências** |

---

### 2. Tamanho dos Bancos de Dados

| Arquivo | Tamanho (Bytes) | Tamanho (MB) | Observação |
|---|---|---|---|
| `primoauto_money_b53_legacy.db` | 20,250,624 bytes | 19.31 MB | Banco pré-migração (LegacyReal) |
| `primoauto_money_b53_cents.db` | 20,275,200 bytes | 19.34 MB | Banco pós-migração (CentsV1 INTEGER) |
| `primoauto_money_b53_pre_migration.db` | 20,250,624 bytes | 19.31 MB | Cópia física de backup |
| `primoauto_money_b53_rollback_test.db` | 20,250,624 bytes | 19.31 MB | Cópia pós-teste de rollback |

**Análise de Tamanho:**  
A variação de tamanho após a migração (+24,576 bytes) decorre da compactação natural das páginas B-Tree do SQLite durante o processo de shadow table rebuild (equivalente a um VACUUM controlado) e substituição de registros IEEE 754 float de 8 bytes por inteiros variáveis SQLite.
