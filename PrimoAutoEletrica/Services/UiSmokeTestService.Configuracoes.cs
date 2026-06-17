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
        // Checks de configuracoes comerciais, backup e impressao.

        private void RunConfiguracoesComerciaisBackupChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "Configuracoes:ComercialBackupRestauracao", () =>
            {
                GarantirBancoIsoladoDoSmoke("configuracoes, backup e restauracao");
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do smoke test ainda nao foi inicializada.");
                var originalConfiguration = BusinessConfigurationService.LoadOrCreateDefault(App.RuntimeAppDataPath, _logger);
                var originalStationConfiguration = StationService.GetConfiguration(App.RuntimeAppDataPath);
                var originalStationSnapshot = new StationConfiguration
                {
                    MachineName = originalStationConfiguration.MachineName,
                    StationName = originalStationConfiguration.StationName,
                    StationType = originalStationConfiguration.StationType,
                    Description = originalStationConfiguration.Description,
                    UseConfiguredPdvPrinter = originalStationConfiguration.UseConfiguredPdvPrinter,
                    PreferredPdvPrinterName = originalStationConfiguration.PreferredPdvPrinterName,
                    UseLocalSync = originalStationConfiguration.UseLocalSync,
                    LocalSyncPort = originalStationConfiguration.LocalSyncPort,
                    ConfiguredAt = originalStationConfiguration.ConfiguredAt
                };
                var databaseSettings = DatabaseConnectionSettingsService.LoadOrCreateDefault(App.RuntimeAppDataPath, _logger);
                var runtimeIsSqlServer = string.Equals(App.Database.RuntimeProvider, "SqlServer", StringComparison.OrdinalIgnoreCase);
                var token = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                var smokeDirectory = Path.Combine(App.RuntimeLogDirectory, "configuracoes-smoke");
                Directory.CreateDirectory(smokeDirectory);

                var logoPath = Path.Combine(smokeDirectory, $"logo-smoke-{token}.png");
                var bitmap = BitmapSource.Create(
                    1,
                    1,
                    96,
                    96,
                    PixelFormats.Bgra32,
                    null,
                    new byte[] { 0x1E, 0x8A, 0x5A, 0xFF },
                    4);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using (var logoStream = File.Create(logoPath))
                {
                    encoder.Save(logoStream);
                }

                var backupExtension = runtimeIsSqlServer ? ".bak" : ".db";
                var backupPath = Path.Combine(smokeDirectory, $"PrimoAutoEletrica_Backup_ConfigSmoke_{token}{backupExtension}");
                Produto? produtoPosteriorAoBackup = null;
                ConfiguracoesSistemaWindow? window = null;

                try
                {
                    if (databaseSettings.IsSqlServer)
                    {
                        if (!runtimeIsSqlServer ||
                            App.Database.IsUsingUnsupportedProviderFallback ||
                            !string.Equals(databaseSettings.SqlServerDatabase, App.Database.DatabasePath, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException("O smoke nao esta usando o banco SQL Server isolado configurado para homologacao automatizada.");
                        }
                    }
                    else if (!databaseSettings.IsSQLite ||
                             !string.Equals(
                                 Path.GetFullPath(databaseSettings.ResolveSqlitePath(App.RuntimeAppDataPath)),
                                 Path.GetFullPath(App.Database.DatabasePath),
                                 StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("O smoke nao esta usando o banco SQLite isolado configurado para homologacao automatizada.");
                    }

                    var smokeConfiguration = new BusinessConfiguration
                    {
                        CompanyDisplayName = "Smoke Auto Eletrica",
                        CompanyLegalName = "Smoke Auto Eletrica LTDA",
                        CompanyDocument = "12.345.678/0001-90",
                        CompanyPhone = "(11) 98888-0000",
                        CompanyAddress = "Rua Smoke Teste, 123",
                        LogoPath = logoPath,
                        ReceiptHeader = "Cabecalho smoke comprovante",
                        ReceiptFooter = "Rodape smoke comprovante"
                    };

                    BusinessConfigurationService.Save(App.RuntimeAppDataPath, smokeConfiguration);
                    var loadedConfiguration = BusinessConfigurationService.LoadOrCreateDefault(App.RuntimeAppDataPath, _logger);
                    if (!string.Equals(loadedConfiguration.CompanyDisplayName, smokeConfiguration.CompanyDisplayName, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Configuracao comercial nao foi persistida corretamente.");
                    }

                    var logoValidation = BusinessConfigurationService.ValidateLogoPath(loadedConfiguration.LogoPath);
                    if (!logoValidation.IsValid)
                    {
                        throw new InvalidOperationException($"Logo smoke nao foi validado: {logoValidation.Message}");
                    }

                    var comprovante = new VendaComprovanteService(loadedConfiguration).CriarDocumento(fixture.Venda);
                    var comprovanteTexto = new TextRange(comprovante.ContentStart, comprovante.ContentEnd).Text;
                    if (!comprovanteTexto.Contains("Smoke Auto Eletrica", StringComparison.Ordinal) ||
                        !comprovanteTexto.Contains("Rodape smoke comprovante", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Comprovante nao refletiu os dados comerciais configurados.");
                    }

                    if (!comprovante.Blocks.OfType<BlockUIContainer>().Any(block => block.Child is Image))
                    {
                        throw new InvalidOperationException("Comprovante nao recebeu o logo configurado como imagem.");
                    }

                    var relatorioPath = Path.Combine(smokeDirectory, $"RelatorioConfigMarca_{token}.pdf");
                    new RelatorioExportService(loadedConfiguration).ExportarParaPDF(
                        new List<DadoFinanceiro>
                        {
                            new()
                            {
                                Data = DateTime.Today,
                                Tipo = "Receita",
                                Categoria = "Smoke",
                                Descricao = "Receita smoke configuracao",
                                Valor = 120m,
                                FormaPagamento = "PIX",
                                Usuario = syntheticUser.Nome
                            }
                        },
                        new List<DadoVenda>
                        {
                            new()
                            {
                                Data = DateTime.Today,
                                ClienteNome = fixture.Cliente.Nome,
                                VendedorNome = syntheticUser.Nome,
                                ValorTotal = 120m,
                                FormaPagamento = "PIX",
                                Status = "Concluida",
                                ItensQuantidade = 1
                            }
                        },
                        relatorioPath);
                    EnsureGeneratedFile(relatorioPath, "PDF de relatorio com marca configurada");
                    using (var relatorioPdf = PdfReader.Open(relatorioPath, PdfDocumentOpenMode.ReadOnly))
                    {
                        if (!string.Equals(relatorioPdf.Info.Author, loadedConfiguration.EffectiveCompanyName, StringComparison.Ordinal))
                        {
                            throw new InvalidOperationException("Relatorio PDF nao refletiu a empresa configurada nos metadados.");
                        }
                    }

                    App.Backups.CriarBackupManual(backupPath);
                    if (!App.Backups.VerificarBackup(backupPath))
                    {
                        throw new InvalidOperationException("Backup smoke nao passou na verificacao de integridade.");
                    }

                    produtoPosteriorAoBackup = CreatePersistedProdutoEstoqueSmoke(
                        "pos-backup",
                        quantidadeEstoque: 3,
                        quantidadeMinima: 1,
                        quantidadeMaxima: 10,
                        precoCompra: 7m,
                        semCodigoOperacional: false,
                        dataUltimaVenda: DateTime.Today,
                        totalVendas: 1,
                        vendasUltimoMes: 1);

                    window = new ConfiguracoesSistemaWindow(syntheticUser);
                    ShowWindowForInteraction(window);
                    SelectTabByHeader(window, "Comercial / Comprovante");

                    var companyText = FindElementByName<TextBox>(window, "CompanyDisplayNameTextBox")?.Text;
                    if (!string.Equals(companyText, smokeConfiguration.CompanyDisplayName, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Janela de configuracoes nao carregou o nome comercial persistido.");
                    }

                    SelectTabByHeader(window, "Multiusuario / Rede");
                    var printerCombo = FindElementByName<ComboBox>(window, "PdvPrinterComboBox")
                        ?? throw new InvalidOperationException("PdvPrinterComboBox nao foi localizado na tela de configuracoes.");
                    var printer = printerCombo.Items.OfType<PrinterDiagnosticInfo>().FirstOrDefault();
                    if (printer == null)
                    {
                        printer = new PrinterDiagnosticInfo
                        {
                            Name = $"Smoke Printer PDV {token[^6..]}",
                            DriverName = "Automacao",
                            PortName = "SMOKE:",
                            IsVirtual = true
                        };
                        printerCombo.ItemsSource = new List<PrinterDiagnosticInfo> { printer };
                    }

                    printerCombo.SelectedItem = printer;
                    var stationName = $"Caixa Smoke {token[^6..]}";
                    SetTextBoxValue(window, "StationNameTextBox", stationName);
                    SetTextBoxValue(window, "StationDescriptionTextBox", "Estacao de automacao para validar impressora preferencial do PDV.");
                    DefinirComboBoxPorTag(window, "StationTypeComboBox", "Caixa");
                    SetCheckBoxValue(window, "UseConfiguredPdvPrinterCheckBox", true);
                    ClickButton(window, "SalvarConfiguracoesEstacaoButton");

                    var stationSaved = StationService.GetConfiguration(App.RuntimeAppDataPath);
                    var printerStatus = FindElementByName<TextBlock>(window, "PdvPrinterStatusTextBlock")?.Text;
                    if (!stationSaved.UseConfiguredPdvPrinter ||
                        !string.Equals(stationSaved.PreferredPdvPrinterName, printer.Name, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(stationSaved.StationName, stationName, StringComparison.Ordinal) ||
                        stationSaved.StationType != StationType.Caixa ||
                        string.IsNullOrWhiteSpace(printerStatus) ||
                        !printerStatus.Contains("salva", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Configuracao de impressora preferencial do PDV por estacao nao foi persistida pela tela.");
                    }

                    SelectTabByHeader(window, "Backup");
                    SetTextBoxValue(window, "RestoreBackupPathTextBox", backupPath);
                    InvokeButtonHandler(window, "ValidarBackupRestauracaoButton_Click", null);

                    var restoreStatus = FindElementByName<TextBlock>(window, "RestoreBackupStatusTextBlock")?.Text;
                    if (string.IsNullOrWhiteSpace(restoreStatus) ||
                        !restoreStatus.Contains("Backup valido", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Janela de configuracoes nao validou o backup para restauracao segura.");
                    }

                    ClickButton(window, "RestoreBackupButton");
                    restoreStatus = FindElementByName<TextBlock>(window, "RestoreBackupStatusTextBlock")?.Text;
                    if (string.IsNullOrWhiteSpace(restoreStatus) ||
                        !restoreStatus.Contains("restaurado com sucesso", StringComparison.OrdinalIgnoreCase) ||
                        App.Repositories.Produtos.ObterPorId(produtoPosteriorAoBackup.Id) != null)
                    {
                        throw new InvalidOperationException("A restauracao controlada nao recuperou o estado anterior do banco isolado.");
                    }

                    SelectTabByHeader(window, "Banco de Dados");
                    ClickButton(window, "AtualizarInformacoesBancoButton");
                    var databaseType = FindElementByName<TextBlock>(window, "CurrentDatabaseTypeTextBlock")?.Text;
                    var databaseName = FindElementByName<TextBlock>(window, "CurrentDatabaseNameTextBlock")?.Text;
                    var databaseVersion = FindElementByName<TextBlock>(window, "CurrentDatabaseVersionTextBlock")?.Text;
                    var expectedDatabaseType = runtimeIsSqlServer ? "SQL Server" : "SQLite Local";
                    var expectedDatabaseName = runtimeIsSqlServer ? databaseSettings.SqlServerDatabase : Path.GetFileName(databaseSettings.SQLitePath);
                    if (!string.Equals(databaseType, expectedDatabaseType, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(databaseName, expectedDatabaseName, StringComparison.OrdinalIgnoreCase) ||
                        string.IsNullOrWhiteSpace(databaseVersion) ||
                        string.Equals(databaseVersion, "N/A", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("A tela de configuracoes nao confirmou corretamente o banco ativo.");
                    }

                    SelectTabByHeader(window, "Comercial / Comprovante");
                    InvokeButtonHandler(window, "GerarPreviaComprovanteButton_Click", null);
                    var previewText = FindElementByName<TextBlock>(window, "ReceiptPreviewTextBlock")?.Text;
                    if (string.IsNullOrWhiteSpace(previewText) ||
                        !previewText.Contains("Rodape smoke comprovante", StringComparison.Ordinal) ||
                        !previewText.Contains(Path.GetFileName(logoPath), StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Previa do comprovante nao exibiu a configuracao comercial.");
                    }
                }
                finally
                {
                    if (window?.IsVisible == true)
                    {
                        window.Close();
                    }

                    BusinessConfigurationService.Save(App.RuntimeAppDataPath, originalConfiguration);
                    StationService.SaveConfiguration(App.RuntimeAppDataPath, originalStationSnapshot);
                }
            });

            RunCheck(result, "Configuracoes:SistemaPersistenciaPermissoes", () =>
            {
                GarantirBancoIsoladoDoSmoke("configuracoes do sistema persistidas no banco");

                var service = new SystemConfigurationService(App.Database, _logger);
                var originalSystemConfiguration = service.LoadOrCreate(App.RuntimeAppDataPath);
                var originalBusinessConfiguration = BusinessConfigurationService.LoadOrCreateDefault(App.RuntimeAppDataPath, _logger);
                var themeService = new ThemeService();
                var originalTheme = themeService.GetCurrentTheme();
                ConfiguracoesSistemaWindow? window = null;

                try
                {
                    var token = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                    var adminPermission = new PermissionService(syntheticUser, _logger, App.Database);
                    var unauthorizedUser = CreateSyntheticUser("Vendedor", "Smoke Sem Configuracao");
                    var unauthorizedPermission = new PermissionService(unauthorizedUser, _logger, App.Database);
                    var configuration = new SystemConfiguration
                    {
                        CompanyDisplayName = $"Oficina Config Smoke {token[^6..]}",
                        CompanyLegalName = "Oficina Config Smoke LTDA",
                        CompanyDocument = "22.333.444/0001-55",
                        CompanyPhone = "(11) 97777-0000",
                        CompanyWhatsApp = "(11) 96666-0000",
                        CompanyAddress = "Rua Persistencia, 456",
                        PreferredTheme = nameof(AppTheme.Dark),
                        AutoBackupEnabled = false,
                        AutoBackupRetentionCopies = 17,
                        DefaultPrinterName = "Smoke Printer Operacional",
                        OsNumberPrefix = "OS-SMK-{yyyy}-",
                        OsNextNumber = 321,
                        OrcamentoNumberPrefix = "ORC-SMK-{yyyyMMdd}-",
                        OrcamentoNextNumber = 654,
                        PermissionPolicySummary = "Controle smoke por perfis.",
                        MessageTemplateOrcamento = "Smoke orcamento {Numero} para {Cliente}.",
                        MessageTemplateOrdemPronta = "Smoke OS {Numero} pronta para {Cliente}.",
                        MessageTemplateGarantia = "Smoke garantia de {DiasGarantia} dias.",
                        DefaultWarrantyDays = 120,
                        DefaultProductMarginPercent = 42.5m,
                        UpdatedBy = syntheticUser.Nome
                    };

                    var unauthorizedBlocked = false;
                    try
                    {
                        service.SaveAuthorized(configuration, unauthorizedUser, unauthorizedPermission);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        unauthorizedBlocked = true;
                    }

                    if (!unauthorizedBlocked)
                    {
                        throw new InvalidOperationException("Perfil sem SISTEMA_CONFIGURAR conseguiu alterar configuracoes do sistema.");
                    }

                    service.SaveAuthorized(configuration, syntheticUser, adminPermission);
                    var persisted = service.ReadPersistedValues();
                    foreach (var key in new[]
                    {
                        nameof(SystemConfiguration.CompanyDisplayName),
                        nameof(SystemConfiguration.CompanyWhatsApp),
                        nameof(SystemConfiguration.PreferredTheme),
                        nameof(SystemConfiguration.AutoBackupEnabled),
                        nameof(SystemConfiguration.OsNumberPrefix),
                        nameof(SystemConfiguration.OrcamentoNumberPrefix),
                        nameof(SystemConfiguration.MessageTemplateOrcamento),
                        nameof(SystemConfiguration.DefaultWarrantyDays),
                        nameof(SystemConfiguration.DefaultProductMarginPercent)
                    })
                    {
                        if (!persisted.ContainsKey(key))
                        {
                            throw new InvalidOperationException($"Configuracao '{key}' nao foi persistida na tabela ConfiguracoesSistema.");
                        }
                    }

                    using (var connection = App.Database.GetConnection())
                    {
                        connection.Open();
                        var osPolicy = SystemConfigurationService.ResolveOrdemServicoNumberingPolicy(connection, new DateTime(2026, 6, 11));
                        var orcamentoPolicy = SystemConfigurationService.ResolveOrcamentoNumberingPolicy(connection, new DateTime(2026, 6, 11));
                        if (!string.Equals(osPolicy.Prefix, "OS-SMK-2026-", StringComparison.Ordinal) ||
                            osPolicy.NextNumber != 321 ||
                            !string.Equals(orcamentoPolicy.Prefix, "ORC-SMK-20260611-", StringComparison.Ordinal) ||
                            orcamentoPolicy.NextNumber != 654)
                        {
                            throw new InvalidOperationException("Politica de numeracao persistida nao foi resolvida corretamente.");
                        }
                    }

                    window = new ConfiguracoesSistemaWindow(syntheticUser);
                    ShowWindowForInteraction(window);
                    SelectTabByHeader(window, "Operacao");

                    var osPrefixText = FindElementByName<TextBox>(window, "OsNumberPrefixTextBox")?.Text;
                    var margemText = FindElementByName<TextBox>(window, "DefaultProductMarginTextBox")?.Text;
                    var mensagemOrcamento = FindElementByName<TextBox>(window, "MessageTemplateOrcamentoTextBox")?.Text;
                    var autoBackup = FindElementByName<CheckBox>(window, "AutoBackupEnabledCheckBox")?.IsChecked;
                    if (!string.Equals(osPrefixText, configuration.OsNumberPrefix, StringComparison.Ordinal) ||
                        string.IsNullOrWhiteSpace(margemText) ||
                        !margemText.Contains("42", StringComparison.Ordinal) ||
                        mensagemOrcamento?.Contains("Smoke orcamento", StringComparison.Ordinal) != true ||
                        autoBackup != false)
                    {
                        throw new InvalidOperationException("A aba Operacao nao carregou as configuracoes persistidas no banco.");
                    }

                    DefinirComboBoxPorTag(window, "ThemeModeComboBox", "Light");
                    SetCheckBoxValue(window, "AutoBackupEnabledCheckBox", true);
                    SetTextBoxValue(window, "AutoBackupRetentionTextBox", "19");
                    SetTextBoxValue(window, "OsNumberPrefixTextBox", "OS-UI-{yyyy}-");
                    SetTextBoxValue(window, "OsNextNumberTextBox", "777");
                    SetTextBoxValue(window, "OrcamentoNumberPrefixTextBox", "ORC-UI-{yyyyMMdd}-");
                    SetTextBoxValue(window, "OrcamentoNextNumberTextBox", "888");
                    SetTextBoxValue(window, "DefaultWarrantyDaysTextBox", "180");
                    SetTextBoxValue(window, "DefaultProductMarginTextBox", "55.25");
                    SetTextBoxValue(window, "MessageTemplateOrcamentoTextBox", "UI orcamento {Numero}.");
                    SetTextBoxValue(window, "MessageTemplateOrdemProntaTextBox", "UI OS pronta {Numero}.");
                    SetTextBoxValue(window, "MessageTemplateGarantiaTextBox", "UI garantia {DiasGarantia}.");
                    ClickButton(window, "SalvarConfiguracoesOperacionaisButton");

                    var loaded = service.LoadOrCreate(App.RuntimeAppDataPath);
                    if (!loaded.AutoBackupEnabled ||
                        loaded.AutoBackupRetentionCopies != 19 ||
                        !string.Equals(loaded.OsNumberPrefix, "OS-UI-{yyyy}-", StringComparison.Ordinal) ||
                        loaded.OsNextNumber != 777 ||
                        !string.Equals(loaded.OrcamentoNumberPrefix, "ORC-UI-{yyyyMMdd}-", StringComparison.Ordinal) ||
                        loaded.OrcamentoNextNumber != 888 ||
                        loaded.DefaultWarrantyDays != 180 ||
                        loaded.DefaultProductMarginPercent != 55.25m ||
                        !string.Equals(loaded.MessageTemplateGarantia, "UI garantia {DiasGarantia}.", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException("Salvamento operacional pela tela nao persistiu corretamente no banco.");
                    }
                }
                finally
                {
                    if (window?.IsVisible == true)
                    {
                        window.Close();
                    }

                    service.SaveAuthorized(originalSystemConfiguration, syntheticUser, new PermissionService(syntheticUser, _logger, App.Database));
                    BusinessConfigurationService.Save(App.RuntimeAppDataPath, originalBusinessConfiguration);
                    themeService.ApplyTheme(originalTheme);
                }
            });
        }

    }
}
