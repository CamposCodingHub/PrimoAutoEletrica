using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace PrimoAutoEletrica.Views
{
    public sealed class AtalhoTecladoInfo
    {
        public string Atalho { get; init; } = string.Empty;
        public string Acao { get; init; } = string.Empty;
        public string Modulo { get; init; } = string.Empty;
    }

    public partial class AtalhosTecladoWindow : Window
    {
        public AtalhosTecladoWindow()
        {
            InitializeComponent();
            AtalhosItemsControl.ItemsSource = CriarListaImplementada();
        }

        public static IReadOnlyList<AtalhoTecladoInfo> CriarListaImplementada()
        {
            return new List<AtalhoTecladoInfo>
            {
                new() { Atalho = "Ctrl + K", Acao = "Abrir paleta de comandos", Modulo = "Global" },
                new() { Atalho = "Ctrl + Shift + /", Acao = "Mostrar esta ajuda de atalhos", Modulo = "Global" },
                new() { Atalho = "F1", Acao = "Abrir Ajuda", Modulo = "Global" },
                new() { Atalho = "F2", Acao = "Ir para Orcamentos", Modulo = "Global" },
                new() { Atalho = "F3", Acao = "Ir para Ordens de Servico", Modulo = "Global" },
                new() { Atalho = "F4", Acao = "Ir para Clientes", Modulo = "Global" },
                new() { Atalho = "F5", Acao = "Atualizar modulo atual", Modulo = "Global" },
                new() { Atalho = "F6", Acao = "Ir para Estoque", Modulo = "Global" },
                new() { Atalho = "F12", Acao = "Abrir Configuracoes", Modulo = "Global" },
                new() { Atalho = "Ctrl + N", Acao = "Novo registro", Modulo = "Clientes / Veiculos / OS / Orcamentos" },
                new() { Atalho = "Ctrl + F", Acao = "Focar pesquisa", Modulo = "Clientes / Veiculos / OS / Orcamentos" },
                new() { Atalho = "Enter", Acao = "Abrir / editar item selecionado", Modulo = "Listas dos modulos" },
                new() { Atalho = "Duplo clique", Acao = "Abrir / editar item", Modulo = "Listas dos modulos" },
                new() { Atalho = "Ctrl + S", Acao = "Salvar", Modulo = "Janelas de OS e Orcamento" },
                new() { Atalho = "Esc", Acao = "Cancelar / fechar dialogo", Modulo = "Janelas e paleta" },
            };
        }

        private void Fechar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }
    }
}
