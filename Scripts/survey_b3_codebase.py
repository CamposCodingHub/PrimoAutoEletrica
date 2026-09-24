import os
import glob
import sqlite3
import re

def main():
    def count_files(folder, pattern):
        return sorted(glob.glob(f'{folder}/**/{pattern}', recursive=True))

    views_xaml = count_files('PrimoAutoEletrica', '*.xaml')
    viewmodels = count_files('PrimoAutoEletrica/ViewModels', '*.cs')
    services = count_files('PrimoAutoEletrica/Services', '*.cs')
    repositories = count_files('PrimoAutoEletrica/Repositories', '*.cs')
    models = count_files('PrimoAutoEletrica/Models', '*.cs')

    db_path = r'C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto_operacional.db'
    conn = sqlite3.connect(db_path)
    c = conn.cursor()
    tables = [row[0] for row in c.execute("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'").fetchall()]
    
    # Check permissions
    perfis = c.execute("SELECT count(*) FROM PerfisAcesso").fetchone()[0]
    permissoes = c.execute("SELECT count(*) FROM Permissoes").fetchone()[0]
    conn.close()

    print(f"Views/XAML count: {len(views_xaml)}")
    print(f"ViewModels count: {len(viewmodels)}")
    print(f"Services count: {len(services)}")
    print(f"Repositories count: {len(repositories)}")
    print(f"Models count: {len(models)}")
    print(f"SQLite Tables: {len(tables)}")
    print(f"PerfisAcesso: {perfis}, Permissoes: {permissoes}")

    # Inspect API routes
    api_prog = "PrimoAutoEletrica.Api/Program.cs"
    if os.path.exists(api_prog):
        with open(api_prog, "r", encoding="utf-8") as f:
            content = f.read()
        routes = re.findall(r'app\.Map(Get|Post|Put|Delete)\("([^"]+)"', content)
        print(f"API Routes count: {len(routes)}")
        for method, route in routes:
            print(f"  {method} {route}")

if __name__ == "__main__":
    main()
