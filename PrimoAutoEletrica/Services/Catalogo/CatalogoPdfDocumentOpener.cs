using System;
using System.IO;
using UglyToad.PdfPig;

namespace PrimoAutoEletrica.Services.Catalogo
{
    internal static class CatalogoPdfDocumentOpener
    {
        public static PdfDocument Open(string caminhoArquivo)
        {
            try
            {
                return PdfDocument.Open(
                    caminhoArquivo,
                    new ParsingOptions
                    {
                        UseLenientParsing = true
                    });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Nao foi possivel abrir o PDF '{Path.GetFileName(caminhoArquivo)}'. " +
                    "Verifique se o arquivo nao esta corrompido, protegido por senha ou em uso. " +
                    $"Detalhe: {ex.Message}",
                    ex);
            }
        }
    }
}
