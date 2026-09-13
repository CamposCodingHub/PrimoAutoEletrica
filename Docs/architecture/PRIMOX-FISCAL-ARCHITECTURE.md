# PRIMOX — Arquitetura Fiscal

**Status do documento:** LIVING · alinhado a NET10-26 (`1372e11`) · 2026-09-13  
**Canônico de classificação:** [`Docs/qa/PRIMOX-NET10-26-FISCAL-COMPLETE-IMPLEMENTATION.md`](../qa/PRIMOX-NET10-26-FISCAL-COMPLETE-IMPLEMENTATION.md)  
**Importação:** REAL (`NFeService`)  
**Tag `v1.0.0`:** intacta — emissão live **não** faz parte do significado comercial do 1.0.0

---

## 1. Escopo fiscal típico da oficina

| Operação | Documento | Estado atual (código) |
|----------|-----------|------------------------|
| Compra de peças (entrada) | NF-e fornecedor (XML) | REAL — import |
| Venda / OS (saída) | NF-e | PARTIAL — Focus homolog path + Fake; LIVE BLOCKED_EXTERNAL |
| Venda balcão | NFC-e | SCAFFOLD + Fake |
| Serviço | NFS-e | SCAFFOLD + Fake (`INfseProvider`) |
| DANFE | PDF | Informativo local (`IDanfeGenerator`); oficial via provider = externo |
| Cancelamento / consulta / XML | — | Focus HTTP + Fake testados; live precisa token |

---

## 2. Estados do documento

```text
Draft → Validating → Pending/Processing → Authorized | Rejected | Denied | Error | Unknown
                              ↘ Cancelled (após Authorized, homolog)
```

Produção: `FiscalProductionGuard` bloqueia por padrão.

---

## 3. Decisão arquitetural — Opção B (provedor)

**Mantida:** provedor especializado (Focus NFe principal; PlugNotas scaffold).

```text
UI / PDV / FiscalOperations
        │
        ▼
FiscalApplicationService (idempotência, audit, store)
        │
        ▼
IFiscalProvider
   ├── FocusNfeProvider  (POST/GET/DELETE + XML download homolog)
   ├── PlugNotasProvider (scaffold)
   └── FakeFiscalProvider (TEST ONLY — fora do DI comercial)
        │
FiscalEmpresas / EmpresaId / FiscalArtifactStorage / IDanfeGenerator
ICertificateProvider / IXmlSigner / IWhatsAppProvider / IFiscalWebhookProcessor
```

---

## 4. Multiempresa fiscal

`Empresa → Configuração → Série → Documento → Itens → Eventos`  
Persistência: `FiscalEmpresas` + `EmpresaId` nullable em operações/documentos/eventos (migration `202609130001`).

---

## 5. Tributação extensível

Totalização determinística (`FiscalItemTotaller`) sem inventar regra SEFAZ.  
Campos de item (NCM/CFOP/impostos) via modelo existente.  
**IBS/CBS / reforma 2026:** só com schema/fonte oficial — preparar extensão, não hardcode rígido exclusivo ICMS/IPI/PIS/COFINS.

---

## 6. Segurança

- Token Focus: DPAPI; nunca logar token/senha/PFX  
- Download XML Focus: host homolog + anti-SSRF  
- Artefatos: path-safe sob AppData  
- Webhook: processador idempotente **sem** host HTTP inseguro no WPF  
- Produção: bloqueada

---

## 7. O que NÃO afirmar

- “XML SEFAZ VALIDADO” sem XSD  
- “DANFE oficial” para PDF informativo  
- “PRODUCTION READY” / emissão live sem evidência  
- Endpoints PlugNotas inventados
