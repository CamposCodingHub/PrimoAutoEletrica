using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;

namespace PrimoAutoEletrica.ViewModels
{
    public class ClienteListItemViewModel
    {
        public Cliente Cliente { get; init; } = new();
        public string Nome { get; init; } = string.Empty;
        public string TipoPessoaResumo { get; init; } = string.Empty;
        public string DocumentoResumo { get; init; } = string.Empty;
        public string ContatoPrincipal { get; init; } = string.Empty;
        public string VeiculoPrincipal { get; init; } = string.Empty;
        public string UltimaVisita { get; init; } = string.Empty;
        public string TicketMedio { get; init; } = string.Empty;
        public string StatusResumo { get; init; } = string.Empty;
        public string LgpdResumo { get; init; } = string.Empty;
        public string ImagemUrl { get; init; } = string.Empty;
        public string Iniciais { get; init; } = string.Empty;
    }
}
