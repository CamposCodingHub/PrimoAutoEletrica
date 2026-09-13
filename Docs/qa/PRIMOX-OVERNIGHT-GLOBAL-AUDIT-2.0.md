# PRIMOX — Overnight Global Audit 2.0

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

**Execução:** 2026-09-08 (noite)  
**HEAD inicial:** `13af145`  
**TAG v1.0.0:** `a4ad6fe` INTACTA  
**WIP:** `Scripts/Atualizar-PrimoAuto.bat`, `Scripts/Deploy-ToInstalledApp.ps1` PRESERVADOS

## Inventário (aprox., excl. bin/obj)

| Tipo | Qtd |
|------|-----|
| *.cs | 434 |
| *.xaml | 105 |
| *.csproj | 13 |
| *.md | 101 |
| *.ps1 | 42 |
| *.bat | 1 |

## Correção crítica — Ajuda Light Mode

**Causa:** `HelpControl.xaml.cs` usava `FindResource` (snapshot). Após Dark→Light, cards/conteúdo dinâmico permaneciam com brushes escuros.  
**Correção:** `SetResourceReference` / `Bind(...)` + `RefreshThemeBoundContent()` no toggle de tema.  
**Resultado:** CORRIGIDO (causa arquitetural).

## Escopo da noite

- Help theme fix (P1)
- Auditoria + Fiscal Operations Center 2.0 (missão fiscal anexada na mesma janela)
- Sem NFC-e / produção / SaaS
- Sem remoção agressiva de código morto (candidatos documentados)

## Limitações

- Calendar Dark header nativo: LIMITATION (sem rewrite CalendarItem)
- Live Focus homolog: NOT EXECUTED
- Page-by-page visual humano 100%: parcial via Exhaustive/QaEngine (ver relatório UI)

## Segurança

- Segredos reais no Git: não encontrados nesta varredura
- Produção fiscal: BLOCKED

## Decisão

`READY WITH LIMITATIONS`
