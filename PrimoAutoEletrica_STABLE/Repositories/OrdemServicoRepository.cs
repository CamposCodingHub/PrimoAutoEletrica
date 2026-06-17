using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace PrimoAutoEletrica.Repositories
{
    public sealed class OrdemServicoRepository : IOrdemServicoRepository
    {
        private readonly Func<DbConnection> _connectionFactory;
        private readonly LoggerService _logger;

        private const string OrdemColumns = @"
            Id,
            Numero,
            ClienteId,
            VeiculoId,
            TecnicoId,
            AgendamentoId,
            ClienteNomeSnapshot,
            TelefoneClienteSnapshot,
            VeiculoDescricaoSnapshot,
            PlacaSnapshot,
            Status,
            Prioridade,
            Origem,
            ProblemaRelatado,
            Diagnostico,
            DiagnosticoInicial,
            DiagnosticoFinal,
            ObservacoesInternas,
            ObservacoesCliente,
            ChecklistEntrada,
            ChecklistEntrega,
            ChecklistSaida,
            FotosAntes,
            FotosDepois,
            GarantiaObservacoes,
            TermoAutorizacao,
            AssinaturaClienteUrl,
            AprovadaCliente,
            MetodoAprovacao,
            DataAbertura,
            DataPrevisao,
            DataAprovacao,
            DataInicio,
            DataConclusao,
            DataEntrega,
            GarantiaValidaAte,
            TempoPrevistoMinutos,
            TempoRealMinutos,
            OrcamentoId,
            ValorMaoObra,
            Desconto,
            Ativo";

        private const string ItemColumns = @"
            Id,
            OrdemServicoId,
            ProdutoId,
            Tipo,
            Descricao,
            Quantidade,
            ValorUnitario,
            CustoUnitario,
            Observacoes,
            OrdemExibicao,
            EstoqueMovimentado";

        private const string EventoColumns = @"
            Id,
            OrdemServicoId,
            DataEvento,
            Titulo,
            Descricao,
            Tipo,
            Usuario";

        private sealed record EstoqueAuditoriaItem(
            Guid ProdutoId,
            string Codigo,
            string Nome,
            decimal PrecoCompra,
            int EstoqueAnterior,
            int EstoqueNovo,
            int ReservadoAnterior,
            int ReservadoNovo,
            int Quantidade,
            string Acao,
            string Detalhes);

        public OrdemServicoRepository(Func<DbConnection> connectionFactory, LoggerService logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public string GerarProximoNumero()
        {
            using var connection = _connectionFactory();
            connection.Open();

            return GerarProximoNumero(connection, null);
        }

        private string GerarProximoNumero(DbConnection connection, DbTransaction? transaction)
        {
            var policy = SystemConfigurationService.ResolveOrdemServicoNumberingPolicy(connection, DateTime.Now);
            var prefixo = policy.Prefix;

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = IsSqlServerConnection(connection)
                ? @"
                    SELECT TOP (1) Numero
                    FROM OrdensServico
                    WHERE Numero LIKE @Prefixo + '%'
                    ORDER BY Numero DESC;
                "
                : @"
                    SELECT Numero
                    FROM OrdensServico
                    WHERE Numero LIKE @Prefixo || '%'
                    ORDER BY Numero DESC
                    LIMIT 1;
                ";
            command.Parameters.AddWithValue("@Prefixo", prefixo);

            var ultimoNumero = command.ExecuteScalar() as string;
            if (string.IsNullOrWhiteSpace(ultimoNumero))
            {
                return $"{prefixo}{policy.NextNumber:0000}";
            }

            var sufixo = ultimoNumero.Substring(prefixo.Length);
            if (!int.TryParse(sufixo, out var sequencia))
            {
                sequencia = 0;
            }

            return $"{prefixo}{Math.Max(sequencia + 1, policy.NextNumber):0000}";
        }

        public List<OrdemServico> ObterTodos(bool incluirInativas = false)
        {
            using var connection = _connectionFactory();
            connection.Open();
            return ObterOrdensServico(connection, null, null, incluirInativas);
        }

        public List<OrdemServico> ObterPorClienteId(Guid clienteId, bool incluirInativas = false)
        {
            using var connection = _connectionFactory();
            connection.Open();
            return ObterOrdensServico(connection, null, clienteId, incluirInativas);
        }

        public OrdemServico? ObterPorId(Guid id)
        {
            using var connection = _connectionFactory();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT {OrdemColumns}
                FROM OrdensServico
                WHERE Id = @Id
                {LimitOne(connection)};
            ";
            command.Parameters.AddWithValue("@Id", id.ToString());

            OrdemServico ordem;
            using (var reader = command.ExecuteReader())
            {
                if (!reader.Read())
                {
                    return null;
                }

                ordem = MapOrdemServico(reader);
            }

            ordem.Itens = ObterItensDaOrdem(ordem.Id, connection, null);
            ordem.Eventos = ObterEventosDaOrdem(ordem.Id, connection, null);
            return ordem;
        }

        public void Inserir(OrdemServico ordem)
        {
            for (var attempt = 1; attempt <= 4; attempt++)
            {
                try
                {
                    SalvarOrdemServico(ordem, inserir: true);
                    return;
                }
                catch (SqlException ex) when (IsRetryableSqlServerInsertFailure(ex) && attempt < 4)
                {
                    ordem.Numero = string.Empty;
                    _logger.LogWarning($"Tentando salvar OS novamente apos conflito SQL Server ({ex.Number}). Tentativa {attempt + 1}/4.");
                    Thread.Sleep(120 * attempt);
                }
            }
        }

        public void Atualizar(OrdemServico ordem)
        {
            SalvarOrdemServico(ordem, inserir: false);
        }

        public void Excluir(Guid id)
        {
            using var connection = _connectionFactory();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            List<EstoqueAuditoriaItem> auditoriasEstoque;
            string numeroOrdem = id.ToString("N");

            try
            {
                var clienteId = ObterClienteIdDaOrdem(id, connection, transaction);
                var itensMovimentados = ObterItensDaOrdem(id, connection, transaction);
                numeroOrdem = ObterNumeroDaOrdem(id, connection, transaction) ?? numeroOrdem;

                auditoriasEstoque = RestaurarEstoqueDaOrdemExcluida(itensMovimentados, connection, transaction);
                ExcluirFilhosDaOrdem(id, connection, transaction);
                ExcluirOrdem(id, connection, transaction);

                if (clienteId.HasValue)
                {
                    AtualizarResumoCliente(connection, transaction, clienteId.Value);
                }

                transaction.Commit();
                RegistrarAuditoriaEstoque(
                    auditoriasEstoque,
                    id,
                    numeroOrdem,
                    "EstornoOrdemServicoExcluida",
                    $"Numero={numeroOrdem}; OrdemServicoId={id}");
            }
            catch (Exception ex)
            {
                SafeRollback(transaction, "Rollback ao excluir ordem de servico.");
                _logger.LogError($"Falha ao excluir ordem de servico '{id}'.", ex);
                throw;
            }
        }

        private List<OrdemServico> ObterOrdensServico(
            DbConnection connection,
            DbTransaction? transaction,
            Guid? clienteId,
            bool incluirInativas)
        {
            var ordens = new List<OrdemServico>();

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $@"
                SELECT {OrdemColumns}
                FROM OrdensServico
                WHERE (@ClienteId IS NULL OR ClienteId = @ClienteId)
                  AND (@IncluirInativas = 1 OR Ativo = 1)
                ORDER BY DataAbertura DESC, Numero DESC;
            ";
            command.Parameters.AddWithValue("@ClienteId", clienteId?.ToString() ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@IncluirInativas", incluirInativas ? 1 : 0);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    ordens.Add(MapOrdemServico(reader));
                }
            }

            foreach (var ordem in ordens)
            {
                ordem.Itens = ObterItensDaOrdem(ordem.Id, connection, transaction);
                ordem.Eventos = ObterEventosDaOrdem(ordem.Id, connection, transaction);
            }

            return ordens;
        }

        private void SalvarOrdemServico(OrdemServico ordem, bool inserir)
        {
            if (ordem == null)
            {
                throw new ArgumentNullException(nameof(ordem));
            }

            NormalizarOrdemServico(ordem, inserir);

            using var connection = _connectionFactory();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            var auditoriasEstoque = new List<EstoqueAuditoriaItem>();

            try
            {
                if (inserir && IsSqlServerConnection(connection))
                {
                    ReservarNumeroOrdemSqlServer(connection, transaction, ordem);
                }

                using (var command = connection.CreateCommand())
                {
                    command.Transaction = transaction;
                    command.CommandText = inserir ? CriarInsertOrdemSql() : CriarUpdateOrdemSql();
                    AddOrdemParameters(command, connection, ordem);
                    command.ExecuteNonQuery();
                }

                SalvarItensDaOrdem(ordem, connection, transaction);
                SalvarEventosDaOrdem(ordem, connection, transaction);

                if (StatusDeveBaixarEstoque(ordem.Status))
                {
                    auditoriasEstoque = AplicarBaixaEstoqueSeNecessaria(ordem, connection, transaction);
                }

                if (string.Equals(ordem.Status, "Entregue", StringComparison.OrdinalIgnoreCase))
                {
                    FinanceiroDatabaseService.RegistrarReceitaOrdemServicoIntegrada(connection, transaction, ordem, registrarAuditoria: false);
                }

                AtualizarResumoCliente(connection, transaction, ordem.ClienteId);
                transaction.Commit();

                if (auditoriasEstoque.Count > 0)
                {
                    RegistrarAuditoriaEstoque(
                        auditoriasEstoque,
                        ordem.Id,
                        ordem.Numero,
                        "BaixaOrdemServico",
                        $"Numero={ordem.Numero}; OrdemServicoId={ordem.Id}; Origem={ordem.Origem}; AgendamentoId={ordem.AgendamentoId?.ToString() ?? "Nao vinculado"}");
                }

                if (string.Equals(ordem.Status, "Entregue", StringComparison.OrdinalIgnoreCase))
                {
                    global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                        "Financeiro",
                        "OrdemServicoIntegrada",
                        "OrdemServico",
                        ordem.Id.ToString(),
                        $"Numero={ordem.Numero}; Cliente={ordem.ClienteNomeSnapshot}");
                }
            }
            catch (Exception ex)
            {
                SafeRollback(transaction, "Rollback ao salvar ordem de servico.");
                _logger.LogError($"Falha ao salvar ordem de servico '{ordem.Numero}'.", ex);
                throw;
            }
        }

        private static string CriarInsertOrdemSql()
        {
            return @"
                INSERT INTO OrdensServico
                (
                    Id,
                    Numero,
                    ClienteId,
                    VeiculoId,
                    TecnicoId,
                    AgendamentoId,
                    ClienteNomeSnapshot,
                    TelefoneClienteSnapshot,
                    VeiculoDescricaoSnapshot,
                    PlacaSnapshot,
                    Status,
                    Prioridade,
                    Origem,
                    ProblemaRelatado,
                    Diagnostico,
                    DiagnosticoInicial,
                    DiagnosticoFinal,
                    ObservacoesInternas,
                    ObservacoesCliente,
                    ChecklistEntrada,
                    ChecklistEntrega,
                    ChecklistSaida,
                    FotosAntes,
                    FotosDepois,
                    GarantiaObservacoes,
                    TermoAutorizacao,
                    AssinaturaClienteUrl,
                    AprovadaCliente,
                    MetodoAprovacao,
                    DataAbertura,
                    DataPrevisao,
                    DataAprovacao,
                    DataInicio,
                    DataConclusao,
                    DataEntrega,
                    GarantiaValidaAte,
                    TempoPrevistoMinutos,
                    TempoRealMinutos,
                    OrcamentoId,
                    ValorMaoObra,
                    Desconto,
                    Ativo
                )
                VALUES
                (
                    @Id,
                    @Numero,
                    @ClienteId,
                    @VeiculoId,
                    @TecnicoId,
                    @AgendamentoId,
                    @ClienteNomeSnapshot,
                    @TelefoneClienteSnapshot,
                    @VeiculoDescricaoSnapshot,
                    @PlacaSnapshot,
                    @Status,
                    @Prioridade,
                    @Origem,
                    @ProblemaRelatado,
                    @Diagnostico,
                    @DiagnosticoInicial,
                    @DiagnosticoFinal,
                    @ObservacoesInternas,
                    @ObservacoesCliente,
                    @ChecklistEntrada,
                    @ChecklistEntrega,
                    @ChecklistSaida,
                    @FotosAntes,
                    @FotosDepois,
                    @GarantiaObservacoes,
                    @TermoAutorizacao,
                    @AssinaturaClienteUrl,
                    @AprovadaCliente,
                    @MetodoAprovacao,
                    @DataAbertura,
                    @DataPrevisao,
                    @DataAprovacao,
                    @DataInicio,
                    @DataConclusao,
                    @DataEntrega,
                    @GarantiaValidaAte,
                    @TempoPrevistoMinutos,
                    @TempoRealMinutos,
                    @OrcamentoId,
                    @ValorMaoObra,
                    @Desconto,
                    @Ativo
                );";
        }

        private static string CriarUpdateOrdemSql()
        {
            return @"
                UPDATE OrdensServico
                SET
                    Numero = @Numero,
                    ClienteId = @ClienteId,
                    VeiculoId = @VeiculoId,
                    TecnicoId = @TecnicoId,
                    AgendamentoId = @AgendamentoId,
                    ClienteNomeSnapshot = @ClienteNomeSnapshot,
                    TelefoneClienteSnapshot = @TelefoneClienteSnapshot,
                    VeiculoDescricaoSnapshot = @VeiculoDescricaoSnapshot,
                    PlacaSnapshot = @PlacaSnapshot,
                    Status = @Status,
                    Prioridade = @Prioridade,
                    Origem = @Origem,
                    ProblemaRelatado = @ProblemaRelatado,
                    Diagnostico = @Diagnostico,
                    DiagnosticoInicial = @DiagnosticoInicial,
                    DiagnosticoFinal = @DiagnosticoFinal,
                    ObservacoesInternas = @ObservacoesInternas,
                    ObservacoesCliente = @ObservacoesCliente,
                    ChecklistEntrada = @ChecklistEntrada,
                    ChecklistEntrega = @ChecklistEntrega,
                    ChecklistSaida = @ChecklistSaida,
                    FotosAntes = @FotosAntes,
                    FotosDepois = @FotosDepois,
                    GarantiaObservacoes = @GarantiaObservacoes,
                    TermoAutorizacao = @TermoAutorizacao,
                    AssinaturaClienteUrl = @AssinaturaClienteUrl,
                    AprovadaCliente = @AprovadaCliente,
                    MetodoAprovacao = @MetodoAprovacao,
                    DataAbertura = @DataAbertura,
                    DataPrevisao = @DataPrevisao,
                    DataAprovacao = @DataAprovacao,
                    DataInicio = @DataInicio,
                    DataConclusao = @DataConclusao,
                    DataEntrega = @DataEntrega,
                    GarantiaValidaAte = @GarantiaValidaAte,
                    TempoPrevistoMinutos = @TempoPrevistoMinutos,
                    TempoRealMinutos = @TempoRealMinutos,
                    OrcamentoId = @OrcamentoId,
                    ValorMaoObra = @ValorMaoObra,
                    Desconto = @Desconto,
                    Ativo = @Ativo
                WHERE Id = @Id;";
        }

        private void NormalizarOrdemServico(OrdemServico ordem, bool inserir)
        {
            if (ordem.Id == Guid.Empty)
            {
                ordem.Id = Guid.NewGuid();
            }

            if (ordem.ClienteId == Guid.Empty)
            {
                throw new InvalidOperationException("Selecione um cliente para a ordem de servico.");
            }

            ordem.Numero = string.IsNullOrWhiteSpace(ordem.Numero)
                ? GerarProximoNumero()
                : ordem.Numero.Trim();

            ordem.Status = string.IsNullOrWhiteSpace(ordem.Status) ? "Rascunho" : ordem.Status.Trim();
            ordem.Prioridade = string.IsNullOrWhiteSpace(ordem.Prioridade) ? "Normal" : ordem.Prioridade.Trim();
            ordem.Origem = string.IsNullOrWhiteSpace(ordem.Origem) ? "Balcao" : ordem.Origem.Trim();
            ordem.DiagnosticoInicial = ordem.DiagnosticoInicial?.Trim() ?? string.Empty;
            ordem.DiagnosticoFinal = ordem.DiagnosticoFinal?.Trim() ?? string.Empty;
            ordem.Diagnostico = string.IsNullOrWhiteSpace(ordem.Diagnostico)
                ? (string.IsNullOrWhiteSpace(ordem.DiagnosticoFinal) ? ordem.DiagnosticoInicial : ordem.DiagnosticoFinal)
                : ordem.Diagnostico.Trim();
            ordem.ChecklistEntrada = ordem.ChecklistEntrada?.Trim() ?? string.Empty;
            ordem.ChecklistEntrega = ordem.ChecklistEntrega?.Trim() ?? string.Empty;
            ordem.ChecklistSaida = string.IsNullOrWhiteSpace(ordem.ChecklistSaida)
                ? ordem.ChecklistEntrega
                : ordem.ChecklistSaida.Trim();
            ordem.ChecklistEntrega = string.IsNullOrWhiteSpace(ordem.ChecklistEntrega)
                ? ordem.ChecklistSaida
                : ordem.ChecklistEntrega;
            ordem.FotosAntes = ordem.FotosAntes?.Trim() ?? string.Empty;
            ordem.FotosDepois = ordem.FotosDepois?.Trim() ?? string.Empty;
            ordem.GarantiaObservacoes = ordem.GarantiaObservacoes?.Trim() ?? string.Empty;
            ordem.TermoAutorizacao = ordem.TermoAutorizacao?.Trim() ?? string.Empty;
            ordem.AssinaturaClienteUrl = ordem.AssinaturaClienteUrl?.Trim() ?? string.Empty;
            ordem.TempoPrevistoMinutos = Math.Max(0, ordem.TempoPrevistoMinutos);
            ordem.TempoRealMinutos = Math.Max(0, ordem.TempoRealMinutos);
            ordem.DataAbertura = ordem.DataAbertura == default ? DateTime.Now : ordem.DataAbertura;

            ordem.Eventos ??= new List<OrdemServicoEvento>();
            ordem.Itens ??= new List<OrdemServicoItem>();

            foreach (var item in ordem.Itens)
            {
                if (item.Id == Guid.Empty)
                {
                    item.Id = Guid.NewGuid();
                }

                item.OrdemServicoId = ordem.Id;
                item.Tipo = string.IsNullOrWhiteSpace(item.Tipo) ? "Servico" : item.Tipo.Trim();
                item.Descricao = item.Descricao?.Trim() ?? string.Empty;
                item.Observacoes = item.Observacoes?.Trim() ?? string.Empty;
                ComercialValidationHelper.GarantirTextoObrigatorio(item.Descricao, "a descricao do item da ordem de servico");
                ComercialValidationHelper.GarantirQuantidadePositiva(item.Quantidade, $"a quantidade do item '{item.Descricao}'");
                ComercialValidationHelper.GarantirValorMaiorOuIgualZero(item.ValorUnitario, $"o valor unitario do item '{item.Descricao}'");
                ComercialValidationHelper.GarantirValorMaiorOuIgualZero(item.CustoUnitario, $"o custo do item '{item.Descricao}'");
            }

            var subtotalItens = ordem.Itens.Sum(i => i.Total);
            ordem.ValorMaoObra = ordem.Itens
                .Where(i => string.Equals(i.Tipo, "Servico", StringComparison.OrdinalIgnoreCase))
                .Sum(i => i.Total);
            ComercialValidationHelper.GarantirDescontoValido(ordem.Desconto, subtotalItens, "O desconto da ordem de servico");
            ComercialValidationHelper.GarantirDataFinalNaoAnterior(ordem.DataAbertura, ordem.DataPrevisao, "a data de abertura", "A data de previsao");

            if (ordem.AprovadaCliente && !ordem.DataAprovacao.HasValue)
            {
                ordem.DataAprovacao = DateTime.Now;
            }

            if (StatusImplicaInicio(ordem.Status) && !ordem.DataInicio.HasValue)
            {
                ordem.DataInicio = DateTime.Now;
            }

            if (StatusImplicaConclusao(ordem.Status) && !ordem.DataConclusao.HasValue)
            {
                ordem.DataConclusao = DateTime.Now;
            }

            if (string.Equals(ordem.Status, "Entregue", StringComparison.OrdinalIgnoreCase) && !ordem.DataEntrega.HasValue)
            {
                ordem.DataEntrega = DateTime.Now;
            }

            ComercialValidationHelper.GarantirDataFinalNaoAnterior(ordem.DataInicio, ordem.DataConclusao, "a data de inicio", "A data de conclusao");
            ComercialValidationHelper.GarantirDataFinalNaoAnterior(ordem.DataConclusao ?? ordem.DataInicio, ordem.DataEntrega, "a data operacional anterior", "A data de entrega");
            ComercialValidationHelper.GarantirDataFinalNaoAnterior(ordem.DataEntrega ?? ordem.DataAbertura, ordem.GarantiaValidaAte, "a data de entrega", "A validade da garantia");

            if (inserir && !ordem.Eventos.Any())
            {
                ordem.Eventos.Add(new OrdemServicoEvento
                {
                    OrdemServicoId = ordem.Id,
                    DataEvento = DateTime.Now,
                    Titulo = "OS criada",
                    Descricao = "Ordem de servico registrada no sistema.",
                    Tipo = "Cadastro",
                    Usuario = "Sistema"
                });
            }
        }

        private void ReservarNumeroOrdemSqlServer(DbConnection connection, DbTransaction transaction, OrdemServico ordem)
        {
            using (var lockCommand = connection.CreateCommand())
            {
                lockCommand.Transaction = transaction;
                lockCommand.CommandText = @"
                    DECLARE @Result int;
                    EXEC @Result = sp_getapplock
                        @Resource = N'PrimoAutoEletrica:OrdemServico:Numero',
                        @LockMode = N'Exclusive',
                        @LockOwner = N'Transaction',
                        @LockTimeout = 15000;
                    SELECT @Result;";

                var result = Convert.ToInt32(lockCommand.ExecuteScalar(), CultureInfo.InvariantCulture);
                if (result < 0)
                {
                    throw new TimeoutException($"Nao foi possivel reservar numeracao de OS no SQL Server. Codigo sp_getapplock={result}.");
                }
            }

            ordem.Numero = GerarProximoNumero(connection, transaction);
        }

        private static void AddOrdemParameters(DbCommand command, DbConnection connection, OrdemServico ordem)
        {
            AddGuidParameter(command, connection, "@Id", ordem.Id);
            command.Parameters.AddWithValue("@Numero", ordem.Numero);
            AddGuidParameter(command, connection, "@ClienteId", ordem.ClienteId);
            AddNullableGuidParameter(command, connection, "@VeiculoId", ordem.VeiculoId);
            command.Parameters.AddWithValue("@TecnicoId", ordem.TecnicoId ?? (object)DBNull.Value);
            AddNullableGuidParameter(command, connection, "@AgendamentoId", ordem.AgendamentoId);
            command.Parameters.AddWithValue("@ClienteNomeSnapshot", ToDbNullableString(ordem.ClienteNomeSnapshot));
            command.Parameters.AddWithValue("@TelefoneClienteSnapshot", ToDbNullableString(ordem.TelefoneClienteSnapshot));
            command.Parameters.AddWithValue("@VeiculoDescricaoSnapshot", ToDbNullableString(ordem.VeiculoDescricaoSnapshot));
            command.Parameters.AddWithValue("@PlacaSnapshot", ToDbNullableString(ordem.PlacaSnapshot));
            command.Parameters.AddWithValue("@Status", ordem.Status);
            command.Parameters.AddWithValue("@Prioridade", ordem.Prioridade);
            command.Parameters.AddWithValue("@Origem", ToDbNullableString(ordem.Origem));
            command.Parameters.AddWithValue("@ProblemaRelatado", ToDbNullableString(ordem.ProblemaRelatado));
            command.Parameters.AddWithValue("@Diagnostico", ToDbNullableString(ordem.Diagnostico));
            command.Parameters.AddWithValue("@DiagnosticoInicial", ToDbNullableString(ordem.DiagnosticoInicial));
            command.Parameters.AddWithValue("@DiagnosticoFinal", ToDbNullableString(ordem.DiagnosticoFinal));
            command.Parameters.AddWithValue("@ObservacoesInternas", ToDbNullableString(ordem.ObservacoesInternas));
            command.Parameters.AddWithValue("@ObservacoesCliente", ToDbNullableString(ordem.ObservacoesCliente));
            command.Parameters.AddWithValue("@ChecklistEntrada", ToDbNullableString(ordem.ChecklistEntrada));
            command.Parameters.AddWithValue("@ChecklistEntrega", ToDbNullableString(ordem.ChecklistEntrega));
            command.Parameters.AddWithValue("@ChecklistSaida", ToDbNullableString(ordem.ChecklistSaida));
            command.Parameters.AddWithValue("@FotosAntes", ToDbNullableString(ordem.FotosAntes));
            command.Parameters.AddWithValue("@FotosDepois", ToDbNullableString(ordem.FotosDepois));
            command.Parameters.AddWithValue("@GarantiaObservacoes", ToDbNullableString(ordem.GarantiaObservacoes));
            command.Parameters.AddWithValue("@TermoAutorizacao", ToDbNullableString(ordem.TermoAutorizacao));
            command.Parameters.AddWithValue("@AssinaturaClienteUrl", ToDbNullableString(ordem.AssinaturaClienteUrl));
            command.Parameters.AddWithValue("@AprovadaCliente", ordem.AprovadaCliente ? 1 : 0);
            command.Parameters.AddWithValue("@MetodoAprovacao", ToDbNullableString(ordem.MetodoAprovacao));
            command.Parameters.AddWithValue("@DataAbertura", ordem.DataAbertura);
            command.Parameters.AddWithValue("@DataPrevisao", ToDbNullableDate(ordem.DataPrevisao, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@DataAprovacao", ToDbNullableDate(ordem.DataAprovacao, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@DataInicio", ToDbNullableDate(ordem.DataInicio, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@DataConclusao", ToDbNullableDate(ordem.DataConclusao, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@DataEntrega", ToDbNullableDate(ordem.DataEntrega, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@GarantiaValidaAte", ToDbNullableDate(ordem.GarantiaValidaAte, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@TempoPrevistoMinutos", ordem.TempoPrevistoMinutos);
            command.Parameters.AddWithValue("@TempoRealMinutos", ordem.TempoRealMinutos);
            AddNullableGuidParameter(command, connection, "@OrcamentoId", ordem.OrcamentoId);
            command.Parameters.AddWithValue("@ValorMaoObra", ordem.ValorMaoObra);
            command.Parameters.AddWithValue("@Desconto", ordem.Desconto);
            command.Parameters.AddWithValue("@Ativo", ordem.Ativo ? 1 : 0);
        }

        private static void AddItemParameters(DbCommand command, Guid ordemId, OrdemServicoItem item)
        {
            command.Parameters.AddWithValue("@Id", item.Id.ToString());
            command.Parameters.AddWithValue("@OrdemServicoId", ordemId.ToString());
            command.Parameters.AddWithValue("@ProdutoId", item.ProdutoId?.ToString() ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Tipo", item.Tipo);
            command.Parameters.AddWithValue("@Descricao", item.Descricao);
            command.Parameters.AddWithValue("@Quantidade", item.Quantidade);
            command.Parameters.AddWithValue("@ValorUnitario", item.ValorUnitario);
            command.Parameters.AddWithValue("@CustoUnitario", item.CustoUnitario);
            command.Parameters.AddWithValue("@Observacoes", ToDbNullableString(item.Observacoes));
            command.Parameters.AddWithValue("@OrdemExibicao", item.OrdemExibicao);
            command.Parameters.AddWithValue("@EstoqueMovimentado", item.EstoqueMovimentado ? 1 : 0);
        }

        private void SalvarItensDaOrdem(
            OrdemServico ordem,
            DbConnection connection,
            DbTransaction transaction)
        {
            var estoquePorItemId = new Dictionary<Guid, bool>();

            using (var selectCommand = connection.CreateCommand())
            {
                selectCommand.Transaction = transaction;
                selectCommand.CommandText = @"
                    SELECT Id, EstoqueMovimentado
                    FROM OrdemServicoItens
                    WHERE OrdemServicoId = @OrdemServicoId;
                ";
                selectCommand.Parameters.AddWithValue("@OrdemServicoId", ordem.Id.ToString());

                using var reader = selectCommand.ExecuteReader();
                while (reader.Read())
                {
                    estoquePorItemId[ReadGuid(reader, 0)] = ReadBool(reader, 1);
                }
            }

            var itemIds = ordem.Itens.Select(i => i.Id.ToString()).ToList();

            using (var deleteCommand = connection.CreateCommand())
            {
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = itemIds.Count == 0
                    ? "DELETE FROM OrdemServicoItens WHERE OrdemServicoId = @OrdemServicoId;"
                    : $@"
                        DELETE FROM OrdemServicoItens
                        WHERE OrdemServicoId = @OrdemServicoId
                          AND Id NOT IN ({string.Join(", ", itemIds.Select((_, index) => $"@ItemId{index}"))});
                    ";
                deleteCommand.Parameters.AddWithValue("@OrdemServicoId", ordem.Id.ToString());

                for (var index = 0; index < itemIds.Count; index++)
                {
                    deleteCommand.Parameters.AddWithValue($"@ItemId{index}", itemIds[index]);
                }

                deleteCommand.ExecuteNonQuery();
            }

            for (var index = 0; index < ordem.Itens.Count; index++)
            {
                var item = ordem.Itens[index];
                item.OrdemExibicao = index + 1;

                if (estoquePorItemId.TryGetValue(item.Id, out var estoqueMovimentado))
                {
                    item.EstoqueMovimentado = item.EstoqueMovimentado || estoqueMovimentado;
                }

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    UPDATE OrdemServicoItens
                    SET
                        OrdemServicoId = @OrdemServicoId,
                        ProdutoId = @ProdutoId,
                        Tipo = @Tipo,
                        Descricao = @Descricao,
                        Quantidade = @Quantidade,
                        ValorUnitario = @ValorUnitario,
                        CustoUnitario = @CustoUnitario,
                        Observacoes = @Observacoes,
                        OrdemExibicao = @OrdemExibicao,
                        EstoqueMovimentado = @EstoqueMovimentado
                    WHERE Id = @Id;
                ";

                AddItemParameters(command, ordem.Id, item);
                if (command.ExecuteNonQuery() > 0)
                {
                    continue;
                }

                command.Parameters.Clear();
                command.CommandText = @"
                    INSERT INTO OrdemServicoItens
                    (
                        Id,
                        OrdemServicoId,
                        ProdutoId,
                        Tipo,
                        Descricao,
                        Quantidade,
                        ValorUnitario,
                        CustoUnitario,
                        Observacoes,
                        OrdemExibicao,
                        EstoqueMovimentado
                    )
                    VALUES
                    (
                        @Id,
                        @OrdemServicoId,
                        @ProdutoId,
                        @Tipo,
                        @Descricao,
                        @Quantidade,
                        @ValorUnitario,
                        @CustoUnitario,
                        @Observacoes,
                        @OrdemExibicao,
                        @EstoqueMovimentado
                    );
                ";
                AddItemParameters(command, ordem.Id, item);
                command.ExecuteNonQuery();
            }
        }

        private static void SalvarEventosDaOrdem(
            OrdemServico ordem,
            DbConnection connection,
            DbTransaction transaction)
        {
            using (var deleteCommand = connection.CreateCommand())
            {
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = "DELETE FROM OrdemServicoEventos WHERE OrdemServicoId = @OrdemServicoId;";
                deleteCommand.Parameters.AddWithValue("@OrdemServicoId", ordem.Id.ToString());
                deleteCommand.ExecuteNonQuery();
            }

            foreach (var evento in ordem.Eventos.OrderBy(e => e.DataEvento))
            {
                if (evento.Id == Guid.Empty)
                {
                    evento.Id = Guid.NewGuid();
                }

                evento.OrdemServicoId = ordem.Id;

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    INSERT INTO OrdemServicoEventos
                    (
                        Id,
                        OrdemServicoId,
                        DataEvento,
                        Titulo,
                        Descricao,
                        Tipo,
                        Usuario
                    )
                    VALUES
                    (
                        @Id,
                        @OrdemServicoId,
                        @DataEvento,
                        @Titulo,
                        @Descricao,
                        @Tipo,
                        @Usuario
                    );
                ";

                command.Parameters.AddWithValue("@Id", evento.Id.ToString());
                command.Parameters.AddWithValue("@OrdemServicoId", ordem.Id.ToString());
                command.Parameters.AddWithValue("@DataEvento", evento.DataEvento);
                command.Parameters.AddWithValue("@Titulo", evento.Titulo);
                command.Parameters.AddWithValue("@Descricao", ToDbNullableString(evento.Descricao));
                command.Parameters.AddWithValue("@Tipo", ToDbNullableString(evento.Tipo));
                command.Parameters.AddWithValue("@Usuario", ToDbNullableString(evento.Usuario));
                command.ExecuteNonQuery();
            }
        }

        private List<EstoqueAuditoriaItem> AplicarBaixaEstoqueSeNecessaria(
            OrdemServico ordem,
            DbConnection connection,
            DbTransaction transaction)
        {
            var itensPendentes = ordem.Itens
                .Where(i =>
                    !i.EstoqueMovimentado &&
                    i.ProdutoId.HasValue &&
                    string.Equals(i.Tipo, "Peca", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var auditorias = new List<EstoqueAuditoriaItem>();

            foreach (var item in itensPendentes)
            {
                var quantidade = ValidarQuantidadeInteira(item);
                var (
                    codigoProduto,
                    nomeProduto,
                    precoCompra,
                    estoqueAtual,
                    reservadoNoAgendamento,
                    reservasAtivas) = ObterEstoqueAtual(connection, transaction, ordem.AgendamentoId, item);
                var reservasExternas = Math.Max(0, reservasAtivas - reservadoNoAgendamento);
                var disponibilidadeOperacional = estoqueAtual - reservasExternas;

                if (disponibilidadeOperacional < quantidade)
                {
                    throw new InvalidOperationException(
                        $"Estoque insuficiente para '{nomeProduto}'. Disponivel operacional: {disponibilidadeOperacional}. Necessario: {quantidade}.");
                }

                using (var updateCommand = connection.CreateCommand())
                {
                    updateCommand.Transaction = transaction;
                    updateCommand.CommandText = @"
                        UPDATE Produtos
                        SET
                            QuantidadeEstoque = QuantidadeEstoque - @Quantidade,
                            ValorTotalEstoque = (QuantidadeEstoque - @Quantidade) * PrecoCompra,
                            DataUltimaAtualizacao = @DataAtualizacao
                        WHERE Id = @ProdutoId;
                    ";
                    updateCommand.Parameters.AddWithValue("@Quantidade", quantidade);
                    updateCommand.Parameters.AddWithValue("@DataAtualizacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                    updateCommand.Parameters.AddWithValue("@ProdutoId", item.ProdutoId!.Value.ToString());
                    updateCommand.ExecuteNonQuery();
                }

                if (ordem.AgendamentoId.HasValue && reservadoNoAgendamento > 0)
                {
                    AtualizarReservaDoAgendamento(connection, transaction, ordem.AgendamentoId.Value, item.ProdutoId!.Value, reservar: false);
                }

                item.EstoqueMovimentado = true;

                using var itemCommand = connection.CreateCommand();
                itemCommand.Transaction = transaction;
                itemCommand.CommandText = @"
                    UPDATE OrdemServicoItens
                    SET EstoqueMovimentado = 1
                    WHERE Id = @Id;
                ";
                itemCommand.Parameters.AddWithValue("@Id", item.Id.ToString());
                itemCommand.ExecuteNonQuery();

                auditorias.Add(new EstoqueAuditoriaItem(
                    item.ProdutoId!.Value,
                    codigoProduto,
                    nomeProduto,
                    precoCompra,
                    estoqueAtual,
                    estoqueAtual - quantidade,
                    reservasAtivas,
                    ordem.AgendamentoId.HasValue && reservadoNoAgendamento > 0 ? reservasExternas : reservasAtivas,
                    quantidade,
                    "BaixaOrdemServico",
                    $"NumeroOS={ordem.Numero}; Item={item.Descricao}; Quantidade={quantidade}; AgendamentoId={ordem.AgendamentoId?.ToString() ?? "Nao vinculado"}"));
            }

            return auditorias;
        }

        private static List<EstoqueAuditoriaItem> RestaurarEstoqueDaOrdemExcluida(
            IEnumerable<OrdemServicoItem> itens,
            DbConnection connection,
            DbTransaction transaction)
        {
            var auditorias = new List<EstoqueAuditoriaItem>();

            foreach (var item in itens.Where(i =>
                         i.EstoqueMovimentado &&
                         i.ProdutoId.HasValue &&
                         string.Equals(i.Tipo, "Peca", StringComparison.OrdinalIgnoreCase)))
            {
                var quantidade = ValidarQuantidadeInteira(item);
                var (codigoProduto, nomeProduto, precoCompra, estoqueAtual, reservasAtivas) = ObterEstoqueResumo(connection, transaction, item.ProdutoId!.Value, item.Descricao);

                using var updateCommand = connection.CreateCommand();
                updateCommand.Transaction = transaction;
                updateCommand.CommandText = @"
                    UPDATE Produtos
                    SET
                        QuantidadeEstoque = QuantidadeEstoque + @Quantidade,
                        ValorTotalEstoque = (QuantidadeEstoque + @Quantidade) * PrecoCompra,
                        DataUltimaAtualizacao = @DataAtualizacao
                    WHERE Id = @ProdutoId;
                ";
                updateCommand.Parameters.AddWithValue("@Quantidade", quantidade);
                updateCommand.Parameters.AddWithValue("@DataAtualizacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                updateCommand.Parameters.AddWithValue("@ProdutoId", item.ProdutoId!.Value.ToString());
                updateCommand.ExecuteNonQuery();

                auditorias.Add(new EstoqueAuditoriaItem(
                    item.ProdutoId!.Value,
                    codigoProduto,
                    nomeProduto,
                    precoCompra,
                    estoqueAtual,
                    estoqueAtual + quantidade,
                    reservasAtivas,
                    reservasAtivas,
                    quantidade,
                    "EstornoOrdemServicoExcluida",
                    $"Item={item.Descricao}; Quantidade={quantidade}"));
            }

            return auditorias;
        }

        private static void AtualizarResumoCliente(
            DbConnection connection,
            DbTransaction? transaction,
            Guid clienteId)
        {
            decimal totalGasto = 0;
            var totalServicos = 0;
            DateTime? ultimaVisita = null;

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = IsSqlServerConnection(connection)
                    ? @"
                        SELECT
                            COALESCE(SUM(Resumo.ValorItens - COALESCE(Resumo.Desconto, 0)), 0) AS TotalGasto,
                            COUNT(*) AS TotalServicos,
                            MAX(Resumo.UltimaVisita) AS UltimaVisita
                        FROM
                        (
                            SELECT
                                os.Id,
                                os.Desconto,
                                COALESCE(os.DataEntrega, os.DataConclusao, os.DataAbertura) AS UltimaVisita,
                                COALESCE(SUM(itens.Quantidade * itens.ValorUnitario), 0) AS ValorItens
                            FROM OrdensServico os
                            LEFT JOIN OrdemServicoItens itens ON itens.OrdemServicoId = os.Id
                            WHERE os.ClienteId = @ClienteId
                              AND os.Ativo = 1
                              AND (os.Status = 'Entregue' OR os.DataEntrega IS NOT NULL)
                            GROUP BY os.Id, os.Desconto, os.DataEntrega, os.DataConclusao, os.DataAbertura
                        ) AS Resumo;"
                    : @"
                        SELECT
                            COALESCE(SUM(
                                COALESCE(
                                    (
                                        SELECT SUM(Quantidade * ValorUnitario)
                                        FROM OrdemServicoItens itens
                                        WHERE itens.OrdemServicoId = os.Id
                                    ),
                                    0
                                ) - COALESCE(os.Desconto, 0)
                            ), 0) AS TotalGasto,
                            COUNT(*) AS TotalServicos,
                            MAX(COALESCE(os.DataEntrega, os.DataConclusao, os.DataAbertura)) AS UltimaVisita
                        FROM OrdensServico os
                        WHERE os.ClienteId = @ClienteId
                          AND os.Ativo = 1
                          AND (os.Status = 'Entregue' OR os.DataEntrega IS NOT NULL);";
                command.Parameters.AddWithValue("@ClienteId", clienteId.ToString());

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    totalGasto = ReadDecimal(reader, 0);
                    totalServicos = ReadInt32(reader, 1);
                    ultimaVisita = ReadDateTime(reader, 2);
                }
            }

            using var updateCommand = connection.CreateCommand();
            updateCommand.Transaction = transaction;
            updateCommand.CommandText = @"
                UPDATE Clientes
                SET
                    TotalGasto = @TotalGasto,
                    TotalServicos = @TotalServicos,
                    PontosFidelidade = @PontosFidelidade,
                    UltimaVisita = @UltimaVisita
                WHERE Id = @Id;
            ";
            updateCommand.Parameters.AddWithValue("@TotalGasto", totalGasto);
            updateCommand.Parameters.AddWithValue("@TotalServicos", totalServicos);
            updateCommand.Parameters.AddWithValue("@PontosFidelidade", Math.Max(0, (int)Math.Floor(totalGasto / 10m)));
            updateCommand.Parameters.AddWithValue("@UltimaVisita", ToDbNullableDate(ultimaVisita, "yyyy-MM-dd HH:mm:ss"));
            updateCommand.Parameters.AddWithValue("@Id", clienteId.ToString());
            updateCommand.ExecuteNonQuery();
        }

        private Guid? ObterClienteIdDaOrdem(
            Guid id,
            DbConnection connection,
            DbTransaction transaction)
        {
            using var selectCommand = connection.CreateCommand();
            selectCommand.Transaction = transaction;
            selectCommand.CommandText = $"SELECT ClienteId FROM OrdensServico WHERE Id = @Id {LimitOne(connection)};";
            selectCommand.Parameters.AddWithValue("@Id", id.ToString());

            var value = selectCommand.ExecuteScalar();
            if (value is null || value == DBNull.Value)
            {
                return null;
            }

            return value is Guid guid ? guid : Guid.Parse(Convert.ToString(value) ?? string.Empty);
        }

        private static string? ObterNumeroDaOrdem(
            Guid id,
            DbConnection connection,
            DbTransaction transaction)
        {
            using var selectCommand = connection.CreateCommand();
            selectCommand.Transaction = transaction;
            selectCommand.CommandText = $"SELECT Numero FROM OrdensServico WHERE Id = @Id {LimitOne(connection)};";
            selectCommand.Parameters.AddWithValue("@Id", id.ToString());

            return Convert.ToString(selectCommand.ExecuteScalar());
        }

        private static void ExcluirFilhosDaOrdem(
            Guid id,
            DbConnection connection,
            DbTransaction transaction)
        {
            using (var eventosCommand = connection.CreateCommand())
            {
                eventosCommand.Transaction = transaction;
                eventosCommand.CommandText = "DELETE FROM OrdemServicoEventos WHERE OrdemServicoId = @Id;";
                eventosCommand.Parameters.AddWithValue("@Id", id.ToString());
                eventosCommand.ExecuteNonQuery();
            }

            using (var itensCommand = connection.CreateCommand())
            {
                itensCommand.Transaction = transaction;
                itensCommand.CommandText = "DELETE FROM OrdemServicoItens WHERE OrdemServicoId = @Id;";
                itensCommand.Parameters.AddWithValue("@Id", id.ToString());
                itensCommand.ExecuteNonQuery();
            }
        }

        private static void ExcluirOrdem(
            Guid id,
            DbConnection connection,
            DbTransaction transaction)
        {
            using var ordemCommand = connection.CreateCommand();
            ordemCommand.Transaction = transaction;
            ordemCommand.CommandText = "DELETE FROM OrdensServico WHERE Id = @Id;";
            ordemCommand.Parameters.AddWithValue("@Id", id.ToString());
            ordemCommand.ExecuteNonQuery();
        }

        private static OrdemServico MapOrdemServico(DbDataReader reader)
        {
            var ordem = new OrdemServico
            {
                Id = ReadGuid(reader, 0),
                Numero = ReadString(reader, 1),
                ClienteId = ReadGuid(reader, 2),
                VeiculoId = ReadNullableGuid(reader, 3),
                TecnicoId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                AgendamentoId = ReadNullableGuid(reader, 5),
                ClienteNomeSnapshot = ReadString(reader, 6),
                TelefoneClienteSnapshot = ReadString(reader, 7),
                VeiculoDescricaoSnapshot = ReadString(reader, 8),
                PlacaSnapshot = ReadString(reader, 9),
                Status = ReadString(reader, 10),
                Prioridade = ReadString(reader, 11),
                Origem = ReadString(reader, 12),
                ProblemaRelatado = ReadString(reader, 13),
                Diagnostico = ReadString(reader, 14),
                DiagnosticoInicial = ReadString(reader, 15),
                DiagnosticoFinal = ReadString(reader, 16),
                ObservacoesInternas = ReadString(reader, 17),
                ObservacoesCliente = ReadString(reader, 18),
                ChecklistEntrada = ReadString(reader, 19),
                ChecklistEntrega = ReadString(reader, 20),
                ChecklistSaida = ReadString(reader, 21),
                FotosAntes = ReadString(reader, 22),
                FotosDepois = ReadString(reader, 23),
                GarantiaObservacoes = ReadString(reader, 24),
                TermoAutorizacao = ReadString(reader, 25),
                AssinaturaClienteUrl = ReadString(reader, 26),
                AprovadaCliente = ReadBool(reader, 27),
                MetodoAprovacao = ReadString(reader, 28),
                DataAbertura = ReadDateTime(reader, 29) ?? DateTime.Now,
                DataPrevisao = ReadDateTime(reader, 30),
                DataAprovacao = ReadDateTime(reader, 31),
                DataInicio = ReadDateTime(reader, 32),
                DataConclusao = ReadDateTime(reader, 33),
                DataEntrega = ReadDateTime(reader, 34),
                GarantiaValidaAte = ReadDateTime(reader, 35),
                TempoPrevistoMinutos = ReadInt32(reader, 36),
                TempoRealMinutos = ReadInt32(reader, 37),
                OrcamentoId = ReadNullableGuid(reader, 38),
                ValorMaoObra = ReadDecimal(reader, 39),
                Desconto = ReadDecimal(reader, 40),
                Ativo = ReadBool(reader, 41)
            };

            if (string.IsNullOrWhiteSpace(ordem.DiagnosticoFinal))
            {
                ordem.DiagnosticoFinal = ordem.Diagnostico;
            }

            if (string.IsNullOrWhiteSpace(ordem.ChecklistSaida))
            {
                ordem.ChecklistSaida = ordem.ChecklistEntrega;
            }

            return ordem;
        }

        private static List<OrdemServicoItem> ObterItensDaOrdem(
            Guid ordemId,
            DbConnection connection,
            DbTransaction? transaction)
        {
            var itens = new List<OrdemServicoItem>();

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $@"
                SELECT {ItemColumns}
                FROM OrdemServicoItens
                WHERE OrdemServicoId = @OrdemServicoId
                ORDER BY OrdemExibicao, Descricao;
            ";
            command.Parameters.AddWithValue("@OrdemServicoId", ordemId.ToString());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                itens.Add(new OrdemServicoItem
                {
                    Id = ReadGuid(reader, 0),
                    OrdemServicoId = ReadGuid(reader, 1),
                    ProdutoId = ReadNullableGuid(reader, 2),
                    Tipo = ReadString(reader, 3),
                    Descricao = ReadString(reader, 4),
                    Quantidade = ReadDecimal(reader, 5),
                    ValorUnitario = ReadDecimal(reader, 6),
                    CustoUnitario = ReadDecimal(reader, 7),
                    Observacoes = ReadString(reader, 8),
                    OrdemExibicao = ReadInt32(reader, 9),
                    EstoqueMovimentado = ReadBool(reader, 10)
                });
            }

            return itens;
        }

        private static List<OrdemServicoEvento> ObterEventosDaOrdem(
            Guid ordemId,
            DbConnection connection,
            DbTransaction? transaction)
        {
            var eventos = new List<OrdemServicoEvento>();

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $@"
                SELECT {EventoColumns}
                FROM OrdemServicoEventos
                WHERE OrdemServicoId = @OrdemServicoId
                ORDER BY DataEvento DESC;
            ";
            command.Parameters.AddWithValue("@OrdemServicoId", ordemId.ToString());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                eventos.Add(new OrdemServicoEvento
                {
                    Id = ReadGuid(reader, 0),
                    OrdemServicoId = ReadGuid(reader, 1),
                    DataEvento = ReadDateTime(reader, 2) ?? DateTime.Now,
                    Titulo = ReadString(reader, 3),
                    Descricao = ReadString(reader, 4),
                    Tipo = ReadString(reader, 5),
                    Usuario = ReadString(reader, 6)
                });
            }

            return eventos;
        }

        private static bool StatusImplicaInicio(string status)
        {
            return string.Equals(status, "Aprovada", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Em diagnostico", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Em execucao", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Aguardando peca", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Finalizada", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Aguardando pagamento", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Pronta para entrega", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Entregue", StringComparison.OrdinalIgnoreCase);
        }

        private static bool StatusImplicaConclusao(string status)
        {
            return string.Equals(status, "Pronta para entrega", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Finalizada", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Aguardando pagamento", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Entregue", StringComparison.OrdinalIgnoreCase);
        }

        private static bool StatusDeveBaixarEstoque(string status)
        {
            return string.Equals(status, "Finalizada", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Aguardando pagamento", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Entregue", StringComparison.OrdinalIgnoreCase);
        }

        private static int ValidarQuantidadeInteira(OrdemServicoItem item)
        {
            if (item.Quantidade <= 0)
            {
                throw new InvalidOperationException($"O item '{item.Descricao}' precisa ter quantidade maior que zero.");
            }

            if (decimal.Truncate(item.Quantidade) != item.Quantidade)
            {
                throw new InvalidOperationException(
                    $"O item '{item.Descricao}' usa produto de estoque e precisa de quantidade inteira.");
            }

            return decimal.ToInt32(item.Quantidade);
        }

        private static void AtualizarReservaDoAgendamento(
            DbConnection connection,
            DbTransaction transaction,
            Guid agendamentoId,
            Guid produtoId,
            bool reservar)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                UPDATE AgendamentoProdutos
                SET
                    Reservado = @Reservado,
                    DataReserva = @DataReserva
                WHERE AgendamentoId = @AgendamentoId
                  AND ProdutoId = @ProdutoId;";
            command.Parameters.AddWithValue("@Reservado", reservar ? 1 : 0);
            command.Parameters.AddWithValue("@DataReserva", reservar
                ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                : (object)DBNull.Value);
            command.Parameters.AddWithValue("@AgendamentoId", agendamentoId.ToString());
            command.Parameters.AddWithValue("@ProdutoId", produtoId.ToString());
            command.ExecuteNonQuery();
        }

        private static (string Codigo, string Nome, decimal PrecoCompra, int EstoqueAtual, int ReservadoNoAgendamento, int ReservasAtivas) ObterEstoqueAtual(
            DbConnection connection,
            DbTransaction transaction,
            Guid? agendamentoId,
            OrdemServicoItem item)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT
                    COALESCE(Nome, ''),
                    COALESCE(QuantidadeEstoque, 0),
                    COALESCE(Codigo, ''),
                    COALESCE(PrecoCompra, 0),
                    (
                        SELECT COALESCE(SUM(ap.Quantidade), 0)
                        FROM AgendamentoProdutos ap
                        WHERE @AgendamentoId IS NOT NULL
                          AND ap.AgendamentoId = @AgendamentoId
                          AND ap.ProdutoId = @ProdutoId
                          AND ap.Reservado = 1
                    ) AS ReservadoNoAgendamento,
                    (
                        SELECT COALESCE(SUM(ap.Quantidade), 0)
                        FROM AgendamentoProdutos ap
                        INNER JOIN Agendamentos a ON a.Id = ap.AgendamentoId
                        WHERE ap.ProdutoId = @ProdutoId
                          AND ap.Reservado = 1
                          AND ap.Quantidade > 0
                          AND COALESCE(a.Status, '') NOT IN ('Cancelado', 'Finalizado')
                    ) AS ReservasAtivas
                FROM Produtos
                WHERE Id = @ProdutoId
                " + LimitOne(connection) + ";";
            command.Parameters.AddWithValue("@AgendamentoId", agendamentoId.HasValue
                ? agendamentoId.Value.ToString()
                : (object)DBNull.Value);
            command.Parameters.AddWithValue("@ProdutoId", item.ProdutoId!.Value.ToString());

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                throw new InvalidOperationException($"O produto vinculado ao item '{item.Descricao}' nao foi encontrado.");
            }

            return (
                ReadString(reader, 2),
                ReadString(reader, 0),
                ReadDecimal(reader, 3),
                ReadInt32(reader, 1),
                ReadInt32(reader, 4),
                ReadInt32(reader, 5));
        }

        private static (string Codigo, string Nome, decimal PrecoCompra, int EstoqueAtual, int ReservasAtivas) ObterEstoqueResumo(
            DbConnection connection,
            DbTransaction transaction,
            Guid produtoId,
            string descricaoFallback)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT
                    COALESCE(Nome, ''),
                    COALESCE(QuantidadeEstoque, 0),
                    COALESCE(Codigo, ''),
                    COALESCE(PrecoCompra, 0),
                    (
                        SELECT COALESCE(SUM(ap.Quantidade), 0)
                        FROM AgendamentoProdutos ap
                        INNER JOIN Agendamentos a ON a.Id = ap.AgendamentoId
                        WHERE ap.ProdutoId = @ProdutoId
                          AND ap.Reservado = 1
                          AND ap.Quantidade > 0
                          AND COALESCE(a.Status, '') NOT IN ('Cancelado', 'Finalizado')
                    ) AS ReservasAtivas
                FROM Produtos
                WHERE Id = @ProdutoId
                " + LimitOne(connection) + ";";
            command.Parameters.AddWithValue("@ProdutoId", produtoId.ToString());

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                throw new InvalidOperationException($"O produto vinculado ao item '{descricaoFallback}' nao foi encontrado.");
            }

            var nome = ReadString(reader, 0);
            return (
                ReadString(reader, 2),
                string.IsNullOrWhiteSpace(nome) ? descricaoFallback : nome,
                ReadDecimal(reader, 3),
                ReadInt32(reader, 1),
                ReadInt32(reader, 4));
        }

        private static void RegistrarAuditoriaEstoque(
            IEnumerable<EstoqueAuditoriaItem> auditorias,
            Guid ordemId,
            string numeroOrdem,
            string acaoPadrao,
            string detalhesBase)
        {
            foreach (var auditoria in auditorias)
            {
                global::PrimoAutoEletrica.App.Audit.Registrar(
                    categoria: "Estoque",
                    acao: string.IsNullOrWhiteSpace(auditoria.Acao) ? acaoPadrao : auditoria.Acao,
                    entidade: "Produto",
                    entidadeId: auditoria.ProdutoId.ToString(),
                    detalhes: $"{detalhesBase}; Produto={auditoria.Nome}; Codigo={auditoria.Codigo}; Quantidade={auditoria.Quantidade}; {auditoria.Detalhes}",
                    valorAnterior: EstoqueOperationalService.CriarSnapshot(auditoria.Codigo, auditoria.Nome, auditoria.PrecoCompra, auditoria.EstoqueAnterior, auditoria.ReservadoAnterior),
                    valorNovo: EstoqueOperationalService.CriarSnapshot(auditoria.Codigo, auditoria.Nome, auditoria.PrecoCompra, auditoria.EstoqueNovo, auditoria.ReservadoNovo),
                    correlationId: ordemId.ToString("N"));
            }

            global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                "Estoque",
                acaoPadrao,
                "OrdemServico",
                ordemId.ToString(),
                $"Numero={numeroOrdem}; Movimentacoes={auditorias.Count()}");
        }

        private void SafeRollback(DbTransaction transaction, string context)
        {
            try
            {
                transaction.Rollback();
            }
            catch (Exception rollbackException)
            {
                _logger.LogError(context, rollbackException);
            }
        }

        private static string LimitOne(DbConnection connection)
        {
            return IsSqlServerConnection(connection) ? string.Empty : "LIMIT 1";
        }

        private static void AddGuidParameter(DbCommand command, DbConnection connection, string name, Guid value)
        {
            command.Parameters.AddWithValue(name, IsSqlServerConnection(connection) ? value : value.ToString());
        }

        private static void AddNullableGuidParameter(DbCommand command, DbConnection connection, string name, Guid? value)
        {
            command.Parameters.AddWithValue(name, value.HasValue
                ? (IsSqlServerConnection(connection) ? value.Value : value.Value.ToString())
                : DBNull.Value);
        }

        private static bool IsRetryableSqlServerInsertFailure(SqlException exception)
        {
            foreach (SqlError error in exception.Errors)
            {
                if (error.Number is 1205 or 2601 or 2627)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSqlServerConnection(DbConnection connection)
        {
            return connection.GetType().FullName?.Contains("SqlClient", StringComparison.OrdinalIgnoreCase) == true;
        }

        private static Guid ReadGuid(DbDataReader reader, int index)
        {
            var value = reader.GetValue(index);
            return value is Guid guid ? guid : Guid.Parse(Convert.ToString(value) ?? string.Empty);
        }

        private static Guid? ReadNullableGuid(DbDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
            {
                return null;
            }

            var raw = reader.GetValue(index);
            if (raw is Guid guid)
            {
                return guid;
            }

            var value = Convert.ToString(raw);
            return string.IsNullOrWhiteSpace(value) ? null : Guid.Parse(value);
        }

        private static string ReadString(DbDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? string.Empty : Convert.ToString(reader.GetValue(index)) ?? string.Empty;
        }

        private static bool ReadBool(DbDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
            {
                return false;
            }

            var value = reader.GetValue(index);
            return value is bool boolean ? boolean : Convert.ToInt32(value, CultureInfo.InvariantCulture) != 0;
        }

        private static int ReadInt32(DbDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
            {
                return 0;
            }

            return Convert.ToInt32(reader.GetValue(index), CultureInfo.InvariantCulture);
        }

        private static decimal ReadDecimal(DbDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
            {
                return 0m;
            }

            return Convert.ToDecimal(reader.GetValue(index), CultureInfo.InvariantCulture);
        }

        private static DateTime? ReadDateTime(DbDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
            {
                return null;
            }

            var value = reader.GetValue(index);
            if (value is DateTime dateTime)
            {
                return dateTime;
            }

            return DateTime.TryParse(Convert.ToString(value), CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
                ? result
                : null;
        }

        private static object ToDbNullableString(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
        }

        private static object ToDbNullableDate(DateTime? value, string format)
        {
            return value.HasValue
                ? value.Value
                : DBNull.Value;
        }
    }
}
