using System.Collections.Generic;

namespace PrimoAutoEletrica.Models.BibliotecaTecnica
{
    public sealed class CentralEletricaFusivel
    {
        public int Id { get; set; }
        public string CodigoCentral { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Montadora { get; set; } = string.Empty;
        public string ModelosAplicacao { get; set; } = string.Empty;
        public string Localizacao { get; set; } = "Vão do Motor"; // "Vão do Motor", "Abaixo do Painel", "Chassi / Baterias"
        public string TensaoNominal { get; set; } = "12V"; // "12V" ou "24V"
        public List<FusivelItemInfo> Fusiveis { get; set; } = new();
        public List<ReleItemInfo> Reles { get; set; } = new();
    }

    public sealed class FusivelItemInfo
    {
        public string Numero { get; set; } = string.Empty; // "F01", "F12"
        public int CapacidadeAmperes { get; set; } // 10, 15, 20, 30, 40
        public string CorPadrao { get; set; } = string.Empty; // "Vermelho (10A)", "Azul (15A)", etc.
        public string CircuitoProtegido { get; set; } = string.Empty;
        public string ReleAssociado { get; set; } = string.Empty;
        public string StatusVerificacao { get; set; } = "OK";
    }

    public sealed class ReleItemInfo
    {
        public string Posicao { get; set; } = string.Empty; // "R01", "R02"
        public string NomeFuncao { get; set; } = string.Empty;
        public string TipoPinos { get; set; } = "4 Pinos (Mini 40A)";
        public string PinagemReferencia { get; set; } = string.Empty;
    }
}
