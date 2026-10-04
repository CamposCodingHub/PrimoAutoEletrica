namespace PrimoAutoEletrica.Models.BibliotecaTecnica
{
    public sealed class VeiculoPesado24VEspecificacao
    {
        public int Id { get; set; }
        public string Montadora { get; set; } = string.Empty; // Scania, Mercedes-Benz, Volvo
        public string Modelo { get; set; } = string.Empty;    // R440, Atego 2426, FH 440
        public string TensaoSistema { get; set; } = "24V Nominal (28.4V Carregando)";
        public string AlternadorEspecificacao { get; set; } = string.Empty;
        public string BateriasEspecificacao { get; set; } = string.Empty;
        public string ConsumoStandbyMaximo { get; set; } = "<= 50mA";
        public string TorqueCabecote { get; set; } = string.Empty;
        public string FolgaValvulas { get; set; } = string.Empty;
        public string ArCondicionadoGasGramas { get; set; } = string.Empty;
        public string ArCondicionadoOleoTipo { get; set; } = string.Empty;
        public string DicasEletricasChassi { get; set; } = string.Empty;
    }
}
