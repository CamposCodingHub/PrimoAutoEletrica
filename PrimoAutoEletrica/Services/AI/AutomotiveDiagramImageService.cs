using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services.AI
{
    public sealed class AutomotiveDiagramImageItem
    {
        public string Titulo { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string FonteUrl { get; set; } = string.Empty;
        public string? LocalPath { get; set; }
    }

    /// <summary>
    /// Serviço de busca e download de imagens de diagramas e esquemas elétricos na internet.
    /// Suporta repositório local embarcado e download em tempo real de diagramas da web.
    /// </summary>
    public sealed class AutomotiveDiagramImageService
    {
        private static readonly HttpClient _httpClient;
        private static readonly ConcurrentDictionary<string, List<AutomotiveDiagramImageItem>> _cacheBusca = new();
        private readonly string _cacheDirectory;

        static AutomotiveDiagramImageService()
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                UseCookies = true
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(6)
            };

            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8");
            _httpClient.DefaultRequestHeaders.Add("Referer", "https://www.bing.com/");
        }

        public AutomotiveDiagramImageService()
        {
            _cacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DiagramasCache");
            try
            {
                if (!Directory.Exists(_cacheDirectory))
                {
                    Directory.CreateDirectory(_cacheDirectory);
                }
            }
            catch { }
        }

        public string ObterDiretorioCache() => _cacheDirectory;

        /// <summary>
        /// Localiza um diagrama de alta resolução pré-embarcado no aplicativo.
        /// </summary>
        public string? ObterDiagramaLocalPreEmbarcado(string veiculoOuTermo)
        {
            if (string.IsNullOrWhiteSpace(veiculoOuTermo)) return null;
            var t = veiculoOuTermo.ToLowerInvariant();
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var assetsDir = Path.Combine(baseDir, "Assets", "Diagramas");

            // 1. Hyundai HB20 (Bosch ME 17.9.11 / 17.9.11.1)
            if (t.Contains("hb20") || t.Contains("hyundai") || t.Contains("me17.9.11") || t.Contains("me 17.9.11") || t.Contains("kappa"))
            {
                var f1 = Path.Combine(assetsDir, "hb20_1.0_bosch_me17.9.11.png");
                if (File.Exists(f1)) return f1;
                var f2 = Path.Combine(assetsDir, "hb20_sete_treinamentos.png");
                if (File.Exists(f2)) return f2;
            }

            // 2. VW Gol G5 / EA111 / IAW 4GV
            if (t.Contains("gol") || t.Contains("g5") || t.Contains("iaw 4gv") || t.Contains("ea111"))
            {
                var fGol = Path.Combine(assetsDir, "gol_g5_iaw_4gv.jpg");
                if (File.Exists(fGol)) return fGol;
            }

            // 3. Scania Série R (R440 / R450 2013) Farol, Seta e Módulo VIS
            if (t.Contains("scania") || t.Contains("r440") || t.Contains("r450") || t.Contains("coo7") || t.Contains("vis"))
            {
                var fScania = Path.Combine(assetsDir, "scania_r440_farol_seta_vis.png");
                if (File.Exists(fScania)) return fScania;
            }

            // 4. GM Onix / Prisma SPE/4 Injeção Delco E83 e Bobina 4 Vias
            if (t.Contains("onix") || t.Contains("prisma") || t.Contains("e83") || t.Contains("spe/4") || t.Contains("spe4"))
            {
                var fOnix = Path.Combine(assetsDir, "onix_prisma_e83_bobina.png");
                if (File.Exists(fOnix)) return fOnix;
            }

            // 5. Fiat Palio / Uno Fire Injeção IAW 4AF / 4SF
            if (t.Contains("palio") || t.Contains("uno") || t.Contains("fire") || t.Contains("iaw 4af") || t.Contains("4af") || t.Contains("4sf"))
            {
                var fPalio = Path.Combine(assetsDir, "palio_fire_iaw_4af.png");
                if (File.Exists(fPalio)) return fPalio;
            }

            return null;
        }

        /// <summary>
        /// Pesquisa imagens de esquemas elétricos e diagramas na web em tempo real.
        /// </summary>
        public async Task<List<AutomotiveDiagramImageItem>> PesquisarImagensWebAsync(string consulta, CancellationToken ct = default)
        {
            string query = $"esquema eletrico {consulta}".Trim();
            string key = query.ToLowerInvariant();
            if (_cacheBusca.TryGetValue(key, out var cached)) return cached;

            var resultados = new List<AutomotiveDiagramImageItem>();

            try
            {
                var bingUrl = $"https://www.bing.com/images/search?q={Uri.EscapeDataString(query)}&setlang=pt-br";
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(5));

                var resp = await _httpClient.GetAsync(bingUrl, cts.Token);
                if (resp.IsSuccessStatusCode)
                {
                    var html = await resp.Content.ReadAsStringAsync(cts.Token);
                    var matches = Regex.Matches(html, @"(?:murl&quot;:&quot;|""murl""\s*:\s*"")(https?://[^&""]+?)(?:&quot;|"")", RegexOptions.Singleline);
                    foreach (Match m in matches)
                    {
                        var imgUrl = m.Groups[1].Value.Replace("\\/", "/");
                        var cleanUrl = imgUrl.Split('?')[0].ToLowerInvariant();
                        bool ehImgValida = cleanUrl.EndsWith(".jpg") || cleanUrl.EndsWith(".jpeg") || cleanUrl.EndsWith(".png") || cleanUrl.EndsWith(".webp")
                                        || imgUrl.Contains("image") || imgUrl.Contains("diagram") || imgUrl.Contains("esquema") || imgUrl.Contains("upload");

                        if (ehImgValida && !imgUrl.Contains("draw.io") && !imgUrl.Contains("canva") && !imgUrl.Contains("miro") && !imgUrl.Contains("lucidchart"))
                        {
                            resultados.Add(new AutomotiveDiagramImageItem
                            {
                                Titulo = $"Esquema Elétrico: {consulta}",
                                ImageUrl = imgUrl,
                                FonteUrl = imgUrl
                            });

                            if (resultados.Count >= 6) break;
                        }
                    }
                }
            }
            catch { }

            _cacheBusca[key] = resultados;
            return resultados;
        }

        /// <summary>
        /// Baixa a imagem do diagrama da web e a salva localmente no cache do programa.
        /// </summary>
        public async Task<string?> BaixarImagemDiagramaAsync(string imageUrl, string prefixoNome, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return null;

            try
            {
                var ext = Path.GetExtension(imageUrl);
                if (string.IsNullOrWhiteSpace(ext) || ext.Length > 5) ext = ".png";
                if (ext.Contains("?")) ext = ext.Substring(0, ext.IndexOf("?"));

                var safeName = Regex.Replace(prefixoNome, @"[^\w-]", "_") + "_" + Math.Abs(imageUrl.GetHashCode()) + ext;
                var localFile = Path.Combine(_cacheDirectory, safeName);

                if (File.Exists(localFile) && new FileInfo(localFile).Length > 1024)
                {
                    return localFile;
                }

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(6));

                var resp = await _httpClient.GetAsync(imageUrl, cts.Token);
                if (resp.IsSuccessStatusCode)
                {
                    var bytes = await resp.Content.ReadAsByteArrayAsync(cts.Token);
                    if (bytes.Length > 2048) // Arquivo de imagem válido
                    {
                        await File.WriteAllBytesAsync(localFile, bytes, cts.Token);
                        return localFile;
                    }
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Resolve e garante uma imagem de diagrama (pré-embarcada ou baixada da web).
        /// </summary>
        public async Task<(string? CaminhoLocal, string Titulo, bool EhBaixado)> ObterOuBaixarDiagramaAsync(
            string veiculo, 
            string? sistema, 
            CancellationToken ct = default)
        {
            // 1. Tentar diagrama local pré-embarcado
            var localPre = ObterDiagramaLocalPreEmbarcado($"{veiculo} {sistema}");
            if (!string.IsNullOrWhiteSpace(localPre))
            {
                string titLocal = Path.GetFileNameWithoutExtension(localPre).Replace("_", " ").ToUpperInvariant();
                return (localPre, titLocal, false);
            }

            // 2. Tentar buscar e baixar na internet
            var busca = await PesquisarImagensWebAsync($"{veiculo} {sistema}", ct);
            if (busca.Count > 0)
            {
                var primeira = busca[0];
                var arquivoBaixado = await BaixarImagemDiagramaAsync(primeira.ImageUrl, veiculo, ct);
                if (!string.IsNullOrWhiteSpace(arquivoBaixado))
                {
                    return (arquivoBaixado, primeira.Titulo, true);
                }
            }

            return (null, string.Empty, false);
        }
    }
}
