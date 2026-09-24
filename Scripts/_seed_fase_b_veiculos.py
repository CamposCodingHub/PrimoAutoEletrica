import sqlite3, uuid, re, unicodedata, shutil
from datetime import datetime
from pathlib import Path

db = Path(r"C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db")
bak = db.with_suffix(".db.bak.before-fase-b")
if not bak.exists():
    shutil.copy2(db, bak)

c = sqlite3.connect(str(db))
cur = c.cursor()
cur.executescript("""
CREATE TABLE IF NOT EXISTS CatalogoVeiculos (
  Id TEXT PRIMARY KEY,
  Marca TEXT NOT NULL,
  Modelo TEXT NOT NULL,
  AnoInicial INTEGER NOT NULL,
  AnoFinal INTEGER NOT NULL,
  Motor TEXT,
  Aliases TEXT,
  Segmento TEXT,
  Ativo INTEGER NOT NULL DEFAULT 1,
  DataCriacao TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS CatalogoPecaVeiculos (
  CatalogoPecaId TEXT NOT NULL,
  CatalogoVeiculoId TEXT NOT NULL,
  Fonte TEXT,
  DataVinculo TEXT NOT NULL,
  PRIMARY KEY (CatalogoPecaId, CatalogoVeiculoId)
);
CREATE INDEX IF NOT EXISTS IDX_CatalogoVeiculos_MarcaModelo ON CatalogoVeiculos (Marca, Modelo);
CREATE INDEX IF NOT EXISTS IDX_CatalogoPecaVeiculos_Veiculo ON CatalogoPecaVeiculos (CatalogoVeiculoId);
""")
# schema migrations columns may vary
try:
    cur.execute("INSERT OR IGNORE INTO SchemaMigrations (Id, Name, AppliedAt) VALUES (?,?,datetime('now'))",
                ("202609190001", "Catalogo veiculos + vinculo N:N peca (auto eletrica)"))
except Exception as ex:
    print("schemaMigrations note", ex)
    try:
        cols = [r[1] for r in cur.execute("PRAGMA table_info(SchemaMigrations)")]
        print("SchemaMigrations cols", cols)
        if "Id" in cols:
            cur.execute("INSERT OR IGNORE INTO SchemaMigrations (Id) VALUES (?)", ("202609190001",))
    except Exception as ex2:
        print("skip migration row", ex2)

cs = Path(r"C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\Services\Catalogo\CatalogoVeiculoService.cs").read_text(encoding="utf-8")
pat = r'new\("([^"]+)", "([^"]+)", (\d+), (\d+), "([^"]*)", "([^"]*)"\)'
rows = re.findall(pat, cs)
print("seed rows", len(rows))

cur.execute("SELECT COUNT(*) FROM CatalogoVeiculos")
if cur.fetchone()[0] == 0:
    now = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    for marca, modelo, ai, af, motor, aliases in rows:
        cur.execute(
            "INSERT INTO CatalogoVeiculos VALUES (?,?,?,?,?,?,?,?,?,?)",
            (str(uuid.uuid4()), marca, modelo, int(ai), int(af), motor, aliases, "Leve", 1, now),
        )
    print("inserted vehicles", len(rows))
else:
    print("vehicles already", cur.execute("SELECT COUNT(*) FROM CatalogoVeiculos").fetchone()[0])

veiculos = list(cur.execute("SELECT Id, Marca, Modelo, AnoInicial, AnoFinal, Motor, Aliases FROM CatalogoVeiculos WHERE Ativo=1"))
pecas = list(cur.execute("SELECT Id, Marca, Nome, Descricao, Aplicacao, VeiculoAplicacao, Categoria, ObservacoesTecnicas FROM CatalogoPecas WHERE Ativo=1"))

mec = {"SKF", "IKRO"}
excl_cat = {"Rolamentos", "Freios", "Pastilha", "Disco"}
pref = {"DNI", "BOSCH", "NGK", "UETA", "HELLA", "VALEO", "MAGNETI", "MAHLE", "GAUSS"}

def norm(s):
    if not s:
        return " "
    s = unicodedata.normalize("NFD", s)
    s = "".join(ch for ch in s if unicodedata.category(ch) != "Mn")
    s = s.lower()
    s = re.sub(r"[^a-z0-9\s\-\.]", " ", s)
    s = re.sub(r"\s+", " ", s).strip()
    return f" {s} "

def is_auto(marca, cat, nome):
    if (marca or "").upper() in mec:
        return False
    if (cat or "") in excl_cat:
        return False
    n = (nome or "").lower()
    if "pastilha" in n or "rolamento" in n or "disco de freio" in n:
        return False
    if (marca or "").upper() in pref:
        return True
    catl = (cat or "").lower()
    keys = ("sensor", "rele", "igni", "inje", "modulo", "chave", "ilumin", "bateria", "altern", "partida", "eletr")
    if any(k in catl for k in keys):
        return True
    return any(k in n for k in ("rele", "sensor", "vela", "bobina", "altern", "partida", "modulo", "chicote", "farol", "lampada", "buzina", "injetor"))

def tokens(v):
    yield norm(v[2])
    for a in (v[6] or "").split(","):
        a = a.strip()
        if a:
            yield norm(a)

cur.execute("DELETE FROM CatalogoPecaVeiculos")
now = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
batch = []
auto_count = 0
for p in pecas:
    pid, marca, nome, desc, apl, vap, cat, obs = p
    if not is_auto(marca, cat, nome):
        continue
    auto_count += 1
    texto = norm(f"{nome} {desc} {apl} {vap} {obs}")
    matched = []
    for v in veiculos:
        if any(t.strip() and len(t.strip()) >= 3 and t in texto for t in tokens(v)):
            matched.append(v[0])
    if not matched:
        marcas_hit = set()
        for v in veiculos:
            m = norm(v[1])
            if m.strip() and m in texto:
                marcas_hit.add(v[1])
        if marcas_hit:
            matched = [v[0] for v in veiculos if v[1] in marcas_hit]
        elif (" uso geral" in texto) or ("universal" in texto) or ("auxiliares universais" in texto) or ("aplicacao geral" in texto):
            matched = [v[0] for v in veiculos]
    for vid in set(matched):
        batch.append((pid, vid, "auto-eletrica-linker", now))
        if len(batch) >= 2000:
            cur.executemany("INSERT OR IGNORE INTO CatalogoPecaVeiculos VALUES (?,?,?,?)", batch)
            batch.clear()

if batch:
    cur.executemany("INSERT OR IGNORE INTO CatalogoPecaVeiculos VALUES (?,?,?,?)", batch)

c.commit()
print("auto eletrica pecas", auto_count)
print("vehicles", cur.execute("SELECT COUNT(*) FROM CatalogoVeiculos").fetchone()[0])
print("links", cur.execute("SELECT COUNT(*) FROM CatalogoPecaVeiculos").fetchone()[0])
print("HB20 2014", cur.execute("""
SELECT COUNT(DISTINCT p.Id)
FROM CatalogoPecas p
JOIN CatalogoPecaVeiculos pv ON pv.CatalogoPecaId = p.Id
JOIN CatalogoVeiculos v ON v.Id = pv.CatalogoVeiculoId
WHERE v.Modelo LIKE '%HB20%' AND v.AnoInicial <= 2014 AND v.AnoFinal >= 2014
  AND upper(IFNULL(p.Marca,'')) NOT IN ('SKF','IKRO')
""").fetchone()[0])
c.close()
print("OK")
