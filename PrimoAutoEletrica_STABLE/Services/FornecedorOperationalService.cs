using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimoAutoEletrica.Services
{
    public sealed class FornecedorOperationalService
    {
        private readonly DatabaseService _databaseService;
        private readonly IProdutoRepository _produtoRepository;

        public FornecedorOperationalService(DatabaseService? databaseService = null, IProdutoRepository? produtoRepository = null)
        {
            _databaseService = databaseService ?? global::PrimoAutoEletrica.App.Database;
            _produtoRepository = produtoRepository ?? global::PrimoAutoEletrica.App.Repositories.Produtos;
        }

        public FornecedorOperationalInsights CriarInsights(Fornecedor fornecedor, IReadOnlyCollection<Fornecedor>? universo = null)
        {
            var produtos = ObterProdutosRelacionados(fornecedor);
            var produtoFornecedores = SincronizarEObterProdutoFornecedores(fornecedor, produtos);
            var notasRecentes = ObterNotasRecentes(fornecedor);
            var contasPagar = ObterContasPagarVinculadas(fornecedor);
            var contatoPrincipal = fornecedor.Contatos?
                .OrderByDescending(contato => contato.Principal)
                .ThenBy(contato => contato.Nome)
                .FirstOrDefault();

            var categoriaPreferencial = !string.IsNullOrWhiteSpace(fornecedor.CategoriaPreferencial)
                ? fornecedor.CategoriaPreferencial
                : produtos.Where(produto => !string.IsNullOrWhiteSpace(produto.Categoria))
                    .GroupBy(produto => produto.Categoria)
                    .OrderByDescending(grupo => grupo.Count())
                    .ThenBy(grupo => grupo.Key)
                    .Select(grupo => grupo.Key)
                    .FirstOrDefault() ?? fornecedor.Categoria;

            var ultimaCompraProdutos = produtos
                .Where(produto => produto.DataUltimaCompra.HasValue)
                .Select(produto => produto.DataUltimaCompra!.Value)
                .DefaultIfEmpty(DateTime.MinValue)
                .Max();

            var ultimaCompraNotas = notasRecentes
                .Where(item => item.DataReferencia.HasValue)
                .Select(item => item.DataReferencia!.Value)
                .DefaultIfEmpty(DateTime.MinValue)
                .Max();

            var ultimaCompraProdutoFornecedores = produtoFornecedores
                .Where(item => item.DataUltimaCompra.HasValue)
                .Select(item => item.DataUltimaCompra!.Value)
                .DefaultIfEmpty(DateTime.MinValue)
                .Max();

            var ultimaCompra = new[]
                {
                    fornecedor.UltimaCompra,
                    ultimaCompraProdutos == DateTime.MinValue ? null : ultimaCompraProdutos,
                    ultimaCompraNotas == DateTime.MinValue ? null : ultimaCompraNotas,
                    ultimaCompraProdutoFornecedores == DateTime.MinValue ? null : ultimaCompraProdutoFornecedores
                }
                .Where(data => data.HasValue)
                .Select(data => data!.Value)
                .DefaultIfEmpty(DateTime.MinValue)
                .Max();

            var ranking = 0;
            if (universo != null && universo.Count > 0)
            {
                ranking = universo
                    .OrderByDescending(CalcularScoreRanking)
                    .ThenByDescending(item => item.Nota)
                    .ThenByDescending(item => item.UltimaCompra ?? DateTime.MinValue)
                    .Select((item, index) => new { item.Id, Posicao = index + 1 })
                    .FirstOrDefault(item => item.Id == fornecedor.Id)?.Posicao ?? 0;
            }

            var totalComprasProdutoFornecedores = produtoFornecedores.Sum(item => item.ValorCompras);
            var quantidadeComprasProdutoFornecedores = produtoFornecedores.Sum(item => item.QuantidadeCompras);
            var prazoOperacional = CalcularPrazoMedioOperacional(produtoFornecedores, fornecedor.PrazoMedioEntregaDias);
            var totalComprasConsolidado = new[]
                {
                    fornecedor.TotalCompras,
                    notasRecentes.Sum(item => item.ValorTotal),
                    totalComprasProdutoFornecedores
                }
                .Max();

            var alertas = new List<string>();
            if (!fornecedor.Ativo)
            {
                alertas.Add("Fornecedor inativo");
            }

            if (fornecedor.Nota <= 2)
            {
                alertas.Add("Baixa avaliacao");
            }

            if (produtos.Count == 0)
            {
                alertas.Add("Sem produtos vinculados");
            }

            if (prazoOperacional == 0)
            {
                alertas.Add("Prazo de entrega nao definido");
            }

            return new FornecedorOperationalInsights
            {
                ContatoPrincipalNome = contatoPrincipal?.Nome ?? "Nao informado",
                ContatoPrincipalCargo = contatoPrincipal?.Cargo ?? "Sem cargo",
                ContatoPrincipalTelefone = contatoPrincipal?.Telefone ?? fornecedor.Telefone,
                ContatoPrincipalEmail = contatoPrincipal?.Email ?? fornecedor.Email,
                WhatsAppVendedor = string.IsNullOrWhiteSpace(fornecedor.WhatsAppVendedor) ? fornecedor.Celular : fornecedor.WhatsAppVendedor,
                ProdutosRelacionados = produtos.Count,
                ProdutosAtivos = produtos.Count(produto => produto.Ativo),
                ProdutosEstoqueBaixo = produtos.Count(produto => produto.Ativo && produto.QuantidadeEstoque <= produto.QuantidadeMinima),
                ValorEstoqueVinculado = produtos.Sum(produto => produto.ValorTotalEstoque),
                CategoriaPreferencial = string.IsNullOrWhiteSpace(categoriaPreferencial) ? "Nao definida" : categoriaPreferencial,
                RankingGeral = ranking,
                UltimaCompra = ultimaCompra == DateTime.MinValue ? fornecedor.UltimaCompra : ultimaCompra,
                TotalCompras = totalComprasConsolidado,
                TotalComprasProdutoFornecedor = totalComprasProdutoFornecedores,
                QuantidadeComprasProdutoFornecedor = quantidadeComprasProdutoFornecedores,
                TicketMedioCompra = quantidadeComprasProdutoFornecedores > 0
                    ? totalComprasProdutoFornecedores / quantidadeComprasProdutoFornecedores
                    : 0m,
                PrazoMedioEntregaDias = prazoOperacional,
                CondicaoPagamento = MontarCondicaoPagamento(fornecedor),
                ProdutoFornecedores = produtoFornecedores,
                HistoricoNotas = notasRecentes,
                ContasPagarVinculadas = contasPagar.Count,
                ValorContasPagarAberto = contasPagar.Where(item => item.EmAberto).Sum(item => item.Valor),
                ValorContasPagarTotal = contasPagar.Sum(item => item.Valor),
                ContasPagarRecentes = contasPagar,
                ProdutosPrincipais = produtoFornecedores.Count > 0
                    ? produtoFornecedores
                        .OrderByDescending(item => item.DataUltimaCompra ?? DateTime.MinValue)
                        .ThenByDescending(item => item.ValorCompras)
                        .ThenBy(item => item.NomeProduto)
                        .Take(5)
                        .Select(item => new FornecedorProdutoResumo
                        {
                            Nome = item.NomeProduto,
                            Categoria = item.CategoriaProduto,
                            Estoque = produtos.FirstOrDefault(produto => produto.Id == item.ProdutoId)?.QuantidadeEstoque ?? 0,
                            ValorEstoque = produtos.FirstOrDefault(produto => produto.Id == item.ProdutoId)?.ValorTotalEstoque ?? 0m,
                            UltimaCompra = item.DataUltimaCompra,
                            PrecoUltimaCompra = item.PrecoUltimaCompra,
                            QuantidadeCompras = item.QuantidadeCompras,
                            ValorCompras = item.ValorCompras,
                            PrazoEntregaDias = item.PrazoEntregaDias,
                            UltimaNFe = item.NumeroUltimaNFe
                        })
                        .ToList()
                    : produtos
                    .OrderByDescending(produto => produto.DataUltimaCompra ?? DateTime.MinValue)
                    .ThenByDescending(produto => produto.QuantidadeEstoque)
                    .Take(5)
                    .Select(produto => new FornecedorProdutoResumo
                    {
                        Nome = produto.Nome,
                        Categoria = produto.Categoria,
                        Estoque = produto.QuantidadeEstoque,
                        ValorEstoque = produto.ValorTotalEstoque,
                        UltimaCompra = produto.DataUltimaCompra,
                        PrecoUltimaCompra = produto.PrecoCompra,
                        PrazoEntregaDias = fornecedor.PrazoMedioEntregaDias
                    })
                    .ToList(),
                Alertas = alertas
            };
        }

        public List<Produto> ObterProdutosRelacionados(Fornecedor fornecedor)
        {
            var produtos = _produtoRepository.ObterTodos();
            var documento = NormalizarDocumento(fornecedor.CNPJ);
            var nomes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                NormalizarTexto(fornecedor.NomeFantasia),
                NormalizarTexto(fornecedor.RazaoSocial)
            };

            return produtos
                .Where(produto =>
                    produto.FornecedorId == fornecedor.Id ||
                    (!string.IsNullOrWhiteSpace(documento) && NormalizarDocumento(produto.CNPJFornecedor) == documento) ||
                    nomes.Contains(NormalizarTexto(produto.Fornecedor)))
                .OrderByDescending(produto => produto.DataUltimaCompra ?? DateTime.MinValue)
                .ThenBy(produto => produto.Nome)
                .ToList();
        }

        public List<ProdutoFornecedor> SincronizarEObterProdutoFornecedores(Fornecedor fornecedor, IReadOnlyCollection<Produto>? produtosRelacionados = null)
        {
            var produtos = produtosRelacionados?.ToList() ?? ObterProdutosRelacionados(fornecedor);

            if (fornecedor.Id == Guid.Empty || produtos.Count == 0)
            {
                return produtos.Select(produto => CriarProdutoFornecedorFallback(fornecedor, produto)).ToList();
            }

            try
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                MarcarProdutoFornecedoresComoInativos(connection, fornecedor.Id);

                foreach (var produto in produtos)
                {
                    var vinculo = CriarProdutoFornecedorPersistido(connection, fornecedor, produto);
                    SalvarProdutoFornecedor(connection, vinculo);
                }

                return LerProdutoFornecedores(connection, fornecedor.Id);
            }
            catch
            {
                return produtos.Select(produto => CriarProdutoFornecedorFallback(fornecedor, produto)).ToList();
            }
        }

        private static ProdutoFornecedor CriarProdutoFornecedorFallback(Fornecedor fornecedor, Produto produto)
        {
            return new ProdutoFornecedor
            {
                ProdutoId = produto.Id,
                FornecedorId = fornecedor.Id,
                CodigoProduto = produto.Codigo,
                NomeProduto = produto.Nome,
                CategoriaProduto = produto.Categoria,
                PrecoUltimaCompra = produto.PrecoCompra,
                PrazoEntregaDias = fornecedor.PrazoMedioEntregaDias,
                QuantidadeCompras = produto.DataUltimaCompra.HasValue ? 1 : 0,
                DataUltimaCompra = produto.DataUltimaCompra,
                Ativo = produto.Ativo,
                Origem = "Estoque",
                Observacoes = "Vinculo derivado do cadastro de produto."
            };
        }

        private ProdutoFornecedor CriarProdutoFornecedorPersistido(DbConnection connection, Fornecedor fornecedor, Produto produto)
        {
            var compra = ObterResumoComprasProdutoFornecedor(connection, fornecedor, produto);

            return new ProdutoFornecedor
            {
                Id = ObterProdutoFornecedorIdExistente(connection, fornecedor.Id, produto.Id) ?? Guid.NewGuid(),
                ProdutoId = produto.Id,
                FornecedorId = fornecedor.Id,
                CodigoProduto = produto.Codigo,
                NomeProduto = produto.Nome,
                CategoriaProduto = produto.Categoria,
                CodigoFornecedor = compra.CodigoFornecedor,
                PrecoUltimaCompra = compra.PrecoUltimaCompra > 0 ? compra.PrecoUltimaCompra : produto.PrecoCompra,
                QuantidadeUltimaCompra = compra.QuantidadeUltimaCompra,
                PrazoEntregaDias = Math.Max(0, fornecedor.PrazoMedioEntregaDias),
                QuantidadeCompras = compra.QuantidadeCompras,
                ValorCompras = compra.ValorCompras,
                DataUltimaCompra = compra.DataUltimaCompra ?? produto.DataUltimaCompra,
                ChaveUltimaNFe = compra.ChaveUltimaNFe,
                NumeroUltimaNFe = compra.NumeroUltimaNFe,
                Ativo = produto.Ativo,
                Origem = compra.QuantidadeCompras > 0 ? "NF-e" : "Estoque",
                Observacoes = compra.QuantidadeCompras > 0
                    ? "Vinculo sincronizado pelo historico de importacao NF-e."
                    : "Vinculo sincronizado pelo cadastro de produto."
            };
        }

        private static void MarcarProdutoFornecedoresComoInativos(DbConnection connection, Guid fornecedorId)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE ProdutoFornecedores
                SET
                    Ativo = 0,
                    DataUltimaAtualizacao = @DataUltimaAtualizacao
                WHERE FornecedorId = @FornecedorId;";
            AddGuidParameter(command, connection, "@FornecedorId", fornecedorId);
            command.Parameters.AddWithValue("@DataUltimaAtualizacao", ToDbDate(connection, DateTime.Now, "yyyy-MM-dd HH:mm:ss"));
            command.ExecuteNonQuery();
        }

        private static void SalvarProdutoFornecedor(DbConnection connection, ProdutoFornecedor vinculo)
        {
            using var command = connection.CreateCommand();
            command.CommandText = IsSqlServerConnection(connection)
                ? @"
                UPDATE ProdutoFornecedores
                SET
                    CodigoProduto = @CodigoProduto,
                    NomeProduto = @NomeProduto,
                    CategoriaProduto = @CategoriaProduto,
                    CodigoFornecedor = @CodigoFornecedor,
                    PrecoUltimaCompra = @PrecoUltimaCompra,
                    QuantidadeUltimaCompra = @QuantidadeUltimaCompra,
                    PrazoEntregaDias = @PrazoEntregaDias,
                    QuantidadeCompras = @QuantidadeCompras,
                    ValorCompras = @ValorCompras,
                    DataUltimaCompra = @DataUltimaCompra,
                    ChaveUltimaNFe = @ChaveUltimaNFe,
                    NumeroUltimaNFe = @NumeroUltimaNFe,
                    Ativo = @Ativo,
                    Origem = @Origem,
                    Observacoes = @Observacoes,
                    DataUltimaAtualizacao = @DataUltimaAtualizacao
                WHERE ProdutoId = @ProdutoId
                  AND FornecedorId = @FornecedorId;

                IF @@ROWCOUNT = 0
                BEGIN
                    INSERT INTO ProdutoFornecedores
                    (
                        Id,
                        ProdutoId,
                        FornecedorId,
                        CodigoProduto,
                        NomeProduto,
                        CategoriaProduto,
                        CodigoFornecedor,
                        PrecoUltimaCompra,
                        QuantidadeUltimaCompra,
                        PrazoEntregaDias,
                        QuantidadeCompras,
                        ValorCompras,
                        DataUltimaCompra,
                        ChaveUltimaNFe,
                        NumeroUltimaNFe,
                        Ativo,
                        Origem,
                        Observacoes,
                        DataCadastro,
                        DataUltimaAtualizacao
                    )
                    VALUES
                    (
                        @Id,
                        @ProdutoId,
                        @FornecedorId,
                        @CodigoProduto,
                        @NomeProduto,
                        @CategoriaProduto,
                        @CodigoFornecedor,
                        @PrecoUltimaCompra,
                        @QuantidadeUltimaCompra,
                        @PrazoEntregaDias,
                        @QuantidadeCompras,
                        @ValorCompras,
                        @DataUltimaCompra,
                        @ChaveUltimaNFe,
                        @NumeroUltimaNFe,
                        @Ativo,
                        @Origem,
                        @Observacoes,
                        @DataCadastro,
                        @DataUltimaAtualizacao
                    );
                END;"
                : @"
                INSERT INTO ProdutoFornecedores
                (
                    Id,
                    ProdutoId,
                    FornecedorId,
                    CodigoProduto,
                    NomeProduto,
                    CategoriaProduto,
                    CodigoFornecedor,
                    PrecoUltimaCompra,
                    QuantidadeUltimaCompra,
                    PrazoEntregaDias,
                    QuantidadeCompras,
                    ValorCompras,
                    DataUltimaCompra,
                    ChaveUltimaNFe,
                    NumeroUltimaNFe,
                    Ativo,
                    Origem,
                    Observacoes,
                    DataCadastro,
                    DataUltimaAtualizacao
                )
                VALUES
                (
                    @Id,
                    @ProdutoId,
                    @FornecedorId,
                    @CodigoProduto,
                    @NomeProduto,
                    @CategoriaProduto,
                    @CodigoFornecedor,
                    @PrecoUltimaCompra,
                    @QuantidadeUltimaCompra,
                    @PrazoEntregaDias,
                    @QuantidadeCompras,
                    @ValorCompras,
                    @DataUltimaCompra,
                    @ChaveUltimaNFe,
                    @NumeroUltimaNFe,
                    @Ativo,
                    @Origem,
                    @Observacoes,
                    @DataCadastro,
                    @DataUltimaAtualizacao
                )
                ON CONFLICT(ProdutoId, FornecedorId) DO UPDATE SET
                    CodigoProduto = excluded.CodigoProduto,
                    NomeProduto = excluded.NomeProduto,
                    CategoriaProduto = excluded.CategoriaProduto,
                    CodigoFornecedor = excluded.CodigoFornecedor,
                    PrecoUltimaCompra = excluded.PrecoUltimaCompra,
                    QuantidadeUltimaCompra = excluded.QuantidadeUltimaCompra,
                    PrazoEntregaDias = excluded.PrazoEntregaDias,
                    QuantidadeCompras = excluded.QuantidadeCompras,
                    ValorCompras = excluded.ValorCompras,
                    DataUltimaCompra = excluded.DataUltimaCompra,
                    ChaveUltimaNFe = excluded.ChaveUltimaNFe,
                    NumeroUltimaNFe = excluded.NumeroUltimaNFe,
                    Ativo = excluded.Ativo,
                    Origem = excluded.Origem,
                    Observacoes = excluded.Observacoes,
                    DataUltimaAtualizacao = excluded.DataUltimaAtualizacao;";

            AddProdutoFornecedorParameters(command, connection, vinculo);
            command.ExecuteNonQuery();
        }

        private static Guid? ObterProdutoFornecedorIdExistente(DbConnection connection, Guid fornecedorId, Guid produtoId)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT {SelectTop(connection, 1)} Id
                FROM ProdutoFornecedores
                WHERE FornecedorId = @FornecedorId
                  AND ProdutoId = @ProdutoId
                {Limit(connection, 1)};";
            AddGuidParameter(command, connection, "@FornecedorId", fornecedorId);
            AddGuidParameter(command, connection, "@ProdutoId", produtoId);

            var value = Convert.ToString(command.ExecuteScalar());
            return Guid.TryParse(value, out var id) ? id : null;
        }

        private static List<ProdutoFornecedor> LerProdutoFornecedores(DbConnection connection, Guid fornecedorId)
        {
            var resultado = new List<ProdutoFornecedor>();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    Id,
                    ProdutoId,
                    FornecedorId,
                    CodigoProduto,
                    NomeProduto,
                    CategoriaProduto,
                    CodigoFornecedor,
                    PrecoUltimaCompra,
                    QuantidadeUltimaCompra,
                    PrazoEntregaDias,
                    QuantidadeCompras,
                    ValorCompras,
                    DataUltimaCompra,
                    ChaveUltimaNFe,
                    NumeroUltimaNFe,
                    Ativo,
                    Origem,
                    Observacoes,
                    DataCadastro,
                    DataUltimaAtualizacao
                FROM ProdutoFornecedores
                WHERE FornecedorId = @FornecedorId
                  AND Ativo = 1
                ORDER BY COALESCE(DataUltimaCompra, DataUltimaAtualizacao, DataCadastro) DESC, NomeProduto;";
            AddGuidParameter(command, connection, "@FornecedorId", fornecedorId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                resultado.Add(new ProdutoFornecedor
                {
                    Id = ReadGuid(reader, 0),
                    ProdutoId = ReadGuid(reader, 1),
                    FornecedorId = ReadGuid(reader, 2),
                    CodigoProduto = ReadString(reader, 3),
                    NomeProduto = ReadString(reader, 4),
                    CategoriaProduto = ReadString(reader, 5),
                    CodigoFornecedor = ReadString(reader, 6),
                    PrecoUltimaCompra = ReadDecimal(reader, 7),
                    QuantidadeUltimaCompra = ReadDecimal(reader, 8),
                    PrazoEntregaDias = ReadInt(reader, 9),
                    QuantidadeCompras = ReadInt(reader, 10),
                    ValorCompras = ReadDecimal(reader, 11),
                    DataUltimaCompra = ReadNullableDate(reader, 12),
                    ChaveUltimaNFe = ReadString(reader, 13),
                    NumeroUltimaNFe = ReadString(reader, 14),
                    Ativo = ReadBool(reader, 15),
                    Origem = ReadString(reader, 16),
                    Observacoes = ReadString(reader, 17),
                    DataCadastro = ReadDate(reader, 18, DateTime.Now),
                    DataUltimaAtualizacao = ReadNullableDate(reader, 19)
                });
            }

            return resultado;
        }

        private static void AddProdutoFornecedorParameters(DbCommand command, DbConnection connection, ProdutoFornecedor vinculo)
        {
            AddGuidParameter(command, connection, "@Id", vinculo.Id);
            AddGuidParameter(command, connection, "@ProdutoId", vinculo.ProdutoId);
            AddGuidParameter(command, connection, "@FornecedorId", vinculo.FornecedorId);
            command.Parameters.AddWithValue("@CodigoProduto", ToDbNullableString(vinculo.CodigoProduto));
            command.Parameters.AddWithValue("@NomeProduto", ToDbNullableString(vinculo.NomeProduto));
            command.Parameters.AddWithValue("@CategoriaProduto", ToDbNullableString(vinculo.CategoriaProduto));
            command.Parameters.AddWithValue("@CodigoFornecedor", ToDbNullableString(vinculo.CodigoFornecedor));
            command.Parameters.AddWithValue("@PrecoUltimaCompra", vinculo.PrecoUltimaCompra);
            command.Parameters.AddWithValue("@QuantidadeUltimaCompra", vinculo.QuantidadeUltimaCompra);
            command.Parameters.AddWithValue("@PrazoEntregaDias", vinculo.PrazoEntregaDias);
            command.Parameters.AddWithValue("@QuantidadeCompras", vinculo.QuantidadeCompras);
            command.Parameters.AddWithValue("@ValorCompras", vinculo.ValorCompras);
            command.Parameters.AddWithValue("@DataUltimaCompra", ToDbNullableDate(connection, vinculo.DataUltimaCompra, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@ChaveUltimaNFe", ToDbNullableString(vinculo.ChaveUltimaNFe));
            command.Parameters.AddWithValue("@NumeroUltimaNFe", ToDbNullableString(vinculo.NumeroUltimaNFe));
            command.Parameters.AddWithValue("@Ativo", vinculo.Ativo ? 1 : 0);
            command.Parameters.AddWithValue("@Origem", ToDbNullableString(vinculo.Origem));
            command.Parameters.AddWithValue("@Observacoes", ToDbNullableString(vinculo.Observacoes));
            command.Parameters.AddWithValue("@DataCadastro", ToDbDate(connection, vinculo.DataCadastro, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@DataUltimaAtualizacao", ToDbDate(connection, DateTime.Now, "yyyy-MM-dd HH:mm:ss"));
        }

        private static FornecedorCompraProdutoResumo ObterResumoComprasProdutoFornecedor(DbConnection connection, Fornecedor fornecedor, Produto produto)
        {
            var resumo = new FornecedorCompraProdutoResumo();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = $@"
                    SELECT
                        COUNT(1),
                        COALESCE(SUM(COALESCE(i.ValorTotal, 0)), 0)
                    FROM ImportacoesItens i
                    INNER JOIN ImportacoesNFe n ON n.Id = i.ImportacaoId
                    WHERE {CriarFiltroFornecedorNfe()}
                      AND {CriarFiltroProdutoImportado()};";
                AddFornecedorNfeParameters(command, fornecedor);
                AddProdutoImportadoParameters(command, produto);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    resumo.QuantidadeCompras = ReadInt(reader, 0);
                    resumo.ValorCompras = ReadDecimal(reader, 1);
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = $@"
                    SELECT {SelectTop(connection, 1)}
                        COALESCE(i.Quantidade, 0),
                        COALESCE(i.ValorUnitario, 0),
                        COALESCE(i.ValorTotal, 0),
                        COALESCE(n.ChaveAcesso, ''),
                        COALESCE(n.Numero, ''),
                        COALESCE(n.DataEntrada, n.DataEmissao, n.DataImportacao),
                        COALESCE(i.Codigo, '')
                    FROM ImportacoesItens i
                    INNER JOIN ImportacoesNFe n ON n.Id = i.ImportacaoId
                    WHERE {CriarFiltroFornecedorNfe()}
                      AND {CriarFiltroProdutoImportado()}
                    ORDER BY {DateTimeOrderExpression(connection, "COALESCE(n.DataEntrada, n.DataEmissao, n.DataImportacao)")} DESC
                    {Limit(connection, 1)};";
                AddFornecedorNfeParameters(command, fornecedor);
                AddProdutoImportadoParameters(command, produto);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    resumo.QuantidadeUltimaCompra = ReadDecimal(reader, 0);
                    resumo.PrecoUltimaCompra = ReadDecimal(reader, 1);
                    resumo.ChaveUltimaNFe = ReadString(reader, 3);
                    resumo.NumeroUltimaNFe = ReadString(reader, 4);
                    resumo.DataUltimaCompra = ReadNullableDate(reader, 5);
                    resumo.CodigoFornecedor = ReadString(reader, 6);
                }
            }

            return resumo;
        }

        private static string CriarFiltroFornecedorNfe()
        {
            return @"(
                    (
                        @FornecedorCnpjNormalizado <> ''
                    AND replace(replace(replace(lower(COALESCE(n.FornecedorCNPJ, '')), '.', ''), '/', ''), '-', '') = @FornecedorCnpjNormalizado
                    )
                 OR (
                        @FornecedorNomeNormalizado <> ''
                    AND lower(ltrim(rtrim(COALESCE(n.FornecedorNome, '')))) = @FornecedorNomeNormalizado
                 )
                 OR (
                        @FornecedorRazaoNormalizada <> ''
                    AND lower(ltrim(rtrim(COALESCE(n.FornecedorNome, '')))) = @FornecedorRazaoNormalizada
                 )
                )";
        }

        private static string CriarFiltroProdutoImportado()
        {
            return @"(
                    (
                        @ProdutoId <> ''
                    AND COALESCE(i.ProdutoExistenteId, '') = @ProdutoId
                    )
                 OR (
                        @ProdutoCodigoNormalizado <> ''
                    AND lower(ltrim(rtrim(COALESCE(i.Codigo, '')))) = @ProdutoCodigoNormalizado
                 )
                 OR (
                        @ProdutoCodigoBarrasNormalizado <> ''
                    AND lower(ltrim(rtrim(COALESCE(i.CodigoBarras, '')))) = @ProdutoCodigoBarrasNormalizado
                 )
                 OR (
                        @ProdutoNomeNormalizado <> ''
                    AND lower(ltrim(rtrim(COALESCE(i.Nome, '')))) = @ProdutoNomeNormalizado
                    )
                )";
        }

        private static void AddFornecedorNfeParameters(DbCommand command, Fornecedor fornecedor)
        {
            command.Parameters.AddWithValue("@FornecedorCnpjNormalizado", NormalizarDocumento(fornecedor.CNPJ));
            command.Parameters.AddWithValue("@FornecedorNomeNormalizado", NormalizarTexto(fornecedor.NomeFantasia));
            command.Parameters.AddWithValue("@FornecedorRazaoNormalizada", NormalizarTexto(fornecedor.RazaoSocial));
        }

        private static void AddProdutoImportadoParameters(DbCommand command, Produto produto)
        {
            command.Parameters.AddWithValue("@ProdutoId", produto.Id == Guid.Empty ? string.Empty : produto.Id.ToString());
            command.Parameters.AddWithValue("@ProdutoCodigoNormalizado", NormalizarTexto(produto.Codigo));
            command.Parameters.AddWithValue("@ProdutoCodigoBarrasNormalizado", NormalizarTexto(produto.CodigoBarras));
            command.Parameters.AddWithValue("@ProdutoNomeNormalizado", NormalizarTexto(produto.Nome));
        }

        private decimal CalcularScoreRanking(Fornecedor fornecedor)
        {
            var produtos = ObterProdutosRelacionados(fornecedor);
            var valorEstoque = produtos.Sum(produto => produto.ValorTotalEstoque);
            return Math.Max(fornecedor.TotalCompras, valorEstoque);
        }

        private static int CalcularPrazoMedioOperacional(IReadOnlyCollection<ProdutoFornecedor> produtoFornecedores, int prazoFornecedor)
        {
            var prazos = produtoFornecedores
                .Where(item => item.Ativo && item.PrazoEntregaDias > 0)
                .Select(item => item.PrazoEntregaDias)
                .ToList();

            if (prazos.Count == 0)
            {
                return Math.Max(0, prazoFornecedor);
            }

            return (int)Math.Round(prazos.Average(), MidpointRounding.AwayFromZero);
        }

        private List<FornecedorNotaHistoricoItem> ObterNotasRecentes(Fornecedor fornecedor)
        {
            var itens = new List<FornecedorNotaHistoricoItem>();

            try
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = $@"
                    SELECT {SelectTop(connection, 6)}
                        Numero,
                        Serie,
                        DataEntrada,
                        DataEmissao,
                        ValorTotal,
                        Status
                    FROM ImportacoesNFe
                    WHERE (
                            @Cnpj <> ''
                        AND replace(replace(replace(lower(COALESCE(FornecedorCNPJ, '')), '.', ''), '/', ''), '-', '') = @Cnpj
                        )
                       OR (
                            @Nome <> ''
                        AND lower(ltrim(rtrim(COALESCE(FornecedorNome, '')))) = @Nome
                       )
                       OR (
                            @RazaoSocial <> ''
                        AND lower(ltrim(rtrim(COALESCE(FornecedorNome, '')))) = @RazaoSocial
                       )
                    ORDER BY {DateTimeOrderExpression(connection, "COALESCE(DataEntrada, DataEmissao, DataImportacao)")} DESC
                    {Limit(connection, 6)};";
                command.Parameters.AddWithValue("@Cnpj", NormalizarDocumento(fornecedor.CNPJ));
                command.Parameters.AddWithValue("@Nome", NormalizarTexto(fornecedor.NomeFantasia));
                command.Parameters.AddWithValue("@RazaoSocial", NormalizarTexto(fornecedor.RazaoSocial));

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    itens.Add(new FornecedorNotaHistoricoItem
                    {
                        Numero = reader.IsDBNull(0) ? "Sem numero" : reader.GetString(0),
                        Serie = reader.IsDBNull(1) ? "-" : reader.GetString(1),
                        DataReferencia = TentarLerData(reader, 2) ?? TentarLerData(reader, 3),
                        ValorTotal = reader.IsDBNull(4) ? 0m : Convert.ToDecimal(reader.GetValue(4)),
                        Status = reader.IsDBNull(5) ? "Importada" : reader.GetString(5)
                    });
                }
            }
            catch
            {
                // Historico de notas e complementar; nao bloqueia a tela se o banco estiver em transicao.
            }

            return itens;
        }

        private List<FornecedorContaPagarResumo> ObterContasPagarVinculadas(Fornecedor fornecedor)
        {
            var itens = new List<FornecedorContaPagarResumo>();

            try
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                if (!TabelaExiste(connection, "ContasPagar"))
                {
                    return itens;
                }

                using var command = connection.CreateCommand();
                command.CommandText = $@"
                    SELECT {SelectTop(connection, 8)}
                        Descricao,
                        Valor,
                        DataVencimento,
                        Status,
                        COALESCE(Categoria, ''),
                        COALESCE(Origem, ''),
                        COALESCE(Fornecedor, ''),
                        COALESCE(Observacoes, '')
                    FROM ContasPagar
                    WHERE (
                            @Documento <> ''
                        AND (
                               replace(replace(replace(lower(COALESCE(Fornecedor, '')), '.', ''), '/', ''), '-', '') LIKE {ContainsExpression(connection, "@Documento")}
                            OR replace(replace(replace(lower(COALESCE(Descricao, '')), '.', ''), '/', ''), '-', '') LIKE {ContainsExpression(connection, "@Documento")}
                            OR replace(replace(replace(lower(COALESCE(Observacoes, '')), '.', ''), '/', ''), '-', '') LIKE {ContainsExpression(connection, "@Documento")}
                        )
                    )
                    OR (
                            @NomeFantasia <> ''
                        AND (
                               lower(ltrim(rtrim(COALESCE(Fornecedor, '')))) = @NomeFantasia
                            OR lower(COALESCE(Fornecedor, '')) LIKE {ContainsExpression(connection, "@NomeFantasia")}
                            OR lower(COALESCE(Descricao, '')) LIKE {ContainsExpression(connection, "@NomeFantasia")}
                            OR lower(COALESCE(Observacoes, '')) LIKE {ContainsExpression(connection, "@NomeFantasia")}
                        )
                    )
                    OR (
                            @RazaoSocial <> ''
                        AND (
                               lower(ltrim(rtrim(COALESCE(Fornecedor, '')))) = @RazaoSocial
                            OR lower(COALESCE(Fornecedor, '')) LIKE {ContainsExpression(connection, "@RazaoSocial")}
                            OR lower(COALESCE(Descricao, '')) LIKE {ContainsExpression(connection, "@RazaoSocial")}
                            OR lower(COALESCE(Observacoes, '')) LIKE {ContainsExpression(connection, "@RazaoSocial")}
                        )
                    )
                    ORDER BY
                        CASE
                            WHEN lower(COALESCE(Status, '')) IN ('pago', 'quitado', 'cancelado') THEN 1
                            ELSE 0
                        END,
                        {DateOnlyOrderExpression(connection, "DataVencimento")} ASC,
                        {LatestRowExpression(connection)} DESC
                    {Limit(connection, 8)};";
                command.Parameters.AddWithValue("@Documento", NormalizarDocumento(fornecedor.CNPJ));
                command.Parameters.AddWithValue("@NomeFantasia", NormalizarTexto(fornecedor.NomeFantasia));
                command.Parameters.AddWithValue("@RazaoSocial", NormalizarTexto(fornecedor.RazaoSocial));

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var status = ReadString(reader, 3);
                    itens.Add(new FornecedorContaPagarResumo
                    {
                        Descricao = ReadString(reader, 0),
                        Valor = ReadDecimal(reader, 1),
                        DataVencimento = ReadDate(reader, 2, DateTime.MinValue),
                        Status = string.IsNullOrWhiteSpace(status) ? "Pendente" : status,
                        Categoria = ReadString(reader, 4),
                        Origem = ReadString(reader, 5),
                        Fornecedor = ReadString(reader, 6),
                        Observacoes = ReadString(reader, 7),
                        EmAberto = EstaContaPagarEmAberto(status)
                    });
                }
            }
            catch
            {
                // Contas a pagar sao uma integracao operacional; falhas nao devem bloquear a ficha.
            }

            return itens;
        }

        private static bool TabelaExiste(DbConnection connection, string tabela)
        {
            using var command = connection.CreateCommand();
            command.CommandText = IsSqlServerConnection(connection)
                ? @"
                SELECT 1
                FROM sys.tables
                WHERE name = @Tabela;"
                : @"
                SELECT 1
                FROM sqlite_master
                WHERE type = 'table'
                  AND name = @Tabela
                LIMIT 1;";
            command.Parameters.AddWithValue("@Tabela", tabela);
            return command.ExecuteScalar() != null;
        }

        private static bool EstaContaPagarEmAberto(string? status)
        {
            var normalizado = NormalizarTexto(status);
            return normalizado != "pago" &&
                   normalizado != "quitado" &&
                   normalizado != "cancelado";
        }

        private static DateTime? TentarLerData(DbDataReader reader, int index)
        {
            return reader.IsDBNull(index) || !DateTime.TryParse(Convert.ToString(reader.GetValue(index)), out var data)
                ? null
                : data;
        }

        private static string MontarCondicaoPagamento(Fornecedor fornecedor)
        {
            var forma = string.IsNullOrWhiteSpace(fornecedor.FormaPagamento) ? "Nao informada" : fornecedor.FormaPagamento;
            var prazo = string.IsNullOrWhiteSpace(fornecedor.PrazoPagamento) ? "sem prazo definido" : fornecedor.PrazoPagamento;
            var prazoMedio = fornecedor.PrazoMedioPagamentoDias > 0
                ? $"media {fornecedor.PrazoMedioPagamentoDias} dia(s)"
                : string.Empty;
            return string.IsNullOrWhiteSpace(prazoMedio)
                ? $"{forma} | {prazo}"
                : $"{forma} | {prazo} | {prazoMedio}";
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

        private static string NormalizarTexto(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor)
                ? string.Empty
                : valor.Trim().ToLowerInvariant();
        }

        private static object ToDbNullableString(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
        }

        private static object ToDbNullableDate(DbConnection connection, DateTime? value, string format)
        {
            return value.HasValue ? ToDbDate(connection, value.Value, format) : DBNull.Value;
        }

        private static object ToDbDate(DbConnection connection, DateTime value, string format)
        {
            return IsSqlServerConnection(connection) ? value : value.ToString(format);
        }

        private static void AddGuidParameter(DbCommand command, DbConnection connection, string name, Guid value)
        {
            command.Parameters.AddWithValue(name, IsSqlServerConnection(connection) ? value : value.ToString());
        }

        private static string SelectTop(DbConnection connection, int count)
        {
            return IsSqlServerConnection(connection) ? $"TOP ({count})" : string.Empty;
        }

        private static string Limit(DbConnection connection, int count)
        {
            return IsSqlServerConnection(connection) ? string.Empty : $"LIMIT {count}";
        }

        private static string ContainsExpression(DbConnection connection, string parameterName)
        {
            return IsSqlServerConnection(connection)
                ? $"'%' + {parameterName} + '%'"
                : $"'%' || {parameterName} || '%'";
        }

        private static string DateTimeOrderExpression(DbConnection connection, string expression)
        {
            return IsSqlServerConnection(connection)
                ? $"TRY_CONVERT(datetime, {expression}, 120)"
                : $"datetime({expression})";
        }

        private static string DateOnlyOrderExpression(DbConnection connection, string expression)
        {
            return IsSqlServerConnection(connection)
                ? $"TRY_CONVERT(date, {expression}, 120)"
                : $"date({expression})";
        }

        private static string LatestRowExpression(DbConnection connection)
        {
            return IsSqlServerConnection(connection) ? "Id" : "rowid";
        }

        private static bool IsSqlServerConnection(DbConnection connection)
        {
            var typeName = connection.GetType().FullName ?? string.Empty;
            return typeName.Contains("SqlClient", StringComparison.OrdinalIgnoreCase) ||
                   typeName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadString(DbDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? string.Empty : Convert.ToString(reader.GetValue(index)) ?? string.Empty;
        }

        private static int ReadInt(DbDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0 : Convert.ToInt32(reader.GetValue(index));
        }

        private static decimal ReadDecimal(DbDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0m : Convert.ToDecimal(reader.GetValue(index));
        }

        private static bool ReadBool(DbDataReader reader, int index)
        {
            return !reader.IsDBNull(index) && Convert.ToInt32(reader.GetValue(index)) == 1;
        }

        private static Guid ReadGuid(DbDataReader reader, int index)
        {
            return reader.IsDBNull(index) || !Guid.TryParse(Convert.ToString(reader.GetValue(index)), out var value)
                ? Guid.Empty
                : value;
        }

        private static DateTime ReadDate(DbDataReader reader, int index, DateTime fallback)
        {
            return reader.IsDBNull(index) || !DateTime.TryParse(Convert.ToString(reader.GetValue(index)), out var value)
                ? fallback
                : value;
        }

        private static DateTime? ReadNullableDate(DbDataReader reader, int index)
        {
            return reader.IsDBNull(index) || !DateTime.TryParse(Convert.ToString(reader.GetValue(index)), out var value)
                ? null
                : value;
        }

        private sealed class FornecedorCompraProdutoResumo
        {
            public string CodigoFornecedor { get; set; } = string.Empty;
            public decimal PrecoUltimaCompra { get; set; }
            public decimal QuantidadeUltimaCompra { get; set; }
            public int QuantidadeCompras { get; set; }
            public decimal ValorCompras { get; set; }
            public DateTime? DataUltimaCompra { get; set; }
            public string ChaveUltimaNFe { get; set; } = string.Empty;
            public string NumeroUltimaNFe { get; set; } = string.Empty;
        }
    }

    public sealed class FornecedorOperationalInsights
    {
        public string ContatoPrincipalNome { get; init; } = string.Empty;
        public string ContatoPrincipalCargo { get; init; } = string.Empty;
        public string ContatoPrincipalTelefone { get; init; } = string.Empty;
        public string ContatoPrincipalEmail { get; init; } = string.Empty;
        public string WhatsAppVendedor { get; init; } = string.Empty;
        public int ProdutosRelacionados { get; init; }
        public int ProdutosAtivos { get; init; }
        public int ProdutosEstoqueBaixo { get; init; }
        public decimal ValorEstoqueVinculado { get; init; }
        public string CategoriaPreferencial { get; init; } = string.Empty;
        public int RankingGeral { get; init; }
        public DateTime? UltimaCompra { get; init; }
        public decimal TotalCompras { get; init; }
        public decimal TotalComprasProdutoFornecedor { get; init; }
        public int QuantidadeComprasProdutoFornecedor { get; init; }
        public decimal TicketMedioCompra { get; init; }
        public int PrazoMedioEntregaDias { get; init; }
        public string CondicaoPagamento { get; init; } = string.Empty;
        public List<ProdutoFornecedor> ProdutoFornecedores { get; init; } = new();
        public List<FornecedorNotaHistoricoItem> HistoricoNotas { get; init; } = new();
        public int ContasPagarVinculadas { get; init; }
        public decimal ValorContasPagarAberto { get; init; }
        public decimal ValorContasPagarTotal { get; init; }
        public List<FornecedorContaPagarResumo> ContasPagarRecentes { get; init; } = new();
        public List<FornecedorProdutoResumo> ProdutosPrincipais { get; init; } = new();
        public List<string> Alertas { get; init; } = new();
        public bool TemAlertas => Alertas.Count > 0;
        public string ResumoAlertas => Alertas.Count == 0 ? "Operacao estavel" : string.Join(" | ", Alertas);
    }

    public sealed class FornecedorProdutoResumo
    {
        public string Nome { get; init; } = string.Empty;
        public string Categoria { get; init; } = string.Empty;
        public int Estoque { get; init; }
        public decimal ValorEstoque { get; init; }
        public DateTime? UltimaCompra { get; init; }
        public decimal PrecoUltimaCompra { get; init; }
        public int QuantidadeCompras { get; init; }
        public decimal ValorCompras { get; init; }
        public int PrazoEntregaDias { get; init; }
        public string UltimaNFe { get; init; } = string.Empty;
    }

    public sealed class FornecedorNotaHistoricoItem
    {
        public string Numero { get; init; } = string.Empty;
        public string Serie { get; init; } = string.Empty;
        public DateTime? DataReferencia { get; init; }
        public decimal ValorTotal { get; init; }
        public string Status { get; init; } = string.Empty;
    }

    public sealed class FornecedorContaPagarResumo
    {
        public string Descricao { get; init; } = string.Empty;
        public decimal Valor { get; init; }
        public DateTime DataVencimento { get; init; }
        public string Status { get; init; } = string.Empty;
        public string Categoria { get; init; } = string.Empty;
        public string Origem { get; init; } = string.Empty;
        public string Fornecedor { get; init; } = string.Empty;
        public string Observacoes { get; init; } = string.Empty;
        public bool EmAberto { get; init; }
    }
}
