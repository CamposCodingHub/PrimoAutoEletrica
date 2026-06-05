# Validação de Reimpressão PDV (Plano)

Objetivo: documentar passos e critérios para validar reimpressão de comprovantes em impressora física.

Passos:
- Preparar uma estação PDV com impressora térmica conectada (USB ou rede).
- Abrir o `PDV` e efetuar uma venda de teste com pelo menos um item e pagamento concluído.
- No histórico operacional do PDV, selecionar a venda e acionar `Reimprimir`.
- Validar que o dispositivo imprime o comprovante corretamente sem truncamento e com dados consistentes (itens, preços, NCF/CF-e quando aplicável).
- Testar cenários de falha: impressora desligada, papel insuficiente, conteúdo muito extenso. Verificar fallback escrito em XAML/PDF quando impressão falhar.
- Repetir testes para impressoras de diversos fornecedores/portas (USB, Serial, Rede) e para modelos configurados via Windows.

Critérios de aceitação:
- Reimpressão bem-sucedida em 5 modelos diferentes de impressora.
- Fallback (geração XAML/PDF) acionado quando impressão falhar.
- Registro de auditoria gerado para cada tentativa de reimpressão (sucesso/falha).

Observações:
- Automação total desta validação não é possível sem acesso físico a dispositivos; por isso o processo exige execução manual e registro dos resultados.
