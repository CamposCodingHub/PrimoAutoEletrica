using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services.Knowledge;
using PrimoAutoEletrica.UserControls;
using PrimoAutoEletrica.ViewModels;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        private void RunKnowledgeSearchChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "Search:Empty", () =>
            {
                var control = CreateSearchControlHosted(syntheticUser, out var host);
                try
                {
                    control.SmokeClear();
                    AssertSearchState(control, KnowledgeSearchUiState.NoQuery);
                    if (!string.Equals(control.ViewModel.StatusMessage, KnowledgeSearchService.EmptyQueryMessage, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Empty query must show Digite algo para pesquisar.");
                    }
                }
                finally { host.Close(); }
            });

            RunCheck(result, "Search:NoResults", () =>
            {
                var control = CreateSearchControlHosted(syntheticUser, out var host);
                try
                {
                    control.SmokeSearchAsync("XYZ_NAO_EXISTE_PRIMOX_999").GetAwaiter().GetResult();
                    AssertSearchState(control, KnowledgeSearchUiState.NoResults);
                    if (control.ViewModel.FlatResults.Count != 0)
                    {
                        throw new InvalidOperationException("NoResults must not invent hits.");
                    }
                }
                finally { host.Close(); }
            });

            RunCheck(result, "Search:D01", () =>
            {
                var control = CreateSearchControlHosted(syntheticUser, out var host);
                try
                {
                    control.SmokeSearchAsync("D01").GetAwaiter().GetResult();
                    AssertSearchState(control, KnowledgeSearchUiState.Found);
                    if (!control.ViewModel.FlatResults.Any(r => r.Code.Equals("D01", StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException("D01 not found in search results.");
                    }
                }
                finally { host.Close(); }
            });

            RunCheck(result, "Search:D17", () =>
            {
                var control = CreateSearchControlHosted(syntheticUser, out var host);
                try
                {
                    control.SmokeSearchAsync("D17").GetAwaiter().GetResult();
                    AssertSearchState(control, KnowledgeSearchUiState.Found);
                    if (!control.ViewModel.FlatResults.Any(r => r.Code.Equals("D17", StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException("D17 not found in search results.");
                    }
                }
                finally { host.Close(); }
            });

            RunCheck(result, "Search:Knowledge", () =>
            {
                var control = CreateSearchControlHosted(syntheticUser, out var host);
                try
                {
                    control.SmokeSearchAsync("queda de tensão").GetAwaiter().GetResult();
                    AssertSearchState(control, KnowledgeSearchUiState.Found);
                    if (control.ViewModel.FlatResults.Count == 0)
                    {
                        throw new InvalidOperationException("Knowledge search for queda de tensão returned empty.");
                    }
                    if (control.ViewModel.FlatResults.Any(r => string.IsNullOrWhiteSpace(r.SourceType) || string.IsNullOrWhiteSpace(r.SourceId)))
                    {
                        throw new InvalidOperationException("Every hit must expose SourceType+SourceId.");
                    }
                }
                finally { host.Close(); }
            });

            RunCheck(result, "Search:DiagnosticCase", () =>
            {
                var control = CreateSearchControlHosted(syntheticUser, out var host);
                try
                {
                    // Diagnostic cases may be empty in synthetic smoke DB — search still must not crash.
                    control.SmokeSearchAsync("partida").GetAwaiter().GetResult();
                    if (control.ViewModel.State == KnowledgeSearchUiState.Error)
                    {
                        throw new InvalidOperationException("DiagnosticCase search entered Error state unexpectedly.");
                    }
                }
                finally { host.Close(); }
            });

            RunCheck(result, "Search:OpenSource", () =>
            {
                var control = CreateSearchControlHosted(syntheticUser, out var host);
                try
                {
                    control.SmokeSearchAsync("D01").GetAwaiter().GetResult();
                    var hit = control.ViewModel.FlatResults.FirstOrDefault(r => r.Code.Equals("D01", StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidOperationException("D01 missing for OpenSource.");
                    // Traceability fields present; opening summary must not throw.
                    if (string.IsNullOrWhiteSpace(hit.SourceType) || string.IsNullOrWhiteSpace(hit.SourceId))
                    {
                        throw new InvalidOperationException("OpenSource requires SourceType/SourceId.");
                    }
                    // Do not ShowDialog in headless smoke — validate callable path via GetBySourceId when possible.
                    var svc = App.Services?.GetService(typeof(PrimoAutoEletrica.Services.Knowledge.IKnowledgeSearchService))
                        as PrimoAutoEletrica.Services.Knowledge.IKnowledgeSearchService;
                    if (svc != null)
                    {
                        // procedures from roteiros use SourceEntity DiagnosticoGuiadoRoteiro / code id
                        _ = svc.GetBySourceId(hit.SourceType, hit.SourceId, hasFinancePermission: true);
                    }
                }
                finally { host.Close(); }
            });

            RunCheck(result, "Search:Clear", () =>
            {
                var control = CreateSearchControlHosted(syntheticUser, out var host);
                try
                {
                    control.SmokeSearchAsync("D01").GetAwaiter().GetResult();
                    control.SmokeClear();
                    AssertSearchState(control, KnowledgeSearchUiState.NoQuery);
                    if (control.ViewModel.FlatResults.Count != 0 || !string.IsNullOrWhiteSpace(control.SearchBox.Text))
                    {
                        throw new InvalidOperationException("Clear must reset query and results.");
                    }
                }
                finally { host.Close(); }
            });

            RunCheck(result, "Search:BaseConhecimentoHost", () =>
            {
                var window = new MainWindow(syntheticUser);
                PrepareWindow(window);
                try
                {
                    var nav = new NavigationService(new PermissionService(syntheticUser, _logger, App.Database), _logger);
                    var page = nav.Navigate("BaseConhecimento") as BaseConhecimentoControl
                        ?? throw new InvalidOperationException("BaseConhecimento navigation failed.");
                    PrepareElement(page);
                    var search = FindElementByName<PrimoxKnowledgeSearchControl>(page, "PrimoxSearchControl")
                        ?? throw new InvalidOperationException("PrimoxSearchControl missing in BaseConhecimento.");
                    search.SmokeSearchAsync("D01").GetAwaiter().GetResult();
                    AssertSearchState(search, KnowledgeSearchUiState.Found);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private PrimoxKnowledgeSearchControl CreateSearchControlHosted(Funcionario user, out Window host)
        {
            host = new Window
            {
                Title = "C2.2 Search Smoke Host",
                Width = 1280,
                Height = 720,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ShowInTaskbar = false
            };
            Helpers.WindowOwnerHelper.ConfigureOwner(host);
            var control = new PrimoxKnowledgeSearchControl();
            host.Content = control;
            PrepareWindow(host);
            PrepareElement(control);
            return control;
        }

        private static void AssertSearchState(PrimoxKnowledgeSearchControl control, KnowledgeSearchUiState expected)
        {
            if (control.ViewModel.State != expected)
            {
                throw new InvalidOperationException(
                    $"Expected search state {expected}, got {control.ViewModel.State} ({control.ViewModel.StatusMessage}).");
            }
        }
    }
}
