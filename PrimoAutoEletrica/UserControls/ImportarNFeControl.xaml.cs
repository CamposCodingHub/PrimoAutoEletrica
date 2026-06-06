using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using PrimoAutoEletrica.Data.Repositories;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Views;

namespace PrimoAutoEletrica.UserControls
{
    public partial class ImportarNFeControl : UserControl
    {
        private readonly ImportacaoRepository? _importacaoRepository;
        private readonly List<ImportacaoNFeGridItem> _historicoCompleto = new();
        private readonly List<ImportacaoNFeGridItem> _historicoFiltrado = new();
        private int _totalExclusoesAuditadas;
        private int _totalRollbacksAuditados;
        private bool _loadedOnce;
        private bool _suspendFilters;
        private bool _permitirAlteracoesDestrutivasEmSmoke;

        public ImportarNFeControl()
        {
            InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            _importacaoRepository = new ImportacaoRepository(App.Database);
            InicializarFiltros();
            Loaded += ImportarNFeControl_Loaded;
        }

        internal void HabilitarAlteracoesDestrutivasParaSmoke()
        {
            if (!App.IsSmokeTestMode ||
                !App.Database.DatabasePath.Contains("AutomatedTests", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Alteracoes destrutivas automatizadas so podem ser habilitadas no banco isolado do smoke test.");
            }

            _permitirAlteracoesDestrutivasEmSmoke = true;
        }

        private void ImportarNFeControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_loadedOnce)
            {
                return;
            }

            _loadedOnce = true;
            CarregarHistoricoImportacoes();
        }

        private void InicializarFiltros()
        {
            _suspendFilters = true;
            StatusComboBox.ItemsSource = new[]
            {
                "Todos os status",
                "Pendente",
                "Em processamento",
                "Concluida",
                "Parcial",
                "Erro",
                "Duplicada"
            };
            StatusComboBox.SelectedIndex = 0;

            FornecedorComboBox.ItemsSource = new[] { "Todos os fornecedores" };
            FornecedorComboBox.SelectedIndex = 0;
            _suspendFilters = false;
        }

        private void CarregarHistoricoImportacoes()
        {
            if (_importacaoRepository == null)
            {
                return;
            }

            try
            {
                _historicoCompleto.Clear();
                _totalExclusoesAuditadas = _importacaoRepository.ObterTotalExclusoesAuditadas();
                _totalRollbacksAuditados = _importacaoRepository.ObterTotalRollbacksAuditados();

                foreach (var notaResumo in _importacaoRepository.ObterHistoricoImportacoes(120))
                {
                    var notaCompleta = _importacaoRepository.ObterImportacaoPorId(notaResumo.Id) ?? notaResumo;
                    var produtos = notaCompleta.Produtos ?? new List<ProdutoImportado>();

                    _historicoCompleto.Add(new ImportacaoNFeGridItem
                    {
                        Id = notaCompleta.Id,
                        DataImportacao = notaCompleta.DataImportacao,
                        Fornecedor = TextoOuPadrao(notaCompleta.Fornecedor.Nome),
                        CnpjFornecedor = TextoOuPadrao(notaCompleta.Fornecedor.CNPJ),
                        NumeroNota = TextoOuPadrao(notaCompleta.Numero),
                        Serie = TextoOuPadrao(notaCompleta.Serie),
                        ChaveAcesso = TextoOuPadrao(notaCompleta.ChaveAcesso),
                        QuantidadeProdutos = produtos.Count,
                        ProdutosNovos = produtos.Count(produto => produto.Status == StatusImportacao.Novo),
                        ProdutosAtualizados = produtos.Count(produto => produto.Status == StatusImportacao.Atualizado),
                        ValorTotal = notaCompleta.ValorTotal,
                        Status = FormatarStatus(notaCompleta.Status),
                        Usuario = string.IsNullOrWhiteSpace(notaCompleta.UsuarioNome) ? "Sistema" : notaCompleta.UsuarioNome.Trim(),
                        TemPendencia = notaCompleta.Status is StatusImportacaoNota.Pendente or StatusImportacaoNota.Parcial or StatusImportacaoNota.Erro
                            || produtos.Any(produto => produto.Status == StatusImportacao.Erro),
                        Nota = notaCompleta
                    });
                }

                AtualizarFornecedoresFiltro();
                AplicarFiltros();
            }
            catch (Exception ex)
            {
                HistoricoDataGrid.ItemsSource = null;
                _historicoFiltrado.Clear();
                AtualizarCards(_historicoFiltrado);
                AtualizarEstadoExcluirImportacao();

                EmptyStateTitleText.Text = "Falha ao carregar historico";
                EmptyStateDescriptionText.Text = $"Nao foi possivel consultar as importacoes registradas: {ex.Message}";
                EmptyStateBorder.Visibility = Visibility.Visible;
                HistoricoDataGrid.Visibility = Visibility.Collapsed;

                AlertasText.Text = "O carregamento do historico falhou. Revise a conexao do banco e tente atualizar novamente.";
                PendenciasText.Text = "Sem dados de pendencias enquanto a consulta do historico nao for concluida.";
                UltimaImportacaoText.Text = "Nenhum registro disponivel.";
                DicasUsoText.Text = "Use o botao Atualizar historico depois de validar o banco de dados e a estrutura de importacoes.";
            }
        }

        private void AtualizarFornecedoresFiltro()
        {
            var fornecedorAtual = FornecedorComboBox.SelectedItem as string;
            var fornecedores = _historicoCompleto
                .Select(item => item.Fornecedor)
                .Where(item => !string.IsNullOrWhiteSpace(item) && item != "-")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .ToList();

            fornecedores.Insert(0, "Todos os fornecedores");

            _suspendFilters = true;
            FornecedorComboBox.ItemsSource = fornecedores;
            FornecedorComboBox.SelectedItem = fornecedores.Contains(fornecedorAtual ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                ? fornecedorAtual
                : fornecedores[0];
            _suspendFilters = false;
        }

        private void AplicarFiltros()
        {
            if (_suspendFilters)
            {
                return;
            }

            var busca = (BuscaTextBox.Text ?? string.Empty).Trim();
            var statusSelecionado = StatusComboBox.SelectedItem as string;
            var fornecedorSelecionado = FornecedorComboBox.SelectedItem as string;
            var dataInicial = PeriodoInicialDatePicker.SelectedDate?.Date;
            var dataFinal = PeriodoFinalDatePicker.SelectedDate?.Date.AddDays(1).AddTicks(-1);
            var selecionadoAtual = HistoricoDataGrid.SelectedItem as ImportacaoNFeGridItem;

            var consulta = _historicoCompleto.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                consulta = consulta.Where(item =>
                    Contem(item.Fornecedor, busca) ||
                    Contem(item.CnpjFornecedor, busca) ||
                    Contem(item.NumeroNota, busca) ||
                    Contem(item.ChaveAcesso, busca) ||
                    Contem(item.Usuario, busca));
            }

            if (!string.IsNullOrWhiteSpace(statusSelecionado) &&
                !string.Equals(statusSelecionado, "Todos os status", StringComparison.OrdinalIgnoreCase))
            {
                consulta = consulta.Where(item => string.Equals(item.Status, statusSelecionado, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(fornecedorSelecionado) &&
                !string.Equals(fornecedorSelecionado, "Todos os fornecedores", StringComparison.OrdinalIgnoreCase))
            {
                consulta = consulta.Where(item => string.Equals(item.Fornecedor, fornecedorSelecionado, StringComparison.OrdinalIgnoreCase));
            }

            if (dataInicial.HasValue)
            {
                consulta = consulta.Where(item => item.DataImportacao >= dataInicial.Value);
            }

            if (dataFinal.HasValue)
            {
                consulta = consulta.Where(item => item.DataImportacao <= dataFinal.Value);
            }

            _historicoFiltrado.Clear();
            _historicoFiltrado.AddRange(consulta.OrderByDescending(item => item.DataImportacao));

            HistoricoDataGrid.ItemsSource = null;
            HistoricoDataGrid.ItemsSource = _historicoFiltrado;
            AtualizarCards(_historicoFiltrado);
            AtualizarEstadoVazio();

            if (_historicoFiltrado.Count > 0)
            {
                var itemSelecionado = selecionadoAtual != null
                    ? _historicoFiltrado.FirstOrDefault(item => item.Id == selecionadoAtual.Id)
                    : null;

                HistoricoDataGrid.SelectedItem = itemSelecionado ?? _historicoFiltrado[0];
                AtualizarPainelLateral(HistoricoDataGrid.SelectedItem as ImportacaoNFeGridItem);
            }
            else
            {
                HistoricoDataGrid.SelectedItem = null;
                AtualizarPainelLateral(null);
            }

            AtualizarEstadoExcluirImportacao();
        }

        private void AtualizarCards(IReadOnlyCollection<ImportacaoNFeGridItem> itens)
        {
            TotalImportacoesText.Text = itens.Count.ToString(CultureInfo.InvariantCulture);
            ProdutosCriadosText.Text = itens.Sum(item => item.ProdutosNovos).ToString(CultureInfo.InvariantCulture);
            ProdutosAtualizadosText.Text = itens.Sum(item => item.ProdutosAtualizados).ToString(CultureInfo.InvariantCulture);
            ImportacoesPendenciaText.Text = itens.Count(item => item.TemPendencia).ToString(CultureInfo.InvariantCulture);
        }

        private void AtualizarEstadoVazio()
        {
            var existeHistorico = _historicoCompleto.Count > 0;
            var existeResultado = _historicoFiltrado.Count > 0;

            HistoricoDataGrid.Visibility = existeResultado ? Visibility.Visible : Visibility.Collapsed;
            EmptyStateBorder.Visibility = existeResultado ? Visibility.Collapsed : Visibility.Visible;

            if (existeResultado)
            {
                return;
            }

            if (!existeHistorico)
            {
                EmptyStateTitleText.Text = "Nenhuma importacao de NF-e registrada";
                EmptyStateDescriptionText.Text = "Importe um XML para criar ou atualizar produtos do estoque com conferencia de fornecedor, valores e dados fiscais.";
                return;
            }

            EmptyStateTitleText.Text = "Nenhuma importacao encontrada";
            EmptyStateDescriptionText.Text = "Ajuste busca, status, periodo ou fornecedor para localizar os registros desejados.";
        }

        private void AtualizarPainelLateral(ImportacaoNFeGridItem? itemSelecionado)
        {
            var referencia = itemSelecionado ?? _historicoFiltrado.FirstOrDefault() ?? _historicoCompleto.FirstOrDefault();
            var pendentes = _historicoCompleto.Count(item => item.TemPendencia);
            var semUsuario = _historicoCompleto.Count(item => string.Equals(item.Usuario, "Sistema", StringComparison.OrdinalIgnoreCase));

            if (referencia == null)
            {
                AlertasText.Text = $"Nenhum XML foi importado ainda. Exclusoes auditadas: {_totalExclusoesAuditadas}. Rollbacks auditados: {_totalRollbacksAuditados}.";
                PendenciasText.Text = "Sem pendencias registradas no momento. Quando um XML for excluido ou produtos forem desfeitos, um resumo fica guardado na auditoria.";
                UltimaImportacaoText.Text = "Nenhuma importacao concluida ate agora.";
                DicasUsoText.Text = "1. Clique em Nova importacao XML. 2. Confira fornecedor e produtos. 3. Volte aqui para acompanhar o historico.";
                return;
            }

            AlertasText.Text = pendentes > 0
                ? $"{pendentes} importacao(oes) exigem revisao de status, conferencia ou observacoes antes de qualquer automacao futura."
                : $"Historico carregado sem pendencias criticas conhecidas. Exclusoes auditadas: {_totalExclusoesAuditadas}. Rollbacks auditados: {_totalRollbacksAuditados}.";

            PendenciasText.Text = $"Importacoes com pendencia: {pendentes}\nRegistros sem usuario identificado: {semUsuario}\nExclusoes auditadas: {_totalExclusoesAuditadas}\nRollbacks auditados: {_totalRollbacksAuditados}\nPara relancar uma nota, use Desfazer produtos e depois Excluir XML selecionado; produtos atualizados so sao restaurados quando o snapshot ainda confere.";


            UltimaImportacaoText.Text =
                $"Fornecedor: {referencia.Fornecedor}\n" +
                $"NF-e: {referencia.NumeroNota} / Serie {referencia.Serie}\n" +
                $"Status: {referencia.Status}\n" +
                $"Produtos: {referencia.QuantidadeProdutos} | Novos: {referencia.ProdutosNovos} | Atualizados: {referencia.ProdutosAtualizados}\n" +
                $"Importada em: {referencia.DataImportacao:dd/MM/yyyy HH:mm}";

            DicasUsoText.Text =
                "Use a busca para localizar fornecedor, numero da nota, chave ou CNPJ. " +
                "Atualize o historico apos fechar a janela antiga de importacao. " +
                "Use Desfazer produtos para remover produtos novos seguros e restaurar atualizacoes com snapshot valido; use Excluir XML selecionado para remover a importacao do historico antes de relancar a nota.";
        }

        private void NovaImportacaoButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new ImportarNotaWindow();
            ConfigurarOwner(window);

            window.ShowDialog();
            CarregarHistoricoImportacoes();
        }

        private void AtualizarHistoricoButton_Click(object sender, RoutedEventArgs e)
        {
            CarregarHistoricoImportacoes();
        }

        private void ExcluirImportacaoSelecionadaButton_Click(object sender, RoutedEventArgs e)
        {
            if (_importacaoRepository == null)
            {
                return;
            }

            if (HistoricoDataGrid.SelectedItem is not ImportacaoNFeGridItem item)
            {
                MessageBox.Show(
                    "Selecione um XML na grade antes de excluir.",
                    "Importacao NF-e",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (App.IsAutomatedTestMode && !_permitirAlteracoesDestrutivasEmSmoke)
            {
                App.Logger.LogInfo($"Exclusao de importacao NF-e validada em automacao sem apagar dados sinteticos. Id={item.Id}");
                return;
            }

            var confirmado = CriticalActionDialogService.ConfirmarExclusao(
                Window.GetWindow(this),
                "importacao de NF-e",
                $"NF-e {item.NumeroNota} / Serie {item.Serie}",
                $"Fornecedor: {item.Fornecedor}\nCNPJ: {item.CnpjFornecedor}\nChave: {item.ChaveAcesso}",
                "O historico desta importacao e os itens vinculados serao removidos. Produtos ja criados no estoque nao serao apagados automaticamente.");

            if (!confirmado)
            {
                return;
            }

            try
            {
                _importacaoRepository.ExcluirImportacao(
                    item.Id,
                    App.Session.UserName,
                    "Exclusao manual pela pagina Importar NF-e para permitir relancamento da nota.");
                App.Audit.RegistrarAcaoCritica(
                    "Fiscal",
                    "ExcluirImportacaoNFe",
                    "ImportacaoNFe",
                    item.Id.ToString(),
                    $"Numero={item.NumeroNota}; Serie={item.Serie}; Chave={item.ChaveAcesso}; Fornecedor={item.Fornecedor}");

                MessageBox.Show(
                    "XML removido do historico com sucesso. A nota pode ser importada novamente.",
                    "Importacao NF-e",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                CarregarHistoricoImportacoes();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao excluir XML importado: {ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DesfazerProdutosImportacaoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_importacaoRepository == null)
            {
                return;
            }

            if (HistoricoDataGrid.SelectedItem is not ImportacaoNFeGridItem item)
            {
                MessageBox.Show(
                    "Selecione um XML na grade antes de desfazer produtos.",
                    "Importacao NF-e",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (App.IsAutomatedTestMode && !_permitirAlteracoesDestrutivasEmSmoke)
            {
                App.Logger.LogInfo($"Rollback de produtos NF-e validado em automacao sem apagar dados sinteticos. Id={item.Id}");
                return;
            }

            var confirmado = CriticalActionDialogService.ConfirmarAcao(
                Window.GetWindow(this),
                new CriticalActionRequest
                {
                    WindowTitle = "Desfazer produtos da NF-e",
                    Header = "Rollback auditavel da importacao",
                    Summary = $"Voce esta prestes a desfazer produtos criados pela NF-e {item.NumeroNota} / Serie {item.Serie}.",
                    Details = $"Fornecedor: {item.Fornecedor}\nCNPJ: {item.CnpjFornecedor}\nChave: {item.ChaveAcesso}\nProdutos novos no historico: {item.ProdutosNovos}\nProdutos atualizados: {item.ProdutosAtualizados}",
                    Impact = "Produtos criados por esta importacao so serao removidos se nao tiverem venda, OS, orcamento, agendamento, alteracao posterior ou divergencia de estoque. Produtos atualizados so serao restaurados se o snapshot posterior ainda conferir com o estado atual.",
                    Keyword = "DESFAZER",
                    ConfirmButtonText = "Desfazer produtos"
                });

            if (!confirmado)
            {
                return;
            }

            try
            {
                var resultado = _importacaoRepository.DesfazerProdutosDaImportacao(
                    item.Id,
                    App.Session.UserName,
                    "Rollback manual pela pagina Importar NF-e.");

                App.Audit.RegistrarAcaoCritica(
                    "Fiscal",
                    "RollbackProdutosImportacaoNFe",
                    "ImportacaoNFe",
                    item.Id.ToString(),
                    $"Numero={item.NumeroNota}; Serie={item.Serie}; Removidos={resultado.ProdutosRemovidos}; Bloqueados={resultado.ProdutosBloqueados}; Ignorados={resultado.ProdutosIgnorados}; AtualizacoesRevertidas={resultado.AtualizacoesRevertidas}; AtualizacoesIgnoradas={resultado.AtualizacoesIgnoradas}");

                var mensagem = new StringBuilder();
                mensagem.AppendLine("Rollback processado com auditoria.");
                mensagem.AppendLine();
                mensagem.AppendLine(resultado.GerarResumo());
                mensagem.AppendLine();
                mensagem.AppendLine(resultado.GerarDetalhes());

                if (resultado.HouveAlteracao)
                {
                    mensagem.AppendLine();
                    mensagem.AppendLine("Para relancar esta nota, use agora Excluir XML selecionado.");
                }

                MessageBox.Show(
                    mensagem.ToString(),
                    "Rollback NF-e",
                    MessageBoxButton.OK,
                    resultado.HouveAlteracao ? MessageBoxImage.Information : MessageBoxImage.Warning);

                CarregarHistoricoImportacoes();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao desfazer produtos da importacao: {ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AbrirPastaXmlsButton_Click(object sender, RoutedEventArgs e)
        {
            var pastaXmls = Path.Combine(App.RuntimeAppDataPath, "Imports");
            Directory.CreateDirectory(pastaXmls);

            if (App.IsAutomatedTestMode)
            {
                MessageBox.Show(
                    $"Pasta de XMLs preparada em:\n{pastaXmls}",
                    "Importacao NF-e",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = pastaXmls,
                UseShellExecute = true
            });
        }

        private void LimparFiltrosButton_Click(object sender, RoutedEventArgs e)
        {
            _suspendFilters = true;
            BuscaTextBox.Text = string.Empty;
            StatusComboBox.SelectedIndex = 0;
            FornecedorComboBox.SelectedIndex = 0;
            PeriodoInicialDatePicker.SelectedDate = null;
            PeriodoFinalDatePicker.SelectedDate = null;
            _suspendFilters = false;

            AplicarFiltros();
        }

        private void BuscaTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void Filtros_Changed(object sender, EventArgs e)
        {
            AplicarFiltros();
        }

        private void HistoricoDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AtualizarPainelLateral(HistoricoDataGrid.SelectedItem as ImportacaoNFeGridItem);
            AtualizarEstadoExcluirImportacao();
        }

        private void AtualizarEstadoExcluirImportacao()
        {
            if (ExcluirImportacaoSelecionadaButton == null || DesfazerProdutosImportacaoButton == null)
            {
                return;
            }

            var temSelecao = HistoricoDataGrid?.SelectedItem is ImportacaoNFeGridItem;
            ExcluirImportacaoSelecionadaButton.IsEnabled = temSelecao;
            ExcluirImportacaoSelecionadaButton.ToolTip = ExcluirImportacaoSelecionadaButton.IsEnabled
                ? "Exclui apenas o XML selecionado no historico."
                : "Selecione um XML no historico para excluir.";

            DesfazerProdutosImportacaoButton.IsEnabled = temSelecao;
            DesfazerProdutosImportacaoButton.ToolTip = temSelecao
                ? "Remove somente produtos novos criados por esta importacao quando nao houver vinculos posteriores."
                : "Selecione um XML no historico para desfazer produtos.";
        }

        private void VisualizarImportacaoButton_Click(object sender, RoutedEventArgs e)
        {
            var item = ObterGridItem(sender);
            if (item == null)
            {
                return;
            }

            var nota = item.Nota;
            var detalhes =
                $"Fornecedor: {item.Fornecedor}\n" +
                $"CNPJ: {item.CnpjFornecedor}\n" +
                $"Numero/Serie: {item.NumeroNota}/{item.Serie}\n" +
                $"Chave: {item.ChaveAcesso}\n" +
                $"Produtos: {item.QuantidadeProdutos}\n" +
                $"Novos: {item.ProdutosNovos}\n" +
                $"Atualizados: {item.ProdutosAtualizados}\n" +
                $"Valor total: {item.ValorTotal:C}\n" +
                $"Status: {item.Status}\n" +
                $"Usuario: {item.Usuario}\n" +
                $"Importada em: {item.DataImportacao:dd/MM/yyyy HH:mm}\n" +
                $"{(string.IsNullOrWhiteSpace(nota.Erro) ? "Sem observacoes adicionais." : $"Observacao: {nota.Erro}")}";

            MessageBox.Show(
                detalhes,
                "Resumo da importacao",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ProdutosImportacaoButton_Click(object sender, RoutedEventArgs e)
        {
            var item = ObterGridItem(sender);
            if (item == null)
            {
                return;
            }

            if (item.Nota.Produtos == null || item.Nota.Produtos.Count == 0)
            {
                MessageBox.Show(
                    "Detalhamento de produtos importados ainda nao esta disponivel para este registro.",
                    "Produtos importados",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var detalhes = new StringBuilder();
            detalhes.AppendLine($"Fornecedor: {item.Fornecedor}");
            detalhes.AppendLine($"NF-e: {item.NumeroNota} / Serie {item.Serie}");
            detalhes.AppendLine();

            foreach (var produto in item.Nota.Produtos.Take(18))
            {
                detalhes.AppendLine($"- {TextoOuPadrao(produto.Nome)} | Qtd: {produto.Quantidade} | Status: {produto.StatusDescricao}");
            }

            if (item.Nota.Produtos.Count > 18)
            {
                detalhes.AppendLine();
                detalhes.AppendLine($"Mais {item.Nota.Produtos.Count - 18} item(ns) disponiveis neste registro.");
            }

            MessageBox.Show(
                detalhes.ToString(),
                "Produtos da importacao",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ReprocessarImportacaoButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Reprocessamento sera implementado apos a conferencia segura do XML original.",
                "Reprocessamento em preparacao",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void CancelarImportacaoButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Use o botao Excluir XML selecionado no topo da pagina para remover apenas a importacao selecionada.",
                "Exclusao em preparacao",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private void ConfigurarOwner(Window dialog)
        {
            var owner = Window.GetWindow(this);
            if (owner != null && owner != dialog && owner.IsLoaded && owner.IsVisible)
            {
                dialog.Owner = owner;
            }
        }

        private static ImportacaoNFeGridItem? ObterGridItem(object sender)
        {
            return (sender as FrameworkElement)?.DataContext as ImportacaoNFeGridItem;
        }

        private static string FormatarStatus(StatusImportacaoNota status)
        {
            return status switch
            {
                StatusImportacaoNota.Pendente => "Pendente",
                StatusImportacaoNota.EmProcessamento => "Em processamento",
                StatusImportacaoNota.Concluida => "Concluida",
                StatusImportacaoNota.Parcial => "Parcial",
                StatusImportacaoNota.Erro => "Erro",
                StatusImportacaoNota.Duplicada => "Duplicada",
                _ => "Pendente"
            };
        }

        private static bool Contem(string valor, string busca)
        {
            return valor.Contains(busca, StringComparison.OrdinalIgnoreCase);
        }

        private static string TextoOuPadrao(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? "-" : valor.Trim();
        }

        private sealed class ImportacaoNFeGridItem
        {
            public Guid Id { get; init; }
            public DateTime DataImportacao { get; init; }
            public string Fornecedor { get; init; } = "-";
            public string CnpjFornecedor { get; init; } = "-";
            public string NumeroNota { get; init; } = "-";
            public string Serie { get; init; } = "-";
            public string ChaveAcesso { get; init; } = "-";
            public string ChaveResumida => ChaveAcesso.Length <= 20
                ? ChaveAcesso
                : $"{ChaveAcesso[..12]}...{ChaveAcesso[^4..]}";
            public int QuantidadeProdutos { get; init; }
            public int ProdutosNovos { get; init; }
            public int ProdutosAtualizados { get; init; }
            public decimal ValorTotal { get; init; }
            public string Status { get; init; } = "Pendente";
            public string Usuario { get; init; } = "Sistema";
            public bool TemPendencia { get; init; }
            public NotaFiscalImportada Nota { get; init; } = new();
        }
    }
}
