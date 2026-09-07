using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PrimoAutoEletrica.UserControls
{
    /// <summary>
    /// Central de Ajuda integrada do PrimoAutoEletrica.
    /// Exibe tutoriais, FAQ, atalhos e informações de suporte.
    /// </summary>
    public partial class HelpControl : UserControl
    {
        private readonly Dictionary<string, HelpTopic> _topics;

        public HelpControl()
        {
            InitializeComponent();
            _topics = InitializeHelpTopics();
        }

        private void HelpTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem item)
            {
                var tag = item.Tag?.ToString();
                if (!string.IsNullOrEmpty(tag) && _topics.TryGetValue(tag, out var topic))
                {
                    DisplayTopic(topic);
                }
            }
        }

        /// <summary>
        /// Navega para um tópico específico por tag (pode ser chamado externamente via F1).
        /// </summary>
        public void NavigateToTopic(string topicTag)
        {
            if (!string.IsNullOrEmpty(topicTag) && _topics.TryGetValue(topicTag, out var topic))
            {
                DisplayTopic(topic);
            }
        }

        private void DisplayTopic(HelpTopic topic)
        {
            ContentArea.Children.Clear();

            // Título
            ContentArea.Children.Add(CreateTextBlock(topic.Title, 24, FontWeights.Bold, "PrimaryTextBrush", 0, 0, 0, 16));

            // Seções de conteúdo
            foreach (var section in topic.Sections)
            {
                if (!string.IsNullOrEmpty(section.Heading))
                {
                    ContentArea.Children.Add(CreateTextBlock(section.Heading, 16, FontWeights.SemiBold, "PrimaryTextBrush", 0, 16, 0, 8));
                }

                if (!string.IsNullOrEmpty(section.Content))
                {
                    ContentArea.Children.Add(CreateTextBlock(section.Content, 13, FontWeights.Normal, "SecondaryTextBrush", 0, 0, 0, 8, true));
                }

                // Bullet points
                if (section.BulletPoints != null)
                {
                    foreach (var bullet in section.BulletPoints)
                    {
                        ContentArea.Children.Add(CreateTextBlock($"  •  {bullet}", 13, FontWeights.Normal, "PrimaryTextBrush", 8, 2, 0, 2));
                    }
                }

                // Tabela de atalhos
                if (section.KeyboardShortcuts != null)
                {
                    var grid = CreateShortcutsGrid(section.KeyboardShortcuts);
                    grid.Margin = new Thickness(0, 8, 0, 8);
                    ContentArea.Children.Add(grid);
                }
            }

            // Dica final
            if (!string.IsNullOrEmpty(topic.Tip))
            {
                var tipBorder = new Border
                {
                    Background = FindBrush("InfoCardBackgroundBrush"),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16),
                    Margin = new Thickness(0, 16, 0, 0)
                };

                tipBorder.Child = CreateTextBlock($"💡 {topic.Tip}", 13, FontWeights.Normal, "InfoBrush", 0, 0, 0, 0);
                ContentArea.Children.Add(tipBorder);
            }

            // Tópicos relacionados
            if (topic.RelatedTopics?.Length > 0)
            {
                ContentArea.Children.Add(CreateTextBlock("📚 Veja também:", 14, FontWeights.SemiBold, "PrimaryTextBrush", 0, 20, 0, 8));

                foreach (var related in topic.RelatedTopics)
                {
                    if (_topics.TryGetValue(related, out var relatedTopic))
                    {
                        var link = CreateTextBlock($"  → {relatedTopic.Title}", 13, FontWeights.Normal, "PrimaryBrush", 0, 4, 0, 2);
                        link.Cursor = System.Windows.Input.Cursors.Hand;
                        link.Tag = related;
                        link.MouseLeftButtonDown += (s, e) =>
                        {
                            if (s is TextBlock tb && tb.Tag is string tag)
                            {
                                NavigateToTopic(tag);
                            }
                        };
                        ContentArea.Children.Add(link);
                    }
                }
            }
        }

        private TextBlock CreateTextBlock(string text, double fontSize, FontWeight fontWeight, string brushKey,
            double left = 0, double top = 0, double right = 0, double bottom = 0, bool wrap = true)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = fontWeight,
                Foreground = FindBrush(brushKey),
                TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
                LineHeight = fontSize * 1.6,
                Margin = new Thickness(left, top, right, bottom)
            };
        }

        private Grid CreateShortcutsGrid(Dictionary<string, string> shortcuts)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            int row = 0;

            // Header
            grid.RowDefinitions.Add(new RowDefinition());
            var headerKey = CreateTextBlock("Atalho", 12, FontWeights.SemiBold, "MutedTextBrush", 12, 6, 0, 6);
            var headerAction = CreateTextBlock("Ação", 12, FontWeights.SemiBold, "MutedTextBrush", 12, 6, 0, 6);
            Grid.SetRow(headerKey, row);
            Grid.SetColumn(headerKey, 0);
            Grid.SetRow(headerAction, row);
            Grid.SetColumn(headerAction, 1);
            grid.Children.Add(headerKey);
            grid.Children.Add(headerAction);
            row++;

            foreach (var shortcut in shortcuts)
            {
                grid.RowDefinitions.Add(new RowDefinition());

                var bg = row % 2 == 0 ? FindBrush("SurfaceAltBrush") : FindBrush("CardBackgroundBrush");

                var keyBorder = new Border { Background = bg, Padding = new Thickness(12, 6, 0, 6) };
                var keyText = new TextBlock
                {
                    Text = shortcut.Key,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    FontFamily = new FontFamily("Consolas"),
                    Foreground = FindBrush("PrimaryBrush")
                };
                keyBorder.Child = keyText;

                var actionBorder = new Border { Background = bg, Padding = new Thickness(12, 6, 0, 6) };
                var actionText = new TextBlock
                {
                    Text = shortcut.Value,
                    FontSize = 13,
                    Foreground = FindBrush("PrimaryTextBrush")
                };
                actionBorder.Child = actionText;

                Grid.SetRow(keyBorder, row);
                Grid.SetColumn(keyBorder, 0);
                Grid.SetRow(actionBorder, row);
                Grid.SetColumn(actionBorder, 1);
                grid.Children.Add(keyBorder);
                grid.Children.Add(actionBorder);
                row++;
            }

            return grid;
        }

        private Brush FindBrush(string key)
        {
            try
            {
                return (Brush)FindResource(key);
            }
            catch
            {
                return Brushes.Gray;
            }
        }

        // =================================================================
        // DADOS DOS TÓPICOS DE AJUDA
        // =================================================================

        private static Dictionary<string, HelpTopic> InitializeHelpTopics()
        {
            return new Dictionary<string, HelpTopic>
            {
                ["primeiro-acesso"] = new HelpTopic
                {
                    Title = "🎯 Primeiro Acesso",
                    Sections = new[]
                    {
                        new HelpSection { Heading = "Bem-vindo ao PrimoAutoEletrica!", Content = "Siga os passos abaixo para começar a usar o sistema." },
                        new HelpSection { Heading = "1. Login", BulletPoints = new[] { "Abra o aplicativo", "Digite seu usuario (login)", "Digite sua senha", "Clique em 'Entrar'" } },
                        new HelpSection { Heading = "2. Configuracao Inicial", BulletPoints = new[] { "Va em Configuracoes (engrenagem no canto superior direito)", "Defina sua filial", "Configure os dados da empresa" } },
                        new HelpSection { Heading = "Proximas Etapas", BulletPoints = new[] { "Crie seu primeiro cliente", "Registre um veiculo", "Crie um orcamento" } }
                    },
                    Tip = "Use o Help (F1) para orientacoes sobre cada tela!",
                    RelatedTopics = new[] { "criar-cliente", "registrar-veiculo" }
                },

                ["criar-cliente"] = new HelpTopic
                {
                    Title = "👥 Como Criar um Novo Cliente",
                    Sections = new[]
                    {
                        new HelpSection { Heading = "Passo a Passo", Content = "Siga as instrucoes abaixo para cadastrar um novo cliente no sistema." },
                        new HelpSection { Heading = "1. Acesse o modulo 'Clientes'", Content = "Clique em 'Clientes' no menu lateral esquerdo." },
                        new HelpSection { Heading = "2. Clique em '+ Novo Cliente'", Content = "Botao verde no topo a direita da tela." },
                        new HelpSection
                        {
                            Heading = "3. Preencha os Dados",
                            Content = "Campos obrigatorios:",
                            BulletPoints = new[] { "Nome Completo", "CPF ou CNPJ", "Telefone OU Email" }
                        },
                        new HelpSection
                        {
                            Content = "Campos opcionais:",
                            BulletPoints = new[] { "Endereco", "Veiculos", "Observacoes" }
                        },
                        new HelpSection { Heading = "4. Salve", Content = "Clique em 'Salvar' (Ctrl+S)." }
                    },
                    Tip = "Voce pode importar multiplos clientes via Excel!",
                    RelatedTopics = new[] { "registrar-veiculo", "criar-orcamento" }
                },

                ["registrar-veiculo"] = new HelpTopic
                {
                    Title = "🚗 Como Registrar um Veículo",
                    Sections = new[]
                    {
                        new HelpSection { Heading = "Vincule um veiculo a um cliente", Content = "Cada veiculo deve estar associado a um cliente previamente cadastrado." },
                        new HelpSection { Heading = "1. Acesse o modulo 'Veiculos'", Content = "No menu lateral, clique em 'Veiculos'." },
                        new HelpSection { Heading = "2. Clique em '+ Novo Veiculo'", Content = "Preencha placa, marca, modelo, ano e quilometragem." },
                        new HelpSection { Heading = "3. Associe ao Cliente", Content = "Selecione o cliente proprietario do veiculo." },
                        new HelpSection { Heading = "4. Salve", Content = "Clique em 'Salvar' para concluir o cadastro." }
                    },
                    Tip = "O historico de servicos do veiculo ficara disponivel no perfil do cliente.",
                    RelatedTopics = new[] { "criar-cliente", "criar-orcamento" }
                },

                ["criar-orcamento"] = new HelpTopic
                {
                    Title = "📋 Como Criar um Orçamento",
                    Sections = new[]
                    {
                        new HelpSection { Heading = "Crie orcamentos profissionais", Content = "Orcamentos podem ser convertidos em ordens de servico apos aprovacao do cliente." },
                        new HelpSection { Heading = "1. Acesse 'Orcamentos'", Content = "Clique em 'Orcamentos' no menu lateral." },
                        new HelpSection { Heading = "2. Clique em '+ Novo Orcamento'", Content = "Selecione o cliente e o veiculo." },
                        new HelpSection { Heading = "3. Adicione Servicos e Pecas", BulletPoints = new[] { "Pesquise servicos no catalogo", "Adicione pecas do estoque", "Ajuste quantidades e valores" } },
                        new HelpSection { Heading = "4. Revise e Salve", Content = "Confira o total, aplique desconto se necessario, e salve." },
                        new HelpSection { Heading = "5. Envie ao Cliente", Content = "Gere o PDF e envie por email ou WhatsApp." }
                    },
                    Tip = "Orcamentos aprovados podem ser convertidos em OS com um clique!",
                    RelatedTopics = new[] { "modulo-orcamentos", "modulo-os" }
                },

                ["gerar-relatorio"] = new HelpTopic
                {
                    Title = "📊 Como Gerar Relatórios",
                    Sections = new[]
                    {
                        new HelpSection { Heading = "Relatorios disponiveis", Content = "O sistema oferece diversos tipos de relatorios para analise do negocio." },
                        new HelpSection { Heading = "Tipos de Relatorio", BulletPoints = new[] { "Faturamento (diario, semanal, mensal, anual)", "Servicos realizados", "Estoque (posicao, movimentacao)", "Clientes (cadastros, historico)", "Financeiro (contas a pagar/receber)", "Comissoes de funcionarios" } },
                        new HelpSection { Heading = "Como Gerar", BulletPoints = new[] { "Acesse 'Relatorios' no menu lateral", "Selecione o tipo de relatorio", "Defina o periodo (datas)", "Clique em 'Gerar Relatorio'", "Exporte como PDF ou Excel" } }
                    },
                    Tip = "Use Ctrl+P para pre-visualizar antes de imprimir.",
                    RelatedTopics = new[] { "modulo-relatorios", "modulo-financeiro" }
                },

                ["modulo-dashboard"] = new HelpTopic
                {
                    Title = "📊 Dashboard",
                    Sections = new[]
                    {
                        new HelpSection { Heading = "Visao geral do negocio", Content = "O Dashboard exibe indicadores chave de performance (KPIs) em tempo real." },
                        new HelpSection { Heading = "Indicadores Disponiveis", BulletPoints = new[] { "Faturamento do mes", "Ordens de servico abertas", "Orcamentos pendentes", "Destaques do sistema" } },
                        new HelpSection { Heading = "Atualizacao", Content = "Clique no botao 'Atualizar' para recarregar os dados. Os indicadores sao carregados automaticamente ao abrir o Dashboard." }
                    },
                    Tip = "O Dashboard e a tela inicial apos o login."
                },

                ["modulo-clientes"] = new HelpTopic
                {
                    Title = "👥 Módulo de Clientes",
                    Sections = new[]
                    {
                        new HelpSection { Heading = "Gerenciamento de Clientes", Content = "Cadastre, edite e consulte o historico de todos os seus clientes." },
                        new HelpSection { Heading = "Funcionalidades", BulletPoints = new[] { "Cadastro completo (nome, CPF/CNPJ, contato, endereco)", "Historico de servicos por cliente", "Veiculos vinculados", "Orcamentos e ordens de servico", "Exportacao para Excel/PDF", "Busca rapida por nome, CPF ou telefone" } }
                    },
                    RelatedTopics = new[] { "criar-cliente", "registrar-veiculo" }
                },

                ["modulo-orcamentos"] = new HelpTopic
                {
                    Title = "📋 Módulo de Orçamentos",
                    Sections = new[]
                    {
                        new HelpSection { Heading = "Gestao de Orcamentos", Content = "Crie, gerencie e acompanhe orcamentos de servicos." },
                        new HelpSection { Heading = "Fluxo do Orcamento", BulletPoints = new[] { "Pendente → Enviado → Aprovado → Convertido em OS", "Pendente → Enviado → Recusado", "Qualquer status → Cancelado" } },
                        new HelpSection { Heading = "Funcionalidades", BulletPoints = new[] { "Catalogo de servicos e pecas", "Calculos automaticos de mao de obra", "Desconto por item ou total", "Geracao de PDF profissional", "Envio por email e WhatsApp", "Conversao em Ordem de Servico" } }
                    },
                    RelatedTopics = new[] { "criar-orcamento", "modulo-os" }
                },

                ["modulo-os"] = new HelpTopic
                {
                    Title = "🔧 Ordens de Serviço",
                    Sections = new[]
                    {
                        new HelpSection { Heading = "Gestao de Ordens de Servico", Content = "Acompanhe o andamento dos servicos desde a abertura ate a entrega ao cliente." },
                        new HelpSection { Heading = "Status da OS", BulletPoints = new[] { "Aberta → Em andamento → Aguardando peca → Finalizada → Entregue", "Quadro Kanban visual para acompanhamento" } },
                        new HelpSection { Heading = "Funcionalidades", BulletPoints = new[] { "Atribuicao de mecanico responsavel", "Registro de pecas utilizadas", "Fotos antes/depois do servico", "Historico de alteracoes", "Impressao de ordem para oficina" } }
                    },
                    RelatedTopics = new[] { "modulo-orcamentos", "modulo-estoque" }
                },

                ["modulo-estoque"] = new HelpTopic
                {
                    Title = "📦 Módulo de Estoque",
                    Sections = new[]
                    {
                        new HelpSection { Heading = "Controle de Estoque", Content = "Gerencie produtos, pecas e insumos do seu estoque." },
                        new HelpSection { Heading = "Funcionalidades", BulletPoints = new[] { "Cadastro de produtos com codigo de barras", "Controle de entrada e saida", "Alerta de estoque minimo", "Importacao de NF-e (XML)", "Historico de movimentacoes", "Etiquetas para impressao", "Catalogo de pecas automotivas" } }
                    },
                    RelatedTopics = new[] { "modulo-os" }
                },

                ["modulo-financeiro"] = new HelpTopic
                {
                    Title = "💰 Módulo Financeiro",
                    Sections = new[]
                    {
                        new HelpSection { Heading = "Gestao Financeira", Content = "Controle contas a pagar, contas a receber e fluxo de caixa." },
                        new HelpSection { Heading = "Funcionalidades", BulletPoints = new[] { "Contas a Pagar (fornecedores, despesas)", "Contas a Receber (clientes, servicos)", "Fluxo de caixa (entradas vs saidas)", "Conciliacao bancaria", "DRE simplificado", "Exportacao contabil" } }
                    },
                    RelatedTopics = new[] { "modulo-relatorios", "gerar-relatorio" }
                },

                ["modulo-relatorios"] = new HelpTopic
                {
                    Title = "📈 Módulo de Relatórios",
                    Sections = new[]
                    {
                        new HelpSection { Heading = "Central de Relatorios", Content = "Gere relatorios detalhados para analise do negocio." },
                        new HelpSection { Heading = "Tipos Disponiveis", BulletPoints = new[] { "Faturamento por periodo", "Servicos mais realizados", "Pecas mais vendidas", "Ranking de clientes", "Performance de funcionarios", "Analise de estoque", "Exportacao para PDF e Excel" } }
                    },
                    RelatedTopics = new[] { "gerar-relatorio", "modulo-financeiro" }
                },

                ["modulo-pdv"] = new HelpTopic
                {
                    Title = "🛒 PDV (Ponto de Venda)",
                    Sections = new[]
                    {
                        new HelpSection { Heading = "Ponto de Venda", Content = "Realize vendas rapidas de pecas e produtos diretamente no balcao." },
                        new HelpSection { Heading = "Funcionalidades", BulletPoints = new[] { "Venda rapida com leitor de codigo de barras", "Busca de produtos por nome ou codigo", "Multiplas formas de pagamento", "Impressao de comprovante", "Abertura e fechamento de caixa", "Vendas suspensas" } }
                    },
                    RelatedTopics = new[] { "modulo-estoque" }
                },

                ["atalhos-teclado"] = new HelpTopic
                {
                    Title = "⌨️ Atalhos de Teclado",
                    Sections = new[]
                    {
                        new HelpSection
                        {
                            Heading = "Atalhos Globais",
                            KeyboardShortcuts = new Dictionary<string, string>
                            {
                                ["Ctrl+N"] = "Novo Registro",
                                ["Ctrl+S"] = "Salvar",
                                ["Ctrl+D"] = "Deletar",
                                ["Ctrl+K"] = "Busca Global",
                                ["F1"] = "Help do Modulo",
                                ["F5"] = "Atualizar",
                                ["Alt+F4"] = "Sair"
                            }
                        },
                        new HelpSection
                        {
                            Heading = "Atalhos de Navegacao",
                            KeyboardShortcuts = new Dictionary<string, string>
                            {
                                ["Ctrl+1"] = "Dashboard",
                                ["Ctrl+2"] = "Clientes",
                                ["Ctrl+3"] = "Orcamentos",
                                ["Ctrl+4"] = "Ordens de Servico",
                                ["Ctrl+5"] = "Estoque",
                                ["Ctrl+6"] = "Financeiro"
                            }
                        }
                    },
                    Tip = "Customize atalhos em Configuracoes > Atalhos de Teclado."
                },

                ["faq"] = new HelpTopic
                {
                    Title = "❓ Perguntas Frequentes",
                    Sections = new[]
                    {
                        new HelpSection { Heading = "Como exportar dados para Excel?", Content = "Abra o modulo desejado > Clique em 'Exportar' (icone de planilha) > Selecione o periodo > O Excel sera baixado automaticamente." },
                        new HelpSection { Heading = "Posso usar em dois computadores?", Content = "Sim! Desde que estejam conectados a mesma rede com o banco de dados compartilhado (SQL Server)." },
                        new HelpSection { Heading = "Como fazer backup dos dados?", Content = "Configuracoes > Backup > 'Fazer Backup Agora'. O backup automatico e feito diariamente." },
                        new HelpSection { Heading = "Esqueci minha senha. O que faco?", Content = "Contate o administrador do sistema. Ele pode redefinir sua senha em Configuracoes > Usuarios." },
                        new HelpSection { Heading = "O programa esta lento. Como otimizar?", BulletPoints = new[] { "Limpe cache em Configuracoes > Limpeza", "Verifique a conexao de rede", "Reinicie o programa" } },
                        new HelpSection { Heading = "Como importar NF-e?", Content = "Acesse Estoque > Importar NF-e > Selecione o arquivo XML > Confira os produtos > Confirme a importacao." },
                        new HelpSection { Heading = "Como adicionar usuarios?", Content = "Apenas Administradores: Configuracoes > Usuarios > + Novo Usuario > Defina perfil e permissoes." }
                    }
                },

                ["suporte"] = new HelpTopic
                {
                    Title = "📞 Suporte Técnico",
                    Sections = new[]
                    {
                        new HelpSection { Heading = "Canais de Suporte", Content = "Estamos aqui para ajudar! Escolha o canal mais conveniente:" },
                        new HelpSection { Heading = "📧 Email", Content = "suporte@primoauto.com\nTempo de resposta: ate 24h uteis." },
                        new HelpSection { Heading = "💬 WhatsApp", Content = "Disponivel de segunda a sexta, das 9h as 18h." },
                        new HelpSection { Heading = "🐛 Relatar um Bug", BulletPoints = new[] { "Anote os passos para reproduzir o problema", "Capture uma screenshot (Print Screen)", "Envie por email com o titulo 'BUG: [descricao]'", "Inclua a versao do sistema (menu Sobre)" } },
                        new HelpSection { Heading = "💡 Sugerir Melhoria", Content = "Envie suas sugestoes por email ou WhatsApp. As melhores sugestoes viram novas funcionalidades!" }
                    }
                }
            };
        }
    }

    // =================================================================
    // MODELOS DE DADOS PARA TÓPICOS DE AJUDA
    // =================================================================

    /// <summary>
    /// Representa um tópico de ajuda com título, seções de conteúdo e metadados.
    /// </summary>
    public class HelpTopic
    {
        public string Title { get; set; } = string.Empty;
        public HelpSection[] Sections { get; set; } = Array.Empty<HelpSection>();
        public string? Tip { get; set; }
        public string[]? RelatedTopics { get; set; }
    }

    /// <summary>
    /// Representa uma seção dentro de um tópico de ajuda.
    /// </summary>
    public class HelpSection
    {
        public string? Heading { get; set; }
        public string? Content { get; set; }
        public string[]? BulletPoints { get; set; }
        public Dictionary<string, string>? KeyboardShortcuts { get; set; }
    }
}
