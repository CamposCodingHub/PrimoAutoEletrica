import json

with open('TestResults/b1_comprehensive_audit_data.json', encoding='utf-8') as f:
    d = json.load(f)

print(f"TOTAL VIEWS: {len(d['views'])}")
for v in sorted(d['views'], key=lambda x: x['name']):
    print(f"{v['name']:<35} | VM: {v['vm']:<28} | Lines: {v['lines']:<5} | HardcodedCols: {v['has_hardcoded_colors']:<3} | Path: {v['rel_path']}")
