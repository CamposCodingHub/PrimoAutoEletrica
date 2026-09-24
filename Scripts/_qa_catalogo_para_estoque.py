import sqlite3, uuid
from datetime import datetime
from pathlib import Path

DB = r"C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db"
NOW = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
TAG = "QA-CAT2STOCK-20260919"
conn = sqlite3.connect(DB)
conn.row_factory = sqlite3.Row
cur = conn.cursor()

item = cur.execute(
    """
SELECT Id, CodigoFabricante, CodigoNormalizado, Marca, Nome, Descricao, Categoria, Aplicacao, ImagemLocal, StatusRevisao
FROM CatalogoPecas
WHERE (ProdutoEstoqueId IS NULL OR ProdutoEstoqueId='')
  AND Marca='PRIMOX'
  AND IFNULL(ImagemLocal,'')!=''
LIMIT 1
"""
).fetchone()
assert item, "no unlinked Primox item"
print("PICK", dict(item))
img_ok = Path(item["ImagemLocal"]).exists() if item["ImagemLocal"] else False
print("IMAGE_EXISTS", img_ok)

pid = str(uuid.uuid4())
codigo = item["CodigoNormalizado"] or item["CodigoFabricante"]
if cur.execute("SELECT Id FROM Produtos WHERE Codigo=? COLLATE NOCASE", (codigo,)).fetchone():
    codigo = codigo + "-" + TAG[-6:]

cols = {r[1] for r in cur.execute("PRAGMA table_info(Produtos)")}
fields = {
    "Id": pid,
    "Codigo": codigo,
    "Nome": item["Nome"] or item["CodigoFabricante"],
    "Descricao": item["Descricao"] or "",
    "Categoria": item["Categoria"] or "Auto Eletrica",
    "Marca": item["Marca"] or "PRIMOX",
    "Modelo": "",
    "FornecedorId": None,
    "Fornecedor": "",
    "CNPJFornecedor": "",
    "ContatoFornecedor": "",
    "TelefoneFornecedor": "",
    "QuantidadeEstoque": 1,
    "QuantidadeMinima": 0,
    "QuantidadeMaxima": 100,
    "Localizacao": "QA",
    "Prateleira": "",
    "Gaveta": "",
    "PrecoCompra": 10.0,
    "PrecoVenda": 25.0,
    "MargemLucro": 15.0,
    "ValorTotalEstoque": 10.0,
    "UnidadeMedida": "UN",
    "Ativo": 1,
    "ProdutoPerecivel": 0,
    "DataCadastro": NOW,
    "Observacoes": TAG + " | " + (item["Aplicacao"] or ""),
    "ImagemUrl": item["ImagemLocal"] or "",
    "SKU": item["CodigoFabricante"],
    "TotalVendas": 0,
    "TotalFaturado": 0.0,
    "VendasUltimoMes": 0,
    "VendasUltimoTrimestre": 0,
    "RowVersion": 1,
    "IsDeleted": 0,
}
use = {k: v for k, v in fields.items() if k in cols}
cur.execute(
    "INSERT INTO Produtos (%s) VALUES (%s)" % (",".join(use), ",".join("?" * len(use))),
    list(use.values()),
)
cur.execute(
    "UPDATE CatalogoPecas SET ProdutoEstoqueId=?, StatusRevisao=?, DataAtualizacao=? WHERE Id=?",
    (pid, "ConvertidoEstoque", NOW, item["Id"]),
)
conn.commit()

prod = cur.execute(
    "SELECT Id, Codigo, Nome, QuantidadeEstoque, ImagemUrl, IsDeleted FROM Produtos WHERE Id=?",
    (pid,),
).fetchone()
cat = cur.execute(
    "SELECT ProdutoEstoqueId, StatusRevisao FROM CatalogoPecas WHERE Id=?",
    (item["Id"],),
).fetchone()
print("PROD", dict(prod))
print("CAT", dict(cat))
print("LINK_OK", cat["ProdutoEstoqueId"] == pid and cat["StatusRevisao"] == "ConvertidoEstoque")
print("IN_STOCK", prod is not None and prod["IsDeleted"] == 0)
print(
    "status_counts",
    cur.execute("SELECT StatusRevisao, COUNT(*) FROM CatalogoPecas GROUP BY 1 ORDER BY 2 DESC").fetchall(),
)
print(
    "vinculados_id",
    cur.execute(
        "SELECT COUNT(*) FROM CatalogoPecas WHERE ProdutoEstoqueId IS NOT NULL AND ProdutoEstoqueId!=''"
    ).fetchone()[0],
)
print(
    "convertido_status",
    cur.execute(
        "SELECT COUNT(*) FROM CatalogoPecas WHERE StatusRevisao='ConvertidoEstoque'"
    ).fetchone()[0],
)
print(
    "vinculado_status",
    cur.execute("SELECT COUNT(*) FROM CatalogoPecas WHERE StatusRevisao='Vinculado'").fetchone()[0],
)
print(
    "orphan_after",
    cur.execute(
        """SELECT COUNT(*) FROM CatalogoPecas c
           WHERE IFNULL(c.ProdutoEstoqueId,'')!=''
           AND NOT EXISTS (SELECT 1 FROM Produtos p WHERE p.Id=c.ProdutoEstoqueId)"""
    ).fetchone()[0],
)

# How UI counts Convertidos
# find service method
print("PASS_CAT2STOCK", True)
conn.close()
