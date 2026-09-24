from pathlib import Path
import re

p = Path('PrimoAutoEletrica/Services/AgendamentoReportService.cs')
t = p.read_text(encoding='utf-8')
print('AgendamentoReportService lines', len(t.splitlines()))
print('--- first 80 lines ---')
print('\n'.join(t.splitlines()[:80]))
print('--- public methods ---')
for m in re.findall(r'public [^\n{]+', t)[:30]:
    print(m[:120])

login = Path('PrimoAutoEletrica/Views/LoginWindow.xaml.cs').read_text(encoding='utf-8')
print('\n=== Login methods ===')
for m in re.findall(r'(private|public|protected)[^\n(]+?\([^)]*\)', login)[:40]:
    print(m[:140])
print('TwoFactor refs', login.count('TwoFactor'), 'ITwoFactor', 'TwoFactor' in login)
# auth block
idx = login.find('Authenticate')
print(login[idx:idx+1200] if idx>=0 else 'no Authenticate')

ae = Path('PrimoAutoEletrica/UserControls/AutoEletricaTecnicaControl.xaml.cs').read_text(encoding='utf-8')
print('\n=== AutoEletrica methods ===')
for m in re.findall(r'(private|public)[^\n(]+?\([^)]*\)', ae)[:40]:
    print(m[:140])

x = Path('PrimoAutoEletrica/UserControls/AutoEletricaTecnicaControl.xaml').read_text(encoding='utf-8')
print('\n=== AutoEletrica buttons ===')
for line in x.splitlines():
    if 'Button' in line and ('x:Name' in line or 'Click=' in line or 'Content=' in line):
        print(line.strip()[:160])
