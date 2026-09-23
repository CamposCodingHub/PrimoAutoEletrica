import os
import re
import csv
import json

code_dir = "PrimoAutoEletrica"
classification_path = r"Docs/audit/2026-09-20/P2_1_MONEY_CLASSIFICATION.csv"

# Load semantic categories
money_cols = set()
non_money_cols = set()
with open(classification_path, encoding='utf-8') as f:
    r = csv.DictReader(f)
    for row in r:
        t = row["Tabela"]
        c = row["Coluna"]
        cat = row["CategoriaSemantica"]
        if row["EMonetario"] == "SIM":
            money_cols.add(c.lower())
        else:
            non_money_cols.add(c.lower())

patterns = [
    (r"\bGetDouble\s*\(", "GetDouble"),
    (r"\bGetDecimal\s*\(", "GetDecimal"),
    (r"\bConvert\.ToDecimal\s*\(", "Convert.ToDecimal"),
    (r"\bConvert\.ToDouble\s*\(", "Convert.ToDouble"),
    (r"\bdouble\.Parse\s*\(", "double.Parse"),
    (r"\bfloat\.Parse\s*\(", "float.Parse")
]

occurrences = []

for root, _, files in os.walk(code_dir):
    for f in files:
        if not f.endswith(".cs"):
            continue
        filepath = os.path.join(root, f)
        rel_path = os.path.relpath(filepath, code_dir)
        with open(filepath, "r", encoding="latin1") as fp:
            lines = fp.readlines()
            
        for line_no, line in enumerate(lines, 1):
            for pat, pat_name in patterns:
                if re.search(pat, line):
                    snippet = line.strip()
                    # Determine classification A, B, C, D
                    lower_line = line.lower()
                    
                    # Check if line mentions known non-money terms
                    is_b = False
                    if any(nm in lower_line for nm in ["margem", "percentual", "latitude", "longitude", "quantidade", "progress", "width", "height", "ratio", "taxa", "versao"]):
                        is_b = True
                        
                    # Check if line mentions known money terms
                    is_a = False
                    if any(m in lower_line for m in ["preco", "valor", "salario", "total", "subtotal", "desconto", "acrescimo", "custo", "saldo", "sangria", "suprimento", "faturado", "gasto", "comissao", "imposto"]):
                        is_a = True
                        
                    # Check if in View/UI or Test or helper
                    is_c = False
                    if "xaml.cs" in rel_path.lower() or "test" in rel_path.lower() or "cache" in rel_path.lower() or "log" in rel_path.lower():
                        is_c = True

                    classification = "D"
                    justification = ""
                    if is_b and not is_a:
                        classification = "B"
                        justification = "Campo ou cálculo não-monetário (percentual, quantidade, coordenada, dimensão visual)."
                    elif is_c:
                        classification = "C"
                        justification = "Falso positivo: contexto de UI, log, cache ou validação em memória, não é I/O de persistência SQLite."
                    elif is_a:
                        classification = "A"
                        justification = "Acesso a campo monetário em I/O de banco de dados SQLite que requer blindagem MoneyIO."
                    else:
                        classification = "C"
                        justification = "Falso positivo: conversão utilitária genérica sem vínculo com coluna monetária de banco."

                    occurrences.append({
                        "file": rel_path,
                        "line": line_no,
                        "pattern": pat_name,
                        "classification": classification,
                        "justification": justification,
                        "snippet": snippet
                    })

print(f"Total occurrences: {len(occurrences)}")
counts = {}
for o in occurrences:
    counts[o["classification"]] = counts.get(o["classification"], 0) + 1
print("Counts by classification:", counts)

with open(r"c:\Projetos\PrimoAutoEletrica\TestResults\Rehearsal_Fase2_2\old_money_access_audit.json", "w", encoding="utf-8") as f:
    json.dump(occurrences, f, indent=2, ensure_ascii=False)
