using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Serviço para gerenciamento de múltiplas filiais
    /// </summary>
    public class FilialService
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggerService _logger;
        private List<Filial> _filiaisCache = new();
        private Filial? _filialAtual;

        public FilialService(DatabaseService databaseService, LoggerService logger)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtém a filial atualmente selecionada
        /// </summary>
        public Filial? FilialAtual
        {
            get => _filialAtual;
            set
            {
                if (_filialAtual != value)
                {
                    _filialAtual = value;
                    _logger.LogInfo($"Filial atual alterada para: {value?.Nome ?? "Nenhuma"}");
                }
            }
        }

        /// <summary>
        /// Carrega todas as filiais do banco de dados
        /// </summary>
        public async Task<List<Filial>> CarregarFiliaisAsync()
        {
            try
            {
                // Simulação - em produção, buscar do banco de dados
                _filiaisCache = new List<Filial>
                {
                    new Filial
                    {
                        Id = Guid.NewGuid(),
                        Codigo = "MATRIZ",
                        Nome = "Matriz São Paulo",
                        Endereco = "Av. Paulista, 1000",
                        Cidade = "São Paulo",
                        Estado = "SP",
                        Cnpj = "12.345.678/0001-90",
                        Telefone = "(11) 3456-7890",
                        Email = "matriz@primoautoeletrica.com.br",
                        Gerente = "João Silva",
                        IsMatriz = true,
                        Ativa = true,
                        DataAbertura = new DateTime(2010, 1, 15),
                        CapacidadeEstoque = 1000
                    },
                    new Filial
                    {
                        Id = Guid.NewGuid(),
                        Codigo = "001",
                        Nome = "Filial Rio de Janeiro",
                        Endereco = "Rua das Flores, 200",
                        Cidade = "Rio de Janeiro",
                        Estado = "RJ",
                        Cnpj = "12.345.678/0002-91",
                        Telefone = "(21) 2345-6789",
                        Email = "rj@primoautoeletrica.com.br",
                        Gerente = "Maria Santos",
                        IsMatriz = false,
                        Ativa = true,
                        DataAbertura = new DateTime(2015, 3, 20),
                        CapacidadeEstoque = 500
                    }
                };

                _logger.LogInfo($"Carregadas {_filiaisCache.Count} filiais");
                return _filiaisCache;
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao carregar filiais", ex);
                return new List<Filial>();
            }
        }

        /// <summary>
        /// Obtém todas as filiais ativas
        /// </summary>
        public List<Filial> ObterFiliaisAtivas()
        {
            return _filiaisCache.Where(f => f.Ativa).ToList();
        }

        /// <summary>
        /// Obtém uma filial específica por ID
        /// </summary>
        public Filial? ObterFilialPorId(Guid id)
        {
            return _filiaisCache.FirstOrDefault(f => f.Id == id);
        }

        /// <summary>
        /// Obtém uma filial específica por código
        /// </summary>
        public Filial? ObterFilialPorCodigo(string codigo)
        {
            return _filiaisCache.FirstOrDefault(f => f.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Adiciona uma nova filial
        /// </summary>
        public async Task<bool> AdicionarFilialAsync(Filial filial)
        {
            try
            {
                if (filial == null)
                    throw new ArgumentNullException(nameof(filial));

                // Validações básicas
                if (string.IsNullOrWhiteSpace(filial.Codigo))
                    throw new ArgumentException("Código da filial é obrigatório");

                if (string.IsNullOrWhiteSpace(filial.Nome))
                    throw new ArgumentException("Nome da filial é obrigatório");

                if (_filiaisCache.Any(f => f.Codigo.Equals(filial.Codigo, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException("Já existe uma filial com este código");

                filial.Id = Guid.NewGuid();
                filial.DataCadastro = DateTime.Now;
                filial.DataUltimaAtualizacao = DateTime.Now;

                _filiaisCache.Add(filial);
                _logger.LogInfo($"Filial adicionada: {filial.Nome} ({filial.Codigo})");

                // Em produção, salvar no banco de dados
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao adicionar filial: {filial?.Nome}", ex);
                return false;
            }
        }

        /// <summary>
        /// Atualiza uma filial existente
        /// </summary>
        public async Task<bool> AtualizarFilialAsync(Filial filial)
        {
            try
            {
                if (filial == null)
                    throw new ArgumentNullException(nameof(filial));

                var existente = _filiaisCache.FirstOrDefault(f => f.Id == filial.Id);
                if (existente == null)
                    throw new InvalidOperationException("Filial não encontrada");

                // Atualiza os dados
                existente.Nome = filial.Nome;
                existente.Endereco = filial.Endereco;
                existente.Cidade = filial.Cidade;
                existente.Estado = filial.Estado;
                existente.Cnpj = filial.Cnpj;
                existente.Telefone = filial.Telefone;
                existente.Email = filial.Email;
                existente.Gerente = filial.Gerente;
                existente.Ativa = filial.Ativa;
                existente.CapacidadeEstoque = filial.CapacidadeEstoque;
                existente.Latitude = filial.Latitude;
                existente.Longitude = filial.Longitude;
                existente.Observacoes = filial.Observacoes;
                existente.DataUltimaAtualizacao = DateTime.Now;

                _logger.LogInfo($"Filial atualizada: {filial.Nome} ({filial.Codigo})");

                // Em produção, atualizar no banco de dados
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao atualizar filial: {filial?.Nome}", ex);
                return false;
            }
        }

        /// <summary>
        /// Remove uma filial (soft delete)
        /// </summary>
        public async Task<bool> RemoverFilialAsync(Guid id)
        {
            try
            {
                var filial = _filiaisCache.FirstOrDefault(f => f.Id == id);
                if (filial == null)
                    throw new InvalidOperationException("Filial não encontrada");

                if (filial.IsMatriz)
                    throw new InvalidOperationException("Não é possível remover a filial matriz");

                filial.Ativa = false;
                filial.DataUltimaAtualizacao = DateTime.Now;

                _logger.LogInfo($"Filial removida: {filial.Nome} ({filial.Codigo})");

                // Em produção, atualizar no banco de dados
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao remover filial com ID: {id}", ex);
                return false;
            }
        }

        /// <summary>
        /// Define a filial atual para o usuário
        /// </summary>
        public void DefinirFilialAtual(Guid filialId)
        {
            var filial = ObterFilialPorId(filialId);
            if (filial == null)
                throw new InvalidOperationException("Filial não encontrada");

            if (!filial.Ativa)
                throw new InvalidOperationException("Filial inativa não pode ser selecionada");

            FilialAtual = filial;
        }

        /// <summary>
        /// Obtém estatísticas de uma filial
        /// </summary>
        public Dictionary<string, object> ObterEstatisticasFilial(Guid filialId)
        {
            var filial = ObterFilialPorId(filialId);
            if (filial == null)
                throw new InvalidOperationException("Filial não encontrada");

            return new Dictionary<string, object>
            {
                ["Nome"] = filial.Nome,
                ["Codigo"] = filial.Codigo,
                ["Cidade"] = filial.Cidade,
                ["Estado"] = filial.Estado,
                ["CapacidadeEstoque"] = filial.CapacidadeEstoque,
                ["Ativa"] = filial.Ativa,
                ["IsMatriz"] = filial.IsMatriz,
                ["DataAbertura"] = filial.DataAbertura,
                ["Gerente"] = filial.Gerente
            };
        }

        /// <summary>
        /// Verifica se há filiais disponíveis
        /// </summary>
        public bool TemFiliaisDisponiveis()
        {
            return _filiaisCache.Any(f => f.Ativa);
        }

        /// <summary>
        /// Obtém a filial matriz
        /// </summary>
        public Filial? ObterMatriz()
        {
            return _filiaisCache.FirstOrDefault(f => f.IsMatriz && f.Ativa);
        }
    }
}