using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace PrimoAutoEletrica.Services
{
    public class ProdutoImportacaoService
    {
        private readonly IProdutoRepository _produtoRepository;
        private static LoggerService Logger => global::PrimoAutoEletrica.App.Logger;

        public ProdutoImportacaoService(DatabaseService databaseService, IProdutoRepository? produtoRepository = null)
        {
            _produtoRepository = produtoRepository ?? global::PrimoAutoEletrica.App.Repositories.Produtos;
        }

        public void PrepararConferencia(NotaFiscalImportada nota)
        {
            if (nota == null)
            {
                throw new ArgumentNullException(nameof(nota));
            }

            var todosProdutos = _produtoRepository.ObterTodos();
            var duplicatasInternas = DetectarDuplicatasInternas(nota);

            foreach (var produtoImportado in nota.Produtos)
            {
                produtoImportado.Nome = NormalizarNome(produtoImportado.Nome);
                produtoImportado.Codigo = produtoImportado.Codigo?.Trim() ?? string.Empty;
                produtoImportado.CodigoBarras = NormalizarCodigoBarras(produtoImportado.CodigoBarras, produtoImportado.Codigo);
                produtoImportado.CategoriaSugerida = string.IsNullOrWhiteSpace(produtoImportado.CategoriaSugerida)
                    ? DetectarCategoria(produtoImportado.Nome)
                    : produtoImportado.CategoriaSugerida.Trim();
                produtoImportado.MargemAplicada = produtoImportado.MargemAplicada <= 0 ? 200m : produtoImportado.MargemAplicada;
                produtoImportado.RecalcularPrecoVenda();
                produtoImportado.ObservacaoConferencia = produtoImportado.ObservacaoConferencia?.Trim() ?? string.Empty;

                if (duplicatasInternas.Contains(CriarChaveDuplicata(produtoImportado)))
                {
                    produtoImportado.SelecionadoParaImportacao = false;
                    produtoImportado.AcaoPlanejada = AcoesPlanejadas.Ignorar;
                    produtoImportado.Status = StatusImportacao.Duplicado;
                    produtoImportado.MotivoIgnorado = "Item duplicado dentro do mesmo XML.";
                    continue;
                }

                if (produtoImportado.Status == StatusImportacao.Ignorado)
                {
                    produtoImportado.SelecionadoParaImportacao = false;
                    produtoImportado.AcaoPlanejada = AcoesPlanejadas.Ignorar;
                    continue;
                }

                var produtoExistente = BuscarProdutoExistente(produtoImportado, todosProdutos);
                if (produtoExistente != null)
                {
                    produtoImportado.ProdutoExistenteId = produtoExistente.Id;
                    produtoImportado.ProdutoVinculadoReferencia = string.IsNullOrWhiteSpace(produtoImportado.ProdutoVinculadoReferencia)
                        ? $"{produtoExistente.Codigo} - {produtoExistente.Nome}".Trim()
                        : produtoImportado.ProdutoVinculadoReferencia.Trim();
                    produtoImportado.AcaoPlanejada = AcoesPlanejadas.AtualizarExistente;
                    produtoImportado.Status = StatusImportacao.Atualizado;
                    produtoImportado.MotivoIgnorado = string.Empty;
                }
                else
                {
                    produtoImportado.ProdutoExistenteId = null;
                    produtoImportado.ProdutoVinculadoReferencia = string.Empty;
                    produtoImportado.AcaoPlanejada = AcoesPlanejadas.CriarNovo;
                    produtoImportado.Status = StatusImportacao.Novo;
                    produtoImportado.MotivoIgnorado = string.Empty;
                }
            }
        }

        public void ProcessarProdutos(NotaFiscalImportada nota)
        {
            if (nota == null)
            {
                throw new ArgumentNullException(nameof(nota));
            }

            var cacheProdutos = _produtoRepository.ObterTodos();

            foreach (var produtoImportado in nota.Produtos)
            {
                try
                {
                    ProcessarProduto(nota, produtoImportado, cacheProdutos);
                }
                catch (Exception ex)
                {
                    produtoImportado.Status = StatusImportacao.Erro;
                    produtoImportado.MotivoIgnorado = ex.Message;
                    Logger.LogError($"Erro ao processar item '{produtoImportado.Nome}' na importacao de NF-e.", ex);
                }
            }
        }

        public List<string> DetectarDuplicatas(NotaFiscalImportada nota)
        {
            var mensagens = new List<string>();
            var chaves = DetectarDuplicatasInternas(nota);

            foreach (var produtoImportado in nota.Produtos.Where(produto => chaves.Contains(CriarChaveDuplicata(produto))))
            {
                mensagens.Add($"Item duplicado no XML: {produtoImportado.Nome} ({produtoImportado.Codigo})");
            }

            return mensagens
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void ProcessarProduto(NotaFiscalImportada nota, ProdutoImportado produtoImportado, IReadOnlyCollection<Produto> cacheProdutos)
        {
            produtoImportado.Nome = NormalizarNome(produtoImportado.Nome);
            produtoImportado.Codigo = produtoImportado.Codigo?.Trim() ?? string.Empty;
            produtoImportado.CodigoBarras = NormalizarCodigoBarras(produtoImportado.CodigoBarras, produtoImportado.Codigo);

            if (!produtoImportado.SelecionadoParaImportacao || string.Equals(produtoImportado.AcaoPlanejada, AcoesPlanejadas.Ignorar, StringComparison.OrdinalIgnoreCase))
            {
                produtoImportado.Status = StatusImportacao.Ignorado;
                produtoImportado.MotivoIgnorado = string.IsNullOrWhiteSpace(produtoImportado.MotivoIgnorado)
                    ? "Item ignorado na conferencia."
                    : produtoImportado.MotivoIgnorado;
                return;
            }

            ValidarItemParaImportacao(produtoImportado);

            if (string.Equals(produtoImportado.AcaoPlanejada, AcoesPlanejadas.AtualizarExistente, StringComparison.OrdinalIgnoreCase))
            {
                var produtoExistente = ResolverProdutoParaAtualizacao(produtoImportado, cacheProdutos);
                if (produtoExistente == null)
                {
                    produtoImportado.Status = StatusImportacao.Erro;
                    produtoImportado.MotivoIgnorado = "Nao foi possivel localizar o produto escolhido para atualizacao.";
                    return;
                }

                produtoImportado.ProdutoExistenteId = produtoExistente.Id;
                produtoImportado.ProdutoSnapshotAnterior = ProdutoImportacaoSnapshot.Criar(produtoExistente).ToJson();
                AtualizarProdutoExistente(produtoExistente, produtoImportado);
                var produtoAtualizado = _produtoRepository.ObterPorId(produtoExistente.Id) ?? produtoExistente;
                produtoImportado.ProdutoSnapshotPosterior = ProdutoImportacaoSnapshot.Criar(produtoAtualizado).ToJson();
                produtoImportado.Status = StatusImportacao.Atualizado;
                produtoImportado.MotivoIgnorado = string.Empty;
                return;
            }

            if (!string.Equals(produtoImportado.AcaoPlanejada, AcoesPlanejadas.CriarNovo, StringComparison.OrdinalIgnoreCase))
            {
                produtoImportado.Status = StatusImportacao.Erro;
                produtoImportado.MotivoIgnorado = "Acao planejada invalida para este item.";
                return;
            }

            var novoProduto = CriarNovoProduto(produtoImportado, nota);
            _produtoRepository.Inserir(novoProduto);
            produtoImportado.ProdutoExistenteId = novoProduto.Id;
            produtoImportado.ProdutoVinculadoReferencia = $"{novoProduto.Codigo} - {novoProduto.Nome}".Trim();
            produtoImportado.Status = StatusImportacao.Novo;
            produtoImportado.MotivoIgnorado = string.Empty;
        }

        private static void ValidarItemParaImportacao(ProdutoImportado produtoImportado)
        {
            if (string.IsNullOrWhiteSpace(produtoImportado.Nome))
            {
                throw new InvalidOperationException("Item sem descricao valida.");
            }

            if (produtoImportado.Quantidade <= 0)
            {
                throw new InvalidOperationException("Quantidade invalida para importacao.");
            }

            if (produtoImportado.ValorUnitario <= 0)
            {
                throw new InvalidOperationException("Valor unitario invalido para importacao.");
            }

            if (string.Equals(produtoImportado.AcaoPlanejada, AcoesPlanejadas.CriarNovo, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(produtoImportado.CategoriaSugerida))
                {
                    throw new InvalidOperationException("Defina uma categoria para o novo produto.");
                }

                if (produtoImportado.PrecoVendaSugerido <= 0)
                {
                    throw new InvalidOperationException("Defina um preco de venda valido para o novo produto.");
                }
            }
        }

        private static string NormalizarNome(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                return string.Empty;
            }

            nome = Regex.Replace(nome, @"\s+", " ").Trim();
            nome = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(nome.ToLowerInvariant());
            nome = nome.Replace("Alt", "Alternador");
            nome = nome.Replace("Mot", "Motor");
            nome = nome.Replace("Bob", "Bobina");
            nome = nome.Replace("Sen", "Sensor");
            nome = nome.Replace("Mod", "Modulo");
            nome = nome.Replace("Fus", "Fusivel");
            nome = nome.Replace("Rel", "Rele");

            return nome;
        }

        private Produto? BuscarProdutoExistente(ProdutoImportado produtoImportado, IReadOnlyCollection<Produto>? cacheProdutos = null)
        {
            var todosProdutos = cacheProdutos?.ToList() ?? _produtoRepository.ObterTodos();

            var porCodigo = todosProdutos.FirstOrDefault(p =>
                !string.IsNullOrWhiteSpace(produtoImportado.Codigo) &&
                string.Equals(p.Codigo, produtoImportado.Codigo, StringComparison.OrdinalIgnoreCase));
            if (porCodigo != null)
            {
                return porCodigo;
            }

            var porCodigoBarras = todosProdutos.FirstOrDefault(p =>
                !string.IsNullOrWhiteSpace(produtoImportado.CodigoBarras) &&
                string.Equals(p.CodigoBarras, produtoImportado.CodigoBarras, StringComparison.OrdinalIgnoreCase));
            if (porCodigoBarras != null)
            {
                return porCodigoBarras;
            }

            var porNome = todosProdutos.FirstOrDefault(p =>
                string.Equals(p.Nome, produtoImportado.Nome, StringComparison.OrdinalIgnoreCase));
            if (porNome != null)
            {
                return porNome;
            }

            return todosProdutos.FirstOrDefault(p =>
                !string.IsNullOrWhiteSpace(p.Nome) &&
                (p.Nome.Contains(produtoImportado.Nome, StringComparison.OrdinalIgnoreCase) ||
                 produtoImportado.Nome.Contains(p.Nome, StringComparison.OrdinalIgnoreCase)));
        }

        private Produto? ResolverProdutoParaAtualizacao(ProdutoImportado produtoImportado, IReadOnlyCollection<Produto> cacheProdutos)
        {
            if (produtoImportado.ProdutoExistenteId.HasValue)
            {
                var porId = _produtoRepository.ObterPorId(produtoImportado.ProdutoExistenteId.Value);
                if (porId != null)
                {
                    return porId;
                }
            }

            if (!string.IsNullOrWhiteSpace(produtoImportado.ProdutoVinculadoReferencia))
            {
                var referencia = produtoImportado.ProdutoVinculadoReferencia.Trim();
                var porReferencia = cacheProdutos.FirstOrDefault(produto =>
                    string.Equals(produto.Codigo, referencia, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(produto.CodigoBarras, referencia, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(produto.Nome, referencia, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals($"{produto.Codigo} - {produto.Nome}".Trim(), referencia, StringComparison.OrdinalIgnoreCase));

                if (porReferencia != null)
                {
                    return porReferencia;
                }
            }

            return BuscarProdutoExistente(produtoImportado, cacheProdutos);
        }

        private void AtualizarProdutoExistente(Produto produtoExistente, ProdutoImportado produtoImportado)
        {
            produtoExistente.PrecoCompra = produtoImportado.ValorUnitario;
            produtoExistente.QuantidadeEstoque += ConverterQuantidade(produtoImportado.Quantidade);
            produtoExistente.ValorTotalEstoque = produtoExistente.QuantidadeEstoque * produtoExistente.PrecoCompra;
            produtoExistente.DataUltimaCompra = DateTime.Now;
            produtoExistente.DataUltimaAtualizacao = DateTime.Now;
            produtoExistente.CodigoBarras = string.IsNullOrWhiteSpace(produtoImportado.CodigoBarras)
                ? produtoExistente.CodigoBarras
                : produtoImportado.CodigoBarras;
            produtoExistente.NCMS = string.IsNullOrWhiteSpace(produtoImportado.NCM) ? produtoExistente.NCMS : produtoImportado.NCM;
            produtoExistente.CFOP = string.IsNullOrWhiteSpace(produtoImportado.CFOP) ? produtoExistente.CFOP : produtoImportado.CFOP;
            produtoExistente.UnidadeMedida = string.IsNullOrWhiteSpace(produtoImportado.UnidadeMedida) ? produtoExistente.UnidadeMedida : produtoImportado.UnidadeMedida;
            produtoExistente.Categoria = string.IsNullOrWhiteSpace(produtoImportado.CategoriaSugerida) ? produtoExistente.Categoria : produtoImportado.CategoriaSugerida;
            produtoExistente.PrecoVenda = produtoImportado.PrecoVendaSugerido > 0
                ? produtoImportado.PrecoVendaSugerido
                : produtoExistente.PrecoVenda;
            produtoExistente.MargemLucro = produtoExistente.PrecoVenda > 0
                ? Math.Round(((produtoExistente.PrecoVenda - produtoExistente.PrecoCompra) / produtoExistente.PrecoVenda) * 100m, 2, MidpointRounding.AwayFromZero)
                : 0;

            _produtoRepository.Atualizar(produtoExistente);
        }

        private Produto CriarNovoProduto(ProdutoImportado produtoImportado, NotaFiscalImportada nota)
        {
            var quantidade = ConverterQuantidade(produtoImportado.Quantidade);
            var precoVenda = produtoImportado.PrecoVendaSugerido > 0
                ? produtoImportado.PrecoVendaSugerido
                : CalcularPrecoVenda(produtoImportado.ValorUnitario, produtoImportado.MargemAplicada);

            return new Produto
            {
                Id = Guid.NewGuid(),
                Codigo = produtoImportado.Codigo,
                Nome = produtoImportado.Nome,
                Descricao = $"Importado da NF-e {nota.Numero} - {nota.Fornecedor.Nome}",
                Categoria = produtoImportado.CategoriaSugerida,
                Marca = DetectarMarca(produtoImportado.Nome),
                Modelo = produtoImportado.Codigo,
                QuantidadeEstoque = quantidade,
                QuantidadeMinima = 5,
                QuantidadeMaxima = 0,
                Localizacao = "A1",
                Prateleira = "Gaveta 1",
                Gaveta = "Importacao",
                PrecoCompra = produtoImportado.ValorUnitario,
                PrecoVenda = precoVenda,
                MargemLucro = precoVenda > 0
                    ? Math.Round(((precoVenda - produtoImportado.ValorUnitario) / precoVenda) * 100m, 2, MidpointRounding.AwayFromZero)
                    : 0,
                ValorTotalEstoque = quantidade * produtoImportado.ValorUnitario,
                Fornecedor = nota.Fornecedor.Nome,
                TelefoneFornecedor = nota.Fornecedor.Telefone,
                Ativo = true,
                ProdutoPerecivel = false,
                DataCadastro = DateTime.Now,
                DataUltimaCompra = DateTime.Now,
                DataUltimaAtualizacao = DateTime.Now,
                Observacoes = CriarObservacoesImportacao(nota, produtoImportado),
                CodigoBarras = NormalizarCodigoBarras(produtoImportado.CodigoBarras, produtoImportado.Codigo),
                NCMS = produtoImportado.NCM,
                CFOP = produtoImportado.CFOP,
                UnidadeMedida = produtoImportado.UnidadeMedida,
                TotalVendas = 0,
                TotalFaturado = 0,
                VendasUltimoMes = 0,
                VendasUltimoTrimestre = 0
            };
        }

        private static int ConverterQuantidade(decimal quantidade)
        {
            return quantidade <= 0
                ? 0
                : decimal.ToInt32(decimal.Round(quantidade, 0, MidpointRounding.AwayFromZero));
        }

        private static string CriarObservacoesImportacao(NotaFiscalImportada nota, ProdutoImportado produtoImportado)
        {
            var observacoes = $"Importado automaticamente da NF-e {nota.Numero} de {nota.DataEmissao:dd/MM/yyyy}.";
            if (!string.IsNullOrWhiteSpace(produtoImportado.ObservacaoConferencia))
            {
                observacoes += $" Conferencia: {produtoImportado.ObservacaoConferencia}.";
            }

            return observacoes;
        }

        private static string DetectarCategoria(string nome)
        {
            nome = nome.ToLowerInvariant();

            if (nome.Contains("alternador"))
                return "Alternadores";
            if (nome.Contains("motor") || nome.Contains("partida"))
                return "Motores de Partida";
            if (nome.Contains("bobina"))
                return "Bobinas de Ignicao";
            if (nome.Contains("sensor"))
                return "Sensores";
            if (nome.Contains("fusivel"))
                return "Fusiveis";
            if (nome.Contains("rele"))
                return "Reles";
            if (nome.Contains("lampada") || nome.Contains("lanterna"))
                return "Lampadas";
            if (nome.Contains("bateria"))
                return "Baterias";
            if (nome.Contains("cabo") || nome.Contains("fio"))
                return "Cabos e Fios";
            if (nome.Contains("terminal") || nome.Contains("conector"))
                return "Terminais e Conectores";
            if (nome.Contains("modulo") || nome.Contains("central"))
                return "Modulos e Centralinas";
            if (nome.Contains("soquete"))
                return "Soquetes";

            return "Pecas Diversas";
        }

        private static string DetectarMarca(string nome)
        {
            nome = nome.ToUpperInvariant();

            if (nome.Contains("BOSCH"))
                return "Bosch";
            if (nome.Contains("VALEO"))
                return "Valeo";
            if (nome.Contains("DELCO"))
                return "Delco";
            if (nome.Contains("DENSO"))
                return "Denso";
            if (nome.Contains("MAGNETI"))
                return "Magneti Marelli";
            if (nome.Contains("NGK"))
                return "NGK";
            if (nome.Contains("HELLA"))
                return "Hella";
            if (nome.Contains("PHILIPS"))
                return "Philips";
            if (nome.Contains("OSRAM"))
                return "Osram";
            if (nome.Contains("KITEC"))
                return "Kitec";
            if (nome.Contains("OKEI"))
                return "Okei";
            if (nome.Contains("VONDER"))
                return "Vonder";

            return "Generico";
        }

        private static decimal CalcularPrecoVenda(decimal precoCompra, decimal margemAplicada)
        {
            var fator = 1m + (Math.Max(0, margemAplicada) / 100m);
            return Math.Round(precoCompra * fator, 2, MidpointRounding.AwayFromZero);
        }

        private static HashSet<string> DetectarDuplicatasInternas(NotaFiscalImportada nota)
        {
            return nota.Produtos
                .GroupBy(CriarChaveDuplicata, StringComparer.OrdinalIgnoreCase)
                .Where(grupo => !string.IsNullOrWhiteSpace(grupo.Key) && grupo.Count() > 1)
                .Select(grupo => grupo.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static string CriarChaveDuplicata(ProdutoImportado produto)
        {
            return $"{produto.Codigo}|{produto.Nome}|{produto.ValorUnitario.ToString("F2", CultureInfo.InvariantCulture)}";
        }

        private static string NormalizarCodigoBarras(string? codigoBarras, string codigoFallback)
        {
            return string.IsNullOrWhiteSpace(codigoBarras)
                ? codigoFallback?.Trim() ?? string.Empty
                : codigoBarras.Trim();
        }
    }
}
