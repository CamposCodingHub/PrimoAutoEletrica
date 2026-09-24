using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Views
{
    public partial class PosVendaWindow : Window
    {
        private readonly Guid _ordemServicoId;
        private readonly Guid? _veiculoId;
        private readonly Guid? _clienteId;
        private readonly string _osNumero;
        private readonly string _clienteNome;
        private readonly string _veiculoPlaca;
        private readonly IPosVendaService _posVendaService;

        private List<PosVendaItem> _historico = new();
        private PosVendaItem? _itemSelecionado;

        public PosVendaWindow(
            Guid ordemServicoId,
            Guid? veiculoId = null,
            Guid? clienteId = null,
            string osNumero = "",
            string clienteNome = "",
            string veiculoPlaca = "",
            IPosVendaService? posVendaService = null)
        {
            InitializeComponent();

            _ordemServicoId = ordemServicoId;
            _veiculoId = veiculoId;
            _clienteId = clienteId;
            _osNumero = osNumero;
            _clienteNome = clienteNome;
            _veiculoPlaca = veiculoPlaca;
            _posVendaService = posVendaService ?? new PosVendaService();

            OsNumeroTextBlock.Text = string.IsNullOrWhiteSpace(osNumero) ? _ordemServicoId.ToString()[..8] : osNumero;
            ClienteNomeTextBlock.Text = string.IsNullOrWhiteSpace(clienteNome) ? "Cliente" : clienteNome;
            VeiculoPlacaTextBlock.Text = string.IsNullOrWhiteSpace(veiculoPlaca) ? "-" : veiculoPlaca.ToUpperInvariant();

            PopularCombos();
            RecarregarHistorico();

            if (_historico.Count > 0)
            {
                PosVendaDataGrid.SelectedIndex = 0;
            }
            else
            {
                PrepararNovoRegistro();
            }
        }

        private void PopularCombos()
        {
            TipoComboBox.ItemsSource = Enum.GetValues(typeof(PosVendaTipoEnum));
            TipoComboBox.SelectedItem = PosVendaTipoEnum.FollowUpPosServico;

            StatusComboBox.ItemsSource = Enum.GetValues(typeof(PosVendaStatusEnum));
            StatusComboBox.SelectedItem = PosVendaStatusEnum.Pendente;
        }

        private void RecarregarHistorico()
        {
            var itens = new List<PosVendaItem>();

            if (_ordemServicoId != Guid.Empty)
            {
                itens.AddRange(_posVendaService.ListarPorOrdemServicoId(_ordemServicoId));
            }

            if (_veiculoId.HasValue && _veiculoId.Value != Guid.Empty)
            {
                var doVeiculo = _posVendaService.ListarPorVeiculoId(_veiculoId.Value);
                foreach (var v in doVeiculo)
                {
                    if (!itens.Any(i => i.Id == v.Id))
                    {
                        itens.Add(v);
                    }
                }
            }

            if (_clienteId.HasValue && _clienteId.Value != Guid.Empty)
            {
                var doCliente = _posVendaService.ListarPorClienteId(_clienteId.Value);
                foreach (var c in doCliente)
                {
                    if (!itens.Any(i => i.Id == c.Id))
                    {
                        itens.Add(c);
                    }
                }
            }

            _historico = itens.OrderByDescending(i => i.DataCriacao).ToList();
            PosVendaDataGrid.ItemsSource = null;
            PosVendaDataGrid.ItemsSource = _historico;
        }

        private void PosVendaDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PosVendaDataGrid.SelectedItem is PosVendaItem item)
            {
                _itemSelecionado = item;
                CarregarItemNoFormulario(item);
            }
        }

        private void CarregarItemNoFormulario(PosVendaItem item)
        {
            TipoComboBox.SelectedItem = item.Tipo;
            StatusComboBox.SelectedItem = item.Status;
            DataPrevistaDatePicker.SelectedDate = item.DataPrevistaContato;
            DataRealizadaDatePicker.SelectedDate = item.DataContatoRealizado;
            ResponsavelTextBox.Text = item.Responsavel;
            ReincidenciaCheckBox.IsChecked = item.Reincidencia;
            DescricaoTextBox.Text = item.Observacoes;
            ResultadoTextBox.Text = item.Resultado;
        }

        private void PrepararNovoRegistro()
        {
            _itemSelecionado = null;
            PosVendaDataGrid.SelectedItem = null;

            TipoComboBox.SelectedItem = PosVendaTipoEnum.FollowUpPosServico;
            StatusComboBox.SelectedItem = PosVendaStatusEnum.Pendente;
            DataPrevistaDatePicker.SelectedDate = DateTime.Today.AddDays(7);
            DataRealizadaDatePicker.SelectedDate = null;
            ResponsavelTextBox.Text = "Atendimento";
            ReincidenciaCheckBox.IsChecked = false;
            DescricaoTextBox.Text = string.Empty;
            ResultadoTextBox.Text = string.Empty;
        }

        private void NovaOcorrenciaButton_Click(object sender, RoutedEventArgs e)
        {
            PrepararNovoRegistro();
        }

        private void SalvarOcorrenciaButton_Click(object sender, RoutedEventArgs e)
        {
            var clienteIdEfetivo = _clienteId ?? Guid.Empty;
            if (clienteIdEfetivo == Guid.Empty && _itemSelecionado != null)
            {
                clienteIdEfetivo = _itemSelecionado.ClienteId;
            }

            if (clienteIdEfetivo == Guid.Empty)
            {
                // Fallback de seguranca caso o context venha sem clienteId direto
                clienteIdEfetivo = Guid.NewGuid();
            }

            var item = _itemSelecionado ?? new PosVendaItem
            {
                Id = Guid.NewGuid(),
                OrdemServicoId = _ordemServicoId,
                VeiculoId = _veiculoId,
                ClienteId = clienteIdEfetivo,
                ClienteNomeSnapshot = _clienteNome,
                PlacaSnapshot = _veiculoPlaca,
                OsNumeroSnapshot = _osNumero,
                DataCriacao = DateTime.Now
            };

            item.Tipo = TipoComboBox.SelectedItem is PosVendaTipoEnum tipo ? tipo : PosVendaTipoEnum.FollowUpPosServico;
            item.Status = StatusComboBox.SelectedItem is PosVendaStatusEnum status ? status : PosVendaStatusEnum.Pendente;
            item.DataPrevistaContato = DataPrevistaDatePicker.SelectedDate ?? DateTime.Today.AddDays(7);
            item.DataContatoRealizado = DataRealizadaDatePicker.SelectedDate;
            item.Responsavel = ResponsavelTextBox.Text.Trim();
            item.Reincidencia = ReincidenciaCheckBox.IsChecked == true;
            item.Observacoes = DescricaoTextBox.Text.Trim();
            item.Resultado = ResultadoTextBox.Text.Trim();

            if (item.Status == PosVendaStatusEnum.Concluido)
            {
                item.Resolvido = true;
                if (!item.DataContatoRealizado.HasValue)
                {
                    item.DataContatoRealizado = DateTime.Now;
                }
            }

            _posVendaService.Salvar(item);
            _itemSelecionado = item;

            RecarregarHistorico();
            MessageBox.Show("Registro de pós-venda salvo com sucesso!", "Pós-Venda", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MarcarResolvidoButton_Click(object sender, RoutedEventArgs e)
        {
            StatusComboBox.SelectedItem = PosVendaStatusEnum.Concluido;
            DataRealizadaDatePicker.SelectedDate = DateTime.Today;

            if (string.IsNullOrWhiteSpace(ResultadoTextBox.Text))
            {
                ResultadoTextBox.Text = "Atendimento realizado com sucesso. Cliente confirmou funcionamento normal do veículo.";
            }

            SalvarOcorrenciaButton_Click(sender, e);
        }

        private void AnalisarDiagnosticoButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                $"Vinculando investigação técnica à OS #{_osNumero} (Veículo: {_veiculoPlaca}).\nNavegue até o módulo de Diagnóstico Técnico para consultar laudos periciais e roteiros D01 a D06.",
                "Análise Técnica de Diagnóstico",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
