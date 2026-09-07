using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Controls;
using PrimoAutoEletrica.UserControls;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Servico centralizado de navegacao que gerencia criacao e cache de controles,
    /// validacao de permissoes e historico de navegacao.
    /// </summary>
    public class NavigationService : INavigationService
    {
        private readonly PermissionService _permissionService;
        private readonly LoggerService? _logger;
        private readonly Dictionary<string, UserControl> _pageCache;
        private readonly List<string> _cacheOrder;
        private readonly Stack<string> _navigationHistory;
        private readonly int _maxCacheSize;
        private string _currentModule = string.Empty;

        private readonly Dictionary<string, Type> _moduleMapping = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Dashboard", typeof(DashboardControl) },
            { "Clientes", typeof(ClientesControl) },
            { "Veiculos", typeof(VeiculosControl) },
            { "AutoEletricaTecnica", typeof(AutoEletricaTecnicaControl) },
            { "Orcamentos", typeof(OrcamentosControl) },
            { "OrdensServico", typeof(OrdensServicoControl) },
            { "OficinaKanban", typeof(OficinaKanbanControl) },
            { "PDV", typeof(PDVControl) },
            { "Estoque", typeof(EstoqueControl) },
            { "CatalogoPecas", typeof(CatalogoPecasControl) },
            { "ImportarNFe", typeof(ImportarNFeControl) },
            { "Financeiro", typeof(FinanceiroControl) },
            { "Fornecedores", typeof(FornecedoresControl) },
            { "Funcionarios", typeof(FuncionariosControl) },
            { "Agendamentos", typeof(AgendamentosControl) },
            { "Relatorios", typeof(RelatoriosControl) }
        };

        public event EventHandler<NavigationEventArgs>? NavigationCompleted;
        public event EventHandler<NavigationErrorEventArgs>? NavigationError;
        public event EventHandler<NavigationStateChangedEventArgs>? NavigationStateChanged;

        public string CurrentModule => _currentModule;

        public bool CanNavigateBack => _navigationHistory.Count > 1;

        public NavigationService(PermissionService permissionService, LoggerService? logger = null, int maxCacheSize = 20)
        {
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _logger = logger;
            _maxCacheSize = Math.Max(1, maxCacheSize);
            _pageCache = new Dictionary<string, UserControl>(StringComparer.OrdinalIgnoreCase);
            _cacheOrder = new List<string>();
            _navigationHistory = new Stack<string>();
        }

        /// <summary>
        /// Navega para um modulo especifico, validando permissoes e usando cache.
        /// </summary>
        public UserControl? Navigate(string moduleName)
        {
            return NavigateInternal(moduleName, true);
        }

        /// <summary>
        /// Navega para a pagina anterior no historico.
        /// </summary>
        public UserControl? NavigateBack()
        {
            if (_navigationHistory.Count <= 1)
            {
                return null;
            }

            _navigationHistory.Pop();
            var previousModule = _navigationHistory.Peek();

            return NavigateInternal(previousModule, false);
        }

        /// <summary>
        /// Limpa o cache de todas as paginas.
        /// </summary>
        public void ClearCache()
        {
            _pageCache.Clear();
            _cacheOrder.Clear();
            _logger?.LogInfo("Cache de navegacao limpo.");
            RaiseNavigationStateChanged();
        }

        /// <summary>
        /// Remove uma pagina especifica do cache.
        /// </summary>
        public void RemoveFromCache(string moduleName)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                return;
            }

            if (_pageCache.Remove(moduleName))
            {
                _cacheOrder.Remove(moduleName);
                _logger?.LogInfo($"Modulo removido manualmente do cache: {moduleName}");
                RaiseNavigationStateChanged();
            }
        }

        /// <summary>
        /// Recarrega o modulo atual removendo sua instancia do cache antes de recriar o controle.
        /// </summary>
        public UserControl? RefreshCurrent()
        {
            if (string.IsNullOrWhiteSpace(_currentModule))
            {
                return null;
            }

            RemoveFromCache(_currentModule);
            return NavigateInternal(_currentModule, false);
        }

        /// <summary>
        /// Registra um novo tipo de controle no mapeamento de modulos.
        /// Permite extensibilidade para adicionar novos modulos dinamicamente.
        /// </summary>
        public void RegisterModule(string moduleName, Type controlType)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                throw new ArgumentException("Nome do modulo invalido", nameof(moduleName));
            }

            if (controlType == null || !typeof(UserControl).IsAssignableFrom(controlType))
            {
                throw new ArgumentException("Tipo deve ser derivado de UserControl", nameof(controlType));
            }

            _moduleMapping[moduleName] = controlType;
        }

        private UserControl? NavigateInternal(string moduleName, bool trackHistory)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
            {
                RaiseNavigationError(moduleName, new ArgumentException("Nome do modulo invalido"));
                return null;
            }

            try
            {
                if (!_permissionService.TemPermissao(moduleName))
                {
                    var unauthorizedException = new UnauthorizedAccessException($"Usuario nao tem permissao para acessar: {moduleName}");
                    RaiseNavigationError(moduleName, unauthorizedException);
                    return null;
                }

                var control = GetOrCreateControl(moduleName);
                if (control == null)
                {
                    return null;
                }

                _currentModule = moduleName;
                if (trackHistory)
                {
                    RegisterNavigation(moduleName);
                }

                RaiseNavigationStateChanged();
                RaiseNavigationCompleted(moduleName, control);
                return control;
            }
            catch (Exception ex)
            {
                var exception = UnwrapInvocationException(ex);
                _logger?.LogError($"Erro ao navegar para {moduleName}", exception);
                RaiseNavigationError(moduleName, exception);
                return null;
            }
        }

        private UserControl? GetOrCreateControl(string moduleName)
        {
            if (_pageCache.TryGetValue(moduleName, out var cachedControl))
            {
                _logger?.LogInfo($"Obtido do cache: {moduleName}");
                TouchCache(moduleName);
                return cachedControl;
            }

            if (!_moduleMapping.TryGetValue(moduleName, out var controlType))
            {
                var mappingException = new InvalidOperationException($"Modulo nao mapeado: {moduleName}");
                RaiseNavigationError(moduleName, mappingException);
                return null;
            }

            var control = Activator.CreateInstance(controlType) as UserControl;
            if (control == null)
            {
                var creationException = new InvalidOperationException($"Falha ao criar instancia de: {moduleName}");
                RaiseNavigationError(moduleName, creationException);
                return null;
            }

            if (_pageCache.Count >= _maxCacheSize && _cacheOrder.Count > 0)
            {
                var oldest = _cacheOrder[0];
                _cacheOrder.RemoveAt(0);

                if (_pageCache.Remove(oldest))
                {
                    _logger?.LogInfo($"Removido do cache (LRU): {oldest}");
                }
            }

            _pageCache[moduleName] = control;
            TouchCache(moduleName);
            _logger?.LogInfo($"Adicionado ao cache: {moduleName}");

            return control;
        }

        private static Exception UnwrapInvocationException(Exception exception)
        {
            return exception is TargetInvocationException { InnerException: not null } invocationException
                ? invocationException.InnerException!
                : exception;
        }

        private void TouchCache(string moduleName)
        {
            _cacheOrder.Remove(moduleName);
            _cacheOrder.Add(moduleName);
        }

        private void RegisterNavigation(string moduleName)
        {
            if (_navigationHistory.Count == 0 || !string.Equals(_navigationHistory.Peek(), moduleName, StringComparison.OrdinalIgnoreCase))
            {
                _navigationHistory.Push(moduleName);
            }

            _logger?.LogInfo($"Navegacao registrada: {moduleName}");
        }

        private void RaiseNavigationCompleted(string moduleName, UserControl control)
        {
            NavigationCompleted?.Invoke(this, new NavigationEventArgs
            {
                ModuleName = moduleName,
                Control = control
            });
        }

        private void RaiseNavigationError(string moduleName, Exception ex)
        {
            NavigationError?.Invoke(this, new NavigationErrorEventArgs
            {
                ModuleName = moduleName,
                Exception = ex
            });
        }

        private void RaiseNavigationStateChanged()
        {
            NavigationStateChanged?.Invoke(this, new NavigationStateChangedEventArgs
            {
                CurrentModule = _currentModule,
                CanNavigateBack = CanNavigateBack,
                HistoryDepth = _navigationHistory.Count
            });
        }
    }
}
