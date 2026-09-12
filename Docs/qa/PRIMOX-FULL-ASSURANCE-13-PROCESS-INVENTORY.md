# PRIMOX FULL ASSURANCE-13 — PROCESS.START INVENTORY

**Data:** 11/09/2026  
**HEAD baseline:** `5cd5549`  
**Escopo:** inventário completo pós-migração A13 (produto + launcher).

## Resumo

| Classificação | Quantidade |
|---|---|
| A — SAFE | 0 |
| B — HARDENED | 22 call sites produto + 2 Process.Start no launcher |
| C — REVIEW | 0 |
| D — UNSAFE | 0 |

**Process.Start residual no produto (fora de `SecureProcessLauncher`):** **0**

---

## Launcher (única superfície de Process.Start)

### A13-P-001
- **arquivo:** `PrimoAutoEletrica/Services/SecureProcessLauncher.cs`
- **linha:** ~34 (`OpenUri`)
- **função:** `OpenUri`
- **origem do input:** callers (URLs http/https/mailto já montadas)
- **input controlado pelo usuário?:** indireto (telefone/email/mensagem via callers)
- **FileName:** URI validada
- **Arguments:** (nenhum)
- **UseShellExecute:** true
- **WorkingDirectory:** default
- **validation:** scheme allowlist + rejeição de `" ' ; | \` CR LF`
- **sanitization:** trim; WhatsApp via `OpenWhatsAppLink`
- **SecureProcessLauncher:** N/A (é o próprio)
- **risco:** baixo — UseShellExecute em URI tipada
- **classificação:** **B — HARDENED**

### A13-P-002
- **arquivo:** `PrimoAutoEletrica/Services/SecureProcessLauncher.cs`
- **linha:** ~60 (`OpenFileOrDirectory`)
- **função:** `OpenFileOrDirectory`
- **origem do input:** path de arquivo/pasta
- **input controlado pelo usuário?:** parcial (anexos/pastas sob raízes autorizadas)
- **FileName:** path normalizado
- **Arguments:** (nenhum)
- **UseShellExecute:** true
- **WorkingDirectory:** default
- **validation:** `PathSecurityHelper.RequireUnderAnyRoot` + exists
- **sanitization:** `NormalizeFullPath`
- **SecureProcessLauncher:** sim
- **risco:** baixo — path jail
- **classificação:** **B — HARDENED**

---

## Call sites migrados (produto) — todos via SecureProcessLauncher

| ID | Arquivo | Função / uso | Input usuário? | API | Classificação |
|---|---|---|---|---|---|
| A13-C-01 | `UserControls/ClientesControl.xaml.cs` | WhatsApp | telefone (sanitizado) | OpenWhatsAppLink | B |
| A13-C-02 | `Views/Clientes/VisualizarClienteWindow.xaml.cs` | WhatsApp | telefone | OpenWhatsAppLink | B |
| A13-C-03 | `Views/Clientes/VisualizarClienteWindow.xaml.cs` | abrir arquivo | path sob Media | OpenFileOrDirectory | B |
| A13-C-04 | `Views/Clientes/EditarClienteWindow.xaml.cs` | WhatsApp | telefone | OpenWhatsAppLink | B |
| A13-C-05 | `Views/Clientes/EditarClienteWindow.xaml.cs` | abrir arquivo | path sob Media | OpenFileOrDirectory | B |
| A13-C-06 | `Views/NovoProdutoWindow.xaml.cs` | abrir anexo | path resolvido | OpenFileOrDirectory | B |
| A13-C-07 | `Views/EditarProdutoWindow.xaml.cs` | abrir anexo | path resolvido | OpenFileOrDirectory | B |
| A13-C-08 | `ViewModels/EstoqueViewModel.cs` | etiqueta PDF | path gerado | OpenFileOrDirectory | B |
| A13-C-09 | `Views/ConfiguracoesSistemaWindow.xaml.cs` | abrir pasta | path config | OpenFileOrDirectory | B |
| A13-C-10 | `UserControls/OficinaKanbanControl.xaml.cs` | WhatsApp | URL wa.me | OpenWhatsAppLink | B |
| A13-C-11 | `UserControls/ImportarNFeControl.xaml.cs` | pasta Imports | AppData/Imports | OpenFileOrDirectory | B |
| A13-C-12 | `UserControls/OrcamentosControl.xaml.cs` | PDF temp | Temp/PDF | OpenFileOrDirectory | B |
| A13-C-13 | `UserControls/OrcamentosControl.xaml.cs` | WhatsApp | wa.me | OpenWhatsAppLink | B |
| A13-C-14 | `UserControls/OrcamentosControl.xaml.cs` | mailto | email cliente | OpenUri | B |
| A13-C-15 | `UserControls/OrdensServicoControl.xaml.cs` | PDF Documents | MyDocuments/Primo… | OpenFileOrDirectory | B |
| A13-C-16 | `UserControls/OrdensServicoControl.xaml.cs` | WhatsApp | wa.me | OpenWhatsAppLink | B |
| A13-C-17 | `ViewModels/AgendamentosViewModel.cs` | WhatsApp | wa.me+text | OpenWhatsAppLink | B |
| A13-C-18 | `ViewModels/AgendamentosViewModel.cs` | mailto | email | OpenUri | B |

## Notas

- `PathSecurityHelper.GetDefaultOpenFileRoots` inclui AppData (Media/Exports/Imports/Logs/Backups), `MyDocuments\PrimoAutoEletrica(+PDFs)`, Temp.
- Testes ofensivos (canário fora das raízes, `cmd.exe`, `file://`, metacharacters, traversal) via smoke `A13Security` → **PASS**.
- Harness/testes (`UiSmokeTestService.Assurance12/13`) chamam o launcher apenas para asserts de rejeição — não são superfícies de produto.
