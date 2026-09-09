using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PrimoAutoEletrica.Services.Fiscal;

namespace PrimoAutoEletrica.UserControls
{
    public partial class FiscalOperationsControl : UserControl
    {
        private readonly FiscalOperationsCenterService _center;
        private bool _busy;

        public FiscalOperationsControl()
        {
            InitializeComponent();
            _center = App.Services.GetRequiredService<FiscalOperationsCenterService>();
            Loaded += (_, _) => RefreshAll();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e) => RefreshAll();

        private void RefreshAll()
        {
            try
            {
                LoadIssuer();
                var health = _center.GetHealth(requireLiveCredential: true);
                HealthSummaryText.Text = health.ReadyForHomologEmission
                    ? $"Status: READY — {health.Summary}"
                    : $"Status: {health.Status} — {health.Summary}";

                HealthDetailsText.Text =
                    $"Emitente: {(health.EmitenteOk ? "OK" : "PENDENTE")} · " +
                    $"Ambiente Homolog: {(health.AmbienteHomologacaoOk ? "OK" : "FALHA")} · " +
                    $"Provider: {(health.ProviderOk ? "OK" : "FALHA")} · " +
                    $"Credencial: {(health.CredentialOk ? "OK" : "AUSENTE")} · " +
                    $"Live HTTP: {(health.LiveHttpEnabled ? "ON" : "OFF")}\n" +
                    string.Join("\n", System.Linq.Enumerable.Select(health.Issues, i => $"• [{i.Code}] {i.Message}"));

                PendingProductsGrid.ItemsSource = health.PendingProducts;
                HistoryGrid.ItemsSource = _center.ListHistory(80);
                UpdateSelectionUi();
            }
            catch (Exception ex)
            {
                HealthSummaryText.Text = "Erro ao avaliar saúde fiscal.";
                HealthDetailsText.Text = ex.Message;
            }
        }

        private void LoadIssuer()
        {
            var cfg = _center.LoadConfiguration();
            var i = cfg.Issuer;
            CnpjTextBox.Text = i.Cnpj;
            IeTextBox.Text = i.InscricaoEstadual;
            RazaoTextBox.Text = i.RazaoSocial;
            FantasiaTextBox.Text = i.NomeFantasia;
            CrtTextBox.Text = i.RegimeTributario;
            SerieTextBox.Text = i.SerieNFe;
            LogradouroTextBox.Text = i.Logradouro;
            NumeroTextBox.Text = i.Numero;
            ComplementoTextBox.Text = i.Complemento;
            BairroTextBox.Text = i.Bairro;
            MunicipioTextBox.Text = i.Municipio;
            IbgeTextBox.Text = i.CodigoMunicipioIbge;
            UfTextBox.Text = i.Uf;
            CepTextBox.Text = i.Cep;
            CsosnTextBox.Text = i.DefaultIcmsSituacaoTributaria;
            OrigemTextBox.Text = i.DefaultIcmsOrigem;
        }

        private void SaveIssuerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var cfg = _center.LoadConfiguration();
                cfg.Issuer.Cnpj = CnpjTextBox.Text.Trim();
                cfg.Issuer.InscricaoEstadual = IeTextBox.Text.Trim();
                cfg.Issuer.RazaoSocial = RazaoTextBox.Text.Trim();
                cfg.Issuer.NomeFantasia = FantasiaTextBox.Text.Trim();
                cfg.Issuer.RegimeTributario = CrtTextBox.Text.Trim();
                cfg.Issuer.SerieNFe = SerieTextBox.Text.Trim();
                cfg.Issuer.Logradouro = LogradouroTextBox.Text.Trim();
                cfg.Issuer.Numero = NumeroTextBox.Text.Trim();
                cfg.Issuer.Complemento = ComplementoTextBox.Text.Trim();
                cfg.Issuer.Bairro = BairroTextBox.Text.Trim();
                cfg.Issuer.Municipio = MunicipioTextBox.Text.Trim();
                cfg.Issuer.CodigoMunicipioIbge = IbgeTextBox.Text.Trim();
                cfg.Issuer.Uf = UfTextBox.Text.Trim().ToUpperInvariant();
                cfg.Issuer.Cep = CepTextBox.Text.Trim();
                cfg.Issuer.DefaultIcmsSituacaoTributaria = CsosnTextBox.Text.Trim();
                cfg.Issuer.DefaultIcmsOrigem = OrigemTextBox.Text.Trim();
                cfg.Environment = FiscalEnvironment.Homologation;
                cfg.ProductionUnlocked = false;
                _center.SaveConfiguration(cfg);
                MessageBox.Show(
                    "Configuração do emitente salva. Ambiente permanece em Homologação. Produção bloqueada.",
                    "Operações Fiscais",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                RefreshAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha ao salvar: {ex.Message}", "Operações Fiscais", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HistoryGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => UpdateSelectionUi();

        private void UpdateSelectionUi()
        {
            if (HistoryGrid.SelectedItem is not FiscalOperationListItem item)
            {
                ConsultarButton.IsEnabled = false;
                CancelarButton.IsEnabled = false;
                DetailTextBox.Text = string.Empty;
                return;
            }

            var detail = _center.GetDetail(item.OperationId);
            if (detail == null)
            {
                return;
            }

            ConsultarButton.IsEnabled = detail.CanConsult && !_busy;
            CancelarButton.IsEnabled = detail.CanCancel && !_busy;
            var doc = detail.Document;
            DetailTextBox.Text =
                $"Id: {detail.Operation.Id:N}\n" +
                $"Status: {detail.Operation.Status}\n" +
                $"Provider: {detail.Operation.Provider} | Env: {detail.Operation.Environment}\n" +
                $"Key: {detail.Operation.IdempotencyKey}\n" +
                $"Chave: {doc?.ChaveAcesso}\n" +
                $"Protocolo: {doc?.Protocolo}\n" +
                $"Número/Série: {doc?.Numero}/{doc?.Serie}\n" +
                $"Erro: {detail.Operation.LastErrorMessage}\n\n" +
                "Timeline:\n" +
                string.Join("\n", detail.Timeline);
        }

        private async void ConsultarButton_Click(object sender, RoutedEventArgs e)
        {
            if (HistoryGrid.SelectedItem is not FiscalOperationListItem item || _busy)
            {
                return;
            }

            await RunBusyAsync(async () =>
            {
                var result = await _center.ConsultarAsync(item.OperationId).ConfigureAwait(true);
                MessageBox.Show(
                    FiscalUserMessages.For(result),
                    "Consulta fiscal",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                RefreshAll();
            });
        }

        private async void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            if (HistoryGrid.SelectedItem is not FiscalOperationListItem item || _busy)
            {
                return;
            }

            var confirm = MessageBox.Show(
                "Cancelar NF-e AUTHORIZED apenas em HOMOLOGAÇÃO?\n\nJustificativa padrão de teste será usada (mín. 15 caracteres).",
                "Cancelamento Homologação",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            const string justificativa = "Cancelamento homologacao operacional PRIMOX";
            await RunBusyAsync(async () =>
            {
                var result = await _center.CancelarHomologacaoAsync(item.OperationId, justificativa).ConfigureAwait(true);
                MessageBox.Show(
                    FiscalUserMessages.For(result),
                    "Cancelamento fiscal",
                    MessageBoxButton.OK,
                    result.Status == FiscalDocumentStatus.Cancelled ? MessageBoxImage.Information : MessageBoxImage.Warning);
                RefreshAll();
            });
        }

        private async Task RunBusyAsync(Func<Task> action)
        {
            _busy = true;
            ConsultarButton.IsEnabled = false;
            CancelarButton.IsEnabled = false;
            try
            {
                await action().ConfigureAwait(true);
            }
            finally
            {
                _busy = false;
                UpdateSelectionUi();
            }
        }
    }
}
