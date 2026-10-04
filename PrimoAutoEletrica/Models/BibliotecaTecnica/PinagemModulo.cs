using System.Collections.Generic;

namespace PrimoAutoEletrica.Models.BibliotecaTecnica
{
    public sealed class PinagemModulo
    {
        public int Id { get; set; }
        public string CodigoModulo { get; set; } = string.Empty;
        public string NomeModulo { get; set; } = string.Empty;
        public string Montadora { get; set; } = string.Empty;
        public string ModelosAplicacao { get; set; } = string.Empty;
        public string SistemaTipo { get; set; } = string.Empty;
        public string TensaoOperacao { get; set; } = "12V"; // "12V" ou "24V"
        public string DescricaoConectores { get; set; } = string.Empty;
        public string ObservacoesTecnicas { get; set; } = string.Empty;
        public List<PinoConectorInfo> Pinos { get; set; } = new();
    }

    public sealed class PinoConectorInfo
    {
        public int Id { get; set; }
        public int ModuloId { get; set; }
        public string Conector { get; set; } = string.Empty;
        public string NumeroPino { get; set; } = string.Empty;
        public string FuncaoSinal { get; set; } = string.Empty;
        public string TipoSinal { get; set; } = string.Empty; // "Alimentação Positiva", "Massa / Aterramento", "Sinal Sensor", "Saída Atuador / PWM", "Comunicação CAN / K"
        public string CorFio { get; set; } = string.Empty;
        public string TensaoEsperada { get; set; } = string.Empty;
        public string ObservacoesTecnicas { get; set; } = string.Empty;
    }
}
