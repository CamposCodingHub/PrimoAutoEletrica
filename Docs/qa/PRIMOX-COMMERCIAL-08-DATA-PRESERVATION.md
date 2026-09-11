# PRIMOX-COMMERCIAL-08 — Data Preservation

## Política do produto

- **Binários** (`{app}` / Program Files ou DIR de teste): removidos no uninstall  
- **Dados** (`%LOCALAPPDATA%\PrimoAutoEletrica` e data dirs isolados de teste): **preservados**  
- Uninstall **não** usa `UninstallDelete` em AppData

## Dados criados (controle)

| Campo | Valor |
|-------|-------|
| Cliente | `PRIMOX INSTALLER TEST CLIENT` |
| Veículo | `PRIMOX-TEST-001` |
| OS note | `PRIMOX INSTALLER E2E TEST` |
| Armazenamento | Tabela marker `C08_InstallerMarker` no DB de teste isolado |
| Marker produção | `%LOCALAPPDATA%\PrimoAutoEletrica\c08-retention-marker.txt` |

Ferramenta: `Scripts/tools/Commercial08DataSeed/` (não altera schema de negócio / migrations).

## Uninstall

Após cada uninstall silencioso (3 ciclos):

- `primoauto.db` do dataDir de teste **ainda existe**
- Marker de produção **ainda existe**
- Conteúdo marker legível: `PRIMOX INSTALLER TEST CLIENT|PRIMOX-TEST-001|PRIMOX INSTALLER E2E TEST`
- `integrity=ok`

## Reinstall

Após reinstall:

- Mesmos dados encontrados (**PASS**)
- App inicia (**PASS**)
- QaEngine no pacote instalado (**PASS** Exit=0) — CRUD/navegação no DB preservado/criado

## CRUD pós-reinstall

| Evidência | Resultado |
|-----------|-----------|
| QaEngine instalado (dataDir fresco pós-reinstall path) | Exit=0 |
| migrations | 28 |
| integrity | ok |
| fk | 0 |

## Conclusão

**INSTALLER ≠ DATA DESTRUÍDA** — confirmado.
