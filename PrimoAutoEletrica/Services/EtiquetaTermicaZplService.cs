using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services
{
    public interface IEtiquetaTermicaZplService
    {
        string GerarZplChaveVeiculo(string numeroOs, string placa, string cliente, string modeloVeiculo, string? tecnico, DateTime dataEntrada);
        string GerarZplPecaAlmoxarifado(string codigo, string descricao, decimal precoVenda, string? codigoBarras, string? localizacao, string? marca);
        string GerarZplFerramentaAtivo(string codigoPatrimonio, string nomeFerramenta, string? categoria, string? localizacao);
        Task<bool> ImprimirViaRedeTcpAsync(string ipImpressora, int porta, string zplConteudo);
        Task<string> SalvarArquivoZplAsync(string zplConteudo, string nomeArquivo);
    }

    public sealed class EtiquetaTermicaZplService : IEtiquetaTermicaZplService
    {
        private readonly LoggerService? _logger;

        public EtiquetaTermicaZplService(LoggerService? logger = null)
        {
            _logger = logger;
        }

        public string GerarZplChaveVeiculo(
            string numeroOs,
            string placa,
            string cliente,
            string modeloVeiculo,
            string? tecnico,
            DateTime dataEntrada)
        {
            var placaLimpa = (placa ?? "").Trim().ToUpperInvariant();
            var osLimpa = (numeroOs ?? "").Trim().ToUpperInvariant();
            var clienteCurto = cliente != null && cliente.Length > 24 ? cliente.Substring(0, 24) : (cliente ?? "");
            var modeloCurto = modeloVeiculo != null && modeloVeiculo.Length > 24 ? modeloVeiculo.Substring(0, 24) : (modeloVeiculo ?? "");
            var tec = string.IsNullOrWhiteSpace(tecnico) ? "Mecânica Geral" : tecnico;

            // ZPL II padrão para etiquetas de chaveiro / tag veicular (50mm x 30mm @ 203 DPI)
            var sb = new StringBuilder();
            sb.AppendLine("^XA");
            sb.AppendLine("^PW400"); // Largura em pontos (50mm ~ 400 dots)
            sb.AppendLine("^LL240"); // Altura em pontos (30mm ~ 240 dots)
            sb.AppendLine("^LH0,0");

            // Caixa delimitadora
            sb.AppendLine("^FO10,10^GB380,220,2^FS");

            // Placa do veículo em destaque grande
            sb.AppendLine($"^FO20,20^A0N,36,36^FD{placaLimpa}^FS");
            sb.AppendLine($"^FO220,24^A0N,22,22^FDOS: {osLimpa}^FS");

            // Modelo e Cliente
            sb.AppendLine($"^FO20,65^A0N,20,20^FD{modeloCurto}^FS");
            sb.AppendLine($"^FO20,90^A0N,18,18^FDCliente: {clienteCurto}^FS");
            sb.AppendLine($"^FO20,115^A0N,18,18^FDResp: {tec} | {dataEntrada:dd/MM HH:mm}^FS");

            // Código de barras Code 128 com o número da OS
            sb.AppendLine($"^FO40,140^BY2,2,45^BCN,45,Y,N,N^FD{osLimpa}^FS");

            sb.AppendLine("^XZ");
            return sb.ToString();
        }

        public string GerarZplPecaAlmoxarifado(
            string codigo,
            string descricao,
            decimal precoVenda,
            string? codigoBarras,
            string? localizacao,
            string? marca)
        {
            var codLimpo = (codigo ?? "").Trim();
            var descLinha1 = descricao != null && descricao.Length > 25 ? descricao.Substring(0, 25) : (descricao ?? "");
            var descLinha2 = descricao != null && descricao.Length > 25 ? (descricao.Length > 50 ? descricao.Substring(25, 25) : descricao.Substring(25)) : "";
            var codBarras = string.IsNullOrWhiteSpace(codigoBarras) ? codLimpo : codigoBarras.Trim();
            var loc = string.IsNullOrWhiteSpace(localizacao) ? "GERAL" : localizacao.Trim();
            var mar = string.IsNullOrWhiteSpace(marca) ? "PRIMOX" : marca.Trim();

            var sb = new StringBuilder();
            sb.AppendLine("^XA");
            sb.AppendLine("^PW440");
            sb.AppendLine("^LL280");
            sb.AppendLine("^LH0,0");

            // Borda
            sb.AppendLine("^FO10,10^GB420,260,2^FS");

            // Código e Marca
            sb.AppendLine($"^FO20,20^A0N,22,22^FD{codLimpo}^FS");
            sb.AppendLine($"^FO260,20^A0N,20,20^FD{mar}^FS");

            // Descrição da Peça
            sb.AppendLine($"^FO20,50^A0N,22,22^FD{descLinha1}^FS");
            if (!string.IsNullOrEmpty(descLinha2))
            {
                sb.AppendLine($"^FO20,74^A0N,20,20^FD{descLinha2}^FS");
            }

            // Localização no estoque
            sb.AppendLine($"^FO20,105^A0N,18,18^FDRua/Prat: {loc}^FS");

            // Preço destacado
            sb.AppendLine($"^FO240,100^A0N,30,30^FDR$ {precoVenda:N2}^FS");

            // Código de barras EAN / Code 128
            sb.AppendLine($"^FO40,145^BY2,2,60^BCN,60,Y,N,N^FD{codBarras}^FS");

            sb.AppendLine("^XZ");
            return sb.ToString();
        }

        public string GerarZplFerramentaAtivo(
            string codigoPatrimonio,
            string nomeFerramenta,
            string? categoria,
            string? localizacao)
        {
            var cod = (codigoPatrimonio ?? "").Trim().ToUpperInvariant();
            var nome = nomeFerramenta != null && nomeFerramenta.Length > 25 ? nomeFerramenta.Substring(0, 25) : (nomeFerramenta ?? "");
            var cat = string.IsNullOrWhiteSpace(categoria) ? "Ferramental" : categoria.Trim();
            var loc = string.IsNullOrWhiteSpace(localizacao) ? "Bancada Central" : localizacao.Trim();

            var sb = new StringBuilder();
            sb.AppendLine("^XA");
            sb.AppendLine("^PW400");
            sb.AppendLine("^LL220");
            sb.AppendLine("^LH0,0");

            sb.AppendLine("^FO10,10^GB380,200,2^FS");
            sb.AppendLine("^FO20,20^A0N,20,20^FDCONTROLE DE PATRIMÔNIO^FS");
            sb.AppendLine($"^FO20,45^A0N,26,26^FD{cod}^FS");
            sb.AppendLine($"^FO20,80^A0N,20,20^FD{nome}^FS");
            sb.AppendLine($"^FO20,105^A0N,16,16^FDCat: {cat}^FS");
            sb.AppendLine($"^FO20,125^A0N,16,16^FDLoc: {loc}^FS");

            // QR Code bidimensional com o código da ferramenta para leitura rápida por câmera/scanner
            sb.AppendLine($"^FO260,35^BQN,2,4^FDQA,{cod}^FS");

            sb.AppendLine("^XZ");
            return sb.ToString();
        }

        public async Task<bool> ImprimirViaRedeTcpAsync(string ipImpressora, int porta, string zplConteudo)
        {
            if (string.IsNullOrWhiteSpace(ipImpressora))
                throw new ArgumentException("IP da impressora é obrigatório.", nameof(ipImpressora));
            if (string.IsNullOrWhiteSpace(zplConteudo))
                throw new ArgumentException("Conteúdo ZPL não pode ser vazio.", nameof(zplConteudo));

            try
            {
                var bytes = Encoding.UTF8.GetBytes(zplConteudo);
                using var client = new TcpClient();
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));

                await client.ConnectAsync(ipImpressora, porta, cts.Token);
                using var stream = client.GetStream();
                await stream.WriteAsync(bytes, 0, bytes.Length, cts.Token);
                await stream.FlushAsync(cts.Token);

                _logger?.LogInfo($"[EtiquetaZpl] ZPL enviado com sucesso para impressora térmica {ipImpressora}:{porta}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[EtiquetaZpl] Falha ao enviar ZPL para impressora {ipImpressora}:{porta}: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<string> SalvarArquivoZplAsync(string zplConteudo, string nomeArquivo)
        {
            var pasta = Path.Combine(AppContext.BaseDirectory, "EtiquetasZPL");
            Directory.CreateDirectory(pasta);

            var safeName = string.Join("_", nomeArquivo.Split(Path.GetInvalidFileNameChars()));
            if (!safeName.EndsWith(".zpl", StringComparison.OrdinalIgnoreCase))
            {
                safeName += ".zpl";
            }

            var caminhoCompleto = Path.Combine(pasta, safeName);
            await File.WriteAllTextAsync(caminhoCompleto, zplConteudo, Encoding.UTF8);
            return caminhoCompleto;
        }
    }
}
