using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimoAutoEletrica.Models
{
    public sealed class ImportacaoRollbackResult
    {
        public Guid ImportacaoId { get; set; }
        public string NumeroNota { get; set; } = string.Empty;
        public string Serie { get; set; } = string.Empty;
        public string Fornecedor { get; set; } = string.Empty;
        public int TotalItens { get; set; }
        public int ProdutosRemovidos { get; set; }
        public int ProdutosBloqueados { get; set; }
        public int ProdutosIgnorados { get; set; }
        public int AtualizacoesRevertidas { get; set; }
        public int AtualizacoesIgnoradas { get; set; }
        public List<ImportacaoRollbackItemResult> Itens { get; } = new();

        public bool HouveAlteracao => ProdutosRemovidos > 0 || AtualizacoesRevertidas > 0;

        public string GerarResumo()
        {
            return
                $"Produtos removidos: {ProdutosRemovidos}\n" +
                $"Produtos bloqueados: {ProdutosBloqueados}\n" +
                $"Itens ignorados: {ProdutosIgnorados}\n" +
                $"Atualizacoes revertidas: {AtualizacoesRevertidas}\n" +
                $"Atualizacoes nao revertidas: {AtualizacoesIgnoradas}";
        }

        public string GerarDetalhes(int limite = 12)
        {
            if (Itens.Count == 0)
            {
                return "Nenhum item processado.";
            }

            var linhas = Itens
                .Take(Math.Max(1, limite))
                .Select(item => $"- {item.ProdutoNome}: {item.Resultado} ({item.Motivo})")
                .ToList();

            if (Itens.Count > linhas.Count)
            {
                linhas.Add($"- Mais {Itens.Count - linhas.Count} item(ns) registrado(s) na auditoria.");
            }

            return string.Join(Environment.NewLine, linhas);
        }
    }

    public sealed class ImportacaoRollbackItemResult
    {
        public Guid? ProdutoId { get; set; }
        public string ProdutoNome { get; set; } = string.Empty;
        public string Resultado { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
    }
}
