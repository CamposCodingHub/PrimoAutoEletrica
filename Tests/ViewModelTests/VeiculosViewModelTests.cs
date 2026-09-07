using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.UserControls;
using PrimoAutoEletrica.ViewModels;
using System.Collections.Generic;
using Xunit;

namespace PrimoAutoEletrica.Tests.ViewModelTests
{
    public class VeiculosViewModelTests
    {
        [Fact]
        public void ApplyFilters_SearchTermUpdatesFilteredListAndCountText()
        {
            var viewModel = new VeiculosViewModel
            {
                AllVeiculos = new List<VeiculoViewModel>
                {
                    new VeiculoViewModel(
                        new Veiculo { Placa = "ABC1234", Marca = "Fiat", Modelo = "Uno" },
                        new Dictionary<System.Guid, Cliente>(),
                        new List<OrdemServico>(),
                        new List<Agendamento>(),
                        new List<Orcamento>()),
                    new VeiculoViewModel(
                        new Veiculo { Placa = "XYZ9876", Marca = "Ford", Modelo = "Ka" },
                        new Dictionary<System.Guid, Cliente>(),
                        new List<OrdemServico>(),
                        new List<Agendamento>(),
                        new List<Orcamento>())
                }
            };

            viewModel.ApplyFilters("Uno", "Todos os sistemas", "Todos os tipos");

            Assert.Single(viewModel.FilteredVeiculos);
            Assert.Contains("Uno", viewModel.FilteredVeiculos[0].MarcaModelo);
            Assert.Equal("1 veiculo encontrado", viewModel.ResultCountText);
        }

        [Fact]
        public void ResetFilters_SetsDefaultsAndUpdatesCountText()
        {
            var viewModel = new VeiculosViewModel
            {
                AllVeiculos = new List<VeiculoViewModel>
                {
                    new VeiculoViewModel(
                        new Veiculo { Placa = "ABC1234", Marca = "Fiat", Modelo = "Uno" },
                        new Dictionary<System.Guid, Cliente>(),
                        new List<OrdemServico>(),
                        new List<Agendamento>(),
                        new List<Orcamento>()),
                    new VeiculoViewModel(
                        new Veiculo { Placa = "XYZ9876", Marca = "Ford", Modelo = "Ka" },
                        new Dictionary<System.Guid, Cliente>(),
                        new List<OrdemServico>(),
                        new List<Agendamento>(),
                        new List<Orcamento>())
                }
            };

            viewModel.ResetFilters();

            Assert.Equal(string.Empty, viewModel.SearchTerm);
            Assert.Equal("Todos os sistemas", viewModel.SelectedSistema);
            Assert.Equal("Todos os tipos", viewModel.SelectedTipo);
            Assert.Equal("2 veiculos encontrados", viewModel.ResultCountText);
            Assert.Equal(2, viewModel.FilteredVeiculos.Count);
        }
    }
}
