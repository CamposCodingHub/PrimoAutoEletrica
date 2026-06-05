using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Views;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.Views.Clientes
{
    public partial class EditarClienteWindow : Window
    {
        private readonly DatabaseService _databaseService = App.Database;
        private readonly RecordLockService _recordLockService;
        private readonly Cliente _cliente;
        private readonly List<Veiculo> _veiculos = new();
        private string _caminhoDocumentoSelecionado = string.Empty;
        private string _caminhoAssinaturaGerada = string.Empty;
        private string _caminhoFotoSelecionada = string.Empty;
        private string _imagemPendenteParaExcluir = string.Empty;
        private bool _lockObtido = false;
        private bool _aplicandoMascaras;

        public EditarClienteWindow(Cliente cliente)
        {
            InitializeComponent();

            _databaseService = App.Database;
            _recordLockService = new RecordLockService(_databaseService, App.Logger, App.Session, App.Audit);
            _cliente = App.Repositories.Clientes.ObterPorId(cliente.Id) ?? cliente;
            _veiculos = _cliente.Veiculos.Select(CloneVeiculo).ToList();
            VeiculosDataGrid.ItemsSource = _veiculos;
            CpfTextBox.TextChanged += DocumentoTextBox_TextChanged;
            TelefoneTextBox.TextChanged += TelefoneTextBox_TextChanged;
            WhatsAppTextBox.TextChanged += WhatsAppTextBox_TextChanged;

            Loaded += EditarClienteWindow_Loaded;
            Closed += EditarClienteWindow_Closed;

            CarregarDados();
        }

        private void EditarClienteWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var lockResult = _recordLockService.TryLock("Cliente", _cliente.Id.ToString(), _cliente.Nome);
            if (!lockResult.Success)
            {
                MessageBox.Show(
                    lockResult.Message,
                    "Registro bloqueado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                Close();
                return;
            }
            _lockObtido = true;
        }

        private void EditarClienteWindow_Closed(object? sender, EventArgs e)
        {
            if (_lockObtido)
            {
                _recordLockService.ReleaseLock("Cliente", _cliente.Id.ToString());
            }
        }

        private void CarregarDados()
        {
            TituloClienteTextBlock.Text = _cliente.Nome;
            NomeTextBox.Text = _cliente.Nome;
            CpfTextBox.Text = CadastroValidationHelper.FormatarDocumento(_cliente.CPF);
            TelefoneTextBox.Text = CadastroValidationHelper.FormatarTelefone(_cliente.Telefone);
            WhatsAppTextBox.Text = CadastroValidationHelper.FormatarTelefone(_cliente.WhatsApp);
            EmailTextBox.Text = _cliente.Email;
            CepTextBox.Text = _cliente.CEP;
            RuaTextBox.Text = _cliente.Rua;
            NumeroTextBox.Text = _cliente.Numero;
            BairroTextBox.Text = _cliente.Bairro;
            CidadeTextBox.Text = _cliente.Cidade;
            EstadoTextBox.Text = _cliente.Estado;
            ObservacoesTextBox.Text = _cliente.Observacoes;
            ClienteVipCheckBox.IsChecked = _cliente.ClienteVip;
            ConsentimentoLgpdCheckBox.IsChecked = _cliente.ConsentimentoLGPD;
            AutorizaWhatsAppCheckBox.IsChecked = _cliente.AutorizaContatoWhatsApp;
            PontosTextBox.Text = _cliente.PontosFidelidade.ToString();

            TotalGastoTextBlock.Text = _cliente.TotalGasto.ToString("C");
            TotalServicosTextBlock.Text = _cliente.TotalServicos.ToString();
            FrotaTextBlock.Text = _cliente.Veiculos.Count.ToString();
            UltimaVisitaTextBlock.Text = _cliente.UltimaVisita?.ToString("dd/MM/yyyy") ?? "-";
            AtualizarPreviewFoto(_cliente.ImagemUrl);
            AtualizarResumoAnexos();
            AtualizarResumoLgpd();
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void WhatsAppButton_Click(object sender, RoutedEventArgs e)
        {
            var contatoAtual = string.IsNullOrWhiteSpace(WhatsAppTextBox.Text)
                ? TelefoneTextBox.Text
                : WhatsAppTextBox.Text;

            if (!CadastroValidationHelper.TryObterTelefoneWhatsApp(contatoAtual, out var telefone))
            {
                WindowInteractionHelper.ShowMessage(
                    "Este cliente nao possui telefone/WhatsApp cadastrado.",
                    "Contato ausente",
                    MessageBoxImage.Information,
                    "Clientes");
                return;
            }

            if (!(_cliente.ConsentimentoLGPD && _cliente.AutorizaContatoWhatsApp))
            {
                var confirmado = CriticalActionDialogService.ConfirmarAcao(
                    this,
                    new CriticalActionRequest
                    {
                        WindowTitle = "Contato LGPD pendente",
                        Header = "Revise o consentimento antes do contato",
                        Summary = $"O cliente '{_cliente.Nome}' nao possui consentimento completo para WhatsApp.",
                        Details = $"LGPD: {(_cliente.ConsentimentoLGPD ? "registrado" : "pendente")}\nWhatsApp: {(_cliente.AutorizaContatoWhatsApp ? "autorizado" : "nao autorizado")}",
                        Impact = "Confirme somente se este contato for necessario para atendimento em andamento ou se o consentimento foi validado fora do sistema.",
                        Keyword = "CONTATAR",
                        ConfirmButtonText = "Abrir WhatsApp"
                    });

                if (!confirmado)
                {
                    return;
                }
            }

            if (App.IsAutomatedTestMode)
            {
                WindowInteractionHelper.LogAutomationExternalAction(
                    $"WhatsApp do cliente validado em automacao para o telefone {telefone} sem abrir aplicativo externo.",
                    "Clientes");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = $"https://wa.me/{telefone}",
                UseShellExecute = true
            });
        }

        private void HistoricoButton_Click(object sender, RoutedEventArgs e)
        {
            var clienteAtualizado = MontarClienteDaTela();
            var janela = new HistoricoClienteWindow(clienteAtualizado);

            if (App.IsAutomatedTestMode)
            {
                ValidarJanelaEmAutomacao(janela, "historico do cliente");
                return;
            }

            janela.Owner = this;
            janela.ShowDialog();
        }

        private void NovaOsButton_Click(object sender, RoutedEventArgs e)
        {
            var clienteAtualizado = MontarClienteDaTela();
            var janela = new OrdemServicoWindow(_databaseService, null, clienteAtualizado);

            if (App.IsAutomatedTestMode)
            {
                ValidarJanelaEmAutomacao(janela, "nova ordem de servico do cliente");
                return;
            }

            janela.Owner = this;
            janela.ShowDialog();
        }

        private void AdicionarVeiculoButton_Click(object sender, RoutedEventArgs e)
        {
            _veiculos.Add(new Veiculo
            {
                Ano = DateTime.Now.Year.ToString()
            });

            VeiculosDataGrid.Items.Refresh();
            FrotaTextBlock.Text = _veiculos.Count.ToString();
        }

        private void SelecionarFotoButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.IsAutomatedTestMode)
            {
                WindowInteractionHelper.LogAutomationExternalAction(
                    "Selecao de foto do cliente validada em automacao sem abrir seletor de arquivos.",
                    "Clientes");
                return;
            }

            var dialog = new OpenFileDialog
            {
                Filter = ClienteMediaService.SupportedImageFilter
            };

            if (dialog.ShowDialog() == true)
            {
                _caminhoFotoSelecionada = dialog.FileName;
                AtualizarPreviewFoto(_caminhoFotoSelecionada);
            }
        }

        private void RemoverFotoButton_Click(object sender, RoutedEventArgs e)
        {
            _caminhoFotoSelecionada = string.Empty;
            _imagemPendenteParaExcluir = _cliente.ImagemUrl;
            AtualizarPreviewFoto(null);
        }

        private void SelecionarDocumentoButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.IsAutomatedTestMode)
            {
                _caminhoDocumentoSelecionado = CriarArquivoAutomacao(
                    "documento-cliente-edicao-smoke.txt",
                    "Documento sintetico vinculado pela edicao do cliente.");
                AtualizarResumoAnexos();
                WindowInteractionHelper.LogAutomationExternalAction(
                    "Documento sintetico do cliente vinculado em automacao sem abrir seletor de arquivos.",
                    "Clientes");
                return;
            }

            var dialog = new OpenFileDialog
            {
                Filter = "Documentos|*.pdf;*.doc;*.docx;*.png;*.jpg"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                _caminhoDocumentoSelecionado = CopiarArquivoCliente(dialog.FileName, "Documentos");
                AtualizarResumoAnexos();
                WindowInteractionHelper.ShowMessage(
                    "Documento anexado. Clique em Salvar para gravar no cadastro.",
                    "Documento",
                    MessageBoxImage.Information,
                    "Clientes");
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao substituir documento do cliente.", ex);
                WindowInteractionHelper.ShowMessage(
                    $"Erro ao anexar documento:\n{ex.Message}",
                    "Documento",
                    MessageBoxImage.Error,
                    "Clientes",
                    ex);
            }
        }

        private void RegistrarAssinaturaButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NomeTextBox.Text))
            {
                WindowInteractionHelper.ShowMessage(
                    "Informe o nome do cliente antes de registrar a assinatura digital.",
                    "Assinatura Digital",
                    MessageBoxImage.Warning,
                    "Clientes");
                NomeTextBox.Focus();
                return;
            }

            if (!CriticalActionDialogService.ConfirmarAcao(
                this,
                new CriticalActionRequest
                {
                    WindowTitle = "Assinatura digital",
                    Header = "Registro de aceite do cliente",
                    Summary = $"Voce esta prestes a registrar novo aceite digital do cliente '{NomeTextBox.Text.Trim()}'.",
                    Details = $"Documento: {CpfTextBox.Text.Trim()}\nOperador: {App.Session.UserName}\nSessao: {(App.Session.SessionId == Guid.Empty ? "Local" : App.Session.SessionId.ToString())}",
                    Impact = "O sistema vai gerar novo termo com hash e rastreabilidade. Clique em Salvar para gravar no cadastro.",
                    Keyword = "ASSINAR",
                    ConfirmButtonText = "Registrar assinatura"
                }))
            {
                return;
            }

            try
            {
                _caminhoAssinaturaGerada = GerarTermoAssinaturaDigital();
                AtualizarResumoAnexos();
                App.Audit.RegistrarAcaoCritica(
                    "Clientes",
                    "AssinaturaDigitalRegistrada",
                    "Cliente",
                    _cliente.Id.ToString(),
                    _caminhoAssinaturaGerada);
                WindowInteractionHelper.ShowMessage(
                    "Assinatura digital registrada. Clique em Salvar para gravar no cadastro.",
                    "Assinatura Digital",
                    MessageBoxImage.Information,
                    "Clientes");
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao registrar assinatura digital do cliente.", ex);
                WindowInteractionHelper.ShowMessage(
                    $"Erro ao registrar assinatura digital:\n{ex.Message}",
                    "Assinatura Digital",
                    MessageBoxImage.Error,
                    "Clientes",
                    ex);
            }
        }

        private void AbrirDocumentoButton_Click(object sender, RoutedEventArgs e)
        {
            AbrirArquivoSeExistir(CaminhoDocumentoAtual(), "documento");
        }

        private void AbrirAssinaturaButton_Click(object sender, RoutedEventArgs e)
        {
            AbrirArquivoSeExistir(CaminhoAssinaturaAtual(), "assinatura");
        }

        private void SalvarButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarCampos())
            {
                return;
            }

            try
            {
                var clienteAtualizado = MontarClienteDaTela();
                App.Repositories.Clientes.Atualizar(clienteAtualizado);
                LimparImagemPendenteSeNecessario();

                WindowInteractionHelper.ShowMessage(
                    "Cliente atualizado com sucesso!",
                    "Sucesso",
                    MessageBoxImage.Information,
                    "Clientes");

                WindowInteractionHelper.CloseWithDialogResult(this, true, "Clientes");
            }
            catch (Exception ex)
            {
                WindowInteractionHelper.ShowMessage(
                    $"Erro ao atualizar cliente:\n{ex.Message}",
                    "Erro",
                    MessageBoxImage.Error,
                    "Clientes",
                    ex);
            }
        }

        private Cliente MontarClienteDaTela()
        {
            _ = int.TryParse(PontosTextBox.Text, out var pontos);

            _cliente.Nome = NomeTextBox.Text.Trim();
            _cliente.CPF = CpfTextBox.Text.Trim();
            _cliente.Telefone = TelefoneTextBox.Text.Trim();
            _cliente.WhatsApp = WhatsAppTextBox.Text.Trim();
            _cliente.Email = EmailTextBox.Text.Trim();
            _cliente.CEP = CepTextBox.Text.Trim();
            _cliente.Rua = RuaTextBox.Text.Trim();
            _cliente.Numero = NumeroTextBox.Text.Trim();
            _cliente.Bairro = BairroTextBox.Text.Trim();
            _cliente.Cidade = CidadeTextBox.Text.Trim();
            _cliente.Estado = EstadoTextBox.Text.Trim();
            _cliente.Observacoes = ObservacoesTextBox.Text.Trim();
            _cliente.ClienteVip = ClienteVipCheckBox.IsChecked == true;
            var lgpdAntes = _cliente.ConsentimentoLGPD;
            _cliente.ConsentimentoLGPD = ConsentimentoLgpdCheckBox.IsChecked == true;
            _cliente.DataConsentimentoLGPD = _cliente.ConsentimentoLGPD
                ? _cliente.DataConsentimentoLGPD ?? DateTime.Now
                : null;
            _cliente.OrigemConsentimentoLGPD = _cliente.ConsentimentoLGPD
                ? string.IsNullOrWhiteSpace(_cliente.OrigemConsentimentoLGPD) || !lgpdAntes
                    ? "Edicao de cliente"
                    : _cliente.OrigemConsentimentoLGPD
                : string.Empty;
            _cliente.AutorizaContatoWhatsApp = _cliente.ConsentimentoLGPD && AutorizaWhatsAppCheckBox.IsChecked == true;
            _cliente.PontosFidelidade = Math.Max(0, pontos);
            _cliente.CaminhoDocumento = CaminhoDocumentoAtual();
            _cliente.CaminhoAssinatura = CaminhoAssinaturaAtual();
            _cliente.ImagemUrl = ResolverImagemPersistida(_cliente.Id, _cliente.Nome, _cliente.ImagemUrl);
            _cliente.Veiculos = _veiculos
                .Where(v =>
                    !string.IsNullOrWhiteSpace(v.Marca) ||
                    !string.IsNullOrWhiteSpace(v.Modelo) ||
                    !string.IsNullOrWhiteSpace(v.Placa))
                .Select(CloneVeiculo)
                .ToList();

            return _cliente;
        }

        private bool ValidarCampos()
        {
            if (string.IsNullOrWhiteSpace(NomeTextBox.Text))
            {
                return ExibirErroValidacao("Informe o nome do cliente antes de salvar.", NomeTextBox, "Cadastro incompleto");
            }

            var erroDocumento = CadastroValidationHelper.ValidarCpfOuCnpj(CpfTextBox.Text, obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroDocumento))
            {
                return ExibirErroValidacao(erroDocumento, CpfTextBox, "Documento invalido");
            }

            var erroTelefone = CadastroValidationHelper.ValidarTelefone(TelefoneTextBox.Text, obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroTelefone))
            {
                return ExibirErroValidacao(erroTelefone, TelefoneTextBox, "Contato invalido");
            }

            var erroWhatsApp = CadastroValidationHelper.ValidarTelefone(WhatsAppTextBox.Text, "WhatsApp", obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroWhatsApp))
            {
                return ExibirErroValidacao(erroWhatsApp, WhatsAppTextBox, "Contato invalido");
            }

            var erroEmail = CadastroValidationHelper.ValidarEmail(EmailTextBox.Text, obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroEmail))
            {
                return ExibirErroValidacao(erroEmail, EmailTextBox, "Contato invalido");
            }

            if (!string.IsNullOrWhiteSpace(PontosTextBox.Text) &&
                (!int.TryParse(PontosTextBox.Text, out var pontos) || pontos < 0))
            {
                return ExibirErroValidacao("Informe uma quantidade de pontos valida.", PontosTextBox, "Valor invalido");
            }

            return true;
        }

        private void DocumentoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarMascara(CpfTextBox, CadastroValidationHelper.FormatarDocumento);
        }

        private void TelefoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarMascara(TelefoneTextBox, CadastroValidationHelper.FormatarTelefone);
        }

        private void WhatsAppTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarMascara(WhatsAppTextBox, CadastroValidationHelper.FormatarTelefone);
        }

        private void ConsentimentoLgpdCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            AtualizarResumoLgpd();
        }

        private void AplicarMascara(TextBox textBox, Func<string?, string> formatador)
        {
            if (_aplicandoMascaras)
            {
                return;
            }

            var textoOriginal = textBox.Text;
            var textoFormatado = formatador(textoOriginal);
            if (string.Equals(textoOriginal, textoFormatado, StringComparison.Ordinal))
            {
                return;
            }

            _aplicandoMascaras = true;
            var posicaoCursor = textBox.SelectionStart;
            textBox.Text = textoFormatado;
            textBox.SelectionStart = Math.Min(textoFormatado.Length, posicaoCursor + Math.Max(0, textoFormatado.Length - textoOriginal.Length));
            _aplicandoMascaras = false;
        }

        private static bool ExibirErroValidacao(string mensagem, Control campo, string titulo)
        {
            WindowInteractionHelper.ShowMessage(mensagem, titulo, MessageBoxImage.Warning, "Clientes");
            campo.Focus();
            return false;
        }

        private static void ValidarJanelaEmAutomacao(Window janela, string contexto)
        {
            try
            {
                janela.ApplyTemplate();
                if (janela.Content is FrameworkElement content)
                {
                    content.ApplyTemplate();
                    content.Measure(new Size(1280, 720));
                    content.Arrange(new Rect(0, 0, 1280, 720));
                    content.UpdateLayout();
                }

                App.Logger.LogInfo($"Janela de {contexto} validada em automacao sem abrir modal bloqueante.", "Clientes");
            }
            finally
            {
                janela.Close();
            }
        }

        private static Veiculo CloneVeiculo(Veiculo veiculo)
        {
            return new Veiculo
            {
                Id = veiculo.Id,
                ClienteId = veiculo.ClienteId,
                Marca = veiculo.Marca,
                Modelo = veiculo.Modelo,
                Ano = veiculo.Ano,
                Cor = veiculo.Cor,
                Placa = veiculo.Placa,
                Chassi = veiculo.Chassi,
                Renavam = veiculo.Renavam,
                Motor = veiculo.Motor,
                Combustivel = veiculo.Combustivel,
                Quilometragem = veiculo.Quilometragem,
                Observacoes = veiculo.Observacoes
            };
        }

        private string ResolverImagemPersistida(Guid clienteId, string nomeCliente, string imagemExistente)
        {
            var imagemExistenteResolvida = ClienteMediaService.ResolveExistingPath(imagemExistente);
            if (string.IsNullOrWhiteSpace(_caminhoFotoSelecionada))
            {
                if (!string.IsNullOrWhiteSpace(_imagemPendenteParaExcluir) && !string.IsNullOrWhiteSpace(imagemExistenteResolvida))
                {
                    return string.Empty;
                }

                return imagemExistente;
            }

            if (string.Equals(
                ClienteMediaService.ResolveExistingPath(_caminhoFotoSelecionada),
                imagemExistenteResolvida,
                StringComparison.OrdinalIgnoreCase))
            {
                return imagemExistente;
            }

            var imagemPersistida = ClienteMediaService.PersistSelectedImage(_caminhoFotoSelecionada, clienteId, nomeCliente);
            if (!string.IsNullOrWhiteSpace(imagemExistenteResolvida))
            {
                _imagemPendenteParaExcluir = imagemExistenteResolvida;
            }

            return imagemPersistida;
        }

        private void LimparImagemPendenteSeNecessario()
        {
            if (string.IsNullOrWhiteSpace(_imagemPendenteParaExcluir))
            {
                return;
            }

            ClienteMediaService.DeleteManagedImageIfOwned(_imagemPendenteParaExcluir);
            _imagemPendenteParaExcluir = string.Empty;
        }

        private void AtualizarPreviewFoto(string? caminho)
        {
            var preview = ClienteMediaService.TryCreatePreviewSource(caminho);
            FotoPreviewImage.Source = preview;
            FotoPreviewImage.Visibility = preview == null ? Visibility.Collapsed : Visibility.Visible;
            FotoPlaceholderText.Visibility = preview == null ? Visibility.Visible : Visibility.Collapsed;
            RemoverFotoButton.IsEnabled = preview != null || !string.IsNullOrWhiteSpace(_caminhoFotoSelecionada);
        }

        private string CaminhoDocumentoAtual()
        {
            return !string.IsNullOrWhiteSpace(_caminhoDocumentoSelecionado)
                ? _caminhoDocumentoSelecionado
                : _cliente.CaminhoDocumento;
        }

        private string CaminhoAssinaturaAtual()
        {
            return !string.IsNullOrWhiteSpace(_caminhoAssinaturaGerada)
                ? _caminhoAssinaturaGerada
                : _cliente.CaminhoAssinatura;
        }

        private void AtualizarResumoAnexos()
        {
            var caminhoDocumento = CaminhoDocumentoAtual();
            var caminhoAssinatura = CaminhoAssinaturaAtual();

            DocumentoStatusTextBlock.Text = string.IsNullOrWhiteSpace(caminhoDocumento)
                ? "Nenhum documento anexado."
                : $"Documento vinculado: {Path.GetFileName(caminhoDocumento)}";
            AssinaturaStatusTextBlock.Text = string.IsNullOrWhiteSpace(caminhoAssinatura)
                ? "Nenhuma assinatura registrada."
                : $"Assinatura registrada: {Path.GetFileName(caminhoAssinatura)}";
            AbrirDocumentoButton.IsEnabled = !string.IsNullOrWhiteSpace(caminhoDocumento);
            AbrirAssinaturaButton.IsEnabled = !string.IsNullOrWhiteSpace(caminhoAssinatura);
        }

        private void AtualizarResumoLgpd()
        {
            var consentido = ConsentimentoLgpdCheckBox?.IsChecked == true;
            if (AutorizaWhatsAppCheckBox != null)
            {
                AutorizaWhatsAppCheckBox.IsEnabled = consentido;
                if (!consentido)
                {
                    AutorizaWhatsAppCheckBox.IsChecked = false;
                }
            }

            if (LgpdStatusTextBlock != null)
            {
                LgpdStatusTextBlock.Text = consentido
                    ? $"LGPD ativo desde {(_cliente.DataConsentimentoLGPD?.ToString("dd/MM/yyyy HH:mm") ?? "agora")}."
                    : "LGPD pendente: evite contato ativo ate registrar consentimento.";
            }
        }

        private string GerarTermoAssinaturaDigital()
        {
            var agora = DateTime.Now;
            var assinaturaId = Guid.NewGuid();
            var nomeCliente = NomeTextBox.Text.Trim();
            var documento = CpfTextBox.Text.Trim();
            var operador = App.Session.UserName;
            var sessao = App.Session.SessionId == Guid.Empty ? "Sessao local" : App.Session.SessionId.ToString();

            var payload = string.Join(Environment.NewLine,
                $"AssinaturaId: {assinaturaId}",
                $"ClienteId: {_cliente.Id}",
                $"Cliente: {nomeCliente}",
                $"Documento: {documento}",
                $"DataHora: {agora:O}",
                $"Operador: {operador}",
                $"Sessao: {sessao}");

            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
            var conteudo = string.Join(Environment.NewLine,
                "TERMO DE ACEITE DIGITAL - PRIMO AUTO ELETRICA",
                "================================================",
                payload,
                $"HashSHA256: {hash}",
                string.Empty,
                "Declaracao:",
                "O cliente confirma a ciencia e o aceite dos dados informados neste cadastro.",
                "Este arquivo representa um registro digital rastreavel do aceite.");

            var pasta = ObterPastaCliente("Assinaturas");
            var nomeArquivo = $"{agora:yyyyMMddHHmmss}_{NormalizarNomeArquivo(nomeCliente)}_{assinaturaId:N}.txt";
            var caminho = Path.Combine(pasta, nomeArquivo);

            File.WriteAllText(caminho, conteudo, Encoding.UTF8);
            App.Logger.LogInfo($"Assinatura digital registrada para cliente '{nomeCliente}' em '{caminho}'.", "Clientes");
            return caminho;
        }

        private string CopiarArquivoCliente(string origem, string categoria)
        {
            var pasta = ObterPastaCliente(categoria);
            var nomeBase = NormalizarNomeArquivo(NomeTextBox.Text);
            var extensao = Path.GetExtension(origem);
            var destino = Path.Combine(pasta, $"{DateTime.Now:yyyyMMddHHmmss}_{nomeBase}{extensao}");

            File.Copy(origem, destino, overwrite: false);
            App.Logger.LogInfo($"Arquivo de cliente anexado em '{destino}'.", "Clientes");
            return destino;
        }

        private static string CriarArquivoAutomacao(string nomeArquivo, string conteudo)
        {
            var pasta = ObterPastaCliente("Automacao");
            var caminho = Path.Combine(pasta, nomeArquivo);
            File.WriteAllText(caminho, conteudo, Encoding.UTF8);
            App.Logger.LogInfo($"Arquivo sintetico de cliente gerado em '{caminho}'.", "Clientes");
            return caminho;
        }

        private static string ObterPastaCliente(string categoria)
        {
            var pasta = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PrimoAutoEletrica",
                "Clientes",
                categoria);

            Directory.CreateDirectory(pasta);
            return pasta;
        }

        private static string NormalizarNomeArquivo(string? valor)
        {
            var texto = string.IsNullOrWhiteSpace(valor) ? "cliente" : valor.Trim();
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                texto = texto.Replace(invalidChar, '_');
            }

            return texto.Replace(' ', '_');
        }

        private void AbrirArquivoSeExistir(string? caminho, string descricao)
        {
            if (string.IsNullOrWhiteSpace(caminho) || !File.Exists(caminho))
            {
                WindowInteractionHelper.ShowMessage(
                    $"Nenhuma {descricao} vinculada a este cliente.",
                    "Arquivo ausente",
                    MessageBoxImage.Information,
                    "Clientes");
                return;
            }

            if (App.IsAutomatedTestMode)
            {
                WindowInteractionHelper.LogAutomationExternalAction(
                    $"Abertura de {descricao} validada em automacao sem executar aplicativo externo.",
                    "Clientes");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = caminho,
                UseShellExecute = true
            });
        }
    }
}
