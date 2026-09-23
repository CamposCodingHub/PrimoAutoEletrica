import os
import re
import csv
import json

classification_path = r"Docs/audit/2026-09-20/P2_1_MONEY_CLASSIFICATION.csv"
code_root = r"PrimoAutoEletrica"

# Load money columns
money_fields = {}
with open(classification_path, encoding='utf-8') as f:
    r = csv.DictReader(f)
    for row in r:
        if row["EMonetario"] == "SIM":
            t = row["Tabela"]
            c = row["Coluna"]
            money_fields.setdefault(t, []).append(c)

print(f"Loaded {sum(len(v) for v in money_fields.values())} money columns across {len(money_fields)} tables.")

# Search in C# files
findings = []

for root, dirs, files in os.walk(code_root):
    for file in files:
        if not file.endswith(".cs"):
            continue
        filepath = os.path.join(root, file)
        rel_path = os.path.relpath(filepath, code_root)
        with open(filepath, "r", encoding="utf-8", errors="ignore") as f:
            lines = f.readlines()
            
        for i, line in enumerate(lines, 1):
            # Check for GetDouble on money fields
            if "GetDouble" in line:
                for tbl, cols in money_fields.items():
                    for col in cols:
                        if col.lower() in line.lower() or f'"{col}"' in line:
                            findings.append({
                                "file": rel_path,
                                "line": i,
                                "type": "GETDOUBLE_RISK",
                                "col": f"{tbl}.{col}",
                                "snippet": line.strip()
                            })
                            
            # Check for SUM(col)
            for tbl, cols in money_fields.items():
                for col in cols:
                    if re.search(rf"\bSUM\s*\(\s*(?:\[?\w+\]?\.)?\[?{col}\]?\s*\)", line, re.IGNORECASE):
                        findings.append({
                            "file": rel_path,
                            "line": i,
                            "type": "SQL_SUM_AGGREGATE",
                            "col": f"{tbl}.{col}",
                            "snippet": line.strip()
                        })
                    if re.search(rf"\bAVG\s*\(\s*(?:\[?\w+\]?\.)?\[?{col}\]?\s*\)", line, re.IGNORECASE):
                        findings.append({
                            "file": rel_path,
                            "line": i,
                            "type": "SQL_AVG_AGGREGATE",
                            "col": f"{tbl}.{col}",
                            "snippet": line.strip()
                        })
                        
            # Check for AddWithValue with decimal parameter on money fields
            if "AddWithValue" in line or "Parameters.Add" in line:
                for tbl, cols in money_fields.items():
                    for col in cols:
                        if f"@{col}" in line or f'"{col}"' in line:
                            findings.append({
                                "file": rel_path,
                                "line": i,
                                "type": "PARAM_BINDING",
                                "col": f"{tbl}.{col}",
                                "snippet": line.strip()
                            })

print(f"Total findings: {len(findings)}")
with open(r"c:\Projetos\PrimoAutoEletrica\TestResults\Rehearsal_Fase2_2\repo_audit_findings.json", "w", encoding="utf-8") as f:
    json.dump(findings, f, indent=2, ensure_ascii=False)

types_cnt = {}
for fd in findings:
    types_cnt[fd["type"]] = types_cnt.get(fd["type"], 0) + 1
print("Findings by type:", types_cnt)
