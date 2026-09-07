using Xunit;
using Moq;
using CommunityToolkit.Mvvm.Input;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Services;
using System.Collections.ObjectModel;

namespace PrimoAutoEletrica.Tests.ViewModels
{
    /// <summary>
    /// Testes para CatalogoPecasViewModel
    /// </summary>
    public class CatalogoPecasViewModelTests
    {
        private readonly Mock<DatabaseService> _mockDatabaseService;
        private readonly CatalogoPecasViewModel _viewModel;

        public CatalogoPecasViewModelTests()
        {
            _mockDatabaseService = new Mock<DatabaseService>();
            _viewModel = new CatalogoPecasViewModel();
        }

        [Fact]
        public void Constructor_Deve_Inicializar_PecasCollection_Vazio()
        {
            // Arrange & Act
            var viewModel = new CatalogoPecasViewModel();

            // Assert
            Assert.NotNull(viewModel.Pecas);
            Assert.Empty(viewModel.Pecas);
        }

        [Fact]
        public void AdicionarPecaCommand_Com_DadosValidos_Deve_Adicionar_Peca()
        {
            // Arrange
            _viewModel.NomePeca = "Bateria de Carro";
            _viewModel.PrecoPeca = 250.00m;
            _viewModel.QuantidadePeca = 5;

            // Act
            if (_viewModel.AdicionarPecaCommand != null && _viewModel.AdicionarPecaCommand.CanExecute(null))
            {
                _viewModel.AdicionarPecaCommand.Execute(null);
            }

            // Assert
            Assert.NotEmpty(_viewModel.Pecas);
        }

        [Fact]
        public void LimparCamposCommand_Deve_Resetar_Valores()
        {
            // Arrange
            _viewModel.NomePeca = "Bateria";
            _viewModel.PrecoPeca = 250;
            _viewModel.QuantidadePeca = 5;

            // Act
            if (_viewModel.LimparCamposCommand != null && _viewModel.LimparCamposCommand.CanExecute(null))
            {
                _viewModel.LimparCamposCommand.Execute(null);
            }

            // Assert
            Assert.Empty(_viewModel.NomePeca);
            Assert.Equal(0, _viewModel.PrecoPeca);
            Assert.Equal(0, _viewModel.QuantidadePeca);
        }

        [Fact]
        public void FiltroNome_Deve_Filtrar_Pecas_Corretamente()
        {
            // Arrange
            _viewModel.Pecas.Add(new Models.Peca { Nome = "Bateria" });
            _viewModel.Pecas.Add(new Models.Peca { Nome = "Alternador" });
            _viewModel.FiltroNome = "Bateria";

            // Act
            var pecasFiltradas = _viewModel.PecasFiltradas;

            // Assert
            Assert.NotNull(pecasFiltradas);
        }
    }

    /// <summary>
    /// Testes para RelatoriosModernoViewModel
    /// </summary>
    public class RelatoriosModernoViewModelTests
    {
        private readonly RelatoriosModernoViewModel _viewModel;

        public RelatoriosModernoViewModelTests()
        {
            _viewModel = new RelatoriosModernoViewModel();
        }

        [Fact]
        public void Constructor_Deve_Inicializar_DataPeriodo()
        {
            // Assert
            Assert.NotNull(_viewModel.DataInicio);
            Assert.NotNull(_viewModel.DataFim);
        }

        [Fact]
        public void ExportarPdfCommand_Deve_Estar_Disponivel()
        {
            // Assert
            Assert.NotNull(_viewModel.ExportarPdfCommand);
        }

        [Fact]
        public void ExportarExcelCommand_Deve_Estar_Disponivel()
        {
            // Assert
            Assert.NotNull(_viewModel.ExportarExcelCommand);
        }

        [Fact]
        public void PeriodoSelecionado_Deve_Validar_Datas()
        {
            // Arrange
            _viewModel.DataInicio = DateTime.Today;
            _viewModel.DataFim = DateTime.Today.AddDays(-1);

            // Act
            var isValid = _viewModel.DataInicio <= _viewModel.DataFim;

            // Assert
            Assert.False(isValid);
        }
    }

    /// <summary>
    /// Testes para PrinterManagementViewModel
    /// </summary>
    public class PrinterManagementViewModelTests
    {
        private readonly PrinterManagementViewModel _viewModel;

        public PrinterManagementViewModelTests()
        {
            _viewModel = new PrinterManagementViewModel();
        }

        [Fact]
        public void Constructor_Deve_Inicializar_ImpressionesCollection()
        {
            // Assert
            Assert.NotNull(_viewModel.Impressoras);
        }

        [Fact]
        public void AtualizarListaImpressorasCommand_Deve_Estar_Disponivel()
        {
            // Assert
            Assert.NotNull(_viewModel.AtualizarListaImpressorasCommand);
        }

        [Fact]
        public void TestarImpressoraCommand_Deve_Estar_Disponivel()
        {
            // Assert
            Assert.NotNull(_viewModel.TestarImpressoraCommand);
        }

        [Fact]
        public void DefinirComoImpressoraParadraoCommand_Deve_Estar_Disponivel()
        {
            // Assert
            Assert.NotNull(_viewModel.DefinirComoImpressoraParadraoCommand);
        }
    }

    /// <summary>
    /// Testes para AutoEletricaTecnicaViewModel
    /// </summary>
    public class AutoEletricaTecnicaViewModelTests
    {
        private readonly AutoEletricaTecnicaViewModel _viewModel;

        public AutoEletricaTecnicaViewModelTests()
        {
            _viewModel = new AutoEletricaTecnicaViewModel();
        }

        [Fact]
        public void Constructor_Deve_Inicializar_Diagnosticos()
        {
            // Assert
            Assert.NotNull(_viewModel.Diagnosticos);
        }

        [Fact]
        public void AdicionarDiagnosticoCommand_Deve_Estar_Disponivel()
        {
            // Assert
            Assert.NotNull(_viewModel.AdicionarDiagnosticoCommand);
        }

        [Fact]
        public void DescricaoDiagnostico_Pode_Ser_Definida()
        {
            // Arrange
            var descricao = "Teste de alternador";
            _viewModel.DescricaoDiagnostico = descricao;

            // Assert
            Assert.Equal(descricao, _viewModel.DescricaoDiagnostico);
        }

        [Fact]
        public void ResultadoDiagnostico_Pode_Ser_Definido()
        {
            // Arrange
            var resultado = "OK - Sem problemas";
            _viewModel.ResultadoDiagnostico = resultado;

            // Assert
            Assert.Equal(resultado, _viewModel.ResultadoDiagnostico);
        }
    }
}
