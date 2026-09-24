from pathlib import Path
import re
t = Path('PrimoAutoEletrica/Services/DocumentoPdfService.cs').read_text(encoding='utf-8')
print('lines', len(t.splitlines()))
for m in re.findall(r'public [^\n{]+', t)[:40]:
    print(m[:140])
print('--- AutoEletrica snapshot model ---')
print(Path('PrimoAutoEletrica/Models/AutoEletricaTecnica.cs').read_text(encoding='utf-8')[:4000])
