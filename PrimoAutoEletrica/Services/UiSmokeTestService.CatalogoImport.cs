using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services.Catalogo;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        private void RunCatalogoImportRealChecks(UiSmokeTestRunResult result)
        {
            GarantirBancoIsoladoDoSmoke("Catalogo Import Real");
            var importService = new CatalogoImportacaoService();
            var fixturesRoot = ResolverPastaFixturesCatalogo();
            Directory.CreateDirectory(fixturesRoot);

            var dniPdf = Path.Combine(fixturesRoot, "Catalogo-DNI-Mini.pdf");
            GerarPdfCatalogoDniMini(dniPdf);

            var fila = new List<(string Nome, string Arquivo, string Marca, Action<CatalogoImportacaoPreview> AssertPrevia)>
            {
                ("CsvDni", Path.Combine(fixturesRoot, "Catalogo-DNI-Amostra.csv"), "DNI", previa =>
                {
                    if (previa.TotalItens < 5)
                    {
                        throw new InvalidOperationException($"CSV DNI esperava >=5 itens, veio {previa.TotalItens}.");
                    }

                    if (previa.Itens.Any(i => !i.CodigoFabricante.Contains("DNI", StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException("CSV DNI gerou codigo sem prefixo DNI.");
                    }
                }),
                ("CsvUeta", Path.Combine(fixturesRoot, "Catalogo-UETA-Amostra.csv"), "UETA", previa =>
                {
                    if (previa.TotalItens < 5)
                    {
                        throw new InvalidOperationException($"CSV UETA esperava >=5 itens, veio {previa.TotalItens}.");
                    }

                    if (!previa.Itens.All(i => i.CodigoFabricante.Contains("U-", StringComparison.OrdinalIgnoreCase) ||
                                               i.Marca.Equals("UETA", StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException("CSV UETA nao preservou marca/codigos U-.");
                    }
                }),
                ("CsvBoschNgk", Path.Combine(fixturesRoot, "Catalogo-BOSCH-NGK-Amostra.csv"), "BOSCH", previa =>
                {
                    if (previa.TotalItens < 4)
                    {
                        throw new InvalidOperationException($"CSV BOSCH/NGK esperava >=4 itens, veio {previa.TotalItens}.");
                    }
                }),
                ("PdfDniMini", dniPdf, "DNI", previa =>
                {
                    if (previa.TotalItens < 2)
                    {
                        throw new InvalidOperationException($"PDF DNI mini esperava >=2 itens, veio {previa.TotalItens}.");
                    }

                    if (!string.Equals(previa.MarcaDetectada, "DNI", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Marca detectada={previa.MarcaDetectada}, esperado DNI.");
                    }
                })
            };

            var gfPdf = Path.Combine(fixturesRoot, "Catalogo-GF.pdf");
            if (File.Exists(gfPdf))
            {
                fila.Add(("PdfGfReal", gfPdf, "GF", previa =>
                {
                    if (!string.Equals(previa.MarcaDetectada, "GF", StringComparison.OrdinalIgnoreCase) &&
                        !previa.Itens.All(i => i.Marca.Equals("GF", StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException(
                            $"GF sobrescrito indevidamente. MarcaDetectada={previa.MarcaDetectada}; amostra={previa.Itens.FirstOrDefault()?.Marca}");
                    }

                    if (previa.Itens.Any(i => i.CodigoFabricante.StartsWith("GERAL ", StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException("PDF GF ainda gera codigos lixo com prefixo GERAL.");
                    }

                    if (previa.TotalItens == 0)
                    {
                        throw new InvalidOperationException("PDF GF nao extraiu nenhum codigo.");
                    }
                }));
            }

            var ngkPdf = Path.Combine(fixturesRoot, "Catalogo-NGK-Autos.pdf");
            if (File.Exists(ngkPdf))
            {
                fila.Add(("PdfNgkAutosReal", ngkPdf, "NGK", previa =>
                {
                    if (!string.Equals(previa.MarcaDetectada, "NGK", StringComparison.OrdinalIgnoreCase) &&
                        !previa.Itens.Any(i => i.Marca.Equals("NGK", StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException($"NGK nao detectado. MarcaDetectada={previa.MarcaDetectada}");
                    }

                    if (previa.TotalItens == 0)
                    {
                        throw new InvalidOperationException("PDF NGK Autos nao extraiu nenhum codigo/produto.");
                    }
                }));
            }

            var ngkMotosPdf = Path.Combine(fixturesRoot, "Catalogo-NGK-Motos.pdf");
            if (File.Exists(ngkMotosPdf))
            {
                fila.Add(("PdfNgkMotosReal", ngkMotosPdf, "NGK", previa =>
                {
                    if (previa.TotalItens == 0)
                    {
                        throw new InvalidOperationException("PDF NGK Motos nao extraiu nenhum codigo/produto.");
                    }
                }));
            }

            foreach (var (nome, arquivo, marca, assertPrevia) in fila)
            {
                RunCheck(result, $"CatalogoImport:{nome}", () =>
                {
                    if (!File.Exists(arquivo))
                    {
                        throw new FileNotFoundException($"Fixture de catalogo ausente: {arquivo}");
                    }

                    var previa = importService.CriarPreviaImportacao(
                        arquivo,
                        tipoArquivo: "AUTO",
                        fonteCatalogo: $"Smoke {nome}",
                        marca: marca);

                    assertPrevia(previa);

                    // Evita duplicar no mesmo smoke: marca itens ja existentes como tal e importa so novos.
                    var confirmacao = importService.ConfirmarImportacao(previa);
                    if (confirmacao.TotalImportados + confirmacao.TotalDuplicados <= 0 && previa.TotalItens > 0)
                    {
                        throw new InvalidOperationException(
                            $"Importacao {nome} nao persistiu itens. Status={confirmacao.Status}; Erros={confirmacao.TotalComErro}");
                    }

                    _logger.LogInfo(
                        $"CatalogoImport {nome}: lidos={confirmacao.TotalLidos}, importados={confirmacao.TotalImportados}, " +
                        $"duplicados={confirmacao.TotalDuplicados}, erros={confirmacao.TotalComErro}, status={confirmacao.Status}");
                });
            }

            RunCheck(result, "CatalogoImport:MarcaCustomNaoViraGeral", () =>
            {
                var perfil = CatalogoMarcaDetector.ObterPerfil("GF");
                if (string.Equals(perfil.Marca, "GERAL", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("ObterPerfil(GF) ainda retorna GERAL.");
                }

                if (!string.Equals(perfil.Marca, "GF", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"ObterPerfil(GF) retornou '{perfil.Marca}'.");
                }
            });
        }

        private static string ResolverPastaFixturesCatalogo()
        {
            var candidates = new[]
            {
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TestData", "Catalogos")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "TestData", "Catalogos")),
                Path.Combine(App.RuntimeAppDataPath, "TestData", "Catalogos")
            };

            foreach (var candidate in candidates)
            {
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            var fallback = candidates[0];
            Directory.CreateDirectory(fallback);
            return fallback;
        }

        private static void GerarPdfCatalogoDniMini(string caminho)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);
            if (File.Exists(caminho))
            {
                File.Delete(caminho);
            }

            var document = new PdfDocument();
            var page = document.AddPage();
            using var gfx = XGraphics.FromPdfPage(page);
            var font = new XFont("Arial", 12);
            var y = 40.0;
            gfx.DrawString("Catalogo DNI Automotive - amostra smoke", font, XBrushes.Black, 40, y);
            y += 28;
            gfx.DrawString("www.dni.com.br", font, XBrushes.Black, 40, y);
            y += 28;
            gfx.DrawString("Luz de Advertencia Portatil Portable Warning Light DNI 2042 Branco", font, XBrushes.Black, 40, y);
            y += 24;
            gfx.DrawString("Chave de Luz combinada DNI 711", font, XBrushes.Black, 40, y);
            y += 24;
            gfx.DrawString("Rele Auxiliar 5 terminais DNI 410", font, XBrushes.Black, 40, y);
            y += 24;
            gfx.DrawString("Sensor de Temperatura DNI 7524", font, XBrushes.Black, 40, y);
            y += 24;
            gfx.DrawString("Interruptor de Freio DNI 8110", font, XBrushes.Black, 40, y);
            document.Save(caminho);
        }
    }
}
