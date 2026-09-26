using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Xunit;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.UserControls;
using PrimoAutoEletrica.ViewModels;

namespace PrimoAutoEletrica.Tests.C1
{
    public class C1BugFixRegressionTests
    {
        private static void RunOnSta(Action action)
        {
            Exception? ex = null;
            var t = new Thread(() =>
            {
                try
                {
                    if (Application.Current == null)
                    {
                        var app = new Application();
                        var colorsUri = new Uri("/PrimoAutoEletrica;component/Themes/Colors.Light.xaml", UriKind.RelativeOrAbsolute);
                        var stylesUri = new Uri("/PrimoAutoEletrica;component/Themes/GlobalStyles.xaml", UriKind.RelativeOrAbsolute);
                        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = colorsUri });
                        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = stylesUri });
                    }
                    action();
                }
                catch (Exception e)
                {
                    ex = e;
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            if (ex != null)
            {
                throw new Exception("Falha na execução em thread STA: " + ex.Message, ex);
            }
        }

        #region BUG-001 Tests - ModernTabControl e ModernTabItem

        [Fact]
        public void Bug001_TabsResourceDictionary_DeveConterEstilosModernTab()
        {
            RunOnSta(() =>
            {
                var tabsDictUri = new Uri("/PrimoAutoEletrica;component/Themes/Tabs.xaml", UriKind.RelativeOrAbsolute);
                var tabsDict = new ResourceDictionary { Source = tabsDictUri };

                Assert.True(tabsDict.Contains("ModernTabControl"), "Tabs.xaml deve conter o estilo 'ModernTabControl'.");
                Assert.True(tabsDict.Contains("ModernTabItem"), "Tabs.xaml deve conter o estilo 'ModernTabItem'.");

                var tabControlStyle = tabsDict["ModernTabControl"] as Style;
                var tabItemStyle = tabsDict["ModernTabItem"] as Style;

                Assert.NotNull(tabControlStyle);
                Assert.NotNull(tabItemStyle);
                Assert.Equal(typeof(TabControl), tabControlStyle.TargetType);
                Assert.Equal(typeof(TabItem), tabItemStyle.TargetType);
            });
        }

        [Fact]
        public void Bug001_BaseConhecimentoControl_DeveInicializarSemXamlParseException()
        {
            RunOnSta(() =>
            {
                var control = new BaseConhecimentoControl();
                Assert.NotNull(control);
            });
        }

        [Fact]
        public void Bug001_NecessidadesCompraControl_DeveInicializarSemXamlParseException()
        {
            RunOnSta(() =>
            {
                var control = new NecessidadesCompraControl();
                Assert.NotNull(control);
            });
        }

        #endregion

        #region BUG-002 Tests - RBAC ESTOQUE_CRIAR TemPermissaoCodigo vs TemPermissao

        [Fact]
        public void Bug002_TemPermissaoCodigo_Administrador_DeveTerPermissaoEstoqueCriar()
        {
            var admin = new Funcionario
            {
                Id = 1,
                Nome = "Administrador Teste",
                PerfilAcesso = "Administrador",
                Ativo = true
            };

            var permService = new PermissionService(admin, (LoggerService?)null);

            var permitido = permService.TemPermissaoCodigo("ESTOQUE_CRIAR");
            Assert.True(permitido, "Administrador deve ter permissão para ESTOQUE_CRIAR via TemPermissaoCodigo.");
        }

        [Fact]
        public void Bug002_TemPermissao_ComCodigoAcao_DeveRetornarFalse_ProvandoDiferencaSemantica()
        {
            var admin = new Funcionario
            {
                Id = 1,
                Nome = "Administrador Teste",
                PerfilAcesso = "Administrador",
                Ativo = true
            };

            var permService = new PermissionService(admin, (LoggerService?)null);

            var permitidoModulo = permService.TemPermissao("ESTOQUE_CRIAR");
            Assert.False(permitidoModulo, "TemPermissao('ESTOQUE_CRIAR') deve falhar pois 'ESTOQUE_CRIAR' é ação, não módulo.");
        }

        [Fact]
        public void Bug002_TemPermissaoCodigo_UsuarioSemPermissao_DeveSerNegado_FailClosed()
        {
            var usuarioSemPermissao = new Funcionario
            {
                Id = 2,
                Nome = "Operador Sem Estoque",
                PerfilAcesso = "Operador",
                Ativo = true
            };

            var permService = new PermissionService(usuarioSemPermissao, (LoggerService?)null);

            var permitido = permService.TemPermissaoCodigo("ESTOQUE_CRIAR");
            Assert.False(permitido, "Perfil sem ESTOQUE_CRIAR deve ter acesso negado (fail-closed).");
        }

        [Fact]
        public void Bug002_CatalogoPecasViewModel_Admin_DeveHabilitarCriacaoDeProduto()
        {
            try
            {
                App.Session.StartSession(new Funcionario
                {
                    Id = 1,
                    Nome = "Admin Teste",
                    PerfilAcesso = "Administrador",
                    Ativo = true
                });

                var vm = new CatalogoPecasViewModel();
                Assert.True(vm.PodeCriarProduto, "CatalogoPecasViewModel.PodeCriarProduto deve ser true para Administrador.");
            }
            finally
            {
                App.Session.EndSession();
            }
        }

        [Fact]
        public void Bug002_CatalogoPecasViewModel_SemPermissao_NaoDeveHabilitarCriacaoDeProduto()
        {
            try
            {
                App.Session.StartSession(new Funcionario
                {
                    Id = 99,
                    Nome = "Usuario Bloqueado",
                    PerfilAcesso = "Visualizador",
                    Ativo = true
                });

                var vm = new CatalogoPecasViewModel();
                Assert.False(vm.PodeCriarProduto, "CatalogoPecasViewModel.PodeCriarProduto deve ser false para usuário sem permissão.");
            }
            finally
            {
                App.Session.EndSession();
            }
        }

        #endregion

        #region BUG-003 Tests - FerramentasControl inicialização sem NullReferenceException

        [Fact]
        public void Bug003_FerramentasControl_DeveInicializarSemNullReferenceException()
        {
            RunOnSta(() =>
            {
                var control = new FerramentasControl();
                Assert.NotNull(control);
            });
        }

        #endregion
    }
}
