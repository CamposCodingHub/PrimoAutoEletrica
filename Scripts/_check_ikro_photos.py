import sqlite3
from pathlib import Path

db = r"C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db"
media = Path(r"C:\Users\campo\AppData\Local\PrimoAutoEletrica\Media\Catalogo")
c = sqlite3.connect(db)

rows = c.execute(
    """
    SELECT CodigoFabricante, ImagemUrl, ImagemLocal, length(IFNULL(ImagemLocal,''))
    FROM CatalogoPecas
    WHERE Marca='IKRO'
    ORDER BY CodigoFabricante
    """
).fetchall()

ok = missing = empty = 0
print(f"TOTAL_IKRO={len(rows)}")
print(f"JPG_ON_DISK={len(list(media.glob('IKRO_*.jpg')))}")
print("---")
for codigo, url, local, n in rows:
    exists = Path(local).exists() if local else False
    if not local:
        empty += 1
        status = "EMPTY"
    elif not exists:
        missing += 1
        status = "FILE_MISSING"
    else:
        ok += 1
        status = "OK"
    if status != "OK" or codigo in ("IK6003DDU", "IKB823D", "IK62201"):
        print(f"{status}\t{codigo}\tlocal={local!r}\texists={exists}\tsize={Path(local).stat().st_size if exists else 0}")

print("---")
print(f"OK={ok} EMPTY={empty} FILE_MISSING={missing}")

# also check if UI might expect relative path / different folder
sample = c.execute("SELECT ImagemLocal FROM CatalogoPecas WHERE IFNULL(ImagemLocal,'')!='' AND Marca!='IKRO' LIMIT 5").fetchall()
print("SAMPLE_OTHER_BRANDS_ImagemLocal:")
for (p,) in sample:
    print(" ", p, "exists=", Path(p).exists() if p else False)
