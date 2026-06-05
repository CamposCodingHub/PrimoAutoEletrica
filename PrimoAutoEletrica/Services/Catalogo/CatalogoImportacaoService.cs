using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace PrimoAutoEletrica.Services.Catalogo
{
    public sealed class CatalogoImportacaoService
    {
        private readonly CatalogoPecasService _catalogoPecasService;
        private readonly CatalogoPdfExtractorService _pdfExtractorService;
        private readonly CatalogoCsvExcelImporterService _csvExcelImporterService;
        private readonly CatalogoXmlImporterService _xmlImporterService;
        private readonly DatabaseService _databaseService;
        private readonly LoggerService _logger;

        public CatalogoImportacaoService(
            CatalogoPecasService? catalogoPecasService = null,
            CatalogoPdfExtractorService? pdfExtractorService = null,
            CatalogoCsvExcelImporterService? csvExcelImporterService = null,
            CatalogoXmlImporterService? xmlImporterService = null,
            DatabaseService? databaseService = null,
            LoggerService? logger = null)
        {
            _catalogoPecasService = catalogoPecasService ?? new CatalogoPecasService(databaseService, logger);
            _pdfExtractorService = pdfExtractorService ?? new CatalogoPdfExtractorService(logger);
            _csvExcelImporterService = csvExcelImporterService ?? new CatalogoCsvExcelImporterService(logger);
            _xmlImporterService = xmlImporterService ?? new CatalogoXmlImporterService();
            _databaseService = databaseService ?? global::PrimoAutoEletrica.App.Database;
            _logger = logger ?? global::PrimoAutoEletrica.App.Logger;
        }

        public CatalogoImportacaoPreview CriarPreviaImportacao(
            string caminhoArquivo,
            string? tipoArquivo = null,
            string? fonteCatalogo = null,
            string? marca = null)
        {
            if (string.IsNullOrWhiteSpace(caminhoArquivo))
            {
                throw new InvalidOperationException("Informe o caminho do arquivo do catalogo.");
            }

            if (!File.Exists(caminhoArquivo))
            {
                throw new FileNotFoundException("Arquivo de catalogo nao encontrado.", caminhoArquivo);
            }

            var tipoDetectado = DetectarTipoArquivo(caminhoArquivo, tipoArquivo);
            var (marcaDetectada, fonte) = CatalogoMarcaDetector.ResolverMarcaEFonte(
                caminhoArquivo,
                marca,
                !string.IsNullOrWhiteSpace(fonteCatalogo) ? fonteCatalogo.Trim() : null);

            _logger.LogInfo($"Inicio da previa de importacao de catalogo. Arquivo='{caminhoArquivo}', Tipo='{tipoDetectado}', Fonte='{fonte}', Marca='{marcaDetectada}'.");

            var preview = new CatalogoImportacaoPreview
            {
                ArquivoOrigem = caminhoArquivo,
                TipoArquivo = tipoDetectado,
                FonteDetectada = fonte,
                MarcaDetectada = marcaDetectada
            };

            (List<CatalogoImportacaoPreviewItem> Itens, List<CatalogoImportacaoErro> Erros) resultado = tipoDetectado switch
            {
                "PDF" => _pdfExtractorService.Extrair(caminhoArquivo, fonte, marcaDetectada),
                "CSV" => _csvExcelImporterService.ImportarCsv(caminhoArquivo, fonte, marcaDetectada),
                "EXCEL" => _csvExcelImporterService.ImportarExcel(caminhoArquivo, fonte, marcaDetectada),
                "XML" => _xmlImporterService.Importar(caminhoArquivo, fonte, marcaDetectada),
                _ => throw new InvalidOperationException($"Tipo de arquivo '{tipoDetectado}' ainda nao e suportado.")
            };

            preview.Erros.AddRange(resultado.Erros);

            foreach (var item in resultado.Itens)
            {
                ValidarPreviewItem(item, tipoDetectado, fonte, marcaDetectada, preview.ArquivoOrigem, preview.Erros);
                preview.Itens.Add(item);
            }

            preview.TotalItens = preview.Itens.Count;
            preview.TotalDuplicadosProvaveis = preview.Itens.Count(item => item.DuplicadoProvavel);
            preview.TotalJaExistentes = preview.Itens.Count(item => string.Equals(item.StatusRevisao, "Ja existente", StringComparison.OrdinalIgnoreCase));
            preview.TotalIncompletos = preview.Itens.Count(item => string.Equals(item.StatusRevisao, "Incompleto", StringComparison.OrdinalIgnoreCase));
            preview.TotalComErro = preview.Erros.Count;
            preview.TotalValidos = preview.Itens.Count(item =>
                !string.Equals(item.StatusRevisao, "Ja existente", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(item.StatusRevisao, "Incompleto", StringComparison.OrdinalIgnoreCase));

            CatalogoImportacaoQualidade.RegistrarAlertasNaPrevia(preview);

            return preview;
        }

        public CatalogoImportacao ConfirmarImportacao(CatalogoImportacaoPreview preview)
        {
            if (preview == null)
            {
                throw new ArgumentNullException(nameof(preview));
            }

            var importacao = new CatalogoImportacao
            {
                Id = Guid.NewGuid(),
                DataImportacao = DateTime.Now,
                ArquivoNome = Path.GetFileName(preview.ArquivoOrigem),
                ArquivoCaminho = preview.ArquivoOrigem,
                TipoArquivo = preview.TipoArquivo,
                FonteCatalogo = preview.FonteDetectada,
                MarcaDetectada = preview.MarcaDetectada,
                TotalLidos = preview.TotalItens,
                Usuario = ObterUsuarioAtual(),
                Status = "Concluida"
            };

            _logger.LogInfo($"Confirmando importacao de catalogo '{importacao.ArquivoNome}' com {preview.TotalItens} item(ns).");

            var log = new StringBuilder();
            var duplicados = new List<CatalogoImportacaoPreviewItem>();
            var importados = new List<CatalogoPeca>();
            var erros = preview.Erros.Select(CloneErro).ToList();

            foreach (var previewItem in preview.Itens)
            {
                try
                {
                    if (string.Equals(previewItem.StatusRevisao, "Ja existente", StringComparison.OrdinalIgnoreCase))
                    {
                        duplicados.Add(previewItem);
                        log.AppendLine($"IGNORADO_EXISTENTE | {previewItem.CodigoFabricante} | {previewItem.Marca}");
                        continue;
                    }

                    var statusPersistente = CatalogoCodeNormalizer.NormalizeStatusForPersistence(previewItem.StatusRevisao);
                    var item = ConverterPreviewParaCatalogo(previewItem, statusPersistente);
                    _catalogoPecasService.Inserir(item);
                    importados.Add(item);
                    log.AppendLine($"IMPORTADO | {item.CodigoFabricante} | {item.Marca} | {item.StatusRevisao}");
                }
                catch (Exception ex)
                {
                    erros.Add(new CatalogoImportacaoErro
                    {
                        ImportacaoId = importacao.Id,
                        LinhaOrigem = previewItem.Linha,
                        CodigoDetectado = previewItem.CodigoFabricante,
                        MensagemErro = ex.Message,
                        ConteudoOriginal = previewItem.ConteudoOriginal,
                        DataErro = DateTime.Now
                    });

                    log.AppendLine($"ERRO | {previewItem.CodigoFabricante} | {ex.Message}");
                }
            }

            importacao.TotalImportados = importados.Count;
            importacao.TotalDuplicados = duplicados.Count + preview.TotalJaExistentes;
            importacao.TotalComErro = erros.Count;
            importacao.Resumo = $"Lidos={importacao.TotalLidos}; Importados={importacao.TotalImportados}; Duplicados={importacao.TotalDuplicados}; Erros={importacao.TotalComErro}";
            importacao.LogDetalhado = log.ToString();
            if (erros.Count > 0)
            {
                importacao.Status = importados.Count > 0 ? "Parcial" : "Erro";
            }

            PersistirImportacao(importacao, erros);
            var relatorio = GerarRelatorioImportacao(importacao, importados, duplicados, erros);
            _logger.LogInfo($"Importacao de catalogo concluida. Arquivo='{importacao.ArquivoNome}', Status='{importacao.Status}', Relatorio='{relatorio}'.");

            return importacao;
        }

        public List<CatalogoImportacao> ObterHistoricoImportacoes(int limit = 100)
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    Id,
                    DataImportacao,
                    ArquivoNome,
                    ArquivoCaminho,
                    TipoArquivo,
                    FonteCatalogo,
                    MarcaDetectada,
                    TotalLidos,
                    TotalImportados,
                    TotalDuplicados,
                    TotalComErro,
                    Status,
                    Resumo,
                    LogDetalhado,
                    Usuario
                FROM CatalogoImportacoes
                ORDER BY DataImportacao DESC
                LIMIT @Limite;";
            command.Parameters.AddWithValue("@Limite", limit);

            var historico = new List<CatalogoImportacao>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                historico.Add(new CatalogoImportacao
                {
                    Id = Guid.TryParse(Convert.ToString(reader.GetValue(0), CultureInfo.InvariantCulture), out var id) ? id : Guid.Empty,
                    DataImportacao = DateTime.TryParse(Convert.ToString(reader.GetValue(1), CultureInfo.InvariantCulture), out var data) ? data : DateTime.Now,
                    ArquivoNome = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    ArquivoCaminho = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    TipoArquivo = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    FonteCatalogo = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    MarcaDetectada = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    TotalLidos = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                    TotalImportados = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                    TotalDuplicados = reader.IsDBNull(9) ? 0 : reader.GetInt32(9),
                    TotalComErro = reader.IsDBNull(10) ? 0 : reader.GetInt32(10),
                    Status = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                    Resumo = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                    LogDetalhado = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                    Usuario = reader.IsDBNull(14) ? string.Empty : reader.GetString(14)
                });
            }

            return historico;
        }

        public List<CatalogoImportacaoErro> ObterErrosImportacao(Guid importacaoId)
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    Id,
                    ImportacaoId,
                    LinhaOrigem,
                    CodigoDetectado,
                    MensagemErro,
                    ConteudoOriginal,
                    DataErro
                FROM CatalogoImportacaoErros
                WHERE ImportacaoId = @ImportacaoId
                ORDER BY DataErro DESC;";
            command.Parameters.AddWithValue("@ImportacaoId", importacaoId.ToString());

            var erros = new List<CatalogoImportacaoErro>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                erros.Add(new CatalogoImportacaoErro
                {
                    Id = Guid.TryParse(Convert.ToString(reader.GetValue(0), CultureInfo.InvariantCulture), out var id) ? id : Guid.Empty,
                    ImportacaoId = Guid.TryParse(Convert.ToString(reader.GetValue(1), CultureInfo.InvariantCulture), out var importacao) ? importacao : null,
                    LinhaOrigem = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    CodigoDetectado = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    MensagemErro = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    ConteudoOriginal = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    DataErro = DateTime.TryParse(Convert.ToString(reader.GetValue(6), CultureInfo.InvariantCulture), out var data) ? data : DateTime.Now
                });
            }

            return erros;
        }

        private void ValidarPreviewItem(
            CatalogoImportacaoPreviewItem item,
            string tipoArquivo,
            string fonteCatalogo,
            string marcaPadrao,
            string arquivoOrigem,
            ICollection<CatalogoImportacaoErro> erros)
        {
            item.FonteCatalogo = string.IsNullOrWhiteSpace(item.FonteCatalogo) ? fonteCatalogo : item.FonteCatalogo.Trim();
            item.Marca = string.IsNullOrWhiteSpace(item.Marca) ? marcaPadrao.Trim().ToUpperInvariant() : item.Marca.Trim().ToUpperInvariant();
            item.CodigoFabricante = CatalogoCodeNormalizer.FormatManufacturerCode(item.CodigoFabricante, item.Marca);
            item.CodigoNormalizado = CatalogoCodeNormalizer.NormalizeCode(item.CodigoFabricante, item.Marca);
            item.Nome = CatalogoCodeNormalizer.SanitizeFreeText(item.Nome);
            item.Descricao = CatalogoCodeNormalizer.SanitizeFreeText(item.Descricao);
            item.Categoria = CatalogoCodeNormalizer.SanitizeFreeText(item.Categoria);
            item.Subcategoria = CatalogoCodeNormalizer.SanitizeFreeText(item.Subcategoria);
            item.Aplicacao = CatalogoCodeNormalizer.SanitizeFreeText(item.Aplicacao);
            item.PaginaCatalogo = CatalogoCodeNormalizer.SanitizeFreeText(item.PaginaCatalogo);
            item.ArquivoOrigem = string.IsNullOrWhiteSpace(item.ArquivoOrigem) ? Path.GetFileName(arquivoOrigem) : item.ArquivoOrigem;
            item.ObservacoesTecnicas = CatalogoCodeNormalizer.SanitizeFreeText(item.ObservacoesTecnicas);
            item.StatusRevisao = CatalogoCodeNormalizer.NormalizePreviewStatus(item.StatusRevisao);

            if (string.Equals(tipoArquivo, "PDF", StringComparison.OrdinalIgnoreCase))
            {
                item.StatusRevisao = "Pendente de revisao";
            }

            if (string.IsNullOrWhiteSpace(item.CodigoFabricante) || string.IsNullOrWhiteSpace(item.CodigoNormalizado))
            {
                item.StatusRevisao = "Incompleto";
                item.MensagemValidacao = "Item sem codigo fabricante consistente.";
                erros.Add(new CatalogoImportacaoErro
                {
                    LinhaOrigem = item.Linha,
                    CodigoDetectado = item.CodigoFabricante,
                    MensagemErro = item.MensagemValidacao,
                    ConteudoOriginal = item.ConteudoOriginal,
                    DataErro = DateTime.Now
                });
                return;
            }

            if (string.IsNullOrWhiteSpace(item.Nome) && string.IsNullOrWhiteSpace(item.Descricao))
            {
                if (string.Equals(tipoArquivo, "PDF", StringComparison.OrdinalIgnoreCase))
                {
                    item.Nome = CatalogoPdfTextParser.GerarNomeFallback(item.Marca, item.CodigoFabricante);
                    item.MensagemValidacao = "Item sem nome legivel no PDF; nome generico aplicado.";
                }
                else
                {
                    item.StatusRevisao = "Incompleto";
                    item.MensagemValidacao = "Item sem nome ou descricao suficiente.";
                    return;
                }
            }
            else if (string.IsNullOrWhiteSpace(item.Nome))
            {
                item.Nome = CatalogoPdfTextParser.GerarNomeFallback(item.Marca, item.CodigoFabricante);
            }

            if (string.Equals(tipoArquivo, "PDF", StringComparison.OrdinalIgnoreCase) &&
                CatalogoPdfTextParser.EhNomeFallback(item.Nome, item.Marca, item.CodigoFabricante))
            {
                item.MensagemValidacao = string.IsNullOrWhiteSpace(item.Descricao)
                    ? "Nome generico detectado; revisar descricao e titulo."
                    : item.MensagemValidacao;
            }

            if (_catalogoPecasService.ExisteCodigo(item.CodigoNormalizado, item.Marca, item.FonteCatalogo))
            {
                item.StatusRevisao = "Ja existente";
                item.MensagemValidacao = "Codigo ja existe para a mesma marca e fonte.";
                return;
            }

            var duplicadoProvavel = _catalogoPecasService.ObterDuplicadoProvavel(item.CodigoNormalizado, item.Marca, item.FonteCatalogo);
            if (duplicadoProvavel != null)
            {
                item.DuplicadoProvavel = true;
                item.StatusRevisao = "DuplicadoProvavel";
                item.MensagemValidacao = $"Possivel duplicidade com a fonte '{duplicadoProvavel.FonteCatalogo}'.";
                return;
            }

            if (string.Equals(item.StatusRevisao, "Pendente de revisao", StringComparison.OrdinalIgnoreCase))
            {
                item.MensagemValidacao = "Item extraido de PDF e marcado para revisao.";
                return;
            }

            item.StatusRevisao = "Novo";
            item.MensagemValidacao = "Pronto para importacao.";
        }

        private static CatalogoPeca ConverterPreviewParaCatalogo(CatalogoImportacaoPreviewItem previewItem, string statusPersistente)
        {
            return new CatalogoPeca
            {
                Id = Guid.NewGuid(),
                CodigoFabricante = previewItem.CodigoFabricante,
                CodigoNormalizado = previewItem.CodigoNormalizado,
                Marca = previewItem.Marca,
                Nome = previewItem.Nome,
                Descricao = previewItem.Descricao,
                Categoria = previewItem.Categoria,
                Subcategoria = previewItem.Subcategoria,
                Aplicacao = previewItem.Aplicacao,
                VeiculoAplicacao = previewItem.VeiculoAplicacao,
                Voltagem = previewItem.Voltagem,
                Amperagem = previewItem.Amperagem,
                QuantidadeTerminais = previewItem.QuantidadeTerminais,
                TipoProduto = previewItem.TipoProduto,
                PaginaCatalogo = previewItem.PaginaCatalogo,
                FonteCatalogo = previewItem.FonteCatalogo,
                ArquivoOrigem = previewItem.ArquivoOrigem,
                ObservacoesTecnicas = previewItem.ObservacoesTecnicas,
                StatusRevisao = statusPersistente,
                DataImportacao = DateTime.Now,
                DataAtualizacao = DateTime.Now,
                Ativo = true
            };
        }

        private void PersistirImportacao(CatalogoImportacao importacao, IReadOnlyCollection<CatalogoImportacaoErro> erros)
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            using var insertImportacao = connection.CreateCommand();
            insertImportacao.Transaction = transaction;
            insertImportacao.CommandText = @"
                INSERT INTO CatalogoImportacoes
                (
                    Id,
                    DataImportacao,
                    ArquivoNome,
                    ArquivoCaminho,
                    TipoArquivo,
                    FonteCatalogo,
                    MarcaDetectada,
                    TotalLidos,
                    TotalImportados,
                    TotalDuplicados,
                    TotalComErro,
                    Status,
                    Resumo,
                    LogDetalhado,
                    Usuario
                )
                VALUES
                (
                    @Id,
                    @DataImportacao,
                    @ArquivoNome,
                    @ArquivoCaminho,
                    @TipoArquivo,
                    @FonteCatalogo,
                    @MarcaDetectada,
                    @TotalLidos,
                    @TotalImportados,
                    @TotalDuplicados,
                    @TotalComErro,
                    @Status,
                    @Resumo,
                    @LogDetalhado,
                    @Usuario
                );";
            insertImportacao.Parameters.AddWithValue("@Id", importacao.Id.ToString());
            insertImportacao.Parameters.AddWithValue("@DataImportacao", importacao.DataImportacao.ToString("yyyy-MM-dd HH:mm:ss"));
            insertImportacao.Parameters.AddWithValue("@ArquivoNome", importacao.ArquivoNome);
            insertImportacao.Parameters.AddWithValue("@ArquivoCaminho", importacao.ArquivoCaminho);
            insertImportacao.Parameters.AddWithValue("@TipoArquivo", importacao.TipoArquivo);
            insertImportacao.Parameters.AddWithValue("@FonteCatalogo", importacao.FonteCatalogo);
            insertImportacao.Parameters.AddWithValue("@MarcaDetectada", importacao.MarcaDetectada);
            insertImportacao.Parameters.AddWithValue("@TotalLidos", importacao.TotalLidos);
            insertImportacao.Parameters.AddWithValue("@TotalImportados", importacao.TotalImportados);
            insertImportacao.Parameters.AddWithValue("@TotalDuplicados", importacao.TotalDuplicados);
            insertImportacao.Parameters.AddWithValue("@TotalComErro", importacao.TotalComErro);
            insertImportacao.Parameters.AddWithValue("@Status", importacao.Status);
            insertImportacao.Parameters.AddWithValue("@Resumo", importacao.Resumo);
            insertImportacao.Parameters.AddWithValue("@LogDetalhado", importacao.LogDetalhado);
            insertImportacao.Parameters.AddWithValue("@Usuario", importacao.Usuario);
            insertImportacao.ExecuteNonQuery();

            foreach (var erro in erros)
            {
                using var insertErro = connection.CreateCommand();
                insertErro.Transaction = transaction;
                insertErro.CommandText = @"
                    INSERT INTO CatalogoImportacaoErros
                    (
                        Id,
                        ImportacaoId,
                        LinhaOrigem,
                        CodigoDetectado,
                        MensagemErro,
                        ConteudoOriginal,
                        DataErro
                    )
                    VALUES
                    (
                        @Id,
                        @ImportacaoId,
                        @LinhaOrigem,
                        @CodigoDetectado,
                        @MensagemErro,
                        @ConteudoOriginal,
                        @DataErro
                    );";
                insertErro.Parameters.AddWithValue("@Id", erro.Id == Guid.Empty ? Guid.NewGuid().ToString() : erro.Id.ToString());
                insertErro.Parameters.AddWithValue("@ImportacaoId", importacao.Id.ToString());
                insertErro.Parameters.AddWithValue("@LinhaOrigem", erro.LinhaOrigem ?? string.Empty);
                insertErro.Parameters.AddWithValue("@CodigoDetectado", erro.CodigoDetectado ?? string.Empty);
                insertErro.Parameters.AddWithValue("@MensagemErro", erro.MensagemErro ?? string.Empty);
                insertErro.Parameters.AddWithValue("@ConteudoOriginal", erro.ConteudoOriginal ?? string.Empty);
                insertErro.Parameters.AddWithValue("@DataErro", erro.DataErro.ToString("yyyy-MM-dd HH:mm:ss"));
                insertErro.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        private static CatalogoImportacaoErro CloneErro(CatalogoImportacaoErro erro)
        {
            return new CatalogoImportacaoErro
            {
                Id = Guid.NewGuid(),
                ImportacaoId = erro.ImportacaoId,
                LinhaOrigem = erro.LinhaOrigem,
                CodigoDetectado = erro.CodigoDetectado,
                MensagemErro = erro.MensagemErro,
                ConteudoOriginal = erro.ConteudoOriginal,
                DataErro = erro.DataErro == default ? DateTime.Now : erro.DataErro
            };
        }

        private string GerarRelatorioImportacao(
            CatalogoImportacao importacao,
            IReadOnlyCollection<CatalogoPeca> importados,
            IReadOnlyCollection<CatalogoImportacaoPreviewItem> duplicados,
            IReadOnlyCollection<CatalogoImportacaoErro> erros)
        {
            var pastaRelatorios = CatalogoWorkspacePaths.GetImportReportDirectory();
            var caminho = Path.Combine(
                pastaRelatorios,
                $"RELATORIO_IMPORTACAO_CATALOGO_{DateTime.Now:yyyyMMdd_HHmmss}.md");

            var builder = new StringBuilder();
            builder.AppendLine("# Relatorio de Importacao de Catalogo");
            builder.AppendLine();
            builder.AppendLine($"- Arquivo: `{importacao.ArquivoNome}`");
            builder.AppendLine($"- Fonte: `{importacao.FonteCatalogo}`");
            builder.AppendLine($"- Marca: `{importacao.MarcaDetectada}`");
            builder.AppendLine($"- Data: `{importacao.DataImportacao:dd/MM/yyyy HH:mm:ss}`");
            builder.AppendLine($"- Total lidos: `{importacao.TotalLidos}`");
            builder.AppendLine($"- Total importados: `{importacao.TotalImportados}`");
            builder.AppendLine($"- Total duplicados: `{importacao.TotalDuplicados}`");
            builder.AppendLine($"- Total erros: `{importacao.TotalComErro}`");
            builder.AppendLine($"- Usuario: `{importacao.Usuario}`");
            builder.AppendLine();
            builder.AppendLine("## Primeiros 50 importados");
            foreach (var item in importados.Take(50))
            {
                builder.AppendLine($"- `{item.CodigoFabricante}` | {item.Marca} | {item.NomeExibicao} | {item.Categoria} | {item.StatusRevisao}");
            }

            builder.AppendLine();
            builder.AppendLine("## Primeiros 50 duplicados");
            foreach (var item in duplicados.Take(50))
            {
                builder.AppendLine($"- `{item.CodigoFabricante}` | {item.Marca} | {item.MensagemValidacao}");
            }

            builder.AppendLine();
            builder.AppendLine("## Erros");
            foreach (var erro in erros.Take(200))
            {
                builder.AppendLine($"- Linha `{erro.LinhaOrigem}` | Codigo `{erro.CodigoDetectado}` | {erro.MensagemErro}");
            }

            builder.AppendLine();
            builder.AppendLine("## Observacoes");
            builder.AppendLine($"- Status final: `{importacao.Status}`");
            builder.AppendLine($"- Resumo: `{importacao.Resumo}`");

            File.WriteAllText(caminho, builder.ToString());
            return caminho;
        }

        private static string DetectarTipoArquivo(string caminhoArquivo, string? tipoForcado)
        {
            if (!string.IsNullOrWhiteSpace(tipoForcado) &&
                !string.Equals(tipoForcado.Trim(), "AUTO", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(tipoForcado.Trim(), "AUTO DETECTAR", StringComparison.OrdinalIgnoreCase))
            {
                return tipoForcado.Trim().ToUpperInvariant();
            }

            return Path.GetExtension(caminhoArquivo).ToLowerInvariant() switch
            {
                ".pdf" => "PDF",
                ".csv" => "CSV",
                ".xlsx" => "EXCEL",
                ".xls" => "EXCEL",
                ".xml" => "XML",
                _ => throw new InvalidOperationException("Nao foi possivel detectar automaticamente o tipo do arquivo.")
            };
        }

        private static string ObterUsuarioAtual()
        {
            return global::PrimoAutoEletrica.App.Session.IsAuthenticated
                ? global::PrimoAutoEletrica.App.Session.UserName
                : "Sistema";
        }
    }
}
