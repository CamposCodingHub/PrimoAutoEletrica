using System;

namespace PrimoAutoEletrica.Models
{
    public sealed class CatalogoVeiculo
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public int AnoInicial { get; set; }
        public int AnoFinal { get; set; }
        public string Motor { get; set; } = string.Empty;
        public string Aliases { get; set; } = string.Empty;
        public string Segmento { get; set; } = "Leve";
        public bool Ativo { get; set; } = true;
        public DateTime DataCriacao { get; set; } = DateTime.Now;

        public string Exibicao => string.IsNullOrWhiteSpace(Motor)
            ? $"{Marca} {Modelo} ({AnoInicial}-{AnoFinal})"
            : $"{Marca} {Modelo} {Motor} ({AnoInicial}-{AnoFinal})";
    }
}
