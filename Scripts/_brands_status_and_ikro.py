import sqlite3
from pathlib import Path
import re

c=sqlite3.connect(r'C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db')
print('Produtos by Marca:')
for r in c.execute("SELECT IFNULL(Marca,'(vazio)'), COUNT(*) FROM Produtos WHERE IFNULL(IsDeleted,0)=0 GROUP BY 1 ORDER BY 2 DESC"):
    print(r)
print('Catalogo:')
for r in c.execute('SELECT Marca, COUNT(*) FROM CatalogoPecas GROUP BY Marca ORDER BY 2 DESC'):
    print(r)
print('total produtos', c.execute('SELECT COUNT(*) FROM Produtos WHERE IFNULL(IsDeleted,0)=0').fetchone()[0])

path=Path(r'C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\Services\Catalogo\CatalogoMarcaDetector.cs')
t=path.read_text(encoding='utf-8')
changed=False
if '"IKRO"' not in t:
    t2=t.replace('"SKF", "NSK", "FAG", "INA", "NTN", "TIMKEN"','"IKRO", "SKF", "NSK", "FAG", "INA", "NTN", "TIMKEN"')
    if t2!=t:
        t=t2; changed=True
        print('added IKRO to known brands set')
if 'ikro.com.br' not in t:
    needle='if (Regex.IsMatch(value, @"\\bGF\\b"'
    # find GF block end and insert after
    m=re.search(r'if \(Regex\.IsMatch\(value, @"\\bGF\\b".*?return "GF";\s*\}', t, re.S)
    if m:
        insert='''

        if (Regex.IsMatch(value, @"\\bIKRO\\b|ikro\\.com\\.br|CAT[AÁ]LOGO[_\\s-]*ROLAMENTOS", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            return "IKRO";
        }'''
        t=t[:m.end()]+insert+t[m.end():]
        changed=True
        print('added IKRO detect block')
    else:
        print('GF block not found for insert')
if changed:
    path.write_text(t, encoding='utf-8')
print('IKRO mentions', t.count('IKRO'))
