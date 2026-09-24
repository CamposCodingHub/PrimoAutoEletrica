import sqlite3

def main():
    db_path = r'C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto_operacional.db'
    conn = sqlite3.connect(db_path)
    cur = conn.cursor()

    cur.execute('PRAGMA integrity_check;')
    integrity = cur.fetchall()

    cur.execute('PRAGMA foreign_key_check;')
    fk_check = cur.fetchall()

    cur.execute('PRAGMA user_version;')
    user_version = cur.fetchone()[0]

    cur.execute("SELECT type, count(*) FROM sqlite_master GROUP BY type;")
    objects_summary = dict(cur.fetchall())

    cur.execute("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';")
    tables = [row[0] for row in cur.fetchall()]

    total_records = 0
    table_counts = {}
    for t in tables:
        try:
            cur.execute(f"SELECT count(*) FROM [{t}]")
            cnt = cur.fetchone()[0]
            table_counts[t] = cnt
            total_records += cnt
        except Exception as e:
            table_counts[t] = str(e)

    conn.close()

    print("INTEGRITY_CHECK:", integrity)
    print("FOREIGN_KEY_CHECK_VIOLATIONS:", len(fk_check))
    print("USER_VERSION:", user_version)
    print("OBJECTS_SUMMARY:", objects_summary)
    print("TOTAL_TABLES:", len(tables))
    print("TOTAL_RECORDS:", total_records)
    print("TOP_10_TABLES_BY_ROWS:")
    for name, cnt in sorted(table_counts.items(), key=lambda x: x[1] if isinstance(x[1], int) else 0, reverse=True)[:10]:
        print(f"  - {name}: {cnt}")

if __name__ == '__main__':
    main()
