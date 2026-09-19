using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public sealed class AutoEletricaRoteiroPersistencia
    {
        public string Codigo { get; set; } = string.Empty;
        public string Resultado { get; set; } = string.Empty;
        public string Conclusao { get; set; } = string.Empty;
        public DateTime AtualizadoEm { get; set; }
    }

    public sealed class AutoEletricaRoteiroPersistService
    {
        private static string Arquivo()
        {
            var dir = Path.Combine(App.RuntimeAppDataPath, "AutoEletrica");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "roteiros-resultados.json");
        }

        public void SalvarResultado(DiagnosticoGuiadoRoteiro roteiro)
        {
            if (roteiro == null || string.IsNullOrWhiteSpace(roteiro.Codigo)) return;
            var lista = Carregar();
            lista.RemoveAll(x => string.Equals(x.Codigo, roteiro.Codigo, StringComparison.OrdinalIgnoreCase));
            lista.Add(new AutoEletricaRoteiroPersistencia
            {
                Codigo = roteiro.Codigo,
                Resultado = roteiro.Resultado ?? string.Empty,
                Conclusao = roteiro.Conclusao ?? string.Empty,
                AtualizadoEm = DateTime.Now
            });
            File.WriteAllText(Arquivo(), JsonSerializer.Serialize(lista, new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
        }

        public void AplicarEm(IEnumerable<DiagnosticoGuiadoRoteiro> roteiros)
        {
            var mapa = Carregar().ToDictionary(x => x.Codigo, StringComparer.OrdinalIgnoreCase);
            foreach (var r in roteiros)
            {
                if (mapa.TryGetValue(r.Codigo, out var saved))
                {
                    if (!string.IsNullOrWhiteSpace(saved.Resultado)) r.Resultado = saved.Resultado;
                    if (!string.IsNullOrWhiteSpace(saved.Conclusao)) r.Conclusao = saved.Conclusao;
                }
            }
        }

        private static List<AutoEletricaRoteiroPersistencia> Carregar()
        {
            var path = Arquivo();
            if (!File.Exists(path)) return new List<AutoEletricaRoteiroPersistencia>();
            try
            {
                return JsonSerializer.Deserialize<List<AutoEletricaRoteiroPersistencia>>(File.ReadAllText(path, Encoding.UTF8))
                       ?? new List<AutoEletricaRoteiroPersistencia>();
            }
            catch { return new List<AutoEletricaRoteiroPersistencia>(); }
        }
    }
}
