using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using PdfSharpCore.Pdf.IO;
using PrimoAutoEletrica.Data.Repositories;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.UserControls;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views;
using PrimoAutoEletrica.Views.Clientes;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        // Checks de produtos, anexos, cadastro, etiquetas e helpers correlatos.

        private void RunProdutosCamposAnexosChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Produtos:CamposAnexosOperacionais", () =>
            {
                GarantirBancoIsoladoDoSmoke("campos, foto e anexos de produtos");
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do smoke test ainda nao foi inicializada.");
                var produto = App.Repositories.Produtos.ObterPorId(fixture.Produto.Id)
                    ?? throw new InvalidOperationException("Produto sintetico nao encontrado para validacao de campos completos.");

                var fichaTecnica = CriarArquivoProdutoSmoke(
                    "ficha-tecnica",
                    $"Ficha tecnica sintetica do produto {produto.Id}.{Environment.NewLine}Material: Cobre estanhado.");
                var garantiaFornecedor = CriarArquivoProdutoSmoke(
                    "garantia-fornecedor",
                    $"Garantia sintetica vinculada ao produto {produto.Id}.{Environment.NewLine}Prazo: 90 dias.");
                var fotoProduto = CriarImagemPngSmoke("foto-produto");

                produto.Cor = "Preto fosco";
                produto.Material = "Cobre estanhado";
                produto.Peso = "0,45 kg";
                produto.Dimensoes = "12x8x4 cm";
                produto.ImagemUrl = ProdutoMediaService.PersistSelectedImage(fotoProduto, produto.Id, produto.Nome);
                produto.Anexos = ProdutoMediaService.PersistSelectedAttachments(
                    new[] { fichaTecnica, garantiaFornecedor },
                    produto.Id,
                    produto.Nome);

                App.Repositories.Produtos.Atualizar(produto);

                var recarregado = App.Repositories.Produtos.ObterPorId(produto.Id)
                    ?? throw new InvalidOperationException("Produto com campos completos nao foi recarregado.");

                if (!string.Equals(recarregado.Cor, "Preto fosco", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(recarregado.Material, "Cobre estanhado", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Cor e material do produto nao persistiram corretamente.");
                }

                var anexos = ProdutoMediaService.DeserializeAttachmentPaths(recarregado.Anexos);
                if (anexos.Count != 2 || anexos.Any(path => !File.Exists(path)))
                {
                    throw new InvalidOperationException($"Anexos do produto ficaram inconsistentes. Quantidade={anexos.Count}.");
                }

                if (anexos.Any(path => !ProdutoMediaService.IsSupportedAttachmentFile(path)))
                {
                    throw new InvalidOperationException("Um anexo persistido ficou com extensao nao suportada.");
                }

                if (!File.Exists(recarregado.ImagemUrl) || ProdutoMediaService.TryCreatePreviewSource(recarregado.ImagemUrl) == null)
                {
                    throw new InvalidOperationException("A foto do produto nao foi persistida ou nao pode ser carregada.");
                }

                var imagemAntesEdicao = Path.GetFullPath(recarregado.ImagemUrl);
                var anexosAntesEdicao = anexos.Select(Path.GetFullPath).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList();
                var observacaoEdicao = $"Edicao pela tela validada em {DateTime.Now:yyyy-MM-dd HH:mm:ss}.";

                var novoProdutoWindow = new NovoProdutoWindow();
                try
                {
                    PrepareWindow(novoProdutoWindow);
                }
                finally
                {
                    novoProdutoWindow.Close();
                }

                var editarProdutoWindow = new EditarProdutoWindow(recarregado);
                try
                {
                    ShowWindowForInteraction(editarProdutoWindow);
                    SelectTabByHeader(editarProdutoWindow, "Anexos");

                    var anexosListBox = FindElementByName<ListBox>(editarProdutoWindow, "AnexosListBox")
                        ?? throw new InvalidOperationException("Lista de anexos nao foi localizada na edicao do produto.");
                    if (anexosListBox.Items.Count != 2)
                    {
                        throw new InvalidOperationException($"A edicao do produto exibiu {anexosListBox.Items.Count} anexo(s), mas eram esperados 2.");
                    }

                    anexosListBox.SelectedIndex = 0;
                    WaitForUiIdle();
                    ClickButton(editarProdutoWindow, "AbrirAnexoProdutoButton");

                    SetTextBoxValue(editarProdutoWindow, "ObservacoesTextBox", observacaoEdicao);
                    ClickButton(editarProdutoWindow, "SalvarProdutoButton");
                    WaitForCondition(
                        () => !editarProdutoWindow.IsVisible,
                        TimeSpan.FromSeconds(5),
                        "A janela de edicao do produto nao fechou depois de salvar.");
                }
                finally
                {
                    if (editarProdutoWindow.IsVisible)
                    {
                        editarProdutoWindow.Close();
                    }
                }

                var editado = App.Repositories.Produtos.ObterPorId(produto.Id)
                    ?? throw new InvalidOperationException("Produto editado pela tela nao foi recarregado.");
                var anexosDepoisEdicao = ProdutoMediaService.DeserializeAttachmentPaths(editado.Anexos)
                    .Select(Path.GetFullPath)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (!string.Equals(editado.Observacoes, observacaoEdicao, StringComparison.Ordinal) ||
                    !string.Equals(Path.GetFullPath(editado.ImagemUrl), imagemAntesEdicao, StringComparison.OrdinalIgnoreCase) ||
                    !anexosAntesEdicao.SequenceEqual(anexosDepoisEdicao, StringComparer.OrdinalIgnoreCase) ||
                    anexosDepoisEdicao.Any(path => !File.Exists(path)))
                {
                    throw new InvalidOperationException("A edicao pela tela nao preservou corretamente a foto, os anexos ou a observacao.");
                }
            });
        }

        private void RunProdutosCadastroCompletoChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Produtos:CadastroCompletoPelaTela", () =>
            {
                GarantirBancoIsoladoDoSmoke("cadastro completo de produto pela tela");
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do produto nao foi preparada.");
                var token = DateTime.Now.ToString("HHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
                var codigo = $"CAD-{token}";
                var nome = $"Produto Cadastro Completo {token}";
                var foto = CriarImagemPngSmoke("produto-cadastro");
                var ficha = CriarArquivoProdutoSmoke("manual-cadastro", $"Manual sintetico do produto {codigo}.");
                var garantia = CriarArquivoProdutoSmoke("garantia-cadastro", $"Garantia sintetica do produto {codigo}.");
                var window = new NovoProdutoWindow();

                try
                {
                    ShowWindowForInteraction(window);
                    SetTextBoxValue(window, "CodigoTextBox", codigo);
                    SetTextBoxValue(window, "NomeTextBox", nome);
                    DefinirComboBoxTexto(window, "CategoriaComboBox", "Eletrica");
                    DefinirComboBoxTexto(window, "UnidadeMedidaComboBox", "UN");
                    SetTextBoxValue(window, "MarcaTextBox", "Smoke Marca");
                    SetTextBoxValue(window, "ModeloTextBox", "Modulo 12V");
                    SetTextBoxValue(window, "DescricaoTextBox", "Produto cadastrado pela janela real durante smoke test.");

                    SelectTabByHeader(window, "Foto e Identificacao");
                    SetTextBoxValue(window, "CodigoBarrasTextBox", token.PadLeft(13, '0')[..13]);
                    SetTextBoxValue(window, "SkuTextBox", $"SKU-CAD-{token}");
                    SetTextBoxValue(window, "CorTextBox", "Preto fosco");
                    SetTextBoxValue(window, "MaterialTextBox", "Cobre estanhado");
                    SetTextBoxValue(window, "PesoTextBox", "0,45 kg");
                    SetTextBoxValue(window, "DimensoesTextBox", "12x8x4 cm");
                    window.CarregarMidiasParaAutomacao(foto, new[] { ficha, garantia });

                    SelectTabByHeader(window, "Estoque e Localizacao");
                    SetTextBoxValue(window, "QuantidadeTextBox", "15");
                    SetTextBoxValue(window, "QuantidadeMinimaTextBox", "2");
                    SetTextBoxValue(window, "QuantidadeMaximaTextBox", "40");
                    SetTextBoxValue(window, "LocalizacaoTextBox", "A1");
                    SetTextBoxValue(window, "PrateleiraTextBox", "P1");
                    SetTextBoxValue(window, "GavetaTextBox", "G1");

                    SelectTabByHeader(window, "Precos e Margem");
                    SetTextBoxValue(window, "PrecoCompraTextBox", "10,50");
                    SetTextBoxValue(window, "PrecoVendaTextBox", "25,90");

                    SelectTabByHeader(window, "Fiscal");
                    SetTextBoxValue(window, "NcmsTextBox", "85364100");
                    SetTextBoxValue(window, "CestTextBox", "0100100");
                    SetTextBoxValue(window, "CfopTextBox", "5102");

                    SelectTabByHeader(window, "Fornecedor");
                    SetTextBoxValue(window, "FornecedorTextBox", fixture.Fornecedor.NomeFantasia);
                    SetTextBoxValue(window, "CnpjFornecedorTextBox", fixture.Fornecedor.CNPJ);
                    SetTextBoxValue(window, "ContatoFornecedorTextBox", fixture.Fornecedor.Email);
                    SetTextBoxValue(window, "TelefoneFornecedorTextBox", fixture.Fornecedor.Telefone);

                    SelectTabByHeader(window, "Validade e Lote");
                    var perecivel = FindElementByName<CheckBox>(window, "ProdutoPerecivelCheckBox")
                        ?? throw new InvalidOperationException("ProdutoPerecivelCheckBox nao foi localizado.");
                    perecivel.IsChecked = true;
                    WaitForUiIdle();
                    var dataFabricacao = FindElementByName<DatePicker>(window, "DataFabricacaoDatePicker")
                        ?? throw new InvalidOperationException("DataFabricacaoDatePicker nao foi localizado.");
                    var dataValidade = FindElementByName<DatePicker>(window, "DataValidadeDatePicker")
                        ?? throw new InvalidOperationException("DataValidadeDatePicker nao foi localizado.");
                    dataFabricacao.SelectedDate = DateTime.Today.AddDays(-10);
                    dataValidade.SelectedDate = DateTime.Today.AddYears(1);
                    SetTextBoxValue(window, "LoteTextBox", $"LOT-{token}");

                    SelectTabByHeader(window, "Observacoes");
                    SetTextBoxValue(window, "ObservacoesTextBox", "Cadastro completo validado pelo smoke test.");

                    ClickButton(window, "SalvarProdutoButton");
                    WaitForCondition(
                        () => !window.IsVisible && window.ProdutoCriado != null,
                        TimeSpan.FromSeconds(5),
                        "A janela de novo produto nao concluiu o cadastro completo.");

                    var produtoCriado = App.Repositories.Produtos.ObterTodos()
                        .FirstOrDefault(produto => string.Equals(produto.Codigo, codigo, StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidOperationException("Produto cadastrado pela tela nao foi localizado no repositorio.");
                    var anexos = ProdutoMediaService.DeserializeAttachmentPaths(produtoCriado.Anexos);

                    if (!string.Equals(produtoCriado.Nome, nome, StringComparison.Ordinal) ||
                        !string.Equals(produtoCriado.SKU, $"SKU-CAD-{token}", StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(produtoCriado.NCMS, "85364100", StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(produtoCriado.CEST, "0100100", StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(produtoCriado.CFOP, "5102", StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(produtoCriado.Cor, "Preto fosco", StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(produtoCriado.Material, "Cobre estanhado", StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(produtoCriado.Peso, "0,45 kg", StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(produtoCriado.Dimensoes, "12x8x4 cm", StringComparison.OrdinalIgnoreCase) ||
                        produtoCriado.QuantidadeEstoque != 15 ||
                        produtoCriado.PrecoCompra != 10.50m ||
                        produtoCriado.PrecoVenda != 25.90m ||
                        !File.Exists(produtoCriado.ImagemUrl) ||
                        ProdutoMediaService.TryCreatePreviewSource(produtoCriado.ImagemUrl) == null ||
                        anexos.Count != 2 ||
                        anexos.Any(path => !File.Exists(path)))
                    {
                        throw new InvalidOperationException("Produto completo cadastrado pela tela ficou inconsistente apos persistencia.");
                    }
                }
                finally
                {
                    if (window.IsVisible)
                    {
                        window.Close();
                    }
                }
            });
        }

        private void RunProdutosEtiquetaPdfChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Produtos:EtiquetaPdfPelaTela", () =>
            {
                GarantirBancoIsoladoDoSmoke("geracao de etiqueta PDF de produtos");
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do estoque nao foi preparada.");
                var diretorio = Path.Combine(App.RuntimeLogDirectory, "produtos-smoke");
                Directory.CreateDirectory(diretorio);
                var inicio = DateTime.Now.AddSeconds(-1);
                var hostWindow = CreateHostWindow(new EstoqueControl(), nameof(EstoqueControl));

                try
                {
                    ShowWindowForInteraction(hostWindow);
                    if (hostWindow.Content is not EstoqueControl control)
                    {
                        throw new InvalidOperationException("Host de EstoqueControl nao conseguiu carregar a geracao de etiquetas.");
                    }

                    var dataGrid = FindElementByName<DataGrid>(control, "ProdutosDataGrid")
                        ?? throw new InvalidOperationException("ProdutosDataGrid nao foi localizado para gerar a etiqueta.");

                    WaitForCondition(
                        () => LocalizarProdutoNoEstoque(dataGrid, fixture.Produto.Id) != null,
                        TimeSpan.FromSeconds(5),
                        "O produto sintetico nao apareceu na grade para gerar a etiqueta.");

                    SelecionarProdutoNoEstoque(dataGrid, fixture.Produto.Id);
                    ClickButton(control, "EtiquetaProdutoButton");

                    var arquivo = Directory.EnumerateFiles(diretorio, "Etiqueta_*.pdf")
                        .Select(path => new FileInfo(path))
                        .Where(info => info.LastWriteTime >= inicio)
                        .OrderByDescending(info => info.LastWriteTime)
                        .FirstOrDefault()
                        ?? throw new InvalidOperationException("A etiqueta PDF nao foi criada pela tela.");

                    if (arquivo.Length < 1000)
                    {
                        throw new InvalidOperationException($"A etiqueta PDF foi criada vazia ou incompleta. Tamanho={arquivo.Length} bytes.");
                    }

                    using var stream = arquivo.OpenRead();
                    var assinatura = new byte[5];
                    if (stream.Read(assinatura, 0, assinatura.Length) != assinatura.Length ||
                        Encoding.ASCII.GetString(assinatura) != "%PDF-")
                    {
                        throw new InvalidOperationException("O arquivo de etiqueta gerado nao possui uma assinatura PDF valida.");
                    }
                }
                finally
                {
                    if (hostWindow.IsVisible)
                    {
                        hostWindow.Close();
                    }
                }
            });
        }

        private static bool SelecionarClienteNaGrade(DataGrid dataGrid, Guid clienteId)
        {
            var item = dataGrid.Items
                .Cast<object>()
                .FirstOrDefault(candidate =>
                    candidate.GetType()
                        .GetProperty("Cliente", BindingFlags.Instance | BindingFlags.Public)
                        ?.GetValue(candidate) is Cliente cliente &&
                    cliente.Id == clienteId);

            if (item == null)
            {
                return false;
            }

            dataGrid.SelectedItem = item;
            dataGrid.ScrollIntoView(item);
            if (dataGrid.Columns.Count > 0)
            {
                dataGrid.CurrentCell = new DataGridCellInfo(item, dataGrid.Columns[0]);
            }

            WaitForUiIdle();
            return true;
        }

        private static bool ExisteAuditoriaClienteDesde(DateTime inicio, Guid clienteId, string acao)
        {
            using var connection = App.Database.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(1)
                FROM AuditLogs
                WHERE Categoria = 'Clientes'
                  AND Acao = @Acao
                  AND Entidade = 'Cliente'
                  AND EntidadeId = @ClienteId
                  AND Sucesso = 1
                  AND DataHora >= @Inicio;";
            command.Parameters.AddWithValue("@Acao", acao);
            command.Parameters.AddWithValue("@ClienteId", clienteId.ToString());
            command.Parameters.AddWithValue("@Inicio", inicio.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

    }
}
