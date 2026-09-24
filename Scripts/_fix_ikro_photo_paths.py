import sqlite3
import shutil
from pathlib import Path
from datetime import datetime

db = r"C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db"
src_root = Path(r"C:\Users\campo\AppData\Local\PrimoAutoEletrica\Media\Catalogo")
dst_root = Path(r"C:\Users\campo\AppData\Local\PrimoAutoEletrica\Catalogo\Imagens\IKRO\CATALOGO_ROLAMENTOS")
dst_root.mkdir(parents=True, exist_ok=True)

conn = sqlite3.connect(db)
cur = conn.cursor()
rows = cur.execute(
    "SELECT Id, CodigoFabricante, ImagemLocal FROM CatalogoPecas WHERE Marca='IKRO' COLLATE NOCASE"
).fetchall()

moved = 0
updated = 0
missing = 0
for pid, codigo, local in rows:
    old = Path(local) if local else None
    if not old or not old.exists():
        # try find by code in Media folder
        candidates = list(src_root.glob(f"IKRO_{codigo}_*.jpg")) + list(src_root.glob(f"IKRO_*{codigo}*.jpg"))
        if not candidates:
            missing += 1
            print(f"MISSING {codigo} old={local!r}")
            continue
        old = candidates[0]

    new_path = dst_root / old.name
    shutil.copy2(old, new_path)
    moved += 1
    cur.execute(
        "UPDATE CatalogoPecas SET ImagemLocal=?, ImagemUrl=?, DataAtualizacao=? WHERE Id=?",
        (str(new_path), str(new_path), datetime.now().isoformat(sep=' ', timespec='seconds'), pid),
    )
    updated += 1
    print(f"OK {codigo} -> {new_path}")

conn.commit()

# verify
ok = 0
for pid, codigo, local in cur.execute(
    "SELECT Id, CodigoFabricante, ImagemLocal FROM CatalogoPecas WHERE Marca='IKRO' COLLATE NOCASE"
):
    if local and Path(local).exists():
        ok += 1
    else:
        print(f"STILL_BAD {codigo} {local}")
conn.close()
print(f"MOVED={moved} UPDATED={updated} MISSING={missing} VERIFY_OK={ok} DST={dst_root}")
print(f"FILES_IN_DST={len(list(dst_root.glob('*.jpg')))}")
