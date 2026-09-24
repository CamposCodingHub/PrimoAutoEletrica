# -*- coding: utf-8 -*-
"""Expand Phase B: more vehicles (heavy+intl), include bearings, regenerate seed + relink."""
from pathlib import Path
import re, textwrap, sqlite3, uuid, shutil, unicodedata
from datetime import datetime

ROOT = Path(r"C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica")
SVC = ROOT / "Services" / "Catalogo" / "CatalogoVeiculoService.cs"
DB = Path(r"C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db")

# ---- Expanded fleet ----
leve = [
    ("Hyundai","HB20",2012,2019,"1.0","HB20,HB 20"),
    ("Hyundai","HB20",2012,2019,"1.6","HB20"),
    ("Hyundai","HB20",2020,2026,"1.0","HB20,HB20S"),
    ("Hyundai","HB20S",2013,2026,"1.6","HB20S"),
    ("Hyundai","Creta",2017,2026,"1.6","Creta"),
    ("Hyundai","Creta",2022,2026,"1.0 Turbo","Creta"),
    ("Hyundai","Tucson",2010,2026,"2.0","Tucson"),
    ("Hyundai","ix35",2010,2015,"2.0","ix35"),
    ("Hyundai","HR",2013,2026,"2.5","HR"),
    ("Volkswagen","Gol",2009,2023,"1.0","Gol,G5,G6,G7,G8"),
    ("Volkswagen","Gol",2009,2023,"1.6","Gol"),
    ("Volkswagen","Voyage",2009,2023,"1.6","Voyage"),
    ("Volkswagen","Polo",2018,2026,"1.0 TSI","Polo"),
    ("Volkswagen","Virtus",2018,2026,"1.6","Virtus"),
    ("Volkswagen","T-Cross",2019,2026,"1.0 TSI","T-Cross,TCross"),
    ("Volkswagen","Nivus",2020,2026,"1.0 TSI","Nivus"),
    ("Volkswagen","Saveiro",2010,2026,"1.6","Saveiro"),
    ("Volkswagen","Fox",2010,2021,"1.6","Fox"),
    ("Volkswagen","Up",2014,2021,"1.0","Up,up!"),
    ("Volkswagen","Jetta",2011,2018,"2.0","Jetta"),
    ("Volkswagen","Amarok",2010,2026,"2.0","Amarok"),
    ("Chevrolet","Onix",2013,2019,"1.0","Onix"),
    ("Chevrolet","Onix",2013,2019,"1.4","Onix"),
    ("Chevrolet","Onix",2020,2026,"1.0 Turbo","Onix,Onix Plus"),
    ("Chevrolet","Prisma",2013,2019,"1.4","Prisma"),
    ("Chevrolet","Tracker",2020,2026,"1.0 Turbo","Tracker"),
    ("Chevrolet","Spin",2013,2026,"1.8","Spin"),
    ("Chevrolet","S10",2012,2026,"2.8","S10,S-10"),
    ("Chevrolet","Montana",2011,2026,"1.2","Montana"),
    ("Chevrolet","Cruze",2012,2016,"1.8","Cruze"),
    ("Chevrolet","Cruze",2017,2022,"1.4 Turbo","Cruze"),
    ("Chevrolet","Cobalt",2012,2020,"1.8","Cobalt"),
    ("Fiat","Argo",2017,2026,"1.0","Argo"),
    ("Fiat","Argo",2017,2026,"1.3","Argo"),
    ("Fiat","Cronos",2018,2026,"1.3","Cronos"),
    ("Fiat","Mobi",2016,2026,"1.0","Mobi"),
    ("Fiat","Strada",2014,2026,"1.4","Strada"),
    ("Fiat","Toro",2016,2026,"1.8","Toro"),
    ("Fiat","Uno",2010,2021,"1.0","Uno"),
    ("Fiat","Palio",2010,2017,"1.0","Palio"),
    ("Fiat","Siena",2010,2016,"1.4","Siena"),
    ("Fiat","Pulse",2021,2026,"1.0 Turbo","Pulse"),
    ("Fiat","Fastback",2022,2026,"1.0 Turbo","Fastback"),
    ("Fiat","Fiorino",2014,2026,"1.4","Fiorino"),
    ("Fiat","Ducato",2010,2026,"2.3","Ducato"),
    ("Toyota","Corolla",2015,2019,"2.0","Corolla"),
    ("Toyota","Corolla",2020,2026,"1.8 Hybrid","Corolla"),
    ("Toyota","Hilux",2016,2026,"2.8","Hilux"),
    ("Toyota","Yaris",2018,2026,"1.5","Yaris"),
    ("Toyota","Etios",2013,2021,"1.5","Etios"),
    ("Toyota","SW4",2016,2026,"2.8","SW4,SW-4,Fortuner"),
    ("Honda","Civic",2012,2021,"2.0","Civic"),
    ("Honda","City",2015,2026,"1.5","City"),
    ("Honda","Fit",2015,2021,"1.5","Fit"),
    ("Honda","HR-V",2016,2026,"1.5","HR-V,HRV"),
    ("Honda","WR-V",2017,2023,"1.5","WR-V,WRV"),
    ("Honda","CR-V",2012,2026,"1.5","CR-V,CRV"),
    ("Ford","Ka",2014,2021,"1.0","Ka,New Ka"),
    ("Ford","Ka",2014,2021,"1.5","Ka"),
    ("Ford","EcoSport",2013,2021,"1.5","EcoSport,Ecosport"),
    ("Ford","Ranger",2013,2026,"2.2","Ranger"),
    ("Ford","Ranger",2013,2026,"3.2","Ranger"),
    ("Ford","Territory",2021,2026,"1.5","Territory"),
    ("Ford","Focus",2014,2019,"2.0","Focus"),
    ("Renault","Sandero",2015,2026,"1.0","Sandero"),
    ("Renault","Sandero",2015,2026,"1.6","Sandero"),
    ("Renault","Logan",2014,2026,"1.0","Logan"),
    ("Renault","Kwid",2017,2026,"1.0","Kwid"),
    ("Renault","Duster",2015,2026,"1.6","Duster"),
    ("Renault","Oroch",2016,2026,"1.6","Oroch"),
    ("Renault","Captur",2017,2022,"1.6","Captur"),
    ("Renault","Master",2014,2026,"2.3","Master"),
    ("Jeep","Renegade",2015,2026,"1.8","Renegade"),
    ("Jeep","Compass",2017,2026,"2.0","Compass"),
    ("Jeep","Commander",2021,2026,"1.3 Turbo","Commander"),
    ("Nissan","Kicks",2016,2026,"1.6","Kicks"),
    ("Nissan","Versa",2012,2026,"1.6","Versa"),
    ("Nissan","Frontier",2014,2026,"2.3","Frontier"),
    ("Nissan","March",2012,2020,"1.0","March"),
    ("Nissan","Sentra",2014,2022,"2.0","Sentra"),
    ("Peugeot","208",2014,2026,"1.6","208"),
    ("Peugeot","2008",2015,2026,"1.6","2008"),
    ("Peugeot","Boxer",2012,2026,"2.2","Boxer"),
    ("Citroen","C3",2013,2026,"1.2","C3"),
    ("Citroen","C4 Cactus",2018,2026,"1.6","C4 Cactus,Cactus"),
    ("Citroen","Jumper",2012,2026,"2.2","Jumper"),
    ("Mitsubishi","L200",2013,2026,"2.4","L200,Triton"),
    ("Mitsubishi","ASX",2011,2022,"2.0","ASX"),
    ("Mitsubishi","Pajero",2010,2021,"3.2","Pajero"),
    ("Chery","Tiggo 5X",2019,2026,"1.5","Tiggo 5X,Tiggo5X"),
    ("Chery","Arrizo 6",2020,2026,"1.5","Arrizo 6"),
    ("Caoa Chery","Tiggo 7",2019,2026,"1.5","Tiggo 7"),
    ("Caoa Chery","Tiggo 8",2021,2026,"1.6","Tiggo 8"),
    ("BYD","Dolphin",2023,2026,"Eletrico","Dolphin"),
    ("BYD","Yuan Plus",2023,2026,"Eletrico","Yuan Plus,Yuan"),
    ("BYD","Song Plus",2023,2026,"Hybrid","Song Plus"),
    ("Kia","Sportage",2011,2026,"2.0","Sportage"),
    ("Kia","Cerato",2010,2021,"1.6","Cerato"),
    ("Kia","Bongo",2012,2026,"2.5","Bongo"),
    ("Ram","Rampage",2023,2026,"2.0","Rampage"),
    ("Ram","2500",2012,2026,"6.7","2500,Ram 2500"),
]

# Heavy / commercial (linha pesada) — forte da oficina
pesada = [
    ("Volkswagen","Delivery",2010,2026,"4.5","Delivery,VW Delivery"),
    ("Volkswagen","Delivery Express",2018,2026,"2.3","Delivery Express"),
    ("Volkswagen","Worker",2008,2020,"Worker","Worker"),
    ("Volkswagen","Constellation",2010,2026,"17.280","Constellation,VW Constellation"),
    ("Volkswagen","Constellation",2010,2026,"24.280","Constellation"),
    ("Volkswagen","Meteor",2020,2026,"29.520","Meteor"),
    ("Mercedes-Benz","Accelo",2012,2026,"815","Accelo,MB Accelo"),
    ("Mercedes-Benz","Atego",2010,2026,"1719","Atego,MB Atego"),
    ("Mercedes-Benz","Atego",2010,2026,"2426","Atego"),
    ("Mercedes-Benz","Actros",2012,2026,"2651","Actros,MB Actros"),
    ("Mercedes-Benz","Axor",2010,2022,"2544","Axor"),
    ("Mercedes-Benz","Sprinter",2012,2026,"415","Sprinter"),
    ("Mercedes-Benz","Sprinter",2012,2026,"515","Sprinter"),
    ("Volvo","VM",2012,2026,"270","VM,Volvo VM"),
    ("Volvo","FH",2013,2026,"460","FH,Volvo FH"),
    ("Volvo","FH",2013,2026,"540","FH"),
    ("Volvo","FMX",2015,2026,"500","FMX"),
    ("Scania","P-Series",2012,2026,"P310","Scania P,P-Series"),
    ("Scania","G-Series",2012,2026,"G410","Scania G,G-Series"),
    ("Scania","R-Series",2012,2026,"R450","Scania R,R-Series"),
    ("Scania","S-Series",2018,2026,"S500","Scania S,S-Series"),
    ("Iveco","Daily",2012,2026,"35S14","Daily,Iveco Daily"),
    ("Iveco","Daily",2012,2026,"70C17","Daily"),
    ("Iveco","Tector",2012,2026,"9-190","Tector"),
    ("Iveco","Tector",2012,2026,"17-280","Tector"),
    ("Iveco","Hi-Way",2013,2026,"480","Hi-Way,HiWay,Hi Way"),
    ("Iveco","Hi-Road",2014,2026,"440","Hi-Road,HiRoad"),
    ("Ford","Cargo",2010,2026,"816","Cargo,Ford Cargo"),
    ("Ford","Cargo",2010,2026,"2429","Cargo"),
    ("Ford","Cargo",2015,2026,"2842","Cargo"),
    ("MAN","TGX",2014,2026,"29.480","TGX,MAN TGX"),
    ("MAN","TGS",2014,2026,"TGS","TGS"),
    ("DAF","XF",2015,2026,"XF105","DAF XF,XF"),
    ("DAF","CF",2015,2026,"CF85","DAF CF,CF"),
    ("Hino","500",2012,2026,"GH","Hino 500"),
    ("Isuzu","NPR",2012,2026,"NPR","NPR,Isuzu NPR"),
    ("Isuzu","FTR",2012,2026,"FTR","FTR"),
    ("Foton","Aumark",2015,2026,"3.5","Aumark"),
    ("Foton","Auman",2016,2026,"Auman","Auman"),
    ("JAC","V260",2018,2026,"2.0","V260"),
    ("JAC","N35",2019,2026,"N35","N35"),
    ("Shacman","X3000",2018,2026,"X3000","Shacman,X3000"),
    ("Sinotruk","Howo",2016,2026,"Howo","Howo,Sinotruk"),
    # Onibus / chassis
    ("Mercedes-Benz","OF-1721",2012,2026,"OF","OF-1721,OF 1721,chassis MB"),
    ("Mercedes-Benz","O-500",2012,2026,"O500","O-500,O500"),
    ("Volkswagen","17.230 OD",2014,2026,"17.230","17.230 OD,VW Onibus"),
    ("Volvo","B270F",2014,2026,"B270","B270F"),
    ("Agrale","MA 8.0",2015,2026,"MA","Agrale MA,MA 8.0"),
    ("Marcopolo","Torino",2012,2026,"Torino","Torino"),  # carroceria - util para busca
    ("Marcopolo","Ideale",2014,2026,"Ideale","Ideale"),
    ("Caio","Apache",2012,2026,"Apache","Apache Vip"),
]

# International light / utility extras
intl = [
    ("Toyota","Land Cruiser",2010,2026,"4.5","Land Cruiser,LandCruiser"),
    ("Toyota","Hiace",2012,2026,"2.8","Hiace"),
    ("Ford","Transit",2014,2026,"2.2","Transit"),
    ("Ford","F-350",2012,2026,"6.2","F-350,F350"),
    ("Ford","F-4000",2010,2026,"F4000","F-4000,F4000"),
    ("Chevrolet","Silverado",2014,2026,"5.3","Silverado"),
    ("Chevrolet","D-Max",2015,2026,"2.5","D-Max,DMax"),
    ("Nissan","Navara",2015,2026,"2.3","Navara"),
    ("Mitsubishi","Canter",2012,2026,"Canter","Canter"),
    ("Hyundai","HD78",2012,2026,"HD78","HD78,HD 78"),
    ("Hyundai","HD80",2014,2026,"HD80","HD80"),
    ("MAZDA","BT-50",2012,2022,"3.2","BT-50,BT50"),
    ("Suzuki","Jimny",2012,2026,"1.5","Jimny"),
    ("BMW","X1",2012,2026,"2.0","X1"),
    ("BMW","320i",2012,2026,"2.0","320i"),
    ("Audi","A3",2013,2026,"1.4","A3"),
    ("Audi","Q3",2013,2026,"1.4","Q3"),
    ("Mercedes-Benz","Classe A",2013,2026,"1.6","Classe A,A200"),
    ("Mercedes-Benz","Classe C",2012,2026,"1.6","Classe C,C180"),
]

def uniq(rows):
    seen=set(); out=[]
    for r in rows:
        k=(r[0].lower(), r[1].lower(), r[2], r[3], str(r[4]).lower())
        if k not in seen:
            seen.add(k); out.append(r)
    return out

vehicles = uniq(leve + pesada + intl)
print("total vehicles", len(vehicles))

# Rewrite SeedRows in C# service
text = SVC.read_text(encoding="utf-8")
rows_cs = ",\n".join(
    f'            new("{m}", "{mo}", {ai}, {af}, "{mot}", "{al}")' for m,mo,ai,af,mot,al in vehicles
)
# Replace SeedRows array body
pat = r"private static readonly SeedRow\[\] SeedRows =\s*\{.*?\n        \};"
repl = "private static readonly SeedRow[] SeedRows =\n        {\n" + rows_cs + "\n        };"
new_text, n = re.subn(pat, repl, text, count=1, flags=re.S)
if n != 1:
    raise SystemExit(f"SeedRows replace failed n={n}")

# Include bearings / SKF / IKRO in auto-eletrica linker (oficina troca rolamento de alternador)
old_excl = '''        private static readonly HashSet<string> MarcasPecaMecanica = new(StringComparer.OrdinalIgnoreCase)
        {
            "SKF", "IKRO"
        };

        private static readonly HashSet<string> CategoriasExcluidas = new(StringComparer.OrdinalIgnoreCase)
        {
            "Rolamentos", "Freios", "Pastilha", "Disco"
        };'''

new_excl = '''        // Oficina auto eletrica tambem troca rolamento de alternador/partida (SKF/IKRO).
        // Exclui apenas freio puro (pastilha/disco), nao rolamentos.
        private static readonly HashSet<string> MarcasPecaMecanica = new(StringComparer.OrdinalIgnoreCase)
        {
        };

        private static readonly HashSet<string> CategoriasExcluidas = new(StringComparer.OrdinalIgnoreCase)
        {
            "Freios", "Pastilha", "Disco"
        };'''

if old_excl not in new_text:
    # try already modified
    print("WARN: marcas mecanica block not exact - patching EhPecaAutoEletrica loosely")
else:
    new_text = new_text.replace(old_excl, new_excl)

# Soften name filter to allow rolamento
new_text = new_text.replace(
    'if (n.Contains("pastilha") || n.Contains("rolamento") || n.Contains("disco de freio"))',
    'if (n.Contains("pastilha") || n.Contains("disco de freio"))'
)

# Prefer SKF/IKRO too
new_text = new_text.replace(
    '"DNI", "BOSCH", "NGK", "UETA", "HELLA", "VALEO", "MAGNETI", "MAHLE", "GAUSS"',
    '"DNI", "BOSCH", "NGK", "UETA", "HELLA", "VALEO", "MAGNETI", "MAHLE", "GAUSS", "SKF", "IKRO"'
)

# Allow rolamento keyword
new_text = new_text.replace(
    'return n.Contains("rele") || n.Contains("sensor") || n.Contains("vela") || n.Contains("bobina") ||\n                   n.Contains("altern") || n.Contains("partida") || n.Contains("modulo") || n.Contains("chicote") ||\n                   n.Contains("farol") || n.Contains("lampada") || n.Contains("buzina") || n.Contains("injetor");',
    'return n.Contains("rele") || n.Contains("sensor") || n.Contains("vela") || n.Contains("bobina") ||\n                   n.Contains("altern") || n.Contains("partida") || n.Contains("modulo") || n.Contains("chicote") ||\n                   n.Contains("farol") || n.Contains("lampada") || n.Contains("buzina") || n.Contains("injetor") ||\n                   n.Contains("rolamento") || n.Contains("bearing");'
)

# Segmento for heavy
# Keep seed Segmento='Leve' for all in INSERT for simplicity OR set Pesado for pesada brands
# Update INSERT in SeedFrotaSeVazia to use 'Pesado' when marca in heavy list - optional later

SVC.write_text(new_text, encoding="utf-8")
print("CatalogoVeiculoService.cs updated")

# Also fix CatalogoPecasService SomenteAutoEletrica to keep SKF/IKRO/Rolamentos
pcs = (ROOT / "Services" / "Catalogo" / "CatalogoPecasService.cs").read_text(encoding="utf-8")
pcs2 = pcs.replace(
'''                if (filtro.SomenteAutoEletrica)
                {
                    resultado = resultado.Where(item =>
                        !string.Equals(item.Marca, "SKF", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(item.Marca, "IKRO", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(item.Categoria, "Rolamentos", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(item.Categoria, "Freios", StringComparison.OrdinalIgnoreCase));
                }''',
'''                if (filtro.SomenteAutoEletrica)
                {
                    // Mantem rolamentos (SKF/IKRO) — uso tipico: rolamento de alternador/partida.
                    resultado = resultado.Where(item =>
                        !string.Equals(item.Categoria, "Freios", StringComparison.OrdinalIgnoreCase));
                }'''
)
(ROOT / "Services" / "Catalogo" / "CatalogoPecasService.cs").write_text(pcs2, encoding="utf-8")
print("CatalogoPecasService filter updated")

# Force reseed DB: clear vehicles + links, insert all, relink including bearings
bak = DB.with_suffix(".db.bak.before-fase-b-expand")
if not bak.exists():
    shutil.copy2(DB, bak)

c = sqlite3.connect(str(DB))
cur = c.cursor()
cur.execute("DELETE FROM CatalogoPecaVeiculos")
cur.execute("DELETE FROM CatalogoVeiculos")
now = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
heavy_brands = {r[0] for r in pesada}
for m,mo,ai,af,mot,al in vehicles:
    seg = "Pesado" if m in heavy_brands or mo in {r[1] for r in pesada} else "Leve"
    # better: mark by membership in pesada list
    seg = "Pesado" if (m,mo,ai,af,mot,al) in pesada or any(m==x[0] and mo==x[1] for x in pesada) else "Leve"
    cur.execute("INSERT INTO CatalogoVeiculos VALUES (?,?,?,?,?,?,?,?,?,?)",
                (str(uuid.uuid4()), m, mo, ai, af, mot, al, seg, 1, now))
print("inserted", cur.execute("SELECT COUNT(*) FROM CatalogoVeiculos").fetchone()[0])
print("pesados", cur.execute("SELECT COUNT(*) FROM CatalogoVeiculos WHERE Segmento='Pesado'").fetchone()[0])

veiculos = list(cur.execute("SELECT Id, Marca, Modelo, AnoInicial, AnoFinal, Motor, Aliases FROM CatalogoVeiculos WHERE Ativo=1"))
pecas = list(cur.execute("SELECT Id, Marca, Nome, Descricao, Aplicacao, VeiculoAplicacao, Categoria, ObservacoesTecnicas FROM CatalogoPecas WHERE Ativo=1"))

pref = {"DNI","BOSCH","NGK","UETA","HELLA","VALEO","MAGNETI","MAHLE","GAUSS","SKF","IKRO"}
excl_cat = {"Freios","Pastilha","Disco"}

def norm(s):
    if not s: return " "
    s = unicodedata.normalize("NFD", s)
    s = "".join(ch for ch in s if unicodedata.category(ch) != "Mn")
    s = s.lower()
    s = re.sub(r"[^a-z0-9\s\-\.]", " ", s)
    s = re.sub(r"\s+", " ", s).strip()
    return f" {s} "

def is_auto(marca, cat, nome):
    if (cat or "") in excl_cat: return False
    n = (nome or "").lower()
    if "pastilha" in n or "disco de freio" in n: return False
    if (marca or "").upper() in pref: return True
    catl = (cat or "").lower()
    keys = ("sensor","rele","igni","inje","modulo","chave","ilumin","bateria","altern","partida","eletr","rolament")
    if any(k in catl for k in keys): return True
    return any(k in n for k in ("rele","sensor","vela","bobina","altern","partida","modulo","chicote","farol","lampada","buzina","injetor","rolamento","bearing"))

def tokens(v):
    yield norm(v[2])
    for a in (v[6] or "").split(","):
        a=a.strip()
        if a: yield norm(a)

batch=[]; auto=0
for p in pecas:
    pid, marca, nome, desc, apl, vap, cat, obs = p
    if not is_auto(marca, cat, nome):
        continue
    auto += 1
    texto = norm(f"{nome} {desc} {apl} {vap} {obs}")
    matched=[]
    for v in veiculos:
        if any(t.strip() and len(t.strip())>=3 and t in texto for t in tokens(v)):
            matched.append(v[0])
    if not matched:
        marcas_hit=set()
        for v in veiculos:
            m=norm(v[1])
            if m.strip() and m in texto:
                marcas_hit.add(v[1])
        if marcas_hit:
            matched=[v[0] for v in veiculos if v[1] in marcas_hit]
        elif (" uso geral" in texto) or ("universal" in texto) or ("auxiliares universais" in texto) or ("aplicacao geral" in texto) or ("rolamento" in texto):
            # rolamentos/universais: liga a frota inteira (leve+pesada) para busca por veiculo
            matched=[v[0] for v in veiculos]
    for vid in set(matched):
        batch.append((pid, vid, "auto-eletrica-linker-v2", now))
        if len(batch)>=3000:
            cur.executemany("INSERT OR IGNORE INTO CatalogoPecaVeiculos VALUES (?,?,?,?)", batch)
            batch.clear()
if batch:
    cur.executemany("INSERT OR IGNORE INTO CatalogoPecaVeiculos VALUES (?,?,?,?)", batch)
c.commit()
print("auto pecas", auto)
print("links", cur.execute("SELECT COUNT(*) FROM CatalogoPecaVeiculos").fetchone()[0])
print("HB20 2014", cur.execute("""
SELECT COUNT(DISTINCT p.Id) FROM CatalogoPecas p
JOIN CatalogoPecaVeiculos pv ON pv.CatalogoPecaId=p.Id
JOIN CatalogoVeiculos v ON v.Id=pv.CatalogoVeiculoId
WHERE v.Modelo LIKE '%HB20%' AND v.AnoInicial<=2014 AND v.AnoFinal>=2014""").fetchone()[0])
print("Actros rolamentos", cur.execute("""
SELECT COUNT(DISTINCT p.Id) FROM CatalogoPecas p
JOIN CatalogoPecaVeiculos pv ON pv.CatalogoPecaId=p.Id
JOIN CatalogoVeiculos v ON v.Id=pv.CatalogoVeiculoId
WHERE v.Modelo LIKE '%Actros%' AND (p.Marca IN ('SKF','IKRO') OR IFNULL(p.Categoria,'')='Rolamentos' OR lower(p.Nome) LIKE '%rolament%')
""").fetchone()[0])
print("marcas sample", [r[0] for r in cur.execute("SELECT DISTINCT Marca FROM CatalogoVeiculos ORDER BY Marca LIMIT 25")])
c.close()
print("DONE")
