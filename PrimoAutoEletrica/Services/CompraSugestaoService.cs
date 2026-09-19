using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PrimoAutoEletrica.Services
{
    public sealed class CompraSugestaoItem
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public decimal EstoqueAtual { get; set; }
        public decimal EstoqueMinimo { get; set; }
        public decimal QuantidadeSugerida { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }

    public sealed class CompraSugestaoService
    {
        public IReadOnlyList<CompraSugestaoItem> Sugerir()
        {
            var produtos = App.Repositories.Produtos.ObterTodos()
                .Where(p => p.Ativo)
                .ToList();

            var lista = new List<CompraSugestaoItem>();
            foreach (var p in produtos)
            {
                var minimo = p.QuantidadeMinima;
                var atual = p.QuantidadeDisponivel;
                if (minimo <= 0)
                {
                    continue;
                }

                if (atual <= minimo)
                {
                    var sugerido = Math.Max(minimo * 2 - atual, minimo);
                    lista.Add(new CompraSugestaoItem
                    {
                        Codigo = p.Codigo,
                        Nome = p.Nome,
                        EstoqueAtual = atual,
                        EstoqueMinimo = minimo,
                        QuantidadeSugerida = sugerido,
                        Motivo = atual <= 0 ? "Ruptura" : "Abaixo do minimo"
                    });
                }
            }

            return lista.OrderBy(x => x.EstoqueAtual).ThenByDescending(x => x.QuantidadeSugerida).ToList();
        }

        public string ExportarCsv(IEnumerable<CompraSugestaoItem> itens, string caminho)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Codigo;Nome;EstoqueAtual;EstoqueMinimo;QtdSugerida;Motivo");
            foreach (var i in itens)
            {
                sb.AppendLine($"{i.Codigo};{(i.Nome ?? string.Empty).Replace(';', ',')};{i.EstoqueAtual:F2};{i.EstoqueMinimo:F2};{i.QuantidadeSugerida:F2};{i.Motivo}");
            }

            File.WriteAllText(caminho, sb.ToString(), Encoding.UTF8);
            return caminho;
        }
    }
}
