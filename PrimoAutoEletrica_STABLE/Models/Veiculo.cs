using System;

namespace PrimoAutoEletrica.Models
{
    public class Veiculo
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? ClienteId { get; set; }

        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public string Ano { get; set; } = string.Empty;
        public string Cor { get; set; } = string.Empty;

        public string Placa { get; set; } = string.Empty;
        public string Chassi { get; set; } = string.Empty;
        public string Renavam { get; set; } = string.Empty;
        public string ImagemUrl { get; set; } = string.Empty;
        public string DocumentoImagemUrl { get; set; } = string.Empty;
        public string TipoVeiculo { get; set; } = string.Empty;
        public string SistemaEletrico { get; set; } = string.Empty;

        public string Motor { get; set; } = string.Empty;
        public string Combustivel { get; set; } = string.Empty;
        public string BateriaPrincipal { get; set; } = string.Empty;
        public string BateriaAuxiliar { get; set; } = string.Empty;
        public string BateriaInstalada { get; set; } = string.Empty;
        public string BateriaMarca { get; set; } = string.Empty;
        public string BateriaAmperagem { get; set; } = string.Empty;
        public DateTime? BateriaDataInstalacao { get; set; }
        public string Alternador { get; set; } = string.Empty;
        public string MotorPartida { get; set; } = string.Empty;

        public int Quilometragem { get; set; }

        public string TesteTensaoRepouso { get; set; } = string.Empty;
        public string TesteTensaoPartida { get; set; } = string.Empty;
        public string TesteCargaAlternador { get; set; } = string.Empty;
        public string CorrenteFuga { get; set; } = string.Empty;
        public string EstadoAterramentos { get; set; } = string.Empty;
        public string ChicotesReparados { get; set; } = string.Empty;
        public string FusiveisSubstituidos { get; set; } = string.Empty;
        public string RelesSubstituidos { get; set; } = string.Empty;
        public string LampadasSubstituidas { get; set; } = string.Empty;
        public string AcessoriosInstalados { get; set; } = string.Empty;
        public string ObservacoesTecnicasEletricas { get; set; } = string.Empty;
        public string FotosTecnicas { get; set; } = string.Empty;

        public string HistoricoTecnico { get; set; } = string.Empty;
        public string ObservacoesEletricasRecorrentes { get; set; } = string.Empty;
        public string ProblemaRecorrente { get; set; } = string.Empty;
        public string ObservacaoImportanteTecnico { get; set; } = string.Empty;
        public DateTime? RetornoRecomendadoEm { get; set; }
        public DateTime? GarantiaValidaAte { get; set; }
        public DateTime? ProximaRevisaoEm { get; set; }
        public string Observacoes { get; set; } = string.Empty;
    }
}
