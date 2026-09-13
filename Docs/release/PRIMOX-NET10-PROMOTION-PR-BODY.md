# PR body (preparação — PR **não** criado automaticamente)

> **DOCUMENTO REESCRITO EM CAMADAS — 2026-09-13**
>
> | Camada | Uso |
> |--------|-----|
> | **Estado atual** | Fonte operacional hoje · ver também `Docs/CURRENT-TRUTH.md` e NET10-26 |
> | **Avanços desta fase (histórico)** | Registro do que esta execução entregou — **não** sobrescrever mentalmente o estado atual |
>
> HEAD de referência pós-NET10-26: `1372e11` · TFM `net10.0-windows` · Branch `migration/net10`
> Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md`

## Estado atual (pós NET10-26 · 2026-09-13)

| Item | Valor |
|------|-------|
| Branch | `migration/net10` |
| HEAD fiscal foundation | `1372e11` |
| TFM | `net10.0-windows` |
| Unit | **194/194** |
| QaEngine | **43/43** |
| DeepQa | **6/6** (baseline NET10-26) |
| Fiscal LIVE / WhatsApp API / Code signing | **BLOCKED_EXTERNAL** |
| Calendar Dark | Mitigado (`CalendarContrastHealer`) — não citar KNOWN LIMITATION antigo como atual |
| NF-e | PARTIAL + TESTED (Focus path + Fake) |
| NFC-e / NFS-e | SCAFFOLD + FAKE_ONLY |
| DANFE | PDF informativo (≠ SEFAZ oficial) |
| Multiempresa fiscal | IMPLEMENTED + TESTED (DB) |

**Claims abaixo sobre net6, “emissão NÃO IMPLEMENTADO”, Unit 173, DANFE/cancel NI, Calendar Dark KNOWN LIMITATION, etc. pertencem ao registro histórico da fase.**

---

## Avanços desta fase (registro histórico — preservar)

**TITLE:** `release: PRIMOX Workshop 1.1.0 NET10`

**SOURCE:** `migration/net10`  
**TARGET:** `main`  
**PR_CREATION:** **NOT_EXECUTED** (`gh` sem login nesta sessão)

---

## Summary

- Promove o produto WPF de .NET 6 para .NET 10 (`net10.0-windows`) com hardening visual e limpeza de dependências.
- Mantém superfície funcional (navegação/módulos) alinhada ao baseline `main`.
- Inclui evidência de build, testes, DB isolado, segurança, publish, installer PackagingE2E e desktop.

## Why

O runtime .NET 6 está em ciclo de suporte encerrado para novas linhas; NET10 é o TFM alvo validado na trilha `migration/net10` com gates reproduzíveis.

## .NET migration

- TFM produto: `net6.0-windows` → `net10.0-windows`
- Pacotes: Microsoft.Data.Sqlite 9.0.9, SQLitePCLRaw 3.0.5, Extensions 9.0.9
- Remoção LiveCharts (NU1701/OpenTK)

## Functional regression

- `NavigationService` module map: **sem alteração** vs `main`
- Journey 12 módulos + Tema + Calendar: **fails=0** (esta sessão)
- QaEngine 43/43 · DeepQa 6/6 · Unit 173/173

## Security

- A12Security 3/3 · A13Security 1/1 · RedTeam Critical=0 High=0

## Database

- A13Database PASS · Configurações backup/restore PASS · ambiente isolado (`--app-data`)

## Installer

- PackagingE2E `PRIMOX-Workshop-Setup-1.1.0-PackagingE2E.exe` · 3 ciclos install/start/smoke/uninstall · data preservation PASS
- Comercial `1.0.0` **não** sobrescrito

## Desktop deployment

- `C:\Program Files\PRIMOX\Workshop` em net10 · smoke Login/PreCheck/Tema/Clientes/Dashboard PASS
- Calendar captura visual em Program Files: LIMITATION (ACL)

## Performance

- Startup×10 (process Responding) avg ~1557 ms · fails=0  
- NET6_COMPARE = BLOCKED_EXTERNAL

## Fiscal status

- Fake/unit (filtro Fiscal|FakeFiscal|Homolog): **46/46 PASS**
- LIVE: **BLOCKED_EXTERNAL** (sem token)

## Signing status

- **BLOCKED_EXTERNAL** (SignTool/certificado comercial ausente no host)

## Compatibility

- Satellite projects net9 permanecem fora do produto desktop ACTIVE
- ACTIVE produto: `net10.0-windows` · ACTIVE `net6.0-windows` produto = 0

## Known limitations

- AssemblyVersion ainda 1.0.0 (`VERSION_PREPARATION_REQUIRED`)
- Externos: Fiscal LIVE · Code Signing · NET6 SxS

## Validation evidence

`TestResults/Net10-Overnight/20260913/NET10-24-Promotion-Prep/`  
`Docs/qa/PRIMOX-NET10-24-PROMOTION-PREPARATION.md`

## Rollback strategy

- **Não mergear** até decisão humana.
- `main` permanece `29b19b1` (NET6).
- `v1.0.0` permanece `a4ad6fe`.
- `primox-net6-final` permanece `29b19b1`.
- Rollback = não promover / reverter PR sem tocar tags.
