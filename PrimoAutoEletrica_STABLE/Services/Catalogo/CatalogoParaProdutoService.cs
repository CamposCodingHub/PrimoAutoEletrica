using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Views;
using System;
using System.Linq;
using System.Text;
using System.Windows;

namespace PrimoAutoEletrica.Services.Catalogo
{
    public sealed class CatalogoParaProdutoService
    {
        private readonly CatalogoPecasService _catalogoPecasService;
        private readonly PermissionService _permissionService;
        private readonly LoggerService _logger;

        public CatalogoParaProdutoService(
            CatalogoPecasService? catalogoPecasService = null,
            PermissionService? permissionService = null,
            LoggerService? logger = null)
        {
            _catalogoPecasService = catalogoPecasService ?? new CatalogoPecasService();
            _permissionService = permissionService ?? PermissionService.CriarParaSessaoAtual(global::PrimoAutoEletrica.App.Logger);
            _logger = logger ?? global::PrimoAutoEletrica.App.Logger;
        }

        public Produto? IniciarConversao(Window? owner, CatalogoPeca item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (!_permissionService.TemPermissaoCodigo("CATALOGO_CRIAR_PRODUTO"))
            {
                MessageBox.Show(
                    owner,
                    "Sua sessao nao possui permissao para criar produtos a partir do catalogo.",
                    "Catalogo de Pecas",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return null;
            }

            var produtoExistente = EncontrarProdutoExistente(item);
            if (produtoExistente != null)
            {
                var resultado = MessageBox.Show(
                    owner,
                    $"Ja existe um produto com codigo ou SKU semelhante.\n\nCodigo: {produtoExistente.Codigo}\nNome: {produtoExistente.Nome}\n\nSim = vincular ao produto existente\nNao = abrir cadastro preenchido mesmo assim\nCancelar = abortar",
                    "Produto ja cadastrado",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    _catalogoPecasService.VincularProdutoEstoque(item.Id, produtoExistente.Id);
                    RegistrarConversao(item, produtoExistente, vinculoExistente: true);
                    return produtoExistente;
                }

                if (resultado == MessageBoxResult.Cancel)
                {
                    return null;
                }
            }

            var produtoBase = CriarProdutoBase(item);
            var window = new NovoProdutoWindow(produtoBase);

            if (owner != null && owner.IsLoaded && owner.IsVisible)
            {
                window.Owner = owner;
            }

            var sucesso = window.ShowDialog() == true && window.ProdutoCriado != null;
            if (!sucesso)
            {
                return null;
            }

            var produtoCriado = window.ProdutoCriado!;
            _catalogoPecasService.VincularProdutoEstoque(item.Id, produtoCriado.Id);
            RegistrarConversao(item, produtoCriado, vinculoExistente: false);
            return produtoCriado;
        }

        public Produto CriarProdutoBase(CatalogoPeca item)
        {
            var nome = !string.IsNullOrWhiteSpace(item.Nome)
                ? item.Nome
                : CatalogoPdfTextParser.GerarNomeFallback(item.Marca, item.CodigoFabricante);

            var descricao = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(item.Descricao))
            {
                descricao.Append(item.Descricao.Trim());
            }

            if (!string.IsNullOrWhiteSpace(item.ObservacoesTecnicas))
            {
                if (descricao.Length > 0)
                {
                    descricao.Append(" | ");
                }

                descricao.Append(item.ObservacoesTecnicas.Trim());
            }

            return new Produto
            {
                Id = Guid.NewGuid(),
                Codigo = !string.IsNullOrWhiteSpace(item.CodigoNormalizado) ? item.CodigoNormalizado : item.CodigoFabricante,
                SKU = item.CodigoFabricante,
                CodigoBarras = string.Empty,
                Nome = nome,
                Descricao = descricao.ToString(),
                Categoria = item.Categoria,
                Marca = item.Marca,
                Modelo = string.Empty,
                Fornecedor = string.Empty,
                QuantidadeEstoque = 0,
                QuantidadeMinima = 0,
                QuantidadeMaxima = 0,
                Localizacao = string.Empty,
                Prateleira = string.Empty,
                Gaveta = string.Empty,
                PrecoCompra = 0,
                PrecoVenda = 0,
                UnidadeMedida = "UN",
                ImagemUrl = item.ImagemLocal,
                Observacoes = item.Aplicacao
            };
        }

        private Produto? EncontrarProdutoExistente(CatalogoPeca item)
        {
            var produtos = global::PrimoAutoEletrica.App.Repositories.Produtos.ObterTodos();
            return produtos.FirstOrDefault(produto =>
                (!string.IsNullOrWhiteSpace(item.CodigoNormalizado) &&
                 string.Equals(produto.Codigo, item.CodigoNormalizado, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(item.CodigoFabricante) &&
                 string.Equals(produto.SKU, item.CodigoFabricante, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(item.CodigoFabricante) &&
                 string.Equals(produto.Codigo, item.CodigoFabricante, StringComparison.OrdinalIgnoreCase)));
        }

        private void RegistrarConversao(CatalogoPeca item, Produto produto, bool vinculoExistente)
        {
            var detalhes = $"Catalogo={item.CodigoFabricante}; Produto={produto.Codigo}; ProdutoId={produto.Id}; VinculoExistente={vinculoExistente}";
            _logger.LogInfo($"Conversao de catalogo para estoque concluida. {detalhes}");
            global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                "Catalogo",
                vinculoExistente ? "VincularCatalogoProdutoExistente" : "CriarProdutoAPartirDoCatalogo",
                "CatalogoPecas",
                item.Id.ToString(),
                detalhes);
        }

    }
}
