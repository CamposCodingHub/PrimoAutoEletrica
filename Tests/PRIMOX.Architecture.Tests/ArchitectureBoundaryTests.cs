using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace PRIMOX.Architecture.Tests
{
    public class ArchitectureBoundaryTests
    {
        [Fact]
        public void Domain_Nao_Deve_Referenciar_Infrastructure_Ou_Application()
        {
            var domainAssembly = typeof(PRIMOX.Domain.Entities.OrdemServico).Assembly;
            var referencedAssemblies = domainAssembly.GetReferencedAssemblies().Select(a => a.Name).ToList();

            Assert.DoesNotContain("PRIMOX.Application", referencedAssemblies);
            Assert.DoesNotContain("PRIMOX.Infrastructure", referencedAssemblies);
            Assert.DoesNotContain("PrimoAutoEletrica", referencedAssemblies);
            Assert.DoesNotContain("PresentationFramework", referencedAssemblies);
            Assert.DoesNotContain("PresentationCore", referencedAssemblies);
            Assert.DoesNotContain("WindowsBase", referencedAssemblies);
            Assert.DoesNotContain("Microsoft.Data.Sqlite", referencedAssemblies);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore", referencedAssemblies);
            Assert.DoesNotContain("Microsoft.AspNetCore", referencedAssemblies);
        }

        [Fact]
        public void Application_Nao_Deve_Referenciar_Infrastructure_Ou_Desktop()
        {
            var appAssembly = typeof(PRIMOX.Application.UseCases.OrdensServico.AbrirOrdemServicoUseCase).Assembly;
            var referencedAssemblies = appAssembly.GetReferencedAssemblies().Select(a => a.Name).ToList();

            Assert.DoesNotContain("PRIMOX.Infrastructure", referencedAssemblies);
            Assert.DoesNotContain("PrimoAutoEletrica", referencedAssemblies);
            Assert.DoesNotContain("PresentationFramework", referencedAssemblies);
            Assert.DoesNotContain("PresentationCore", referencedAssemblies);
            Assert.DoesNotContain("WindowsBase", referencedAssemblies);
        }

        [Fact]
        public void Domain_E_Application_Nao_Devem_Conter_Tipos_Graficos_WPF()
        {
            var domainAssembly = typeof(PRIMOX.Domain.Entities.OrdemServico).Assembly;
            var appAssembly = typeof(PRIMOX.Application.UseCases.OrdensServico.AbrirOrdemServicoUseCase).Assembly;

            var forbiddenNamespaces = new[]
            {
                "System.Windows",
                "System.Windows.Controls",
                "System.Windows.Media",
                "System.Windows.Data"
            };

            foreach (var type in domainAssembly.GetTypes())
            {
                var ns = type.Namespace ?? "";
                foreach (var forbidden in forbiddenNamespaces)
                {
                    Assert.False(ns.StartsWith(forbidden),
                        $"Tipo {type.FullName} em Domain viola fronteira arquitetural ao residir em namespace gráfico {forbidden}.");
                }
            }

            foreach (var type in appAssembly.GetTypes())
            {
                var ns = type.Namespace ?? "";
                foreach (var forbidden in forbiddenNamespaces)
                {
                    Assert.False(ns.StartsWith(forbidden),
                        $"Tipo {type.FullName} em Application viola fronteira arquitetural ao residir em namespace gráfico {forbidden}.");
                }
            }
        }
    }
}
