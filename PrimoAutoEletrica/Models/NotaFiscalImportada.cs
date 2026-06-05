using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Models
{
    public class NotaFiscalImportada
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ChaveAcesso { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Serie { get; set; } = string.Empty;
        public DateTime DataEmissao { get; set; }
        public DateTime DataEntrada { get; set; } = DateTime.Now;
        public decimal ValorTotal { get; set; }
        public decimal ValorProdutos { get; set; }
        public string Modelo { get; set; } = "55"; // NF-e
        public FornecedorNota Fornecedor { get; set; } = new FornecedorNota();
        public List<ProdutoImportado> Produtos { get; set; } = new List<ProdutoImportado>();
        public StatusImportacaoNota Status { get; set; } = StatusImportacaoNota.Pendente;
        public string CaminhoArquivo { get; set; } = string.Empty;
        public string Erro { get; set; } = string.Empty;
        public DateTime DataImportacao { get; set; } = DateTime.Now;
        public Guid UsuarioId { get; set; }
        public string UsuarioNome { get; set; } = string.Empty;
    }

    public enum StatusImportacaoNota
    {
        Pendente,
        EmProcessamento,
        Concluida,
        Parcial,
        Erro,
        Duplicada
    }
}
