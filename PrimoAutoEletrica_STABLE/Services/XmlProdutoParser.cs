using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public class XmlProdutoParser
    {
        private static LoggerService Logger => global::PrimoAutoEletrica.App.Logger;

        public NotaFiscalImportada ParseXmlNFe(string caminhoArquivo)
        {
            if (!File.Exists(caminhoArquivo))
            {
                throw new FileNotFoundException($"Arquivo nao encontrado: {caminhoArquivo}");
            }

            var xml = SecureXmlLoader.Load(caminhoArquivo);
            var ns = GetNamespace(xml);

            var nota = new NotaFiscalImportada
            {
                CaminhoArquivo = caminhoArquivo,
                Fornecedor = ParseFornecedor(xml, ns),
                Produtos = ParseProdutos(xml, ns),
                ChaveAcesso = ParseChaveAcesso(xml, ns),
                Numero = ParseNumero(xml, ns),
                Serie = ParseSerie(xml, ns),
                DataEmissao = ParseDataEmissao(xml, ns),
                ValorTotal = ParseValorTotal(xml, ns),
                ValorProdutos = ParseValorProdutos(xml, ns)
            };

            nota.Status = StatusImportacaoNota.Pendente;

            return nota;
        }

        private XNamespace GetNamespace(XDocument xml)
        {
            var root = xml.Root;
            if (root == null)
            {
                return "";
            }

            var nfeElement = root.Element(XName.Get("NFe", "http://www.portalfiscal.inf.br/nfe"));
            if (nfeElement != null)
            {
                return "http://www.portalfiscal.inf.br/nfe";
            }

            return root.GetDefaultNamespace();
        }

        private FornecedorNota ParseFornecedor(XDocument xml, XNamespace ns)
        {
            var fornecedor = new FornecedorNota();

            try
            {
                var emit = xml.Descendants(XName.Get("emit", ns.NamespaceName)).FirstOrDefault();
                if (emit == null)
                {
                    return fornecedor;
                }

                fornecedor.CNPJ = emit.Element(XName.Get("CNPJ", ns.NamespaceName))?.Value ?? "";
                fornecedor.Nome = emit.Element(XName.Get("xNome", ns.NamespaceName))?.Value ?? "";
                fornecedor.NomeFantasia = emit.Element(XName.Get("xFant", ns.NamespaceName))?.Value ?? "";
                fornecedor.IE = emit.Element(XName.Get("IE", ns.NamespaceName))?.Value ?? "";

                var enderEmit = emit.Element(XName.Get("enderEmit", ns.NamespaceName));
                if (enderEmit != null)
                {
                    fornecedor.Logradouro = enderEmit.Element(XName.Get("xLgr", ns.NamespaceName))?.Value ?? "";
                    fornecedor.Numero = enderEmit.Element(XName.Get("nro", ns.NamespaceName))?.Value ?? "";
                    fornecedor.Complemento = enderEmit.Element(XName.Get("xCpl", ns.NamespaceName))?.Value ?? "";
                    fornecedor.Bairro = enderEmit.Element(XName.Get("xBairro", ns.NamespaceName))?.Value ?? "";
                    fornecedor.Municipio = enderEmit.Element(XName.Get("xMun", ns.NamespaceName))?.Value ?? "";
                    fornecedor.UF = enderEmit.Element(XName.Get("UF", ns.NamespaceName))?.Value ?? "";
                    fornecedor.CEP = enderEmit.Element(XName.Get("CEP", ns.NamespaceName))?.Value ?? "";
                }

                fornecedor.Telefone = emit.Element(XName.Get("fone", ns.NamespaceName))?.Value ?? "";
            }
            catch (Exception ex)
            {
                Logger.LogError("Erro ao parsear fornecedor na leitura do XML da NF-e.", ex);
            }

            return fornecedor;
        }

        private List<ProdutoImportado> ParseProdutos(XDocument xml, XNamespace ns)
        {
            var produtos = new List<ProdutoImportado>();

            try
            {
                var detElements = xml.Descendants(XName.Get("det", ns.NamespaceName));

                foreach (var det in detElements)
                {
                    var prod = det.Element(XName.Get("prod", ns.NamespaceName));
                    if (prod == null)
                    {
                        continue;
                    }

                    var produtoImportado = new ProdutoImportado
                    {
                        Codigo = prod.Element(XName.Get("cProd", ns.NamespaceName))?.Value ?? "",
                        CodigoBarras = ObterCodigoBarras(prod, ns),
                        Nome = prod.Element(XName.Get("xProd", ns.NamespaceName))?.Value ?? "",
                        NCM = prod.Element(XName.Get("NCM", ns.NamespaceName))?.Value ?? "",
                        CFOP = prod.Element(XName.Get("CFOP", ns.NamespaceName))?.Value ?? "",
                        Quantidade = ParseDecimal(prod.Element(XName.Get("qCom", ns.NamespaceName))?.Value),
                        ValorUnitario = ParseDecimal(prod.Element(XName.Get("vUnCom", ns.NamespaceName))?.Value),
                        ValorTotal = ParseDecimal(prod.Element(XName.Get("vProd", ns.NamespaceName))?.Value),
                        UnidadeMedida = prod.Element(XName.Get("uCom", ns.NamespaceName))?.Value ?? "",
                        Status = StatusImportacao.Pendente,
                        SelecionadoParaImportacao = true
                    };

                    produtoImportado.RecalcularPrecoVenda();

                    if (string.IsNullOrWhiteSpace(produtoImportado.Nome))
                    {
                        produtoImportado.Status = StatusImportacao.Ignorado;
                        produtoImportado.MotivoIgnorado = "Produto sem nome";
                    }
                    else if (produtoImportado.Quantidade <= 0)
                    {
                        produtoImportado.Status = StatusImportacao.Ignorado;
                        produtoImportado.MotivoIgnorado = "Quantidade invalida";
                    }
                    else if (produtoImportado.ValorUnitario <= 0)
                    {
                        produtoImportado.Status = StatusImportacao.Ignorado;
                        produtoImportado.MotivoIgnorado = "Valor unitario invalido";
                    }
                    else
                    {
                        produtoImportado.Status = StatusImportacao.Pendente;
                    }

                    produtos.Add(produtoImportado);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Erro ao parsear produtos na leitura do XML da NF-e.", ex);
            }

            return produtos;
        }

        private string ParseChaveAcesso(XDocument xml, XNamespace ns)
        {
            try
            {
                var protNFe = xml.Descendants(XName.Get("protNFe", ns.NamespaceName)).FirstOrDefault();
                if (protNFe != null)
                {
                    var infProt = protNFe.Element(XName.Get("infProt", ns.NamespaceName));
                    if (infProt != null)
                    {
                        return infProt.Element(XName.Get("chNFe", ns.NamespaceName))?.Value ?? "";
                    }
                }

                var infNFe = xml.Descendants(XName.Get("infNFe", ns.NamespaceName)).FirstOrDefault();
                if (infNFe != null)
                {
                    return infNFe.Attribute("Id")?.Value?.Replace("NFe", "") ?? "";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Erro ao parsear chave de acesso da NF-e.", ex);
            }

            return "";
        }

        private string ParseNumero(XDocument xml, XNamespace ns)
        {
            try
            {
                var ide = xml.Descendants(XName.Get("ide", ns.NamespaceName)).FirstOrDefault();
                return ide?.Element(XName.Get("nNF", ns.NamespaceName))?.Value ?? "";
            }
            catch (Exception ex)
            {
                Logger.LogError("Erro ao parsear numero da NF-e.", ex);
            }

            return "";
        }

        private string ParseSerie(XDocument xml, XNamespace ns)
        {
            try
            {
                var ide = xml.Descendants(XName.Get("ide", ns.NamespaceName)).FirstOrDefault();
                return ide?.Element(XName.Get("serie", ns.NamespaceName))?.Value ?? "";
            }
            catch (Exception ex)
            {
                Logger.LogError("Erro ao parsear serie da NF-e.", ex);
            }

            return "";
        }

        private DateTime ParseDataEmissao(XDocument xml, XNamespace ns)
        {
            try
            {
                var ide = xml.Descendants(XName.Get("ide", ns.NamespaceName)).FirstOrDefault();
                var dhEmi = ide?.Element(XName.Get("dhEmi", ns.NamespaceName))?.Value ?? "";

                if (DateTime.TryParse(dhEmi, out var data))
                {
                    return data;
                }

                var dEmi = ide?.Element(XName.Get("dEmi", ns.NamespaceName))?.Value ?? "";
                if (DateTime.TryParse(dEmi, out var dataAntiga))
                {
                    return dataAntiga;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Erro ao parsear data de emissao da NF-e.", ex);
            }

            return DateTime.Now;
        }

        private decimal ParseValorTotal(XDocument xml, XNamespace ns)
        {
            try
            {
                var total = xml.Descendants(XName.Get("total", ns.NamespaceName)).FirstOrDefault();
                if (total == null)
                {
                    return 0;
                }

                var icmsTot = total.Element(XName.Get("ICMSTot", ns.NamespaceName));
                var valorNota = icmsTot?.Element(XName.Get("vNF", ns.NamespaceName))?.Value ?? "0";
                return ParseDecimal(valorNota);
            }
            catch (Exception ex)
            {
                Logger.LogError("Erro ao parsear valor total da NF-e.", ex);
            }

            return 0;
        }

        private decimal ParseValorProdutos(XDocument xml, XNamespace ns)
        {
            try
            {
                var total = xml.Descendants(XName.Get("total", ns.NamespaceName)).FirstOrDefault();
                if (total != null)
                {
                    var icmsTot = total.Element(XName.Get("ICMSTot", ns.NamespaceName));
                    if (icmsTot != null)
                    {
                        var vProd = icmsTot.Element(XName.Get("vProd", ns.NamespaceName))?.Value ?? "0";
                        return ParseDecimal(vProd);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Erro ao parsear valor total dos produtos da NF-e.", ex);
            }

            return 0;
        }

        private decimal ParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            if (decimal.TryParse(
                value,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var result))
            {
                return result;
            }

            if (decimal.TryParse(value.Replace(".", ","), out var result2))
            {
                return result2;
            }

            return 0;
        }

        private static string ObterCodigoBarras(XElement prod, XNamespace ns)
        {
            var codigo = prod.Element(XName.Get("cEAN", ns.NamespaceName))?.Value ?? "";
            if (string.IsNullOrWhiteSpace(codigo) || codigo.Equals("SEM GTIN", StringComparison.OrdinalIgnoreCase))
            {
                codigo = prod.Element(XName.Get("cEANTrib", ns.NamespaceName))?.Value ?? "";
            }

            return string.Equals(codigo, "SEM GTIN", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : codigo;
        }

        public bool IsValidNFeXml(string caminhoArquivo)
        {
            try
            {
                if (!File.Exists(caminhoArquivo))
                {
                    return false;
                }

                var xml = SecureXmlLoader.Load(caminhoArquivo);
                var root = xml.Root;

                var nfeElement = root?.Element(XName.Get("NFe", "http://www.portalfiscal.inf.br/nfe"));
                if (nfeElement != null)
                {
                    return true;
                }

                var infNFe = root?.Descendants(XName.Get("infNFe", "http://www.portalfiscal.inf.br/nfe")).FirstOrDefault();
                if (infNFe != null)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Arquivo rejeitado como NF-e valida: '{caminhoArquivo}'. Detalhes: {ex.Message}");
                return false;
            }
        }
    }
}
