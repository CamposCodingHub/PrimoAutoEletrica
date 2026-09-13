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
                var snapshot = Resolver360Service().ObterCliente360(_cliente.Id);
                AplicarResumo360(snapshot);

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

                var pagamentos = CarregarPagamentosFinanceirosVinculados();
                PagamentosItemsControl.ItemsSource = pagamentos;
                DebitosItemsControl.ItemsSource = pagamentos
                    .Where(pagamento => !pagamento.Pago)
                    .Select(pagamento => new
                    {
                        Descricao = string.IsNullOrWhiteSpace(pagamento.Metodo)
                            ? "Conta a receber vinculada (ID)"
                            : pagamento.Metodo,
                        pagamento.Valor,
                        pagamento.Vencimento
                    })
                    .ToList();

                DocumentosItemsControl.ItemsSource = CarregarDocumentosCliente(ordensServico);

                TimelineItemsControl.ItemsSource = snapshot.Timeline
                    .Select(item => new EventoTimeline
                    {
                        Titulo = item.Titulo,
                        Descricao = item.Descricao,
                        Data = item.Data,
                        Tipo = item.Tipo,
                        Icone = item.Tipo switch
                        {
                            "OrdemServico" => "OS",
                            "Orcamento" => "ORC",
                            "Venda" => "VEN",
                            "Pagamento" => "$",
                            _ => "Cliente"
                        }
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar dados reais: {ex.Message}", UiText.T("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                App.Logger.LogError("Erro ao carregar dados reais do historico do cliente.", ex);
            }
        }

        private void AplicarResumo360(Cliente360Snapshot snapshot)
        {
            if (Cliente360KpisTextBlock == null)
            {
                return;
            }

            var ticket = snapshot.TicketMedioOs.HasValue
                ? snapshot.TicketMedioOs.Value.ToString("C")
                : "N/A";
            var ultima = snapshot.UltimaVisita.HasValue
                ? snapshot.UltimaVisita.Value.ToString("dd/MM/yyyy")
                : "N/A";
            var dias = snapshot.DiasDesdeUltimaVisita.HasValue
                ? snapshot.DiasDesdeUltimaVisita.Value.ToString(CultureInfo.CurrentCulture)
                : "N/A";

            Cliente360KpisTextBlock.Text =
                $"Veículos: {snapshot.VeiculosCount} · OS: {snapshot.OsCount} · Visitas (OS): {snapshot.VisitasCount}\n" +
                $"Receita total (OS+Vendas por ID): {snapshot.ReceitaTotal:C} · 12 meses: {snapshot.Receita12Meses:C}\n" +
                $"Ticket médio OS: {ticket} · Orçamentos: {snapshot.OrcamentosCount} (aprovados {snapshot.OrcamentosAprovados} / recusados {snapshot.OrcamentosRecusados})\n" +
                $"Valor perdido (recusados): {snapshot.ValorPerdidoOrcamentos:C} · Última visita: {ultima} · Dias sem visita: {dias}\n" +
                $"Total gasto (cadastro): {snapshot.TotalGastoCadastro:C} · Dívida vinculada OS/Orç (ID): {snapshot.DividaVinculadaPorId:C} ({snapshot.ContasReceberVinculadasPendentes} pendentes)";

            Cliente360DividaNotaTextBlock.Text =
                $"Dívida total cliente: {snapshot.DividaTotalDisplay}. {snapshot.FonteDivida}";
        }

        private static IPrimox360Service Resolver360Service()
        {
            try
            {
                if (App.Services?.GetService(typeof(IPrimox360Service)) is IPrimox360Service svc)
                {
                    return svc;
                }
            }
            catch
            {
            }

            return new Primox360Service(App.Repositories.Clientes, App.Repositories.OrdensServico);
        }

        private List<Orcamento> CarregarOrcamentosCliente()
        {
            try
            {
                return new OrcamentoDatabaseService()
                    .ObterTodosOrcamentos()
                    .Where(orcamento => orcamento.ClienteId == _cliente.Id)
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
                    .Where(venda => venda.Cliente?.Id == _cliente.Id)
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

        private ObservableCollection<PagamentoCliente> CarregarPagamentosFinanceirosVinculados()
        {
            var vinculados = Resolver360Service().ObterContasReceberVinculadasAoCliente(_cliente.Id);
            var pagamentos = vinculados.Select(conta => new PagamentoCliente
            {
                Valor = conta.Valor,
                Metodo = string.IsNullOrWhiteSpace(conta.FormaPagamento) ? conta.Descricao : conta.FormaPagamento,
                Vencimento = conta.DataVencimento,
                DataPagamento = conta.DataPagamento,
                Pago = conta.Pago,
                Status = string.IsNullOrWhiteSpace(conta.Status) ? "Pendente" : conta.Status
            });

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
