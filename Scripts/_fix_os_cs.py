from pathlib import Path

cs = Path('PrimoAutoEletrica/Views/OrdemServicoWindow.xaml.cs')
t = cs.read_text(encoding='utf-8')
if 'using PrimoAutoEletrica.Services;' not in t:
    t = 'using PrimoAutoEletrica.Services;\n' + t

methods = r'''
        private OrdemServico SincronizarOrdemDaTelaParaDocumento()
        {
            var ordem = _ordemEmEdicao ?? new OrdemServico();
            if (ClienteComboBox.SelectedItem is Cliente cliente)
            {
                ordem.ClienteId = cliente.Id;
                ordem.ClienteNomeSnapshot = cliente.Nome;
            }

            ordem.Numero = (NumeroTextBlock.Text ?? string.Empty).Trim();
            ordem.VeiculoId = (VeiculoComboBox.SelectedItem as Veiculo)?.Id;
            ordem.TecnicoId = (TecnicoComboBox.SelectedItem as Funcionario)?.Id;
            ordem.TelefoneClienteSnapshot = (TelefoneClienteTextBox.Text ?? string.Empty).Trim();
            ordem.VeiculoDescricaoSnapshot = (VeiculoDescricaoTextBox.Text ?? string.Empty).Trim();
            ordem.PlacaSnapshot = (PlacaTextBox.Text ?? string.Empty).Trim();
            ordem.ProblemaRelatado = (ProblemaTextBox.Text ?? string.Empty).Trim();
            ordem.ChecklistEntrada = (ChecklistEntradaTextBox.Text ?? string.Empty).Trim();
            ordem.ChecklistEntrega = (ChecklistEntregaTextBox.Text ?? string.Empty).Trim();
            ordem.ChecklistSaida = (ChecklistSaidaTextBox.Text ?? string.Empty).Trim();
            ordem.GarantiaObservacoes = (GarantiaObservacoesTextBox.Text ?? string.Empty).Trim();
            ordem.GarantiaValidaAte = GarantiaValidaAteDatePicker.SelectedDate?.Date.AddHours(18);
            return ordem;
        }

        private void GerarChecklistPdfOsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ordem = SincronizarOrdemDaTelaParaDocumento();
                var path = CommercialDocumentActions.GerarChecklistOs(ordem);
                CommercialDocumentActions.AbrirArquivo(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Checklist PDF", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GerarTermoGarantiaOsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ordem = SincronizarOrdemDaTelaParaDocumento();
                CommercialDocumentActions.AplicarGarantiaPadraoSeVazia(ordem);
                if (ordem.GarantiaValidaAte.HasValue)
                {
                    GarantiaValidaAteDatePicker.SelectedDate = ordem.GarantiaValidaAte.Value.Date;
                }

                if (!string.IsNullOrWhiteSpace(ordem.GarantiaObservacoes))
                {
                    GarantiaObservacoesTextBox.Text = ordem.GarantiaObservacoes;
                }

                var path = CommercialDocumentActions.GerarTermoGarantiaOs(ordem);
                CommercialDocumentActions.AbrirArquivo(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Termo garantia", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnviarDocsWhatsAppOsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ordem = SincronizarOrdemDaTelaParaDocumento();
                CommercialDocumentActions.AplicarGarantiaPadraoSeVazia(ordem);
                var checklist = CommercialDocumentActions.GerarChecklistOs(ordem);
                var garantia = CommercialDocumentActions.GerarTermoGarantiaOs(ordem);
                var msg = $"Documentos da OS {ordem.Numero}: checklist e termo de garantia.";
                CommercialDocumentActions.EnviarWhatsAppArquivo(ordem.TelefoneClienteSnapshot, msg, garantia);
                CommercialDocumentActions.AbrirArquivo(checklist);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "WhatsApp docs", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

'''

# Also patch SalvarOrdem garantia
needle = 'ordem.GarantiaValidaAte = GarantiaValidaAteDatePicker.SelectedDate?.Date.AddHours(18);'
if needle in t and 'CommercialDocumentActions.AplicarGarantiaPadraoSeVazia(ordem)' not in t:
    t = t.replace(
        needle,
        needle + '\n            CommercialDocumentActions.AplicarGarantiaPadraoSeVazia(ordem);\n            if (ordem.GarantiaValidaAte.HasValue)\n            {\n                GarantiaValidaAteDatePicker.SelectedDate = ordem.GarantiaValidaAte.Value.Date;\n            }\n            if (!string.IsNullOrWhiteSpace(ordem.GarantiaObservacoes))\n            {\n                GarantiaObservacoesTextBox.Text = ordem.GarantiaObservacoes;\n            }',
        1,
    )
    print('patched salvar garantia')

nested = t.find('    public sealed class OrdemServicoItemEditor')
if nested < 0:
    raise SystemExit('nested missing')
t = t[:nested] + methods + '\n' + t[nested:]
cs.write_text(t, encoding='utf-8')
print('rewrote OS cs methods cleanly')

# Fix DocumentoPdfService laudo call if broken
p2 = Path('PrimoAutoEletrica/Services/DocumentoPdfService.cs')
t2 = p2.read_text(encoding='utf-8')
# show end of laudo method
i = t2.find('LAUDO TECNICO')
j = t2.find('public string GerarTermoGarantia', i)
print('LAUDO TAIL:')
print(t2[j-400:j])
