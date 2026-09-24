import csv

with open('Docs/audit/2026-09-20/B4_PRODUCT_MATRIX.csv', encoding='utf-8') as f:
    reader = csv.DictReader(f)
    counts = {}
    for r in reader:
        st = r['Status']
        counts[st] = counts.get(st, 0) + 1
        print(f"{r['ID']} | {r['Modulo']} | {r['Status']} | {r['Problema'][:35]} | {r['Prioridade']}")
    print("\nResumo:")
    for k, v in counts.items():
        print(f"  {k}: {v}")
    print(f"Total: {sum(counts.values())}")
