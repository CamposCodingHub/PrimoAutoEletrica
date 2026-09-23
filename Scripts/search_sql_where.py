import os
import re

code_dir = 'PrimoAutoEletrica'
money_cols = [
    'Total', 'Desconto', 'Valor', 'PrecoVenda', 'PrecoCompra',
    'ValorTotalEstoque', 'Salario', 'ValorAbertura', 'TotalVendas',
    'PrecoUnitario', 'Subtotal', 'ValorMaoObra'
]

pattern = re.compile(rf'WHERE\b[^;\"\']*?\b({"|".join(money_cols)})\b\s*([<>=!]+|BETWEEN)\s*([@\w\d\.]+)', re.IGNORECASE)

findings = []
for root, _, files in os.walk(code_dir):
    for f in files:
        if f.endswith('.cs'):
            p = os.path.join(root, f)
            with open(p, 'r', encoding='utf-8', errors='ignore') as fp:
                content = fp.read()
                for m in pattern.finditer(content):
                    snippet = " ".join(m.group(0).split())
                    findings.append((f, snippet))

print(f"Total findings: {len(findings)}")
for f, s in findings:
    print(f"  {f} -> {s}")
