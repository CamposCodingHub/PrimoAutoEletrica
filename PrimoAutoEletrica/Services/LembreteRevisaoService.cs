using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public sealed class LembreteRevisaoItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? ClienteId { get; set; }
        public Guid? VeiculoId { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public string Veiculo { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public DateTime DataLembrete { get; set; }
        public string Motivo { get; set; } = "Revisao eletrica preventiva";
        public bool Enviado { get; set; }
        public string OrigemOs { get; set; } = string.Empty;
    }

    public sealed class LembreteRevisaoService
    {
        private static string Arquivo()
        {
            var dir = Path.Combine(App.RuntimeAppDataPath, "Comercial");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "lembretes-revisao.json");
        }

        public List<LembreteRevisaoItem> Carregar()
        {
            var path = Arquivo();
            if (!File.Exists(path)) return new List<LembreteRevisaoItem>();
            try
            {
                return JsonSerializer.Deserialize<List<LembreteRevisaoItem>>(File.ReadAllText(path, Encoding.UTF8))
                       ?? new List<LembreteRevisaoItem>();
            }
            catch { return new List<LembreteRevisaoItem>(); }
        }

        public void Salvar(List<LembreteRevisaoItem> itens)
        {
            File.WriteAllText(Arquivo(), JsonSerializer.Serialize(itens, new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
        }

        public LembreteRevisaoItem CriarDeOs(OrdemServico ordem, int dias = 180)
        {
            var item = new LembreteRevisaoItem
            {
                ClienteId = ordem.ClienteId,
                VeiculoId = ordem.VeiculoId,
                ClienteNome = ordem.ClienteNomeSnapshot ?? string.Empty,
                Veiculo = ordem.VeiculoDescricaoSnapshot ?? string.Empty,
                Telefone = ordem.TelefoneClienteSnapshot ?? string.Empty,
                DataLembrete = (ordem.DataEntrega ?? ordem.DataConclusao ?? DateTime.Now).Date.AddDays(Math.Max(30, dias)),
                Motivo = "Revisao eletrica preventiva",
                OrigemOs = ordem.Numero
            };
            var lista = Carregar();
            lista.RemoveAll(x => string.Equals(x.OrigemOs, ordem.Numero, StringComparison.OrdinalIgnoreCase));
            lista.Add(item);
            Salvar(lista);
            return item;
        }

        public IReadOnlyList<LembreteRevisaoItem> VencidosOuHoje()
        {
            var hoje = DateTime.Today;
            return Carregar().Where(x => !x.Enviado && x.DataLembrete.Date <= hoje).OrderBy(x => x.DataLembrete).ToList();
        }
    }
}
