# QA de campo - pendencias finais PrimoAutoEletrica

Atualizado em: 06/06/2026 08:16

Este documento separa o que ja foi validado por automacao do que ainda exige ambiente real. O roteiro principal continua sendo `Docs/ROTEIRO_QA_MANUAL_FINAL_PRIMOAUTOELETRICA.md`; este arquivo apenas organiza a execucao dos 5 itens restantes.

## Status atual

- Total rastreado: 62 itens.
- Aprovados: 57 itens.
- Pendentes de campo: 5 itens.
- Reprovados: 0.
- Status calculado por: `Scripts/Get-QaManualStatus.ps1`.
- Pacote de campo geravel por: `Scripts/New-QaFieldEvidencePackage.ps1`.
- Ultimo pacote validado: `Artifacts/QA_FIELD_VALIDATION_20260606_081545`.

## Como gerar o pacote de evidencias

Execute na raiz do projeto:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Scripts\New-QaFieldEvidencePackage.ps1
```

O script cria uma pasta em `Artifacts/QA_FIELD_VALIDATION_*` com:

- `CHECKLIST_CAMPO.md`: checklist preenchivel para executar em campo.
- `CHECKLIST_CAMPO.csv`: planilha simples para controle.
- `QA_MANUAL_STATUS.md`: snapshot do placar no momento da geracao.
- `manifest.txt`: resumo do pacote.
- `PDV_REIMPRESSION_VALIDATION.md`: guia de reimpressao, quando disponivel.

## Itens restantes

| Secao | Item | Evidencia minima | Criterio de aceite |
| --- | --- | --- | --- |
| Pre-check de ambiente | Validar login com usuario administrador real | Print da tela principal com usuario/perfil real e horario da entrada | Login abre o sistema sem erro e com acesso aos modulos administrativos |
| Importar NF-e | Importar XML de producao com fornecedor real e produtos reais | Chave NF-e, fornecedor, produtos, totais e print do historico | Dados importados batem com o XML real e nao geram duplicidade indevida |
| Fornecedores | Importar NF-e real e confirmar compras/prazos na ficha | Ficha do fornecedor apos importacao, ultima NF-e, produtos vinculados e prazo medio | Compra real aparece no fornecedor correto com indicadores coerentes |
| PDV e caixa | Abrir caixa com operador real | Operador, numero do caixa, valor de abertura e print do PDV aberto | Operador real consegue abrir caixa conforme permissao e saldo inicial correto |
| PDV e caixa | Reimprimir comprovante em impressora fisica real | Modelo/fila da impressora, ID da venda, foto do comprovante e auditoria | Comprovante fisico sai sem truncamento e registra auditoria de reimpressao |

## Observacoes

- A configuracao de impressora preferencial por estacao ja foi aprovada por smoke de Configuracoes.
- A reimpressao automatizada do PDV ja monta o comprovante e registra auditoria, mas a impressao fisica ainda precisa de dispositivo real.
- A importacao NF-e sintetica e o relancamento/exclusao pela tela ja foram aprovados; a pendencia restante e somente conferir dados fiscais/comerciais de producao.
- A ficha de fornecedores ja possui ProdutoFornecedor, compras, prazos e ranking validados com dados controlados; a pendencia restante e confirmar a mesma cadeia com NF-e real.
