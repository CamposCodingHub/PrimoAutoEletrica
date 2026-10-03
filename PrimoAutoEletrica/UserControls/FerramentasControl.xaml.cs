using Microsoft.Extensions.DependencyInjection;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.UserControls
{
    public partial class FerramentasControl : UserControl
    {
        private readonly IFerramentaService _ferramentaService;
        private bool _isLoaded;

        public FerramentasControl()
        {
            InitializeComponent();

            _ferramentaService = App.Services.GetService<IFerramentaService>()
                ?? new FerramentaService(App.Database, App.Logger);

            Loaded += FerramentasControl_Loaded;
        }

        private async void FerramentasControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isLoaded) return;
            _isLoaded = true;

            InicializarFiltros();
            await CarregarDadosAsync();
        }

        private void InicializarFiltros()
        {
            CategoriaComboBox.Items.Clear();
            CategoriaComboBox.Items.Add(new ComboBoxItem { Content = "Todas as Categorias", Tag = null });
            CategoriaComboBox.Items.Add(new ComboBoxItem { Content = "Diagnóstico Eletrônico", Tag = CategoriaFerramenta.DiagnosticoEletronico });
            CategoriaComboBox.Items.Add(new ComboBoxItem { Content = "Medição Elétrica", Tag = CategoriaFerramenta.MedicaoEletrica });
            CategoriaComboBox.Items.Add(new ComboBoxItem { Content = "Baterias & Carga", Tag = CategoriaFerramenta.BateriasECarga });
            CategoriaComboBox.Items.Add(new ComboBoxItem { Content = "Elétrica & Montagem", Tag = CategoriaFerramenta.EletricaEMontagem });
            CategoriaComboBox.Items.Add(new ComboBoxItem { Content = "Mecânica Geral", Tag = CategoriaFerramenta.MecanicaGeral });
            CategoriaComboBox.SelectedIndex = 0;

            StatusComboBox.Items.Clear();
            StatusComboBox.Items.Add(new ComboBoxItem { Content = "Todos os Status", Tag = null });
            StatusComboBox.Items.Add(new ComboBoxItem { Content = "Disponíveis no Armário", Tag = StatusFerramenta.Disponivel });
            StatusComboBox.Items.Add(new ComboBoxItem { Content = "Em Uso nas Bancadas", Tag = StatusFerramenta.EmUso });
            StatusComboBox.Items.Add(new ComboBoxItem { Content = "Em Manutenção", Tag = StatusFerramenta.EmManutencao });
            StatusComboBox.Items.Add(new ComboBoxItem { Content = "Avariadas / Atenção", Tag = StatusFerramenta.Avariada });
            StatusComboBox.Items.Add(new ComboBoxItem { Content = "Extraviadas", Tag = StatusFerramenta.Extraviada });
            StatusComboBox.SelectedIndex = 0;
        }

        public async Task CarregarDadosAsync()
        {
            try
            {
                LoadingPanel.Visibility = Visibility.Visible;
                CardsScrollViewer.Visibility = Visibility.Collapsed;
                GridContainerBorder.Visibility = Visibility.Collapsed;
                EmptyPanel.Visibility = Visibility.Collapsed;

                var termo = BuscaTextBox.Text?.Trim();
                var categoria = (CategoriaComboBox.SelectedItem as ComboBoxItem)?.Tag as CategoriaFerramenta?;
                var status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Tag as StatusFerramenta?;

                var tarefas = new Task[]
                {
                    Task.Run(async () =>
                    {
                        var ferramentas = await _ferramentaService.ListarFerramentasAsync(termo, categoria, status);
                        var resumo = await _ferramentaService.ObterResumoAsync();

                        await Dispatcher.InvokeAsync(() =>
                        {
                            TotalFerramentasText.Text = $"{resumo.TotalFerramentas} equipamentos";
                            DisponiveisText.Text = resumo.Disponiveis.ToString();
                            EmUsoText.Text = resumo.EmUso.ToString();
                            ManutencaoText.Text = resumo.EmManutencao.ToString();
                            AvariadasText.Text = resumo.Avariadas.ToString();

                            FerramentasItemsControl.ItemsSource = ferramentas;
                            FerramentasDataGrid.ItemsSource = ferramentas;

                            bool temItens = ferramentas.Count > 0;
                            EmptyPanel.Visibility = temItens ? Visibility.Collapsed : Visibility.Visible;

                            AtualizarVisibilidadeModo(temItens);
                        });
                    })
                };

                await Task.WhenAll(tarefas);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar catálogo de ferramentas:\n{ex.Message}", "Ferramentaria", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void AtualizarVisibilidadeModo(bool temItens)
        {
            if (!temItens)
            {
                CardsScrollViewer.Visibility = Visibility.Collapsed;
                GridContainerBorder.Visibility = Visibility.Collapsed;
                return;
            }

            if (ViewCardsRadio.IsChecked == true)
            {
                CardsScrollViewer.Visibility = Visibility.Visible;
                GridContainerBorder.Visibility = Visibility.Collapsed;
            }
            else
            {
                CardsScrollViewer.Visibility = Visibility.Collapsed;
                GridContainerBorder.Visibility = Visibility.Visible;
            }
        }

        private async void BuscaTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isLoaded) return;
            await CarregarDadosAsync();
        }

        private async void Filtros_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            await CarregarDadosAsync();
        }

        private void ViewMode_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;
            var temItens = FerramentasDataGrid.Items.Count > 0;
            AtualizarVisibilidadeModo(temItens);
        }

        private async void LimparFiltrosButton_Click(object sender, RoutedEventArgs e)
        {
            BuscaTextBox.Text = string.Empty;
            CategoriaComboBox.SelectedIndex = 0;
            StatusComboBox.SelectedIndex = 0;
            await CarregarDadosAsync();
        }

        private async void AtualizarButton_Click(object sender, RoutedEventArgs e)
        {
            await CarregarDadosAsync();
        }

        private async void EmprestarDevolverButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new EmprestarDevolverFerramentaDialog(null)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                await CarregarDadosAsync();
            }
        }

        private async void CardAcaoButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is Ferramenta ferramenta)
            {
                var dialog = new EmprestarDevolverFerramentaDialog(ferramenta.Id)
                {
                    Owner = Window.GetWindow(this)
                };

                if (dialog.ShowDialog() == true)
                {
                    await CarregarDadosAsync();
                }
            }
        }

        private async void NovaFerramentaButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CadastroFerramentaDialog(null)
            {
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
            {
                await CarregarDadosAsync();
            }
        }

        private async void CardEditarButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is Ferramenta ferramenta)
            {
                var dialog = new CadastroFerramentaDialog(ferramenta)
                {
                    Owner = Window.GetWindow(this)
                };

                if (dialog.ShowDialog() == true)
                {
                    await CarregarDadosAsync();
                }
            }
        }

        private void HistoricoMovimentacoesButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new HistoricoFerramentasDialog()
            {
                Owner = Window.GetWindow(this)
            };
            dialog.ShowDialog();
        }
    }
}
