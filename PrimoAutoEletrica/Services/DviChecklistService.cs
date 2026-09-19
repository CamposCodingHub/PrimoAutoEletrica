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

    public sealed class DviChecklistService
    {
        private static string Pasta()
        {
            var dir = Path.Combine(App.RuntimeAppDataPath, "Dvi");
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static string Arquivo(Guid ordemId) =>
            Path.Combine(Pasta(), $"{ordemId:N}.json");

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
                var path = Arquivo(ordemId.Value);
                if (File.Exists(path))
                {
                    try
                    {
                        var json = File.ReadAllText(path, Encoding.UTF8);
                        var lista = JsonSerializer.Deserialize<List<DviChecklistItem>>(json);
                        if (lista != null && lista.Count > 0)
                        {
                            return new ObservableCollection<DviChecklistItem>(lista);
                        }
                    }
                    catch
                    {
                    }
                }
            }

            return CriarPadrao();
        }

        public void Salvar(Guid ordemId, IEnumerable<DviChecklistItem> itens)
        {
            if (ordemId == Guid.Empty) return;
            var lista = (itens ?? Enumerable.Empty<DviChecklistItem>()).ToList();
            File.WriteAllText(
                Arquivo(ordemId),
                JsonSerializer.Serialize(lista, new JsonSerializerOptions { WriteIndented = true }),
                Encoding.UTF8);
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
    }
}
