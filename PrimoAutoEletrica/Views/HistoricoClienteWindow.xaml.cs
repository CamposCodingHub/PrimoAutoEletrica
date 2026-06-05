using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Views
{
    public partial class HistoricoClienteWindow : Window
    {
        private readonly Cliente _cliente;

        public HistoricoClienteWindow(Cliente cliente)
        {
            InitializeComponent();
            _cliente = cliente;
            CarregarDadosCliente();
            CarregarDadosReais();
        }

        private void CarregarDadosCliente()
        {
            try
            {
                NomeClienteTextBlock.Text = _cliente.Nome;
                NomeTextBlock.Text = _cliente.Nome;
                TelefoneTextBlock.Text = _cliente.Telefone;
                CPFTextBlock.Text = _cliente.CPF;
                DataCadastroTextBlock.Text = _cliente.DataCadastro.ToString("dd/MM/yyyy");
                ObservacoesTextBox.Text = _cliente.Observacoes;

                VeiculosItemsControl.ItemsSource = _cliente.Veiculos;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar dados do cliente: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                App.Logger.LogError("Erro ao carregar dados do cliente no historico.", ex);
            }
        }

        private void CarregarDadosReais()
        {
            try
            {
                var servicos = new ObservableCollection<ServicoCliente>();
                foreach (var servico in _cliente.HistoricoServicos)
                {
                    servicos.Add(new ServicoCliente
                    {
                        Nome = servico.Descricao,
                        Descricao = servico.Observacoes,
                        Data = servico.DataServico,
                        Valor = servico.Valor,
                        Status = "Concluido"
                    });
                }
                ServicosDataGrid.ItemsSource = servicos;

                var pagamentos = CarregarPagamentosFinanceiros();
                PagamentosItemsControl.ItemsSource = pagamentos;

                var timeline = new ObservableCollection<EventoTimeline>
                {
                    new()
                    {
                        Titulo = "Cliente Cadastrado",
                        Descricao = "Novo cliente registrado no sistema",
                        Data = _cliente.DataCadastro,
                        Tipo = "Cadastro",
                        Icone = "Cliente"
                    }
                };

                if (_cliente.UltimaVisita.HasValue)
                {
                    timeline.Add(new EventoTimeline
                    {
                        Titulo = "Ultima Visita",
                        Descricao = "Cliente visitou a oficina",
                        Data = _cliente.UltimaVisita.Value,
                        Tipo = "Visita",
                        Icone = "Visita"
                    });
                }

                foreach (var pagamento in pagamentos
                    .Where(p => p.DataPagamento.HasValue)
                    .OrderByDescending(p => p.DataPagamento)
                    .Take(5))
                {
                    timeline.Add(new EventoTimeline
                    {
                        Titulo = "Pagamento Registrado",
                        Descricao = $"{pagamento.Metodo} - {pagamento.Valor:C2}",
                        Data = pagamento.DataPagamento!.Value,
                        Tipo = "Pagamento",
                        Icone = "$"
                    });
                }

                TimelineItemsControl.ItemsSource = timeline;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar dados reais: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                App.Logger.LogError("Erro ao carregar dados reais do historico do cliente.", ex);
            }
        }

        private ObservableCollection<PagamentoCliente> CarregarPagamentosFinanceiros()
        {
            var pagamentos = new ObservableCollection<PagamentoCliente>();
            var financeiroService = new FinanceiroDatabaseService();

            foreach (object? conta in financeiroService.ObterContasReceber())
            {
                if (conta == null)
                {
                    continue;
                }

                var nomeCliente = Convert.ToString(LerValorPropriedade(conta, "Cliente"), CultureInfo.CurrentCulture) ?? string.Empty;
                if (!ClienteCorresponde(nomeCliente))
                {
                    continue;
                }

                var status = Convert.ToString(LerValorPropriedade(conta, "Status"), CultureInfo.CurrentCulture) ?? "Pendente";
                var formaPagamento = Convert.ToString(LerValorPropriedade(conta, "FormaPagamento"), CultureInfo.CurrentCulture) ?? string.Empty;
                var descricao = Convert.ToString(LerValorPropriedade(conta, "Descricao"), CultureInfo.CurrentCulture) ?? string.Empty;
                var dataPagamento = ParseDataOpcional(Convert.ToString(LerValorPropriedade(conta, "DataPagamento"), CultureInfo.CurrentCulture));
                var valor = LerValorPropriedade(conta, "Valor");

                pagamentos.Add(new PagamentoCliente
                {
                    Valor = ConverterDecimal(valor),
                    Metodo = string.IsNullOrWhiteSpace(formaPagamento) ? descricao : formaPagamento,
                    Vencimento = ParseData(Convert.ToString(LerValorPropriedade(conta, "DataVencimento"), CultureInfo.CurrentCulture), DateTime.Today),
                    DataPagamento = dataPagamento,
                    Pago = dataPagamento.HasValue || status.Equals("Pago", StringComparison.OrdinalIgnoreCase),
                    Status = string.IsNullOrWhiteSpace(status) ? "Pendente" : status
                });
            }

            return new ObservableCollection<PagamentoCliente>(
                pagamentos.OrderByDescending(p => p.DataPagamento ?? p.Vencimento));
        }

        private bool ClienteCorresponde(string nomeFinanceiro)
        {
            var clienteAtual = NormalizarTexto(_cliente.Nome);
            var clienteFinanceiro = NormalizarTexto(nomeFinanceiro);

            if (string.IsNullOrWhiteSpace(clienteAtual) || string.IsNullOrWhiteSpace(clienteFinanceiro))
            {
                return false;
            }

            return clienteAtual == clienteFinanceiro ||
                   clienteAtual.Contains(clienteFinanceiro, StringComparison.OrdinalIgnoreCase) ||
                   clienteFinanceiro.Contains(clienteAtual, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizarTexto(string? texto)
        {
            return string.IsNullOrWhiteSpace(texto)
                ? string.Empty
                : texto.Trim().ToUpperInvariant();
        }

        private static DateTime ParseData(string? valor, DateTime fallback)
        {
            return ParseDataOpcional(valor) ?? fallback;
        }

        private static DateTime? ParseDataOpcional(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return null;
            }

            if (DateTime.TryParse(valor, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var data) ||
                DateTime.TryParse(valor, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out data))
            {
                return data;
            }

            return null;
        }

        private static decimal ConverterDecimal(object? valor)
        {
            if (valor is decimal decimalValue)
            {
                return decimalValue;
            }

            return valor is IConvertible
                ? Convert.ToDecimal(valor, CultureInfo.InvariantCulture)
                : 0m;
        }

        private static object? LerValorPropriedade(object origem, string nomePropriedade)
        {
            var propriedade = origem.GetType().GetProperty(nomePropriedade);
            return propriedade?.GetValue(origem);
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
