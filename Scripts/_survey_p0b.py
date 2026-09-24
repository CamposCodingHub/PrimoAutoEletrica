from pathlib import Path
t = Path('PrimoAutoEletrica/Services/AgendamentoReportService.cs').read_text(encoding='utf-8')
i = t.find('ExportarParaPDF')
print(t[i:i+2200])
print('==== TwoFactorService ====')
print(Path('PrimoAutoEletrica/Services/TwoFactorService.cs').read_text(encoding='utf-8')[:3500])
print('==== Login success path ====')
login = Path('PrimoAutoEletrica/Views/LoginWindow.xaml.cs').read_text(encoding='utf-8')
# find password success
for needle in ['LoginCommand', 'OnLogin', 'PasswordBox', 'Authenticated', 'ShowDialog', 'TryLogin', 'EntrarButton', 'LoginButton']:
    print(needle, login.find(needle))
# dump more around Authenticated
idx = login.find('_viewModel.AuthenticatedFuncionario')
print(login[max(0,idx-800):idx+400])
print('==== DefaultWarranty ====')
for p in Path('PrimoAutoEletrica').rglob('*Configuration*.cs'):
    txt = p.read_text(encoding='utf-8', errors='ignore')
    if 'DefaultWarranty' in txt or 'WarrantyDays' in txt:
        print(p)
        for line in txt.splitlines():
            if 'Warranty' in line or 'Garantia' in line:
                print(' ', line.strip()[:120])
