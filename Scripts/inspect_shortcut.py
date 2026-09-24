import re

lnk = r"C:\Users\campo\OneDrive\Desktop\PRIMOX Workshop.lnk"
with open(lnk, "rb") as f:
    content = f.read()

paths = re.findall(rb'[A-Za-z]:\\[A-Za-z0-9_\-\\ ]+', content)
for p in set(paths):
    try:
        print(p.decode("latin1"))
    except:
        pass
