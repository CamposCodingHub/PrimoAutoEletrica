import sqlite3, hashlib, os, shutil, datetime, uuid

test_dir = r'C:\Projetos\PrimoAutoEletrica\TestResults\B5_2_Validation'
os.makedirs(test_dir, exist_ok=True)
db_source = r'C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto_operacional.db'
test_db = os.path.join(test_dir, 'primoauto_b5_2_test.db')
shutil.copy2(db_source, test_db)

conn = sqlite3.connect(test_db)
cur = conn.cursor()

cliente_id = str(uuid.uuid4())
veiculo_id = str(uuid.uuid4())
os_id = str(uuid.uuid4())
now_str = datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')

# Insert controlled test records
cur.execute('''
INSERT INTO Clientes (
    Id, Nome, TipoPessoa, Ativo, ClienteVip, TotalGasto, TotalServicos,
    PontosFidelidade, ConsentimentoLGPD, AutorizaContatoWhatsApp, DataCadastro,
    RowVersion, IsDeleted, Telefone
) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
''', (
    cliente_id, 'Cliente Teste B5.2', 'Fisica', 1, 0, 0.0, 1,
    0, 1, 1, now_str, 1, 0, '11999990000'
))

cur.execute('''
INSERT INTO Veiculos (
    Id, ClienteId, Placa, Marca, Modelo, Ano, Quilometragem,
    RowVersion, IsDeleted
) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
''', (
    veiculo_id, cliente_id, 'TST5200', 'Volkswagen', 'Gol 1.6', 2020, 55000, 1, 0
))

cur.execute('''
INSERT INTO OrdensServico (
    Id, Numero, ClienteId, VeiculoId, Status, Prioridade,
    AprovadaCliente, DataAbertura, TempoPrevistoMinutos, TempoRealMinutos,
    ValorMaoObra, Desconto, Ativo, RowVersion, IsDeleted
) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
''', (
    os_id, 'OS-B52-001', cliente_id, veiculo_id, 'Aberta', 'Normal',
    1, now_str, 60, 0, 150.0, 0.0, 1, 1, 0
))

conn.commit()

# Checkpoint WAL so changes are fully in main db file
cur.execute('PRAGMA wal_checkpoint(TRUNCATE);')

# Timestamped backup via SQLite Backup API
timestamp = datetime.datetime.now().strftime('%Y%m%d_%H%M%S')
backup_path = os.path.join(test_dir, f'primoauto_operacional_{timestamp}.db')
b_conn = sqlite3.connect(backup_path)
conn.backup(b_conn)
b_conn.commit()

# Hash before
with open(test_db, 'rb') as f:
    sha_before = hashlib.sha256(f.read()).hexdigest().upper()

cur.execute('PRAGMA user_version;')
uv_before = cur.fetchone()[0]

# Verify backup
with open(backup_path, 'rb') as f:
    sha_backup = hashlib.sha256(f.read()).hexdigest().upper()

b_cur = b_conn.cursor()
b_cur.execute('PRAGMA integrity_check;')
b_ic = b_cur.fetchall()
b_cur.execute('PRAGMA foreign_key_check;')
b_fk = b_cur.fetchall()
b_conn.close()

# Simulate controlled update failure & rollback
try:
    cur.execute('BEGIN TRANSACTION;')
    cur.execute('UPDATE Clientes SET Nome = ? WHERE Id = ?', ('CORRUPTED DATA', cliente_id))
    # Simulated failure
    raise RuntimeError('Simulated migration exception during update')
except Exception as e:
    conn.rollback()

# Verify rollback
cur.execute('SELECT Nome FROM Clientes WHERE Id = ?', (cliente_id,))
nome_after_rb = cur.fetchone()[0]

# Simulate recovery from backup
cur.execute('UPDATE Clientes SET Nome = ? WHERE Id = ?', ('DATA MODIFIED BEFORE RESTORE', cliente_id))
conn.commit()
cur.execute('PRAGMA wal_checkpoint(TRUNCATE);')
conn.close()

# Restore from backup
shutil.copy2(backup_path, test_db)
r_conn = sqlite3.connect(test_db)
r_cur = r_conn.cursor()
r_cur.execute('SELECT Nome FROM Clientes WHERE Id = ?', (cliente_id,))
nome_restored = r_cur.fetchone()[0]
r_cur.execute('PRAGMA integrity_check;')
r_ic = r_cur.fetchall()
r_cur.execute('PRAGMA foreign_key_check;')
r_fk = r_cur.fetchall()
r_conn.close()

print('=== B5.2 LIFECYCLE RESULTS ===')
print(f'SHA Before: {sha_before}')
print(f'SHA Backup: {sha_backup}')
print(f'User Version: {uv_before}')
print(f'Backup Integrity: {b_ic}')
print(f'Backup FK: {b_fk}')
print(f'Rollback Verification: {nome_after_rb == "Cliente Teste B5.2"}')
print(f'Recovery Verification: {nome_restored == "Cliente Teste B5.2"}')
print(f'Restored Integrity: {r_ic}')
print(f'Restored FK: {r_fk}')
