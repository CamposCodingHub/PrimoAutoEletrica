using System;

namespace PrimoAutoEletrica.Models
{
    public class PerfilAcesso
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string NivelHierarquico { get; set; } = string.Empty; // Executivo, Gerencial, Operacional, Básico
        public bool Ativo { get; set; } = true;
        public DateTime DataCriacao { get; set; }
        public DateTime? DataUltimaModificacao { get; set; }
        public string CriadoPor { get; set; } = string.Empty;
        public string ModificadoPor { get; set; } = string.Empty;
        public bool PodeDeletar { get; set; } = true; // Perfis de sistema não podem ser deletados
        public int OrdemExibicao { get; set; } = 0;
    }

    public enum NivelPerfil
    {
        Executivo = 1,    // Administrador, Diretor
        Gerencial = 2,    // Gerente, Supervisor
        Operacional = 3,  // Mecânico, Vendedor, Caixa
        Basico = 4        // Estagiário, Temporário
    }
}

