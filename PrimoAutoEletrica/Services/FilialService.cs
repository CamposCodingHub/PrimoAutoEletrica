using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Contexto de unidade de trabalho local.
    /// Multi-filial real NÃO está disponível no PRIMOX Workshop 1.0.0.
    /// Este serviço NÃO simula filiais SP/RJ nem isolamento de estoque/caixa.
    /// </summary>
    public class FilialService
    {
        /// <summary>Flag honesta: gestão de múltiplas filiais ainda não está disponível.</summary>
        public const bool MultiFilialDisponivel = false;

        private static readonly Guid UnidadeLocalId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        private readonly DatabaseService _databaseService;
        private readonly LoggerService _logger;
        private List<Filial> _filiaisCache = new();
        private Filial? _filialAtual;

        public FilialService(DatabaseService databaseService, LoggerService logger)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Filial? FilialAtual
        {
            get => _filialAtual;
            set
            {
                if (_filialAtual != value)
                {
                    _filialAtual = value;
                    _logger.LogInfo($"Unidade de trabalho atual: {value?.Nome ?? "Nenhuma"}");
                }
            }
        }

        /// <summary>
        /// True somente se multi-filial estiver habilitado e houver 2+ unidades.
        /// No 1.0.0 sempre false — evita tela falsa de escolha SP/RJ.
        /// </summary>
        public bool DeveExibirSelecaoFilial() => MultiFilialDisponivel && _filiaisCache.Count(f => f.Ativa) > 1;

        public async Task<List<Filial>> CarregarFiliaisAsync()
        {
            try
            {
                await Task.CompletedTask;

                // Unidade local única e estável — não inventar segunda filial.
                var unidade = CriarUnidadeLocal();
                _filiaisCache = new List<Filial> { unidade };
                FilialAtual ??= unidade;

                _logger.LogInfo(
                    MultiFilialDisponivel
                        ? $"Carregadas {_filiaisCache.Count} filiais (multi-filial)."
                        : "Multi-filial indisponivel: usando apenas Unidade local.");

                return _filiaisCache;
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao carregar unidade de trabalho", ex);
                return new List<Filial>();
            }
        }

        private static Filial CriarUnidadeLocal() => new()
        {
            Id = UnidadeLocalId,
            Codigo = "LOCAL",
            Nome = "Unidade local",
            Endereco = string.Empty,
            Cidade = string.Empty,
            Estado = string.Empty,
            Cnpj = string.Empty,
            Telefone = string.Empty,
            Email = string.Empty,
            Gerente = string.Empty,
            IsMatriz = true,
            Ativa = true,
            DataAbertura = DateTime.Today,
            CapacidadeEstoque = 0,
            Observacoes = "Gestao de multiplas filiais ainda nao esta disponivel neste PRIMOX 1.0.0."
        };

        public List<Filial> ObterFiliaisAtivas() => _filiaisCache.Where(f => f.Ativa).ToList();

        public Filial? ObterFilialPorId(Guid id) => _filiaisCache.FirstOrDefault(f => f.Id == id);

        public Filial? ObterFilialPorCodigo(string codigo) =>
            _filiaisCache.FirstOrDefault(f => f.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase));

        public Task<bool> AdicionarFilialAsync(Filial filial)
        {
            _logger.LogInfo("AdicionarFilial bloqueado: multi-filial nao disponivel.");
            return Task.FromResult(false);
        }

        public Task<bool> AtualizarFilialAsync(Filial filial)
        {
            _logger.LogInfo("AtualizarFilial bloqueado: multi-filial nao disponivel.");
            return Task.FromResult(false);
        }

        public Task<bool> RemoverFilialAsync(Guid id)
        {
            _logger.LogInfo("RemoverFilial bloqueado: multi-filial nao disponivel.");
            return Task.FromResult(false);
        }

        public void DefinirFilialAtual(Guid filialId)
        {
            var filial = ObterFilialPorId(filialId) ?? CriarUnidadeLocal();
            if (!_filiaisCache.Any(f => f.Id == filial.Id))
                _filiaisCache.Add(filial);
            FilialAtual = filial;
        }

        public Dictionary<string, object> ObterEstatisticasFilial(Guid filialId)
        {
            var filial = ObterFilialPorId(filialId) ?? CriarUnidadeLocal();
            return new Dictionary<string, object>
            {
                ["Nome"] = filial.Nome,
                ["Codigo"] = filial.Codigo,
                ["MultiFilialDisponivel"] = MultiFilialDisponivel,
                ["Ativa"] = filial.Ativa,
                ["IsMatriz"] = filial.IsMatriz,
                ["Observacoes"] = filial.Observacoes ?? string.Empty
            };
        }

        public bool TemFiliaisDisponiveis() => _filiaisCache.Any(f => f.Ativa);

        public Filial? ObterMatriz() => _filiaisCache.FirstOrDefault(f => f.IsMatriz && f.Ativa)
                                       ?? _filiaisCache.FirstOrDefault(f => f.Ativa);
    }
}
