using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Models
{
    public enum StatusContratoFrota
    {
        Ativo = 1,
        Suspenso = 2,
        Encerrado = 3
    }

    public enum StatusFaturaFrota
    {
        Aberta = 1,
        Enviada = 2,
        Paga = 3,
        Cancelada = 4
    }

    public enum CriticidadeRevisao
    {
        EmDia = 1,
        Proxima = 2,
        Vencida = 3
    }

    public sealed class ContratoFrota
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int ClienteId { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public string NumeroContrato { get; set; } = string.Empty; // Ex: CTR-2026-FROT-01
        public string Descricao { get; set; } = string.Empty;
        
        public DateTime DataInicio { get; set; } = DateTime.Now;
        public DateTime DataVencimento { get; set; } = DateTime.Now.AddYears(1);
        
        // Parâmetros de negociação contratual
        public decimal DescontoPecasPercentual { get; set; }     // Ex: 15.0%
        public decimal DescontoServicosPercentual { get; set; }  // Ex: 20.0%
        public decimal ValorHoraTecnicaNegociada { get; set; }   // Ex: R$ 130,00
        
        // Regras de faturamento periódico agrupado
        public int DiaFechamentoFatura { get; set; } = 30;       // Ex: todo dia 30
        public int DiasVencimentoBoleto { get; set; } = 15;      // Ex: 15 dias após emissão
        public decimal LimiteCreditoMensal { get; set; } = 50000;
        public bool ExigeAutorizacaoPrevia { get; set; } = true;
        
        public StatusContratoFrota Status { get; set; } = StatusContratoFrota.Ativo;
        public string Observacoes { get; set; } = string.Empty;
        public DateTime DataCadastro { get; set; } = DateTime.Now;

        public bool IsVigente => Status == StatusContratoFrota.Ativo && DateTime.Today <= DataVencimento.Date;
    }

    public sealed class VeiculoFrota
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int VeiculoId { get; set; }
        public Guid ContratoFrotaId { get; set; }
        public string PrefixoFrota { get; set; } = string.Empty; // Ex: CARRETA-04, VUC-12, CAMINHAO-88
        public string Placa { get; set; } = string.Empty;
        public string MarcaModelo { get; set; } = string.Empty;
        public string MotoristaResponsavel { get; set; } = string.Empty;
        public string CentroCusto { get; set; } = string.Empty;
        
        // Telemetria e odômetro/horímetro para revisão programada
        public int KmAtual { get; set; }
        public int HorimetroAtual { get; set; }
        public int UltimaRevisaoKm { get; set; }
        public int IntervaloRevisaoKm { get; set; } = 10000; // Revisão a cada 10.000 km
        public int ProximaRevisaoKm => UltimaRevisaoKm + IntervaloRevisaoKm;
        
        public bool Ativo { get; set; } = true;

        public int KmAteProximaRevisao => ProximaRevisaoKm - KmAtual;

        public CriticidadeRevisao CriticidadeManutencao
        {
            get
            {
                if (KmAtual >= ProximaRevisaoKm) return CriticidadeRevisao.Vencida;
                if (KmAteProximaRevisao <= 1000) return CriticidadeRevisao.Proxima;
                return CriticidadeRevisao.EmDia;
            }
        }
    }

    public sealed class FaturaFrota
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ContratoFrotaId { get; set; }
        public int ClienteId { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public string NumeroFatura { get; set; } = string.Empty; // Ex: FAT-FROT-2026-0001
        
        public DateTime PeriodoInicio { get; set; }
        public DateTime PeriodoFim { get; set; }
        public DateTime DataEmissao { get; set; } = DateTime.Now;
        public DateTime DataVencimento { get; set; } = DateTime.Now.AddDays(15);
        
        public decimal ValorBruto { get; set; }
        public decimal ValorDescontosContratuais { get; set; }
        public decimal ValorLiquido => ValorBruto - ValorDescontosContratuais;
        
        public StatusFaturaFrota Status { get; set; } = StatusFaturaFrota.Aberta;
        public int TotalOrdensServico => ItensOS.Count;
        
        public string? LinhaDigitavelBoleto { get; set; }
        public string? PixCopiaECola { get; set; }
        public string Observacoes { get; set; } = string.Empty;
        
        public List<FaturaFrotaItemOS> ItensOS { get; set; } = new();

        public string StatusDescricao => Status switch
        {
            StatusFaturaFrota.Aberta => "Aberta / Consolidada",
            StatusFaturaFrota.Enviada => "Enviada ao Frotista",
            StatusFaturaFrota.Paga => "Paga / Liquidada",
            StatusFaturaFrota.Cancelada => "Cancelada",
            _ => "Indefinido"
        };
    }

    public sealed class FaturaFrotaItemOS
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid FaturaFrotaId { get; set; }
        public int OrdemServicoId { get; set; }
        public string NumeroOS { get; set; } = string.Empty;
        public string PlacaVeiculo { get; set; } = string.Empty;
        public string PrefixoVeiculo { get; set; } = string.Empty;
        public DateTime DataOS { get; set; }
        public decimal ValorTotalOriginal { get; set; }
        public decimal ValorComDescontoContrato { get; set; }
    }

    public sealed class AlertaManutencaoFrota
    {
        public Guid VeiculoFrotaId { get; set; }
        public string Placa { get; set; } = string.Empty;
        public string Prefixo { get; set; } = string.Empty;
        public string ClienteNome { get; set; } = string.Empty;
        public string Componente { get; set; } = "Alternador, Bateria e Sistema de Carga";
        public int KmAtual { get; set; }
        public int KmProgramado { get; set; }
        public int DiferencaKm => KmAtual - KmProgramado;
        public CriticidadeRevisao Criticidade { get; set; }
        
        public string MensagemAlerta => Criticidade switch
        {
            CriticidadeRevisao.Vencida => $"ALERTA CRÍTICO: Veículo {Prefixo} ({Placa}) ultrapassou a revisão em {Math.Abs(DiferencaKm)} km!",
            CriticidadeRevisao.Proxima => $"ATENÇÃO: Veículo {Prefixo} ({Placa}) faltam apenas {Math.Abs(DiferencaKm)} km para a revisão elétrica preventiva programada.",
            _ => $"Veículo {Prefixo} ({Placa}) com manutenção em dia."
        };
    }
}
