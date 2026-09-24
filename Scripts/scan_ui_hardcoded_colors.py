import glob
import re

def main():
    hardcoded_colors = []
    for f in glob.glob('PrimoAutoEletrica/**/*.xaml', recursive=True):
        if 'Themes' in f:
            continue
        with open(f, 'r', encoding='utf-8', errors='ignore') as fp:
            for idx, line in enumerate(fp, 1):
                # match Background="#..." or Foreground="#..."
                m = re.search(r'(Background|Foreground|BorderBrush)="(#(?!00|FF)[0-9A-Fa-f]{6,8})"', line)
                if m:
                    hardcoded_colors.append((f, idx, m.group(1), m.group(2)))

    print(f"Total hardcoded hex colors outside Themes: {len(hardcoded_colors)}")
    for item in hardcoded_colors[:15]:
        print(f"  {item[0]}:{item[1]} - {item[2]}={item[3]}")

if __name__ == "__main__":
    main()
