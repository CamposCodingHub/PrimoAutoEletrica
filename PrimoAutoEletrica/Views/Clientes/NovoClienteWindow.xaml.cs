using Microsoft.Win32;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.Views.Clientes
{
    public partial class NovoClienteWindow : Window
    {
        private readonly DatabaseService _databaseService = App.Database;
        private readonly List<Veiculo> _veiculos = new();
        private Cliente? _clienteSalvo;
        private string _caminhoDocumento = string.Empty;
        private string _caminhoAssinatura = string.Empty;
        private string _caminhoFotoSelecionada = string.Empty;
        private string _imagemPendenteParaExcluir = string.Empty;
        private bool _aplicandoMascaras;

        public NovoClienteWindow()
        {
            InitializeComponent();
            DataContext = new ViewModels.ClientesViewModel();
            VeiculosDataGrid.ItemsSource = _veiculos;
            CpfTextBox.TextChanged += DocumentoTextBox_TextChanged;
            TelefoneTextBox.TextChanged += TelefoneTextBox_TextChanged;
            WhatsAppTextBox.TextChanged += WhatsAppTextBox_TextChanged;
            AtualizarPreviewFoto(null);
            AtualizarResumoAnexos();
            AtualizarResumoLgpd();
        }

        public void CarregarFotoParaAutomacao(string caminhoFoto)
        {
            if (!App.IsAutomatedTestMode)
            {
                throw new InvalidOperationException("Carga direta de foto so e permitida em automacao.");
            }

            if (string.IsNullOrWhiteSpace(caminhoFoto) ||
                !File.Exists(caminhoFoto) ||
                !ClienteMediaService.IsSupportedImageFile(caminhoFoto))
            {
                throw new InvalidOperationException($"Foto de cliente invalida para automacao: {caminhoFoto}");
            }

            _caminhoFotoSelecionada = caminhoFoto;
            AtualizarPreviewFoto(_caminhoFotoSelecionada);
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
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
            if (_clienteSalvo != null)
            {
                _imagemPendenteParaExcluir = _clienteSalvo.ImagemUrl;
            }

            AtualizarPreviewFoto(null);
        }

        private void UploadDocumentoButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.IsAutomatedTestMode)
            {
                _caminhoDocumento = CriarArquivoAutomacao("documento-cliente-smoke.txt", "Documento sintetico do smoke test.");
                WindowInteractionHelper.ShowMessage(
                    "Documento sintetico vinculado ao cadastro em automacao.",
                    "Documento",
                    MessageBoxImage.Information,
                    "Clientes");
                AtualizarResumoAnexos();
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
                _caminhoDocumento = CopiarArquivoCliente(dialog.FileName, "Documentos");

                WindowInteractionHelper.ShowMessage(
                    "Documento anexado e vinculado ao cadastro.",
                    "Documento",
                    MessageBoxImage.Information,
                    "Clientes");
                AtualizarResumoAnexos();
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao anexar documento do cliente.", ex);
                WindowInteractionHelper.ShowMessage(
                    $"Erro ao anexar documento:\n{ex.Message}",
                    "Documento",
                    MessageBoxImage.Error,
                    "Clientes",
                    ex);
            }
        }

        private void AssinaturaButton_Click(object sender, RoutedEventArgs e)
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
                    Summary = $"Voce esta prestes a registrar o aceite digital do cliente '{NomeTextBox.Text.Trim()}'.",
                    Details = $"Documento: {CpfTextBox.Text.Trim()}\nOperador: {App.Session.UserName}\nSessao: {(App.Session.SessionId == Guid.Empty ? "Local" : App.Session.SessionId.ToString())}",
                    Impact = "O sistema vai gerar um termo com hash e rastreabilidade para comprovar o aceite digital deste cadastro.",
                    Keyword = "ASSINAR",
                    ConfirmButtonText = "Registrar assinatura"
                }))
            {
                return;
            }

            try
            {
                _caminhoAssinatura = GerarTermoAssinaturaDigital();
                App.Audit.RegistrarAcaoCritica(
                    "Clientes",
                    "AssinaturaDigitalRegistrada",
                    "Cliente",
                    string.IsNullOrWhiteSpace(CpfTextBox.Text) ? NomeTextBox.Text.Trim() : CpfTextBox.Text.Trim(),
                    _caminhoAssinatura);

                WindowInteractionHelper.ShowMessage(
                    "Assinatura digital registrada e vinculada ao cadastro.",
                    "Assinatura Digital",
                    MessageBoxImage.Information,
                    "Clientes");
                AtualizarResumoAnexos();
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

        private void AdicionarVeiculoButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.IsAutomatedTestMode)
            {
                _veiculos.Add(new Veiculo
                {
                    ClienteId = _clienteSalvo?.Id,
                    Marca = "Smoke",
                    Modelo = "Veiculo",
                    Ano = DateTime.Today.Year.ToString(),
                    Cor = "Prata",
                    Placa = $"SMK{DateTime.Now:HHm}",
                    Quilometragem = 1000,
                    Observacoes = "Veiculo sintetico adicionado em automacao."
                });

                VeiculosDataGrid.Items.Refresh();
                WindowInteractionHelper.LogAutomationExternalAction(
                    "Veiculo sintetico adicionado ao cadastro em automacao sem abrir modal.",
                    "Clientes");
                return;
            }

            if (!GarantirClienteSalvoParaVeiculo())
            {
                return;
            }

            var clientes = App.Repositories.Clientes.ObterTodos()
                .ToDictionary(c => c.Id, c => c);

            var novoVeiculoWindow = new NovoVeiculoWindow(_databaseService, clientes, clientePreSelecionado: _clienteSalvo);

            if (novoVeiculoWindow.ShowDialog() == true)
            {
                RecarregarVeiculosDoCliente();
            }
        }

        private void SalvarButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarCampos())
            {
                return;
            }

            try
            {
                var cliente = MontarClienteDaTela();

                if (_clienteSalvo != null)
                {
                    cliente.Id = _clienteSalvo.Id;
                    cliente.TotalGasto = _clienteSalvo.TotalGasto;
                    cliente.TotalServicos = _clienteSalvo.TotalServicos;
                    cliente.DataCadastro = _clienteSalvo.DataCadastro;
                    cliente.UltimaVisita = _clienteSalvo.UltimaVisita;
                    App.Repositories.Clientes.Atualizar(cliente);
                }
                else
                {
                    App.Repositories.Clientes.Inserir(cliente);
                }

                LimparImagemPendenteSeNecessario();

                WindowInteractionHelper.ShowMessage(
                    $"Cliente {cliente.Nome} salvo com sucesso!",
                    "Sucesso",
                    MessageBoxImage.Information,
                    "Clientes");

                WindowInteractionHelper.CloseWithDialogResult(this, true, "Clientes");
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao salvar cliente.", ex);
                WindowInteractionHelper.ShowMessage(
                    $"Erro ao salvar cliente:\n{ex.Message}",
                    "Erro",
                    MessageBoxImage.Error,
                    "Clientes",
                    ex);
            }
        }

        private bool GarantirClienteSalvoParaVeiculo()
        {
            if (_clienteSalvo != null)
            {
                return true;
            }

            if (!ValidarCampos())
            {
                return false;
            }

            try
            {
                var cliente = MontarClienteDaTela();
                App.Repositories.Clientes.Inserir(cliente);

                _clienteSalvo = App.Repositories.Clientes.ObterPorId(cliente.Id);

                if (_clienteSalvo == null)
                {
                    WindowInteractionHelper.ShowMessage(
                        "Erro ao recuperar cliente salvo.",
                        "Erro",
                        MessageBoxImage.Error,
                        "Clientes");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao salvar cliente antes de adicionar veiculo.", ex);
                WindowInteractionHelper.ShowMessage(
                    $"Erro ao salvar cliente:\n{ex.Message}",
                    "Erro",
                    MessageBoxImage.Error,
                    "Clientes",
                    ex);
                return false;
            }
        }

        private Cliente MontarClienteDaTela()
        {
            _ = int.TryParse(PontosTextBox.Text, out var pontos);
            var clienteId = _clienteSalvo?.Id ?? Guid.NewGuid();

            return new Cliente
            {
                Id = clienteId,
                Nome = NomeTextBox.Text.Trim(),
                TipoPessoa = ObterTipoPessoaSelecionado(),
                CPF = CpfTextBox.Text.Trim(),
                RG = RgTextBox.Text.Trim(),
                Telefone = TelefoneTextBox.Text.Trim(),
                WhatsApp = WhatsAppTextBox.Text.Trim(),
                Email = EmailTextBox.Text.Trim(),
                CEP = CepTextBox.Text.Trim(),
                Rua = RuaTextBox.Text.Trim(),
                Numero = NumeroTextBox.Text.Trim(),
                Bairro = BairroTextBox.Text.Trim(),
                Cidade = CidadeTextBox.Text.Trim(),
                Estado = EstadoTextBox.Text.Trim(),
                Ativo = ObterStatusAtivoSelecionado(),
                ClienteVip = ClienteVipCheckBox.IsChecked == true,
                ConsentimentoLGPD = ConsentimentoLgpdCheckBox.IsChecked == true,
                DataConsentimentoLGPD = ConsentimentoLgpdCheckBox.IsChecked == true
                    ? _clienteSalvo?.DataConsentimentoLGPD ?? DateTime.Now
                    : null,
                OrigemConsentimentoLGPD = ConsentimentoLgpdCheckBox.IsChecked == true ? "Cadastro de cliente" : string.Empty,
                AutorizaContatoWhatsApp = ConsentimentoLgpdCheckBox.IsChecked == true && AutorizaWhatsAppCheckBox.IsChecked == true,
                PontosFidelidade = Math.Max(0, pontos),
                Observacoes = ObservacoesTextBox.Text.Trim(),
                CaminhoDocumento = EscolherCaminhoPersistido(_caminhoDocumento, _clienteSalvo?.CaminhoDocumento),
                CaminhoAssinatura = EscolherCaminhoPersistido(_caminhoAssinatura, _clienteSalvo?.CaminhoAssinatura),
                ImagemUrl = ResolverImagemPersistida(clienteId, NomeTextBox.Text, _clienteSalvo?.ImagemUrl),
                Veiculos = _veiculos
                    .Where(v =>
                        !string.IsNullOrWhiteSpace(v.Marca) ||
                        !string.IsNullOrWhiteSpace(v.Modelo) ||
                        !string.IsNullOrWhiteSpace(v.Placa))
                    .ToList()
            };
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

        private void RecarregarVeiculosDoCliente()
        {
            if (_clienteSalvo == null)
            {
                return;
            }

            var veiculosDoCliente = App.Repositories.Clientes.ObterTodosVeiculos()
                .Where(v => v.ClienteId == _clienteSalvo.Id)
                .ToList();

            _veiculos.Clear();
            _veiculos.AddRange(veiculosDoCliente);
            VeiculosDataGrid.Items.Refresh();
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
            App.Logger.LogInfo($"Assinatura digital registrada para cliente '{nomeCliente}' em '{caminho}'.");

            return caminho;
        }

        private string CopiarArquivoCliente(string origem, string categoria)
        {
            var pasta = ObterPastaCliente(categoria);
            var nomeBase = NormalizarNomeArquivo(NomeTextBox.Text);
            var extensao = Path.GetExtension(origem);
            var destino = Path.Combine(pasta, $"{DateTime.Now:yyyyMMddHHmmss}_{nomeBase}{extensao}");

            File.Copy(origem, destino, overwrite: false);
            App.Logger.LogInfo($"Arquivo de cliente anexado em '{destino}'.");

            return destino;
        }

        private static string ObterPastaCliente(string categoria)
        {
            var raiz = App.IsAutomatedTestMode
                ? App.RuntimeAppDataPath
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PrimoAutoEletrica");
            var pasta = Path.Combine(raiz, "Clientes", categoria);

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

        private static string EscolherCaminhoPersistido(string caminhoAtual, string? caminhoExistente)
        {
            return !string.IsNullOrWhiteSpace(caminhoAtual)
                ? caminhoAtual
                : caminhoExistente ?? string.Empty;
        }

        private string CriarArquivoAutomacao(string nomeArquivo, string conteudo)
        {
            var pasta = ObterPastaCliente("Automacao");
            var caminho = Path.Combine(pasta, nomeArquivo);
            File.WriteAllText(caminho, conteudo, Encoding.UTF8);
            App.Logger.LogInfo($"Arquivo sintetico de cliente gerado em '{caminho}'.", "Clientes");
            return caminho;
        }

        private string ResolverImagemPersistida(Guid clienteId, string nomeCliente, string? imagemExistente)
        {
            var imagemExistenteResolvida = ClienteMediaService.ResolveExistingPath(imagemExistente);
            if (string.IsNullOrWhiteSpace(_caminhoFotoSelecionada))
            {
                if (!string.IsNullOrWhiteSpace(_imagemPendenteParaExcluir) && !string.IsNullOrWhiteSpace(imagemExistenteResolvida))
                {
                    return string.Empty;
                }

                return imagemExistente ?? string.Empty;
            }

            if (string.Equals(
                ClienteMediaService.ResolveExistingPath(_caminhoFotoSelecionada),
                imagemExistenteResolvida,
                StringComparison.OrdinalIgnoreCase))
            {
                return imagemExistente ?? string.Empty;
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
            var preview = ClienteMediaService.TryCreatePreviewSource(caminho ?? _clienteSalvo?.ImagemUrl);
            FotoPreviewImage.Source = preview;
            FotoPreviewImage.Visibility = preview == null ? Visibility.Collapsed : Visibility.Visible;
            FotoPlaceholderText.Visibility = preview == null ? Visibility.Visible : Visibility.Collapsed;
            RemoverFotoButton.IsEnabled = preview != null || !string.IsNullOrWhiteSpace(_caminhoFotoSelecionada);
        }

        private void AtualizarResumoAnexos()
        {
            DocumentoStatusTextBlock.Text = string.IsNullOrWhiteSpace(EscolherCaminhoPersistido(_caminhoDocumento, _clienteSalvo?.CaminhoDocumento))
                ? "Nenhum documento anexado."
                : "Documento pronto para consulta operacional.";
            AssinaturaStatusTextBlock.Text = string.IsNullOrWhiteSpace(EscolherCaminhoPersistido(_caminhoAssinatura, _clienteSalvo?.CaminhoAssinatura))
                ? "Nenhuma assinatura registrada."
                : "Assinatura digital vinculada ao cadastro.";
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
                    ? "LGPD registrado: contato ativo permitido conforme canais autorizados."
                    : "LGPD pendente: registre o aceite antes de campanhas ou contatos ativos.";
            }
        }

        private string ObterTipoPessoaSelecionado()
        {
            return (TipoPessoaComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() == "Juridica"
                ? "Juridica"
                : "Fisica";
        }

        private bool ObterStatusAtivoSelecionado()
        {
            return !string.Equals(
                (StatusClienteComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString(),
                "Inativo",
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
