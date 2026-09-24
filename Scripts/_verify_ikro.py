import sqlite3
from pathlib import Path
db=r'C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db'
c=sqlite3.connect(db)
rows=c.execute("SELECT CodigoFabricante, PaginaCatalogo, ImagemLocal, Nome FROM CatalogoPecas WHERE Marca='IKRO' ORDER BY CAST(PaginaCatalogo AS INT), CodigoFabricante").fetchall()
print('count', len(rows))
for codigo, pag, img, nome in rows:
    tail = (img or '')[-50:]
    print(f'{codigo}\tp{pag}\t{tail}')
media=Path(r'C:\Users\campo\AppData\Local\PrimoAutoEletrica\Media\Catalogo')
imgs=list(media.glob('IKRO_*.jpg'))
print('jpg_files', len(imgs), 'bytes', sum(i.stat().st_size for i in imgs))
