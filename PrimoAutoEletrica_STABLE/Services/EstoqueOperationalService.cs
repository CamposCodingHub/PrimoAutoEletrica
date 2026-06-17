using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PrimoAutoEletrica.Services
{
    public sealed class EstoqueOperationalService
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggerService _logger;

        private sealed record ProdutoResumo(Guid Id, string Codigo, string Nome, decimal PrecoCompra, int QuantidadeEstoque);
        private sealed record ReservaAuditoria(
            Guid ProdutoId,
            string Codigo,
            string Nome,
            decimal PrecoCompra,
            int EstoqueAtual,
            int ReservadoAnterior,
            int ReservadoNovo,
            string Acao,
            string Detalhes,
            string? CorrelationId = null);

        public EstoqueOperationalService(DatabaseService? databaseService = null, LoggerService? logger = null)
        {
            _databaseService = databaseService ?? global::PrimoAutoEletrica.App.Database;
            _logger = logger ?? global::PrimoAutoEletrica.App.Logger;
        }

        public void EnriquecerProdutosComReservas(IEnumerable<Produto> produtos)
        {
            var lista = produtos?
                .Where(produto => produto != null && produto.Id != Guid.Empty)
                .ToList();

            if (lista == null || lista.Count == 0)
            {
                return;
            }

            var reservas = ObterReservasAtivasPorProduto(lista.Select(produto => produto.Id));
            foreach (var produto in lista)
            {
                produto.QuantidadeReservada = reservas.TryGetValue(produto.Id, out var quantidade)
                    ? quantidade
                    : 0;
            }
        }

        public int ObterReservaAtivaProduto(Guid produtoId, Guid? ignorarAgendamentoId = null)
        {
            if (produtoId == Guid.Empty)
            {
                return 0;
            }

            using var connection = _databaseService.GetConnection();
            connection.Open();
            return ObterReservasAtivasPorProduto(connection, transaction: null, new[] { produtoId }, ignorarAgendamentoId)
                .TryGetValue(produtoId, out var quantidade)
                ? quantidade
                : 0;
        }

        public Dictionary<Guid, int> ObterReservasAtivasPorProduto(IEnumerable<Guid> produtoIds, Guid? ignorarAgendamentoId = null)
        {
            var ids = produtoIds
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
            {
                return new Dictionary<Guid, int>();
            }

            using var connection = _databaseService.GetConnection();
            connection.Open();
            return ObterReservasAtivasPorProduto(connection, transaction: null, ids, ignorarAgendamentoId);
        }

        public void ReservarProdutosDoAgendamento(Agendamento agendamento, string usuario = "Sistema")
        {
            ArgumentNullException.ThrowIfNull(agendamento);

            var produtos = ObterProdutosValidos(agendamento);
            if (produtos.Count == 0)
            {
                return;
            }

            var correlationId = Guid.NewGuid().ToString("N");
            var auditorias = new List<ReservaAuditoria>();

            using var connection = _databaseService.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var grupo in produtos
                    .GroupBy(produto => produto.ProdutoId)
                    .Where(grupo => grupo.Key != Guid.Empty))
                {
                    var produtoId = grupo.Key;
                    var quantidadeSolicitada = grupo.Sum(item => Math.Max(0, item.Quantidade));
                    if (quantidadeSolicitada <= 0)
                    {
                        continue;
                    }

                    var produtoResumo = ObterProdutoResumo(connection, transaction, produtoId)
                        ?? throw new InvalidOperationException($"Produto '{grupo.First().ProdutoNome}' nao encontrado para reserva operacional.");
                    var reservasExternas = ObterReservasAtivasPorProduto(connection, transaction, new[] { produtoId }, agendamento.Id)
                        .TryGetValue(produtoId, out var quantidadeReservadaExterna)
                        ? quantidadeReservadaExterna
                        : 0;
                    var reservasAnteriores = ObterReservaDoAgendamento(connection, transaction, agendamento.Id, produtoId);
                    var disponibilidade = produtoResumo.QuantidadeEstoque - reservasExternas;

                    if (disponibilidade < quantidadeSolicitada)
                    {
                        throw new InvalidOperationException(
                            $"Reserva operacional insuficiente para '{produtoResumo.Nome}'. Disponivel: {disponibilidade}. Necessario: {quantidadeSolicitada}.");
                    }

                    if (reservasAnteriores == quantidadeSolicitada)
                    {
                        continue;
                    }

                    AtualizarReservaDoAgendamento(connection, transaction, agendamento.Id, produtoId, reservar: true);
                    auditorias.Add(new ReservaAuditoria(
                        produtoId,
                        produtoResumo.Codigo,
                        produtoResumo.Nome,
                        produtoResumo.PrecoCompra,
                        produtoResumo.QuantidadeEstoque,
                        reservasExternas + reservasAnteriores,
                        reservasExternas + quantidadeSolicitada,
                        "ReservaAgendamento",
                        $"Agendamento={agendamento.Numero}; Quantidade={quantidadeSolicitada}; Usuario={NormalizarUsuario(usuario)}",
                        correlationId));
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError($"Falha ao reservar produtos do agendamento '{agendamento.Id}'.", ex);
                throw;
            }

            foreach (var produto in agendamento.Produtos.Where(produto => produto.Quantidade > 0))
            {
                produto.Reservado = true;
                produto.DataReserva = DateTime.Now;
            }

            RegistrarAuditoria(auditorias);
        }

        public void LiberarReservasDoAgendamento(Agendamento agendamento, string motivo, string usuario = "Sistema")
        {
            ArgumentNullException.ThrowIfNull(agendamento);

            var auditorias = new List<ReservaAuditoria>();

            using var connection = _databaseService.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var reservasAgendamento = ObterReservasDoAgendamento(connection, transaction, agendamento.Id);
                foreach (var reserva in reservasAgendamento.Where(item => item.Value > 0))
                {
                    var produtoResumo = ObterProdutoResumo(connection, transaction, reserva.Key)
                        ?? throw new InvalidOperationException($"Produto '{reserva.Key}' nao encontrado para liberar reserva.");
                    var reservasTotaisAntes = ObterReservasAtivasPorProduto(connection, transaction, new[] { reserva.Key }, ignorarAgendamentoId: null)
                        .TryGetValue(reserva.Key, out var quantidadeReservada)
                        ? quantidadeReservada
                        : 0;

                    AtualizarReservaDoAgendamento(connection, transaction, agendamento.Id, reserva.Key, reservar: false);
                    auditorias.Add(new ReservaAuditoria(
                        reserva.Key,
                        produtoResumo.Codigo,
                        produtoResumo.Nome,
                        produtoResumo.PrecoCompra,
                        produtoResumo.QuantidadeEstoque,
                        reservasTotaisAntes,
                        Math.Max(0, reservasTotaisAntes - reserva.Value),
                        "LiberacaoReservaAgendamento",
                        $"Agendamento={agendamento.Numero}; Quantidade={reserva.Value}; Motivo={motivo}; Usuario={NormalizarUsuario(usuario)}"));
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError($"Falha ao liberar reservas do agendamento '{agendamento.Id}'.", ex);
                throw;
            }

            foreach (var produto in agendamento.Produtos)
            {
                produto.Reservado = false;
                produto.DataReserva = null;
            }

            RegistrarAuditoria(auditorias);
        }

        public void RegistrarInventario(Guid produtoId, int quantidadeContada, string motivo, string usuario = "Sistema")
        {
            if (produtoId == Guid.Empty)
            {
                throw new InvalidOperationException("Selecione um produto valido para registrar o inventario.");
            }

            if (quantidadeContada < 0)
            {
                throw new InvalidOperationException("A quantidade contada do inventario nao pode ser negativa.");
            }

            ComercialValidationHelper.GarantirTextoObrigatorio(motivo, "o motivo do inventario");

            ProdutoResumo produtoResumo;
            int reservasAtivas;
            int quantidadeAnterior;

            using var connection = _databaseService.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                produtoResumo = ObterProdutoResumo(connection, transaction, produtoId)
                    ?? throw new InvalidOperationException("Produto nao encontrado para registrar inventario.");
                reservasAtivas = ObterReservasAtivasPorProduto(connection, transaction, new[] { produtoId }, ignorarAgendamentoId: null)
                    .TryGetValue(produtoId, out var quantidadeReservada)
                    ? quantidadeReservada
                    : 0;
                quantidadeAnterior = produtoResumo.QuantidadeEstoque;

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    UPDATE Produtos
                    SET
                        QuantidadeEstoque = @QuantidadeEstoque,
                        ValorTotalEstoque = @ValorTotalEstoque,
                        DataUltimaAtualizacao = @DataUltimaAtualizacao,
                        RowVersion = COALESCE(RowVersion, 0) + 1,
                        DataUltimaAlteracao = @DataUltimaAlteracao
                    WHERE Id = @Id;";
                command.Parameters.AddWithValue("@QuantidadeEstoque", quantidadeContada);
                command.Parameters.AddWithValue("@ValorTotalEstoque", quantidadeContada * produtoResumo.PrecoCompra);
                command.Parameters.AddWithValue("@DataUltimaAtualizacao", ToDbDate(connection, DateTime.Now));
                command.Parameters.AddWithValue("@DataUltimaAlteracao", ToDbDate(connection, DateTime.Now));
                AddGuidParameter(command, connection, "@Id", produtoId);
                command.ExecuteNonQuery();

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError($"Falha ao registrar inventario do produto '{produtoId}'.", ex);
                throw;
            }

            global::PrimoAutoEletrica.App.Audit.Registrar(
                categoria: "Estoque",
                acao: "InventarioProduto",
                entidade: "Produto",
                entidadeId: produtoId.ToString(),
                detalhes: $"Produto={produtoResumo.Nome}; QuantidadeContada={quantidadeContada}; Divergencia={quantidadeContada - quantidadeAnterior}; Reservado={reservasAtivas}; Motivo={motivo}; Usuario={NormalizarUsuario(usuario)}",
                usuarioNomeOverride: NormalizarUsuario(usuario),
                valorAnterior: CriarSnapshot(produtoResumo.Codigo, produtoResumo.Nome, produtoResumo.PrecoCompra, quantidadeAnterior, reservasAtivas),
                valorNovo: CriarSnapshot(produtoResumo.Codigo, produtoResumo.Nome, produtoResumo.PrecoCompra, quantidadeContada, reservasAtivas));
        }

        public void RegistrarMovimentacaoManual(
            Guid produtoId,
            int quantidade,
            string operacao,
            string motivo,
            string usuario = "Sistema",
            bool permitirDisponivelNegativo = false)
        {
            if (produtoId == Guid.Empty)
            {
                throw new InvalidOperationException("Selecione um produto valido para movimentar estoque.");
            }

            if (quantidade <= 0)
            {
                throw new InvalidOperationException("A quantidade da movimentacao precisa ser maior que zero.");
            }

            var isEntrada = string.Equals(operacao, "Entrada", StringComparison.OrdinalIgnoreCase);
            var isSaida = string.Equals(operacao, "Saida", StringComparison.OrdinalIgnoreCase);

            if (!isEntrada && !isSaida)
            {
                throw new InvalidOperationException("Operacao de estoque invalida. Use Entrada ou Saida.");
            }

            ComercialValidationHelper.GarantirTextoObrigatorio(motivo, "o motivo da movimentacao");

            ProdutoResumo produtoResumo;
            int reservasAtivas;
            int quantidadeAnterior;
            int quantidadeNova;

            using var connection = _databaseService.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                produtoResumo = ObterProdutoResumo(connection, transaction, produtoId)
                    ?? throw new InvalidOperationException("Produto nao encontrado para movimentar estoque.");
                reservasAtivas = ObterReservasAtivasPorProduto(connection, transaction, new[] { produtoId }, ignorarAgendamentoId: null)
                    .TryGetValue(produtoId, out var quantidadeReservada)
                    ? quantidadeReservada
                    : 0;
                quantidadeAnterior = produtoResumo.QuantidadeEstoque;
                quantidadeNova = quantidadeAnterior + (isEntrada ? quantidade : -quantidade);
                var disponibilidadeNova = quantidadeNova - reservasAtivas;

                if (quantidadeNova < 0 && !permitirDisponivelNegativo)
                {
                    throw new InvalidOperationException("A movimentacao deixaria o estoque fisico negativo.");
                }

                if (isSaida && disponibilidadeNova < 0 && !permitirDisponivelNegativo)
                {
                    throw new InvalidOperationException(
                        $"A saida excede a disponibilidade operacional. Disponivel atual: {quantidadeAnterior - reservasAtivas}; Reservado: {reservasAtivas}.");
                }

                var observacao = $"Movimentacao dedicada em {DateTime.Now:dd/MM/yyyy HH:mm}: {(isEntrada ? "Entrada" : "Saida")} de {quantidade} - Motivo: {motivo}";

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = $@"
                    UPDATE Produtos
                    SET
                        QuantidadeEstoque = @QuantidadeEstoque,
                        ValorTotalEstoque = @ValorTotalEstoque,
                        DataUltimaAtualizacao = @DataUltimaAtualizacao,
                        Observacoes = CASE
                            WHEN COALESCE(Observacoes, '') = '' THEN @Observacao
                            ELSE {ConcatExpression(connection, "Observacoes", "@Separador", "@Observacao")}
                        END,
                        RowVersion = COALESCE(RowVersion, 0) + 1,
                        DataUltimaAlteracao = @DataUltimaAlteracao
                    WHERE Id = @Id;";
                command.Parameters.AddWithValue("@QuantidadeEstoque", quantidadeNova);
                command.Parameters.AddWithValue("@ValorTotalEstoque", quantidadeNova * produtoResumo.PrecoCompra);
                command.Parameters.AddWithValue("@DataUltimaAtualizacao", ToDbDate(connection, DateTime.Now));
                command.Parameters.AddWithValue("@DataUltimaAlteracao", ToDbDate(connection, DateTime.Now));
                command.Parameters.AddWithValue("@Observacao", observacao);
                command.Parameters.AddWithValue("@Separador", Environment.NewLine);
                AddGuidParameter(command, connection, "@Id", produtoId);
                command.ExecuteNonQuery();

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError($"Falha ao registrar movimentacao manual do produto '{produtoId}'.", ex);
                throw;
            }

            global::PrimoAutoEletrica.App.Audit.Registrar(
                categoria: "Estoque",
                acao: isEntrada ? "EntradaEstoqueDedicada" : "SaidaEstoqueDedicada",
                entidade: "Produto",
                entidadeId: produtoId.ToString(),
                detalhes: $"Produto={produtoResumo.Nome}; Operacao={(isEntrada ? "Entrada" : "Saida")}; Quantidade={quantidade}; Reservado={reservasAtivas}; Motivo={motivo}; Usuario={NormalizarUsuario(usuario)}",
                usuarioNomeOverride: NormalizarUsuario(usuario),
                valorAnterior: CriarSnapshot(produtoResumo.Codigo, produtoResumo.Nome, produtoResumo.PrecoCompra, quantidadeAnterior, reservasAtivas),
                valorNovo: CriarSnapshot(produtoResumo.Codigo, produtoResumo.Nome, produtoResumo.PrecoCompra, quantidadeNova, reservasAtivas));
        }

        public List<DadoAuditoria> ObterHistoricoProduto(Guid produtoId, int limite = 25)
        {
            var historico = new List<DadoAuditoria>();
            if (produtoId == Guid.Empty || limite <= 0)
            {
                return historico;
            }

            using var connection = _databaseService.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT {SelectTop(connection, "@Limite")}
                    Id,
                    DataHora,
                    COALESCE(UsuarioNome, 'Sistema'),
                    COALESCE(Acao, ''),
                    COALESCE(Entidade, ''),
                    COALESCE(EntidadeId, ''),
                    COALESCE(Categoria, ''),
                    COALESCE(Severidade, 'Info'),
                    COALESCE(Sucesso, 1),
                    COALESCE(Perfil, ''),
                    COALESCE(CorrelationId, ''),
                    COALESCE(Maquina, ''),
                    COALESCE(ValorAnterior, ''),
                    COALESCE(ValorNovo, '')
                FROM AuditLogs
                WHERE Categoria = 'Estoque'
                  AND Entidade = 'Produto'
                  AND EntidadeId = @EntidadeId
                ORDER BY DataHora DESC
                {Limit(connection, "@Limite")};";
            command.Parameters.AddWithValue("@EntidadeId", produtoId.ToString());
            command.Parameters.AddWithValue("@Limite", limite);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                historico.Add(new DadoAuditoria
                {
                    Id = ReadGuid(reader, 0),
                    DataHora = ReadDate(reader, 1, DateTime.Now),
                    Usuario = ReadString(reader, 2),
                    Acao = ReadString(reader, 3),
                    Tabela = ReadString(reader, 4),
                    RegistroId = Guid.TryParse(ReadString(reader, 5), out var registroId) ? registroId : Guid.Empty,
                    Categoria = ReadString(reader, 6),
                    Severidade = ReadString(reader, 7),
                    Sucesso = ReadBool(reader, 8, fallback: true),
                    Perfil = ReadString(reader, 9),
                    CorrelationId = ReadString(reader, 10),
                    Maquina = ReadString(reader, 11),
                    ValorAnterior = ReadString(reader, 12),
                    ValorNovo = ReadString(reader, 13)
                });
            }

            return historico;
        }

        public static string CriarSnapshot(Produto produto, int quantidadeEstoque, int quantidadeReservada)
        {
            return CriarSnapshot(produto.Codigo, produto.Nome, produto.PrecoCompra, quantidadeEstoque, quantidadeReservada);
        }

        public static string CriarSnapshot(string codigo, string nome, decimal precoCompra, int quantidadeEstoque, int quantidadeReservada)
        {
            var quantidadeDisponivel = quantidadeEstoque - quantidadeReservada;
            return $"Codigo={codigo}; Nome={nome}; Estoque={quantidadeEstoque}; Reservado={quantidadeReservada}; Disponivel={quantidadeDisponivel}; ValorTotal={(quantidadeEstoque * precoCompra):C}";
        }

        private static string NormalizarUsuario(string usuario)
        {
            return string.IsNullOrWhiteSpace(usuario)
                ? "Sistema"
                : usuario.Trim();
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

        private static int ObterReservaDoAgendamento(
            DbConnection connection,
            DbTransaction transaction,
            Guid agendamentoId,
            Guid produtoId)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT COALESCE(SUM(Quantidade), 0)
                FROM AgendamentoProdutos
                WHERE AgendamentoId = @AgendamentoId
                  AND ProdutoId = @ProdutoId
                  AND Reservado = 1;";
            command.Parameters.AddWithValue("@AgendamentoId", agendamentoId.ToString());
            command.Parameters.AddWithValue("@ProdutoId", produtoId.ToString());
            return Convert.ToInt32(command.ExecuteScalar() ?? 0);
        }

        private static Dictionary<Guid, int> ObterReservasDoAgendamento(
            DbConnection connection,
            DbTransaction transaction,
            Guid agendamentoId)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT ProdutoId, COALESCE(SUM(Quantidade), 0)
                FROM AgendamentoProdutos
                WHERE AgendamentoId = @AgendamentoId
                  AND Reservado = 1
                GROUP BY ProdutoId;";
            command.Parameters.AddWithValue("@AgendamentoId", agendamentoId.ToString());

            using var reader = command.ExecuteReader();
            var reservas = new Dictionary<Guid, int>();
            while (reader.Read())
            {
                if (!reader.IsDBNull(0) && Guid.TryParse(reader.GetString(0), out var produtoId))
                {
                    reservas[produtoId] = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                }
            }

            return reservas;
        }

        private static ProdutoResumo? ObterProdutoResumo(DbConnection connection, DbTransaction transaction, Guid produtoId)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $@"
                SELECT {SelectTop(connection, "1")} Id, Codigo, Nome, PrecoCompra, QuantidadeEstoque
                FROM Produtos
                WHERE Id = @Id
                {Limit(connection, "1")};";
            AddGuidParameter(command, connection, "@Id", produtoId);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return new ProdutoResumo(
                ReadGuid(reader, 0),
                ReadString(reader, 1),
                ReadString(reader, 2),
                ReadDecimal(reader, 3),
                ReadInt(reader, 4));
        }

        private static Dictionary<Guid, int> ObterReservasAtivasPorProduto(
            DbConnection connection,
            DbTransaction? transaction,
            IReadOnlyCollection<Guid> produtoIds,
            Guid? ignorarAgendamentoId)
        {
            if (produtoIds.Count == 0)
            {
                return new Dictionary<Guid, int>();
            }

            using var command = connection.CreateCommand();
            command.Transaction = transaction;

            var parametros = produtoIds
                .Select((_, indice) => $"@ProdutoId{indice}")
                .ToArray();

            command.CommandText = $@"
                SELECT ap.ProdutoId, COALESCE(SUM(ap.Quantidade), 0) AS QuantidadeReservada
                FROM AgendamentoProdutos ap
                INNER JOIN Agendamentos a ON a.Id = ap.AgendamentoId
                WHERE ap.Reservado = 1
                  AND ap.Quantidade > 0
                  AND ap.ProdutoId IN ({string.Join(", ", parametros)})
                  AND COALESCE(a.Status, '') NOT IN ('Cancelado', 'Finalizado')
                  AND (@IgnorarAgendamentoId IS NULL OR ap.AgendamentoId <> @IgnorarAgendamentoId)
                GROUP BY ap.ProdutoId;";

            for (var indice = 0; indice < produtoIds.Count; indice++)
            {
                command.Parameters.AddWithValue($"@ProdutoId{indice}", produtoIds.ElementAt(indice).ToString());
            }

            command.Parameters.AddWithValue("@IgnorarAgendamentoId", ignorarAgendamentoId.HasValue
                ? ignorarAgendamentoId.Value.ToString()
                : (object)DBNull.Value);

            using var reader = command.ExecuteReader();
            var reservas = new Dictionary<Guid, int>();
            while (reader.Read())
            {
                if (!reader.IsDBNull(0) && Guid.TryParse(reader.GetString(0), out var produtoId))
                {
                    reservas[produtoId] = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                }
            }

            return reservas;
        }

        private List<AgendamentoProduto> ObterProdutosValidos(Agendamento agendamento)
        {
            if (agendamento.Produtos != null && agendamento.Produtos.Count > 0)
            {
                return agendamento.Produtos
                    .Where(produto => produto.ProdutoId != Guid.Empty && produto.Quantidade > 0)
                    .ToList();
            }

            using var connection = _databaseService.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT ProdutoId, ProdutoNome, ProdutoCodigo, Quantidade, PrecoUnitario, PrecoTotal, Reservado, DataReserva
                FROM AgendamentoProdutos
                WHERE AgendamentoId = @AgendamentoId
                  AND Quantidade > 0;";
            command.Parameters.AddWithValue("@AgendamentoId", agendamento.Id.ToString());

            using var reader = command.ExecuteReader();
            var produtos = new List<AgendamentoProduto>();
            while (reader.Read())
            {
                produtos.Add(new AgendamentoProduto
                {
                    ProdutoId = ReadGuid(reader, 0),
                    ProdutoNome = ReadString(reader, 1),
                    ProdutoCodigo = ReadString(reader, 2),
                    Quantidade = ReadInt(reader, 3),
                    PrecoUnitario = ReadDecimal(reader, 4),
                    PrecoTotal = ReadDecimal(reader, 5),
                    Reservado = ReadBool(reader, 6),
                    DataReserva = reader.IsDBNull(7) ? null : ReadDate(reader, 7, DateTime.Now)
                });
            }

            return produtos;
        }

        private static void RegistrarAuditoria(IEnumerable<ReservaAuditoria> auditorias)
        {
            foreach (var auditoria in auditorias)
            {
                global::PrimoAutoEletrica.App.Audit.Registrar(
                    categoria: "Estoque",
                    acao: auditoria.Acao,
                    entidade: "Produto",
                    entidadeId: auditoria.ProdutoId.ToString(),
                    detalhes: auditoria.Detalhes,
                    valorAnterior: CriarSnapshot(auditoria.Codigo, auditoria.Nome, auditoria.PrecoCompra, auditoria.EstoqueAtual, auditoria.ReservadoAnterior),
                    valorNovo: CriarSnapshot(auditoria.Codigo, auditoria.Nome, auditoria.PrecoCompra, auditoria.EstoqueAtual, auditoria.ReservadoNovo),
                    correlationId: auditoria.CorrelationId);
            }
        }

        private static void AddGuidParameter(DbCommand command, DbConnection connection, string name, Guid value)
        {
            command.Parameters.AddWithValue(name, IsSqlServerConnection(connection) ? value : value.ToString());
        }

        private static object ToDbDate(DbConnection connection, DateTime value)
        {
            return IsSqlServerConnection(connection)
                ? value
                : value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private static string SelectTop(DbConnection connection, string countExpression)
        {
            return IsSqlServerConnection(connection) ? $"TOP ({countExpression})" : string.Empty;
        }

        private static string Limit(DbConnection connection, string countExpression)
        {
            return IsSqlServerConnection(connection) ? string.Empty : $"LIMIT {countExpression}";
        }

        private static string ConcatExpression(DbConnection connection, params string[] parts)
        {
            return IsSqlServerConnection(connection)
                ? $"CONCAT({string.Join(", ", parts)})"
                : string.Join(" || ", parts);
        }

        private static bool IsSqlServerConnection(DbConnection connection)
        {
            var typeName = connection.GetType().FullName ?? string.Empty;
            return typeName.Contains("SqlClient", StringComparison.OrdinalIgnoreCase) ||
                   typeName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadString(DbDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? string.Empty : Convert.ToString(reader.GetValue(index), CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static int ReadInt(DbDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0 : Convert.ToInt32(reader.GetValue(index), CultureInfo.InvariantCulture);
        }

        private static decimal ReadDecimal(DbDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0m : Convert.ToDecimal(reader.GetValue(index), CultureInfo.InvariantCulture);
        }

        private static bool ReadBool(DbDataReader reader, int index, bool fallback = false)
        {
            if (reader.IsDBNull(index))
            {
                return fallback;
            }

            return reader.GetValue(index) switch
            {
                bool value => value,
                byte value => value != 0,
                short value => value != 0,
                int value => value != 0,
                long value => value != 0,
                _ => bool.TryParse(ReadString(reader, index), out var parsed) ? parsed : fallback
            };
        }

        private static Guid ReadGuid(DbDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
            {
                return Guid.Empty;
            }

            var value = reader.GetValue(index);
            return value is Guid guid || Guid.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out guid)
                ? guid
                : Guid.Empty;
        }

        private static DateTime ReadDate(DbDataReader reader, int index, DateTime fallback)
        {
            if (reader.IsDBNull(index))
            {
                return fallback;
            }

            var value = reader.GetValue(index);
            return value is DateTime date ||
                   DateTime.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
                ? date
                : fallback;
        }
    }
}
