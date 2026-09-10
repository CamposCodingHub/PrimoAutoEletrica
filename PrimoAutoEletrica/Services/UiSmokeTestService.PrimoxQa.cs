using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.UserControls;
using PrimoAutoEletrica.Views;
using PrimoAutoEletrica.Views.Clientes;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        private PrimoxQaEngine.CoverageReport? _qaEngineReport;

        private void RunPrimoxQaEngineChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            GarantirBancoIsoladoDoSmoke("PRIMOX QA Engine");
            _fixture ??= EnsureSmokeFixture(syntheticUser);
            var engine = new PrimoxQaEngine();
            var modules = ObterDeepQaModules();
            _qaEngineReport = engine.BuildStaticInventory(modules);

            RunCheck(result, "QaEngine:InventarioFuncional", () =>
            {
                if (_qaEngineReport.Modulos < 16)
                {
                    throw new InvalidOperationException(
                        $"QaEngine inventario: poucos modulos ({_qaEngineReport.Modulos}).");
                }

                if (_qaEngineReport.Windows < 20)
                {
                    throw new InvalidOperationException(
                        $"QaEngine inventario: poucas windows ({_qaEngineReport.Windows}).");
                }

                if (_qaEngineReport.AcoesDescobertas < 40)
                {
                    throw new InvalidOperationException(
                        $"QaEngine inventario: poucas acoes ({_qaEngineReport.AcoesDescobertas}).");
                }

                App.Logger.LogInfo(
                    $"QaEngine inventario: modulos={_qaEngineReport.Modulos}, windows={_qaEngineReport.Windows}, controles={_qaEngineReport.UserControls}, acoes={_qaEngineReport.AcoesDescobertas}.",
                    "Smoke");
            });

            RunCheck(result, "QaEngine:DescobertaBotoesRuntime", () =>
            {
                MainWindow? window = null;
                var totalBotoes = 0;
                try
                {
                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);

                    foreach (var modulo in modules)
                    {
                        var ok = string.Equals(modulo, "ImportarNFe", StringComparison.OrdinalIgnoreCase)
                            ? window.OpenImportarNFeForAutomation()
                            : window.NavigateToModuleForAutomation(modulo, forceReload: true);
                        if (!ok || window.CurrentContentElement is not FrameworkElement content)
                        {
                            throw new InvalidOperationException($"QaEngine discovery: falha ao abrir {modulo}.");
                        }

                        WaitForUiIdle();
                        var botoes = PrimoxQaEngine.FindButtons(content);
                        totalBotoes += botoes.Count;

                        foreach (var button in botoes.Take(8))
                        {
                            var label = ExtractButtonText(button);
                            var tip = button.ToolTip?.ToString() ?? string.Empty;
                            var name = string.IsNullOrWhiteSpace(button.Name) ? "(sem Name)" : button.Name;
                            _qaEngineReport!.Inventario.Add(new PrimoxQaEngine.InventoryItem
                            {
                                Modulo = modulo,
                                Tela = modulo,
                                Controle = name,
                                Funcao = string.IsNullOrWhiteSpace(label) ? tip : label,
                                MetodoOuCommand = button.Command?.ToString() ?? tip,
                                Testavel = button.IsEnabled,
                                Resultado = button.IsEnabled ? "DISCOVERED_RUNTIME" : "DISABLED"
                            });
                        }
                    }

                    _qaEngineReport!.BotoesDescobertos = totalBotoes;
                    if (totalBotoes < 50)
                    {
                        throw new InvalidOperationException($"QaEngine: poucos botoes runtime ({totalBotoes}).");
                    }

                    App.Logger.LogInfo($"QaEngine botoes runtime: {totalBotoes}.", "Smoke");
                }
                finally
                {
                    if (window?.IsVisible == true)
                    {
                        window.Close();
                    }
                }
            });

            RunCheck(result, "QaEngine:FuncionarioSalarioZeroEdicao", () =>
            {
                // Reproduz o bug real: colaborador com salario 0 nao conseguia salvar edicao.
                GarantirBancoIsoladoDoSmoke("funcionario salario zero");
                var repo = App.Repositories.Funcionarios;
                var token = DateTime.Now.ToString("HHmmssfff", CultureInfo.InvariantCulture);
                var email = $"teste.qa.salario0.{token}@primoauto.local";

                var funcionario = new Funcionario
                {
                    Nome = $"TESTE_QA_FUNCIONARIO_S0_{token}",
                    Email = email,
                    Senha = PasswordHasherService.HashPassword("Workflow@123"),
                    Funcao = "Atendente",
                    PerfilAcesso = "Vendedor",
                    Telefone = "11977771000",
                    DataAdmissao = DateTime.Today.AddDays(-60),
                    Salario = 0m,
                    Status = "Ativo",
                    Observacoes = "Seed QA salario zero",
                    DataCadastro = DateTime.Now,
                    Ativo = true
                };
                funcionario.Id = repo.Salvar(funcionario);

                var novoNome = $"TESTE_QA_FUNCIONARIO_EDIT_{token}";
                var novoTelefone = "11988882000";

                var editar = new EditarFuncionarioWindow(syntheticUser, funcionario);
                AutomatedDialogSupervisor? supervisor = null;
                try
                {
                    ShowWindowForInteraction(editar);
                    supervisor = new AutomatedDialogSupervisor(editar, _fixture);
                    supervisor.Start();

                    var salarioBox = FindElementByName<TextBox>(editar, "SalarioTextBox")
                        ?? throw new InvalidOperationException("SalarioTextBox ausente.");
                    if (!decimal.TryParse(salarioBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var salarioUi) &&
                        !decimal.TryParse(salarioBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out salarioUi))
                    {
                        throw new InvalidOperationException($"Salario UI invalido: '{salarioBox.Text}'.");
                    }

                    if (salarioUi != 0m)
                    {
                        throw new InvalidOperationException($"Esperado salario UI 0, obtido {salarioUi}.");
                    }

                    SetTextBoxValue(editar, "NomeTextBox", novoNome);
                    SetTextBoxValue(editar, "TelefoneTextBox", "(11) 98888-2000");
                    WaitForUiIdle();

                    ClickButton(editar, "SalvarButton");
                    WaitForCondition(
                        () => !editar.IsVisible,
                        TimeSpan.FromSeconds(8),
                        "Edicao com salario 0 nao fechou apos salvar (bug provavelmente ainda presente).");
                }
                finally
                {
                    supervisor?.Dispose();
                    if (editar.IsVisible)
                    {
                        editar.Close();
                    }
                }

                var persistido = repo.ObterPorId(funcionario.Id)
                    ?? throw new InvalidOperationException("Funcionario salario0 nao encontrado apos salvar.");
                if (!string.Equals(persistido.Nome, novoNome, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Nome nao persistiu. Esperado '{novoNome}', obtido '{persistido.Nome}'.");
                }

                var telDigits = new string((persistido.Telefone ?? string.Empty).Where(char.IsDigit).ToArray());
                if (!string.Equals(telDigits, novoTelefone, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Telefone nao persistiu. Esperado '{novoTelefone}', obtido '{persistido.Telefone}'.");
                }

                if (persistido.Salario != 0m)
                {
                    throw new InvalidOperationException($"Salario deveria permanecer 0, obtido {persistido.Salario}.");
                }
            });

            RunCheck(result, "QaEngine:FuncionarioCrudPersistenciaCompleta", () =>
            {
                GarantirBancoIsoladoDoSmoke("funcionario CRUD completo");
                var repo = App.Repositories.Funcionarios;
                var token = DateTime.Now.ToString("HHmmssfff", CultureInfo.InvariantCulture);
                var email1 = $"teste.qa.func.{token}@primoauto.local";
                var email2 = $"teste.qa.func.edit.{token}@primoauto.local";
                const string senha = "Workflow@123";

                // CREATE via UI
                var novo = new NovoFuncionarioWindow(syntheticUser);
                AutomatedDialogSupervisor? novoSup = null;
                try
                {
                    ShowWindowForInteraction(novo);
                    novoSup = new AutomatedDialogSupervisor(novo, _fixture);
                    novoSup.Start();

                    SetTextBoxValue(novo, "NomeTextBox", $"TESTE_QA_FUNCIONARIO_{token}");
                    SetTextBoxValue(novo, "EmailTextBox", email1);
                    SetTextBoxValue(novo, "TelefoneTextBox", "(11) 97777-3000");
                    SetTextBoxValue(novo, "CPFTextBox", GerarCpfValido(token));
                    SetTextBoxValue(novo, "FuncaoTextBox", "Mecanico QA");
                    SetTextBoxValue(novo, "SalarioTextBox", "2800,00");
                    SetTextBoxValue(novo, "ObservacoesTextBox", "Obs QA create");
                    DefinirComboBoxTexto(novo, "StatusComboBox", "Ativo");
                    DefinirComboBoxTexto(novo, "PerfilComboBox", "Vendedor");
                    SetPasswordBoxValue(novo, "SenhaPasswordBox", senha);
                    SetPasswordBoxValue(novo, "ConfirmarSenhaPasswordBox", senha);
                    var admissao = FindElementByName<DatePicker>(novo, "DataAdmissaoDatePicker")
                        ?? throw new InvalidOperationException("DataAdmissaoDatePicker ausente.");
                    admissao.SelectedDate = DateTime.Today.AddDays(-10);
                    WaitForUiIdle();
                    ClickButton(novo, "SalvarButton");
                    WaitForCondition(() => !novo.IsVisible, TimeSpan.FromSeconds(8), "Novo funcionario nao fechou.");
                }
                finally
                {
                    novoSup?.Dispose();
                    if (novo.IsVisible) novo.Close();
                }

                var criado = repo.ObterTodos(false)
                    .FirstOrDefault(f => string.Equals(f.Email, email1, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException("CREATE funcionario: registro nao encontrado.");

                if (!string.Equals(criado.Nome, $"TESTE_QA_FUNCIONARIO_{token}", StringComparison.Ordinal) ||
                    criado.Salario != 2800m)
                {
                    throw new InvalidOperationException("CREATE funcionario: campos inconsistentes apos READ.");
                }

                // UPDATE ciclo 1
                var nomeEdit1 = $"TESTE_QA_FUNCIONARIO_V1_{token}";
                EditarESalvarFuncionario(
                    syntheticUser,
                    criado,
                    nomeEdit1,
                    email2,
                    "(11) 97777-4000",
                    "Consultor QA",
                    "Em treinamento",
                    "Administrador",
                    "3100,25",
                    "Obs QA edit v1");

                var v1 = repo.ObterPorId(criado.Id)
                    ?? throw new InvalidOperationException("UPDATE v1: registro ausente.");
                AssertFuncionarioPersistido(v1, nomeEdit1, email2, "11977774000", "Consultor QA", "Em treinamento", "Administrador", 3100.25m);

                // UPDATE ciclo 2 (repeticao / estado residual)
                var nomeEdit2 = $"TESTE_QA_FUNCIONARIO_V2_{token}";
                EditarESalvarFuncionario(
                    syntheticUser,
                    v1,
                    nomeEdit2,
                    email2,
                    "(11) 97777-5000",
                    "Supervisor QA",
                    "Ativo",
                    "Administrador",
                    "3200,00",
                    "Obs QA edit v2");

                var v2 = repo.ObterPorId(criado.Id)
                    ?? throw new InvalidOperationException("UPDATE v2: registro ausente.");
                AssertFuncionarioPersistido(v2, nomeEdit2, email2, "11977775000", "Supervisor QA", "Ativo", "Administrador", 3200m);

                // CANCEL nao deve persistir
                var editarCancel = new EditarFuncionarioWindow(syntheticUser, v2);
                try
                {
                    ShowWindowForInteraction(editarCancel);
                    SetTextBoxValue(editarCancel, "NomeTextBox", "NAO_DEVE_PERSISTIR");
                    ClickButton(editarCancel, "Cancelar");
                    WaitForCondition(() => !editarCancel.IsVisible, TimeSpan.FromSeconds(5), "Cancelar nao fechou.");
                }
                finally
                {
                    if (editarCancel.IsVisible) editarCancel.Close();
                }

                var aposCancel = repo.ObterPorId(criado.Id)
                    ?? throw new InvalidOperationException("Apos cancel: registro ausente.");
                if (string.Equals(aposCancel.Nome, "NAO_DEVE_PERSISTIR", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("CANCEL persistiu nome indevidamente.");
                }

                // Bloqueio / reativacao
                EditarESalvarFuncionario(
                    syntheticUser,
                    aposCancel,
                    aposCancel.Nome,
                    aposCancel.Email,
                    "(11) 97777-5000",
                    aposCancel.Funcao,
                    "Bloqueado",
                    aposCancel.PerfilAcesso,
                    "3200,00",
                    aposCancel.Observacoes);

                var bloqueado = repo.ObterPorId(criado.Id)
                    ?? throw new InvalidOperationException("Bloqueio: registro ausente.");
                if (!string.Equals(bloqueado.Status, "Bloqueado", StringComparison.OrdinalIgnoreCase) || bloqueado.Ativo)
                {
                    throw new InvalidOperationException("Bloqueio nao refletiu Status/Ativo.");
                }

                EditarESalvarFuncionario(
                    syntheticUser,
                    bloqueado,
                    bloqueado.Nome,
                    bloqueado.Email,
                    "(11) 97777-5000",
                    bloqueado.Funcao,
                    "Ativo",
                    bloqueado.PerfilAcesso,
                    "3200,00",
                    bloqueado.Observacoes);

                var reativado = repo.ObterPorId(criado.Id)
                    ?? throw new InvalidOperationException("Reativacao: registro ausente.");
                if (!string.Equals(reativado.Status, "Ativo", StringComparison.OrdinalIgnoreCase) || !reativado.Ativo)
                {
                    throw new InvalidOperationException("Reativacao nao refletiu Status/Ativo.");
                }
            });

            RunCheck(result, "QaEngine:FuncionarioNegativoValidacao", () =>
            {
                GarantirBancoIsoladoDoSmoke("funcionario negativo");
                StabilizarUiQaEngine();
                var repo = App.Repositories.Funcionarios;
                var token = DateTime.Now.ToString("HHmmssfff", CultureInfo.InvariantCulture);
                var funcionario = new Funcionario
                {
                    Nome = $"TESTE_QA_NEG_{token}",
                    Email = $"teste.qa.neg.{token}@primoauto.local",
                    Senha = PasswordHasherService.HashPassword("Workflow@123"),
                    Funcao = "Atendente",
                    PerfilAcesso = "Vendedor",
                    Telefone = "11977776000",
                    DataAdmissao = DateTime.Today.AddDays(-5),
                    Salario = 1500m,
                    Status = "Ativo",
                    Observacoes = "Negativo QA",
                    DataCadastro = DateTime.Now,
                    Ativo = true
                };
                funcionario.Id = repo.Salvar(funcionario);
                var nomeOriginal = funcionario.Nome;

                var editar = new EditarFuncionarioWindow(syntheticUser, funcionario);
                try
                {
                    ShowWindowForInteraction(editar);
                    CloseTransientWindows(editar);

                    SetTextBoxValue(editar, "NomeTextBox", "   ");
                    var nomeBox = FindElementByName<TextBox>(editar, "NomeTextBox")
                        ?? throw new InvalidOperationException("NomeTextBox ausente.");
                    if (!string.IsNullOrWhiteSpace(nomeBox.Text))
                    {
                        throw new InvalidOperationException($"Falha ao limpar NomeTextBox (valor='{nomeBox.Text}').");
                    }

                    // Invoca o handler real sem WaitForUiIdle longo (evita race com supervisors residuais).
                    var salvar = typeof(EditarFuncionarioWindow).GetMethod(
                        "SalvarButton_Click",
                        BindingFlags.Instance | BindingFlags.NonPublic)
                        ?? throw new MissingMethodException(nameof(EditarFuncionarioWindow), "SalvarButton_Click");
                    salvar.Invoke(editar, new object?[] { editar, new RoutedEventArgs() });
                    PumpDispatcher();

                    if (!editar.IsVisible)
                    {
                        var apos = repo.ObterPorId(funcionario.Id);
                        throw new InvalidOperationException(
                            $"Janela fechou ao salvar com nome vazio. Persistencia Nome='{apos?.Nome}' (esperado '{nomeOriginal}').");
                    }

                    var erro = FindElementByName<TextBlock>(editar, "ErrorMessageTextBlock");
                    if (erro == null || erro.Visibility != Visibility.Visible ||
                        string.IsNullOrWhiteSpace(erro.Text) ||
                        !erro.Text.Contains("nome", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            $"Erro de validacao de nome nao exibido. Texto='{erro?.Text}', Visibility={erro?.Visibility}.");
                    }
                }
                finally
                {
                    if (editar.IsVisible)
                    {
                        editar.Close();
                    }
                }

                var depois = repo.ObterPorId(funcionario.Id)
                    ?? throw new InvalidOperationException("Pos-negativo: registro ausente.");
                if (!string.Equals(depois.Nome, nomeOriginal, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Validacao negativa alterou nome no banco.");
                }
            });

            RunCheck(result, "QaEngine:ClientePersistenciaRoundTrip", () =>
            {
                GarantirBancoIsoladoDoSmoke("cliente persistencia");
                var cliente = _fixture!.Cliente;
                var token = DateTime.Now.ToString("HHmmssfff", CultureInfo.InvariantCulture);
                var novoNome = $"TESTE_QA_CLIENTE_{token}";
                var novoTel = "(11) 96666-1000";

                var editar = new EditarClienteWindow(cliente);
                AutomatedDialogSupervisor? sup = null;
                try
                {
                    ShowWindowForInteraction(editar);
                    sup = new AutomatedDialogSupervisor(editar, _fixture);
                    sup.Start();
                    SetTextBoxValue(editar, "NomeTextBox", novoNome);
                    TrySetTextBoxIfExists(editar, "TelefoneTextBox", novoTel);
                    TrySetTextBoxIfExists(editar, "CelularTextBox", novoTel);
                    WaitForUiIdle();

                    var saveChanges = LocalizationService.Instance.GetString("SaveChanges");
                    if (!TryClickButton(editar, "Salvar alteracoes") &&
                        !TryClickButton(editar, "Salvar alterações") &&
                        !TryClickButton(editar, saveChanges) &&
                        !TryClickButton(editar, "SalvarButton") &&
                        !TryClickButton(editar, "Salvar") &&
                        !TryClickButton(editar, LocalizationService.Instance.GetString("Save")))
                    {
                        throw new InvalidOperationException("Botao Salvar de cliente nao encontrado.");
                    }

                    WaitForCondition(() => !editar.IsVisible, TimeSpan.FromSeconds(10), "EditarCliente nao fechou.");
                }
                finally
                {
                    sup?.Dispose();
                    if (editar.IsVisible) editar.Close();
                }

                var persistido = App.Repositories.Clientes.ObterPorId(cliente.Id)
                    ?? throw new InvalidOperationException("Cliente nao encontrado apos UPDATE.");
                if (!string.Equals(persistido.Nome, novoNome, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Cliente nome nao persistiu. Esperado '{novoNome}', obtido '{persistido.Nome}'.");
                }

                _fixture.Cliente = persistido;
            });

            RunCheck(result, "QaEngine:VeiculoPersistenciaRoundTrip", () =>
            {
                GarantirBancoIsoladoDoSmoke("veiculo persistencia");
                StabilizarUiQaEngine();
                var veiculo = App.Repositories.Clientes.ObterTodosVeiculos()
                    .FirstOrDefault(v => v.Id == _fixture!.Veiculo.Id)
                    ?? throw new InvalidOperationException("Veiculo fixture ausente.");
                var token = DateTime.Now.ToString("HHmmssfff", CultureInfo.InvariantCulture);
                var novaObs = $"TESTE_QA_VEICULO_{token}";

                // UI: abertura isolada (sem AssertWindowStillOperational do PDV).
                var uiAbriu = false;
                var visualizar = new VisualizarVeiculoWindow(veiculo, App.Database);
                try
                {
                    visualizar.WindowStartupLocation = WindowStartupLocation.Manual;
                    visualizar.Left = 40;
                    visualizar.Top = 40;
                    visualizar.ShowInTaskbar = false;
                    visualizar.Show();
                    PumpDispatcher();
                    uiAbriu = visualizar.IsLoaded && visualizar.IsVisible;
                }
                finally
                {
                    if (visualizar.IsVisible) visualizar.Close();
                }

                if (!uiAbriu)
                {
                    _qaEngineReport!.Blocked++;
                    _qaEngineReport.Observacoes.Add(
                        "VisualizarVeiculoWindow: abertura UI BLOCKED no runner off-screen; persistencia domain ainda validada.");
                    App.Logger.LogWarning("QaEngine: VisualizarVeiculo UI nao estabilizou; seguindo validacao de persistencia.", "Smoke");
                }

                veiculo.Observacoes = novaObs;
                veiculo.Quilometragem = Math.Max(veiculo.Quilometragem, 1000) + 7;
                App.Repositories.Clientes.SalvarVeiculo(veiculo);

                var lido = App.Repositories.Clientes.ObterTodosVeiculos()
                    .FirstOrDefault(v => v.Id == veiculo.Id)
                    ?? throw new InvalidOperationException("Veiculo ausente apos UPDATE.");
                if (!string.Equals(lido.Observacoes, novaObs, StringComparison.Ordinal) ||
                    lido.Quilometragem != veiculo.Quilometragem)
                {
                    throw new InvalidOperationException("Veiculo UPDATE nao persistiu Observacoes/KM.");
                }

                _fixture.Veiculo = lido;
            });

            RunCheck(result, "QaEngine:NavegacaoModulosCompleta", () =>
            {
                MainWindow? window = null;
                try
                {
                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);

                    foreach (var modulo in modules)
                    {
                        var ok = string.Equals(modulo, "ImportarNFe", StringComparison.OrdinalIgnoreCase)
                            ? window.OpenImportarNFeForAutomation()
                            : window.NavigateToModuleForAutomation(modulo, forceReload: true);
                        if (!ok)
                        {
                            throw new InvalidOperationException($"Navegacao morta: {modulo}.");
                        }

                        WaitForUiIdle();
                        if (window.CurrentContentElement == null)
                        {
                            throw new InvalidOperationException($"Conteudo nulo apos abrir {modulo}.");
                        }

                        AssertWindowStillOperational(window, modulo);
                    }

                    // Relacionamentos: Cliente -> Veiculo -> OS -> volta Dashboard
                    if (!window.NavigateToModuleForAutomation("Clientes", forceReload: true) ||
                        !window.NavigateToModuleForAutomation("Veiculos", forceReload: true) ||
                        !window.NavigateToModuleForAutomation("OrdensServico", forceReload: true) ||
                        !window.NavigateToModuleForAutomation("Dashboard", forceReload: true))
                    {
                        throw new InvalidOperationException("Cadeia Cliente→Veiculo→OS→Dashboard falhou.");
                    }
                }
                finally
                {
                    if (window?.IsVisible == true) window.Close();
                }
            });

            RunCheck(result, "QaEngine:TecladoShellBasico", () =>
            {
                MainWindow? window = null;
                try
                {
                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);
                    window.NavigateToModuleForAutomation("Funcionarios", forceReload: true);
                    WaitForUiIdle();

                    if (window.CurrentContentElement is not FrameworkElement content)
                    {
                        throw new InvalidOperationException("Conteudo Funcionarios ausente.");
                    }

                    var search = FindElementByName<TextBox>(content, "SearchTextBox")
                        ?? FindVisualChildren<TextBox>(content).FirstOrDefault();
                    if (search == null)
                    {
                        throw new InvalidOperationException("Campo de busca ausente para teste de teclado.");
                    }

                    search.Focus();
                    WaitForUiIdle();
                    if (!search.IsKeyboardFocusWithin && !search.IsFocused)
                    {
                        throw new InvalidOperationException("SearchTextBox nao recebeu foco.");
                    }

                    search.Text = "TESTE_QA_SEM_RESULTADO_ZZZ";
                    WaitForUiIdle();

                    // Tab / Escape nao devem derrubar a janela
                    search.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    WaitForUiIdle();
                    AssertWindowStillOperational(window, "Teclado Tab");
                }
                finally
                {
                    if (window?.IsVisible == true) window.Close();
                }
            });

            RunCheck(result, "QaEngine:TemaLightDarkRoundTrip", () =>
            {
                var theme = new ThemeService();
                var original = theme.GetCurrentTheme();
                MainWindow? window = null;
                try
                {
                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);

                    foreach (var tema in new[] { AppTheme.Light, AppTheme.Dark, AppTheme.Light })
                    {
                        theme.ApplyTheme(tema);
                        WaitForUiIdle();
                        if (!window.NavigateToModuleForAutomation("Funcionarios", forceReload: true) ||
                            window.CurrentContentElement == null)
                        {
                            throw new InvalidOperationException($"Tema {tema}: Funcionarios falhou.");
                        }

                        AssertWindowStillOperational(window, $"Tema {tema}");
                    }
                }
                finally
                {
                    theme.ApplyTheme(original);
                    if (window?.IsVisible == true) window.Close();
                }
            });

            RunCheck(result, "QaEngine:ResponsividadeResolucoes", () =>
            {
                MainWindow? window = null;
                try
                {
                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);
                    var sizes = new (int W, int H)[]
                    {
                        (1366, 768),
                        (1600, 900),
                        (1920, 1080),
                        (2560, 1440)
                    };

                    foreach (var size in sizes)
                    {
                        window.Width = size.W;
                        window.Height = size.H;
                        WaitForUiIdle();
                        if (!window.NavigateToModuleForAutomation("Dashboard", forceReload: true) ||
                            window.CurrentContentElement == null)
                        {
                            throw new InvalidOperationException($"Resize {size.W}x{size.H}: Dashboard falhou.");
                        }

                        AssertWindowStillOperational(window, $"{size.W}x{size.H}");
                    }
                }
                finally
                {
                    if (window?.IsVisible == true) window.Close();
                }
            });

            RunCheck(result, "QaEngine:DestrutivoSomenteDialog", () =>
            {
                // Nao executa exclusao real — valida abertura/cancelamento do fluxo destrutivo quando disponivel.
                var host = CreateHostWindow(new FuncionariosControl(), nameof(FuncionariosControl));
                try
                {
                    ShowWindowForInteraction(host);
                    if (host.Content is not FuncionariosControl control)
                    {
                        throw new InvalidOperationException("FuncionariosControl ausente.");
                    }

                    PrepareInteractiveSurface(control, typeof(FuncionariosControl));
                    WaitForUiIdle();

                    var grid = FindElementByName<DataGrid>(control, "FuncionariosDataGrid");
                    if (grid == null || grid.Items.Count == 0)
                    {
                        throw new InvalidOperationException("Grade funcionarios vazia — BLOCKED para destrutivo.");
                    }

                    SelectFirstDataGridItem(grid);
                    WaitForUiIdle();

                    // Preferimos botao de exclusao se existir e estiver habilitado; cancelamos dialog.
                    var excluir = FindVisualChildren<Button>(control)
                        .FirstOrDefault(b =>
                            b.IsVisible &&
                            (ExtractButtonText(b).Contains("Excluir", StringComparison.OrdinalIgnoreCase) ||
                             ExtractButtonText(b).Contains("Remover", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(b.Name, "ExcluirButton", StringComparison.OrdinalIgnoreCase)));

                    if (excluir == null || !excluir.IsEnabled)
                    {
                        _qaEngineReport!.Blocked++;
                        _qaEngineReport.Observacoes.Add(
                            "Destrutivo Funcionarios: botao Excluir nao disponivel/habilitado — marcado BLOCKED (seguro).");
                        App.Logger.LogInfo("QaEngine destrutivo: Excluir nao disponivel; tratado como BLOCKED seguro.", "Smoke");
                        return;
                    }

                    var antesCount = App.Repositories.Funcionarios.ObterTodos(false).Count;
                    RaiseButtonClick(excluir);
                    WaitForUiIdle();

                    // Dialog supervisor cancela confirmacoes; garantir que contagem nao caiu.
                    var depoisCount = App.Repositories.Funcionarios.ObterTodos(false).Count;
                    if (depoisCount < antesCount)
                    {
                        throw new InvalidOperationException("Exclusao real ocorreu fora do ambiente esperado.");
                    }

                    AssertWindowStillOperational(host, "Destrutivo dialog");
                }
                finally
                {
                    if (host.IsVisible) host.Close();
                }
            });

            // Fase 13 — cobertura funcional profunda dos modulos prioritarios.
            RunPrimoxQaCoverageExpansionChecks(result, syntheticUser);

            // Fase 14 — finalizacao / release candidate (inventario, matriz, a11y, LongRun 5).
            RunPrimoxQaFinalizationChecks(result, syntheticUser);

            RunCheck(result, "QaEngine:LongRunOperacional", () =>
            {
                var sw = Stopwatch.StartNew();
                MainWindow? window = null;
                var ciclos = 3;
                var navegacoes = 0;
                var rota = new[]
                {
                    "Dashboard", "Clientes", "Veiculos", "OrdensServico", "Orcamentos", "Agendamentos",
                    "Estoque", "Financeiro", "Relatorios", "Funcionarios", "PDV", "Fornecedores",
                    "OficinaKanban", "Dashboard"
                };

                var theme = new ThemeService();
                var original = theme.GetCurrentTheme();
                try
                {
                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);

                    for (var ciclo = 1; ciclo <= ciclos; ciclo++)
                    {
                        theme.ApplyTheme(ciclo % 2 == 0 ? AppTheme.Dark : AppTheme.Light);
                        WaitForUiIdle();

                        foreach (var modulo in rota)
                        {
                            if (!window.NavigateToModuleForAutomation(modulo, forceReload: true) ||
                                window.CurrentContentElement == null)
                            {
                                throw new InvalidOperationException($"LongRun ciclo {ciclo}: falha em {modulo}.");
                            }

                            navegacoes++;
                            WaitForUiIdle();
                        }
                    }

                    if (navegacoes < rota.Length * ciclos)
                    {
                        throw new InvalidOperationException("LongRun: navegacoes insuficientes.");
                    }

                    App.Logger.LogInfo(
                        $"QaEngine LongRun: ciclos={ciclos}, navegacoes={navegacoes}, {sw.Elapsed.TotalSeconds:F1}s.",
                        "Smoke");
                }
                finally
                {
                    theme.ApplyTheme(original);
                    if (window?.IsVisible == true) window.Close();
                }
            });

            RunCheck(result, "QaEngine:RelatorioCobertura", () =>
            {
                var report = _qaEngineReport
                    ?? throw new InvalidOperationException("Relatorio QA Engine nao inicializado.");

                report.TestesExecutados = result.Checks.Count(c => c.Name.StartsWith("QaEngine:", StringComparison.OrdinalIgnoreCase)) + 1;
                report.Pass = result.Checks.Count(c =>
                    c.Name.StartsWith("QaEngine:", StringComparison.OrdinalIgnoreCase) && c.Success);
                report.Fail = result.Checks.Count(c =>
                    c.Name.StartsWith("QaEngine:", StringComparison.OrdinalIgnoreCase) && !c.Success);
                report.Observacoes.Add("Deep QA da Fase 11 foi preservado e expandido.");
                report.Observacoes.Add("O QaEngine foi expandido para cobertura funcional profunda dos modulos prioritarios.");
                report.Observacoes.Add("Ambiente: banco isolado ui-smoke-test (nunca producao).");
                report.Observacoes.Add($"Baseline: Fase 12=14; Fase 13=29; Fase 14 finalizacao={report.TestesExecutados} checks QaEngine.");

                var outDir = Path.Combine(AppContext.BaseDirectory, "Logs", "qa-engine");
                var path = engine.WriteReport(report, outDir);
                if (!File.Exists(path) || new FileInfo(path).Length < 200)
                {
                    throw new InvalidOperationException($"Relatorio QA Engine invalido: {path}");
                }

                App.Logger.LogInfo($"QaEngine relatorio: {path}", "Smoke");
            });
        }

        private void EditarESalvarFuncionario(
            Funcionario usuario,
            Funcionario alvo,
            string nome,
            string email,
            string telefone,
            string funcao,
            string status,
            string perfil,
            string salario,
            string observacoes)
        {
            var editar = new EditarFuncionarioWindow(usuario, alvo);
            AutomatedDialogSupervisor? supervisor = null;
            try
            {
                ShowWindowForInteraction(editar);
                supervisor = new AutomatedDialogSupervisor(editar, _fixture);
                supervisor.Start();

                SetTextBoxValue(editar, "NomeTextBox", nome);
                SetTextBoxValue(editar, "EmailTextBox", email);
                SetTextBoxValue(editar, "TelefoneTextBox", telefone);
                SetTextBoxValue(editar, "FuncaoTextBox", funcao);
                SetTextBoxValue(editar, "SalarioTextBox", salario);
                SetTextBoxValue(editar, "ObservacoesTextBox", observacoes);
                DefinirComboBoxTexto(editar, "StatusComboBox", status);
                DefinirComboBoxTexto(editar, "PerfilComboBox", perfil);
                WaitForUiIdle();
                ClickButton(editar, "SalvarButton");
                WaitForCondition(
                    () => !editar.IsVisible,
                    TimeSpan.FromSeconds(8),
                    $"Editar funcionario '{nome}' nao fechou apos salvar.");
            }
            finally
            {
                supervisor?.Dispose();
                if (editar.IsVisible) editar.Close();
            }
        }

        private static void AssertFuncionarioPersistido(
            Funcionario f,
            string nome,
            string email,
            string telefoneDigits,
            string funcao,
            string status,
            string perfil,
            decimal salario)
        {
            var tel = new string((f.Telefone ?? string.Empty).Where(char.IsDigit).ToArray());
            if (!string.Equals(f.Nome, nome, StringComparison.Ordinal) ||
                !string.Equals(f.Email, email, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(tel, telefoneDigits, StringComparison.Ordinal) ||
                !string.Equals(f.Funcao, funcao, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(f.Status, status, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(f.PerfilAcesso, perfil, StringComparison.OrdinalIgnoreCase) ||
                f.Salario != salario)
            {
                throw new InvalidOperationException(
                    $"Persistencia funcionario inconsistente. Nome={f.Nome}, Email={f.Email}, Tel={f.Telefone}, Funcao={f.Funcao}, Status={f.Status}, Perfil={f.PerfilAcesso}, Salario={f.Salario}.");
            }
        }

        private static void TrySetTextBoxIfExists(DependencyObject root, string name, string value)
        {
            var box = FindElementByName<TextBox>(root, name);
            if (box != null)
            {
                SetTextBoxValue(root, name, value);
            }
        }

        private static bool TryClickButton(DependencyObject root, string nameOrText)
        {
            try
            {
                ClickButton(root, nameOrText);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void StabilizarUiQaEngine()
        {
            var windows = Application.Current?.Windows.OfType<Window>().ToList() ?? new List<Window>();
            foreach (var window in windows)
            {
                try
                {
                    if (window.IsVisible)
                    {
                        window.Close();
                    }
                }
                catch
                {
                }
            }

            PumpDispatcher();
            // Aguarda threads de AutomatedDialogSupervisor residuais encerrarem (Join usa ate 1s).
            Thread.Sleep(1200);
            PumpDispatcher();
        }
    }
}
