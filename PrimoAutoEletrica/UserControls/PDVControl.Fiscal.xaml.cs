using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.Fiscal;

namespace PrimoAutoEletrica.UserControls
{
    public partial class PDVControl
    {
        private async void EmitirNFeHomologacaoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("PDV_FINALIZAR", "Voce nao possui permissao para operacoes fiscais do PDV."))
            {
                return;
            }

            var confirm = ExibirMensagem(
                "Esta operacao enviara uma NF-e para o ambiente de HOMOLOGACAO.\n\n" +
                "Nao e emissao de PRODUCAO.\n\n" +
                "Deseja continuar?",
                "NF-e Homologacao",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var venda = SelecionarVendaOperacional(
                titulo: "Emitir NF-e (Homologacao)",
                subtitulo: "Escolha uma venda concluida com cliente e produtos fiscais configurados.",
                textoConfirmacao: "Emitir NF-e Homologacao",
                incluirCanceladas: false,
                filtro: item => !string.Equals(item.Status, "Cancelada", StringComparison.OrdinalIgnoreCase));

            if (venda == null)
            {
                return;
            }

            try
            {
                EmitirNFeHomologacaoButton.IsEnabled = false;
                var homolog = App.Services.GetRequiredService<NFeHomologationService>();
                var registry = App.Services.GetRequiredService<RepositoryRegistry>();

                Cliente? cliente = venda.Cliente;
                if (cliente == null)
                {
                    ExibirMensagem("Venda sem cliente vinculado. NF-e bloqueada.", "NF-e Homologacao", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(cliente.Rua) || string.IsNullOrWhiteSpace(cliente.CPF))
                {
                    var loaded = registry.Clientes.ObterPorId(cliente.Id);
                    if (loaded != null)
                    {
                        cliente = loaded;
                    }
                }

                var produtos = new Dictionary<Guid, Produto>();
                foreach (var item in venda.Itens.Where(i => i.ProdutoId.HasValue))
                {
                    var id = item.ProdutoId!.Value;
                    if (produtos.ContainsKey(id)) continue;
                    var produto = item.Produto ?? registry.Produtos.ObterPorId(id);
                    if (produto != null)
                    {
                        produtos[id] = produto;
                    }
                }

                var validation = homolog.ValidateVenda(venda, cliente, produtos, requireProviderCredential: true);
                if (!validation.IsValid)
                {
                    ExibirMensagem(
                        "NF-e bloqueada na validacao local:\n\n" + validation.Summarize(),
                        "NF-e Homologacao",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var center = App.Services.GetRequiredService<FiscalOperationsCenterService>();
                var preview = center.BuildPreviewFromVenda(venda, cliente, produtos, requireProviderCredential: false);
                var previewConfirm = ExibirMensagem(
                    "Pre-visualizacao fiscal (NAO e DANFE):\n\n" +
                    TruncateUi(preview.TextoCompleto, 1800) +
                    "\n\nEnviar para HOMOLOGACAO agora?",
                    "Pre-visualizacao NF-e Homologacao",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);
                if (previewConfirm != MessageBoxResult.Yes)
                {
                    return;
                }

                var result = await homolog.EmitirHomologacaoFromVendaAsync(venda, cliente, produtos).ConfigureAwait(true);
                ExibirMensagem(FormatFiscalUiMessage(result), "NF-e Homologacao", MessageBoxButton.OK, MapIcon(result));
            }
            catch (Exception ex)
            {
                App.Logger.LogWarning($"NF-e Homologacao falhou: {ex.Message}", "PDV");
                ExibirMensagem(
                    "Nao foi possivel concluir a operacao fiscal.\n\nCodigo interno: FISCAL-UI-ERROR",
                    "NF-e Homologacao",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                EmitirNFeHomologacaoButton.IsEnabled = true;
            }
        }

        private static string TruncateUi(string value, int max)
            => string.IsNullOrEmpty(value) ? string.Empty : (value.Length <= max ? value : value[..max] + "\n…");

        private static string FormatFiscalUiMessage(FiscalProviderResult result)
        {
            if (result.Status == FiscalDocumentStatus.Authorized)
            {
                return $"Autorizada (Homologacao)\n" +
                       $"Operacao: {result.FiscalOperationId:N}\n" +
                       $"Chave: {result.ChaveAcesso ?? "(nao informada)"}\n" +
                       $"Protocolo: {result.Protocolo ?? "(nao informado)"}";
            }

            if (result.Status == FiscalDocumentStatus.Rejected)
            {
                return $"Rejeitada\nCodigo: {result.ProviderCode ?? result.InternalCode}\nMotivo: {result.ProviderMessage ?? result.Message}";
            }

            if (result.ErrorKind == FiscalErrorKind.Timeout || result.Status == FiscalDocumentStatus.Unknown)
            {
                return "Resultado ainda nao confirmado.\nConsulte novamente antes de tentar uma nova emissao.\n\n" +
                       FiscalUserMessages.For(result);
            }

            return FiscalUserMessages.For(result);
        }

        private static MessageBoxImage MapIcon(FiscalProviderResult result) => result.Status switch
        {
            FiscalDocumentStatus.Authorized => MessageBoxImage.Information,
            FiscalDocumentStatus.Rejected => MessageBoxImage.Warning,
            FiscalDocumentStatus.ProductionBlocked => MessageBoxImage.Stop,
            _ => MessageBoxImage.Warning
        };
    }
}
