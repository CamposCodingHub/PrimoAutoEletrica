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
        public List<DiagnosticoTecnico> DiagnosticosEstruturados { get; set; } = new();
    }

    public enum MedicaoResultadoEnum
    {
        NORMAL,
        FORA_DO_ESPERADO,
        INCONCLUSIVO,
        NAO_REALIZADO,
        NAO_DISPONIVEL
    }

    public enum CausaStatusEnum
    {
        CONFIRMADA,
        PROVAVEL,
        NAO_DETERMINADA
    }

    public enum DiagnosticoStatusEnum
    {
        EmAndamento,
        Concluido,
        Cancelado
    }

    public enum GrandezaEletricaEnum
    {
        Tensao,
        QuedaTensao,
        Corrente,
        FugaCorrente,
        Resistencia,
        CCA,
        Frequencia,
        Rotacao,
        Temperatura,
        DutyCycle,
        Pressao,
        InspecaoVisual
    }

    public sealed class DiagnosticoMedicao
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DiagnosticoId { get; set; }
        public string NomeTeste { get; set; } = string.Empty;
        public GrandezaEletricaEnum TipoGrandeza { get; set; } = GrandezaEletricaEnum.Tensao;
        public string Instrumento { get; set; } = "Multimetro";
        public string Unidade { get; set; } = "V";
        public string Momento { get; set; } = string.Empty;
        public string Condicao { get; set; } = string.Empty;
        public string EvidenciaPath { get; set; } = string.Empty;
        public decimal? ValorReferenciaMin { get; set; }
        public decimal? ValorReferenciaMax { get; set; }
        public string TextoReferencia { get; set; } = string.Empty;
        public decimal ValorInicial { get; set; }
        public decimal? ValorPosReparo { get; set; }
        public MedicaoResultadoEnum Resultado { get; set; } = MedicaoResultadoEnum.NORMAL;
        public string Observacao { get; set; } = string.Empty;

        public bool TemValidacaoPosReparo => ValorPosReparo.HasValue;
        public decimal? DeltaPosReparo => ValorPosReparo.HasValue ? (ValorPosReparo.Value - ValorInicial) : null;
    }

    public enum ChecklistStatusEnum
    {
        NaoTestado,
        OK,
        Atencao,
        Critico,
        NaoDisponivel,
        NaoAplicavel
    }

    public sealed class ChecklistTecnicoItem
    {
        public string ItemId { get; set; } = string.Empty;
        public string Secao { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public ChecklistStatusEnum Status { get; set; } = ChecklistStatusEnum.NaoTestado;
        public bool Conforme { get => Status == ChecklistStatusEnum.OK; set { if (value) Status = ChecklistStatusEnum.OK; } }
        public bool NaoAplicavel { get => Status == ChecklistStatusEnum.NaoAplicavel; set { if (value) Status = ChecklistStatusEnum.NaoAplicavel; } }
        public decimal? ValorMedido { get; set; }
        public string Unidade { get; set; } = "V";
        public string Momento { get; set; } = "AntesReparo";
        public string Condicao { get; set; } = string.Empty;
        public decimal? ValorPosReparo { get; set; }
        public decimal? DeltaPosReparo => (ValorPosReparo.HasValue && ValorMedido.HasValue) ? (ValorPosReparo.Value - ValorMedido.Value) : null;
        public string EvidenciaPath { get; set; } = string.Empty;
        public string Observacao { get; set; } = string.Empty;
    }

    public sealed class ChecklistTecnicoOS
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrdemServicoId { get; set; }
        public Guid VeiculoId { get; set; }
        public Guid? ClienteId { get; set; }
        public DateTime DataRegistro { get; set; } = DateTime.Now;
        public DateTime DataCriacao { get => DataRegistro; set => DataRegistro = value; }
        public DateTime? DataConclusao { get; set; }
        public bool Concluido { get; set; }
        public string TecnicoResponsavel { get; set; } = string.Empty;
        public string ObservacoesGerais { get; set; } = string.Empty;
        public string ContextoTensao { get; set; } = "12V";
        public List<ChecklistTecnicoItem> Itens { get; set; } = new();

        public int TotalItens => Itens?.Count ?? 0;
        public int TotalConforme => Itens?.Count(i => i.Status == ChecklistStatusEnum.OK) ?? 0;
        public int TotalAtencao => Itens?.Count(i => i.Status == ChecklistStatusEnum.Atencao) ?? 0;
        public int TotalCritico => Itens?.Count(i => i.Status == ChecklistStatusEnum.Critico) ?? 0;
        public int TotalPendentes => Itens?.Count(i => i.Status == ChecklistStatusEnum.NaoTestado) ?? 0;
        public int PercentualConcluido => TotalItens > 0 ? (int)Math.Round((double)Itens.Count(i => i.Status != ChecklistStatusEnum.NaoTestado) / TotalItens * 100.0) : 0;
    }

    public sealed class DiagnosticoTecnico
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrdemServicoId { get; set; }
        public Guid VeiculoId { get; set; }
        public Guid? ClienteId { get; set; }
        public string TecnicoId { get; set; } = string.Empty;
        public DateTime DataHora { get; set; } = DateTime.Now;
        public DateTime? DataConclusao { get; set; }
        public DiagnosticoStatusEnum Status { get; set; } = DiagnosticoStatusEnum.EmAndamento;
        public string RoteiroCodigo { get; set; } = string.Empty;
        public string SintomaRelatado { get; set; } = string.Empty;
        public string SintomaCategoria { get; set; } = string.Empty;
        public string DiagnosticoLaudo { get; set; } = string.Empty;
        public CausaStatusEnum CausaStatus { get; set; } = CausaStatusEnum.PROVAVEL;
        public string CausaDescricao { get; set; } = string.Empty;
        public string CorrecaoExecutada { get; set; } = string.Empty;
        public Guid? PecaUtilizadaId { get; set; }
        public Guid? ServicoUtilizadoId { get; set; }
        public string Observacoes { get; set; } = string.Empty;
        public bool OrigemLegado { get; set; }
        public List<DiagnosticoMedicao> Medicoes { get; set; } = new();
    }
}

