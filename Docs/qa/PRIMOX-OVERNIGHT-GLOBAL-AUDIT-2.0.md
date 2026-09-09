# PRIMOX — Overnight Global Audit 2.0

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
