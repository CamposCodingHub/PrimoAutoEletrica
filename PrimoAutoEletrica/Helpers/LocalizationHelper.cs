using System;
using System.ComponentModel;
using System.Windows;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Helpers
{
    /// <summary>
    /// Helper para localização que pode ser usado em XAML como StaticResource
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
            _localizationService.CultureChanged += (s, e) => OnPropertyChanged();
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

        // Propriedades convenientes para strings comuns
        public string Dashboard => GetString("Dashboard");
        public string Clients => GetString("Clients");
        public string Inventory => GetString("Inventory");
        public string Employees => GetString("Employees");
        public string Finance => GetString("Finance");
        public string Reports => GetString("Reports");
        public string Settings => GetString("Settings");
        public string SystemName => GetString("SystemName");
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

        public void SetLanguage(string languageCode)
        {
            _localizationService.SetLanguage(languageCode);
            OnPropertyChanged(nameof(Dashboard));
            OnPropertyChanged(nameof(Clients));
            OnPropertyChanged(nameof(Inventory));
            OnPropertyChanged(nameof(Employees));
            OnPropertyChanged(nameof(Finance));
            OnPropertyChanged(nameof(Reports));
            OnPropertyChanged(nameof(Settings));
            OnPropertyChanged(nameof(SystemName));
            OnPropertyChanged(nameof(Save));
            OnPropertyChanged(nameof(Cancel));
            OnPropertyChanged(nameof(Delete));
            OnPropertyChanged(nameof(Edit));
            OnPropertyChanged(nameof(Add));
            OnPropertyChanged(nameof(Search));
            OnPropertyChanged(nameof(SearchPlaceholder));
            OnPropertyChanged(nameof(Filter));
            OnPropertyChanged(nameof(Export));
            OnPropertyChanged(nameof(Import));
            OnPropertyChanged(nameof(Print));
            OnPropertyChanged(nameof(Close));
            OnPropertyChanged(nameof(Yes));
            OnPropertyChanged(nameof(No));
            OnPropertyChanged(nameof(Ok));
            OnPropertyChanged(nameof(Error));
            OnPropertyChanged(nameof(Warning));
            OnPropertyChanged(nameof(Information));
            OnPropertyChanged(nameof(Success));
            OnPropertyChanged(nameof(Loading));
            OnPropertyChanged(nameof(PleaseWait));
            OnPropertyChanged(nameof(NoDataFound));
            OnPropertyChanged(nameof(AreYouSure));
            OnPropertyChanged(nameof(OperationCompleted));
            OnPropertyChanged(nameof(OperationFailed));
            OnPropertyChanged(nameof(RequiredField));
            OnPropertyChanged(nameof(InvalidValue));
            OnPropertyChanged(nameof(Refresh));
            OnPropertyChanged(nameof(Close));
        }
    }
}