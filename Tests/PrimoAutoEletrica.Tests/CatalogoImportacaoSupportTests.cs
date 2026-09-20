using System;
using System.IO;
using System.Text;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.Catalogo;
using Xunit;

namespace PrimoAutoEletrica.Tests
{
    public class CatalogoImportacaoSupportTests
    {
        [Theory]
        [InlineData("CATÁLOGO_ROLAMENTOS.pdf", "PDF")]
        [InlineData("CATÁLOGO_PORTA_ESCOVAS.pdf", "PDF")]
        [InlineData("CATÁLOGO_REGULADORES_2023.pdf", "PDF")]
        [InlineData("Catálogo GF.pdf", "PDF")]
        [InlineData("SKF catalog.pdf", "PDF")]
        [InlineData("foto-peca.jpeg", "IMAGEM")]
        [InlineData("foto-peca.jpg", "IMAGEM")]
        [InlineData("foto-peca.png", "IMAGEM")]
        [InlineData("foto-peca.webp", "IMAGEM")]
        [InlineData("lista.csv", "CSV")]
        public void DetectarTipo_AceitaPdfAcentuadoEImagens(string fileName, string tipoEsperado)
        {
            var tipo = CatalogoArquivoSupport.DetectarTipo(fileName, "AUTO");
            Assert.Equal(tipoEsperado, tipo);
        }

        [Fact]
        public void FiltroDialogo_IncluiPdfEImagensComuns()
        {
            var filtro = CatalogoArquivoSupport.FiltroDialogo;
            Assert.Contains("*.pdf", filtro, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("*.jpg", filtro, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("*.jpeg", filtro, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("*.png", filtro, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("*.webp", filtro, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ValidarECopiar_AceitaNomeUnicode()
        {
            var pasta = Path.Combine(Path.GetTempPath(), "primox-catalogo-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(pasta);
            var origem = Path.Combine(pasta, "CATÁLOGO_ROLAMENTOS.pdf");
            File.WriteAllBytes(origem, Encoding.ASCII.GetBytes("%PDF-1.1\n1 0 obj<<>>endobj\ntrailer<<>>\n%%EOF"));

            var validado = CatalogoArquivoSupport.ValidarArquivo(origem);
            Assert.True(File.Exists(validado));

            var copiado = CatalogoArquivoSupport.CopiarParaWorkspace(origem);
            Assert.True(File.Exists(copiado));
            Assert.Equal(".pdf", Path.GetExtension(copiado), StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void CriarItemDocumento_PreservaNomeOriginalECategoria()
        {
            var item = CatalogoArquivoSupport.CriarItemDocumento(
                @"C:\Downloads\CATÁLOGO_ROLAMENTOS.pdf",
                @"C:\AppData\Media\Catalogo\arquivo.pdf",
                "Catalogo importado",
                "GERAL",
                "PDF");

            Assert.Equal("CATÁLOGO_ROLAMENTOS.pdf", item.ArquivoOrigem);
            Assert.Equal("CATÁLOGO_ROLAMENTOS", item.Nome);
            Assert.Equal("Rolamentos", item.Categoria);
            Assert.False(string.IsNullOrWhiteSpace(item.CodigoFabricante));
            Assert.Equal(@"C:\AppData\Media\Catalogo\arquivo.pdf", item.ImagemLocal);
        }

        [Theory]
        [InlineData("CATÁLOGO_ROLAMENTOS.pdf", "")]
        [InlineData("Catálogo GF.pdf", "GF")]
        [InlineData("SKF catalog 2024.pdf", "SKF")]
        [InlineData("catalogo-dni-2025.pdf", "DNI")]
        public void DetectarMarcaPorArquivo_NaoForcaDniEmCatalogosGenericos(string fileName, string marcaEsperada)
        {
            var marca = CatalogoMarcaDetector.DetectarMarcaPorArquivo(fileName, "DNI");
            Assert.Equal(marcaEsperada, marca);
        }

        [Fact]
        public void ResolverMarcaEFonte_CatalogoAcentuadoUsaGeral()
        {
            var (marca, fonte) = CatalogoMarcaDetector.ResolverMarcaEFonte(
                "CATÁLOGO_RETIFICADORES.pdf",
                marcaInformada: null,
                fonteInformada: null);

            Assert.Equal("GERAL", marca);
            Assert.Contains("CATÁLOGO_RETIFICADORES", fonte, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void DetectarTipo_RejeitaExtensaoDesconhecidaComMensagemClara()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                CatalogoArquivoSupport.DetectarTipo("arquivo.exe", "AUTO"));
            Assert.Contains("nao suportado", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ConfirmarExclusao_MarcaAcaoComoDestrutiva()
        {
            var request = new CriticalActionRequest
            {
                Keyword = "EXCLUIR",
                IsDestructive = true
            };

            Assert.True(request.IsDestructive);
            Assert.Equal("EXCLUIR", request.Keyword);
        }

        [Fact]
        public void ValidarArquivo_RejeitaArquivoVazioComMensagemClara()
        {
            var pasta = Path.Combine(Path.GetTempPath(), "primox-catalogo-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(pasta);
            var origem = Path.Combine(pasta, "Catálogo GF.pdf");
            File.WriteAllBytes(origem, Array.Empty<byte>());

            var ex = Assert.Throws<InvalidOperationException>(() => CatalogoArquivoSupport.ValidarArquivo(origem));
            Assert.Contains("vazio", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SanitizarSegmentoArquivo_PreservaAcentos()
        {
            var segmento = CatalogoArquivoSupport.SanitizarSegmentoArquivo("CATÁLOGO ROLAMENTOS");
            Assert.Equal("CATÁLOGO_ROLAMENTOS", segmento);
        }

        [Theory]
        [InlineData("CATÁLOGO_REGULADORES_2023.pdf", "Reguladores")]
        [InlineData("CATÁLOGO_RETIFICADORES.pdf", "Retificadores")]
        [InlineData("CATÁLOGO_PORTA_ESCOVAS.pdf", "Porta-escovas")]
        public void InferirCategoriaDoNome_UsaNomeDoArquivo(string fileName, string categoria)
        {
            Assert.Equal(categoria, CatalogoArquivoSupport.InferirCategoriaDoNome(fileName));
        }

        [Fact]
        public void CriarItemDocumento_ImagemMarcaConsultaVisual()
        {
            var item = CatalogoArquivoSupport.CriarItemDocumento(
                "foto-peca.webp",
                Path.Combine("Media", "Catalogo", "foto-peca.webp"),
                "Catalogo importado",
                "GERAL",
                "IMAGEM");

            Assert.Equal("foto-peca.webp", item.ArquivoOrigem);
            Assert.Contains("Imagem", item.MensagemValidacao, StringComparison.OrdinalIgnoreCase);
            Assert.False(string.IsNullOrWhiteSpace(item.ImagemUrl));
        }
    }
}
