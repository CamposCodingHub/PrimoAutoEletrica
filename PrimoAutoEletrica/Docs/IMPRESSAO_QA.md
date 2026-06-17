# QA de Impressao e Exportacao

Este roteiro valida os documentos gerados pelo Primo Auto Eletrica antes de uso real na oficina.

## Impressora A4

- Gerar PDF de orcamento, OS, checklist, recibo, termo de garantia, termo de autorizacao e relatorio financeiro.
- Abrir cada arquivo em um leitor PDF comum.
- Imprimir em papel A4 com escala `Ajustar a pagina`.
- Conferir se logo/nome da oficina, dados do cliente, veiculo, itens, valores, observacoes e assinatura aparecem quando aplicavel.
- Conferir se as margens nao cortam cabecalho, rodape ou assinatura.
- Conferir se documentos com muitos itens quebram pagina sem sobrepor rodape.
- Conferir se fonte fica legivel a distancia normal de atendimento.

## Impressora termica futura

- Usar apenas comprovante de venda/recibo quando uma impressora termica for homologada.
- Configurar a impressora padrao do PDV em Configuracoes.
- Conferir largura do papel, corte, acentos e totalizadores.
- Nao usar termo de garantia, termo de autorizacao, OS completa ou relatorio financeiro em bobina sem layout especifico.

## Checklist de aceite

- PDF abre sem erro.
- Arquivo possui assinatura `%PDF-`.
- Tamanho do arquivo e maior que zero.
- Cabecalho identifica a oficina.
- Dados do cliente e veiculo estao presentes quando aplicavel.
- Itens e valores conferem com a tela.
- Observacoes aparecem no documento.
- Campo de assinatura aparece em OS, checklist, recibo e termos.
- Impressao A4 nao corta texto.
- Exportacoes CSV continuam abrindo no Excel/LibreOffice.

## Evidencia manual

Ao testar em oficina real, registrar:

- Data do teste.
- Nome do computador.
- Impressora usada.
- Tipo de documento.
- Resultado: aprovado/reprovado.
- Observacao do problema, se houver.
