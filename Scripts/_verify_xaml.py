from pathlib import Path
import re
path = Path(r"C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\UserControls\CatalogoPecasControl.xaml")
t = path.read_text(encoding="utf-8-sig")
print("resources", len(re.findall(r"<UserControl\.Resources>", t)))
print("keys", t.count('x:Key="ImagePathToImageSourceConverter"'))
print("foto", t.count('Header="Foto"'))
print("xmlns", "xmlns:converters" in t)
print("has_img_binding", "Binding ImagemLocal" in t or "Binding={Binding ImagemLocal" in t)
# fix if resources missing
if "<UserControl.Resources>" not in t:
    t = t.replace(
        'Background="{DynamicResource AppBackgroundBrush}">',
        'Background="{DynamicResource AppBackgroundBrush}">\n\n    <UserControl.Resources>\n        <converters:ImagePathToImageSourceConverter x:Key="ImagePathToImageSourceConverter"/>\n    </UserControl.Resources>\n',
        1,
    )
    path.write_text(t, encoding="utf-8")
    t = path.read_text(encoding="utf-8-sig")
    print("AFTER_FIX resources", len(re.findall(r"<UserControl\.Resources>", t)))
    print("AFTER_FIX keys", t.count('x:Key="ImagePathToImageSourceConverter"'))
# show lines 1-20 ascii-safe
for i, line in enumerate(t.splitlines()[:22], 1):
    print(f"{i}: {line.encode('ascii','replace').decode()}")
