# PRIMOX — Fiscal Foundation (atualizado NET10-26)

**Status:** Fundação fiscal **implementada e testada** · Focus HTTP homolog preparado · Produção **bloqueada**  
**Atualizado:** 2026-09-13 · HEAD `1372e11`  
**Canônico:** [`PRIMOX-NET10-26-FISCAL-COMPLETE-IMPLEMENTATION.md`](../qa/PRIMOX-NET10-26-FISCAL-COMPLETE-IMPLEMENTATION.md)

---

## Objetivo

Arquitetura segura e testável para NF-e (e scaffolds NFC-e/NFS-e) **sem** emissão produção.

```text
PRIMOX
 ├── FiscalApplicationService (idempotência, audit, XML/DANFE orchestration)
 ├── FiscalEmpresaStore / FiscalOperationStore / FiscalArtifactStorage
 ├── IFiscalProvider
 │    ├── FocusNfeProvider (POST/GET/DELETE + XML download)
 │    ├── PlugNotasProvider (scaffold)
 │    └── FakeFiscalProvider (TEST ONLY)
 ├── IDanfeGenerator (PDF informativo)
 ├── ICertificateProvider / IXmlSigner (blocked sem cert)
 ├── IWhatsAppProvider (manual wa.me fallback)
 └── IFiscalWebhookProcessor (idempotente; sem host WPF)
```

---

## O que existe

| Layer | Conteúdo |
|-------|----------|
| Contratos | `IFiscalProvider`, requests/results, operations/documents |
| Guard | `FiscalProductionGuard` |
| Config | JSON + DPAPI token |
| DB | `202609080001` + `202609130001` (multiempresa/artefatos) |
| Focus | Emit, consult, cancel DELETE, ObterXml |
| Fake | Ciclos NFe/NFCe/NFSe + XML fixture |
| Testes | Unit 194 · fiscal mega/stress |

---

## Avanços desde Foundation 1.0 (08/09)

| Antes (1.0) | Depois (NET10-26) |
|-------------|-------------------|
| Focus NotImplemented cancel/XML | HTTP cancel + XML download |
| Sem HttpClient fiscal | `FocusNfeHttpClient` |
| Sem EmpresaId | `FiscalEmpresas` + EmpresaId |
| Sem DANFE | PDF informativo |
| Sem WhatsApp/Cert/Webhook ports | Abstrações + Fake/manual/blocked |

---

## Explicitamente NÃO

- Produção  
- “XML SEFAZ validado” sem XSD  
- DANFE layout oficial  
- Token/cert inventados
