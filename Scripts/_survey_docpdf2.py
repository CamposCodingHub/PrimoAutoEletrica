from pathlib import Path
t = Path('PrimoAutoEletrica/Services/DocumentoPdfService.cs').read_text(encoding='utf-8')
# print GerarChecklist and GerarTermoGarantia bodies
for name in ['GerarChecklist', 'GerarTermoGarantia', 'GerarOrdemServico', 'GerarLaudo', 'Laudo']:
    i = t.find(name)
    print('===', name, 'at', i, '===')
    if i >= 0:
        print(t[i:i+900])
        print()

# Who calls these?
import subprocess, os
print('=== callers ===')
