# PRIMOX RELEASE / DISTRIBUTION READINESS — 01 — FINAL REPORT

**Data:** 12/09/2026  
**Missão:** Preparar distribuição comercial 1.0.0 sem alterar núcleo estável desnecessariamente  
**Baseline HEAD:** `58b8e1f`  
**Final HEAD:** `1b948ea`  
**Tag v1.0.0:** `72d85fa` → commit `a4ad6fe` **PRESERVED**  
**Branch:** `main`

## Decisão

**OVERALL: YELLOW**

**COMMERCIAL RELEASE READY WITH SIGNING BLOCKER**

Produto, installer lifecycle, QA e packaging: **READY**.  
Code signing comercial: **BLOCKED** (externo).  
Fiscal LIVE: **EXTERNAL BLOCKED** (não executado nesta fase).

Não declarar “fully commercial released” enquanto assinatura comercial estiver ausente.

---

## Alterações de produto (cirúrgicas)

| Mudança | Justificativa |
|---|---|
| 9 janelas: título `Primo Auto Eletrica` → `PRIMOX` | Superfície comercial legado |
| Publish comercial: `DebugType=None` + strip `*.pdb` | Higiene do pacote; PDB não entra no setup |

Sem redesign, sem schema, sem fiscal, sem auto-update, sem rename de namespaces/EXE/AppData.

---

## Artefato oficial

| Campo | Valor |
|---|---|
| Setup | `artifacts/installer/PRIMOX-Workshop-Setup-1.0.0.exe` |
| Size | 61 687 781 bytes |
| SHA256 | `0BECE6AE5E7C582C51C3B81783DE8557A81F70881B82CA2505373433988607BF` |
| Publish EXE | `artifacts/publish/win-x64/PrimoAutoEletrica.exe` |
| EXE SHA256 | `E5EE9DF4E3EE40E9F152AB1EC66061F47217DA0FD31EDCD6C90318BD1CDA9995` |
| Publish | self-contained `win-x64` · Trim=false · SingleFile=false · **PDB=0** · 590 files |
| Signature | **UNSIGNED** |

PackagingE2E (AppId isolado): `PRIMOX-Workshop-Setup-1.0.0-PackagingE2E.exe` SHA `2A884019…F93313` — usado nos ciclos E2E.

---

## Gates (evidência RELEASE-01)

| Suite | Resultado | Evidência |
|---|---|---|
| Build Debug/Release | PASS (0 errors) | sessão |
| Unit | **173/173 PASS** | `TestResults/Release01/20260912/Unit/unit-release01.trx` |
| QaEngine | **43/43 PASS** | `…/QaEngine/ui-smoke-summary.json` |
| DeepQa | **6/6 PASS** | `…/DeepQa-final/` |
| ExhaustiveUi | **PASS** | `…/ExhaustiveUi-final/` (~24 min) |
| LongRun | PASS | `…/LongRun/` |
| I18n07 | PASS | `…/I18n07/` |
| Tema | PASS | `…/Tema/` |
| A12Security | **3/3 PASS** | `…/A12Security/` |
| Installer E2E 3 ciclos | **fails=0** | `TestResults/Commercial08/commercial-08-e2e-20260912-074302.md` |
| Signing readiness | PIPELINE READY / CERT **BLOCKED** | `PRIMOX-RELEASE-01-SIGNING-READINESS.md` |

Performance: herda A13 — **YELLOW / OBSERVATION** (~71→179 MB; handles 366→734). Sem nova investigação.

Security: herda A13 **GREEN COM LIMITAÇÕES** + regressão A12Security PASS; Critical/High/Medium product = 0.

---

## Final gates

| Gate | Cor |
|---|---|
| PRODUCT | **GREEN** |
| SECURITY | **GREEN** (com limitações A13 conhecidas) |
| DATABASE | **GREEN** |
| INSTALLER | **GREEN** |
| PERFORMANCE | **YELLOW** |
| FISCAL | **YELLOW** (LIVE EXTERNAL BLOCKED) |
| SIGNING | **RED / BLOCKED** (externo; pipeline READY) |
| **OVERALL** | **YELLOW** |

---

## Blockers

### INTERNAL PRODUCT BLOCKERS
Nenhum P0/P1/P2 de produto nesta fase.

### EXTERNAL BLOCKERS
- Certificado de code signing comercial ausente  
- Fiscal LIVE (credenciais / provedor / homologação real)  
- SmartScreen reputation (não verificável sem assinatura + distribuição)

### KNOWN LIMITATIONS
- Calendar Dark Header (visual conhecido)  
- Performance YELLOW (crescimento esperado A13)  
- EXE / AppData / namespaces `PrimoAutoEletrica` (nomes técnicos intencionais)  
- Auto-update NOT IMPLEMENTED  
- Pacote não bit-identical deterministic (timestamp / rebuild muda SHA)

### OUT OF SCOPE
NFC-e · NFS-e · SaaS · multi-tenant · mobile · Assurance-14 · auto-update · compra de certificado

---

## Documentação desta fase

- `Docs/qa/PRIMOX-RELEASE-01-INVENTORY.md`
- `Docs/qa/PRIMOX-RELEASE-01-CAPABILITY-MATRIX.md`
- `Docs/qa/PRIMOX-RELEASE-01-PACKAGE-AUDIT.md`
- `Docs/qa/PRIMOX-RELEASE-01-INSTALLER.md`
- `Docs/qa/PRIMOX-RELEASE-01-SIGNING-READINESS.md`
- `Docs/qa/PRIMOX-RELEASE-01-DATA-POLICY.md`
- `Docs/qa/PRIMOX-RELEASE-01-FINAL-REPORT.md`

---

## STOP

Não push. Não mover `v1.0.0`. Não Fiscal LIVE. Não comprar certificado. Não auto-update / SaaS / mobile. Não Assurance-14.
