using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Views
{
    /// <summary>
    /// Lista local de lembretes pós-venda / revisão.
    /// Sem envio WhatsApp Cloud — apenas marcar tratado localmente.
    /// </summary>
    public sealed class LembretesRevisaoWindow : Window
    {
        private readonly DataGrid _grid = new()
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            Margin = new Thickness(0, 12, 0, 0),
            SelectionMode = DataGridSelectionMode.Single
        };

        private readonly CheckBox _somentePendentes = new()
        {
            Content = "Somente pendentes",
            IsChecked = true,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0)
        };

        private readonly TextBlock _resumo = new()
        {
            Margin = new Thickness(0, 8, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };

        public LembretesRevisaoWindow()
        {
            Title = "Lembretes de revisao (local)";
            Width = 980;
            Height = 560;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            MontarColunas();

            var root = new DockPanel { Margin = new Thickness(16) };
            var top = new StackPanel { Orientation = Orientation.Horizontal };

            var refresh = new Button { Content = "Atualizar", Width = 100, Margin = new Thickness(0, 0, 8, 0) };
            refresh.Click += (_, _) => Carregar();
            top.Children.Add(refresh);

            _somentePendentes.Checked += (_, _) => Carregar();
            _somentePendentes.Unchecked += (_, _) => Carregar();
            top.Children.Add(_somentePendentes);

            var tratar = new Button { Content = "Marcar tratado (local)", Width = 180, Margin = new Thickness(0, 0, 8, 0) };
            tratar.Click += (_, _) => MarcarTratado();
            top.Children.Add(tratar);

            var reabrir = new Button { Content = "Reabrir pendente", Width = 140, Margin = new Thickness(0, 0, 8, 0) };
            reabrir.Click += (_, _) => Reabrir();
            top.Children.Add(reabrir);

            var aviso = new TextBlock
            {
                Text = "Sem WhatsApp Cloud — so lista local.",
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0.75
            };
            top.Children.Add(aviso);

            DockPanel.SetDock(top, Dock.Top);
            root.Children.Add(top);

            DockPanel.SetDock(_resumo, Dock.Bottom);
            root.Children.Add(_resumo);
            root.Children.Add(_grid);
            Content = root;
            Loaded += (_, _) => Carregar();
        }

        private void MontarColunas()
        {
            _grid.Columns.Add(new DataGridTextColumn { Header = "Data", Binding = new System.Windows.Data.Binding("DataLembrete") { StringFormat = "dd/MM/yyyy" }, Width = 100 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "Cliente", Binding = new System.Windows.Data.Binding("ClienteNome"), Width = 160 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "Veiculo", Binding = new System.Windows.Data.Binding("Veiculo"), Width = 140 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "OS", Binding = new System.Windows.Data.Binding("OrigemOs"), Width = 90 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "Telefone", Binding = new System.Windows.Data.Binding("Telefone"), Width = 110 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "Motivo", Binding = new System.Windows.Data.Binding("Motivo"), Width = 160 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "Status", Binding = new System.Windows.Data.Binding("StatusLocal"), Width = 110 });
            _grid.Columns.Add(new DataGridTextColumn { Header = "Obs local", Binding = new System.Windows.Data.Binding("ObservacaoLocal"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        }

        private void Carregar()
        {
            var svc = new LembreteRevisaoService();
            var somentePend = _somentePendentes.IsChecked == true;
            var itens = svc.ListarOrdenados(somentePendentes: somentePend);
            var vencidos = svc.VencidosOuHoje();

            _grid.ItemsSource = itens.Select(x => new LinhaLembrete
            {
                Id = x.Id,
                DataLembrete = x.DataLembrete,
                ClienteNome = x.ClienteNome,
                Veiculo = x.Veiculo,
                OrigemOs = x.OrigemOs,
                Telefone = x.Telefone,
                Motivo = x.Motivo,
                StatusLocal = x.Enviado ? "Tratado local" : (x.DataLembrete.Date <= DateTime.Today ? "Vencido/hoje" : "Agendado"),
                ObservacaoLocal = x.ObservacaoLocal,
                Enviado = x.Enviado
            }).ToList();

            _resumo.Text =
                $"Total na vista: {itens.Count} · Pendentes vencidos/hoje: {vencidos.Count} · " +
                "Acao 'Marcar tratado' NAO envia WhatsApp.";
        }

        private void MarcarTratado()
        {
            if (_grid.SelectedItem is not LinhaLembrete row)
            {
                MessageBox.Show("Selecione um lembrete.", "Lembretes", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (row.Enviado)
            {
                MessageBox.Show("Este lembrete ja esta tratado localmente.", "Lembretes", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var ok = new LembreteRevisaoService().MarcarTratadoLocalmente(row.Id);
            if (!ok)
            {
                MessageBox.Show("Nao foi possivel marcar o lembrete.", "Lembretes", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Carregar();
        }

        private void Reabrir()
        {
            if (_grid.SelectedItem is not LinhaLembrete row)
            {
                MessageBox.Show("Selecione um lembrete.", "Lembretes", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!row.Enviado)
            {
                MessageBox.Show("Este lembrete ja esta pendente.", "Lembretes", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            new LembreteRevisaoService().ReabrirPendente(row.Id);
            Carregar();
        }

        private sealed class LinhaLembrete
        {
            public Guid Id { get; set; }
            public DateTime DataLembrete { get; set; }
            public string ClienteNome { get; set; } = string.Empty;
            public string Veiculo { get; set; } = string.Empty;
            public string OrigemOs { get; set; } = string.Empty;
            public string Telefone { get; set; } = string.Empty;
            public string Motivo { get; set; } = string.Empty;
            public string StatusLocal { get; set; } = string.Empty;
            public string ObservacaoLocal { get; set; } = string.Empty;
            public bool Enviado { get; set; }
        }
    }
}
