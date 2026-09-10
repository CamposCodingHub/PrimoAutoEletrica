# PRIMOX-I18N-07 — RESIDUALS

## Antes → Depois

| Item | I18N-06 | I18N-07 |
|------|--------:|--------:|
| TRANSLATION_REQUIRED | 337 | **322** |
| UNKNOWN | 1348 | **1345** |
| EN strict PASS modules | 4/15 | **14/15** |
| ES strict PASS modules | 2/15 | **14/15** |

## P0 / P1

- **P0 user-visible operacional:** 0 nos critical flows auditados (EN/ES)
- **P1 critical flows:** 0 residual classificado como PT chrome nos módulos Clientes→Relatórios

## Remanescentes documentados (não-bloqueantes)

| Item | Motivo | Categoria | Impacto | Decisão |
|------|--------|-----------|---------|--------|
| Help Extended (guias longos PT) | Conteúdo extensivo secundário | P3_HELP | Usuário opera sem Help Extended | KEEP · exceção YELLOW |
| Audit filter tokens (`Sucesso`/`Falha`/`Funcionario`…) | Valores alinhados a códigos internos de filtro | TECHNICAL | Traduzir quebraria filtro | KEEP |
| UNKNOWN ~1345 attrs | Maioria não user-visible / sem classificador forte | UNKNOWN non-UV | Não mass-translate | KEEP |
| UNUSED catalog ~317 | Chaves órfãs históricas | INTERNAL | Não remover em massa | KEEP |
| DUPLICATE_CANDIDATE ~45 | Sem evidência de consolidação segura | INTERNAL | KEEP | KEEP |
| Native Calendar Dark Header | Limitação WPF | TECHNICAL | Sem impacto i18n | KEEP |
| Dados reais / placas / nomes | Domínio | DATA | Não traduzir | KEEP |
| Termos fiscais NCM/CFOP/… | Domínio BR | FISCAL | Não traduzir | KEEP |
| Brand PRIMOX | Marca | BRAND | Não traduzir | KEEP |

## Detector

- Cognatos ES (`Editar`/`Cancelar`/`Buscar`/`Sucesso`) não contam como residual PT em `es-ES`
- Exact TECHNICAL allowlist para filtros de auditoria
- Word-boundary evita falso positivo `Configura` ⊂ `Configuración`
