using System;

namespace PrimoAutoEletrica.Models
{
    /// <summary>
    /// Representa uma filial da empresa
    /// </summary>
    public class Filial
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        /// <summary>
        /// Código da filial (ex: 001, 002, MATRIZ)
        /// </summary>
        public string Codigo { get; set; } = string.Empty;
        
        /// <summary>
        /// Nome da filial
        /// </summary>
        public string Nome { get; set; } = string.Empty;
        
        /// <summary>
        /// Endereço completo
        /// </summary>
        public string Endereco { get; set; } = string.Empty;
        
        /// <summary>
        /// Cidade
        /// </summary>
        public string Cidade { get; set; } = string.Empty;
        
        /// <summary>
        /// Estado (UF)
        /// </summary>
        public string Estado { get; set; } = string.Empty;
        
        /// <summary>
        /// CNPJ da filial
        /// </summary>
        public string Cnpj { get; set; } = string.Empty;
        
        /// <summary>
        /// Telefone da filial
        /// </summary>
        public string Telefone { get; set; } = string.Empty;
        
        /// <summary>
        /// Email da filial
        /// </summary>
        public string Email { get; set; } = string.Empty;
        
        /// <summary>
        /// Nome do gerente responsável
        /// </summary>
        public string Gerente { get; set; } = string.Empty;
        
        /// <summary>
        /// Indica se é a filial matriz
        /// </summary>
        public bool IsMatriz { get; set; }
        
        /// <summary>
        /// Indica se a filial está ativa
        /// </summary>
        public bool Ativa { get; set; } = true;
        
        /// <summary>
        /// Data de abertura da filial
        /// </summary>
        public DateTime DataAbertura { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Capacidade de estoque da filial
        /// </summary>
        public int CapacidadeEstoque { get; set; }
        
        /// <summary>
        /// Latitude para geolocalização
        /// </summary>
        public double? Latitude { get; set; }
        
        /// <summary>
        /// Longitude para geolocalização
        /// </summary>
        public double? Longitude { get; set; }
        
        /// <summary>
        /// Observações sobre a filial
        /// </summary>
        public string Observacoes { get; set; } = string.Empty;
        
        /// <summary>
        /// Data de cadastro
        /// </summary>
        public DateTime DataCadastro { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Data da última atualização
        /// </summary>
        public DateTime? DataUltimaAtualizacao { get; set; }
        
        public override string ToString()
        {
            return $"{Codigo} - {Nome} ({Cidade}/{Estado})";
        }
    }
}