using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.UserControls
{
    public partial class RelatoriosControl : UserControl
    {
        private readonly RelatoriosViewModel _viewModel;
        private readonly PermissionService _permissionService;
        private bool _cargaInicialSolicitada;

        public RelatoriosViewModel ViewModel => _viewModel;

        public RelatoriosControl()
        {
            InitializeComponent();
            _viewModel = new RelatoriosViewModel(autoLoad: false);
            _permissionService = PermissionService.CriarParaSessaoAtual(App.Logger, App.Database);
            DataContext = _viewModel;
            Loaded += RelatoriosControl_Loaded;
        }

        private async void RelatoriosControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_cargaInicialSolicitada)
            {
                return;
            }

            _cargaInicialSolicitada = true;
            await ExecutarAcaoAssincronaAsync(
                () => _viewModel.CarregarDadosAsync(),
                "Falha ao carregar a central de relatorios.",
                "Relatorios");
        }

        private async void Atualizar_Click(object sender, RoutedEventArgs e)
        {
            await ExecutarAcaoAssincronaAsync(
                () => _viewModel.CarregarDadosAsync(),
                "Falha ao atualizar a central de relatorios.",
                "Relatorios");
        }

        private void ExportarPDF_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("RELATORIOS_EXPORTAR", "Voce nao possui permissao para exportar relatorios."))
                return;

            try
            {
                var caminho = _viewModel.ExportarPDF();
                ExibirMensagem($"PDF gerado com sucesso:\n{caminho}", "Exportar PDF", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                global::PrimoAutoEletrica.App.Logger.LogError("Falha ao exportar PDF dos relatorios.", ex, "Relatorios");
                ExibirMensagem($"Erro ao exportar PDF: {ex.Message}", "Erro", MessageBoxImage.Error, ex);
            }
        }

        private void ExportarExcel_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("RELATORIOS_EXPORTAR", "Voce nao possui permissao para exportar relatorios."))
                return;

            try
            {
                var caminho = _viewModel.ExportarExcel();
                ExibirMensagem($"Excel/CSV gerado com sucesso:\n{caminho}", "Exportar Excel", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                global::PrimoAutoEletrica.App.Logger.LogError("Falha ao exportar Excel/CSV dos relatorios.", ex, "Relatorios");
                ExibirMensagem($"Erro ao exportar Excel/CSV: {ex.Message}", "Erro", MessageBoxImage.Error, ex);
            }
        }

        private void ExportarEvidencias_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("RELATORIOS_EXPORTAR", "Voce nao possui permissao para exportar relatorios."))
                return;

            try
            {
                var pacote = _viewModel.ExportarPacoteEvidencias();
                ExibirMensagem(
                    $"Pacote de evidencias gerado com {pacote.TotalArquivos} arquivo(s):\n{pacote.Diretorio}",
                    "Evidencias",
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                global::PrimoAutoEletrica.App.Logger.LogError("Falha ao exportar pacote de evidencias dos relatorios.", ex, "Relatorios");
                ExibirMensagem($"Erro ao exportar evidencias: {ex.Message}", "Erro", MessageBoxImage.Error, ex);
            }
        }

        private void Imprimir_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("RELATORIOS_IMPRIMIR", "Voce nao possui permissao para imprimir relatorios."))
                return;

            try
            {
                if (App.IsAutomatedTestMode)
                {
                    global::PrimoAutoEletrica.App.Logger.LogInfo("Impressao do painel de relatorios validada em automacao sem abrir dialogo de impressora.", "Relatorios");
                    return;
                }

                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintVisual(this, $"Relatorios executivos - {DateTime.Now:dd/MM/yyyy HH:mm}");
                }
            }
            catch (Exception ex)
            {
                global::PrimoAutoEletrica.App.Logger.LogError("Falha ao imprimir o painel de relatorios.", ex, "Relatorios");
                ExibirMensagem($"Erro ao imprimir: {ex.Message}", "Erro", MessageBoxImage.Error, ex);
            }
        }

        private bool ValidarPermissao(string codigoPermissao, string mensagem)
        {
            if (_permissionService.TemPermissaoCodigo(codigoPermissao))
                return true;

            global::PrimoAutoEletrica.App.Logger.LogWarning($"Permissao negada no workspace de relatorios: {codigoPermissao}.", "Seguranca");
            ExibirMensagem(mensagem, "Acesso negado", MessageBoxImage.Warning);
            return false;
        }

        private void Favoritos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.AlternarFavorito();
                var mensagem = _viewModel.RelatorioFavorito
                    ? "Workspace de relatorios marcado como favorito."
                    : "Workspace de relatorios removido dos favoritos.";

                ExibirMensagem(mensagem, "Favoritos", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                global::PrimoAutoEletrica.App.Logger.LogError("Falha ao alterar favorito do workspace de relatorios.", ex, "Relatorios");
                ExibirMensagem($"Erro ao alterar favoritos: {ex.Message}", "Erro", MessageBoxImage.Error, ex);
            }
        }

        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.SalvarWorkspaceAtual();
                ExibirMensagem("Filtros e preferencias do workspace salvos com sucesso!", "Salvar", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                global::PrimoAutoEletrica.App.Logger.LogError("Falha ao salvar workspace de relatorios.", ex, "Relatorios");
                ExibirMensagem($"Erro ao salvar filtros: {ex.Message}", "Erro", MessageBoxImage.Error, ex);
            }
        }

        private void ModoExecutivo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.AlternarModoExecutivo();
                var mensagem = _viewModel.ModoExecutivoAtivo
                    ? "Modo executivo ativado."
                    : "Modo executivo desativado.";

                ExibirMensagem(mensagem, "Modo Executivo", MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                global::PrimoAutoEletrica.App.Logger.LogError("Falha ao alternar modo executivo dos relatorios.", ex, "Relatorios");
                ExibirMensagem($"Erro ao alternar modo executivo: {ex.Message}", "Erro", MessageBoxImage.Error, ex);
            }
        }

        private void TelaCheia_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.WindowState = window.WindowState == WindowState.Maximized
                        ? WindowState.Normal
                        : WindowState.Maximized;
                }
            }
            catch (Exception ex)
            {
                global::PrimoAutoEletrica.App.Logger.LogError("Falha ao alternar tela cheia do painel de relatorios.", ex, "Relatorios");
                ExibirMensagem($"Erro ao alternar tela cheia: {ex.Message}", "Erro", MessageBoxImage.Error, ex);
            }
        }

        private async void AplicarFiltros_Click(object sender, RoutedEventArgs e)
        {
            await ExecutarAcaoAssincronaAsync(
                () => _viewModel.AplicarFiltrosAsync(),
                "Falha ao aplicar filtros dos relatorios.",
                "Relatorios");
        }

        private async void LimparFiltros_Click(object sender, RoutedEventArgs e)
        {
            await ExecutarAcaoAssincronaAsync(
                () => _viewModel.LimparFiltrosAsync(),
                "Falha ao limpar filtros dos relatorios.",
                "Relatorios");
        }

        private async void AplicarAuditoriaFiltros_Click(object sender, RoutedEventArgs e)
        {
            await ExecutarAcaoAssincronaAsync(
                () => _viewModel.AplicarFiltrosAuditoriaAsync(),
                "Falha ao consultar auditoria operacional.",
                "Auditoria");
        }

        private async void AuditoriaAnterior_Click(object sender, RoutedEventArgs e)
        {
            await ExecutarAcaoAssincronaAsync(
                () => _viewModel.RetrocederPaginaAuditoriaAsync(),
                "Falha ao voltar a pagina da auditoria.",
                "Auditoria");
        }

        private async void AuditoriaProxima_Click(object sender, RoutedEventArgs e)
        {
            await ExecutarAcaoAssincronaAsync(
                () => _viewModel.AvancarPaginaAuditoriaAsync(),
                "Falha ao avancar a pagina da auditoria.",
                "Auditoria");
        }

        private async Task ExecutarAcaoAssincronaAsync(Func<Task> acao, string mensagemErro, string area)
        {
            try
            {
                await acao();
            }
            catch (Exception ex)
            {
                global::PrimoAutoEletrica.App.Logger.LogError(mensagemErro, ex, area);
                ExibirMensagem($"{mensagemErro}\n\n{ex.Message}", "Erro", MessageBoxImage.Error, ex);
            }
        }

        private static void ExibirMensagem(string mensagem, string titulo, MessageBoxImage imagem, Exception? ex = null)
        {
            if (App.IsAutomatedTestMode)
            {
                var texto = $"{titulo}: {mensagem}";
                if (imagem == MessageBoxImage.Error)
                {
                    global::PrimoAutoEletrica.App.Logger.LogError(texto, ex, "Relatorios");
                }
                else if (imagem == MessageBoxImage.Warning)
                {
                    global::PrimoAutoEletrica.App.Logger.LogWarning(texto, "Relatorios");
                }
                else
                {
                    global::PrimoAutoEletrica.App.Logger.LogInfo(texto, "Relatorios");
                }

                return;
            }

            MessageBox.Show(mensagem, titulo, MessageBoxButton.OK, imagem);
        }
    }
}
