using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Models
{
    public sealed class ProntuarioEletricoCampo
    {
        public string Nome { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
        public string Grupo { get; set; } = string.Empty;
        public bool Critico { get; set; }
    }

    public sealed class ProntuarioEletricoVeiculo
    {
        public Guid VeiculoId { get; set; }
        public string Veiculo { get; set; } = string.Empty;
        public string Placa { get; set; } = string.Empty;
        public string SistemaEletrico { get; set; } = string.Empty;
        public List<ProntuarioEletricoCampo> Campos { get; set; } = new();
        public List<string> FotosTecnicas { get; set; } = new();
    }

    public sealed class DiagnosticoGuiadoRoteiro
    {
        public string Codigo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Sintoma { get; set; } = string.Empty;
        public List<string> PossiveisCausas { get; set; } = new();
        public List<string> Ferramentas { get; set; } = new();
        public List<string> SequenciaTestes { get; set; } = new();
        public List<string> ValoresEsperados { get; set; } = new();
        public string Resultado { get; set; } = string.Empty;
        public string Conclusao { get; set; } = string.Empty;
        public string FotoAnexa { get; set; } = string.Empty;
        public bool PermiteGerarOrcamento { get; set; } = true;
        public List<string> ServicosSugeridos { get; set; } = new();
        public List<string> PecasSugeridas { get; set; } = new();
    }

    public sealed class BibliotecaTecnicaItem
    {
        public string Titulo { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Resumo { get; set; } = string.Empty;
        public List<string> Ferramentas { get; set; } = new();
        public List<string> Passos { get; set; } = new();
        public List<string> ValoresReferencia { get; set; } = new();
        public List<string> Cuidados { get; set; } = new();
    }

    public sealed class DefeitoRecorrenteResumo
    {
        public string Tipo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public int TempoMedioMinutos { get; set; }
        public decimal ValorTotal { get; set; }
        public DateTime? UltimaOcorrencia { get; set; }
        public bool EmGarantia { get; set; }
    }

    public sealed class SugestaoPecasServico
    {
        public string Servico { get; set; } = string.Empty;
        public List<string> Pecas { get; set; } = new();
        public string Observacao { get; set; } = string.Empty;
    }

    public sealed class ServicoTecnicoAutoEletrica
    {
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public decimal ValorPadrao { get; set; }
        public int TempoMedioMinutos { get; set; }
        public int GarantiaPadraoDias { get; set; }
        public List<string> PecasSugeridas { get; set; } = new();
    }

    public sealed class OrcamentoDiagnosticoDraft
    {
        public Guid ClienteId { get; set; }
        public Guid? VeiculoId { get; set; }
        public string Roteiro { get; set; } = string.Empty;
        public string Resultado { get; set; } = string.Empty;
        public string Conclusao { get; set; } = string.Empty;
        public decimal ValorMaoObra { get; set; }
        public List<string> Pecas { get; set; } = new();
    }

    public sealed class AutoEletricaTecnicaSnapshot
    {
        public List<DiagnosticoGuiadoRoteiro> RoteirosDiagnostico { get; set; } = new();
        public List<BibliotecaTecnicaItem> BibliotecaTecnica { get; set; } = new();
        public List<DefeitoRecorrenteResumo> DefeitosRecorrentes { get; set; } = new();
        public List<SugestaoPecasServico> SugestoesPecas { get; set; } = new();
        public List<ServicoTecnicoAutoEletrica> ServicosTecnicos { get; set; } = new();
        public ProntuarioEletricoVeiculo? Prontuario { get; set; }
    }
}
