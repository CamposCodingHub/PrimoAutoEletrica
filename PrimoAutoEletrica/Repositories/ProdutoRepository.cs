using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PrimoAutoEletrica.Repositories
{
    public sealed class ProdutoRepository : IProdutoRepository
    {
        private readonly Func<SqliteConnection> _connectionFactory;
        private readonly LoggerService _logger;

        private const string ProdutoColumns = @"
            Id,
            Codigo,
            Nome,
            Descricao,
            Categoria,
            Marca,
            Modelo,
            FornecedorId,
            Fornecedor,
            CNPJFornecedor,
            ContatoFornecedor,
            TelefoneFornecedor,
            QuantidadeEstoque,
            QuantidadeMinima,
            QuantidadeMaxima,
            Localizacao,
            Prateleira,
            Gaveta,
            PrecoCompra,
            PrecoVenda,
            MargemLucro,
            ValorTotalEstoque,
            UnidadeMedida,
            Peso,
            Dimensoes,
            Cor,
            Material,
            CodigoBarras,
            SKU,
            NCMS,
            CEST,
            CFOP,
            Ativo,
            ProdutoPerecivel,
            DataValidade,
            DataFabricacao,
            Lote,
            DataCadastro,
            DataUltimaCompra,
            DataUltimaVenda,
            DataUltimaAtualizacao,
            Observacoes,
            ImagemUrl,
            Anexos,
            TotalVendas,
            TotalFaturado,
            VendasUltimoMes,
            VendasUltimoTrimestre";

        public ProdutoRepository(Func<SqliteConnection> connectionFactory, LoggerService logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<Produto> ObterTodos()
        {
            var produtos = new List<Produto>();

            try
            {
                using var connection = _connectionFactory();
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = $@"
                    SELECT {ProdutoColumns}
                    FROM Produtos
                    ORDER BY DataCadastro DESC;";

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    try
                    {
                        produtos.Add(MaterializarProduto(reader));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Erro ao materializar produto na listagem.", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao obter produtos no repositorio.", ex);
            }

            return produtos;
        }

        public Produto? ObterPorId(Guid id)
        {
            using var connection = _connectionFactory();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT {ProdutoColumns}
                FROM Produtos
                WHERE Id = @Id
                LIMIT 1;";
            command.Parameters.AddWithValue("@Id", id.ToString());

            using var reader = command.ExecuteReader();
            return reader.Read() ? MaterializarProduto(reader) : null;
        }

        public void Inserir(Produto produto)
        {
            if (produto == null)
            {
                throw new ArgumentNullException(nameof(produto));
            }

            NormalizarProduto(produto);
            produto.Id = produto.Id == Guid.Empty ? Guid.NewGuid() : produto.Id;
            produto.DataCadastro = produto.DataCadastro == default ? DateTime.Now : produto.DataCadastro;
            produto.DataUltimaAtualizacao = DateTime.Now;
            produto.ValorTotalEstoque = produto.QuantidadeEstoque * produto.PrecoCompra;

            using var connection = _connectionFactory();
            connection.Open();
            ResolverFornecedorPrincipal(connection, produto);
            ValidarProdutoParaPersistencia(connection, produto, anterior: null);

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                INSERT INTO Produtos
                (
                    {ProdutoColumns}
                )
                VALUES
                (
                    @Id,
                    @Codigo,
                    @Nome,
                    @Descricao,
                    @Categoria,
                    @Marca,
                    @Modelo,
                    @FornecedorId,
                    @Fornecedor,
                    @CNPJFornecedor,
                    @ContatoFornecedor,
                    @TelefoneFornecedor,
                    @QuantidadeEstoque,
                    @QuantidadeMinima,
                    @QuantidadeMaxima,
                    @Localizacao,
                    @Prateleira,
                    @Gaveta,
                    @PrecoCompra,
                    @PrecoVenda,
                    @MargemLucro,
                    @ValorTotalEstoque,
                    @UnidadeMedida,
                    @Peso,
                    @Dimensoes,
                    @Cor,
                    @Material,
                    @CodigoBarras,
                    @SKU,
                    @NCMS,
                    @CEST,
                    @CFOP,
                    @Ativo,
                    @ProdutoPerecivel,
                    @DataValidade,
                    @DataFabricacao,
                    @Lote,
                    @DataCadastro,
                    @DataUltimaCompra,
                    @DataUltimaVenda,
                    @DataUltimaAtualizacao,
                    @Observacoes,
                    @ImagemUrl,
                    @Anexos,
                    @TotalVendas,
                    @TotalFaturado,
                    @VendasUltimoMes,
                    @VendasUltimoTrimestre
                );";

            AddProdutoParameters(command, produto);
            command.ExecuteNonQuery();

            RegistrarAuditoria("ProdutoCriado", produto, null, CriarSnapshot(produto));
        }

        public void Atualizar(Produto produto)
        {
            if (produto == null)
            {
                throw new ArgumentNullException(nameof(produto));
            }

            var anterior = ObterPorId(produto.Id);
            NormalizarProduto(produto);
            produto.DataUltimaAtualizacao = DateTime.Now;
            produto.ValorTotalEstoque = produto.QuantidadeEstoque * produto.PrecoCompra;

            using var connection = _connectionFactory();
            connection.Open();
            ResolverFornecedorPrincipal(connection, produto);
            ValidarProdutoParaPersistencia(connection, produto, anterior);

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Produtos
                SET
                    Codigo = @Codigo,
                    Nome = @Nome,
                    Descricao = @Descricao,
                    Categoria = @Categoria,
                    Marca = @Marca,
                    Modelo = @Modelo,
                    FornecedorId = @FornecedorId,
                    Fornecedor = @Fornecedor,
                    CNPJFornecedor = @CNPJFornecedor,
                    ContatoFornecedor = @ContatoFornecedor,
                    TelefoneFornecedor = @TelefoneFornecedor,
                    QuantidadeEstoque = @QuantidadeEstoque,
                    QuantidadeMinima = @QuantidadeMinima,
                    QuantidadeMaxima = @QuantidadeMaxima,
                    Localizacao = @Localizacao,
                    Prateleira = @Prateleira,
                    Gaveta = @Gaveta,
                    PrecoCompra = @PrecoCompra,
                    PrecoVenda = @PrecoVenda,
                    MargemLucro = @MargemLucro,
                    ValorTotalEstoque = @ValorTotalEstoque,
                    UnidadeMedida = @UnidadeMedida,
                    Peso = @Peso,
                    Dimensoes = @Dimensoes,
                    Cor = @Cor,
                    Material = @Material,
                    CodigoBarras = @CodigoBarras,
                    SKU = @SKU,
                    NCMS = @NCMS,
                    CEST = @CEST,
                    CFOP = @CFOP,
                    Ativo = @Ativo,
                    ProdutoPerecivel = @ProdutoPerecivel,
                    DataValidade = @DataValidade,
                    DataFabricacao = @DataFabricacao,
                    Lote = @Lote,
                    DataUltimaCompra = @DataUltimaCompra,
                    DataUltimaVenda = @DataUltimaVenda,
                    DataUltimaAtualizacao = @DataUltimaAtualizacao,
                    Observacoes = @Observacoes,
                    ImagemUrl = @ImagemUrl,
                    Anexos = @Anexos,
                    TotalVendas = @TotalVendas,
                    TotalFaturado = @TotalFaturado,
                    VendasUltimoMes = @VendasUltimoMes,
                    VendasUltimoTrimestre = @VendasUltimoTrimestre,
                    RowVersion = COALESCE(RowVersion, 0) + 1,
                    DataUltimaAlteracao = @DataUltimaAlteracao
                WHERE Id = @Id;";

            AddProdutoParameters(command, produto);
            command.Parameters.AddWithValue("@DataUltimaAlteracao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.ExecuteNonQuery();

            RegistrarAuditoria("ProdutoAtualizado", produto, CriarSnapshot(anterior), CriarSnapshot(produto));
        }

        public void Excluir(Guid id)
        {
            var produto = ObterPorId(id);

            using var connection = _connectionFactory();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Produtos WHERE Id = @Id;";
            command.Parameters.AddWithValue("@Id", id.ToString());
            command.ExecuteNonQuery();

            RegistrarAuditoria("ProdutoExcluido", produto, CriarSnapshot(produto), null, id);
        }

        public int InserirEmMassa(string caminhoArquivo)
        {
            if (!File.Exists(caminhoArquivo))
            {
                throw new FileNotFoundException($"Arquivo nao encontrado: {caminhoArquivo}");
            }

            var inseridos = 0;
            foreach (var linha in File.ReadAllLines(caminhoArquivo))
            {
                if (string.IsNullOrWhiteSpace(linha) || linha.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                var partes = linha.Split(';');
                if (partes.Length < 13)
                {
                    continue;
                }

                try
                {
                    var produto = new Produto
                    {
                        Id = Guid.NewGuid(),
                        Codigo = partes[0],
                        Nome = partes[0],
                        Descricao = partes.Length > 12 ? partes[12] : string.Empty,
                        Categoria = partes[1],
                        Marca = partes[2],
                        Modelo = partes[3],
                        QuantidadeEstoque = int.TryParse(partes[6], out var quantidade) ? quantidade : 0,
                        QuantidadeMinima = int.TryParse(partes[7], out var minima) ? minima : 0,
                        QuantidadeMaxima = 0,
                        Localizacao = partes[8],
                        Prateleira = partes[9],
                        Gaveta = partes[10],
                        PrecoCompra = decimal.TryParse(partes[4], out var precoCompra) ? precoCompra : 0,
                        PrecoVenda = decimal.TryParse(partes[5], out var precoVenda) ? precoVenda : 0,
                        Fornecedor = partes[11],
                        Ativo = true,
                        ProdutoPerecivel = false,
                        DataCadastro = DateTime.Now,
                        Observacoes = partes.Length > 12 ? partes[12] : string.Empty
                    };

                    if (produto.PrecoVenda > 0)
                    {
                        produto.MargemLucro = ((produto.PrecoVenda - produto.PrecoCompra) / produto.PrecoVenda) * 100;
                    }

                    Inserir(produto);
                    inseridos++;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Erro ao inserir produto importado da linha '{linha}'.", ex);
                }
            }

            _logger.LogInfo($"Importacao em massa de produtos concluida com {inseridos} item(ns) inserido(s).");
            return inseridos;
        }

        public void BaixarProdutosDoEstoquePorAgendamento(IEnumerable<AgendamentoProduto> produtos, Guid agendamentoId)
        {
            var produtosValidos = produtos.Where(produto => produto.Quantidade > 0).ToList();
            if (produtosValidos.Count == 0)
            {
                return;
            }

            var auditorias = new List<(Guid ProdutoId, string Codigo, string Nome, decimal PrecoCompra, int EstoqueAnterior, int EstoqueNovo, int ReservadoAnterior, int ReservadoNovo, int Quantidade)>();

            using var connection = _connectionFactory();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var produto in produtosValidos)
                {
                    var (codigoProduto, nomeProduto, precoCompra, estoqueAtual, reservadoNoAgendamento, reservasAtivas) = ObterEstoqueAtual(connection, transaction, agendamentoId, produto);
                    var reservasExternas = Math.Max(0, reservasAtivas - reservadoNoAgendamento);
                    var disponibilidadeOperacional = estoqueAtual - reservasExternas;
                    if (disponibilidadeOperacional < produto.Quantidade)
                    {
                        throw new InvalidOperationException($"Estoque insuficiente para '{nomeProduto}'. Disponivel operacional: {disponibilidadeOperacional}. Necessario: {produto.Quantidade}.");
                    }

                    using var updateCommand = connection.CreateCommand();
                    updateCommand.Transaction = transaction;
                    updateCommand.CommandText = @"
                        UPDATE Produtos
                        SET
                            QuantidadeEstoque = QuantidadeEstoque - @Quantidade,
                            ValorTotalEstoque = (QuantidadeEstoque - @Quantidade) * PrecoCompra,
                            DataUltimaAtualizacao = @DataUltimaAtualizacao,
                            RowVersion = COALESCE(RowVersion, 0) + 1,
                            DataUltimaAlteracao = @DataUltimaAlteracao
                        WHERE Id = @Id;";
                    updateCommand.Parameters.AddWithValue("@Quantidade", produto.Quantidade);
                    updateCommand.Parameters.AddWithValue("@DataUltimaAtualizacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    updateCommand.Parameters.AddWithValue("@DataUltimaAlteracao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    updateCommand.Parameters.AddWithValue("@Id", produto.ProdutoId.ToString());
                    updateCommand.ExecuteNonQuery();

                    if (reservadoNoAgendamento > 0)
                    {
                        using var reservaCommand = connection.CreateCommand();
                        reservaCommand.Transaction = transaction;
                        reservaCommand.CommandText = @"
                            UPDATE AgendamentoProdutos
                            SET
                                Reservado = 0,
                                DataReserva = NULL
                            WHERE AgendamentoId = @AgendamentoId
                              AND ProdutoId = @ProdutoId;";
                        reservaCommand.Parameters.AddWithValue("@AgendamentoId", agendamentoId.ToString());
                        reservaCommand.Parameters.AddWithValue("@ProdutoId", produto.ProdutoId.ToString());
                        reservaCommand.ExecuteNonQuery();
                    }

                    auditorias.Add((
                        produto.ProdutoId,
                        codigoProduto,
                        nomeProduto,
                        precoCompra,
                        estoqueAtual,
                        estoqueAtual - produto.Quantidade,
                        reservasAtivas,
                        reservadoNoAgendamento > 0 ? reservasExternas : reservasAtivas,
                        produto.Quantidade));
                }

                transaction.Commit();

                foreach (var auditoria in auditorias)
                {
                    global::PrimoAutoEletrica.App.Audit.Registrar(
                        categoria: "Estoque",
                        acao: "BaixaAgendamentoDireta",
                        entidade: "Produto",
                        entidadeId: auditoria.ProdutoId.ToString(),
                        detalhes: $"Agendamento={agendamentoId}; Quantidade={auditoria.Quantidade}",
                        valorAnterior: EstoqueOperationalService.CriarSnapshot(auditoria.Codigo, auditoria.Nome, auditoria.PrecoCompra, auditoria.EstoqueAnterior, auditoria.ReservadoAnterior),
                        valorNovo: EstoqueOperationalService.CriarSnapshot(auditoria.Codigo, auditoria.Nome, auditoria.PrecoCompra, auditoria.EstoqueNovo, auditoria.ReservadoNovo),
                        correlationId: agendamentoId.ToString("N"));
                }

                global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                    "Estoque",
                    "BaixaAgendamento",
                    "Agendamento",
                    agendamentoId.ToString(),
                    $"Produtos: {produtosValidos.Count}; Quantidade total: {produtosValidos.Sum(p => p.Quantidade)}");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError($"Falha ao baixar produtos do estoque para agendamento '{agendamentoId}'.", ex);
                throw;
            }
        }

        private static (string Codigo, string Nome, decimal PrecoCompra, int EstoqueAtual, int ReservadoNoAgendamento, int ReservasAtivas) ObterEstoqueAtual(
            SqliteConnection connection,
            SqliteTransaction transaction,
            Guid agendamentoId,
            AgendamentoProduto produto)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT
                    Nome,
                    QuantidadeEstoque,
                    COALESCE(Codigo, ''),
                    COALESCE(PrecoCompra, 0),
                    (
                        SELECT COALESCE(SUM(ap.Quantidade), 0)
                        FROM AgendamentoProdutos ap
                        WHERE ap.AgendamentoId = @AgendamentoId
                          AND ap.ProdutoId = @Id
                          AND ap.Reservado = 1
                    ) AS ReservadoNoAgendamento,
                    (
                        SELECT COALESCE(SUM(ap.Quantidade), 0)
                        FROM AgendamentoProdutos ap
                        INNER JOIN Agendamentos a ON a.Id = ap.AgendamentoId
                        WHERE ap.ProdutoId = @Id
                          AND ap.Reservado = 1
                          AND ap.Quantidade > 0
                          AND COALESCE(a.Status, '') NOT IN ('Cancelado', 'Finalizado')
                    ) AS ReservasAtivas
                FROM Produtos
                WHERE Id = @Id;";
            command.Parameters.AddWithValue("@AgendamentoId", agendamentoId.ToString());
            command.Parameters.AddWithValue("@Id", produto.ProdutoId.ToString());

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                throw new InvalidOperationException($"Produto '{produto.ProdutoNome}' nao encontrado no estoque.");
            }

            return (
                reader.IsDBNull(2) ? produto.ProdutoCodigo : reader.GetString(2),
                reader.IsDBNull(0) ? produto.ProdutoNome : reader.GetString(0),
                reader.IsDBNull(3) ? 0m : Convert.ToDecimal(reader.GetDouble(3)),
                reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                reader.IsDBNull(5) ? 0 : reader.GetInt32(5));
        }

        private static Produto MaterializarProduto(SqliteDataReader reader)
        {
            return new Produto
            {
                Id = ReadGuid(reader, 0),
                Codigo = ReadString(reader, 1),
                Nome = ReadString(reader, 2),
                Descricao = ReadString(reader, 3),
                Categoria = ReadString(reader, 4),
                Marca = ReadString(reader, 5),
                Modelo = ReadString(reader, 6),
                FornecedorId = ReadNullableGuid(reader, 7),
                Fornecedor = ReadString(reader, 8),
                CNPJFornecedor = ReadString(reader, 9),
                ContatoFornecedor = ReadString(reader, 10),
                TelefoneFornecedor = ReadString(reader, 11),
                QuantidadeEstoque = ReadInt(reader, 12),
                QuantidadeMinima = ReadInt(reader, 13),
                QuantidadeMaxima = ReadInt(reader, 14),
                Localizacao = ReadString(reader, 15),
                Prateleira = ReadString(reader, 16),
                Gaveta = ReadString(reader, 17),
                PrecoCompra = ReadDecimal(reader, 18),
                PrecoVenda = ReadDecimal(reader, 19),
                MargemLucro = ReadDecimal(reader, 20),
                ValorTotalEstoque = ReadDecimal(reader, 21),
                UnidadeMedida = ReadString(reader, 22),
                Peso = ReadString(reader, 23),
                Dimensoes = ReadString(reader, 24),
                Cor = ReadString(reader, 25),
                Material = ReadString(reader, 26),
                CodigoBarras = ReadString(reader, 27),
                SKU = ReadString(reader, 28),
                NCMS = ReadString(reader, 29),
                CEST = ReadString(reader, 30),
                CFOP = ReadString(reader, 31),
                Ativo = ReadBool(reader, 32),
                ProdutoPerecivel = ReadBool(reader, 33),
                DataValidade = ReadNullableDate(reader, 34),
                DataFabricacao = ReadNullableDate(reader, 35),
                Lote = ReadString(reader, 36),
                DataCadastro = ReadDate(reader, 37, DateTime.Now),
                DataUltimaCompra = ReadNullableDate(reader, 38),
                DataUltimaVenda = ReadNullableDate(reader, 39),
                DataUltimaAtualizacao = ReadNullableDate(reader, 40),
                Observacoes = ReadString(reader, 41),
                ImagemUrl = ReadString(reader, 42),
                Anexos = ReadString(reader, 43),
                TotalVendas = ReadInt(reader, 44),
                TotalFaturado = ReadDecimal(reader, 45),
                VendasUltimoMes = ReadInt(reader, 46),
                VendasUltimoTrimestre = ReadInt(reader, 47)
            };
        }

        private void ValidarProdutoParaPersistencia(SqliteConnection connection, Produto produto, Produto? anterior)
        {
            if (string.IsNullOrWhiteSpace(produto.Nome))
            {
                throw new InvalidOperationException("O nome do produto e obrigatorio.");
            }

            if (produto.QuantidadeMinima < 0)
            {
                throw new InvalidOperationException("A quantidade minima do produto nao pode ser negativa.");
            }

            if (produto.QuantidadeMaxima < 0)
            {
                throw new InvalidOperationException("A quantidade maxima do produto nao pode ser negativa.");
            }

            if (produto.QuantidadeMaxima > 0 && produto.QuantidadeMaxima < produto.QuantidadeMinima)
            {
                throw new InvalidOperationException("A quantidade maxima nao pode ser menor que a minima.");
            }

            if (produto.PrecoCompra < 0 || produto.PrecoVenda < 0)
            {
                throw new InvalidOperationException("Os precos do produto nao podem ser negativos.");
            }

            ValidarDuplicidade(connection, produto);
            ValidarEstoqueNegativo(produto, anterior);
        }

        private static void ResolverFornecedorPrincipal(SqliteConnection connection, Produto produto)
        {
            if (produto.FornecedorId.HasValue && produto.FornecedorId.Value != Guid.Empty)
            {
                return;
            }

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    Id,
                    NomeFantasia,
                    CNPJ
                FROM Fornecedores
                WHERE (
                        @CnpjNormalizado <> ''
                    AND replace(replace(replace(lower(COALESCE(CNPJ, '')), '.', ''), '/', ''), '-', '') = @CnpjNormalizado
                    )
                   OR (
                        @NomeFornecedor <> ''
                    AND (
                           lower(trim(COALESCE(NomeFantasia, ''))) = lower(trim(@NomeFornecedor))
                        OR lower(trim(COALESCE(RazaoSocial, ''))) = lower(trim(@NomeFornecedor))
                    )
                   )
                ORDER BY Ativo DESC, Nota DESC, DataCadastro DESC
                LIMIT 1;";
            command.Parameters.AddWithValue("@CnpjNormalizado", NormalizarDocumento(produto.CNPJFornecedor));
            command.Parameters.AddWithValue("@NomeFornecedor", produto.Fornecedor ?? string.Empty);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return;
            }

            produto.FornecedorId = ReadNullableGuid(reader, 0);

            if (string.IsNullOrWhiteSpace(produto.Fornecedor))
            {
                produto.Fornecedor = ReadString(reader, 1);
            }

            if (string.IsNullOrWhiteSpace(produto.CNPJFornecedor))
            {
                produto.CNPJFornecedor = ReadString(reader, 2);
            }
        }

        private void ValidarDuplicidade(SqliteConnection connection, Produto produto)
        {
            var ignorarId = produto.Id == Guid.Empty ? string.Empty : produto.Id.ToString();

            if (!string.IsNullOrWhiteSpace(produto.Codigo))
            {
                using var codigoCommand = connection.CreateCommand();
                codigoCommand.CommandText = @"
                    SELECT 1
                    FROM Produtos
                    WHERE lower(trim(Codigo)) = lower(trim(@Codigo))
                      AND (@IgnorarId = '' OR Id <> @IgnorarId)
                    LIMIT 1;";
                codigoCommand.Parameters.AddWithValue("@Codigo", produto.Codigo);
                codigoCommand.Parameters.AddWithValue("@IgnorarId", ignorarId);

                if (codigoCommand.ExecuteScalar() != null)
                {
                    throw new InvalidOperationException($"Ja existe um produto cadastrado com o codigo '{produto.Codigo}'.");
                }
            }

            if (!string.IsNullOrWhiteSpace(produto.Nome))
            {
                using var nomeCommand = connection.CreateCommand();
                nomeCommand.CommandText = @"
                    SELECT 1
                    FROM Produtos
                    WHERE lower(trim(Nome)) = lower(trim(@Nome))
                      AND (@IgnorarId = '' OR Id <> @IgnorarId)
                    LIMIT 1;";
                nomeCommand.Parameters.AddWithValue("@Nome", produto.Nome);
                nomeCommand.Parameters.AddWithValue("@IgnorarId", ignorarId);

                if (nomeCommand.ExecuteScalar() != null)
                {
                    throw new InvalidOperationException($"Ja existe um produto cadastrado com o nome '{produto.Nome}'.");
                }
            }
        }

        private void ValidarEstoqueNegativo(Produto produto, Produto? anterior)
        {
            if (produto.QuantidadeEstoque >= 0)
            {
                return;
            }

            var permissionService = PermissionService.CriarParaSessaoAtual(_logger, global::PrimoAutoEletrica.App.Database);
            if (!permissionService.TemPermissaoCodigo("ESTOQUE_PERMITIR_NEGATIVO"))
            {
                throw new InvalidOperationException("Estoque negativo exige permissao gerencial. Solicite um gerente ou administrador.");
            }

            if (anterior != null && anterior.QuantidadeEstoque == produto.QuantidadeEstoque)
            {
                return;
            }

            var detalhes = $"Codigo={produto.Codigo}; Nome={produto.Nome}; EstoqueAnterior={anterior?.QuantidadeEstoque.ToString() ?? "N/A"}; EstoqueNovo={produto.QuantidadeEstoque}";
            _logger.LogWarning($"Estoque negativo autorizado para o produto '{produto.Nome}'. {detalhes}");
            global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                "Estoque",
                "EstoqueNegativoAutorizado",
                "Produto",
                produto.Id.ToString(),
                detalhes);
        }

        private static void NormalizarProduto(Produto produto)
        {
            produto.Codigo = produto.Codigo?.Trim() ?? string.Empty;
            produto.Nome = produto.Nome?.Trim() ?? string.Empty;
            produto.Descricao = produto.Descricao?.Trim() ?? string.Empty;
            produto.Categoria = produto.Categoria?.Trim() ?? string.Empty;
            produto.Marca = produto.Marca?.Trim() ?? string.Empty;
            produto.Modelo = produto.Modelo?.Trim() ?? string.Empty;
            produto.Localizacao = produto.Localizacao?.Trim() ?? string.Empty;
            produto.Prateleira = produto.Prateleira?.Trim() ?? string.Empty;
            produto.Gaveta = produto.Gaveta?.Trim() ?? string.Empty;
            produto.Fornecedor = produto.Fornecedor?.Trim() ?? string.Empty;
            produto.CNPJFornecedor = produto.CNPJFornecedor?.Trim() ?? string.Empty;
            produto.ContatoFornecedor = produto.ContatoFornecedor?.Trim() ?? string.Empty;
            produto.TelefoneFornecedor = produto.TelefoneFornecedor?.Trim() ?? string.Empty;
            produto.Observacoes = produto.Observacoes?.Trim() ?? string.Empty;
            produto.Anexos = ProdutoMediaService.SerializeAttachmentPaths(ProdutoMediaService.DeserializeAttachmentPaths(produto.Anexos));
        }

        private static void AddProdutoParameters(SqliteCommand command, Produto produto)
        {
            command.Parameters.AddWithValue("@Id", produto.Id.ToString());
            command.Parameters.AddWithValue("@Codigo", ToDbNullableString(produto.Codigo));
            command.Parameters.AddWithValue("@Nome", ToDbNullableString(produto.Nome));
            command.Parameters.AddWithValue("@Descricao", ToDbNullableString(produto.Descricao));
            command.Parameters.AddWithValue("@Categoria", ToDbNullableString(produto.Categoria));
            command.Parameters.AddWithValue("@Marca", ToDbNullableString(produto.Marca));
            command.Parameters.AddWithValue("@Modelo", ToDbNullableString(produto.Modelo));
            command.Parameters.AddWithValue("@FornecedorId", produto.FornecedorId.HasValue ? produto.FornecedorId.Value.ToString() : DBNull.Value);
            command.Parameters.AddWithValue("@Fornecedor", ToDbNullableString(produto.Fornecedor));
            command.Parameters.AddWithValue("@CNPJFornecedor", ToDbNullableString(produto.CNPJFornecedor));
            command.Parameters.AddWithValue("@ContatoFornecedor", ToDbNullableString(produto.ContatoFornecedor));
            command.Parameters.AddWithValue("@TelefoneFornecedor", ToDbNullableString(produto.TelefoneFornecedor));
            command.Parameters.AddWithValue("@QuantidadeEstoque", produto.QuantidadeEstoque);
            command.Parameters.AddWithValue("@QuantidadeMinima", produto.QuantidadeMinima);
            command.Parameters.AddWithValue("@QuantidadeMaxima", produto.QuantidadeMaxima);
            command.Parameters.AddWithValue("@Localizacao", ToDbNullableString(produto.Localizacao));
            command.Parameters.AddWithValue("@Prateleira", ToDbNullableString(produto.Prateleira));
            command.Parameters.AddWithValue("@Gaveta", ToDbNullableString(produto.Gaveta));
            command.Parameters.AddWithValue("@PrecoCompra", produto.PrecoCompra);
            command.Parameters.AddWithValue("@PrecoVenda", produto.PrecoVenda);
            command.Parameters.AddWithValue("@MargemLucro", produto.MargemLucro);
            command.Parameters.AddWithValue("@ValorTotalEstoque", produto.ValorTotalEstoque);
            command.Parameters.AddWithValue("@UnidadeMedida", ToDbNullableString(produto.UnidadeMedida));
            command.Parameters.AddWithValue("@Peso", ToDbNullableString(produto.Peso));
            command.Parameters.AddWithValue("@Dimensoes", ToDbNullableString(produto.Dimensoes));
            command.Parameters.AddWithValue("@Cor", ToDbNullableString(produto.Cor));
            command.Parameters.AddWithValue("@Material", ToDbNullableString(produto.Material));
            command.Parameters.AddWithValue("@CodigoBarras", ToDbNullableString(produto.CodigoBarras));
            command.Parameters.AddWithValue("@SKU", ToDbNullableString(produto.SKU));
            command.Parameters.AddWithValue("@NCMS", ToDbNullableString(produto.NCMS));
            command.Parameters.AddWithValue("@CEST", ToDbNullableString(produto.CEST));
            command.Parameters.AddWithValue("@CFOP", ToDbNullableString(produto.CFOP));
            command.Parameters.AddWithValue("@Ativo", produto.Ativo ? 1 : 0);
            command.Parameters.AddWithValue("@ProdutoPerecivel", produto.ProdutoPerecivel ? 1 : 0);
            command.Parameters.AddWithValue("@DataValidade", ToDbNullableDate(produto.DataValidade, "yyyy-MM-dd"));
            command.Parameters.AddWithValue("@DataFabricacao", ToDbNullableDate(produto.DataFabricacao, "yyyy-MM-dd"));
            command.Parameters.AddWithValue("@Lote", ToDbNullableString(produto.Lote));
            command.Parameters.AddWithValue("@DataCadastro", produto.DataCadastro.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@DataUltimaCompra", ToDbNullableDate(produto.DataUltimaCompra, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@DataUltimaVenda", ToDbNullableDate(produto.DataUltimaVenda, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@DataUltimaAtualizacao", ToDbNullableDate(produto.DataUltimaAtualizacao, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@Observacoes", ToDbNullableString(produto.Observacoes));
            command.Parameters.AddWithValue("@ImagemUrl", ToDbNullableString(produto.ImagemUrl));
            command.Parameters.AddWithValue("@Anexos", ToDbNullableString(produto.Anexos));
            command.Parameters.AddWithValue("@TotalVendas", produto.TotalVendas);
            command.Parameters.AddWithValue("@TotalFaturado", produto.TotalFaturado);
            command.Parameters.AddWithValue("@VendasUltimoMes", produto.VendasUltimoMes);
            command.Parameters.AddWithValue("@VendasUltimoTrimestre", produto.VendasUltimoTrimestre);
        }

        private static object ToDbNullableString(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
        }

        private static object ToDbNullableDate(DateTime? value, string format)
        {
            return value.HasValue ? value.Value.ToString(format) : DBNull.Value;
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

        private static Guid ReadGuid(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) || !Guid.TryParse(Convert.ToString(reader.GetValue(index)), out var value)
                ? Guid.NewGuid()
                : value;
        }

        private static Guid? ReadNullableGuid(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) || !Guid.TryParse(Convert.ToString(reader.GetValue(index)), out var value)
                ? null
                : value;
        }

        private static DateTime ReadDate(SqliteDataReader reader, int index, DateTime fallback)
        {
            return reader.IsDBNull(index) || !DateTime.TryParse(Convert.ToString(reader.GetValue(index)), out var value)
                ? fallback
                : value;
        }

        private static DateTime? ReadNullableDate(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) || !DateTime.TryParse(Convert.ToString(reader.GetValue(index)), out var value)
                ? null
                : value;
        }

        private static string? CriarSnapshot(Produto? produto)
        {
            return produto == null
                ? null
                : $"Codigo={produto.Codigo}; Nome={produto.Nome}; Fornecedor={produto.Fornecedor}; Estoque={produto.QuantidadeEstoque}; PrecoCompra={produto.PrecoCompra:C}; PrecoVenda={produto.PrecoVenda:C}; Anexos={ProdutoMediaService.DeserializeAttachmentPaths(produto.Anexos).Count}; Ativo={produto.Ativo}";
        }

        private static string NormalizarDocumento(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return string.Empty;
            }

            return valor.Trim()
                .Replace(".", string.Empty, StringComparison.Ordinal)
                .Replace("/", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .ToLowerInvariant();
        }

        private static void RegistrarAuditoria(string acao, Produto? produto, string? anterior, string? novo, Guid? idOverride = null)
        {
            global::PrimoAutoEletrica.App.Audit.Registrar(
                categoria: "Estoque",
                acao: acao,
                entidade: "Produto",
                entidadeId: (produto?.Id ?? idOverride ?? Guid.Empty).ToString(),
                detalhes: produto == null ? "Produto nao localizado." : $"Codigo={produto.Codigo}; Nome={produto.Nome}",
                valorAnterior: anterior,
                valorNovo: novo);
        }
    }
}
