# PRIMOX — Integration Matrix 1.0

**Audit:** Total Codebase + Integration Audit 1.0 · HEAD `d0e69b7` · Tag `v1.0.0`→`a4ad6fe`  
**Hierarquia:** runtime > código > banco > testes > docs

| Área | Estado atual | Evidência | Problema | Prioridade | Próxima ação |
| ---- | ------------ | --------- | -------- | ---------- | ------------ |
| WhatsApp wa.me | REAL+TESTADO | `OficinaProfissionalService.CriarWhatsAppUrl`, Orcamentos/Agenda/OS/Clientes `Process.Start` | Não é Cloud API | P2 | Manter; documentar limites |
| WhatsApp Cloud API | NÃO IMPLEMENTADO | Sem Graph/Meta HTTP | Ausente | P2 | Decisão produto + IWhatsAppProvider |
| NotificationService WA/SMS | PLACEHOLDER | `Task.Delay` + return true; sem callers UI | Falso “Enviado” | P1 | Desligar fake success ou implementar |
| Twilio/SMS HTTP | NÃO IMPLEMENTADO | Sem SDK/package | — | P3 | Só se P0 comunicação SMS |
| E-mail SMTP | NÃO IMPLEMENTADO | Sem MailKit/SendGrid/SmtpClient send | — | P2 | mailto REAL permanece |
| E-mail mailto | REAL | Orcamentos/Agenda | Depende do cliente OS | P3 | KEEP |
| PIX gateway | NÃO IMPLEMENTADO | Sem Asaas/MP/Stripe | — | P3 | — |
| PIX forma pagamento | REAL+TESTADO | PDV/Financeiro string PIX | Só rótulo interno | — | KEEP |
| NF-e import | REAL+TESTADO | `NFeService` + smoke | — | — | KEEP |
| NF-e emissão SEFAZ | NÃO IMPLEMENTADO | `NFeEmissaoService.cs` 0 bytes | Compliance gap | P0 futuro | Decisão A/B provider |
| NFC-e | NÃO IMPLEMENTADO | Docs only | — | P1 futuro | Após NF-e |
| NFS-e | NÃO IMPLEMENTADO | Docs only | Municipal | P2 futuro | — |
| Certificado A1/A3 | NÃO IMPLEMENTADO | Sem X509/PFX uso | Cryptography.Xml package ORPHAN | P0 c/ fiscal | Com emissão |
| API REST | REAL+PARCIAL | Minimal maps health/orcamentos/estoque/financeiro | Sem auth | P1 | JWT + policies |
| JWT/Keycloak | SCAFFOLD | Packages + empty Keycloak json | Não wired | P1 | — |
| RBAC WPF | REAL | PermissionService | — | — | KEEP |
| RBAC API policies | NÃO IMPLEMENTADO | Sem AdminOnly etc. | — | P1 | — |
| Multi-filial | SCAFFOLD | FilialService hardcoded | Sem DB | P1 | Persistência |
| Sync remoto | NÃO IMPLEMENTADO | Sem ProcessarFilaOfflineAsync | — | P1 | Após API+filial |
| Sync LAN UDP | PARCIAL | LocalSyncService | Não multi-loja cloud | P3 | Documentar |
| SaaS/Tenant | NÃO IMPLEMENTADO | Arch docs only | — | P3 | — |
| LicenseService | SCAFFOLD/ORPHAN | JSON local; window não ligada | — | P3 | — |
| Backup/Restore | REAL+TESTADO | DatabaseBackupService + UI + smoke | — | — | KEEP |
| ExternalBackupService | ORPHAN | Zero refs | — | P4 | DEPRECATE |
| Pagamentos cartão/boleto gateway | NÃO IMPLEMENTADO | — | — | P3 | — |

## Custo relativo (qualitativo)

| Integração | Fornecedor | Infra | Manutenção | Lock-in | Risco |
|------------|------------|-------|------------|---------|-------|
| WhatsApp Cloud | ALTO | MÉDIO | MÉDIO | MÉDIO | MÉDIO |
| SMS/Twilio | MÉDIO | BAIXO | BAIXO | BAIXO | BAIXO |
| E-mail SMTP | BAIXO | BAIXO | BAIXO | BAIXO | BAIXO |
| Fiscal provider | ALTO | MÉDIO | MÉDIO | ALTO | ALTO (compliance) |
| SEFAZ direta | BAIXO R$ / ALTO eng | MÉDIO | MUITO ALTO | BAIXO | MUITO ALTO |
| PIX gateway | MÉDIO | MÉDIO | MÉDIO | MÉDIO | MÉDIO |
| Cloud SaaS | ALTO | ALTO | ALTO | MÉDIO | ALTO |
