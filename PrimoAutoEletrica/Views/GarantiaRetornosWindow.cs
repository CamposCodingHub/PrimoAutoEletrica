using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Views
{
    public sealed class GarantiaRetornosWindow : Window
    {
        private readonly DataGrid _grid = new() { AutoGenerateColumns = true, IsReadOnly = true, Margin = new Thickness(0, 12, 0, 0) };

        public GarantiaRetornosWindow()
        {
            Title = "Retornos em garantia";
            Width = 920;
            Height = 520;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var root = new DockPanel { Margin = new Thickness(16) };
            var top = new StackPanel { Orientation = Orientation.Horizontal };
            var refresh = new Button { Content = "Atualizar", Width = 100, Margin = new Thickness(0, 0, 8, 0) };
            refresh.Click += (_, _) => Carregar();
            top.Children.Add(refresh);
            var export = new Button { Content = "Exportar CSV", Width = 120, Margin = new Thickness(0, 0, 8, 0) };
            export.Click += (_, _) => Exportar();
            top.Children.Add(export);
            var marcar = new Button { Content = "Marcar reincidencia", Width = 160 };
            marcar.Click += (_, _) => MarcarReincidencia();
            top.Children.Add(marcar);
            DockPanel.SetDock(top, Dock.Top);
            root.Children.Add(top);
            root.Children.Add(_grid);
            Content = root;
            Loaded += (_, _) => Carregar();
        }

        private void Carregar()
        {
            var hoje = DateTime.Today;
            var linhas = App.Repositories.OrdensServico.ObterTodos()
                .Where(o => o.Ativo && o.GarantiaValidaAte.HasValue && o.GarantiaValidaAte.Value.Date >= hoje)
                .OrderBy(o => o.GarantiaValidaAte)
                .Select(o => new
                {
                    o.Numero,
                    Cliente = o.ClienteNomeSnapshot,
                    Veiculo = o.VeiculoDescricaoSnapshot,
                    Placa = o.PlacaSnapshot,
                    Validade = o.GarantiaValidaAte,
                    Problema = o.ProblemaRelatado,
                    Status = o.Status,
                    Reincidencia = EhReincidencia(o.Id) ? "Sim" : "Nao"
                })
                .ToList();
            _grid.ItemsSource = linhas;
        }

        private void Exportar()
        {
            var dir = Path.Combine(App.RuntimeAppDataPath, "Comercial");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"garantia-retornos-{DateTime.Now:yyyyMMddHHmmss}.csv");
            var sb = new StringBuilder();
            sb.AppendLine("Numero;Cliente;Veiculo;Placa;Validade;Status;Reincidencia;Problema");
            foreach (dynamic row in _grid.ItemsSource ?? Array.Empty<object>())
            {
                sb.AppendLine($"{row.Numero};{row.Cliente};{row.Veiculo};{row.Placa};{row.Validade:dd/MM/yyyy};{row.Status};{row.Reincidencia};{((string)row.Problema)?.Replace(';', ',')}");
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            SecureProcessLauncher.OpenFileOrDirectory(path);
        }

        private void MarcarReincidencia()
        {
            if (_grid.SelectedItem == null)
            {
                MessageBox.Show("Selecione uma OS.", "Garantia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            dynamic row = _grid.SelectedItem;
            string numero = row.Numero;
            var ordem = App.Repositories.OrdensServico.ObterTodos().FirstOrDefault(o => o.Numero == numero);
            if (ordem == null) return;
            var set = CarregarReincidencias();
            set.Add(ordem.Id.ToString());
            SalvarReincidencias(set);
            MessageBox.Show($"OS {numero} marcada como reincidencia.", "Garantia", MessageBoxButton.OK, MessageBoxImage.Information);
            Carregar();
        }

        private static bool EhReincidencia(Guid id)
        {
            return CarregarReincidencias().Contains(id.ToString());
        }

        private static HashSet<string> CarregarReincidencias()
        {
            var path = Arquivo();
            if (!File.Exists(path)) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                return JsonSerializer.Deserialize<HashSet<string>>(File.ReadAllText(path))
                       ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            catch { return new HashSet<string>(StringComparer.OrdinalIgnoreCase); }
        }

        private static void SalvarReincidencias(HashSet<string> set)
        {
            File.WriteAllText(Arquivo(), JsonSerializer.Serialize(set.ToList(), new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
        }

        private static string Arquivo()
        {
            var dir = Path.Combine(App.RuntimeAppDataPath, "Comercial");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "garantia-reincidencias.json");
        }
    }
}
