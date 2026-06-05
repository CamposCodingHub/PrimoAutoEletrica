using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Repositories
{
    public sealed class FuncionarioRepository : IFuncionarioRepository
    {
        private readonly Func<SqliteConnection> _connectionFactory;
        private readonly LoggerService _logger;

        public FuncionarioRepository(Func<SqliteConnection> connectionFactory, LoggerService logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<Funcionario> ObterTodos(bool somenteAtivos = true)
        {
            var funcionarios = new List<Funcionario>();

            try
            {
                using var connection = _connectionFactory();
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT
                        Id,
                        Nome,
                        CPF,
                        Email,
                        Funcao,
                        PerfilAcesso,
                        Telefone,
                        Foto,
                        DataAdmissao,
                        Salario,
                        Status,
                        DataCadastro,
                        DataUltimoLogin,
                        Ativo
                    FROM Funcionarios
                    WHERE (@SomenteAtivos = 0 OR Ativo = 1)
                    ORDER BY Nome;";
                command.Parameters.AddWithValue("@SomenteAtivos", somenteAtivos ? 1 : 0);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    funcionarios.Add(MaterializarFuncionario(reader));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao obter funcionarios no repositorio.", ex);
            }

            return funcionarios;
        }

        public Funcionario? ObterPorId(int id)
        {
            using var connection = _connectionFactory();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    Id,
                    Nome,
                    CPF,
                    Email,
                    Funcao,
                    PerfilAcesso,
                    Telefone,
                    Foto,
                    DataAdmissao,
                    Salario,
                    Status,
                    DataCadastro,
                    DataUltimoLogin,
                    Ativo
                FROM Funcionarios
                WHERE Id = @Id
                LIMIT 1;";
            command.Parameters.AddWithValue("@Id", id);

            using var reader = command.ExecuteReader();
            return reader.Read() ? MaterializarFuncionario(reader) : null;
        }

        public bool EmailExiste(string email, int? ignorarFuncionarioId = null)
        {
            var emailNormalizado = CadastroValidationHelper.NormalizarEmail(email);
            if (string.IsNullOrWhiteSpace(emailNormalizado))
            {
                return false;
            }

            using var connection = _connectionFactory();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*)
                FROM Funcionarios
                WHERE lower(Email) = lower(@Email)
                  AND Ativo = 1
                  AND (@IgnorarFuncionarioId IS NULL OR Id <> @IgnorarFuncionarioId);";
            command.Parameters.AddWithValue("@Email", emailNormalizado);
            command.Parameters.AddWithValue(
                "@IgnorarFuncionarioId",
                ignorarFuncionarioId.HasValue ? ignorarFuncionarioId.Value : DBNull.Value);

            return Convert.ToInt32(command.ExecuteScalar() ?? 0) > 0;
        }

        public int Salvar(Funcionario funcionario)
        {
            if (funcionario == null)
            {
                throw new ArgumentNullException(nameof(funcionario));
            }

            PrepararEValidarFuncionario(funcionario, ignorarFuncionarioId: null);

            if (EmailExiste(funcionario.Email))
            {
                throw new InvalidOperationException("Este email ja esta cadastrado para outro funcionario ativo.");
            }

            using var connection = _connectionFactory();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Funcionarios
                (
                    Nome,
                    CPF,
                    Email,
                    Senha,
                    Funcao,
                    PerfilAcesso,
                    Telefone,
                    Foto,
                    DataAdmissao,
                    Salario,
                    Status,
                    DataCadastro,
                    Ativo
                )
                VALUES
                (
                    @Nome,
                    @CPF,
                    @Email,
                    @Senha,
                    @Funcao,
                    @PerfilAcesso,
                    @Telefone,
                    @Foto,
                    @DataAdmissao,
                    @Salario,
                    @Status,
                    @DataCadastro,
                    @Ativo
                );
                SELECT last_insert_rowid();";

            PreencherParametrosFuncionario(command, funcionario, incluirSenha: true);
            var novoId = Convert.ToInt32(command.ExecuteScalar());

            RegistrarAuditoria("FuncionarioCriado", novoId, CriarSnapshot(funcionario));
            return novoId;
        }

        public void Atualizar(Funcionario funcionario)
        {
            if (funcionario == null)
            {
                throw new ArgumentNullException(nameof(funcionario));
            }

            if (funcionario.Id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(funcionario.Id), "Funcionario invalido.");
            }

            PrepararEValidarFuncionario(funcionario, funcionario.Id);

            if (EmailExiste(funcionario.Email, funcionario.Id))
            {
                throw new InvalidOperationException("Este email ja esta cadastrado para outro funcionario ativo.");
            }

            var atualizarSenha = !string.IsNullOrWhiteSpace(funcionario.Senha);

            using var connection = _connectionFactory();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = atualizarSenha
                ? @"
                    UPDATE Funcionarios
                    SET Nome = @Nome,
                        CPF = @CPF,
                        Email = @Email,
                        Senha = @Senha,
                        Funcao = @Funcao,
                        PerfilAcesso = @PerfilAcesso,
                        Telefone = @Telefone,
                        Foto = @Foto,
                        DataAdmissao = @DataAdmissao,
                        Salario = @Salario,
                        Status = @Status,
                        Ativo = @Ativo,
                        RowVersion = COALESCE(RowVersion, 0) + 1,
                        DataUltimaAlteracao = @DataUltimaAlteracao
                    WHERE Id = @Id;"
                : @"
                    UPDATE Funcionarios
                    SET Nome = @Nome,
                        CPF = @CPF,
                        Email = @Email,
                        Funcao = @Funcao,
                        PerfilAcesso = @PerfilAcesso,
                        Telefone = @Telefone,
                        Foto = @Foto,
                        DataAdmissao = @DataAdmissao,
                        Salario = @Salario,
                        Status = @Status,
                        Ativo = @Ativo,
                        RowVersion = COALESCE(RowVersion, 0) + 1,
                        DataUltimaAlteracao = @DataUltimaAlteracao
                    WHERE Id = @Id;";

            PreencherParametrosFuncionario(command, funcionario, atualizarSenha);
            command.Parameters.AddWithValue("@Id", funcionario.Id);
            command.Parameters.AddWithValue("@DataUltimaAlteracao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.ExecuteNonQuery();

            RegistrarAuditoria("FuncionarioAtualizado", funcionario.Id, CriarSnapshot(funcionario));
        }

        public void ResetarSenha(int funcionarioId, string senhaHash)
        {
            if (funcionarioId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(funcionarioId), "Funcionario invalido.");
            }

            if (string.IsNullOrWhiteSpace(senhaHash))
            {
                throw new InvalidOperationException("Informe uma senha valida para redefinicao.");
            }

            using var connection = _connectionFactory();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Funcionarios
                SET Senha = @Senha,
                    RowVersion = COALESCE(RowVersion, 0) + 1,
                    DataUltimaAlteracao = @DataUltimaAlteracao
                WHERE Id = @Id;";
            command.Parameters.AddWithValue("@Id", funcionarioId);
            command.Parameters.AddWithValue("@Senha", senhaHash);
            command.Parameters.AddWithValue("@DataUltimaAlteracao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.ExecuteNonQuery();

            RegistrarAuditoria("FuncionarioSenhaResetada", funcionarioId, $"FuncionarioId={funcionarioId}");
        }

        public void AtualizarStatusAcesso(int funcionarioId, bool ativo, string status)
        {
            if (funcionarioId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(funcionarioId), "Funcionario invalido.");
            }

            using var connection = _connectionFactory();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var (nome, perfil, statusAtual, ativoAtual) = ObterFuncionarioParaAcesso(connection, transaction, funcionarioId);
                if (ativoAtual == ativo && string.Equals(statusAtual, status, StringComparison.OrdinalIgnoreCase))
                {
                    transaction.Rollback();
                    return;
                }

                ValidarUltimoAdministrador(connection, transaction, funcionarioId, perfil, ativo);

                using var updateCommand = connection.CreateCommand();
                updateCommand.Transaction = transaction;
                updateCommand.CommandText = @"
                    UPDATE Funcionarios
                    SET Ativo = @Ativo,
                        Status = @Status,
                        RowVersion = COALESCE(RowVersion, 0) + 1,
                        DataUltimaAlteracao = @DataUltimaAlteracao
                    WHERE Id = @Id;";
                updateCommand.Parameters.AddWithValue("@Id", funcionarioId);
                updateCommand.Parameters.AddWithValue("@Ativo", ativo ? 1 : 0);
                updateCommand.Parameters.AddWithValue("@Status", string.IsNullOrWhiteSpace(status) ? (ativo ? "Ativo" : "Inativo") : status.Trim());
                updateCommand.Parameters.AddWithValue("@DataUltimaAlteracao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                updateCommand.ExecuteNonQuery();

                transaction.Commit();

                RegistrarAuditoria(
                    ativo ? "FuncionarioReativado" : "FuncionarioBloqueado",
                    funcionarioId,
                    $"Nome={nome}; Perfil={perfil}; Status={status}; Ativo={ativo}");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError($"Falha ao atualizar status de acesso do funcionario '{funcionarioId}'.", ex);
                throw;
            }
        }

        public void Excluir(int funcionarioId)
        {
            AtualizarStatusAcesso(funcionarioId, ativo: false, status: "Inativo");
        }

        private static (string Nome, string Perfil, string StatusAtual, bool AtivoAtual) ObterFuncionarioParaAcesso(
            SqliteConnection connection,
            SqliteTransaction transaction,
            int funcionarioId)
        {
            using var funcionarioCommand = connection.CreateCommand();
            funcionarioCommand.Transaction = transaction;
            funcionarioCommand.CommandText = @"
                SELECT Nome, PerfilAcesso, Status, Ativo
                FROM Funcionarios
                WHERE Id = @Id
                LIMIT 1;";
            funcionarioCommand.Parameters.AddWithValue("@Id", funcionarioId);

            using var reader = funcionarioCommand.ExecuteReader();
            if (!reader.Read())
            {
                throw new InvalidOperationException("Funcionario nao encontrado.");
            }

            return (
                ReadString(reader, 0),
                ReadString(reader, 1),
                ReadString(reader, 2),
                ReadBool(reader, 3));
        }

        private static void ValidarUltimoAdministrador(SqliteConnection connection, SqliteTransaction transaction, int funcionarioId, string perfil, bool novoStatusAtivo)
        {
            if (novoStatusAtivo || !string.Equals(perfil, "Administrador", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            using var adminCommand = connection.CreateCommand();
            adminCommand.Transaction = transaction;
            adminCommand.CommandText = @"
                SELECT COUNT(*)
                FROM Funcionarios
                WHERE Ativo = 1
                  AND PerfilAcesso = 'Administrador'
                  AND Id <> @Id;";
            adminCommand.Parameters.AddWithValue("@Id", funcionarioId);

            var outrosAdministradores = Convert.ToInt32(adminCommand.ExecuteScalar() ?? 0);
            if (outrosAdministradores == 0)
            {
                throw new InvalidOperationException("Nao e possivel desativar o ultimo administrador ativo.");
            }
        }

        private static void PreencherParametrosFuncionario(SqliteCommand command, Funcionario funcionario, bool incluirSenha)
        {
            command.Parameters.AddWithValue("@Nome", funcionario.Nome.Trim());
            command.Parameters.AddWithValue("@CPF", string.IsNullOrWhiteSpace(funcionario.CPF) ? DBNull.Value : funcionario.CPF.Trim());
            command.Parameters.AddWithValue("@Email", funcionario.Email.Trim());
            command.Parameters.AddWithValue("@Funcao", funcionario.Funcao.Trim());
            command.Parameters.AddWithValue("@PerfilAcesso", funcionario.PerfilAcesso.Trim());
            command.Parameters.AddWithValue("@Telefone", funcionario.Telefone?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("@Foto", string.IsNullOrWhiteSpace(funcionario.Foto) ? DBNull.Value : funcionario.Foto.Trim());
            command.Parameters.AddWithValue("@DataAdmissao", funcionario.DataAdmissao.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@Salario", funcionario.Salario);
            command.Parameters.AddWithValue("@Status", string.IsNullOrWhiteSpace(funcionario.Status) ? "Ativo" : funcionario.Status.Trim());
            command.Parameters.AddWithValue(
                "@DataCadastro",
                funcionario.DataCadastro == default
                    ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    : funcionario.DataCadastro.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue(
                "@Ativo",
                string.Equals(funcionario.Status, "Inativo", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(funcionario.Status, "Bloqueado", StringComparison.OrdinalIgnoreCase)
                    ? 0
                    : 1);

            if (incluirSenha)
            {
                command.Parameters.AddWithValue("@Senha", funcionario.Senha);
            }
        }

        private static Funcionario MaterializarFuncionario(SqliteDataReader reader)
        {
            return new Funcionario
            {
                Id = ReadInt(reader, 0),
                Nome = ReadString(reader, 1),
                CPF = ReadString(reader, 2),
                Email = ReadString(reader, 3),
                Funcao = ReadString(reader, 4),
                PerfilAcesso = ReadString(reader, 5),
                Telefone = ReadString(reader, 6),
                Foto = ReadString(reader, 7),
                DataAdmissao = ReadDateTime(reader, 8) ?? DateTime.Today,
                Salario = ReadDecimal(reader, 9),
                Status = ReadString(reader, 10),
                DataCadastro = ReadDateTime(reader, 11) ?? DateTime.Today,
                DataUltimoLogin = ReadDateTime(reader, 12),
                Ativo = ReadBool(reader, 13)
            };
        }

        private static string ReadString(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? string.Empty : Convert.ToString(reader.GetValue(index)) ?? string.Empty;
        }

        private static int ReadInt(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0 : Convert.ToInt32(reader.GetValue(index));
        }

        private static bool ReadBool(SqliteDataReader reader, int index)
        {
            return !reader.IsDBNull(index) && Convert.ToInt32(reader.GetValue(index)) == 1;
        }

        private static decimal ReadDecimal(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0 : Convert.ToDecimal(reader.GetValue(index));
        }

        private static DateTime? ReadDateTime(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) || !DateTime.TryParse(Convert.ToString(reader.GetValue(index)), out var value)
                ? null
                : value;
        }

        private static string CriarSnapshot(Funcionario funcionario)
        {
            return $"Nome={funcionario.Nome}; CPF={funcionario.CPF}; Email={funcionario.Email}; Funcao={funcionario.Funcao}; Perfil={funcionario.PerfilAcesso}; Status={funcionario.Status}; Ativo={funcionario.Ativo}; Foto={(string.IsNullOrWhiteSpace(funcionario.Foto) ? "Nao" : "Sim")}";
        }

        private static void PrepararEValidarFuncionario(Funcionario funcionario, int? ignorarFuncionarioId)
        {
            funcionario.Nome = funcionario.Nome?.Trim() ?? string.Empty;
            funcionario.CPF = CadastroValidationHelper.NormalizarDocumento(funcionario.CPF);
            funcionario.Email = CadastroValidationHelper.NormalizarEmail(funcionario.Email);
            funcionario.Telefone = CadastroValidationHelper.NormalizarTelefone(funcionario.Telefone);
            funcionario.Foto = funcionario.Foto?.Trim() ?? string.Empty;
            funcionario.Funcao = funcionario.Funcao?.Trim() ?? string.Empty;
            funcionario.PerfilAcesso = funcionario.PerfilAcesso?.Trim() ?? string.Empty;
            funcionario.Status = string.IsNullOrWhiteSpace(funcionario.Status) ? "Ativo" : funcionario.Status.Trim();

            if (string.IsNullOrWhiteSpace(funcionario.Nome))
            {
                throw new InvalidOperationException("Informe o nome do funcionario.");
            }

            var erroCpf = CadastroValidationHelper.ValidarCpf(funcionario.CPF, obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroCpf))
            {
                throw new InvalidOperationException(erroCpf);
            }

            var erroEmail = CadastroValidationHelper.ValidarEmail(funcionario.Email, obrigatorio: true);
            if (!string.IsNullOrWhiteSpace(erroEmail))
            {
                throw new InvalidOperationException(erroEmail);
            }

            var erroTelefone = CadastroValidationHelper.ValidarTelefone(funcionario.Telefone, obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroTelefone))
            {
                throw new InvalidOperationException(erroTelefone);
            }

            if (string.IsNullOrWhiteSpace(funcionario.Funcao))
            {
                throw new InvalidOperationException("Informe a funcao do funcionario.");
            }

            if (string.IsNullOrWhiteSpace(funcionario.PerfilAcesso))
            {
                throw new InvalidOperationException("Selecione um perfil de acesso valido.");
            }

            var erroData = CadastroValidationHelper.ValidarDataNaoFutura(funcionario.DataAdmissao, "a data de admissao", obrigatorio: true);
            if (!string.IsNullOrWhiteSpace(erroData))
            {
                throw new InvalidOperationException(erroData);
            }

            var erroSalario = CadastroValidationHelper.ValidarDecimal(funcionario.Salario, "um salario", permitirZero: false);
            if (!string.IsNullOrWhiteSpace(erroSalario))
            {
                throw new InvalidOperationException(erroSalario);
            }
        }

        private static void RegistrarAuditoria(string acao, int funcionarioId, string detalhes)
        {
            global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                "Funcionarios",
                acao,
                "Funcionario",
                funcionarioId.ToString(),
                detalhes);
        }
    }
}
