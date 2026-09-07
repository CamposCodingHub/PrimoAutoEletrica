using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.ViewModels
{
    /// <summary>
    /// ViewModel para o módulo de Autoeletricidade Técnica
    /// Gerencia roteiros de diagnóstico, biblioteca técnica e histórico de defeitos
    /// </summary>
    public partial class AutoEletricaTecnicaViewModel : BaseViewModel
    {
        private readonly AutoEletricaTecnicaService _service;
        private AutoEletricaTecnicaSnapshot _snapshot;

        [ObservableProperty]
        private ObservableCollection<object> roteirosDiagnostico;

        [ObservableProperty]
        private ObservableCollection<object> bibliotecaTecnica;

        [ObservableProperty]
        private ObservableCollection<object> defeitosRecorrentes;

        [ObservableProperty]
        private ObservableCollection<object> servicosTecnicos;

        [ObservableProperty]
        private ObservableCollection<object> sugestoesPecas;

        [ObservableProperty]
        private ObservableCollection<ProntuarioEletricoCampo> prontuarioVeiculo;

        [ObservableProperty]
        private string prontuarioVeiculoTexto;

        [ObservableProperty]
        private string resumoIndicadores;

        [ObservableProperty]
        private bool isCarregando;

        [ObservableProperty]
        private string mensagemErro;

        public AutoEletricaTecnicaViewModel()
        {
            _service = new AutoEletricaTecnicaService();
            _snapshot = new AutoEletricaTecnicaSnapshot();
            
            RoteirosDiagnostico = new ObservableCollection<object>();
            BibliotecaTecnica = new ObservableCollection<object>();
            DefeitosRecorrentes = new ObservableCollection<object>();
            ServicosTecnicos = new ObservableCollection<object>();
            SugestoesPecas = new ObservableCollection<object>();
            ProntuarioVeiculo = new ObservableCollection<ProntuarioEletricoCampo>();
        }

        [RelayCommand]
        public void CarregarDados()
        {
            try
            {
                IsCarregando = true;
                MensagemErro = string.Empty;

                _snapshot = _service.CriarSnapshot();

                // Limpar e recarregar coleções
                RoteirosDiagnostico.Clear();
                foreach (var item in _snapshot.RoteirosDiagnostico ?? new())
                {
                    RoteirosDiagnostico.Add(item);
                }

                BibliotecaTecnica.Clear();
                foreach (var item in _snapshot.BibliotecaTecnica ?? new())
                {
                    BibliotecaTecnica.Add(item);
                }

                DefeitosRecorrentes.Clear();
                foreach (var item in _snapshot.DefeitosRecorrentes ?? new())
                {
                    DefeitosRecorrentes.Add(item);
                }

                ServicosTecnicos.Clear();
                foreach (var item in _snapshot.ServicosTecnicos ?? new())
                {
                    ServicosTecnicos.Add(item);
                }

                SugestoesPecas.Clear();
                foreach (var item in _snapshot.SugestoesPecas ?? new())
                {
                    SugestoesPecas.Add(item);
                }

                CarregarProntuario();
                AtualizarResumo();
            }
            catch (Exception ex)
            {
                MensagemErro = $"Erro ao carregar dados: {ex.Message}";
                App.Logger?.LogError("Erro ao carregar Auto Elétrica Técnica", ex);
            }
            finally
            {
                IsCarregando = false;
            }
        }

        private void CarregarProntuario()
        {
            ProntuarioVeiculo.Clear();
            
            if (_snapshot.Prontuario == null)
            {
                ProntuarioVeiculoTexto = "Nenhum veículo encontrado para exibir o prontuário elétrico.";
                return;
            }

            ProntuarioVeiculoTexto =
                $"{_snapshot.Prontuario.Veiculo} | Placa {_snapshot.Prontuario.Placa} | Sistema {_snapshot.Prontuario.SistemaEletrico}";

            foreach (var campo in _snapshot.Prontuario.Campos ?? new())
            {
                ProntuarioVeiculo.Add(campo);
            }
        }

        private void AtualizarResumo()
        {
            var resumoPartes = new[]
            {
                $"Roteiros: {RoteirosDiagnostico.Count}",
                $"Itens na Biblioteca: {BibliotecaTecnica.Count}",
                $"Defeitos Registrados: {DefeitosRecorrentes.Count}",
                $"Serviços Técnicos: {ServicosTecnicos.Count}",
                $"Sugestões de Peças: {SugestoesPecas.Count}"
            };

            ResumoIndicadores = string.Join(" | ", resumoPartes);
        }

        [RelayCommand]
        public void SelecionarRoteiro(object roteiro)
        {
            // Implementar lógica de seleção de roteiro
            try
            {
                MensagemErro = string.Empty;
                CarregarProntuario();
            }
            catch (Exception ex)
            {
                MensagemErro = $"Erro ao selecionar roteiro: {ex.Message}";
                App.Logger?.LogError("Erro ao selecionar roteiro", ex);
            }
        }

        [RelayCommand]
        public void ExportarRelatorio()
        {
            try
            {
                MensagemErro = string.Empty;
                // Implementar exportação de relatório técnico
                var pdfService = new OrcamentoPdfService();
                // Adicionar lógica de exportação
            }
            catch (Exception ex)
            {
                MensagemErro = $"Erro ao exportar relatório: {ex.Message}";
                App.Logger?.LogError("Erro ao exportar relatório técnico", ex);
            }
        }
    }
}
