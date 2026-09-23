import sqlite3
import os
import hashlib

db_path = r"c:\Projetos\PrimoAutoEletrica\TestResults\Rehearsal_Fase2_2\primoauto_work_copy.db"
orig_path = r"C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db"

def sha256_file(filepath):
    h = hashlib.sha256()
    with open(filepath, "rb") as f:
        while chunk := f.read(65536):
            h.update(chunk)
    return h.hexdigest().upper()

print("Original Path:", orig_path)
print("Original Size:", os.path.getsize(orig_path))
orig_hash = sha256_file(orig_path)
print("Original SHA-256:", orig_hash)

print("\nCopy Path:", db_path)
print("Copy Size:", os.path.getsize(db_path))
copy_hash = sha256_file(db_path)
print("Copy SHA-256:", copy_hash)
print("Hashes Identical?:", orig_hash == copy_hash)

conn = sqlite3.connect(db_path)
cur = conn.cursor()
cur.execute("SELECT sqlite_version()")
print("\nSQLite Version:", cur.fetchone()[0])
cur.execute("PRAGMA user_version")
print("user_version:", cur.fetchone()[0])
cur.execute("SELECT type, count(*) FROM sqlite_master GROUP BY type")
for r in cur.fetchall():
    print(f"sqlite_master type '{r[0]}': {r[1]}")

cur.execute("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name")
tables = [r[0] for r in cur.fetchall()]
print(f"\nTotal Tables ({len(tables)}):")
for t in tables:
    cur.execute(f"SELECT count(*) FROM \"{t}\"")
    cnt = cur.fetchone()[0]
    print(f"  - {t}: {cnt} rows")

conn.close()
