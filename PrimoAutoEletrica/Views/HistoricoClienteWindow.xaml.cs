using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.ViewModels;

namespace PrimoAutoEletrica.Views
{
    public partial class HistoricoClienteWindow : Window
    {
        private Cliente _cliente;

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
                    .Select(orcamento => new OrcamentoHistoricoRow
                    {
                        Id = orcamento.Id,
                        Numero = orcamento.Numero,
                        Data = orcamento.DataCriacao,
                        Status = orcamento.Status,
                        Total = orcamento.Total,
                        Itens = orcamento.Itens.Count
                    })
                    .ToList();

                var ordensServico = CarregarOrdensServicoCliente();
                OrdensServicoDataGrid.ItemsSource = ordensServico
                    .Select(ordem => new OrdemServicoHistoricoRow
                    {
                        Id = ordem.Id,
                        Numero = ordem.Numero,
                        Data = ordem.DataAbertura,
                        Status = ordem.Status,
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

                try
                {
                    var posVendaService = new PosVendaService();
                    var posVendaItens = posVendaService.ListarPorClienteId(_cliente.Id);
                    PosVendaDataGrid.ItemsSource = posVendaItens
                        .OrderByDescending(i => i.DataCriacao)
                        .Select(i => new PosVendaHistoricoRow
                        {
                            Id = i.Id,
                            OrdemServicoId = i.OrdemServicoId,
                            VeiculoId = i.VeiculoId,
                            DataCriacao = i.DataCriacao,
                            Tipo = i.Tipo.ToString(),
                            Status = i.Status.ToString(),
                            Referencia = $"OS: {(string.IsNullOrWhiteSpace(i.OsNumeroSnapshot) ? (i.OrdemServicoId != Guid.Empty ? i.OrdemServicoId.ToString()[..6] : "-") : i.OsNumeroSnapshot)} | Placa: {(string.IsNullOrWhiteSpace(i.PlacaSnapshot) ? "-" : i.PlacaSnapshot)}",
                            Responsavel = string.IsNullOrWhiteSpace(i.Responsavel) ? "-" : i.Responsavel
                        })
                        .ToList();
                }
                catch
                {
                    // Degrada graciosamente se tabela não estiver disponível
                }

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

        // P0-03 (2026-09-19): matching financeiro por NOME removido.
        // Contas a receber / pagamentos usam exclusivamente ClienteId via Primox360Service.
        // Não reintroduzir Contains/igualdade parcial de nome para dados financeiros.

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

        private void NovaOs360Button_Click(object sender, RoutedEventArgs e)
        {
            var janela = new OrdemServicoWindow(App.Database, null, _cliente);
            WindowOwnerHelper.ConfigureOwner(janela, this);

            App.Audit.RegistrarAcaoCritica(
                "Clientes",
                "Cliente360NovaOs",
                "Cliente",
                _cliente.Id.ToString(),
                $"Nome={_cliente.Nome}; Origem=HistoricoCliente360");

            if (App.IsAutomatedTestMode)
            {
                ValidarJanelaEmAutomacao(janela, "nova OS do Cliente 360");
                return;
            }

            janela.ShowDialog();
        }

        private void NovoOrcamento360Button_Click(object sender, RoutedEventArgs e)
        {
            var janela = new NovoOrcamentoWindow(_cliente);
            WindowOwnerHelper.ConfigureOwner(janela, this);

            App.Audit.RegistrarAcaoCritica(
                "Clientes",
                "Cliente360NovoOrcamento",
                "Cliente",
                _cliente.Id.ToString(),
                $"Nome={_cliente.Nome}; Origem=HistoricoCliente360");

            if (App.IsAutomatedTestMode)
            {
                ValidarJanelaEmAutomacao(janela, "novo orcamento do Cliente 360");
                return;
            }

            janela.ShowDialog();
        }

        private void Agendar360Button_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = new NovoAgendamentoPremiumViewModel();
            viewModel.PrefillFromCliente(_cliente);
            var janela = new NovoAgendamentoPremiumWindow(viewModel);
            WindowOwnerHelper.ConfigureOwner(janela, this);

            App.Audit.RegistrarAcaoCritica(
                "Clientes",
                "Cliente360Agendar",
                "Cliente",
                _cliente.Id.ToString(),
                $"Nome={_cliente.Nome}; Origem=HistoricoCliente360");

            if (App.IsAutomatedTestMode)
            {
                ValidarJanelaEmAutomacao(janela, "agendamento do Cliente 360");
                return;
            }

            janela.ShowDialog();
        }

        private void NovoVeiculo360Button_Click(object sender, RoutedEventArgs e)
        {
            var clientes = App.Repositories.Clientes.ObterTodos()
                .ToDictionary(c => c.Id, c => c);
            var janela = new NovoVeiculoWindow(App.Database, clientes, clientePreSelecionado: _cliente);
            WindowOwnerHelper.ConfigureOwner(janela, this);

            App.Audit.RegistrarAcaoCritica(
                "Clientes",
                "Cliente360NovoVeiculo",
                "Cliente",
                _cliente.Id.ToString(),
                $"Nome={_cliente.Nome}; Origem=HistoricoCliente360");

            if (App.IsAutomatedTestMode)
            {
                ValidarJanelaEmAutomacao(janela, "novo veiculo do Cliente 360");
                return;
            }

            if (janela.ShowDialog() == true)
            {
                try
                {
                    var atualizado = App.Repositories.Clientes.ObterPorId(_cliente.Id);
                    if (atualizado != null)
                    {
                        _cliente = atualizado;
                        CarregarDadosCliente();
                        CarregarDadosReais();
                    }
                }
                catch (Exception ex)
                {
                    App.Logger.LogWarning($"Cliente360 refresh apos novo veiculo: {ex.Message}", "Clientes");
                }
            }
        }

        private void AbrirVeiculo360FromCliente_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button { Tag: Veiculo veiculo })
            {
                return;
            }

            var janela = new VisualizarVeiculoWindow(veiculo, App.Database);
            WindowOwnerHelper.ConfigureOwner(janela, this);

            App.Audit.RegistrarAcaoCritica(
                "Clientes",
                "Cliente360AbrirVeiculo",
                "Veiculo",
                veiculo.Id.ToString(),
                $"Placa={veiculo.Placa}; ClienteId={_cliente.Id}");

            if (App.IsAutomatedTestMode)
            {
                ValidarJanelaEmAutomacao(janela, "veiculo 360 a partir do Cliente 360");
                return;
            }

            janela.ShowDialog();
        }

        private void WhatsApp360Button_Click(object sender, RoutedEventArgs e)
        {
            var contatoAtual = string.IsNullOrWhiteSpace(_cliente.WhatsApp)
                ? _cliente.Telefone
                : _cliente.WhatsApp;

            if (!CadastroValidationHelper.TryObterTelefoneWhatsApp(contatoAtual, out var telefone))
            {
                MessageBox.Show(
                    UiText.T("ClientNoWhatsApp"),
                    UiText.T("ContactMissing"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (!_cliente.ConsentimentoLGPD || !_cliente.AutorizaContatoWhatsApp)
            {
                var confirmado = CriticalActionDialogService.ConfirmarAcao(
                    this,
                    new CriticalActionRequest
                    {
                        WindowTitle = "Contato LGPD pendente",
                        Header = "Revise o consentimento antes do contato",
                        Summary = $"O cliente '{_cliente.Nome}' nao possui consentimento completo para contato por WhatsApp.",
                        Details = $"LGPD: {(_cliente.ConsentimentoLGPD ? "registrado" : "pendente")}\nWhatsApp: {(_cliente.AutorizaContatoWhatsApp ? "autorizado" : "nao autorizado")}",
                        Impact = "Confirme somente se este contato for necessario para atendimento em andamento ou se o consentimento foi validado fora do sistema.",
                        Keyword = "CONTATAR",
                        ConfirmButtonText = "Abrir WhatsApp"
                    });

                if (!confirmado)
                {
                    return;
                }
            }

            App.Audit.RegistrarAcaoCritica(
                "Clientes",
                "Cliente360WhatsApp",
                "Cliente",
                _cliente.Id.ToString(),
                $"Nome={_cliente.Nome}; LGPD={_cliente.ConsentimentoLGPD}; WhatsAppAutorizado={_cliente.AutorizaContatoWhatsApp}");

            if (App.IsAutomatedTestMode)
            {
                WindowInteractionHelper.LogAutomationExternalAction(
                    $"WhatsApp do Cliente 360 validado em automacao para o telefone {telefone}.",
                    "Clientes");
                return;
            }

            SecureProcessLauncher.OpenWhatsAppLink($"https://wa.me/{telefone}");
        }

        private static void ValidarJanelaEmAutomacao(Window janela, string contexto)
        {
            try
            {
                janela.ApplyTemplate();
                if (janela.Content is FrameworkElement content)
                {
                    content.ApplyTemplate();
                    content.Measure(new Size(1280, 720));
                    content.Arrange(new Rect(0, 0, 1280, 720));
                    content.UpdateLayout();
                }

                App.Logger.LogInfo($"Janela de {contexto} validada em automacao sem abrir modal bloqueante.", "Clientes");
            }
            finally
            {
                janela.Close();
            }
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OrdensServicoDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (OrdensServicoDataGrid.SelectedItem is not OrdemServicoHistoricoRow row)
            {
                return;
            }

            try
            {
                var ordem = App.Repositories.OrdensServico.ObterPorId(row.Id);
                if (ordem == null)
                {
                    MessageBox.Show("OS não encontrada.", UiText.T("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var janela = new OrdemServicoWindow(App.Database, ordem);
                WindowOwnerHelper.ConfigureOwner(janela, this);
                App.Audit.RegistrarAcaoCritica(
                    "Clientes",
                    "Cliente360AbrirOs",
                    "OrdemServico",
                    ordem.Id.ToString(),
                    $"Numero={ordem.Numero}; Origem=HistoricoCliente360");

                if (App.IsAutomatedTestMode)
                {
                    ValidarJanelaEmAutomacao(janela, "OS do Cliente 360");
                    return;
                }

                janela.ShowDialog();
                CarregarDadosReais();
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao abrir OS a partir do Cliente 360.", ex, "Clientes");
                MessageBox.Show($"Erro ao abrir OS:\n{ex.Message}", UiText.T("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OrcamentosDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (OrcamentosDataGrid.SelectedItem is not OrcamentoHistoricoRow row)
            {
                return;
            }

            try
            {
                var orcamento = new OrcamentoDatabaseService().ObterOrcamentoPorId(row.Id);
                if (orcamento == null)
                {
                    MessageBox.Show("Orçamento não encontrado.", UiText.T("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var janela = new NovoOrcamentoWindow(orcamento);
                WindowOwnerHelper.ConfigureOwner(janela, this);
                App.Audit.RegistrarAcaoCritica(
                    "Clientes",
                    "Cliente360AbrirOrcamento",
                    "Orcamento",
                    orcamento.Id.ToString(),
                    $"Numero={orcamento.Numero}; Origem=HistoricoCliente360");

                if (App.IsAutomatedTestMode)
                {
                    ValidarJanelaEmAutomacao(janela, "orcamento do Cliente 360");
                    return;
                }

                janela.ShowDialog();
                CarregarDadosReais();
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao abrir orcamento a partir do Cliente 360.", ex, "Clientes");
                MessageBox.Show($"Erro ao abrir orçamento:\n{ex.Message}", UiText.T("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PosVenda360Button_Click(object sender, RoutedEventArgs e)
        {
            var ordens = CarregarOrdensServicoCliente();
            var ultima = ordens.FirstOrDefault();
            var osId = ultima?.Id ?? Guid.Empty;

            var janela = new PosVendaWindow(
                ordemServicoId: osId,
                veiculoId: ultima?.VeiculoId,
                clienteId: _cliente.Id,
                osNumero: ultima?.Numero ?? "AVULSO",
                clienteNome: _cliente.Nome,
                veiculoPlaca: ultima?.VeiculoDescricaoSnapshot ?? string.Empty);

            WindowOwnerHelper.ConfigureOwner(janela, this);
            App.Audit.RegistrarAcaoCritica(
                "Clientes",
                "Cliente360PosVenda",
                "Cliente",
                _cliente.Id.ToString(),
                $"Nome={_cliente.Nome}; Origem=HistoricoCliente360");

            if (App.IsAutomatedTestMode)
            {
                ValidarJanelaEmAutomacao(janela, "pós-venda do Cliente 360");
                return;
            }

            janela.ShowDialog();
            CarregarDadosReais();
        }

        private void PosVendaDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PosVendaDataGrid.SelectedItem is not PosVendaHistoricoRow row)
                return;

            var janela = new PosVendaWindow(
                ordemServicoId: row.OrdemServicoId,
                veiculoId: row.VeiculoId,
                clienteId: _cliente.Id,
                osNumero: row.Referencia,
                clienteNome: _cliente.Nome);

            WindowOwnerHelper.ConfigureOwner(janela, this);
            if (App.IsAutomatedTestMode)
            {
                ValidarJanelaEmAutomacao(janela, "pós-venda detalhe do Cliente 360");
                return;
            }

            janela.ShowDialog();
            CarregarDadosReais();
        }

        private sealed class PosVendaHistoricoRow
        {
            public Guid Id { get; set; }
            public Guid OrdemServicoId { get; set; }
            public Guid? VeiculoId { get; set; }
            public DateTime DataCriacao { get; set; }
            public string Tipo { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string Referencia { get; set; } = string.Empty;
            public string Responsavel { get; set; } = string.Empty;
        }

        private sealed class OrdemServicoHistoricoRow
        {
            public Guid Id { get; set; }
            public string Numero { get; set; } = string.Empty;
            public DateTime Data { get; set; }
            public string Status { get; set; } = string.Empty;
            public string Veiculo { get; set; } = string.Empty;
            public decimal Total { get; set; }
        }

        private sealed class OrcamentoHistoricoRow
        {
            public Guid Id { get; set; }
            public string Numero { get; set; } = string.Empty;
            public DateTime Data { get; set; }
            public string Status { get; set; } = string.Empty;
            public decimal Total { get; set; }
            public int Itens { get; set; }
        }
    }
}
