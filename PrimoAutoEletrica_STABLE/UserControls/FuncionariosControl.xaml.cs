using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.UserControls
{
    public partial class FuncionariosControl : UserControl
    {
        private readonly IFuncionarioRepository _funcionarioRepository;
        private readonly Funcionario _funcionarioLogado;
        private readonly PermissionService _permissionService;
        private readonly FuncionarioOperationalService _funcionarioOperationalService;
        private List<Funcionario> _funcionarios = new();

        public FuncionarioPainelOperacional? UltimoPainelOperacional { get; private set; }

        public FuncionariosControl()
        {
            InitializeComponent();

            _funcionarioRepository = global::PrimoAutoEletrica.App.Repositories.Funcionarios;
            _funcionarioLogado = ObterFuncionarioLogado();
            _permissionService = new PermissionService(_funcionarioLogado, App.Logger, App.Database);
            _funcionarioOperationalService = new FuncionarioOperationalService(App.Database);

            Loaded += FuncionariosControl_Loaded;
        }

        private Funcionario ObterFuncionarioLogado()
        {
            if (global::PrimoAutoEletrica.App.Session.CurrentUser is Funcionario funcionario)
            {
                return funcionario;
            }

            return new Funcionario
            {
                Id = 1,
                Nome = "Administrador",
                Email = "admin@primoauto.com",
                PerfilAcesso = "Administrador",
                Ativo = true
            };
        }

        private void FuncionariosControl_Loaded(object sender, RoutedEventArgs e)
        {
            CarregarFuncionarios();
            AtualizarEstatisticas();
            AplicarFiltros();
        }

        private void CarregarFuncionarios()
        {
            try
            {
                var selecionadoId = (FuncionariosDataGrid.SelectedItem as Funcionario)?.Id;
                _funcionarios = _funcionarioRepository.ObterTodos(somenteAtivos: false);
                AtualizarEstatisticas();
                AplicarFiltros();

                if (selecionadoId.HasValue)
                {
                    FuncionariosDataGrid.SelectedItem = ((IEnumerable<Funcionario>)FuncionariosDataGrid.ItemsSource ?? Array.Empty<Funcionario>())
                        .FirstOrDefault(funcionario => funcionario.Id == selecionadoId.Value);
                }

                if (FuncionariosDataGrid.SelectedItem == null)
                {
                    FuncionariosDataGrid.SelectedIndex = FuncionariosDataGrid.Items.Count > 0 ? 0 : -1;
                }

                AtualizarPainelFuncionario(FuncionariosDataGrid.SelectedItem as Funcionario);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao carregar funcionários:\n\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AtualizarEstatisticas()
        {
            TotalFuncionariosTextBlock.Text = _funcionarios.Count.ToString();
            FuncionariosAtivosTextBlock.Text = _funcionarios.Count(funcionario => funcionario.Ativo).ToString();
            FuncionariosInativosTextBlock.Text = _funcionarios.Count(funcionario => !funcionario.Ativo).ToString();
            FuncionariosAdminTextBlock.Text = _funcionarios.Count(funcionario => string.Equals(funcionario.PerfilAcesso, "Administrador", StringComparison.OrdinalIgnoreCase)).ToString();
        }

        private void AplicarFiltros()
        {
            var busca = SearchTextBox?.Text?.Trim() ?? string.Empty;
            var itens = _funcionarios.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                itens = itens.Where(funcionario =>
                    funcionario.Nome.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    funcionario.Email.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    funcionario.Funcao.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    funcionario.PerfilAcesso.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                    funcionario.Status.Contains(busca, StringComparison.OrdinalIgnoreCase));
            }

            var lista = itens.ToList();
            FuncionariosDataGrid.ItemsSource = lista;

            if (lista.Count == 0)
            {
                AtualizarPainelFuncionario(null);
                return;
            }

            if (FuncionariosDataGrid.SelectedItem is not Funcionario selecionado || !lista.Any(item => item.Id == selecionado.Id))
            {
                FuncionariosDataGrid.SelectedIndex = 0;
            }

            AtualizarPainelFuncionario(FuncionariosDataGrid.SelectedItem as Funcionario);
        }

        private void AtualizarPainelFuncionario(Funcionario? funcionario)
        {
            FuncionarioNomeTextBlock.Text = funcionario?.Nome ?? "Selecione um colaborador";
            FuncionarioFuncaoTextBlock.Text = funcionario?.Funcao ?? "Sem funcao";
            FuncionarioPerfilTextBlock.Text = funcionario?.PerfilAcesso ?? "-";
            FuncionarioStatusTextBlock.Text = funcionario?.Status ?? "-";
            FuncionarioUltimoAcessoTextBlock.Text = funcionario?.DataUltimoLogin?.ToString("dd/MM/yyyy HH:mm") ?? "Sem acesso";
            FuncionarioDataCadastroTextBlock.Text = funcionario?.DataCadastro.ToString("dd/MM/yyyy HH:mm") ?? "-";
            FuncionarioEmailTextBlock.Text = string.IsNullOrWhiteSpace(funcionario?.Email) ? "-" : funcionario.Email;
            FuncionarioTelefoneTextBlock.Text = string.IsNullOrWhiteSpace(funcionario?.Telefone) ? "-" : funcionario.Telefone;
            FuncionarioObservacoesTextBlock.Text = string.IsNullOrWhiteSpace(funcionario?.Observacoes)
                ? "Sem observacoes operacionais."
                : funcionario.Observacoes;

            var preview = FuncionarioMediaService.TryCreatePreviewSource(funcionario?.Foto);
            FuncionarioFotoImage.Source = preview;
            FuncionarioFotoImage.Visibility = preview == null ? Visibility.Collapsed : Visibility.Visible;
            FuncionarioFotoPlaceholderText.Visibility = preview == null ? Visibility.Visible : Visibility.Collapsed;

            var podeReativar = funcionario != null && !funcionario.Ativo;
            BloquearFuncionarioButton.Visibility = podeReativar ? Visibility.Collapsed : Visibility.Visible;
            ReativarFuncionarioButton.Visibility = podeReativar ? Visibility.Visible : Visibility.Collapsed;

            var habilitarAcoes = funcionario != null;
            RedefinirSenhaButton.IsEnabled = habilitarAcoes;
            BloquearFuncionarioButton.IsEnabled = habilitarAcoes;
            ReativarFuncionarioButton.IsEnabled = habilitarAcoes;

            AtualizarPainelOperacional(funcionario);
        }

        private void AtualizarPainelOperacional(Funcionario? funcionario)
        {
            try
            {
                UltimoPainelOperacional = _funcionarioOperationalService.ObterPainelOperacional(funcionario);
                FuncionarioProdutividadeResumoTextBlock.Text = UltimoPainelOperacional.ProdutividadeResumo;
                FuncionarioOperacaoResumoTextBlock.Text = UltimoPainelOperacional.ProdutividadeOperacionalResumo;
                FuncionarioCaixaResumoTextBlock.Text = UltimoPainelOperacional.CaixaOperadoResumo;
                FuncionarioLoginResumoTextBlock.Text = UltimoPainelOperacional.LoginResumo;
                FuncionarioAuditoriaResumoTextBlock.Text = UltimoPainelOperacional.AuditoriaResumo;
                FuncionarioPermissoesResumoTextBlock.Text = UltimoPainelOperacional.PermissoesResumo;
                FuncionarioPermissoesAcoesTextBlock.Text = UltimoPainelOperacional.AcoesPermitidasResumo;
                FuncionarioAcoesRecentesTextBlock.Text = UltimoPainelOperacional.AcoesRecentesResumo;
                FuncionarioUltimaAcaoTextBlock.Text = UltimoPainelOperacional.UltimaAcao.HasValue
                    ? $"Ultima acao auditada: {UltimoPainelOperacional.UltimaAcao.Value:dd/MM/yyyy HH:mm}"
                    : "Ultima acao auditada: sem registro no periodo.";
            }
            catch (Exception ex)
            {
                UltimoPainelOperacional = FuncionarioPainelOperacional.Vazio("Falha ao carregar painel operacional.");
                FuncionarioProdutividadeResumoTextBlock.Text = "Falha ao carregar produtividade.";
                FuncionarioOperacaoResumoTextBlock.Text = "-";
                FuncionarioCaixaResumoTextBlock.Text = "-";
                FuncionarioLoginResumoTextBlock.Text = "-";
                FuncionarioAuditoriaResumoTextBlock.Text = ex.Message;
                FuncionarioPermissoesResumoTextBlock.Text = "Revise auditoria/permissoes e tente atualizar.";
                FuncionarioPermissoesAcoesTextBlock.Text = "-";
                FuncionarioAcoesRecentesTextBlock.Text = "-";
                FuncionarioUltimaAcaoTextBlock.Text = "Ultima acao auditada: indisponivel.";
                App.Logger.LogError("Falha ao carregar painel operacional do funcionario.", ex, "Funcionarios");
            }
        }

        private Funcionario? ObterFuncionarioDoContexto(object sender)
        {
            if (sender is FrameworkElement element && element.Tag is Funcionario funcionarioTag)
            {
                return funcionarioTag;
            }

            return FuncionariosDataGrid.SelectedItem as Funcionario;
        }

        private Funcionario? ObterFuncionarioSelecionado()
        {
            return FuncionariosDataGrid.SelectedItem as Funcionario;
        }

        private void AtualizarButton_Click(object sender, RoutedEventArgs e)
        {
            CarregarFuncionarios();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void LimparFiltrosButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            AplicarFiltros();
        }

        private void FuncionariosDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AtualizarPainelFuncionario(FuncionariosDataGrid.SelectedItem as Funcionario);
        }

        private void NovoFuncionarioButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarPermissao("FUNCIONARIOS_CRIAR", "Voce nao possui permissao para criar funcionarios."))
                {
                    return;
                }

                var window = new NovoFuncionarioWindow(_funcionarioLogado);
                if (window.ShowDialog() == true)
                {
                    CarregarFuncionarios();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro:\n\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void GerenciarPerfisButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarPermissao("PERFIS_GERENCIAR", "Voce nao possui permissao para gerenciar perfis de acesso."))
                {
                    return;
                }

                var window = new GerenciarPerfisWindow(_funcionarioLogado);
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro:\n\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ConfigurarPermissoesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarPermissao("PERMISSOES_CONFIGURAR", "Voce nao possui permissao para configurar permissoes."))
                {
                    return;
                }

                var window = new ConfigurarPermissoesWindow(_funcionarioLogado);
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro:\n\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void VisualizarFuncionarioButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var funcionario = ObterFuncionarioDoContexto(sender);
                if (funcionario == null)
                {
                    return;
                }

                FuncionariosDataGrid.SelectedItem = funcionario;

                var mensagem =
                    $"Nome: {funcionario.Nome}\n" +
                    $"Funcao: {funcionario.Funcao}\n" +
                    $"Perfil: {funcionario.PerfilAcesso}\n" +
                    $"Status: {funcionario.Status}\n" +
                    $"Ultimo acesso: {funcionario.DataUltimoLogin?.ToString("dd/MM/yyyy HH:mm") ?? "Sem acesso"}\n" +
                    $"Admissao: {funcionario.DataAdmissao:dd/MM/yyyy}\n" +
                    $"Salario: R$ {funcionario.Salario:F2}\n" +
                    $"Email: {funcionario.Email}\n" +
                    $"Telefone: {funcionario.Telefone}\n" +
                    $"Observacoes: {(string.IsNullOrWhiteSpace(funcionario.Observacoes) ? "Sem observacoes" : funcionario.Observacoes)}";

                MessageBox.Show(mensagem, "Detalhes do funcionario", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao visualizar funcionario:\n\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void EditarFuncionarioButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarPermissao("FUNCIONARIOS_EDITAR", "Voce nao possui permissao para editar funcionarios."))
                {
                    return;
                }

                var funcionario = ObterFuncionarioDoContexto(sender);
                if (funcionario == null)
                {
                    return;
                }

                FuncionariosDataGrid.SelectedItem = funcionario;

                var window = new EditarFuncionarioWindow(_funcionarioLogado, funcionario);
                if (window.ShowDialog() == true)
                {
                    CarregarFuncionarios();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao editar funcionario:\n\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ExcluirFuncionarioButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarPermissao("FUNCIONARIOS_EXCLUIR", "Voce nao possui permissao para excluir funcionarios."))
                {
                    return;
                }

                var funcionario = ObterFuncionarioDoContexto(sender);
                if (funcionario == null)
                {
                    return;
                }

                if (funcionario.Id == _funcionarioLogado.Id)
                {
                    MessageBox.Show(
                        "Nao e possivel inativar o proprio usuario logado.",
                        "Operacao bloqueada",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (CriticalActionDialogService.ConfirmarAcao(
                    Window.GetWindow(this),
                    new CriticalActionRequest
                    {
                        WindowTitle = "Inativar funcionario",
                        Header = "Inativacao de funcionario",
                        Summary = $"Voce esta prestes a inativar o funcionario '{funcionario.Nome}'.",
                        Details = $"Funcao: {funcionario.Funcao}\nE-mail: {funcionario.Email}\nPerfil: {funcionario.PerfilAcesso}",
                        Impact = "O colaborador perdera acesso ao sistema, mas o historico operacional sera preservado.",
                        Keyword = "INATIVAR",
                        ConfirmButtonText = "Inativar funcionario"
                    }))
                {
                    _funcionarioRepository.Excluir(funcionario.Id);
                    CarregarFuncionarios();

                    MessageBox.Show(
                        $"Funcionario inativado com sucesso.\n\nFuncionario: {funcionario.Nome}",
                        "Funcionario inativado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao inativar funcionario:\n\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void RedefinirSenhaButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarPermissao("FUNCIONARIOS_EDITAR", "Voce nao possui permissao para redefinir senhas."))
                {
                    return;
                }

                var funcionario = ObterFuncionarioSelecionado();
                if (funcionario == null)
                {
                    return;
                }

                if (!CriticalActionDialogService.ConfirmarAcao(
                    Window.GetWindow(this),
                    new CriticalActionRequest
                    {
                        WindowTitle = "Redefinir senha",
                        Header = "Redefinicao de acesso",
                        Summary = $"Uma senha temporaria sera gerada para '{funcionario.Nome}'.",
                        Details = $"Email: {funcionario.Email}\nPerfil: {funcionario.PerfilAcesso}",
                        Impact = "A senha atual deixara de funcionar imediatamente. Informe a nova senha temporaria apenas ao colaborador correto.",
                        Keyword = "SENHA",
                        ConfirmButtonText = "Gerar senha temporaria"
                    }))
                {
                    return;
                }

                var senhaTemporaria = GerarSenhaTemporaria(funcionario);
                _funcionarioRepository.ResetarSenha(funcionario.Id, PasswordHasherService.HashPassword(senhaTemporaria));

                MessageBox.Show(
                    $"Senha temporaria gerada para {funcionario.Nome}:\n\n{senhaTemporaria}\n\nO colaborador devera trocar essa senha no proximo login.",
                    "Senha temporaria",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao redefinir senha:\n\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BloquearFuncionarioButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarPermissao("FUNCIONARIOS_EXCLUIR", "Voce nao possui permissao para bloquear usuarios."))
                {
                    return;
                }

                var funcionario = ObterFuncionarioSelecionado();
                if (funcionario == null)
                {
                    return;
                }

                if (funcionario.Id == _funcionarioLogado.Id)
                {
                    MessageBox.Show(
                        "Nao e possivel bloquear o proprio usuario logado.",
                        "Operacao bloqueada",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (!CriticalActionDialogService.ConfirmarAcao(
                    Window.GetWindow(this),
                    new CriticalActionRequest
                    {
                        WindowTitle = "Bloquear usuario",
                        Header = "Bloqueio administrativo",
                        Summary = $"O usuario '{funcionario.Nome}' sera bloqueado.",
                        Details = $"Email: {funcionario.Email}\nPerfil: {funcionario.PerfilAcesso}",
                        Impact = "O colaborador nao conseguira mais autenticar no sistema ate ser reativado por um gestor.",
                        Keyword = "BLOQUEAR",
                        ConfirmButtonText = "Bloquear usuario"
                    }))
                {
                    return;
                }

                _funcionarioRepository.AtualizarStatusAcesso(funcionario.Id, ativo: false, status: "Bloqueado");
                CarregarFuncionarios();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao bloquear usuario:\n\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ReativarFuncionarioButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarPermissao("FUNCIONARIOS_EDITAR", "Voce nao possui permissao para reativar usuarios."))
                {
                    return;
                }

                var funcionario = ObterFuncionarioSelecionado();
                if (funcionario == null)
                {
                    return;
                }

                _funcionarioRepository.AtualizarStatusAcesso(funcionario.Id, ativo: true, status: "Ativo");
                CarregarFuncionarios();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao reativar usuario:\n\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private bool ValidarPermissao(string codigoPermissao, string mensagem)
        {
            if (_permissionService.TemPermissaoCodigo(codigoPermissao))
            {
                return true;
            }

            MessageBox.Show(
                mensagem,
                "Acesso Negado",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return false;
        }

        private static string GerarSenhaTemporaria(Funcionario funcionario)
        {
            var sufixo = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
            return $"Primo{funcionario.Id}{sufixo}!";
        }
    }
}
