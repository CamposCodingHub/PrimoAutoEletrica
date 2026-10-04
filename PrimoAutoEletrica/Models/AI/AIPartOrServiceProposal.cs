using System;

namespace PrimoAutoEletrica.Models.AI
{
    public class AIPartOrServiceProposal
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Tipo { get; set; } = "Peca"; // "Peca" ou "Servico"
        public string Descricao { get; set; } = string.Empty;
        public string? CodigoFabricante { get; set; }
        public decimal Quantidade { get; set; } = 1;
        public decimal PrecoSugerido { get; set; }
        public decimal? SaldoEstoque { get; set; }
        public bool PossuiEstoque => SaldoEstoque.HasValue && SaldoEstoque.Value >= Quantidade;
        public Guid? ProdutoId { get; set; }
        public int? TempoEstimadoMinutos { get; set; }
        public bool Selecionado { get; set; } = true;

        public decimal ValorTotal => Quantidade * PrecoSugerido;

        public string StatusEstoqueFormatado
        {
            get
            {
                if (Tipo == "Servico") return "⚡ Mão de Obra";
                if (!SaldoEstoque.HasValue) return "ℹ️ Sob Consulta";
                if (SaldoEstoque.Value <= 0) return "⚠️ Sem Estoque (Ruptura)";
                if (SaldoEstoque.Value < Quantidade) return $"⚠️ Estoque Baixo ({SaldoEstoque.Value:N0} un)";
                return $"✅ Em Estoque ({SaldoEstoque.Value:N0} un)";
            }
        }
    }
}
