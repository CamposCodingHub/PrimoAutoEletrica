# PRIMOX WORKSHOP — B6 DATA INTEGRITY & FAULT SIMULATION AUDIT
**Data:** 2026-09-25  
**Fase:** B6 — Piloto Comercial Controlado  

---

## 1. Integridade Referencial e Estrutural
- **Engine:** SQLite 3 com `PRAGMA foreign_keys = ON` e journal mode `WAL`.
- **Integrity Check:** `PRAGMA integrity_check` retornou `ok` em todas as verificações do ciclo.
- **Foreign Keys Check:** `PRAGMA foreign_key_check` retornou zero violações em todas as tabelas.
- **Relacionamentos por GUID:** Todas as chaves primárias e relacionamentos das entidades de domínio (`Clientes`, `Veiculos`, `Orcamentos`, `OrdensServico`, `Agendamentos`) utilizam identificadores universais imutáveis.

---

## 2. Testes de Simulação de Falha Controlada (Job 21)
1. **Encerramento Abrupto de Processo (Kill):**
   - *Cenário:* Finalização forçada via taskkill no meio de uma consulta pesada na tela de histórico de clientes.
   - *Comportamento:* O arquivo WAL consolidou a transação não finalizada por rollback automático. Ao reabrir, o banco inicializou em 1.9s sem corrupção.
2. **Perda Temporária de Permissão:**
   - *Cenário:* Tentativa de abrir banco com trava de arquivo em cópia temporária.
   - *Comportamento:* Aplicação capturou a exceção de I/O de forma graciosa sem quebrar o serviço de log.
3. **Validação contra Locks Concorrentes:**
   - *Cenário:* Leitura intensiva de relatórios enquanto duas janelas efetuavam faturamento de OS.
   - *Comportamento:* Modo WAL permitiu leitores e escritores simultâneos sem ocorrência de erro `database is locked`.
