using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public sealed class ComissaoLinha
    {
        public string Tecnico { get; set; } = string.Empty;
        public int QuantidadeOs { get; set; }
        public decimal TotalServicos { get; set; }
        public decimal TotalPecas { get; set; }
        public decimal BaseCalculo { get; set; }
        public decimal Percentual { get; set; }
        public decimal Comissao { get; set; }
    }

    public sealed class ComissaoSettlementService
    {
        public IReadOnlyList<ComissaoLinha> Calcular(DateTime inicio, DateTime fim, decimal percentualPadrao = 0.10m)
        {
            var funcionarios = App.Repositories.Funcionarios.ObterTodos()
                .ToDictionary(f => f.Id, f => f.Nome);

            var ordens = App.Repositories.OrdensServico.ObterTodos()
                .Where(o => o.Ativo && IsFinalizada(o) && InPeriodo(o, inicio, fim))
                .ToList();

            return ordens
                .GroupBy(o =>
                {
                    if (o.TecnicoId.HasValue && funcionarios.TryGetValue(o.TecnicoId.Value, out var nome))
                    {
                        return nome;
                    }

                    return "(sem tecnico)";
                })
                .Select(g =>
                {
                    var servicos = g.Sum(x => x.ValorMaoObra > 0
                        ? x.ValorMaoObra
                        : x.Itens.Where(i => string.Equals(i.Tipo, "Servico", StringComparison.OrdinalIgnoreCase)).Sum(i => i.Total));
                    var pecas = g.Sum(x => x.Itens.Where(i => string.Equals(i.Tipo, "Produto", StringComparison.OrdinalIgnoreCase)
                                                           || string.Equals(i.Tipo, "Peca", StringComparison.OrdinalIgnoreCase))
                        .Sum(i => i.Total));
                    var baseCalc = servicos > 0 ? servicos : (servicos + pecas) * 0.5m;
                    return new ComissaoLinha
                    {
                        Tecnico = g.Key,
                        QuantidadeOs = g.Count(),
                        TotalServicos = servicos,
                        TotalPecas = pecas,
                        BaseCalculo = baseCalc,
                        Percentual = percentualPadrao,
                        Comissao = Math.Round(baseCalc * percentualPadrao, 2)
                    };
                })
                .OrderByDescending(x => x.Comissao)
                .ToList();
        }

        public string ExportarCsv(IEnumerable<ComissaoLinha> linhas, string caminho)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Tecnico;QtdOS;TotalServicos;TotalPecas;BaseCalculo;Percentual;Comissao");
            foreach (var l in linhas)
            {
                sb.AppendLine($"{Escape(l.Tecnico)};{l.QuantidadeOs};{l.TotalServicos:F2};{l.TotalPecas:F2};{l.BaseCalculo:F2};{l.Percentual:P0};{l.Comissao:F2}");
            }

            File.WriteAllText(caminho, sb.ToString(), Encoding.UTF8);
            return caminho;
        }

        private static bool IsFinalizada(OrdemServico o)
        {
            var s = o.Status ?? string.Empty;
            return s.Contains("conclu", StringComparison.OrdinalIgnoreCase)
                   || s.Contains("final", StringComparison.OrdinalIgnoreCase)
                   || s.Contains("entreg", StringComparison.OrdinalIgnoreCase);
        }

        private static bool InPeriodo(OrdemServico o, DateTime inicio, DateTime fim)
        {
            var data = o.DataConclusao ?? o.DataEntrega ?? o.DataAbertura;
            return data.Date >= inicio.Date && data.Date <= fim.Date;
        }

        private static string Escape(string value) =>
            string.IsNullOrEmpty(value) ? string.Empty : value.Replace(';', ',');
    }
}
