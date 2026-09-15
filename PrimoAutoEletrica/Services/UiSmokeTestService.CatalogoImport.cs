using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services.Catalogo;
using PrimoAutoEletrica.UserControls;

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

            var fila = new List<(string Nome, string Arquivo, string Marca, bool Confirmar, Action<CatalogoImportacaoPreview> AssertPrevia)>
            {
                ("CsvDni", Path.Combine(fixturesRoot, "Catalogo-DNI-Amostra.csv"), "DNI", true, previa =>
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
                ("CsvUeta", Path.Combine(fixturesRoot, "Catalogo-UETA-Amostra.csv"), "UETA", true, previa =>
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
                ("CsvBoschNgk", Path.Combine(fixturesRoot, "Catalogo-BOSCH-NGK-Amostra.csv"), "BOSCH", true, previa =>
                {
                    if (previa.TotalItens < 4)
                    {
                        throw new InvalidOperationException($"CSV BOSCH/NGK esperava >=4 itens, veio {previa.TotalItens}.");
                    }
                }),
                ("PdfDniMini", dniPdf, "DNI", true, previa =>
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
                fila.Add(("PdfGfReal", gfPdf, "GF", true, previa =>
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
                fila.Add(("PdfNgkAutosReal", ngkPdf, "NGK", true, previa =>
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
                fila.Add(("PdfNgkMotosReal", ngkMotosPdf, "NGK", true, previa =>
                {
                    if (previa.TotalItens == 0)
                    {
                        throw new InvalidOperationException("PDF NGK Motos nao extraiu nenhum codigo/produto.");
                    }
                }));
            }

            var dniFullPdf = Path.Combine(fixturesRoot, "Catalogo-DNI-2025-2026.pdf");
            if (File.Exists(dniFullPdf))
            {
                // Previa-only: extracao completa e custosa; confirmacao fica no import headless de producao.
                fila.Add(("PdfDniFullReal", dniFullPdf, "DNI", false, previa =>
                {
                    if (!string.Equals(previa.MarcaDetectada, "DNI", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"DNI full: marca={previa.MarcaDetectada}");
                    }

                    if (previa.TotalItens < 500)
                    {
                        throw new InvalidOperationException($"DNI full esperava >=500 itens, veio {previa.TotalItens}.");
                    }

                    var comFoto = previa.Itens.Count(i => !string.IsNullOrWhiteSpace(i.ImagemLocal));
                    if (comFoto < 100)
                    {
                        throw new InvalidOperationException($"DNI full esperava >=100 fotos, veio {comFoto}.");
                    }

                    _logger.LogInfo($"CatalogoImport PdfDniFullReal previa: itens={previa.TotalItens}, comFoto={comFoto}");
                }));
            }

            foreach (var (nome, arquivo, marca, confirmar, assertPrevia) in fila)
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

                    if (!confirmar)
                    {
                        _logger.LogInfo($"CatalogoImport {nome}: previa-only itens={previa.TotalItens}");
                        return;
                    }

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

            RunCheck(result, "CatalogoPecas:PaginaCarregaComItens", () =>
            {
                var pecasService = new CatalogoPecasService();
                var totalDb = pecasService.ObterTodos().Count;
                if (totalDb <= 0)
                {
                    throw new InvalidOperationException("CatalogoPecas vazio apos imports do smoke.");
                }

                var control = new CatalogoPecasControl();
                control.Measure(new Size(1366, 768));
                control.Arrange(new Rect(0, 0, 1366, 768));
                control.UpdateLayout();
                WaitForUiIdle();

                var grid = FindElementByName<DataGrid>(control, "CatalogoDataGrid")
                    ?? FindVisualChildren<DataGrid>(control).FirstOrDefault()
                    ?? throw new InvalidOperationException("DataGrid do Catalogo de Pecas nao encontrado.");

                var resumo = pecasService.ObterResumo();
                _logger.LogInfo(
                    $"CatalogoPecas pagina OK. DB={totalDb}, pendentes={resumo.PendentesRevisao}, grid={grid.Items.Count}");
            });
        }

        private static string ResolverPastaFixturesCatalogo()
        {
            var candidates = new[]
            {
                Path.Combine(App.RuntimeAppDataPath, "TestData", "Catalogos"),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TestData", "Catalogos")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "TestData", "Catalogos")),
                @"C:\Projetos\PrimoAutoEletrica\TestData\Catalogos"
            };

            foreach (var candidate in candidates)
            {
                if (Directory.Exists(candidate) &&
                    Directory.EnumerateFiles(candidate).Any())
                {
                    return candidate;
                }
            }

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
            gfx.DrawString("Chave de Luz combinada DNI 0711", font, XBrushes.Black, 40, y);
            y += 24;
            gfx.DrawString("Rele Auxiliar 5 terminais DNI 0410", font, XBrushes.Black, 40, y);
            y += 24;
            gfx.DrawString("Sensor de Temperatura DNI 7524", font, XBrushes.Black, 40, y);
            y += 24;
            gfx.DrawString("Interruptor de Freio DNI 8110", font, XBrushes.Black, 40, y);
            document.Save(caminho);
        }
    }
}
