# PRIMOX FULL ASSURANCE-12 — PROCESS SECURITY

> **RELATÓRIO DE AVANÇO / FASE — 2026-09-13**
>
> Este arquivo registra **melhorias e evidências da fase em que foi escrito**.
> **Não** é inventário operacional atual.
>
> Verdade atual: `Docs/CURRENT-TRUTH.md` · Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md` · Índice: `Docs/DOCUMENTATION-INDEX.md`
> HEAD pós-NET10-26: `1372e11` · TFM `net10.0-windows`

---

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
