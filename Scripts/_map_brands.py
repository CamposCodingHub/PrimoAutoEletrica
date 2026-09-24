import sqlite3
db=r'C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db'
c=sqlite3.connect(db)
print('=== CatalogoPecas by Marca ===')
for r in c.execute('SELECT Marca, COUNT(*) FROM CatalogoPecas GROUP BY Marca ORDER BY 2 DESC'):
    print(r)
print('=== tables prod/marca ===')
for r in c.execute("SELECT name FROM sqlite_master WHERE type='table' ORDER BY 1"):
    n=r[0]
    if any(x in n.lower() for x in ('prod','marca','brand','estoq','catalog')):
        print(n)
print('=== Produtos count if exists ===')
tables={r[0] for r in c.execute("SELECT name FROM sqlite_master WHERE type='table'")}
for t in ['Produtos','Produto','Estoque','Marcas','Fabricantes']:
    if t in tables:
        print(t, c.execute(f'SELECT COUNT(*) FROM {t}').fetchone()[0])
        cols=[x[1] for x in c.execute(f'PRAGMA table_info({t})')]
        print(' cols', cols[:20])
        if 'Marca' in cols or 'Fabricante' in cols:
            col='Marca' if 'Marca' in cols else 'Fabricante'
            for r in c.execute(f'SELECT {col}, COUNT(*) FROM {t} GROUP BY {col} ORDER BY 2 DESC LIMIT 15'):
                print(' ', r)
