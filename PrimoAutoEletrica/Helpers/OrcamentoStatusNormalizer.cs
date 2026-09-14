using System;

namespace PrimoAutoEletrica.Helpers
{
    /// <summary>
    /// Canonicaliza status de orçamento (G011) — evita drift Recusado vs Rejeitado.
    /// </summary>
    public static class OrcamentoStatusNormalizer
    {
        public static string Normalizar(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return "Rascunho";
            }

            var s = status.Trim();
            if (s.Equals("Rejeitado", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("Recusada", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("Rejeitada", StringComparison.OrdinalIgnoreCase))
            {
                return "Recusado";
            }

            if (s.Equals("Aprovada", StringComparison.OrdinalIgnoreCase))
            {
                return "Aprovado";
            }

            if (s.Equals("Enviada", StringComparison.OrdinalIgnoreCase))
            {
                return "Enviado";
            }

            if (s.Equals("Convertido em Venda", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("ConvertidoEmVenda", StringComparison.OrdinalIgnoreCase))
            {
                return "Convertido em Venda";
            }

            if (s.Equals("Convertido em OS", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("ConvertidoEmOS", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("Convertido em Ordem de Servico", StringComparison.OrdinalIgnoreCase))
            {
                return "Convertido em OS";
            }

            return s;
        }

        public static bool EhRecusado(string? status)
        {
            var n = Normalizar(status);
            return n.Equals("Recusado", StringComparison.OrdinalIgnoreCase);
        }
    }
}
