import os
import re
import csv
import json
import sqlite3

def run_comprehensive_audit():
    print("Gathering comprehensive B1 audit data...")
    db_path = r"TestResults\Homologacao_Fase2_4\primoauto_money_v24_source.db"
    con = sqlite3.connect(db_path)
    cur = con.cursor()
    
    # Tables and counts
    tables = [r[0] for r in cur.execute("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'").fetchall()]
    table_info = {}
    for t in tables:
        cols = cur.execute(f"PRAGMA table_info([{t}])").fetchall()
        cnt = cur.execute(f"SELECT count(*) FROM [{t}]").fetchone()[0]
        table_info[t] = {
            "cols": [c[1] for c in cols],
            "col_types": {c[1]: c[2] for c in cols},
            "count": cnt
        }
    con.close()
    
    # Views
    views = []
    for root, _, files in os.walk("PrimoAutoEletrica/Views"):
        for f in files:
            if f.endswith(".xaml"):
                p = os.path.join(root, f)
                with open(p, "r", encoding="utf-8", errors="ignore") as fp:
                    content = fp.read()
                vm_match = re.search(r'd:DataContext="{d:DesignInstance.*?Type=.*?([A-Za-z0-9_]+ViewModel)', content)
                vm = vm_match.group(1) if vm_match else "None"
                views.append({
                    "name": f,
                    "rel_path": os.path.relpath(p, "PrimoAutoEletrica"),
                    "vm": vm,
                    "has_dynamic_resource": "DynamicResource" in content,
                    "has_hardcoded_colors": len(re.findall(r'#(?:[0-9a-fA-F]{3,4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})\b', content)),
                    "lines": len(content.splitlines())
                })
                
    # ViewModels
    viewmodels = []
    for root, _, files in os.walk("PrimoAutoEletrica/ViewModels"):
        for f in files:
            if f.endswith(".cs"):
                p = os.path.join(root, f)
                with open(p, "r", encoding="utf-8", errors="ignore") as fp:
                    content = fp.read()
                viewmodels.append({
                    "name": f,
                    "rel_path": os.path.relpath(p, "PrimoAutoEletrica"),
                    "lines": len(content.splitlines())
                })
                
    # Services
    services = []
    for root, _, files in os.walk("PrimoAutoEletrica/Services"):
        for f in files:
            if f.endswith(".cs"):
                p = os.path.join(root, f)
                with open(p, "r", encoding="utf-8", errors="ignore") as fp:
                    content = fp.read()
                services.append({
                    "name": f,
                    "rel_path": os.path.relpath(p, "PrimoAutoEletrica"),
                    "lines": len(content.splitlines())
                })
                
    # Repositories
    repos = []
    for root, _, files in os.walk("PrimoAutoEletrica/Repositories"):
        for f in files:
            if f.endswith(".cs"):
                p = os.path.join(root, f)
                with open(p, "r", encoding="utf-8", errors="ignore") as fp:
                    content = fp.read()
                repos.append({
                    "name": f,
                    "rel_path": os.path.relpath(p, "PrimoAutoEletrica"),
                    "lines": len(content.splitlines())
                })
                
    # APIs in Program.cs
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
                    
    # Tests in PrimoAutoEletrica.Tests
    test_files = []
    total_test_methods = 0
    for root, _, files in os.walk("Tests/PrimoAutoEletrica.Tests"):
        for f in files:
            if f.endswith(".cs"):
                p = os.path.join(root, f)
                with open(p, "r", encoding="utf-8", errors="ignore") as fp:
                    content = fp.read()
                facts = len(re.findall(r'\[Fact', content))
                theories = len(re.findall(r'\[Theory', content))
                if facts + theories > 0:
                    total_test_methods += (facts + theories)
                    test_files.append({
                        "file": f,
                        "rel_path": os.path.relpath(p, "Tests/PrimoAutoEletrica.Tests"),
                        "facts": facts,
                        "theories": theories,
                        "total": facts + theories
                    })

    out = {
        "tables": table_info,
        "views": views,
        "viewmodels": viewmodels,
        "services": services,
        "repositories": repos,
        "endpoints": endpoints,
        "test_files": test_files,
        "total_test_methods": total_test_methods
    }
    
    with open("TestResults/b1_comprehensive_audit_data.json", "w", encoding="utf-8") as fp:
        json.dump(out, fp, indent=2, ensure_ascii=False)
        
    print(f"Data gathered successfully: {len(tables)} tables, {len(views)} views, {len(viewmodels)} VMs, {len(services)} services, {len(repos)} repos, {len(endpoints)} API endpoints, {len(test_files)} test files ({total_test_methods} tests).")

if __name__ == "__main__":
    run_comprehensive_audit()
