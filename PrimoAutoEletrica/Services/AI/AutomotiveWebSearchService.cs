using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services.AI
{
    public sealed class AutomotiveWebSnippet
    {
        public string Titulo { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Dominio { get; set; } = string.Empty;
    }

    public sealed class AutomotiveWebSearchResult
    {
        public bool Sucesso { get; set; }
        public string Consulta { get; set; } = string.Empty;
        public List<AutomotiveWebSnippet> Resultados { get; set; } = new();
        public string ContextoFormatadoMarkdown { get; set; } = string.Empty;
        public long LatenciaMs { get; set; }
        public string? MensagemErro { get; set; }
    }

    /// <summary>
    /// Serviço de busca técnica em tempo real na internet (Web Search Automotivo)
    /// Pesquisa esquemas elétricos, manuais de oficina, pinagens e defeitos crônicos na web sem custos de API.
    /// </summary>
    public sealed class AutomotiveWebSearchService
    {
        private static readonly HttpClient _httpClient;
        private static readonly ConcurrentDictionary<string, (DateTime Expiracao, AutomotiveWebSearchResult Resultado)> _cache = new();
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

        static AutomotiveWebSearchService()
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                UseCookies = true
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(4)
            };

            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7");
            _httpClient.DefaultRequestHeaders.Add("Referer", "https://www.bing.com/");
        }

        public async Task<bool> TestarConexaoInternetAsync(CancellationToken ct = default)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(3));
                var resp = await _httpClient.GetAsync("https://www.bing.com", cts.Token);
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Realiza a busca técnica na internet por esquemas elétricos, diagramas e defeitos crônicos.
        /// Utiliza múltiplos motores (Bing com fallback DuckDuckGo) sem custos de API e com alta disponibilidade.
        /// </summary>
        public async Task<AutomotiveWebSearchResult> PesquisarAsync(string sintomaOuPergunta, string? veiculo = null, CancellationToken ct = default)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            string query = ConstruirQueryAutomotiva(sintomaOuPergunta, veiculo);
            string cacheKey = query.ToLowerInvariant().Trim();

            if (_cache.TryGetValue(cacheKey, out var itemCache) && DateTime.UtcNow < itemCache.Expiracao)
            {
                return itemCache.Resultado;
            }

            try
            {
                List<AutomotiveWebSnippet> snippets = new();

                // 1. Motor Primário: Bing Search (Alta velocidade, diagramas técnicos em PDF e sem bloqueios de captcha)
                try
                {
                    var bingEndpoint = $"https://www.bing.com/search?q={Uri.EscapeDataString(query)}&setlang=pt-br";
                    using var ctsBing = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    ctsBing.CancelAfter(TimeSpan.FromSeconds(4.0));

                    var respBing = await _httpClient.GetAsync(bingEndpoint, ctsBing.Token);
                    if (respBing.IsSuccessStatusCode)
                    {
                        var htmlBing = await respBing.Content.ReadAsStringAsync(ctsBing.Token);
                        snippets = ExtrairResultadosBingHtml(htmlBing, query);
                    }
                }
                catch { }

                // 2. Motor Secundário / Fallback: DuckDuckGo HTML
                if (snippets.Count == 0)
                {
                    try
                    {
                        var ddgEndpoint = $"https://html.duckduckgo.com/html/?q={Uri.EscapeDataString(query)}";
                        using var ctsDdg = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        ctsDdg.CancelAfter(TimeSpan.FromSeconds(4.0));

                        var respDdg = await _httpClient.GetAsync(ddgEndpoint, ctsDdg.Token);
                        if (respDdg.IsSuccessStatusCode)
                        {
                            var htmlDdg = await respDdg.Content.ReadAsStringAsync(ctsDdg.Token);
                            snippets = ExtrairResultadosDuckDuckGoHtml(htmlDdg, query);
                        }
                    }
                    catch { }
                }

                sw.Stop();

                if (snippets.Count == 0)
                {
                    return new AutomotiveWebSearchResult
                    {
                        Sucesso = false,
                        Consulta = query,
                        LatenciaMs = sw.ElapsedMilliseconds,
                        MensagemErro = "Nenhum resultado técnico retornado nos motores de busca."
                    };
                }

                var markdown = FormatarMarkdownContexto(query, snippets);

                var resultado = new AutomotiveWebSearchResult
                {
                    Sucesso = true,
                    Consulta = query,
                    Resultados = snippets,
                    ContextoFormatadoMarkdown = markdown,
                    LatenciaMs = sw.ElapsedMilliseconds
                };

                _cache[cacheKey] = (DateTime.UtcNow.Add(CacheTtl), resultado);
                return resultado;
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new AutomotiveWebSearchResult
                {
                    Sucesso = false,
                    Consulta = query,
                    LatenciaMs = sw.ElapsedMilliseconds,
                    MensagemErro = ex.Message
                };
            }
        }

        public static readonly string[] SitesNaoAutomotivos = new[]
        {
            // Redes sociais e vídeos não técnicos
            "instagram.com", "facebook.com", "fb.com", "fb.watch", "tiktok.com", "twitter.com",
            "x.com", "threads.net", "pinterest.com", "linkedin.com", "snapchat.com",

            // Ferramentas de fluxograma genérico e design de software
            "draw.io", "diagrams.net", "lucidchart", "lucid.co", "miro.com", "canva.com",
            "creately", "smartdraw", "visual-paradigm",

            // Dicionários, definições e enciclopédias escolares genéricas
            "definicion.edu.lat", "definicion.de", "concepto.de", "esquemaker.com", "nuevaescuelamexicana.org",
            "todamateria.com.br", "dicio.com.br", "significados.com.br", "significado.com.br", "significados.com",
            "wikipedia.org/wiki/Esquema", "diferenciador.com", "ejemplos.co", "psicologiaymente.com",
            "lifeder.com", "dle.rae.es", "rae.es", "priberam.pt", "wordreference.com", "brainly.com.br",
            "brainly.lat", "brasilescola.uol.com.br", "mundoeducacao.uol.com.br", "infoescola.com",
            "educalingo.com",

            // Imobiliárias e e-commerces genéricos sem contexto técnico
            "esquemaimoveis", "imoveis", "imobiliaria", "shopee.com", "magazineluiza.com", "magalu.com",
            "mercadolivre.com.br/c/", "aliexpress"
        };

        public static bool EhSiteNaoAutomotivo(string url, string dominio = "", string titulo = "", string texto = "")
        {
            string unificado = $"{url} {dominio} {titulo} {texto}".ToLowerInvariant();
            foreach (var site in SitesNaoAutomotivos)
            {
                if (unificado.Contains(site)) return true;
            }

            if (unificado.Contains("imóveis") || unificado.Contains("imoveis") || unificado.Contains("imobiliária")
                || unificado.Contains("imobiliaria") || unificado.Contains("diccionario") || unificado.Contains("qué es un esquema")
                || unificado.Contains("definición de esquema") || unificado.Contains("o que é um esquema")
                || unificado.Contains("o que significa") || unificado.Contains("significado de")
                || unificado.Contains("definicion de") || unificado.Contains("definición de")
                || unificado.Contains("conceito de") || unificado.Contains("sinonimos de")
                || unificado.Contains("instagram profile") || unificado.Contains("ver perfil"))
            {
                return true;
            }

            return false;
        }

        public static string RemoverAcentos(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
            var normalizedString = texto.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder(normalizedString.Length);

            foreach (var c in normalizedString)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        public static bool PossuiRelevanciaAutomotiva(string titulo, string texto, string query)
        {
            string bruto = $"{titulo} {texto}".ToLowerInvariant();
            string conteudo = RemoverAcentos(bruto);

            // Descartar se tiver forte indício de definição teórica, psicologia, filosofia ou dicionário
            if (conteudo.Contains("significado de") || conteudo.Contains("o que significa") ||
                conteudo.Contains("psicologia") || conteudo.Contains("filosofia") ||
                conteudo.Contains("representacao mental") || conteudo.Contains("figura de linguagem") ||
                conteudo.Contains("conceito de") || conteudo.Contains("sinonimos de"))
            {
                return false;
            }

            string[] termosAutomotivos = new[]
            {
                "eletric", "chicote", "fusivel", "rele",
                "modulo", "ecu", "bcm", "pinagem", "motor", "ignicao", "partida",
                "alternador", "bateria", "sensor", "atuador", "valvula", "bico", "injec",
                "farol", "lanterna", "obd", "bobina", "vela", "bomba", "ventoinha",
                "volts", "tensao", "corrente", "amper", "arrefec", "oficina",
                "veiculo", "carro", "caminhao", "fiat", "volkswagen", "vw", "chevrolet",
                "gm", "ford", "hyundai", "toyota", "honda", "renault", "peugeot", "citroen", "scania", "volvo"
            };

            foreach (var t in termosAutomotivos)
            {
                if (conteudo.Contains(t)) return true;
            }

            var termosQuery = RemoverAcentos(query.ToLowerInvariant())
                                   .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                   .Where(w => w.Length > 3 && w != "esquema" && w != "diagrama");
            foreach (var tq in termosQuery)
            {
                if (conteudo.Contains(tq)) return true;
            }

            return false;
        }

        public static string ConstruirQueryAutomotiva(string sintomaOuPergunta, string? veiculo = null)
        {
            var limpo = Regex.Replace(sintomaOuPergunta ?? string.Empty, @"[^\w\s-]", " ").Trim();
            limpo = Regex.Replace(limpo, @"\s+", " ");

            // Remover termos vagos de conversa e preposições
            var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ola", "oi", "boa", "noite", "dia", "tarde", "amigo", "como", "faco", "achar", "o", "a",
                "estou", "com", "um", "uma", "carro", "aqui", "na", "no", "oficina", "ele", "ela", "nao", "esta", "por", "favor",
                "ajuda", "me", "diga", "consegue", "fornecer", "passar", "voce", "você", "tem", "queria", "quero",
                "qual", "quais", "mostrar", "saber", "onde", "fica", "ver", "preciso", "gostaria", "de", "do", "da", "dos", "das"
            };

            var palavras = limpo.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                .Where(p => !stopWords.Contains(p) && p.Length > 1)
                                .ToList();

            string termosBase = string.Join(" ", palavras);

            if (!string.IsNullOrWhiteSpace(veiculo) && !termosBase.Contains(veiculo, StringComparison.OrdinalIgnoreCase))
            {
                termosBase = $"{veiculo.Trim()} {termosBase}";
            }

            if (!termosBase.Contains("esquema", StringComparison.OrdinalIgnoreCase) && !termosBase.Contains("diagrama", StringComparison.OrdinalIgnoreCase))
            {
                termosBase = $"esquema eletrico {termosBase}";
            }

            if (!termosBase.Contains("eletrico", StringComparison.OrdinalIgnoreCase) && !termosBase.Contains("eletrica", StringComparison.OrdinalIgnoreCase))
            {
                termosBase = $"{termosBase} eletrico";
            }

            return $"{termosBase} oficina reparo".Trim();
        }

        private static List<AutomotiveWebSnippet> ExtrairResultadosBingHtml(string html, string query = "")
        {
            var lista = new List<AutomotiveWebSnippet>();
            if (string.IsNullOrWhiteSpace(html)) return lista;

            var algos = Regex.Matches(html, @"<li class=\""b_algo\""[^>]*>(.*?)</li>", RegexOptions.Singleline);
            foreach (Match m in algos)
            {
                if (lista.Count >= 5) break;
                var bloco = m.Groups[1].Value;

                var titleMatch = Regex.Match(bloco, @"<h2[^>]*><a[^>]*>(.*?)</a></h2>", RegexOptions.Singleline);
                var hrefMatch = Regex.Match(bloco, @"<h2[^>]*><a[^>]+href=\""([^\""]+)\""", RegexOptions.Singleline);
                var snipMatch = Regex.Match(bloco, @"<div class=\""b_caption\"">.*?<p[^>]*>(.*?)</p>", RegexOptions.Singleline);
                var citeMatch = Regex.Match(bloco, @"<cite[^>]*>(.*?)</cite>", RegexOptions.Singleline);

                string titulo = LimparHtml(titleMatch.Groups[1].Value);
                string texto = LimparHtml(snipMatch.Groups[1].Value);
                string dominio = LimparHtml(citeMatch.Groups[1].Value);
                string url = hrefMatch.Groups[1].Value;

                if (string.IsNullOrWhiteSpace(url) || url == "#")
                {
                    if (!string.IsNullOrWhiteSpace(dominio))
                    {
                        var d = dominio.Replace(" › ", "/").Replace(" ", "");
                        url = d.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? d : $"https://{d}";
                    }
                    else
                    {
                        url = "#";
                    }
                }

                if (string.IsNullOrWhiteSpace(texto) || texto.Length < 15) continue;
                if (EhSiteNaoAutomotivo(url, dominio, titulo, texto)) continue;
                if (!string.IsNullOrWhiteSpace(query) && !PossuiRelevanciaAutomotiva(titulo, texto, query)) continue;

                lista.Add(new AutomotiveWebSnippet
                {
                    Titulo = titulo,
                    Snippet = texto,
                    Dominio = dominio,
                    Url = url
                });
            }

            return lista;
        }

        private static List<AutomotiveWebSnippet> ExtrairResultadosDuckDuckGoHtml(string html, string query = "")
        {
            var lista = new List<AutomotiveWebSnippet>();
            if (string.IsNullOrWhiteSpace(html)) return lista;

            var titles = Regex.Matches(html, @"<a[^>]+class=\""result__a\""[^>]*>(.*?)</a>", RegexOptions.Singleline);
            var snippets = Regex.Matches(html, @"<a[^>]+class=\""result__snippet\""[^>]*>(.*?)</a>", RegexOptions.Singleline);
            var urls = Regex.Matches(html, @"<a[^>]+class=\""result__url\""[^>]*>(.*?)</a>", RegexOptions.Singleline);

            int total = Math.Min(titles.Count, snippets.Count);

            for (int i = 0; i < total && lista.Count < 5; i++)
            {
                string rawTitle = titles[i].Groups[1].Value;
                string rawSnippet = snippets[i].Groups[1].Value;
                string rawUrl = (i < urls.Count) ? urls[i].Groups[1].Value : string.Empty;

                string titulo = LimparHtml(rawTitle);
                string texto = LimparHtml(rawSnippet);
                string dominio = LimparHtml(rawUrl);

                if (string.IsNullOrWhiteSpace(texto) || texto.Length < 15) continue;
                if (EhSiteNaoAutomotivo(rawUrl, dominio, titulo, texto)) continue;
                if (!string.IsNullOrWhiteSpace(query) && !PossuiRelevanciaAutomotiva(titulo, texto, query)) continue;

                lista.Add(new AutomotiveWebSnippet
                {
                    Titulo = titulo,
                    Snippet = texto,
                    Dominio = dominio,
                    Url = string.IsNullOrWhiteSpace(dominio) ? "#" : $"https://{dominio}"
                });
            }

            return lista;
        }

        private static string LimparHtml(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var semTags = Regex.Replace(input, @"<[^>]+?>", string.Empty);
            semTags = System.Net.WebUtility.HtmlDecode(semTags);
            return Regex.Replace(semTags, @"\s+", " ").Trim();
        }

        private static string FormatarMarkdownContexto(string query, List<AutomotiveWebSnippet> snippets)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"🌐 **Pesquisa Técnica Web em Tempo Real (Internet):**");
            sb.AppendLine($"*Resultados coletados para busca: \"{query}\"*");
            sb.AppendLine();

            foreach (var item in snippets)
            {
                sb.AppendLine($"• **{item.Titulo}**");
                sb.AppendLine($"  _{item.Snippet}_");
                if (!string.IsNullOrWhiteSpace(item.Dominio))
                {
                    sb.AppendLine($"  *Fonte: {item.Dominio}*");
                }
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }
    }
}
