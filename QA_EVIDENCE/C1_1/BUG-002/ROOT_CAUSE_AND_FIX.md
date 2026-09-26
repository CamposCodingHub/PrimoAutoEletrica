# BUG-002 Evidence — TemPermissao vs TemPermissaoCodigo

## Reproducao
Permissao negada Admin Smoke Test: Tipo=Modulo; Alvo=ESTOQUE_CRIAR
CatalogoPecasViewModel usava TemPermissao("ESTOQUE_CRIAR")

## Causa raiz
TemPermissao(string) = modulo (ModulePermissionCodes / lista de modulos).
TemPermissaoCodigo(string) = codigo granular de acao; Admin = Allowed.
ESTOQUE_CRIAR e codigo de acao, nao nome de modulo → TemPermissao retorna false (fail-closed correto, API errada na call site).

## Correcao
CatalogoPecasViewModel: TemPermissaoCodigo("ESTOQUE_CRIAR")

## Auditoria
Nao encontrado outro TemPermissao("CODIGO_COM_UNDERSCORE") residual.
Demais acoes ja usam TemPermissaoCodigo.

## Testes fail-closed
- Admin TemPermissaoCodigo ESTOQUE_CRIAR = true
- TemPermissao("ESTOQUE_CRIAR") = false (prova semantica)
- Operador/Visualizador TemPermissaoCodigo = false
- CatalogoPecasViewModel PodeCriarProduto admin true / visualizador false

## Status
FIXED
