using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Printing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.ViewModels
{
    /// <summary>
    /// ViewModel para gerenciamento de impressoras
    /// Permite diagnosticar, configurar e testar impressoras
    /// </summary>
    public partial class PrinterManagementViewModel : BaseViewModel
    {
        private readonly PrinterDiagnosticsService _printerService;
        private PrinterDiagnosticsSnapshot _currentSnapshot;

        [ObservableProperty]
        private ObservableCollection<PrinterDiagnosticInfo> impressorasDisponiveis;

        [ObservableProperty]
        private PrinterDiagnosticInfo impressoraSelecionada;

        [ObservableProperty]
        private string statusImpressora;

        [ObservableProperty]
        private bool isCarregando;

        [ObservableProperty]
        private string mensagemStatus;

        [ObservableProperty]
        private string mensagemErro;

        [ObservableProperty]
        private bool impressoraDisponivelParaPrinting;

        public PrinterManagementViewModel()
        {
            _printerService = new PrinterDiagnosticsService();
            _currentSnapshot = new PrinterDiagnosticsSnapshot();
            
            ImpressorasDisponiveis = new ObservableCollection<PrinterDiagnosticInfo>();
            ImpressoraSelecionada = new PrinterDiagnosticInfo();
        }

        [RelayCommand]
        public void AtualizarListaImpressoras()
        {
            try
            {
                IsCarregando = true;
                MensagemErro = string.Empty;

                _currentSnapshot = _printerService.CaptureSnapshot();

                if (!string.IsNullOrEmpty(_currentSnapshot.CaptureError))
                {
                    MensagemErro = $"Erro ao capturar snapshot de impressoras: {_currentSnapshot.CaptureError}";
                    MensagemStatus = "Falha ao atualizar";
                    return;
                }

                ImpressorasDisponiveis.Clear();
                foreach (var printer in _currentSnapshot.Printers)
                {
                    ImpressorasDisponiveis.Add(printer);
                }

                MensagemStatus = $"{ImpressorasDisponiveis.Count} impressora(s) encontrada(s)";

                if (ImpressorasDisponiveis.Count > 0)
                {
                    ImpressoraSelecionada = ImpressorasDisponiveis[0];
                    AtualizarStatusImpressora();
                }
            }
            catch (Exception ex)
            {
                MensagemErro = $"Erro ao atualizar lista: {ex.Message}";
                App.Logger?.LogError("Erro ao atualizar impressoras", ex);
            }
            finally
            {
                IsCarregando = false;
            }
        }

        [RelayCommand]
        public void SelecionarImpressora(PrinterDiagnosticInfo printer)
        {
            if (printer != null)
            {
                ImpressoraSelecionada = printer;
                AtualizarStatusImpressora();
            }
        }

        private void AtualizarStatusImpressora()
        {
            if (ImpressoraSelecionada == null)
            {
                StatusImpressora = "Nenhuma impressora selecionada";
                ImpressoraDisponivelParaPrinting = false;
                return;
            }

            var status = new List<string>();
            status.Add($"Nome: {ImpressoraSelecionada.Name}");
            status.Add($"Driver: {ImpressoraSelecionada.DriverName ?? "N/A"}");
            status.Add($"Porta: {ImpressoraSelecionada.PortName ?? "N/A"}");
            status.Add($"Padrão: {(ImpressoraSelecionada.IsDefault ? "Sim" : "Não")}");
            status.Add($"Compartilhada: {(ImpressoraSelecionada.IsShared ? "Sim" : "Não")}");
            status.Add($"Offline: {(ImpressoraSelecionada.IsOffline ? "Sim (⚠️ OFFLINE)" : "Não (✓ Online)")}");

            StatusImpressora = string.Join("\n", status);
            ImpressoraDisponivelParaPrinting = !ImpressoraSelecionada.IsOffline;

            MensagemStatus = ImpressoraDisponivelParaPrinting 
                ? "Impressora pronta para uso" 
                : "Impressora offline - não pode ser usada";
        }

        [RelayCommand]
        public void TestarImpressora()
        {
            if (ImpressoraSelecionada == null)
            {
                MensagemErro = "Selecione uma impressora primeiro";
                return;
            }

            try
            {
                IsCarregando = true;
                MensagemErro = string.Empty;

                if (ImpressoraSelecionada.IsOffline)
                {
                    MensagemErro = "Impressora está offline. Verifique a conexão.";
                    return;
                }

                // Teste básico: verificar se conseguimos criar um PrintQueue
                try
                {
                    using (var printServer = new LocalPrintServer())
                    {
                        var printerQueues = printServer.GetPrintQueues();
                        var targetQueue = printerQueues.FirstOrDefault(q =>
                            q.FullName == ImpressoraSelecionada.Name || q.Name == ImpressoraSelecionada.Name);

                        if (targetQueue != null)
                        {
                            targetQueue.Refresh();
                            MensagemStatus = $"Teste OK: Impressora '{ImpressoraSelecionada.Name}' respondeu corretamente.";
                        }
                        else
                        {
                            MensagemErro = "Não foi possível acessar a fila de impressão";
                        }
                    }
                }
                catch (Exception ex)
                {
                    MensagemErro = $"Erro ao testar impressora: {ex.Message}";
                }
            }
            finally
            {
                IsCarregando = false;
            }
        }

        [RelayCommand]
        public void DefinirComoImpressoraParpadrao()
        {
            if (ImpressoraSelecionada == null)
            {
                MensagemErro = "Selecione uma impressora primeiro";
                return;
            }

            try
            {
                IsCarregando = true;
                MensagemErro = string.Empty;

                using (var printServer = new LocalPrintServer())
                {
                    var printerQueues = printServer.GetPrintQueues();
                    var targetQueue = printerQueues.FirstOrDefault(q =>
                        q.FullName == ImpressoraSelecionada.Name || q.Name == ImpressoraSelecionada.Name);

                    if (targetQueue != null)
                    {
                        printServer.DefaultPrintQueue = targetQueue;
                        MensagemStatus = $"Impressora '{ImpressoraSelecionada.Name}' definida como padrão.";
                        
                        // Atualizar snapshot para refletir a mudança
                        AtualizarListaImpressoras();
                    }
                    else
                    {
                        MensagemErro = "Não foi possível definir como padrão";
                    }
                }
            }
            catch (Exception ex)
            {
                MensagemErro = $"Erro ao definir impressora padrão: {ex.Message}";
                App.Logger?.LogError("Erro ao definir impressora padrão", ex);
            }
            finally
            {
                IsCarregando = false;
            }
        }

        [RelayCommand]
        public void ExportarRelatorioImpressoras()
        {
            try
            {
                IsCarregando = true;
                MensagemErro = string.Empty;

                var caminhoExporte = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"Diagnostico_Impressoras_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                );

                var linhas = new List<string>
                {
                    "=== DIAGNÓSTICO DE IMPRESSORAS ===",
                    $"Data/Hora: {DateTime.Now:dd/MM/yyyy HH:mm:ss}",
                    $"Total de Impressoras: {ImpressorasDisponiveis.Count}",
                    ""
                };

                foreach (var printer in ImpressorasDisponiveis)
                {
                    linhas.Add($"--- {printer.Name} ---");
                    linhas.Add($"Driver: {printer.DriverName ?? "N/A"}");
                    linhas.Add($"Porta: {printer.PortName ?? "N/A"}");
                    linhas.Add($"Padrão: {(printer.IsDefault ? "Sim" : "Não")}");
                    linhas.Add($"Compartilhada: {(printer.IsShared ? "Sim" : "Não")}");
                    linhas.Add($"Status: {(printer.IsOffline ? "OFFLINE" : "ONLINE")}");
                    linhas.Add("");
                }

                System.IO.File.WriteAllLines(caminhoExporte, linhas);
                MensagemStatus = $"Relatório exportado: {caminhoExporte}";
            }
            catch (Exception ex)
            {
                MensagemErro = $"Erro ao exportar relatório: {ex.Message}";
                App.Logger?.LogError("Erro ao exportar relatório de impressoras", ex);
            }
            finally
            {
                IsCarregando = false;
            }
        }
    }
}
