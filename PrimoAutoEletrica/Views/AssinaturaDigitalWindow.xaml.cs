using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PrimoAutoEletrica.Views
{
    public partial class AssinaturaDigitalWindow : Window
    {
        private readonly string _contexto;

        public string SignaturePath { get; private set; } = string.Empty;

        public AssinaturaDigitalWindow(string contexto = "assinatura")
        {
            InitializeComponent();
            _contexto = string.IsNullOrWhiteSpace(contexto) ? "assinatura" : contexto.Trim();
            TituloTextBlock.Text = $"Assinatura digital - {_contexto}";
            SignatureInkCanvas.StrokeCollected += (_, _) =>
            {
                AjudaTextBlock.Visibility = SignatureInkCanvas.Strokes.Count == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
                StatusTextBlock.Text = $"{SignatureInkCanvas.Strokes.Count} traco(s) capturado(s).";
            };
        }

        private void LimparButton_Click(object sender, RoutedEventArgs e)
        {
            SignatureInkCanvas.Strokes.Clear();
            AjudaTextBlock.Visibility = Visibility.Visible;
            StatusTextBlock.Text = "Assinatura limpa.";
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SalvarButton_Click(object sender, RoutedEventArgs e)
        {
            if (SignatureInkCanvas.Strokes.Count == 0)
            {
                if (App.IsAutomatedTestMode)
                {
                    GerarAssinaturaAutomatica();
                }
                else
                {
                    MessageBox.Show(
                        "Assine dentro da area branca antes de salvar.",
                        "Assinatura vazia",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }
            }

            SignaturePath = SalvarImagemAssinatura();
            try
            {
                DialogResult = true;
            }
            catch (InvalidOperationException)
            {
                // A janela tambem e usada em smoke com Show(); nesse modo basta fechar com SignaturePath preenchido.
            }
            Close();
        }

        private void GerarAssinaturaAutomatica()
        {
            var stylusPoints = new System.Windows.Input.StylusPointCollection();
            stylusPoints.Add(new System.Windows.Input.StylusPoint(80, 120));
            stylusPoints.Add(new System.Windows.Input.StylusPoint(170, 90));
            stylusPoints.Add(new System.Windows.Input.StylusPoint(260, 130));
            stylusPoints.Add(new System.Windows.Input.StylusPoint(360, 86));
            stylusPoints.Add(new System.Windows.Input.StylusPoint(470, 125));

            SignatureInkCanvas.Strokes.Add(new System.Windows.Ink.Stroke(stylusPoints)
            {
                DrawingAttributes =
                {
                    Color = Colors.Black,
                    Width = 3,
                    Height = 3
                }
            });
        }

        private string SalvarImagemAssinatura()
        {
            SignatureInkCanvas.UpdateLayout();
            var width = Math.Max(1, (int)SignatureInkCanvas.ActualWidth);
            var height = Math.Max(1, (int)SignatureInkCanvas.ActualHeight);

            var renderBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(SignatureInkCanvas);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            var pasta = Path.Combine(App.RuntimeAppDataPath, "AssinaturasDigitais");
            Directory.CreateDirectory(pasta);

            var nomeSeguro = string.Concat(_contexto.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            if (string.IsNullOrWhiteSpace(nomeSeguro))
            {
                nomeSeguro = "assinatura";
            }

            var caminho = Path.Combine(pasta, $"{nomeSeguro}-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.png");
            using var stream = File.Create(caminho);
            encoder.Save(stream);
            return caminho;
        }
    }
}
