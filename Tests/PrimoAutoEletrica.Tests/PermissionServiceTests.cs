using System;
using System.Linq;
using System.Reflection;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests
{
    public class PermissionServiceTests
    {
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
        public void PermissionService_DeveTerMetodosDeValidacao()
        {
            // Verifica se todos os métodos de validação existem
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var permissionServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "PermissionService");

            if (permissionServiceType == null)
            {
                return;
            }

            var methods = permissionServiceType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.StartsWith("Validate"))
                .ToList();

            Assert.True(methods.Count >= 4, "Deve ter pelo menos 4 métodos de validação");
        }

        [Fact]
        public void PermissionService_DeveSerClassePublica()
        {
            // Verifica se PermissionService é uma classe pública
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var permissionServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "PermissionService");

            if (permissionServiceType == null)
            {
                return;
            }

            Assert.True(permissionServiceType.IsClass);
            Assert.True(permissionServiceType.IsPublic);
        }
    }
}