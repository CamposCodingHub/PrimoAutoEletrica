# Roteiro QA manual final - PrimoAutoEletrica

Atualizado em: 04/06/2026 10:32

Este roteiro concentra a homologacao manual que ainda depende de app aberto, dados reais, impressora fisica, arquivos de producao ou conferencia visual humana. Use uma linha por execucao, preencha resultado/evidencia e registre qualquer falha no relatorio geral antes da entrega final.

## Como registrar

| Campo | Uso |
| --- | --- |
| Resultado | `[ ] Pendente`, `[x] Aprovado`, `[!] Reprovado`, `[~] Aprovado com ressalva` |
| Evidencia | Caminho de arquivo, print, numero de OS/venda/NF-e, comprovante impresso ou observacao objetiva |
| Responsavel/Data | Quem executou e quando |

## Pre-check de ambiente

| Item | Resultado | Evidencia | Responsavel/Data |
| --- | --- | --- | --- |
| Abrir aplicativo pelo executavel de Debug/entrega sem erro inicial | [ ] Pendente |  |  |
| Validar login com usuario administrador real | [ ] Pendente |  |  |
| Alternar tema claro/escuro e conferir contraste em Dashboard, PDV, Estoque, Importar NF-e, Fornecedores e Relatorios | [ ] Pendente |  |  |
| Conferir se nao ha janelas modais presas ao navegar entre modulos | [x] Aprovado | Smoke filtrado `PreCheck:SemModaisPresasNavegacao` navegou modulos centrais, Importar NF-e e Configuracoes sem deixar janela transiente visivel; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-04-18-29-43.txt` | Codex / 04/06/2026 18:29 |
| Confirmar banco SQLite correto para homologacao e backup antes dos testes | [ ] Pendente |  |  |

## Produtos e estoque

| Item | Resultado | Evidencia | Responsavel/Data |
| --- | --- | --- | --- |
| Criar produto real com foto, codigo, SKU, unidade, NCM/CEST/CFOP, cor, material, peso e dimensoes | [ ] Pendente |  |  |
| Anexar ficha tecnica/manual/garantia e abrir os anexos pelo Windows | [ ] Pendente |  |  |
| Editar produto preservando foto/anexos existentes | [ ] Pendente |  |  |
| Executar entrada de estoque e conferir historico/movimentacao | [ ] Pendente |  |  |
| Executar saida de estoque e conferir quantidade disponivel/reservada | [ ] Pendente |  |  |
| Gerar etiqueta/PDF e conferir layout antes de imprimir | [ ] Pendente |  |  |
| Testar filtros: estoque baixo, estoque alto, produto parado, sem codigo/SKU, Curva ABC e ranking de mais vendidos | [ ] Pendente |  |  |

## Importar NF-e

| Item | Resultado | Evidencia | Responsavel/Data |
| --- | --- | --- | --- |
| Importar XML de producao com fornecedor real e produtos reais | [ ] Pendente |  |  |
| Conferir historico, total de importacoes, pendencias e ultima importacao | [ ] Pendente |  |  |
| Excluir apenas o XML selecionado e confirmar que ele desaparece do historico | [ ] Pendente |  |  |
| Relancar o mesmo XML apos exclusao sem duplicidade indevida | [ ] Pendente |  |  |
| Usar rollback/desfazer produtos criados e conferir auditoria | [ ] Pendente |  |  |
| Decidir se sera necessario snapshot futuro para desfazer produtos atualizados | [x] Aprovado | Snapshot anterior/posterior implementado para produtos atualizados; smoke filtrado `ImportarNFe:RollbackAtualizacaoComSnapshot` aprovado em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-04-18-18-54.txt` | Codex / 04/06/2026 18:18 |

## Fornecedores

| Item | Resultado | Evidencia | Responsavel/Data |
| --- | --- | --- | --- |
| Conferir que a coluna Acoes possui apenas Ver/Editar e que Excluir fica fora da coluna | [ ] Pendente |  |  |
| Selecionar um fornecedor e excluir somente o selecionado | [ ] Pendente |  |  |
| Abrir ficha de fornecedor e conferir ProdutoFornecedor, compras, prazo, ranking, ticket medio e ultima NF-e | [ ] Pendente |  |  |
| Importar NF-e real e confirmar que compras/prazos aparecem na ficha do fornecedor | [ ] Pendente |  |  |
| Editar prazo medio/categoria/contato e confirmar reflexo na ficha | [ ] Pendente |  |  |

## Clientes e veiculos

| Item | Resultado | Evidencia | Responsavel/Data |
| --- | --- | --- | --- |
| Criar cliente real com LGPD, WhatsApp autorizado, documento e assinatura digital | [ ] Pendente |  |  |
| Abrir documento/assinatura e conferir hash/rastreabilidade | [ ] Pendente |  |  |
| Usar atalhos WhatsApp, nova OS e novo orcamento a partir do cliente | [ ] Pendente |  |  |
| Criar veiculo com foto/documento real e campos tecnicos de auto eletrica | [ ] Pendente |  |  |
| Conferir alertas de retorno, garantia e proxima revisao | [ ] Pendente |  |  |
| Exportar veiculos e conferir CSV/arquivo gerado | [ ] Pendente |  |  |

## Ordens de servico, orcamentos e agendamentos

| Item | Resultado | Evidencia | Responsavel/Data |
| --- | --- | --- | --- |
| Criar OS real com checklist de entrada/saida, diagnostico inicial/final e fotos antes/depois | [ ] Pendente |  |  |
| Entregar OS e conferir integracao com conta a receber/financeiro | [ ] Pendente |  |  |
| Criar orcamento real, gerar PDF e enviar/abrir WhatsApp respeitando autorizacao LGPD | [ ] Pendente |  |  |
| Converter orcamento em OS sem duplicidade | [ ] Pendente |  |  |
| Converter orcamento em venda/PDV e conferir financeiro | [ ] Pendente |  |  |
| Criar agendamento, reagendar, check-in/check-out e converter em OS/orcamento | [ ] Pendente |  |  |
| Conferir visoes diaria, semanal e mensal da agenda | [ ] Pendente |  |  |

## PDV e caixa

| Item | Resultado | Evidencia | Responsavel/Data |
| --- | --- | --- | --- |
| Abrir caixa com operador real | [ ] Pendente |  |  |
| Selecionar cliente pela janela do PDV e tambem testar consumidor final | [ ] Pendente |  |  |
| Adicionar produto, aplicar desconto permitido e finalizar venda em dinheiro/PIX/cartao | [ ] Pendente |  |  |
| Finalizar pagamento misto com rateio correto | [ ] Pendente |  |  |
| Suspender e retomar venda | [ ] Pendente |  |  |
| Registrar suprimento e sangria | [ ] Pendente |  |  |
| Reimprimir comprovante em impressora fisica real | [ ] Pendente |  |  |
| Cancelar venda concluida com motivo e conferir estorno/auditoria | [ ] Pendente |  |  |
| Fechar caixa e conferir saldo/resumo | [ ] Pendente |  |  |

## Financeiro e relatorios

| Item | Resultado | Evidencia | Responsavel/Data |
| --- | --- | --- | --- |
| Conferir contas a pagar/receber, vencidos, hoje, semana e baixa de contas | [ ] Pendente |  |  |
| Conferir graficos de fluxo/formas de pagamento e alertas de divergencia | [ ] Pendente |  |  |
| Exportar relatorio financeiro e conferir arquivo | [ ] Pendente |  |  |
| Gerar relatorios: Curva ABC, produtos parados, margem por produto, vendas por hora/dia, DRE e conciliacao | [ ] Pendente |  |  |
| Exportar pacote de evidencias de relatorios e conferir manifesto | [ ] Pendente |  |  |

## Funcionarios, permissoes e login

| Item | Resultado | Evidencia | Responsavel/Data |
| --- | --- | --- | --- |
| Criar/editar funcionario real e validar CPF/email/perfil | [ ] Pendente |  |  |
| Bloquear/reativar funcionario e conferir login | [ ] Pendente |  |  |
| Conferir permissoes por perfil em modulos sensiveis | [ ] Pendente |  |  |
| Validar negacao de acesso com usuario sem permissao | [ ] Pendente |  |  |
| Testar lockout por senha errada, logout e expiracao por inatividade | [ ] Pendente |  |  |
| Conferir painel de produtividade/auditoria do colaborador | [ ] Pendente |  |  |

## Configuracoes e entrega

| Item | Resultado | Evidencia | Responsavel/Data |
| --- | --- | --- | --- |
| Configurar logo real e conferir no comprovante/relatorios | [ ] Pendente |  |  |
| Configurar impressora preferencial do PDV por estacao | [ ] Pendente |  |  |
| Gerar backup e restaurar em ambiente controlado, nao no banco de producao | [ ] Pendente |  |  |
| Conferir textos comerciais do comprovante | [x] Aprovado | Smoke filtrado `Configuracoes:ComercialBackupRestauracao` validou persistencia de cabecalho/rodape e previa/documento do comprovante; evidencia em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-04-18-34-59.txt` | Codex / 04/06/2026 18:34 |
| Rodar build final e smoke completo sem travar em modal | [x] Aprovado | `dotnet build .\PrimoAutoEletrica.csproj --no-restore` 0 erros/0 avisos; smoke `154/154` em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-04-10-19-37.txt` | Codex / 04/06/2026 10:19 |
| Regenerar pacote final e confirmar manifesto com 0 entradas proibidas | [x] Aprovado | `Artifacts/PrimoAutoEletrica_Source_20260604_QA.manifest.txt`: 319 arquivos e 0 entradas proibidas | Codex / 04/06/2026 |

## Observacoes da rodada

- Em 04/06/2026, a selecao de cliente do PDV foi identificada como janela que podia travar execucoes automatizadas se acionada pela varredura generica. A janela agora se fecha automaticamente como `Consumidor final` em `App.IsAutomatedTestMode`, mas continua precisando ser testada manualmente no bloco PDV acima.
- O bloco de Fornecedores ja possui cobertura automatizada dedicada em `Fornecedores:ProdutoFornecedorComprasPrazosRanking`; a homologacao manual deve usar NF-e real para confirmar dados comerciais reais.
- Em 04/06/2026 10:27, os itens automatizados finais de build/smoke e pacote/manifesto foram marcados como aprovados porque possuem evidencia objetiva ja gerada; os demais itens continuam dependendo de execucao humana com dados reais.
- Em 04/06/2026 16:00, a limpeza estrutural removeu controles/telas orfaos que nao eram usados pela navegacao ativa. Os testes manuais devem continuar usando os modulos reais `PDVControl`, `RelatoriosControl`, `AgendamentosControl` e controles operacionais da sidebar.
- Em 04/06/2026 18:18, a pendencia tecnica de snapshot para produtos atualizados por NF-e foi resolvida: o historico grava snapshot anterior/posterior, o rollback restaura apenas quando o estado atual ainda confere e o smoke filtrado permite validar checks especificos sem rodar toda a suite.
- Em 04/06/2026 18:29, o pre-check de modais presas foi automatizado e aprovado pelo smoke filtrado `PreCheck:SemModaisPresasNavegacao`; a homologacao visual de tema/login/app aberto continua pendente.
- Em 04/06/2026 18:34, os textos comerciais do comprovante foram validados por smoke filtrado; logo real, impressora fisica e restauracao em ambiente controlado continuam pendentes de campo.
