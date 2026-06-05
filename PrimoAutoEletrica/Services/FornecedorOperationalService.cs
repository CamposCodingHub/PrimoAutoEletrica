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

        private ProdutoFornecedor CriarProdutoFornecedorPersistido(SqliteConnection connection, Fornecedor fornecedor, Produto produto)
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

        private static void MarcarProdutoFornecedoresComoInativos(SqliteConnection connection, Guid fornecedorId)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE ProdutoFornecedores
                SET
                    Ativo = 0,
                    DataUltimaAtualizacao = @DataUltimaAtualizacao
                WHERE FornecedorId = @FornecedorId;";
            command.Parameters.AddWithValue("@FornecedorId", fornecedorId.ToString());
            command.Parameters.AddWithValue("@DataUltimaAtualizacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.ExecuteNonQuery();
        }

        private static void SalvarProdutoFornecedor(SqliteConnection connection, ProdutoFornecedor vinculo)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
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

            AddProdutoFornecedorParameters(command, vinculo);
            command.ExecuteNonQuery();
        }

        private static Guid? ObterProdutoFornecedorIdExistente(SqliteConnection connection, Guid fornecedorId, Guid produtoId)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id
                FROM ProdutoFornecedores
                WHERE FornecedorId = @FornecedorId
                  AND ProdutoId = @ProdutoId
                LIMIT 1;";
            command.Parameters.AddWithValue("@FornecedorId", fornecedorId.ToString());
            command.Parameters.AddWithValue("@ProdutoId", produtoId.ToString());

            var value = Convert.ToString(command.ExecuteScalar());
            return Guid.TryParse(value, out var id) ? id : null;
        }

        private static List<ProdutoFornecedor> LerProdutoFornecedores(SqliteConnection connection, Guid fornecedorId)
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
            command.Parameters.AddWithValue("@FornecedorId", fornecedorId.ToString());

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

        private static void AddProdutoFornecedorParameters(SqliteCommand command, ProdutoFornecedor vinculo)
        {
            command.Parameters.AddWithValue("@Id", vinculo.Id.ToString());
            command.Parameters.AddWithValue("@ProdutoId", vinculo.ProdutoId.ToString());
            command.Parameters.AddWithValue("@FornecedorId", vinculo.FornecedorId.ToString());
            command.Parameters.AddWithValue("@CodigoProduto", ToDbNullableString(vinculo.CodigoProduto));
            command.Parameters.AddWithValue("@NomeProduto", ToDbNullableString(vinculo.NomeProduto));
            command.Parameters.AddWithValue("@CategoriaProduto", ToDbNullableString(vinculo.CategoriaProduto));
            command.Parameters.AddWithValue("@CodigoFornecedor", ToDbNullableString(vinculo.CodigoFornecedor));
            command.Parameters.AddWithValue("@PrecoUltimaCompra", vinculo.PrecoUltimaCompra);
            command.Parameters.AddWithValue("@QuantidadeUltimaCompra", vinculo.QuantidadeUltimaCompra);
            command.Parameters.AddWithValue("@PrazoEntregaDias", vinculo.PrazoEntregaDias);
            command.Parameters.AddWithValue("@QuantidadeCompras", vinculo.QuantidadeCompras);
            command.Parameters.AddWithValue("@ValorCompras", vinculo.ValorCompras);
            command.Parameters.AddWithValue("@DataUltimaCompra", ToDbNullableDate(vinculo.DataUltimaCompra, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@ChaveUltimaNFe", ToDbNullableString(vinculo.ChaveUltimaNFe));
            command.Parameters.AddWithValue("@NumeroUltimaNFe", ToDbNullableString(vinculo.NumeroUltimaNFe));
            command.Parameters.AddWithValue("@Ativo", vinculo.Ativo ? 1 : 0);
            command.Parameters.AddWithValue("@Origem", ToDbNullableString(vinculo.Origem));
            command.Parameters.AddWithValue("@Observacoes", ToDbNullableString(vinculo.Observacoes));
            command.Parameters.AddWithValue("@DataCadastro", vinculo.DataCadastro.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@DataUltimaAtualizacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        private static FornecedorCompraProdutoResumo ObterResumoComprasProdutoFornecedor(SqliteConnection connection, Fornecedor fornecedor, Produto produto)
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
                    SELECT
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
                    ORDER BY datetime(COALESCE(n.DataEntrada, n.DataEmissao, n.DataImportacao)) DESC
                    LIMIT 1;";
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
                    AND (
                           lower(trim(COALESCE(n.FornecedorNome, ''))) = @FornecedorNomeNormalizado
                        OR lower(trim(COALESCE(n.FornecedorNome, ''))) = @FornecedorRazaoNormalizada
                    )
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
                    AND lower(trim(COALESCE(i.Codigo, ''))) = @ProdutoCodigoNormalizado
                    )
                 OR (
                        @ProdutoCodigoBarrasNormalizado <> ''
                    AND lower(trim(COALESCE(i.CodigoBarras, ''))) = @ProdutoCodigoBarrasNormalizado
                    )
                 OR (
                        @ProdutoNomeNormalizado <> ''
                    AND lower(trim(COALESCE(i.Nome, ''))) = @ProdutoNomeNormalizado
                    )
                )";
        }

        private static void AddFornecedorNfeParameters(SqliteCommand command, Fornecedor fornecedor)
        {
            command.Parameters.AddWithValue("@FornecedorCnpjNormalizado", NormalizarDocumento(fornecedor.CNPJ));
            command.Parameters.AddWithValue("@FornecedorNomeNormalizado", NormalizarTexto(fornecedor.NomeFantasia));
            command.Parameters.AddWithValue("@FornecedorRazaoNormalizada", NormalizarTexto(fornecedor.RazaoSocial));
        }

        private static void AddProdutoImportadoParameters(SqliteCommand command, Produto produto)
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
                command.CommandText = @"
                    SELECT
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
                        AND (
                               lower(trim(COALESCE(FornecedorNome, ''))) = @Nome
                            OR lower(trim(COALESCE(FornecedorNome, ''))) = @RazaoSocial
                        )
                       )
                    ORDER BY COALESCE(DataEntrada, DataEmissao) DESC
                    LIMIT 6;";
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

        private static DateTime? TentarLerData(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) || !DateTime.TryParse(Convert.ToString(reader.GetValue(index)), out var data)
                ? null
                : data;
        }

        private static string MontarCondicaoPagamento(Fornecedor fornecedor)
        {
            var forma = string.IsNullOrWhiteSpace(fornecedor.FormaPagamento) ? "Nao informada" : fornecedor.FormaPagamento;
            var prazo = string.IsNullOrWhiteSpace(fornecedor.PrazoPagamento) ? "sem prazo definido" : fornecedor.PrazoPagamento;
            return $"{forma} | {prazo}";
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

        private static decimal ReadDecimal(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0m : Convert.ToDecimal(reader.GetValue(index));
        }

        private static bool ReadBool(SqliteDataReader reader, int index)
        {
            return !reader.IsDBNull(index) && Convert.ToInt32(reader.GetValue(index)) == 1;
        }

        private static Guid ReadGuid(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) || !Guid.TryParse(Convert.ToString(reader.GetValue(index)), out var value)
                ? Guid.Empty
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
}
