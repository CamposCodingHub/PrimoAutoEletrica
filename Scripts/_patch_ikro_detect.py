from pathlib import Path
import re
path=Path(r'C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\Services\Catalogo\CatalogoMarcaDetector.cs')
t=path.read_text(encoding='utf-8')
# show DetectarMarcaNoTexto region
idx=t.find('DetectarMarcaNoTexto')
print(t[idx:idx+1800] if idx>=0 else 'method missing')
# if no explicit IKRO return yet beyond set, inject before final return string.Empty of that method
if 'return "IKRO"' not in t:
    # find private static string DetectarMarcaNoTexto ... return string.Empty;
    m=re.search(r'(private static string DetectarMarcaNoTexto\(string value\)\s*\{)(.*?)(return string\.Empty;\s*\})', t, re.S)
    if m:
        insert='''
        if (Regex.IsMatch(value, @"\\bIKRO\\b|ikro\\.com\\.br", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) ||
            Regex.IsMatch(value, @"CAT[AÁÀÃ]LOGO[_\\s-]*ROLAMENT", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            return "IKRO";
        }

        '''
        t=t[:m.start(3)]+insert+t[m.start(3):]
        path.write_text(t, encoding='utf-8')
        print('INSERTED IKRO return')
    else:
        print('DetectarMarcaNoTexto structure not matched')
else:
    print('already has return IKRO')
