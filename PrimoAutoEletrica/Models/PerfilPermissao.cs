using System;

namespace PrimoAutoEletrica.Models
{
    public class PerfilPermissao
    {
        public int Id { get; set; }
        public int PerfilId { get; set; }
        public int PermissaoId { get; set; }
        public bool Concedida { get; set; } = true;
        public DateTime DataConcessao { get; set; }
        public string ConcedidaPor { get; set; } = string.Empty;
        public DateTime? DataRevogacao { get; set; }
        public string RevogadaPor { get; set; } = string.Empty;
        public bool Ativa { get; set; } = true;
        public string Justificativa { get; set; } = string.Empty;

        // Propriedades de navegação
        public PerfilAcesso? Perfil { get; set; }
        public Permissao? Permissao { get; set; }
    }
}
