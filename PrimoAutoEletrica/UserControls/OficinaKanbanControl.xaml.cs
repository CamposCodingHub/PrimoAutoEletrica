using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Views;
using PrimoAutoEletrica.Helpers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.UserControls
{
    public partial class OficinaKanbanControl : UserControl, INotifyPropertyChanged
    {
        private readonly OficinaProfissionalService _service;
        private OficinaKanbanCard? _selectedCard;

        public ObservableCollection<OficinaKanbanColumn> KanbanColumns { get; } = new();
        public ObservableCollection<ChecklistVisualItem> ChecklistPadrao { get; } = new();
        public ObservableCollection<OficinaTimelineItem> Timeline { get; } = new();
        public ObservableCollection<ModeloMensagemCliente> Mensagens { get; } = new();

        public string[] StatusOptions { get; } =
        {
            "Agendado",
            "Recebido",
            "Em diagnostico",
            "Aguardando aprovacao",
            "Aprovada",
            "Aguardando peca",
            "Em execucao",
            "Finalizada",
            "Aguardando pagamento",
            "Pronta para entrega",
            "Entregue",
            "Cancelada"
        };

        public OficinaKanbanCard? SelectedCard
        {
            get => _selectedCard;
            private set
            {
                if (ReferenceEquals(_selectedCard, value))
                {
                    return;
                }

                _selectedCard = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public OficinaKanbanControl()
        {
            InitializeComponent();
            _service = new OficinaProfissionalService();
            DataContext = this;
            Carregar();
        }

        private void Carregar(Guid? selecionarOrdemId = null)
        {
            var snapshot = _service.CriarSnapshot();

            KanbanColumns.Clear();
            foreach (var coluna in snapshot.Kanban)
            {
                KanbanColumns.Add(coluna);
            }

            ChecklistPadrao.Clear();
            foreach (var item in snapshot.ChecklistPadrao)
            {
                ChecklistPadrao.Add(item);
            }

            ResumoKanbanTextBlock.Text = snapshot.Resumo;

            var card = KanbanColumns
                .SelectMany(coluna => coluna.Cards)
                .FirstOrDefault(c => selecionarOrdemId.HasValue && c.Id == selecionarOrdemId.Value)
                ?? KanbanColumns.SelectMany(coluna => coluna.Cards).FirstOrDefault();

            SelecionarCard(card);
        }

        private void SelecionarCard(OficinaKanbanCard? card)
        {
            SelectedCard = card;
            Timeline.Clear();
            Mensagens.Clear();

            var timeline = _service.ObterTimeline(card?.ClienteId, card?.VeiculoId);
            foreach (var item in timeline)
            {
                Timeline.Add(item);
            }

            foreach (var mensagem in _service.CriarModelosMensagem(card))
            {
                Mensagens.Add(mensagem);
            }
        }

        private void AtualizarKanbanButton_Click(object sender, RoutedEventArgs e)
        {
            Carregar(SelectedCard?.Id);
        }

        private void SelecionarCardButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is OficinaKanbanCard card)
            {
                SelecionarCard(card);
            }
        }

        private void AbrirOsSelecionadaButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedCard == null)
            {
                ExibirMensagem("Selecione uma OS no Kanban primeiro.", "Kanban", MessageBoxImage.Information);
                return;
            }

            AbrirOrdem(SelectedCard.Id);
        }

        private void AvancarStatusCardButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is OficinaKanbanCard card)
            {
                try
                {
                    _service.AvancarStatusOrdem(card.Id);
                    Carregar(card.Id);
                }
                catch (Exception ex)
                {
                    ExibirMensagem($"Nao foi possivel avancar a OS: {ex.Message}", "Kanban", MessageBoxImage.Warning);
                }
            }
        }

        private void AlterarStatusSelecionadoButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedCard == null)
            {
                ExibirMensagem("Selecione uma OS no Kanban primeiro.", "Kanban", MessageBoxImage.Information);
                return;
            }

            if (NovoStatusComboBox.SelectedItem is not string status)
            {
                ExibirMensagem("Selecione o novo status.", "Kanban", MessageBoxImage.Information);
                return;
            }

            try
            {
                _service.AlterarStatusOrdem(SelectedCard.Id, status);
                Carregar(SelectedCard.Id);
            }
            catch (Exception ex)
            {
                ExibirMensagem($"Nao foi possivel alterar o status: {ex.Message}", "Kanban", MessageBoxImage.Warning);
            }
        }

        private void WhatsAppCardButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is OficinaKanbanCard card)
            {
                SelecionarCard(card);
                EnviarWhatsApp(card);
            }
        }

        private void CopiarMensagemButton_Click(object sender, RoutedEventArgs e)
        {
            var mensagem = Mensagens.FirstOrDefault()?.Mensagem;
            if (string.IsNullOrWhiteSpace(mensagem))
            {
                ExibirMensagem("Nao ha mensagem sugerida para copiar.", "Comunicacao", MessageBoxImage.Information);
                return;
            }

            Clipboard.SetText(mensagem);
            ExibirMensagem("Mensagem copiada para a area de transferencia.", "Comunicacao", MessageBoxImage.Information);
        }

        private void AbrirOrdem(Guid ordemId)
        {
            var ordem = App.Repositories.OrdensServico.ObterPorId(ordemId);
            if (ordem == null)
            {
                ExibirMensagem("Nao foi possivel localizar a OS selecionada.", "Kanban", MessageBoxImage.Warning);
                return;
            }

            var janela = new OrdemServicoWindow(App.Database, ordem);
            WindowOwnerHelper.ConfigureOwner(janela, this);

            if (App.IsAutomatedTestMode)
            {
                App.Logger.LogInfo($"Abertura da OS {ordem.Numero} validada em automacao pelo Kanban.");
                return;
            }

            if (janela.ShowDialog() == true)
            {
                Carregar(ordemId);
            }
        }

        private void EnviarWhatsApp(OficinaKanbanCard card)
        {
            var mensagem = Mensagens.FirstOrDefault()?.Mensagem
                ?? $"Ola, segue atualizacao da OS {card.Numero}.";

            try
            {
                var url = OficinaProfissionalService.CriarWhatsAppUrl(card.Telefone, mensagem);

                if (App.IsAutomatedTestMode)
                {
                    App.Logger.LogInfo($"WhatsApp do Kanban validado em automacao para OS {card.Numero}: {url}");
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ExibirMensagem($"Nao foi possivel abrir WhatsApp: {ex.Message}", "WhatsApp", MessageBoxImage.Warning);
            }
        }

        private static void ExibirMensagem(string mensagem, string titulo, MessageBoxImage imagem)
        {
            if (App.IsSmokeTestMode || App.IsAutomatedTestMode)
            {
                App.Logger.LogInfo($"Dialogo suprimido [{titulo}]: {mensagem}");
                return;
            }

            MessageBox.Show(mensagem, titulo, MessageBoxButton.OK, imagem);
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
