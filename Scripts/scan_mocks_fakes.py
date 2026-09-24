import os
import glob
import re

def search_patterns():
    patterns = {
        "NotImplemented": r"throw\s+new\s+NotImplementedException",
        "TODO": r"//\s*TODO\b|/\*\s*TODO\b",
        "Mock": r"\b(Mock|Fake|Stub)\w*",
        "Scaffold": r"\b(Scaffold|CommercialScaffold)\w*",
        "Sample": r"\b(Sample|Exemplo)\w*"
    }
    
    results = {k: [] for k in patterns}
    
    cs_files = glob.glob('PrimoAutoEletrica/**/*.cs', recursive=True)
    for f in cs_files:
        with open(f, 'r', encoding='utf-8', errors='ignore') as fp:
            for idx, line in enumerate(fp, 1):
                for p_name, regex in patterns.items():
                    if re.search(regex, line, re.IGNORECASE):
                        results[p_name].append((f, idx, line.strip()))
                        
    print("=== SCAN RESULTS ===")
    for k, v in results.items():
        print(f"{k}: {len(v)} occurrences")
        
    print("\n--- NotImplemented Details ---")
    for f, line_no, content in results["NotImplemented"]:
        print(f"{f}:{line_no}: {content}")

    print("\n--- Scaffold Details ---")
    for f, line_no, content in results["Scaffold"][:15]:
        print(f"{f}:{line_no}: {content}")

    print("\n--- Selected Mocks/Fakes in Production Code (excluding Tests/Smoke) ---")
    prod_mocks = [item for item in results["Mock"] if "Test" not in item[0] and "Smoke" not in item[0]]
    print(f"Prod Mocks count: {len(prod_mocks)}")
    for f, line_no, content in prod_mocks[:15]:
        print(f"{f}:{line_no}: {content}")

if __name__ == "__main__":
    search_patterns()
