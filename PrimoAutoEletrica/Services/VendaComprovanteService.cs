using PrimoAutoEletrica.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PrimoAutoEletrica.Services
{
    public sealed class VendaComprovanteService
    {
        private readonly BusinessConfiguration _configuration;

        public VendaComprovanteService()
            : this(BusinessConfigurationService.LoadOrCreateDefault(global::PrimoAutoEletrica.App.RuntimeAppDataPath, global::PrimoAutoEletrica.App.Logger))
        {
        }

        public VendaComprovanteService(BusinessConfiguration configuration)
        {
            _configuration = configuration ?? new BusinessConfiguration();
        }

        public FlowDocument CriarDocumento(Venda venda)
        {
            ArgumentNullException.ThrowIfNull(venda);

            var documento = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                Foreground = Brushes.Black
            };

            TryAddLogo(documento);

            documento.Blocks.Add(new Paragraph(new Run(_configuration.EffectiveCompanyName))
            {
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4)
            });

            var dadosEmpresa = BuildBusinessDetails();
            if (!string.IsNullOrWhiteSpace(dadosEmpresa))
            {
                documento.Blocks.Add(new Paragraph(new Run(dadosEmpresa))
                {
                    Foreground = Brushes.DimGray,
                    Margin = new Thickness(0, 0, 0, 8)
                });
            }

            if (!string.IsNullOrWhiteSpace(_configuration.ReceiptHeader))
            {
                documento.Blocks.Add(new Paragraph(new Run(_configuration.ReceiptHeader))
                {
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 10)
                });
            }

            documento.Blocks.Add(new Paragraph(new Run(
                $"Comprovante de venda | {venda.Data:dd/MM/yyyy HH:mm}\nVenda: {venda.Id}\nOperador: {venda.Usuario}\nCliente: {venda.Cliente?.Nome ?? "Consumidor final"}"))
            {
                Foreground = Brushes.DimGray,
                Margin = new Thickness(0, 0, 0, 18)
            });

            foreach (var item in venda.Itens)
            {
                documento.Blocks.Add(new Paragraph(new Run(
                    $"{item.NomeExibicao} | Qtd {item.Quantidade} | Unit {item.PrecoUnitario:C} | Desc {item.Desconto:C} | Subtotal {item.Subtotal:C}"))
                {
                    Margin = new Thickness(0, 0, 0, 6)
                });
            }

            documento.Blocks.Add(new Paragraph(new Run(
                $"Forma de pagamento: {venda.FormaPagamento}\nDesconto geral: {venda.Desconto:C}\nTotal: {venda.Total:C}\nStatus: {venda.Status}"))
            {
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 16, 0, 0)
            });

            if (!string.IsNullOrWhiteSpace(_configuration.ReceiptFooter))
            {
                documento.Blocks.Add(new Paragraph(new Run(_configuration.ReceiptFooter))
                {
                    Foreground = Brushes.DimGray,
                    Margin = new Thickness(0, 16, 0, 0)
                });
            }

            return documento;
        }

        public string CriarResumoOperacional(Venda venda)
        {
            ArgumentNullException.ThrowIfNull(venda);

            var resumo = new StringBuilder();
            resumo.AppendLine($"Venda: {venda.Id}");
            resumo.AppendLine($"Data: {venda.Data:dd/MM/yyyy HH:mm}");
            resumo.AppendLine($"Cliente: {venda.Cliente?.Nome ?? "Consumidor final"}");
            resumo.AppendLine($"Forma: {venda.FormaPagamento}");
            resumo.AppendLine($"Status: {venda.Status}");
            resumo.AppendLine($"Itens: {venda.Itens.Sum(item => item.Quantidade)}");
            resumo.AppendLine($"Total: {venda.Total:C}");

            if (!string.IsNullOrWhiteSpace(venda.MotivoCancelamento))
            {
                resumo.AppendLine($"Motivo cancelamento: {venda.MotivoCancelamento}");
            }

            return resumo.ToString().Trim();
        }

        private string BuildBusinessDetails()
        {
            var builder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(_configuration.CompanyLegalName) &&
                !string.Equals(_configuration.CompanyLegalName, _configuration.CompanyDisplayName, StringComparison.OrdinalIgnoreCase))
            {
                builder.AppendLine(_configuration.CompanyLegalName);
            }

            if (!string.IsNullOrWhiteSpace(_configuration.CompanyDocument))
            {
                builder.AppendLine($"CNPJ/CPF: {_configuration.CompanyDocument}");
            }

            if (!string.IsNullOrWhiteSpace(_configuration.CompanyPhone))
            {
                builder.AppendLine($"Telefone: {_configuration.CompanyPhone}");
            }

            if (!string.IsNullOrWhiteSpace(_configuration.CompanyAddress))
            {
                builder.AppendLine(_configuration.CompanyAddress);
            }

            return builder.ToString().Trim();
        }

        private void TryAddLogo(FlowDocument documento)
        {
            var validation = BusinessConfigurationService.ValidateLogoPath(_configuration.LogoPath);
            if (!validation.IsValid || string.IsNullOrWhiteSpace(_configuration.LogoPath))
            {
                return;
            }

            try
            {
                var image = new Image
                {
                    Source = new BitmapImage(new Uri(Path.GetFullPath(_configuration.LogoPath), UriKind.Absolute)),
                    Width = 160,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                documento.Blocks.Add(new BlockUIContainer(image)
                {
                    Margin = new Thickness(0, 0, 0, 8)
                });
            }
            catch
            {
                // A falha de logo nao deve impedir a reimpressao do comprovante.
            }
        }
    }
}
