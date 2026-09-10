using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace PrimoAutoEletrica.Views.Clientes
{
    public partial class VisualizarClienteWindow : Window
    {
        private Cliente _cliente;

        public VisualizarClienteWindow()
            : this(new Cliente())
        {
        }

        public VisualizarClienteWindow(Cliente cliente)
        {
            InitializeComponent();
                DataContext = new ViewModels.ClientesViewModel();
            _cliente = cliente.Id == Guid.Empty
                ? cliente
                : App.Repositories.Clientes.ObterPorId(cliente.Id) ?? cliente;
            CarregarDados();
        }

        private void CarregarDados()
        {
            NomeClienteHeaderText.Text = string.IsNullOrWhiteSpace(_cliente.Nome) ? "Cliente" : _cliente.Nome;
            ResumoHeaderText.Text = CriarResumoHeader();

            NomeTextBlock.Text = ValorOuPadrao(_cliente.Nome);
            TelefoneTextBlock.Text = ValorOuPadrao(_cliente.Telefone);
            WhatsAppTextBlock.Text = ValorOuPadrao(_cliente.WhatsApp);
            DocumentoTextBlock.Text = ValorOuPadrao(_cliente.Documento);
            TipoPessoaTextBlock.Text = _cliente.TipoPessoaDescricao;
            RgIeTextBlock.Text = ValorOuPadrao(_cliente.RG, "Nao informado.");
            EmailTextBlock.Text = ValorOuPadrao(_cliente.Email);
            CategoriaTextBlock.Text = _cliente.ClienteVip ? "VIP" : "Relacionamento padrao";
            LgpdTextBlock.Text = CriarResumoLgpd();
            EnderecoTextBlock.Text = MontarEndereco();
            ObservacoesTextBlock.Text = ValorOuPadrao(_cliente.Observacoes, "Sem observacoes registradas.");

            TotalGastoTextBlock.Text = _cliente.TotalGasto.ToString("C");
            TotalServicosTextBlock.Text = _cliente.TotalServicos.ToString();
            PontosTextBlock.Text = _cliente.PontosFidelidade.ToString();
            UltimaVisitaTextBlock.Text = _cliente.UltimaVisita?.ToString("dd/MM/yyyy") ?? "-";

            DocumentoStatusTextBlock.Text = string.IsNullOrWhiteSpace(_cliente.CaminhoDocumento)
                ? "Nenhum documento anexado."
                : $"Documento vinculado: {Path.GetFileName(_cliente.CaminhoDocumento)}";
            AssinaturaStatusTextBlock.Text = string.IsNullOrWhiteSpace(_cliente.CaminhoAssinatura)
                ? "Nenhuma assinatura registrada."
                : $"Assinatura vinculada: {Path.GetFileName(_cliente.CaminhoAssinatura)}";

            VeiculosDataGrid.ItemsSource = _cliente.Veiculos
                .Select(veiculo => new
                {
                    veiculo.Marca,
                    veiculo.Modelo,
                    veiculo.Ano,
                    veiculo.Placa,
                    veiculo.Motor,
                    veiculo.Combustivel,
                    veiculo.Quilometragem
                })
                .ToList();

            TimelineItemsControl.ItemsSource = CriarTimeline();
            AplicarStatus();
            AplicarInsights();
            AtualizarPreviewFoto();
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void EditarButton_Click(object sender, RoutedEventArgs e)
        {
            var janela = new EditarClienteWindow(_cliente);
            WindowOwnerHelper.ConfigureOwner(janela, this);

            if (App.IsAutomatedTestMode)
            {
                ValidarJanelaEmAutomacao(janela);
                return;
            }

            if (janela.ShowDialog() == true)
            {
                _cliente = App.Repositories.Clientes.ObterPorId(_cliente.Id) ?? _cliente;
                CarregarDados();
            }
        }

        private void WhatsAppButton_Click(object sender, RoutedEventArgs e)
        {
            var contatoAtual = string.IsNullOrWhiteSpace(_cliente.WhatsApp)
                ? _cliente.Telefone
                : _cliente.WhatsApp;

            if (!CadastroValidationHelper.TryObterTelefoneWhatsApp(contatoAtual, out var telefone))
            {
                WindowInteractionHelper.ShowMessage(
                    UiText.T("ClientNoWhatsApp"),
                    UiText.T("ContactMissing"),
                    MessageBoxImage.Information,
                    "Clientes");
                return;
            }

            if (!(_cliente.ConsentimentoLGPD && _cliente.AutorizaContatoWhatsApp))
            {
                var confirmado = CriticalActionDialogService.ConfirmarAcao(
                    this,
                    new CriticalActionRequest
                    {
                        WindowTitle = "Contato LGPD pendente",
                        Header = "Revise o consentimento antes do contato",
                        Summary = $"O cliente '{_cliente.Nome}' nao possui consentimento completo para WhatsApp.",
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

            if (App.IsAutomatedTestMode)
            {
                WindowInteractionHelper.LogAutomationExternalAction(
                    $"WhatsApp do cliente validado em automacao para o telefone {telefone} sem abrir aplicativo externo.",
                    "Clientes");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = $"https://wa.me/{telefone}",
                UseShellExecute = true
            });
        }

        private void AbrirDocumentoButton_Click(object sender, RoutedEventArgs e)
        {
            AbrirArquivoSeExistir(_cliente.CaminhoDocumento, "documento");
        }

        private void AbrirAssinaturaButton_Click(object sender, RoutedEventArgs e)
        {
            AbrirArquivoSeExistir(_cliente.CaminhoAssinatura, "assinatura");
        }

        private void AtualizarPreviewFoto()
        {
            var preview = ClienteMediaService.TryCreatePreviewSource(_cliente.ImagemUrl);
            FotoPreviewImage.Source = preview;
            FotoPreviewImage.Visibility = preview == null ? Visibility.Collapsed : Visibility.Visible;
            FotoPlaceholderText.Visibility = preview == null ? Visibility.Visible : Visibility.Collapsed;
        }

        private string CriarResumoHeader()
        {
            var primeiroVeiculo = _cliente.Veiculos.FirstOrDefault();
            var resumoVeiculo = primeiroVeiculo == null
                ? "Sem veiculos vinculados"
                : $"{primeiroVeiculo.Marca} {primeiroVeiculo.Modelo}".Trim();
            var ultimaVisita = _cliente.UltimaVisita?.ToString("dd/MM/yyyy") ?? "sem ultima visita";
            var status = !_cliente.Ativo
                ? "Cliente inativo"
                : _cliente.ClienteVip
                    ? "Cliente VIP"
                    : "Cliente ativo";
            return $"{status} • {_cliente.TipoPessoaDescricao} • {resumoVeiculo} • Ultima visita {ultimaVisita}";
        }

        private string MontarEndereco()
        {
            var partes = new List<string>
            {
                _cliente.Rua,
                _cliente.Numero,
                _cliente.Bairro,
                _cliente.Cidade,
                _cliente.Estado,
                _cliente.CEP
            };

            var endereco = string.Join(", ", partes.Where(parte => !string.IsNullOrWhiteSpace(parte)));
            return ValorOuPadrao(endereco, "Endereco nao informado.");
        }

        private string CriarResumoLgpd()
        {
            if (!_cliente.ConsentimentoLGPD)
            {
                return "Consentimento LGPD pendente. Evite contatos ativos ate registrar aceite.";
            }

            var data = _cliente.DataConsentimentoLGPD?.ToString("dd/MM/yyyy HH:mm") ?? "data nao registrada";
            var canal = string.IsNullOrWhiteSpace(_cliente.OrigemConsentimentoLGPD)
                ? "origem nao informada"
                : _cliente.OrigemConsentimentoLGPD;
            var whatsapp = _cliente.AutorizaContatoWhatsApp ? "WhatsApp autorizado" : "WhatsApp nao autorizado";
            return $"Consentimento registrado em {data} ({canal}). {whatsapp}.";
        }

        private void AplicarStatus()
        {
            var diasSemVisita = _cliente.UltimaVisita.HasValue
                ? (DateTime.Today - _cliente.UltimaVisita.Value.Date).Days
                : int.MaxValue;

            if (!_cliente.Ativo)
            {
                StatusBorder.Background = App.Current.FindResource("DangerBrush") as System.Windows.Media.Brush;
                StatusTextBlock.Text = "Cadastro inativo";
                StatusResumoTextBlock.Text = "Cliente mantido apenas para consulta e historico.";
                return;
            }

            if (_cliente.ClienteVip)
            {
                StatusBorder.Background = App.Current.FindResource("WarningBrush") as System.Windows.Media.Brush;
                StatusTextBlock.Text = "VIP em acompanhamento";
                StatusResumoTextBlock.Text = "Relacionamento prioritario e atendimento acelerado.";
                return;
            }

            StatusBorder.Background = App.Current.FindResource(diasSemVisita > 90 ? "DangerBrush" : "SuccessBrush") as System.Windows.Media.Brush;
            StatusTextBlock.Text = diasSemVisita > 90 ? "Sem retorno recente" : "Relacionamento ativo";
            StatusResumoTextBlock.Text = diasSemVisita > 90
                ? "Vale programar contato comercial para reativacao."
                : "Cadastro atualizado e pronto para novos atendimentos.";
        }

        private void AplicarInsights()
        {
            var diasSemVisita = _cliente.UltimaVisita.HasValue
                ? (DateTime.Today - _cliente.UltimaVisita.Value.Date).Days
                : 0;
            var ticketMedio = _cliente.TotalServicos > 0 ? _cliente.TotalGasto / _cliente.TotalServicos : 0;

            InsightPrimarioTextBlock.Text = _cliente.Veiculos.Count == 0
                ? "Cliente sem frota vinculada. Vale completar o cadastro tecnico no proximo atendimento."
                : $"Frota vinculada: {_cliente.Veiculos.Count} veiculo(s) pronta para abrir OS e orcamentos.";
            InsightSecundarioTextBlock.Text = ticketMedio <= 0
                ? "Ainda sem historico financeiro consolidado."
                : $"Ticket medio aproximado de {_cliente.TotalGasto / Math.Max(1, _cliente.TotalServicos):C}.";
            InsightTerciarioTextBlock.Text = _cliente.UltimaVisita.HasValue
                ? $"Ultima movimentacao registrada ha {diasSemVisita} dia(s)."
                : "Nenhuma visita registrada ate o momento.";
        }

        private List<TimelineEventoViewModel> CriarTimeline()
        {
            var timeline = new List<TimelineEventoViewModel>
            {
                new()
                {
                    Titulo = "Cadastro criado",
                    Subtitulo = _cliente.DataCadastro.ToString("dd/MM/yyyy"),
                    Descricao = "Cliente registrado no sistema com dados iniciais de relacionamento.",
                    Data = _cliente.DataCadastro
                }
            };

            if (_cliente.UltimaVisita.HasValue)
            {
                timeline.Add(new TimelineEventoViewModel
                {
                    Titulo = "Ultima visita",
                    Subtitulo = "Presenca operacional mais recente",
                    Descricao = "A oficina registrou atendimento ou entrega vinculada a este cliente.",
                    Data = _cliente.UltimaVisita.Value
                });
            }

            timeline.AddRange(_cliente.HistoricoServicos
                .OrderByDescending(servico => servico.DataServico)
                .Take(6)
                .Select(servico => new TimelineEventoViewModel
                {
                    Titulo = servico.Descricao,
                    Subtitulo = servico.Valor > 0 ? servico.Valor.ToString("C") : "Servico concluido",
                    Descricao = string.IsNullOrWhiteSpace(servico.Observacoes)
                        ? "Historico de servico consolidado no cadastro do cliente."
                        : servico.Observacoes,
                    Data = servico.DataServico
                }));

            if (timeline.Count == 1)
            {
                timeline.Add(new TimelineEventoViewModel
                {
                    Titulo = "Sem servicos registrados",
                    Subtitulo = "Acompanhamento inicial",
                    Descricao = "Este cadastro ainda nao possui movimentacoes de servico consolidadas.",
                    Data = _cliente.DataCadastro
                });
            }

            return timeline
                .OrderByDescending(item => item.Data)
                .ToList();
        }

        private void AbrirArquivoSeExistir(string? caminho, string descricao)
        {
            if (string.IsNullOrWhiteSpace(caminho) || !File.Exists(caminho))
            {
                WindowInteractionHelper.ShowMessage(
                    $"Nenhuma {descricao} vinculada a este cliente.",
                    "Arquivo ausente",
                    MessageBoxImage.Information,
                    "Clientes");
                return;
            }

            if (App.IsAutomatedTestMode)
            {
                WindowInteractionHelper.LogAutomationExternalAction(
                    $"Abertura de {descricao} validada em automacao sem executar aplicativo externo.",
                    "Clientes");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = caminho,
                UseShellExecute = true
            });
        }

        private static string ValorOuPadrao(string? valor, string padrao = "-")
        {
            return string.IsNullOrWhiteSpace(valor) ? padrao : valor.Trim();
        }

        private static void ValidarJanelaEmAutomacao(Window janela)
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
            }
            finally
            {
                janela.Close();
            }
        }

        private sealed class TimelineEventoViewModel
        {
            public string Titulo { get; init; } = string.Empty;
            public string Subtitulo { get; init; } = string.Empty;
            public string Descricao { get; init; } = string.Empty;
            public DateTime Data { get; init; }
        }
    }
}
