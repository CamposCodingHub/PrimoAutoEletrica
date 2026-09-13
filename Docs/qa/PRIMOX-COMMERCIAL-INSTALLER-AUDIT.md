# PRIMOX — COMMERCIAL INSTALLER AUDIT (Script 7) — reescrito em camadas

**Papel:** Relatório de **avanço** de packaging/signing (09–10/09/2026) + ponte para estado atual  
**Branch na época:** `main` · HEAD baseline `38a86b4`  
**Atualizado (camada atual):** 2026-09-13

---

## Estado atual (2026-09-13)

| Item | Valor |
|------|-------|
| Trabalho ativo | `migration/net10` @ `1372e11` · TFM `net10.0-windows` |
| Tag `v1.0.0` | `72d85fa` **intacta** |
| Code signing | Continua **BLOCKED_EXTERNAL** (sem cert comercial) |
| Pipeline Inno / AppData policy | Continuam a base comercial |
| Deploy NET10 desktop | Ver `PRIMOX-NET10-DESKTOP-DEPLOY.md` |
| Fiscal | Fundação NET10-26; live blocked — **não** misturar com este audit de installer |

---

## Avanços desta fase (registro histórico Script 7)

### Decisão da época
**YELLOW** — `READY FOR COMMERCIAL SIGNING` · `CODE SIGNING BLOCKED BY EXTERNAL CERTIFICATE`

### Identidade / metadata
Publisher/Company alinhados a **CamposCodingHub**; produto **PRIMOX Workshop** 1.0.0; DefaultDir `{commonpf}\PRIMOX\Workshop`; AppData preservado no uninstall.

### Packaging
- Publish win-x64 self-contained PASS  
- ISCC PASS · manifest PASS  
- Signing hook BLOCKED (sem thumbprint Code Signing)  
- Cert `localhost` rejeitado pelo pipeline  

### SHA256 (época)
| Artefato | SHA256 |
|----------|--------|
| `PRIMOX-Workshop-Setup-1.0.0.exe` | `B1AAE306EE7E4108C8FDCF1622B03FA6992CFDEFA55F3151D7B275531F2799F2` |
| PackagingE2E rebuild | `9B742BA201E1D333970ECB5CB6D6A481D5EA1AB96677FB4B248295242579B557` |

### QA da época
Fiscal unit 46/46 · QaEngine 43/43 · Uninstall silencioso E2E com limitação/timeout documentada (não inventar PASS).

### Como assinar quando houver certificado
1. Instalar Authenticode OV/EV  
2. `$env:PRIMOX_CODESIGN_THUMBPRINT`  
3. `Check-CodeSigningReadiness.ps1` → READY_TO_SIGN  
4. `Build-PrimoXCommercialRelease.ps1`  
5. `signtool verify /pa`

---

## O que mudou depois

- Migração NET10 e deploy desktop documentados em relatórios NET10-20…24  
- Fundação fiscal NET10-26 (não altera a conclusão de signing deste Script 7)  
- Unit 46 fiscais → cobertura fiscal expandida (194 unit total)

**Este arquivo continua sendo a referência do avanço de installer/signing — não da fiscalidade atual.**
