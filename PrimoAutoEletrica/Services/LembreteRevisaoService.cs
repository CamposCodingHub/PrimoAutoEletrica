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
        public Guid? OrdemServicoId { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public string Veiculo { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public DateTime DataLembrete { get; set; }
        public string Motivo { get; set; } = "Revisao eletrica preventiva";
        /// <summary>
        /// Tratado localmente (lista pós-venda). NÃO significa envio WhatsApp Cloud.
        /// </summary>
        public bool Enviado { get; set; }
        public DateTime? TratadoEmUtc { get; set; }
        public string OrigemOs { get; set; } = string.Empty;
        public string ObservacaoLocal { get; set; } = string.Empty;
    }

    public sealed class LembreteRevisaoService
    {
        private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

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
            File.WriteAllText(Arquivo(), JsonSerializer.Serialize(itens, JsonOpts), Encoding.UTF8);
        }

        public IReadOnlyList<LembreteRevisaoItem> ListarOrdenados(bool somentePendentes = false)
        {
            var q = Carregar().AsEnumerable();
            if (somentePendentes)
            {
                q = q.Where(x => !x.Enviado);
            }

            return q.OrderBy(x => x.Enviado)
                .ThenBy(x => x.DataLembrete)
                .ToList();
        }

        public LembreteRevisaoItem CriarDeOs(OrdemServico ordem, int dias = 180)
        {
            if (ordem == null) throw new ArgumentNullException(nameof(ordem));

            var item = new LembreteRevisaoItem
            {
                ClienteId = ordem.ClienteId == Guid.Empty ? null : ordem.ClienteId,
                VeiculoId = ordem.VeiculoId,
                OrdemServicoId = ordem.Id == Guid.Empty ? null : ordem.Id,
                ClienteNome = ordem.ClienteNomeSnapshot ?? string.Empty,
                Veiculo = ordem.VeiculoDescricaoSnapshot ?? string.Empty,
                Telefone = ordem.TelefoneClienteSnapshot ?? string.Empty,
                DataLembrete = (ordem.DataEntrega ?? ordem.DataConclusao ?? DateTime.Now).Date.AddDays(Math.Max(30, dias)),
                Motivo = "Revisao eletrica preventiva",
                OrigemOs = ordem.Numero ?? string.Empty,
                Enviado = false
            };
            var lista = Carregar();
            // Dedup por OS Id quando possível; senão por número.
            if (item.OrdemServicoId.HasValue)
            {
                lista.RemoveAll(x => x.OrdemServicoId == item.OrdemServicoId);
            }
            else if (!string.IsNullOrWhiteSpace(item.OrigemOs))
            {
                lista.RemoveAll(x => string.Equals(x.OrigemOs, item.OrigemOs, StringComparison.OrdinalIgnoreCase));
            }

            lista.Add(item);
            Salvar(lista);
            return item;
        }

        public IReadOnlyList<LembreteRevisaoItem> VencidosOuHoje()
        {
            var hoje = DateTime.Today;
            return Carregar().Where(x => !x.Enviado && x.DataLembrete.Date <= hoje).OrderBy(x => x.DataLembrete).ToList();
        }

        /// <summary>
        /// Marca lembrete como tratado na lista local. Não envia WhatsApp.
        /// </summary>
        public bool MarcarTratadoLocalmente(Guid lembreteId, string? observacao = null)
        {
            var lista = Carregar();
            var item = lista.FirstOrDefault(x => x.Id == lembreteId);
            if (item == null) return false;
            item.Enviado = true;
            item.TratadoEmUtc = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(observacao))
            {
                item.ObservacaoLocal = observacao.Trim();
            }
            else if (string.IsNullOrWhiteSpace(item.ObservacaoLocal))
            {
                item.ObservacaoLocal = "Tratado localmente (sem WhatsApp Cloud).";
            }

            Salvar(lista);
            return true;
        }

        public bool ReabrirPendente(Guid lembreteId)
        {
            var lista = Carregar();
            var item = lista.FirstOrDefault(x => x.Id == lembreteId);
            if (item == null) return false;
            item.Enviado = false;
            item.TratadoEmUtc = null;
            Salvar(lista);
            return true;
        }
    }
}
