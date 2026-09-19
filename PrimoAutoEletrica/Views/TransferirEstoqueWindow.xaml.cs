using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;
using System.Windows;

namespace PrimoAutoEletrica.Views
{
    public partial class TransferirEstoqueWindow : Window
    {
        private readonly Produto _produto;
        private readonly DatabaseService _databaseService = new();
        private readonly LoggerService _logger;

        public TransferirEstoqueWindow(Produto produto)
        {
            InitializeComponent();
            _produto = produto ?? throw new ArgumentNullException(nameof(produto));
            _logger = App.Logger;
            TituloText.Text = $"Transferir — {_produto.Codigo}";
            SubtituloText.Text = $"{_produto.Nome}\nQtd atual: {_produto.QuantidadeEstoque} | Local atual: {_produto.Localizacao} / {_produto.Prateleira} / {_produto.Gaveta}";
            LocalizacaoTextBox.Text = _produto.Localizacao ?? string.Empty;
            PrateleiraTextBox.Text = _produto.Prateleira ?? string.Empty;
            GavetaTextBox.Text = _produto.Gaveta ?? string.Empty;
            MotivoTextBox.Text = "Reorganizacao de localizacao na oficina";
        }

        private void ConfirmarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var loc = (LocalizacaoTextBox.Text ?? string.Empty).Trim();
                var prat = (PrateleiraTextBox.Text ?? string.Empty).Trim();
                var gav = (GavetaTextBox.Text ?? string.Empty).Trim();
                var motivo = (MotivoTextBox.Text ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(motivo))
                {
                    throw new InvalidOperationException("Informe o motivo da transferencia.");
                }

                var anterior = $"{_produto.Localizacao}/{_produto.Prateleira}/{_produto.Gaveta}";
                var novo = $"{loc}/{prat}/{gav}";
                if (string.Equals(anterior, novo, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Informe uma localizacao diferente da atual.");
                }

                using var connection = _databaseService.GetConnection();
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
UPDATE Produtos
SET Localizacao=@Localizacao, Prateleira=@Prateleira, Gaveta=@Gaveta,
    DataUltimaAtualizacao=@Agora, DataUltimaAlteracao=@Agora,
    Observacoes = CASE WHEN COALESCE(Observacoes,'')='' THEN @Obs ELSE Observacoes || @Sep || @Obs END,
    RowVersion = COALESCE(RowVersion,0)+1
WHERE Id=@Id;";
                var agora = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.AddWithValue("@Localizacao", loc);
                cmd.Parameters.AddWithValue("@Prateleira", prat);
                cmd.Parameters.AddWithValue("@Gaveta", gav);
                cmd.Parameters.AddWithValue("@Agora", agora);
                cmd.Parameters.AddWithValue("@Obs", $"Transferencia {agora}: {anterior} -> {novo} | {motivo}");
                cmd.Parameters.AddWithValue("@Sep", " | ");
                cmd.Parameters.AddWithValue("@Id", _produto.Id.ToString());
                if (cmd.ExecuteNonQuery() <= 0)
                {
                    throw new InvalidOperationException("Produto nao encontrado para transferir.");
                }

                _produto.Localizacao = loc;
                _produto.Prateleira = prat;
                _produto.Gaveta = gav;
                _logger.LogInfo($"Transferencia estoque {_produto.Codigo}: {anterior} -> {novo}");
                App.Audit.RegistrarAcaoCritica("Estoque", "TransferirLocalizacao", "Produto", _produto.Id.ToString(),
                    $"Codigo={_produto.Codigo}; De={anterior}; Para={novo}; Motivo={motivo}");
                WindowInteractionHelper.CloseWithDialogResult(this, true, "Estoque");
            }
            catch (Exception ex)
            {
                WindowInteractionHelper.ShowMessage($"Falha ao transferir:\n{ex.Message}", "Transferir estoque", MessageBoxImage.Error, "Estoque", ex);
            }
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            WindowInteractionHelper.CloseWithDialogResult(this, false, "Estoque");
        }
    }
}
