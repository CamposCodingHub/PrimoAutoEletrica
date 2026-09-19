using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace PrimoAutoEletrica.Services
{
    public sealed class TwoFactorUsuarioRegistro
    {
        public int FuncionarioId { get; set; }
        public bool Enabled { get; set; }
        public string Secret { get; set; } = string.Empty;
    }

    public sealed class TwoFactorStoreService
    {
        private static string Arquivo()
        {
            var dir = Path.Combine(App.RuntimeAppDataPath, "Seguranca");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "2fa-usuarios.json");
        }

        public TwoFactorUsuarioRegistro? Obter(int funcionarioId) =>
            Carregar().FirstOrDefault(x => x.FuncionarioId == funcionarioId);

        public void Salvar(TwoFactorUsuarioRegistro registro)
        {
            var lista = Carregar();
            lista.RemoveAll(x => x.FuncionarioId == registro.FuncionarioId);
            lista.Add(registro);
            File.WriteAllText(Arquivo(), JsonSerializer.Serialize(lista, new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
        }

        public void Desativar(int funcionarioId)
        {
            var lista = Carregar();
            var item = lista.FirstOrDefault(x => x.FuncionarioId == funcionarioId);
            if (item == null) return;
            item.Enabled = false;
            File.WriteAllText(Arquivo(), JsonSerializer.Serialize(lista, new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
        }

        private static List<TwoFactorUsuarioRegistro> Carregar()
        {
            var path = Arquivo();
            if (!File.Exists(path)) return new List<TwoFactorUsuarioRegistro>();
            try
            {
                return JsonSerializer.Deserialize<List<TwoFactorUsuarioRegistro>>(File.ReadAllText(path, Encoding.UTF8))
                       ?? new List<TwoFactorUsuarioRegistro>();
            }
            catch { return new List<TwoFactorUsuarioRegistro>(); }
        }
    }
}
