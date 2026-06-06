# Validacao de reimpressao PDV

Objetivo: documentar passos e criterios para validar reimpressao de comprovantes em impressora fisica real.

## Preparacao

- Usar uma estacao PDV com impressora termica ou laser conectada por USB, rede ou fila Windows.
- Conferir em `Configuracoes > Multiusuario / Rede` se a impressora preferencial do PDV esta selecionada quando a oficina quiser impressao automatica.
- Abrir o sistema com operador real autorizado para PDV.
- Criar ou selecionar uma venda concluida de teste.

## Passos

1. Abrir o modulo `PDV`.
2. Confirmar que existe venda concluida recente para reimpressao.
3. Acionar `Reimprimir`.
4. Se a impressora preferencial estiver habilitada, confirmar que o sistema usa essa fila sem abrir seletor manual.
5. Se a impressora preferencial nao estiver habilitada, selecionar a impressora manualmente no dialogo do Windows.
6. Conferir o comprovante fisico impresso.
7. Registrar ID da venda, modelo/fila da impressora, horario e evidencia fotografica/digitalizada.

## Criterios de aceite

- O comprovante fisico deve conter itens, quantidades, valores, descontos, forma de pagamento, total, operador e cliente sem truncamento critico.
- A reimpressao deve registrar auditoria `VendaReimpressa`.
- Quando impressora preferencial estiver configurada e disponivel, a fila usada deve corresponder a configuracao da estacao.
- Quando nao houver impressora instalada, o fallback deve salvar o comprovante em arquivo e registrar auditoria de fallback.

## Cenarios de falha recomendados

- Impressora desligada.
- Papel insuficiente.
- Fila Windows indisponivel.
- Impressora preferencial configurada, mas removida da estacao.
- Comprovante com muitos itens.

## Observacao

Automacao total desta validacao nao e possivel sem acesso fisico ao dispositivo. O smoke do PDV ja valida a criacao do documento e auditoria em modo automatizado; este roteiro cobre a etapa fisica de campo.
