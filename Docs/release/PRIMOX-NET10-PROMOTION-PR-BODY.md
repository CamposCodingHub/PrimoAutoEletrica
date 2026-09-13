# PR body (preparação — PR **não** criado automaticamente)

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
