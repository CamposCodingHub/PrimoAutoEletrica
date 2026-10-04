using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Models.Dvi
{
    public enum DviStatusSeveridade
    {
        Ok = 1,          // Verde - Em perfeito estado
        Atencao = 2,     // Amarelo - Revisão preventiva recomendada
        Critico = 3      // Vermelho - Defeito crítico / risco iminente
    }

    public enum DviStatusAprovacao
    {
        Pendente = 0,
        EnviadoWhatsApp = 1,
        AprovadoCliente = 2,
        RecusadoCliente = 3
    }

    public class InspecaoDvi
    {
        public int Id { get; set; }
        public string? OrdemServicoId { get; set; }
        public int? VeiculoId { get; set; }
        public int? ClienteId { get; set; }
        public string PlacaVeiculo { get; set; } = string.Empty;
        public string ModeloVeiculo { get; set; } = string.Empty;
        public string ClienteNome { get; set; } = string.Empty;
        public string ClienteTelefone { get; set; } = string.Empty;
        public DateTime DataInspecao { get; set; } = DateTime.Now;
        public string ResponsavelTecnico { get; set; } = "Eletricista Técnico";
        public DviStatusAprovacao StatusAprovacao { get; set; } = DviStatusAprovacao.Pendente;
        public string? ObservacoesGerais { get; set; }
        public string? TokenAprovacaoRemota { get; set; }
        public DateTime? DataAprovacao { get; set; }
        public decimal ValorTotalEstimado { get; set; }
        public List<InspecaoDviItem> Itens { get; set; } = new();

        public int TotalItensCriticos => Itens?.FindAll(i => i.Severidade == DviStatusSeveridade.Critico).Count ?? 0;
        public int TotalItensAtencao => Itens?.FindAll(i => i.Severidade == DviStatusSeveridade.Atencao).Count ?? 0;
        public int TotalItensOk => Itens?.FindAll(i => i.Severidade == DviStatusSeveridade.Ok).Count ?? 0;
    }

    public class InspecaoDviItem
    {
        public int Id { get; set; }
        public int InspecaoDviId { get; set; }
        public string Categoria { get; set; } = "Elétrica"; // Elétrica & Bateria, Iluminação, Chicotes & Relés, Sensores, Segurança, Avarias
        public string NomeItem { get; set; } = string.Empty;
        public DviStatusSeveridade Severidade { get; set; } = DviStatusSeveridade.Ok;
        public string? ObservacaoTecnica { get; set; }
        public decimal? ValorEstimadoReparo { get; set; }
        public bool AprovadoPeloCliente { get; set; }
        public List<InspecaoDviFoto> Fotos { get; set; } = new();
    }

    public class InspecaoDviFoto
    {
        public int Id { get; set; }
        public int InspecaoDviItemId { get; set; }
        public string CaminhoArquivo { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public string? AnotacoesJson { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.Now;
    }
}
