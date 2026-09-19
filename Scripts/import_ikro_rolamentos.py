# -*- coding: utf-8 -*-
"""Import CATÁLOGO_ROLAMENTOS.pdf (IKRO) into PrimoAutoEletrica CatalogoPecas."""
from __future__ import annotations

import re
import sqlite3
import uuid
from datetime import datetime
from pathlib import Path

import pymupdf

PDF = Path(r"C:\Projetos\PrimoAutoEletrica\TestData\Catalogos\CATALOGO_ROLAMENTOS.pdf")
DB = Path(r"C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db")
MEDIA = Path(r"C:\Users\campo\AppData\Local\PrimoAutoEletrica\Media\Catalogo")
MEDIA.mkdir(parents=True, exist_ok=True)

# IKRO part codes seen in this catalog
CODE_RE = re.compile(
    r"\b(IK(?:B?\d{3,5}[A-Z]{0,4}|B\d{3,5}|[0-9]{2}BD[0-9]{3,4}|[0-9]{4}(?:DDU|T1|DW)?))\b",
    re.I,
)
EAN_RE = re.compile(r"\b(7894774\d{6})\b")
SPEC_RE = re.compile(r"ESPECIFICA[^\n]{0,200}", re.I)
EQ_RE = re.compile(r"EQUIVALENTE[^\n]{0,200}", re.I)

def normalize(code: str) -> str:
    return re.sub(r"[^A-Z0-9]", "", code.upper())

def extract():
    doc = pymupdf.open(PDF)
    items = {}  # norm -> dict
    for page_index, page in enumerate(doc, start=1):
        text = page.get_text("text") or ""
        # product images: large enough, skip logos ~472x238
        images = []
        for img in page.get_images(full=True):
            xref = img[0]
            try:
                pix = pymupdf.Pixmap(doc, xref)
                if pix.n >= 5:  # CMYK
                    pix = pymupdf.Pixmap(pymupdf.csRGB, pix)
                w, h = pix.width, pix.height
                if w >= 600 and h >= 500:
                    images.append((w * h, xref, pix))
                else:
                    pix = None
            except Exception:
                continue
        images.sort(key=lambda t: -t[0])

        codes = []
        for m in CODE_RE.finditer(text):
            code = m.group(1).upper()
            # filter false positives
            if len(normalize(code)) < 5:
                continue
            if code not in codes:
                codes.append(code)

        eans = EAN_RE.findall(text)
        specs = SPEC_RE.findall(text)
        eqs = EQ_RE.findall(text)

        for i, code in enumerate(codes):
            norm = normalize(code)
            if norm in items:
                continue
            desc_parts = []
            if i < len(specs):
                desc_parts.append(specs[i].strip())
            if i < len(eqs):
                desc_parts.append(eqs[i].strip())
            if i < len(eans):
                desc_parts.append(f"EAN {eans[i]}")
            desc = " | ".join(desc_parts)

            img_path = ""
            if i < len(images):
                _, xref, pix = images[i]
                fname = f"IKRO_{norm}_p{page_index}.jpg"
                out = MEDIA / fname
                pix.save(str(out))
                img_path = str(out)

            items[norm] = {
                "codigo": code,
                "norm": norm,
                "nome": f"IKRO {code}",
                "descricao": desc,
                "pagina": str(page_index),
                "imagem": img_path,
            }
    doc.close()
    return list(items.values())


def upsert(parts):
    conn = sqlite3.connect(str(DB))
    cur = conn.cursor()
    # ensure table exists
    cur.execute("SELECT name FROM sqlite_master WHERE type='table' AND name='CatalogoPecas'")
    if not cur.fetchone():
        raise SystemExit("Tabela CatalogoPecas nao encontrada no banco")

    now = datetime.now().isoformat(sep=" ", timespec="seconds")
    import_id = str(uuid.uuid4())
    inserted = updated = skipped = 0

    cur.execute(
        """INSERT INTO CatalogoImportacoes
        (Id, DataImportacao, ArquivoNome, ArquivoCaminho, TipoArquivo, FonteCatalogo, MarcaDetectada,
         TotalLidos, TotalImportados, TotalDuplicados, TotalComErro, Status, Resumo, LogDetalhado, Usuario)
        VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)""",
        (
            import_id,
            now,
            PDF.name,
            str(PDF),
            "PDF",
            "Catalogo IKRO Rolamentos",
            "IKRO",
            len(parts),
            0,
            0,
            0,
            "Em andamento",
            "",
            "",
            "New Bot",
        ),
    )

    for p in parts:
        cur.execute(
            "SELECT Id, ImagemLocal FROM CatalogoPecas WHERE CodigoNormalizado=? AND Marca=? COLLATE NOCASE",
            (p["norm"], "IKRO"),
        )
        row = cur.fetchone()
        if row:
            pid, old_img = row
            cur.execute(
                """UPDATE CatalogoPecas SET Nome=?, Descricao=?, PaginaCatalogo=?, FonteCatalogo=?, ArquivoOrigem=?,
                   ImagemUrl=?, ImagemLocal=CASE WHEN ?!='' THEN ? ELSE ImagemLocal END,
                   StatusRevisao=?, DataAtualizacao=?, Ativo=1 WHERE Id=?""",
                (
                    p["nome"],
                    p["descricao"],
                    p["pagina"],
                    "Catalogo IKRO Rolamentos",
                    PDF.name,
                    p["imagem"],
                    p["imagem"],
                    p["imagem"],
                    "Importado",
                    now,
                    pid,
                ),
            )
            updated += 1
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
                    p["codigo"],
                    p["norm"],
                    "IKRO",
                    p["nome"],
                    p["descricao"],
                    "Rolamentos",
                    "",
                    "115",
                    "",
                    "",
                    None,
                    None,
                    "",
                    "",
                    "",
                    "Rolamento",
                    p["pagina"],
                    "Catalogo IKRO Rolamentos",
                    PDF.name,
                    "",
                    p["imagem"],
                    p["imagem"],
                    "Importado",
                    None,
                    now,
                    now,
                    1,
                ),
            )
            inserted += 1

    cur.execute(
        """UPDATE CatalogoImportacoes SET TotalImportados=?, TotalDuplicados=?, Status=?, Resumo=? WHERE Id=?""",
        (
            inserted,
            updated,
            "Concluida",
            f"IKRO: {inserted} novos, {updated} atualizados, {len(parts)} lidos; fotos em {MEDIA}",
            import_id,
        ),
    )
    conn.commit()
    total = cur.execute("SELECT COUNT(*) FROM CatalogoPecas WHERE Marca='IKRO' COLLATE NOCASE").fetchone()[0]
    com_foto = cur.execute(
        "SELECT COUNT(*) FROM CatalogoPecas WHERE Marca='IKRO' COLLATE NOCASE AND IFNULL(ImagemLocal,'')!=''"
    ).fetchone()[0]
    conn.close()
    return inserted, updated, total, com_foto, import_id


def main():
    if not PDF.exists():
        raise SystemExit(f"PDF nao encontrado: {PDF}")
    parts = extract()
    print(f"EXTRAIDOS: {len(parts)}")
    for p in parts[:5]:
        print(f"  sample {p['codigo']} p{p['pagina']} img={bool(p['imagem'])}")
    ins, upd, total, fotos, iid = upsert(parts)
    print(f"INSERTADOS: {ins}")
    print(f"ATUALIZADOS: {upd}")
    print(f"TOTAL_IKRO_NO_BANCO: {total}")
    print(f"COM_FOTO: {fotos}")
    print(f"IMPORTACAO_ID: {iid}")
    print(f"MEDIA: {MEDIA}")


if __name__ == "__main__":
    main()
