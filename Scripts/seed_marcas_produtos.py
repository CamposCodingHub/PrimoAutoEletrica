import sqlite3, uuid, csv, re
from datetime import datetime
from pathlib import Path

DB = r"C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db"
conn = sqlite3.connect(DB)
conn.row_factory = sqlite3.Row
cur = conn.cursor()
now = datetime.now().isoformat(sep=" ", timespec="seconds")

def cols(table):
    return [r[1] for r in cur.execute(f"PRAGMA table_info({table})")]

print("Produtos cols:", cols("Produtos"))
print("CatalogoPecas sample status:", list(cur.execute("SELECT StatusRevisao, COUNT(*) FROM CatalogoPecas GROUP BY 1")))

# --- import CSVs into CatalogoPecas ---
csvs = [
    Path(r"C:\Projetos\PrimoAutoEletrica\TestData\Catalogos\Catalogo-UETA-Amostra.csv"),
    Path(r"C:\Projetos\PrimoAutoEletrica\TestData\Catalogos\Catalogo-BOSCH-NGK-Amostra.csv"),
    Path(r"C:\Projetos\PrimoAutoEletrica\TestData\Catalogos\Catalogo-DNI-Amostra.csv"),
]

def norm_code(code, marca):
    raw = re.sub(r"[^A-Za-z0-9]", "", (code or "").upper())
    return raw

cat_ins = cat_skip = 0
for path in csvs:
    with path.open(encoding="utf-8-sig", newline="") as f:
        reader = csv.DictReader(f, delimiter=";")
        for row in reader:
            codigo = (row.get("CodigoFabricante") or "").strip()
            marca = (row.get("Marca") or "GERAL").strip().upper()
            if not codigo:
                continue
            n = norm_code(codigo, marca)
            exists = cur.execute(
                "SELECT Id FROM CatalogoPecas WHERE CodigoNormalizado=? AND Marca=? COLLATE NOCASE",
                (n, marca),
            ).fetchone()
            if exists:
                cat_skip += 1
                continue
            cur.execute(
                """INSERT INTO CatalogoPecas
                (Id, CodigoFabricante, CodigoNormalizado, Marca, Nome, Descricao, Categoria, Subcategoria, Linha,
                 Aplicacao, VeiculoAplicacao, AnoInicial, AnoFinal, Voltagem, Amperagem, QuantidadeTerminais, TipoProduto,
                 PaginaCatalogo, FonteCatalogo, ArquivoOrigem, ObservacoesTecnicas, ImagemUrl, ImagemLocal,
                 StatusRevisao, ProdutoEstoqueId, DataImportacao, DataAtualizacao, Ativo)
                VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)""",
                (
                    str(uuid.uuid4()), codigo, n, marca,
                    row.get("Nome") or codigo,
                    row.get("Descricao") or "",
                    row.get("Categoria") or "",
                    "", "",
                    row.get("Aplicacao") or "",
                    "", None, None,
                    row.get("Voltagem") or "",
                    "", "", "",
                    "",
                    f"Amostra {marca}",
                    path.name,
                    "", "", "",
                    "Importado",
                    None, now, now, 1,
                ),
            )
            cat_ins += 1

print(f"CSV catalog inserted={cat_ins} skipped={cat_skip}")

# --- seed Produtos from catalog brands (skip Teste/Smoke junk optionally keep) ---
# Pick up to 8 per marca from CatalogoPecas
marcas = [r[0] for r in cur.execute("SELECT DISTINCT Marca FROM CatalogoPecas WHERE IFNULL(Marca,'')!='' ORDER BY 1")]
print("Marcas no catalogo:", marcas)

prod_cols = set(cols("Produtos"))
prod_ins = prod_skip = 0
for marca in marcas:
    rows = cur.execute(
        """SELECT Id, CodigoFabricante, CodigoNormalizado, Nome, Descricao, Categoria, Voltagem, Aplicacao, ImagemLocal
           FROM CatalogoPecas WHERE Marca=? COLLATE NOCASE AND Ativo=1
           ORDER BY DataImportacao DESC LIMIT 8""",
        (marca,),
    ).fetchall()
    for r in rows:
        codigo = r["CodigoFabricante"]
        # avoid duplicate by Codigo
        if cur.execute("SELECT Id FROM Produtos WHERE Codigo=? COLLATE NOCASE", (codigo,)).fetchone():
            prod_skip += 1
            continue
        # also skip if already linked
        if cur.execute(
            "SELECT Id FROM CatalogoPecas WHERE Id=? AND IFNULL(ProdutoEstoqueId,'')!=''",
            (r["Id"],),
        ).fetchone():
            # still ok to create? skip if linked
            pass
        pid = str(uuid.uuid4())
        nome = r["Nome"] or f"{marca} {codigo}"
        # Build insert dynamically for required-ish columns
        fields = {
            "Id": pid,
            "Codigo": codigo,
            "Nome": nome,
            "Descricao": r["Descricao"] or "",
            "Categoria": r["Categoria"] or "Auto Eletrica",
            "Marca": marca,
            "Modelo": "",
            "FornecedorId": None,
            "Fornecedor": "",
            "CNPJFornecedor": "",
            "ContatoFornecedor": "",
            "TelefoneFornecedor": "",
            "QuantidadeEstoque": 5,
            "QuantidadeMinima": 1,
            "QuantidadeMaxima": 50,
            "Localizacao": "A1",
            "Prateleira": "",
            "Gaveta": "",
            "PrecoCompra": 25.0,
            "PrecoVenda": 49.9,
        }
        # optional columns if exist
        optional = {
            "ImagemUrl": r["ImagemLocal"] or "",
            "Ativo": 1,
            "DataCadastro": now,
            "DataAtualizacao": now,
            "Unidade": "UN",
            "NCM": "",
            "Observacoes": f"Seed a partir do catalogo {marca}",
        }
        for k,v in optional.items():
            if k in prod_cols:
                fields[k] = v
        use = {k:v for k,v in fields.items() if k in prod_cols}
        keys = ",".join(use.keys())
        qs = ",".join(["?"]*len(use))
        cur.execute(f"INSERT INTO Produtos ({keys}) VALUES ({qs})", list(use.values()))
        # link catalog item
        if "ProdutoEstoqueId" in cols("CatalogoPecas"):
            cur.execute(
                "UPDATE CatalogoPecas SET ProdutoEstoqueId=?, StatusRevisao=?, DataAtualizacao=? WHERE Id=?",
                (pid, "Vinculado", now, r["Id"]),
            )
        prod_ins += 1

conn.commit()
print(f"Produtos inserted={prod_ins} skipped={prod_skip}")
print("=== Produtos by Marca ===")
for r in cur.execute("SELECT IFNULL(Marca,'(null)'), COUNT(*) FROM Produtos GROUP BY 1 ORDER BY 2 DESC"):
    print(r)
print("=== Catalogo by Marca ===")
for r in cur.execute("SELECT Marca, COUNT(*) FROM CatalogoPecas GROUP BY Marca ORDER BY 2 DESC"):
    print(r)
conn.close()
print("DONE")
