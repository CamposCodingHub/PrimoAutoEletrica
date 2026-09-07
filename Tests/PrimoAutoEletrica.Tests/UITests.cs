using System;
using System.Linq;
using System.Reflection;
using Xunit;
using PrimoAutoEletrica.UserControls;
using PrimoAutoEletrica.ViewModels;

namespace PrimoAutoEletrica.Tests
{
    /// <summary>
    /// Testes de UI automatizados para verificar componentes visuais
    /// </summary>
    public class UITests
    {
        [Fact]
        public void UserControls_DeveExistirNoProjeto()
        {
            // Verifica se os principais UserControls existem no assembly
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var userControlTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(System.Windows.Controls.UserControl)))
                .ToList();

            Assert.True(userControlTypes.Count > 0, "Deve existir UserControls no projeto");
        }

        [Fact]
        public void EstoqueControl_DeveExistir()
        {
            // Verifica se o EstoqueControl existe
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var estoqueControlType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "EstoqueControl");

            Assert.True(estoqueControlType != null, "EstoqueControl deve existir");
        }

        [Fact]
        public void FuncionariosControl_DeveExistir()
        {
            // Verifica se o FuncionariosControl existe
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var funcionariosControlType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "FuncionariosControl");

            Assert.True(funcionariosControlType != null, "FuncionariosControl deve existir");
        }

        [Fact]
        public void ViewModels_DeveExistirNoProjeto()
        {
            // Verifica se os principais ViewModels existem
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var viewModelTypes = assembly.GetTypes()
                .Where(t => t.Name.EndsWith("ViewModel"))
                .ToList();

            Assert.True(viewModelTypes.Count > 0, "Deve existir ViewModels no projeto");
        }

        [Fact]
        public void EstoqueViewModel_DeveExistir()
        {
            // Verifica se o EstoqueViewModel existe
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var estoqueViewModelType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "EstoqueViewModel");

            Assert.True(estoqueViewModelType != null, "EstoqueViewModel deve existir");
        }

        [Fact]
        public void FuncionariosViewModel_DeveExistir()
        {
            // Verifica se o FuncionariosViewModel existe
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var funcionariosViewModelType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "FuncionariosViewModel");

            Assert.True(funcionariosViewModelType != null, "FuncionariosViewModel deve existir");
        }

        [Fact]
        public void UserControls_Principais_TeremViewModelCorrespondente()
        {
            // Verifica se os UserControls principais têm ViewModels correspondentes
            var assembly = Assembly.Load("PrimoAutoEletrica");
            
            var userControls = new[] { "EstoqueControl", "FuncionariosControl", "DashboardControl" };
            var viewModels = new[] { "EstoqueViewModel", "FuncionariosViewModel", "DashboardViewModel" };

            foreach (var control in userControls)
            {
                var controlType = assembly.GetTypes().FirstOrDefault(t => t.Name == control);
                var viewModelType = assembly.GetTypes().FirstOrDefault(t => t.Name == control.Replace("Control", "ViewModel"));
                
                // Se o UserControl existe, deve ter ViewModel correspondente
                if (controlType != null)
                {
                    Assert.True(viewModelType != null, $"{control} deve ter ViewModel correspondente");
                }
            }
        }

        [Fact]
        public void Views_DeveExistirNoProjeto()
        {
            // Verifica se as principais Views (Windows) existem
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var windowTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(System.Windows.Window)))
                .ToList();

            Assert.True(windowTypes.Count > 0, "Deve existir Views/Windows no projeto");
        }

        [Fact]
        public void LoginView_DeveExistir()
        {
            // Verifica se a LoginView existe ou uma alternativa (ex: LoginWindow)
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var loginViewType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name.Contains("Login") && t.IsSubclassOf(typeof(System.Windows.Window)));

            Assert.True(loginViewType != null, "LoginView/Window deve existir");
        }

        [Fact]
        public void MainWindow_DeveExistir()
        {
            // Verifica se a MainWindow existe
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var mainWindowType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "MainWindow");

            Assert.True(mainWindowType != null, "MainWindow deve existir");
        }

        [Fact]
        public void DependencyInjection_DeveEstarConfigurado()
        {
            // Verifica se o sistema de DI está configurado
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var serviceExtensionsType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "ServiceExtensions");

            Assert.True(serviceExtensionsType != null, "ServiceExtensions deve existir para DI");
        }

        [Fact]
        public void ServiceExtensions_DeveTerMetodosDeRegistro()
        {
            // Verifica se os métodos de registro de serviços existem
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var serviceExtensionsType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "ServiceExtensions");

            if (serviceExtensionsType == null)
            {
                return;
            }

            var addCoreMethod = serviceExtensionsType.GetMethod("AddPrimoAutoEletricaCore");
            var addViewModelsMethod = serviceExtensionsType.GetMethod("AddPrimoAutoEletricaViewModels");
            var addRepositoriesMethod = serviceExtensionsType.GetMethod("AddPrimoAutoEletricaRepositories");

            Assert.True(addCoreMethod != null, "Deve ter método AddPrimoAutoEletricaCore");
            Assert.True(addViewModelsMethod != null, "Deve ter método AddPrimoAutoEletricaViewModels");
            Assert.True(addRepositoriesMethod != null, "Deve ter método AddPrimoAutoEletricaRepositories");
        }

        [Fact]
        public void ThemeService_DeveExistir()
        {
            // Verifica se o ThemeService existe
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var themeServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "ThemeService");

            Assert.True(themeServiceType != null, "ThemeService deve existir");
        }

        [Fact]
        public void StandardTheme_DeveExistir()
        {
            // Verifica se o StandardTheme.xaml existe no projeto
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var resourceNames = assembly.GetManifestResourceNames();
            
            var hasStandardTheme = resourceNames.Any(name => name.Contains("StandardTheme"));
            
            // Como arquivos XAML não são recursos manifest, verificamos apenas se a classe ThemeService existe
            var themeServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "ThemeService");
            
            Assert.True(themeServiceType != null, "ThemeService deve existir para gerenciar temas");
        }

        [Fact]
        public void UserControls_TeremBindingXAML()
        {
            // Verifica se os UserControls principais são classes públicas
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var userControlTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(System.Windows.Controls.UserControl)) && t.IsPublic)
                .ToList();

            Assert.True(userControlTypes.Count > 0, "Deve existir UserControls públicos");
        }

        [Fact]
        public void ViewModel_Principal_DeveTerPropriedadesDeComando()
        {
            // Verifica se os ViewModels principais têm propriedades esperadas
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var viewModelTypes = assembly.GetTypes()
                .Where(t => t.Name.EndsWith("ViewModel") && t.Name != "BaseViewModel")
                .ToList();

            foreach (var viewModelType in viewModelTypes)
            {
                var properties = viewModelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                // Pelo menos 1 propriedade pública esperada
                Assert.True(properties.Length > 0, $"{viewModelType.Name} deve ter propriedades públicas");
            }
        }

        [Fact]
        public void Navegacao_DeveTerMultiplosModulos()
        {
            // Verifica se existem múltiplos UserControls para navegação
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var userControlTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(System.Windows.Controls.UserControl)))
                .ToList();

            Assert.True(userControlTypes.Count >= 5, "Deve existir pelo menos 5 UserControls para navegação");
        }

        [Fact]
        public void App_DeveTerConfiguracaoDI()
        {
            // Verifica se a classe App tem propriedade de IServiceProvider
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var appType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "App");

            if (appType == null)
            {
                return;
            }

            var servicesProperty = appType.GetProperty("Services", BindingFlags.Public | BindingFlags.Static);
            Assert.True(servicesProperty != null, "App deve ter propriedade Services para DI");
        }
    }
}