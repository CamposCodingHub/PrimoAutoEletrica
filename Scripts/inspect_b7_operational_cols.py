import sqlite3, csv

with open('Docs/audit/2026-09-20/B7_MONEY_FIELD_FINAL_MATRIX.csv', 'r', encoding='utf-8') as f:
    reader = csv.DictReader(f)
    fields = [(r['Table'], r['Column']) for r in reader if r['Category'] in ('MONEY', 'MONEY_DERIVED')]

conn = sqlite3.connect('C:/Users/campo/AppData/Local/PrimoAutoEletrica/primoauto_operacional.db')
c = conn.cursor()
types = {}
for tbl, col in fields:
    c.execute(f'PRAGMA table_info("{tbl}")')
    cols = {r[1]: r[2] for r in c.fetchall()}
    types[(tbl, col)] = cols.get(col, 'MISSING')

print(f'Total mapped fields: {len(fields)}')
real_count = sum(1 for t in types.values() if t.upper() in ('REAL', 'NUMERIC', 'FLOAT', 'DOUBLE'))
int_count = sum(1 for t in types.values() if t.upper() in ('INTEGER', 'INT', 'BIGINT'))
print(f'REAL count: {real_count}, INTEGER count: {int_count}')
for (tbl, col), t in types.items():
    print(f'{tbl}.{col}: {t}')
