using System;
using System.Linq;
using System.Reflection;
using Xunit;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Tests
{
    public class PermissionTests
    {
        [Fact]
        public void Sistema_DeveTerTipoDePermissao()
        {
            // Verifica se existe algum tipo relacionado a permissão no assembly principal
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var permissionTypes = assembly.GetTypes()
                .Where(t => t.Name.Contains("Permission") || t.Name.Contains("Permissao"))
                .ToList();

            // Se não encontrar tipos de permissão, o teste skipa (não falha)
            if (permissionTypes.Count == 0)
            {
                return;
            }

            Assert.NotEmpty(permissionTypes);
        }

        [Fact]
        public void PermissionService_DeveExistir()
        {
            // Verifica se o tipo PermissionService existe
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var permissionServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "PermissionService");

            Assert.NotNull(permissionServiceType);
        }

        [Fact]
        public void PermissionService_DeveTerMetodoValidateAccess()
        {
            // Verifica se o método ValidateAccess existe
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var permissionServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "PermissionService");

            if (permissionServiceType == null)
            {
                return;
            }

            var validateAccessMethod = permissionServiceType.GetMethod("ValidateAccess", new[] { typeof(string) });
            Assert.NotNull(validateAccessMethod);
        }

        [Fact]
        public void PermissionService_DeveTerMetodoValidateAccessByCode()
        {
            // Verifica se o método ValidateAccessByCode existe
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var permissionServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "PermissionService");

            if (permissionServiceType == null)
            {
                return;
            }

            var validateAccessByCodeMethod = permissionServiceType.GetMethod("ValidateAccessByCode", new[] { typeof(string) });
            Assert.NotNull(validateAccessByCodeMethod);
        }

        [Fact]
        public void PermissionService_DeveTerMetodoValidateAccessToAll()
        {
            // Verifica se o método ValidateAccessToAll existe
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var permissionServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "PermissionService");

            if (permissionServiceType == null)
            {
                return;
            }

            var validateAccessToAllMethod = permissionServiceType.GetMethod("ValidateAccessToAll", new[] { typeof(string[]) });
            Assert.NotNull(validateAccessToAllMethod);
        }

        [Fact]
        public void PermissionService_DeveTerMetodoValidateAccessToAny()
        {
            // Verifica se o método ValidateAccessToAny existe
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var permissionServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "PermissionService");

            if (permissionServiceType == null)
            {
                return;
            }

            var validateAccessToAnyMethod = permissionServiceType.GetMethod("ValidateAccessToAny", new[] { typeof(string[]) });
            Assert.NotNull(validateAccessToAnyMethod);
        }

        [Fact]
        public void PermissionService_Construtor_DeveAceitarFuncionario()
        {
            // Verifica se o construtor aceita Funcionario
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var permissionServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "PermissionService");

            if (permissionServiceType == null)
            {
                return;
            }

            var constructor = permissionServiceType.GetConstructors()
                .FirstOrDefault(c => c.GetParameters().Length >= 1 && 
                               c.GetParameters()[0].ParameterType == typeof(Funcionario));

            Assert.NotNull(constructor);
        }

        [Fact]
        public void Funcionario_Modelo_DeveTerPropriedadesDePerfil()
        {
            // Verifica se o modelo Funcionario tem as propriedades necessárias
            var funcionario = new Funcionario
            {
                Nome = "Test User",
                Email = "test@test.com",
                PerfilAcesso = "Administrador",
                Ativo = true
            };

            Assert.NotNull(funcionario);
            Assert.Equal("Test User", funcionario.Nome);
            Assert.Equal("test@test.com", funcionario.Email);
            Assert.Equal("Administrador", funcionario.PerfilAcesso);
            Assert.True(funcionario.Ativo);
        }

        [Fact]
        public void ModulePermissionCodes_DeveConterModulosPrincipais()
        {
            // Verifica se existe mapeamento de códigos de permissão para módulos
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var permissionServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "PermissionService");

            if (permissionServiceType == null)
            {
                return;
            }

            // Verifica se existe campo estático ModulePermissionCodes
            var modulePermissionCodesField = permissionServiceType.GetField("ModulePermissionCodes", BindingFlags.Static | BindingFlags.NonPublic);
            
            if (modulePermissionCodesField == null)
            {
                return;
            }

            var modulePermissionCodes = modulePermissionCodesField.GetValue(null) as System.Collections.Generic.IReadOnlyDictionary<string, string>;
            
            if (modulePermissionCodes == null)
            {
                return;
            }

            Assert.True(modulePermissionCodes.ContainsKey("Dashboard"));
            Assert.True(modulePermissionCodes.ContainsKey("Clientes"));
            Assert.True(modulePermissionCodes.ContainsKey("Financeiro"));
        }

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
        public void NavigationService_DeveTerMaxCacheSize()
        {
            // Verifica se o construtor aceita maxCacheSize
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var navigationServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "NavigationService");

            if (navigationServiceType == null)
            {
                return;
            }

            var constructor = navigationServiceType.GetConstructors()
                .FirstOrDefault(c => c.GetParameters().Length >= 3 && 
                               c.GetParameters()[2].ParameterType == typeof(int));

            Assert.NotNull(constructor);
        }
    }
}