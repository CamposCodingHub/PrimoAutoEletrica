import sqlite3
import shutil
import hashlib
import os

pre_backup = r"c:\Projetos\PrimoAutoEletrica\TestResults\Rehearsal_Fase2_2\primoauto_pre_migration_backup.db"
test_rollback_db = r"c:\Projetos\PrimoAutoEletrica\TestResults\Rehearsal_Fase2_2\test_rollback_simulation.db"
test_restore_db = r"c:\Projetos\PrimoAutoEletrica\TestResults\Rehearsal_Fase2_2\test_restore_validation.db"

def sha256_file(filepath):
    h = hashlib.sha256()
    with open(filepath, "rb") as f:
        while chunk := f.read(65536):
            h.update(chunk)
    return h.hexdigest().upper()

print("=== TESTE 19 & 21: ROLLBACK E BACKUP/RESTORE VALIDATION ===")

if os.path.exists(test_rollback_db):
    os.remove(test_rollback_db)

shutil.copyfile(pre_backup, test_rollback_db)
pre_hash = sha256_file(test_rollback_db)
print(f"Base rollback test DB criada. SHA-256: {pre_hash}")

conn = sqlite3.connect(test_rollback_db, isolation_level=None)
cur = conn.cursor()

# Get baseline count and sum of Produtos
cur.execute("SELECT count(*), sum(PrecoVenda) FROM Produtos")
base_cnt, base_sum = cur.fetchone()
print(f"Baseline Produtos: {base_cnt} rows, SUM(PrecoVenda)={base_sum}")

# SCENARIO 1: Erro durante cópia / conversão dentro da transação -> ROLLBACK
print("\n--- SIMULAÇÃO 1: Erro intencional durante cópia de dados -> ROLLBACK ---")
cur.execute("PRAGMA foreign_keys = OFF")
cur.execute("BEGIN TRANSACTION")
cur.execute("CREATE TABLE Produtos_sim_err (Id TEXT PRIMARY KEY, PrecoVenda INTEGER)")
try:
    cur.execute("INSERT INTO Produtos_sim_err SELECT Id, PrecoVenda FROM Produtos")
    # Forçar erro sintático no SQLite
    cur.execute("INSERT INTO Produtos_sim_err (Id, PrecoVenda) VALUES ('duplicate_key', RAISE(FAIL, 'Simulated Failure'))")
except Exception as e:
    print(f"Erro capturado com sucesso: {e}")
    cur.execute("ROLLBACK")
    print("ROLLBACK executado.")

# Verificar se tabela de erro sumiu e Produtos permanece intacto
cur.execute("SELECT count(*) FROM sqlite_master WHERE name='Produtos_sim_err'")
assert cur.fetchone()[0] == 0, "Tabela temporária não foi removida no rollback!"
cur.execute("SELECT count(*), sum(PrecoVenda) FROM Produtos")
c, s = cur.fetchone()
assert c == base_cnt and s == base_sum, "Produtos corrompido após rollback!"
print(f"Verificação Pós-Rollback 1: {c} rows, SUM={s} [PASS - Inalterado]")

# SCENARIO 2: Erro durante recriação de índice/constraint -> ROLLBACK
print("\n--- SIMULAÇÃO 2: Erro durante recriação de índice -> ROLLBACK ---")
cur.execute("BEGIN TRANSACTION")
cur.execute("CREATE TABLE Clientes_sim (Id TEXT PRIMARY KEY)")
try:
    # Índice inválido
    cur.execute("CREATE INDEX idx_invalid_fail ON Clientes_sim (NonExistentCol)")
except Exception as e:
    print(f"Erro de índice capturado com sucesso: {e}")
    cur.execute("ROLLBACK")
    print("ROLLBACK executado.")

cur.execute("SELECT count(*) FROM Clientes")
assert cur.fetchone()[0] == 1, "Clientes corrompido!"
cur.execute("SELECT count(*) FROM sqlite_master WHERE name='Clientes_sim'")
assert cur.fetchone()[0] == 0, "Clientes_sim não desfeito!"
print("Verificação Pós-Rollback 2: Clientes [PASS - Inalterado]")

# SCENARIO 3: Erro de Foreign Key -> ROLLBACK
print("\n--- SIMULAÇÃO 3: Violação de Foreign Key antes de commit -> ROLLBACK ---")
cur.execute("PRAGMA foreign_keys = ON")
cur.execute("BEGIN TRANSACTION")
try:
    # Tentar inserir item de OS apontando para OS inexistente (violação direta)
    cur.execute("INSERT INTO OrdemServicoItens (Id, OrdemServicoId, Quantidade, ValorUnitario) VALUES ('err-fk', 'non-existent-os-id', 1, 100)")
except Exception as e:
    print(f"Violação detectada: {e}")
    cur.execute("ROLLBACK")
    print("ROLLBACK executado.")

cur.execute("SELECT count(*) FROM OrdemServicoItens WHERE Id = 'err-fk'")
assert cur.fetchone()[0] == 0, "Registro inválido persistido!"
print("Verificação Pós-Rollback 3: [PASS - 0 registros órfãos persistidos]")

# SCENARIO 4: Processo encerrado sem COMMIT (desconexão abrupta)
print("\n--- SIMULAÇÃO 4: Conexão abortada antes de COMMIT ---")
cur.execute("BEGIN TRANSACTION")
cur.execute("UPDATE Funcionarios SET Salario = 999999.00 WHERE Id = 1")
# Fechar sem commit
conn.close()
print("Conexão fechada abruptamente.")

# Reabrir e verificar
conn2 = sqlite3.connect(test_rollback_db, isolation_level=None)
cur2 = conn2.cursor()
cur2.execute("SELECT Salario FROM Funcionarios WHERE Id = 1")
assert cur2.fetchone()[0] != 999999.00, "Update não comitado foi persistido!"
print("Verificação Pós-Desconexão 4: Salario permanece original [PASS]")
conn2.close()

# SCENARIO 5: Restauração da cópia física (Nível 3)
print("\n--- SIMULAÇÃO 5: Restauração de Cópia Física ---")
if os.path.exists(test_rollback_db):
    os.remove(test_rollback_db)
shutil.copyfile(pre_backup, test_rollback_db)
restored_hash = sha256_file(test_rollback_db)
assert restored_hash == pre_hash, "Hash de restauração não confere!"
print(f"Restauração física concluída com SHA-256 idêntico: {restored_hash} [PASS]")

# TESTE 21: TESTAR BACKUP + RESTORE
print("\n=== TESTE 21: BACKUP + RESTORE EM NOVO ARQUIVO ===")
if os.path.exists(test_restore_db):
    os.remove(test_restore_db)

shutil.copyfile(pre_backup, test_restore_db)
conn_res = sqlite3.connect(test_restore_db, isolation_level=None)
cur_res = conn_res.cursor()

cur_res.execute("PRAGMA integrity_check")
chk = cur_res.fetchone()[0]
assert chk == "ok", f"Integrity check failed: {chk}"
print(f"PRAGMA integrity_check no arquivo restaurado: {chk} [PASS]")

# Comparar contagens de tabelas e somas de PKs/FKs
conn_orig = sqlite3.connect(pre_backup, isolation_level=None)
cur_orig = conn_orig.cursor()

cur_orig.execute("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'")
all_tables = [r[0] for r in cur_orig.fetchall()]

divergences = 0
for tbl in all_tables:
    cur_orig.execute(f"SELECT count(*) FROM \"{tbl}\"")
    c_orig = cur_orig.fetchone()[0]
    cur_res.execute(f"SELECT count(*) FROM \"{tbl}\"")
    c_res = cur_res.fetchone()[0]
    if c_orig != c_res:
        divergences += 1
        print(f"Divergência em {tbl}: {c_orig} vs {c_res}")

assert divergences == 0, f"Total divergências no restore: {divergences}"
print(f"Validação de {len(all_tables)} tabelas restauradas: 0 divergências [PASS]")

cur_res.execute("PRAGMA foreign_key_check")
fk_res = cur_res.fetchall()
assert len(fk_res) == 0, f"FK errors no restore: {fk_res}"
print("PRAGMA foreign_key_check no restaurado: 0 violações [PASS]")

conn_orig.close()
conn_res.close()

# Cleanup test files
if os.path.exists(test_rollback_db):
    os.remove(test_rollback_db)
if os.path.exists(test_restore_db):
    os.remove(test_restore_db)

print("\n>>> TESTES 19 (ROLLBACK) E 21 (BACKUP + RESTORE) CONCLUÍDOS COM 100% SUCESSO (PASS).")
