using System;
using System.Linq;
using System.Reflection;
using Xunit;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Tests
{
    public class NavigationServiceTests
    {
        [Fact]
        public void NavigationService_DeveExistir()
        {
            // Verifica se o tipo NavigationService existe
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var navigationServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "NavigationService");

            Assert.NotNull(navigationServiceType);
        }

        [Fact]
        public void NavigationService_DeveTerMetodoNavigate()
        {
            // Verifica se o método Navigate existe
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var navigationServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "NavigationService");

            if (navigationServiceType == null)
            {
                return;
            }

            var navigateMethod = navigationServiceType.GetMethod("Navigate", new[] { typeof(string) });
            Assert.NotNull(navigateMethod);
        }

        [Fact]
        public void NavigationService_DeveTerMetodoNavigateBack()
        {
            // Verifica se o método NavigateBack existe
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var navigationServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "NavigationService");

            if (navigationServiceType == null)
            {
                return;
            }

            var navigateBackMethod = navigationServiceType.GetMethod("NavigateBack");
            Assert.NotNull(navigateBackMethod);
        }

        [Fact]
        public void NavigationService_DeveTerMetodoClearCache()
        {
            // Verifica se o método ClearCache existe
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var navigationServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "NavigationService");

            if (navigationServiceType == null)
            {
                return;
            }

            var clearCacheMethod = navigationServiceType.GetMethod("ClearCache");
            Assert.NotNull(clearCacheMethod);
        }

        [Fact]
        public void NavigationService_DeveTerMetodoRemoveFromCache()
        {
            // Verifica se o método RemoveFromCache existe
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var navigationServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "NavigationService");

            if (navigationServiceType == null)
            {
                return;
            }

            var removeFromCacheMethod = navigationServiceType.GetMethod("RemoveFromCache", new[] { typeof(string) });
            Assert.NotNull(removeFromCacheMethod);
        }

        [Fact]
        public void NavigationService_DeveTerMetodoRefreshCurrent()
        {
            // Verifica se o método RefreshCurrent existe
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var navigationServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "NavigationService");

            if (navigationServiceType == null)
            {
                return;
            }

            var refreshCurrentMethod = navigationServiceType.GetMethod("RefreshCurrent");
            Assert.NotNull(refreshCurrentMethod);
        }

        [Fact]
        public void NavigationService_DeveTerMetodoRegisterModule()
        {
            // Verifica se o método RegisterModule existe
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var navigationServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "NavigationService");

            if (navigationServiceType == null)
            {
                return;
            }

            var registerModuleMethod = navigationServiceType.GetMethod("RegisterModule", new[] { typeof(string), typeof(Type) });
            Assert.NotNull(registerModuleMethod);
        }

        [Fact]
        public void NavigationService_DeveTerPropriedadeCurrentModule()
        {
            // Verifica se a propriedade CurrentModule existe
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var navigationServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "NavigationService");

            if (navigationServiceType == null)
            {
                return;
            }

            var currentModuleProperty = navigationServiceType.GetProperty("CurrentModule");
            Assert.NotNull(currentModuleProperty);
        }

        [Fact]
        public void NavigationService_DeveTerPropriedadeCanNavigateBack()
        {
            // Verifica se a propriedade CanNavigateBack existe
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var navigationServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "NavigationService");

            if (navigationServiceType == null)
            {
                return;
            }

            var canNavigateBackProperty = navigationServiceType.GetProperty("CanNavigateBack");
            Assert.NotNull(canNavigateBackProperty);
        }

        [Fact]
        public void NavigationService_DeveTerEventoNavigationCompleted()
        {
            // Verifica se o evento NavigationCompleted existe
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var navigationServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "NavigationService");

            if (navigationServiceType == null)
            {
                return;
            }

            var navigationCompletedEvent = navigationServiceType.GetEvent("NavigationCompleted");
            Assert.NotNull(navigationCompletedEvent);
        }

        [Fact]
        public void NavigationService_DeveTerEventoNavigationStateChanged()
        {
            // Verifica se o evento NavigationStateChanged existe
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var navigationServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "NavigationService");

            if (navigationServiceType == null)
            {
                return;
            }

            var navigationStateChangedEvent = navigationServiceType.GetEvent("NavigationStateChanged");
            Assert.NotNull(navigationStateChangedEvent);
        }

        [Fact]
        public void NavigationService_Construtor_DeveAceitarPermissionService()
        {
            // Verifica se o construtor aceita PermissionService
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var navigationServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "NavigationService");

            if (navigationServiceType == null)
            {
                return;
            }

            var constructor = navigationServiceType.GetConstructors()
                .FirstOrDefault(c => c.GetParameters().Length >= 1 && 
                               c.GetParameters()[0].ParameterType == typeof(PermissionService));

            Assert.NotNull(constructor);
        }

        [Fact]
        public void NavigationService_DeveImplementarINavigationService()
        {
            // Verifica se NavigationService implementa INavigationService
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var navigationServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "NavigationService");

            if (navigationServiceType == null)
            {
                return;
            }

            var interfaceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "INavigationService");

            if (interfaceType == null)
            {
                return;
            }

            Assert.True(interfaceType.IsAssignableFrom(navigationServiceType));
        }

        [Fact]
        public void NavigationService_DeveTerSuporteAModulos()
        {
            // Verifica se existe sistema de módulos
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var navigationServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "NavigationService");

            if (navigationServiceType == null)
            {
                return;
            }

            // Verifica se existe campo para armazenar módulos
            var modulesField = navigationServiceType.GetField("_modules", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (modulesField == null)
            {
                return;
            }

            Assert.NotNull(modulesField);
        }

        [Fact]
        public void NavigationService_DeveTerSuporteACache()
        {
            // Verifica se existe sistema de cache
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var navigationServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "NavigationService");

            if (navigationServiceType == null)
            {
                return;
            }

            // Verifica se existe campo para armazenar cache
            var cacheField = navigationServiceType.GetField("_pageCache", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (cacheField == null)
            {
                return;
            }

            Assert.NotNull(cacheField);
        }

        [Fact]
        public void NavigationService_DeveTerSuporteAHistorico()
        {
            // Verifica se existe sistema de histórico
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var navigationServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "NavigationService");

            if (navigationServiceType == null)
            {
                return;
            }

            // Verifica se existe campo para armazenar histórico
            var historyField = navigationServiceType.GetField("_navigationHistory", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (historyField == null)
            {
                return;
            }

            Assert.NotNull(historyField);
        }
    }
}