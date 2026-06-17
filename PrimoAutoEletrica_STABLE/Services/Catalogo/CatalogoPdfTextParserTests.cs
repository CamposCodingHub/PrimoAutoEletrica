#if DEBUG
using System.Text.RegularExpressions;

namespace PrimoAutoEletrica.Services.Catalogo
{
    /// <summary>
    /// Testes manuais/debug: executar via depurador ou expandir para projeto de testes.
    /// </summary>
    internal static class CatalogoPdfTextParserTests
    {
        public static bool ExecutarSanidade()
        {
            var regexDni = CatalogoMarcaDetector.ObterPerfil("DNI").CodigoRegex;
            var linhaDni = "www.dni.com.br147Luz de Advertencia PortatilPortable Warning LightDNI 2042 Branco";
            var match = regexDni.Match(linhaDni);
            if (!match.Success)
            {
                return false;
            }

            var (nome, descricao) = CatalogoPdfTextParser.ExtrairNomeDescricao(linhaDni, match.Index, match.Length, regexDni);
            if (string.IsNullOrWhiteSpace(nome) || !nome.Contains("Advert", System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var regexUeta = CatalogoMarcaDetector.ObterPerfil("UETA").CodigoRegex;
            var linhaUeta = "RELÉS AUXILIARESU-060 ..................16U-061";
            var matchUeta = regexUeta.Match(linhaUeta);
            return matchUeta.Success && matchUeta.Groups["codigo"].Value.Equals("U-060", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
#endif
