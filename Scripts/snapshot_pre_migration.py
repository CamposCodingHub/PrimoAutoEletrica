import sqlite3
import csv
import json
from decimal import Decimal

db_path = r"c:\Projetos\PrimoAutoEletrica\TestResults\Rehearsal_Fase2_2\primoauto_work_copy.db"
classification_path = r"Docs/audit/2026-09-20/P2_1_MONEY_CLASSIFICATION.csv"

conn = sqlite3.connect(db_path)
cur = conn.cursor()

# Load classification
rows = []
with open(classification_path, encoding='utf-8') as f:
    reader = csv.DictReader(f)
    for r in reader:
        rows.append(r)

results = []

# Empty tables specially noted
empty_target_tables = {"Vendas", "VendaItens", "CaixaSessoes", "MovimentacoesCaixa"}

for row in rows:
    table = row["Tabela"]
    col = row["Coluna"]
    cat = row["CategoriaSemantica"]
    is_money = row["EMonetario"] == "SIM"
    
    # Check if table exists
    cur.execute("SELECT name FROM sqlite_master WHERE type='table' AND name=?", (table,))
    if not cur.fetchone():
        results.append({
            "table": table,
            "column": col,
            "category": cat,
            "status": "TABLE_NOT_FOUND",
            "count": 0
        })
        continue

    # Get Table PK
    cur.execute(f"PRAGMA table_info(\"{table}\")")
    cols_info = cur.fetchall()
    pks = [c[1] for c in cols_info if c[5] > 0]
    
    # Get FKs
    cur.execute(f"PRAGMA foreign_key_list(\"{table}\")")
    fks = [{"from": f[3], "to_table": f[2], "to_col": f[4]} for f in cur.fetchall()]
    
    # Get Indexes
    cur.execute(f"PRAGMA index_list(\"{table}\")")
    indexes = []
    for idx in cur.fetchall():
        idx_name = idx[1]
        unique = idx[2]
        cur.execute(f"PRAGMA index_info(\"{idx_name}\")")
        idx_cols = [ic[2] for ic in cur.fetchall()]
        indexes.append({"name": idx_name, "unique": unique, "columns": idx_cols})
        
    # Get DDL constraints
    cur.execute("SELECT sql FROM sqlite_master WHERE type='table' AND name=?", (table,))
    ddl = cur.fetchone()[0]

    # Query column data
    cur.execute(f"SELECT count(*), count(\"{col}\") FROM \"{table}\"")
    total_rows, non_null_count = cur.fetchone()
    null_count = total_rows - non_null_count
    
    cur.execute(f"SELECT \"{col}\" FROM \"{table}\" WHERE \"{col}\" IS NOT NULL")
    raw_vals = [r[0] for r in cur.fetchall()]
    
    zeros = sum(1 for v in raw_vals if v == 0)
    negatives = sum(1 for v in raw_vals if v < 0)
    
    # Check decimal places
    more_than_2_dec = 0
    dec_vals = []
    for v in raw_vals:
        # Convert float to str / Decimal carefully
        s = f"{v:.10f}".rstrip('0').rstrip('.')
        if '.' in s:
            dec_part = s.split('.')[1]
            if len(dec_part) > 2:
                # check if it's float imprecision
                d = Decimal(str(v))
                if abs(d - round(d, 2)) > Decimal('0.0000001'):
                    more_than_2_dec += 1
        dec_vals.append(Decimal(str(v)))
        
    if dec_vals:
        val_sum = sum(dec_vals)
        val_min = min(dec_vals)
        val_max = max(dec_vals)
        sample = [str(x) for x in raw_vals[:5]]
    else:
        val_sum = Decimal(0)
        val_min = None
        val_max = None
        sample = []
        
    status_note = ""
    if total_rows == 0 and table in empty_target_tables:
        status_note = "SEM DADOS REAIS — validação operacional realizada somente com dados sintéticos."
    elif total_rows == 0:
        status_note = "TABELA SEM REGISTROS"
        
    results.append({
        "table": table,
        "column": col,
        "category": cat,
        "is_money": is_money,
        "total_rows": total_rows,
        "null_count": null_count,
        "sum": str(val_sum),
        "min": str(val_min) if val_min is not None else None,
        "max": str(val_max) if val_max is not None else None,
        "zeros_count": zeros,
        "negatives_count": negatives,
        "more_than_2_dec": more_than_2_dec,
        "samples": sample,
        "primary_key": pks,
        "foreign_keys": fks,
        "indexes": indexes,
        "status_note": status_note,
        "ddl": ddl
    })

conn.close()

with open(r"c:\Projetos\PrimoAutoEletrica\TestResults\Rehearsal_Fase2_2\snapshot_pre_migration.json", "w", encoding="utf-8") as f:
    json.dump(results, f, indent=2, ensure_ascii=False)

print(f"Snapshot pré-migration concluído com sucesso. {len(results)} campos catalogados.")
