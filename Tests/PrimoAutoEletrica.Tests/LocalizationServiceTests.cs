using System;
using System.Globalization;
using System.IO;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests
{
    public class LocalizationServiceTests
    {
        [Fact]
        public void LocalizationService_Instance_ShouldNotBeNull()
        {
            Assert.NotNull(LocalizationService.Instance);
        }

        [Fact]
        public void DefaultLanguage_ShouldBePtBrWhenInvalid()
        {
            var service = LocalizationService.Instance;
            service.SetLanguage("xx-INVALID");
            Assert.Equal("pt-BR", service.CurrentCulture.Name);
        }

        [Fact]
        public void GetString_WithValidKey_ShouldReturnLocalizedString()
        {
            var service = LocalizationService.Instance;
            service.SetLanguage("pt-BR");
            Assert.Equal("Painel", service.GetString("Dashboard"));
        }

        [Fact]
        public void GetString_WithInvalidKey_ShouldReturnKey()
        {
            var service = LocalizationService.Instance;
            var invalidKey = "InvalidKey_12345";
            Assert.Equal(invalidKey, service.GetString(invalidKey));
        }

        [Fact]
        public void SetLanguage_ChangingLanguage_ShouldUpdateCulture()
        {
            var service = LocalizationService.Instance;
            service.SetLanguage("pt-BR");
            service.SetLanguage("en-US");
            Assert.Equal("en-US", service.CurrentCulture.Name);
            service.SetLanguage("pt-BR");
        }

        [Fact]
        public void GetAvailableLanguages_ShouldReturnSupportedLanguages()
        {
            var languages = LocalizationService.Instance.GetAvailableLanguages();
            Assert.Equal(3, languages.Count);
            Assert.Contains(languages, l => l.Name == "pt-BR");
            Assert.Contains(languages, l => l.Name == "en-US");
            Assert.Contains(languages, l => l.Name == "es-ES");
        }

        [Fact]
        public void GetString_WithEnglishLanguage_ShouldReturnEnglishTranslation()
        {
            var service = LocalizationService.Instance;
            service.SetLanguage("en-US");
            Assert.Equal("Dashboard", service.GetString("Dashboard"));
            Assert.Equal("Clients", service.GetString("Clients"));
            Assert.Equal("Work Orders", service.GetString("WorkOrders"));
            service.SetLanguage("pt-BR");
        }

        [Fact]
        public void GetString_WithSpanishLanguage_ShouldReturnSpanishTranslation()
        {
            var service = LocalizationService.Instance;
            service.SetLanguage("es-ES");
            Assert.Equal("Panel", service.GetString("Dashboard"));
            Assert.Equal("Clientes", service.GetString("Clients"));
            Assert.Equal("Órdenes de Trabajo", service.GetString("WorkOrders"));
            service.SetLanguage("pt-BR");
        }

        [Fact]
        public void GetString_WithParameters_ShouldFormatString()
        {
            var service = LocalizationService.Instance;
            service.SetLanguage("pt-BR");
            Assert.Equal("Abrir Painel", service.GetString("OpenModule", "Painel"));
        }

        [Fact]
        public void CultureChangedEvent_ShouldFireWhenLanguageChanges()
        {
            var service = LocalizationService.Instance;
            service.SetLanguage("pt-BR");
            var eventFired = false;
            void Handler(object? s, EventArgs e) => eventFired = true;
            service.CultureChanged += Handler;
            try
            {
                service.SetLanguage("en-US");
                Assert.True(eventFired);
            }
            finally
            {
                service.CultureChanged -= Handler;
                service.SetLanguage("pt-BR");
            }
        }

        [Fact]
        public void RuntimeSwitch_PtEnEsPt_ShouldNotThrow()
        {
            var service = LocalizationService.Instance;
            foreach (var code in new[] { "pt-BR", "en-US", "es-ES", "pt-BR", "en-US", "es-ES", "pt-BR", "en-US", "es-ES", "pt-BR" })
            {
                service.SetLanguage(code);
                Assert.Equal(code, service.CurrentLanguageCode);
                Assert.False(string.IsNullOrWhiteSpace(service.GetString("Logout")));
            }
        }

        [Fact]
        public void Fallback_MissingKey_NeverEmpty()
        {
            var service = LocalizationService.Instance;
            service.SetLanguage("en-US");
            var value = service.GetString("TotallyMissingKey_XYZ");
            Assert.False(string.IsNullOrEmpty(value));
            Assert.Equal("TotallyMissingKey_XYZ", value);
            service.SetLanguage("pt-BR");
        }

        [Fact]
        public void IsSupported_ShouldValidateOfficialLanguages()
        {
            var service = LocalizationService.Instance;
            Assert.True(service.IsSupported("pt-BR"));
            Assert.True(service.IsSupported("en"));
            Assert.True(service.IsSupported("es-ES"));
            Assert.False(service.IsSupported("fr-FR"));
            Assert.False(service.IsSupported(null));
        }

        [Fact]
        public void Persistence_WritesLanguageSettingsJson()
        {
            var service = LocalizationService.Instance;
            service.SetLanguage("es-ES");

            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PrimoAutoEletrica",
                "language_settings.json");

            Assert.True(File.Exists(path));
            var json = File.ReadAllText(path);
            Assert.Contains("es-ES", json, StringComparison.OrdinalIgnoreCase);

            service.SetLanguage("pt-BR");
        }

        [Fact]
        public void FormatCulture_RemainsPtBrForBusinessData()
        {
            var service = LocalizationService.Instance;
            service.SetLanguage("en-US");
            Assert.Equal("en-US", CultureInfo.CurrentUICulture.Name);
            Assert.Equal("pt-BR", CultureInfo.CurrentCulture.Name);
            service.SetLanguage("pt-BR");
        }

        [Fact]
        public void LanguageManager_ChangeLanguage_AcceptsFullCultureCodes()
        {
            LanguageManager.Instance.ChangeLanguage("en-US");
            Assert.Equal("en-US", LocalizationService.Instance.CurrentLanguageCode);
            LanguageManager.Instance.ChangeLanguage("pt-BR");
            Assert.Equal("pt-BR", LocalizationService.Instance.CurrentLanguageCode);
        }

        [Fact]
        public void ShellKeys_ExistInAllLanguages()
        {
            var service = LocalizationService.Instance;
            var keys = new[]
            {
                "Dashboard", "Clients", "Vehicles", "WorkOrders", "Quotes", "Pdv",
                "Inventory", "Finance", "Suppliers", "Employees", "Appointments",
                "Reports", "PartsCatalog", "ImportNfe", "Help", "Settings", "Logout"
            };

            foreach (var lang in new[] { "pt-BR", "en-US", "es-ES" })
            {
                service.SetLanguage(lang);
                foreach (var key in keys)
                {
                    var value = service.GetString(key);
                    Assert.False(string.IsNullOrWhiteSpace(value));
                    // Em en-US algumas labels sao iguais a chave (ex.: Dashboard) — isso e valido.
                    if (!string.Equals(lang, "en-US", StringComparison.OrdinalIgnoreCase))
                    {
                        Assert.NotEqual(key, value);
                    }
                }
            }

            service.SetLanguage("pt-BR");
        }

        [Fact]
        public void ModuleCatalog_Titles_ExistInAllLanguages()
        {
            var service = LocalizationService.Instance;
            var keys = new[]
            {
                "OperationsCenter", "ClientsTitle", "VehiclesTitle", "WorkOrdersTitle", "QuotesTitle",
                "PdvTitle", "InventoryTitle", "FinanceTitle", "SuppliersTitle", "EmployeesTitle",
                "AppointmentsTitle", "ReportsTitle", "PartsCatalogTitle", "ImportNfeTitle",
                "SettingsTitle", "HelpTitle", "NewClient", "NewOs", "FinalizeSale", "OsStatusInDiagnosis"
            };

            foreach (var lang in new[] { "pt-BR", "en-US", "es-ES" })
            {
                service.SetLanguage(lang);
                foreach (var key in keys)
                {
                    var value = service.GetString(key);
                    Assert.False(string.IsNullOrWhiteSpace(value));
                    Assert.NotEqual(key, value);
                }
            }

            service.SetLanguage("pt-BR");
            Assert.Equal("Em diagnóstico", service.GetString("OsStatusInDiagnosis"));
            service.SetLanguage("en-US");
            Assert.Equal("In diagnosis", service.GetString("OsStatusInDiagnosis"));
            service.SetLanguage("es-ES");
            Assert.Equal("En diagnóstico", service.GetString("OsStatusInDiagnosis"));
            service.SetLanguage("pt-BR");
        }

        [Fact]
        public void I18n04_CriticalActionKeys_ExistInAllLanguages()
        {
            var service = LocalizationService.Instance;
            var keys = new[]
            {
                "SaveDraft", "SaveChanges", "SaveEmitente", "SaveSignature", "SaveNewPassword",
                "CancelSaleShortcut", "EscCancel", "NewEmployee", "ConfigurePermissions",
                "SearchClientShortcut", "ManageProfiles", "CollapseMenu", "ConfirmExit"
            };

            foreach (var lang in new[] { "pt-BR", "en-US", "es-ES" })
            {
                service.SetLanguage(lang);
                foreach (var key in keys)
                {
                    var value = service.GetString(key);
                    Assert.False(string.IsNullOrWhiteSpace(value));
                    Assert.NotEqual(key, value);
                }
            }

            service.SetLanguage("en-US");
            Assert.Equal("Save draft", service.GetString("SaveDraft"));
            Assert.Equal("New employee", service.GetString("NewEmployee"));
            service.SetLanguage("es-ES");
            Assert.Equal("Guardar borrador", service.GetString("SaveDraft"));
            service.SetLanguage("pt-BR");
        }


        [Fact]
        public void WorkOrderStatusLocalizer_DoesNotChangeInternalIds()
        {
            LocalizationService.Instance.SetLanguage("en-US");
            Assert.Equal("In diagnosis", WorkOrderStatusLocalizer.Display("Em diagnostico"));
            Assert.Equal("In diagnosis", WorkOrderStatusLocalizer.Display("Em diagnóstico"));
            LocalizationService.Instance.SetLanguage("pt-BR");
            Assert.Equal("Em diagnóstico", WorkOrderStatusLocalizer.Display("Em diagnostico"));
        }
    }
}
