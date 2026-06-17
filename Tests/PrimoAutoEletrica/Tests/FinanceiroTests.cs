using Xunit;
using System;

namespace PrimoAutoEletrica.Tests
{
    public class FinanceiroTests
    {
        [Fact]
        public void FinanceiroViewModel_DeveExistir()
        {
            // Verifica se o arquivo FinanceiroViewModel.cs existe
            var financeiroViewModelPath = @"..\..\..\PrimoAutoEletrica\ViewModels\FinanceiroViewModel.cs";
            Assert.True(System.IO.File.Exists(financeiroViewModelPath), "FinanceiroViewModel.cs deve existir");
        }

        [Fact]
        public void FinanceiroViewModel_DeveTerMetodoCarregarContasPagar()
        {
            // Verifica se o arquivo contém o método CarregarContasPagar
            var financeiroViewModelPath = @"..\..\..\PrimoAutoEletrica\ViewModels\FinanceiroViewModel.cs";
            var content = System.IO.File.ReadAllText(financeiroViewModelPath);
            Assert.Contains("CarregarContasPagar", content, "FinanceiroViewModel deve ter método CarregarContasPagar");
        }

        [Fact]
        public void FinanceiroViewModel_DeveTerMetodoCarregarContasReceber()
        {
            // Verifica se o arquivo contém o método CarregarContasReceber
            var financeiroViewModelPath = @"..\..\..\PrimoAutoEletrica\ViewModels\FinanceiroViewModel.cs";
            var content = System.IO.File.ReadAllText(financeiroViewModelPath);
            Assert.Contains("CarregarContasReceber", content, "FinanceiroViewModel deve ter método CarregarContasReceber");
        }

        [Fact]
        public void FinanceiroViewModel_DeveTerMetodoPagarConta()
        {
            // Verifica se o arquivo contém o método PagarConta
            var financeiroViewModelPath = @"..\..\..\PrimoAutoEletrica\ViewModels\FinanceiroViewModel.cs";
            var content = System.IO.File.ReadAllText(financeiroViewModelPath);
            Assert.Contains("PagarConta", content, "FinanceiroViewModel deve ter método PagarConta");
        }

        [Fact]
        public void FinanceiroViewModel_DeveTerMetodoReceberConta()
        {
            // Verifica se o arquivo contém o método ReceberConta
            var financeiroViewModelPath = @"..\..\..\PrimoAutoEletrica\ViewModels\FinanceiroViewModel.cs";
            var content = System.IO.File.ReadAllText(financeiroViewModelPath);
            Assert.Contains("ReceberConta", content, "FinanceiroViewModel deve ter método ReceberConta");
        }
    }
}
