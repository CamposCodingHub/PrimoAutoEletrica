# Checklist continuo - Auditoria PrimoAutoEletrica

Atualizado em: 04/06/2026 18:34

Este arquivo e a lista viva para acompanhar a auditoria/melhoria por fases. A ideia e manter o trabalho seguro: fases pequenas, build apos cada bloco e nada de duplicar XAML, x:Class ou arquivos de backup dentro da compilacao.

## Legenda

- `[x] Concluido`: entregue e validado no estado atual.
- `[~] Parcial`: existe implementacao operacional, mas ainda ha melhorias da lista ampla.
- `[ ] Pendente`: ainda nao iniciado ou depende de validacao/manual.
- `[!] Atencao`: item sensivel ou com risco operacional.

## Validacao atual

- `[x] dotnet clean`: executado em 31/05/2026 21:02, 0 avisos, 0 erros.
- `[x] dotnet build`: executado em 04/06/2026 10:11, 0 avisos, 0 erros.
- `[x] x:Class duplicado`: auditoria inicial nao encontrou duplicidade nas telas criticas fora de bin/obj.
- `[x] Arquivos proibidos`: varredura em 01/06/2026 23:43 nao encontrou `.backup`, `.old`, `.copy`, `.teste`, `.tmp`, `.bak`, `.orig`, `.rej`, `.lscache`, `.log`, `.binlog`, bancos locais, ZIPs ou pacotes fora de `.vs/bin/obj/Artifacts`.
- `[x] Smoke test normal`: 154/154 em 04/06/2026 10:19, incluindo `Produtos:CamposAnexosOperacionais`, `Fornecedores:ProdutoFornecedorComprasPrazosRanking`, `Clientes:LGPDAtalhosOperacionais`, `Clientes:AnexosAssinatura`, `Veiculos:AlertasMidiaDocumentos`, `OrdensServico:MidiasChecklistFinanceiro`, `Orcamentos:ConversoesPdfWhatsAppAlertas`, `Agendamentos:VisualizacoesFiltrosConversoes`, `Financeiro:GraficosAlertasDivergencia`, `Relatorios:ExportacoesEvidencias`, `ImportarNFe:RollbackProdutosAuditavel`, `ImportarNFe:XmlRealExcluirRelancar`, `Funcionarios:AuditoriaProdutividadePermissoes`, `Configuracoes:ComercialBackupRestauracao`, `LoginSessao:MensagensLockoutLogoutPermissoes` e `PDV:InteracaoCompletaTela`.
- `[x] Evidencia smoke`: `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-04-10-19-37.txt`.
- `[x] Evidencia pacote limpo`: `../Artifacts/PrimoAutoEletrica_Source_20260604_QA.zip`, manifesto `../Artifacts/PrimoAutoEletrica_Source_20260604_QA.manifest.txt`, 319 arquivos e 0 entradas proibidas.
- `[x] Roteiro QA manual final`: `Docs/ROTEIRO_QA_MANUAL_FINAL_PRIMOAUTOELETRICA.md` criado em 04/06/2026 para registrar evidencias reais por modulo.
- `[~] Status QA manual`: 62 itens rastreados; 5 aprovados por evidencia automatizada/decisao tecnica implementada, 0 reprovados, 0 ressalvas e 57 pendentes de execucao manual. Validador: `../Scripts/Get-QaManualStatus.ps1`; evidencia gerada: `../Artifacts/QA_MANUAL_STATUS_20260604.md`.
- `[x] Codigo legado de agendamento`: removidos os services antigos excluidos da compilacao (`AgendamentoIntegrationService`, `AgendamentoPerformanceService`, `AgendamentoTestService`) e retirada a excecao `Compile Remove` do `.csproj`; build OK em 04/06/2026 10:39.
- `[x] Duplicado fora do projeto`: removido `../Services/DatabaseService.cs`, arquivo legado fora do `.sln/.csproj` que ainda entrava no pacote-fonte.
- `[x] Seed inicial seguro`: senhas padrao fixas foram removidas do fonte; banco novo gera senha temporaria aleatoria para `admin@primoauto.com` e grava orientacao local em `credenciais-iniciais-admin.txt`.
- `[x] Controles/telas orfaos`: removidos `AgendamentoControl`, `AlertasInteligentesControl`, `ControleTecnicosControl`, `ControleVeiculosControl`, `PainelClientesControl`, `PainelServicosControl`, `StatusServicosControl`, `PDVView` e `RelatoriosView`; os modulos reais continuam em `AgendamentosControl`, `PDVControl`, `RelatoriosControl` e demais controles operacionais usados pela navegacao.
- `[x] Evidencia Configuracoes`: `bin/Debug/net9.0-windows/Logs/configuracoes-smoke/PrimoAutoEletrica_Backup_ConfigSmoke_20260601192407455.db`.
- `[x] Evidencia Relatorios`: `bin/Debug/net9.0-windows/Logs/relatorios-exportacoes/Evidencias_Relatorios_20260601_135656269/ManifestoEvidencias_20260601_135656269.txt`.
- `[x] Evidencia NF-e XML real`: `bin/Debug/net9.0-windows/Logs/nfe-smoke/NFeTeste-smoke-20260601181331607.xml`.
- `[x] Banco NF-e`: historico de importacoes zerado no banco local, com backup antes da limpeza.
- `[x] Snapshot rollback NF-e`: produtos existentes atualizados por NF-e agora gravam snapshot anterior/posterior e podem ser restaurados com trava de estado atual; smoke filtrado `ImportarNFe:RollbackAtualizacaoComSnapshot` aprovado em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-04-18-18-54.txt`.
- `[x] Smoke filtrado`: `--smoke-filter=NomeDoCheck` permite validar checks especificos sem acionar a suite completa.
- `[x] Pre-check sem modais presas`: smoke filtrado `PreCheck:SemModaisPresasNavegacao` aprovado em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-04-18-29-43.txt`.
- `[x] Textos do comprovante`: smoke filtrado `Configuracoes:ComercialBackupRestauracao` aprovado em `bin/Debug/net9.0-windows/Logs/smoke-tests/ui-smoke-2026-06-04-18-34-59.txt`, cobrindo persistencia de cabecalho/rodape e reflexo no comprovante.

## Checklist por fase

| Fase | Status | Situacao atual | Proximo passo |
| --- | --- | --- | --- |
| 0 - Auditoria inicial de seguranca | [x] Concluido | Clean/build passaram; x:Class e arquivos proibidos conferidos. | Repetir antes de entrega final. |
| 1 - Correcao visual global | [x] Concluido operacional | Tema claro/escuro, modal e recursos globais ja possuem auditoria/documentacao previa. | Revisao visual manual final. |
| 2 - Padronizacao de botoes | [x] Concluido operacional | Estilos globais de botoes existem e telas principais usam padrao. | Conferir telas secundarias no QA final. |
| 3 - Cards e tabelas | [x] Concluido operacional | Cards, DataGrid e contraste foram padronizados nas telas principais. | Varredura visual manual em tema escuro. |
| 4 - Sidebar e header | [x] Concluido operacional | Menu agrupado, header e navegacao incluindo Importar NF-e estao implementados. | Smoke navegando por todas as telas. |
| 5 - Produto com foto e campos completos | [x] Concluido operacional | Produto tem foto, campos fiscais, margem, validade/lote, unidade, codigo/SKU, cor, material, peso/dimensoes, anexos operacionais persistidos e exibicao em Estoque/PDV; cobertura no smoke `Produtos:CamposAnexosOperacionais`. | QA manual com fotos/anexos reais e conferencia de abertura dos arquivos no Windows. |
| 6 - Estoque profissional | [x] Concluido operacional | Estoque exibe foto/codigo/SKU/unidade/margem, ajuste/historico, filtros de estoque baixo/alto, produto parado, sem codigo/SKU, Curva ABC, ranking de mais vendidos, PDF de etiquetas e entradas/saidas dedicadas auditadas. | QA manual com impressora/etiqueta fisica e fluxo real de estoque. |
| 7 - PDV profissional | [x] Concluido operacional | PDV esta estabilizado; pagamento misto valida rateio auditavel; venda suspensa/retomada funciona na sessao; reimpressao usa impressora preferencial da estacao; permissoes finas foram criadas para recursos sensiveis. | QA manual com impressora fisica real, sangria/suprimento/fechamento e fluxo completo de caixa. |
| 8 - Importacao de NF-e | [x] Concluido operacional | Pagina Importar NF-e existe, historico aparece, exclusao de XML selecionado grava auditoria, produtos novos importados guardam o ID criado, o botao Desfazer produtos executa rollback auditavel com travas, produtos atualizados guardam snapshot anterior/posterior e podem ser restaurados quando o estado atual ainda confere; os smokes `ImportarNFe:XmlRealExcluirRelancar` e `ImportarNFe:RollbackAtualizacaoComSnapshot` validam os fluxos criticos. | QA manual com XML de producao e conferencia visual do rollback em dados reais. |
| 9 - Clientes | [x] Concluido operacional | Foto persistente, ficha/perfil, historicos principais, anexos, assinatura digital, consentimento LGPD persistente, autorizacao de WhatsApp e atalhos WhatsApp/OS/orcamento foram trabalhados e cobertos no smoke. | QA manual com dados reais: anexar documento, registrar assinatura, abrir WhatsApp, nova OS e novo orcamento. |
| 10 - Veiculos | [x] Concluido operacional | Campos tecnicos de auto eletrica, visualizacao/historico, foto, documento, alertas de garantia/revisao/retorno e exportacao operacional foram trabalhados e cobertos no smoke. | QA manual com foto/documento reais e fluxo de oficina. |
| 11 - Ordens de Servico | [x] Concluido operacional | OS separa checklist de entrega e saida, persiste fotos antes/depois, assinatura e garantia, e integra conta a receber ao financeiro quando entregue; cobertura no smoke `OrdensServico:MidiasChecklistFinanceiro`. | QA manual com fluxo real de oficina, entrega ao cliente e eventual cobranca no PDV/balcao. |
| 12 - Orcamentos | [x] Concluido operacional | Conversao para OS reaproveita vinculo sem duplicar, conversao para venda/PDV integra financeiro, PDF e WhatsApp respeitando LGPD/autorizacao foram cobertos no smoke `Orcamentos:ConversoesPdfWhatsAppAlertas`. | QA manual com cliente real: PDF, WhatsApp, aprovacao, OS e venda no balcao. |
| 13 - Agendamentos | [x] Concluido operacional | Visao diaria/semanal/mensal alimenta a lista principal, filtros combinados foram validados, conversao em OS evita duplicidade e agendamento finalizado gera orcamento operacional idempotente; cobertura no smoke `Agendamentos:VisualizacoesFiltrosConversoes`. | QA manual de agenda com rotina real: reagendar, check-in/check-out, lembretes e impressao/exportacao. |
| 14 - Financeiro | [x] Concluido operacional | Graficos dedicados de fluxo/formas de pagamento exibem resumos calculados; modulo ganhou alertas de divergencia para vencidos, inadimplencia, status sem data, saldo projetado negativo e receita sem forma de pagamento; cobertura no smoke `Financeiro:GraficosAlertasDivergencia`. | QA manual de exportacao/impressao e conferencia financeira com dados reais. |
| 15 - Relatorios | [x] Concluido operacional | Botoes/visual foram padronizados; relatorios principais existem; Curva ABC, ranking de produtos parados, margem por produto, vendas por hora/dia, inadimplencia detalhada, DRE operacional e conciliacao financeira foram adicionados; exportacoes individuais e pacote de evidencias com PDF, CSV e manifesto foram validados no smoke `Relatorios:ExportacoesEvidencias`. | QA manual de Relatorios/exportacoes com dados reais e conferencia do arquivo gerado. |
| 16 - Fornecedores | [x] Concluido operacional | Tela corrigida: excluir saiu da coluna Acoes, botao superior exclui apenas selecionado, relacoes com produtos sao limpas; ProdutoFornecedor operacional foi criado com tabela persistente, sincronizacao por produto/fornecedor, compras por NF-e, preco/quantidade da ultima compra, prazo medio, ranking e ficha enriquecida do fornecedor. Smoke `Fornecedores:ProdutoFornecedorComprasPrazosRanking` aprovado. | QA manual com fornecedores reais, NF-e real e conferencia de ranking/compras/prazos na ficha. |
| 17 - Funcionarios e permissoes | [x] Concluido operacional | Perfis/permissoes e controle principal existem; painel do colaborador agora consolida produtividade/auditoria dos ultimos 30 dias, eventos recentes, permissoes sensiveis e permissoes por acao do perfil; cobertura no smoke `Funcionarios:AuditoriaProdutividadePermissoes`. | QA manual com perfis reais, bloqueio/reativacao e conferencia de permissoes por usuario. |
| 18 - Configuracoes | [x] Concluido operacional | Configuracoes existem para tema/sistema, multiusuario, backup, impressora preferencial do PDV por estacao, marca/logo, textos do comprovante e restauracao segura com validacao previa; textos comerciais do comprovante validados no smoke filtrado `Configuracoes:ComercialBackupRestauracao`. | QA manual com logo real, comprovante impresso, impressora fisica e restauracao em ambiente controlado. |
| 19 - Login e sessao | [x] Concluido operacional | Login, usuario atual, header, permissoes, mensagens de validacao, alerta visual, lockout temporario, logout/inatividade encerrando sessao persistida, auditoria de permissao negada por perfil e seed inicial sem senha fixa no fonte estao cobertos/validados. | QA manual com usuarios reais, troca de senha/redefinicao e sessao expirada em tempo real. |
| 20 - Limpeza do projeto | [x] Concluido operacional | `.gitignore` raiz e do projeto foram reforcados; services legados de agendamento, o `Services/DatabaseService.cs` duplicado fora do projeto, controles antigos sem navegacao ativa e wrappers antigos de PDV/Relatorios foram removidos; pacote final e gerado por script com manifesto e 0 entradas proibidas, sem `bin/obj/.vs` na entrega. | Regerar pacote pelo script antes de cada entrega oficial. |
| 21 - Testes obrigatorios | [~] Automatizado completo / manual pendente | Build OK e smoke UI 154/154 no build normal, evidencia `ui-smoke-2026-06-04-10-19-37.txt`; smokes filtrados de snapshot NF-e, pre-check sem modais e textos do comprovante aprovados; roteiro QA possui 62 itens, sendo 5 aprovados por evidencia automatizada/decisao tecnica e 57 pendentes de campo. | Executar os 57 itens manuais pendentes com app aberto, dados reais e impressora fisica. |
| 22 - Relatorio final | [~] Em andamento | Relatorios anteriores existem; este checklist centraliza o status, o roteiro QA final foi criado e o script `Scripts/Get-QaManualStatus.ps1` gera o placar do QA. | Atualizar relatorio final apos executar o roteiro QA manual e anexar evidencias. |

## Proximo bloco recomendado

1. Executar os 57 itens pendentes de `Docs/ROTEIRO_QA_MANUAL_FINAL_PRIMOAUTOELETRICA.md` com o app aberto: Clientes, Veiculos, OS, tema claro/escuro, Estoque, Relatorios, PDV, Importar NF-e com XML de producao, Fornecedores com NF-e real, Funcionarios/perfis e impressora fisica.
2. Corrigir eventuais reprovacoes do QA manual real.
3. Rodar `Scripts/Get-QaManualStatus.ps1`, atualizar o relatorio final apos o QA manual, anexando pacote limpo e evidencias finais.

## Pendencias principais restantes

- QA manual de Importar NF-e com XML de producao, incluindo conferencia visual de rollback de produtos criados e restauracao segura de produtos atualizados.
- QA manual de Produtos com fotos/anexos reais, abrindo ficha tecnica/manual/garantia pelo Windows.
- QA manual de Clientes para anexos, assinatura e atalhos operacionais com telefone/cliente reais.
- QA manual de Veiculos com foto/documento reais, alertas de retorno/garantia/revisao e exportacao CSV.
- QA manual de Configuracoes com logo real, comprovante impresso, impressora fisica e restauracao em ambiente controlado.
- QA manual de Login/sessao com usuarios reais, recuperacao/troca de senha e expiracao real por inatividade.
- QA manual completo em todas as telas, tema claro/escuro, estoque fisico/impressao de etiquetas e fluxo real de oficina.
