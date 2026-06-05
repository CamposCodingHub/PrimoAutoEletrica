using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PrimoAutoEletrica.Services
{
    public sealed class FuncionarioOperationalService
    {
        private readonly DatabaseService _databaseService;

        public FuncionarioOperationalService(DatabaseService? databaseService = null)
        {
            _databaseService = databaseService ?? global::PrimoAutoEletrica.App.Database;
        }

        public FuncionarioPainelOperacional ObterPainelOperacional(Funcionario? funcionario, DateTime? dataInicio = null, DateTime? dataFim = null)
        {
            if (funcionario == null)
            {
                return FuncionarioPainelOperacional.Vazio("Selecione um colaborador para ver auditoria, produtividade e permissoes.");
            }

            var inicio = (dataInicio ?? DateTime.Today.AddDays(-30)).Date;
            var fim = dataFim ?? DateTime.Now;
            var auditoria = ObterResumoAuditoria(funcionario, inicio, fim);
            var permissoes = ObterResumoPermissoes(funcionario.PerfilAcesso);

            return new FuncionarioPainelOperacional(
                funcionario.Id,
                funcionario.Nome,
                funcionario.PerfilAcesso,
                inicio,
                fim,
                auditoria.TotalAcoes,
                auditoria.TotalFalhas,
                auditoria.TotalCriticas,
                auditoria.UltimaAcao,
                auditoria.CategoriaMaisAtiva,
                auditoria.AcoesPorCategoria,
                auditoria.AcoesRecentes,
                permissoes.TotalPermissoes,
                permissoes.TotalEssenciais,
                permissoes.TotalSensiveis,
                permissoes.ModulosLiberados,
                permissoes.PermissoesPorAcao);
        }

        private AuditoriaFuncionarioResumo ObterResumoAuditoria(Funcionario funcionario, DateTime inicio, DateTime fim)
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();

            var filtroUsuario = CriarFiltroUsuario(funcionario);
            var totalAcoes = ObterTotalAuditoria(connection, filtroUsuario, inicio, fim, apenasFalhas: false);
            var totalFalhas = ObterTotalAuditoria(connection, filtroUsuario, inicio, fim, apenasFalhas: true);
            var totalCriticas = ObterTotalAuditoriaCritica(connection, filtroUsuario, inicio, fim);
            var ultimaAcao = ObterUltimaAcao(connection, filtroUsuario, inicio, fim);
            var acoesPorCategoria = ObterAcoesPorCategoria(connection, filtroUsuario, inicio, fim);
            var acoesRecentes = ObterAcoesRecentes(connection, filtroUsuario, inicio, fim);

            return new AuditoriaFuncionarioResumo
            {
                TotalAcoes = totalAcoes,
                TotalFalhas = totalFalhas,
                TotalCriticas = totalCriticas,
                UltimaAcao = ultimaAcao,
                CategoriaMaisAtiva = acoesPorCategoria.FirstOrDefault()?.Categoria ?? "Sem categoria ativa",
                AcoesPorCategoria = acoesPorCategoria,
                AcoesRecentes = acoesRecentes
            };
        }

        private PermissoesFuncionarioResumo ObterResumoPermissoes(string perfilAcesso)
        {
            if (string.IsNullOrWhiteSpace(perfilAcesso))
            {
                return new PermissoesFuncionarioResumo();
            }

            using var connection = _databaseService.GetConnection();
            connection.Open();

            var permissoesPorAcao = new List<FuncionarioPermissaoAcaoResumo>();
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    p.Acao,
                    COUNT(*) AS Total,
                    SUM(CASE WHEN p.Essencial = 1 THEN 1 ELSE 0 END) AS Essenciais,
                    SUM(CASE
                        WHEN p.Acao IN ('Excluir', 'Configurar', 'Aprovar', 'Cancelar', 'AjustarPreco', 'Inventariar')
                             OR p.Codigo LIKE '%EXCLUIR%'
                             OR p.Codigo LIKE '%CANCELAR%'
                             OR p.Codigo LIKE '%CONFIGURAR%'
                        THEN 1 ELSE 0 END) AS Sensiveis
                FROM PerfisAcesso pa
                INNER JOIN PerfilPermissoes pp ON pp.PerfilId = pa.Id
                INNER JOIN Permissoes p ON p.Id = pp.PermissaoId
                WHERE lower(pa.Nome) = lower(@Perfil)
                  AND pa.Ativo = 1
                  AND pp.Ativa = 1
                  AND pp.Concedida = 1
                  AND p.Ativo = 1
                GROUP BY p.Acao
                ORDER BY Total DESC, p.Acao;";
            command.Parameters.AddWithValue("@Perfil", perfilAcesso.Trim());

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    permissoesPorAcao.Add(new FuncionarioPermissaoAcaoResumo(
                        ReadString(reader, 0),
                        ReadInt(reader, 1),
                        ReadInt(reader, 2),
                        ReadInt(reader, 3)));
                }
            }

            var modulosLiberados = ObterModulosLiberados(connection, perfilAcesso);

            return new PermissoesFuncionarioResumo
            {
                PermissoesPorAcao = permissoesPorAcao,
                ModulosLiberados = modulosLiberados,
                TotalPermissoes = permissoesPorAcao.Sum(item => item.Total),
                TotalEssenciais = permissoesPorAcao.Sum(item => item.Essenciais),
                TotalSensiveis = permissoesPorAcao.Sum(item => item.Sensiveis)
            };
        }

        private static UsuarioAuditFilter CriarFiltroUsuario(Funcionario funcionario)
        {
            return new UsuarioAuditFilter(funcionario.Id, funcionario.Nome);
        }

        private static int ObterTotalAuditoria(SqliteConnection connection, UsuarioAuditFilter filtroUsuario, DateTime inicio, DateTime fim, bool apenasFalhas)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT COUNT(*)
                FROM AuditLogs
                WHERE DataHora BETWEEN @Inicio AND @Fim
                  AND ({filtroUsuario.Sql})
                  {(apenasFalhas ? "AND Sucesso = 0" : string.Empty)};";
            filtroUsuario.Preencher(command);
            command.Parameters.AddWithValue("@Inicio", inicio.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("@Fim", fim.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
            return Convert.ToInt32(command.ExecuteScalar() ?? 0);
        }

        private static int ObterTotalAuditoriaCritica(SqliteConnection connection, UsuarioAuditFilter filtroUsuario, DateTime inicio, DateTime fim)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT COUNT(*)
                FROM AuditLogs
                WHERE DataHora BETWEEN @Inicio AND @Fim
                  AND ({filtroUsuario.Sql})
                  AND (
                    Severidade IN ('Warning', 'Error', 'Critical')
                    OR Acao LIKE '%Exclu%'
                    OR Acao LIKE '%Bloque%'
                    OR Acao LIKE '%Senha%'
                    OR Categoria = 'Seguranca'
                  );";
            filtroUsuario.Preencher(command);
            command.Parameters.AddWithValue("@Inicio", inicio.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("@Fim", fim.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
            return Convert.ToInt32(command.ExecuteScalar() ?? 0);
        }

        private static DateTime? ObterUltimaAcao(SqliteConnection connection, UsuarioAuditFilter filtroUsuario, DateTime inicio, DateTime fim)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT MAX(DataHora)
                FROM AuditLogs
                WHERE DataHora BETWEEN @Inicio AND @Fim
                  AND ({filtroUsuario.Sql});";
            filtroUsuario.Preencher(command);
            command.Parameters.AddWithValue("@Inicio", inicio.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("@Fim", fim.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));

            var value = Convert.ToString(command.ExecuteScalar());
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var data)
                ? data
                : null;
        }

        private static List<FuncionarioAuditoriaCategoriaResumo> ObterAcoesPorCategoria(SqliteConnection connection, UsuarioAuditFilter filtroUsuario, DateTime inicio, DateTime fim)
        {
            var itens = new List<FuncionarioAuditoriaCategoriaResumo>();
            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT Categoria, COUNT(*), SUM(CASE WHEN Sucesso = 0 THEN 1 ELSE 0 END)
                FROM AuditLogs
                WHERE DataHora BETWEEN @Inicio AND @Fim
                  AND ({filtroUsuario.Sql})
                GROUP BY Categoria
                ORDER BY COUNT(*) DESC, Categoria
                LIMIT 6;";
            filtroUsuario.Preencher(command);
            command.Parameters.AddWithValue("@Inicio", inicio.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("@Fim", fim.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                itens.Add(new FuncionarioAuditoriaCategoriaResumo(
                    ReadString(reader, 0),
                    ReadInt(reader, 1),
                    ReadInt(reader, 2)));
            }

            return itens;
        }

        private static List<FuncionarioAcaoAuditadaResumo> ObterAcoesRecentes(SqliteConnection connection, UsuarioAuditFilter filtroUsuario, DateTime inicio, DateTime fim)
        {
            var itens = new List<FuncionarioAcaoAuditadaResumo>();
            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT Categoria, Acao, DataHora, Sucesso, Severidade
                FROM AuditLogs
                WHERE DataHora BETWEEN @Inicio AND @Fim
                  AND ({filtroUsuario.Sql})
                ORDER BY DataHora DESC
                LIMIT 5;";
            filtroUsuario.Preencher(command);
            command.Parameters.AddWithValue("@Inicio", inicio.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("@Fim", fim.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                itens.Add(new FuncionarioAcaoAuditadaResumo(
                    ReadString(reader, 0),
                    ReadString(reader, 1),
                    ReadDate(reader, 2),
                    ReadInt(reader, 3) == 1,
                    ReadString(reader, 4)));
            }

            return itens;
        }

        private static List<string> ObterModulosLiberados(SqliteConnection connection, string perfilAcesso)
        {
            var itens = new List<string>();
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
                ORDER BY p.Modulo;";
            command.Parameters.AddWithValue("@Perfil", perfilAcesso.Trim());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                itens.Add(ReadString(reader, 0));
            }

            return itens;
        }

        private static string ReadString(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? string.Empty : Convert.ToString(reader.GetValue(index)) ?? string.Empty;
        }

        private static int ReadInt(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0 : Convert.ToInt32(reader.GetValue(index));
        }

        private static DateTime ReadDate(SqliteDataReader reader, int index)
        {
            return DateTime.TryParse(ReadString(reader, index), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var value)
                ? value
                : DateTime.MinValue;
        }

        private sealed record UsuarioAuditFilter(int Id, string Nome)
        {
            public string Sql => "UsuarioId = @UsuarioId OR lower(COALESCE(UsuarioNome, '')) = lower(@UsuarioNome)";

            public void Preencher(SqliteCommand command)
            {
                command.Parameters.AddWithValue("@UsuarioId", Id);
                command.Parameters.AddWithValue("@UsuarioNome", Nome ?? string.Empty);
            }
        }

        private sealed class AuditoriaFuncionarioResumo
        {
            public int TotalAcoes { get; init; }
            public int TotalFalhas { get; init; }
            public int TotalCriticas { get; init; }
            public DateTime? UltimaAcao { get; init; }
            public string CategoriaMaisAtiva { get; init; } = string.Empty;
            public IReadOnlyList<FuncionarioAuditoriaCategoriaResumo> AcoesPorCategoria { get; init; } = Array.Empty<FuncionarioAuditoriaCategoriaResumo>();
            public IReadOnlyList<FuncionarioAcaoAuditadaResumo> AcoesRecentes { get; init; } = Array.Empty<FuncionarioAcaoAuditadaResumo>();
        }

        private sealed class PermissoesFuncionarioResumo
        {
            public int TotalPermissoes { get; init; }
            public int TotalEssenciais { get; init; }
            public int TotalSensiveis { get; init; }
            public IReadOnlyList<string> ModulosLiberados { get; init; } = Array.Empty<string>();
            public IReadOnlyList<FuncionarioPermissaoAcaoResumo> PermissoesPorAcao { get; init; } = Array.Empty<FuncionarioPermissaoAcaoResumo>();
        }
    }

    public sealed record FuncionarioPainelOperacional(
        int FuncionarioId,
        string Nome,
        string Perfil,
        DateTime DataInicio,
        DateTime DataFim,
        int TotalAcoesAuditadas,
        int TotalFalhas,
        int TotalAcoesCriticas,
        DateTime? UltimaAcao,
        string CategoriaMaisAtiva,
        IReadOnlyList<FuncionarioAuditoriaCategoriaResumo> AcoesPorCategoria,
        IReadOnlyList<FuncionarioAcaoAuditadaResumo> AcoesRecentes,
        int TotalPermissoes,
        int TotalPermissoesEssenciais,
        int TotalPermissoesSensiveis,
        IReadOnlyList<string> ModulosLiberados,
        IReadOnlyList<FuncionarioPermissaoAcaoResumo> PermissoesPorAcao)
    {
        public static FuncionarioPainelOperacional Vazio(string mensagem)
        {
            return new FuncionarioPainelOperacional(
                0,
                mensagem,
                string.Empty,
                DateTime.Today.AddDays(-30),
                DateTime.Now,
                0,
                0,
                0,
                null,
                "Sem dados",
                Array.Empty<FuncionarioAuditoriaCategoriaResumo>(),
                Array.Empty<FuncionarioAcaoAuditadaResumo>(),
                0,
                0,
                0,
                Array.Empty<string>(),
                Array.Empty<FuncionarioPermissaoAcaoResumo>());
        }

        public string ProdutividadeResumo => TotalAcoesAuditadas == 0
            ? "Nenhuma acao auditada nos ultimos 30 dias."
            : $"{TotalAcoesAuditadas} acao(oes) auditada(s) nos ultimos 30 dias; categoria mais ativa: {CategoriaMaisAtiva}.";

        public string AuditoriaResumo => TotalFalhas == 0
            ? $"Sem falhas registradas. Acoes criticas acompanhadas: {TotalAcoesCriticas}."
            : $"{TotalFalhas} falha(s) registrada(s) e {TotalAcoesCriticas} acao(oes) critica(s) no periodo.";

        public string PermissoesResumo => TotalPermissoes == 0
            ? "Perfil sem permissoes ativas configuradas."
            : $"{TotalPermissoes} permissao(oes) ativas em {ModulosLiberados.Count} modulo(s); {TotalPermissoesSensiveis} permissao(oes) sensiveis.";

        public string AcoesPermitidasResumo => PermissoesPorAcao.Count == 0
            ? "Nenhuma acao liberada para este perfil."
            : string.Join(", ", PermissoesPorAcao.Take(6).Select(item => $"{item.Acao}: {item.Total}"));

        public string AcoesRecentesResumo => AcoesRecentes.Count == 0
            ? "Sem eventos recentes para este colaborador."
            : string.Join("\n", AcoesRecentes.Select(item => $"{item.DataHora:dd/MM HH:mm} - {item.Categoria}/{item.Acao} ({(item.Sucesso ? "OK" : "Falha")})"));
    }

    public sealed record FuncionarioAuditoriaCategoriaResumo(string Categoria, int Total, int Falhas);

    public sealed record FuncionarioAcaoAuditadaResumo(string Categoria, string Acao, DateTime DataHora, bool Sucesso, string Severidade);

    public sealed record FuncionarioPermissaoAcaoResumo(string Acao, int Total, int Essenciais, int Sensiveis);
}
