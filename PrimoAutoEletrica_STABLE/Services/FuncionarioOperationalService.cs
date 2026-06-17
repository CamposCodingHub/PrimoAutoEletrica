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
            var produtividade = ObterResumoProdutividade(funcionario, inicio, fim);

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
                permissoes.PermissoesPorAcao,
                produtividade.TotalLogins,
                produtividade.UltimoLogin,
                produtividade.OrdensExecutadas,
                produtividade.OrdensFinalizadas,
                produtividade.ValorOrdensExecutadas,
                produtividade.VendasRealizadas,
                produtividade.ValorVendasRealizadas,
                produtividade.CaixasOperados,
                produtividade.MovimentacoesCaixa,
                produtividade.ValorCaixaOperado,
                produtividade.UltimaOperacaoCaixa);
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
            return new UsuarioAuditFilter(funcionario.Id, funcionario.Nome, funcionario.Email);
        }

        private static int ObterTotalAuditoria(DbConnection connection, UsuarioAuditFilter filtroUsuario, DateTime inicio, DateTime fim, bool apenasFalhas)
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

        private static int ObterTotalAuditoriaCritica(DbConnection connection, UsuarioAuditFilter filtroUsuario, DateTime inicio, DateTime fim)
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

        private static DateTime? ObterUltimaAcao(DbConnection connection, UsuarioAuditFilter filtroUsuario, DateTime inicio, DateTime fim)
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

        private static List<FuncionarioAuditoriaCategoriaResumo> ObterAcoesPorCategoria(DbConnection connection, UsuarioAuditFilter filtroUsuario, DateTime inicio, DateTime fim)
        {
            var itens = new List<FuncionarioAuditoriaCategoriaResumo>();
            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT {TopClause(connection, 6)} Categoria, COUNT(*), SUM(CASE WHEN Sucesso = 0 THEN 1 ELSE 0 END)
                FROM AuditLogs
                WHERE DataHora BETWEEN @Inicio AND @Fim
                  AND ({filtroUsuario.Sql})
                GROUP BY Categoria
                ORDER BY COUNT(*) DESC, Categoria
                {LimitClause(connection, 6)};";
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

        private static List<FuncionarioAcaoAuditadaResumo> ObterAcoesRecentes(DbConnection connection, UsuarioAuditFilter filtroUsuario, DateTime inicio, DateTime fim)
        {
            var itens = new List<FuncionarioAcaoAuditadaResumo>();
            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT {TopClause(connection, 5)} Categoria, Acao, DataHora, Sucesso, Severidade
                FROM AuditLogs
                WHERE DataHora BETWEEN @Inicio AND @Fim
                  AND ({filtroUsuario.Sql})
                ORDER BY DataHora DESC
                {LimitClause(connection, 5)};";
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

        private static List<string> ObterModulosLiberados(DbConnection connection, string perfilAcesso)
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

        private FuncionarioProdutividadeOperacionalResumo ObterResumoProdutividade(Funcionario funcionario, DateTime inicio, DateTime fim)
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();

            var resumo = new FuncionarioProdutividadeOperacionalResumo
            {
                UltimoLogin = funcionario.DataUltimoLogin
            };

            if (TabelaExiste(connection, "AuditLogs"))
            {
                var login = ObterResumoLogin(connection, funcionario, inicio, fim);
                resumo = resumo with
                {
                    TotalLogins = login.TotalLogins,
                    UltimoLogin = login.UltimoLogin ?? funcionario.DataUltimoLogin
                };
            }

            if (TabelaExiste(connection, "OrdensServico"))
            {
                var os = ObterResumoOrdensServico(connection, funcionario, inicio, fim);
                resumo = resumo with
                {
                    OrdensExecutadas = os.OrdensExecutadas,
                    OrdensFinalizadas = os.OrdensFinalizadas,
                    ValorOrdensExecutadas = os.ValorOrdensExecutadas
                };
            }

            if (TabelaExiste(connection, "Vendas"))
            {
                var vendas = ObterResumoVendas(connection, funcionario, inicio, fim);
                resumo = resumo with
                {
                    VendasRealizadas = vendas.VendasRealizadas,
                    ValorVendasRealizadas = vendas.ValorVendasRealizadas
                };
            }

            if (TabelaExiste(connection, "CaixaSessoes"))
            {
                var caixa = ObterResumoCaixa(connection, funcionario, inicio, fim);
                resumo = resumo with
                {
                    CaixasOperados = caixa.CaixasOperados,
                    ValorCaixaOperado = caixa.ValorCaixaOperado,
                    UltimaOperacaoCaixa = caixa.UltimaOperacaoCaixa
                };
            }

            if (TabelaExiste(connection, "MovimentacoesCaixa"))
            {
                resumo = resumo with
                {
                    MovimentacoesCaixa = ObterTotalMovimentacoesCaixa(connection, funcionario, inicio, fim)
                };
            }

            return resumo;
        }

        private static FuncionarioProdutividadeOperacionalResumo ObterResumoLogin(
            DbConnection connection,
            Funcionario funcionario,
            DateTime inicio,
            DateTime fim)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*), MAX(DataHora)
                FROM AuditLogs
                WHERE DataHora BETWEEN @Inicio AND @Fim
                  AND Categoria = 'Seguranca'
                  AND Acao LIKE '%Login%'
                  AND (
                        lower(COALESCE(UsuarioNome, '')) = lower(@Nome)
                     OR lower(COALESCE(UsuarioNome, '')) = lower(@Email)
                     OR (@Email <> '' AND lower(COALESCE(Detalhes, '')) LIKE lower(@EmailLike))
                  );";
            PreencherPeriodoFuncionario(command, funcionario, inicio, fim);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return new FuncionarioProdutividadeOperacionalResumo();
            }

            return new FuncionarioProdutividadeOperacionalResumo
            {
                TotalLogins = ReadInt(reader, 0),
                UltimoLogin = ReadNullableDate(reader, 1)
            };
        }

        private static FuncionarioProdutividadeOperacionalResumo ObterResumoOrdensServico(
            DbConnection connection,
            Funcionario funcionario,
            DateTime inicio,
            DateTime fim)
        {
            var possuiItens = TabelaExiste(connection, "OrdemServicoItens");

            if (IsSqlServerConnection(connection) && possuiItens)
            {
                using var sqlServerCommand = connection.CreateCommand();
                sqlServerCommand.CommandText = @"
                    SELECT
                        COUNT(*),
                        SUM(CASE
                            WHEN lower(COALESCE(os.Status, '')) IN ('finalizada', 'aguardando pagamento', 'entregue')
                                 OR os.DataConclusao IS NOT NULL
                            THEN 1 ELSE 0 END),
                        COALESCE(SUM(COALESCE(os.ValorMaoObra, 0) + COALESCE(itens.TotalItens, 0) - COALESCE(os.Desconto, 0)), 0)
                    FROM OrdensServico os
                    LEFT JOIN (
                        SELECT OrdemServicoId,
                               SUM(COALESCE(Quantidade, 0) * COALESCE(ValorUnitario, 0)) AS TotalItens
                        FROM OrdemServicoItens
                        GROUP BY OrdemServicoId
                    ) itens ON itens.OrdemServicoId = os.Id
                    WHERE os.TecnicoId = @FuncionarioId
                      AND COALESCE(os.DataConclusao, os.DataEntrega, os.DataInicio, os.DataAbertura) BETWEEN @Inicio AND @Fim;";
                PreencherPeriodoFuncionario(sqlServerCommand, funcionario, inicio, fim);

                using var sqlServerReader = sqlServerCommand.ExecuteReader();
                if (!sqlServerReader.Read())
                {
                    return new FuncionarioProdutividadeOperacionalResumo();
                }

                return new FuncionarioProdutividadeOperacionalResumo
                {
                    OrdensExecutadas = ReadInt(sqlServerReader, 0),
                    OrdensFinalizadas = ReadInt(sqlServerReader, 1),
                    ValorOrdensExecutadas = ReadDecimal(sqlServerReader, 2)
                };
            }

            var totalExpression = possuiItens
                ? @"COALESCE(os.ValorMaoObra, 0) +
                    COALESCE((
                        SELECT SUM(COALESCE(i.Quantidade, 0) * COALESCE(i.ValorUnitario, 0))
                        FROM OrdemServicoItens i
                        WHERE i.OrdemServicoId = os.Id
                    ), 0) -
                    COALESCE(os.Desconto, 0)"
                : "COALESCE(os.ValorMaoObra, 0) - COALESCE(os.Desconto, 0)";

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT
                    COUNT(*),
                    SUM(CASE
                        WHEN lower(COALESCE(Status, '')) IN ('finalizada', 'aguardando pagamento', 'entregue')
                             OR DataConclusao IS NOT NULL
                        THEN 1 ELSE 0 END),
                    COALESCE(SUM({totalExpression}), 0)
                FROM OrdensServico os
                WHERE TecnicoId = @FuncionarioId
                  AND COALESCE(DataConclusao, DataEntrega, DataInicio, DataAbertura) BETWEEN @Inicio AND @Fim;";
            PreencherPeriodoFuncionario(command, funcionario, inicio, fim);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return new FuncionarioProdutividadeOperacionalResumo();
            }

            return new FuncionarioProdutividadeOperacionalResumo
            {
                OrdensExecutadas = ReadInt(reader, 0),
                OrdensFinalizadas = ReadInt(reader, 1),
                ValorOrdensExecutadas = ReadDecimal(reader, 2)
            };
        }

        private static FuncionarioProdutividadeOperacionalResumo ObterResumoVendas(
            DbConnection connection,
            Funcionario funcionario,
            DateTime inicio,
            DateTime fim)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*), COALESCE(SUM(Total), 0)
                FROM Vendas
                WHERE Data BETWEEN @Inicio AND @Fim
                  AND COALESCE(Status, 'Concluida') <> 'Cancelada'
                  AND (
                        lower(COALESCE(Usuario, '')) = lower(@Nome)
                     OR lower(COALESCE(Usuario, '')) = lower(@Email)
                     OR (@Nome <> '' AND lower(COALESCE(Usuario, '')) LIKE lower(@NomeLike))
                  );";
            PreencherPeriodoFuncionario(command, funcionario, inicio, fim);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return new FuncionarioProdutividadeOperacionalResumo();
            }

            return new FuncionarioProdutividadeOperacionalResumo
            {
                VendasRealizadas = ReadInt(reader, 0),
                ValorVendasRealizadas = ReadDecimal(reader, 1)
            };
        }

        private static FuncionarioProdutividadeOperacionalResumo ObterResumoCaixa(
            DbConnection connection,
            Funcionario funcionario,
            DateTime inicio,
            DateTime fim)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*), COALESCE(SUM(TotalVendas), 0), MAX(COALESCE(DataUltimaMovimentacao, DataFechamento, DataAbertura))
                FROM CaixaSessoes
                WHERE DataAbertura BETWEEN @Inicio AND @Fim
                  AND (
                        OperadorId = @FuncionarioId
                     OR lower(COALESCE(OperadorNome, '')) = lower(@Nome)
                     OR lower(COALESCE(OperadorNome, '')) = lower(@Email)
                  );";
            PreencherPeriodoFuncionario(command, funcionario, inicio, fim);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return new FuncionarioProdutividadeOperacionalResumo();
            }

            return new FuncionarioProdutividadeOperacionalResumo
            {
                CaixasOperados = ReadInt(reader, 0),
                ValorCaixaOperado = ReadDecimal(reader, 1),
                UltimaOperacaoCaixa = ReadNullableDate(reader, 2)
            };
        }

        private static int ObterTotalMovimentacoesCaixa(
            DbConnection connection,
            Funcionario funcionario,
            DateTime inicio,
            DateTime fim)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*)
                FROM MovimentacoesCaixa
                WHERE Data BETWEEN @Inicio AND @Fim
                  AND (
                        lower(COALESCE(Operador, '')) = lower(@Nome)
                     OR lower(COALESCE(Operador, '')) = lower(@Email)
                     OR (@Nome <> '' AND lower(COALESCE(Operador, '')) LIKE lower(@NomeLike))
                  );";
            PreencherPeriodoFuncionario(command, funcionario, inicio, fim);
            return Convert.ToInt32(command.ExecuteScalar() ?? 0);
        }

        private static void PreencherPeriodoFuncionario(DbCommand command, Funcionario funcionario, DateTime inicio, DateTime fim)
        {
            command.Parameters.AddWithValue("@FuncionarioId", funcionario.Id);
            command.Parameters.AddWithValue("@Nome", funcionario.Nome ?? string.Empty);
            command.Parameters.AddWithValue("@Email", funcionario.Email ?? string.Empty);
            command.Parameters.AddWithValue("@NomeLike", CriarFiltroLike(funcionario.Nome));
            command.Parameters.AddWithValue("@EmailLike", CriarFiltroLike(funcionario.Email));
            command.Parameters.AddWithValue("@Inicio", inicio.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("@Fim", fim.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
        }

        private static bool TabelaExiste(DbConnection connection, string tabela)
        {
            using var command = connection.CreateCommand();
            command.CommandText = IsSqlServerConnection(connection)
                ? @"
                    SELECT TOP (1) name
                    FROM sys.tables
                    WHERE name = @Tabela;"
                : @"
                    SELECT name
                    FROM sqlite_master
                    WHERE type = 'table'
                      AND name = @Tabela
                    LIMIT 1;";
            command.Parameters.AddWithValue("@Tabela", tabela);
            return command.ExecuteScalar() != null;
        }

        private static string CriarFiltroLike(string? valor)
        {
            return $"%{valor?.Trim() ?? string.Empty}%";
        }

        private static bool IsSqlServerConnection(DbConnection connection)
        {
            return connection.GetType().FullName?.Contains("SqlClient", StringComparison.OrdinalIgnoreCase) == true;
        }

        private static string TopClause(DbConnection connection, int quantidade)
        {
            return IsSqlServerConnection(connection) ? $"TOP ({quantidade})" : string.Empty;
        }

        private static string LimitClause(DbConnection connection, int quantidade)
        {
            return IsSqlServerConnection(connection) ? string.Empty : $"LIMIT {quantidade}";
        }

        private static string ReadString(DbDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? string.Empty : Convert.ToString(reader.GetValue(index)) ?? string.Empty;
        }

        private static int ReadInt(DbDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0 : Convert.ToInt32(reader.GetValue(index));
        }

        private static DateTime ReadDate(DbDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
            {
                return DateTime.MinValue;
            }

            var rawValue = reader.GetValue(index);
            if (rawValue is DateTime dateTime)
            {
                return dateTime;
            }

            return DateTime.TryParse(Convert.ToString(rawValue), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var value)
                ? value
                : DateTime.MinValue;
        }

        private static DateTime? ReadNullableDate(DbDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
            {
                return null;
            }

            var rawValue = reader.GetValue(index);
            if (rawValue is DateTime dateTime)
            {
                return dateTime;
            }

            return DateTime.TryParse(Convert.ToString(rawValue), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var value)
                ? value
                : null;
        }

        private static decimal ReadDecimal(DbDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0m : Convert.ToDecimal(reader.GetValue(index));
        }

        private sealed record UsuarioAuditFilter(int Id, string Nome, string Email)
        {
            public string Sql => @"
                UsuarioId = @UsuarioId
                OR lower(COALESCE(UsuarioNome, '')) = lower(@UsuarioNome)
                OR (
                    @UsuarioEmail <> ''
                    AND (
                           lower(COALESCE(UsuarioNome, '')) = lower(@UsuarioEmail)
                        OR lower(COALESCE(Detalhes, '')) LIKE lower(@UsuarioEmailLike)
                    )
                )";

            public void Preencher(DbCommand command)
            {
                command.Parameters.AddWithValue("@UsuarioId", Id);
                command.Parameters.AddWithValue("@UsuarioNome", Nome ?? string.Empty);
                command.Parameters.AddWithValue("@UsuarioEmail", Email ?? string.Empty);
                command.Parameters.AddWithValue("@UsuarioEmailLike", CriarFiltroLike(Email));
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

        private sealed record FuncionarioProdutividadeOperacionalResumo
        {
            public int TotalLogins { get; init; }
            public DateTime? UltimoLogin { get; init; }
            public int OrdensExecutadas { get; init; }
            public int OrdensFinalizadas { get; init; }
            public decimal ValorOrdensExecutadas { get; init; }
            public int VendasRealizadas { get; init; }
            public decimal ValorVendasRealizadas { get; init; }
            public int CaixasOperados { get; init; }
            public int MovimentacoesCaixa { get; init; }
            public decimal ValorCaixaOperado { get; init; }
            public DateTime? UltimaOperacaoCaixa { get; init; }
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
        IReadOnlyList<FuncionarioPermissaoAcaoResumo> PermissoesPorAcao,
        int TotalLogins,
        DateTime? UltimoLogin,
        int OrdensExecutadas,
        int OrdensFinalizadas,
        decimal ValorOrdensExecutadas,
        int VendasRealizadas,
        decimal ValorVendasRealizadas,
        int CaixasOperados,
        int MovimentacoesCaixa,
        decimal ValorCaixaOperado,
        DateTime? UltimaOperacaoCaixa)
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
                Array.Empty<FuncionarioPermissaoAcaoResumo>(),
                0,
                null,
                0,
                0,
                0m,
                0,
                0m,
                0,
                0,
                0m,
                null);
        }

        public string ProdutividadeResumo => TotalAcoesAuditadas == 0
            ? "Nenhuma acao auditada nos ultimos 30 dias."
            : $"{TotalAcoesAuditadas} acao(oes) auditada(s) nos ultimos 30 dias; categoria mais ativa: {CategoriaMaisAtiva}.";

        public string ProdutividadeOperacionalResumo =>
            $"{OrdensExecutadas} OS executada(s), {OrdensFinalizadas} finalizada(s), R$ {ValorOrdensExecutadas:F2} em OS; " +
            $"{VendasRealizadas} venda(s), R$ {ValorVendasRealizadas:F2} em vendas.";

        public string CaixaOperadoResumo => CaixasOperados == 0 && MovimentacoesCaixa == 0
            ? "Sem caixa operado no periodo."
            : $"{CaixasOperados} sessao(oes) de caixa, {MovimentacoesCaixa} movimento(s), R$ {ValorCaixaOperado:F2} em vendas no caixa.";

        public string LoginResumo => TotalLogins == 0
            ? $"Login: sem eventos no periodo; ultimo acesso cadastrado: {(UltimoLogin.HasValue ? UltimoLogin.Value.ToString("dd/MM/yyyy HH:mm") : "sem registro")}."
            : $"Login: {TotalLogins} evento(s) no periodo; ultimo em {(UltimoLogin.HasValue ? UltimoLogin.Value.ToString("dd/MM/yyyy HH:mm") : "sem registro")}.";

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
