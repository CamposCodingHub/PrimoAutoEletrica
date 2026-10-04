using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using PRIMOX.Application.Interfaces;
using PRIMOX.Domain.Entities;
using PRIMOX.Domain.Enums;
using PRIMOX.Domain.ValueObjects;

namespace PRIMOX.Infrastructure.Persistence
{
    /// <summary>
    /// Repositório SQLite para o Agregado OrdemServico.
    /// Utiliza o mesmo esquema de banco de dados do PRIMOX (tabelas OrdensServico, OrdemServicoItens, OrdemServicoEventos)
    /// sem quebrar compatibilidade ou duplicar tabelas legadas.
    /// </summary>
    public class SqliteOrdemServicoRepository : IOrdemServicoRepository
    {
        private readonly string _connectionString;

        public SqliteOrdemServicoRepository(string connectionString)
        {
            _connectionString = string.IsNullOrWhiteSpace(connectionString)
                ? "Data Source=primoauto.db"
                : connectionString;

            EnsureTablesCreated();
        }

        public void EnsureTablesCreated()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS OrdensServico (
                    Id TEXT PRIMARY KEY,
                    Numero TEXT NOT NULL UNIQUE,
                    ClienteId TEXT NOT NULL,
                    VeiculoId TEXT NOT NULL,
                    ClienteNomeSnapshot TEXT,
                    PlacaSnapshot TEXT,
                    VeiculoDescricaoSnapshot TEXT,
                    Status TEXT NOT NULL,
                    Prioridade TEXT NOT NULL,
                    ProblemaRelatado TEXT,
                    Diagnostico TEXT,
                    ObservacoesInternas TEXT,
                    DataAbertura TEXT NOT NULL,
                    DataPrevisao TEXT,
                    DataConclusao TEXT,
                    Desconto REAL NOT NULL DEFAULT 0,
                    TenantId TEXT,
                    FilialId TEXT,
                    SessaoDiagnosticoId TEXT,
                    Ativo INTEGER NOT NULL DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS OrdemServicoItens (
                    Id TEXT PRIMARY KEY,
                    OrdemServicoId TEXT NOT NULL,
                    ProdutoId TEXT,
                    Tipo TEXT NOT NULL,
                    Descricao TEXT NOT NULL,
                    Quantidade REAL NOT NULL DEFAULT 1,
                    ValorUnitario REAL NOT NULL DEFAULT 0,
                    CustoUnitario REAL NOT NULL DEFAULT 0,
                    Observacoes TEXT,
                    OrdemExibicao INTEGER NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS OrdemServicoEventos (
                    Id TEXT PRIMARY KEY,
                    OrdemServicoId TEXT NOT NULL,
                    DataEvento TEXT NOT NULL,
                    Titulo TEXT NOT NULL,
                    Descricao TEXT,
                    Tipo TEXT,
                    Usuario TEXT
                );
            ";
            cmd.ExecuteNonQuery();
        }

        public async Task<string> GerarProximoNumeroAsync(CancellationToken cancellationToken = default)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var ano = DateTime.UtcNow.Year;
            var prefixo = $"OS-{ano}-";

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT Numero 
                FROM OrdensServico 
                WHERE Numero LIKE @prefixo || '%' 
                ORDER BY Numero DESC 
                LIMIT 1;
            ";
            cmd.Parameters.AddWithValue("@prefixo", prefixo);

            var result = await cmd.ExecuteScalarAsync(cancellationToken) as string;

            var proximoSeq = 1;
            if (!string.IsNullOrWhiteSpace(result) && result.Length > prefixo.Length)
            {
                var sufixo = result.Substring(prefixo.Length);
                if (int.TryParse(sufixo, out var seq))
                {
                    proximoSeq = seq + 1;
                }
            }

            return $"{prefixo}{proximoSeq:0000}";
        }

        public async Task<OrdemServico?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT 
                    Id, Numero, ClienteId, VeiculoId, ClienteNomeSnapshot, PlacaSnapshot,
                    VeiculoDescricaoSnapshot, Status, Prioridade, ProblemaRelatado,
                    Diagnostico, ObservacoesInternas, DataAbertura, DataPrevisao,
                    DataConclusao, Desconto, TenantId, FilialId, SessaoDiagnosticoId
                FROM OrdensServico
                WHERE Id = @id AND Ativo = 1;
            ";
            cmd.Parameters.AddWithValue("@id", id.ToString());

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            var numero = reader.GetString(1);
            var clienteId = Guid.Parse(reader.GetString(2));
            var veiculoId = Guid.Parse(reader.GetString(3));
            var clienteNome = reader.IsDBNull(4) ? "" : reader.GetString(4);
            var placa = reader.IsDBNull(5) ? "" : reader.GetString(5);
            var modelo = reader.IsDBNull(6) ? "" : reader.GetString(6);

            var statusStr = reader.GetString(7);
            var status = Enum.TryParse<StatusOrdemServico>(statusStr, true, out var s) ? s : StatusOrdemServico.Aberta;

            var prioridadeStr = reader.GetString(8);
            var prioridade = Enum.TryParse<PrioridadeOS>(prioridadeStr, true, out var p) ? p : PrioridadeOS.Normal;

            var queixa = reader.IsDBNull(9) ? "" : reader.GetString(9);
            var diagnostico = reader.IsDBNull(10) ? "" : reader.GetString(10);
            var obsInternas = reader.IsDBNull(11) ? "" : reader.GetString(11);

            var dataAbertura = DateTimeOffset.Parse(reader.GetString(12), CultureInfo.InvariantCulture);
            DateTimeOffset? dataPrevisao = reader.IsDBNull(13) ? null : DateTimeOffset.Parse(reader.GetString(13), CultureInfo.InvariantCulture);
            DateTimeOffset? dataConclusao = reader.IsDBNull(14) ? null : DateTimeOffset.Parse(reader.GetString(14), CultureInfo.InvariantCulture);

            var descontoVal = Convert.ToDecimal(reader.GetDouble(15));
            var desconto = Money.FromBRL(descontoVal);

            var tenantId = reader.IsDBNull(16) ? Guid.Empty : (Guid.TryParse(reader.GetString(16), out var t) ? t : Guid.Empty);
            var filialId = reader.IsDBNull(17) ? Guid.Empty : (Guid.TryParse(reader.GetString(17), out var f) ? f : Guid.Empty);
            Guid? sessaoDiagId = reader.IsDBNull(18) ? null : (Guid.TryParse(reader.GetString(18), out var sd) ? sd : null);

            await reader.CloseAsync();

            // Ler itens
            var itensPeca = new List<ItemPecaOS>();
            var itensServico = new List<ItemServicoOS>();

            await using var cmdItens = connection.CreateCommand();
            cmdItens.CommandText = @"
                SELECT Id, ProdutoId, Tipo, Descricao, Quantidade, ValorUnitario, CustoUnitario, Observacoes
                FROM OrdemServicoItens
                WHERE OrdemServicoId = @osId;
            ";
            cmdItens.Parameters.AddWithValue("@osId", id.ToString());

            await using var readerItens = await cmdItens.ExecuteReaderAsync(cancellationToken);
            while (await readerItens.ReadAsync(cancellationToken))
            {
                var itemId = Guid.Parse(readerItens.GetString(0));
                Guid? prodId = readerItens.IsDBNull(1) ? null : Guid.Parse(readerItens.GetString(1));
                var tipo = readerItens.GetString(2);
                var desc = readerItens.GetString(3);
                var qtd = Convert.ToDecimal(readerItens.GetDouble(4));
                var vlUnit = Convert.ToDecimal(readerItens.GetDouble(5));
                var custoUnit = Convert.ToDecimal(readerItens.GetDouble(6));
                var obs = readerItens.IsDBNull(7) ? "" : readerItens.GetString(7);

                if (string.Equals(tipo, "Peca", StringComparison.OrdinalIgnoreCase))
                {
                    itensPeca.Add(new ItemPecaOS(
                        itemId,
                        id,
                        desc,
                        qtd,
                        Money.FromBRL(vlUnit),
                        codigo: obs,
                        custoUnitario: Money.FromBRL(custoUnit),
                        produtoId: prodId));
                }
                else
                {
                    itensServico.Add(new ItemServicoOS(
                        itemId,
                        id,
                        desc,
                        qtd,
                        Money.FromBRL(vlUnit),
                        tecnicoResponsavel: obs,
                        servicoId: prodId));
                }
            }
            await readerItens.CloseAsync();

            // Ler histórico de eventos
            var historico = new List<HistoricoStatusOS>();
            await using var cmdEventos = connection.CreateCommand();
            cmdEventos.CommandText = @"
                SELECT Id, DataEvento, Titulo, Descricao, Tipo, Usuario
                FROM OrdemServicoEventos
                WHERE OrdemServicoId = @osId
                ORDER BY DataEvento ASC;
            ";
            cmdEventos.Parameters.AddWithValue("@osId", id.ToString());

            await using var readerEventos = await cmdEventos.ExecuteReaderAsync(cancellationToken);
            while (await readerEventos.ReadAsync(cancellationToken))
            {
                var evId = Guid.Parse(readerEventos.GetString(0));
                var dataEv = DateTimeOffset.Parse(readerEventos.GetString(1), CultureInfo.InvariantCulture);
                var motivo = readerEventos.IsDBNull(3) ? "" : readerEventos.GetString(3);
                var tipoStatus = readerEventos.IsDBNull(4) ? "Aberta" : readerEventos.GetString(4);
                var usuario = readerEventos.IsDBNull(5) ? "Sistema" : readerEventos.GetString(5);

                var statusEv = Enum.TryParse<StatusOrdemServico>(tipoStatus, true, out var se)
                    ? se
                    : StatusOrdemServico.Aberta;

                historico.Add(new HistoricoStatusOS(
                    evId,
                    id,
                    statusEv,
                    statusEv,
                    motivo,
                    usuario,
                    dataEv));
            }

            return OrdemServico.Reconstituir(
                id,
                numero,
                clienteId,
                veiculoId,
                clienteNome,
                placa,
                modelo,
                status,
                prioridade,
                queixa,
                diagnostico,
                obsInternas,
                dataAbertura,
                dataPrevisao,
                dataConclusao,
                desconto,
                sessaoDiagId,
                tenantId,
                filialId,
                itensPeca,
                itensServico,
                historico);
        }

        public async Task AdicionarAsync(OrdemServico os, CancellationToken cancellationToken = default)
        {
            if (os == null) throw new ArgumentNullException(nameof(os));

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                await using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction as SqliteTransaction;
                cmd.CommandText = @"
                    INSERT INTO OrdensServico (
                        Id, Numero, ClienteId, VeiculoId, ClienteNomeSnapshot, PlacaSnapshot,
                        VeiculoDescricaoSnapshot, Status, Prioridade, ProblemaRelatado,
                        Diagnostico, ObservacoesInternas, DataAbertura, DataPrevisao,
                        DataConclusao, Desconto, TenantId, FilialId, SessaoDiagnosticoId, Ativo
                    ) VALUES (
                        @Id, @Numero, @ClienteId, @VeiculoId, @ClienteNomeSnapshot, @PlacaSnapshot,
                        @VeiculoDescricaoSnapshot, @Status, @Prioridade, @ProblemaRelatado,
                        @Diagnostico, @ObservacoesInternas, @DataAbertura, @DataPrevisao,
                        @DataConclusao, @Desconto, @TenantId, @FilialId, @SessaoDiagnosticoId, 1
                    );
                ";

                cmd.Parameters.AddWithValue("@Id", os.Id.ToString());
                cmd.Parameters.AddWithValue("@Numero", os.Numero);
                cmd.Parameters.AddWithValue("@ClienteId", os.ClienteId.ToString());
                cmd.Parameters.AddWithValue("@VeiculoId", os.VeiculoId.ToString());
                cmd.Parameters.AddWithValue("@ClienteNomeSnapshot", os.ClienteNomeSnapshot);
                cmd.Parameters.AddWithValue("@PlacaSnapshot", os.VeiculoPlacaSnapshot);
                cmd.Parameters.AddWithValue("@VeiculoDescricaoSnapshot", os.VeiculoModeloSnapshot);
                cmd.Parameters.AddWithValue("@Status", os.Status.ToString());
                cmd.Parameters.AddWithValue("@Prioridade", os.Prioridade.ToString());
                cmd.Parameters.AddWithValue("@ProblemaRelatado", os.QueixaCliente);
                cmd.Parameters.AddWithValue("@Diagnostico", os.DiagnosticoTecnico);
                cmd.Parameters.AddWithValue("@ObservacoesInternas", os.ObservacoesInternas);
                cmd.Parameters.AddWithValue("@DataAbertura", os.DataAbertura.ToString("o"));
                cmd.Parameters.AddWithValue("@DataPrevisao", (object?)os.DataPrevisaoConclusao?.ToString("o") ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DataConclusao", (object?)os.DataConclusao?.ToString("o") ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Desconto", (double)os.Desconto.Amount);
                cmd.Parameters.AddWithValue("@TenantId", os.TenantId.ToString());
                cmd.Parameters.AddWithValue("@FilialId", os.FilialId.ToString());
                cmd.Parameters.AddWithValue("@SessaoDiagnosticoId", (object?)os.SessaoDiagnosticoId?.ToString() ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync(cancellationToken);

                // Inserir histórico inicial
                foreach (var ev in os.Historico)
                {
                    await InserirEventoAsync(connection, transaction as SqliteTransaction, ev, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task AtualizarAsync(OrdemServico os, CancellationToken cancellationToken = default)
        {
            if (os == null) throw new ArgumentNullException(nameof(os));

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                await using var cmd = connection.CreateCommand();
                cmd.Transaction = transaction as SqliteTransaction;
                cmd.CommandText = @"
                    UPDATE OrdensServico SET
                        Status = @Status,
                        Prioridade = @Prioridade,
                        Diagnostico = @Diagnostico,
                        ObservacoesInternas = @ObservacoesInternas,
                        DataPrevisao = @DataPrevisao,
                        DataConclusao = @DataConclusao,
                        Desconto = @Desconto,
                        SessaoDiagnosticoId = @SessaoDiagnosticoId
                    WHERE Id = @Id;
                ";

                cmd.Parameters.AddWithValue("@Id", os.Id.ToString());
                cmd.Parameters.AddWithValue("@Status", os.Status.ToString());
                cmd.Parameters.AddWithValue("@Prioridade", os.Prioridade.ToString());
                cmd.Parameters.AddWithValue("@Diagnostico", os.DiagnosticoTecnico);
                cmd.Parameters.AddWithValue("@ObservacoesInternas", os.ObservacoesInternas);
                cmd.Parameters.AddWithValue("@DataPrevisao", (object?)os.DataPrevisaoConclusao?.ToString("o") ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DataConclusao", (object?)os.DataConclusao?.ToString("o") ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Desconto", (double)os.Desconto.Amount);
                cmd.Parameters.AddWithValue("@SessaoDiagnosticoId", (object?)os.SessaoDiagnosticoId?.ToString() ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync(cancellationToken);

                // Sincronizar itens: remove existentes da OS e reinsere
                await using var cmdDelItens = connection.CreateCommand();
                cmdDelItens.Transaction = transaction as SqliteTransaction;
                cmdDelItens.CommandText = "DELETE FROM OrdemServicoItens WHERE OrdemServicoId = @osId;";
                cmdDelItens.Parameters.AddWithValue("@osId", os.Id.ToString());
                await cmdDelItens.ExecuteNonQueryAsync(cancellationToken);

                foreach (var peca in os.ItensPeca)
                {
                    await using var cmdInsPeca = connection.CreateCommand();
                    cmdInsPeca.Transaction = transaction as SqliteTransaction;
                    cmdInsPeca.CommandText = @"
                        INSERT INTO OrdemServicoItens (
                            Id, OrdemServicoId, ProdutoId, Tipo, Descricao, Quantidade, ValorUnitario, CustoUnitario, Observacoes
                        ) VALUES (
                            @Id, @OrdemServicoId, @ProdutoId, 'Peca', @Descricao, @Quantidade, @ValorUnitario, @CustoUnitario, @Observacoes
                        );
                    ";
                    cmdInsPeca.Parameters.AddWithValue("@Id", peca.Id.ToString());
                    cmdInsPeca.Parameters.AddWithValue("@OrdemServicoId", os.Id.ToString());
                    cmdInsPeca.Parameters.AddWithValue("@ProdutoId", (object?)peca.ProdutoId?.ToString() ?? DBNull.Value);
                    cmdInsPeca.Parameters.AddWithValue("@Descricao", peca.Descricao);
                    cmdInsPeca.Parameters.AddWithValue("@Quantidade", (double)peca.Quantidade);
                    cmdInsPeca.Parameters.AddWithValue("@ValorUnitario", (double)peca.ValorUnitario.Amount);
                    cmdInsPeca.Parameters.AddWithValue("@CustoUnitario", (double)peca.CustoUnitario.Amount);
                    cmdInsPeca.Parameters.AddWithValue("@Observacoes", peca.Codigo);
                    await cmdInsPeca.ExecuteNonQueryAsync(cancellationToken);
                }

                foreach (var serv in os.ItensServico)
                {
                    await using var cmdInsServ = connection.CreateCommand();
                    cmdInsServ.Transaction = transaction as SqliteTransaction;
                    cmdInsServ.CommandText = @"
                        INSERT INTO OrdemServicoItens (
                            Id, OrdemServicoId, ProdutoId, Tipo, Descricao, Quantidade, ValorUnitario, CustoUnitario, Observacoes
                        ) VALUES (
                            @Id, @OrdemServicoId, @ProdutoId, 'Servico', @Descricao, @Quantidade, @ValorUnitario, 0, @Observacoes
                        );
                    ";
                    cmdInsServ.Parameters.AddWithValue("@Id", serv.Id.ToString());
                    cmdInsServ.Parameters.AddWithValue("@OrdemServicoId", os.Id.ToString());
                    cmdInsServ.Parameters.AddWithValue("@ProdutoId", (object?)serv.ServicoId?.ToString() ?? DBNull.Value);
                    cmdInsServ.Parameters.AddWithValue("@Descricao", serv.Descricao);
                    cmdInsServ.Parameters.AddWithValue("@Quantidade", (double)serv.QuantidadeHoras);
                    cmdInsServ.Parameters.AddWithValue("@ValorUnitario", (double)serv.ValorHora.Amount);
                    cmdInsServ.Parameters.AddWithValue("@Observacoes", serv.TecnicoResponsavel ?? "");
                    await cmdInsServ.ExecuteNonQueryAsync(cancellationToken);
                }

                // Sincronizar eventos (inserir novos)
                await using var cmdDelEv = connection.CreateCommand();
                cmdDelEv.Transaction = transaction as SqliteTransaction;
                cmdDelEv.CommandText = "DELETE FROM OrdemServicoEventos WHERE OrdemServicoId = @osId;";
                cmdDelEv.Parameters.AddWithValue("@osId", os.Id.ToString());
                await cmdDelEv.ExecuteNonQueryAsync(cancellationToken);

                foreach (var ev in os.Historico)
                {
                    await InserirEventoAsync(connection, transaction as SqliteTransaction, ev, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static async Task InserirEventoAsync(
            SqliteConnection connection,
            SqliteTransaction? transaction,
            HistoricoStatusOS ev,
            CancellationToken cancellationToken)
        {
            await using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
                INSERT INTO OrdemServicoEventos (
                    Id, OrdemServicoId, DataEvento, Titulo, Descricao, Tipo, Usuario
                ) VALUES (
                    @Id, @OrdemServicoId, @DataEvento, @Titulo, @Descricao, @Tipo, @Usuario
                );
            ";
            cmd.Parameters.AddWithValue("@Id", ev.Id.ToString());
            cmd.Parameters.AddWithValue("@OrdemServicoId", ev.OrdemServicoId.ToString());
            cmd.Parameters.AddWithValue("@DataEvento", ev.DataHora.ToString("o"));
            cmd.Parameters.AddWithValue("@Titulo", "Alteração de Status");
            cmd.Parameters.AddWithValue("@Descricao", ev.Motivo);
            cmd.Parameters.AddWithValue("@Tipo", ev.NovoStatus.ToString());
            cmd.Parameters.AddWithValue("@Usuario", ev.Responsavel);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
