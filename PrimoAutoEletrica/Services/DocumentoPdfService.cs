using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace PrimoAutoEletrica.Services
{
    public sealed class DocumentoPdfService
    {
        private const double Margin = 38d;
        private const double FooterMargin = 34d;
        private const double LineHeight = 14d;
        private readonly BusinessConfiguration _configuration;

        public DocumentoPdfService()
            : this(BusinessConfigurationService.LoadOrCreateDefault(App.RuntimeAppDataPath, App.Logger))
        {
        }

        public DocumentoPdfService(BusinessConfiguration configuration)
        {
            _configuration = configuration ?? new BusinessConfiguration();
        }

        public string GerarOrcamento(Orcamento orcamento, string caminhoArquivo)
        {
            ArgumentNullException.ThrowIfNull(orcamento);
            var cliente = orcamento.Cliente ?? (orcamento.ClienteId.HasValue ? App.Repositories.Clientes.ObterPorId(orcamento.ClienteId.Value) : null);
            var veiculo = orcamento.Veiculo ?? ResolverVeiculo(orcamento.VeiculoId, cliente);

            return GerarDocumento(
                "ORCAMENTO",
                caminhoArquivo,
                new[]
                {
                    Secao("Dados do cliente", DadosCliente(cliente)),
                    Secao("Dados do veiculo", DadosVeiculo(veiculo)),
                    Secao("Resumo comercial", new[]
                    {
                        Linha("Numero", orcamento.Numero),
                        Linha("Status", orcamento.Status),
                        Linha("Criacao", orcamento.DataCriacao.ToString("dd/MM/yyyy HH:mm")),
                        Linha("Validade", orcamento.DataValidade?.ToString("dd/MM/yyyy") ?? "Nao definida")
                    }),
                    Secao("Itens", orcamento.Itens.Select(item => $"{item.Quantidade}x {item.ProdutoNome} | {item.TipoDescricao} | {item.PrecoUnitario:C} | {item.Subtotal:C}")),
                    Secao("Valores", new[]
                    {
                        Linha("Subtotal", orcamento.Subtotal.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))),
                        Linha("Desconto", orcamento.Desconto.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))),
                        Linha("Acrescimo", orcamento.Acrescimo.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))),
                        Linha("Total", orcamento.Total.ToString("C", CultureInfo.GetCultureInfo("pt-BR")))
                    }),
                    Secao("Diagnostico e observacoes", new[] { ValorOuPadrao(orcamento.Diagnostico, "Sem diagnostico."), ValorOuPadrao(orcamento.Observacoes, "Sem observacoes.") })
                },
                incluirAssinatura: false);
        }

        public string GerarOrdemServico(OrdemServico ordem, string caminhoArquivo)
        {
            ArgumentNullException.ThrowIfNull(ordem);
            var cliente = App.Repositories.Clientes.ObterPorId(ordem.ClienteId);
            var veiculo = ResolverVeiculo(ordem.VeiculoId, cliente);

            return GerarDocumento(
                "ORDEM DE SERVICO",
                caminhoArquivo,
                new[]
                {
                    Secao("Dados do cliente", DadosCliente(cliente, ordem.ClienteNomeSnapshot, ordem.TelefoneClienteSnapshot)),
                    Secao("Dados do veiculo", DadosVeiculo(veiculo, ordem.VeiculoDescricaoSnapshot, ordem.PlacaSnapshot)),
                    Secao("Execucao", new[]
                    {
                        Linha("Numero", ordem.Numero),
                        Linha("Status", ordem.Status),
                        Linha("Prioridade", ordem.Prioridade),
                        Linha("Abertura", ordem.DataAbertura.ToString("dd/MM/yyyy HH:mm")),
                        Linha("Previsao", ordem.DataPrevisao?.ToString("dd/MM/yyyy") ?? "Nao definida"),
                        Linha("Tecnico", ordem.TecnicoId?.ToString(CultureInfo.InvariantCulture) ?? "Nao informado")
                    }),
                    Secao("Defeito e diagnostico", new[]
                    {
                        ValorOuPadrao(ordem.ProblemaRelatado, "Sem defeito relatado."),
                        ValorOuPadrao(PrimeiroValor(ordem.Diagnostico, ordem.DiagnosticoInicial, ordem.DiagnosticoFinal), "Sem diagnostico.")
                    }),
                    Secao("Itens", ordem.Itens.Select(item => $"{item.Quantidade:N2}x {item.Descricao} | {item.Tipo} | {item.ValorUnitario:C} | {item.Total:C}")),
                    Secao("Valores", new[]
                    {
                        Linha("Mao de obra", ordem.ValorMaoObra.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))),
                        Linha("Desconto", ordem.Desconto.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))),
                        Linha("Total estimado", (ordem.Itens.Sum(item => item.Total) + ordem.ValorMaoObra - ordem.Desconto).ToString("C", CultureInfo.GetCultureInfo("pt-BR")))
                    }),
                    Secao("Observacoes", new[] { ValorOuPadrao(ordem.ObservacoesCliente, "Sem observacoes ao cliente."), ValorOuPadrao(ordem.ObservacoesInternas, "Sem observacoes internas.") })
                },
                incluirAssinatura: true);
        }

        public string GerarChecklist(OrdemServico ordem, string caminhoArquivo)
        {
            ArgumentNullException.ThrowIfNull(ordem);
            return GerarDocumento(
                "CHECKLIST DE OFICINA",
                caminhoArquivo,
                new[]
                {
                    Secao("OS", new[] { Linha("Numero", ordem.Numero), Linha("Status", ordem.Status), Linha("Veiculo", ordem.VeiculoDescricaoSnapshot), Linha("Placa", ordem.PlacaSnapshot) }),
                    Secao("Entrada", SepararLinhas(PrimeiroValor(ordem.ChecklistEntrada, "Sem checklist de entrada registrado."))),
                    Secao("Saida/entrega", SepararLinhas(PrimeiroValor(ordem.ChecklistSaida, ordem.ChecklistEntrega, "Sem checklist de saida registrado."))),
                    Secao("Fotos", new[] { Linha("Antes", ValorOuPadrao(ordem.FotosAntes, "Sem fotos antes.")), Linha("Depois", ValorOuPadrao(ordem.FotosDepois, "Sem fotos depois.")) })
                },
                incluirAssinatura: true);
        }

        public string GerarRecibo(string numero, Cliente? cliente, decimal valor, string referente, string formaPagamento, string caminhoArquivo)
        {
            return GerarDocumento(
                "RECIBO",
                caminhoArquivo,
                new[]
                {
                    Secao("Recebemos de", DadosCliente(cliente)),
                    Secao("Pagamento", new[]
                    {
                        Linha("Numero", ValorOuPadrao(numero, Guid.NewGuid().ToString("N")[..8].ToUpperInvariant())),
                        Linha("Valor", valor.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))),
                        Linha("Forma", ValorOuPadrao(formaPagamento, "Nao informada")),
                        Linha("Referente", ValorOuPadrao(referente, "Servicos/produtos automotivos")),
                        Linha("Data", DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                    })
                },
                incluirAssinatura: true);
        }

        public string GerarComprovanteVenda(Venda venda, string caminhoArquivo)
        {
            ArgumentNullException.ThrowIfNull(venda);
            return GerarDocumento(
                "COMPROVANTE DE VENDA",
                caminhoArquivo,
                new[]
                {
                    Secao("Cliente", DadosCliente(venda.Cliente)),
                    Secao("Venda", new[]
                    {
                        Linha("Id", venda.Id.ToString()),
                        Linha("Data", venda.Data.ToString("dd/MM/yyyy HH:mm")),
                        Linha("Operador", venda.Usuario),
                        Linha("Forma", venda.FormaPagamento),
                        Linha("Status", venda.Status)
                    }),
                    Secao("Itens", venda.Itens.Select(item => $"{item.Quantidade}x {item.NomeExibicao} | {item.PrecoUnitario:C} | {item.Subtotal:C}")),
                    Secao("Valores", new[] { Linha("Desconto", venda.Desconto.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))), Linha("Total", venda.Total.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))) })
                },
                incluirAssinatura: false);
        }

        public string GerarTermoGarantia(OrdemServico ordem, string caminhoArquivo)
        {
            ArgumentNullException.ThrowIfNull(ordem);
            return GerarDocumento(
                "TERMO DE GARANTIA",
                caminhoArquivo,
                new[]
                {
                    Secao("Identificacao", new[] { Linha("OS", ordem.Numero), Linha("Cliente", ordem.ClienteNomeSnapshot), Linha("Veiculo", ordem.VeiculoDescricaoSnapshot), Linha("Placa", ordem.PlacaSnapshot) }),
                    Secao("Garantia", new[]
                    {
                        Linha("Validade", ordem.GarantiaValidaAte?.ToString("dd/MM/yyyy") ?? "Nao definida"),
                        ValorOuPadrao(ordem.GarantiaObservacoes, "Garantia conforme servicos e pecas aprovados, exceto mau uso, instalacao externa e danos por terceiros.")
                    }),
                    Secao("Itens cobertos", ordem.Itens.Select(item => $"{item.Descricao} | {item.Tipo}"))
                },
                incluirAssinatura: true);
        }

        public string GerarTermoAutorizacao(OrdemServico ordem, string caminhoArquivo)
        {
            ArgumentNullException.ThrowIfNull(ordem);
            return GerarDocumento(
                "TERMO DE AUTORIZACAO",
                caminhoArquivo,
                new[]
                {
                    Secao("Identificacao", new[] { Linha("OS", ordem.Numero), Linha("Cliente", ordem.ClienteNomeSnapshot), Linha("Veiculo", ordem.VeiculoDescricaoSnapshot), Linha("Placa", ordem.PlacaSnapshot) }),
                    Secao("Autorizacao", new[]
                    {
                        ValorOuPadrao(ordem.TermoAutorizacao, "Cliente autoriza diagnostico, desmontagens necessarias, testes eletricos e execucao dos servicos aprovados."),
                        Linha("Aprovada pelo cliente", ordem.AprovadaCliente ? "Sim" : "Nao"),
                        Linha("Metodo de aprovacao", ValorOuPadrao(ordem.MetodoAprovacao, "Assinatura/confirmacao manual"))
                    })
                },
                incluirAssinatura: true);
        }

        public string GerarRelatorioFinanceiro(IEnumerable<DadoFinanceiro> dados, string caminhoArquivo, string titulo = "Relatorio financeiro")
        {
            var lista = (dados ?? Enumerable.Empty<DadoFinanceiro>()).ToList();
            return GerarDocumento(
                "RELATORIO FINANCEIRO",
                caminhoArquivo,
                new[]
                {
                    Secao("Resumo", new[]
                    {
                        Linha("Titulo", titulo),
                        Linha("Total de registros", lista.Count.ToString(CultureInfo.InvariantCulture)),
                        Linha("Receitas", lista.Where(item => item.Tipo.Contains("Receita", StringComparison.OrdinalIgnoreCase)).Sum(item => item.Valor).ToString("C", CultureInfo.GetCultureInfo("pt-BR"))),
                        Linha("Despesas", lista.Where(item => item.Tipo.Contains("Despesa", StringComparison.OrdinalIgnoreCase)).Sum(item => item.Valor).ToString("C", CultureInfo.GetCultureInfo("pt-BR"))),
                        Linha("Periodo gerado", DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                    }),
                    Secao("Lancamentos", lista.OrderBy(item => item.Data).Select(item => $"{item.Data:dd/MM/yyyy} | {item.Tipo} | {item.Categoria} | {item.Descricao} | {item.Valor:C}"))
                },
                incluirAssinatura: false);
        }

        private string GerarDocumento(string titulo, string caminhoArquivo, IEnumerable<DocumentoSecao> secoes, bool incluirAssinatura)
        {
            if (string.IsNullOrWhiteSpace(caminhoArquivo))
            {
                throw new ArgumentException("Caminho do PDF nao informado.", nameof(caminhoArquivo));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(caminhoArquivo) ?? ".");

            using var document = new PdfDocument();
            document.Info.Title = $"{titulo} - {_configuration.EffectiveCompanyName}";
            document.Info.Author = _configuration.EffectiveCompanyName;
            document.Info.Subject = titulo;

            var page = document.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;
            var gfx = XGraphics.FromPdfPage(page);
            var state = new PdfDrawState(document, page, gfx);

            DrawHeader(state, titulo);
            foreach (var secao in secoes)
            {
                DrawSection(state, secao);
            }

            if (incluirAssinatura)
            {
                DrawSignature(state);
            }

            DrawFooter(state);
            state.Graphics.Dispose();
            document.Save(caminhoArquivo);
            return caminhoArquivo;
        }

        private void DrawHeader(PdfDrawState state, string titulo)
        {
            var dark = XColor.FromArgb(24, 37, 56);
            var orange = XColor.FromArgb(228, 108, 10);
            var width = state.ContentWidth;

            state.Graphics.DrawRectangle(new XSolidBrush(dark), Margin, state.Y, width, 92);
            state.Graphics.DrawRectangle(new XSolidBrush(orange), Margin, state.Y, 10, 92);
            state.Graphics.DrawString(_configuration.EffectiveCompanyName, Font(20, true), XBrushes.White, new XRect(Margin + 22, state.Y + 14, width - 44, 24), XStringFormats.TopLeft);
            state.Graphics.DrawString(titulo, Font(15, true), XBrushes.White, new XRect(Margin + 22, state.Y + 42, width - 44, 20), XStringFormats.TopLeft);
            state.Graphics.DrawString(BuildBusinessDetails(), Font(8), new XSolidBrush(XColor.FromArgb(216, 225, 234)), new XRect(Margin + 22, state.Y + 66, width - 44, 18), XStringFormats.TopLeft);
            state.Y += 110;
        }

        private void DrawSection(PdfDrawState state, DocumentoSecao secao)
        {
            var linhas = secao.Linhas.Where(linha => !string.IsNullOrWhiteSpace(linha)).ToList();
            if (linhas.Count == 0)
            {
                linhas.Add("Sem informacoes registradas.");
            }

            EnsureSpace(state, 28 + (linhas.Count * LineHeight));
            state.Graphics.DrawString(secao.Titulo, Font(12, true), Brush(31, 41, 55), new XRect(Margin, state.Y, state.ContentWidth, 16), XStringFormats.TopLeft);
            state.Y += 20;

            foreach (var linha in linhas)
            {
                EnsureSpace(state, LineHeight + 2);
                state.Graphics.DrawString(linha, Font(9), Brush(71, 85, 105), new XRect(Margin + 8, state.Y, state.ContentWidth - 16, LineHeight), XStringFormats.TopLeft);
                state.Y += LineHeight;
            }

            state.Y += 10;
        }

        private void DrawSignature(PdfDrawState state)
        {
            EnsureSpace(state, 72);
            state.Y += 8;
            var lineY = state.Y + 36;
            state.Graphics.DrawLine(new XPen(Brush(100, 116, 139), 0.8), Margin + 40, lineY, Margin + state.ContentWidth - 40, lineY);
            state.Graphics.DrawString("Assinatura do cliente / responsavel", Font(9), Brush(71, 85, 105), new XRect(Margin, lineY + 8, state.ContentWidth, 14), XStringFormats.TopCenter);
            state.Y += 68;
        }

        private void DrawFooter(PdfDrawState state)
        {
            var y = state.Page.Height.Point - FooterMargin;
            state.Graphics.DrawLine(new XPen(Brush(226, 232, 240), 0.8), Margin, y - 8, Margin + state.ContentWidth, y - 8);
            state.Graphics.DrawString($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm} | {_configuration.EffectiveCompanyName}", Font(8), Brush(100, 116, 139), new XRect(Margin, y, state.ContentWidth, 12), XStringFormats.TopCenter);
        }

        private void EnsureSpace(PdfDrawState state, double required)
        {
            if (state.Y + required <= state.Page.Height.Point - FooterMargin - 12)
            {
                return;
            }

            DrawFooter(state);
            state.Graphics.Dispose();
            state.Page = state.Document.AddPage();
            state.Page.Size = PdfSharpCore.PageSize.A4;
            state.Graphics = XGraphics.FromPdfPage(state.Page);
            state.Y = Margin;
            DrawHeader(state, state.Document.Info.Subject);
        }

        private string BuildBusinessDetails()
        {
            return string.Join(" | ", new[]
            {
                _configuration.CompanyLegalName,
                string.IsNullOrWhiteSpace(_configuration.CompanyDocument) ? string.Empty : $"Doc: {_configuration.CompanyDocument}",
                _configuration.CompanyPhone,
                _configuration.CompanyAddress
            }.Where(item => !string.IsNullOrWhiteSpace(item)));
        }

        private static DocumentoSecao Secao(string titulo, IEnumerable<string> linhas)
        {
            return new DocumentoSecao(titulo, linhas.ToList());
        }

        private static string Linha(string label, string? value)
        {
            return $"{label}: {ValorOuPadrao(value, "-")}";
        }

        private static IEnumerable<string> DadosCliente(Cliente? cliente, string? nomeFallback = null, string? telefoneFallback = null)
        {
            yield return Linha("Nome", PrimeiroValor(cliente?.Nome, nomeFallback, "Nao informado"));
            yield return Linha("Documento", ValorOuPadrao(cliente?.Documento, "Nao informado"));
            yield return Linha("Telefone", PrimeiroValor(cliente?.Telefone, cliente?.WhatsApp, telefoneFallback, "Nao informado"));
            yield return Linha("Email", ValorOuPadrao(cliente?.Email, "Nao informado"));
        }

        private static IEnumerable<string> DadosVeiculo(Veiculo? veiculo, string? descricaoFallback = null, string? placaFallback = null)
        {
            yield return Linha("Veiculo", veiculo == null ? ValorOuPadrao(descricaoFallback, "Nao informado") : $"{veiculo.Marca} {veiculo.Modelo} {veiculo.Ano}".Trim());
            yield return Linha("Placa", PrimeiroValor(veiculo?.Placa, placaFallback, "Nao informada"));
            yield return Linha("Sistema eletrico", ValorOuPadrao(veiculo?.SistemaEletrico, "Nao informado"));
            yield return Linha("Quilometragem", veiculo?.Quilometragem > 0 ? $"{veiculo.Quilometragem:N0} km" : "Nao informada");
        }

        private static Veiculo? ResolverVeiculo(Guid? veiculoId, Cliente? cliente)
        {
            if (veiculoId.HasValue)
            {
                var veiculo = App.Repositories.Clientes.ObterTodosVeiculos().FirstOrDefault(item => item.Id == veiculoId.Value);
                if (veiculo != null)
                {
                    return veiculo;
                }
            }

            return cliente?.Veiculos.FirstOrDefault();
        }

        private static List<string> SepararLinhas(string texto)
        {
            return texto
                .Split(new[] { '\r', '\n', ';', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        private static string PrimeiroValor(params string?[] valores)
        {
            return valores.FirstOrDefault(valor => !string.IsNullOrWhiteSpace(valor))?.Trim() ?? string.Empty;
        }

        private static string ValorOuPadrao(string? valor, string fallback)
        {
            return string.IsNullOrWhiteSpace(valor) ? fallback : valor.Trim();
        }

        private static XFont Font(double size, bool bold = false)
        {
            return new XFont("Arial", size, bold ? XFontStyle.Bold : XFontStyle.Regular);
        }

        private static XSolidBrush Brush(byte r, byte g, byte b)
        {
            return new XSolidBrush(XColor.FromArgb(r, g, b));
        }

        private sealed record DocumentoSecao(string Titulo, List<string> Linhas);

        private sealed class PdfDrawState
        {
            public PdfDrawState(PdfDocument document, PdfPage page, XGraphics graphics)
            {
                Document = document;
                Page = page;
                Graphics = graphics;
                Y = Margin;
            }

            public PdfDocument Document { get; }
            public PdfPage Page { get; set; }
            public XGraphics Graphics { get; set; }
            public double Y { get; set; }
            public double ContentWidth => Page.Width.Point - (Margin * 2);
        }
    }
}
