using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimoAutoEletrica.Services
{
    public partial class DatabaseService
    {
        private sealed record PermissaoSeed(string Nome, string Descricao, string Modulo, string Acao, string Codigo, bool Essencial, int Ordem);
        private sealed record PerfilSeed(string Nome, string Descricao, string Nivel, bool PodeDeletar, int Ordem, string[] Modulos);

        private void InitializeAccessControlSchema(DbConnection connection)
        {
            using var command = connection.CreateCommand();

            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Permissoes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nome TEXT NOT NULL,
                    Descricao TEXT NOT NULL,
                    Modulo TEXT NOT NULL,
                    Acao TEXT NOT NULL,
                    Codigo TEXT NOT NULL UNIQUE,
                    Ativo INTEGER NOT NULL DEFAULT 1,
                    DataCriacao TEXT NOT NULL,
                    Essencial INTEGER NOT NULL DEFAULT 0,
                    OrdemExibicao INTEGER NOT NULL DEFAULT 0
                );";
            command.ExecuteNonQuery();

            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS PerfisAcesso (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nome TEXT NOT NULL UNIQUE,
                    Descricao TEXT NOT NULL,
                    NivelHierarquico TEXT NOT NULL,
                    Ativo INTEGER NOT NULL DEFAULT 1,
                    DataCriacao TEXT NOT NULL,
                    DataUltimaModificacao TEXT,
                    CriadoPor TEXT,
                    ModificadoPor TEXT,
                    PodeDeletar INTEGER NOT NULL DEFAULT 1,
                    OrdemExibicao INTEGER NOT NULL DEFAULT 0
                );";
            command.ExecuteNonQuery();

            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS PerfilPermissoes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PerfilId INTEGER NOT NULL,
                    PermissaoId INTEGER NOT NULL,
                    Concedida INTEGER NOT NULL DEFAULT 1,
                    DataConcessao TEXT NOT NULL,
                    ConcedidaPor TEXT,
                    DataRevogacao TEXT,
                    RevogadaPor TEXT,
                    Ativa INTEGER NOT NULL DEFAULT 1,
                    Justificativa TEXT,
                    UNIQUE (PerfilId, PermissaoId)
                );";
            command.ExecuteNonQuery();

            HardenAccessControlIndexes(connection);
            SeedPermissoes(connection);
            SeedPerfis(connection);
        }

        private static void HardenAccessControlIndexes(DbConnection connection)
        {
            using var dedupePermissoes = connection.CreateCommand();
            dedupePermissoes.CommandText = @"
                DELETE FROM Permissoes
                WHERE Id NOT IN
                (
                    SELECT MIN(Id)
                    FROM Permissoes
                    GROUP BY Codigo
                );";
            dedupePermissoes.ExecuteNonQuery();

            using var indexPermissoes = connection.CreateCommand();
            indexPermissoes.CommandText = "CREATE UNIQUE INDEX IF NOT EXISTS UX_Permissoes_Codigo ON Permissoes (Codigo);";
            indexPermissoes.ExecuteNonQuery();

            using var dedupePerfis = connection.CreateCommand();
            dedupePerfis.CommandText = @"
                DELETE FROM PerfisAcesso
                WHERE Id NOT IN
                (
                    SELECT MIN(Id)
                    FROM PerfisAcesso
                    GROUP BY lower(Nome)
                );";
            dedupePerfis.ExecuteNonQuery();

            using var indexPerfis = connection.CreateCommand();
            indexPerfis.CommandText = "CREATE UNIQUE INDEX IF NOT EXISTS UX_PerfisAcesso_Nome ON PerfisAcesso (Nome);";
            indexPerfis.ExecuteNonQuery();

            using var dedupePerfilPermissoes = connection.CreateCommand();
            dedupePerfilPermissoes.CommandText = @"
                DELETE FROM PerfilPermissoes
                WHERE Id NOT IN
                (
                    SELECT MIN(Id)
                    FROM PerfilPermissoes
                    GROUP BY PerfilId, PermissaoId
                );";
            dedupePerfilPermissoes.ExecuteNonQuery();

            using var indexPerfilPermissoes = connection.CreateCommand();
            indexPerfilPermissoes.CommandText = "CREATE UNIQUE INDEX IF NOT EXISTS UX_PerfilPermissoes_Perfil_Permissao ON PerfilPermissoes (PerfilId, PermissaoId);";
            indexPerfilPermissoes.ExecuteNonQuery();
        }

        public List<Permissao> ObterPermissoes(bool incluirInativas = true)
        {
            var permissoes = new List<Permissao>();

            using var connection = GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Nome, Descricao, Modulo, Acao, Codigo, Ativo, DataCriacao, Essencial, OrdemExibicao
                FROM Permissoes
                WHERE @IncluirInativas = 1 OR Ativo = 1
                ORDER BY OrdemExibicao, Nome;";
            command.Parameters.AddWithValue("@IncluirInativas", incluirInativas ? 1 : 0);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                permissoes.Add(new Permissao
                {
                    Id = reader.GetInt32(0),
                    Nome = ReadAccessString(reader, 1),
                    Descricao = ReadAccessString(reader, 2),
                    Modulo = ReadAccessString(reader, 3),
                    Acao = ReadAccessString(reader, 4),
                    Codigo = ReadAccessString(reader, 5),
                    Ativo = ReadAccessBool(reader, 6),
                    DataCriacao = ReadAccessDate(reader, 7, DateTime.Now),
                    Essencial = ReadAccessBool(reader, 8),
                    OrdemExibicao = reader.IsDBNull(9) ? 0 : reader.GetInt32(9)
                });
            }

            return permissoes;
        }

        public List<PerfilAcesso> ObterPerfisAcesso(bool incluirInativos = true)
        {
            var perfis = new List<PerfilAcesso>();

            using var connection = GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Nome, Descricao, NivelHierarquico, Ativo, DataCriacao,
                       DataUltimaModificacao, CriadoPor, ModificadoPor, PodeDeletar, OrdemExibicao
                FROM PerfisAcesso
                WHERE @IncluirInativos = 1 OR Ativo = 1
                ORDER BY OrdemExibicao, Nome;";
            command.Parameters.AddWithValue("@IncluirInativos", incluirInativos ? 1 : 0);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                perfis.Add(new PerfilAcesso
                {
                    Id = reader.GetInt32(0),
                    Nome = ReadAccessString(reader, 1),
                    Descricao = ReadAccessString(reader, 2),
                    NivelHierarquico = ReadAccessString(reader, 3),
                    Ativo = ReadAccessBool(reader, 4),
                    DataCriacao = ReadAccessDate(reader, 5, DateTime.Now),
                    DataUltimaModificacao = ReadAccessNullableDate(reader, 6),
                    CriadoPor = ReadAccessString(reader, 7),
                    ModificadoPor = ReadAccessString(reader, 8),
                    PodeDeletar = ReadAccessBool(reader, 9),
                    OrdemExibicao = reader.IsDBNull(10) ? 0 : reader.GetInt32(10)
                });
            }

            return perfis;
        }

        public HashSet<int> ObterPermissoesDoPerfil(int perfilId)
        {
            var permissoes = new HashSet<int>();

            using var connection = GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT PermissaoId
                FROM PerfilPermissoes
                WHERE PerfilId = @PerfilId AND Ativa = 1 AND Concedida = 1;";
            command.Parameters.AddWithValue("@PerfilId", perfilId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                permissoes.Add(reader.GetInt32(0));
            }

            return permissoes;
        }

        public HashSet<string> ObterModulosPermitidosPorPerfil(string perfilAcesso)
        {
            var modulos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(perfilAcesso))
            {
                return modulos;
            }

            using var connection = GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT DISTINCT p.Modulo
                FROM PerfisAcesso pa
                INNER JOIN PerfilPermissoes pp ON pp.PerfilId = pa.Id
                INNER JOIN Permissoes p ON p.Id = pp.PermissaoId
                WHERE lower(pa.Nome) = lower(@Perfil)
                  AND pa.Ativo = 1
                  AND pp.Ativa = 1
                  AND pp.Concedida = 1
                  AND p.Ativo = 1
                  AND (
                      p.Codigo LIKE '%_VER'
                      OR p.Codigo = 'IMPORTAR_NFE_EXECUTAR'
                      OR p.Codigo = 'CATALOGO_VISUALIZAR'
                      OR p.Codigo = 'SISTEMA_CONFIGURAR'
                  );";
            command.Parameters.AddWithValue("@Perfil", perfilAcesso);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                modulos.Add(reader.GetString(0));
            }

            return modulos;
        }

        public bool? ObterPermissaoPorPerfilECodigo(string perfilAcesso, string codigoPermissao)
        {
            if (string.IsNullOrWhiteSpace(perfilAcesso) || string.IsNullOrWhiteSpace(codigoPermissao))
            {
                return null;
            }

            using var connection = GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            var selectFirst = IsSqlServerConnection(connection) ? "SELECT TOP (1)" : "SELECT";
            var limitOne = IsSqlServerConnection(connection) ? string.Empty : "LIMIT 1";
            command.CommandText = $@"
                {selectFirst} pa.Ativo, pp.Ativa, pp.Concedida, p.Ativo
                FROM PerfisAcesso pa
                INNER JOIN PerfilPermissoes pp ON pp.PerfilId = pa.Id
                INNER JOIN Permissoes p ON p.Id = pp.PermissaoId
                WHERE lower(pa.Nome) = lower(@Perfil)
                  AND upper(p.Codigo) = upper(@Codigo)
                {limitOne};";
            command.Parameters.AddWithValue("@Perfil", perfilAcesso.Trim());
            command.Parameters.AddWithValue("@Codigo", codigoPermissao.Trim());

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            var perfilAtivo = ReadAccessBool(reader, 0);
            var vinculoAtivo = ReadAccessBool(reader, 1);
            var concedida = ReadAccessBool(reader, 2);
            var permissaoAtiva = ReadAccessBool(reader, 3);

            return perfilAtivo && vinculoAtivo && concedida && permissaoAtiva;
        }

        public bool NomePerfilExiste(string nome, int? ignorarPerfilId = null)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                return false;
            }

            using var connection = GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*)
                FROM PerfisAcesso
                WHERE lower(Nome) = lower(@Nome)
                  AND Ativo = 1
                  AND (@IgnorarPerfilId IS NULL OR Id <> @IgnorarPerfilId);";
            command.Parameters.AddWithValue("@Nome", nome.Trim());
            command.Parameters.AddWithValue("@IgnorarPerfilId", ignorarPerfilId.HasValue
                ? ignorarPerfilId.Value
                : DBNull.Value);

            return Convert.ToInt32(command.ExecuteScalar() ?? 0) > 0;
        }

        public int CriarPerfilAcesso(PerfilAcesso perfil, IEnumerable<int> permissaoIds, string operador)
        {
            if (perfil == null)
            {
                throw new ArgumentNullException(nameof(perfil));
            }

            if (NomePerfilExiste(perfil.Nome))
            {
                throw new InvalidOperationException("Este nome de perfil ja esta cadastrado.");
            }

            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            using var insertPerfil = connection.CreateCommand();
            insertPerfil.Transaction = transaction;
            insertPerfil.CommandText = IsSqlServerConnection(connection)
                ? @"
                    INSERT INTO PerfisAcesso
                    (
                        Nome,
                        Descricao,
                        NivelHierarquico,
                        DataCriacao,
                        CriadoPor,
                        Ativo,
                        OrdemExibicao,
                        PodeDeletar
                    )
                    OUTPUT INSERTED.Id
                    VALUES
                    (
                        @Nome,
                        @Descricao,
                        @NivelHierarquico,
                        @DataCriacao,
                        @CriadoPor,
                        @Ativo,
                        @OrdemExibicao,
                        @PodeDeletar
                    );"
                : @"
                    INSERT INTO PerfisAcesso
                    (
                        Nome,
                        Descricao,
                        NivelHierarquico,
                        DataCriacao,
                        CriadoPor,
                        Ativo,
                        OrdemExibicao,
                        PodeDeletar
                    )
                    VALUES
                    (
                        @Nome,
                        @Descricao,
                        @NivelHierarquico,
                        @DataCriacao,
                        @CriadoPor,
                        @Ativo,
                        @OrdemExibicao,
                        @PodeDeletar
                    );
                    SELECT last_insert_rowid();";
            insertPerfil.Parameters.AddWithValue("@Nome", perfil.Nome.Trim());
            insertPerfil.Parameters.AddWithValue("@Descricao", perfil.Descricao.Trim());
            insertPerfil.Parameters.AddWithValue("@NivelHierarquico", perfil.NivelHierarquico.Trim());
            insertPerfil.Parameters.AddWithValue("@DataCriacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            insertPerfil.Parameters.AddWithValue("@CriadoPor", string.IsNullOrWhiteSpace(operador) ? "Sistema" : operador.Trim());
            insertPerfil.Parameters.AddWithValue("@Ativo", perfil.Ativo ? 1 : 0);
            insertPerfil.Parameters.AddWithValue("@OrdemExibicao", perfil.OrdemExibicao);
            insertPerfil.Parameters.AddWithValue("@PodeDeletar", perfil.PodeDeletar ? 1 : 0);

            var perfilId = Convert.ToInt32(insertPerfil.ExecuteScalar());

            foreach (var permissaoId in permissaoIds.Distinct())
            {
                UpsertPerfilPermissao(connection, transaction, perfilId, permissaoId, string.IsNullOrWhiteSpace(operador) ? "Sistema" : operador.Trim());
            }

            transaction.Commit();

            global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                "Seguranca",
                "PerfilCriado",
                "PerfilAcesso",
                perfilId.ToString(),
                $"Nome: {perfil.Nome}; Permissoes: {permissaoIds.Distinct().Count()}");

            return perfilId;
        }

        public void SalvarPermissao(Permissao permissao, string operador = "")
        {
            if (permissao == null)
            {
                throw new ArgumentNullException(nameof(permissao));
            }

            var operadorNormalizado = NormalizarOperador(operador);
            using var connection = GetConnection();
            connection.Open();

            var permissaoAnterior = permissao.Id > 0
                ? ObterPermissaoPorId(connection, permissao.Id)
                : null;

            using var command = connection.CreateCommand();
            if (permissao.Id == 0)
            {
                command.CommandText = IsSqlServerConnection(connection)
                    ? @"
                        INSERT INTO Permissoes (Nome, Descricao, Modulo, Acao, Codigo, Ativo, DataCriacao, Essencial, OrdemExibicao)
                        OUTPUT INSERTED.Id
                        VALUES (@Nome, @Descricao, @Modulo, @Acao, @Codigo, @Ativo, @DataCriacao, @Essencial, @OrdemExibicao);"
                    : @"
                        INSERT INTO Permissoes (Nome, Descricao, Modulo, Acao, Codigo, Ativo, DataCriacao, Essencial, OrdemExibicao)
                        VALUES (@Nome, @Descricao, @Modulo, @Acao, @Codigo, @Ativo, @DataCriacao, @Essencial, @OrdemExibicao);
                        SELECT last_insert_rowid();";
                command.Parameters.AddWithValue("@DataCriacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            else
            {
                command.CommandText = @"
                    UPDATE Permissoes
                    SET Nome = @Nome,
                        Descricao = @Descricao,
                        Modulo = @Modulo,
                        Acao = @Acao,
                        Codigo = @Codigo,
                        Ativo = @Ativo,
                        Essencial = @Essencial,
                        OrdemExibicao = @OrdemExibicao
                    WHERE Id = @Id;";
                command.Parameters.AddWithValue("@Id", permissao.Id);
            }

            command.Parameters.AddWithValue("@Nome", permissao.Nome.Trim());
            command.Parameters.AddWithValue("@Descricao", permissao.Descricao.Trim());
            command.Parameters.AddWithValue("@Modulo", permissao.Modulo.Trim());
            command.Parameters.AddWithValue("@Acao", permissao.Acao.Trim());
            command.Parameters.AddWithValue("@Codigo", permissao.Codigo.Trim().ToUpperInvariant());
            command.Parameters.AddWithValue("@Ativo", permissao.Ativo ? 1 : 0);
            command.Parameters.AddWithValue("@Essencial", permissao.Essencial ? 1 : 0);
            command.Parameters.AddWithValue("@OrdemExibicao", permissao.OrdemExibicao);

            if (permissao.Id == 0)
            {
                permissao.Id = Convert.ToInt32(command.ExecuteScalar());
            }
            else
            {
                command.ExecuteNonQuery();
            }

            global::PrimoAutoEletrica.App.Audit.Registrar(
                categoria: "Seguranca",
                acao: permissaoAnterior == null ? "PermissaoCriada" : "PermissaoAtualizada",
                entidade: "Permissao",
                entidadeId: permissao.Id.ToString(),
                detalhes: $"Operador={operadorNormalizado}",
                usuarioNomeOverride: operadorNormalizado,
                valorAnterior: permissaoAnterior == null ? null : FormatarPermissaoAuditoria(permissaoAnterior),
                valorNovo: FormatarPermissaoAuditoria(permissao));
        }

        public void ExcluirPermissao(int permissaoId, string operador = "")
        {
            var operadorNormalizado = NormalizarOperador(operador);
            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var permissao = ObterPermissaoPorId(connection, permissaoId, transaction)
                ?? throw new InvalidOperationException("Permissao nao encontrada.");

            if (permissao.Essencial)
            {
                throw new InvalidOperationException("Permissoes essenciais nao podem ser excluidas.");
            }

            using var deleteLinks = connection.CreateCommand();
            deleteLinks.Transaction = transaction;
            deleteLinks.CommandText = "DELETE FROM PerfilPermissoes WHERE PermissaoId = @PermissaoId;";
            deleteLinks.Parameters.AddWithValue("@PermissaoId", permissaoId);
            deleteLinks.ExecuteNonQuery();

            using var deletePermissao = connection.CreateCommand();
            deletePermissao.Transaction = transaction;
            deletePermissao.CommandText = "DELETE FROM Permissoes WHERE Id = @Id;";
            deletePermissao.Parameters.AddWithValue("@Id", permissaoId);
            if (deletePermissao.ExecuteNonQuery() == 0)
            {
                throw new InvalidOperationException("Nao foi possivel excluir a permissao informada.");
            }

            transaction.Commit();

            global::PrimoAutoEletrica.App.Audit.Registrar(
                categoria: "Seguranca",
                acao: "PermissaoExcluida",
                entidade: "Permissao",
                entidadeId: permissaoId.ToString(),
                detalhes: $"Operador={operadorNormalizado}",
                usuarioNomeOverride: operadorNormalizado,
                valorAnterior: FormatarPermissaoAuditoria(permissao));
        }

        public void AtualizarPerfil(PerfilAcesso perfil, IEnumerable<int> permissaoIds, string operador)
        {
            if (perfil == null)
            {
                throw new ArgumentNullException(nameof(perfil));
            }

            var operadorNormalizado = NormalizarOperador(operador);
            if (NomePerfilExiste(perfil.Nome, perfil.Id))
            {
                throw new InvalidOperationException("Este nome de perfil ja esta cadastrado.");
            }

            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var perfilAnterior = ObterPerfilAcessoPorId(connection, perfil.Id, transaction)
                ?? throw new InvalidOperationException("Perfil de acesso nao encontrado.");
            var permissoesAnteriores = ObterPermissoesAtivasDoPerfil(connection, perfil.Id, transaction);
            var permissoesAtualizadas = permissaoIds.Distinct().ToList();

            using var updatePerfil = connection.CreateCommand();
            updatePerfil.Transaction = transaction;
            updatePerfil.CommandText = @"
                UPDATE PerfisAcesso
                SET Nome = @Nome,
                    Descricao = @Descricao,
                    DataUltimaModificacao = @DataUltimaModificacao,
                    ModificadoPor = @ModificadoPor
                WHERE Id = @Id;";
            updatePerfil.Parameters.AddWithValue("@Nome", perfil.Nome.Trim());
            updatePerfil.Parameters.AddWithValue("@Descricao", perfil.Descricao.Trim());
            updatePerfil.Parameters.AddWithValue("@DataUltimaModificacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            updatePerfil.Parameters.AddWithValue("@ModificadoPor", operadorNormalizado);
            updatePerfil.Parameters.AddWithValue("@Id", perfil.Id);
            updatePerfil.ExecuteNonQuery();

            using var clearPermissoes = connection.CreateCommand();
            clearPermissoes.Transaction = transaction;
            clearPermissoes.CommandText = "UPDATE PerfilPermissoes SET Ativa = 0, DataRevogacao = @Data, RevogadaPor = @Usuario WHERE PerfilId = @PerfilId;";
            clearPermissoes.Parameters.AddWithValue("@Data", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            clearPermissoes.Parameters.AddWithValue("@Usuario", operadorNormalizado);
            clearPermissoes.Parameters.AddWithValue("@PerfilId", perfil.Id);
            clearPermissoes.ExecuteNonQuery();

            foreach (var permissaoId in permissoesAtualizadas)
            {
                UpsertPerfilPermissao(connection, transaction, perfil.Id, permissaoId, operadorNormalizado);
            }

            transaction.Commit();

            global::PrimoAutoEletrica.App.Audit.Registrar(
                categoria: "Seguranca",
                acao: "PerfilAtualizado",
                entidade: "PerfilAcesso",
                entidadeId: perfil.Id.ToString(),
                detalhes: $"Operador={operadorNormalizado}; PermissoesAntes={permissoesAnteriores.Count}; PermissoesDepois={permissoesAtualizadas.Count}",
                usuarioNomeOverride: operadorNormalizado,
                valorAnterior: FormatarPerfilAuditoria(perfilAnterior, permissoesAnteriores.Count),
                valorNovo: FormatarPerfilAuditoria(perfil, permissoesAtualizadas.Count));
        }

        public void ExcluirPerfilAcesso(int perfilId, string operador = "")
        {
            var operadorNormalizado = NormalizarOperador(operador);
            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var perfil = ObterPerfilAcessoPorId(connection, perfilId, transaction)
                ?? throw new InvalidOperationException("Perfil de acesso nao encontrado.");
            if (!perfil.PodeDeletar)
            {
                throw new InvalidOperationException("Este perfil nao pode ser excluido.");
            }

            var permissoesVinculadas = ObterPermissoesAtivasDoPerfil(connection, perfilId, transaction);

            using var deleteLinks = connection.CreateCommand();
            deleteLinks.Transaction = transaction;
            deleteLinks.CommandText = "DELETE FROM PerfilPermissoes WHERE PerfilId = @PerfilId;";
            deleteLinks.Parameters.AddWithValue("@PerfilId", perfilId);
            deleteLinks.ExecuteNonQuery();

            using var deletePerfil = connection.CreateCommand();
            deletePerfil.Transaction = transaction;
            deletePerfil.CommandText = "DELETE FROM PerfisAcesso WHERE Id = @Id;";
            deletePerfil.Parameters.AddWithValue("@Id", perfilId);
            if (deletePerfil.ExecuteNonQuery() == 0)
            {
                throw new InvalidOperationException("Nao foi possivel excluir o perfil informado.");
            }

            transaction.Commit();

            global::PrimoAutoEletrica.App.Audit.Registrar(
                categoria: "Seguranca",
                acao: "PerfilExcluido",
                entidade: "PerfilAcesso",
                entidadeId: perfilId.ToString(),
                detalhes: $"Operador={operadorNormalizado}; PermissoesVinculadas={permissoesVinculadas.Count}",
                usuarioNomeOverride: operadorNormalizado,
                valorAnterior: FormatarPerfilAuditoria(perfil, permissoesVinculadas.Count));
        }

        private static string NormalizarOperador(string operador)
        {
            return string.IsNullOrWhiteSpace(operador) ? "Sistema" : operador.Trim();
        }

        private static string FormatarPermissaoAuditoria(Permissao permissao)
        {
            return $"Codigo={permissao.Codigo}; Nome={permissao.Nome}; Modulo={permissao.Modulo}; Acao={permissao.Acao}; Ativo={permissao.Ativo}; Essencial={permissao.Essencial}";
        }

        private static string FormatarPerfilAuditoria(PerfilAcesso perfil, int totalPermissoes)
        {
            return $"Nome={perfil.Nome}; Descricao={perfil.Descricao}; Nivel={perfil.NivelHierarquico}; Ativo={perfil.Ativo}; PodeDeletar={perfil.PodeDeletar}; Permissoes={totalPermissoes}";
        }

        private static Permissao? ObterPermissaoPorId(DbConnection connection, int permissaoId, DbTransaction? transaction = null)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            var selectFirst = IsSqlServerConnection(connection) ? "SELECT TOP (1)" : "SELECT";
            var limitOne = IsSqlServerConnection(connection) ? string.Empty : "LIMIT 1";
            command.CommandText = $@"
                {selectFirst} Id, Nome, Descricao, Modulo, Acao, Codigo, Ativo, Essencial, OrdemExibicao
                FROM Permissoes
                WHERE Id = @Id
                {limitOne};";
            command.Parameters.AddWithValue("@Id", permissaoId);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return new Permissao
            {
                Id = reader.GetInt32(0),
                Nome = reader.GetString(1),
                Descricao = reader.GetString(2),
                Modulo = reader.GetString(3),
                Acao = reader.GetString(4),
                Codigo = reader.GetString(5),
                Ativo = ReadAccessBool(reader, 6),
                Essencial = ReadAccessBool(reader, 7),
                OrdemExibicao = reader.GetInt32(8)
            };
        }

        private static PerfilAcesso? ObterPerfilAcessoPorId(DbConnection connection, int perfilId, DbTransaction? transaction = null)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            var selectFirst = IsSqlServerConnection(connection) ? "SELECT TOP (1)" : "SELECT";
            var limitOne = IsSqlServerConnection(connection) ? string.Empty : "LIMIT 1";
            command.CommandText = $@"
                {selectFirst} Id, Nome, Descricao, NivelHierarquico, Ativo, PodeDeletar, OrdemExibicao
                FROM PerfisAcesso
                WHERE Id = @Id
                {limitOne};";
            command.Parameters.AddWithValue("@Id", perfilId);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return new PerfilAcesso
            {
                Id = reader.GetInt32(0),
                Nome = reader.GetString(1),
                Descricao = reader.GetString(2),
                NivelHierarquico = reader.GetString(3),
                Ativo = ReadAccessBool(reader, 4),
                PodeDeletar = ReadAccessBool(reader, 5),
                OrdemExibicao = reader.GetInt32(6)
            };
        }

        private static List<int> ObterPermissoesAtivasDoPerfil(DbConnection connection, int perfilId, DbTransaction? transaction = null)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT PermissaoId
                FROM PerfilPermissoes
                WHERE PerfilId = @PerfilId
                  AND Ativa = 1;";
            command.Parameters.AddWithValue("@PerfilId", perfilId);

            using var reader = command.ExecuteReader();
            var permissoes = new List<int>();
            while (reader.Read())
            {
                permissoes.Add(reader.GetInt32(0));
            }

            return permissoes;
        }

        private void SeedPermissoes(DbConnection connection)
        {
            var permissoes = new[]
            {
                new PermissaoSeed("Dashboard", "Acesso ao painel principal", "Dashboard", "Visualizar", "DASHBOARD_VER", true, 1),
                new PermissaoSeed("Clientes", "Gestao de clientes", "Clientes", "Visualizar", "CLIENTES_VER", true, 2),
                new PermissaoSeed("Veiculos", "Gestao de veiculos", "Veiculos", "Visualizar", "VEICULOS_VER", true, 3),
                new PermissaoSeed("Orcamentos", "Criacao e acompanhamento de orcamentos", "Orcamentos", "Visualizar", "ORCAMENTOS_VER", true, 4),
                new PermissaoSeed("Ordens de Servico", "Gestao de ordens de servico", "OrdensServico", "Visualizar", "ORDENS_SERVICO_VER", true, 5),
                new PermissaoSeed("PDV", "Ponto de venda", "PDV", "Visualizar", "PDV_VER", true, 6),
                new PermissaoSeed("Estoque", "Gestao de estoque", "Estoque", "Visualizar", "ESTOQUE_VER", true, 7),
                new PermissaoSeed("Catalogo de Pecas", "Base tecnica de consulta importada de fabricantes e fornecedores", "CatalogoPecas", "Visualizar", "CATALOGO_VISUALIZAR", true, 8),
                new PermissaoSeed("Importar NF-e", "Importacao de notas fiscais", "ImportarNFe", "Executar", "IMPORTAR_NFE_EXECUTAR", true, 9),
                new PermissaoSeed("Financeiro", "Gestao financeira", "Financeiro", "Visualizar", "FINANCEIRO_VER", true, 10),
                new PermissaoSeed("Relatorios", "Geracao de relatorios", "Relatorios", "Visualizar", "RELATORIOS_VER", true, 11),
                new PermissaoSeed("Fornecedores", "Gestao de fornecedores", "Fornecedores", "Visualizar", "FORNECEDORES_VER", true, 12),
                new PermissaoSeed("Funcionarios", "Gestao de funcionarios", "Funcionarios", "Visualizar", "FUNCIONARIOS_VER", true, 13),
                new PermissaoSeed("Agendamentos", "Gestao de agendamentos", "Agendamentos", "Visualizar", "AGENDAMENTOS_VER", true, 14),
                new PermissaoSeed("Sistema", "Configuracoes administrativas", "Sistema", "Configurar", "SISTEMA_CONFIGURAR", true, 15),
                new PermissaoSeed("Excluir clientes", "Permite excluir cadastros de clientes", "Clientes", "Excluir", "CLIENTES_EXCLUIR", false, 16),
                new PermissaoSeed("Excluir veiculos", "Permite excluir cadastros de veiculos", "Veiculos", "Excluir", "VEICULOS_EXCLUIR", false, 17),
                new PermissaoSeed("Excluir fornecedores", "Permite excluir fornecedores", "Fornecedores", "Excluir", "FORNECEDORES_EXCLUIR", false, 18),
                new PermissaoSeed("Criar funcionarios", "Permite cadastrar novos funcionarios", "Funcionarios", "Criar", "FUNCIONARIOS_CRIAR", false, 19),
                new PermissaoSeed("Editar funcionarios", "Permite editar funcionarios", "Funcionarios", "Editar", "FUNCIONARIOS_EDITAR", false, 20),
                new PermissaoSeed("Excluir funcionarios", "Permite excluir ou inativar funcionarios", "Funcionarios", "Excluir", "FUNCIONARIOS_EXCLUIR", false, 21),
                new PermissaoSeed("Gerenciar perfis", "Permite administrar perfis de acesso", "Funcionarios", "Gerenciar", "PERFIS_GERENCIAR", false, 22),
                new PermissaoSeed("Configurar permissoes", "Permite alterar permissoes do sistema", "Funcionarios", "Configurar", "PERMISSOES_CONFIGURAR", false, 23),
                new PermissaoSeed("Registrar venda", "Permite finalizar vendas no PDV", "PDV", "Executar", "PDV_REGISTRAR_VENDA", false, 24),
                new PermissaoSeed("Cancelar venda", "Permite cancelar a venda atual no PDV", "PDV", "Cancelar", "PDV_CANCELAR_VENDA", false, 25),
                new PermissaoSeed("Aplicar desconto", "Permite aplicar desconto no PDV", "PDV", "Aprovar", "PDV_APLICAR_DESCONTO", false, 26),
                new PermissaoSeed("Editar ordens de servico", "Permite criar e editar ordens de servico", "OrdensServico", "Editar", "ORDENS_SERVICO_EDITAR", false, 27),
                new PermissaoSeed("Aprovar ordens de servico", "Permite aprovar ordens de servico com o cliente", "OrdensServico", "Aprovar", "ORDENS_SERVICO_APROVAR", false, 28),
                new PermissaoSeed("Avancar status da OS", "Permite avancar o fluxo operacional da OS", "OrdensServico", "Executar", "ORDENS_SERVICO_AVANCAR_STATUS", false, 29),
                new PermissaoSeed("Excluir ordens de servico", "Permite excluir ordens de servico", "OrdensServico", "Excluir", "ORDENS_SERVICO_EXCLUIR", false, 30),
                new PermissaoSeed("Exportar relatorios", "Permite exportar relatorios para PDF ou Excel", "Relatorios", "Exportar", "RELATORIOS_EXPORTAR", false, 31),
                new PermissaoSeed("Imprimir relatorios", "Permite imprimir relatorios", "Relatorios", "Imprimir", "RELATORIOS_IMPRIMIR", false, 32),
                new PermissaoSeed("Exportar financeiro", "Permite exportar relatorios financeiros", "Financeiro", "Exportar", "FINANCEIRO_EXPORTAR", false, 33),
                new PermissaoSeed("Imprimir financeiro", "Permite imprimir relatorios financeiros", "Financeiro", "Imprimir", "FINANCEIRO_IMPRIMIR", false, 34),
                new PermissaoSeed("Exportar clientes", "Permite exportar cadastros de clientes com dados pessoais", "Clientes", "Exportar", "CLIENTES_EXPORTAR", false, 78),
                new PermissaoSeed("Exportar veiculos", "Permite exportar veiculos com vinculos de clientes e dados tecnicos", "Veiculos", "Exportar", "VEICULOS_EXPORTAR", false, 79),
                new PermissaoSeed("Exportar orcamentos", "Permite exportar orcamentos em PDF", "Orcamentos", "Exportar", "ORCAMENTOS_EXPORTAR", false, 35),
                new PermissaoSeed("Imprimir orcamentos", "Permite imprimir orcamentos", "Orcamentos", "Imprimir", "ORCAMENTOS_IMPRIMIR", false, 36),
                new PermissaoSeed("Converter orcamentos em venda", "Permite converter orcamentos em venda", "Orcamentos", "Executar", "ORCAMENTOS_CONVERTER_VENDA", false, 37),
                new PermissaoSeed("Cancelar agendamentos", "Permite cancelar agendamentos", "Agendamentos", "Cancelar", "AGENDAMENTOS_CANCELAR", false, 38),
                new PermissaoSeed("Gerar OS a partir do agendamento", "Permite converter agendamento em ordem de servico", "Agendamentos", "Executar", "AGENDAMENTOS_GERAR_OS", false, 39),
                new PermissaoSeed("Exportar agenda", "Permite exportar a agenda operacional", "Agendamentos", "Exportar", "AGENDAMENTOS_EXPORTAR", false, 40),
                new PermissaoSeed("Imprimir agenda", "Permite imprimir a agenda operacional", "Agendamentos", "Imprimir", "AGENDAMENTOS_IMPRIMIR", false, 41),
                new PermissaoSeed("Criar produtos", "Permite cadastrar novos produtos no estoque", "Estoque", "Criar", "ESTOQUE_CRIAR", false, 42),
                new PermissaoSeed("Editar produtos", "Permite editar cadastros de produtos", "Estoque", "Editar", "ESTOQUE_EDITAR", false, 43),
                new PermissaoSeed("Excluir produtos", "Permite excluir produtos do estoque", "Estoque", "Excluir", "ESTOQUE_EXCLUIR", false, 44),
                new PermissaoSeed("Ajustar estoque", "Permite realizar entradas e saidas manuais de estoque", "Estoque", "Executar", "ESTOQUE_AJUSTAR", false, 45),
                new PermissaoSeed("Ajustar precos do estoque", "Permite reajustar precos diretamente no estoque", "Estoque", "AjustarPreco", "ESTOQUE_AJUSTAR_PRECO", false, 46),
                new PermissaoSeed("Permitir estoque negativo", "Permite salvar estoque negativo sob responsabilidade gerencial", "Estoque", "Aprovar", "ESTOQUE_PERMITIR_NEGATIVO", false, 47),
                new PermissaoSeed("Abrir caixa", "Permite iniciar uma sessao operacional de caixa", "PDV", "Executar", "CAIXA_ABRIR", false, 48),
                new PermissaoSeed("Fechar caixa", "Permite encerrar a sessao operacional de caixa", "PDV", "Executar", "CAIXA_FECHAR", false, 49),
                new PermissaoSeed("Registrar sangria", "Permite retirar valores do caixa operacional", "PDV", "Executar", "CAIXA_SANGRIA", false, 50),
                new PermissaoSeed("Registrar suprimento", "Permite adicionar valores ao caixa operacional", "PDV", "Executar", "CAIXA_SUPRIMENTO", false, 51),
                new PermissaoSeed("Reimprimir comprovante", "Permite reimprimir o comprovante da ultima venda no PDV", "PDV", "Imprimir", "PDV_REIMPRIMIR", false, 52),
                new PermissaoSeed("Cancelar venda concluida", "Permite cancelar uma venda ja concluida com reversao operacional", "PDV", "Cancelar", "PDV_CANCELAR_VENDA_REGISTRADA", false, 53),
                new PermissaoSeed("Importar catalogos", "Permite gerar previa e confirmar importacoes de catalogo", "CatalogoPecas", "Importar", "CATALOGO_IMPORTAR", false, 74),
                new PermissaoSeed("Revisar catalogo", "Permite revisar, editar e ignorar itens do catalogo", "CatalogoPecas", "Revisar", "CATALOGO_REVISAR", false, 75),
                new PermissaoSeed("Criar produto a partir do catalogo", "Permite converter itens do catalogo em produtos reais", "CatalogoPecas", "CriarProduto", "CATALOGO_CRIAR_PRODUTO", false, 76),
                new PermissaoSeed("Exportar catalogo", "Permite exportar consultas do catalogo para CSV", "CatalogoPecas", "Exportar", "CATALOGO_EXPORTAR", false, 77),
                new PermissaoSeed("Pagamento misto", "Permite finalizar vendas com rateio entre formas de pagamento", "PDV", "Aprovar", "PDV_PAGAMENTO_MISTO", false, 71),
                new PermissaoSeed("Suspender venda", "Permite suspender uma venda em andamento no PDV", "PDV", "Executar", "PDV_SUSPENDER_VENDA", false, 72),
                new PermissaoSeed("Retomar venda suspensa", "Permite retomar vendas suspensas no PDV", "PDV", "Executar", "PDV_RETOMAR_VENDA", false, 73),
                new PermissaoSeed("Criar clientes", "Permite cadastrar novos clientes", "Clientes", "Criar", "CLIENTES_CRIAR", false, 54),
                new PermissaoSeed("Editar clientes", "Permite editar cadastros de clientes", "Clientes", "Editar", "CLIENTES_EDITAR", false, 55),
                new PermissaoSeed("Criar veiculos", "Permite cadastrar novos veiculos", "Veiculos", "Criar", "VEICULOS_CRIAR", false, 56),
                new PermissaoSeed("Editar veiculos", "Permite editar veiculos", "Veiculos", "Editar", "VEICULOS_EDITAR", false, 57),
                new PermissaoSeed("Criar fornecedores", "Permite cadastrar novos fornecedores", "Fornecedores", "Criar", "FORNECEDORES_CRIAR", false, 58),
                new PermissaoSeed("Editar fornecedores", "Permite editar fornecedores", "Fornecedores", "Editar", "FORNECEDORES_EDITAR", false, 59),
                new PermissaoSeed("Criar orcamentos", "Permite criar novos orcamentos", "Orcamentos", "Criar", "ORCAMENTOS_CRIAR", false, 60),
                new PermissaoSeed("Editar orcamentos", "Permite editar orcamentos e salvar rascunhos", "Orcamentos", "Editar", "ORCAMENTOS_EDITAR", false, 61),
                new PermissaoSeed("Duplicar orcamentos", "Permite duplicar orcamentos existentes", "Orcamentos", "Duplicar", "ORCAMENTOS_DUPLICAR", false, 62),
                new PermissaoSeed("Compartilhar orcamentos", "Permite compartilhar orcamentos por WhatsApp ou e-mail", "Orcamentos", "Compartilhar", "ORCAMENTOS_COMPARTILHAR", false, 63),
                new PermissaoSeed("Inventariar estoque", "Permite registrar contagem de inventario e reconciliar saldo fisico", "Estoque", "Inventariar", "ESTOQUE_INVENTARIAR", false, 64),
                new PermissaoSeed("Criar agendamentos", "Permite cadastrar novos agendamentos operacionais", "Agendamentos", "Criar", "AGENDAMENTOS_CRIAR", false, 65),
                new PermissaoSeed("Editar agendamentos", "Permite editar agendamentos e atualizar seus dados operacionais", "Agendamentos", "Editar", "AGENDAMENTOS_EDITAR", false, 66),
                new PermissaoSeed("Reagendar atendimentos", "Permite alterar a data operacional de um agendamento", "Agendamentos", "Reagendar", "AGENDAMENTOS_REAGENDAR", false, 67),
                new PermissaoSeed("Duplicar agendamentos", "Permite duplicar agendamentos existentes", "Agendamentos", "Duplicar", "AGENDAMENTOS_DUPLICAR", false, 68),
                new PermissaoSeed("Registrar check-in", "Permite iniciar o atendimento e reservar pecas para o agendamento", "Agendamentos", "CheckIn", "AGENDAMENTOS_CHECKIN", false, 69),
                new PermissaoSeed("Registrar check-out", "Permite finalizar o atendimento e concluir suas integracoes operacionais", "Agendamentos", "CheckOut", "AGENDAMENTOS_CHECKOUT", false, 70),
                new PermissaoSeed("Compartilhar agendamentos", "Permite enviar lembretes e atualizacoes do agendamento por WhatsApp ou e-mail", "Agendamentos", "Compartilhar", "AGENDAMENTOS_COMPARTILHAR", false, 71)
            };

            foreach (var permissao in permissoes)
            {
                using var command = connection.CreateCommand();
                command.CommandText = IsSqlServerConnection(connection)
                    ? @"
                        UPDATE Permissoes
                        SET Nome = @Nome,
                            Descricao = @Descricao,
                            Modulo = @Modulo,
                            Acao = @Acao,
                            Essencial = @Essencial,
                            OrdemExibicao = @OrdemExibicao,
                            Ativo = 1
                        WHERE Codigo = @Codigo;

                        IF @@ROWCOUNT = 0
                        BEGIN
                            INSERT INTO Permissoes (Nome, Descricao, Modulo, Acao, Codigo, Ativo, DataCriacao, Essencial, OrdemExibicao)
                            VALUES (@Nome, @Descricao, @Modulo, @Acao, @Codigo, 1, @DataCriacao, @Essencial, @OrdemExibicao);
                        END;"
                    : @"
                        INSERT INTO Permissoes (Nome, Descricao, Modulo, Acao, Codigo, Ativo, DataCriacao, Essencial, OrdemExibicao)
                        VALUES (@Nome, @Descricao, @Modulo, @Acao, @Codigo, 1, @DataCriacao, @Essencial, @OrdemExibicao)
                        ON CONFLICT(Codigo)
                        DO UPDATE SET Nome = excluded.Nome,
                                      Descricao = excluded.Descricao,
                                      Modulo = excluded.Modulo,
                                      Acao = excluded.Acao,
                                      Essencial = excluded.Essencial,
                                      OrdemExibicao = excluded.OrdemExibicao,
                                      Ativo = 1;";
                command.Parameters.AddWithValue("@Nome", permissao.Nome);
                command.Parameters.AddWithValue("@Descricao", permissao.Descricao);
                command.Parameters.AddWithValue("@Modulo", permissao.Modulo);
                command.Parameters.AddWithValue("@Acao", permissao.Acao);
                command.Parameters.AddWithValue("@Codigo", permissao.Codigo);
                command.Parameters.AddWithValue("@DataCriacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@Essencial", permissao.Essencial ? 1 : 0);
                command.Parameters.AddWithValue("@OrdemExibicao", permissao.Ordem);
                command.ExecuteNonQuery();
            }
        }

        private void SeedPerfis(DbConnection connection)
        {
            var perfis = new[]
            {
                new PerfilSeed("Administrador", "Acesso total ao sistema", "Executivo", false, 1, new[] { "*" }),
                new PerfilSeed("Gerente", "Gestao operacional completa sem configuracoes criticas", "Gerencial", false, 2, new[] { "Dashboard", "Clientes", "Veiculos", "Orcamentos", "OrdensServico", "PDV", "Estoque", "CatalogoPecas", "ImportarNFe", "Financeiro", "Relatorios", "Fornecedores", "Funcionarios", "Agendamentos" }),
                new PerfilSeed("Mecanico", "Operacao tecnica da oficina", "Operacional", false, 3, new[] { "Dashboard", "Veiculos", "OrdensServico", "Agendamentos" }),
                new PerfilSeed("Mecânico", "Operacao tecnica da oficina", "Operacional", false, 4, new[] { "Dashboard", "Veiculos", "OrdensServico", "Agendamentos" }),
                new PerfilSeed("Vendedor", "Atendimento comercial e orcamentos", "Operacional", false, 5, new[] { "Dashboard", "Clientes", "Veiculos", "Orcamentos", "OrdensServico", "CatalogoPecas", "Agendamentos" }),
                new PerfilSeed("Caixa", "PDV e financeiro operacional", "Operacional", false, 6, new[] { "Dashboard", "PDV", "Financeiro", "Relatorios" }),
                new PerfilSeed("Estoquista", "Controle de estoque e fornecedores", "Operacional", false, 7, new[] { "Dashboard", "Estoque", "CatalogoPecas", "ImportarNFe", "Relatorios", "Fornecedores" }),
                new PerfilSeed("Almoxarife", "Controle de estoque e fornecedores", "Operacional", false, 8, new[] { "Dashboard", "Estoque", "CatalogoPecas", "ImportarNFe", "Relatorios", "Fornecedores" }),
                new PerfilSeed("Financeiro", "Rotinas financeiras e relatorios", "Operacional", false, 9, new[] { "Dashboard", "Financeiro", "Relatorios" }),
                new PerfilSeed("Tecnico", "Operacao tecnica da oficina", "Operacional", false, 10, new[] { "Dashboard", "Veiculos", "OrdensServico", "Agendamentos" })
            };

            foreach (var perfil in perfis)
            {
                using var command = connection.CreateCommand();
                command.CommandText = IsSqlServerConnection(connection)
                    ? @"
                        UPDATE PerfisAcesso
                        SET Descricao = @Descricao,
                            NivelHierarquico = @Nivel,
                            Ativo = 1,
                            PodeDeletar = @PodeDeletar,
                            OrdemExibicao = @Ordem
                        WHERE Nome = @Nome;

                        IF @@ROWCOUNT = 0
                        BEGIN
                            INSERT INTO PerfisAcesso (Nome, Descricao, NivelHierarquico, Ativo, DataCriacao, CriadoPor, PodeDeletar, OrdemExibicao)
                            VALUES (@Nome, @Descricao, @Nivel, 1, @DataCriacao, 'Sistema', @PodeDeletar, @Ordem);
                        END;"
                    : @"
                        INSERT INTO PerfisAcesso (Nome, Descricao, NivelHierarquico, Ativo, DataCriacao, CriadoPor, PodeDeletar, OrdemExibicao)
                        VALUES (@Nome, @Descricao, @Nivel, 1, @DataCriacao, 'Sistema', @PodeDeletar, @Ordem)
                        ON CONFLICT(Nome)
                        DO UPDATE SET Descricao = excluded.Descricao,
                                      NivelHierarquico = excluded.NivelHierarquico,
                                      Ativo = 1,
                                      PodeDeletar = excluded.PodeDeletar,
                                      OrdemExibicao = excluded.OrdemExibicao;";
                command.Parameters.AddWithValue("@Nome", perfil.Nome);
                command.Parameters.AddWithValue("@Descricao", perfil.Descricao);
                command.Parameters.AddWithValue("@Nivel", perfil.Nivel);
                command.Parameters.AddWithValue("@DataCriacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@PodeDeletar", perfil.PodeDeletar ? 1 : 0);
                command.Parameters.AddWithValue("@Ordem", perfil.Ordem);
                command.ExecuteNonQuery();

                VincularPermissoesPadrao(connection, perfil);
            }
        }

        private void VincularPermissoesPadrao(DbConnection connection, PerfilSeed perfil)
        {
            var perfilId = ObterIdPerfil(connection, perfil.Nome);
            if (perfilId == 0)
            {
                return;
            }

            var permissaoIds = ObterIdsPermissoes(connection, perfil.Modulos, somenteEssenciais: !perfil.Modulos.Contains("*"));
            if (!perfil.Modulos.Contains("*"))
            {
                permissaoIds.AddRange(ObterIdsPermissoesPorCodigo(connection, PermissionService.ObterCodigosAcaoPadraoPorPerfil(perfil.Nome)));
                permissaoIds = permissaoIds.Distinct().ToList();
            }

            foreach (var permissaoId in permissaoIds)
            {
                UpsertPerfilPermissao(connection, null, perfilId, permissaoId, "Sistema");
            }
        }

        private static void UpsertPerfilPermissao(DbConnection connection, DbTransaction? transaction, int perfilId, int permissaoId, string usuario)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = IsSqlServerConnection(connection)
                ? @"
                    UPDATE PerfilPermissoes
                    SET Concedida = 1,
                        DataConcessao = @DataConcessao,
                        ConcedidaPor = @ConcedidaPor,
                        DataRevogacao = NULL,
                        RevogadaPor = NULL,
                        Ativa = 1
                    WHERE PerfilId = @PerfilId
                      AND PermissaoId = @PermissaoId;

                    IF @@ROWCOUNT = 0
                    BEGIN
                        INSERT INTO PerfilPermissoes (PerfilId, PermissaoId, Concedida, DataConcessao, ConcedidaPor, Ativa)
                        VALUES (@PerfilId, @PermissaoId, 1, @DataConcessao, @ConcedidaPor, 1);
                    END;"
                : @"
                    INSERT INTO PerfilPermissoes (PerfilId, PermissaoId, Concedida, DataConcessao, ConcedidaPor, Ativa)
                    VALUES (@PerfilId, @PermissaoId, 1, @DataConcessao, @ConcedidaPor, 1)
                    ON CONFLICT(PerfilId, PermissaoId)
                    DO UPDATE SET Concedida = 1,
                                  DataConcessao = excluded.DataConcessao,
                                  ConcedidaPor = excluded.ConcedidaPor,
                                  DataRevogacao = NULL,
                                  RevogadaPor = NULL,
                                  Ativa = 1;";
            command.Parameters.AddWithValue("@PerfilId", perfilId);
            command.Parameters.AddWithValue("@PermissaoId", permissaoId);
            command.Parameters.AddWithValue("@DataConcessao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@ConcedidaPor", usuario);
            command.ExecuteNonQuery();
        }
        private static int ObterIdPerfil(DbConnection connection, string nome)
        {
            using var command = connection.CreateCommand();
            command.CommandText = IsSqlServerConnection(connection)
                ? "SELECT TOP (1) Id FROM PerfisAcesso WHERE Nome = @Nome;"
                : "SELECT Id FROM PerfisAcesso WHERE Nome = @Nome LIMIT 1;";
            command.Parameters.AddWithValue("@Nome", nome);
            return Convert.ToInt32(command.ExecuteScalar() ?? 0);
        }

        private static List<int> ObterIdsPermissoes(DbConnection connection, IReadOnlyCollection<string> modulos, bool somenteEssenciais = false)
        {
            using var command = connection.CreateCommand();
            if (modulos.Contains("*"))
            {
                command.CommandText = somenteEssenciais
                    ? "SELECT Id FROM Permissoes WHERE Ativo = 1 AND Essencial = 1;"
                    : "SELECT Id FROM Permissoes WHERE Ativo = 1;";
            }
            else
            {
                var parametros = modulos.Select((_, index) => $"@Modulo{index}").ToArray();
                var filtroEssenciais = somenteEssenciais ? " AND Essencial = 1" : string.Empty;
                command.CommandText = $"SELECT Id FROM Permissoes WHERE Ativo = 1{filtroEssenciais} AND Modulo IN ({string.Join(",", parametros)});";
                var index = 0;
                foreach (var modulo in modulos)
                {
                    command.Parameters.AddWithValue($"@Modulo{index}", modulo);
                    index++;
                }
            }

            var ids = new List<int>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                ids.Add(reader.GetInt32(0));
            }

            return ids;
        }

        private static List<int> ObterIdsPermissoesPorCodigo(DbConnection connection, IReadOnlyCollection<string> codigos)
        {
            var codigosNormalizados = codigos
                .Where(codigo => !string.IsNullOrWhiteSpace(codigo))
                .Select(codigo => codigo.Trim().ToUpperInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (codigosNormalizados.Length == 0)
            {
                return new List<int>();
            }

            using var command = connection.CreateCommand();
            var parametros = codigosNormalizados.Select((_, index) => $"@Codigo{index}").ToArray();
            command.CommandText = $"SELECT Id FROM Permissoes WHERE Ativo = 1 AND Codigo IN ({string.Join(",", parametros)});";

            for (var index = 0; index < codigosNormalizados.Length; index++)
            {
                command.Parameters.AddWithValue(parametros[index], codigosNormalizados[index]);
            }

            var ids = new List<int>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                ids.Add(reader.GetInt32(0));
            }

            return ids;
        }

        private static string ReadAccessString(DbDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? string.Empty : Convert.ToString(reader.GetValue(ordinal)) ?? string.Empty;
        }

        private static bool ReadAccessBool(DbDataReader reader, int ordinal)
        {
            return !reader.IsDBNull(ordinal) && Convert.ToInt32(reader.GetValue(ordinal)) == 1;
        }

        private static DateTime ReadAccessDate(DbDataReader reader, int ordinal, DateTime fallback)
        {
            if (reader.IsDBNull(ordinal))
            {
                return fallback;
            }

            var value = reader.GetValue(ordinal);
            return value is DateTime date || DateTime.TryParse(Convert.ToString(value), out date)
                ? date
                : fallback;
        }

        private static DateTime? ReadAccessNullableDate(DbDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            var value = reader.GetValue(ordinal);
            return value is DateTime date || DateTime.TryParse(Convert.ToString(value), out date)
                ? date
                : null;
        }
    }
}

