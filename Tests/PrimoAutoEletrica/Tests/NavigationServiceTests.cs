using Xunit;
using System;

namespace PrimoAutoEletrica.Tests
{
    public class NavigationServiceTests
    {
        [Fact]
        public void NavigationService_DeveExistir()
        {
            // Verifica se o arquivo NavigationService.cs existe
            var navigationServicePath = @"..\..\..\PrimoAutoEletrica\Services\NavigationService.cs";
            Assert.True(System.IO.File.Exists(navigationServicePath), "NavigationService.cs deve existir");
        }

        [Fact]
        public void NavigationService_DeveTerMetodoNavigateTo()
        {
            // Verifica se o arquivo contém o método NavigateTo
            var navigationServicePath = @"..\..\..\PrimoAutoEletrica\Services\NavigationService.cs";
            var content = System.IO.File.ReadAllText(navigationServicePath);
            Assert.Contains("NavigateTo", content, "NavigationService deve ter método NavigateTo");
        }

        [Fact]
        public void NavigationService_DeveTerMetodoGoBack()
        {
            // Verifica se o arquivo contém o método GoBack
            var navigationServicePath = @"..\..\..\PrimoAutoEletrica\Services\NavigationService.cs";
            var content = System.IO.File.ReadAllText(navigationServicePath);
            Assert.Contains("GoBack", content, "NavigationService deve ter método GoBack");
        }

        [Fact]
        public void NavigationService_DeveTerMetodoCanGoBack()
        {
            // Verifica se o arquivo contém o método CanGoBack
            var navigationServicePath = @"..\..\..\PrimoAutoEletrica\Services\NavigationService.cs";
            var content = System.IO.File.ReadAllText(navigationServicePath);
            Assert.Contains("CanGoBack", content, "NavigationService deve ter método CanGoBack");
        }
    }
}
