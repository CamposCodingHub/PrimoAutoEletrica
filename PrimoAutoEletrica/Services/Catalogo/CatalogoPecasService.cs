using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PrimoAutoEletrica.Services.Catalogo
{
    public sealed class CatalogoPecasService
    {
        private const string CatalogoColumns = @"
            Id,
            CodigoFabricante,
            CodigoNormalizado,
            Marca,
            Nome,
            Descricao,
            Categoria,
            Subcategoria,
            Linha,
            Aplicacao,
            VeiculoAplicacao,
            AnoInicial,
            AnoFinal,
            Voltagem,
            Amperagem,
            QuantidadeTerminais,
            TipoProduto,
            PaginaCatalogo,
            FonteCatalogo,
            ArquivoOrigem,
            ObservacoesTecnicas,
            ImagemUrl,
            ImagemLocal,
            StatusRevisao,
            ProdutoEstoqueId,
            DataImportacao,
            DataAtualizacao,
            Ativo";

        private readonly DatabaseService _databaseService;
        private readonly LoggerService _logger;

        public CatalogoPecasService(DatabaseService? databaseService = null, LoggerService? logger = null)
        {
            _databaseService = databaseService ?? global::PrimoAutoEletrica.App.Database;
            _logger = logger ?? global::PrimoAutoEletrica.App.Logger;
        }

        public List<CatalogoPeca> ObterTodos(bool incluirInativos = false)
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT {CatalogoColumns}
                FROM CatalogoPecas
                WHERE @IncluirInativos = 1 OR Ativo = 1
                ORDER BY DataImportacao DESC, Marca, CodigoFabricante;";
            command.Parameters.AddWithValue("@IncluirInativos", incluirInativos ? 1 : 0);

            return MaterializarLista(command);
        }

        public List<CatalogoPeca> Buscar(string termo)
        {
            var filtro = new CatalogoPecaFiltro { Termo = termo };
            return Buscar(filtro);
        }

        public List<CatalogoPeca> Buscar(CatalogoPecaFiltro filtro)
        {
            filtro ??= new CatalogoPecaFiltro();
            var resultado = ObterTodos(filtro.IncluirInativos).AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filtro.Termo))
            {
                var termo = filtro.Termo.Trim();
                resultado = resultado.Where(item =>
                    Contem(item.CodigoFabricante, termo) ||
                    Contem(item.CodigoNormalizado, termo) ||
                    Contem(item.Nome, termo) ||
                    Contem(item.Descricao, termo) ||
                    Contem(item.Categoria, termo) ||
                    Contem(item.Subcategoria, termo) ||
                    Contem(item.Aplicacao, termo) ||
                    Contem(item.VeiculoAplicacao, termo));
            }

            if (!string.IsNullOrWhiteSpace(filtro.Marca))
            {
                resultado = resultado.Where(item => string.Equals(item.Marca, filtro.Marca.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filtro.Categoria))
            {
                resultado = resultado.Where(item => string.Equals(item.Categoria, filtro.Categoria.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (filtro.SomentePendentes)
            {
                resultado = resultado.Where(item => string.Equals(item.StatusRevisao, "Pendente", StringComparison.OrdinalIgnoreCase));
            }
            else if (!string.IsNullOrWhiteSpace(filtro.StatusRevisao))
            {
                resultado = resultado.Where(item => string.Equals(item.StatusRevisao, filtro.StatusRevisao.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            return resultado
                .OrderBy(item => item.Marca, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.CodigoFabricante, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public CatalogoPeca? ObterPorId(Guid id)
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT {CatalogoColumns}
                FROM CatalogoPecas
                WHERE Id = @Id
                LIMIT 1;";
            command.Parameters.AddWithValue("@Id", id.ToString());

            using var reader = command.ExecuteReader();
            return reader.Read() ? Materializar(reader) : null;
        }

        public CatalogoPeca? ObterPorCodigo(string codigo, string marca)
        {
            var codigoNormalizado = CatalogoCodeNormalizer.NormalizeCode(codigo, marca);
            using var connection = _databaseService.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT {CatalogoColumns}
                FROM CatalogoPecas
                WHERE upper(trim(CodigoNormalizado)) = upper(trim(@CodigoNormalizado))
                  AND upper(trim(COALESCE(Marca, ''))) = upper(trim(@Marca))
                ORDER BY DataImportacao DESC
                LIMIT 1;";
            command.Parameters.AddWithValue("@CodigoNormalizado", codigoNormalizado);
            command.Parameters.AddWithValue("@Marca", marca?.Trim() ?? string.Empty);

            using var reader = command.ExecuteReader();
            return reader.Read() ? Materializar(reader) : null;
        }

        public void Inserir(CatalogoPeca item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            NormalizeItem(item);
            item.Id = item.Id == Guid.Empty ? Guid.NewGuid() : item.Id;
            item.DataImportacao = item.DataImportacao == default ? DateTime.Now : item.DataImportacao;
            item.DataAtualizacao = DateTime.Now;

            using var connection = _databaseService.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                INSERT INTO CatalogoPecas
                (
                    {CatalogoColumns}
                )
                VALUES
                (
                    @Id,
                    @CodigoFabricante,
                    @CodigoNormalizado,
                    @Marca,
                    @Nome,
                    @Descricao,
                    @Categoria,
                    @Subcategoria,
                    @Linha,
                    @Aplicacao,
                    @VeiculoAplicacao,
                    @AnoInicial,
                    @AnoFinal,
                    @Voltagem,
                    @Amperagem,
                    @QuantidadeTerminais,
                    @TipoProduto,
                    @PaginaCatalogo,
                    @FonteCatalogo,
                    @ArquivoOrigem,
                    @ObservacoesTecnicas,
                    @ImagemUrl,
                    @ImagemLocal,
                    @StatusRevisao,
                    @ProdutoEstoqueId,
                    @DataImportacao,
                    @DataAtualizacao,
                    @Ativo
                );";
            AddCatalogoParameters(command, item);
            command.ExecuteNonQuery();
        }

        public void Atualizar(CatalogoPeca item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            NormalizeItem(item);
            item.DataAtualizacao = DateTime.Now;

            using var connection = _databaseService.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE CatalogoPecas
                SET
                    CodigoFabricante = @CodigoFabricante,
                    CodigoNormalizado = @CodigoNormalizado,
                    Marca = @Marca,
                    Nome = @Nome,
                    Descricao = @Descricao,
                    Categoria = @Categoria,
                    Subcategoria = @Subcategoria,
                    Linha = @Linha,
                    Aplicacao = @Aplicacao,
                    VeiculoAplicacao = @VeiculoAplicacao,
                    AnoInicial = @AnoInicial,
                    AnoFinal = @AnoFinal,
                    Voltagem = @Voltagem,
                    Amperagem = @Amperagem,
                    QuantidadeTerminais = @QuantidadeTerminais,
                    TipoProduto = @TipoProduto,
                    PaginaCatalogo = @PaginaCatalogo,
                    FonteCatalogo = @FonteCatalogo,
                    ArquivoOrigem = @ArquivoOrigem,
                    ObservacoesTecnicas = @ObservacoesTecnicas,
                    ImagemUrl = @ImagemUrl,
                    ImagemLocal = @ImagemLocal,
                    StatusRevisao = @StatusRevisao,
                    ProdutoEstoqueId = @ProdutoEstoqueId,
                    DataAtualizacao = @DataAtualizacao,
                    Ativo = @Ativo
                WHERE Id = @Id;";
            AddCatalogoParameters(command, item);
            command.ExecuteNonQuery();
        }

        public bool ExisteCodigo(string codigoNormalizado, string marca, string fonte)
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT 1
                FROM CatalogoPecas
                WHERE upper(trim(CodigoNormalizado)) = upper(trim(@CodigoNormalizado))
                  AND upper(trim(COALESCE(Marca, ''))) = upper(trim(@Marca))
                  AND upper(trim(COALESCE(FonteCatalogo, ''))) = upper(trim(@FonteCatalogo))
                LIMIT 1;";
            command.Parameters.AddWithValue("@CodigoNormalizado", codigoNormalizado?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("@Marca", marca?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("@FonteCatalogo", fonte?.Trim() ?? string.Empty);
            return command.ExecuteScalar() != null;
        }

        public CatalogoPeca? ObterDuplicadoProvavel(string codigoNormalizado, string marca, string? fonte)
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT {CatalogoColumns}
                FROM CatalogoPecas
                WHERE upper(trim(CodigoNormalizado)) = upper(trim(@CodigoNormalizado))
                  AND upper(trim(COALESCE(Marca, ''))) = upper(trim(@Marca))
                  AND upper(trim(COALESCE(FonteCatalogo, ''))) <> upper(trim(@FonteCatalogo))
                ORDER BY DataImportacao DESC
                LIMIT 1;";
            command.Parameters.AddWithValue("@CodigoNormalizado", codigoNormalizado?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("@Marca", marca?.Trim() ?? string.Empty);
            command.Parameters.AddWithValue("@FonteCatalogo", fonte?.Trim() ?? string.Empty);

            using var reader = command.ExecuteReader();
            return reader.Read() ? Materializar(reader) : null;
        }

        public void VincularProdutoEstoque(Guid catalogoId, Guid produtoId)
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE CatalogoPecas
                SET
                    ProdutoEstoqueId = @ProdutoEstoqueId,
                    StatusRevisao = 'ConvertidoEstoque',
                    DataAtualizacao = @DataAtualizacao
                WHERE Id = @Id;";
            command.Parameters.AddWithValue("@ProdutoEstoqueId", produtoId.ToString());
            command.Parameters.AddWithValue("@DataAtualizacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@Id", catalogoId.ToString());
            command.ExecuteNonQuery();
        }

        public void MarcarStatus(Guid catalogoId, string status)
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE CatalogoPecas
                SET
                    StatusRevisao = @StatusRevisao,
                    DataAtualizacao = @DataAtualizacao
                WHERE Id = @Id;";
            command.Parameters.AddWithValue("@StatusRevisao", CatalogoCodeNormalizer.NormalizeStatusForPersistence(status));
            command.Parameters.AddWithValue("@DataAtualizacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@Id", catalogoId.ToString());
            command.ExecuteNonQuery();
        }

        public CatalogoPecaResumo ObterResumo()
        {
            var itens = ObterTodos();
            return new CatalogoPecaResumo
            {
                TotalItens = itens.Count,
                PendentesRevisao = itens.Count(item => string.Equals(item.StatusRevisao, "Pendente", StringComparison.OrdinalIgnoreCase)),
                ConvertidosEstoque = itens.Count(item => string.Equals(item.StatusRevisao, "ConvertidoEstoque", StringComparison.OrdinalIgnoreCase)),
                DuplicadosProvaveis = itens.Count(item => string.Equals(item.StatusRevisao, "DuplicadoProvavel", StringComparison.OrdinalIgnoreCase)),
                Incompletos = itens.Count(item => string.Equals(item.StatusRevisao, "Incompleto", StringComparison.OrdinalIgnoreCase)),
                Ignorados = itens.Count(item => string.Equals(item.StatusRevisao, "Ignorado", StringComparison.OrdinalIgnoreCase))
            };
        }

        public List<string> ObterMarcas()
        {
            return ObterTodos()
                .Select(item => item.Marca)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public List<string> ObterCategorias()
        {
            return ObterTodos()
                .Select(item => item.Categoria)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public string ExportarCsv(IEnumerable<CatalogoPeca> itens, string caminhoDestino)
        {
            var linhas = new List<string>
            {
                "Codigo;Marca;Nome;Descricao;Categoria;Subcategoria;Aplicacao;Pagina;Status;Fonte;ProdutoEstoqueId"
            };

            foreach (var item in itens ?? Enumerable.Empty<CatalogoPeca>())
            {
                linhas.Add(string.Join(";",
                    EscapeCsv(item.CodigoFabricante),
                    EscapeCsv(item.Marca),
                    EscapeCsv(item.Nome),
                    EscapeCsv(item.Descricao),
                    EscapeCsv(item.Categoria),
                    EscapeCsv(item.Subcategoria),
                    EscapeCsv(item.Aplicacao),
                    EscapeCsv(item.PaginaCatalogo),
                    EscapeCsv(item.StatusRevisao),
                    EscapeCsv(item.FonteCatalogo),
                    EscapeCsv(item.ProdutoEstoqueId?.ToString() ?? string.Empty)));
            }

            System.IO.File.WriteAllLines(caminhoDestino, linhas);
            return caminhoDestino;
        }

        private static bool Contem(string? origem, string termo)
        {
            return !string.IsNullOrWhiteSpace(origem) &&
                   origem.Contains(termo, StringComparison.OrdinalIgnoreCase);
        }

        private static string EscapeCsv(string value)
        {
            return (value ?? string.Empty).Replace(";", ",").Replace(Environment.NewLine, " ").Trim();
        }

        private List<CatalogoPeca> MaterializarLista(SqliteCommand command)
        {
            var itens = new List<CatalogoPeca>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                itens.Add(Materializar(reader));
            }

            return itens;
        }

        private static CatalogoPeca Materializar(SqliteDataReader reader)
        {
            return new CatalogoPeca
            {
                Id = ReadGuid(reader, 0),
                CodigoFabricante = ReadString(reader, 1),
                CodigoNormalizado = ReadString(reader, 2),
                Marca = ReadString(reader, 3),
                Nome = ReadString(reader, 4),
                Descricao = ReadString(reader, 5),
                Categoria = ReadString(reader, 6),
                Subcategoria = ReadString(reader, 7),
                Linha = ReadString(reader, 8),
                Aplicacao = ReadString(reader, 9),
                VeiculoAplicacao = ReadString(reader, 10),
                AnoInicial = ReadNullableInt(reader, 11),
                AnoFinal = ReadNullableInt(reader, 12),
                Voltagem = ReadString(reader, 13),
                Amperagem = ReadString(reader, 14),
                QuantidadeTerminais = ReadString(reader, 15),
                TipoProduto = ReadString(reader, 16),
                PaginaCatalogo = ReadString(reader, 17),
                FonteCatalogo = ReadString(reader, 18),
                ArquivoOrigem = ReadString(reader, 19),
                ObservacoesTecnicas = ReadString(reader, 20),
                ImagemUrl = ReadString(reader, 21),
                ImagemLocal = ReadString(reader, 22),
                StatusRevisao = ReadString(reader, 23),
                ProdutoEstoqueId = ReadNullableGuid(reader, 24),
                DataImportacao = ReadDate(reader, 25, DateTime.Now),
                DataAtualizacao = ReadDate(reader, 26, DateTime.Now),
                Ativo = ReadBool(reader, 27)
            };
        }

        private static void NormalizeItem(CatalogoPeca item)
        {
            item.CodigoFabricante = CatalogoCodeNormalizer.FormatManufacturerCode(item.CodigoFabricante, item.Marca);
            item.CodigoNormalizado = CatalogoCodeNormalizer.NormalizeCode(item.CodigoFabricante, item.Marca);
            item.Marca = CatalogoCodeNormalizer.SanitizeFreeText(item.Marca).ToUpperInvariant();
            item.Nome = CatalogoCodeNormalizer.SanitizeFreeText(item.Nome);
            item.Descricao = CatalogoCodeNormalizer.SanitizeFreeText(item.Descricao);
            item.Categoria = CatalogoCodeNormalizer.SanitizeFreeText(item.Categoria);
            item.Subcategoria = CatalogoCodeNormalizer.SanitizeFreeText(item.Subcategoria);
            item.Linha = CatalogoCodeNormalizer.SanitizeFreeText(item.Linha);
            item.Aplicacao = CatalogoCodeNormalizer.SanitizeFreeText(item.Aplicacao);
            item.VeiculoAplicacao = CatalogoCodeNormalizer.SanitizeFreeText(item.VeiculoAplicacao);
            item.Voltagem = CatalogoCodeNormalizer.SanitizeFreeText(item.Voltagem);
            item.Amperagem = CatalogoCodeNormalizer.SanitizeFreeText(item.Amperagem);
            item.QuantidadeTerminais = CatalogoCodeNormalizer.SanitizeFreeText(item.QuantidadeTerminais);
            item.TipoProduto = CatalogoCodeNormalizer.SanitizeFreeText(item.TipoProduto);
            item.PaginaCatalogo = CatalogoCodeNormalizer.SanitizeFreeText(item.PaginaCatalogo);
            item.FonteCatalogo = CatalogoCodeNormalizer.SanitizeFreeText(item.FonteCatalogo);
            item.ArquivoOrigem = CatalogoCodeNormalizer.SanitizeFreeText(item.ArquivoOrigem);
            item.ObservacoesTecnicas = CatalogoCodeNormalizer.SanitizeFreeText(item.ObservacoesTecnicas);
            item.ImagemUrl = CatalogoCodeNormalizer.SanitizeFreeText(item.ImagemUrl);
            item.ImagemLocal = CatalogoCodeNormalizer.SanitizeFreeText(item.ImagemLocal);
            item.StatusRevisao = CatalogoCodeNormalizer.NormalizeStatusForPersistence(item.StatusRevisao);
        }

        private static void AddCatalogoParameters(SqliteCommand command, CatalogoPeca item)
        {
            command.Parameters.AddWithValue("@Id", item.Id.ToString());
            command.Parameters.AddWithValue("@CodigoFabricante", ToDbString(item.CodigoFabricante));
            command.Parameters.AddWithValue("@CodigoNormalizado", ToDbString(item.CodigoNormalizado));
            command.Parameters.AddWithValue("@Marca", ToDbString(item.Marca));
            command.Parameters.AddWithValue("@Nome", ToDbString(item.Nome));
            command.Parameters.AddWithValue("@Descricao", ToDbString(item.Descricao));
            command.Parameters.AddWithValue("@Categoria", ToDbString(item.Categoria));
            command.Parameters.AddWithValue("@Subcategoria", ToDbString(item.Subcategoria));
            command.Parameters.AddWithValue("@Linha", ToDbString(item.Linha));
            command.Parameters.AddWithValue("@Aplicacao", ToDbString(item.Aplicacao));
            command.Parameters.AddWithValue("@VeiculoAplicacao", ToDbString(item.VeiculoAplicacao));
            command.Parameters.AddWithValue("@AnoInicial", item.AnoInicial.HasValue ? item.AnoInicial.Value : DBNull.Value);
            command.Parameters.AddWithValue("@AnoFinal", item.AnoFinal.HasValue ? item.AnoFinal.Value : DBNull.Value);
            command.Parameters.AddWithValue("@Voltagem", ToDbString(item.Voltagem));
            command.Parameters.AddWithValue("@Amperagem", ToDbString(item.Amperagem));
            command.Parameters.AddWithValue("@QuantidadeTerminais", ToDbString(item.QuantidadeTerminais));
            command.Parameters.AddWithValue("@TipoProduto", ToDbString(item.TipoProduto));
            command.Parameters.AddWithValue("@PaginaCatalogo", ToDbString(item.PaginaCatalogo));
            command.Parameters.AddWithValue("@FonteCatalogo", ToDbString(item.FonteCatalogo));
            command.Parameters.AddWithValue("@ArquivoOrigem", ToDbString(item.ArquivoOrigem));
            command.Parameters.AddWithValue("@ObservacoesTecnicas", ToDbString(item.ObservacoesTecnicas));
            command.Parameters.AddWithValue("@ImagemUrl", ToDbString(item.ImagemUrl));
            command.Parameters.AddWithValue("@ImagemLocal", ToDbString(item.ImagemLocal));
            command.Parameters.AddWithValue("@StatusRevisao", ToDbString(item.StatusRevisao));
            command.Parameters.AddWithValue("@ProdutoEstoqueId", item.ProdutoEstoqueId.HasValue ? item.ProdutoEstoqueId.Value.ToString() : DBNull.Value);
            command.Parameters.AddWithValue("@DataImportacao", item.DataImportacao.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("@DataAtualizacao", item.DataAtualizacao.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("@Ativo", item.Ativo ? 1 : 0);
        }

        private static object ToDbString(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
        }

        private static string ReadString(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? string.Empty : Convert.ToString(reader.GetValue(index)) ?? string.Empty;
        }

        private static bool ReadBool(SqliteDataReader reader, int index)
        {
            return !reader.IsDBNull(index) && Convert.ToInt32(reader.GetValue(index), CultureInfo.InvariantCulture) == 1;
        }

        private static int? ReadNullableInt(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? null : Convert.ToInt32(reader.GetValue(index), CultureInfo.InvariantCulture);
        }

        private static Guid ReadGuid(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) || !Guid.TryParse(Convert.ToString(reader.GetValue(index), CultureInfo.InvariantCulture), out var value)
                ? Guid.Empty
                : value;
        }

        private static Guid? ReadNullableGuid(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) || !Guid.TryParse(Convert.ToString(reader.GetValue(index), CultureInfo.InvariantCulture), out var value)
                ? null
                : value;
        }

        private static DateTime ReadDate(SqliteDataReader reader, int index, DateTime fallback)
        {
            return reader.IsDBNull(index) || !DateTime.TryParse(Convert.ToString(reader.GetValue(index), CultureInfo.InvariantCulture), out var value)
                ? fallback
                : value;
        }
    }
}
