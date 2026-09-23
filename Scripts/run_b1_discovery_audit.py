import os
import re
import csv
import json
import sqlite3

def run_discovery():
    print("=== EXECUTANDO MOTOR DE DESCOBERTA B1 ===")
    
    # 1. Mapeamento de Views
    views_dir = r"PrimoAutoEletrica/Views"
    views = []
    for root, _, files in os.walk(views_dir):
        for f in files:
            if f.endswith(".xaml"):
                full_path = os.path.join(root, f)
                rel_path = os.path.relpath(full_path, "PrimoAutoEletrica")
                views.append((f, rel_path, full_path))
                
    # 2. Mapeamento de ViewModels
    vms_dir = r"PrimoAutoEletrica/ViewModels"
    vms = []
    for root, _, files in os.walk(vms_dir):
        for f in files:
            if f.endswith(".cs"):
                vms.append(f)
                
    # 3. Mapeamento de Services
    services_dir = r"PrimoAutoEletrica/Services"
    services = []
    for root, _, files in os.walk(services_dir):
        for f in files:
            if f.endswith(".cs"):
                services.append(f)
                
    # 4. Mapeamento de Repositories
    repos_dir = r"PrimoAutoEletrica/Repositories"
    repos = []
    for root, _, files in os.walk(repos_dir):
        for f in files:
            if f.endswith(".cs") and not f.startswith("I"):
                repos.append(f)
                
    # 5. Mapeamento de Tabelas
    db_path = r"C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db"
    con = sqlite3.connect(db_path)
    tables = [r[0] for r in con.execute("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'").fetchall()]
    con.close()
    
    print(f"Views encontradas: {len(views)}")
    print(f"ViewModels encontrados: {len(vms)}")
    print(f"Services encontrados: {len(services)}")
    print(f"Repositories encontrados: {len(repos)}")
    print(f"Tabelas de banco encontradas: {len(tables)}")

    # 6. Auditoria de Mocks, Fakes, TODOs, NotImplemented
    print("\n--- AUDITANDO MOCKS, FAKES E NOT_IMPLEMENTED ---")
    mock_findings = []
    patterns = [
        (r"\bTODO\b", "TODO"),
        (r"\bFIXME\b", "FIXME"),
        (r"\bNotImplementedException\b", "NotImplementedException"),
        (r"\bmock\b", "mock"),
        (r"\bfake\b", "fake"),
        (r"\bplaceholder\b", "placeholder")
    ]
    
    for root, _, files in os.walk("PrimoAutoEletrica"):
        for f in files:
            if not (f.endswith(".cs") or f.endswith(".xaml")):
                continue
            path = os.path.join(root, f)
            with open(path, "r", encoding="latin1") as fp:
                for line_no, line in enumerate(fp, 1):
                    for pat, pat_name in patterns:
                        if re.search(pat, line, re.IGNORECASE):
                            mock_findings.append({
                                "file": os.path.relpath(path, "PrimoAutoEletrica"),
                                "line": line_no,
                                "type": pat_name,
                                "snippet": line.strip()
                            })
                            
    print(f"Total de ocorrências de mock/todo/fake: {len(mock_findings)}")
    with open("TestResults/b1_mock_findings.json", "w", encoding="utf-8") as f:
        json.dump(mock_findings, f, indent=2, ensure_ascii=False)
        
    return views, vms, services, repos, tables, mock_findings

if __name__ == "__main__":
    os.makedirs("TestResults", exist_ok=True)
    run_discovery()
