using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public sealed class DviChecklistItem : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString("N");
        private string _categoria = string.Empty;
        private string _nome = string.Empty;
        private bool _okEntrada;
        private bool _okSaida;
        private string _observacoes = string.Empty;
        private string _fotoPath = string.Empty;

        public string Id { get => _id; set => SetField(ref _id, value); }
        public string Categoria { get => _categoria; set => SetField(ref _categoria, value); }
        public string Nome { get => _nome; set => SetField(ref _nome, value); }
        public bool OkEntrada { get => _okEntrada; set => SetField(ref _okEntrada, value); }
        public bool OkSaida { get => _okSaida; set => SetField(ref _okSaida, value); }
        public string Observacoes { get => _observacoes; set => SetField(ref _observacoes, value); }
        public string FotoPath { get => _fotoPath; set => SetField(ref _fotoPath, value); }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// Documento DVI local (JSON em AppData). Sem cloud approval.
    /// VÃ­nculo obrigatÃ³rio por OrdemServicoId; OrcamentoId opcional (espelho).
    /// </summary>
    public sealed class DviChecklistDocument
    {
        public Guid OrdemServicoId { get; set; }
        public Guid? OrcamentoId { get; set; }
        public DateTime AtualizadoEmUtc { get; set; } = DateTime.UtcNow;
        public List<DviChecklistItem> Itens { get; set; } = new();
    }

    public sealed class DviChecklistService
    {
        private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

        private static string Pasta()
        {
            var dir = Path.Combine(App.RuntimeAppDataPath, "Dvi");
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static string ArquivoOs(Guid ordemId) =>
            Path.Combine(Pasta(), $"os-{ordemId:N}.json");

        private static string ArquivoOsLegado(Guid ordemId) =>
            Path.Combine(Pasta(), $"{ordemId:N}.json");

        private static string ArquivoOrcamento(Guid orcamentoId) =>
            Path.Combine(Pasta(), $"orc-{orcamentoId:N}.json");

        public ObservableCollection<DviChecklistItem> CriarPadrao()
        {
            var itens = new OficinaProfissionalService().CriarChecklistPadrao()
                .Select(x => new DviChecklistItem
                {
                    Categoria = x.Categoria,
                    Nome = x.Nome,
                    Observacoes = string.Empty
                })
                .ToList();
            return new ObservableCollection<DviChecklistItem>(itens);
        }

        public ObservableCollection<DviChecklistItem> CarregarOuPadrao(Guid? ordemId)
        {
            if (ordemId.HasValue && ordemId.Value != Guid.Empty)
            {
                var doc = TentarLerDocumentoOs(ordemId.Value);
                if (doc?.Itens is { Count: > 0 })
                {
                    return new ObservableCollection<DviChecklistItem>(doc.Itens);
                }
            }

            return CriarPadrao();
        }

        public ObservableCollection<DviChecklistItem> CarregarPorOrcamentoOuPadrao(Guid? orcamentoId)
        {
            if (orcamentoId.HasValue && orcamentoId.Value != Guid.Empty)
            {
                var path = ArquivoOrcamento(orcamentoId.Value);
                if (File.Exists(path))
                {
                    try
                    {
                        var doc = JsonSerializer.Deserialize<DviChecklistDocument>(File.ReadAllText(path, Encoding.UTF8));
                        if (doc?.Itens is { Count: > 0 })
                        {
                            return new ObservableCollection<DviChecklistItem>(doc.Itens);
                        }
                    }
                    catch
                    {
                        // fallback padrao
                    }
                }
            }

            return CriarPadrao();
        }

        public DviChecklistDocument? TentarLerDocumentoOs(Guid ordemId)
        {
            foreach (var path in new[] { ArquivoOs(ordemId), ArquivoOsLegado(ordemId) })
            {
                if (!File.Exists(path)) continue;
                try
                {
                    var json = File.ReadAllText(path, Encoding.UTF8);
                    // Formato novo (documento)
                    if (json.Contains("\"Itens\"", StringComparison.Ordinal) || json.Contains("\"itens\"", StringComparison.Ordinal))
                    {
                        var doc = JsonSerializer.Deserialize<DviChecklistDocument>(json);
                        if (doc != null)
                        {
                            if (doc.OrdemServicoId == Guid.Empty) doc.OrdemServicoId = ordemId;
                            return doc;
                        }
                    }

                    // Legado: lista pura
                    var lista = JsonSerializer.Deserialize<List<DviChecklistItem>>(json);
                    if (lista != null && lista.Count > 0)
                    {
                        return new DviChecklistDocument
                        {
                            OrdemServicoId = ordemId,
                            Itens = lista,
                            AtualizadoEmUtc = File.GetLastWriteTimeUtc(path)
                        };
                    }
                }
                catch
                {
                    // tenta proximo path
                }
            }

            return null;
        }

        /// <summary>
        /// Persiste DVI ligado Ã  OS; se OrcamentoId informado, espelha sob chave do orÃ§amento (sem cloud).
        /// Remonta fotos pendentes cujo path ainda referencia um Guid temporÃ¡rio.
        /// </summary>
        public void Salvar(Guid ordemId, IEnumerable<DviChecklistItem> itens, Guid? orcamentoId = null, Guid? pendingMediaOrdemId = null)
        {
            if (ordemId == Guid.Empty) return;
            var lista = (itens ?? Enumerable.Empty<DviChecklistItem>()).ToList();

            if (pendingMediaOrdemId.HasValue
                && pendingMediaOrdemId.Value != Guid.Empty
                && pendingMediaOrdemId.Value != ordemId)
            {
                RemontarFotosPendentes(lista, pendingMediaOrdemId.Value, ordemId);
            }

            var doc = new DviChecklistDocument
            {
                OrdemServicoId = ordemId,
                OrcamentoId = orcamentoId is { } o && o != Guid.Empty ? o : null,
                AtualizadoEmUtc = DateTime.UtcNow,
                Itens = lista
            };

            var json = JsonSerializer.Serialize(doc, JsonOpts);
            File.WriteAllText(ArquivoOs(ordemId), json, Encoding.UTF8);

            // MantÃ©m path legado sincronizado para leitores antigos (PDF etc.)
            File.WriteAllText(ArquivoOsLegado(ordemId), JsonSerializer.Serialize(lista, JsonOpts), Encoding.UTF8);

            if (doc.OrcamentoId.HasValue)
            {
                File.WriteAllText(ArquivoOrcamento(doc.OrcamentoId.Value), json, Encoding.UTF8);
            }
        }

        /// <summary>Compat overload â€” sem orÃ§amento.</summary>
        public void Salvar(Guid ordemId, IEnumerable<DviChecklistItem> itens) =>
            Salvar(ordemId, itens, orcamentoId: null, pendingMediaOrdemId: null);

        private static void RemontarFotosPendentes(List<DviChecklistItem> itens, Guid pendingId, Guid ordemId)
        {
            var pendingToken = pendingId.ToString("N");
            var finalToken = ordemId.ToString("N");
            foreach (var item in itens)
            {
                if (string.IsNullOrWhiteSpace(item.FotoPath)) continue;
                if (!item.FotoPath.Contains(pendingToken, StringComparison.OrdinalIgnoreCase)) continue;
                if (!File.Exists(item.FotoPath)) continue;

                try
                {
                    var destDir = Path.GetDirectoryName(item.FotoPath) ?? OrdemServicoMediaService.GetManagedMediaDirectory();
                    Directory.CreateDirectory(destDir);
                    var fileName = Path.GetFileName(item.FotoPath).Replace(pendingToken, finalToken, StringComparison.OrdinalIgnoreCase);
                    var dest = Path.Combine(destDir, fileName);
                    if (!string.Equals(item.FotoPath, dest, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Copy(item.FotoPath, dest, overwrite: true);
                        item.FotoPath = dest;
                    }
                }
                catch
                {
                    // mantÃ©m path original se remount falhar
                }
            }
        }

        public string ResumoTexto(IEnumerable<DviChecklistItem> itens)
        {
            var sb = new StringBuilder();
            foreach (var item in itens ?? Enumerable.Empty<DviChecklistItem>())
            {
                var entrada = item.OkEntrada ? "OK" : "PEND";
                var saida = item.OkSaida ? "OK" : "PEND";
                sb.AppendLine($"{item.Categoria}/{item.Nome}: E={entrada} S={saida}" +
                              (string.IsNullOrWhiteSpace(item.Observacoes) ? string.Empty : $" | {item.Observacoes}") +
                              (string.IsNullOrWhiteSpace(item.FotoPath) ? string.Empty : $" | foto={Path.GetFileName(item.FotoPath)}"));
            }

            return sb.ToString().Trim();
        }


        /// <summary>
        /// Persiste DVI so pelo OrcamentoId (antes da OS). OrdemServicoId fica Empty ate converter.
        /// Sem cloud. Quando a OS salvar com o mesmo OrcamentoId, espelha/sobrescreve este arquivo.
        /// </summary>
        public void SalvarSomenteOrcamento(Guid orcamentoId, IEnumerable<DviChecklistItem> itens)
        {
            if (orcamentoId == Guid.Empty) return;
            var lista = (itens ?? Enumerable.Empty<DviChecklistItem>()).ToList();
            var doc = new DviChecklistDocument
            {
                OrdemServicoId = Guid.Empty,
                OrcamentoId = orcamentoId,
                AtualizadoEmUtc = DateTime.UtcNow,
                Itens = lista
            };
            File.WriteAllText(ArquivoOrcamento(orcamentoId), JsonSerializer.Serialize(doc, JsonOpts), Encoding.UTF8);
        }

        public bool ExisteParaOrcamento(Guid orcamentoId) =>
            orcamentoId != Guid.Empty && File.Exists(ArquivoOrcamento(orcamentoId));
        public bool ExisteParaOrdem(Guid ordemId) =>
            ordemId != Guid.Empty && (File.Exists(ArquivoOs(ordemId)) || File.Exists(ArquivoOsLegado(ordemId)));
    }
}
