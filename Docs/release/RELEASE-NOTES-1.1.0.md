# Release Notes — PRIMOX Workshop 1.1.0 (candidato NET10)

> Documento de preparação. **Não** declara lançamento. Tag `v1.1.0` não criada.

## NEW

- Runtime alvo **.NET 10** (`net10.0-windows`) para o produto desktop
- Probe de compatibilidade `Tools/Net10CompatProbe`
- Documentação completa da trilha NET10-00…NET10-24

## IMPROVED

- Contraste do Calendar em tema Dark (`CalendarContrastHealer`)
- Scripts de build/smoke/installer alinhados ao TFM do csproj
- Clipboard sob carga (`ClipboardHelper.SetTextWithRetry`)
- Pacotes Sqlite / SQLitePCLRaw / Extensions atualizados para linha 9.x / 3.x

## FIXED

- NU1903 (SQLitePCLRaw) e caminhos de script apontando TFM stale
- Tabs de smoke alinhados a headers localizados
- Paths do SecurityRedTeam para net10

## SECURITY

- Gates A12 (authz backup, path traversal, process URI) e A13 (process residual) revalidados nesta sessão
- RedTeam: Critical=0 · High=0
- Sem novos Critical/High nesta preparação

## COMPATIBILITY

- Mapa de navegação de módulos **idêntico** a `main` (NET6)
- LiveCharts removido sem regressão funcional (não havia uso em código/XAML)
- Installer comercial `PRIMOX-Workshop-Setup-1.0.0.exe` **intacto** (SHA `9A08494D…A9C5`)

## KNOWN LIMITATIONS

- `AssemblyInfo` / FileVersion ainda reportam **1.0.0** até decisão humana (`VERSION_PREPARATION_REQUIRED`)
- Publish raw `dotnet publish` pode incluir 1× `.pdb`; pacote comercial PackagingE2E exclui PDBs
- Smoke Calendar `QaVisualDarkLight` no EXE em Program Files falha ao gravar captura em `Program Files\...\Logs\qa-visual` (ACL) — checks funcionais de Calendar Dark passam; captura visual validada no smoke de build com app-data gravável

## EXTERNAL REQUIREMENTS

- Token Focus homologação para Fiscal LIVE
- Certificado de code signing comercial + SignTool no PATH de release
- Host com SDK/runtime net6 apenas se for necessária comparação SxS
