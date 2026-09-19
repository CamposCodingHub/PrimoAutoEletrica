# -*- coding: utf-8 -*-
"""Gera catálogo auto-elétrica Primox com fotos PNG, PDF, CSV e importa no DB."""
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
from reportlab.platypus import Image as RLImage
from reportlab.platypus import Paragraph, SimpleDocTemplate, Spacer, Table, TableStyle

ROOT = Path(r"C:\Projetos\PrimoAutoEletrica")
DB = Path(r"C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db")
IMG_DIR = Path(r"C:\Users\campo\AppData\Local\PrimoAutoEletrica\Catalogo\Imagens\PrimoxAE")
OUT_DIR = ROOT / "TestData" / "Catalogos"
DOWNLOADS = Path(r"C:\Users\campo\Downloads")
CSV_PATH = OUT_DIR / "Catalogo-Primox-AutoEletrica.csv"
PDF_PATH = OUT_DIR / "Catalogo-Primox-AutoEletrica.pdf"
PDF_DL = DOWNLOADS / "Catalogo-Primox-AutoEletrica.pdf"

NOW = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
FONTE = "Primox Auto Eletrica Seed 2026-09"
ARQUIVO = "Catalogo-Primox-AutoEletrica.csv"

# (codigo, marca, nome, descricao, categoria, subcategoria, voltagem, amperagem, aplicacao, segmento_alvo)
# segmento_alvo: Pesado | Leve | Ambos
PRODUCTS = [
    # Alternadores / reguladores
    ("PX-ALT-12-90", "PRIMOX", "Alternador 12V 90A", "Alternador regenerado linha leve", "Alternadores", "12V", "12V", "90A", "HB20 Onix Gol Ka Argo", "Leve"),
    ("PX-ALT-12-120", "PRIMOX", "Alternador 12V 120A", "Alternador regenerado pickup/SUV", "Alternadores", "12V", "12V", "120A", "Hilux S10 Ranger Amarok", "Leve"),
    ("PX-ALT-24-80", "PRIMOX", "Alternador 24V 80A", "Alternador linha pesada 24V", "Alternadores", "24V", "24V", "80A", "Actros FH Scania R Daily Constellation", "Pesado"),
    ("PX-ALT-24-110", "PRIMOX", "Alternador 24V 110A", "Alternador pesado alta carga", "Alternadores", "24V", "24V", "110A", "Actros FH Scania P/R Atego", "Pesado"),
    ("PX-REG-12", "PRIMOX", "Regulador de Voltagem 12V", "Regulador eletrico 12V", "Reguladores", "12V", "12V", "", "Linha leve geral", "Leve"),
    ("PX-REG-24", "PRIMOX", "Regulador de Voltagem 24V", "Regulador eletrico 24V", "Reguladores", "24V", "24V", "", "Linha pesada geral", "Pesado"),
    # Partidas
    ("PX-PRT-12-1.4", "PRIMOX", "Motor de Partida 12V 1.4kW", "Partida regenerada leve", "Partidas", "12V", "12V", "", "Gol Onix HB20 Fiesta", "Leve"),
    ("PX-PRT-12-2.0", "PRIMOX", "Motor de Partida 12V 2.0kW", "Partida pickup", "Partidas", "12V", "12V", "", "Hilux S10 Ranger", "Leve"),
    ("PX-PRT-24-4.5", "PRIMOX", "Motor de Partida 24V 4.5kW", "Partida pesada", "Partidas", "24V", "24V", "", "Actros FH Scania Daily", "Pesado"),
    ("PX-PRT-24-6.0", "PRIMOX", "Motor de Partida 24V 6.0kW", "Partida pesada alta potencia", "Partidas", "24V", "24V", "", "Actros Axor FH FM", "Pesado"),
    # BOSCH
    ("0 986 046 340", "BOSCH", "Alternador Bosch 12V", "Alternador OE equivalente", "Alternadores", "OE", "12V", "120A", "VW Gol Fox Saveiro", "Leve"),
    ("0 986 014 280", "BOSCH", "Motor de Partida Bosch", "Partida OE equivalente", "Partidas", "OE", "12V", "", "Fiat Palio Uno Argo", "Leve"),
    ("0 986 AG0 506", "BOSCH", "Sensor ABS Bosch", "Sensor de velocidade roda", "Sensores", "ABS", "5V", "", "Gol Voyage Saveiro", "Leve"),
    ("0 281 002 209", "BOSCH", "Sensor MAP Bosch", "Sensor pressao coletor", "Sensores", "MAP", "5V", "", "Flex line VW Fiat", "Leve"),
    ("0 261 230 266", "BOSCH", "Sensor de Rotacao", "Sensor CKP", "Sensores", "CKP", "5V", "", "Motor Fire Evo", "Leve"),
    ("0 280 158 107", "BOSCH", "Bobina de Ignicao", "Bobina stick", "Ignicao", "Bobinas", "12V", "", "Flex 1.0 1.4", "Leve"),
    ("1 987 302 053", "BOSCH", "Rele Auxiliar 12V 40A", "Rele 5 pinos", "Reles", "Auxiliar", "12V", "40A", "Uso geral 12V", "Leve"),
    ("1 987 302 054", "BOSCH", "Rele Auxiliar 24V 40A", "Rele 5 pinos 24V", "Reles", "Auxiliar", "24V", "40A", "Linha pesada", "Pesado"),
    # DNI
    ("DNI 0410", "DNI", "Rele Auxiliar 5 Terminais", "Rele 40A uso geral", "Reles", "Auxiliar", "12V", "40A", "Automotivo geral", "Ambos"),
    ("DNI 0411", "DNI", "Rele Auxiliar 4 Terminais", "Rele 30A", "Reles", "Auxiliar", "12V", "30A", "Automotivo geral", "Ambos"),
    ("DNI 7524", "DNI", "Sensor Temperatura Agua", "Sensor refrigerante", "Sensores", "Temperatura", "5V", "", "Motor gasolina/flex", "Leve"),
    ("DNI 8110", "DNI", "Interruptor Freio", "Pedal de freio", "Interruptores", "Freio", "12V", "", "Linha leve", "Leve"),
    ("DNI 0711", "DNI", "Chave de Luz", "Chave combinada farol/pisca", "Chaves", "Coluna", "12V", "", "Linha leve", "Leve"),
    ("DNI 2042", "DNI", "Luz Advertencia Portatil", "Lanterna advertencia 12V", "Iluminacao", "Auxiliar", "12V", "", "Uso geral", "Ambos"),
    ("DNI 0888", "DNI", "Botao Vidro Eletrico", "Interruptor vidro", "Interruptores", "Vidro", "12V", "", "Portas dianteiras", "Leve"),
    ("DNI 1501", "DNI", "Sensor Oxigenio Pre", "Sonda lambda pre-catalisador", "Sensores", "O2", "1V", "", "Flex line", "Leve"),
    ("DNI 1502", "DNI", "Sensor Oxigenio Pos", "Sonda lambda pos-catalisador", "Sensores", "O2", "1V", "", "Flex line", "Leve"),
    ("DNI 2201", "DNI", "Modulo Vidro Eletrico", "Modulo 2 portas", "Modulos", "Conforto", "12V", "", "Linha leve", "Leve"),
    ("DNI 3010", "DNI", "Farol Auxiliar LED", "Farol milha LED", "Iluminacao", "LED", "12V", "", "Pickup SUV", "Leve"),
    ("DNI 3011", "DNI", "Farol Trabalho LED 24V", "Farol trabalho linha pesada", "Iluminacao", "LED", "24V", "", "Caminhao onibus", "Pesado"),
    ("DNI 4100", "DNI", "Chicote Alternador", "Chicote reparo alternador", "Chicotes", "Alternador", "12V", "", "Reparos gerais", "Ambos"),
    ("DNI 4101", "DNI", "Chicote Partida", "Chicote reparo motor partida", "Chicotes", "Partida", "12/24V", "", "Reparos gerais", "Ambos"),
    ("DNI 5500", "DNI", "Fusivel Lamina 10A", "Kit 10 un", "Fusiveis", "Lamina", "12/24V", "10A", "Uso geral", "Ambos"),
    ("DNI 5501", "DNI", "Fusivel Lamina 15A", "Kit 10 un", "Fusiveis", "Lamina", "12/24V", "15A", "Uso geral", "Ambos"),
    ("DNI 5502", "DNI", "Fusivel Lamina 20A", "Kit 10 un", "Fusiveis", "Lamina", "12/24V", "20A", "Uso geral", "Ambos"),
    ("DNI 5503", "DNI", "Fusivel Lamina 30A", "Kit 10 un", "Fusiveis", "Lamina", "12/24V", "30A", "Uso geral", "Ambos"),
    ("DNI 5600", "DNI", "Fusivel MIDI 40A", "Fusivel MIDI", "Fusiveis", "MIDI", "12/24V", "40A", "Linha pesada", "Pesado"),
    ("DNI 5601", "DNI", "Fusivel MIDI 60A", "Fusivel MIDI", "Fusiveis", "MIDI", "12/24V", "60A", "Linha pesada", "Pesado"),
    ("DNI 7001", "DNI", "Sensor Pressao Oleo", "Interruptor oleo", "Sensores", "Oleo", "12V", "", "Motor geral", "Ambos"),
    ("DNI 7002", "DNI", "Sensor Nivel Combustivel", "Boia eletrica", "Sensores", "Combustivel", "12V", "", "Tanque metalico", "Leve"),
    # NGK
    ("BKR6E", "NGK", "Vela Ignicao BKR6E", "Vela cobre", "Ignicao", "Velas", "", "", "Flex 1.0-1.6", "Leve"),
    ("BKR5E", "NGK", "Vela Ignicao BKR5E", "Vela cobre", "Ignicao", "Velas", "", "", "Flex antigo", "Leve"),
    ("ILZKR7B11", "NGK", "Vela Iridium ILZKR7B11", "Vela iridium longa vida", "Ignicao", "Velas", "", "", "Honda Toyota", "Leve"),
    ("LZKR6B-10E", "NGK", "Vela Laser Platinum", "Vela platinum", "Ignicao", "Velas", "", "", "Hyundai Kia", "Leve"),
    ("DILKR7A11", "NGK", "Vela Iridium DILKR7A11", "Vela iridium", "Ignicao", "Velas", "", "", "Nissan Flex", "Leve"),
    ("U1579", "NGK", "Bobina NGK U1579", "Bobina stick", "Ignicao", "Bobinas", "12V", "", "Honda Fit City", "Leve"),
    ("U5021", "NGK", "Bobina NGK U5021", "Bobina stick", "Ignicao", "Bobinas", "12V", "", "Toyota Corolla", "Leve"),
    # UETA
    ("UET-REL-40", "UETA", "Rele Universal 40A", "Rele 5 pinos", "Reles", "Universal", "12V", "40A", "Uso geral", "Leve"),
    ("UET-SEN-TMP", "UETA", "Sensor Temperatura UETA", "Sensor agua", "Sensores", "Temperatura", "5V", "", "Linha leve", "Leve"),
    ("UET-CHV-IGN", "UETA", "Chave Ignicao UETA", "Cilindro + chave", "Chaves", "Ignicao", "12V", "", "Linha leve", "Leve"),
    ("UET-FAR-H4", "UETA", "Lampada H4 12V", "Farol H4", "Iluminacao", "Halogena", "12V", "", "Linha leve", "Leve"),
    ("UET-FAR-H7", "UETA", "Lampada H7 12V", "Farol H7", "Iluminacao", "Halogena", "12V", "", "Linha leve", "Leve"),
    # Primox mais itens auto elétrica oficina
    ("PX-BAT-60", "PRIMOX", "Bateria 12V 60Ah", "Bateria estacionaria automotiva", "Baterias", "12V", "12V", "60Ah", "Linha leve", "Leve"),
    ("PX-BAT-75", "PRIMOX", "Bateria 12V 75Ah", "Bateria pickup", "Baterias", "12V", "12V", "75Ah", "Pickup SUV", "Leve"),
    ("PX-BAT-150", "PRIMOX", "Bateria 12V 150Ah", "Bateria auxiliar caminhao", "Baterias", "12V", "12V", "150Ah", "Caminhao (banco 24V)", "Pesado"),
    ("PX-INV-600", "PRIMOX", "Inversor 12V 600W", "Inversor onda modificada", "Acessorios", "Inversor", "12V", "", "Utilitario", "Ambos"),
    ("PX-CAR-10A", "PRIMOX", "Carregador Bateria 10A", "Carregador inteligente", "Acessorios", "Carregador", "110/220V", "10A", "Oficina", "Ambos"),
    ("PX-TST-ALT", "PRIMOX", "Testador Alternador", "Teste carga/voltagem", "Ferramentas", "Diagnostico", "", "", "Bancada", "Ambos"),
    ("PX-TST-BAT", "PRIMOX", "Testador Bateria CCA", "Analisador bateria", "Ferramentas", "Diagnostico", "", "", "Bancada", "Ambos"),
    ("PX-OSC-USB", "PRIMOX", "Osciloscopio USB", "2 canais diagnostico", "Ferramentas", "Diagnostico", "USB", "", "Oficina", "Ambos"),
    ("PX-SCN-OBD", "PRIMOX", "Scanner OBD2 Bluetooth", "Leitor DTC generico", "Ferramentas", "Diagnostico", "12V", "", "Linha leve", "Leve"),
    ("PX-SCN-HD", "PRIMOX", "Scanner Linha Pesada", "Diagnostico caminhao", "Ferramentas", "Diagnostico", "24V", "", "Pesado", "Pesado"),
    ("PX-CAB-J1939", "PRIMOX", "Cabo Diagnostico J1939", "Cabo Deutsch 9 vias", "Chicotes", "Diagnostico", "24V", "", "Caminhao", "Pesado"),
    ("PX-CAB-OBD", "PRIMOX", "Cabo Extensao OBD2", "Extensao 1.5m", "Chicotes", "Diagnostico", "12V", "", "Linha leve", "Leve"),
    ("PX-CON-6.3", "PRIMOX", "Terminal Faston 6.3", "Pacote 100 un", "Terminais", "Faston", "", "", "Eletrica geral", "Ambos"),
    ("PX-CON-DEU", "PRIMOX", "Conector Deutsch 2 vias", "Par macho/femea", "Conectores", "Deutsch", "", "", "Pesado", "Pesado"),
    ("PX-CON-WEA", "PRIMOX", "Conector Weather Pack", "Par 2 vias", "Conectores", "Weather", "", "", "Motor bay", "Ambos"),
    ("PX-FIO-1.5", "PRIMOX", "Fio Automotivo 1.5mm", "Rolo 100m", "Fios", "Cabo", "12/24V", "", "Instalacao", "Ambos"),
    ("PX-FIO-2.5", "PRIMOX", "Fio Automotivo 2.5mm", "Rolo 100m", "Fios", "Cabo", "12/24V", "", "Instalacao", "Ambos"),
    ("PX-FIO-4.0", "PRIMOX", "Fio Automotivo 4.0mm", "Rolo 50m", "Fios", "Cabo", "12/24V", "", "Alimentacao", "Ambos"),
    ("PX-FIO-16", "PRIMOX", "Cabo Bateria 16mm2", "Rolo 25m", "Fios", "Bateria", "12/24V", "", "Partida", "Ambos"),
    ("PX-GND-KIT", "PRIMOX", "Kit Terra Motor", "Trancas e terminais terra", "Kits", "Terra", "", "", "Reparos", "Ambos"),
    ("PX-LED-BAR", "PRIMOX", "Barra LED 52cm 24V", "Barra trabalho", "Iluminacao", "LED", "24V", "", "Caminhao", "Pesado"),
    ("PX-LED-PLA", "PRIMOX", "Placa LED Freio", "Sinalizacao traseira", "Iluminacao", "LED", "12/24V", "", "Utilitario", "Ambos"),
    ("PX-SIR-12", "PRIMOX", "Sirene Re 12V", "Alarme marcha-re", "Sinalizacao", "Sirene", "12V", "", "Pickup", "Leve"),
    ("PX-SIR-24", "PRIMOX", "Sirene Re 24V", "Alarme marcha-re", "Sinalizacao", "Sirene", "24V", "", "Caminhao", "Pesado"),
    ("PX-CAM-RE", "PRIMOX", "Camera Re Universal", "Camera + monitor 7 pol", "Acessorios", "Camera", "12/24V", "", "Utilitario", "Ambos"),
    ("PX-GPS-TRK", "PRIMOX", "Rastreador GPS Oficina", "Demo/teste instalacao", "Acessorios", "GPS", "12/24V", "", "Frota", "Ambos"),
    ("PX-MOD-CAN", "PRIMOX", "Interface CAN USB", "Analise barramento", "Ferramentas", "CAN", "USB", "", "Diagnostico", "Ambos"),
    ("PX-SEN-HALL", "PRIMOX", "Sensor Hall Generico", "Sensor efeito Hall", "Sensores", "Hall", "5V", "", "Rotacao/posicao", "Ambos"),
    ("PX-SEN-TPS", "PRIMOX", "Sensor TPS Generico", "Potenciometro borboleta", "Sensores", "TPS", "5V", "", "Motor aspirado", "Leve"),
    ("PX-SEN-IAT", "PRIMOX", "Sensor IAT", "Temperatura ar admissao", "Sensores", "IAT", "5V", "", "Admissao", "Leve"),
    ("PX-SEN-CKP", "PRIMOX", "Sensor CKP Primox", "Rotacao virabrequim", "Sensores", "CKP", "5V", "", "Flex", "Leve"),
    ("PX-SEN-CMP", "PRIMOX", "Sensor CMP Primox", "Fase comando", "Sensores", "CMP", "5V", "", "Flex", "Leve"),
    ("PX-ATU-ISC", "PRIMOX", "Atuador Marcha Lenta", "Motor de passo ISC", "Atuadores", "ISC", "12V", "", "TBI", "Leve"),
    ("PX-BOM-COM", "PRIMOX", "Bomba Combustivel Eletrica", "Modulo tanque 12V", "Bombas", "Combustivel", "12V", "", "Flex", "Leve"),
    ("PX-BOM-AGU", "PRIMOX", "Bomba Agua Eletrica Aux", "Auxiliar refrigeracao", "Bombas", "Agua", "12V", "", "Performance/leve", "Leve"),
    ("PX-EGR-VLV", "PRIMOX", "Valvula EGR Eletrica", "Controle EGR", "Atuadores", "EGR", "12V", "", "Diesel leve", "Leve"),
    ("PX-TUR-ACT", "PRIMOX", "Atuador Turbina VGT", "Atuador eletronico", "Atuadores", "Turbo", "12/24V", "", "Diesel", "Pesado"),
    ("PX-URE-SEN", "PRIMOX", "Sensor Nivel Arla", "Sensor tanque ARLA", "Sensores", "ARLA", "24V", "", "Euro 5/6", "Pesado"),
    ("PX-NOX-SEN", "PRIMOX", "Sensor NOx", "Sensor gases escapamento", "Sensores", "NOx", "24V", "", "Euro 5/6", "Pesado"),
    ("PX-DEF-BOM", "PRIMOX", "Bomba Dosadora ARLA", "Modulo dosagem", "Bombas", "ARLA", "24V", "", "Euro 5/6", "Pesado"),
    ("PX-ABS-MOD", "PRIMOX", "Modulo ABS Reman", "Modulo remanufaturado", "Modulos", "ABS", "12/24V", "", "Sob consulta", "Ambos"),
    ("PX-BCM-UNI", "PRIMOX", "Modulo Conforto BCM", "Body control generico", "Modulos", "BCM", "12V", "", "Linha leve", "Leve"),
    ("PX-ECU-BEN", "PRIMOX", "Bancada ECU Teste", "Harness bancada", "Ferramentas", "ECU", "", "", "Oficina", "Ambos"),
]

BRAND_COLORS = {
    "PRIMOX": ((15, 76, 129), (255, 255, 255)),
    "DNI": ((180, 30, 30), (255, 255, 255)),
    "BOSCH": ((0, 90, 50), (255, 255, 255)),
    "NGK": ((200, 30, 40), (255, 255, 255)),
    "UETA": ((40, 40, 40), (255, 200, 0)),
}

CAT_ICONS = {
    "Alternadores": "ALT",
    "Partidas": "PRT",
    "Reguladores": "REG",
    "Sensores": "SEN",
    "Reles": "REL",
    "Ignicao": "IGN",
    "Iluminacao": "LED",
    "Fusiveis": "FUS",
    "Chaves": "CHV",
    "Interruptores": "INT",
    "Chicotes": "CHI",
    "Modulos": "MOD",
    "Baterias": "BAT",
    "Ferramentas": "TL",
    "Acessorios": "ACS",
    "Terminais": "TER",
    "Conectores": "CON",
    "Fios": "FIO",
    "Kits": "KIT",
    "Sinalizacao": "SIG",
    "Atuadores": "ATU",
    "Bombas": "BOM",
}


def norm_code(code: str, marca: str) -> str:
    raw = re.sub(r"[^A-Za-z0-9]", "", (code or "").upper())
    return raw


def make_thumb(path: Path, marca: str, codigo: str, categoria: str) -> None:
    bg, fg = BRAND_COLORS.get(marca.upper(), ((60, 60, 60), (255, 255, 255)))
    img = Image.new("RGB", (320, 240), bg)
    draw = ImageDraw.Draw(img)
    # top bar
    draw.rectangle([0, 0, 320, 48], fill=(0, 0, 0))
    try:
        font_b = ImageFont.truetype("arial.ttf", 22)
        font_s = ImageFont.truetype("arial.ttf", 16)
        font_t = ImageFont.truetype("arialbd.ttf", 36)
    except OSError:
        font_b = ImageFont.load_default()
        font_s = font_b
        font_t = font_b
    draw.text((12, 12), marca.upper(), fill=fg, font=font_b)
    icon = CAT_ICONS.get(categoria, "AE")
    # circle icon
    draw.ellipse([110, 70, 210, 170], fill=fg)
    bbox = draw.textbbox((0, 0), icon, font=font_t)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    draw.text((160 - tw / 2, 120 - th / 2 - 4), icon, fill=bg, font=font_t)
    # code bottom
    draw.rectangle([0, 190, 320, 240], fill=(0, 0, 0))
    code_show = codigo if len(codigo) <= 22 else codigo[:20] + ".."
    draw.text((12, 202), code_show, fill=fg, font=font_s)
    path.parent.mkdir(parents=True, exist_ok=True)
    img.save(path, "PNG", optimize=True)


def write_csv(rows: list[dict]) -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    fields = [
        "CodigoFabricante",
        "Nome",
        "Descricao",
        "Categoria",
        "Subcategoria",
        "Marca",
        "Voltagem",
        "Amperagem",
        "Aplicacao",
        "ImagemLocal",
    ]
    with CSV_PATH.open("w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=fields, delimiter=";")
        w.writeheader()
        for r in rows:
            w.writerow({k: r.get(k, "") for k in fields})


def write_pdf(rows: list[dict]) -> None:
    styles = getSampleStyleSheet()
    title = ParagraphStyle(
        "T",
        parent=styles["Heading1"],
        fontSize=16,
        textColor=colors.HexColor("#0F4C81"),
        spaceAfter=8,
    )
    body = ParagraphStyle("B", parent=styles["Normal"], fontSize=8, leading=10)
    story = []
    story.append(Paragraph("Catalogo Primox — Auto Eletrica (oficina)", title))
    story.append(
        Paragraph(
            f"Gerado em {NOW} · {len(rows)} itens · foco auto eletrica leve e pesada · fotos locais",
            body,
        )
    )
    story.append(Spacer(1, 6))

    data = [["Foto", "Codigo", "Marca", "Nome", "Cat.", "V", "Aplicacao"]]
    for r in rows:
        thumb = r["ImagemLocal"]
        img = RLImage(thumb, width=18 * mm, height=13 * mm) if Path(thumb).exists() else ""
        data.append(
            [
                img,
                Paragraph(r["CodigoFabricante"], body),
                r["Marca"],
                Paragraph(r["Nome"], body),
                Paragraph(r["Categoria"], body),
                r["Voltagem"] or "-",
                Paragraph(r["Aplicacao"][:60], body),
            ]
        )

    # paginate in chunks of 12 for readability
    for i in range(0, len(data) - 1, 12):
        chunk = [data[0]] + data[1 + i : 1 + i + 12]
        t = Table(chunk, colWidths=[22 * mm, 28 * mm, 16 * mm, 42 * mm, 22 * mm, 12 * mm, 48 * mm])
        t.setStyle(
            TableStyle(
                [
                    ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#0F4C81")),
                    ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
                    ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
                    ("FONTSIZE", (0, 0), (-1, -1), 7),
                    ("GRID", (0, 0), (-1, -1), 0.3, colors.grey),
                    ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
                    ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, colors.HexColor("#F5F8FC")]),
                    ("LEFTPADDING", (0, 0), (-1, -1), 3),
                    ("RIGHTPADDING", (0, 0), (-1, -1), 3),
                    ("TOPPADDING", (0, 0), (-1, -1), 3),
                    ("BOTTOMPADDING", (0, 0), (-1, -1), 3),
                ]
            )
        )
        story.append(t)
        story.append(Spacer(1, 8))

    doc = SimpleDocTemplate(
        str(PDF_PATH),
        pagesize=A4,
        leftMargin=12 * mm,
        rightMargin=12 * mm,
        topMargin=12 * mm,
        bottomMargin=12 * mm,
    )
    doc.build(story)
    PDF_DL.write_bytes(PDF_PATH.read_bytes())


def vehicle_ids(cur, segmento: str) -> list[str]:
    if segmento == "Pesado":
        rows = cur.execute(
            """SELECT Id FROM CatalogoVeiculos WHERE Ativo=1 AND (
                Segmento='Pesado' OR Marca IN ('Mercedes-Benz','Volvo','Scania','Iveco','MAN','DAF','Hino','Foton','Shacman','Sinotruk','Agrale','Marcopolo','Caio')
                OR Modelo LIKE '%Actros%' OR Modelo LIKE '%Constellation%' OR Modelo LIKE '%Daily%' OR Modelo LIKE '%FH%' OR Modelo LIKE '%Atego%'
            )"""
        ).fetchall()
    elif segmento == "Leve":
        rows = cur.execute(
            """SELECT Id FROM CatalogoVeiculos WHERE Ativo=1 AND IFNULL(Segmento,'Leve')!='Pesado'
               AND Marca NOT IN ('Mercedes-Benz','Volvo','Scania','Iveco','MAN','DAF','Hino','Foton','Shacman','Sinotruk','Agrale','Marcopolo','Caio')
               LIMIT 80"""
        ).fetchall()
    else:
        rows = cur.execute("SELECT Id FROM CatalogoVeiculos WHERE Ativo=1").fetchall()
    return [r[0] for r in rows]


def import_db(rows: list[dict]) -> tuple[int, int, int]:
    conn = sqlite3.connect(str(DB))
    cur = conn.cursor()
    ins = upd = links = 0
    for r in rows:
        marca = r["Marca"]
        codigo = r["CodigoFabricante"]
        n = norm_code(codigo, marca)
        existing = cur.execute(
            "SELECT Id FROM CatalogoPecas WHERE CodigoNormalizado=? AND Marca=? COLLATE NOCASE",
            (n, marca),
        ).fetchone()
        if existing:
            pid = existing[0]
            cur.execute(
                """UPDATE CatalogoPecas SET Nome=?, Descricao=?, Categoria=?, Subcategoria=?,
                   Aplicacao=?, Voltagem=?, Amperagem=?, ImagemLocal=?, ImagemUrl=?,
                   FonteCatalogo=?, ArquivoOrigem=?, StatusRevisao='Importado', DataAtualizacao=?, Ativo=1
                   WHERE Id=?""",
                (
                    r["Nome"],
                    r["Descricao"],
                    r["Categoria"],
                    r["Subcategoria"],
                    r["Aplicacao"],
                    r["Voltagem"],
                    r["Amperagem"],
                    r["ImagemLocal"],
                    r["ImagemLocal"],
                    FONTE,
                    ARQUIVO,
                    NOW,
                    pid,
                ),
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
                (
                    pid,
                    codigo,
                    n,
                    marca,
                    r["Nome"],
                    r["Descricao"],
                    r["Categoria"],
                    r["Subcategoria"],
                    "Auto Eletrica",
                    r["Aplicacao"],
                    "",
                    None,
                    None,
                    r["Voltagem"],
                    r["Amperagem"],
                    "",
                    r["Categoria"],
                    "",
                    FONTE,
                    ARQUIVO,
                    "Seed Primox com foto",
                    r["ImagemLocal"],
                    r["ImagemLocal"],
                    "Importado",
                    None,
                    NOW,
                    NOW,
                    1,
                ),
            )
            ins += 1

        vids = vehicle_ids(cur, r["Segmento"])
        # cap links per part to avoid explosion
        if r["Segmento"] == "Ambos":
            vids = vids[:40]
        elif r["Segmento"] == "Leve":
            vids = vids[:25]
        else:
            vids = vids[:30]
        batch = [(pid, vid, FONTE, NOW) for vid in vids]
        if batch:
            cur.executemany(
                "INSERT OR IGNORE INTO CatalogoPecaVeiculos (CatalogoPecaId, CatalogoVeiculoId, Fonte, DataVinculo) VALUES (?,?,?,?)",
                batch,
            )
            links += len(batch)

    conn.commit()
    total = cur.execute("SELECT COUNT(*) FROM CatalogoPecas").fetchone()[0]
    fotos = cur.execute(
        "SELECT COUNT(*) FROM CatalogoPecas WHERE IFNULL(ImagemLocal,'')!=''"
    ).fetchone()[0]
    seed_count = cur.execute(
        "SELECT COUNT(*) FROM CatalogoPecas WHERE FonteCatalogo=?", (FONTE,)
    ).fetchone()[0]
    conn.close()
    print(f"DB insert={ins} update={upd} link_attempts={links}")
    print(f"Catalogo total={total} com_foto={fotos} seed_fonte={seed_count}")
    return ins, upd, links


def main() -> None:
    IMG_DIR.mkdir(parents=True, exist_ok=True)
    rows = []
    for codigo, marca, nome, desc, cat, sub, volt, amp, apl, seg in PRODUCTS:
        safe = re.sub(r"[^A-Za-z0-9_-]+", "_", codigo)
        img_path = IMG_DIR / f"{marca}_{safe}.png"
        make_thumb(img_path, marca, codigo, cat)
        rows.append(
            {
                "CodigoFabricante": codigo,
                "Nome": nome,
                "Descricao": desc,
                "Categoria": cat,
                "Subcategoria": sub,
                "Marca": marca,
                "Voltagem": volt,
                "Amperagem": amp,
                "Aplicacao": apl,
                "ImagemLocal": str(img_path),
                "Segmento": seg,
            }
        )

    write_csv(rows)
    write_pdf(rows)
    ins, upd, links = import_db(rows)
    print(f"CSV: {CSV_PATH}")
    print(f"PDF: {PDF_PATH}")
    print(f"PDF Downloads: {PDF_DL}")
    print(f"Imagens: {IMG_DIR} ({len(list(IMG_DIR.glob('*.png')))} png)")
    print("DONE", len(rows), ins, upd, links)


if __name__ == "__main__":
    main()
