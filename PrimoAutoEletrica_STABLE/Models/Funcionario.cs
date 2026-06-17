using System;

namespace PrimoAutoEletrica.Models
{
    public class Funcionario
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string CPF { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public string Funcao { get; set; } = string.Empty;
        public string Cargo { get => Funcao; set => Funcao = value; }
        public string PerfilAcesso { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Foto { get; set; } = string.Empty;
        public DateTime DataAdmissao { get; set; }
        public decimal Salario { get; set; }
        public string Status { get; set; } = "Ativo";
        public string Observacoes { get; set; } = string.Empty;
        public DateTime DataCadastro { get; set; }
        public DateTime? DataUltimoLogin { get; set; }
        public bool ExigirTrocaSenha { get; set; }
        public bool Ativo { get; set; } = true;
    }
}
