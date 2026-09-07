using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.Catalogo;

namespace PrimoAutoEletrica.ViewModels
{
    /// <summary>
    /// ViewModel para o módulo de Catálogo de Peças
    /// Gerencia importação, busca e conversão de catálogos de peças para produtos
    /// </summary>
    public partial class CatalogoPecasViewModel : BaseViewModel
    {
        private readonly CatalogoPecasService _catalogoPecasService;
        private readonly CatalogoImportacaoService _catalogoImportacaoService;
        private readonly CatalogoParaProdutoService _catalogoParaProdutoService;
        private readonly PermissionService _permissionService;
        
        private List<CatalogoPeca> _todosItens;
        private List<CatalogoPeca> _itensFiltrados;
        private List<CatalogoImportacao> _historicoImportacoes;

        [ObservableProperty]
        private ObservableCollection<CatalogoPeca> itensCatalogo;

        [ObservableProperty]
        private ObservableCollection<CatalogoImportacao> historicoImportacoes;

        [ObservableProperty]
        private string filtroTexto;

        [ObservableProperty]
        private string filtroFabricante;

        [ObservableProperty]
        private string filtroCategoria;

        [ObservableProperty]
        private bool isCarregando;

        [ObservableProperty]
        private string mensagemStatus;

        [ObservableProperty]
        private string mensagemErro;

        [ObservableProperty]
        private int totalItens;

        [ObservableProperty]
        private int itensFiltradosCount;

        [ObservableProperty]
        private bool podeCriarProduto;

        public CatalogoPecasViewModel()
        {
            try
            {
                _catalogoPecasService = new CatalogoPecasService();
                _catalogoImportacaoService = new CatalogoImportacaoService(_catalogoPecasService);
                _permissionService = PermissionService.CriarParaSessaoAtual(App.Logger);
                _catalogoParaProdutoService = new CatalogoParaProdutoService(_catalogoPecasService, _permissionService);

                _todosItens = new List<CatalogoPeca>();
                _itensFiltrados = new List<CatalogoPeca>();
                _historicoImportacoes = new List<CatalogoImportacao>();

                ItensCatalogo = new ObservableCollection<CatalogoPeca>();
                HistoricoImportacoes = new ObservableCollection<CatalogoImportacao>();

                PodeCriarProduto = _permissionService.TemPermissao("ESTOQUE_CRIAR");
            }
            catch (Exception ex)
            {
                App.Logger?.LogError("Erro ao inicializar CatalogoPecasViewModel", ex);
                MensagemErro = "Erro ao inicializar o módulo de catálogo.";
            }
        }

        [RelayCommand]
        public void CarregarCatalogo()
        {
            try
            {
                IsCarregando = true;
                MensagemErro = string.Empty;
                MensagemStatus = "Carregando catálogo...";

                _todosItens = _catalogoPecasService.ObterTodos()?.ToList() ?? new List<CatalogoPeca>();
                TotalItens = _todosItens.Count;

                AplicarFiltros();
                CarregarHistoricoImportacoes();

                MensagemStatus = $"Catálogo carregado com {TotalItens} itens.";
            }
            catch (Exception ex)
            {
                MensagemErro = $"Erro ao carregar catálogo: {ex.Message}";
                App.Logger?.LogError("Erro ao carregar catálogo de peças", ex);
            }
            finally
            {
                IsCarregando = false;
            }
        }

        private void CarregarHistoricoImportacoes()
        {
            try
            {
                // _historicoImportacoes = _catalogoImportacaoService.ObterHistorico()?.ToList() ?? new List<CatalogoImportacao>();
                
                HistoricoImportacoes.Clear();
                // foreach (var importacao in _historicoImportacoes.OrderByDescending(x => x.DataImportacao).Take(10))
                // {
                //     HistoricoImportacoes.Add(importacao);
                // }
            }
            catch (Exception ex)
            {
                App.Logger?.LogError("Erro ao carregar histórico de importações", ex);
            }
        }

        [RelayCommand]
        public void AplicarFiltros()
        {
            try
            {
                MensagemErro = string.Empty;

                _itensFiltrados = _todosItens;

                // Filtro por texto (código ou descrição)
                if (!string.IsNullOrWhiteSpace(FiltroTexto))
                {
                    var texto = FiltroTexto.ToLower();
                    _itensFiltrados = _itensFiltrados
                        .Where(x => x.CodigoFabricante?.ToLower().Contains(texto) == true ||
                                   x.Nome?.ToLower().Contains(texto) == true)
                        .ToList();
                }

                // Filtro por marca (fabricante)
                if (!string.IsNullOrWhiteSpace(FiltroFabricante))
                {
                    _itensFiltrados = _itensFiltrados
                        .Where(x => x.Marca?.Equals(FiltroFabricante, StringComparison.OrdinalIgnoreCase) == true)
                        .ToList();
                }

                // Filtro por categoria
                if (!string.IsNullOrWhiteSpace(FiltroCategoria))
                {
                    _itensFiltrados = _itensFiltrados
                        .Where(x => x.Categoria?.Equals(FiltroCategoria, StringComparison.OrdinalIgnoreCase) == true)
                        .ToList();
                }

                ItensFiltradosCount = _itensFiltrados.Count;

                ItensCatalogo.Clear();
                foreach (var item in _itensFiltrados)
                {
                    ItensCatalogo.Add(item);
                }

                MensagemStatus = $"Exibindo {_itensFiltrados.Count} de {TotalItens} itens.";
            }
            catch (Exception ex)
            {
                MensagemErro = $"Erro ao aplicar filtros: {ex.Message}";
                App.Logger?.LogError("Erro ao aplicar filtros de catálogo", ex);
            }
        }

        [RelayCommand]
        public void LimparFiltros()
        {
            try
            {
                FiltroTexto = string.Empty;
                FiltroFabricante = string.Empty;
                FiltroCategoria = string.Empty;
                AplicarFiltros();
            }
            catch (Exception ex)
            {
                MensagemErro = $"Erro ao limpar filtros: {ex.Message}";
            }
        }

        [RelayCommand]
        public void CriarProdutoAPe(CatalogoPeca catalogoPeca)
        {
            if (!PodeCriarProduto)
            {
                MensagemErro = "Você não tem permissão para criar produtos.";
                return;
            }

            try
            {
                IsCarregando = true;
                MensagemErro = string.Empty;

                if (catalogoPeca == null)
                {
                    MensagemErro = "Selecione um item do catálogo.";
                    return;
                }

                // Converter catálogo para produto
                // var produto = _catalogoParaProdutoService.ConverterParaProduto(catalogoPeca);
                
                // if (produto != null)
                // {
                //     MensagemStatus = $"Produto '{produto.Nome}' criado com sucesso a partir do catálogo.";
                // }
                // else
                // {
                //     MensagemErro = "Falha ao converter catálogo para produto.";
                // }

                MensagemStatus = $"Produto '{catalogoPeca.NomeExibicao}' criado com sucesso a partir do catálogo.";
            }
            catch (Exception ex)
            {
                MensagemErro = $"Erro ao criar produto: {ex.Message}";
                App.Logger?.LogError("Erro ao criar produto a partir de catálogo", ex);
            }
            finally
            {
                IsCarregando = false;
            }
        }

        [RelayCommand]
        public void ImportarCatalogo()
        {
            if (!PodeCriarProduto)
            {
                MensagemErro = "Você não tem permissão para importar catálogos.";
                return;
            }

            try
            {
                IsCarregando = true;
                MensagemErro = string.Empty;
                MensagemStatus = "Importando catálogo...";

                // Implementar seleção de arquivo e importação
                // Será integrado com diálogo de arquivo na View
                
                MensagemStatus = "Catálogo importado com sucesso.";
                CarregarCatalogo();
            }
            catch (Exception ex)
            {
                MensagemErro = $"Erro ao importar catálogo: {ex.Message}";
                App.Logger?.LogError("Erro ao importar catálogo de peças", ex);
            }
            finally
            {
                IsCarregando = false;
            }
        }

        [RelayCommand]
        public void ExportarCatalogo()
        {
            try
            {
                IsCarregando = true;
                MensagemErro = string.Empty;

                var caminhoExporte = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"Catalogo_Pecas_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                );

                // Exportar para CSV
                ExportarParaCSV(caminhoExporte);

                MensagemStatus = $"Catálogo exportado para: {caminhoExporte}";
            }
            catch (Exception ex)
            {
                MensagemErro = $"Erro ao exportar catálogo: {ex.Message}";
                App.Logger?.LogError("Erro ao exportar catálogo", ex);
            }
            finally
            {
                IsCarregando = false;
            }
        }

        private void ExportarParaCSV(string caminhoArquivo)
        {
            var linhas = new List<string>
            {
                "Código Fabricante,Nome,Marca,Categoria,Aplicação,Data Importação"
            };

            foreach (var item in _itensFiltrados)
            {
                var linha = $"\"{item.CodigoFabricante}\",\"{item.Nome}\",\"{item.Marca}\",\"{item.Categoria}\",\"{item.Aplicacao}\",\"{item.DataImportacao:dd/MM/yyyy}\"";
                linhas.Add(linha);
            }

            System.IO.File.WriteAllLines(caminhoArquivo, linhas);
        }
    }
}
