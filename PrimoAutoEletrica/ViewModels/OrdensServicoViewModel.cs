using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PRIMOX.Application.DTOs;
using PRIMOX.Application.UseCases.OrdensServico;

namespace PrimoAutoEletrica.ViewModels
{
    /// <summary>
    /// ViewModel do módulo de Ordens de Serviço do PRIMOX.
    /// Opera como Presentation Layer consumindo estritamente os Use Cases da camada Application (Clean Architecture),
    /// sem acesso direto a SQL, SQLite ou DbContext.
    /// </summary>
    public partial class OrdensServicoViewModel : BaseViewModel
    {
        private readonly AbrirOrdemServicoUseCase _abrirUseCase;
        private readonly ObterOrdemServicoUseCase _obterUseCase;
        private readonly AdicionarItemPecaUseCase _adicionarPecaUseCase;
        private readonly AdicionarItemServicoUseCase _adicionarServicoUseCase;
        private readonly AlterarStatusOrdemServicoUseCase _alterarStatusUseCase;

        [ObservableProperty]
        private string _title = "Ordens de Serviço - PRIMOX 3.0";

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _selectedStatus = "Todas";

        [ObservableProperty]
        private string _mensagemOperacao = string.Empty;

        [ObservableProperty]
        private OrdemServicoDto? _ordemSelecionada;

        public OrdensServicoViewModel() : this(null!, null!, null!, null!, null!)
        {
            // Construtor parameterless para suporte a XAML designer
        }

        public OrdensServicoViewModel(
            AbrirOrdemServicoUseCase abrirUseCase,
            ObterOrdemServicoUseCase obterUseCase,
            AdicionarItemPecaUseCase adicionarPecaUseCase,
            AdicionarItemServicoUseCase adicionarServicoUseCase,
            AlterarStatusOrdemServicoUseCase alterarStatusUseCase)
        {
            _abrirUseCase = abrirUseCase;
            _obterUseCase = obterUseCase;
            _adicionarPecaUseCase = adicionarPecaUseCase;
            _adicionarServicoUseCase = adicionarServicoUseCase;
            _alterarStatusUseCase = alterarStatusUseCase;
        }

        /// <summary>
        /// Caso de Uso: Abertura de OS via Application Layer.
        /// </summary>
        public async Task<AbrirOrdemServicoResult> AbrirNovaOrdemServicoAsync(AbrirOrdemServicoCommand command)
        {
            if (_abrirUseCase == null)
                return new AbrirOrdemServicoResult(false, null, null, null, "Caso de uso não inicializado.");

            IsLoading = true;
            try
            {
                var resultado = await _abrirUseCase.ExecutarAsync(command);
                if (resultado.Sucesso)
                {
                    OrdemSelecionada = resultado.Dados;
                    MensagemOperacao = $"OS #{resultado.Numero} aberta com sucesso via Application UseCase.";
                }
                else
                {
                    MensagemOperacao = $"Falha ao abrir OS: {resultado.MensagemErro}";
                }
                return resultado;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Caso de Uso: Consulta de OS por ID via Application Layer.
        /// </summary>
        public async Task<ObterOrdemServicoResult> ConsultarOrdemServicoAsync(Guid id)
        {
            if (_obterUseCase == null)
                return new ObterOrdemServicoResult(false, null, "Caso de uso não inicializado.");

            IsLoading = true;
            try
            {
                var resultado = await _obterUseCase.ExecutarAsync(new ObterOrdemServicoQuery(id));
                if (resultado.Sucesso)
                {
                    OrdemSelecionada = resultado.Dados;
                }
                return resultado;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Caso de Uso: Adicionar Peça via Application Layer.
        /// </summary>
        public async Task<AdicionarItemPecaResult> AdicionarPecaAsync(AdicionarItemPecaCommand command)
        {
            if (_adicionarPecaUseCase == null)
                return new AdicionarItemPecaResult(false, null, null, "Caso de uso não inicializado.");

            IsLoading = true;
            try
            {
                var resultado = await _adicionarPecaUseCase.ExecutarAsync(command);
                if (resultado.Sucesso)
                {
                    OrdemSelecionada = resultado.OrdemServicoAtualizada;
                    MensagemOperacao = "Peça adicionada e total recalculado pelo Domínio.";
                }
                else
                {
                    MensagemOperacao = $"Falha ao adicionar peça: {resultado.MensagemErro}";
                }
                return resultado;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Caso de Uso: Adicionar Serviço via Application Layer.
        /// </summary>
        public async Task<AdicionarItemServicoResult> AdicionarServicoAsync(AdicionarItemServicoCommand command)
        {
            if (_adicionarServicoUseCase == null)
                return new AdicionarItemServicoResult(false, null, null, "Caso de uso não inicializado.");

            IsLoading = true;
            try
            {
                var resultado = await _adicionarServicoUseCase.ExecutarAsync(command);
                if (resultado.Sucesso)
                {
                    OrdemSelecionada = resultado.OrdemServicoAtualizada;
                    MensagemOperacao = "Serviço adicionado e total recalculado pelo Domínio.";
                }
                else
                {
                    MensagemOperacao = $"Falha ao adicionar serviço: {resultado.MensagemErro}";
                }
                return resultado;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Caso de Uso: Alterar Status via Application Layer (validado pela Máquina de Estados de Domínio).
        /// </summary>
        public async Task<AlterarStatusOrdemServicoResult> AlterarStatusAsync(AlterarStatusOrdemServicoCommand command)
        {
            if (_alterarStatusUseCase == null)
                return new AlterarStatusOrdemServicoResult(false, null, null, null, "Caso de uso não inicializado.");

            IsLoading = true;
            try
            {
                var resultado = await _alterarStatusUseCase.ExecutarAsync(command);
                if (resultado.Sucesso)
                {
                    OrdemSelecionada = resultado.OrdemServicoAtualizada;
                    MensagemOperacao = $"Status alterado para '{resultado.StatusAtual}' com registro no histórico.";
                }
                else
                {
                    MensagemOperacao = $"Transição inválida: {resultado.MensagemErro}";
                }
                return resultado;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            if (Guid.TryParse(SearchText, out var id))
            {
                await ConsultarOrdemServicoAsync(id);
            }
        }

        [RelayCommand]
        private void EditSelected()
        {
            // Edição delegada para modal específica
        }

        [RelayCommand]
        private void PrintOrder()
        {
            // Impressão operacional
        }

        [RelayCommand]
        private void ExportToPDF()
        {
            // Exportação PDF operacional
        }
    }
}