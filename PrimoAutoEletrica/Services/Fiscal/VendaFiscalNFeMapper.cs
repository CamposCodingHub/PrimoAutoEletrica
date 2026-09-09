using System;
using System.Collections.Generic;
using System.Linq;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services.Fiscal
{
    /// <summary>
    /// Mapeia Venda (PDV) → FiscalNFeDocument sem alterar venda/estoque/financeiro.
    /// Não inventa NCM/CFOP/CST — usa produto + perfil fiscal configurado.
    /// </summary>
    public sealed class VendaFiscalNFeMapper
    {
        public FiscalNFeDocument Map(
            Venda venda,
            Cliente cliente,
            IReadOnlyDictionary<Guid, Produto> produtosPorId,
            FiscalIssuerProfile emitente,
            Guid fiscalOperationId,
            string idempotencyKey,
            FiscalEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(venda);
            ArgumentNullException.ThrowIfNull(cliente);
            ArgumentNullException.ThrowIfNull(produtosPorId);
            ArgumentNullException.ThrowIfNull(emitente);

            var itens = new List<FiscalNFeItem>();
            var numero = 1;
            decimal valorProdutos = 0m;

            foreach (var linha in venda.Itens ?? new List<ItemVenda>())
            {
                if (!string.Equals(linha.Tipo, "Produto", StringComparison.OrdinalIgnoreCase) &&
                    linha.ProdutoId == null &&
                    linha.Produto == null)
                {
                    // Serviços não entram na NF-e onda 1 — bloqueio fica no validator se lista vazia.
                    continue;
                }

                Produto? produto = linha.Produto;
                if (produto == null && linha.ProdutoId.HasValue)
                {
                    produtosPorId.TryGetValue(linha.ProdutoId.Value, out produto);
                }

                var ncm = produto?.NCMS?.Trim() ?? string.Empty;
                var cfop = produto?.CFOP?.Trim() ?? string.Empty;
                var cest = string.IsNullOrWhiteSpace(produto?.CEST) ? null : produto!.CEST.Trim();
                var codigo = !string.IsNullOrWhiteSpace(produto?.Codigo)
                    ? produto!.Codigo.Trim()
                    : (linha.ProdutoId?.ToString("N") ?? string.Empty);
                var descricao = !string.IsNullOrWhiteSpace(linha.Descricao)
                    ? linha.Descricao.Trim()
                    : produto?.Nome?.Trim() ?? string.Empty;
                var unidade = string.IsNullOrWhiteSpace(produto?.UnidadeMedida)
                    ? string.Empty
                    : produto!.UnidadeMedida.Trim();

                var valorItem = linha.Subtotal;
                valorProdutos += valorItem;

                itens.Add(new FiscalNFeItem
                {
                    NumeroItem = numero++,
                    Codigo = codigo,
                    Descricao = descricao,
                    Ncm = ncm,
                    Cfop = cfop,
                    Cest = cest,
                    Unidade = unidade,
                    Quantidade = linha.Quantidade,
                    ValorUnitario = linha.PrecoUnitario,
                    ValorTotal = valorItem,
                    IcmsOrigem = emitente.DefaultIcmsOrigem?.Trim() ?? string.Empty,
                    IcmsSituacaoTributaria = emitente.DefaultIcmsSituacaoTributaria?.Trim() ?? string.Empty
                });
            }

            var docDigits = new string((cliente.CPF ?? string.Empty).Where(char.IsDigit).ToArray());
            var isCnpj = docDigits.Length > 11 ||
                         string.Equals(cliente.TipoPessoa, "Juridica", StringComparison.OrdinalIgnoreCase);

            var desconto = venda.Desconto;
            var total = valorProdutos - desconto;

            return new FiscalNFeDocument
            {
                FiscalOperationId = fiscalOperationId,
                IdempotencyKey = idempotencyKey,
                Environment = environment,
                VendaId = venda.Id,
                Emitente = emitente,
                Destinatario = new FiscalNFeDestinatario
                {
                    Nome = cliente.Nome?.Trim() ?? string.Empty,
                    Documento = docDigits,
                    IsCnpj = isCnpj,
                    Logradouro = cliente.Rua?.Trim() ?? string.Empty,
                    Numero = cliente.Numero?.Trim() ?? string.Empty,
                    Bairro = cliente.Bairro?.Trim() ?? string.Empty,
                    Municipio = cliente.Cidade?.Trim() ?? string.Empty,
                    Uf = cliente.Estado?.Trim() ?? string.Empty,
                    Cep = new string((cliente.CEP ?? string.Empty).Where(char.IsDigit).ToArray()),
                    IndicadorIeDestinatario = 9
                },
                Itens = itens,
                ValorProdutos = valorProdutos,
                ValorDesconto = desconto,
                ValorTotal = total,
                FormaPagamentoCodigo = MapFormaPagamento(venda.FormaPagamento)
            };
        }

        public FiscalEmissionRequest ToEmissionRequest(FiscalNFeDocument document)
        {
            ArgumentNullException.ThrowIfNull(document);
            return new FiscalEmissionRequest
            {
                FiscalOperationId = document.FiscalOperationId,
                IdempotencyKey = document.IdempotencyKey,
                DocumentType = FiscalDocumentType.NFe,
                Environment = document.Environment,
                Provider = FiscalProviderKind.FocusNfe,
                OriginModule = "PDV",
                VendaId = document.VendaId,
                ClienteDocumento = document.Destinatario.Documento,
                ClienteNome = document.Destinatario.Nome,
                Total = document.ValorTotal,
                Items = document.Itens.Select(i => new FiscalDocumentItemDto
                {
                    Codigo = i.Codigo,
                    Descricao = i.Descricao,
                    Ncm = i.Ncm,
                    Cfop = i.Cfop,
                    Quantidade = i.Quantidade,
                    ValorUnitario = i.ValorUnitario
                }).ToList(),
                Observacoes = "NFeHomologation"
            };
        }

        private static string? MapFormaPagamento(string? forma)
        {
            if (string.IsNullOrWhiteSpace(forma)) return null;
            // Não inventa — só mapeia se reconhecido; Focus pode rejeitar se ausente (validator não exige ainda).
            var f = forma.Trim().ToLowerInvariant();
            if (f.Contains("dinheiro")) return "01";
            if (f.Contains("cheque")) return "02";
            if (f.Contains("credito") || f.Contains("crédito")) return "03";
            if (f.Contains("debito") || f.Contains("débito")) return "04";
            if (f.Contains("pix")) return "17";
            return null;
        }
    }
}
