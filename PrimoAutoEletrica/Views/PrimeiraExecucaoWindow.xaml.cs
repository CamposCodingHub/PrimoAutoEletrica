using PrimoAutoEletrica.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using PrimoAutoEletrica.Helpers;
namespace PrimoAutoEletrica.Views
{
    public partial class PrimeiraExecucaoWindow : Window
    {
        private readonly LoggerService _logger;
        private int _currentStep = 1;
        private const int TotalSteps = 7;

        public PrimeiraExecucaoWindow()
        {
            InitializeComponent();

            _logger = App.Logger;

            UpdateStepIndicators();
            ShowStep(_currentStep);
        }

        private void UpdateStepIndicators()
        {
            var ellipses = new[] { Step1Ellipse, Step2Ellipse, Step3Ellipse, Step4Ellipse, Step5Ellipse, Step6Ellipse, Step7Ellipse };

            for (int i = 0; i < ellipses.Length; i++)
            {
                if (i + 1 <= _currentStep)
                {
                    ellipses[i].Fill = new SolidColorBrush(Color.FromRgb(59, 130, 246)); // Blue
                }
                else
                {
                    ellipses[i].Fill = new SolidColorBrush(Color.FromRgb(229, 231, 235)); // Gray
                }
            }

            AnteriorButton.IsEnabled = _currentStep > 1;
        }

        private void ShowStep(int step)
        {
            StepContentPanel.Children.Clear();

            switch (step)
            {
                case 1:
                    ShowStep1();
                    break;
                case 2:
                    ShowStep2();
                    break;
                case 3:
                    ShowStep3();
                    break;
                case 4:
                    ShowStep4();
                    break;
                case 5:
                    ShowStep5();
                    break;
                case 6:
                    ShowStep6();
                    break;
                case 7:
                    ShowStep7();
                    break;
            }

            UpdateStepIndicators();
        }

        private void ShowStep1()
        {
            SubHeaderTextBlock.Text = "Bem-vindo ao Primo Auto Eletrica. Vamos configurar o sistema.";

            var panel = new StackPanel
            {
                Margin = new System.Windows.Thickness(0, 8, 0, 0)
            };

            var title = new TextBlock
            {
                Text = "Tipo de Instalação",
                Style = (Style)Resources["ModalSectionTitle"],
                FontSize = 20
            };

            var description = new TextBlock
            {
                Text = "Selecione o tipo de instalação para este computador.",
                Style = (Style)Resources["ModalSubheaderText"],
                Margin = new System.Windows.Thickness(0, 8, 0, 20),
                TextWrapping = TextWrapping.Wrap
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });

            var localPanel = new StackPanel { Margin = new System.Windows.Thickness(0, 0, 12, 0) };
            var localRadio = new RadioButton
            {
                Content = "Local",
                GroupName = "TipoInstalacao",
                IsChecked = true,
                Margin = new System.Windows.Thickness(0, 0, 0, 8)
            };
            var localDesc = new TextBlock
            {
                Text = "Banco de dados local. Ideal para uso individual ou testes.",
                Style = (Style)Resources["ModalSubheaderText"],
                TextWrapping = TextWrapping.Wrap
            };
            localPanel.Children.Add(localRadio);
            localPanel.Children.Add(localDesc);

            var estacaoPanel = new StackPanel { Margin = new System.Windows.Thickness(12, 0, 12, 0) };
            var estacaoRadio = new RadioButton
            {
                Content = "Estação Cliente",
                GroupName = "TipoInstalacao",
                Margin = new System.Windows.Thickness(0, 0, 0, 8)
            };
            var estacaoDesc = new TextBlock
            {
                Text = "Conectado a servidor SQL Server. Para uso em rede.",
                Style = (Style)Resources["ModalSubheaderText"],
                TextWrapping = TextWrapping.Wrap
            };
            estacaoPanel.Children.Add(estacaoRadio);
            estacaoPanel.Children.Add(estacaoDesc);

            var servidorPanel = new StackPanel { Margin = new System.Windows.Thickness(12, 0, 0, 0) };
            var servidorRadio = new RadioButton
            {
                Content = "Servidor",
                GroupName = "TipoInstalacao",
                Margin = new System.Windows.Thickness(0, 0, 0, 8)
            };
            var servidorDesc = new TextBlock
            {
                Text = "Hospeda SQL Server e banco central.",
                Style = (Style)Resources["ModalSubheaderText"],
                TextWrapping = TextWrapping.Wrap
            };
            servidorPanel.Children.Add(servidorRadio);
            servidorPanel.Children.Add(servidorDesc);

            Grid.SetColumn(localPanel, 0);
            Grid.SetColumn(estacaoPanel, 1);
            Grid.SetColumn(servidorPanel, 2);

            grid.Children.Add(localPanel);
            grid.Children.Add(estacaoPanel);
            grid.Children.Add(servidorPanel);

            panel.Children.Add(title);
            panel.Children.Add(description);
            panel.Children.Add(grid);

            StepContentPanel.Children.Add(panel);
        }

        private void ShowStep2()
        {
            SubHeaderTextBlock.Text = "Configuração do Banco de Dados";

            var panel = new StackPanel
            {
                Margin = new System.Windows.Thickness(0, 8, 0, 0)
            };

            var title = new TextBlock
            {
                Text = "Tipo de Banco de Dados",
                Style = (Style)Resources["ModalSectionTitle"],
                FontSize = 20
            };

            var description = new TextBlock
            {
                Text = "Selecione o tipo de banco de dados a ser usado.",
                Style = (Style)Resources["ModalSubheaderText"],
                Margin = new System.Windows.Thickness(0, 8, 0, 20),
                TextWrapping = TextWrapping.Wrap
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });

            var sqlitePanel = new StackPanel { Margin = new System.Windows.Thickness(0, 0, 12, 0) };
            var sqliteRadio = new RadioButton
            {
                Content = "SQLite Local",
                GroupName = "TipoBanco",
                IsChecked = true,
                Margin = new System.Windows.Thickness(0, 0, 0, 8)
            };
            var sqliteDesc = new TextBlock
            {
                Text = "Banco de dados local em arquivo. Ideal para desenvolvimento e uso individual.",
                Style = (Style)Resources["ModalSubheaderText"],
                TextWrapping = TextWrapping.Wrap
            };
            sqlitePanel.Children.Add(sqliteRadio);
            sqlitePanel.Children.Add(sqliteDesc);

            var sqlServerPanel = new StackPanel { Margin = new System.Windows.Thickness(12, 0, 0, 0) };
            var sqlServerRadio = new RadioButton
            {
                Content = "SQL Server",
                GroupName = "TipoBanco",
                Margin = new System.Windows.Thickness(0, 0, 0, 8)
            };
            var sqlServerDesc = new TextBlock
            {
                Text = "Banco de dados SQL Server. Ideal para produção e multiusuário.",
                Style = (Style)Resources["ModalSubheaderText"],
                TextWrapping = TextWrapping.Wrap
            };
            sqlServerPanel.Children.Add(sqlServerRadio);
            sqlServerPanel.Children.Add(sqlServerDesc);

            Grid.SetColumn(sqlitePanel, 0);
            Grid.SetColumn(sqlServerPanel, 1);

            grid.Children.Add(sqlitePanel);
            grid.Children.Add(sqlServerPanel);

            panel.Children.Add(title);
            panel.Children.Add(description);
            panel.Children.Add(grid);

            StepContentPanel.Children.Add(panel);
        }

        private void ShowStep3()
        {
            SubHeaderTextBlock.Text = "Dados de Conexão";

            var panel = new StackPanel
            {
                Margin = new System.Windows.Thickness(0, 8, 0, 0)
            };

            var title = new TextBlock
            {
                Text = "Configuração de Conexão SQL Server (se aplicável)",
                Style = (Style)Resources["ModalSectionTitle"],
                FontSize = 20
            };

            var description = new TextBlock
            {
                Text = "Configure os parâmetros de conexão com o SQL Server. Se escolheu SQLite, pule este passo.",
                Style = (Style)Resources["ModalSubheaderText"],
                Margin = new System.Windows.Thickness(0, 8, 0, 20),
                TextWrapping = TextWrapping.Wrap
            };

            panel.Children.Add(title);
            panel.Children.Add(description);

            StepContentPanel.Children.Add(panel);
        }

        private void ShowStep4()
        {
            SubHeaderTextBlock.Text = "Criar Administrador";

            var panel = new StackPanel
            {
                Margin = new System.Windows.Thickness(0, 8, 0, 0)
            };

            var title = new TextBlock
            {
                Text = "Criar Usuário Administrador",
                Style = (Style)Resources["ModalSectionTitle"],
                FontSize = 20
            };

            var description = new TextBlock
            {
                Text = "Crie o primeiro usuário administrador do sistema.",
                Style = (Style)Resources["ModalSubheaderText"],
                Margin = new System.Windows.Thickness(0, 8, 0, 20),
                TextWrapping = TextWrapping.Wrap
            };

            panel.Children.Add(title);
            panel.Children.Add(description);

            StepContentPanel.Children.Add(panel);
        }

        private void ShowStep5()
        {
            SubHeaderTextBlock.Text = "Configurar Backups";

            var panel = new StackPanel
            {
                Margin = new System.Windows.Thickness(0, 8, 0, 0)
            };

            var title = new TextBlock
            {
                Text = "Configuração de Backup",
                Style = (Style)Resources["ModalSectionTitle"],
                FontSize = 20
            };

            var description = new TextBlock
            {
                Text = "Configure a pasta de backup e a frequência de backups automáticos.",
                Style = (Style)Resources["ModalSubheaderText"],
                Margin = new System.Windows.Thickness(0, 8, 0, 20),
                TextWrapping = TextWrapping.Wrap
            };

            panel.Children.Add(title);
            panel.Children.Add(description);

            StepContentPanel.Children.Add(panel);
        }

        private void ShowStep6()
        {
            SubHeaderTextBlock.Text = "Configurar Estação";

            var panel = new StackPanel
            {
                Margin = new System.Windows.Thickness(0, 8, 0, 0)
            };

            var title = new TextBlock
            {
                Text = "Identificação da Estação",
                Style = (Style)Resources["ModalSectionTitle"],
                FontSize = 20
            };

            var description = new TextBlock
            {
                Text = "Configure a identificação desta estação para uso em ambiente multiusuário.",
                Style = (Style)Resources["ModalSubheaderText"],
                Margin = new System.Windows.Thickness(0, 8, 0, 20),
                TextWrapping = TextWrapping.Wrap
            };

            panel.Children.Add(title);
            panel.Children.Add(description);

            StepContentPanel.Children.Add(panel);
        }

        private void ShowStep7()
        {
            SubHeaderTextBlock.Text = "Teste Final";

            var panel = new StackPanel
            {
                Margin = new System.Windows.Thickness(0, 8, 0, 0)
            };

            var title = new TextBlock
            {
                Text = "Validação da Configuração",
                Style = (Style)Resources["ModalSectionTitle"],
                FontSize = 20
            };

            var description = new TextBlock
            {
                Text = "Revise a configuração e clique em Concluir para iniciar o sistema.",
                Style = (Style)Resources["ModalSubheaderText"],
                Margin = new System.Windows.Thickness(0, 8, 0, 20),
                TextWrapping = TextWrapping.Wrap
            };

            var summary = new TextBlock
            {
                Text = "Resumo da Configuração:\n\n" +
                       "Tipo de Instalação: Local\n" +
                       "Tipo de Banco: SQLite\n" +
                       "Usuário Administrador: A ser criado\n" +
                       "Backup: Configurado\n" +
                       "Estação: Configurada",
                Margin = new System.Windows.Thickness(0, 16, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };

            panel.Children.Add(title);
            panel.Children.Add(description);
            panel.Children.Add(summary);

            StepContentPanel.Children.Add(panel);

            ProximoButton.Content = "Concluir";
        }

        private void AnteriorButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep > 1)
            {
                _currentStep--;
                ShowStep(_currentStep);
            }
        }

        private void ProximoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep < TotalSteps)
            {
                _currentStep++;
                ShowStep(_currentStep);
            }
            else
            {
                FinalizarConfiguracao();
            }
        }

        private void FinalizarConfiguracao()
        {
            try
            {
                _logger.LogInfo("Configuração inicial concluída com sucesso.");

                MessageBox.Show(
                    "Configuração inicial concluída com sucesso!\n\nO sistema será iniciado agora.",
                    "Configuração Concluída",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao finalizar configuração inicial.", ex);
                MessageBox.Show(
                    $"Erro ao finalizar configuração:\n{ex.Message}",
                    UiText.T("Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
