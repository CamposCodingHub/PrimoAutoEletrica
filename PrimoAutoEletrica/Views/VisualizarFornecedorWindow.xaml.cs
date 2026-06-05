using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace PrimoAutoEletrica.Views
{
    public partial class VisualizarFornecedorWindow : Window
    {
        private readonly Fornecedor _fornecedor;
        private readonly FornecedorOperationalService _operationalService;

        public VisualizarFornecedorWindow(Fornecedor fornecedor)
            : this(global::PrimoAutoEletrica.App.Database, fornecedor)
        {
        }

        public VisualizarFornecedorWindow(DatabaseService databaseService, Fornecedor fornecedor)
        {
            InitializeComponent();
            _fornecedor = fornecedor;
            _operationalService = new FornecedorOperationalService(databaseService);

            CarregarDadosFornecedor();
        }

        private void CarregarDadosFornecedor()
        {
            var universo = global::PrimoAutoEletrica.App.Repositories.Fornecedores.ObterTodos();
            var insights = _operationalService.CriarInsights(_fornecedor, universo);

            RazaoSocialText.Text = Exibir(_fornecedor.RazaoSocial);
            NomeFantasiaText.Text = Exibir(_fornecedor.NomeFantasia);
            CNPJText.Text = Exibir(_fornecedor.CNPJ);
            InscricaoEstadualText.Text = Exibir(_fornecedor.InscricaoEstadual);
            CategoriaText.Text = Exibir(_fornecedor.Categoria);
            CategoriaPreferencialText.Text = Exibir(insights.CategoriaPreferencial);
            TelefoneText.Text = Exibir(_fornecedor.Telefone);
            CelularText.Text = Exibir(_fornecedor.Celular);
            EmailText.Text = Exibir(_fornecedor.Email);
            ContatoPrincipalText.Text = $"{insights.ContatoPrincipalNome} | {insights.ContatoPrincipalCargo}";
            WhatsAppVendedorText.Text = Exibir(insights.WhatsAppVendedor);

            CidadeEstadoText.Text = $"{Exibir(_fornecedor.Cidade)} / {Exibir(_fornecedor.Estado)}";
            EnderecoCompletoText.Text = $"{Exibir(_fornecedor.Rua)}, {Exibir(_fornecedor.Numero)} - {Exibir(_fornecedor.Bairro)} | CEP {Exibir(_fornecedor.CEP)}";
            FormaPagamentoText.Text = Exibir(insights.CondicaoPagamento);
            PedidoMinimoText.Text = $"R$ {_fornecedor.PedidoMinimo:F2}";
            PrazoMedioEntregaText.Text = insights.PrazoMedioEntregaDias > 0 ? $"{insights.PrazoMedioEntregaDias} dia(s)" : "Nao informado";
            ObservacoesText.Text = string.IsNullOrWhiteSpace(_fornecedor.Observacoes) ? "Sem observacoes" : _fornecedor.Observacoes;

            StatusText.Text = _fornecedor.Ativo ? "Ativo" : "Inativo";
            StatusText.Foreground = new SolidColorBrush(_fornecedor.Ativo
                ? Color.FromRgb(16, 185, 129)
                : Color.FromRgb(239, 68, 68));

            RankingText.Text = insights.RankingGeral > 0 ? $"#{insights.RankingGeral} em compras/estoque" : "Sem ranking";
            AlertasResumoText.Text = insights.ResumoAlertas;
            UltimaCompraText.Text = insights.UltimaCompra?.ToString("dd/MM/yyyy") ?? "Nenhuma";
            TotalComprasText.Text = $"R$ {insights.TotalCompras:F2}";
            ComprasProdutoFornecedorText.Text = $"{insights.QuantidadeComprasProdutoFornecedor} compra(s) vinculada(s)";
            ValorProdutoFornecedorText.Text = $"R$ {insights.TotalComprasProdutoFornecedor:F2}";
            TicketMedioCompraText.Text = insights.TicketMedioCompra > 0
                ? $"R$ {insights.TicketMedioCompra:F2}"
                : "Sem historico";
            ProdutosRelacionadosResumoText.Text = $"{insights.ProdutosRelacionados} produto(s) | {insights.ProdutoFornecedores.Count} vinculo(s) | {insights.ProdutosEstoqueBaixo} com alerta";

            ProdutosRelacionadosItemsControl.ItemsSource = insights.ProdutosPrincipais;
            NotasRecentesItemsControl.ItemsSource = insights.HistoricoNotas.Any()
                ? insights.HistoricoNotas.Select(item => new
                {
                    Numero = $"NF {item.Numero} / Serie {item.Serie}",
                    Status = item.DataReferencia?.ToString("dd/MM/yyyy") ?? item.Status,
                    ValorTotal = item.ValorTotal
                }).ToList()
                : new[]
                {
                    new
                    {
                        Numero = "Sem notas recentes",
                        Status = "Nenhum XML vinculado",
                        ValorTotal = 0m
                    }
                };
        }

        private static string Exibir(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? "Nao informado" : valor.Trim();
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
