using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public class DviChecklistServiceTests
    {
        [Fact]
        public void DocumentoDvi_Contrato_VinculaOsEOrcamentoComFoto()
        {
            var doc = new DviChecklistDocument
            {
                OrdemServicoId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                OrcamentoId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Itens = new List<DviChecklistItem>
                {
                    new() { Categoria = "Bateria", Nome = "Tensao", OkEntrada = true, FotoPath = "foto.jpg" }
                }
            };
            Assert.NotEqual(Guid.Empty, doc.OrdemServicoId);
            Assert.NotNull(doc.OrcamentoId);
            Assert.Single(doc.Itens);
            Assert.False(string.IsNullOrWhiteSpace(doc.Itens[0].FotoPath));
        }

        [Fact]
        public void ResumoTexto_IncluiStatusEFoto()
        {
            var svc = new DviChecklistService();
            var txt = svc.ResumoTexto(new[]
            {
                new DviChecklistItem { Categoria = "Farol", Nome = "Esquerdo", OkEntrada = true, OkSaida = false, FotoPath = @"C:\tmp\a.png" }
            });
            Assert.Contains("Farol/Esquerdo", txt);
            Assert.Contains("E=OK", txt);
            Assert.Contains("S=PEND", txt);
            Assert.Contains("a.png", txt);
        }

        [Fact]
        public void Source_SalvarAceitaOrcamentoEPendingMedia()
        {
            var src = File.ReadAllText(Locate("Services", "DviChecklistService.cs"));
            Assert.Contains("pendingMediaOrdemId", src);
            Assert.Contains("ArquivoOrcamento", src);
            Assert.Contains("RemontarFotosPendentes", src);
            Assert.DoesNotContain("CloudApprovalService", src, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ApproveOnCloud", src, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Sem cloud approval", src, StringComparison.OrdinalIgnoreCase);
        }

        private static string Locate(string folder, string fileName)
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "PrimoAutoEletrica", folder, fileName);
                if (File.Exists(candidate)) return candidate;
                candidate = Path.Combine(dir.FullName, folder, fileName);
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            throw new FileNotFoundException($"Nao encontrou {folder}/{fileName}");
        }
    }
}
