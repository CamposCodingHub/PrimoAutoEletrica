using System;
using System.Windows.Controls;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Interface para gerenciamento centralizado de navegacao.
    /// Define o contrato que qualquer servico de navegacao deve implementar.
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// Navega para um modulo especifico. Retorna o UserControl correspondente.
        /// </summary>
        /// <param name="moduleName">Nome do modulo (ex: "Dashboard", "Clientes")</param>
        /// <returns>UserControl do modulo ou null se nao disponivel</returns>
        UserControl? Navigate(string moduleName);

        /// <summary>
        /// Navega para a pagina anterior no historico.
        /// </summary>
        /// <returns>UserControl da pagina anterior ou null se no inicio do historico</returns>
        UserControl? NavigateBack();

        /// <summary>
        /// Verifica se ha paginas anteriores disponiveis no historico.
        /// </summary>
        bool CanNavigateBack { get; }

        /// <summary>
        /// Obtem o nome do modulo atualmente ativo.
        /// </summary>
        string CurrentModule { get; }

        /// <summary>
        /// Evento disparado quando a navegacao e concluida com sucesso.
        /// </summary>
        event EventHandler<NavigationEventArgs>? NavigationCompleted;

        /// <summary>
        /// Evento disparado quando ocorre erro na navegacao.
        /// </summary>
        event EventHandler<NavigationErrorEventArgs>? NavigationError;

        /// <summary>
        /// Evento disparado quando o estado navegavel muda.
        /// </summary>
        event EventHandler<NavigationStateChangedEventArgs>? NavigationStateChanged;

        /// <summary>
        /// Limpa o cache de todas as paginas.
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Remove uma pagina especifica do cache.
        /// </summary>
        void RemoveFromCache(string moduleName);

        /// <summary>
        /// Recarrega o modulo atual ignorando a instancia em cache.
        /// </summary>
        /// <returns>UserControl recarregado ou null se nao houver modulo atual</returns>
        UserControl? RefreshCurrent();
    }

    /// <summary>
    /// Argumentos para evento de navegacao bem-sucedida.
    /// </summary>
    public class NavigationEventArgs : EventArgs
    {
        public string ModuleName { get; set; } = string.Empty;
        public UserControl Control { get; set; } = null!;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Argumentos para evento de erro de navegacao.
    /// </summary>
    public class NavigationErrorEventArgs : EventArgs
    {
        public string ModuleName { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Argumentos para alteracoes de estado navegavel.
    /// </summary>
    public class NavigationStateChangedEventArgs : EventArgs
    {
        public string CurrentModule { get; set; } = string.Empty;
        public bool CanNavigateBack { get; set; }
        public int HistoryDepth { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
