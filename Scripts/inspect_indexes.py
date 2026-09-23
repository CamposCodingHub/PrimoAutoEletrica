import sqlite3

conn = sqlite3.connect(r'c:\Projetos\PrimoAutoEletrica\TestResults\Rehearsal_Fase2_2\primoauto_work_copy.db')
cur = conn.cursor()
cur.execute("SELECT tbl_name, type, name, sql FROM sqlite_master WHERE type IN ('index', 'trigger') AND sql IS NOT NULL ORDER BY tbl_name, name")
items = cur.fetchall()
print(f"Total custom indexes and triggers: {len(items)}")
by_table = {}
for tbl, tp, nm, sql in items:
    by_table.setdefault(tbl, []).append((tp, nm, sql))

for tbl, list_items in sorted(by_table.items()):
    print(f"Table '{tbl}': {len(list_items)} indexes/triggers")
    for tp, nm, sql in list_items:
        print(f"   - {tp} {nm}")
conn.close()
