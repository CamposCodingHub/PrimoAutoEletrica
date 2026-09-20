using System;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Win32;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Views
{
    /// <summary>
    /// DVI editavel no orcamento (JSON local orc-{id}.json). Sem cloud.
    /// </summary>
    public partial class DviOrcamentoWindow : Window
    {
        private readonly Guid _orcamentoId;
        private readonly string _numero;
        private readonly DviChecklistService _dviService = new();
        private readonly ObservableCollection<DviChecklistItem> _dviItens = new();

        public DviOrcamentoWindow(Guid orcamentoId, string? numeroOrcamento = null)
        {
            InitializeComponent();
            _orcamentoId = orcamentoId;
            _numero = string.IsNullOrWhiteSpace(numeroOrcamento) ? orcamentoId.ToString("N")[..8] : numeroOrcamento!;
            DviItemsControl.ItemsSource = _dviItens;
            SubtituloTextBlock.Text = $"Orcamento {_numero} - checklist local (sem cloud).";
            Carregar();
        }

        private void Carregar()
        {
            _dviItens.Clear();
            foreach (var item in _dviService.CarregarPorOrcamentoOuPadrao(_orcamentoId))
            {
                _dviItens.Add(item);
            }
        }

        private void DviCarregarPadraoButton_Click(object sender, RoutedEventArgs e)
        {
            _dviItens.Clear();
            foreach (var item in _dviService.CriarPadrao())
            {
                _dviItens.Add(item);
            }
        }

        private void DviMarcarEntradaOkButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _dviItens)
            {
                item.OkEntrada = true;
            }
        }

        private void DviFotoItemButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not System.Windows.Controls.Button btn || btn.Tag is not DviChecklistItem item)
                {
                    return;
                }

                var dlg = new OpenFileDialog
                {
                    Filter = OrdemServicoMediaService.SupportedImageFilter,
                    Title = $"Foto DVI - {item.Nome}"
                };
                if (dlg.ShowDialog(this) != true) return;

                var path = OrdemServicoMediaService.PersistSelectedImage(
                    dlg.FileName,
                    _orcamentoId,
                    _numero,
                    "dvi-orc");
                item.FotoPath = path;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Foto DVI", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_orcamentoId == Guid.Empty)
                {
                    MessageBox.Show("Orcamento sem Id. Salve o orcamento antes do DVI.", "DVI", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _dviService.SalvarSomenteOrcamento(_orcamentoId, _dviItens);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "DVI", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}