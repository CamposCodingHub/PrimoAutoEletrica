using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Resources;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Servico central de localizacao (pt-BR / en-US / es-ES).
    /// Persistencia: %LOCALAPPDATA%\PrimoAutoEletrica\language_settings.json
    /// Fallback: pt-BR. UI culture muda; cultura de formatacao numerica permanece pt-BR (negocio BR).
    /// </summary>
    public partial class LocalizationService
    {
        private const string SettingsFileName = "language_settings.json";
        private const string DefaultLanguage = "pt-BR";

        private static LocalizationService? _instance;
        private ResourceManager? _resourceManager;
        private CultureInfo _currentCulture;
        private bool _suppressPersist;
        private static readonly CultureInfo FormatCulture = CultureInfo.GetCultureInfo("pt-BR");

        public static LocalizationService Instance
        {
            get
            {
                _instance ??= new LocalizationService();
                return _instance;
            }
        }

        public LocalizationService()
        {
            _currentCulture = CultureInfo.GetCultureInfo(DefaultLanguage);
            LoadResources();
            try
            {
                LoadSavedLanguage();
            }
            catch
            {
                ApplyCultureInternal(CultureInfo.GetCultureInfo(DefaultLanguage), persist: false);
            }
        }

        public CultureInfo CurrentCulture
        {
            get => _currentCulture;
            set
            {
                if (value == null)
                {
                    return;
                }

                var normalized = NormalizeCulture(value.Name);
                if (string.Equals(_currentCulture.Name, normalized.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                ApplyCultureInternal(normalized, persist: !_suppressPersist);
            }
        }

        public string CurrentLanguageCode => _currentCulture.Name;

        public event EventHandler? CultureChanged;

        private static string SettingsPath
        {
            get
            {
                try
                {
                    var root = App.RuntimeAppDataPath;
                    if (!string.IsNullOrWhiteSpace(root))
                    {
                        return Path.Combine(root, SettingsFileName);
                    }
                }
                catch
                {
                    // App ainda nao inicializado
                }

                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PrimoAutoEletrica",
                    SettingsFileName);
            }
        }

        private void LoadResources()
        {
            _resourceManager = new ResourceManager("PrimoAutoEletrica.Resources.Strings", typeof(LocalizationService).Assembly);
        }

        public void InitializeAtStartup()
        {
            LoadSavedLanguage();
        }

        public string GetString(string key, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            var value = ResolveString(key, _currentCulture)
                        ?? ResolveString(key, CultureInfo.GetCultureInfo(DefaultLanguage));

            if (string.IsNullOrEmpty(value))
            {
                return key;
            }

            return args.Length > 0 ? string.Format(CultureInfo.InvariantCulture, value, args) : value;
        }

        private string? ResolveString(string key, CultureInfo culture)
        {
            // Catalogo em memoria e a fonte completa (pt/en/es). .resx legado e overlay opcional.
            var map = BuildCatalog(culture);
            if (map.TryGetValue(key, out var catalogValue) && !string.IsNullOrEmpty(catalogValue))
            {
                return catalogValue;
            }

            try
            {
                if (_resourceManager != null)
                {
                    var resourceValue = _resourceManager.GetString(key, culture);
                    if (!string.IsNullOrEmpty(resourceValue))
                    {
                        return resourceValue;
                    }
                }
            }
            catch
            {
                // fallback ja tratado pelo chamador
            }

            return null;
        }

        public void SetLanguage(string languageCode)
        {
            var culture = NormalizeCulture(languageCode);
            ApplyCultureInternal(culture, persist: true);
        }

        public List<CultureInfo> GetAvailableLanguages()
        {
            return new List<CultureInfo>
            {
                CultureInfo.GetCultureInfo("pt-BR"),
                CultureInfo.GetCultureInfo("en-US"),
                CultureInfo.GetCultureInfo("es-ES")
            };
        }

        public bool IsSupported(string? languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return false;
            }

            var code = languageCode.Trim();
            if (code.Equals("pt", StringComparison.OrdinalIgnoreCase) ||
                code.Equals("pt-BR", StringComparison.OrdinalIgnoreCase) ||
                code.Equals("pt_br", StringComparison.OrdinalIgnoreCase) ||
                code.Equals("en", StringComparison.OrdinalIgnoreCase) ||
                code.Equals("en-US", StringComparison.OrdinalIgnoreCase) ||
                code.Equals("en_us", StringComparison.OrdinalIgnoreCase) ||
                code.Equals("es", StringComparison.OrdinalIgnoreCase) ||
                code.Equals("es-ES", StringComparison.OrdinalIgnoreCase) ||
                code.Equals("es_es", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private void LoadSavedLanguage()
        {
            var path = SettingsPath;
            if (!File.Exists(path))
            {
                ApplyCultureInternal(CultureInfo.GetCultureInfo(DefaultLanguage), persist: false);
                return;
            }

            try
            {
                var json = File.ReadAllText(path);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("language", out var langProp))
                {
                    var code = langProp.GetString();
                    ApplyCultureInternal(NormalizeCulture(code), persist: false);
                    return;
                }
            }
            catch
            {
                // fallback pt-BR
            }

            ApplyCultureInternal(CultureInfo.GetCultureInfo(DefaultLanguage), persist: false);
        }

        private void PersistLanguage(string languageCode)
        {
            try
            {
                var path = SettingsPath;
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var payload = JsonSerializer.Serialize(new { language = languageCode, updatedAt = DateTimeOffset.Now });
                File.WriteAllText(path, payload);
            }
            catch
            {
                // nao bloquear UI por falha de persistencia
            }
        }

        private void ApplyCultureInternal(CultureInfo culture, bool persist)
        {
            _suppressPersist = true;
            try
            {
                _currentCulture = culture;
                LoadResources();

                // UI language
                CultureInfo.CurrentUICulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;

                // Formatacao de negocio permanece BR (R$, datas, decimais fiscais)
                CultureInfo.CurrentCulture = FormatCulture;
                CultureInfo.DefaultThreadCurrentCulture = FormatCulture;

                CultureChanged?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                _suppressPersist = false;
            }

            if (persist)
            {
                PersistLanguage(culture.Name);
            }
        }

        private static CultureInfo NormalizeCulture(string? languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return CultureInfo.GetCultureInfo(DefaultLanguage);
            }

            var code = languageCode.Trim();
            if (code.Equals("pt", StringComparison.OrdinalIgnoreCase) ||
                code.Equals("pt-BR", StringComparison.OrdinalIgnoreCase) ||
                code.Equals("pt_br", StringComparison.OrdinalIgnoreCase))
            {
                return CultureInfo.GetCultureInfo("pt-BR");
            }

            if (code.Equals("en", StringComparison.OrdinalIgnoreCase) ||
                code.Equals("en-US", StringComparison.OrdinalIgnoreCase) ||
                code.Equals("en_us", StringComparison.OrdinalIgnoreCase))
            {
                return CultureInfo.GetCultureInfo("en-US");
            }

            if (code.Equals("es", StringComparison.OrdinalIgnoreCase) ||
                code.Equals("es-ES", StringComparison.OrdinalIgnoreCase) ||
                code.Equals("es_es", StringComparison.OrdinalIgnoreCase))
            {
                return CultureInfo.GetCultureInfo("es-ES");
            }

            try
            {
                var culture = CultureInfo.GetCultureInfo(code);
                var two = culture.TwoLetterISOLanguageName.ToLowerInvariant();
                return two switch
                {
                    "pt" => CultureInfo.GetCultureInfo("pt-BR"),
                    "en" => CultureInfo.GetCultureInfo("en-US"),
                    "es" => CultureInfo.GetCultureInfo("es-ES"),
                    _ => CultureInfo.GetCultureInfo(DefaultLanguage)
                };
            }
            catch
            {
                return CultureInfo.GetCultureInfo(DefaultLanguage);
            }
        }

        private static Dictionary<string, string> BuildCatalog(CultureInfo culture)
        {
            var lang = culture.TwoLetterISOLanguageName.ToLowerInvariant();
            var map = lang switch
            {
                "en" => En(),
                "es" => Es(),
                _ => Pt()
            };

            var modules = lang switch
            {
                "en" => ModulesEn(),
                "es" => ModulesEs(),
                _ => ModulesPt()
            };

            foreach (var pair in modules)
            {
                map[pair.Key] = pair.Value;
            }

            var interaction = lang switch
            {
                "en" => InteractionEn(),
                "es" => InteractionEs(),
                _ => InteractionPt()
            };

            foreach (var pair in interaction)
            {
                map[pair.Key] = pair.Value;
            }

            var content = lang switch
            {
                "en" => ContentEn(),
                "es" => ContentEs(),
                _ => ContentPt()
            };

            foreach (var pair in content)
            {
                map[pair.Key] = pair.Value;
            }

            var closure = lang switch
            {
                "en" => ClosureEn(),
                "es" => ClosureEs(),
                _ => ClosurePt()
            };

            foreach (var pair in closure)
            {
                map[pair.Key] = pair.Value;
            }

            var gate = lang switch
            {
                "en" => GateEn(),
                "es" => GateEs(),
                _ => GatePt()
            };

            foreach (var pair in gate)
            {
                map[pair.Key] = pair.Value;
            }

            return map;
        }

        private static Dictionary<string, string> Pt() => new()
        {
            ["Dashboard"] = "Painel",
            ["Clients"] = "Clientes",
            ["Inventory"] = "Estoque",
            ["Employees"] = "Funcionários",
            ["Finance"] = "Financeiro",
            ["Reports"] = "Relatórios",
            ["Settings"] = "Configurações",
            ["SystemName"] = "PRIMOX Workshop",
            ["ProductName"] = "PRIMOX Workshop",
            ["Login"] = "Entrar",
            ["Logout"] = "Sair",
            ["Save"] = "Salvar",
            ["Cancel"] = "Cancelar",
            ["Delete"] = "Excluir",
            ["Edit"] = "Editar",
            ["Add"] = "Adicionar",
            ["Search"] = "Buscar",
            ["SearchPlaceholder"] = "Buscar...",
            ["Filter"] = "Filtrar",
            ["Export"] = "Exportar",
            ["Import"] = "Importar",
            ["Print"] = "Imprimir",
            ["Close"] = "Fechar",
            ["Yes"] = "Sim",
            ["No"] = "Não",
            ["Ok"] = "OK",
            ["Error"] = "Erro",
            ["Warning"] = "Aviso",
            ["Information"] = "Informação",
            ["Success"] = "Sucesso",
            ["Loading"] = "Carregando...",
            ["PleaseWait"] = "Por favor, aguarde...",
            ["NoDataFound"] = "Nenhum dado encontrado",
            ["AreYouSure"] = "Tem certeza?",
            ["OperationCompleted"] = "Operação concluída com sucesso",
            ["OperationFailed"] = "Operação falhou",
            ["RequiredField"] = "Campo obrigatório",
            ["InvalidValue"] = "Valor inválido",
            ["Refresh"] = "Atualizar",
            ["Appointments"] = "Agendamentos",
            ["Quotes"] = "Orçamentos",
            ["WorkOrders"] = "Ordens de Serviço",
            ["Kanban"] = "Kanban da Oficina",
            ["Pdv"] = "PDV",
            ["ImportNfe"] = "Importar NF-e",
            ["FiscalOps"] = "Operações Fiscais",
            ["Vehicles"] = "Veículos",
            ["Technical"] = "Auto Elétrica Técnica",
            ["PartsCatalog"] = "Catálogo de Peças",
            ["Suppliers"] = "Fornecedores",
            ["Help"] = "Ajuda",
            ["SectionOperation"] = "OPERAÇÃO",
            ["SectionRegisters"] = "CADASTROS",
            ["SectionManagement"] = "GESTÃO",
            ["SectionSystem"] = "SISTEMA",
            ["SectionHelp"] = "AJUDA",
            ["Session"] = "Sessão",
            ["User"] = "Usuário",
            ["Profile"] = "Perfil",
            ["DateLabel"] = "Data",
            ["Theme"] = "Tema",
            ["Comfort"] = "Conforto",
            ["Operations"] = "Operações",
            ["Language"] = "Idioma",
            ["CommandPalette"] = "Paleta de comandos",
            ["Email"] = "E-mail",
            ["Password"] = "Senha",
            ["RememberMe"] = "Lembrar-me",
            ["New"] = "Novo",
            ["View"] = "Ver",
            ["Back"] = "Voltar",
            ["Confirm"] = "Confirmar",
            ["Total"] = "Total",
            ["Subtotal"] = "Subtotal",
            ["Discount"] = "Desconto",
            ["Payment"] = "Pagamento",
            ["Status"] = "Status",
            ["Actions"] = "Ações",
            ["Details"] = "Detalhes",
            ["Description"] = "Descrição",
            ["Quantity"] = "Quantidade",
            ["Price"] = "Preço",
            ["Date"] = "Data",
            ["Name"] = "Nome",
            ["Phone"] = "Telefone",
            ["Address"] = "Endereço",
            ["Plate"] = "Placa",
            ["Mileage"] = "Quilometragem",
            ["Technician"] = "Técnico",
            ["Part"] = "Peça",
            ["Service"] = "Serviço",
            ["Stock"] = "Estoque",
            ["Inbound"] = "Entrada",
            ["Outbound"] = "Saída",
            ["Balance"] = "Saldo",
            ["WorkOrder"] = "Ordem de Serviço",
            ["Quote"] = "Orçamento",
            ["EmptyState"] = "Nada por aqui ainda",
            ["SelectLanguage"] = "Selecionar idioma",
            ["CollapseMenu"] = "Recolher menu",
            ["ExpandMenu"] = "Expandir menu",
            ["OpenModule"] = "Abrir {0}",
            ["KeyboardShortcuts"] = "Atalhos do teclado",
            ["KeyboardShortcutsSubtitle"] = "Lista dos atalhos implementados",
            ["SearchRecords"] = "Buscar registros",
            ["SearchRecordsSubtitle"] = "Foca a busca global do cabeçalho",
            ["RefreshModule"] = "Atualizar módulo",
            ["RefreshModuleSubtitle"] = "Recarrega a tela atual",
            ["OpenHelp"] = "Abrir Ajuda",
            ["OpenHelpSubtitle"] = "Central de ajuda do sistema",
            ["OpenSettings"] = "Abrir Configurações",
            ["OpenSettingsSubtitle"] = "Preferências e administração do sistema",
            ["NewClient"] = "Novo cliente",
            ["NewClientSubtitle"] = "Abre o cadastro de cliente",
            ["NewVehicle"] = "Novo veículo",
            ["NewVehicleSubtitle"] = "Abre o cadastro de veículo",
            ["CategoryHelp"] = "Ajuda",
            ["CategorySearch"] = "Busca",
            ["CategorySystem"] = "Sistema",
            ["CategoryCreate"] = "Criar",
            ["ThemeLight"] = "☀ Claro",
            ["ThemeDark"] = "☾ Escuro",
            ["DensityComfort"] = "Conforto",
            ["DensityCompact"] = "Compacto"
        };

        private static Dictionary<string, string> En() => new()
        {
            ["Dashboard"] = "Dashboard",
            ["Clients"] = "Clients",
            ["Inventory"] = "Inventory",
            ["Employees"] = "Employees",
            ["Finance"] = "Finance",
            ["Reports"] = "Reports",
            ["Settings"] = "Settings",
            ["SystemName"] = "PRIMOX Workshop",
            ["ProductName"] = "PRIMOX Workshop",
            ["Login"] = "Sign in",
            ["Logout"] = "Sign out",
            ["Save"] = "Save",
            ["Cancel"] = "Cancel",
            ["Delete"] = "Delete",
            ["Edit"] = "Edit",
            ["Add"] = "Add",
            ["Search"] = "Search",
            ["SearchPlaceholder"] = "Search...",
            ["Filter"] = "Filter",
            ["Export"] = "Export",
            ["Import"] = "Import",
            ["Print"] = "Print",
            ["Close"] = "Close",
            ["Yes"] = "Yes",
            ["No"] = "No",
            ["Ok"] = "OK",
            ["Error"] = "Error",
            ["Warning"] = "Warning",
            ["Information"] = "Information",
            ["Success"] = "Success",
            ["Loading"] = "Loading...",
            ["PleaseWait"] = "Please wait...",
            ["NoDataFound"] = "No data found",
            ["AreYouSure"] = "Are you sure?",
            ["OperationCompleted"] = "Operation completed successfully",
            ["OperationFailed"] = "Operation failed",
            ["RequiredField"] = "Required field",
            ["InvalidValue"] = "Invalid value",
            ["Refresh"] = "Refresh",
            ["Appointments"] = "Appointments",
            ["Quotes"] = "Quotes",
            ["WorkOrders"] = "Work Orders",
            ["Kanban"] = "Shop Kanban",
            ["Pdv"] = "POS",
            ["ImportNfe"] = "Import NF-e",
            ["FiscalOps"] = "Fiscal Operations",
            ["Vehicles"] = "Vehicles",
            ["Technical"] = "Auto Electrical Tech",
            ["PartsCatalog"] = "Parts Catalog",
            ["Suppliers"] = "Suppliers",
            ["Help"] = "Help",
            ["SectionOperation"] = "OPERATIONS",
            ["SectionRegisters"] = "REGISTERS",
            ["SectionManagement"] = "MANAGEMENT",
            ["SectionSystem"] = "SYSTEM",
            ["SectionHelp"] = "HELP",
            ["Session"] = "Session",
            ["User"] = "User",
            ["Profile"] = "Profile",
            ["DateLabel"] = "Date",
            ["Theme"] = "Theme",
            ["Comfort"] = "Comfort",
            ["Operations"] = "Operations",
            ["Language"] = "Language",
            ["CommandPalette"] = "Command palette",
            ["Email"] = "Email",
            ["Password"] = "Password",
            ["RememberMe"] = "Remember me",
            ["New"] = "New",
            ["View"] = "View",
            ["Back"] = "Back",
            ["Confirm"] = "Confirm",
            ["Total"] = "Total",
            ["Subtotal"] = "Subtotal",
            ["Discount"] = "Discount",
            ["Payment"] = "Payment",
            ["Status"] = "Status",
            ["Actions"] = "Actions",
            ["Details"] = "Details",
            ["Description"] = "Description",
            ["Quantity"] = "Quantity",
            ["Price"] = "Price",
            ["Date"] = "Date",
            ["Name"] = "Name",
            ["Phone"] = "Phone",
            ["Address"] = "Address",
            ["Plate"] = "License plate",
            ["Mileage"] = "Mileage",
            ["Technician"] = "Technician",
            ["Part"] = "Part",
            ["Service"] = "Service",
            ["Stock"] = "Stock",
            ["Inbound"] = "Inbound",
            ["Outbound"] = "Outbound",
            ["Balance"] = "Balance",
            ["WorkOrder"] = "Work Order",
            ["Quote"] = "Quote",
            ["EmptyState"] = "Nothing here yet",
            ["SelectLanguage"] = "Select language",
            ["CollapseMenu"] = "Collapse menu",
            ["ExpandMenu"] = "Expand menu",
            ["OpenModule"] = "Open {0}",
            ["KeyboardShortcuts"] = "Keyboard shortcuts",
            ["KeyboardShortcutsSubtitle"] = "List of implemented shortcuts",
            ["SearchRecords"] = "Search records",
            ["SearchRecordsSubtitle"] = "Focuses the header global search",
            ["RefreshModule"] = "Refresh module",
            ["RefreshModuleSubtitle"] = "Reloads the current screen",
            ["OpenHelp"] = "Open Help",
            ["OpenHelpSubtitle"] = "System help center",
            ["OpenSettings"] = "Open Settings",
            ["OpenSettingsSubtitle"] = "Preferences and system administration",
            ["NewClient"] = "New client",
            ["NewClientSubtitle"] = "Opens client registration",
            ["NewVehicle"] = "New vehicle",
            ["NewVehicleSubtitle"] = "Opens vehicle registration",
            ["CategoryHelp"] = "Help",
            ["CategorySearch"] = "Search",
            ["CategorySystem"] = "System",
            ["CategoryCreate"] = "Create",
            ["ThemeLight"] = "☀ Light",
            ["ThemeDark"] = "☾ Dark",
            ["DensityComfort"] = "Comfort",
            ["DensityCompact"] = "Compact"
        };

        private static Dictionary<string, string> Es() => new()
        {
            ["Dashboard"] = "Panel",
            ["Clients"] = "Clientes",
            ["Inventory"] = "Inventario",
            ["Employees"] = "Empleados",
            ["Finance"] = "Finanzas",
            ["Reports"] = "Informes",
            ["Settings"] = "Configuración",
            ["SystemName"] = "PRIMOX Workshop",
            ["ProductName"] = "PRIMOX Workshop",
            ["Login"] = "Iniciar sesión",
            ["Logout"] = "Cerrar sesión",
            ["Save"] = "Guardar",
            ["Cancel"] = "Cancelar",
            ["Delete"] = "Eliminar",
            ["Edit"] = "Editar",
            ["Add"] = "Agregar",
            ["Search"] = "Buscar",
            ["SearchPlaceholder"] = "Buscar...",
            ["Filter"] = "Filtrar",
            ["Export"] = "Exportar",
            ["Import"] = "Importar",
            ["Print"] = "Imprimir",
            ["Close"] = "Cerrar",
            ["Yes"] = "Sí",
            ["No"] = "No",
            ["Ok"] = "OK",
            ["Error"] = "Error",
            ["Warning"] = "Advertencia",
            ["Information"] = "Información",
            ["Success"] = "Éxito",
            ["Loading"] = "Cargando...",
            ["PleaseWait"] = "Por favor, espere...",
            ["NoDataFound"] = "No se encontraron datos",
            ["AreYouSure"] = "¿Está seguro?",
            ["OperationCompleted"] = "Operación completada con éxito",
            ["OperationFailed"] = "La operación falló",
            ["RequiredField"] = "Campo obligatorio",
            ["InvalidValue"] = "Valor inválido",
            ["Refresh"] = "Actualizar",
            ["Appointments"] = "Citas",
            ["Quotes"] = "Presupuestos",
            ["WorkOrders"] = "Órdenes de Trabajo",
            ["Kanban"] = "Kanban del Taller",
            ["Pdv"] = "TPV",
            ["ImportNfe"] = "Importar NF-e",
            ["FiscalOps"] = "Operaciones Fiscales",
            ["Vehicles"] = "Vehículos",
            ["Technical"] = "Autoeléctrica Técnica",
            ["PartsCatalog"] = "Catálogo de Piezas",
            ["Suppliers"] = "Proveedores",
            ["Help"] = "Ayuda",
            ["SectionOperation"] = "OPERACIÓN",
            ["SectionRegisters"] = "REGISTROS",
            ["SectionManagement"] = "GESTIÓN",
            ["SectionSystem"] = "SISTEMA",
            ["SectionHelp"] = "AYUDA",
            ["Session"] = "Sesión",
            ["User"] = "Usuario",
            ["Profile"] = "Perfil",
            ["DateLabel"] = "Fecha",
            ["Theme"] = "Tema",
            ["Comfort"] = "Confort",
            ["Operations"] = "Operaciones",
            ["Language"] = "Idioma",
            ["CommandPalette"] = "Paleta de comandos",
            ["Email"] = "Correo",
            ["Password"] = "Contraseña",
            ["RememberMe"] = "Recordarme",
            ["New"] = "Nuevo",
            ["View"] = "Ver",
            ["Back"] = "Volver",
            ["Confirm"] = "Confirmar",
            ["Total"] = "Total",
            ["Subtotal"] = "Subtotal",
            ["Discount"] = "Descuento",
            ["Payment"] = "Pago",
            ["Status"] = "Estado",
            ["Actions"] = "Acciones",
            ["Details"] = "Detalles",
            ["Description"] = "Descripción",
            ["Quantity"] = "Cantidad",
            ["Price"] = "Precio",
            ["Date"] = "Fecha",
            ["Name"] = "Nombre",
            ["Phone"] = "Teléfono",
            ["Address"] = "Dirección",
            ["Plate"] = "Matrícula",
            ["Mileage"] = "Kilometraje",
            ["Technician"] = "Técnico",
            ["Part"] = "Pieza",
            ["Service"] = "Servicio",
            ["Stock"] = "Stock",
            ["Inbound"] = "Entrada",
            ["Outbound"] = "Salida",
            ["Balance"] = "Saldo",
            ["WorkOrder"] = "Orden de Trabajo",
            ["Quote"] = "Presupuesto",
            ["EmptyState"] = "Todavía no hay nada aquí",
            ["SelectLanguage"] = "Seleccionar idioma",
            ["CollapseMenu"] = "Contraer menú",
            ["ExpandMenu"] = "Expandir menú",
            ["OpenModule"] = "Abrir {0}",
            ["KeyboardShortcuts"] = "Atajos de teclado",
            ["KeyboardShortcutsSubtitle"] = "Lista de atajos implementados",
            ["SearchRecords"] = "Buscar registros",
            ["SearchRecordsSubtitle"] = "Enfoca la búsqueda global del encabezado",
            ["RefreshModule"] = "Actualizar módulo",
            ["RefreshModuleSubtitle"] = "Recarga la pantalla actual",
            ["OpenHelp"] = "Abrir Ayuda",
            ["OpenHelpSubtitle"] = "Centro de ayuda del sistema",
            ["OpenSettings"] = "Abrir Configuración",
            ["OpenSettingsSubtitle"] = "Preferencias y administración del sistema",
            ["NewClient"] = "Nuevo cliente",
            ["NewClientSubtitle"] = "Abre el registro de cliente",
            ["NewVehicle"] = "Nuevo vehículo",
            ["NewVehicleSubtitle"] = "Abre el registro de vehículo",
            ["CategoryHelp"] = "Ayuda",
            ["CategorySearch"] = "Búsqueda",
            ["CategorySystem"] = "Sistema",
            ["CategoryCreate"] = "Crear",
            ["ThemeLight"] = "☀ Claro",
            ["ThemeDark"] = "☾ Oscuro",
            ["DensityComfort"] = "Confort",
            ["DensityCompact"] = "Compacto"
        };
    }
}
