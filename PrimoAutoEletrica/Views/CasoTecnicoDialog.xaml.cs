using System.Windows;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Views
{
    public partial class CasoTecnicoDialog : Window
    {
        public CasoTecnicoDialog(TechnicalKnowledgeEntry artigo)
        {
            InitializeComponent();

            CodigoTextBlock.Text = artigo.Code;
            TensaoBadgeTextBlock.Text = artigo.Voltage;
            SistemaBadgeTextBlock.Text = artigo.System;
            TituloTextBlock.Text = artigo.Title;
            SubtituloTextBlock.Text = $"Artigo Técnico • Categoria: {artigo.VehicleCategory}";

            AplicacaoTextBlock.Text = $"Categoria: {artigo.VehicleCategory}\nTags: {artigo.Tags ?? "Geral"}";

            SintomaTextBlock.Text = artigo.Symptom;
            MedicoesTextBlock.Text = string.IsNullOrWhiteSpace(artigo.DiagnosticProcedure)
                ? "Procedimento padrão de verificação."
                : artigo.DiagnosticProcedure;

            CausaConfirmadaTextBlock.Text = artigo.PossibleCauses;
            SolucaoTextBlock.Text = artigo.Solution;

            PecasFerramentasTextBlock.Text =
                $"Medições Recomendadas: {artigo.RecommendedMeasurements ?? "Multímetro True RMS / Osciloscópio"}\nAvisos Importantes: {artigo.Warnings ?? "Nenhum aviso crítico"}";

            RodapeInfoTextBlock.Text = $"Criado em: {artigo.CreatedAt:dd/MM/yyyy} • Status: {artigo.Status}";
        }

        public CasoTecnicoDialog(DiagnosticCase caso)
        {
            InitializeComponent();

            CodigoTextBlock.Text = caso.Code;
            TensaoBadgeTextBlock.Text = caso.Voltage;
            SistemaBadgeTextBlock.Text = caso.System;
            TituloTextBlock.Text = caso.Title;
            SubtituloTextBlock.Text = $"Caso Real de Oficina • OS: {caso.WorkOrderNumber ?? "N/A"} • Técnico: {caso.TechnicianName}";

            AplicacaoTextBlock.Text =
                $"Veículo: {caso.VehicleModel}\nPlaca: {caso.VehiclePlate ?? "Não informada"}\nCódigos DTC: {caso.DtcCodes ?? "Nenhum código gravado"}";

            SintomaTextBlock.Text = caso.Symptom;
            MedicoesTextBlock.Text = string.IsNullOrWhiteSpace(caso.Measurements)
                ? (caso.InitialHypotheses ?? "Nenhuma medição instrumental registrada.")
                : caso.Measurements;

            CausaConfirmadaTextBlock.Text = caso.ConfirmedCause;
            SolucaoTextBlock.Text = caso.Solution;

            PecasFerramentasTextBlock.Text =
                $"Peças Aplicadas: {caso.PartsUsed ?? "Nenhuma peça informada"}\nResultado dos Ensaios: {caso.TestResult ?? "Aprovado"}";

            RodapeInfoTextBlock.Text = $"Caso comprovado em bancada/oficina • Registrado em: {caso.CreatedAt:dd/MM/yyyy HH:mm}";
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
