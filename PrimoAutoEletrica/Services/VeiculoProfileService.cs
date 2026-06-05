using System;

namespace PrimoAutoEletrica.Services
{
    public static class VeiculoProfileService
    {
        public static string InferirTipoVeiculo(string? marca, string? tipoAtual = null)
        {
            if (!string.IsNullOrWhiteSpace(tipoAtual))
            {
                return tipoAtual.Trim();
            }

            var texto = (marca ?? string.Empty).Trim().ToUpperInvariant();
            if (texto.Contains("SCANIA") ||
                texto.Contains("VOLVO") ||
                texto.Contains("MERCEDES-BENZ") ||
                texto.Contains("IVECO") ||
                texto.Contains("DAF") ||
                texto.Contains("MAN") ||
                texto.Contains("CARGO") ||
                texto.Contains("TRUCK") ||
                texto.Contains("CONSTELLATION") ||
                texto.Contains("METEOR"))
            {
                return "Caminhao";
            }

            return "Carro";
        }

        public static string InferirSistemaEletrico(string? tipoVeiculo, string? sistemaAtual = null)
        {
            if (!string.IsNullOrWhiteSpace(sistemaAtual))
            {
                return sistemaAtual.Trim();
            }

            return string.Equals(tipoVeiculo, "Caminhao", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(tipoVeiculo, "Onibus", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(tipoVeiculo, "Maquina", StringComparison.OrdinalIgnoreCase)
                ? "24V"
                : "12V";
        }

        public static bool EhVeiculoPesado(string? tipoVeiculo, string? marca)
        {
            var tipo = string.IsNullOrWhiteSpace(tipoVeiculo)
                ? InferirTipoVeiculo(marca)
                : tipoVeiculo.Trim();

            return string.Equals(tipo, "Caminhao", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(tipo, "Onibus", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(tipo, "Maquina", StringComparison.OrdinalIgnoreCase);
        }

        public static string MontarDescricao(string? marca, string? modelo, string? ano)
        {
            return $"{marca} {modelo} {ano}".Replace("  ", " ").Trim();
        }
    }
}
