using Microsoft.Extensions.DependencyInjection;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views;
using PrimoAutoEletrica.Views.Clientes;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PrimoAutoEletrica.UserControls
{
    public partial class OrcamentosControl : UserControl
    {
        private readonly OrcamentosViewModel _viewModel;
        private readonly OrcamentoPdfService _pdfService;
        private readonly PermissionService _permissionService;

        public OrcamentosControl()
        {
            InitializeComponent();
            _viewModel = App.Services.GetRequiredService<OrcamentosViewModel>();
            _pdfService = new OrcamentoPdfService();
            _permissionService = PermissionService.CriarParaSessaoAtual(App.Logger, App.Database);
            DataContext = _viewModel;
            Focusable = true;
            PreviewKeyDown += OrcamentosControl_PreviewKeyDown;
        }

        private void OrcamentosControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
            {
                NovoOrcamento_Click(this, new RoutedEventArgs());
                e.Handled = true;
                return;
            }

            if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                CarteiraCompleta_Click(this, new RoutedEventArgs());
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter
                && Keyboard.Modifiers == ModifierKeys.None
                && _viewModel.OrcamentoAtual is { } orcamento)
            {
                var janela = new NovoOrcamentoWindow(orcamento);
                WindowOwnerHelper.ConfigureOwner(janela, this);
                if (janela.ShowDialog() == true)
                {
                    _viewModel.CarregarOrcamentos();
                    _viewModel.AtualizarDashboard();
                }
                e.Handled = true;
            }
        }

        private void NovoOrcamento_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("ORCAMENTOS_CRIAR", "Voce nao possui permissao para criar orcamentos."))
            {
                return;
            }

            var janela = new NovoOrcamentoWindow();
            if (janela.ShowDialog() == true)
            {
                _viewModel.CarregarOrcamentos();
                _viewModel.AtualizarDashboard();
            }
        }

        private void Duplicar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("ORCAMENTOS_DUPLICAR", "Voce nao possui permissao para duplicar orcamentos."))
            {
                return;
            }

            var orcamento = ObterOrcamentoSelecionadoOuAvisar();
            if (orcamento == null)
            {
                return;
            }

            _viewModel.DuplicarOrcamento(orcamento);
            MessageBox.Show("Orcamento duplicado com sucesso!", "Orcamentos", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportarPDF_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("ORCAMENTOS_EXPORTAR", "Voce nao possui permissao para exportar orcamentos."))
            {
                return;
            }

            var orcamento = ObterOrcamentoSelecionadoOuAvisar();
            if (orcamento != null)
            {
                var caminhoArquivo = _pdfService.SalvarPdfDialog(orcamento);
                if (!string.IsNullOrEmpty(caminhoArquivo))
                {
                    App.Audit.RegistrarAcaoCritica(
                        "Orcamentos",
                        "OrcamentoExportadoPdf",
                        "Orcamento",
                        orcamento.Id.ToString(),
                        $"Numero={orcamento.Numero}; Arquivo={caminhoArquivo}");

                    if (App.IsAutomatedTestMode)
                    {
                        App.Logger.LogInfo($"Exportacao PDF do orcamento '{orcamento.Numero}' validada em automacao: {caminhoArquivo}");
                        return;
                    }

                    MessageBox.Show($"PDF gerado com sucesso: {caminhoArquivo}", "Orcamentos", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void Imprimir_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("ORCAMENTOS_IMPRIMIR", "Voce nao possui permissao para imprimir orcamentos."))
            {
                return;
            }

            var orcamento = ObterOrcamentoSelecionadoOuAvisar();
            if (orcamento != null)
            {
                var tempPdf = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Orcamento_{orcamento.Numero}.pdf");
                _pdfService.GerarPdfOrcamento(orcamento, tempPdf);

                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = tempPdf,
                        UseShellExecute = true
                    });
                    App.Audit.RegistrarAcaoCritica(
                        "Orcamentos",
                        "OrcamentoImpressaoSolicitada",
                        "Orcamento",
                        orcamento.Id.ToString(),
                        $"Numero={orcamento.Numero}");
                    MessageBox.Show("Orcamento enviado para impressao.", "Orcamentos", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao imprimir: {ex.Message}", "Orcamentos", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void WhatsApp_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("ORCAMENTOS_COMPARTILHAR", "Voce nao possui permissao para compartilhar orcamentos."))
            {
                return;
            }

            var orcamento = ObterOrcamentoSelecionadoOuAvisar();
            if (orcamento != null)
            {
                if (!TryBuildWhatsAppShareUrl(orcamento, out var url, out var mensagemErro))
                {
                    MessageBox.Show(mensagemErro, "Orcamentos", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (App.IsAutomatedTestMode)
                {
                    App.Logger.LogInfo($"Compartilhamento WhatsApp do orcamento '{orcamento.Numero}' validado em automacao.");
                    return;
                }

                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao abrir WhatsApp: {ex.Message}", "Orcamentos", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        internal static bool TryBuildWhatsAppShareUrl(Orcamento? orcamento, out string url, out string mensagemErro)
        {
            url = string.Empty;
            mensagemErro = string.Empty;

            if (orcamento?.Cliente == null)
            {
                mensagemErro = "Cliente nao localizado para compartilhamento.";
                return false;
            }

            var cliente = orcamento.Cliente;
            if (!cliente.ConsentimentoLGPD || !cliente.AutorizaContatoWhatsApp)
            {
                mensagemErro = "Cliente sem consentimento LGPD/autorizacao para contato por WhatsApp.";
                return false;
            }

            if (!CadastroValidationHelper.TryObterTelefoneWhatsApp(cliente.WhatsApp, out var telefone) &&
                !CadastroValidationHelper.TryObterTelefoneWhatsApp(cliente.Telefone, out telefone))
            {
                mensagemErro = "Cliente nao possui WhatsApp ou telefone valido cadastrado.";
                return false;
            }

            url = $"https://wa.me/{telefone}?text={Uri.EscapeDataString(BuildWhatsAppShareMessage(orcamento, cliente))}";
            return true;
        }

        internal static string BuildWhatsAppShareMessage(Orcamento orcamento, Cliente cliente)
        {
            var validade = orcamento.DataValidade?.ToString("dd/MM/yyyy") ?? "sem validade definida";
            var veiculo = orcamento.Veiculo?.Placa;
            var textoVeiculo = string.IsNullOrWhiteSpace(veiculo) ? string.Empty : $" para o veiculo {veiculo}";
            return $"Ola {cliente.Nome}! Segue seu orcamento {orcamento.Numero}{textoVeiculo} no valor de {orcamento.Total:C}. Validade: {validade}. Aguardamos seu retorno!";
        }

        private void Email_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("ORCAMENTOS_COMPARTILHAR", "Voce nao possui permissao para compartilhar orcamentos por e-mail."))
            {
                return;
            }

            var orcamento = ObterOrcamentoSelecionadoOuAvisar();
            if (orcamento != null)
            {
                if (orcamento.Cliente == null)
                {
                    MessageBox.Show("Cliente nao localizado para envio de e-mail.", "Orcamentos", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var erroEmail = CadastroValidationHelper.ValidarEmail(orcamento.Cliente.Email, obrigatorio: true);
                if (!string.IsNullOrWhiteSpace(erroEmail))
                {
                    MessageBox.Show(erroEmail, "Orcamentos", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var assunto = $"Orcamento {orcamento.Numero} - Primo Auto Eletrica";
                var corpo = $"Ola {orcamento.Cliente.Nome}!\n\nSegue seu orcamento {orcamento.Numero} no valor de {orcamento.Total:C}.\n\nData de Criacao: {orcamento.DataCriacao:dd/MM/yyyy}\nValidade: {orcamento.DataValidade:dd/MM/yyyy}\n\nAguardamos seu retorno!";
                var url = $"mailto:{orcamento.Cliente.Email}?subject={Uri.EscapeDataString(assunto)}&body={Uri.EscapeDataString(corpo)}";

                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao abrir e-mail: {ex.Message}", "Orcamentos", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Aprovar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("ORCAMENTOS_EDITAR", "Voce nao possui permissao para aprovar orcamentos."))
            {
                return;
            }

            var orcamento = ObterOrcamentoSelecionadoOuAvisar();
            if (orcamento == null)
            {
                return;
            }
            if (string.Equals(orcamento.Status, "Aprovado", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(orcamento.Status, "Convertido em Venda", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(orcamento.Status, "Convertido em OS", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Este orcamento ja esta aprovado ou convertido.", "Orcamentos", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirmado = CriticalActionDialogService.ConfirmarAcao(
                Window.GetWindow(this),
                new CriticalActionRequest
                {
                    WindowTitle = "Aprovar orcamento",
                    Header = "Aprovacao comercial",
                    Summary = $"Voce esta prestes a aprovar o orcamento {orcamento.Numero}.",
                    Details = $"Cliente: {orcamento.Cliente?.Nome ?? "Nao informado"}\nItens: {orcamento.Itens.Count}\nTotal: {orcamento.Total:C}",
                    Impact = "O orcamento ficara apto para conversoes operacionais e financeiras nas proximas etapas do atendimento.",
                    Keyword = "APROVAR",
                    ConfirmButtonText = "Aprovar orcamento"
                });

            if (!confirmado)
            {
                return;
            }

            var statusAnterior = orcamento.Status;
            _viewModel.AprovarOrcamento(orcamento);
            App.Audit.Registrar(
                categoria: "Orcamentos",
                acao: "AprovarOrcamento",
                entidade: "Orcamento",
                entidadeId: orcamento.Id.ToString(),
                detalhes: $"Numero={orcamento.Numero}; Cliente={orcamento.Cliente?.Nome ?? "Nao informado"}; Total={orcamento.Total:C}",
                valorAnterior: $"Status={statusAnterior}",
                valorNovo: "Status=Aprovado");
            MessageBox.Show("Orcamento aprovado com sucesso!", "Orcamentos", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ConverterVenda_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("ORCAMENTOS_CONVERTER_VENDA", "Voce nao possui permissao para converter orcamentos em venda."))
            {
                return;
            }

            var orcamento = ObterOrcamentoSelecionadoOuAvisar();
            if (orcamento != null)
            {
                if (orcamento.Status == "Convertido em Venda")
                {
                    MessageBox.Show("Este orcamento ja foi convertido em venda.", "Orcamentos", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var confirmado = CriticalActionDialogService.ConfirmarAcao(
                    Window.GetWindow(this),
                    new CriticalActionRequest
                    {
                        WindowTitle = "Converter orcamento em venda",
                        Header = "Conversao comercial em venda",
                        Summary = $"Voce esta prestes a converter o orcamento {orcamento.Numero} em venda real.",
                        Details = $"Cliente: {orcamento.Cliente?.Nome ?? "Nao informado"}\nItens: {orcamento.Itens.Count}\nTotal: {orcamento.Total:C}\nCondicao de pagamento: {orcamento.CondicoesPagamento ?? "Nao informada"}",
                        Impact = "O sistema vai registrar uma venda real, movimentar estoque e integrar o valor ao financeiro.",
                        Keyword = "CONVERTER",
                        ConfirmButtonText = "Converter em venda"
                    });

                if (!confirmado)
                {
                    return;
                }

                try
                {
                    var statusAnterior = orcamento.Status;
                    _viewModel.ConverterEmVenda(orcamento);
                    App.Audit.Registrar(
                        categoria: "Orcamentos",
                        acao: "ConverterEmVenda",
                        entidade: "Orcamento",
                        entidadeId: orcamento.Id.ToString(),
                        detalhes: $"Numero={orcamento.Numero}; Cliente={orcamento.Cliente?.Nome ?? "Nao informado"}; Itens={orcamento.Itens.Count}; Total={orcamento.Total:C}",
                        valorAnterior: $"Status={statusAnterior}",
                        valorNovo: "Status=Convertido em Venda");
                    PublicarSugestaoFinanceiro(orcamento);
                    MessageBox.Show("Orcamento convertido em venda com sucesso!", "Orcamentos", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    App.Logger.LogError($"Falha ao converter o orcamento '{orcamento.Numero}' em venda.", ex);
                    App.Audit.RegistrarErro("Orcamentos", "FalhaConverterEmVenda", ex, "Orcamento", orcamento.Id.ToString());
                    MessageBox.Show($"Erro ao converter orcamento em venda: {ex.Message}", "Orcamentos", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ConverterOs_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("ORCAMENTOS_EDITAR", "Voce nao possui permissao para converter orcamentos em OS.") ||
                !ValidarPermissao("ORDENS_SERVICO_EDITAR", "Voce nao possui permissao para criar ordens de servico a partir de orcamentos."))
            {
                return;
            }

            var orcamento = ObterOrcamentoSelecionadoOuAvisar();
            if (orcamento == null)
            {
                return;
            }
            if (orcamento.OrdemServicoId.HasValue)
            {
                MessageBox.Show("Este orcamento ja possui uma ordem de servico vinculada.", "Orcamentos", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirmado = CriticalActionDialogService.ConfirmarAcao(
                Window.GetWindow(this),
                new CriticalActionRequest
                {
                    WindowTitle = "Converter orcamento em OS",
                    Header = "Conversao comercial em ordem de servico",
                    Summary = $"Voce esta prestes a converter o orcamento {orcamento.Numero} em uma ordem de servico.",
                    Details = $"Cliente: {orcamento.Cliente?.Nome ?? "Nao informado"}\nItens: {orcamento.Itens.Count}\nTotal: {orcamento.Total:C}",
                    Impact = "O sistema vai criar uma OS operacional vinculada ao orcamento para seguir com aprovacao, execucao e entrega.",
                    Keyword = "CONVERTER",
                    ConfirmButtonText = "Converter em OS"
                });

            if (!confirmado)
            {
                return;
            }

            var statusAnterior = orcamento.Status;
            var ordem = _viewModel.ConverterEmOrdemServico(orcamento);
            App.Audit.Registrar(
                categoria: "Orcamentos",
                acao: "ConverterEmOS",
                entidade: "Orcamento",
                entidadeId: orcamento.Id.ToString(),
                detalhes: $"Numero={orcamento.Numero}; OrdemServico={ordem.Numero}; Cliente={orcamento.Cliente?.Nome ?? "Nao informado"}; Total={orcamento.Total:C}",
                valorAnterior: $"Status={statusAnterior}",
                valorNovo: $"Status=Convertido em OS; OrdemServicoId={ordem.Id}");
            PublicarSugestaoOrdensServico(orcamento, ordem);
            MessageBox.Show("Orcamento convertido em OS com sucesso!", "Orcamentos", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool ValidarPermissao(string codigoPermissao, string mensagem)
        {
            if (_permissionService.TemPermissaoCodigo(codigoPermissao))
            {
                return true;
            }

            MessageBox.Show(mensagem, "Acesso negado", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        private void SalvarRascunho_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("ORCAMENTOS_EDITAR", "Voce nao possui permissao para editar orcamentos e salvar rascunhos."))
            {
                return;
            }

            var orcamento = ObterOrcamentoSelecionadoOuAvisar();
            if (orcamento != null)
            {
                orcamento.Status = "Rascunho";
                _viewModel.SelecionarOrcamento(orcamento);
                _viewModel.SalvarOrcamento();
                MessageBox.Show("Rascunho salvo com sucesso!", "Orcamentos", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Excluir_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("ORCAMENTOS_EDITAR", "Voce nao possui permissao para excluir orcamentos."))
            {
                return;
            }

            var orcamento = ObterOrcamentoSelecionadoOuAvisar();
            if (orcamento == null)
            {
                return;
            }

            if (orcamento.OrdemServicoId.HasValue ||
                string.Equals(orcamento.Status, "Convertido em OS", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(orcamento.Status, "Convertido em Venda", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "Orcamentos ja convertidos em venda ou OS nao podem ser excluidos para preservar o historico operacional.",
                    "Orcamentos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var confirmado = CriticalActionDialogService.ConfirmarAcao(
                Window.GetWindow(this),
                new CriticalActionRequest
                {
                    WindowTitle = "Excluir orcamento",
                    Header = "Remocao definitiva",
                    Summary = $"Voce esta prestes a excluir o orcamento {orcamento.Numero}.",
                    Details = $"Cliente: {orcamento.Cliente?.Nome ?? "Nao informado"}\nItens: {orcamento.Itens.Count}\nStatus: {orcamento.Status}\nTotal: {orcamento.Total:C}",
                    Impact = "O registro comercial sera removido da carteira e deixara de aparecer em consultas, exportacoes e impressao.",
                    Keyword = "EXCLUIR",
                    ConfirmButtonText = "Excluir orcamento"
                });

            if (!confirmado)
            {
                return;
            }

            _viewModel.ExcluirOrcamento(orcamento.Id);
            App.Audit.Registrar(
                categoria: "Orcamentos",
                acao: "ExcluirOrcamento",
                entidade: "Orcamento",
                entidadeId: orcamento.Id.ToString(),
                detalhes: $"Numero={orcamento.Numero}; Cliente={orcamento.Cliente?.Nome ?? "Nao informado"}; Total={orcamento.Total:C}",
                valorAnterior: $"Status={orcamento.Status}",
                valorNovo: "Excluido");
            MessageBox.Show("Orcamento excluido com sucesso!", "Orcamentos", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private Orcamento? ObterOrcamentoSelecionadoOuAvisar()
        {
            var orcamento = _viewModel.OrcamentoAtual;
            if (orcamento != null)
            {
                return orcamento;
            }

            MessageBox.Show(
                "Selecione um orcamento na carteira para executar esta acao.",
                "Orcamentos",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return null;
        }

        private void CarteiraCompleta_Click(object sender, RoutedEventArgs e)
        {
            var janela = new SelecionarOrcamentoWindow(_viewModel.Orcamentos.ToList());
            WindowOwnerHelper.ConfigureOwner(janela, this);

            if (janela.ShowDialog() == true && janela.OrcamentoSelecionado != null)
            {
                _viewModel.SelecionarOrcamento(janela.OrcamentoSelecionado);
            }
        }

        private void CadastroRapidoCliente_Click(object sender, RoutedEventArgs e)
        {
            var clientesAntes = App.Repositories.Clientes.ObterTodos().Select(cliente => cliente.Id).ToHashSet();
            var janela = new NovoClienteWindow();
            WindowOwnerHelper.ConfigureOwner(janela, this);

            if (janela.ShowDialog() != true)
            {
                return;
            }

            var clienteCriado = App.Repositories.Clientes
                .ObterTodos()
                .Where(cliente => !clientesAntes.Contains(cliente.Id))
                .OrderByDescending(cliente => cliente.DataCadastro)
                .FirstOrDefault();

            if (clienteCriado == null)
            {
                return;
            }

            _viewModel.DefinirClienteAtual(clienteCriado);
            MessageBox.Show(
                $"Cliente {clienteCriado.Nome} vinculado ao orcamento atual.",
                "Orcamentos",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BuscarCliente_Click(object sender, RoutedEventArgs e)
        {
            AbrirHistoricoCliente(_viewModel.ClienteAtual, "Cadastre ou vincule um cliente ao orcamento para continuar.");
        }

        private void HistoricoCompleto_Click(object sender, RoutedEventArgs e)
        {
            AbrirHistoricoCliente(_viewModel.ClienteAtual, "Nenhum cliente vinculado ao orcamento atual.");
        }

        private void AbrirHistoricoCliente(Cliente? cliente, string mensagemAusencia)
        {
            if (cliente == null)
            {
                MessageBox.Show(
                    mensagemAusencia,
                    "Orcamentos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var historicoWindow = new HistoricoClienteWindow(cliente);
            WindowOwnerHelper.ConfigureOwner(historicoWindow, this);
            historicoWindow.ShowDialog();
        }

        public void EmitirSugestaoFinanceiroForAutomation()
        {
            var orcamento = _viewModel.Orcamentos.FirstOrDefault() ?? new Models.Orcamento
            {
                Numero = "ORC-AUTO-001",
                Total = 230m
            };
            PublicarSugestaoFinanceiro(orcamento);
        }

        private static void PublicarSugestaoFinanceiro(Models.Orcamento orcamento)
        {
            ShellNotificationService.PublishNavigationHint(
                title: "Venda integrada ao financeiro",
                message: $"O orcamento {orcamento.Numero} foi convertido em venda e ja pode ser acompanhado no Financeiro.",
                actionModule: "Financeiro",
                actionLabel: "Abrir Financeiro",
                details: $"Total integrado: {orcamento.Total:C}",
                type: ShellNotificationType.Success,
                source: "Orcamentos");
        }

        private static void PublicarSugestaoOrdensServico(Models.Orcamento orcamento, Models.OrdemServico ordem)
        {
            ShellNotificationService.PublishNavigationHint(
                title: "OS criada a partir do orcamento",
                message: $"O orcamento {orcamento.Numero} gerou a OS {ordem.Numero}.",
                actionModule: "OrdensServico",
                actionLabel: "Abrir Ordens de Servico",
                details: $"Cliente: {orcamento.Cliente?.Nome ?? "Nao informado"}",
                type: ShellNotificationType.Success,
                source: "Orcamentos");
        }
    }
}
