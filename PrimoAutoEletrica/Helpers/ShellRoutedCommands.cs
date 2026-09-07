using System.Windows.Input;

namespace PrimoAutoEletrica.Helpers
{
    /// <summary>
    /// Comandos roteados do shell para atalhos globais consistentes (InputBinding / CommandBinding).
    /// </summary>
    public static class ShellRoutedCommands
    {
        public static readonly RoutedCommand OpenCommandPalette = new(
            nameof(OpenCommandPalette),
            typeof(ShellRoutedCommands));

        public static readonly RoutedCommand RefreshModule = new(
            nameof(RefreshModule),
            typeof(ShellRoutedCommands));

        public static readonly RoutedCommand ShowShortcutsHelp = new(
            nameof(ShowShortcutsHelp),
            typeof(ShellRoutedCommands));

        public static readonly RoutedCommand NavigateHelp = new(
            nameof(NavigateHelp),
            typeof(ShellRoutedCommands));

        public static readonly RoutedCommand NavigateOrcamentos = new(
            nameof(NavigateOrcamentos),
            typeof(ShellRoutedCommands));

        public static readonly RoutedCommand NavigateOrdensServico = new(
            nameof(NavigateOrdensServico),
            typeof(ShellRoutedCommands));

        public static readonly RoutedCommand NavigateClientes = new(
            nameof(NavigateClientes),
            typeof(ShellRoutedCommands));

        public static readonly RoutedCommand NavigateEstoque = new(
            nameof(NavigateEstoque),
            typeof(ShellRoutedCommands));

        public static readonly RoutedCommand NavigateSettings = new(
            nameof(NavigateSettings),
            typeof(ShellRoutedCommands));

        public static readonly RoutedCommand FocusGlobalSearch = new(
            nameof(FocusGlobalSearch),
            typeof(ShellRoutedCommands));
    }
}
