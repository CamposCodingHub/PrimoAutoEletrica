using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public sealed class PedidoFornecedorLinha
    {
        public string Fornecedor { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public decimal EstoqueAtual { get; set; }
        public decimal EstoqueMinimo { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }

    public sealed class PedidoFornecedorService
    {
        public IReadOnlyList<PedidoFornecedorLinha> MontarPedido(IEnumerable<CompraSugestaoItem>? sugestoes = null)
        {
            var itens = (sugestoes ?? new CompraSugestaoService().Sugerir()).ToList();
            var produtos = App.Repositories.Produtos.ObterTodos().ToDictionary(p => p.Codigo ?? string.Empty, p => p, StringComparer.OrdinalIgnoreCase);
            var lista = new List<PedidoFornecedorLinha>();
            foreach (var s in itens)
            {
                produtos.TryGetValue(s.Codigo ?? string.Empty, out var prod);
                lista.Add(new PedidoFornecedorLinha
                {
                    Fornecedor = string.IsNullOrWhiteSpace(prod?.Fornecedor) ? "(sem fornecedor)" : prod!.Fornecedor,
                    Codigo = s.Codigo,
                    Nome = s.Nome,
                    Quantidade = s.QuantidadeSugerida,
                    EstoqueAtual = s.EstoqueAtual,
                    EstoqueMinimo = s.EstoqueMinimo,
                    Motivo = s.Motivo
                });
            }

            return lista
                .OrderBy(x => x.Fornecedor, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Codigo, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public string ExportarCsv(IEnumerable<PedidoFornecedorLinha> linhas, string caminho)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Fornecedor;Codigo;Nome;QtdPedido;EstoqueAtual;EstoqueMinimo;Motivo");
            foreach (var l in linhas)
            {
                sb.AppendLine($"{Escape(l.Fornecedor)};{l.Codigo};{Escape(l.Nome)};{l.Quantidade:F2};{l.EstoqueAtual:F2};{l.EstoqueMinimo:F2};{l.Motivo}");
            }

            File.WriteAllText(caminho, sb.ToString(), Encoding.UTF8);
            return caminho;
        }

        public string ExportarTextoImpressao(IEnumerable<PedidoFornecedorLinha> linhas, string caminho)
        {
            var sb = new StringBuilder();
            sb.AppendLine("PEDIDO A FORNECEDOR - Primox");
            sb.AppendLine($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}");
            sb.AppendLine(new string('-', 60));
            foreach (var g in linhas.GroupBy(x => x.Fornecedor))
            {
                sb.AppendLine();
                sb.AppendLine($"Fornecedor: {g.Key}");
                foreach (var l in g)
                {
                    sb.AppendLine($"  - {l.Codigo} | {l.Nome} | Qtd {l.Quantidade:F0} | Motivo: {l.Motivo}");
                }
            }

            File.WriteAllText(caminho, sb.ToString(), Encoding.UTF8);
            return caminho;
        }

        private static string Escape(string? v) => (v ?? string.Empty).Replace(';', ',');
    }
}
