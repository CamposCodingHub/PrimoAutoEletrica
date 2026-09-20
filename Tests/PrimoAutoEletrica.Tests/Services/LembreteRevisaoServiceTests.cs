using System;
using System.IO;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public class LembreteRevisaoServiceTests
    {
        [Fact]
        public void MarcarTratadoLocalmente_NaoImplicaWhatsApp()
        {
            var src = File.ReadAllText(LocateSource("Services", "LembreteRevisaoService.cs"));
            Assert.Contains("MarcarTratadoLocalmente", src);
            Assert.Contains("Não envia WhatsApp", src);
            Assert.DoesNotContain("graph.facebook.com", src);
        }

        [Fact]
        public void CriarDeOs_DedupPorOrdemServicoId_Contrato()
        {
            var ordem = new OrdemServico
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Numero = "OS-TEST-1",
                ClienteId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                ClienteNomeSnapshot = "Cliente Teste",
                VeiculoDescricaoSnapshot = "Gol",
                TelefoneClienteSnapshot = "11999999999",
                DataEntrega = DateTime.Today,
                Status = "Entregue"
            };

            var item = new LembreteRevisaoItem
            {
                ClienteId = ordem.ClienteId,
                OrdemServicoId = ordem.Id,
                ClienteNome = ordem.ClienteNomeSnapshot,
                OrigemOs = ordem.Numero,
                DataLembrete = ordem.DataEntrega!.Value.AddDays(180),
                Enviado = false
            };

            Assert.Equal(ordem.Id, item.OrdemServicoId);
            Assert.Equal(ordem.ClienteId, item.ClienteId);
            Assert.False(item.Enviado);

            var svcSrc = File.ReadAllText(LocateSource("Services", "LembreteRevisaoService.cs"));
            Assert.Contains("lista.RemoveAll(x => x.OrdemServicoId == item.OrdemServicoId)", svcSrc);
        }

        [Fact]
        public void LembretesRevisaoWindow_ExisteESemSendWhatsApp()
        {
            var src = File.ReadAllText(LocateSource("Views", "LembretesRevisaoWindow.cs"));
            Assert.Contains("Marcar tratado (local)", src);
            Assert.Contains("Sem WhatsApp Cloud", src);
            Assert.DoesNotContain("HttpClient", src);
            Assert.DoesNotContain("SendWhatsApp", src);
        }

        private static string LocateSource(string folder, string fileName)
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
