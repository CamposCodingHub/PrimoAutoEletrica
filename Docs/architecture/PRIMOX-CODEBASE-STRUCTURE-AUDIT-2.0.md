# PRIMOX — Codebase Structure Audit 2.0

> **RELATÓRIO DE AVANÇO / FASE — 2026-09-13**
>
> Este arquivo registra **melhorias e evidências da fase em que foi escrito**.
> **Não** é inventário operacional atual.
>
> Verdade atual: `Docs/CURRENT-TRUTH.md` · Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md` · Índice: `Docs/DOCUMENTATION-INDEX.md`
> HEAD pós-NET10-26: `1372e11` · TFM `net10.0-windows`

---

**Date:** 2026-09-08

## Layout

| Área | Classificação |
|------|---------------|
| `PrimoAutoEletrica/` app WPF | CORE / UI |
| `Services/Fiscal/` | SERVICES (fiscal foundation + ops 2.0) |
| `UserControls/` | UI modules |
| `Views/` | UI windows/dialogs |
| `Data/` + migrations | DATABASE |
| `Tests/` | TESTS |
| `Docs/` | DOCS |
| `Scripts/` | SCRIPTS / DEPLOY |
| WIP deploy bat/ps1 | DEPLOY (não commitados sem necessidade) |

## Mapa de navegação (principais)

Dashboard, Clientes, Veículos, AutoEletricaTecnica, Orçamentos, OrdensServico, OficinaKanban, PDV, Estoque, CatalogoPecas, ImportarNFe, **FiscalOperacoes**, Financeiro, Fornecedores, Funcionarios, Agendamentos, Relatorios, Help/Ajuda, ConfiguracoesSistema (janela).

## Código morto / órfãos

- Sem remoção nesta fase (regra de evidência completa).
- Candidatos: revisar `NFeEmissaoService` bridge KEEP-FUTURE; Fake only em testes.
- Não apagar WIP Scripts.

## Duplicidades

- Nenhuma `NFeServiceOld` / `FiscalServiceLegacy` encontrada como duplicata ativa.

## Decisão estrutural

Estrutura coerente para 1.0.0 comercial + fiscal homolog. Produção fiscal permanece bloqueada por camadas.
