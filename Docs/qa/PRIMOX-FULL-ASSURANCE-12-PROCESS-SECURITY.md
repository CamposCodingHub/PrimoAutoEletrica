# PRIMOX FULL ASSURANCE-12 — PROCESS SECURITY

## Control

New `SecureProcessLauncher`:

- `OpenUri` — http/https/mailto only  
- `OpenWhatsAppLink` — wa.me only  
- `OpenFileOrDirectory` — must exist under authorized roots (AppData Media/Exports/Imports/Logs/Backups, Temp)

## Migrated call sites (A12)

- Clientes WhatsApp / file open (control + visualizar/editar)  
- Produto anexo open (Novo/Editar)  
- Estoque etiqueta PDF open  
- Configurações “abrir pasta”

## Remaining REVIEW

Other `Process.Start` usages (OrdensServico, Orcamentos, ImportarNFe, Agendamentos, OficinaKanban) still shell-open; most are fixed URL/PDF generators under app data. Migrate opportunistically.

## Smoke

`A12Security:ProcessSecurity:UriSchemeGate` PASS (rejects `file://` and non-wa.me).
