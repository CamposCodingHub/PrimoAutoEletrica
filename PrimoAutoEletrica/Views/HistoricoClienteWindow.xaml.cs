using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

using PrimoAutoEletrica.Helpers;
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
                MessageBox.Show($"Erro ao carregar dados do cliente: {ex.Message}", UiText.T("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
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

                var orcamentos = CarregarOrcamentosCliente();
                OrcamentosDataGrid.ItemsSource = orcamentos
                    .Select(orcamento => new
                    {
                        orcamento.Numero,
                        Data = orcamento.DataCriacao,
                        orcamento.Status,
                        orcamento.Total,
                        Itens = orcamento.Itens.Count
                    })
                    .ToList();

                var ordensServico = CarregarOrdensServicoCliente();
                OrdensServicoDataGrid.ItemsSource = ordensServico
                    .Select(ordem => new
                    {
                        ordem.Numero,
                        Data = ordem.DataAbertura,
                        ordem.Status,
                        Veiculo = string.IsNullOrWhiteSpace(ordem.VeiculoDescricaoSnapshot)
                            ? "-"
                            : ordem.VeiculoDescricaoSnapshot,
                        Total = ordem.Itens.Sum(item => item.Total) - ordem.Desconto
                    })
                    .ToList();

                var vendas = CarregarVendasCliente();
                VendasDataGrid.ItemsSource = vendas
                    .Select(venda => new
                    {
                        venda.Data,
                        venda.FormaPagamento,
                        venda.Status,
                        Itens = venda.Itens.Count,
                        venda.Total
                    })
                    .ToList();

                var pagamentos = CarregarPagamentosFinanceiros();
                PagamentosItemsControl.ItemsSource = pagamentos;
                DebitosItemsControl.ItemsSource = pagamentos
                    .Where(pagamento => !pagamento.Pago)
                    .Select(pagamento => new
                    {
                        Descricao = string.IsNullOrWhiteSpace(pagamento.Metodo)
                            ? "Conta a receber pendente"
                            : pagamento.Metodo,
                        pagamento.Valor,
                        pagamento.Vencimento
                    })
                    .ToList();

                DocumentosItemsControl.ItemsSource = CarregarDocumentosCliente(ordensServico);

                var timeline = new List<EventoTimeline>
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

                timeline.AddRange(orcamentos.Take(5).Select(orcamento => new EventoTimeline
                {
                    Titulo = $"Orcamento {orcamento.Numero}",
                    Descricao = $"{orcamento.Status} - {orcamento.Total:C2}",
                    Data = orcamento.DataCriacao,
                    Tipo = "Orcamento",
                    Icone = "ORC"
                }));

                timeline.AddRange(ordensServico.Take(5).Select(ordem => new EventoTimeline
                {
                    Titulo = $"OS {ordem.Numero}",
                    Descricao = $"{ordem.Status} - {ordem.VeiculoDescricaoSnapshot}",
                    Data = ordem.DataAbertura,
                    Tipo = "OrdemServico",
                    Icone = "OS"
                }));

                timeline.AddRange(vendas.Take(5).Select(venda => new EventoTimeline
                {
                    Titulo = "Venda registrada",
                    Descricao = $"{venda.FormaPagamento} - {venda.Total:C2} - {venda.Status}",
                    Data = venda.Data,
                    Tipo = "Venda",
                    Icone = "VEN"
                }));

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

                foreach (var debito in pagamentos
                    .Where(p => !p.Pago)
                    .OrderBy(p => p.Vencimento)
                    .Take(5))
                {
                    timeline.Add(new EventoTimeline
                    {
                        Titulo = "Debito pendente",
                        Descricao = $"{debito.Metodo} - {debito.Valor:C2}",
                        Data = debito.Vencimento,
                        Tipo = "Debito",
                        Icone = "REC"
                    });
                }

                TimelineItemsControl.ItemsSource = timeline
                    .OrderByDescending(evento => evento.Data)
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar dados reais: {ex.Message}", UiText.T("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                App.Logger.LogError("Erro ao carregar dados reais do historico do cliente.", ex);
            }
        }

        private List<Orcamento> CarregarOrcamentosCliente()
        {
            try
            {
                return new OrcamentoDatabaseService()
                    .ObterTodosOrcamentos()
                    .Where(orcamento =>
                        orcamento.ClienteId == _cliente.Id ||
                        ClienteCorresponde(orcamento.Cliente?.Nome ?? string.Empty))
                    .OrderByDescending(orcamento => orcamento.DataCriacao)
                    .Take(20)
                    .ToList();
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao carregar orcamentos do historico do cliente.", ex);
                return new List<Orcamento>();
            }
        }

        private List<OrdemServico> CarregarOrdensServicoCliente()
        {
            try
            {
                return App.Repositories.OrdensServico
                    .ObterPorClienteId(_cliente.Id, incluirInativas: true)
                    .OrderByDescending(ordem => ordem.DataAbertura)
                    .Take(20)
                    .ToList();
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao carregar OS do historico do cliente.", ex);
                return new List<OrdemServico>();
            }
        }

        private List<Venda> CarregarVendasCliente()
        {
            try
            {
                return new global::PrimoAutoEletrica.Repositories.VendaRepository()
                    .ObterVendas()
                    .Where(venda =>
                        venda.Cliente?.Id == _cliente.Id ||
                        ClienteCorresponde(venda.Cliente?.Nome ?? string.Empty))
                    .OrderByDescending(venda => venda.Data)
                    .Take(20)
                    .ToList();
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao carregar vendas do historico do cliente.", ex);
                return new List<Venda>();
            }
        }

        private List<object> CarregarDocumentosCliente(IEnumerable<OrdemServico> ordensServico)
        {
            var documentos = new List<object>();

            AdicionarDocumento(documentos, "Foto do cliente", _cliente.ImagemUrl);
            AdicionarDocumento(documentos, "Documento do cliente", _cliente.CaminhoDocumento);
            AdicionarDocumento(documentos, "Assinatura do cliente", _cliente.CaminhoAssinatura);

            foreach (var veiculo in _cliente.Veiculos)
            {
                AdicionarDocumento(documentos, $"Foto veiculo {veiculo.Placa}", veiculo.ImagemUrl);
                AdicionarDocumento(documentos, $"Documento veiculo {veiculo.Placa}", veiculo.DocumentoImagemUrl);
            }

            foreach (var ordem in ordensServico)
            {
                AdicionarDocumento(documentos, $"Fotos antes OS {ordem.Numero}", ordem.FotosAntes);
                AdicionarDocumento(documentos, $"Fotos depois OS {ordem.Numero}", ordem.FotosDepois);
                AdicionarDocumento(documentos, $"Assinatura OS {ordem.Numero}", ordem.AssinaturaClienteUrl);
            }

            if (documentos.Count == 0)
            {
                documentos.Add(new
                {
                    Tipo = "Sem anexos",
                    Resumo = "Nenhuma foto, documento ou assinatura vinculada ao cliente ate o momento."
                });
            }

            return documentos;
        }

        private static void AdicionarDocumento(List<object> documentos, string tipo, string? caminho)
        {
            if (string.IsNullOrWhiteSpace(caminho))
            {
                return;
            }

            documentos.Add(new
            {
                Tipo = tipo,
                Resumo = Path.GetFileName(caminho) ?? caminho
            });
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
