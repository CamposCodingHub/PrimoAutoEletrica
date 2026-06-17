using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.Views
{
    public partial class EditarFornecedorWindow : Window
    {
        private readonly IFornecedorRepository _fornecedorRepository;
        private readonly Fornecedor _fornecedorOriginal;

        public EditarFornecedorWindow(Fornecedor fornecedor)
            : this(global::PrimoAutoEletrica.App.Repositories.Fornecedores, fornecedor)
        {
        }

        public EditarFornecedorWindow(DatabaseService databaseService, Fornecedor fornecedor)
            : this(global::PrimoAutoEletrica.App.Repositories.Fornecedores, fornecedor)
        {
        }

        public EditarFornecedorWindow(IFornecedorRepository fornecedorRepository, Fornecedor fornecedor)
        {
            InitializeComponent();
            _fornecedorRepository = fornecedorRepository;
            _fornecedorOriginal = fornecedor;

            CarregarDadosFornecedor();
        }

        private void CarregarDadosFornecedor()
        {
            RazaoSocialTextBox.Text = _fornecedorOriginal.RazaoSocial;
            NomeFantasiaTextBox.Text = _fornecedorOriginal.NomeFantasia;
            CNPJTextBox.Text = _fornecedorOriginal.CNPJ;
            InscricaoEstadualTextBox.Text = _fornecedorOriginal.InscricaoEstadual;
            TelefoneTextBox.Text = _fornecedorOriginal.Telefone;
            CelularTextBox.Text = _fornecedorOriginal.Celular;
            WhatsAppVendedorTextBox.Text = _fornecedorOriginal.WhatsAppVendedor;
            EmailTextBox.Text = _fornecedorOriginal.Email;
            SiteTextBox.Text = _fornecedorOriginal.Site;
            CEPTextBox.Text = _fornecedorOriginal.CEP;
            RuaTextBox.Text = _fornecedorOriginal.Rua;
            NumeroTextBox.Text = _fornecedorOriginal.Numero;
            BairroTextBox.Text = _fornecedorOriginal.Bairro;
            CidadeTextBox.Text = _fornecedorOriginal.Cidade;
            EstadoTextBox.Text = _fornecedorOriginal.Estado;
            PrazoPagamentoTextBox.Text = _fornecedorOriginal.PrazoPagamento;
            PedidoMinimoTextBox.Text = _fornecedorOriginal.PedidoMinimo.ToString();
            PrazoMedioPagamentoTextBox.Text = _fornecedorOriginal.PrazoMedioPagamentoDias.ToString();
            PrazoMedioEntregaTextBox.Text = _fornecedorOriginal.PrazoMedioEntregaDias.ToString();
            CategoriaPreferencialTextBox.Text = _fornecedorOriginal.CategoriaPreferencial;
            ObservacoesTextBox.Text = _fornecedorOriginal.Observacoes;
            AtivoCheckBox.IsChecked = _fornecedorOriginal.Ativo;
            NotaComboBox.Text = _fornecedorOriginal.Nota.ToString();

            var contatoPrincipal = _fornecedorOriginal.Contatos
                .OrderByDescending(contato => contato.Principal)
                .ThenBy(contato => contato.Nome)
                .FirstOrDefault();

            if (contatoPrincipal != null)
            {
                ContatoPrincipalNomeTextBox.Text = contatoPrincipal.Nome;
                ContatoPrincipalCargoTextBox.Text = contatoPrincipal.Cargo;
                ContatoPrincipalTelefoneTextBox.Text = contatoPrincipal.Telefone;
                ContatoPrincipalEmailTextBox.Text = contatoPrincipal.Email;
            }

            foreach (ComboBoxItem item in CategoriaComboBox.Items)
            {
                if (string.Equals(item.Content?.ToString(), _fornecedorOriginal.Categoria, StringComparison.OrdinalIgnoreCase))
                {
                    CategoriaComboBox.SelectedItem = item;
                    break;
                }
            }

            foreach (ComboBoxItem item in FormaPagamentoComboBox.Items)
            {
                if (string.Equals(item.Content?.ToString(), _fornecedorOriginal.FormaPagamento, StringComparison.OrdinalIgnoreCase))
                {
                    FormaPagamentoComboBox.SelectedItem = item;
                    break;
                }
            }
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
                _fornecedorOriginal.RazaoSocial = RazaoSocialTextBox.Text.Trim();
                _fornecedorOriginal.NomeFantasia = NomeFantasiaTextBox.Text.Trim();
                _fornecedorOriginal.CNPJ = CNPJTextBox.Text.Trim();
                _fornecedorOriginal.InscricaoEstadual = InscricaoEstadualTextBox.Text.Trim();
                _fornecedorOriginal.Telefone = TelefoneTextBox.Text.Trim();
                _fornecedorOriginal.Celular = CelularTextBox.Text.Trim();
                _fornecedorOriginal.WhatsAppVendedor = WhatsAppVendedorTextBox.Text.Trim();
                _fornecedorOriginal.Email = EmailTextBox.Text.Trim();
                _fornecedorOriginal.Site = SiteTextBox.Text.Trim();
                _fornecedorOriginal.CEP = CEPTextBox.Text.Trim();
                _fornecedorOriginal.Rua = RuaTextBox.Text.Trim();
                _fornecedorOriginal.Numero = NumeroTextBox.Text.Trim();
                _fornecedorOriginal.Complemento = string.Empty;
                _fornecedorOriginal.Bairro = BairroTextBox.Text.Trim();
                _fornecedorOriginal.Cidade = CidadeTextBox.Text.Trim();
                _fornecedorOriginal.Estado = EstadoTextBox.Text.Trim();
                _fornecedorOriginal.Categoria = CategoriaComboBox.Text?.Trim() ?? "Pecas";
                _fornecedorOriginal.CategoriaPreferencial = string.IsNullOrWhiteSpace(CategoriaPreferencialTextBox.Text)
                    ? _fornecedorOriginal.Categoria
                    : CategoriaPreferencialTextBox.Text.Trim();
                _fornecedorOriginal.FormaPagamento = FormaPagamentoComboBox.Text?.Trim() ?? string.Empty;
                _fornecedorOriginal.PrazoPagamento = PrazoPagamentoTextBox.Text.Trim();
                _fornecedorOriginal.PrazoMedioPagamentoDias = prazoMedioPagamento;
                _fornecedorOriginal.PrazoMedioEntregaDias = prazoMedioEntrega;
                _fornecedorOriginal.PedidoMinimo = pedidoMinimo;
                _fornecedorOriginal.Ativo = AtivoCheckBox.IsChecked == true;
                _fornecedorOriginal.Nota = nota;
                _fornecedorOriginal.Observacoes = ObservacoesTextBox.Text.Trim();
                _fornecedorOriginal.Contatos = AtualizarContatoPrincipal();

                _fornecedorRepository.Atualizar(_fornecedorOriginal);

                WindowInteractionHelper.ShowMessage(
                    $"Fornecedor {_fornecedorOriginal.NomeFantasia} atualizado com sucesso!",
                    "Sucesso",
                    MessageBoxImage.Information,
                    "Fornecedores");

                WindowInteractionHelper.CloseWithDialogResult(this, true, "Fornecedores");
            }
            catch (Exception ex)
            {
                WindowInteractionHelper.ShowMessage(
                    $"Erro ao salvar fornecedor:\n{ex.Message}",
                    "Erro",
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
                return ExibirErroValidacao("Informe a razao social do fornecedor.", RazaoSocialTextBox, "Campo obrigatorio");
            }

            if (string.IsNullOrWhiteSpace(NomeFantasiaTextBox.Text))
            {
                return ExibirErroValidacao("Informe o nome fantasia do fornecedor.", NomeFantasiaTextBox, "Campo obrigatorio");
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

        private List<ContatoFornecedor> AtualizarContatoPrincipal()
        {
            var contatosSecundarios = _fornecedorOriginal.Contatos
                .Where(contato => !contato.Principal)
                .ToList();

            if (string.IsNullOrWhiteSpace(ContatoPrincipalNomeTextBox.Text) &&
                string.IsNullOrWhiteSpace(ContatoPrincipalTelefoneTextBox.Text) &&
                string.IsNullOrWhiteSpace(ContatoPrincipalEmailTextBox.Text))
            {
                return contatosSecundarios;
            }

            contatosSecundarios.Insert(0, new ContatoFornecedor
            {
                FornecedorId = _fornecedorOriginal.Id,
                Nome = ContatoPrincipalNomeTextBox.Text.Trim(),
                Cargo = ContatoPrincipalCargoTextBox.Text.Trim(),
                Telefone = ContatoPrincipalTelefoneTextBox.Text.Trim(),
                Email = ContatoPrincipalEmailTextBox.Text.Trim(),
                Principal = true
            });

            return contatosSecundarios;
        }

        private static bool ExibirErroValidacao(string mensagem, Control campo, string titulo)
        {
            WindowInteractionHelper.ShowMessage(mensagem, titulo, MessageBoxImage.Warning, "Fornecedores");
            campo.Focus();
            return false;
        }
    }
}
