using System;
using System.IO;
using System.Windows;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        private void RunSidebarShellChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "Sidebar:LayoutPersistenciaCompacta", () =>
            {
                MainWindow? window = null;
                var caminho = Path.Combine(App.RuntimeAppDataPath, "sidebar_settings.json");

                try
                {
                    if (File.Exists(caminho))
                    {
                        File.Delete(caminho);
                    }

                    var layout = new SidebarLayoutService();
                    layout.ApplyExpanded(true);

                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);

                    var alturaBarra = window.GetCommandBarHeightForAutomation();
                    if (Math.Abs(alturaBarra - 56d) > 0.5)
                    {
                        throw new InvalidOperationException($"Command Bar deveria ter 56px; obtido {alturaBarra}.");
                    }

                    var larguraExpandida = window.GetSidebarWidthForAutomation();
                    if (Math.Abs(larguraExpandida - SidebarLayoutService.ExpandedWidth) > 0.5)
                    {
                        throw new InvalidOperationException($"Sidebar expandida deveria ter {SidebarLayoutService.ExpandedWidth}px; obtido {larguraExpandida}.");
                    }

                    if (!window.IsSidebarExpandedForAutomation())
                    {
                        throw new InvalidOperationException("Sidebar deveria iniciar expandida sem preferencia salva.");
                    }

                    window.ToggleSidebarForAutomation();
                    WaitForUiIdle();

                    var larguraCompacta = window.GetSidebarWidthForAutomation();
                    if (Math.Abs(larguraCompacta - SidebarLayoutService.CollapsedWidth) > 0.5
                        || window.IsSidebarExpandedForAutomation())
                    {
                        throw new InvalidOperationException($"Sidebar compacta deveria ter {SidebarLayoutService.CollapsedWidth}px.");
                    }

                    if (!File.Exists(caminho))
                    {
                        throw new InvalidOperationException("Preferencia da sidebar nao foi persistida.");
                    }

                    window.Close();
                    window = null;

                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);

                    if (window.IsSidebarExpandedForAutomation()
                        || Math.Abs(window.GetSidebarWidthForAutomation() - SidebarLayoutService.CollapsedWidth) > 0.5)
                    {
                        throw new InvalidOperationException("Preferencia compacta nao foi recarregada ao reiniciar o shell.");
                    }

                    if (!window.NavigateToModuleForAutomation("Clientes", forceReload: true)
                        || !window.IsModuleHighlightedForAutomation("Clientes"))
                    {
                        throw new InvalidOperationException("Estado ativo da sidebar falhou apos navegacao.");
                    }

                    // Validacao leve de viewport 1366×768 (sem clipping estrutural do shell).
                    window.Width = 1366;
                    window.Height = 768;
                    window.WindowState = WindowState.Normal;
                    window.UpdateLayout();
                    WaitForUiIdle();

                    if (window.GetSidebarWidthForAutomation() > 240.5
                        || Math.Abs(window.GetCommandBarHeightForAutomation() - 56d) > 0.5)
                    {
                        throw new InvalidOperationException("Shell em 1366x768 extrapolou larguras/alturas do PRIMOX.");
                    }
                }
                finally
                {
                    if (window?.IsVisible == true)
                    {
                        window.Close();
                    }

                    try
                    {
                        var restore = new SidebarLayoutService();
                        restore.ApplyExpanded(true);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            });
        }

        private void RunCommandCenterShellChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "CommandCenter:AtalhoCtrlKLista", () =>
            {
                MainWindow? window = null;
                try
                {
                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);

                    var count = window.ValidateCommandCenterForAutomation();
                    if (count < 3)
                    {
                        throw new InvalidOperationException($"Command Center listou poucos itens ({count}).");
                    }
                }
                finally
                {
                    if (window?.IsVisible == true)
                    {
                        window.Close();
                    }
                }
            });
        }
    }
}
