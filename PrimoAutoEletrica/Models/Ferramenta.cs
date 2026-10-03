using System;

namespace PrimoAutoEletrica.Models
{
    public enum StatusFerramenta
    {
        Disponivel = 1,
        EmUso = 2,
        EmManutencao = 3,
        Avariada = 4,
        Extraviada = 5
    }

    public enum CategoriaFerramenta
    {
        DiagnosticoEletronico = 1, // Scanners, programadores de chave, osciloscópios
        MedicaoEletrica = 2,       // Multímetros, alicates amperímetros, canetas de polaridade
        BateriasECarga = 3,        // Testadores de condutância, carregadores e analisadores
        EletricaEMontagem = 4,     // Alicates de crimpagem especializada, soldadores
        MecanicaGeral = 5          // Torquímetros, extratores, soquetes especiais
    }

    public sealed class Ferramenta
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string CodigoPatrimonio { get; set; } = string.Empty; // Ex: FER-0001
        public string Nome { get; set; } = string.Empty;
        public CategoriaFerramenta Categoria { get; set; } = CategoriaFerramenta.DiagnosticoEletronico;
        public string MarcaModelo { get; set; } = string.Empty;
        public string NumeroSerie { get; set; } = string.Empty;
        public string LocalizacaoArmario { get; set; } = string.Empty; // Ex: Armário 01 / Gaveta C
        public StatusFerramenta Status { get; set; } = StatusFerramenta.Disponivel;
        public decimal ValorAquisicao { get; set; }
        public DateTime DataAquisicao { get; set; } = DateTime.Now;

        // Calibração e Manutenção Preventiva
        public bool RequerCalibracaoPeriodica { get; set; }
        public int IntervaloCalibracaoDias { get; set; } = 365;
        public DateTime? UltimaCalibracao { get; set; }
        public DateTime? ProximaCalibracao { get; set; }

        // Vínculo Operacional de Posse Ativa
        public Guid? FuncionarioPosseAtualId { get; set; }
        public string? FuncionarioPosseAtualNome { get; set; }
        public Guid? OrdemServicoAtualId { get; set; }
        public string? NumeroOSAtual { get; set; }
        public DateTime? DataHoraRetiradaAtual { get; set; }

        public string Observacoes { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;

        public bool CalibracaoVencida => RequerCalibracaoPeriodica && ProximaCalibracao.HasValue && ProximaCalibracao.Value.Date < DateTime.Today;

        public string StatusDescricao => Status switch
        {
            StatusFerramenta.Disponivel => "Disponível",
            StatusFerramenta.EmUso => "Em Uso",
            StatusFerramenta.EmManutencao => "Em Manutenção",
            StatusFerramenta.Avariada => "Avariada / Manutenção",
            StatusFerramenta.Extraviada => "Extraviada",
            _ => "Indefinido"
        };

        public string CategoriaDescricao => Categoria switch
        {
            CategoriaFerramenta.DiagnosticoEletronico => "Diagnóstico Eletrônico",
            CategoriaFerramenta.MedicaoEletrica => "Medição Elétrica",
            CategoriaFerramenta.BateriasECarga => "Baterias & Carga",
            CategoriaFerramenta.EletricaEMontagem => "Elétrica & Montagem",
            CategoriaFerramenta.MecanicaGeral => "Mecânica Geral",
            _ => "Geral"
        };
    }

    public sealed class MovimentacaoFerramenta
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid FerramentaId { get; set; }
        public string FerramentaNome { get; set; } = string.Empty;
        public string CodigoPatrimonio { get; set; } = string.Empty;
        public Guid FuncionarioId { get; set; }
        public string FuncionarioNome { get; set; } = string.Empty;
        public Guid? OrdemServicoId { get; set; }
        public string? NumeroOS { get; set; }
        public DateTime DataRetirada { get; set; } = DateTime.Now;
        public DateTime? PrevisaoDevolucao { get; set; }
        public DateTime? DataDevolucao { get; set; }
        public string EstadoConservacaoRetirada { get; set; } = "OK";
        public string? EstadoConservacaoDevolucao { get; set; }
        public string? ObservacaoDevolucao { get; set; }
        public string RegistradoPor { get; set; } = "Sistema";

        public bool Devolvida => DataDevolucao.HasValue;
    }
}
