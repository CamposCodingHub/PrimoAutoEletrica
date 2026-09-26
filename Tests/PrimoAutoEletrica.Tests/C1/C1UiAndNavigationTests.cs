using System;
using System.IO;
using System.Linq;
using Xunit;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.UserControls;
using PrimoAutoEletrica.Views;

namespace PrimoAutoEletrica.Tests.C1
{
    public class C1UiAndNavigationTests
    {
        [Fact]
        public void NavigationService_DeveConterModulosDoCicloC1()
        {
            var admin = new Funcionario
            {
                Id = 1,
                Nome = "Admin Teste",
                PerfilAcesso = "Administrador"
            };

            var permService = new PermissionService(admin, (LoggerService?)null);
            var nav = new NavigationService(permService);

            var modulosCanonicos = nav.GetCanonicalModuleNames();

            Assert.Contains("Ferramentas", modulosCanonicos, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("Compras", modulosCanonicos, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("BaseConhecimento", modulosCanonicos, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void ModulosC1_DevemInstanciarTiposCorretos()
        {
            Assert.True(typeof(System.Windows.Controls.UserControl).IsAssignableFrom(typeof(FerramentasControl)));
            Assert.True(typeof(System.Windows.Controls.UserControl).IsAssignableFrom(typeof(NecessidadesCompraControl)));
            Assert.True(typeof(System.Windows.Controls.UserControl).IsAssignableFrom(typeof(BaseConhecimentoControl)));
        }

        [Fact]
        public void DialogosC1_DevemExistirESeremDerivadosDeWindow()
        {
            Assert.True(typeof(System.Windows.Window).IsAssignableFrom(typeof(Tool360Window)));
            Assert.True(typeof(System.Windows.Window).IsAssignableFrom(typeof(ToolCheckoutDialog)));
            Assert.True(typeof(System.Windows.Window).IsAssignableFrom(typeof(NovaFerramentaDialog)));
            Assert.True(typeof(System.Windows.Window).IsAssignableFrom(typeof(NovaRequisicaoCompraDialog)));
            Assert.True(typeof(System.Windows.Window).IsAssignableFrom(typeof(CasoTecnicoDialog)));
        }

        [Fact]
        public void PermissionService_DeveMapearModulosC1()
        {
            var admin = new Funcionario
            {
                Id = 1,
                Nome = "Administrador",
                PerfilAcesso = "Administrador"
            };

            var permService = new PermissionService(admin, (LoggerService?)null);

            Assert.True(permService.TemPermissao("Ferramentas"));
            Assert.True(permService.TemPermissao("Compras"));
            Assert.True(permService.TemPermissao("NecessidadesCompra"));
            Assert.True(permService.TemPermissao("BaseConhecimento"));
            Assert.True(permService.TemPermissao("Conhecimento"));
            Assert.True(permService.TemPermissao("PrimoxAssist"));
        }
    }
}
