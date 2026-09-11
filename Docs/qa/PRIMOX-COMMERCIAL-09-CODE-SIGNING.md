# PRIMOX-COMMERCIAL-09 — Code Signing & Authenticode Readiness

**Fase:** PRIMOX-COMMERCIAL-09-2026-09  
**Data:** 2026-09-10  
**HEAD inicial:** `1c283f0`  
**Tag `v1.0.0`:** `72d85fa` **intacta**  
**Decisão:** **YELLOW — BLOCKED BY EXTERNAL COMMERCIAL CERTIFICATE**

```text
NÃO SIMULAR CODE SIGNING
Certificate localhost / fake ≠ assinatura comercial
```

## 1. Baseline

| Item | Valor |
|------|-------|
| Branch | `main` |
| Worktree | WIP fiscal preservado |
| Versão | 1.0.0 / 1.0.0.0 |
| TFM | net6.0-windows |
| Pipeline | `Scripts/Build-PrimoXCommercialRelease.ps1` |
| Installer | `Installer/PrimoAutoEletrica.iss` |
| Script oficial | `Scripts/Sign-PRIMOX.ps1` |
| Readiness wrapper | `Scripts/Check-CodeSigningReadiness.ps1` |
| Teste | `Scripts/Test-CodeSigningReadiness.ps1` |

## 2. Inventário de binários (publish win-x64 self-contained)

| Classe | Exemplos | Assinar? |
|--------|----------|----------|
| EXE principal | `PrimoAutoEletrica.exe` | **SIM** (após publish, antes do Inno) |
| Setup | `PRIMOX-Workshop-Setup-1.0.0.exe` | **SIM** (após ISCC) |
| Runtime DLLs (.NET) | ~349 `*.dll` | **NÃO** (runtime embutido; superfície comercial = EXE+Setup) |
| Auxiliar | `createdump.exe` | **NÃO** (diagnóstico, não atalho do produto) |
| Dados | `primoauto.db` | N/A — nunca no pacote |

**Ordem de assinatura (estrutura real):**

1. Publish self-contained  
2. Assinar `PrimoAutoEletrica.exe`  
3. Compilar Inno (Setup embute EXE já assinado)  
4. Assinar Setup  
5. Verificar assinaturas  
6. SHA256  

## 3. SignTool

| Item | Status |
|------|--------|
| `signtool.exe` | **READY** |
| Local | Windows Kits 10 `bin\10.0.26100.0\x64\signtool.exe` |
| Instalação silenciosa | **não** realizada nesta fase |

## 4. Certificado

| Item | Status |
|------|--------|
| Cert comercial Code Signing (OV/EV) | **BLOCKED / ABSENT** |
| Store private key | 1× `CN=localhost` (Server Auth) — **rejeitado** |
| `PRIMOX_CODESIGN_THUMBPRINT` | **ABSENT** (sessão de teste) |
| Teste forçado com thumbprint localhost | **BLOCKED_NOT_COMMERCIAL** · arquivos **não** alterados |

## 5. Configuração (sem segredos no Git)

```powershell
$env:PRIMOX_CODESIGN_THUMBPRINT = '<thumbprint do cert comercial no store>'
# opcional:
$env:PRIMOX_CODESIGN_TIMESTAMP_URL = 'http://timestamp.digicert.com'
```

- Não versionar PFX / senha / chave privada / thumbprint real em arquivos do repo.  
- PFX, se usado, permanece **externo** (importar no Certificate Store).

## 6. Script `Sign-PRIMOX.ps1`

| Mode | Comportamento |
|------|----------------|
| `Readiness` | Detecta signtool, store, EKU, localhost; exit 2 se BLOCKED |
| `Sign` | Exige readiness READY; assina com `/fd SHA256` + `/tr` RFC3161; verifica |
| `Verify` | `Get-AuthenticodeSignature` (+ signtool verify se Valid) |

Pipeline comercial chama o mesmo script via `Build-PrimoXCommercialRelease.ps1`.

## 7. Timestamp

| Item | Status |
|------|--------|
| RFC 3161 | **READY** |
| Default | DigiCert `http://timestamp.digicert.com` |
| Override | `PRIMOX_CODESIGN_TIMESTAMP_URL` (valor não ecoado se custom) |

## 8. Validação (sem cert comercial)

| Alvo | Status Authenticode |
|------|---------------------|
| `PrimoAutoEletrica.exe` (publish) | **NotSigned** |
| Setup 1.0.0 | **NotSigned** |
| Readiness harness | **BLOCKED BY EXTERNAL CERTIFICATE** (esperado) |
| Hash inalterado no readiness/sign abort | **PASS** |

Não declarar PASS de assinatura sem certificado comercial.

## 9–11. Pipeline / Installer

Fluxo oficial inalterado em intenção:

BUILD → PUBLISH → SIGN EXE → ISCC → SIGN SETUP → VERIFY → SHA256 → (install E2E fora desta fase)

Assinatura **não** quebra Inno/uninstall/AppData (hooks só pós-artefato).  
Installer E2E completo com assinatura: **BLOCKED** até cert comercial (Commercial-08 lifecycle permanece válido sem signing).

## 12. Teste sem certificado

Evidência: `TestResults/Commercial09/codesign-readiness-*.md`

- Ausência detectada claramente  
- Localhost rejeitado  
- Nenhum arquivo modificado  
- Exit readiness = 2  

## 13. Teste com certificado

**NÃO EXECUTADO** — certificado comercial ausente.

## 14. Segurança

| Check | Resultado |
|-------|-----------|
| `*.pfx` / `*.p12` / `*.pem` / `*.key` tracked | **0** |
| Private key markers em Scripts | **0** |
| Thumbprint hardcoded | **0** |
| Segredo em logs do Sign-PRIMOX | **não** |

## 15. QA

| Suite | Resultado | Nota |
|-------|-----------|------|
| Build Release | **PASS** 0 errors | |
| Unit tests | **PASS** 162/162 | |
| QaEngine / Deep / Exhaustive / LongRun | **PASS** (baseline COMMERCIAL-08 `c08-regression-20260910-203756`) | Sem alteração de código de produto |
| DB / Fiscal / I18N | **PASS** | Não tocados |

## 16. Limitations

- Compra/instalação de certificado Authenticode OV/EV: **externa**  
- SmartScreen reputation: após assinatura + distribuição  
- Signing real EXE/Setup: **BLOCKED** até cert  

## 17. Decision

```text
YELLOW
CODE SIGNING READY / BLOCKED BY EXTERNAL COMMERCIAL CERTIFICATE
```

Cadeia técnica pronta. Assinatura de produção aguarda certificado comercial no store + `PRIMOX_CODESIGN_THUMBPRINT`.
