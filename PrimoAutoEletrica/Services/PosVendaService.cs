using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public interface IPosVendaService
    {
        PosVendaItem Salvar(PosVendaItem item);
        PosVendaItem? ObterPorId(Guid id);
        IReadOnlyList<PosVendaItem> ListarPorClienteId(Guid clienteId);
        IReadOnlyList<PosVendaItem> ListarPorVeiculoId(Guid veiculoId);
        IReadOnlyList<PosVendaItem> ListarPorOrdemServicoId(Guid osId);
        IReadOnlyList<PosVendaItem> ListarPendentes();
        IReadOnlyList<PosVendaItem> ListarVencidos();
        IReadOnlyList<PosVendaItem> ListarTodos();
        PosVendaItem CriarDeOrdemServico(OrdemServico ordem, PosVendaTipoEnum tipo = PosVendaTipoEnum.FollowUpPosServico, int dias = 7, string responsavel = "");
        bool RegistrarContato(Guid id, string resultado, string responsavel, bool resolvido = true, PosVendaStatusEnum novoStatus = PosVendaStatusEnum.Concluido);
    }

    /// <summary>
    /// Servico de gerenciamento de pos-venda, garantias, revisoes preventivas e retornos.
    /// Opera com associacao rigorosa por ClienteId, VeiculoId e OrdemServicoId.
    /// </summary>
    public sealed class PosVendaService : IPosVendaService
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly object _lock = new();
        private readonly string? _instanceStorageDirectory;

        /// <summary>
        /// Diretorio de testes para isolamento completo em suite automatizada.
        /// </summary>
        public static string? TestStorageDirectoryOverride { get; set; }

        public PosVendaService(string? storageDirectoryOverride = null)
        {
            _instanceStorageDirectory = storageDirectoryOverride;
        }

        private string ObterCaminhoArquivo()
        {
            var dir = _instanceStorageDirectory 
                      ?? TestStorageDirectoryOverride 
                      ?? Path.Combine(App.RuntimeAppDataPath, "Comercial");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "pos-venda.json");
        }

        public List<PosVendaItem> CarregarTodos()
        {
            lock (_lock)
            {
                var caminho = ObterCaminhoArquivo();
                if (!File.Exists(caminho)) return new List<PosVendaItem>();

                try
                {
                    var json = File.ReadAllText(caminho, Encoding.UTF8);
                    return JsonSerializer.Deserialize<List<PosVendaItem>>(json, JsonOpts) ?? new List<PosVendaItem>();
                }
                catch
                {
                    return new List<PosVendaItem>();
                }
            }
        }

        public void SalvarTodos(List<PosVendaItem> itens)
        {
            lock (_lock)
            {
                var caminho = ObterCaminhoArquivo();
                var json = JsonSerializer.Serialize(itens, JsonOpts);
                File.WriteAllText(caminho, json, Encoding.UTF8);
            }
        }

        public PosVendaItem Salvar(PosVendaItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (item.Id == Guid.Empty)
            {
                item.Id = Guid.NewGuid();
            }

            if (item.ClienteId == Guid.Empty)
            {
                throw new ArgumentException("O registro de pos-venda exige um ClienteId valido.", nameof(item.ClienteId));
            }

            if (item.VeiculoId.HasValue && item.VeiculoId.Value == Guid.Empty)
            {
                throw new ArgumentException("Se informado, o VeiculoId precisa ser um identificador valido.", nameof(item.VeiculoId));
            }

            lock (_lock)
            {
                var lista = CarregarTodos();
                var idx = lista.FindIndex(x => x.Id == item.Id);
                if (idx >= 0)
                {
                    lista[idx] = item;
                }
                else
                {
                    lista.Add(item);
                }
                SalvarTodos(lista);
            }

            return item;
        }

        public PosVendaItem? ObterPorId(Guid id)
        {
            if (id == Guid.Empty) return null;
            return CarregarTodos().FirstOrDefault(x => x.Id == id);
        }

        public IReadOnlyList<PosVendaItem> ListarPorClienteId(Guid clienteId)
        {
            if (clienteId == Guid.Empty) return Array.Empty<PosVendaItem>();
            return CarregarTodos()
                .Where(x => x.ClienteId == clienteId)
                .OrderByDescending(x => x.DataCriacao)
                .ToList();
        }

        public IReadOnlyList<PosVendaItem> ListarPorVeiculoId(Guid veiculoId)
        {
            if (veiculoId == Guid.Empty) return Array.Empty<PosVendaItem>();
            return CarregarTodos()
                .Where(x => x.VeiculoId == veiculoId)
                .OrderByDescending(x => x.DataCriacao)
                .ToList();
        }

        public IReadOnlyList<PosVendaItem> ListarPorOrdemServicoId(Guid osId)
        {
            if (osId == Guid.Empty) return Array.Empty<PosVendaItem>();
            return CarregarTodos()
                .Where(x => x.OrdemServicoId == osId)
                .OrderByDescending(x => x.DataCriacao)
                .ToList();
        }

        public IReadOnlyList<PosVendaItem> ListarPendentes()
        {
            return CarregarTodos()
                .Where(x => x.Status == PosVendaStatusEnum.Pendente || x.Status == PosVendaStatusEnum.Agendado)
                .OrderBy(x => x.DataPrevistaContato)
                .ToList();
        }

        public IReadOnlyList<PosVendaItem> ListarVencidos()
        {
            var hoje = DateTime.Today;
            return CarregarTodos()
                .Where(x => (x.Status == PosVendaStatusEnum.Pendente || x.Status == PosVendaStatusEnum.Agendado) && x.DataPrevistaContato.Date < hoje)
                .OrderBy(x => x.DataPrevistaContato)
                .ToList();
        }

        public IReadOnlyList<PosVendaItem> ListarTodos()
        {
            return CarregarTodos()
                .OrderByDescending(x => x.DataCriacao)
                .ToList();
        }

        public PosVendaItem CriarDeOrdemServico(OrdemServico ordem, PosVendaTipoEnum tipo = PosVendaTipoEnum.FollowUpPosServico, int dias = 7, string responsavel = "")
        {
            ArgumentNullException.ThrowIfNull(ordem);

            if (ordem.Id == Guid.Empty)
            {
                throw new ArgumentException("A Ordem de Servico precisa ter um Id valido para gerar Pos-Venda.", nameof(ordem));
            }

            var item = new PosVendaItem
            {
                Id = Guid.NewGuid(),
                OrdemServicoId = ordem.Id,
                ClienteId = ordem.ClienteId,
                VeiculoId = ordem.VeiculoId,
                ClienteNomeSnapshot = ordem.ClienteNomeSnapshot ?? string.Empty,
                VeiculoDescricaoSnapshot = ordem.VeiculoDescricaoSnapshot ?? string.Empty,
                PlacaSnapshot = ordem.PlacaSnapshot ?? string.Empty,
                OsNumeroSnapshot = ordem.Numero ?? string.Empty,
                TelefoneSnapshot = ordem.TelefoneClienteSnapshot ?? string.Empty,
                Tipo = tipo,
                Status = PosVendaStatusEnum.Pendente,
                DataCriacao = DateTime.Now,
                DataPrevistaContato = DateTime.Today.AddDays(Math.Max(1, dias)),
                Responsavel = string.IsNullOrWhiteSpace(responsavel) ? (ordem.TecnicoId.HasValue ? $"Tecnico #{ordem.TecnicoId}" : "Atendimento") : responsavel,
                GarantiaValidaAte = ordem.GarantiaValidaAte,
                Observacoes = $"Gerado a partir da conclusao da OS {ordem.Numero}."
            };

            return Salvar(item);
        }

        public bool RegistrarContato(Guid id, string resultado, string responsavel, bool resolvido = true, PosVendaStatusEnum novoStatus = PosVendaStatusEnum.Concluido)
        {
            var item = ObterPorId(id);
            if (item == null) return false;

            item.DataContatoRealizado = DateTime.Now;
            item.Resultado = resultado ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(responsavel))
            {
                item.Responsavel = responsavel;
            }
            item.Resolvido = resolvido;
            item.Status = novoStatus;

            Salvar(item);
            return true;
        }
    }
}
