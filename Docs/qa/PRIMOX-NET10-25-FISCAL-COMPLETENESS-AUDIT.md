# PRIMOX NET10-25 — Fiscal Completeness Audit

**Timestamp:** 2026-09-13 14:07–14:17 (America/Sao_Paulo)  
**Branch:** `migration/net10`  
**HEAD (início):** `5995de5e3d611656427a1268b811fd72fda8a5e7`  
**TFM produto:** `net10.0-windows`  
**Assembly/File/Info version:** `1.0.0` / `1.0.0.0` / `1.0.0`  
**Veredito:** **FISCAL AUDIT COMPLETE WITH BLOCKERS**

> Zero-trust: conclusões baseadas em código + execução CURRENT nesta sessão.  
> Evidência: `TestResults/Net10-Overnight/20260913/NET10-25-Fiscal-Audit/`

---

## Fase 0 — Proteção Git (CURRENT)

| Ref | Valor | Status |
|-----|-------|--------|
| branch | `migration/net10` | PASS |
| HEAD | `5995de5` | — |
| main | `29b19b1` | INTACTA |
| v1.0.0 | `a4ad6fe` | INTACTA |
| primox-net6-final | `29b19b1` | INTACTA |

PUSH/MERGE/TAG: **não executados**.

---

## Fase 1 — Identidade

| Item | Valor |
|------|-------|
| TargetFramework | `net10.0-windows` |
| RuntimeIdentifier (csproj) | não fixo (publish usa `win-x64`) |
| global.json SDK | `10.0.302` |
| Resíduo ACTIVE net6 produto | **0** |

---

## Fluxo arquitetural REAL (Fase 3)

```
UI (PDVControl.Fiscal / FiscalOperationsControl)
  ↓
NFeHomologationService / FiscalOperationsCenterService / FiscalApplicationService
  ↓
IFiscalProvider  →  FocusNfeProvider (DI)  |  FakeFiscalProvider (TEST ONLY)
  ↓
FocusNfeHttpClient (HTTP JSON Focus homolog)
  ↓
Focus NFe (homologacao.focusnfe.com.br)   [requer token]
  ↓
SEFAZ (via Focus — externo)
```

| Seta | Status |
|------|--------|
| UI → Service | **PRESENTE** |
| Service → IFiscalProvider | **PRESENTE** |
| Focus → HTTP homolog | **PRESENTE** (código) |
| HTTP → Focus live | **BLOCKED_EXTERNAL** sem token |
| PlugNotasProvider | **AUSENTE** (só enum) |
| Assinatura XML local / XSD | **AUSENTE** (JSON Focus) |
| Webhook inbound | **AUSENTE** |
| DANFE PDF | **AUSENTE** (contrato NotImplemented) |

---

## Inventário (Fase 2) — classes principais

| Arquivo | Classe | Finalidade | Status |
|---------|--------|------------|--------|
| `IFiscalProvider.cs` | IFiscalProvider | Porta Emitir/Consultar/Cancelar/Xml/Danfe | REAL (contrato) |
| `Focus/FocusNfeProvider.cs` | FocusNfeProvider | Adapter Focus NF-e homolog | PARTIAL (emit+consult HTTP; cancel/xml/danfe NI) |
| `Focus/FocusNfeHttpClient.cs` | FocusNfeHttpClient + PayloadBuilder + ResponseMapper | HTTP + JSON payload | REAL (homolog path) |
| `Testing/FakeFiscalProvider.cs` | FakeFiscalProvider | Cenários fake | FAKE_ONLY |
| `FiscalApplicationService.cs` | FiscalApplicationService | Idempotência, audit, orquestra | REAL |
| `FiscalOperationStore.cs` | FiscalOperationStore | Persistência ops/docs/events | REAL |
| `FiscalConfigurationService.cs` | FiscalConfigurationService | Config + DPAPI token | REAL |
| `FiscalDocumentValidator.cs` | FiscalDocumentValidator | Validação emitente/itens | REAL |
| `FiscalNFeModels.cs` | FiscalIssuerProfile / FiscalNFeDocument | Modelo NF-e interno | REAL |
| `FiscalNFePreviewBuilder.cs` | FiscalNFePreviewBuilder | Preview texto (NÃO DANFE) | REAL (preview only) |
| `FiscalStateMachine.cs` | FiscalStateMachine | Transições status | REAL |
| `FiscalProductionGuard.cs` | FiscalProductionGuard | Bloqueio produção | REAL |
| `FiscalHealthCheck.cs` | FiscalHealthCheck | Readiness homolog | REAL |
| `NFeHomologationService.cs` | NFeHomologationService | Fluxo PDV homolog | REAL (path) |
| `NFeEmissaoService.cs` | NFeEmissaoService | Emissão orquestrada | REAL (path) |
| `VendaFiscalNFeMapper.cs` | VendaFiscalNFeMapper | Venda → documento | REAL |
| `FiscalOperationsCenterService.cs` | FiscalOperationsCenterService | Centro UI | REAL |
| `PDVControl.Fiscal.xaml.cs` | UI emissão homolog | REAL (UI) |
| `FiscalOperationsControl.xaml*` | UI operações | REAL (UI) |
| `XmlProdutoParser.cs` / `NFeService.cs` | Importação XML compra | REAL (import ≠ emissão) |
| PlugNotas* | — | — | **NOT_IMPLEMENTED** |
| NFCe/NFSe services | — | enums only | **NOT_IMPLEMENTED** |

---

## Matriz por área (Fase 23)

| Área | Estado | Evidência | Pode sem CNPJ | Externo | Próxima fase |
|------|--------|-----------|---------------|---------|--------------|
| NF-e (modelo 55) | **PARTIAL** | Models+validator+Focus emit/consult+UI+Fake 46/46 | Sim (engine/Fake/UI/DB) | Token Focus + emitente | Completar cancel/XML/DANFE; homolog live |
| NFC-e (65) | **NOT_IMPLEMENTED** | Enum `NFCe` apenas | Sim (scaffold) | CSC/SEFAZ via provider | Spec + provider NFC-e |
| NFS-e | **NOT_IMPLEMENTED** | Enum `NFSe` apenas | Sim (scaffold) | Município/provider | Spec municipal |
| XML emissão | **PARTIAL** | JSON Focus payload; paths Xml* no DB; ObterXml=NI | Sim (armazenamento) | Focus XML | Download XML pós-autorização |
| XML import | **REAL** | `XmlProdutoParser` / ImportarNFe | Sim | — | Manter |
| XSD validation | **NOT_IMPLEMENTED** | Sem `.xsd` / schema check emissão | Sim | — | Adicionar se XML local |
| DANFE | **NOT_IMPLEMENTED** | `ObterDanfeAsync` → NotImplemented; Preview ≠ DANFE | Sim (PDF layout) | — | Gerar PDF DANFE |
| Consulta | **PARTIAL** | Focus GET + Fake + ApplicationService | Sim (Fake) | Token | Homolog live |
| Cancelamento | **PARTIAL** | Contrato+Fake+AppService; Focus=NotImplemented | Sim (Fake/UI) | Token+NF autorizada | Implementar cancel Focus |
| Inutilização | **NOT_IMPLEMENTED** | Sem código | Sim | Provider | Evento 110111 etc. |
| CC-e | **NOT_IMPLEMENTED** | Sem código | Sim | Provider | Evento CC-e |
| Manifestação | **NOT_IMPLEMENTED** | Sem código | Sim | Provider | Ciência/confirmação |
| Contingência | **SCAFFOLD** | Status enum Contingency; sem fluxo | Sim | SEFAZ | Desenhar fluxo |
| Webhook | **NOT_IMPLEMENTED** | Sem endpoint | — | Provider | Listener + idempotência |
| Provider Focus | **PARTIAL** | Emit+Consult HTTP; cancel/xml/danfe NI; prod blocked | — | Token | Homolog |
| Provider PlugNotas | **NOT_IMPLEMENTED** | Enum only | — | Conta PlugNotas | Adapter |
| Certificado A1/PFX | **NOT_IMPLEMENTED** | Config comenta secrets; sem load PFX (Focus usa token) | Parcial (store DPAPI) | Cert real se XML local | Só se sair do modelo Focus-token |
| Multiempresa | **NOT_IMPLEMENTED** | Um `Issuer` em `fiscal-foundation.json`; sem EmpresaId nas ops | Sim (schema) | — | Empresa N + configs |
| Banco fiscal | **REAL** | FiscalOperations/Documents/Events + índices + FK | Sim | — | Manter |
| Retry/Idempotência | **REAL** | IdempotencyKey UNIQUE; consult-before-retry; 429 mapped | Sim | — | Testes carga |
| Logs/Audit | **PARTIAL** | AuditLog + events; token não deve ir a log (DPAPI) | Sim | — | Review redaction |
| WhatsApp | **PARTIAL** | wa.me via `SecureProcessLauncher` (manual); sem API Business | Sim (infra deep-link) | Meta/Twilio | Provider API se necessário |

### Detalhe NF-e campos (Fase 4)

| Item | Classificação |
|------|----------------|
| Emitente CNPJ/IE/endereço/regime/série | REAL (modelo+validator+config) — dados vazios até configurar |
| NCM/CFOP/CEST/CSOSN/origem/qtd/valores | REAL (modelo+payload Focus parcial) |
| IPI/PIS/COFINS detalhados | PARTIAL / GAP (payload ICMS-centrado) |
| Frete/seguro/despesas avançados | PARTIAL / GAP |
| Chave/protocolo | REAL via provider response |
| Assinatura XML local | NOT_IMPLEMENTED (Focus assina) |
| Envio autorização | PARTIAL — código HTTP; live BLOCKED_EXTERNAL sem token |
| Cancelamento/inutilização/CC-e/contingência | NOT_IMPLEMENTED ou NI no Focus adapter |

---

## Providers (Fase 7)

| Provider | Classificação | Notas |
|----------|---------------|-------|
| FakeFiscalProvider | **FAKE_ONLY** | Não registrado no DI produção |
| FocusNfeProvider | **PARTIAL** | Homolog HTTP; produção bloqueada; cancel/xml/danfe NI |
| PlugNotas | **NOT_IMPLEMENTED** | |

---

## Fake tests (Fase 8) — CURRENT

```
dotnet test --filter FullyQualifiedName~Fiscal|FakeFiscal|Homolog
Exit=0 · Aprovado 46/46 · Ignorado 0 · ~1s
```

Evidência: `fiscal-fake.log` · 14:09:29

---

## Homologação (Fase 9)

| Item | Status |
|------|--------|
| Código HTTP Focus homolog | PRESENTE |
| DPAPI / env `PRIMOX_FOCUS_HOMOLOG_TOKEN` | PRESENTE (store) |
| Token nesta sessão | **AUSENTE** (`TOKEN_PRESENT=False`) |
| Emitente preenchido | depende config local (não inventar) |

**Classificação:** **BLOCKED_EXTERNAL** para live · arquitetura **READY_FOR_EXTERNAL_HOMOLOGATION** quando token + emitente válidos existirem (sem produção).

---

## Banco (Fase 10) — CURRENT isolado

A13Database smoke: **PASS** 1/1  
DB: `…/smoke-A13Database/appdata/primoauto.db`

| Check | Resultado |
|-------|-----------|
| PRAGMA integrity_check | **ok** (`fase10-pragma.txt`) |
| PRAGMA foreign_key_check | **0 rows** |
| Tabelas Fiscal* | FiscalOperations, FiscalDocuments, FiscalEvents |
| Idempotency UNIQUE | SIM |
| FK OperationId | SIM |

Banco comercial do usuário: **não alterado**.

---

## Certificado (Fase 12)

**NOT_IMPLEMENTED** para A1/PFX local. Modelo atual = **token Focus** em DPAPI (`fiscal-secrets.dpapi`). Sem pedra de certificado nesta auditoria.

---

## Eventos fiscais (Fase 17)

| Evento | Código | Implementado | Fake | Provider | UI | DB |
|--------|--------|--------------|------|----------|----|----|
| Emissão | — | PARTIAL | SIM | Focus emit | PDV/Centro | Events genéricos |
| Consulta | — | PARTIAL | SIM | Focus GET | Centro | SIM |
| Cancelamento | 110111* | PARTIAL (contrato) | SIM | Focus NI | Centro hook | Events |
| CC-e | 110110* | NÃO | NÃO | NÃO | NÃO | NÃO |
| Inutilização | 110111/inut | NÃO | NÃO | NÃO | NÃO | NÃO |
| Manifestação | 2102xx | NÃO | NÃO | NÃO | NÃO | NÃO |

\*códigos SEFAZ de referência; não hardcoded no produto.

---

## WhatsApp (Fase 21)

| Item | Status |
|------|--------|
| Campo/consentimento cliente | REAL |
| Deep-link `wa.me` + SecureProcessLauncher | REAL |
| IWhatsAppProvider / Meta / Twilio | **NOT_IMPLEMENTED** |
| Envio PDF/XML fiscal automático | **NOT_IMPLEMENTED** |
| Credencial externa | N/A |

---

## Testes regressão (Fase 24–25) — CURRENT

| Suite | Resultado | Horário |
|-------|-----------|---------|
| Build Debug | Exit **0** | 14:09:03 |
| Build Release | Exit **0** | 14:09:14 |
| Unit | **173/173** Exit 0 | 14:09:24 |
| Fiscal filter | **46/46** Exit 0 | 14:09:29 |
| QaEngine | **43/43** APROVADO | 14:14:33 |
| DeepQa | **6/6** APROVADO | 14:17:03 |
| A13Database | **1/1** APROVADO | 14:17:05 |

Nenhuma alteração de código de produto nesta fase (somente auditoria/docs). Regressão UI coberta por QaEngine/DeepQa.

---

## Grupos finais (Fase 22)

### GRUPO A — Resolvível sem CNPJ
Engine NF-e, validação, preview, Fake, state machine, idempotência/retry, DB, UI Centro/PDV hooks, logs, schema multiempresa futuro, DANFE PDF local, NFC-e/NFS-e scaffolds, WhatsApp deep-link, cancel Focus adapter (código), download XML pós-auth (código).

### GRUPO B — Depende de provider
Token Focus homolog, HTTP live, XML/DANFE vindos do Focus, PlugNotas, webhooks Focus, WhatsApp Business API.

### GRUPO C — Depende de empresa real
CNPJ/IE/endereço/regime/série reais, credenciamento SEFAZ, token produção, certificado se modelo local, emissão produção.

### GRUPO D — Não implementado
NFC-e completo, NFS-e, PlugNotas, XSD local, DANFE PDF, CC-e, inutilização, manifestação, webhook inbound, multiempresa, certificado A1 load, contingência operacional.

---

## Blockers

### INTERNAL (técnicos — não críticos para “auditoria”)
- Cancel/Xml/Danfe Focus = NotImplemented  
- NFC-e/NFS-e/PlugNotas ausentes  
- Multiempresa ausente  
- DANFE/XSD ausentes  

### EXTERNAL
- Sem `PRIMOX_FOCUS_HOMOLOG_TOKEN` nesta sessão  
- Sem emitente/CNPJ real (não solicitado)  
- Produção deliberadamente bloqueada  

---

## Veredito

**FISCAL AUDIT COMPLETE WITH BLOCKERS**

Fundação NF-e + Fake + Focus homolog path existem e são verificáveis.  
Não declarar “fiscal pronto para produção”. Live homologação e completude de eventos/DANFE/NFC-e/NFS-e permanecem abertos.
