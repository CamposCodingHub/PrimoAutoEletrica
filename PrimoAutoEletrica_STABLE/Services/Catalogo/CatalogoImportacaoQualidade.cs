using PrimoAutoEletrica.Models;
using System;
using System.Linq;

namespace PrimoAutoEletrica.Services.Catalogo
{
    public static class CatalogoImportacaoQualidade
    {
        public const double LimiarAlertaSemNomeReal = 0.30;
        public const double LimiarBloqueioRecomendado = 0.60;

        public static void AplicarAvaliacao(CatalogoImportacaoPreview preview)
        {
            if (preview == null)
            {
                return;
            }

            var candidatos = preview.Itens
                .Where(item => !string.Equals(item.StatusRevisao, "Ja existente", StringComparison.OrdinalIgnoreCase))
                .ToList();

            preview.TotalSemDescricao = candidatos.Count(item => string.IsNullOrWhiteSpace(item.Descricao));
            preview.TotalSemNomeReal = candidatos.Count(item =>
                CatalogoPdfTextParser.EhNomeFallback(item.Nome, item.Marca, item.CodigoFabricante));

            if (candidatos.Count == 0)
            {
                preview.AlertaQualidade = string.Empty;
                preview.ImportacaoArriscada = false;
                return;
            }

            var ratioSemNome = preview.TotalSemNomeReal / (double)candidatos.Count;
            var ratioSemDescricao = preview.TotalSemDescricao / (double)candidatos.Count;
            preview.ImportacaoArriscada = ratioSemNome >= LimiarBloqueioRecomendado ||
                                          (string.Equals(preview.TipoArquivo, "PDF", StringComparison.OrdinalIgnoreCase) &&
                                           ratioSemNome >= LimiarAlertaSemNomeReal &&
                                           ratioSemDescricao >= 0.80);

            if (preview.TotalSemNomeReal == 0 && preview.TotalSemDescricao <= candidatos.Count * 0.5)
            {
                preview.AlertaQualidade = "Qualidade da previa aceitavel. Revise itens sensiveis antes de confirmar.";
                return;
            }

            if (preview.ImportacaoArriscada)
            {
                preview.AlertaQualidade =
                    $"Atencao: {preview.TotalSemNomeReal} de {candidatos.Count} itens ainda usam nome generico (ex.: Produto + codigo) " +
                    $"e {preview.TotalSemDescricao} estao sem descricao. Recomendado revisar ou usar CSV/Excel do fornecedor antes de importar em massa.";
                return;
            }

            preview.AlertaQualidade =
                $"Atencao: {preview.TotalSemNomeReal} item(ns) com nome generico e {preview.TotalSemDescricao} sem descricao. " +
                "Confira a previa antes de confirmar.";
        }

        public static void RegistrarAlertasNaPrevia(CatalogoImportacaoPreview preview)
        {
            AplicarAvaliacao(preview);

            if (string.IsNullOrWhiteSpace(preview.AlertaQualidade))
            {
                return;
            }

            preview.Erros.Insert(0, new CatalogoImportacaoErro
            {
                LinhaOrigem = "ALERTA",
                CodigoDetectado = string.Empty,
                MensagemErro = preview.AlertaQualidade,
                ConteudoOriginal = preview.ImportacaoArriscada
                    ? "Importacao arriscada detectada"
                    : "Revisar qualidade da previa",
                DataErro = DateTime.Now
            });
        }
    }
}
