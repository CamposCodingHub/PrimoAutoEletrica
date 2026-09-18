using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace PrimoAutoEletrica.Services.Catalogo
{
    public static class CatalogoArquivoSupport
    {
        public const long TamanhoMaximoBytes = 500L * 1024L * 1024L;

        private static readonly HashSet<string> ExtensoesPdf = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf"
        };

        private static readonly HashSet<string> ExtensoesImagem = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".bmp"
        };

        private static readonly HashSet<string> ExtensoesPlanilha = new(StringComparer.OrdinalIgnoreCase)
        {
            ".csv",
            ".xlsx",
            ".xls"
        };

        private static readonly HashSet<string> ExtensoesXml = new(StringComparer.OrdinalIgnoreCase)
        {
            ".xml"
        };

        public static string FiltroDialogo =>
            "Catalogos e imagens (PDF, JPG, PNG, WEBP, CSV, Excel, XML)|*.pdf;*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.csv;*.xlsx;*.xls;*.xml|" +
            "PDF|*.pdf|" +
            "Imagens|*.jpg;*.jpeg;*.png;*.webp;*.bmp|" +
            "Planilhas|*.csv;*.xlsx;*.xls|" +
            "XML|*.xml|" +
            "Todos os arquivos|*.*";

        public static IReadOnlyList<string> TiposArquivoCombo => new[]
        {
            "AUTO",
            "PDF",
            "IMAGEM",
            "CSV",
            "EXCEL",
            "XML"
        };

        public static string? ResolverArquivoLocal(CatalogoPeca? item)
        {
            if (item == null)
            {
                return null;
            }

            foreach (var candidato in new[] { item.ImagemLocal, item.ImagemUrl })
            {
                if (!string.IsNullOrWhiteSpace(candidato) && File.Exists(candidato))
                {
                    return candidato;
                }
            }

            return null;
        }

        public static bool EhImagem(string? caminho) =>
            ExtensoesImagem.Contains(ObterExtensao(caminho));

        public static bool EhPdf(string? caminho) =>
            ExtensoesPdf.Contains(ObterExtensao(caminho));

        public static bool EhSuportado(string? caminho)
        {
            var extensao = ObterExtensao(caminho);
            return ExtensoesPdf.Contains(extensao)
                   || ExtensoesImagem.Contains(extensao)
                   || ExtensoesPlanilha.Contains(extensao)
                   || ExtensoesXml.Contains(extensao);
        }

        public static string DetectarTipo(string caminhoArquivo, string? tipoForcado = null)
        {
            if (!string.IsNullOrWhiteSpace(tipoForcado) &&
                !string.Equals(tipoForcado.Trim(), "AUTO", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(tipoForcado.Trim(), "AUTO DETECTAR", StringComparison.OrdinalIgnoreCase))
            {
                return NormalizarTipo(tipoForcado);
            }

            var extensao = ObterExtensao(caminhoArquivo);
            if (ExtensoesPdf.Contains(extensao))
            {
                return "PDF";
            }

            if (ExtensoesImagem.Contains(extensao))
            {
                return "IMAGEM";
            }

            if (string.Equals(extensao, ".csv", StringComparison.OrdinalIgnoreCase))
            {
                return "CSV";
            }

            if (string.Equals(extensao, ".xlsx", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extensao, ".xls", StringComparison.OrdinalIgnoreCase))
            {
                return "EXCEL";
            }

            if (ExtensoesXml.Contains(extensao))
            {
                return "XML";
            }

            throw new InvalidOperationException(
                "Formato nao suportado. Use PDF, imagem (JPG, JPEG, PNG, WEBP), CSV, Excel ou XML.");
        }

        public static string ValidarArquivo(string? caminhoArquivo)
        {
            if (string.IsNullOrWhiteSpace(caminhoArquivo))
            {
                throw new InvalidOperationException("Informe o arquivo do catalogo.");
            }

            var caminho = caminhoArquivo.Trim().Trim('"');
            if (!File.Exists(caminho))
            {
                throw new FileNotFoundException(
                    $"Arquivo de catalogo nao encontrado:\n{caminho}",
                    caminho);
            }

            var info = new FileInfo(caminho);
            if (info.Length <= 0)
            {
                throw new InvalidOperationException("O arquivo selecionado esta vazio.");
            }

            if (info.Length > TamanhoMaximoBytes)
            {
                throw new InvalidOperationException(
                    $"O arquivo tem {FormatarTamanho(info.Length)} e ultrapassa o limite de {FormatarTamanho(TamanhoMaximoBytes)}. " +
                    "Divida o catalogo ou use uma versao menor.");
            }

            if (!EhSuportado(caminho))
            {
                throw new InvalidOperationException(
                    $"A extensao '{ObterExtensao(caminho)}' nao e suportada. " +
                    "Aceitos: PDF, JPG, JPEG, PNG, WEBP, CSV, XLS, XLSX e XML.");
            }

            return Path.GetFullPath(caminho);
        }

        public static string CopiarParaWorkspace(string caminhoOrigem)
        {
            var origem = ValidarArquivo(caminhoOrigem);
            var destinoDir = CatalogoWorkspacePaths.GetMediaDirectory();
            var extensao = ObterExtensao(origem);
            var nomeOriginal = Path.GetFileNameWithoutExtension(origem);
            var segmento = SanitizarSegmentoArquivo(nomeOriginal);
            var destino = Path.Combine(
                destinoDir,
                $"{DateTime.Now:yyyyMMddHHmmssfff}_{segmento}_{Guid.NewGuid():N}{extensao}");

            try
            {
                File.Copy(origem, destino, overwrite: true);
            }
            catch (Exception ex)
            {
                throw new IOException(
                    $"Nao foi possivel copiar o arquivo '{Path.GetFileName(origem)}' para o catalogo: {ex.Message}",
                    ex);
            }

            return destino;
        }

        public static CatalogoImportacaoPreviewItem CriarItemDocumento(
            string caminhoOriginal,
            string caminhoArmazenado,
            string fonteCatalogo,
            string marca,
            string tipoArquivo)
        {
            var nomeOriginal = Path.GetFileName(caminhoOriginal);
            var nomeSemExtensao = Path.GetFileNameWithoutExtension(caminhoOriginal);
            var codigo = GerarCodigoDocumento(nomeSemExtensao, marca);
            var ehImagem = string.Equals(tipoArquivo, "IMAGEM", StringComparison.OrdinalIgnoreCase) || EhImagem(caminhoOriginal);

            return new CatalogoImportacaoPreviewItem
            {
                CodigoFabricante = codigo,
                CodigoNormalizado = CatalogoCodeNormalizer.NormalizeCode(codigo, marca),
                Marca = marca,
                Nome = string.IsNullOrWhiteSpace(nomeSemExtensao) ? nomeOriginal : nomeSemExtensao.Trim(),
                Descricao = ehImagem
                    ? "Foto/imagem de catalogo inserida para consulta visual."
                    : "Arquivo de catalogo inserido para consulta visual (PDF sem pecas extraidas automaticamente).",
                Categoria = InferirCategoriaDoNome(nomeSemExtensao),
                FonteCatalogo = fonteCatalogo,
                ArquivoOrigem = nomeOriginal,
                PaginaCatalogo = ehImagem ? "Imagem" : "Documento",
                ObservacoesTecnicas = $"Arquivo armazenado em: {caminhoArmazenado}",
                ConteudoOriginal = nomeOriginal,
                Linha = "Arquivo",
                StatusRevisao = "Pendente de revisao",
                MensagemValidacao = ehImagem
                    ? "Imagem inserida. Abra o arquivo para consultar o catalogo visual."
                    : "Nenhum codigo de peca foi extraido. O arquivo foi inserido para consulta visual.",
                ImagemUrl = ehImagem ? caminhoArmazenado : string.Empty,
                ImagemLocal = caminhoArmazenado
            };
        }

        public static string GerarCodigoDocumento(string? nomeArquivo, string? marca)
        {
            var baseNome = CatalogoCodeNormalizer.NormalizeHeader(nomeArquivo);
            if (string.IsNullOrWhiteSpace(baseNome))
            {
                baseNome = "catalogo";
            }

            if (baseNome.Length > 24)
            {
                baseNome = baseNome.Substring(0, 24);
            }

            var prefixo = string.IsNullOrWhiteSpace(marca) ? "CAT" : marca.Trim().ToUpperInvariant();
            if (prefixo.Length > 6)
            {
                prefixo = prefixo.Substring(0, 6);
            }

            return $"{prefixo}-{baseNome.ToUpperInvariant()}";
        }

        public static string InferirCategoriaDoNome(string? nomeArquivo)
        {
            var normalizado = CatalogoCodeNormalizer.NormalizeHeader(nomeArquivo);
            if (string.IsNullOrWhiteSpace(normalizado))
            {
                return "Catalogo visual";
            }

            if (normalizado.Contains("rolament"))
            {
                return "Rolamentos";
            }

            if (normalizado.Contains("regulador"))
            {
                return "Reguladores";
            }

            if (normalizado.Contains("retificador"))
            {
                return "Retificadores";
            }

            if (normalizado.Contains("portaescov") || normalizado.Contains("escova"))
            {
                return "Porta-escovas";
            }

            return "Catalogo visual";
        }

        public static string FormatarTamanho(long bytes)
        {
            if (bytes < 1024)
            {
                return $"{bytes} B";
            }

            double value = bytes / 1024d;
            var unidade = "KB";
            if (value >= 1024)
            {
                value /= 1024d;
                unidade = "MB";
            }

            if (value >= 1024)
            {
                value /= 1024d;
                unidade = "GB";
            }

            return string.Format(CultureInfo.GetCultureInfo("pt-BR"), "{0:0.#} {1}", value, unidade);
        }

        public static string SanitizarSegmentoArquivo(string? value)
        {
            var text = string.IsNullOrWhiteSpace(value) ? "catalogo" : value.Trim();
            var builder = new StringBuilder(text.Length);
            foreach (var character in text.Normalize(NormalizationForm.FormC))
            {
                if (Path.GetInvalidFileNameChars().Contains(character))
                {
                    builder.Append('_');
                    continue;
                }

                builder.Append(character == ' ' ? '_' : character);
            }

            var resultado = builder.ToString().Trim('_');
            if (string.IsNullOrWhiteSpace(resultado))
            {
                return "catalogo";
            }

            return resultado.Length > 48 ? resultado.Substring(0, 48) : resultado;
        }

        private static string ObterExtensao(string? caminho)
        {
            return string.IsNullOrWhiteSpace(caminho)
                ? string.Empty
                : Path.GetExtension(caminho.Trim().Trim('"'));
        }

        private static string NormalizarTipo(string tipo)
        {
            var valor = tipo.Trim().ToUpperInvariant();
            return valor switch
            {
                "IMAGE" or "FOTO" or "IMAGEM" or "JPG" or "JPEG" or "PNG" or "WEBP" or "BMP" => "IMAGEM",
                "XLS" or "XLSX" => "EXCEL",
                _ => valor
            };
        }
    }
}
