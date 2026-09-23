import os
import re
import json

def inspect_user_controls_and_views():
    user_controls = []
    for root, _, files in os.walk("PrimoAutoEletrica/UserControls"):
        for f in files:
            if f.endswith(".xaml"):
                p = os.path.join(root, f)
                with open(p, "r", encoding="utf-8", errors="ignore") as fp:
                    content = fp.read()
                cs_path = p + ".cs"
                cs_content = ""
                if os.path.exists(cs_path):
                    with open(cs_path, "r", encoding="utf-8", errors="ignore") as fp:
                        cs_content = fp.read()
                user_controls.append({
                    "name": f,
                    "xaml_lines": len(content.splitlines()),
                    "cs_lines": len(cs_content.splitlines()),
                    "data_context_matches": re.findall(r'([A-Za-z0-9_]+ViewModel)', content + " " + cs_content),
                    "services_referenced": re.findall(r'([A-Za-z0-9_]+Service)', cs_content)
                })
                
    for uc in sorted(user_controls, key=lambda x: x['name']):
        print(f"UC: {uc['name']:<35} | VMs: {list(set(uc['data_context_matches']))} | Svc: {list(set(uc['services_referenced']))[:3]}")

if __name__ == "__main__":
    inspect_user_controls_and_views()
