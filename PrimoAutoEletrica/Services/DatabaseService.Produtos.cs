using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace PrimoAutoEletrica.Services
{
    public partial class DatabaseService
    {
        // CRUD e importacao de produtos mantidos em partial para preservar a API atual.

        // =====================================================
        // PRODUTOS - CRUD
        // =====================================================
        public List<Produto> ObterTodosProdutos()
        {
            if (UsarProdutoRepositoryBridge())
                return global::PrimoAutoEletrica.App.Repositories.Produtos.ObterTodos();

            var produtos = new List<Produto>();

            try
            {
                using var connection = GetConnection();
                connection.Open();

                using var command = connection.CreateCommand();

                command.CommandText = @"
            SELECT
                Id,
                Codigo,
                Nome,
                Descricao,
                Categoria,
                Marca,
                Modelo,
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
                VendasUltimoTrimestre
            FROM Produtos
            ORDER BY DataCadastro DESC
        ";

                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    try
                    {
                        var produto = new Produto
                        {
                            Id = reader.IsDBNull(0)
                                ? Guid.NewGuid()
                                : Guid.Parse(reader.GetString(0)),

                            Codigo = reader.IsDBNull(1)
                                ? ""
                                : reader.GetString(1),

                            Nome = reader.IsDBNull(2)
                                ? ""
                                : reader.GetString(2),

                            Descricao = reader.IsDBNull(3)
                                ? ""
                                : reader.GetString(3),

                            Categoria = reader.IsDBNull(4)
                                ? ""
                                : reader.GetString(4),

                            Marca = reader.IsDBNull(5)
                                ? ""
                                : reader.GetString(5),

                            Modelo = reader.IsDBNull(6)
                                ? ""
                                : reader.GetString(6),

                            Fornecedor = reader.IsDBNull(7)
                                ? ""
                                : reader.GetString(7),

                            CNPJFornecedor = reader.IsDBNull(8)
                                ? ""
                                : reader.GetString(8),

                            ContatoFornecedor = reader.IsDBNull(9)
                                ? ""
                                : reader.GetString(9),

                            TelefoneFornecedor = reader.IsDBNull(10)
                                ? ""
                                : reader.GetString(10),

                            QuantidadeEstoque = reader.IsDBNull(11)
                                ? 0
                                : reader.GetInt32(11),

                            QuantidadeMinima = reader.IsDBNull(12)
                                ? 0
                                : reader.GetInt32(12),

                            QuantidadeMaxima = reader.IsDBNull(13)
                                ? 0
                                : reader.GetInt32(13),

                            Localizacao = reader.IsDBNull(14)
                                ? ""
                                : reader.GetString(14),

                            Prateleira = reader.IsDBNull(15)
                                ? ""
                                : reader.GetString(15),

                            Gaveta = reader.IsDBNull(16)
                                ? ""
                                : reader.GetString(16),

                            PrecoCompra = reader.IsDBNull(17)
                                ? 0
                                : Convert.ToDecimal(reader.GetDouble(17)),

                            PrecoVenda = reader.IsDBNull(18)
                                ? 0
                                : Convert.ToDecimal(reader.GetDouble(18)),

                            MargemLucro = reader.IsDBNull(19)
                                ? 0
                                : Convert.ToDecimal(reader.GetDouble(19)),

                            ValorTotalEstoque = reader.IsDBNull(20)
                                ? 0
                                : Convert.ToDecimal(reader.GetDouble(20)),

                            UnidadeMedida = reader.IsDBNull(21)
                                ? ""
                                : reader.GetString(21),

                            Peso = reader.IsDBNull(22)
                                ? ""
                                : reader.GetString(22),

                            Dimensoes = reader.IsDBNull(23)
                                ? ""
                                : reader.GetString(23),

                            Cor = reader.IsDBNull(24)
                                ? ""
                                : reader.GetString(24),

                            Material = reader.IsDBNull(25)
                                ? ""
                                : reader.GetString(25),

                            CodigoBarras = reader.IsDBNull(26)
                                ? ""
                                : reader.GetString(26),

                            SKU = reader.IsDBNull(27)
                                ? ""
                                : reader.GetString(27),

                            NCMS = reader.IsDBNull(28)
                                ? ""
                                : reader.GetString(28),

                            CEST = reader.IsDBNull(29)
                                ? ""
                                : reader.GetString(29),

                            CFOP = reader.IsDBNull(30)
                                ? ""
                                : reader.GetString(30),

                            Ativo = !reader.IsDBNull(31)
                                && reader.GetBoolean(31),

                            ProdutoPerecivel = !reader.IsDBNull(32)
                                && reader.GetBoolean(32),

                            DataValidade = reader.IsDBNull(33)
                                ? null
                                : DateTime.Parse(reader.GetString(33)),

                            DataFabricacao = reader.IsDBNull(34)
                                ? null
                                : DateTime.Parse(reader.GetString(34)),

                            Lote = reader.IsDBNull(35)
                                ? ""
                                : reader.GetString(35),

                            DataCadastro = reader.IsDBNull(36)
                                ? DateTime.Now
                                : DateTime.Parse(reader.GetString(36)),

                            DataUltimaCompra = reader.IsDBNull(37)
                                ? null
                                : DateTime.Parse(reader.GetString(37)),

                            DataUltimaVenda = reader.IsDBNull(38)
                                ? null
                                : DateTime.Parse(reader.GetString(38)),

                            DataUltimaAtualizacao = reader.IsDBNull(39)
                                ? null
                                : DateTime.Parse(reader.GetString(39)),

                            Observacoes = reader.IsDBNull(40)
                                ? ""
                                : reader.GetString(40),

                            ImagemUrl = reader.IsDBNull(41)
                                ? ""
                                : reader.GetString(41),

                            Anexos = reader.IsDBNull(42)
                                ? ""
                                : reader.GetString(42),

                            TotalVendas = reader.IsDBNull(43)
                                ? 0
                                : reader.GetInt32(43),

                            TotalFaturado = reader.IsDBNull(44)
                                ? 0
                                : Convert.ToDecimal(reader.GetDouble(44)),

                            VendasUltimoMes = reader.IsDBNull(45)
                                ? 0
                                : reader.GetInt32(45),

                            VendasUltimoTrimestre = reader.IsDBNull(46)
                                ? 0
                                : reader.GetInt32(46)
                        };

                        produtos.Add(produto);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Erro ao materializar um produto durante a leitura da listagem.", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Erro ao obter produtos no banco de dados.", ex);
            }

            return produtos;
        }
        public Produto? ObterProdutoPorId(Guid id)
        {
            if (UsarProdutoRepositoryBridge())
                return global::PrimoAutoEletrica.App.Repositories.Produtos.ObterPorId(id);

            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    Id,
                    Codigo,
                    Nome,
                    Descricao,
                    Categoria,
                    Marca,
                    Modelo,
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
                    VendasUltimoTrimestre
                FROM Produtos
                WHERE Id = @Id
                LIMIT 1
            ";

            command.Parameters.AddWithValue("@Id", id.ToString());

            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            return new Produto
            {
                Id = Guid.Parse(reader.GetString(0)),
                Codigo = reader.GetString(1),
                Nome = reader.GetString(2),
                Descricao = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Categoria = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Marca = reader.IsDBNull(5) ? "" : reader.GetString(5),
                Modelo = reader.IsDBNull(6) ? "" : reader.GetString(6),
                Fornecedor = reader.IsDBNull(7) ? "" : reader.GetString(7),
                CNPJFornecedor = reader.IsDBNull(8) ? "" : reader.GetString(8),
                ContatoFornecedor = reader.IsDBNull(9) ? "" : reader.GetString(9),
                TelefoneFornecedor = reader.IsDBNull(10) ? "" : reader.GetString(10),
                QuantidadeEstoque = reader.GetInt32(11),
                QuantidadeMinima = reader.GetInt32(12),
                QuantidadeMaxima = reader.GetInt32(13),
                Localizacao = reader.IsDBNull(14) ? "" : reader.GetString(14),
                Prateleira = reader.IsDBNull(15) ? "" : reader.GetString(15),
                Gaveta = reader.IsDBNull(16) ? "" : reader.GetString(16),
                PrecoCompra = reader.IsDBNull(17) ? 0 : Convert.ToDecimal(reader.GetDouble(17)),
                PrecoVenda = reader.IsDBNull(18) ? 0 : Convert.ToDecimal(reader.GetDouble(18)),
                MargemLucro = reader.IsDBNull(19) ? 0 : Convert.ToDecimal(reader.GetDouble(19)),
                ValorTotalEstoque = reader.IsDBNull(20) ? 0 : Convert.ToDecimal(reader.GetDouble(20)),
                UnidadeMedida = reader.IsDBNull(21) ? "" : reader.GetString(21),
                Peso = reader.IsDBNull(22) ? "" : reader.GetString(22),
                Dimensoes = reader.IsDBNull(23) ? "" : reader.GetString(23),
                Cor = reader.IsDBNull(24) ? "" : reader.GetString(24),
                Material = reader.IsDBNull(25) ? "" : reader.GetString(25),
                CodigoBarras = reader.IsDBNull(26) ? "" : reader.GetString(26),
                SKU = reader.IsDBNull(27) ? "" : reader.GetString(27),
                NCMS = reader.IsDBNull(28) ? "" : reader.GetString(28),
                CEST = reader.IsDBNull(29) ? "" : reader.GetString(29),
                CFOP = reader.IsDBNull(30) ? "" : reader.GetString(30),
                Ativo = reader.GetBoolean(31),
                ProdutoPerecivel = reader.GetBoolean(32),
                DataValidade = reader.IsDBNull(33) ? null : DateTime.Parse(reader.GetString(33)),
                DataFabricacao = reader.IsDBNull(34) ? null : DateTime.Parse(reader.GetString(34)),
                Lote = reader.IsDBNull(35) ? "" : reader.GetString(35),
                DataCadastro = DateTime.Parse(reader.GetString(36)),
                DataUltimaCompra = reader.IsDBNull(37) ? null : DateTime.Parse(reader.GetString(37)),
                DataUltimaVenda = reader.IsDBNull(38) ? null : DateTime.Parse(reader.GetString(38)),
                DataUltimaAtualizacao = reader.IsDBNull(39) ? null : DateTime.Parse(reader.GetString(39)),
                Observacoes = reader.IsDBNull(40) ? "" : reader.GetString(40),
                ImagemUrl = reader.IsDBNull(41) ? "" : reader.GetString(41),
                Anexos = reader.IsDBNull(42) ? "" : reader.GetString(42),
                TotalVendas = reader.GetInt32(43),
                TotalFaturado = reader.IsDBNull(44) ? 0 : Convert.ToDecimal(reader.GetDouble(44)),
                VendasUltimoMes = reader.GetInt32(45),
                VendasUltimoTrimestre = reader.GetInt32(46)
            };
        }

        public void InserirProduto(Produto produto)
        {
            if (UsarProdutoRepositoryBridge())
            {
                global::PrimoAutoEletrica.App.Repositories.Produtos.Inserir(produto);
                return;
            }

            using var connection = GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = @"
        INSERT INTO Produtos
        (
            Id,
            Codigo,
            Nome,
            Descricao,
            Categoria,
            Marca,
            Modelo,
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
            VendasUltimoTrimestre
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
        )
    ";

            // =========================
            // PARÂMETROS
            // =========================

            command.Parameters.AddWithValue("@Id", produto.Id.ToString());

            command.Parameters.AddWithValue(
                "@Codigo",
                string.IsNullOrWhiteSpace(produto.Codigo)
                    ? DBNull.Value
                    : produto.Codigo
            );

            command.Parameters.AddWithValue(
                "@Nome",
                string.IsNullOrWhiteSpace(produto.Nome)
                    ? DBNull.Value
                    : produto.Nome
            );

            command.Parameters.AddWithValue(
                "@Descricao",
                string.IsNullOrWhiteSpace(produto.Descricao)
                    ? DBNull.Value
                    : produto.Descricao
            );

            command.Parameters.AddWithValue(
                "@Categoria",
                string.IsNullOrWhiteSpace(produto.Categoria)
                    ? DBNull.Value
                    : produto.Categoria
            );

            command.Parameters.AddWithValue(
                "@Marca",
                string.IsNullOrWhiteSpace(produto.Marca)
                    ? DBNull.Value
                    : produto.Marca
            );

            command.Parameters.AddWithValue(
                "@Modelo",
                string.IsNullOrWhiteSpace(produto.Modelo)
                    ? DBNull.Value
                    : produto.Modelo
            );

            command.Parameters.AddWithValue(
                "@Fornecedor",
                string.IsNullOrWhiteSpace(produto.Fornecedor)
                    ? DBNull.Value
                    : produto.Fornecedor
            );

            command.Parameters.AddWithValue(
                "@CNPJFornecedor",
                string.IsNullOrWhiteSpace(produto.CNPJFornecedor)
                    ? DBNull.Value
                    : produto.CNPJFornecedor
            );

            command.Parameters.AddWithValue(
                "@ContatoFornecedor",
                string.IsNullOrWhiteSpace(produto.ContatoFornecedor)
                    ? DBNull.Value
                    : produto.ContatoFornecedor
            );

            command.Parameters.AddWithValue(
                "@TelefoneFornecedor",
                string.IsNullOrWhiteSpace(produto.TelefoneFornecedor)
                    ? DBNull.Value
                    : produto.TelefoneFornecedor
            );

            command.Parameters.AddWithValue("@QuantidadeEstoque", produto.QuantidadeEstoque);
            command.Parameters.AddWithValue("@QuantidadeMinima", produto.QuantidadeMinima);
            command.Parameters.AddWithValue("@QuantidadeMaxima", produto.QuantidadeMaxima);

            command.Parameters.AddWithValue(
                "@Localizacao",
                string.IsNullOrWhiteSpace(produto.Localizacao)
                    ? DBNull.Value
                    : produto.Localizacao
            );

            command.Parameters.AddWithValue(
                "@Prateleira",
                string.IsNullOrWhiteSpace(produto.Prateleira)
                    ? DBNull.Value
                    : produto.Prateleira
            );

            command.Parameters.AddWithValue(
                "@Gaveta",
                string.IsNullOrWhiteSpace(produto.Gaveta)
                    ? DBNull.Value
                    : produto.Gaveta
            );

            command.Parameters.AddWithValue("@PrecoCompra", produto.PrecoCompra);
            command.Parameters.AddWithValue("@PrecoVenda", produto.PrecoVenda);
            command.Parameters.AddWithValue("@MargemLucro", produto.MargemLucro);
            command.Parameters.AddWithValue("@ValorTotalEstoque", produto.ValorTotalEstoque);

            command.Parameters.AddWithValue(
                "@UnidadeMedida",
                string.IsNullOrWhiteSpace(produto.UnidadeMedida)
                    ? DBNull.Value
                    : produto.UnidadeMedida
            );

            command.Parameters.AddWithValue("@Peso", produto.Peso);

            command.Parameters.AddWithValue(
                "@Dimensoes",
                string.IsNullOrWhiteSpace(produto.Dimensoes)
                    ? DBNull.Value
                    : produto.Dimensoes
            );

            command.Parameters.AddWithValue(
                "@Cor",
                string.IsNullOrWhiteSpace(produto.Cor)
                    ? DBNull.Value
                    : produto.Cor
            );

            command.Parameters.AddWithValue(
                "@Material",
                string.IsNullOrWhiteSpace(produto.Material)
                    ? DBNull.Value
                    : produto.Material
            );

            command.Parameters.AddWithValue(
                "@CodigoBarras",
                string.IsNullOrWhiteSpace(produto.CodigoBarras)
                    ? DBNull.Value
                    : produto.CodigoBarras
            );

            command.Parameters.AddWithValue(
                "@SKU",
                string.IsNullOrWhiteSpace(produto.SKU)
                    ? DBNull.Value
                    : produto.SKU
            );

            command.Parameters.AddWithValue(
                "@NCMS",
                string.IsNullOrWhiteSpace(produto.NCMS)
                    ? DBNull.Value
                    : produto.NCMS
            );

            command.Parameters.AddWithValue(
                "@CEST",
                string.IsNullOrWhiteSpace(produto.CEST)
                    ? DBNull.Value
                    : produto.CEST
            );

            command.Parameters.AddWithValue(
                "@CFOP",
                string.IsNullOrWhiteSpace(produto.CFOP)
                    ? DBNull.Value
                    : produto.CFOP
            );

            command.Parameters.AddWithValue("@Ativo", produto.Ativo ? 1 : 0);

            command.Parameters.AddWithValue(
                "@ProdutoPerecivel",
                produto.ProdutoPerecivel ? 1 : 0
            );

            command.Parameters.AddWithValue(
                "@DataValidade",
                produto.DataValidade.HasValue
                    ? produto.DataValidade.Value.ToString("yyyy-MM-dd")
                    : DBNull.Value
            );

            command.Parameters.AddWithValue(
                "@DataFabricacao",
                produto.DataFabricacao.HasValue
                    ? produto.DataFabricacao.Value.ToString("yyyy-MM-dd")
                    : DBNull.Value
            );

            command.Parameters.AddWithValue(
                "@Lote",
                string.IsNullOrWhiteSpace(produto.Lote)
                    ? DBNull.Value
                    : produto.Lote
            );

            command.Parameters.AddWithValue(
                "@DataCadastro",
                produto.DataCadastro.ToString("yyyy-MM-dd HH:mm:ss")
            );

            command.Parameters.AddWithValue(
                "@DataUltimaCompra",
                produto.DataUltimaCompra.HasValue
                    ? produto.DataUltimaCompra.Value.ToString("yyyy-MM-dd HH:mm:ss")
                    : DBNull.Value
            );

            command.Parameters.AddWithValue(
                "@DataUltimaVenda",
                produto.DataUltimaVenda.HasValue
                    ? produto.DataUltimaVenda.Value.ToString("yyyy-MM-dd HH:mm:ss")
                    : DBNull.Value
            );

            command.Parameters.AddWithValue(
                "@DataUltimaAtualizacao",
                produto.DataUltimaAtualizacao.HasValue
                    ? produto.DataUltimaAtualizacao.Value.ToString("yyyy-MM-dd HH:mm:ss")
                    : DBNull.Value
            );

            command.Parameters.AddWithValue(
                "@Observacoes",
                string.IsNullOrWhiteSpace(produto.Observacoes)
                    ? DBNull.Value
                    : produto.Observacoes
            );

            command.Parameters.AddWithValue(
                "@ImagemUrl",
                string.IsNullOrWhiteSpace(produto.ImagemUrl)
                    ? DBNull.Value
                    : produto.ImagemUrl
            );

            command.Parameters.AddWithValue(
                "@Anexos",
                string.IsNullOrWhiteSpace(produto.Anexos)
                    ? DBNull.Value
                    : produto.Anexos
            );

            command.Parameters.AddWithValue("@TotalVendas", produto.TotalVendas);
            command.Parameters.AddWithValue("@TotalFaturado", produto.TotalFaturado);
            command.Parameters.AddWithValue("@VendasUltimoMes", produto.VendasUltimoMes);
            command.Parameters.AddWithValue("@VendasUltimoTrimestre", produto.VendasUltimoTrimestre);

            command.ExecuteNonQuery();
            Logger.LogInfo($"Produto criado: {produto.Codigo} - {produto.Nome}");
        }
        public void AtualizarProduto(Produto produto)
        {
            if (UsarProdutoRepositoryBridge())
            {
                global::PrimoAutoEletrica.App.Repositories.Produtos.Atualizar(produto);
                return;
            }

            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Produtos
                SET
                    Codigo = @Codigo,
                    Nome = @Nome,
                    Descricao = @Descricao,
                    Categoria = @Categoria,
                    Marca = @Marca,
                    Modelo = @Modelo,
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
                    VendasUltimoTrimestre = @VendasUltimoTrimestre
                WHERE Id = @Id
            ";

            command.Parameters.AddWithValue("@Codigo", ToDbNullableString(produto.Codigo));
            command.Parameters.AddWithValue("@Nome", ToDbNullableString(produto.Nome));
            command.Parameters.AddWithValue("@Descricao", ToDbNullableString(produto.Descricao));
            command.Parameters.AddWithValue("@Categoria", ToDbNullableString(produto.Categoria));
            command.Parameters.AddWithValue("@Marca", ToDbNullableString(produto.Marca));
            command.Parameters.AddWithValue("@Modelo", ToDbNullableString(produto.Modelo));
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
            command.Parameters.AddWithValue("@Id", produto.Id.ToString());

            command.ExecuteNonQuery();
            Logger.LogInfo($"Produto atualizado: {produto.Codigo} - {produto.Nome}");
        }

        public void ExcluirProduto(Guid id)
        {
            if (UsarProdutoRepositoryBridge())
            {
                global::PrimoAutoEletrica.App.Repositories.Produtos.Excluir(id);
                return;
            }

            var produto = ObterProdutoPorId(id);

            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                DELETE FROM Produtos
                WHERE Id = @Id
            ";

            command.Parameters.AddWithValue("@Id", id.ToString());

            command.ExecuteNonQuery();
            Logger.LogInfo($"Produto excluído: {id}");
        }

        public void InserirProdutosEmMassa(string caminhoArquivo)
        {
            if (UsarProdutoRepositoryBridge())
            {
                global::PrimoAutoEletrica.App.Repositories.Produtos.InserirEmMassa(caminhoArquivo);
                return;
            }

            if (!File.Exists(caminhoArquivo))
                throw new FileNotFoundException($"Arquivo não encontrado: {caminhoArquivo}");

            var linhas = File.ReadAllLines(caminhoArquivo);
            int produtosInseridos = 0;

            foreach (var linha in linhas)
            {
                // Ignorar linhas de comentário ou vazias
                if (string.IsNullOrWhiteSpace(linha) || linha.StartsWith("#"))
                    continue;

                var partes = linha.Split(';');

                if (partes.Length < 13)
                    continue;

                try
                {
                    var produto = new Produto
                    {
                        Id = Guid.NewGuid(),
                        Codigo = partes[0], // Usar o nome como código temporariamente
                        Nome = partes[0], // A primeira coluna é o NOME
                        Descricao = partes.Length > 12 ? partes[12] : "",
                        Categoria = partes[1],
                        Marca = partes[2],
                        Modelo = partes[3],
                        QuantidadeEstoque = int.TryParse(partes[6], out int quantidade) ? quantidade : 0,
                        QuantidadeMinima = int.TryParse(partes[7], out int minima) ? minima : 0,
                        QuantidadeMaxima = 0,
                        Localizacao = partes[8],
                        Prateleira = partes[9],
                        Gaveta = partes[10],
                        PrecoCompra = decimal.TryParse(partes[4], out decimal precoCompra) ? precoCompra : 0,
                        PrecoVenda = decimal.TryParse(partes[5], out decimal precoVenda) ? precoVenda : 0,
                        MargemLucro = 0,
                        ValorTotalEstoque = 0,
                        Fornecedor = partes[11],
                        TelefoneFornecedor = "",
                        Ativo = true,
                        ProdutoPerecivel = false,
                        DataCadastro = DateTime.Now,
                        Observacoes = partes.Length > 12 ? partes[12] : "",
                        TotalVendas = 0,
                        TotalFaturado = 0,
                        VendasUltimoMes = 0,
                        VendasUltimoTrimestre = 0
                    };

                    // Calcular valor total do estoque
                    produto.ValorTotalEstoque = produto.QuantidadeEstoque * produto.PrecoCompra;

                    // Calcular margem de lucro
                    if (produto.PrecoVenda > 0)
                    {
                        produto.MargemLucro = ((produto.PrecoVenda - produto.PrecoCompra) / produto.PrecoVenda) * 100;
                    }

                    InserirProduto(produto);
                    produtosInseridos++;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Erro ao inserir produto importado da linha '{linha}'.", ex);
                }
            }

            Logger.LogInfo($"Importacao em massa de produtos concluida com {produtosInseridos} item(ns) inserido(s).");
        }
    }
}
