# -*- coding: utf-8 -*-
"""Lote grande: farois, lanternas, lampadas, fios e auto eletrica (+1000) com fotos."""
from __future__ import annotations

import csv
import re
import sqlite3
import uuid
from datetime import datetime
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont
from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.platypus import Paragraph, SimpleDocTemplate, Spacer, Table, TableStyle

ROOT = Path(r"C:\Projetos\PrimoAutoEletrica")
DB = Path(r"C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db")
IMG_DIR = Path(r"C:\Users\campo\AppData\Local\PrimoAutoEletrica\Catalogo\Imagens\PrimoxLote2")
OUT_DIR = ROOT / "TestData" / "Catalogos"
DOWNLOADS = Path(r"C:\Users\campo\Downloads")
CSV_PATH = OUT_DIR / "Catalogo-Primox-Lote2-Iluminacao-Fios.csv"
PDF_PATH = OUT_DIR / "Catalogo-Primox-Lote2-Iluminacao-Fios.pdf"
PDF_DL = DOWNLOADS / "Catalogo-Primox-Lote2-Iluminacao-Fios.pdf"

NOW = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
FONTE = "Primox Lote2 Iluminacao Fios 2026-09"
ARQUIVO = CSV_PATH.name

BRAND_COLORS = {
    "PRIMOX": ((15, 76, 129), (255, 255, 255)),
    "DNI": ((180, 30, 30), (255, 255, 255)),
    "BOSCH": ((0, 90, 50), (255, 255, 255)),
    "OSRAM": ((255, 200, 0), (20, 20, 20)),
    "PHILIPS": ((0, 70, 160), (255, 255, 255)),
    "HELLA": ((200, 20, 20), (255, 255, 255)),
    "VALEO": ((0, 100, 160), (255, 255, 255)),
    "NGK": ((200, 30, 40), (255, 255, 255)),
    "UETA": ((40, 40, 40), (255, 200, 0)),
    "TYC": ((30, 30, 90), (255, 255, 255)),
    "MAGNETI": ((120, 0, 40), (255, 255, 255)),
}

try:
    FONT_B = ImageFont.truetype("arial.ttf", 18)
    FONT_S = ImageFont.truetype("arial.ttf", 13)
    FONT_T = ImageFont.truetype("arialbd.ttf", 28)
except OSError:
    FONT_B = FONT_S = FONT_T = ImageFont.load_default()

# thumb cache by (marca, icon)
_THUMB_CACHE: dict[tuple[str, str], Image.Image] = {}


def norm_code(code: str) -> str:
    return re.sub(r"[^A-Za-z0-9]", "", (code or "").upper())


def make_thumb(path: Path, marca: str, codigo: str, icon: str) -> None:
    key = (marca.upper(), icon)
    base = _THUMB_CACHE.get(key)
    bg, fg = BRAND_COLORS.get(marca.upper(), ((60, 60, 60), (255, 255, 255)))
    if base is None:
        img = Image.new("RGB", (200, 150), bg)
        draw = ImageDraw.Draw(img)
        draw.rectangle([0, 0, 200, 36], fill=(0, 0, 0))
        draw.text((8, 8), marca.upper()[:12], fill=fg, font=FONT_B)
        draw.ellipse([65, 48, 135, 118], fill=fg)
        bbox = draw.textbbox((0, 0), icon, font=FONT_T)
        tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
        draw.text((100 - tw / 2, 83 - th / 2 - 2), icon, fill=bg, font=FONT_T)
        _THUMB_CACHE[key] = img
        base = img
    img = base.copy()
    draw = ImageDraw.Draw(img)
    draw.rectangle([0, 118, 200, 150], fill=(0, 0, 0))
    code_show = codigo if len(codigo) <= 18 else codigo[:16] + ".."
    draw.text((8, 126), code_show, fill=fg, font=FONT_S)
    path.parent.mkdir(parents=True, exist_ok=True)
    img.save(path, "PNG", optimize=True)


def build_products() -> list[dict]:
    rows: list[dict] = []
    seg_pesado = "Pesado"
    seg_leve = "Leve"
    seg_ambos = "Ambos"

    # --- Farois (main + aux) ---
    farol_specs = [
        ("H4", "halogena", "12V"),
        ("H7", "halogena", "12V"),
        ("H1", "halogena", "12V"),
        ("H3", "halogena", "12V"),
        ("HB3", "halogena", "12V"),
        ("HB4", "halogena", "12V"),
        ("H11", "halogena", "12V"),
        ("LED-H4", "LED", "12V"),
        ("LED-H7", "LED", "12V"),
        ("LED-H11", "LED", "12V"),
        ("LED-HB3", "LED", "12V"),
        ("XENON-D2S", "xenon", "12V"),
        ("XENON-D3S", "xenon", "12V"),
        ("H4-24V", "halogena", "24V"),
        ("H7-24V", "halogena", "24V"),
        ("LED-24V", "LED", "24V"),
    ]
    sides = [("LD", "direito"), ("LE", "esquerdo"), ("PAR", "par")]
    brands_f = ["HELLA", "VALEO", "TYC", "PRIMOX", "DNI"]
    n = 0
    for brand in brands_f:
        for spec, tech, volt in farol_specs:
            for side, side_pt in sides:
                n += 1
                code = f"FAR-{brand[:3]}-{spec}-{side}-{n:04d}"
                seg = seg_pesado if "24V" in volt or "24V" in spec else seg_leve
                rows.append(dict(
                    codigo=code, marca=brand,
                    nome=f"Farol {spec} {side_pt}",
                    desc=f"Conjunto farol {tech} {volt} lado {side_pt}",
                    cat="Farois", sub=tech, volt=volt, amp="",
                    apl=f"Repos {spec} {volt}", seg=seg, icon="FAR",
                ))

    # Farol auxiliar / milha / trabalho
    for brand in ["PRIMOX", "DNI", "HELLA", "OSRAM"]:
        for i, (nome, volt, seg) in enumerate([
            ("Farol milha LED redondo", "12V", seg_leve),
            ("Farol milha LED quadrado", "12V", seg_leve),
            ("Farol auxiliar neblina", "12V", seg_leve),
            ("Farol trabalho LED 18W", "12V", seg_ambos),
            ("Farol trabalho LED 27W", "12/24V", seg_ambos),
            ("Farol trabalho LED 48W", "24V", seg_pesado),
            ("Barra LED 22 pol", "12/24V", seg_ambos),
            ("Barra LED 32 pol", "12/24V", seg_ambos),
            ("Farol busca magnetico", "12/24V", seg_ambos),
            ("Farol teto cabine", "24V", seg_pesado),
        ], start=1):
            code = f"FAUX-{brand[:3]}-{i:02d}-{volt.replace('/', '-')}"
            rows.append(dict(
                codigo=code, marca=brand, nome=nome,
                desc=f"{nome} {volt}", cat="Farois", sub="Auxiliar",
                volt=volt, amp="", apl="Utilitario / oficina", seg=seg, icon="AUX",
            ))

    # --- Lanternas ---
    lan_types = [
        ("traseira", "completa"), ("traseira LED", "LED"),
        ("pisca dianteiro", "pisca"), ("pisca lateral", "pisca"),
        ("luz placa", "placa"), ("luz re", "re"),
        ("luz freio", "freio"), ("luz neblina traseira", "neblina"),
        ("lanterna carreta", "carreta"), ("lanterna LED oval", "LED"),
        ("lanterna LED redonda", "LED"), ("lanterna galeria", "galeria"),
    ]
    brands_l = ["HELLA", "PRIMOX", "DNI", "TYC", "VALEO"]
    for brand in brands_l:
        for i, (nome_base, sub) in enumerate(lan_types, start=1):
            for side, side_pt in [("LD", "direita"), ("LE", "esquerda"), ("UNI", "universal")]:
                for volt, seg in [("12V", seg_leve), ("24V", seg_pesado)]:
                    code = f"LAN-{brand[:3]}-{i:02d}-{side}-{volt}"
                    rows.append(dict(
                        codigo=code, marca=brand,
                        nome=f"Lanterna {nome_base} {side_pt}",
                        desc=f"Lanterna {nome_base} {volt} {side_pt}",
                        cat="Lanternas", sub=sub, volt=volt, amp="",
                        apl=f"Sinalizacao {volt}", seg=seg, icon="LAN",
                    ))

    # --- Lampadas ---
    lamps = [
        ("H1", "55W", "12V"), ("H3", "55W", "12V"), ("H4", "60/55W", "12V"),
        ("H7", "55W", "12V"), ("H8", "35W", "12V"), ("H9", "65W", "12V"),
        ("H11", "55W", "12V"), ("HB3", "60W", "12V"), ("HB4", "51W", "12V"),
        ("H4", "75/70W", "24V"), ("H7", "70W", "24V"),
        ("P21W", "21W", "12V"), ("P21/5W", "21/5W", "12V"), ("R5W", "5W", "12V"),
        ("R10W", "10W", "12V"), ("T10", "5W", "12V"), ("T20", "21W", "12V"),
        ("W5W", "5W", "12V"), ("WY5W", "5W", "12V"), ("C5W", "5W", "12V"),
        ("PY21W", "21W", "12V"), ("P21W", "21W", "24V"), ("R5W", "5W", "24V"),
        ("LED-T10", "1.5W", "12V"), ("LED-T20", "3W", "12V"), ("LED-P21W", "3W", "12V"),
        ("LED-H4", "40W", "12V"), ("LED-H7", "40W", "12V"), ("LED-H11", "40W", "12V"),
        ("LED-H4", "40W", "24V"), ("LED-T10", "1.5W", "24V"),
        ("D2S", "35W", "12V"), ("D3S", "35W", "12V"), ("D4S", "35W", "12V"),
    ]
    brands_lp = ["OSRAM", "PHILIPS", "PRIMOX", "DNI", "BOSCH"]
    for brand in brands_lp:
        for i, (tipo, pot, volt) in enumerate(lamps, start=1):
            seg = seg_pesado if volt == "24V" else seg_leve
            code = f"LMP-{brand[:3]}-{tipo}-{volt}-{i:02d}"
            rows.append(dict(
                codigo=code, marca=brand,
                nome=f"Lampada {tipo} {pot}",
                desc=f"Lampada {tipo} {pot} {volt}",
                cat="Lampadas", sub=tipo.split("-")[0], volt=volt, amp=pot,
                apl=f"Iluminacao {tipo}", seg=seg, icon="LMP",
            ))

    # --- Fios / cabos ---
    sections = ["0.5", "0.75", "1.0", "1.5", "2.5", "4.0", "6.0", "10", "16", "25", "35"]
    colors_w = ["preto", "vermelho", "amarelo", "azul", "verde", "branco", "marrom", "laranja"]
    brands_w = ["PRIMOX", "DNI", "UETA"]
    for brand in brands_w:
        for sec in sections:
            for col in colors_w:
                code = f"FIO-{brand[:3]}-{sec.replace('.', '')}-{col[:3].upper()}"
                rows.append(dict(
                    codigo=code, marca=brand,
                    nome=f"Fio automotivo {sec}mm {col}",
                    desc=f"Rolo 100m fio {sec}mm2 cor {col}",
                    cat="Fios", sub=f"{sec}mm", volt="12/24V", amp="",
                    apl="Instalacao eletrica", seg=seg_ambos, icon="FIO",
                ))
        for sec in ["16", "25", "35", "50"]:
            code = f"CAB-{brand[:3]}-BAT-{sec}"
            rows.append(dict(
                codigo=code, marca=brand,
                nome=f"Cabo bateria {sec}mm2",
                desc=f"Cabo partida/bateria {sec}mm2",
                cat="Fios", sub="Bateria", volt="12/24V", amp="",
                apl="Partida / terra", seg=seg_ambos, icon="CAB",
            ))

    # --- Chicotes / conectores / terminais ---
    for brand in ["PRIMOX", "DNI", "UETA"]:
        for i, nome in enumerate([
            "Chicote alternador universal", "Chicote partida universal",
            "Chicote farol H4", "Chicote farol H7", "Chicote lanterna traseira",
            "Chicote sensor ABS", "Chicote injecao 4 vias", "Chicote injecao 6 vias",
            "Chicote bomba combustivel", "Chicote ar condicionado",
            "Extensao OBD2 1.5m", "Cabo diagnostico J1939",
            "Chicote camera re", "Chicote som / multimídia",
            "Reparo chicote porta", "Reparo chicote painel",
        ], start=1):
            for volt, seg in [("12V", seg_leve), ("24V", seg_pesado)]:
                code = f"CHI-{brand[:3]}-{i:02d}-{volt}"
                rows.append(dict(
                    codigo=code, marca=brand, nome=f"{nome} {volt}",
                    desc=nome, cat="Chicotes", sub="Reparo", volt=volt, amp="",
                    apl="Eletrica veicular", seg=seg, icon="CHI",
                ))

        for i, nome in enumerate([
            "Terminal olhal M6", "Terminal olhal M8", "Terminal olhal M10",
            "Terminal faston 6.3 macho", "Terminal faston 6.3 femea",
            "Terminal faston 4.8", "Terminal bala macho", "Terminal bala femea",
            "Terminal bateria positivo", "Terminal bateria negativo",
            "Emenda termorretratil kit", "Luva isolante kit",
        ], start=1):
            code = f"TER-{brand[:3]}-{i:02d}"
            rows.append(dict(
                codigo=code, marca=brand, nome=nome,
                desc=f"{nome} pacote", cat="Terminais", sub="Conexao",
                volt="", amp="", apl="Instalacao", seg=seg_ambos, icon="TER",
            ))

        for i, nome in enumerate([
            "Conector Deutsch 2 vias", "Conector Deutsch 4 vias", "Conector Deutsch 6 vias",
            "Conector Weather Pack 2", "Conector Weather Pack 4",
            "Conector Superseal 2", "Conector Superseal 4",
            "Conector Metri-Pack 2", "Conector Metri-Pack 4",
            "Conector sensor oxigenio", "Conector farol H4", "Conector farol H7",
        ], start=1):
            code = f"CON-{brand[:3]}-{i:02d}"
            rows.append(dict(
                codigo=code, marca=brand, nome=nome,
                desc=f"{nome} par", cat="Conectores", sub="Selado",
                volt="", amp="", apl="Motor bay / chicote", seg=seg_ambos, icon="CON",
            ))

    # --- Reles / fusiveis / interruptores extras ---
    for brand in ["PRIMOX", "DNI", "BOSCH", "UETA"]:
        for amp in [20, 30, 40, 50, 70]:
            for volt, seg in [("12V", seg_leve), ("24V", seg_pesado)]:
                code = f"REL-{brand[:3]}-{amp}A-{volt}"
                rows.append(dict(
                    codigo=code, marca=brand,
                    nome=f"Rele auxiliar {amp}A {volt}",
                    desc=f"Rele 5 pinos {amp}A", cat="Reles", sub="Auxiliar",
                    volt=volt, amp=f"{amp}A", apl="Uso geral", seg=seg, icon="REL",
                ))
        for amp in [5, 7.5, 10, 15, 20, 25, 30, 35, 40]:
            code = f"FUS-{brand[:3]}-LAM-{str(amp).replace('.', '')}"
            rows.append(dict(
                codigo=code, marca=brand,
                nome=f"Fusivel lamina {amp}A",
                desc="Kit 10 unidades", cat="Fusiveis", sub="Lamina",
                volt="12/24V", amp=f"{amp}A", apl="Caixa fusivel", seg=seg_ambos, icon="FUS",
            ))
        for amp in [30, 40, 50, 60, 80, 100]:
            code = f"FUS-{brand[:3]}-MIDI-{amp}"
            rows.append(dict(
                codigo=code, marca=brand,
                nome=f"Fusivel MIDI {amp}A",
                desc="Fusivel MIDI", cat="Fusiveis", sub="MIDI",
                volt="12/24V", amp=f"{amp}A", apl="Linha pesada / alta carga", seg=seg_ambos, icon="FUS",
            ))

    # --- Sensores / ignicao / iluminacao interna extras ---
    sens = [
        ("Sensor temperatura agua", "Temperatura", "5V"),
        ("Sensor temperatura ar", "IAT", "5V"),
        ("Sensor pressao oleo", "Oleo", "12V"),
        ("Sensor nivel combustivel", "Combustivel", "12V"),
        ("Sensor ABS dianteiro", "ABS", "5V"),
        ("Sensor ABS traseiro", "ABS", "5V"),
        ("Sensor rotacao CKP", "CKP", "5V"),
        ("Sensor fase CMP", "CMP", "5V"),
        ("Sensor MAP", "MAP", "5V"),
        ("Sensor MAF", "MAF", "5V"),
        ("Sensor TPS", "TPS", "5V"),
        ("Sensor detonacao", "Knock", "5V"),
        ("Sonda lambda pre", "O2", "1V"),
        ("Sonda lambda pos", "O2", "1V"),
        ("Sensor NOx", "NOx", "24V"),
        ("Sensor ARLA nivel", "ARLA", "24V"),
        ("Sensor velocidade", "VSS", "12V"),
        ("Sensor chuva / luz", "Conforto", "12V"),
    ]
    for brand in ["PRIMOX", "DNI", "BOSCH", "NGK", "MAGNETI"]:
        for i, (nome, sub, volt) in enumerate(sens, start=1):
            seg = seg_pesado if volt == "24V" or sub in ("NOx", "ARLA") else seg_leve
            code = f"SEN-{brand[:3]}-{sub[:3].upper()}-{i:02d}"
            rows.append(dict(
                codigo=code, marca=brand, nome=nome,
                desc=f"{nome} {volt}", cat="Sensores", sub=sub,
                volt=volt, amp="", apl=sub, seg=seg, icon="SEN",
            ))

    # Deduplicate by codigo
    seen = set()
    uniq = []
    for r in rows:
        c = r["codigo"]
        if c in seen:
            continue
        seen.add(c)
        uniq.append(r)
    return uniq


def vehicle_ids(cur, segmento: str) -> list[str]:
    if segmento == "Pesado":
        q = """SELECT Id FROM CatalogoVeiculos WHERE Ativo=1 AND (
            Segmento='Pesado' OR Marca IN ('Mercedes-Benz','Volvo','Scania','Iveco','MAN','DAF','Hino','Foton','Shacman','Sinotruk','Agrale','Marcopolo','Caio'))"""
    elif segmento == "Leve":
        q = """SELECT Id FROM CatalogoVeiculos WHERE Ativo=1 AND IFNULL(Segmento,'Leve')!='Pesado'
               AND Marca NOT IN ('Mercedes-Benz','Volvo','Scania','Iveco','MAN','DAF','Hino','Foton','Shacman','Sinotruk','Agrale','Marcopolo','Caio') LIMIT 60"""
    else:
        q = "SELECT Id FROM CatalogoVeiculos WHERE Ativo=1 LIMIT 40"
    return [r[0] for r in cur.execute(q).fetchall()]


def main() -> None:
    products = build_products()
    print(f"products built: {len(products)}")
    IMG_DIR.mkdir(parents=True, exist_ok=True)
    OUT_DIR.mkdir(parents=True, exist_ok=True)

    out_rows = []
    for r in products:
        safe = re.sub(r"[^A-Za-z0-9_-]+", "_", r["codigo"])
        img = IMG_DIR / f"{r['marca']}_{safe}.png"
        make_thumb(img, r["marca"], r["codigo"], r["icon"])
        out_rows.append({
            "CodigoFabricante": r["codigo"],
            "Nome": r["nome"],
            "Descricao": r["desc"],
            "Categoria": r["cat"],
            "Subcategoria": r["sub"],
            "Marca": r["marca"],
            "Voltagem": r["volt"],
            "Amperagem": r["amp"],
            "Aplicacao": r["apl"],
            "ImagemLocal": str(img),
            "Segmento": r["seg"],
        })

    # CSV
    fields = list(out_rows[0].keys())
    fields = [f for f in fields if f != "Segmento"] + ["Segmento"]
    with CSV_PATH.open("w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=fields, delimiter=";")
        w.writeheader()
        w.writerows(out_rows)

    # PDF resumido (sem fotos, por categoria)
    styles = getSampleStyleSheet()
    title = ParagraphStyle("T", parent=styles["Heading1"], fontSize=14, textColor=colors.HexColor("#0F4C81"))
    body = ParagraphStyle("B", parent=styles["Normal"], fontSize=8, leading=10)
    story = [
        Paragraph("Catalogo Primox Lote 2 — Iluminacao, Fios e Auto Eletrica", title),
        Paragraph(f"{len(out_rows)} itens · gerado {NOW} · fotos em Catalogo/Imagens/PrimoxLote2", body),
        Spacer(1, 8),
    ]
    by_cat: dict[str, int] = {}
    for r in out_rows:
        by_cat[r["Categoria"]] = by_cat.get(r["Categoria"], 0) + 1
    cat_data = [["Categoria", "Qtd"]] + [[k, str(v)] for k, v in sorted(by_cat.items())]
    t = Table(cat_data, colWidths=[80 * mm, 30 * mm])
    t.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#0F4C81")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
        ("GRID", (0, 0), (-1, -1), 0.3, colors.grey),
        ("FONTSIZE", (0, 0), (-1, -1), 9),
    ]))
    story.append(t)
    story.append(Spacer(1, 10))
    story.append(Paragraph("Amostra (primeiros 40 itens):", body))
    sample = [["Codigo", "Marca", "Nome", "Cat.", "V"]]
    for r in out_rows[:40]:
        sample.append([r["CodigoFabricante"][:28], r["Marca"], r["Nome"][:36], r["Categoria"], r["Voltagem"] or "-"])
    t2 = Table(sample, colWidths=[45 * mm, 22 * mm, 55 * mm, 25 * mm, 15 * mm])
    t2.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#0F4C81")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
        ("GRID", (0, 0), (-1, -1), 0.25, colors.grey),
        ("FONTSIZE", (0, 0), (-1, -1), 7),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, colors.HexColor("#F5F8FC")]),
    ]))
    story.append(t2)
    SimpleDocTemplate(str(PDF_PATH), pagesize=A4, leftMargin=12 * mm, rightMargin=12 * mm, topMargin=12 * mm, bottomMargin=12 * mm).build(story)
    PDF_DL.write_bytes(PDF_PATH.read_bytes())

    # DB import
    conn = sqlite3.connect(str(DB))
    cur = conn.cursor()
    ins = upd = links = 0
    link_batch = []
    for r in out_rows:
        marca = r["Marca"]
        codigo = r["CodigoFabricante"]
        n = norm_code(codigo)
        existing = cur.execute(
            "SELECT Id FROM CatalogoPecas WHERE CodigoNormalizado=? AND Marca=? COLLATE NOCASE",
            (n, marca),
        ).fetchone()
        if existing:
            pid = existing[0]
            cur.execute(
                """UPDATE CatalogoPecas SET Nome=?, Descricao=?, Categoria=?, Subcategoria=?,
                   Aplicacao=?, Voltagem=?, Amperagem=?, ImagemLocal=?, ImagemUrl=?,
                   FonteCatalogo=?, ArquivoOrigem=?, Linha='Auto Eletrica', StatusRevisao='Importado',
                   DataAtualizacao=?, Ativo=1 WHERE Id=?""",
                (r["Nome"], r["Descricao"], r["Categoria"], r["Subcategoria"], r["Aplicacao"],
                 r["Voltagem"], r["Amperagem"], r["ImagemLocal"], r["ImagemLocal"],
                 FONTE, ARQUIVO, NOW, pid),
            )
            upd += 1
        else:
            pid = str(uuid.uuid4())
            cur.execute(
                """INSERT INTO CatalogoPecas
                (Id, CodigoFabricante, CodigoNormalizado, Marca, Nome, Descricao, Categoria, Subcategoria, Linha,
                 Aplicacao, VeiculoAplicacao, AnoInicial, AnoFinal, Voltagem, Amperagem, QuantidadeTerminais, TipoProduto,
                 PaginaCatalogo, FonteCatalogo, ArquivoOrigem, ObservacoesTecnicas, ImagemUrl, ImagemLocal,
                 StatusRevisao, ProdutoEstoqueId, DataImportacao, DataAtualizacao, Ativo)
                VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)""",
                (pid, codigo, n, marca, r["Nome"], r["Descricao"], r["Categoria"], r["Subcategoria"],
                 "Auto Eletrica", r["Aplicacao"], "", None, None, r["Voltagem"], r["Amperagem"], "",
                 r["Categoria"], "", FONTE, ARQUIVO, "Lote2 com foto", r["ImagemLocal"], r["ImagemLocal"],
                 "Importado", None, NOW, NOW, 1),
            )
            ins += 1

        vids = vehicle_ids(cur, r["Segmento"])
        cap = 20 if r["Segmento"] == "Pesado" else (15 if r["Segmento"] == "Leve" else 12)
        for vid in vids[:cap]:
            link_batch.append((pid, vid, FONTE, NOW))
        if len(link_batch) >= 2000:
            cur.executemany(
                "INSERT OR IGNORE INTO CatalogoPecaVeiculos (CatalogoPecaId, CatalogoVeiculoId, Fonte, DataVinculo) VALUES (?,?,?,?)",
                link_batch,
            )
            links += len(link_batch)
            link_batch.clear()

    if link_batch:
        cur.executemany(
            "INSERT OR IGNORE INTO CatalogoPecaVeiculos (CatalogoPecaId, CatalogoVeiculoId, Fonte, DataVinculo) VALUES (?,?,?,?)",
            link_batch,
        )
        links += len(link_batch)

    conn.commit()
    total = cur.execute("SELECT COUNT(*) FROM CatalogoPecas").fetchone()[0]
    fotos = cur.execute("SELECT COUNT(*) FROM CatalogoPecas WHERE IFNULL(ImagemLocal,'')!=''").fetchone()[0]
    by = cur.execute(
        "SELECT Categoria, COUNT(*) FROM CatalogoPecas WHERE FonteCatalogo=? GROUP BY 1 ORDER BY 2 DESC",
        (FONTE,),
    ).fetchall()
    conn.close()
    print(f"insert={ins} update={upd} link_attempts={links}")
    print(f"total={total} fotos={fotos}")
    print("por categoria fonte:", by)
    print("CSV", CSV_PATH)
    print("PDF", PDF_DL)
    print("imgs", len(list(IMG_DIR.glob('*.png'))))
    print("DONE")


if __name__ == "__main__":
    main()
