using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using PdfSharpCore.Pdf.IO;
using PrimoAutoEletrica.Data.Repositories;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.UserControls;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views;
using PrimoAutoEletrica.Views.Clientes;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        // Checks de funcionarios, permissoes e auditoria.

        private void RunFuncionariosPermissoesAuditoriaChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "Funcionarios:CadastroEdicaoPelaTela", () =>
            {
                GarantirBancoIsoladoDoSmoke("cadastro e edicao de funcionario pela tela");
                var repository = App.Repositories.Funcionarios;
                var token = DateTime.Now.ToString("HHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
                var cpfCadastro = GerarCpfValido(token);
                var cpfEdicao = GerarCpfValido($"{token}7");
                var emailCadastro = $"funcionario.cadastro.{token}@primoauto.local";
                var emailEdicao = $"funcionario.edicao.{token}@primoauto.local";
                const string senhaInicial = "Workflow@123";
                const string senhaEditada = "Workflow@456";

                var novoWindow = new NovoFuncionarioWindow(syntheticUser);
                AutomatedDialogSupervisor? novoSupervisor = null;

                try
                {
                    ShowWindowForInteraction(novoWindow);
                    novoSupervisor = new AutomatedDialogSupervisor(novoWindow, _fixture);
                    novoSupervisor.Start();

                    SetTextBoxValue(novoWindow, "NomeTextBox", $"Funcionario Cadastro {token}");
                    SetTextBoxValue(novoWindow, "EmailTextBox", emailCadastro);
                    SetTextBoxValue(novoWindow, "TelefoneTextBox", "(11) 97777-1000");
                    SetTextBoxValue(novoWindow, "CPFTextBox", cpfCadastro);
                    SetTextBoxValue(novoWindow, "FuncaoTextBox", "Atendente de balcao");
                    SetTextBoxValue(novoWindow, "SalarioTextBox", "3500,50");
                    SetTextBoxValue(novoWindow, "ObservacoesTextBox", "Observacao operacional inicial do smoke.");
                    DefinirComboBoxTexto(novoWindow, "StatusComboBox", "Ativo");
                    DefinirComboBoxTexto(novoWindow, "PerfilComboBox", "Vendedor");
                    SetPasswordBoxValue(novoWindow, "SenhaPasswordBox", senhaInicial);
                    SetPasswordBoxValue(novoWindow, "ConfirmarSenhaPasswordBox", senhaInicial);
                    var admissao = FindElementByName<DatePicker>(novoWindow, "DataAdmissaoDatePicker")
                        ?? throw new InvalidOperationException("DataAdmissaoDatePicker nao foi localizado no cadastro de funcionario.");
                    admissao.SelectedDate = DateTime.Today.AddDays(-30);
                    WaitForUiIdle();

                    ClickButton(novoWindow, "SalvarButton");
                    WaitForCondition(
                        () => !novoWindow.IsVisible,
                        TimeSpan.FromSeconds(5),
                        "A janela de novo funcionario nao fechou apos salvar.");
                }
                finally
                {
                    novoSupervisor?.Dispose();
                    if (novoWindow.IsVisible)
                    {
                        novoWindow.Close();
                    }
                }

                var criado = repository.ObterTodos(somenteAtivos: false)
                    .FirstOrDefault(item => string.Equals(item.Email, emailCadastro, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException("Funcionario cadastrado pela tela nao foi localizado.");
                var cpfCadastroPersistido = new string((criado.CPF ?? string.Empty).Where(char.IsDigit).ToArray());
                if (!string.Equals(cpfCadastroPersistido, cpfCadastro, StringComparison.Ordinal) ||
                    !string.Equals(criado.PerfilAcesso, "Vendedor", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(criado.Status, "Ativo", StringComparison.OrdinalIgnoreCase) ||
                    !criado.Observacoes.Contains("inicial", StringComparison.OrdinalIgnoreCase) ||
                    !criado.Ativo)
                {
                    throw new InvalidOperationException("Funcionario cadastrado pela tela ficou inconsistente apos persistencia.");
                }

                var loginInicial = App.Database.AutenticarFuncionarioDetalhado(emailCadastro, senhaInicial);
                if (!loginInicial.IsSuccess || loginInicial.Funcionario == null)
                {
                    throw new InvalidOperationException("Funcionario cadastrado pela tela nao conseguiu autenticar com a senha inicial.");
                }

                var editarWindow = new EditarFuncionarioWindow(syntheticUser, criado);
                AutomatedDialogSupervisor? editarSupervisor = null;

                try
                {
                    ShowWindowForInteraction(editarWindow);
                    editarSupervisor = new AutomatedDialogSupervisor(editarWindow, _fixture);
                    editarSupervisor.Start();

                    SetTextBoxValue(editarWindow, "NomeTextBox", $"Funcionario Editado {token}");
                    SetTextBoxValue(editarWindow, "EmailTextBox", emailEdicao);
                    SetTextBoxValue(editarWindow, "TelefoneTextBox", "(11) 97777-2000");
                    SetTextBoxValue(editarWindow, "CPFTextBox", cpfEdicao);
                    SetTextBoxValue(editarWindow, "FuncaoTextBox", "Consultor tecnico");
                    SetTextBoxValue(editarWindow, "SalarioTextBox", "4100,75");
                    SetTextBoxValue(editarWindow, "ObservacoesTextBox", "Observacao operacional editada pelo smoke.");
                    DefinirComboBoxTexto(editarWindow, "StatusComboBox", "Em treinamento");
                    DefinirComboBoxTexto(editarWindow, "PerfilComboBox", "Administrador");
                    SetPasswordBoxValue(editarWindow, "SenhaPasswordBox", senhaEditada);
                    SetPasswordBoxValue(editarWindow, "ConfirmarSenhaPasswordBox", senhaEditada);
                    var dataAdmissao = FindElementByName<DatePicker>(editarWindow, "DataAdmissaoDatePicker")
                        ?? throw new InvalidOperationException("DataAdmissaoDatePicker nao foi localizado na edicao de funcionario.");
                    dataAdmissao.SelectedDate = DateTime.Today.AddDays(-20);
                    WaitForUiIdle();

                    ClickButton(editarWindow, "Salvar alteracoes");
                    WaitForCondition(
                        () => !editarWindow.IsVisible,
                        TimeSpan.FromSeconds(5),
                        "A janela de edicao de funcionario nao fechou apos salvar.");
                }
                finally
                {
                    editarSupervisor?.Dispose();
                    if (editarWindow.IsVisible)
                    {
                        editarWindow.Close();
                    }
                }

                var editado = repository.ObterPorId(criado.Id)
                    ?? throw new InvalidOperationException("Funcionario editado pela tela nao foi localizado.");
                var cpfEdicaoPersistido = new string((editado.CPF ?? string.Empty).Where(char.IsDigit).ToArray());
                if (!string.Equals(editado.Email, emailEdicao, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(cpfEdicaoPersistido, cpfEdicao, StringComparison.Ordinal) ||
                    !string.Equals(editado.PerfilAcesso, "Administrador", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(editado.Status, "Em treinamento", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(editado.Funcao, "Consultor tecnico", StringComparison.OrdinalIgnoreCase) ||
                    !editado.Observacoes.Contains("editada", StringComparison.OrdinalIgnoreCase) ||
                    editado.Salario != 4100.75m ||
                    !editado.Ativo)
                {
                    throw new InvalidOperationException("Funcionario editado pela tela ficou inconsistente apos persistencia.");
                }

                var loginAntigo = App.Database.AutenticarFuncionarioDetalhado(emailCadastro, senhaInicial);
                if (loginAntigo.IsSuccess)
                {
                    throw new InvalidOperationException("Login antigo do funcionario continuou valido apos edicao de email/senha.");
                }

                var loginEditado = App.Database.AutenticarFuncionarioDetalhado(emailEdicao, senhaEditada);
                if (!loginEditado.IsSuccess || loginEditado.Funcionario == null)
                {
                    throw new InvalidOperationException("Funcionario editado pela tela nao autenticou com email/senha atualizados.");
                }
            });

            RunCheck(result, "Funcionarios:AuditoriaProdutividadePermissoes", () =>
            {
                var funcionario = App.Repositories.Funcionarios.ObterPorId(syntheticUser.Id)
                    ?? throw new InvalidOperationException("Funcionario sintetico nao encontrado para validacao operacional.");

                App.Audit.Registrar(
                    categoria: "Funcionarios",
                    acao: "SmokeProdutividadeFuncionario",
                    entidade: "Funcionario",
                    entidadeId: funcionario.Id.ToString(),
                    detalhes: "Evento sintetico para validar painel operacional de funcionarios.");
                App.Audit.Registrar(
                    categoria: "Seguranca",
                    acao: "SmokePermissaoAcaoDetalhada",
                    entidade: "Funcionario",
                    entidadeId: funcionario.Id.ToString(),
                    detalhes: "Evento sintetico para validar auditoria e permissoes por acao.");
                App.Audit.RegistrarLogin("Login", funcionario.Email, sucesso: true, "Login sintetico para validar historico do colaborador.");
                InserirProdutividadeFuncionarioSmoke(funcionario);

                var service = new FuncionarioOperationalService(App.Database);
                var painel = service.ObterPainelOperacional(funcionario);
                ValidarPainelOperacionalFuncionario(painel);

                var hostWindow = CreateHostWindow(new FuncionariosControl(), nameof(FuncionariosControl));
                try
                {
                    ShowWindowForInteraction(hostWindow);
                    if (hostWindow.Content is not FuncionariosControl control)
                    {
                        throw new InvalidOperationException("Host de FuncionariosControl nao conseguiu carregar o controle.");
                    }

                    PrepareInteractiveSurface(control, typeof(FuncionariosControl));
                    var dataGrid = FindElementByName<DataGrid>(control, "FuncionariosDataGrid")
                        ?? throw new InvalidOperationException("Grade de funcionarios nao localizada.");
                    var funcionarioGrid = dataGrid.ItemsSource
                        ?.Cast<Funcionario>()
                        .FirstOrDefault(item => item.Id == funcionario.Id)
                        ?? dataGrid.ItemsSource?.Cast<Funcionario>().FirstOrDefault()
                        ?? throw new InvalidOperationException("Nenhum funcionario carregado na tela.");

                    dataGrid.SelectedItem = funcionarioGrid;
                    control.UpdateLayout();
                    PumpDispatcher();

                    if (control.UltimoPainelOperacional == null)
                    {
                        throw new InvalidOperationException("Painel operacional de funcionarios nao foi carregado na tela.");
                    }

                    ValidarPainelOperacionalFuncionario(control.UltimoPainelOperacional);

                    var produtividadeText = FindElementByName<TextBlock>(control, "FuncionarioProdutividadeResumoTextBlock")?.Text;
                    var operacaoText = FindElementByName<TextBlock>(control, "FuncionarioOperacaoResumoTextBlock")?.Text;
                    var caixaText = FindElementByName<TextBlock>(control, "FuncionarioCaixaResumoTextBlock")?.Text;
                    var loginText = FindElementByName<TextBlock>(control, "FuncionarioLoginResumoTextBlock")?.Text;
                    var permissoesText = FindElementByName<TextBlock>(control, "FuncionarioPermissoesAcoesTextBlock")?.Text;
                    if (string.IsNullOrWhiteSpace(produtividadeText) ||
                        string.IsNullOrWhiteSpace(operacaoText) ||
                        !operacaoText.Contains("OS executada", StringComparison.OrdinalIgnoreCase) ||
                        string.IsNullOrWhiteSpace(caixaText) ||
                        !caixaText.Contains("sessao", StringComparison.OrdinalIgnoreCase) ||
                        string.IsNullOrWhiteSpace(loginText) ||
                        !loginText.Contains("Login:", StringComparison.OrdinalIgnoreCase) ||
                        string.IsNullOrWhiteSpace(permissoesText) ||
                        !permissoesText.Contains("Visualizar", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Painel visual de funcionarios nao exibiu produtividade/permissoes por acao.");
                    }
                }
                finally
                {
                    if (hostWindow.IsVisible)
                    {
                        hostWindow.Close();
                    }
                }
            });

            RunCheck(result, "Funcionarios:BloquearReativarLogin", () =>
            {
                var repository = App.Repositories.Funcionarios;
                var funcionario = EnsureFuncionario(
                    "smoke-bloqueio-reativacao@primoauto.com",
                    "Smoke Bloqueio Reativacao",
                    "Vendedor");
                var hostWindow = CreateHostWindow(new FuncionariosControl(), nameof(FuncionariosControl));

                try
                {
                    repository.AtualizarStatusAcesso(funcionario.Id, ativo: true, status: "Ativo");
                    ResetLoginAttempts(funcionario.Id);

                    ShowWindowForInteraction(hostWindow);
                    if (hostWindow.Content is not FuncionariosControl control)
                    {
                        throw new InvalidOperationException("Host de FuncionariosControl nao conseguiu carregar o controle para bloquear/reativar.");
                    }

                    var dataGrid = FindElementByName<DataGrid>(control, "FuncionariosDataGrid")
                        ?? throw new InvalidOperationException("Grade de funcionarios nao localizada para bloquear/reativar.");
                    var bloquearButton = FindElementByName<Button>(control, "BloquearFuncionarioButton")
                        ?? throw new InvalidOperationException("Botao Bloquear nao foi localizado.");
                    var reativarButton = FindElementByName<Button>(control, "ReativarFuncionarioButton")
                        ?? throw new InvalidOperationException("Botao Reativar nao foi localizado.");

                    WaitForCondition(
                        () => dataGrid.ItemsSource?.Cast<Funcionario>().Any(item => item.Id == funcionario.Id) == true,
                        TimeSpan.FromSeconds(5),
                        "Funcionario sintetico nao apareceu na grade para bloquear/reativar.");

                    SelecionarFuncionarioNaGrade(dataGrid, funcionario.Id);
                    if (!bloquearButton.IsEnabled || bloquearButton.Visibility != Visibility.Visible)
                    {
                        throw new InvalidOperationException("Botao Bloquear nao ficou disponivel para o funcionario ativo selecionado.");
                    }

                    ClickButton(control, "BloquearFuncionarioButton");
                    WaitForCondition(
                        () =>
                        {
                            var bloqueado = repository.ObterPorId(funcionario.Id);
                            return bloqueado is { Ativo: false } &&
                                   string.Equals(bloqueado.Status, "Bloqueado", StringComparison.OrdinalIgnoreCase);
                        },
                        TimeSpan.FromSeconds(5),
                        "Clique real em Bloquear nao atualizou o acesso do funcionario.");

                    var loginBloqueado = App.Database.AutenticarFuncionarioDetalhado(funcionario.Email, "Workflow@123");
                    if (loginBloqueado.IsSuccess || loginBloqueado.Funcionario != null)
                    {
                        throw new InvalidOperationException("Funcionario bloqueado ainda conseguiu autenticar.");
                    }

                    SelecionarFuncionarioNaGrade(dataGrid, funcionario.Id);
                    WaitForCondition(
                        () => reativarButton.IsEnabled && reativarButton.Visibility == Visibility.Visible,
                        TimeSpan.FromSeconds(5),
                        "Botao Reativar nao ficou disponivel para o funcionario bloqueado selecionado.");

                    ClickButton(control, "ReativarFuncionarioButton");
                    WaitForCondition(
                        () =>
                        {
                            var reativado = repository.ObterPorId(funcionario.Id);
                            return reativado is { Ativo: true } &&
                                   string.Equals(reativado.Status, "Ativo", StringComparison.OrdinalIgnoreCase);
                        },
                        TimeSpan.FromSeconds(5),
                        "Clique real em Reativar nao restaurou o acesso do funcionario.");

                    var loginReativado = App.Database.AutenticarFuncionarioDetalhado(funcionario.Email, "Workflow@123");
                    if (!loginReativado.IsSuccess || loginReativado.Funcionario == null)
                    {
                        throw new InvalidOperationException("Funcionario reativado nao conseguiu autenticar.");
                    }
                }
                finally
                {
                    ResetLoginAttempts(funcionario.Id);
                    var estadoFinal = repository.ObterPorId(funcionario.Id);
                    if (estadoFinal is { Ativo: false })
                    {
                        repository.AtualizarStatusAcesso(funcionario.Id, ativo: true, status: "Ativo");
                    }

                    if (hostWindow.IsVisible)
                    {
                        hostWindow.Close();
                    }
                }
            });
        }

        private static void SelecionarFuncionarioNaGrade(DataGrid dataGrid, int funcionarioId)
        {
            var funcionario = dataGrid.ItemsSource?.Cast<Funcionario>()
                .SingleOrDefault(item => item.Id == funcionarioId)
                ?? throw new InvalidOperationException($"Funcionario {funcionarioId} nao foi localizado na grade.");

            dataGrid.SelectedItem = funcionario;
            dataGrid.ScrollIntoView(funcionario);
            WaitForUiIdle();
        }

        private void InserirProdutividadeFuncionarioSmoke(Funcionario funcionario)
        {
            var fixture = _fixture ?? throw new InvalidOperationException("Fixture sintetica nao foi inicializada para produtividade de funcionario.");
            var agora = DateTime.Now;
            var caixaId = Guid.NewGuid();
            var vendaId = Guid.NewGuid();

            using var connection = App.Database.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
                    UPDATE OrdensServico
                    SET TecnicoId = @TecnicoId,
                        Status = 'Finalizada',
                        DataInicio = @DataInicio,
                        DataConclusao = @DataConclusao,
                        ValorMaoObra = @ValorMaoObra
                    WHERE Id = @OrdemServicoId;";
                command.Parameters.AddWithValue("@TecnicoId", funcionario.Id);
                command.Parameters.AddWithValue("@DataInicio", agora.AddHours(-3).ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@DataConclusao", agora.AddHours(-1).ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@ValorMaoObra", 180m);
                command.Parameters.AddWithValue("@OrdemServicoId", fixture.OrdemServico.Id.ToString());
                command.ExecuteNonQuery();
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
                    INSERT INTO CaixaSessoes
                    (
                        Id,
                        NumeroCaixa,
                        DataAbertura,
                        DataFechamento,
                        OperadorId,
                        OperadorNome,
                        PerfilOperador,
                        ValorAbertura,
                        ValorEsperado,
                        ValorInformadoFechamento,
                        TotalVendas,
                        TotalSangrias,
                        TotalSuprimentos,
                        QuantidadeVendas,
                        Status,
                        Observacoes,
                        DataCriacao,
                        DataUltimaMovimentacao
                    )
                    VALUES
                    (
                        @Id,
                        @NumeroCaixa,
                        @DataAbertura,
                        @DataFechamento,
                        @OperadorId,
                        @OperadorNome,
                        @PerfilOperador,
                        @ValorAbertura,
                        @ValorEsperado,
                        @ValorInformadoFechamento,
                        @TotalVendas,
                        0,
                        0,
                        1,
                        'Fechado',
                        @Observacoes,
                        @DataCriacao,
                        @DataUltimaMovimentacao
                    );";
                command.Parameters.AddWithValue("@Id", caixaId.ToString());
                command.Parameters.AddWithValue("@NumeroCaixa", $"FUN-{agora:HHmmssfff}");
                command.Parameters.AddWithValue("@DataAbertura", agora.AddHours(-2).ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@DataFechamento", agora.AddMinutes(-30).ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@OperadorId", funcionario.Id);
                command.Parameters.AddWithValue("@OperadorNome", funcionario.Nome);
                command.Parameters.AddWithValue("@PerfilOperador", funcionario.PerfilAcesso);
                command.Parameters.AddWithValue("@ValorAbertura", 100m);
                command.Parameters.AddWithValue("@ValorEsperado", 340m);
                command.Parameters.AddWithValue("@ValorInformadoFechamento", 340m);
                command.Parameters.AddWithValue("@TotalVendas", 240m);
                command.Parameters.AddWithValue("@Observacoes", "Caixa sintetico para painel operacional de funcionario.");
                command.Parameters.AddWithValue("@DataCriacao", agora.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@DataUltimaMovimentacao", agora.AddMinutes(-25).ToString("yyyy-MM-dd HH:mm:ss"));
                command.ExecuteNonQuery();
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
                    INSERT INTO Vendas
                    (
                        Id,
                        Data,
                        ClienteId,
                        ClienteNome,
                        Total,
                        FormaPagamento,
                        Desconto,
                        Usuario,
                        QuantidadeItens,
                        Status,
                        CaixaSessaoId,
                        DataCancelamento,
                        CanceladoPor,
                        MotivoCancelamento
                    )
                    VALUES
                    (
                        @Id,
                        @Data,
                        @ClienteId,
                        @ClienteNome,
                        240,
                        'PIX',
                        0,
                        @Usuario,
                        1,
                        'Concluida',
                        @CaixaSessaoId,
                        NULL,
                        NULL,
                        NULL
                    );";
                command.Parameters.AddWithValue("@Id", vendaId.ToString());
                command.Parameters.AddWithValue("@Data", agora.AddMinutes(-40).ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@ClienteId", fixture.Cliente.Id.ToString());
                command.Parameters.AddWithValue("@ClienteNome", fixture.Cliente.Nome);
                command.Parameters.AddWithValue("@Usuario", funcionario.Nome);
                command.Parameters.AddWithValue("@CaixaSessaoId", caixaId.ToString());
                command.ExecuteNonQuery();
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
                    INSERT INTO MovimentacoesCaixa
                    (
                        Id,
                        CaixaSessaoId,
                        Data,
                        Tipo,
                        ValorMovimento,
                        ValorInicial,
                        ValorFinal,
                        Sangrias,
                        Suprimentos,
                        Diferenca,
                        Operador,
                        FormaPagamento,
                        ReferenciaId,
                        Observacoes
                    )
                    VALUES
                    (
                        @Id,
                        @CaixaSessaoId,
                        @Data,
                        'Venda',
                        240,
                        100,
                        340,
                        0,
                        0,
                        0,
                        @Operador,
                        'PIX',
                        @ReferenciaId,
                        'Movimento sintetico para painel operacional de funcionario.'
                    );";
                command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                command.Parameters.AddWithValue("@CaixaSessaoId", caixaId.ToString());
                command.Parameters.AddWithValue("@Data", agora.AddMinutes(-35).ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@Operador", funcionario.Nome);
                command.Parameters.AddWithValue("@ReferenciaId", vendaId.ToString());
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        private static void ValidarPainelOperacionalFuncionario(FuncionarioPainelOperacional painel)
        {
            if (painel.TotalAcoesAuditadas <= 0)
            {
                throw new InvalidOperationException("Painel operacional de funcionarios nao encontrou acoes auditadas.");
            }

            if (painel.TotalLogins <= 0 || !painel.UltimoLogin.HasValue)
            {
                throw new InvalidOperationException("Painel operacional de funcionarios nao consolidou historico de login.");
            }

            if (painel.OrdensExecutadas <= 0 || painel.OrdensFinalizadas <= 0 || painel.ValorOrdensExecutadas <= 0)
            {
                throw new InvalidOperationException("Painel operacional de funcionarios nao consolidou OS executadas.");
            }

            if (painel.VendasRealizadas <= 0 || painel.ValorVendasRealizadas <= 0)
            {
                throw new InvalidOperationException("Painel operacional de funcionarios nao consolidou vendas realizadas.");
            }

            if (painel.CaixasOperados <= 0 || painel.MovimentacoesCaixa <= 0 || painel.ValorCaixaOperado <= 0)
            {
                throw new InvalidOperationException("Painel operacional de funcionarios nao consolidou caixa operado.");
            }

            if (painel.TotalPermissoes <= 0 || painel.ModulosLiberados.Count == 0)
            {
                throw new InvalidOperationException("Painel operacional de funcionarios nao encontrou permissoes ativas do perfil.");
            }

            if (painel.PermissoesPorAcao.Count == 0 ||
                !painel.PermissoesPorAcao.Any(item => string.Equals(item.Acao, "Visualizar", StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Painel operacional de funcionarios nao consolidou permissoes por acao.");
            }

            if (painel.TotalPermissoesSensiveis <= 0)
            {
                throw new InvalidOperationException("Painel operacional de funcionarios nao identificou permissoes sensiveis do perfil.");
            }

            if (string.IsNullOrWhiteSpace(painel.ProdutividadeResumo) ||
                string.IsNullOrWhiteSpace(painel.PermissoesResumo) ||
                string.IsNullOrWhiteSpace(painel.AcoesPermitidasResumo))
            {
                throw new InvalidOperationException("Painel operacional de funcionarios gerou resumos vazios.");
            }
        }

    }
}
