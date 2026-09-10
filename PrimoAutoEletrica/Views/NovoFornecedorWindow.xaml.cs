using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.Views
{
    public partial class NovoFornecedorWindow : Window
    {
        private readonly IFornecedorRepository _fornecedorRepository;

        public NovoFornecedorWindow()
            : this(global::PrimoAutoEletrica.App.Repositories.Fornecedores)
        {
        }

        public NovoFornecedorWindow(DatabaseService databaseService)
            : this(global::PrimoAutoEletrica.App.Repositories.Fornecedores)
        {
        }

        public NovoFornecedorWindow(IFornecedorRepository fornecedorRepository)
        {
            InitializeComponent();
            _fornecedorRepository = fornecedorRepository;
            CategoriaComboBox.SelectedIndex = 0;
            FormaPagamentoComboBox.SelectedIndex = 0;
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SalvarButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarCampos(out var pedidoMinimo, out var nota, out var prazoMedioPagamento, out var prazoMedioEntrega))
            {
                return;
            }

            try
            {
                var fornecedor = new Fornecedor
                {
                    RazaoSocial = RazaoSocialTextBox.Text.Trim(),
                    NomeFantasia = NomeFantasiaTextBox.Text.Trim(),
                    CNPJ = CNPJTextBox.Text.Trim(),
                    InscricaoEstadual = InscricaoEstadualTextBox.Text.Trim(),
                    Telefone = TelefoneTextBox.Text.Trim(),
                    Celular = CelularTextBox.Text.Trim(),
                    WhatsAppVendedor = WhatsAppVendedorTextBox.Text.Trim(),
                    Email = EmailTextBox.Text.Trim(),
                    Site = SiteTextBox.Text.Trim(),
                    CEP = CEPTextBox.Text.Trim(),
                    Rua = RuaTextBox.Text.Trim(),
                    Numero = NumeroTextBox.Text.Trim(),
                    Complemento = string.Empty,
                    Bairro = BairroTextBox.Text.Trim(),
                    Cidade = CidadeTextBox.Text.Trim(),
                    Estado = EstadoTextBox.Text.Trim(),
                    Categoria = CategoriaComboBox.Text?.Trim() ?? "Pecas",
                    CategoriaPreferencial = string.IsNullOrWhiteSpace(CategoriaPreferencialTextBox.Text) ? CategoriaComboBox.Text?.Trim() ?? string.Empty : CategoriaPreferencialTextBox.Text.Trim(),
                    FormaPagamento = FormaPagamentoComboBox.Text?.Trim() ?? string.Empty,
                    PrazoPagamento = PrazoPagamentoTextBox.Text.Trim(),
                    PrazoMedioPagamentoDias = prazoMedioPagamento,
                    PrazoMedioEntregaDias = prazoMedioEntrega,
                    PedidoMinimo = pedidoMinimo,
                    Ativo = AtivoCheckBox.IsChecked == true,
                    Nota = nota,
                    Observacoes = ObservacoesTextBox.Text.Trim(),
                    Contatos = CriarContatosPrincipais()
                };

                _fornecedorRepository.Inserir(fornecedor);

                WindowInteractionHelper.ShowMessage(
                    $"Fornecedor {fornecedor.NomeFantasia} salvo com sucesso!",
                    UiText.T("Success"),
                    MessageBoxImage.Information,
                    "Fornecedores");

                WindowInteractionHelper.CloseWithDialogResult(this, true, "Fornecedores");
            }
            catch (Exception ex)
            {
                WindowInteractionHelper.ShowMessage(
                    $"Erro ao salvar fornecedor:\n{ex.Message}",
                    UiText.T("Error"),
                    MessageBoxImage.Error,
                    "Fornecedores",
                    ex);
            }
        }

        private bool ValidarCampos(out decimal pedidoMinimo, out int nota, out int prazoMedioPagamento, out int prazoMedioEntrega)
        {
            pedidoMinimo = 0;
            nota = 0;
            prazoMedioPagamento = 0;
            prazoMedioEntrega = 0;

            if (string.IsNullOrWhiteSpace(RazaoSocialTextBox.Text))
            {
                return ExibirErroValidacao("Informe a razao social do fornecedor.", RazaoSocialTextBox, UiText.T("RequiredField"));
            }

            if (string.IsNullOrWhiteSpace(NomeFantasiaTextBox.Text))
            {
                return ExibirErroValidacao("Informe o nome fantasia do fornecedor.", NomeFantasiaTextBox, UiText.T("RequiredField"));
            }

            var erroCnpj = CadastroValidationHelper.ValidarCpfOuCnpj(CNPJTextBox.Text, "CPF ou CNPJ", obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroCnpj))
            {
                return ExibirErroValidacao(erroCnpj, CNPJTextBox, "Documento invalido");
            }

            var erroTelefone = CadastroValidationHelper.ValidarTelefone(TelefoneTextBox.Text, obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroTelefone))
            {
                return ExibirErroValidacao(erroTelefone, TelefoneTextBox, "Contato invalido");
            }

            var erroCelular = CadastroValidationHelper.ValidarTelefone(CelularTextBox.Text, "celular", obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroCelular))
            {
                return ExibirErroValidacao(erroCelular, CelularTextBox, "Contato invalido");
            }

            var erroWhatsApp = CadastroValidationHelper.ValidarTelefone(WhatsAppVendedorTextBox.Text, "celular", obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroWhatsApp))
            {
                return ExibirErroValidacao(erroWhatsApp, WhatsAppVendedorTextBox, "Contato invalido");
            }

            var erroEmail = CadastroValidationHelper.ValidarEmail(EmailTextBox.Text, obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroEmail))
            {
                return ExibirErroValidacao(erroEmail, EmailTextBox, "Contato invalido");
            }

            var erroEmailContato = CadastroValidationHelper.ValidarEmail(ContatoPrincipalEmailTextBox.Text, obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroEmailContato))
            {
                return ExibirErroValidacao(erroEmailContato, ContatoPrincipalEmailTextBox, "Contato invalido");
            }

            var erroTelefoneContato = CadastroValidationHelper.ValidarTelefone(ContatoPrincipalTelefoneTextBox.Text, obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroTelefoneContato))
            {
                return ExibirErroValidacao(erroTelefoneContato, ContatoPrincipalTelefoneTextBox, "Contato invalido");
            }

            if (!string.IsNullOrWhiteSpace(PedidoMinimoTextBox.Text) &&
                !decimal.TryParse(PedidoMinimoTextBox.Text, out pedidoMinimo))
            {
                return ExibirErroValidacao("Informe um pedido minimo valido.", PedidoMinimoTextBox, "Valor invalido");
            }

            var erroPedidoMinimo = CadastroValidationHelper.ValidarDecimal(pedidoMinimo, "um pedido minimo");
            if (!string.IsNullOrWhiteSpace(erroPedidoMinimo))
            {
                return ExibirErroValidacao(erroPedidoMinimo, PedidoMinimoTextBox, "Valor invalido");
            }

            if (!string.IsNullOrWhiteSpace(PrazoMedioEntregaTextBox.Text) &&
                !int.TryParse(PrazoMedioEntregaTextBox.Text, out prazoMedioEntrega))
            {
                return ExibirErroValidacao("Informe um prazo medio de entrega valido em dias.", PrazoMedioEntregaTextBox, "Valor invalido");
            }

            if (!string.IsNullOrWhiteSpace(PrazoMedioPagamentoTextBox.Text) &&
                !int.TryParse(PrazoMedioPagamentoTextBox.Text, out prazoMedioPagamento))
            {
                return ExibirErroValidacao("Informe um prazo medio de pagamento valido em dias.", PrazoMedioPagamentoTextBox, "Valor invalido");
            }

            _ = int.TryParse(NotaComboBox.Text, out nota);
            return true;
        }

        private List<ContatoFornecedor> CriarContatosPrincipais()
        {
            if (string.IsNullOrWhiteSpace(ContatoPrincipalNomeTextBox.Text) &&
                string.IsNullOrWhiteSpace(ContatoPrincipalTelefoneTextBox.Text) &&
                string.IsNullOrWhiteSpace(ContatoPrincipalEmailTextBox.Text))
            {
                return new();
            }

            return new()
            {
                new ContatoFornecedor
                {
                    Nome = ContatoPrincipalNomeTextBox.Text.Trim(),
                    Cargo = ContatoPrincipalCargoTextBox.Text.Trim(),
                    Telefone = ContatoPrincipalTelefoneTextBox.Text.Trim(),
                    Email = ContatoPrincipalEmailTextBox.Text.Trim(),
                    Principal = true
                }
            };
        }

        private static bool ExibirErroValidacao(string mensagem, Control campo, string titulo)
        {
            WindowInteractionHelper.ShowMessage(mensagem, titulo, MessageBoxImage.Warning, "Fornecedores");
            campo.Focus();
            return false;
        }
    }
}
