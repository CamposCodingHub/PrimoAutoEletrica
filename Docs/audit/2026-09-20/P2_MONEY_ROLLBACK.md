# PRIMOX Workshop — ESTRATÉGIA FORMAL DE ROLLBACK (MONEY MIGRATION)

> **Documento:** P2_MONEY_ROLLBACK.md  
> **Data:** 23/09/2026  
> **Status:** DESENHO CONCLUÍDO (Nenhuma execução)  
> **Objetivo:** Garantir reversibilidade determinística, atômica e validável caso qualquer etapa da futura migração falhe.  

---

## 1. PRINCÍPIO DA REVERSIBILIDADE DETERMINÍSTICA

Dizer apenas "temos backup" não é uma estratégia de rollback para um sistema comercial com dados financeiros.  
A estratégia de rollback do PRIMOX é baseada em **3 níveis de contenção**:

```
[Nível 1: Atomicidade de Transação (In-Flight Rollback)]
  └── Se o script DDL/DML falhar em qualquer tabela → ROLLBACK imediato da transação SQLite.

[Nível 2: Snapshots Locais de Tabelas (Table-Level Rollback)]
  └── Cópias shadow `_backup_pre_money_[tabela]` preservam o estado REAL exato da tabela.

[Nível 3: Ponto de Restauração de Arquivo Físico (Full Database Restore)]
  └── Arquivo `.db` congelado em modo binário com hash SHA-256 antes de qualquer abertura com escrita.
```

---

## 2. PROCEDIMENTO PRÉ-MIGRATION (PONTO DE RESTAURAÇÃO)

Antes de executar qualquer migração futura, o procedimento mandatório compreende:

### 2.1 Fechamento e Quiescência
1. Encerrar todas as conexões ativas (`SqliteConnection.ClearAllPools()`).
2. Garantir que nenhuma instância da aplicação ou API esteja em execução (`primoauto.exe`, `dotnet.exe`).
3. Forçar checkpoint do WAL:
   ```sql
   PRAGMA wal_checkpoint(TRUNCATE);
   ```

### 2.2 Backup Físico e Assinatura Criptográfica
1. Criar diretório isolado:
   `AppData\Local\PrimoAutoEletrica\Backups\PreMigration_Money_[TIMESTAMP]\`
2. Copiar os arquivos físicos:
   - `primoauto.db`
   - `primoauto.db-wal` (se existir)
   - `primoauto.db-shm` (se existir)
3. Calcular e registrar hash SHA-256 de `primoauto.db`:
   ```powershell
   Get-FileHash primoauto.db -Algorithm SHA256 > PreMigration_Money.sha256
   ```

### 2.3 Shadow Tables (Snapshots dentro do próprio DB)
Antes de converter cada tabela `T`, criar dentro da mesma transação ou imediatamente antes:
```sql
CREATE TABLE IF NOT EXISTS _shadow_pre_money_T AS SELECT * FROM T;
```

---

## 3. GATILHOS DE DETECÇÃO DE FALHA (CRITÉRIOS DE ABORTO)

O processo de migração deve ser abortado e o rollback disparado automaticamente se QUALQUER uma das seguintes condições for violada:

| Gatilho | Condição de Falha | Ação |
|---|---|---|
| **G01: Contagem de Linhas** | `COUNT(*) pós != COUNT(*) pré` | Aborto imediato + Rollback |
| **G02: Divergência de Soma** | `SUM(cents) != ROUND(SUM(real) * 100)` | Aborto imediato + Rollback |
| **G03: Nulos Indevidos** | `COUNT(cents IS NULL) != COUNT(real IS NULL)` | Aborto imediato + Rollback |
| **G04: Erro de Restrição** | Falha de CHECK, FOREIGN KEY ou UNIQUE | Aborto imediato + Rollback |
| **G05: Timeout ou Bloqueio** | `SQLITE_BUSY` ou `SQLITE_LOCKED` durante migração | Aborto imediato + Rollback |
| **G06: Violação de Integridade** | `PRAGMA integrity_check != 'ok'` | Aborto imediato + Rollback |
| **G07: Falha de Leitura do Repositório** | Teste de carga/leitura pós-migração falhar | Aborto imediato + Rollback |

---

## 4. PROCEDIMENTO DE RESTAURAÇÃO PASSO A PASSO

### Cenário A: Falha Durante a Transação (Nível 1)
Se a falha ocorrer durante o script de migração:
1. Executar comando `ROLLBACK;`.
2. Verificar integridade:
   ```sql
   PRAGMA integrity_check;
   ```
3. O banco permanece no estado original anterior.

### Cenário B: Falha Detectada na Validação Pós-Migração de uma Tabela (Nível 2)
Se uma tabela específica apresentar discrepância nos testes de soma:
1. Reverter a tabela a partir da shadow table:
   ```sql
   BEGIN TRANSACTION;
   DROP TABLE IF EXISTS T;
   ALTER TABLE _shadow_pre_money_T RENAME TO T;
   -- Recriar índices originais de T
   COMMIT;
   ```
2. Validar que a contagem e somas da tabela restaurada conferem 100% com o pré-migration.

### Cenário C: Falha Estrutural ou Corrupção Global (Nível 3)
Se o banco SQLite sofrer falha de I/O, corrupção ou falha catastrófica:
1. Fechar todas as conexões:
   ```csharp
   SqliteConnection.ClearAllPools();
   ```
2. Deletar arquivos corrompidos:
   - `primoauto.db`
   - `primoauto.db-wal`
   - `primoauto.db-shm`
3. Restaurar arquivo físico a partir do diretório de backup:
   ```powershell
   Copy-Item "PreMigration_Money_[TIMESTAMP]\primoauto.db" "primoauto.db"
   ```
4. Validar SHA-256 do arquivo restaurado contra `PreMigration_Money.sha256`:
   ```powershell
   $hash = (Get-FileHash primoauto.db -Algorithm SHA256).Hash
   if ($hash -ne $expectedHash) { throw "Falha crítica de integridade na restauração!" }
   ```

---

## 5. VALIDAÇÃO PÓS-ROLLBACK (PROVA DE INTEGRIDADE)

Após a conclusão de qualquer procedimento de rollback, é obrigatório executar:

1. **Integridade Estrutural**:
   ```sql
   PRAGMA integrity_check;
   PRAGMA foreign_key_check;
   ```
   Ambos DEVEM retornar `ok` (0 violações).

2. **Conferência Quantitativa e Financeira**:
   - `SELECT COUNT(*) FROM OrdensServico` → deve ser igual a 9.
   - `SELECT SUM(ValorMaoObra) FROM OrdensServico` → deve ser igual a 940,00.
   - `SELECT COUNT(*) FROM Orcamentos` → deve ser igual a 20.
   - `SELECT SUM(Total) FROM Orcamentos` → deve ser igual a 4.270,00.
   - `SELECT COUNT(*) FROM Produtos` → deve ser igual a 55.

3. **Smoke Test da Aplicação**:
   - Executar suíte de testes unitários: `dotnet test Tests/PrimoAutoEletrica.Tests`
   - Confirmar 302 testes APROVADOS, 0 falhas.

---

## 6. AUDITORIA E LOGGING DE ROLLBACK

Qualquer acionamento de rollback DEVE registrar evidência formal em:
`AppData\Local\PrimoAutoEletrica\Logs\rollback_audit_[TIMESTAMP].log` contendo:
- Data e hora exata.
- Usuário ou processo responsável.
- Motivo do aborto (gatilho G01-G07 violado).
- Tabela de origem da falha.
- Valores esperados vs valores divergentes encontrados.
- Nível de restauração aplicado (1, 2 ou 3).
- Resultado do hash SHA-256 pós-restauração.
