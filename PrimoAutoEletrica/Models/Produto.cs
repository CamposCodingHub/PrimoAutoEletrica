using System;

namespace PrimoAutoEletrica.Models
{
    public class Produto
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // DADOS BASICOS
        public string Codigo { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;

        // FORNECEDOR
        public Guid? FornecedorId { get; set; }
        public string Fornecedor { get; set; } = string.Empty;
        public string CNPJFornecedor { get; set; } = string.Empty;
        public string ContatoFornecedor { get; set; } = string.Empty;
        public string TelefoneFornecedor { get; set; } = string.Empty;

        // ESTOQUE
        public int QuantidadeEstoque { get; set; }
        public int QuantidadeMinima { get; set; }
        public int QuantidadeMaxima { get; set; }
        public string Localizacao { get; set; } = string.Empty;
        public string Prateleira { get; set; } = string.Empty;
        public string Gaveta { get; set; } = string.Empty;

        // FINANCEIRO
        public decimal PrecoCompra { get; set; }
        public decimal PrecoVenda { get; set; }
        public decimal MargemLucro { get; set; }
        public decimal ValorTotalEstoque { get; set; }

        // ESPECIFICACOES
        public string UnidadeMedida { get; set; } = string.Empty;
        public string Peso { get; set; } = string.Empty;
        public string Dimensoes { get; set; } = string.Empty;
        public string Cor { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;

        // CONTROLE
        public string CodigoBarras { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string NCMS { get; set; } = string.Empty;
        public string CEST { get; set; } = string.Empty;
        public string CFOP { get; set; } = string.Empty;

        // STATUS
        public bool Ativo { get; set; } = true;
        public bool ProdutoPerecivel { get; set; }
        public DateTime? DataValidade { get; set; }
        public DateTime? DataFabricacao { get; set; }
        public string Lote { get; set; } = string.Empty;

        // DATAS
        public DateTime DataCadastro { get; set; } = DateTime.Now;
        public DateTime? DataUltimaCompra { get; set; }
        public DateTime? DataUltimaVenda { get; set; }
        public DateTime? DataUltimaAtualizacao { get; set; }

        // OBSERVACOES
        public string Observacoes { get; set; } = string.Empty;
        public string ImagemUrl { get; set; } = string.Empty;
        public string Anexos { get; set; } = string.Empty;

        // ESTATISTICAS
        public int TotalVendas { get; set; }
        public decimal TotalFaturado { get; set; }
        public int VendasUltimoMes { get; set; }
        public int VendasUltimoTrimestre { get; set; }

        // OPERACIONAL
        public int QuantidadeReservada { get; set; }
        public int QuantidadeDisponivel => QuantidadeEstoque - QuantidadeReservada;

        public override string ToString()
        {
            return $"{Nome} ({Codigo})";
        }
    }
}
