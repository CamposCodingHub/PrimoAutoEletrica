using System.ComponentModel;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Helpers
{
    /// <summary>
    /// Helper de localizacao para bindings XAML (StaticResource LocalizationHelper).
    /// Notifica PropertyChanged(null) em CultureChanged para refrescar todas as propriedades.
    /// </summary>
    public class LocalizationHelper : INotifyPropertyChanged
    {
        private static LocalizationHelper? _instance;
        private readonly LocalizationService _localizationService;

        public static LocalizationHelper Instance
        {
            get
            {
                _instance ??= new LocalizationHelper();
                return _instance;
            }
        }

        public LocalizationHelper()
        {
            _localizationService = LocalizationService.Instance;
            _localizationService.CultureChanged += (_, _) => OnPropertyChanged(null);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string GetString(string key, params object[] args)
        {
            return _localizationService.GetString(key, args);
        }

        public void SetLanguage(string languageCode)
        {
            _localizationService.SetLanguage(languageCode);
        }

        public string Dashboard => GetString("Dashboard");
        public string Clients => GetString("Clients");
        public string Inventory => GetString("Inventory");
        public string Employees => GetString("Employees");
        public string Finance => GetString("Finance");
        public string Reports => GetString("Reports");
        public string Settings => GetString("Settings");
        public string SystemName => GetString("SystemName");
        public string ProductName => GetString("ProductName");
        public string Login => GetString("Login");
        public string Logout => GetString("Logout");
        public string Save => GetString("Save");
        public string Cancel => GetString("Cancel");
        public string Delete => GetString("Delete");
        public string Edit => GetString("Edit");
        public string Add => GetString("Add");
        public string Search => GetString("Search");
        public string SearchPlaceholder => GetString("SearchPlaceholder");
        public string Filter => GetString("Filter");
        public string Export => GetString("Export");
        public string Import => GetString("Import");
        public string Print => GetString("Print");
        public string Close => GetString("Close");
        public string Yes => GetString("Yes");
        public string No => GetString("No");
        public string Ok => GetString("Ok");
        public string Error => GetString("Error");
        public string Warning => GetString("Warning");
        public string Information => GetString("Information");
        public string Success => GetString("Success");
        public string Loading => GetString("Loading");
        public string PleaseWait => GetString("PleaseWait");
        public string NoDataFound => GetString("NoDataFound");
        public string AreYouSure => GetString("AreYouSure");
        public string OperationCompleted => GetString("OperationCompleted");
        public string OperationFailed => GetString("OperationFailed");
        public string RequiredField => GetString("RequiredField");
        public string InvalidValue => GetString("InvalidValue");
        public string Refresh => GetString("Refresh");
        public string Appointments => GetString("Appointments");
        public string Quotes => GetString("Quotes");
        public string WorkOrders => GetString("WorkOrders");
        public string Kanban => GetString("Kanban");
        public string Pdv => GetString("Pdv");
        public string ImportNfe => GetString("ImportNfe");
        public string FiscalOps => GetString("FiscalOps");
        public string Vehicles => GetString("Vehicles");
        public string Technical => GetString("Technical");
        public string PartsCatalog => GetString("PartsCatalog");
        public string Suppliers => GetString("Suppliers");
        public string Help => GetString("Help");
        public string SectionOperation => GetString("SectionOperation");
        public string SectionRegisters => GetString("SectionRegisters");
        public string SectionManagement => GetString("SectionManagement");
        public string SectionSystem => GetString("SectionSystem");
        public string SectionHelp => GetString("SectionHelp");
        public string Session => GetString("Session");
        public string User => GetString("User");
        public string Profile => GetString("Profile");
        public string DateLabel => GetString("DateLabel");
        public string Theme => GetString("Theme");
        public string Comfort => GetString("Comfort");
        public string Operations => GetString("Operations");
        public string Language => GetString("Language");
        public string CommandPalette => GetString("CommandPalette");
        public string Email => GetString("Email");
        public string Password => GetString("Password");
        public string RememberMe => GetString("RememberMe");
        public string New => GetString("New");
        public string View => GetString("View");
        public string Back => GetString("Back");
        public string Confirm => GetString("Confirm");
        public string Total => GetString("Total");
        public string Subtotal => GetString("Subtotal");
        public string Discount => GetString("Discount");
        public string Payment => GetString("Payment");
        public string Status => GetString("Status");
        public string Actions => GetString("Actions");
        public string Details => GetString("Details");
        public string Description => GetString("Description");
        public string Quantity => GetString("Quantity");
        public string Price => GetString("Price");
        public string Date => GetString("Date");
        public string Name => GetString("Name");
        public string Phone => GetString("Phone");
        public string Address => GetString("Address");
        public string Plate => GetString("Plate");
        public string Mileage => GetString("Mileage");
        public string Technician => GetString("Technician");
        public string Part => GetString("Part");
        public string Service => GetString("Service");
        public string Stock => GetString("Stock");
        public string Inbound => GetString("Inbound");
        public string Outbound => GetString("Outbound");
        public string Balance => GetString("Balance");
        public string WorkOrder => GetString("WorkOrder");
        public string Quote => GetString("Quote");
        public string EmptyState => GetString("EmptyState");
        public string SelectLanguage => GetString("SelectLanguage");
    }
}
