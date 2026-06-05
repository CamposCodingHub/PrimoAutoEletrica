using System;
using System.Text.Json;

namespace PrimoAutoEletrica.Models
{
    public sealed class ProdutoImportacaoSnapshot
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false
        };

        public Guid ProdutoId { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public int QuantidadeEstoque { get; set; }
        public decimal PrecoCompra { get; set; }
        public decimal PrecoVenda { get; set; }
        public decimal MargemLucro { get; set; }
        public decimal ValorTotalEstoque { get; set; }
        public string UnidadeMedida { get; set; } = string.Empty;
        public string CodigoBarras { get; set; } = string.Empty;
        public string NCMS { get; set; } = string.Empty;
        public string CFOP { get; set; } = string.Empty;
        public DateTime? DataUltimaCompra { get; set; }
        public DateTime? DataUltimaAtualizacao { get; set; }

        public static ProdutoImportacaoSnapshot Criar(Produto produto)
        {
            return new ProdutoImportacaoSnapshot
            {
                ProdutoId = produto.Id,
                Categoria = produto.Categoria ?? string.Empty,
                QuantidadeEstoque = produto.QuantidadeEstoque,
                PrecoCompra = produto.PrecoCompra,
                PrecoVenda = produto.PrecoVenda,
                MargemLucro = produto.MargemLucro,
                ValorTotalEstoque = produto.ValorTotalEstoque,
                UnidadeMedida = produto.UnidadeMedida ?? string.Empty,
                CodigoBarras = produto.CodigoBarras ?? string.Empty,
                NCMS = produto.NCMS ?? string.Empty,
                CFOP = produto.CFOP ?? string.Empty,
                DataUltimaCompra = produto.DataUltimaCompra,
                DataUltimaAtualizacao = produto.DataUltimaAtualizacao
            };
        }

        public static ProdutoImportacaoSnapshot? FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<ProdutoImportacaoSnapshot>(json, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, JsonOptions);
        }

        public bool EquivaleA(ProdutoImportacaoSnapshot? outro)
        {
            if (outro == null)
            {
                return false;
            }

            return ProdutoId == outro.ProdutoId
                   && TextoIgual(Categoria, outro.Categoria)
                   && QuantidadeEstoque == outro.QuantidadeEstoque
                   && DecimalIgual(PrecoCompra, outro.PrecoCompra)
                   && DecimalIgual(PrecoVenda, outro.PrecoVenda)
                   && DecimalIgual(MargemLucro, outro.MargemLucro)
                   && DecimalIgual(ValorTotalEstoque, outro.ValorTotalEstoque)
                   && TextoIgual(UnidadeMedida, outro.UnidadeMedida)
                   && TextoIgual(CodigoBarras, outro.CodigoBarras)
                   && TextoIgual(NCMS, outro.NCMS)
                   && TextoIgual(CFOP, outro.CFOP)
                   && DataIgual(DataUltimaCompra, outro.DataUltimaCompra)
                   && DataIgual(DataUltimaAtualizacao, outro.DataUltimaAtualizacao);
        }

        private static bool TextoIgual(string? esquerdo, string? direito)
        {
            return string.Equals(esquerdo?.Trim() ?? string.Empty, direito?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private static bool DecimalIgual(decimal esquerdo, decimal direito)
        {
            return Math.Abs(esquerdo - direito) <= 0.0001m;
        }

        private static bool DataIgual(DateTime? esquerdo, DateTime? direito)
        {
            if (!esquerdo.HasValue && !direito.HasValue)
            {
                return true;
            }

            if (!esquerdo.HasValue || !direito.HasValue)
            {
                return false;
            }

            return Math.Abs((esquerdo.Value - direito.Value).TotalSeconds) <= 2;
        }
    }
}
