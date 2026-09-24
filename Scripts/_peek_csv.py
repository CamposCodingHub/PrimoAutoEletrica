from pathlib import Path
# show CSV samples and how CatalogoParaProduto works - read first lines
for p in [
 r'C:\Projetos\PrimoAutoEletrica\TestData\Catalogos\Catalogo-UETA-Amostra.csv',
 r'C:\Projetos\PrimoAutoEletrica\TestData\Catalogos\Catalogo-BOSCH-NGK-Amostra.csv',
 r'C:\Projetos\PrimoAutoEletrica\TestData\Catalogos\Catalogo-DNI-Amostra.csv',
]:
    print('====', Path(p).name)
    print(Path(p).read_text(encoding='utf-8', errors='replace')[:800])
