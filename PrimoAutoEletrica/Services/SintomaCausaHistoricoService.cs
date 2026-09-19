using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public sealed class SintomaCausaRegistro
    {
        public DateTime Data { get; set; } = DateTime.Now;
        public string Sintoma { get; set; } = string.Empty;
        public string Causa { get; set; } = string.Empty;
        public string Resultado { get; set; } = string.Empty;
        public string Conclusao { get; set; } = string.Empty;
        public string RoteiroCodigo { get; set; } = string.Empty;
        public string Veiculo { get; set; } = string.Empty;
        public string Placa { get; set; } = string.Empty;
        public int Contagem { get; set; } = 1;
    }

    public sealed class SintomaCausaHistoricoService
    {
        private static string Arquivo()
        {
            var dir = Path.Combine(App.RuntimeAppDataPath, "AutoEletrica");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "sintoma-causa-historico.json");
        }

        public void Registrar(DiagnosticoGuiadoRoteiro? roteiro, ProntuarioEletricoVeiculo? prontuario)
        {
            if (roteiro == null) return;
            var sintoma = (roteiro.Sintoma ?? string.Empty).Trim();
            var causa = string.Join("; ", (roteiro.PossiveisCausas ?? new List<string>()).Take(3));
            if (string.IsNullOrWhiteSpace(sintoma) && string.IsNullOrWhiteSpace(roteiro.Resultado)) return;

            var lista = Carregar();
            var keySintoma = sintoma.ToLowerInvariant();
            var keyResultado = (roteiro.Resultado ?? string.Empty).Trim().ToLowerInvariant();
            var existente = lista.FirstOrDefault(x =>
                string.Equals(x.Sintoma, sintoma, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.Resultado ?? string.Empty, roteiro.Resultado ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            if (existente != null)
            {
                existente.Contagem++;
                existente.Data = DateTime.Now;
                existente.Conclusao = roteiro.Conclusao ?? existente.Conclusao;
                existente.Causa = string.IsNullOrWhiteSpace(causa) ? existente.Causa : causa;
            }
            else
            {
                lista.Add(new SintomaCausaRegistro
                {
                    Sintoma = sintoma,
                    Causa = causa,
                    Resultado = roteiro.Resultado ?? string.Empty,
                    Conclusao = roteiro.Conclusao ?? string.Empty,
                    RoteiroCodigo = roteiro.Codigo ?? string.Empty,
                    Veiculo = prontuario?.Veiculo ?? string.Empty,
                    Placa = prontuario?.Placa ?? string.Empty
                });
            }

            File.WriteAllText(Arquivo(), JsonSerializer.Serialize(lista.OrderByDescending(x => x.Contagem).ThenByDescending(x => x.Data).Take(500), new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
        }

        public IReadOnlyList<SintomaCausaRegistro> Top(int n = 20) =>
            Carregar().OrderByDescending(x => x.Contagem).ThenByDescending(x => x.Data).Take(n).ToList();

        private static List<SintomaCausaRegistro> Carregar()
        {
            var path = Arquivo();
            if (!File.Exists(path)) return new List<SintomaCausaRegistro>();
            try
            {
                return JsonSerializer.Deserialize<List<SintomaCausaRegistro>>(File.ReadAllText(path, Encoding.UTF8))
                       ?? new List<SintomaCausaRegistro>();
            }
            catch { return new List<SintomaCausaRegistro>(); }
        }
    }
}
