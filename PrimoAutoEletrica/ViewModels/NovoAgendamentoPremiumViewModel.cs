using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

using PrimoAutoEletrica.Helpers;
namespace PrimoAutoEletrica.ViewModels
{
    public class NovoAgendamentoPremiumViewModel : INotifyPropertyChanged
    {
        private readonly AgendamentoDatabaseService _agendamentoService;
        private string _clienteNome = string.Empty;
        private string _clienteTelefone = string.Empty;
        private string _clienteEmail = string.Empty;
        private string _clienteDocumento = string.Empty;
        private string _veiculoPlaca = string.Empty;
        private string _veiculoModelo = string.Empty;
        private string _veiculoMarca = string.Empty;
        private string _veiculoCor = string.Empty;
        private string _veiculoAno = string.Empty;
        private string _tipoServico = string.Empty;
        private string _prioridade = "Normal";
        private DateTime? _dataAgendamento = DateTime.Today;
        private string _horaInicio = string.Empty;
        private string _horaTermino = string.Empty;
        private decimal _valorEstimado = 0;
        private string _descricaoServico = string.Empty;
        private string _tecnicoNome = string.Empty;
        private string _tecnicoEspecialidade = string.Empty;
        private decimal _valorServico = 0;
        private string _formaPagamento = string.Empty;
        private decimal _desconto = 0;
        private decimal _valorFinal = 0;
        private string _observacoes = string.Empty;
        private string _notasInternas = string.Empty;

        public ObservableCollection<string> TiposServico { get; set; }
        public ObservableCollection<string> Prioridades { get; set; }
        public ObservableCollection<string> Tecnicos { get; set; }
        public ObservableCollection<string> FormasPagamento { get; set; }
        public ObservableCollection<TecnicoDisponivel> TecnicosDisponiveis { get; set; }
        public ObservableCollection<ChecklistItem> ChecklistItens { get; set; }

        public ICommand SalvarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand FecharCommand { get; }

        public NovoAgendamentoPremiumViewModel()
        {
            _agendamentoService = new AgendamentoDatabaseService();

            TiposServico = new ObservableCollection<string> { "Manutenção", "Reparo", "Diagnóstico", "Instalação", "Revisão", "Outro" };
            Prioridades = new ObservableCollection<string> { "Baixa", "Normal", "Alta", "Urgente" };
            Tecnicos = new ObservableCollection<string> { "João Silva", "Maria Santos", "Pedro Costa", "Ana Oliveira" };
            FormasPagamento = new ObservableCollection<string> { "Dinheiro", "Cartão de Crédito", "Cartão de Débito", "PIX", "Boleto" };

            TecnicosDisponiveis = new ObservableCollection<TecnicoDisponivel>
            {
                new TecnicoDisponivel { Nome = "João Silva", Especialidade = "Mecânica Geral", Disponivel = true },
                new TecnicoDisponivel { Nome = "Maria Santos", Especialidade = "Elétrica", Disponivel = true },
                new TecnicoDisponivel { Nome = "Pedro Costa", Especialidade = "Injeção Eletrônica", Disponivel = false },
                new TecnicoDisponivel { Nome = "Ana Oliveira", Especialidade = "Freios", Disponivel = true }
            };

            ChecklistItens = new ObservableCollection<ChecklistItem>
            {
                new ChecklistItem { Descricao = "Verificar nível de óleo", Concluido = false },
                new ChecklistItem { Descricao = "Verificar nível de água", Concluido = false },
                new ChecklistItem { Descricao = "Verificar pressão dos pneus", Concluido = false },
                new ChecklistItem { Descricao = "Verificar freios", Concluido = false },
                new ChecklistItem { Descricao = "Verificar bateria", Concluido = false },
                new ChecklistItem { Descricao = "Verificar luzes", Concluido = false }
            };

            SalvarCommand = new RelayCommand(Salvar);
            CancelarCommand = new RelayCommand(Cancelar);
            FecharCommand = new RelayCommand(Fechar);
        }

        public string ClienteNome
        {
            get => _clienteNome;
            set { _clienteNome = value; OnPropertyChanged(); }
        }

        public string ClienteTelefone
        {
            get => _clienteTelefone;
            set { _clienteTelefone = value; OnPropertyChanged(); }
        }

        public string ClienteEmail
        {
            get => _clienteEmail;
            set { _clienteEmail = value; OnPropertyChanged(); }
        }

        public string ClienteDocumento
        {
            get => _clienteDocumento;
            set { _clienteDocumento = value; OnPropertyChanged(); }
        }

        public string VeiculoPlaca
        {
            get => _veiculoPlaca;
            set { _veiculoPlaca = value; OnPropertyChanged(); }
        }

        public string VeiculoModelo
        {
            get => _veiculoModelo;
            set { _veiculoModelo = value; OnPropertyChanged(); }
        }

        public string VeiculoMarca
        {
            get => _veiculoMarca;
            set { _veiculoMarca = value; OnPropertyChanged(); }
        }

        public string VeiculoCor
        {
            get => _veiculoCor;
            set { _veiculoCor = value; OnPropertyChanged(); }
        }

        public string VeiculoAno
        {
            get => _veiculoAno;
            set { _veiculoAno = value; OnPropertyChanged(); }
        }

        public string TipoServico
        {
            get => _tipoServico;
            set { _tipoServico = value; OnPropertyChanged(); }
        }

        public string Prioridade
        {
            get => _prioridade;
            set { _prioridade = value; OnPropertyChanged(); }
        }

        public DateTime? DataAgendamento
        {
            get => _dataAgendamento;
            set { _dataAgendamento = value; OnPropertyChanged(); }
        }

        public string HoraInicio
        {
            get => _horaInicio;
            set { _horaInicio = value; OnPropertyChanged(); }
        }

        public string HoraTermino
        {
            get => _horaTermino;
            set { _horaTermino = value; OnPropertyChanged(); }
        }

        public decimal ValorEstimado
        {
            get => _valorEstimado;
            set { _valorEstimado = value; OnPropertyChanged(); CalcularValorFinal(); }
        }

        public string DescricaoServico
        {
            get => _descricaoServico;
            set { _descricaoServico = value; OnPropertyChanged(); }
        }

        public string TecnicoNome
        {
            get => _tecnicoNome;
            set { _tecnicoNome = value; OnPropertyChanged(); }
        }

        public string TecnicoEspecialidade
        {
            get => _tecnicoEspecialidade;
            set { _tecnicoEspecialidade = value; OnPropertyChanged(); }
        }

        public decimal ValorServico
        {
            get => _valorServico;
            set { _valorServico = value; OnPropertyChanged(); CalcularValorFinal(); }
        }

        public string FormaPagamento
        {
            get => _formaPagamento;
            set { _formaPagamento = value; OnPropertyChanged(); }
        }

        public decimal Desconto
        {
            get => _desconto;
            set { _desconto = value; OnPropertyChanged(); CalcularValorFinal(); }
        }

        public decimal ValorFinal
        {
            get => _valorFinal;
            set { _valorFinal = value; OnPropertyChanged(); }
        }

        public string Observacoes
        {
            get => _observacoes;
            set { _observacoes = value; OnPropertyChanged(); }
        }

        public string NotasInternas
        {
            get => _notasInternas;
            set { _notasInternas = value; OnPropertyChanged(); }
        }

        private void CalcularValorFinal()
        {
            ValorFinal = ValorServico - Desconto;
            if (ValorFinal < 0) ValorFinal = 0;
        }

        private void Salvar()
        {
            try
            {
                var agendamento = new Agendamento
                {
                    Id = Guid.NewGuid(),
                    Numero = $"AG-{DateTime.Now:yyyyMMddHHmm}",
                    ClienteNome = ClienteNome,
                    ClienteTelefone = ClienteTelefone,
                    ClienteEmail = ClienteEmail,
                    ClienteDocumento = ClienteDocumento,
                    VeiculoPlaca = VeiculoPlaca,
                    VeiculoModelo = VeiculoModelo,
                    VeiculoMarca = VeiculoMarca,
                    VeiculoCor = VeiculoCor,
                    VeiculoAno = VeiculoAno,
                    TipoServico = TipoServico,
                    Prioridade = Prioridade,
                    DataAgendamento = DataAgendamento ?? DateTime.Today,
                    HoraInicio = TimeSpan.TryParse(HoraInicio, out var hi) ? DateTime.Today.Add(hi) : null,
                    HoraTermino = TimeSpan.TryParse(HoraTermino, out var ht) ? DateTime.Today.Add(ht) : null,
                    ValorEstimado = ValorEstimado,
                    DescricaoServico = DescricaoServico,
                    TecnicoNome = TecnicoNome,
                    TecnicoEspecialidade = TecnicoEspecialidade,
                    ValorReal = ValorEstimado,
                    FormaPagamento = FormaPagamento,
                    Observacoes = Observacoes,
                    Status = "Agendado",
                    DataCriacao = DateTime.Now
                };

                _agendamentoService.AdicionarAgendamento(agendamento);
                MessageBox.Show(UiText.T("AppointmentCreated"), UiText.T("Success"), MessageBoxButton.OK, MessageBoxImage.Information);
                Fechar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar agendamento: {ex.Message}", UiText.T("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancelar()
        {
            Fechar();
        }

        private void Fechar()
        {
            Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is NovoAgendamentoPremiumWindow)?.Close();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TecnicoDisponivel
    {
        public string Nome { get; set; } = string.Empty;
        public string Especialidade { get; set; } = string.Empty;
        public bool Disponivel { get; set; }
    }

    public class ChecklistItem
    {
        public string Descricao { get; set; } = string.Empty;
        public bool Concluido { get; set; }
    }
}

