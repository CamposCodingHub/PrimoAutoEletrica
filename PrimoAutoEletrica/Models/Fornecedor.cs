using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Models
{
    public class Fornecedor
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        // Dados básicos
        public string RazaoSocial { get; set; } = string.Empty;
        public string NomeFantasia { get; set; } = string.Empty;
        public string CNPJ { get; set; } = string.Empty;
        public string InscricaoEstadual { get; set; } = string.Empty;
        
        // Contato
        public string Telefone { get; set; } = string.Empty;
        public string Celular { get; set; } = string.Empty;
        public string WhatsAppVendedor { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Site { get; set; } = string.Empty;
        
        // Endereço
        public string CEP { get; set; } = string.Empty;
        public string Rua { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Complemento { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        
        // Condições comerciais
        public string FormaPagamento { get; set; } = string.Empty;
        public string PrazoPagamento { get; set; } = string.Empty;
        public int PrazoMedioEntregaDias { get; set; } = 0;
        public decimal PedidoMinimo { get; set; } = 0;
        
        // Categoria e status
        public string Categoria { get; set; } = "Peças"; // Peças, Serviços, Materiais, etc.
        public string CategoriaPreferencial { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;
        
        // Avaliação
        public int Nota { get; set; } = 5; // 1 a 5
        public string Observacoes { get; set; } = string.Empty;
        
        // Controle
        public DateTime DataCadastro { get; set; } = DateTime.Now;
        public DateTime? UltimaCompra { get; set; }
        public decimal TotalCompras { get; set; } = 0;
        
        // Relacionamentos
        public List<ContatoFornecedor> Contatos { get; set; } = new();
    }

    public class ContatoFornecedor
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid FornecedorId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool Principal { get; set; } = false;
    }
}
