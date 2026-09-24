from pathlib import Path
import re
path = Path(r"C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\UserControls\CatalogoPecasControl.xaml")
text = path.read_text(encoding="utf-8")
# remove all UserControl.Resources blocks
text2, n = re.subn(r"\s*<UserControl\.Resources>.*?</UserControl\.Resources>\s*", "\n", text, flags=re.S)
print("removed", n)
# ensure xmlns converters
if "xmlns:converters=" not in text2:
    text2 = text2.replace(
        'xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"',
        'xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"\n             xmlns:converters="clr-namespace:PrimoAutoEletrica.Converters"',
        1,
    )
# insert single resources after opening UserControl tag
text2 = re.sub(
    r'(Background="\{DynamicResource AppBackgroundBrush\}">\s*)',
    r'''\1
    <UserControl.Resources>
        <converters:ImagePathToImageSourceConverter x:Key="ImagePathToImageSourceConverter"/>
    </UserControl.Resources>

''',
    text2,
    count=1,
)
# ensure photo column once
if 'Header="Foto"' not in text2:
    photo = '''
                                <DataGridTemplateColumn Header="Foto"
                                                        Width="72"
                                                        MinWidth="64"
                                                        CanUserResize="False"
                                                        CanUserSort="False">
                                    <DataGridTemplateColumn.CellTemplate>
                                        <DataTemplate>
                                            <Border Width="56" Height="56" CornerRadius="8" Background="{DynamicResource CardBackgroundBrush}" Margin="2">
                                                <Image Source="{Binding ImagemLocal, Converter={StaticResource ImagePathToImageSourceConverter}}"
                                                       Stretch="Uniform"
                                                       ToolTip="{Binding ImagemLocal}"/>
                                            </Border>
                                        </DataTemplate>
                                    </DataGridTemplateColumn.CellTemplate>
                                </DataGridTemplateColumn>
'''
    text2 = text2.replace('<DataGrid.Columns>', '<DataGrid.Columns>' + photo, 1)

path.write_text(text2, encoding="utf-8")
raw = path.read_text(encoding="utf-8")
print("resources", len(re.findall(r"<UserControl\\.Resources>", raw)))
print("keys", len(re.findall(r"ImagePathToImageSourceConverter x:Key", raw)))
print("foto", len(re.findall(r'Header="Foto"', raw)))
print(raw[:650])
