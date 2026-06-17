using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PrimoAutoEletrica.Views
{
    public partial class VisualizarVeiculoWindow : Window
    {
        private readonly Veiculo _veiculo;
        private readonly DatabaseService _databaseService;

        public VisualizarVeiculoWindow(Veiculo veiculo, DatabaseService databaseService)
        {
            InitializeComponent();
            _veiculo = veiculo;
            _databaseService = databaseService;

            RecarregarTela();
        }

        private void RecarregarTela()
        {
            CarregarDadosVeiculo();
            CarregarAlertas();
            CarregarProntuarioEletrico();
            CarregarDefeitosDiagnosticos();
            CarregarPecasAplicadas();
            CarregarHistoricoOS();
            CarregarAgendamentos();
            CarregarOrcamentos();
        }

        private void CarregarDadosVeiculo()
        {
            PlacaHeaderText.Text = string.IsNullOrWhiteSpace(_veiculo.Placa) ? "-" : _veiculo.Placa.ToUpperInvariant();
            VeiculoHeaderText.Text = VeiculoProfileService.MontarDescricao(_veiculo.Marca, _veiculo.Modelo, _veiculo.Ano);

            MarcaText.Text = Exibir(_veiculo.Marca);
            ModeloText.Text = Exibir(_veiculo.Modelo);
            AnoText.Text = Exibir(_veiculo.Ano);
            CorText.Text = Exibir(_veiculo.Cor);
            PlacaText.Text = Exibir(_veiculo.Placa?.ToUpperInvariant());
            ChassiText.Text = Exibir(_veiculo.Chassi);
            RenavamText.Text = Exibir(_veiculo.Renavam);
            TipoText.Text = Exibir(_veiculo.TipoVeiculo);
            SistemaText.Text = Exibir(_veiculo.SistemaEletrico);
            MotorText.Text = Exibir(_veiculo.Motor);
            CombustivelText.Text = Exibir(_veiculo.Combustivel);
            QuilometragemText.Text = _veiculo.Quilometragem > 0 ? $"{_veiculo.Quilometragem:N0} km" : "-";
            BateriaPrincipalText.Text = Exibir(_veiculo.BateriaPrincipal);
            BateriaAuxiliarText.Text = Exibir(_veiculo.BateriaAuxiliar);
            AlternadorText.Text = Exibir(_veiculo.Alternador);
            MotorPartidaText.Text = Exibir(_veiculo.MotorPartida);
            HistoricoTecnicoText.Text = ExibirBloco(_veiculo.HistoricoTecnico, "Sem historico tecnico registrado.");
            ProblemaRecorrenteText.Text = ExibirBloco(_veiculo.ProblemaRecorrente, "Sem problema recorrente informado.");
            ObservacoesRecorrentesText.Text = ExibirBloco(_veiculo.ObservacoesEletricasRecorrentes, "Sem observacoes recorrentes.");
            ObservacaoTecnicoText.Text = ExibirBloco(_veiculo.ObservacaoImportanteTecnico, "Sem observacao critica.");
            ObservacoesText.Text = ExibirBloco(_veiculo.Observacoes, "Sem observacoes gerais.");

            var foto = VeiculoMediaService.TryCreatePreviewSource(_veiculo.ImagemUrl);
            FotoPreviewImage.Source = foto;
            FotoPreviewImage.Visibility = foto == null ? Visibility.Collapsed : Visibility.Visible;
            FotoPlaceholderText.Visibility = foto == null ? Visibility.Visible : Visibility.Collapsed;

            var documento = VeiculoMediaService.TryCreatePreviewSource(_veiculo.DocumentoImagemUrl);
            DocumentoPreviewImage.Source = documento;
            DocumentoPreviewImage.Visibility = documento == null ? Visibility.Collapsed : Visibility.Visible;
            DocumentoPlaceholderText.Visibility = documento == null ? Visibility.Visible : Visibility.Collapsed;

            if (_veiculo.ClienteId.HasValue)
            {
                var cliente = App.Repositories.Clientes.ObterPorId(_veiculo.ClienteId.Value);
                ProprietarioText.Text = cliente?.Nome ?? "Cliente nao encontrado";
            }
            else
            {
                ProprietarioText.Text = "Sem proprietario vinculado";
            }

            ResumoVeiculoText.Text =
                $"Tipo {Exibir(_veiculo.TipoVeiculo)} | Sistema {Exibir(_veiculo.SistemaEletrico)} | " +
                $"Motor {Exibir(_veiculo.Motor)} | Combustivel {Exibir(_veiculo.Combustivel)}";
        }

        private void CarregarAlertas()
        {
            AlertasPanel.Children.Clear();

            AdicionarAlertaData("Retorno recomendado", _veiculo.RetornoRecomendadoEm, "PrimaryBrush");
            AdicionarAlertaData("Garantia", _veiculo.GarantiaValidaAte, "SuccessBrush");
            AdicionarAlertaData("Proxima revisao", _veiculo.ProximaRevisaoEm, "WarningBrush");

            if (!string.IsNullOrWhiteSpace(_veiculo.ProblemaRecorrente))
            {
                AlertasPanel.Children.Add(CriarCardResumo(
                    "Problema recorrente",
                    _veiculo.ProblemaRecorrente,
                    "DangerBrush"));
            }

            if (AlertasPanel.Children.Count == 0)
            {
                AlertasPanel.Children.Add(CriarEstadoVazio("Nenhum alerta operacional ativo."));
            }
        }

        private void AdicionarAlertaData(string titulo, DateTime? data, string brushKey)
        {
            if (!data.HasValue)
            {
                return;
            }

            var hoje = DateTime.Today;
            var diferenca = (data.Value.Date - hoje).Days;
            var subtitulo = diferenca switch
            {
                < 0 => $"Vencido ha {Math.Abs(diferenca)} dia(s)",
                0 => "Vence hoje",
                1 => "Vence amanha",
                _ => $"Programado para {data.Value:dd/MM/yyyy}"
            };

            AlertasPanel.Children.Add(CriarCardResumo(titulo, subtitulo, brushKey));
        }

        private void CarregarHistoricoOS()
        {
            HistoricoOSPanel.Children.Clear();

            var ordens = ObterOrdensDoVeiculo()
                .Take(6)
                .ToList();

            if (ordens.Count == 0)
            {
                HistoricoOSPanel.Children.Add(CriarEstadoVazio("Nenhuma OS vinculada ao veiculo."));
                return;
            }

            foreach (var os in ordens)
            {
                var total = Math.Max(0, os.Itens.Sum(i => i.Total) - os.Desconto);
                var resumo = $"{os.Status} | {os.DataAbertura:dd/MM/yyyy} | {total:C}";
                HistoricoOSPanel.Children.Add(CriarCardResumo(os.Numero, resumo, "PrimaryBrush"));
            }
        }

        private void CarregarAgendamentos()
        {
            AgendamentosPanel.Children.Clear();

            var agendamentos = new AgendamentoDatabaseService()
                .ObterTodosAgendamentos()
                .Where(a => a.VeiculoId == _veiculo.Id ||
                            string.Equals(CadastroValidationHelper.NormalizarPlaca(a.VeiculoPlaca), CadastroValidationHelper.NormalizarPlaca(_veiculo.Placa), StringComparison.Ordinal))
                .OrderByDescending(a => a.DataAgendamento)
                .Take(5)
                .ToList();

            if (agendamentos.Count == 0)
            {
                AgendamentosPanel.Children.Add(CriarEstadoVazio("Nenhum agendamento relacionado."));
                return;
            }

            foreach (var agendamento in agendamentos)
            {
                var resumo = $"{agendamento.Status} | {agendamento.DataAgendamento:dd/MM/yyyy} | {agendamento.TipoServico}";
                AgendamentosPanel.Children.Add(CriarCardResumo(agendamento.Numero, resumo, "InfoBrush"));
            }
        }

        private void CarregarOrcamentos()
        {
            OrcamentosPanel.Children.Clear();

            var ordensIds = ObterOrdensDoVeiculo()
                .Select(os => os.Id)
                .ToHashSet();

            var orcamentos = new OrcamentoDatabaseService()
                .ObterTodosOrcamentos()
                .Where(o =>
                    (_veiculo.ClienteId.HasValue && o.ClienteId == _veiculo.ClienteId) ||
                    (o.OrdemServicoId.HasValue && ordensIds.Contains(o.OrdemServicoId.Value)))
                .OrderByDescending(o => o.DataCriacao)
                .GroupBy(o => o.Id)
                .Select(g => g.First())
                .Take(5)
                .ToList();

            if (orcamentos.Count == 0)
            {
                OrcamentosPanel.Children.Add(CriarEstadoVazio("Nenhum orcamento relacionado ao proprietario ou a OS do veiculo."));
                return;
            }

            foreach (var orcamento in orcamentos)
            {
                var origem = orcamento.OrdemServicoId.HasValue && ordensIds.Contains(orcamento.OrdemServicoId.Value)
                    ? "Vinculo direto por OS"
                    : "Relacionamento por cliente";
                var resumo = $"{orcamento.Status} | {orcamento.Total:C} | {origem}";
                OrcamentosPanel.Children.Add(CriarCardResumo(orcamento.Numero, resumo, "WarningBrush"));
            }
        }

        private void CarregarProntuarioEletrico()
        {
            ProntuarioEletricoPanel.Children.Clear();

            AdicionarProntuario("Sistema", $"{Exibir(_veiculo.SistemaEletrico)} | {Exibir(_veiculo.Combustivel)}");
            AdicionarProntuario("Bateria principal", Exibir(_veiculo.BateriaPrincipal));
            AdicionarProntuario("Bateria auxiliar", Exibir(_veiculo.BateriaAuxiliar));
            AdicionarProntuario("Bateria instalada", Exibir(_veiculo.BateriaInstalada));
            AdicionarProntuario("Marca/amperagem", string.Join(" | ", new[]
            {
                Exibir(_veiculo.BateriaMarca),
                Exibir(_veiculo.BateriaAmperagem),
                _veiculo.BateriaDataInstalacao.HasValue ? $"Instalada em {_veiculo.BateriaDataInstalacao:dd/MM/yyyy}" : "-"
            }.Where(valor => valor != "-")));
            AdicionarProntuario("Alternador", Exibir(_veiculo.Alternador));
            AdicionarProntuario("Motor de partida", Exibir(_veiculo.MotorPartida));
            AdicionarProntuario("Tensao em repouso", Exibir(_veiculo.TesteTensaoRepouso));
            AdicionarProntuario("Tensao na partida", Exibir(_veiculo.TesteTensaoPartida));
            AdicionarProntuario("Carga do alternador", Exibir(_veiculo.TesteCargaAlternador));
            AdicionarProntuario("Corrente de fuga", Exibir(_veiculo.CorrenteFuga));
            AdicionarProntuario("Aterramentos", Exibir(_veiculo.EstadoAterramentos));
            AdicionarProntuario("Chicotes reparados", Exibir(_veiculo.ChicotesReparados));
            AdicionarProntuario("Fusiveis substituidos", Exibir(_veiculo.FusiveisSubstituidos));
            AdicionarProntuario("Reles substituidos", Exibir(_veiculo.RelesSubstituidos));
            AdicionarProntuario("Lampadas substituidas", Exibir(_veiculo.LampadasSubstituidas));
            AdicionarProntuario("Acessorios instalados", Exibir(_veiculo.AcessoriosInstalados));
            AdicionarProntuario("Observacoes tecnicas", Exibir(_veiculo.ObservacoesTecnicasEletricas));
            AdicionarProntuario("Fotos tecnicas", Exibir(_veiculo.FotosTecnicas));

            if (ProntuarioEletricoPanel.Children.Count == 0)
            {
                ProntuarioEletricoPanel.Children.Add(CriarEstadoVazio("Sem dados eletricos registrados."));
            }
        }

        private void AdicionarProntuario(string titulo, string valor)
        {
            if (string.IsNullOrWhiteSpace(valor) || valor == "-")
            {
                return;
            }

            ProntuarioEletricoPanel.Children.Add(CriarCardResumo(titulo, valor, "InfoBrush"));
        }

        private void CarregarDefeitosDiagnosticos()
        {
            DefeitosDiagnosticosPanel.Children.Clear();

            AdicionarDefeito("Problema recorrente", _veiculo.ProblemaRecorrente);
            AdicionarDefeito("Observacoes recorrentes", _veiculo.ObservacoesEletricasRecorrentes);
            AdicionarDefeito("Historico tecnico", _veiculo.HistoricoTecnico);

            foreach (var ordem in ObterOrdensDoVeiculo().Take(8))
            {
                AdicionarDefeito($"OS {ordem.Numero} - defeito", ordem.ProblemaRelatado);
                AdicionarDefeito($"OS {ordem.Numero} - diagnostico", string.Join(" | ", new[]
                {
                    ordem.Diagnostico,
                    ordem.DiagnosticoInicial,
                    ordem.DiagnosticoFinal
                }.Where(valor => !string.IsNullOrWhiteSpace(valor))));
            }

            if (DefeitosDiagnosticosPanel.Children.Count == 0)
            {
                DefeitosDiagnosticosPanel.Children.Add(CriarEstadoVazio("Nenhum defeito ou diagnostico vinculado ao veiculo."));
            }
        }

        private void AdicionarDefeito(string titulo, string? descricao)
        {
            if (string.IsNullOrWhiteSpace(descricao))
            {
                return;
            }

            DefeitosDiagnosticosPanel.Children.Add(CriarCardResumo(titulo, descricao.Trim(), "DangerBrush"));
        }

        private void CarregarPecasAplicadas()
        {
            PecasAplicadasPanel.Children.Clear();

            var pecas = ObterOrdensDoVeiculo()
                .SelectMany(ordem => ordem.Itens.Select(item => new { ordem.Numero, Item = item }))
                .Where(item =>
                    item.Item.ProdutoId.HasValue ||
                    string.Equals(item.Item.Tipo, "Produto", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.Numero)
                .Take(10)
                .ToList();

            foreach (var peca in pecas)
            {
                var resumo = $"OS {peca.Numero} | {peca.Item.Quantidade:N2} x {peca.Item.ValorUnitario:C2} = {peca.Item.Total:C2}";
                PecasAplicadasPanel.Children.Add(CriarCardResumo(Exibir(peca.Item.Descricao), resumo, "SuccessBrush"));
            }

            if (PecasAplicadasPanel.Children.Count == 0)
            {
                PecasAplicadasPanel.Children.Add(CriarEstadoVazio("Nenhuma peca aplicada em OS vinculada."));
            }
        }

        private List<OrdemServico> ObterOrdensDoVeiculo()
        {
            var placaNormalizada = CadastroValidationHelper.NormalizarPlaca(_veiculo.Placa);

            return App.Repositories.OrdensServico.ObterTodos(true)
                .Where(os => os.VeiculoId == _veiculo.Id ||
                             (!string.IsNullOrWhiteSpace(os.PlacaSnapshot) &&
                              string.Equals(CadastroValidationHelper.NormalizarPlaca(os.PlacaSnapshot), placaNormalizada, StringComparison.Ordinal)))
                .OrderByDescending(os => os.DataAbertura)
                .ToList();
        }

        private Border CriarCardResumo(string titulo, string subtitulo, string brushKey)
        {
            var destaque = ObterBrush(brushKey, "#F97316");

            var border = new Border
            {
                Background = ObterBrush("CardBackgroundBrush", "#FFFFFF"),
                BorderBrush = ObterBrush("BorderBrush", "#E2E8F0"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var stack = new StackPanel();
            stack.Children.Add(new Border
            {
                Width = 42,
                Height = 4,
                CornerRadius = new CornerRadius(2),
                Background = destaque,
                Margin = new Thickness(0, 0, 0, 10)
            });
            stack.Children.Add(new TextBlock
            {
                Text = titulo,
                FontWeight = FontWeights.Bold,
                Foreground = ObterBrush("PrimaryTextBrush", "#0F172A"),
                TextWrapping = TextWrapping.Wrap
            });
            stack.Children.Add(new TextBlock
            {
                Text = subtitulo,
                Margin = new Thickness(0, 6, 0, 0),
                Foreground = ObterBrush("SecondaryTextBrush", "#475569"),
                TextWrapping = TextWrapping.Wrap
            });

            border.Child = stack;
            return border;
        }

        private Border CriarEstadoVazio(string mensagem)
        {
            return new Border
            {
                Background = ObterBrush("SurfaceAltBrush", "#F8FAFC"),
                BorderBrush = ObterBrush("BorderBrush", "#E2E8F0"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14),
                Child = new TextBlock
                {
                    Text = mensagem,
                    Foreground = ObterBrush("MutedTextBrush", "#64748B"),
                    TextWrapping = TextWrapping.Wrap
                }
            };
        }

        private Brush ObterBrush(string resourceKey, string fallbackHex)
        {
            return TryFindResource(resourceKey) as Brush
                ?? (SolidColorBrush)new BrushConverter().ConvertFromString(fallbackHex)!;
        }

        private static string Exibir(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? "-" : valor.Trim();
        }

        private static string ExibirBloco(string? valor, string fallback)
        {
            return string.IsNullOrWhiteSpace(valor) ? fallback : valor.Trim();
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void EditarButton_Click(object sender, RoutedEventArgs e)
        {
            var clientes = App.Repositories.Clientes.ObterTodos()
                .ToDictionary(c => c.Id, c => c);

            var janela = new NovoVeiculoWindow(_databaseService, clientes, _veiculo);
            WindowOwnerHelper.ConfigureOwner(janela, this);

            if (janela.ShowDialog() == true)
            {
                RecarregarTela();
            }
        }
    }
}
