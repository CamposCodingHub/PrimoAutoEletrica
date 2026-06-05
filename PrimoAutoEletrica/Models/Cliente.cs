using PrimoAutoEletrica.Helpers;
using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Models
{
    public class Cliente
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // DADOS PESSOAIS
        public string Nome { get; set; } = string.Empty;
        public string CPF { get; set; } = string.Empty;
        public string RG { get; set; } = string.Empty;
        public DateTime? DataNascimento { get; set; }

        // CONTATO
        public string Telefone { get; set; } = string.Empty;
        public string WhatsApp { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // ENDEREÇO
        public string CEP { get; set; } = string.Empty;
        public string Rua { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;

        // STATUS
        public bool Ativo { get; set; } = true;
        public bool ClienteVip { get; set; }

        // FINANCEIRO
        public decimal TotalGasto { get; set; }
        public int TotalServicos { get; set; }

        // FIDELIDADE
        public int PontosFidelidade { get; set; }

        // LGPD E COMUNICACAO
        public bool ConsentimentoLGPD { get; set; }
        public DateTime? DataConsentimentoLGPD { get; set; }
        public string OrigemConsentimentoLGPD { get; set; } = string.Empty;
        public bool AutorizaContatoWhatsApp { get; set; }

        // OBSERVAÇÕES
        public string Observacoes { get; set; } = string.Empty;

        // DOCUMENTOS
        public string CaminhoDocumento { get; set; } = string.Empty;
        public string CaminhoAssinatura { get; set; } = string.Empty;
        public string ImagemUrl { get; set; } = string.Empty;
        public string Documento => string.IsNullOrWhiteSpace(CPF)
            ? RG
            : CadastroValidationHelper.FormatarDocumento(CPF);

        // DATAS
        public DateTime DataCadastro { get; set; } = DateTime.Now;
        public DateTime? UltimaVisita { get; set; }

        // RELACIONAMENTOS
        public List<Veiculo> Veiculos { get; set; } = new();
        public List<HistoricoServico> HistoricoServicos { get; set; } = new();

        public override string ToString()
        {
            return $"{Nome} ({Documento})";
        }
    }
}
