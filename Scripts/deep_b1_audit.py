import os
import re
import csv
import json
import sqlite3

def run_deep_audit():
    print("=== INICIANDO AUDITORIA PROFUNDA DE PRODUTO B1 ===")
    
    # 1. Mapeamento de Telas (Views)
    views = []
    for root, _, files in os.walk("PrimoAutoEletrica/Views"):
        for f in files:
            if f.endswith(".xaml"):
                p = os.path.join(root, f)
                rel = os.path.relpath(p, "PrimoAutoEletrica")
                with open(p, "r", encoding="utf-8", errors="ignore") as fp:
                    content = fp.read()
                # Procurar DataContext ou ViewModel associado
                vm_match = re.search(r'd:DataContext="{d:DesignInstance.*?Type=.*?([A-Za-z0-9_]+ViewModel)', content)
                vm_name = vm_match.group(1) if vm_match else "NOT_EXPLICIT"
                views.append({
                    "file": f,
                    "rel_path": rel,
                    "vm": vm_name,
                    "has_dark": "DynamicResource" in content or "ThemeAware" in content or "Dark" in content
                })
                
    print(f"Total de Views mapeadas: {len(views)}")
    
    # 2. Mapeamento de Tabelas e Dados
    db_path = r"C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db"
    con = sqlite3.connect(db_path)
    cur = con.cursor()
    tables = [r[0] for r in cur.execute("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'").fetchall()]
    table_stats = {}
    for t in tables:
        cols = [c[1] for c in cur.execute(f"PRAGMA table_info([{t}])").fetchall()]
        cnt = cur.execute(f"SELECT count(*) FROM [{t}]").fetchone()[0]
        table_stats[t] = {"cols": cols, "count": cnt}
    con.close()
    print(f"Total de tabelas de banco auditadas: {len(tables)}")
    
    # 3. Auditoria de Associações de ID (Procurar RISK - IDENTITY ASSOCIATION)
    print("\n--- AUDITANDO ASSOCIAÇÕES POR ID VS NOME/TEXTO ---")
    id_risks = []
    where_patterns = [
        (r'WHERE\s+.*Cliente\s*=\s*@', "WHERE Cliente = @ (por nome/texto)"),
        (r'WHERE\s+.*ClienteNome\s*=\s*@', "WHERE ClienteNome = @ (por nome/texto)"),
        (r'WHERE\s+.*Placa\s*=\s*@', "WHERE Placa = @ (por placa sem VeiculoId)")
    ]
    for root, _, files in os.walk("PrimoAutoEletrica"):
        for f in files:
            if not f.endswith(".cs"):
                continue
            p = os.path.join(root, f)
            rel = os.path.relpath(p, "PrimoAutoEletrica")
            with open(p, "r", encoding="utf-8", errors="ignore") as fp:
                for line_no, line in enumerate(fp, 1):
                    for pat, desc in where_patterns:
                        if re.search(pat, line, re.IGNORECASE):
                            # Exclui se tiver também ClienteId ou VeiculoId na query
                            id_risks.append({
                                "file": rel,
                                "line": line_no,
                                "desc": desc,
                                "snippet": line.strip()
                            })
    print(f"Riscos de associação encontrados: {len(id_risks)}")
    with open("TestResults/b1_id_risks.json", "w", encoding="utf-8") as f:
        json.dump(id_risks, f, indent=2, ensure_ascii=False)
        
    # 4. Auditoria de Fiscal
    print("\n--- AUDITANDO FISCAL ---")
    fiscal_files = []
    for root, _, files in os.walk("PrimoAutoEletrica"):
        for f in files:
            if "fiscal" in f.lower() or "nfe" in f.lower() or "nfce" in f.lower() or "nfse" in f.lower():
                fiscal_files.append(os.path.relpath(os.path.join(root, f), "PrimoAutoEletrica"))
    print(f"Arquivos relacionados a fiscal: {len(fiscal_files)}")
    
    # 5. Auditoria de Autoelétrica Técnica
    print("\n--- AUDITANDO AUTOELÉTRICA TÉCNICA ---")
    ae_files = []
    for root, _, files in os.walk("PrimoAutoEletrica"):
        for f in files:
            if "autoeletrica" in f.lower() or "sintoma" in f.lower() or "diagnostico" in f.lower() or "dvi" in f.lower():
                ae_files.append(os.path.relpath(os.path.join(root, f), "PrimoAutoEletrica"))
    print(f"Arquivos relacionados a Autoelétrica/DVI/Diagnóstico: {len(ae_files)}")
    
    # 6. Auditoria de APIs
    print("\n--- AUDITANDO ENDPOINTS DE API ---")
    api_prog = r"PrimoAutoEletrica.Api/Program.cs"
    endpoints = []
    if os.path.exists(api_prog):
        with open(api_prog, "r", encoding="utf-8", errors="ignore") as fp:
            for line_no, line in enumerate(fp, 1):
                m = re.search(r'app\.Map(Get|Post|Put|Delete)\("([^"]+)"', line)
                if m:
                    endpoints.append({
                        "method": m.group(1).upper(),
                        "path": m.group(2),
                        "line": line_no
                    })
    print(f"Endpoints da API encontrados: {len(endpoints)}")
    for ep in endpoints:
        print(f"  {ep['method']:6} {ep['path']}")
        
    # 7. Auditoria de Permissões e Perfis (RBAC)
    print("\n--- AUDITANDO RBAC ---")
    con = sqlite3.connect(db_path)
    perfis = [r[0] for r in con.execute("SELECT Nome FROM PerfisAcesso").fetchall()]
    perms = [r[0] for r in con.execute("SELECT Codigo FROM Permissoes").fetchall()]
    con.close()
    print(f"Perfis cadastrados no banco ({len(perfis)}): {', '.join(perfis)}")
    print(f"Total de permissões cadastradas no banco: {len(perms)}")
    
    return views, table_stats, id_risks, fiscal_files, ae_files, endpoints, perfis, perms

if __name__ == "__main__":
    run_deep_audit()
