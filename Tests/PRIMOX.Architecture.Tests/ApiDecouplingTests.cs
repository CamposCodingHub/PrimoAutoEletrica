using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace PRIMOX.Architecture.Tests
{
    public class ApiDecouplingTests
    {
        [Fact]
        public void Api_Nao_Deve_Referenciar_WPF_Nem_Projeto_Desktop()
        {
            // Obtém o assembly da API através de um tipo contido nela
            var apiAssembly = typeof(PrimoAutoEletrica.Api.Configuration.SwaggerConfiguration).Assembly;
            var referencedAssemblies = apiAssembly.GetReferencedAssemblies().Select(a => a.Name).ToList();

            // A API NUNCA PODE DEPENDER DE WPF / DESKTOP
            Assert.DoesNotContain("PrimoAutoEletrica", referencedAssemblies);
            Assert.DoesNotContain("PresentationFramework", referencedAssemblies);
            Assert.DoesNotContain("PresentationCore", referencedAssemblies);
            Assert.DoesNotContain("WindowsBase", referencedAssemblies);
            Assert.DoesNotContain("System.Windows.Forms", referencedAssemblies);
            Assert.DoesNotContain("OpenTK.GLWpfControl", referencedAssemblies);
            Assert.DoesNotContain("SkiaSharp.Views.WPF", referencedAssemblies);
        }

        [Fact]
        public void Api_Deve_Depender_Apenas_De_Application_E_Infrastructure()
        {
            var apiAssembly = typeof(PrimoAutoEletrica.Api.Configuration.SwaggerConfiguration).Assembly;
            var referencedAssemblies = apiAssembly.GetReferencedAssemblies().Select(a => a.Name).ToList();

            Assert.Contains("PRIMOX.Application", referencedAssemblies);
            Assert.Contains("PRIMOX.Infrastructure", referencedAssemblies);
            Assert.Contains("PRIMOX.Domain", referencedAssemblies);
        }
    }
}
