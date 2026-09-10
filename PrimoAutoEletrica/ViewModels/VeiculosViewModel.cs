using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;
using PrimoAutoEletrica.Helpers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using PrimoAutoEletrica.UserControls;

namespace PrimoAutoEletrica.ViewModels
{
    public class VeiculosViewModel : INotifyPropertyChanged
    {
        private List<VeiculoViewModel> _allVeiculos = new();
        private List<VeiculoViewModel> _filteredVeiculos = new();
        private string _totalVeiculosText = "0";
        private string _veiculosLevesText = "0";
        private string _veiculosPesadosText = "0";
        private string _semProprietarioText = "0";
        private string _searchTerm = string.Empty;
        private string _selectedSistema = "Todos os sistemas";
        private string _selectedTipo = "Todos os tipos";
        private string _resultCountText = "0 veiculos encontrados";

        public List<VeiculoViewModel> AllVeiculos
        {
            get => _allVeiculos;
            set => SetField(ref _allVeiculos, value);
        }

        public List<VeiculoViewModel> FilteredVeiculos
        {
            get => _filteredVeiculos;
            set => SetField(ref _filteredVeiculos, value);
        }

        public string TotalVeiculosText
        {
            get => _totalVeiculosText;
            set => SetField(ref _totalVeiculosText, value);
        }

        public string VeiculosLevesText
        {
            get => _veiculosLevesText;
            set => SetField(ref _veiculosLevesText, value);
        }

        public string VeiculosPesadosText
        {
            get => _veiculosPesadosText;
            set => SetField(ref _veiculosPesadosText, value);
        }

        public string SemProprietarioText
        {
            get => _semProprietarioText;
            set => SetField(ref _semProprietarioText, value);
        }

        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                if (SetField(ref _searchTerm, value))
                {
                    ApplyFilters(_searchTerm, _selectedSistema, _selectedTipo);
                }
            }
        }

        public string SelectedSistema
        {
            get => _selectedSistema;
            set
            {
                if (SetField(ref _selectedSistema, value))
                {
                    ApplyFilters(_searchTerm, _selectedSistema, _selectedTipo);
                }
            }
        }

        public string SelectedTipo
        {
            get => _selectedTipo;
            set
            {
                if (SetField(ref _selectedTipo, value))
                {
                    ApplyFilters(_searchTerm, _selectedSistema, _selectedTipo);
                }
            }
        }

        public string ResultCountText
        {
            get => _resultCountText;
            set => SetField(ref _resultCountText, value);
        }

        public void LoadVeiculos()
        {
            var clientes = App.Repositories.Clientes.ObterTodos().ToDictionary(c => c.Id, c => c);

            var ordens = App.Repositories.OrdensServico.ObterTodos(true);
            var agendamentos = new AgendamentoDatabaseService().ObterTodosAgendamentos();
            var orcamentos = new OrcamentoDatabaseService().ObterTodosOrcamentos();

            AllVeiculos = App.Repositories.Clientes.ObterTodosVeiculos()
                .Select(v => new VeiculoViewModel(v, clientes, ordens, agendamentos, orcamentos))
                .OrderByDescending(v => v.TemAlertaTecnico)
                .ThenByDescending(v => v.RetornoProximo)
                .ThenBy(v => v.MarcaModelo)
                .ToList();

            UpdateIndicators();
            ApplyFilters(string.Empty, "Todos os sistemas", "Todos os tipos");
        }

        public void ApplyFilters(string busca, string filtroSistema, string filtroTipo)
        {
            IEnumerable<VeiculoViewModel> resultados = AllVeiculos;

            if (!string.IsNullOrWhiteSpace(busca))
            {
                resultados = resultados.Where(v =>
                    v.PlacaFormatada.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    v.MarcaModelo.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    v.NomeCliente.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    v.AlertaResumo.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    (v.Veiculo.ProblemaRecorrente?.Contains(busca, StringComparison.OrdinalIgnoreCase) == true) ||
                    (v.Veiculo.ObservacoesEletricasRecorrentes?.Contains(busca, StringComparison.OrdinalIgnoreCase) == true));
            }

            if (!string.Equals(filtroSistema, "Todos os sistemas", StringComparison.OrdinalIgnoreCase))
            {
                resultados = resultados.Where(v => string.Equals(v.Veiculo.SistemaEletrico, filtroSistema, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.Equals(filtroTipo, "Todos os tipos", StringComparison.OrdinalIgnoreCase))
            {
                resultados = resultados.Where(v => string.Equals(v.TipoVeiculo, filtroTipo, StringComparison.OrdinalIgnoreCase));
            }

            var lista = resultados.ToList();
            FilteredVeiculos = lista;
            ResultCountText = lista.Count == 1
                ? UiText.T("VehiclesFoundOne")
                : UiText.T("VehiclesFoundFormat", lista.Count);
        }

        public void ResetFilters()
        {
            SearchTerm = string.Empty;
            SelectedSistema = "Todos os sistemas";
            SelectedTipo = "Todos os tipos";
        }

        private void UpdateIndicators()
        {
            TotalVeiculosText = AllVeiculos.Count.ToString();
            VeiculosLevesText = AllVeiculos.Count(v => v.RetornoProximo).ToString();
            VeiculosPesadosText = AllVeiculos.Count(v => v.GarantiaAtiva).ToString();
            SemProprietarioText = AllVeiculos.Count(v => v.TemAlertaTecnico).ToString();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}

