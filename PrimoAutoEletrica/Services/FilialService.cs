using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Gerencia as filiais corporativas e a unidade ativa de trabalho.
    /// Suporta persistência real no banco de dados e isolamento multi-filial enterprise.
    /// </summary>
    public class FilialService
    {
        public const bool MultiFilialDisponivel = true;

        public static readonly Guid UnidadeLocalId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        private readonly DatabaseService _databaseService;
        private readonly LoggerService? _logger;
        private List<Filial> _filiaisCache = new();
        private Filial? _filialAtual;

        public FilialService(DatabaseService databaseService, LoggerService? logger = null)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger;
        }

        public Filial? FilialAtual
        {
            get => _filialAtual;
            set
            {
                if (_filialAtual != value)
                {
                    _filialAtual = value;
                    _logger?.LogInfo($"Unidade de trabalho ativa alterada para: '{value?.Codigo} - {value?.Nome}'");
                }
            }
        }

        public bool DeveExibirSelecaoFilial() => _filiaisCache.Count(f => f.Ativa) > 1;

        public async Task<List<Filial>> CarregarFiliaisAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var filiais = new List<Filial>();

                    using (var connection = _databaseService.GetConnection())
                    {
                        connection.Open();

                        using var cmd = connection.CreateCommand();
                        cmd.CommandText = "SELECT * FROM Filiais WHERE Ativa = 1 ORDER BY IsMatriz DESC, Codigo ASC;";

                        try
                        {
                            using var reader = cmd.ExecuteReader();
                            while (reader.Read())
                            {
                                filiais.Add(MapearFilial(reader));
                            }
                        }
                        catch
                        {
                            // Tabela pode ainda estar sendo criada no boot
                        }
                    }

                    if (filiais.Count == 0)
                    {
                        var matrizPadrao = CriarUnidadeLocal();
                        AdicionarFilialInterna(matrizPadrao);
                        filiais.Add(matrizPadrao);
                    }

                    _filiaisCache = filiais;
                    FilialAtual ??= ObterMatriz() ?? filiais.First();

                    _logger?.LogInfo($"Filiais carregadas com sucesso: {_filiaisCache.Count} unidade(s) ativa(s).");
                    return _filiaisCache;
                }
                catch (Exception ex)
                {
                    _logger?.LogError("Falha ao carregar filiais do banco", ex);
                    var fallback = CriarUnidadeLocal();
                    _filiaisCache = new List<Filial> { fallback };
                    FilialAtual ??= fallback;
                    return _filiaisCache;
                }
            });
        }

        public async Task<bool> AdicionarFilialAsync(Filial filial)
        {
            if (filial == null) throw new ArgumentNullException(nameof(filial));

            return await Task.Run(() =>
            {
                try
                {
                    AdicionarFilialInterna(filial);

                    var existente = _filiaisCache.FirstOrDefault(f => f.Id == filial.Id);
                    if (existente != null)
                        _filiaisCache.Remove(existente);

                    _filiaisCache.Add(filial);
                    _logger?.LogInfo($"Nova filial corporativa cadastrada: {filial.Codigo} - {filial.Nome}");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Falha ao adicionar filial {filial.Codigo}", ex);
                    return false;
                }
            });
        }

        private void AdicionarFilialInterna(Filial filial)
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO Filiais
                (
                    Id, Codigo, Nome, Endereco, Cidade, Estado, Cnpj, Telefone,
                    Email, Gerente, IsMatriz, Ativa, DataAbertura, CapacidadeEstoque,
                    Observacoes, DataCadastro, DataUltimaAtualizacao
                )
                VALUES
                (
                    @Id, @Codigo, @Nome, @Endereco, @Cidade, @Estado, @Cnpj, @Telefone,
                    @Email, @Gerente, @IsMatriz, @Ativa, @DataAbertura, @CapacidadeEstoque,
                    @Observacoes, @DataCadastro, @DataUltimaAtualizacao
                );";

            cmd.Parameters.AddWithValue("@Id", filial.Id.ToString());
            cmd.Parameters.AddWithValue("@Codigo", filial.Codigo ?? string.Empty);
            cmd.Parameters.AddWithValue("@Nome", filial.Nome ?? string.Empty);
            cmd.Parameters.AddWithValue("@Endereco", filial.Endereco ?? string.Empty);
            cmd.Parameters.AddWithValue("@Cidade", filial.Cidade ?? string.Empty);
            cmd.Parameters.AddWithValue("@Estado", filial.Estado ?? string.Empty);
            cmd.Parameters.AddWithValue("@Cnpj", filial.Cnpj ?? string.Empty);
            cmd.Parameters.AddWithValue("@Telefone", filial.Telefone ?? string.Empty);
            cmd.Parameters.AddWithValue("@Email", filial.Email ?? string.Empty);
            cmd.Parameters.AddWithValue("@Gerente", filial.Gerente ?? string.Empty);
            cmd.Parameters.AddWithValue("@IsMatriz", filial.IsMatriz ? 1 : 0);
            cmd.Parameters.AddWithValue("@Ativa", filial.Ativa ? 1 : 0);
            cmd.Parameters.AddWithValue("@DataAbertura", filial.DataAbertura.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@CapacidadeEstoque", filial.CapacidadeEstoque);
            cmd.Parameters.AddWithValue("@Observacoes", filial.Observacoes ?? string.Empty);
            cmd.Parameters.AddWithValue("@DataCadastro", filial.DataCadastro.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@DataUltimaAtualizacao", (object?)filial.DataUltimaAtualizacao?.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value);

            cmd.ExecuteNonQuery();
        }

        public async Task<bool> AtualizarFilialAsync(Filial filial)
        {
            if (filial == null) throw new ArgumentNullException(nameof(filial));

            return await Task.Run(() =>
            {
                try
                {
                    filial.DataUltimaAtualizacao = DateTime.Now;
                    AdicionarFilialInterna(filial);

                    var idx = _filiaisCache.FindIndex(f => f.Id == filial.Id);
                    if (idx >= 0)
                        _filiaisCache[idx] = filial;

                    if (FilialAtual?.Id == filial.Id)
                        FilialAtual = filial;

                    _logger?.LogInfo($"Filial {filial.Codigo} atualizada com sucesso.");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Falha ao atualizar filial {filial.Codigo}", ex);
                    return false;
                }
            });
        }

        public async Task<bool> RemoverFilialAsync(Guid id)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var connection = _databaseService.GetConnection();
                    connection.Open();

                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = "UPDATE Filiais SET Ativa = 0, DataUltimaAtualizacao = @Agora WHERE Id = @Id;";
                    cmd.Parameters.AddWithValue("@Agora", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@Id", id.ToString());
                    cmd.ExecuteNonQuery();

                    var item = _filiaisCache.FirstOrDefault(f => f.Id == id);
                    if (item != null)
                        _filiaisCache.Remove(item);

                    if (FilialAtual?.Id == id)
                        FilialAtual = ObterMatriz();

                    _logger?.LogInfo($"Filial ID {id} desativada com sucesso.");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Falha ao remover filial {id}", ex);
                    return false;
                }
            });
        }

        public void DefinirFilialAtual(Guid filialId)
        {
            var filial = ObterFilialPorId(filialId) ?? CriarUnidadeLocal();
            if (!_filiaisCache.Any(f => f.Id == filial.Id))
                _filiaisCache.Add(filial);
            FilialAtual = filial;
        }

        public List<Filial> ObterFiliaisAtivas() => _filiaisCache.Where(f => f.Ativa).ToList();

        public Filial? ObterFilialPorId(Guid id) => _filiaisCache.FirstOrDefault(f => f.Id == id);

        public Filial? ObterFilialPorCodigo(string codigo) =>
            _filiaisCache.FirstOrDefault(f => f.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase));

        public Filial? ObterMatriz() => _filiaisCache.FirstOrDefault(f => f.IsMatriz && f.Ativa)
                                       ?? _filiaisCache.FirstOrDefault(f => f.Ativa);

        public bool TemFiliaisDisponiveis() => _filiaisCache.Any(f => f.Ativa);

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

        private static Filial CriarUnidadeLocal() => new()
        {
            Id = UnidadeLocalId,
            Codigo = "LOCAL",
            Nome = "Unidade Matriz",
            Endereco = "Sede Principal",
            Cidade = "São Paulo",
            Estado = "SP",
            Cnpj = "00.000.000/0001-00",
            Telefone = string.Empty,
            Email = string.Empty,
            Gerente = "Administrador Geral",
            IsMatriz = true,
            Ativa = true,
            DataAbertura = DateTime.Today,
            CapacidadeEstoque = 50000,
            Observacoes = "Unidade corporativa central configurada automaticamente pelo PRIMOX."
        };

        private static Filial MapearFilial(DbDataReader reader)
        {
            return new Filial
            {
                Id = Guid.TryParse(reader["Id"]?.ToString(), out var id) ? id : Guid.NewGuid(),
                Codigo = reader["Codigo"]?.ToString() ?? string.Empty,
                Nome = reader["Nome"]?.ToString() ?? string.Empty,
                Endereco = reader["Endereco"]?.ToString() ?? string.Empty,
                Cidade = reader["Cidade"]?.ToString() ?? string.Empty,
                Estado = reader["Estado"]?.ToString() ?? string.Empty,
                Cnpj = reader["Cnpj"]?.ToString() ?? string.Empty,
                Telefone = reader["Telefone"]?.ToString() ?? string.Empty,
                Email = reader["Email"]?.ToString() ?? string.Empty,
                Gerente = reader["Gerente"]?.ToString() ?? string.Empty,
                IsMatriz = Convert.ToInt32(reader["IsMatriz"] ?? 0) == 1,
                Ativa = Convert.ToInt32(reader["Ativa"] ?? 1) == 1,
                DataAbertura = DateTime.TryParse(reader["DataAbertura"]?.ToString(), out var da) ? da : DateTime.Today,
                CapacidadeEstoque = Convert.ToInt32(reader["CapacidadeEstoque"] ?? 0),
                Observacoes = reader["Observacoes"]?.ToString() ?? string.Empty,
                DataCadastro = DateTime.TryParse(reader["DataCadastro"]?.ToString(), out var dc) ? dc : DateTime.Now,
                DataUltimaAtualizacao = DateTime.TryParse(reader["DataUltimaAtualizacao"]?.ToString(), out var dua) ? dua : null
            };
        }
    }
}
