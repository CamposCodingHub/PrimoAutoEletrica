#!/usr/bin/env python3
"""Soft-delete produtos de seed Smoke/Teste no SQLite local."""
from __future__ import annotations

import argparse
import shutil
import sqlite3
from datetime import datetime, timezone
from pathlib import Path

PATTERNS_SQL = """
SELECT Id, Codigo, Nome
FROM Produtos
WHERE IFNULL(IsDeleted, 0) = 0
  AND (
    Codigo LIKE 'SMK%'
    OR UPPER(Codigo) LIKE '%SMOKE%'
    OR Nome LIKE '%Smoke%'
    OR Nome LIKE '%smoke%'
    OR Nome LIKE '%Teste%'
    OR Nome LIKE '%teste%'
    OR Nome LIKE 'Produto Estoque %'
    OR Nome LIKE 'Rele Automotivo Smoke%'
  )
"""


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--database", required=True)
    parser.add_argument("--what-if", action="store_true")
    args = parser.parse_args()

    db = Path(args.database)
    if not db.is_file():
        raise SystemExit(f"Banco nao encontrado: {db}")

    backup = db.with_suffix(db.suffix + f".smoke-cleanup-{datetime.now().strftime('%Y%m%d-%H%M%S')}.bak")
    shutil.copy2(db, backup)
    print(f"Backup: {backup}")

    con = sqlite3.connect(str(db))
    rows = con.execute(PATTERNS_SQL).fetchall()
    print(f"Encontrados {len(rows)} produtos Smoke/Teste:")
    for row in rows:
        print(f"  {row[0]} | {row[1]} | {row[2]}")

    if args.what_if or not rows:
        con.close()
        return 0

    now = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M:%S")
    for product_id, _, _ in rows:
        con.execute(
            """
            UPDATE Produtos
            SET Ativo = 0,
                IsDeleted = 1,
                ExcluidoEm = ?,
                ExcluidoPor = 'SmokeCleanup'
            WHERE Id = ?
            """,
            (now, product_id),
        )
    con.commit()
    con.close()
    print(f"Soft-delete concluido em {len(rows)} produtos.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
