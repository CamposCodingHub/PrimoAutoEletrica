using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Views
{
    public sealed class ComissaoSettlementWindow : Window
    {
        private readonly ObservableCollection<ComissaoLinhaUi> _linhas = new();
        private readonly DatePicker _inicio = new() { SelectedDate = DateTime.Today.AddDays(-30), Margin = new Thickness(0, 0, 8, 0), Width = 140 };
        private readonly DatePicker _fim = new() { SelectedDate = DateTime.Today, Margin = new Thickness(0, 0, 8, 0), Width = 140 };
        private readonly TextBox _percentual = new() { Text = "10", Width = 60, Margin = new Thickness(0, 0, 8, 0) };
        private readonly DataGrid _grid = new() { AutoGenerateColumns = true, IsReadOnly = false, Margin = new Thickness(0, 12, 0, 12) };

        public ComissaoSettlementWindow()
        {
            Title = "Fechamento de comissao - tecnicos";
            Width = 900;
            Height = 560;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var root = new DockPanel { Margin = new Thickness(16) };

            var top = new StackPanel { Orientation = Orientation.Horizontal };
            top.Children.Add(new TextBlock { Text = "De", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
            top.Children.Add(_inicio);
            top.Children.Add(new TextBlock { Text = "Ate", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
            top.Children.Add(_fim);
            top.Children.Add(new TextBlock { Text = "%", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 6, 0) });
            top.Children.Add(_percentual);
            var calc = new Button { Content = "Calcular", Width = 100, Margin = new Thickness(0, 0, 8, 0) };
            calc.Click += (_, _) => Calcular();
            top.Children.Add(calc);
            var export = new Button { Content = "Exportar CSV", Width = 120, Margin = new Thickness(0, 0, 8, 0) };
            export.Click += (_, _) => Exportar();
            top.Children.Add(export);
            var salvar = new Button { Content = "Salvar status", Width = 120 };
            salvar.Click += (_, _) => SalvarStatus();
            top.Children.Add(salvar);
            DockPanel.SetDock(top, Dock.Top);
            root.Children.Add(top);

            _grid.ItemsSource = _linhas;
            root.Children.Add(_grid);
            Content = root;
            Loaded += (_, _) => Calcular();
        }

        private void Calcular()
        {
            _linhas.Clear();
            var ini = _inicio.SelectedDate ?? DateTime.Today.AddDays(-30);
            var fim = _fim.SelectedDate ?? DateTime.Today;
            if (!decimal.TryParse(_percentual.Text?.Replace('%', ' ').Trim(), out var pct) || pct <= 0)
            {
                pct = 10m;
            }

            if (pct > 1) pct /= 100m;
            var calc = new ComissaoSettlementService().Calcular(ini, fim, pct);
            var statusMap = CarregarStatus();
            foreach (var l in calc)
            {
                statusMap.TryGetValue(Chave(l.Tecnico, ini, fim), out var st);
                _linhas.Add(new ComissaoLinhaUi
                {
                    Tecnico = l.Tecnico,
                    QuantidadeOs = l.QuantidadeOs,
                    TotalServicos = l.TotalServicos,
                    TotalPecas = l.TotalPecas,
                    BaseCalculo = l.BaseCalculo,
                    Percentual = l.Percentual,
                    Comissao = l.Comissao,
                    Status = string.IsNullOrWhiteSpace(st) ? "Pendente" : st
                });
            }
        }

        private void Exportar()
        {
            var ini = _inicio.SelectedDate ?? DateTime.Today.AddDays(-30);
            var fim = _fim.SelectedDate ?? DateTime.Today;
            var dir = Path.Combine(App.RuntimeAppDataPath, "Comercial");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"comissao-fechamento-{ini:yyyyMMdd}-{fim:yyyyMMdd}.csv");
            var linhas = _linhas.Select(l => new ComissaoLinha
            {
                Tecnico = l.Tecnico,
                QuantidadeOs = l.QuantidadeOs,
                TotalServicos = l.TotalServicos,
                TotalPecas = l.TotalPecas,
                BaseCalculo = l.BaseCalculo,
                Percentual = l.Percentual,
                Comissao = l.Comissao
            });
            new ComissaoSettlementService().ExportarCsv(linhas, path);
            SecureProcessLauncher.OpenFileOrDirectory(path);
            MessageBox.Show(path, "CSV", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SalvarStatus()
        {
            var ini = _inicio.SelectedDate ?? DateTime.Today.AddDays(-30);
            var fim = _fim.SelectedDate ?? DateTime.Today;
            var map = CarregarStatus();
            foreach (var l in _linhas)
            {
                map[Chave(l.Tecnico, ini, fim)] = string.IsNullOrWhiteSpace(l.Status) ? "Pendente" : l.Status.Trim();
            }

            var path = ArquivoStatus();
            File.WriteAllText(path, JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true }));
            MessageBox.Show("Status de comissao salvo.", "Comissao", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static string Chave(string tecnico, DateTime ini, DateTime fim) =>
            $"{tecnico}|{ini:yyyyMMdd}|{fim:yyyyMMdd}";

        private static string ArquivoStatus()
        {
            var dir = Path.Combine(App.RuntimeAppDataPath, "Comercial");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "comissao-status.json");
        }

        private static Dictionary<string, string> CarregarStatus()
        {
            var path = ArquivoStatus();
            if (!File.Exists(path)) return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))
                       ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private sealed class ComissaoLinhaUi
        {
            public string Tecnico { get; set; } = string.Empty;
            public int QuantidadeOs { get; set; }
            public decimal TotalServicos { get; set; }
            public decimal TotalPecas { get; set; }
            public decimal BaseCalculo { get; set; }
            public decimal Percentual { get; set; }
            public decimal Comissao { get; set; }
            public string Status { get; set; } = "Pendente";
        }
    }
}
