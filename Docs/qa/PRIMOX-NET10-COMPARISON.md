# PRIMOX — Comparação NET6 × NET10

**Timestamp:** 2026-09-13  
**NET6 baseline:** `main` / `v1.0.0` / tag `primox-net6-final`  
**NET10:** branch `migration/net10`  
**Fonte CURRENT NET10:** sessão NET10-22 (`TestResults/.../NET10-22-Hardening/`)  
**Fonte NET6:** refs protegidas + evidência histórica (não reexecutada nesta máquina sem SDK6)

| ITEM | NET6 | NET10 | RESULTADO |
|---|---|---|---|
| Branch / tag | `main`=`29b19b1` · `v1.0.0`=`a4ad6fe` · `primox-net6-final`=`29b19b1` | `migration/net10` | Protegidos **intactos** |
| TFM produto | `net6.0-windows` | `net10.0-windows` | **MIGRATED** |
| TFM testes unit | `net6.0-windows` (histórico) | `net10.0-windows` | **MIGRATED** |
| SDK host | SDK 6 **ausente** neste host | SDK **10.0.302** | Ambiente NET10 only |
| Build Debug/Release | PASS histórico | **PASS CURRENT** 0 erros | PARITY |
| Unit | 173 histórico | **173/173 CURRENT** | PARITY |
| QaEngine | 43/43 histórico | **43/43 CURRENT** | PARITY |
| DeepQa | 6/6 histórico | **6/6 CURRENT** | PARITY |
| DB integrity / FK | ok / 0 histórico | A13Database **APROVADO CURRENT** | PARITY |
| Security smoke | PASS histórico | A12Security **3/3 CURRENT** | PARITY |
| UI / Agenda / Tema | PASS histórico | Calendar/Tema/Agenda **APROVADO** + Dark header **CLOSED** | NET10 **melhor** (header Dark) |
| Gallery multi-res | parcial / NOT EXECUTED em fases antigas | DeepQa + PNGs **PASS** (NET10-21/22) | NET10 **melhor** |
| Performance startup×10 | **HISTORICAL / NOT EXECUTED** neste host (sem EXE net6) | CURRENT avg **1871.5** ms · min 1851 · max 1923 · fails=0 | NET10 medido; NET6 não comparável aqui |
| Publish win-x64 SC | comercial 1.0.0 | 472 files · ~177 MB · PDB=0 · LiveCharts/OpenTK **0** | NET10 leaner pós-remoção |
| Installer | `PRIMOX-Workshop-Setup-1.0.0.exe` SHA `9A08494D…A9C5` | preview `1.1.0-net10-preview` · comercial **INTACTO** | Preview experimental; comercial não tocado |
| Dependências charts | LiveCharts legado / reestruturado sem UI | **LiveCharts removido** (unused) · NU1701 **CLOSED** | NET10 **melhor** |
| NU1701 | N/A ou histórico | **0** após remoção | CLOSED |
| Fiscal LIVE | BLOCKED_EXTERNAL (token) | **BLOCKED_EXTERNAL** | EXTERNAL |
| Code Signing | BLOCKED_EXTERNAL | **BLOCKED_EXTERNAL** | EXTERNAL |
| .NET6 SxS | N/A | **BLOCKED_EXTERNAL** (SDK6/EXE ausentes) | EXTERNAL |
| Residual TFM net6 ativo | era ativo | ACTIVE csproj **0** | CLOSED |

## Notas

- Diferença de ambiente (SDK6 ausente) **não** é regressão de produto.
- Performance NET6 nesta máquina: **NOT EXECUTED** — não inventar baseline.
- Instalador comercial 1.0.0 permanece a linha estável NET6; NET10 usa preview experimental.
