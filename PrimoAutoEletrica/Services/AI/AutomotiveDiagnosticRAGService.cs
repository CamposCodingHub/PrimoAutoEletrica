using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PrimoAutoEletrica.Services.AI
{
    public sealed class DiagnosticEntry
    {
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string System { get; set; } = string.Empty;
        public string Symptoms { get; set; } = string.Empty;
        public List<string> ProbableCauses { get; set; } = new();
        public List<string> GuidedSteps { get; set; } = new();
        public List<string> SuggestedTools { get; set; } = new();
        public List<string> SuggestedStockParts { get; set; } = new();
        public string ReferenceStandard { get; set; } = string.Empty;
        public List<string> ClarifyingQuestions { get; set; } = new();
        public List<string> InteractiveReplyChips { get; set; } = new();
    }

    public sealed class SystemManualEntry
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ModuleNavigationTarget { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<string> Instructions { get; set; } = new();
        public List<string> Tips { get; set; } = new();
        public List<string> Keywords { get; set; } = new();
    }

    public sealed class AutomotiveDiagnosticRAGService
    {
        private readonly List<DiagnosticEntry> _technicalBase;
        private readonly List<SystemManualEntry> _systemManualBase;

        public AutomotiveDiagnosticRAGService()
        {
            _technicalBase = CarregarBaseConhecimentoTecnico();
            _systemManualBase = CarregarBaseManuaisSistema();
        }

        public DiagnosticEntry? BuscarPorCodigoDTC(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return null;

            var trimmed = codigo.Trim().ToUpperInvariant();
            var exact = _technicalBase.FirstOrDefault(d => string.Equals(d.Code, trimmed, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
            {
                return exact;
            }

            var match = Regex.Match(trimmed, @"[PBUS]\d{4}");
            if (match.Success)
            {
                var codeNorm = match.Value;
                return _technicalBase.FirstOrDefault(d => string.Equals(d.Code, codeNorm, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }

        public List<DiagnosticEntry> BuscarPorSintomaOuTermo(string termo, int limite = 3)
        {
            if (string.IsNullOrWhiteSpace(termo)) return new List<DiagnosticEntry>();

            var termoNorm = NormalizarTexto(termo);

            // Consultas puramente informativas de localização ou tabela de fusíveis/relés não devem disparar triagem clínica de defeitos
            bool ehConsultaInformativaFusiveis = (termoNorm.Contains("fusivel") || termoNorm.Contains("fusiveis") || termoNorm.Contains("rele") || termoNorm.Contains("reles"))
                && (termoNorm.Contains("posicao") || termoNorm.Contains("posicoes") || termoNorm.Contains("onde") || termoNorm.Contains("tabela") || termoNorm.Contains("mapa") || termoNorm.Contains("layout") || termoNorm.Contains("diagrama") || termoNorm.Contains("esquema"))
                && !termoNorm.Contains("queima") && !termoNorm.Contains("curto") && !termoNorm.Contains("derrete") && !termoNorm.Contains("rompido") && !termoNorm.Contains("aberto") && !termoNorm.Contains("parou") && !termoNorm.Contains("defeito") && !termoNorm.Contains("falha") && !termoNorm.Contains("nao funciona");

            if (ehConsultaInformativaFusiveis)
            {
                return new List<DiagnosticEntry>();
            }

            var tokens = termoNorm.Split(new[] { ' ', ',', ';', '-', '/', '?', '!', '.' }, StringSplitOptions.RemoveEmptyEntries);

            var resultados = _technicalBase
                .Select(d => new
                {
                    Item = d,
                    Score = CalcularRelevanciaTecnica(d, termoNorm, tokens)
                })
                .Where(r => r.Score > 8)
                .OrderByDescending(r => r.Score)
                .Take(limite)
                .Select(r => r.Item)
                .ToList();

            return resultados;
        }

        public SystemManualEntry? BuscarManualSistema(string termo)
        {
            if (string.IsNullOrWhiteSpace(termo)) return null;

            var termoNorm = NormalizarTexto(termo);
            var tokens = termoNorm.Split(new[] { ' ', ',', ';', '-', '/', '?', '!', '.' }, StringSplitOptions.RemoveEmptyEntries);

            var melhor = _systemManualBase
                .Select(m => new
                {
                    Item = m,
                    Score = CalcularRelevanciaManual(m, termoNorm, tokens)
                })
                .Where(r => r.Score > 10)
                .OrderByDescending(r => r.Score)
                .FirstOrDefault();

            return melhor?.Item;
        }

        public List<DiagnosticEntry> ListarTodosDTCs() => _technicalBase.Where(d => Regex.IsMatch(d.Code, @"^[PBUS]\d{4}$")).ToList();

        public List<SystemManualEntry> ListarTodosManuais() => _systemManualBase;

        public static string NormalizarTexto(string texto)
        {
            var t = texto.Trim().ToLowerInvariant();
            t = t.Replace("-", " ");
            t = t.Replace("á", "a").Replace("à", "a").Replace("ã", "a").Replace("â", "a")
                 .Replace("é", "e").Replace("ê", "e")
                 .Replace("í", "i")
                 .Replace("ó", "o").Replace("õ", "o").Replace("ô", "o")
                 .Replace("ú", "u")
                 .Replace("ç", "c");
            return t;
        }

        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "carro", "veiculo", "veiculos", "esta", "estao", "para", "com", "nao", "tem",
            "meu", "minha", "ele", "ela", "eles", "elas", "esse", "essa", "isso", "aqui",
            "onde", "quando", "qual", "como", "pelo", "pela", "pelos", "pelas", "outro",
            "outra", "outros", "outras", "muito", "mais", "pouco", "algo", "tudo", "nada",
            "coisa", "coisas", "lado", "lados", "defeito", "problema", "ajuda", "olha", "bom", "dia", "noite", "tarde", "sistema",
            "esquema", "diagrama", "eletrico", "eletrica", "eletricos", "eletricas", "consegue", "fornecer", "passar", "ano", "modelo", "manual",
            "posicao", "posicoes", "preciso", "ola"
        };

        private static int CalcularRelevanciaTecnica(DiagnosticEntry d, string termoNorm, string[] tokens)
        {
            // Consultas puramente informativas de layout/tabela de fusíveis/relés
            bool ehConsultaInformativaFusiveis = (termoNorm.Contains("fusivel") || termoNorm.Contains("fusiveis") || termoNorm.Contains("rele") || termoNorm.Contains("reles"))
                && (termoNorm.Contains("posicao") || termoNorm.Contains("posicoes") || termoNorm.Contains("onde") || termoNorm.Contains("tabela") || termoNorm.Contains("mapa") || termoNorm.Contains("layout") || termoNorm.Contains("diagrama") || termoNorm.Contains("esquema"))
                && !termoNorm.Contains("queima") && !termoNorm.Contains("curto") && !termoNorm.Contains("derrete") && !termoNorm.Contains("rompido") && !termoNorm.Contains("aberto") && !termoNorm.Contains("parou") && !termoNorm.Contains("defeito") && !termoNorm.Contains("falha") && !termoNorm.Contains("nao funciona");

            if (ehConsultaInformativaFusiveis)
            {
                return 0;
            }

            // =========================================================================
            // FILTRO DE SEGURANÇA CONTRA FALSOS POSITIVOS (ANTI-CHUTE DE MODELOS/SISTEMAS ESPECÍFICOS)
            // Jamais seleciona um sistema de nicho (ex: Start-Stop, Smart Charge, Fox Anti-Esmagamento, Vidro/Trava)
            // se o usuário não citou explicitamente o sistema ou o modelo daquele carro!
            // =========================================================================
            if (d.Code.Contains("FUSIVEL") && !termoNorm.Contains("fusivel") && !termoNorm.Contains("fusiveis") && !termoNorm.Contains("curto"))
            {
                return 0;
            }

            if ((d.Code.Contains("VIDRO") || d.Code.Contains("TRAVA")) && !termoNorm.Contains("vidro") && !termoNorm.Contains("trava") && !termoNorm.Contains("sobe") && !termoNorm.Contains("desce") && !termoNorm.Contains("canaleta") && !termoNorm.Contains("one touch"))
            {
                return 0;
            }

            if (d.Code.Contains("START_STOP") && !d.Code.Contains("BATERIA") && !termoNorm.Contains("start stop") && !termoNorm.Contains("ibs") && !((termoNorm.Contains("toro") || termoNorm.Contains("renegade")) && (termoNorm.Contains("bateria") || termoNorm.Contains("indisponivel") || termoNorm.Contains("start"))))
            {
                return 0;
            }

            if (d.Code.Contains("SMART_CHARGE") && !termoNorm.Contains("smart charge") && !((termoNorm.Contains("ford") || termoNorm.Contains("ka") || termoNorm.Contains("fiesta") || termoNorm.Contains("ecosport")) && (termoNorm.Contains("alternador") || termoNorm.Contains("bateria") || termoNorm.Contains("14"))))
            {
                return 0;
            }

            if (d.Code.Contains("ANTI_ESMAGAMENTO") && !termoNorm.Contains("anti esmagamento") && !((termoNorm.Contains("fox") || termoNorm.Contains("gol") || termoNorm.Contains("polo")) && (termoNorm.Contains("vidro") || termoNorm.Contains("desce") || termoNorm.Contains("sobe"))))
            {
                return 0;
            }

            if (d.Code.Contains("CHAVE_SETA") && !termoNorm.Contains("chave de seta") && !((termoNorm.Contains("hb20") || termoNorm.Contains("creta")) && (termoNorm.Contains("seta") || termoNorm.Contains("farol"))))
            {
                return 0;
            }

            if (d.Code.Contains("COMANDOS_VOLANTE") && !termoNorm.Contains("cinta") && !termoNorm.Contains("clock spring") && !((termoNorm.Contains("corolla") || termoNorm.Contains("etios")) && (termoNorm.Contains("volante") || termoNorm.Contains("som") || termoNorm.Contains("buzina"))))
            {
                return 0;
            }

            if (d.Code.Contains("MARCADOR_COMBUSTIVEL") && !termoNorm.Contains("boia") && !termoNorm.Contains("marcador") && !(termoNorm.Contains("sandero") && (termoNorm.Contains("tanque") || termoNorm.Contains("combustivel") || termoNorm.Contains("reserva"))))
            {
                return 0;
            }

            if (d.Code.Contains("ABS") && !termoNorm.Contains("abs") && !termoNorm.Contains("velocimetro") && !((termoNorm.Contains("onix") || termoNorm.Contains("prisma")) && (termoNorm.Contains("rolamento") || termoNorm.Contains("sensor") || termoNorm.Contains("parou"))))
            {
                return 0;
            }

            // Se a queixa for esguicho/jogar água/bombinha, não deixar a Chave de Seta do HB20 pontuar
            if ((termoNorm.Contains("esguicho") || termoNorm.Contains("jogar agua") || termoNorm.Contains("jato de agua") || termoNorm.Contains("bombinha") || termoNorm.Contains("brucutu")) && d.Code.Contains("CHAVE_SETA"))
            {
                return 0;
            }

            int score = 0;
            var fullText = NormalizarTexto($"{d.Code} {d.Title} {d.System} {d.Symptoms} {string.Join(" ", d.ProbableCauses)} {string.Join(" ", d.SuggestedTools)}");

            if (fullText.Contains(termoNorm)) score += 50;

            // Pontuação por palavras-chave específicas e sinônimos automotivos comuns
            if ((termoNorm.Contains("nao pega") || termoNorm.Contains("nao liga") || termoNorm.Contains("sem funcionar") || termoNorm.Contains("parou e nao pega") || termoNorm.Contains("morreu") || termoNorm.Contains("comeco os teste") || termoNorm.Contains("por onde comeco")) 
                && d.Code.Contains("CARRO_SEM_FUNCIONAR")) score += 70;

            if ((termoNorm.Contains("lanterna") || termoNorm.Contains("farol") || termoNorm.Contains("lampada") || termoNorm.Contains("pisca") || termoNorm.Contains("luz de freio"))
                && (d.Code.Contains("FAROL") || d.Code.Contains("ILUMINACAO") || d.Code.Contains("LANTERNA"))) score += 60;

            if ((termoNorm.Contains("tec tec") || termoNorm.Contains("arranque") || termoNorm.Contains("partida pesada") || termoNorm.Contains("vira devagar")) && d.Code.Contains("PARTIDA")) score += 50;
            if ((termoNorm.Contains("nao carrega") || termoNorm.Contains("luz de bateria") || termoNorm.Contains("alternador")) && d.Code.Contains("ALTERNADOR")) score += 50;
            if ((termoNorm.Contains("descarrega") || termoNorm.Contains("fuga") || termoNorm.Contains("parasita") || termoNorm.Contains("arreia")) && d.Code.Contains("PARASITA")) score += 50;
            if ((termoNorm.Contains("queima fusivel") || termoNorm.Contains("curto")) && d.Code.Contains("FUSIVEL")) score += 50;
            if ((termoNorm.Contains("oscilando") || termoNorm.Contains("oscila") || termoNorm.Contains("morrendo") || termoNorm.Contains("morre") 
                || termoNorm.Contains("morrer") || termoNorm.Contains("marcha lenta") || termoNorm.Contains("lenta irregular") 
                || (termoNorm.Contains("liga") && termoNorm.Contains("morre")) || (termoNorm.Contains("liga") && termoNorm.Contains("oscil"))
                || termoNorm.Contains("falhando") || termoNorm.Contains("engasgando") || termoNorm.Contains("rateando") || termoNorm.Contains("tres cilindros"))
                && (d.Code.Contains("FALHANDO") || d.Code.Contains("MARCHA_LENTA"))) score += 75;
            if ((termoNorm.Contains("ventoinha") || termoNorm.Contains("ferve") || termoNorm.Contains("temperatura alta") || termoNorm.Contains("esquentando")) && d.Code.Contains("VENTOINHA")) score += 50;
            if ((termoNorm.Contains("vidro") || termoNorm.Contains("trava eletrica")) && d.Code.Contains("VIDRO")) score += 50;
            if ((termoNorm.Contains("ar condicionado") || termoNorm.Contains("nao gela") || termoNorm.Contains("compressor")) && d.Code.Contains("AR_CONDICIONADO")) score += 50;
            if ((termoNorm.Contains("buzina")) && d.Code.Contains("BUZINA")) score += 50;

            // Diferenciação crítica: Esguicho / Lavador / Brucutu vs Palhetas do Limpador
            if ((termoNorm.Contains("esguicho") || termoNorm.Contains("jogar agua") || termoNorm.Contains("jato de agua") 
                || termoNorm.Contains("lavador") || termoNorm.Contains("brucutu") || termoNorm.Contains("bombinha") 
                || (termoNorm.Contains("agua") && termoNorm.Contains("parabrisa"))
                || (termoNorm.Contains("alavanca") && termoNorm.Contains("bombinha"))
                || (termoNorm.Contains("puxo") && termoNorm.Contains("alavanca")))
                && d.Code.Contains("ESGUICHO")) score += 85;

            if ((termoNorm.Contains("esguicho") || termoNorm.Contains("jogar agua") || termoNorm.Contains("jato de agua") || termoNorm.Contains("bombinha") || termoNorm.Contains("brucutu")) && d.Code.Equals("LIMPADOR_PARABRISA"))
            {
                score = 0; // Previne que o limpador de palhetas vença o esguicho
            }

            if ((termoNorm.Contains("limpador") || termoNorm.Contains("palheta")) && !termoNorm.Contains("agua") && !termoNorm.Contains("bombinha") && d.Code.Contains("LIMPADOR")) score += 50;
            if ((termoNorm.Contains("direcao eletrica") || termoNorm.Contains("direcao dura") || termoNorm.Contains("luz do volante") || termoNorm.Contains("eps")) && d.Code.Contains("DIRECAO_ELETRICA")) score += 50;
            if ((termoNorm.Contains("trava eletrica") || termoNorm.Contains("alarme") || termoNorm.Contains("controle")) && d.Code.Contains("TRAVA")) score += 50;
            if ((termoNorm.Contains("abs") || termoNorm.Contains("velocimetro") || termoNorm.Contains("tracao") || (termoNorm.Contains("onix") && (termoNorm.Contains("abs") || termoNorm.Contains("roda") || termoNorm.Contains("rolamento")))) && d.Code.Contains("ABS")) score += 60;
            if ((termoNorm.Contains("smart charge") || termoNorm.Contains("alternador inteligente") || ((termoNorm.Contains("ford ka") || (termoNorm.Contains("ford") && termoNorm.Contains("bateria"))) && (termoNorm.Contains("carga") || termoNorm.Contains("alternador") || termoNorm.Contains("bateria")))) && d.Code.Contains("SMART_CHARGE")) score += 60;
            if ((termoNorm.Contains("anti esmagamento") || termoNorm.Contains("sobe e volta") || termoNorm.Contains("sobe e desce") || (termoNorm.Contains("vidro") && termoNorm.Contains("fox"))) && d.Code.Contains("ANTI_ESMAGAMENTO")) score += 60;
            if ((termoNorm.Contains("marcador") || termoNorm.Contains("boia") || termoNorm.Contains("nivel de combustivel") || termoNorm.Contains("sandero")) && d.Code.Contains("MARCADOR_COMBUSTIVEL")) score += 60;
            if ((termoNorm.Contains("start stop") || termoNorm.Contains("ibs") || (termoNorm.Contains("bateria") && termoNorm.Contains("toro"))) && d.Code.Contains("START_STOP")) score += 60;
            if ((termoNorm.Contains("chave de seta") || (termoNorm.Contains("farol alto") && termoNorm.Contains("direto")) || termoNorm.Contains("hb20")) && d.Code.Contains("CHAVE_SETA")) score += 60;
            if (((termoNorm.Contains("comandos") && termoNorm.Contains("volante")) || (termoNorm.Contains("buzina") && termoNorm.Contains("corolla"))) && d.Code.Contains("COMANDOS_VOLANTE")) score += 60;

            // Pontuações Especializadas de Alta Precisão (Novos Diagnósticos Crônicos)
            if (((termoNorm.Contains("bobina") || termoNorm.Contains("misfire") || termoNorm.Contains("fuga de centelha") || termoNorm.Contains("centelhador") || termoNorm.Contains("trinca")) && (termoNorm.Contains("onix") || termoNorm.Contains("prisma") || termoNorm.Contains("spe") || termoNorm.Contains("delco e83")))
                || termoNorm.Contains("bobina onix") || termoNorm.Contains("bobina do onix"))
            {
                if (d.Code.Contains("BOBINA_ONIX_PRISMA")) score += 130;
                if (d.Code.Contains("ABS")) score = 0; // Desarma boost do Onix no ABS
            }

            if ((termoNorm.Contains("vvt") || termoNorm.Contains("sincronismo") || termoNorm.Contains("correia banhada") || termoNorm.Contains("p0016") || termoNorm.Contains("p0017") || termoNorm.Contains("p0011") || (termoNorm.Contains("ford ka") && (termoNorm.Contains("vvt") || termoNorm.Contains("solenoide") || termoNorm.Contains("fase") || termoNorm.Contains("comando") || termoNorm.Contains("sincronismo"))))
                && d.Code.Contains("FORD_KA_VVT_SINCRONISMO")) score += 130;

            if ((termoNorm.Contains("renault") || termoNorm.Contains("sandero") || termoNorm.Contains("logan") || termoNorm.Contains("duster") || termoNorm.Contains("sce") || termoNorm.Contains("esm"))
                && (termoNorm.Contains("alternador") || termoNorm.Contains("pilotado") || termoNorm.Contains("ibs") || termoNorm.Contains("carga inteligente") || termoNorm.Contains("valeo"))
                && d.Code.Contains("RENAULT_ALTERNADOR_IBS")) score += 130;

            if ((termoNorm.Contains("indutivo") || termoNorm.Contains("efeito hall") || (termoNorm.Contains("hall") && termoNorm.Contains("sensor")) || termoNorm.Contains("indutivo vs hall") || termoNorm.Contains("indutivo ou hall") || (termoNorm.Contains("sensor") && termoNorm.Contains("2 fios") && termoNorm.Contains("3 fios")))
                && d.Code.Contains("SENSOR_INDUTIVO_VS_HALL")) score += 140;

            if ((termoNorm.Contains("rede can") || termoNorm.Contains("can bus") || termoNorm.Contains("linha lin") || termoNorm.Contains("60 ohms") || termoNorm.Contains("120 ohms") || (termoNorm.Contains("can") && termoNorm.Contains("obd")) || (termoNorm.Contains("pino 6") && termoNorm.Contains("14")) || termoNorm.Contains("barramento can"))
                && d.Code.Contains("REDE_CAN_VS_LIN")) score += 140;

            if ((termoNorm.Contains("efb") || termoNorm.Contains("agm") || (termoNorm.Contains("start stop") && (termoNorm.Contains("bateria comum") || termoNorm.Contains("bateria convencional") || termoNorm.Contains("posso colocar") || termoNorm.Contains("qual bateria") || termoNorm.Contains("tipo de bateria"))) || termoNorm.Contains("bateria start stop"))
                && d.Code.Contains("BATERIA_START_STOP_AGM_EFB"))
            {
                score += 140;
                if (d.Code.Contains("FUSIVEL")) score = 0;
            }

            // Farol Aceso Direto / Não Apaga / Relé Colado
            if ((termoNorm.Contains("farol") || termoNorm.Contains("luz")) 
                && (termoNorm.Contains("direto") || termoNorm.Contains("nao apaga") || termoNorm.Contains("nao desliga") || termoNorm.Contains("aceso direto") || termoNorm.Contains("desligo a chave") || termoNorm.Contains("chave desligada") || termoNorm.Contains("travado aceso")))
            {
                if (!termoNorm.Contains("hb20") && !termoNorm.Contains("creta"))
                {
                    if (d.Code.Contains("FAROL_ACESO_DIRETO")) score += 95;
                    if (d.Code.Equals("FAROL_DIREITO_APAGADO")) score = 0;
                }
            }

            // Chicote Sanfonado da Tampa Traseira (Desembaçador + Brake Light / Terceira Luz)
            if (((termoNorm.Contains("desembacador") || termoNorm.Contains("desembacador traseiro")) && (termoNorm.Contains("brake light") || termoNorm.Contains("luz de freio") || termoNorm.Contains("terceira luz") || termoNorm.Contains("limpador traseiro") || termoNorm.Contains("tampa") || termoNorm.Contains("porta malas") || termoNorm.Contains("ao mesmo tempo")))
                || ((termoNorm.Contains("brake light") || termoNorm.Contains("terceira luz")) && termoNorm.Contains("desembacador")))
            {
                if (d.Code.Contains("CHICOTE_TAMPA_TRASEIRA")) score += 95;
                if (d.Code.Equals("LANTERNA_TRASEIRA")) score = 0;
            }

            foreach (var t in tokens)
            {
                if (t.Length < 3 || StopWords.Contains(t)) continue;
                // Previne que 'fire' de Uno Fire bata indevidamente em 'misfire' ou DTC P0300
                if (t == "fire")
                {
                    if (NormalizarTexto(d.Code).Contains("fire") && !NormalizarTexto(d.Code).Contains("misfire")) score += 25;
                    continue;
                }
                if (NormalizarTexto(d.Code).Contains(t)) score += 30;
                if (NormalizarTexto(d.Title).Contains(t)) score += 20;
                if (NormalizarTexto(d.Symptoms).Contains(t)) score += 14;
                if (NormalizarTexto(d.System).Contains(t)) score += 10;
                if (fullText.Contains(t)) score += 4;
            }

            return score;
        }

        private static int CalcularRelevanciaManual(SystemManualEntry m, string termoNorm, string[] tokens)
        {
            int score = 0;
            var fullText = NormalizarTexto($"{m.Key} {m.Title} {m.Summary} {m.ModuleNavigationTarget} {string.Join(" ", m.Keywords)}");

            foreach (var kw in m.Keywords)
            {
                var kwNorm = NormalizarTexto(kw);
                if (termoNorm.Contains(kwNorm)) score += 40;
            }

            // Impulsos específicos para garantir precisão e evitar colisões entre manuais:
            if ((termoNorm.Contains("garantia") || termoNorm.Contains("90 dias") || termoNorm.Contains("termo de garantia") || termoNorm.Contains("cdc") || termoNorm.Contains("retorno em garantia"))
                && m.Key.Equals("RECIBO_TERMO_GARANTIA")) score += 100;

            if ((termoNorm.Contains("comissao") || termoNorm.Contains("comissoes") || termoNorm.Contains("comissao de eletricista") || termoNorm.Contains("pagar mecanico") || termoNorm.Contains("espelho de comissao"))
                && m.Key.Equals("COMISSOES_ELETRICISTAS")) score += 100;

            if ((termoNorm.Contains("historico") || (termoNorm.Contains("placa") && (termoNorm.Contains("passado") || termoNorm.Contains("servicos") || termoNorm.Contains("manutencoes") || termoNorm.Contains("consulta") || termoNorm.Contains("visitas"))))
                && m.Key.Equals("HISTORICO_VEICULO_PLACA")) score += 100;

            foreach (var t in tokens)
            {
                if (t.Length < 3 || StopWords.Contains(t)) continue;
                // Ignorar palavras comuns da rotina de oficina que causam colisão com problemas veiculares
                if (t == "oficina" || t == "teste" || t == "testes" || t == "funciona" || t == "funcionar" || t == "servico" || t == "servicos" || t == "carro" || t == "veiculo")
                    continue;

                if (NormalizarTexto(m.Title).Contains(t)) score += 20;
                if (NormalizarTexto(m.Summary).Contains(t)) score += 10;
                if (fullText.Contains(t)) score += 3;
            }

            return score;
        }

        public string MontarContextoTecnico(string mensagemUsuario)
        {
            var dtc = BuscarPorCodigoDTC(mensagemUsuario);
            var sb = new StringBuilder();

            if (dtc != null)
            {
                sb.AppendLine($"=== PROCEDIMENTO TÉCNICO PARA DTC {dtc.Code}: {dtc.Title} ===");
                sb.AppendLine($"Sistema: {dtc.System}");
                sb.AppendLine($"Sintomas Clínicos: {dtc.Symptoms}");
                sb.AppendLine("Causas Mais Frequentes:");
                foreach (var c in dtc.ProbableCauses) sb.AppendLine($" - {c}");
                sb.AppendLine("Procedimento de Teste Prático:");
                for (int i = 0; i < dtc.GuidedSteps.Count; i++) sb.AppendLine($" {i + 1}. {dtc.GuidedSteps[i]}");
                sb.AppendLine($"Padrões Elétricos de Referência: {dtc.ReferenceStandard}");
                sb.AppendLine($"Instrumentos Recomendados: {string.Join(", ", dtc.SuggestedTools)}");
                sb.AppendLine($"Peças Prováveis: {string.Join(", ", dtc.SuggestedStockParts)}");
                return sb.ToString();
            }

            var manual = BuscarManualSistema(mensagemUsuario);
            if (manual != null)
            {
                sb.AppendLine($"=== GUIA DE OPERAÇÃO DO SISTEMA: {manual.Title} ===");
                sb.AppendLine($"Módulo: {manual.ModuleNavigationTarget}");
                sb.AppendLine($"Resumo: {manual.Summary}");
                sb.AppendLine("Instruções de Uso:");
                foreach (var inst in manual.Instructions) sb.AppendLine($" - {inst}");
                sb.AppendLine();
            }

            var procedimentos = BuscarPorSintomaOuTermo(mensagemUsuario, 2);
            if (procedimentos.Count > 0)
            {
                sb.AppendLine("=== BASE DE CONHECIMENTO TÉCNICO PRIMOX (RAG) ===");
                foreach (var p in procedimentos)
                {
                    sb.AppendLine($"[Assunto: {p.Title} ({p.System})]");
                    sb.AppendLine($"Valores Nominais / Referência: {p.ReferenceStandard}");
                    sb.AppendLine("Passos Guiados:");
                    for (int i = 0; i < p.GuidedSteps.Count; i++) sb.AppendLine($" {i + 1}. {p.GuidedSteps[i]}");
                    sb.AppendLine($"Instrumental: {string.Join(", ", p.SuggestedTools)}");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        private static List<DiagnosticEntry> CarregarBaseConhecimentoTecnico()
        {
            return new List<DiagnosticEntry>
            {
                // =========================================================================
                // 1. ÁRVORES DE DIAGNÓSTICO INTERATIVO POR SINTOMA REAL DE OFICINA
                // =========================================================================
                new DiagnosticEntry
                {
                    Code = "CARRO_SEM_FUNCIONAR_TRIAGEM",
                    Title = "Triagem Técnica: Carro Chegou Sem Funcionar / Não Liga / Não Pega",
                    System = "Diagnóstico Global de Partida e Ignição (Triage Primária)",
                    Symptoms = "Carro rebocado para a oficina ou chegou de guincho sem funcionar; dá na chave e o motor não pega; gira arranque mas não entra em combustão ou nem vira o arranque.",
                    ProbableCauses = new List<string>
                    {
                        "Falta de Faísca / Ignição (Sensor de Rotação CKP sem sinal, bobina de ignição sem disparo, relé principal desarmado)",
                        "Falta de Combustível / Pressão na Flauta (Bomba de combustível queimada ou sem alimentação, fusível da bomba rompido, relé travado)",
                        "Bloqueio de Imobilizador / Transponder da Chave (Luz da chave/cadeado piscando no painel)",
                        "Falha Elétrica Primária / Arranque (Bateria descarregada < 10.5V, estalo de 'tec-tec' no automático, cabo de aterramento do motor partido)",
                        "Fusível Geral da Injeção ou Relé Principal desarmado (Luz da injeção não acende ao ligar a chave)"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. TESTE DA PARTIDA (O motor vira?): Se o arranque NÃO VIRA ou só estala 'tec-tec', o defeito é elétrico primário (bateria descarregada, automático de partida ou cabo terra). Se VIRA FORTE com velocidade normal, vá para o passo 2.",
                        "2. OBSERVE O PAINEL DE INSTRUMENTOS: Ao girar a chave no 1º estágio: A luz da injeção eletrônica acende? (Se NÃO acender, falta alimentação positiva linha 15 ou relé principal da central). A luz da chavinha/cadeado pisca sem parar? (Se piscar, o Imobilizador bloqueou a injeção!).",
                        "3. PRESSURIZAÇÃO DA BOMBA: Ao ligar a chave, escute no tanque o zumbido de 2 segundos da bomba de combustível. Se não houver barulho, meça com lâmpada de teste se chega 12V no conector da bomba no tanque ou teste o fusível da bomba.",
                        "4. TESTE DE FAÍSCA NAS VELAS: Coloque um centelhador de ignição (ou vela de teste aterrada) na ponta do cabo de vela/bobina e dê a partida. Se NÃO houver centelha E os bicos não tiverem pulso, o Sensor de Rotação (CKP) é o principal suspeito (sem sinal de rotação a ECU não libera faísca nem combustível)!",
                        "5. PRESSÃO DE COMBUSTÍVEL: Se houver faísca, engate o manômetro de pressão na flauta de injeção. A pressão deve ficar entre 3.0 e 4.2 bar (conforme o modelo)."
                    },
                    SuggestedTools = new List<string> { "Centelhador de Ignição / Lâmpada de Ponto", "Manômetro de Pressão de Combustível", "Caneta de Polaridade 12V", "Multímetro Digital True RMS", "Scanner OBD-II" },
                    SuggestedStockParts = new List<string> { "Sensor de Rotação (CKP)", "Bomba de Combustível (Refil)", "Relé Principal da Injeção", "Bobina de Ignição", "Bateria Automotiva 60Ah" },
                    ReferenceStandard = "Pressão da Linha de Combustível: 3.0 a 4.2 bar | Tensão da Bateria no Arranque: > 9.6V | Resistência do Sensor CKP Indutivo: 500Ω a 1200Ω (ou sinal digital 0-5V no Hall)",
                    ClarifyingQuestions = new List<string>
                    {
                        "Quando você dá partida na chave, o motor de arranque gira forte, vira pesado ou só faz tec-tec?",
                        "A luz da injeção eletrônica acende no painel ao ligar a chave, e a do cadeado/chave pisca?",
                        "Ao ligar a chave no 1º estágio, você escuta o zumbido da bomba de combustível no tanque?",
                        "Qual é o modelo, motorização e ano do veículo?"
                    },
                    InteractiveReplyChips = new List<string>
                    {
                        "Arranque vira forte mas não pega",
                        "Arranque só faz tec-tec",
                        "Luz da injeção não acende",
                        "Não escuto a bomba de combustível",
                        "Não tem faísca nas velas"
                    }
                },
                new DiagnosticEntry
                {
                    Code = "MOTOR_FALHANDO_ENGASGANDO",
                    Title = "Diagnóstico Interativo: Motor Falhando, Engasgando ou Sem Força",
                    System = "Injeção Eletrônica & Ignição Secundária",
                    Symptoms = "Motor trepida em marcha lenta, falha ao acelerar ou na retomada, sensação de estar rodando em '3 cilindros', luz de injeção piscando sob carga.",
                    ProbableCauses = new List<string>
                    {
                        "Bobina de ignição com fuga de alta tensão na carcaça ou enrolamento secundário queimado",
                        "Cabos de vela com resistência acima de 10kΩ ou centelhamento externo",
                        "Velas de ignição desgastadas ou com folga excessiva entre eletrodos (> 1.0mm)",
                        "Bico injetor travado, entupido ou chicote do bico sem pulso",
                        "Entrada falsa de ar no coletor de admissão ou mangueira de vácuo do servo-freio furada"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. IDENTIFICAÇÃO DO CILINDRO: Com o motor em marcha lenta e alicate isolado, desconecte os cabos de vela (ou conectores das bobinas individuais) um a um. O cilindro em que o motor NÃO mudar o funcionamento é o cilindro que está falhando!",
                        "2. TESTE DA BOBINA / CABO: Retire a vela do cilindro falho e confira a centelha. Se for azulada e forte (> 15kV), a ignição está boa. Se for amarelada, fraca ou inexistente, troque o cabo/bobina.",
                        "3. INSPEÇÃO DAS VELAS: Vela encharcada de combustível indica falta de faísca. Vela seca e esbranquiçada indica falta de injeção de combustível naquele cilindro.",
                        "4. PULSO DO INJETOR: Verifique com caneta de polaridade se o conector do bico do cilindro falho recebe 12V pós-chave e pulso negativo ritmado durante o funcionamento."
                    },
                    SuggestedTools = new List<string> { "Osciloscópio com Pinça Capacitiva / Secundário", "Multímetro Digital", "Caneta de Polaridade", "Scanner OBD-II" },
                    SuggestedStockParts = new List<string> { "Bobina de Ignição", "Jogo de Cabos de Vela", "Jogo de Velas de Ignição", "Bico Injetor", "Filtro de Combustível" },
                    ReferenceStandard = "Resistência Cabos de Vela: 2kΩ a 8kΩ por metro | Abertura dos Eletrodos: 0.8mm a 1.0mm | Resistência Bico Injetor: 11Ω a 16Ω",
                    ClarifyingQuestions = new List<string>
                    {
                        "Qual o modelo e motor do veículo?",
                        "A falha acontece em marcha lenta ou somente quando você pisa fundo na aceleração?",
                        "A luz de injeção acendeu ou piscou no painel?"
                    },
                    InteractiveReplyChips = new List<string>
                    {
                        "Falha ao acelerar fundo",
                        "Falha direta em marcha lenta",
                        "Já troquei velas e cabos",
                        "Luz de injeção piscando",
                        "Bobina está sem faísca no cilindro"
                    }
                },
                new DiagnosticEntry
                {
                    Code = "MOTOR_OSCILANDO_MARCHA_LENTA_MORRENDO",
                    Title = "Diagnóstico Interativo: Motor Oscilando a Marcha Lenta, Falhando ou Morrendo",
                    System = "Gestão de Ar, Admissão & Controle de Marcha Lenta (TBI/IAC)",
                    Symptoms = "Motor liga mas oscila na lenta, rotação sobe e desce, morre em desaceleração ou no semáforo, engasga ao acelerar devagar, marcha lenta irregular, chega até a morrer.",
                    ProbableCauses = new List<string>
                    {
                        "Corpo de borboleta (TBI) com carbonização severa impedindo batente mínimo da borboleta",
                        "Atuador / Motor de passo de marcha lenta (IAC) engripado ou com sujeira na sede cônica",
                        "Entrada falsa de ar (furo em mangueira de hidrovácuo/servo-freio, respiro de cárter blow-by ou junta do coletor trincada)",
                        "Sensor MAP (pressão absoluta) marcando vácuo incorreto ou com chicote em curto/aberto",
                        "Sensor de temperatura do motor (ECT) informando temperatura errada, afogando ou empobrecendo a mistura",
                        "Válvula de purga do cânister travada aberta puxando vapores ricos direto para o coletor"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. IDENTIFICAÇÃO DO VEÍCULO: O primeiro passo mandatório na oficina é saber o modelo e motor: sistemas como VW EA111, Fiat Fire, GM VHC/SPE4 e Hyundai Kappa possuem pontos críticos completamente diferentes.",
                        "2. INSPEÇÃO DO CORPO DE BORBOLETA (TBI): Remova a mangueira do filtro de ar. Se houver anel preto de óleo e carvão ao redor da borboleta, limpe com descarbonizante e realize a adaptação básica com scanner.",
                        "3. TESTE DE ENTRADA FALSA DE AR: Com o motor funcionando, aplique fumaça com gerador ou borrife com cautela água nas mangueiras de vácuo, respiro do óleo e coletor. Se a marcha lenta mudar, há vazamento de ar!",
                        "4. LEITURA DE PARÂMETROS NO SCANNER: Verifique: MAP (normal entre 280 e 420 mbar em marcha lenta), Tempo de Injeção (normal 1.8 a 3.2 ms), e Sonda Lambda oscilando rápido entre 100mV e 900mV.",
                        "5. LINHA DE COMBUSTÍVEL: Meça pressão com manômetro: deve cravar entre 3.0 e 4.2 bar sem oscilar."
                    },
                    SuggestedTools = new List<string> { "Scanner Automotivo com Gráficos", "Máquina de Fumaça (Smoke Tester)", "Manômetro de Pressão de Combustível", "Multímetro" },
                    SuggestedStockParts = new List<string> { "Atuador de Marcha Lenta", "Sensor MAP", "Sensor de Temperatura ECT", "Jogo de Velas e Cabos", "Válvula do Canister" },
                    ReferenceStandard = "Pressão no Coletor (MAP): 280 a 400 mbar na lenta | Pressão de Combustível: 3.0 a 4.2 bar | Sonda Lambda: 100mV a 900mV alternando a cada 1s",
                    ClarifyingQuestions = new List<string>
                    {
                        "👉 Qual é o modelo, ano e motorização do veículo? (Ex: Gol EA111, Palio Fire, Onix, Corsa VHC, HB20?)",
                        "👉 A oscilação acontece com motor frio ou também depois que atinge 90°C?",
                        "👉 O carro morre ao pisar no freio (hidrovácuo) ou ao engatar marcha?"
                    },
                    InteractiveReplyChips = new List<string>
                    {
                        "É um Gol / Fox EA111",
                        "É um Palio / Uno Fire",
                        "É um Corsa / Celta / Onix GM",
                        "É um HB20 1.0",
                        "Já limpei o corpo de borboleta TBI",
                        "Suspeito de entrada falsa de ar"
                    }
                },
                new DiagnosticEntry
                {
                    Code = "VENTOINHA_ARREFECIMENTO",
                    Title = "Diagnóstico Interativo: Ventoinha Não Liga / Motor Aquecendo / Fervendo",
                    System = "Arrefecimento & Gestão Térmica do Motor",
                    Symptoms = "Ponteiro da temperatura sobe até a faixa vermelha no trânsito, líquido ferve e vaza pelo reservatório, eletroventilador não aciona nem com o ar ligado.",
                    ProbableCauses = new List<string>
                    {
                        "Fusível de alta amperagem (Maxi-fusível 30A ou 40A) queimado no cofre do motor",
                        "Relé de acionamento da ventoinha (1ª ou 2ª velocidade) travado ou com bobina aberta",
                        "Resistência de 1ª velocidade da ventoinha rompida (faz a ventoinha não ligar na baixa)",
                        "Motor do eletroventilador travado mecanicamente ou com escovas gastas",
                        "Sensor de temperatura do líquido de arrefecimento (ECT/CTS) com leitura incorreta ou desconectado"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. TESTE DIRETO NO ELETROVENTILADOR: Desconecte o plugue da ventoinha e injete 12V e terra direto da bateria nos terminais do motor. Se não girar forte, a ventoinha está queimada ou travada!",
                        "2. FUSÍVEL MAXI: Confira na caixa de fusíveis do cofre do motor o maxi-fusível de 30A/40A dedicado à ventoinha.",
                        "3. TESTE DE EMERGÊNCIA (SENSOR ECT): Desconecte o plugue do sensor de temperatura da água no motor e ligue a chave. Por estratégia de segurança (fail-safe), a ECU deve acionar a ventoinha na velocidade máxima imediatamente! Se acionar, o circuito de força e relés estão 100% íntegros e o defeito é o sensor ou ar no sistema.",
                        "4. RESISTÊNCIA DA 1ª VELOCIDADE: Localizada no defletor do radiador, meça se há continuidade no filamento de níquel-cromo ou no fusível térmico embutido.",
                        "5. RELÉ DE VELOCIDADE: Remova o relé e faça um jumper (ponte com fio de bitola adequada) entre os pinos 30 e 87 do soquete: a ventoinha deve disparar na hora."
                    },
                    SuggestedTools = new List<string> { "Lâmpada de Teste 12V", "Multímetro Digital", "Fio Jumper com Fusível de Proteção", "Scanner OBD-II" },
                    SuggestedStockParts = new List<string> { "Eletroventilador Completo", "Resistência de Ventoinha 1ª Velocidade", "Relé Auxiliar 40A/50A", "Sensor de Temperatura ECT", "Maxi-Fusível 40A" },
                    ReferenceStandard = "Tensão no Conector da Ventoinha: > 12.5V DC | Resistência da 1ª Velocidade: ~0.8Ω a 1.5Ω | Corrente de Consumo da Ventoinha: 15A a 25A",
                    ClarifyingQuestions = new List<string>
                    {
                        "Qual o modelo e motor do veículo?",
                        "Ao ligar o ar condicionado a ventoinha dispara imediatamente?",
                        "Se você desconectar o sensor de temperatura a ventoinha entra em modo de emergência?"
                    },
                    InteractiveReplyChips = new List<string>
                    {
                        "Liguei direto na bateria e funcionou",
                        "Ventoinha não liga direto",
                        "Ao ligar ar condicionado não dispara",
                        "Maxi-fusível está queimado",
                        "Sensor de temperatura desconectado armou ventoinha"
                    }
                },
                new DiagnosticEntry
                {
                    Code = "VIDRO_ELETRICO_TRAVAS",
                    Title = "Diagnóstico Interativo: Vidro Elétrico Não Sobe / Não Desce ou Trava Inoperante",
                    System = "Conforto & Acessórios de Carroceria",
                    Symptoms = "Vidro do motorista ou passageiro não responde ao botão, faz barulho de motor girando mas vidro não sobe, trava elétrica da porta não aciona pelo controle.",
                    ProbableCauses = new List<string>
                    {
                        "Fios rompidos no chicote de borracha que passa entre a coluna dianteira e a porta (quebra por fadiga mecânica ao abrir/fechar)",
                        "Interruptor / botão de comando com contatos internos oxidados ou platinados gastos",
                        "Cabo de aço da máquina de vidro arrebentado ou enrolado fora da carretilha",
                        "Motor do vidro (mabuchi) travado ou com escovas gastas",
                        "Fusível de conforto/vidros na caixa interna rompido"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. ESCUTE O RUÍDO NA PORTA: Ao apertar o botão, ouve o motor girando? Se o motor gira mas o vidro não sobe, a máquina quebrou os cabos de aço ou o arraste plástico soltou da calha.",
                        "2. CHICOTE DO GUARDA-PÓ DA PORTA: Puxe a borracha sanfonada entre a coluna e a porta. 70% dos defeitos em vidros e travas são fios partidos exatamente nessa passagem flexível!",
                        "3. TESTE DO INTERRUPTOR: Remova o comando de vidro e meça se chegam 12V de alimentação e terra. Ao acionar o botão para cima/baixo, a polaridade nos dois fios que vão para o motor deve inverter (+12V e GND para subir, GND e +12V para descer).",
                        "4. ALIMENTAÇÃO DIRETA NO MOTOR: Injete 12V e massa diretamente nos dois fios do motor do vidro para testar o enrolamento dele em isolamento."
                    },
                    SuggestedTools = new List<string> { "Lâmpada de Teste 12V", "Multímetro Digital", "Espátulas de Desmontagem de Forro de Porta" },
                    SuggestedStockParts = new List<string> { "Máquina de Vidro Elétrico", "Interruptor / Botão de Vidro", "Motor de Vidro Elétrico 12V", "Trava Elétrica Universal 2/5 Fios", "Fusível 20A/30A" },
                    ReferenceStandard = "Tensão no Motor do Vidro: > 11.5V DC sob carga | Inversão de Polaridade completa na comutação",
                    ClarifyingQuestions = new List<string>
                    {
                        "Qual o modelo e se o defeito é em uma porta específica ou em todas?",
                        "Ao apertar o botão, você ouve o motorzinho fazer barulho na porta?",
                        "O vidro desce mas não sobe, ou está totalmente morto?"
                    },
                    InteractiveReplyChips = new List<string>
                    {
                        "Ouve barulho do motor mas não sobe",
                        "Totalmente morto sem barulho",
                        "Chicote da coluna tem fio partido",
                        "Todas as portas pararam juntas",
                        "Já testei o fusível de conforto"
                    }
                },
                new DiagnosticEntry
                {
                    Code = "FAROL_DIREITO_APAGADO",
                    Title = "Diagnóstico Interativo: Farol Direito Apagado ou Luz Fraca",
                    System = "Iluminação Externa & Segurança",
                    Symptoms = "Farol direito não acende (baixo ou alto), lâmpada não liga mesmo trocada, soquete derretido, pisca e meia-luz oscilam juntos.",
                    ProbableCauses = new List<string>
                    {
                        "Fusível individual do farol direito rompido (a maioria dos veículos separa o fusível do farol esquerdo e direito por segurança)",
                        "Lâmpada H4/H7 queimada com filamento partido",
                        "Conector / soquete da lâmpada derretido ou terminais de latão com folga e oxidação",
                        "Aterramento deficiente (ponto de massa W/G na lata próximo ao farol direito com oxidação)",
                        "Chave de seta ou relé de farol com contato carbonizado"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. FUSÍVEL INDIVIDUAL: Vá na caixa de fusíveis e teste com lâmpada de teste o fusível específico do farol baixo direito. Se não houver 12V em ambos os lados do fusível, troque-o.",
                        "2. SOQUETE E LÂMPADA: Remova o soquete traseiro. Verifique se o plástico está escurecido/derretido. Meça se chegam 12V no terminal positivo com a chave ligada.",
                        "3. TESTE DE MASSA (Aterramento): Coloque a ponta do multímetro no terminal negativo do soquete e a outra no polo negativo da bateria. A queda deve ser menor que 0.10V. Se der 12V, a lâmpada não acende porque falta aterramento!",
                        "4. CHAVE DE SETA / RELÉ: Se não chega 12V no fusível, o defeito está no comutador da chave de seta na coluna de direção ou no relé de iluminação."
                    },
                    SuggestedTools = new List<string> { "Lâmpada de Teste 12V", "Multímetro Digital True RMS", "Chave Philips / Fenda" },
                    SuggestedStockParts = new List<string> { "Lâmpada H4 12V 55/60W", "Lâmpada H7 12V 55W", "Conector Cerâmico para Lâmpada H4/H7", "Fusível Lâmina 10A ou 15A", "Relé Auxiliar de Farol 40A" },
                    ReferenceStandard = "Tensão no Soquete: > 12.8V DC com motor ligado | Queda no Negativo (Massa): < 0.10V | Resistência do filamento frio: ~0.4Ω a 0.8Ω",
                    ClarifyingQuestions = new List<string>
                    {
                        "Qual é o modelo, marca e ano do carro?",
                        "O farol alto funciona normalmente no lado direito, ou ambos estão apagados?",
                        "Você tem multímetro ou lâmpada de teste aí na bancada?"
                    },
                    InteractiveReplyChips = new List<string>
                    {
                        "O fusível está bom",
                        "Farol alto funciona normal",
                        "Ambos os faróis apagados",
                        "O soquete está derretido",
                        "Tenho multímetro na mão"
                    }
                },
                new DiagnosticEntry
                {
                    Code = "ILUMINACAO_LANTERNA",
                    Title = "Diagnóstico Interativo: Lanterna Traseira Apagada / Luz de Freio / Meia-Luz",
                    System = "Iluminação Traseira & Sinalização",
                    Symptoms = "Lanterna traseira não acende (direita ou esquerda), luz de freio inoperante, ao ligar a seta a lanterna pisca junto, aviso de lâmpada queimada no painel.",
                    ProbableCauses = new List<string>
                    {
                        "Lâmpada de 2 polos (P21/5W) ou 1 polo queimada com filamento partido",
                        "Placa de circuito da lanterna traseira com trilhas oxidadas ou soquete com folga",
                        "Falta de aterramento no conector da lanterna traseira (efeito 'árvore de natal' onde tudo pisca junto)",
                        "Fusível individual da lanterna / meia-luz rompido na caixa de fusíveis",
                        "Interruptor do pedal de freio com defeito ou desregulado (caso a luz de freio não acenda)"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. LÂMPADA: Retire a lâmpada da lanterna (geralmente P21/5W de 2 polos). Confira se os pinos da base são desencontrados (BAY15d) e se os filamentos estão íntegros.",
                        "2. PLACA E SOQUETES: Inspecione as trilhas de latão da placa da lanterna. É comum haver derretimento plástico ou oxidação verde por infiltração de água da chuva.",
                        "3. ATERRAMENTO (Terminal Massa): Se ao ligar o pisca a lanterna inteira piscar fraca, limpe o parafuso de fixação do fio terra na lataria do porta-malas.",
                        "4. FUSÍVEL: Meça com lâmpada de teste os fusíveis de iluminação de posição (meia-luz) e luz de freio na caixa de fusíveis.",
                        "5. INTERRUPTOR DE FREIO: Se apenas o freio não acende dos dois lados, teste o interruptor no pedal com multímetro (deve dar continuidade ao pisar no pedal)."
                    },
                    SuggestedTools = new List<string> { "Lâmpada de Teste 12V", "Multímetro Digital", "Chave de Fenda / Torx" },
                    SuggestedStockParts = new List<string> { "Lâmpada 2 Polos P21/5W", "Lâmpada 1 Polo P21W", "Placa de Circuito da Lanterna", "Interruptor do Pedal de Freio", "Fusível Lâmina 10A" },
                    ReferenceStandard = "Tensão no Conector da Lanterna: > 12.0V DC | Queda de Tensão no Terra: < 0.15V DC",
                    ClarifyingQuestions = new List<string>
                    {
                        "Qual o modelo e ano do carro?",
                        "É a lanterna traseira esquerda, direita, ou ambas?",
                        "A luz de freio acende quando você pisa no pedal?"
                    },
                    InteractiveReplyChips = new List<string>
                    {
                        "Luz de freio acende normal",
                        "Lâmpada de 2 polos está boa",
                        "Ao dar seta pisca tudo junto",
                        "Placa traseira tem oxidação",
                        "Ambos os lados apagados"
                    }
                },
                new DiagnosticEntry
                {
                    Code = "MOTOR_PARTIDA_TECTEC",
                    Title = "Diagnóstico Interativo: Arranque Faz 'Tec-Tec' ou Vira Pesado",
                    System = "Sistema de Partida & Potência DC",
                    Symptoms = "Gira a chave e ouve apenas o clique 'tec-tec' no automático, arranque gira muito devagar parecendo bateria descarregada, luzes do painel piscam ou apagam na partida.",
                    ProbableCauses = new List<string>
                    {
                        "Automático de partida (solenoide) com contatos de cobre carbonizados (não transfere a linha 30 para o induzido)",
                        "Escovas do motor de arranque no fim da vida útil ou travadas nas guias",
                        "Buchas de bronze dianteira e traseira com folga excessiva (o induzido cola nos ímãs de campo)",
                        "Queda de tensão extrema no cabo positivo B+ da bateria ou na malha de aterramento do motor",
                        "Bateria sem capacidade de partida a frio (CCA baixo ou sulfatada)"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. LINHA 50: Meça a tensão no terminal fino do automático de partida durante a tentativa de partida. Deve ter no mínimo 10.5V. Se cair para < 8V, o comutador de ignição está com defeito.",
                        "2. QUEDA DE TENSÃO NO CABO POSITIVO: Ponta vermelha no polo positivo da bateria e ponta preta no parafuso 30 do arranque durante o arranque. Máximo tolerado: 0.50V DC.",
                        "3. QUEDA DE TENSÃO NA MASSA DO BLOCO: Ponta vermelha na carcaça do motor de arranque e ponta preta no borne negativo da bateria na partida. Máximo: 0.25V DC. Se der alto, limpe o cabo terra do motor!",
                        "4. TENSÃO DA BATERIA SOB CARGA: Meça a bateria DURANTE o arranque. Se cair abaixo de 9.6V a 20°C, a bateria está esgotada ou com placa em curto.",
                        "5. BANCADA: Retire o arranque e teste corrente em vazio (normal: 50A a 80A). Acima de 120A em vazio confirma buchas gastas ou induzido em curto."
                    },
                    SuggestedTools = new List<string> { "Alicate Amperímetro DC (escala 600A)", "Multímetro Digital True RMS", "Testador de Condutância de Bateria" },
                    SuggestedStockParts = new List<string> { "Automático de Partida (Solenoide)", "Porta Escovas do Arranque", "Jogo de Buchas de Bronze", "Bendix / Pinhão de Arranque", "Bateria Automotiva 60Ah" },
                    ReferenceStandard = "Tensão Mínima na Partida: 9.6V DC | Queda Positiva: < 0.5V | Queda Massa: < 0.25V | Corrente de Arranque Normal: 120A a 220A",
                    ClarifyingQuestions = new List<string>
                    {
                        "Qual o modelo e motorização do carro?",
                        "A bateria é nova ou já tem mais de 2 anos?",
                        "Quando você dá a partida, as luzes do painel apagam totalmente?"
                    },
                    InteractiveReplyChips = new List<string>
                    {
                        "Bateria é nova",
                        "Luzes do painel apagam",
                        "Luzes continuam acesas",
                        "Linha 50 tem 12V",
                        "Já testei o aterramento"
                    }
                },
                new DiagnosticEntry
                {
                    Code = "ALTERNADOR_NAO_CARREGA",
                    Title = "Diagnóstico Interativo: Alternador Não Carrega / Luz de Bateria Acesa",
                    System = "Sistema de Carga & Geração de Energia",
                    Symptoms = "Luz de bateria acesa no painel, bateria descarrega com o carro rodando, faróis variam intensidade conforme acelera, chiado agudo na correia.",
                    ProbableCauses = new List<string>
                    {
                        "Regulador de voltagem com defeito ou escovas gastas que perderam contato com o coletor",
                        "Placa de diodos retificadores com diodos abertos ou em curto",
                        "Rotor com enrolamento aberto (pistas de cobre sem continuidade)",
                        "Polia de roda-livre (OAP) travada ou patinando em falso",
                        "Queda de tensão extrema no cabo positivo entre B+ do alternador e a bateria"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. TENSÃO DE CARGA: Meça a tensão nos polos da bateria com o motor a 2000 RPM e faróis altos acesos: deve ficar entre 13.8V e 14.5V DC.",
                        "2. COMPARAÇÃO B+ vs BATERIA: Se estiver abaixo de 13.0V na bateria, meça entre o parafuso B+ do alternador e a carcaça dele. Se no alternador der 14.2V e na bateria 12.5V, o cabo positivo está rompido ou com alta resistência!",
                        "3. TESTE DE RIPPLE (Ondulação AC): Multímetro em escala AC nos polos da bateria com motor ligado. Máximo tolerado: 0.05V AC (50mV). Se der acima, há diodo estourado na placa retificadora.",
                        "4. TESTE DO ROTOR: Em bancada, meça a resistência ôhmica entre os anéis coletores de cobre (normal: 2.2Ω a 4.0Ω). Entre anéis e o eixo de aço deve dar resistência infinita (sem fuga para massa)."
                    },
                    SuggestedTools = new List<string> { "Multímetro Digital True RMS", "Alicate Amperímetro DC", "Testador de Reguladores de Bancada" },
                    SuggestedStockParts = new List<string> { "Regulador de Voltagem", "Placa Retificadora de Diodos", "Rolamentos Dianteiro e Traseiro", "Correia Poly-V" },
                    ReferenceStandard = "Tensão de Carga: 13.8V a 14.5V DC | Ripple AC Máximo: 0.05V AC (50mV) | Resistência do Rotor: 2.2Ω a 4.0Ω",
                    ClarifyingQuestions = new List<string>
                    {
                        "Qual o veículo e se o alternador é convencional ou pilotado por central (LIN/BSS)?",
                        "Quantos Volts o multímetro marca na bateria com o motor ligado?",
                        "A correia está bem esticada e a polia girando normalmente?"
                    },
                    InteractiveReplyChips = new List<string>
                    {
                        "Marca menos de 12.5V ligado",
                        "Marca 14V normal no alternador",
                        "Alternador é pilotado",
                        "Correia está esticada",
                        "Ripple AC deu alto"
                    }
                },
                new DiagnosticEntry
                {
                    Code = "CONSUMO_PARASITA",
                    Title = "Diagnóstico Interativo: Fuga de Corrente / Consumo Parasita em Repouso",
                    System = "Alimentação & Redes em Hibernação",
                    Symptoms = "Bateria descarrega de um dia para o outro ou após o fim de semana mesmo com o alternador carregando perfeitamente.",
                    ProbableCauses = new List<string>
                    {
                        "Módulos eletrônicos que não entram em modo Sleep / Hibernação (BCM, Painel, Multimídia, Trava)",
                        "Rastreador GPS clandestino ou mal instalado com alimentação direta de alta corrente",
                        "Lâmpada do porta-malas ou porta-luvas permanentemente acesa por interruptor quebrado",
                        "Diodo retificador do alternador com micro-fuga para a carcaça",
                        "Módulo de subida de vidro pós-venda ou alarme com relé travado puxando corrente"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. PREPARAÇÃO: Abra o capô e trave a fechadura manualmente com chave de fenda para enganar o sensor de alarme. Feche todas as portas e trave o carro pelo controle.",
                        "2. TEMPO DE HIBERNAÇÃO (SLEEP): Aguarde de 15 a 45 minutos para a rede CAN adormecer completamente.",
                        "3. MEDIÇÃO: Instale o alicate amperímetro na escala de mA no cabo negativo da bateria (ou multímetro em série em 10A DC).",
                        "4. VALOR NOMINAL ACEITÁVEL: Abaixo de 0.050A (50mA). Se tiver rastreador GPS ativo, até 0.070A (70mA).",
                        "5. RASTREAMENTO POR FUSÍVEL: Se estiver alto (ex: 250mA, 600mA), retire os fusíveis um a um. No instante em que a corrente despencar para < 50mA, o fusível retirado identifica o circuito ladrão de carga!"
                    },
                    SuggestedTools = new List<string> { "Alicate Amperímetro DC de Precisão (escala mA)", "Multímetro Digital True RMS", "Extrator de Fusíveis" },
                    SuggestedStockParts = new List<string> { "Relé Auxiliar 40A", "Módulo de Vidro", "Fusíveis Lâmina", "Interruptor de Cortesia" },
                    ReferenceStandard = "Consumo em Repouso Normal: 20mA a 50mA (0.020A - 0.050A) após 30 minutos de trancamento.",
                    ClarifyingQuestions = new List<string>
                    {
                        "Qual o modelo do carro e se ele possui acessórios instalados (alarme pós-venda, multimídia, som potente, rastreador)?",
                        "Você tem alicate amperímetro ou multímetro com escala de mA?",
                        "A bateria descarrega em quantas horas parado?"
                    },
                    InteractiveReplyChips = new List<string>
                    {
                        "Tem som e alarme pós-venda",
                        "Descarrega de um dia pro outro",
                        "Já esperei entrar em sleep",
                        "Consumo medido acima de 300mA",
                        "Vou testar os fusíveis"
                    }
                },
                new DiagnosticEntry
                {
                    Code = "FUSIVEL_QUEIMANDO_CURTO",
                    Title = "Diagnóstico Interativo: Fusível Queimando Direto / Rastreamento de Curto",
                    System = "Chicotes & Distribuição Elétrica",
                    Symptoms = "Fusível queima imediatamente ao colocar outro ou logo ao ligar a chave/dispositivo elétrico (farol, buzina, limpador, ignição).",
                    ProbableCauses = new List<string>
                    {
                        "Fio positivo descascado encostando na lata ou bloco por atrito contínuo do chicote",
                        "Componente elétrico em curto interno (motor do limpador, bomba de combustível, solenoide)",
                        "Umidade acumulada na caixa de fusíveis ou conector com ponte condutora oxidada",
                        "Acessório instalado de forma incorreta com parafuso perfurando o chicote original"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. TÉCNICA DA LÂMPADA EM SÉRIE (Não queime dezenas de fusíveis!): Retire o fusível que está queimando.",
                        "2. Conecte nos 2 terminais do soquete do fusível uma lâmpada halógena de 12V 21W (lâmpada de freio) com garras jacaré.",
                        "3. Se houver curto direto para a carcaça, a lâmpada acenderá com 100% de brilho, absorvendo toda a corrente com segurança!",
                        "4. Comece a movimentar os chicotes no cofre e debaixo do painel, e desconecte os consumidores daquele circuito um a um.",
                        "5. No exato instante em que você afastar o fio descascado da lataria ou desconectar o componente em curto, A LÂMPADA SE APAGARÁ NA HORA! Esse é o ponto exato da avaria."
                    },
                    SuggestedTools = new List<string> { "Lâmpada de Teste 21W com Terminais Jacaré", "Multímetro Digital com Sinal Sonoro", "Espaguete Termorretrátil", "Fita de Tecido Automotivo" },
                    SuggestedStockParts = new List<string> { "Kit Fusíveis Lâmina Variados", "Porta Fusível Estanque", "Fita Isolante de Alta Temperatura" },
                    ReferenceStandard = "Resistência de Isolamento para Massa (circuito desligado): > 10.000Ω. Curto direto: 0.0Ω a 0.5Ω.",
                    ClarifyingQuestions = new List<string>
                    {
                        "Qual é o número ou amperagem do fusível que está queimando (ex: 10A, 15A, 20A)?",
                        "Ele queima na hora que espeta ou só quando você aciona algum botão específico?",
                        "Foi instalado algum acessório recentemente no carro?"
                    },
                    InteractiveReplyChips = new List<string>
                    {
                        "Queima na hora que espeta",
                        "Queima só quando liga o botão",
                        "É fusível de 15A",
                        "Vou fazer o teste da lâmpada",
                        "A lâmpada acendeu com 100%"
                    }
                },
                new DiagnosticEntry
                {
                    Code = "AR_CONDICIONADO_NAO_GELA",
                    Title = "Diagnóstico Interativo: Ar-Condicionado Não Gela / Compressor Não Atraca",
                    System = "Climatização & Gestão Térmica da Cabine",
                    Symptoms = "Ar sopra em temperatura ambiente (não gela), compressor do ar não atraca o acoplamento magnético, botão do A/C acende mas não esfria, chiado de vazamento.",
                    ProbableCauses = new List<string>
                    {
                        "Falta de gás refrigerante (R134a/R1234yf) por microvazamento (pressostato bloqueia o acionamento por segurança)",
                        "Bobina da embreagem eletromagnética do compressor queimada ou fusível térmico interno aberto",
                        "Relé do compressor do ar-condicionado com contatos platinados carbonizados",
                        "Fusível de proteção do circuito do A/C queimado",
                        "Sensor de temperatura do evaporador (anticongelamento) com leitura alterada travando em negativo"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. TESTE DA PRESSÃO DO GÁS (Pressostato): Engate o manifold nas válvulas de alta e baixa com motor desligado. Pressão estática normal a 25°C: 60 a 80 PSI. Se der menos de 30 PSI, há vazamento e o pressostato corta o compressor!",
                        "2. TESTE DA BOBINA DO COMPRESSOR: Desconecte o plugue de 1 ou 2 fios do compressor e meça com o multímetro a resistência ôhmica da bobina: normal entre 3.2Ω e 4.5Ω. Se der circuito aberto (1 no multímetro), a bobina queimou.",
                        "3. RELÉ DO COMPRESSOR (JUMPER): Remova o relé do A/C na caixa de fusíveis e faça um jumper entre os pinos 30 e 87: se a embreagem atracar com estalo metálico firme, todo o circuito de força está perfeito!",
                        "4. PRESSOSTATO LINEAR: Se o gás está cheio e a bobina boa, verifique se chegam 5V de alimentação e terra no pressostato e meça o sinal de retorno (normal: ~1.2V a 1.6V em repouso)."
                    },
                    SuggestedTools = new List<string> { "Manifold de Ar-Condicionado R134a", "Multímetro Digital True RMS", "Lâmpada de Teste 12V", "Termômetro Digital de Difusor" },
                    SuggestedStockParts = new List<string> { "Bobina Magnética do Compressor", "Relé Auxiliar 40A", "Pressostato Linear", "Filtro Secador", "Filtro de Cabine / Ar-Condicionado" },
                    ReferenceStandard = "Pressão Estática de Repouso: 60 a 80 PSI | Resistência da Bobina: 3.2Ω a 4.5Ω | Temperatura no Difusor em Funcionamento: 4°C a 8°C",
                    ClarifyingQuestions = new List<string>
                    {
                        "Qual o modelo e ano do carro?",
                        "Ao ligar o botão do ar-condicionado, você escuta o estalo 'claque' do compressor atracando lá no motor?",
                        "O ventilador interno da cabine sopra vento normalmente nas 4 velocidades?"
                    },
                    InteractiveReplyChips = new List<string>
                    {
                        "Compressor não faz estalo",
                        "Tem gás mas compressor não atraca",
                        "Bobina deu circuito aberto",
                        "Ar sopra fraco na cabine",
                        "Ventoinha do radiador não arma com o ar"
                    }
                },
                new DiagnosticEntry
                {
                    Code = "BUZINA_INOPERANTE",
                    Title = "Diagnóstico Interativo: Buzina Parou de Tocar / Inoperante",
                    System = "Segurança & Sinalização Sonora",
                    Symptoms = "Aperta o volante e a buzina não emite som, toca fraca e rouca, ou só buzina com o volante virado para um dos lados.",
                    ProbableCauses = new List<string>
                    {
                        "Cinta de Airbag / Clock Spring (hard disc da coluna) rompida internamente por fadiga",
                        "Fusível da buzina queimado na caixa de fusíveis do cofre ou interna",
                        "Relé da buzina com contatos carbonizados (clica mas não passa corrente)",
                        "Terminal negativo/massa com oxidação ou ferrugem no ponto de fixação da buzina na lataria",
                        "Caracol da buzina com membrana travada ou enrolamento queimado"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. ESCUTE O RELÉ DA BUZINA: Peça para alguém apertar o volante e encoste a mão no relé da buzina. Se ouvir/sentir o clique do relé, a cinta do volante, botão e comando estão 100% perfeitos! O defeito é daí para a buzina.",
                        "2. SE O RELÉ NÃO CLICA: O defeito é no volante (contato do botão) ou na fita da cinta do airbag (Clock Spring). Se a luz do airbag também estiver acesa, é 100% a cinta partida!",
                        "3. TESTE DIRETO NA BUZINA: Localize a buzina atrás da grade dianteira. Desconecte o plugue e teste com lâmpada de teste se chega 12V ao apertar a buzina.",
                        "4. TESTE DE MASSA: Aterre a carcaça da buzina direto no polo negativo da bateria com um fio de bancada. Se voltar a tocar, limpe a ferrugem do parafuso de fixação."
                    },
                    SuggestedTools = new List<string> { "Lâmpada de Teste 12V", "Multímetro Digital", "Fio Jumper com Garra Jacaré" },
                    SuggestedStockParts = new List<string> { "Buzina Caracol 12V Grave e Aguda", "Relé Auxiliar 4 Pinos 40A", "Cinta de Airbag / Hard Disc", "Fusível Lâmina 15A/20A" },
                    ReferenceStandard = "Tensão no Conector da Buzina: > 12.0V DC | Corrente Consumida: 3.5A a 5.0A por caracol | Queda de Tensão no Terra: < 0.1V",
                    ClarifyingQuestions = new List<string>
                    {
                        "Qual o modelo do carro?",
                        "Ao apertar o volante você escuta o relé dar o clique na caixa de fusíveis?",
                        "A luz do airbag está acesa no painel de instrumentos?"
                    },
                    InteractiveReplyChips = new List<string>
                    {
                        "Relé dá clique mas não buzina",
                        "Relé não dá nenhum clique",
                        "Luz do airbag está acesa no painel",
                        "Buzina toca direto na bateria",
                        "Fusível da buzina está bom"
                    }
                },
                new DiagnosticEntry
                {
                    Code = "LIMPADOR_PARABRISA",
                    Title = "Diagnóstico Interativo: Limpador de Para-Brisa Não Funciona ou Não Volta ao Repouso",
                    System = "Visibilidade & Conforto de Carroceria",
                    Symptoms = "Palhetas do limpador travadas, funcionam apenas na velocidade rápida, ou param no meio do vidro quando você desliga a alavanca.",
                    ProbableCauses = new List<string>
                    {
                        "Contato de retorno automático (linha 31b/53e) quebrado ou oxidado na engrenagem do motor",
                        "Fusível de alimentação do limpador queimado",
                        "Varão articulador mecânico (churrasqueira) engripado por ferrugem e sujeira",
                        "Relé temporizador do limpador com defeito interno ou solda fria",
                        "Escovas do motor do limpador desgastadas na velocidade baixa"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. PARANDO NO MEIO DO VIDRO (Não volta ao repouso): O defeito é 100% no contato interno de retorno automático (pino 31b) da engrenagem do motor do limpador, ou no relé temporizador que não recebe o sinal de parada.",
                        "2. TESTE MECÂNICO DO VARÃO: Com a ignição desligada, tente mover os braços do limpador suavemente. Se estiver duro, solte a churrasqueira plástica e lubrifique os pivôs mecânicos dos braços.",
                        "3. TESTE DE ALIMENTAÇÃO NO PLUGUE DO MOTOR: Conector de 5 vias: Meça linha 15 (+12V pós-chave constante para retorno), Linha 53 (+12V velocidade 1), Linha 53b (+12V velocidade 2) e Linha 31 (Terra de carcaça).",
                        "4. ALIMENTAÇÃO DIRETA NO MOTOR: Injete 12V nos pinos 53 e 53b com terra no 31: se o motor girar forte e silencioso, o defeito é na alavanca da coluna ou relé temporizador."
                    },
                    SuggestedTools = new List<string> { "Multímetro Digital", "Lâmpada de Teste 12V", "Chave Saca-Braço de Limpador" },
                    SuggestedStockParts = new List<string> { "Motor do Limpador de Para-brisa 12V", "Relé Temporizador do Limpador", "Fusível Lâmina 20A", "Mecanismo Varão do Limpador" },
                    ReferenceStandard = "Tensão no Motor: > 12.0V DC | Corrente em Vazio: 2.0A a 3.5A | Corrente sob Chuva: 5A a 8A",
                    ClarifyingQuestions = new List<string>
                    {
                        "Qual o modelo do veículo?",
                        "As palhetas param no meio do vidro ao desligar ou o motor não dá nenhum sinal de vida?",
                        "A função intermitente (temporizada) funciona?"
                    },
                    InteractiveReplyChips = new List<string>
                    {
                        "Para no meio do vidro ao desligar",
                        "Totalmente parado sem barulho",
                        "Funciona só na velocidade máxima",
                        "Varão mecânico está travado duro",
                        "Já testei o fusível do limpador"
                    }
                },
                new DiagnosticEntry
                {
                    Code = "ESGUICHO_LAVADOR_PARABRISA",
                    Title = "Diagnóstico Interativo: Esguicho / Lavador do Para-Brisa Não Funciona (Bombinha d'Água / Brucutu)",
                    System = "Visibilidade & Carroceria Elétrica",
                    Symptoms = "Ao puxar a alavanca do limpador na coluna de direção, não sai água no para-brisa, não se ouve o zumbido elétrico da bombinha ou não chega sinal de 12V no conector do reservatório.",
                    ProbableCauses = new List<string>
                    {
                        "DIFERENÇA CRÍTICA DE ARQUITETURA: Em carros antigos (ex: Gol G2/G3/G4, Uno Mille, Corsa B, Santana, Fusca), a alavanca da coluna aciona a bombinha DIRETO sem relé. Se não vai sinal ao puxar, a causa crônica nº 1 é o contato de cobre interno da chave de seta aberto/gasto ou fusível queimado.",
                        "Em carros intermediários (ex: Astra, Golf, Santana, Gol G5), há relé temporizador conjugado com as palhetas do limpador.",
                        "Em carros modernos (ex: Onix, HB20, Renegade, Compass, Polo TSI), a alavanca envia sinal de rede para o módulo BCM (computador de bordo/carroceria), que comuta relé de reversão para bomba bidirecional.",
                        "Eletrobomba do lavador (bombinha 12V) queimada ou travada por acúmulo de lodo e sujeira no fundo do reservatório",
                        "Fusível de alimentação do circuito do lavador/limpador queimado na caixa de fusíveis",
                        "Bicos ejetores (brucutu) no capô entupidos por cera/calcário ou mangueira de silicone prensada/rompida na dobradiça do capô",
                        "Zinabre ou oxidação com mau contato no plugue de 2 vias da bombinha (sujeito à água de chuva e sujeira da roda)"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. IDENTIFICAÇÃO MANDATÓRIA DO VEÍCULO: O primeiro passo mandatório na oficina é confirmar a MARCA, MODELO e ANO: carros antigos NÃO USAM RELÉ de esguicho (ligação direta linha 15 pela chave de seta); carros modernos utilizam módulo BCM com reversão de polaridade.",
                        "2. TESTE COM LÂMPADA DE TESTE NO PLUGUE DA BOMBA: Desconecte o plugue de 2 vias da bombinha no reservatório. Conecte a lâmpada de teste 12V e peça para puxar a alavanca: Se a lâmpada ACENDER FORTE, a fiação e a alavanca estão perfeitas — a BOMBINHA ESTÁ QUEIMADA OU TRAVADA! (Bata de leve ou substitua a bombinha).",
                        "3. SE NÃO CHEGA SINAL DE 12V AO PUXAR A ALAVANCA: \n   • Em carros antigos (Gol G2/G3/G4, Uno, Corsa, etc.): Teste o fusível. Se estiver bom, o defeito é 90% na lâmina interna de cobre da chave de seta na coluna de direção (solte as capas da coluna e teste continuidade dos contatos com multímetro ao puxar a alavanca).\n   • Em carros modernos: Conecte o scanner na BCM para verificar se o comando da alavanca entra na rede.",
                        "4. SE A BOMBINHA GIRA COM ZUMBIDO MAS NÃO SAI ÁGUA: A parte elétrica está 100% OK! Solte a mangueira plástica na saída da bombinha: se jorrar água, o defeito é mangueira prensada na dobradiça do capô ou os brucutus (bicos ejetores) entupidos por sujeira/cera. Desentupa os furos com agulha fina."
                    },
                    SuggestedTools = new List<string> { "Lâmpada de Teste 12V", "Multímetro Digital", "Agulha Fina para Desentupir Brucutu", "Chave Philips para Capa da Coluna", "Scanner Automotivo (em carros com BCM)" },
                    SuggestedStockParts = new List<string> { "Bomba do Lavador do Para-brisa 12V (Universal / Específica)", "Chave de Seta da Coluna de Direção", "Fusível Lâmina 10A / 15A", "Bico Ejetor Brucutu", "Mangueira Silicone para Esguicho" },
                    ReferenceStandard = "Tensão no Plugue ao Acionar: > 12.0V DC | Corrente da Eletrobomba: 1.5A a 2.5A | Resistência interna da bombinha: 4Ω a 8Ω",
                    ClarifyingQuestions = new List<string>
                    {
                        "👉 Qual é a marca, modelo e ano do veículo? (Ex: Gol G4, Palio Fire, Corsa, Onix, HB20?)",
                        "👉 Quando você puxa a alavanca, você escuta o zumbido elétrico da bombinha funcionando no reservatório?",
                        "👉 As palhetas do limpador chegam a varrer o vidro quando você puxa a alavanca do esguicho?"
                    },
                    InteractiveReplyChips = new List<string>
                    {
                        "É um Gol / Parati / Saveiro G4",
                        "É um Palio / Uno / Strada Fire",
                        "É um Corsa / Celta / Classic GM",
                        "É um Onix / Prisma / HB20",
                        "Não chega sinal 12V no plugue",
                        "Bombinha zune mas não sai água",
                        "O fusível já foi testado e está bom"
                    }
                },
                new DiagnosticEntry
                {
                    Code = "DIRECAO_ELETRICA_EPS",
                    Title = "Diagnóstico Interativo: Direção Elétrica Dura / Luz do Volante Acesa (EPS)",
                    System = "Direção Eletroassistida (EPS)",
                    Symptoms = "Volante fica pesado como caminhão antigo sem assistência, luz de anomalia da direção (volante amarelo/vermelho com exclamação) acesa no painel, direção falha em manobras.",
                    ProbableCauses = new List<string>
                    {
                        "Bateria fraca ou alternador sem carregar (o módulo EPS consome até 60A e desativa por subtensão abaixo de 12.0V)",
                        "Maxi-fusível de alta corrente (60A a 80A) queimado na caixa de distribuição sobre a bateria",
                        "Sensor de torque ou sensor de ângulo de direção (SAS) descalibrado após alinhamento ou desligamento de bateria",
                        "Perda de sinal de rotação do motor ou sinal de velocidade (VSS) via rede CAN",
                        "Motor elétrico da coluna de direção com escovas coladas ou módulo EPS avariado"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. REGRA DE OURO DA DIREÇÃO ELÉTRICA: Antes de condenar a coluna, meça a tensão da bateria com o motor ligado! O alternador DEVE carregar entre 13.8V e 14.5V. Se estiver carregando 12.2V, o módulo EPS desliga sozinho por proteção contra subtensão!",
                        "2. MAXI-FUSÍVEL DA BATERIA: Localize a régua de fusíveis de alta amperagem na tampa da bateria e teste o maxi-fusível de 60A/80A com lâmpada de teste.",
                        "3. SCANNER NO MÓDULO EPS: Acesse o sistema de direção elétrica com o scanner e faça a leitura dos códigos de erro da coluna.",
                        "4. CALIBRAÇÃO DE ÂNGULO (RESET DE PONTO ZERO): Posicione as rodas e o volante 100% retos e execute a função especial 'Calibração do Sensor de Ângulo de Direção (SAS)' no scanner."
                    },
                    SuggestedTools = new List<string> { "Scanner Automotivo com Função Especial EPS", "Multímetro Digital True RMS", "Alicate Amperímetro DC" },
                    SuggestedStockParts = new List<string> { "Maxi-Fusível 60A/80A", "Bateria Automotiva 60Ah Alta Performance", "Módulo / Coluna de Direção EPS" },
                    ReferenceStandard = "Tensão Mínima de Funcionamento: 12.8V DC sob carga | Consumo em Manobra Estacionária: até 45A a 60A | Sinal de Ângulo Zero: 0.0° ± 1.5°",
                    ClarifyingQuestions = new List<string>
                    {
                        "Qual é o veículo (ex: Fox, Fit, Hyundai HB20, Corolla, Ford Ka)?",
                        "A luz da bateria também acendeu no painel?",
                        "A direção ficou dura após descarregar a bateria ou fazer alinhamento de suspensão?"
                    },
                    InteractiveReplyChips = new List<string>
                    {
                        "Alternador está carregando normal",
                        "Bateria descarregou antes da falha",
                        "Maxi-fusível de 60A está bom",
                        "Vou fazer calibração no scanner",
                        "Luz da bateria também acendeu"
                    }
                },
                new DiagnosticEntry
                {
                    Code = "TRAVA_ELETRICA_ALARME",
                    Title = "Diagnóstico Interativo: Trava Elétrica Não Funciona ou Alarme Dispara Sozinho",
                    System = "Segurança, Travamento & Alarme",
                    Symptoms = "Portas não travam pelo controle remoto, uma porta específica não sobe/desce o pino, alarme dispara do nada sem motivo, portas destravam sozinhas logo após trancar.",
                    ProbableCauses = new List<string>
                    {
                        "Microswitch da fechadura elétrica do motorista (porta mestra) com contato de fim-de-curso quebrado",
                        "Fios rompidos no chicote de borracha sanfonada da coluna de porta",
                        "Motor da trava elétrica da porta (atuador mabuchi) cansado sem força mecânica",
                        "Interruptor do capô dianteiro oxidado ou frouxo fazendo o alarme disparar falso",
                        "Fusível de trava/alarme rompido na central de conveniência"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. TESTE DA CHAVE NA PORTA MESTRA: Ao girar a chave na fechadura do motorista, todas as outras portas travam? Se travam pela chave mas NÃO pelo controle, o defeito é o receptor do controle ou bateria do telecomando.",
                        "2. SE UMA PORTA NÃO TRAVA: Remova o forro da porta que falhou. Meça se chegam pulsos de 12V nos dois fios do motor ao acionar a trava.",
                        "3. ALARME DISPARANDO SOZINHO: Desconecte o interruptor do capô no cofre do motor. 80% dos falsos disparos ocorrem pelo sensor do capô enferrujado fechando terra com a vibração!",
                        "4. CHICOTE DA COLUNA DA PORTA: Puxe a sanfona de borracha entre a porta e a coluna e confira os fios tracionados."
                    },
                    SuggestedTools = new List<string> { "Caneta de Polaridade Inteligente", "Multímetro Digital", "Espátulas de Desmontagem de Painéis" },
                    SuggestedStockParts = new List<string> { "Motor de Trava Elétrica Universal 2 Fios", "Fechadura Elétrica Completa", "Interruptor de Capô", "Bateria CR2032 do Controle" },
                    ReferenceStandard = "Pulso de Trava/Destrava: Pulso positivo/negativo de 12V com duração de ~0.8 a 1.2 segundos | Inversão total de polaridade.",
                    ClarifyingQuestions = new List<string>
                    {
                        "O problema é em uma porta específica ou em todas?",
                        "Ao travar pela chave na fechadura a trava central responde?",
                        "O alarme é original de fábrica ou instalado pós-venda (ex: Pósitron)?"
                    },
                    InteractiveReplyChips = new List<string>
                    {
                        "Apenas uma porta não trava",
                        "Todas as portas pararam juntas",
                        "Pela chave na porta funciona",
                        "Alarme dispara sozinho de madrugada",
                        "Sensor do capô está com folga"
                    }
                },

                // =========================================================================
                // 2. ENCICLOPÉDIA COMPLETA DE CÓDIGOS DE SCANNER OBD-II (POWERTRAIN - P)
                // =========================================================================
                new DiagnosticEntry
                {
                    Code = "P0100",
                    Title = "Circuito do Sensor de Fluxo de Massa de Ar (MAF)",
                    System = "Admissão & Injeção Eletrônica",
                    Symptoms = "Motor oscilando em marcha lenta, fumaça preta no escape, perda de potência em retomadas, luz de injeção acesa.",
                    ProbableCauses = new List<string> { "Fio quente do sensor MAF contaminado por poeira ou vapor de óleo", "Falta de alimentação 12V ou 5V de referência", "Chicote rompido ou conector frouxo", "Sensor MAF defeituoso" },
                    GuidedSteps = new List<string>
                    {
                        "1. Medir alimentação positiva (12V ou 5V) e aterramento no conector do MAF.",
                        "2. Em marcha lenta, medir sinal de saída em frequência (Hz) ou tensão (V). Geralmente ~1.0V a 1.4V em lenta, subindo para > 3.5V em aceleração plena.",
                        "3. Inspecionar visualmente o filamento interno do sensor. Se houver sujeira, aplicar spray limpa-contato específico para MAF."
                    },
                    SuggestedTools = new List<string> { "Multímetro Digital", "Osciloscópio Automotivo", "Scanner OBD-II" },
                    SuggestedStockParts = new List<string> { "Sensor MAF", "Spray Limpa Contatos para MAF", "Filtro de Ar do Motor" },
                    ReferenceStandard = "Tensão em Marcha Lenta: 1.0V a 1.4V DC | Aceleração 3000 RPM: 2.2V a 2.8V DC",
                    ClarifyingQuestions = new List<string> { "O sensor já foi limpo recentemente?", "O filtro de ar do motor está em bom estado?" },
                    InteractiveReplyChips = new List<string> { "Filtro de ar está novo", "Tensão em lenta deu 1.2V", "Falta alimentação no conector" }
                },
                new DiagnosticEntry
                {
                    Code = "P0102",
                    Title = "Sensor MAF - Circuito com Entrada Baixa de Tensão",
                    System = "Admissão & Injeção Eletrônica",
                    Symptoms = "Falta de rendimento do motor, engasgos em aceleração rápida, marcha lenta irregular.",
                    ProbableCauses = new List<string> { "Curto-circuito do fio de sinal para a massa", "Falta de terra de referência da central", "Sensor MAF com circuito interno aberto", "Entrada falsa de ar entre o sensor MAF e o corpo de borboleta" },
                    GuidedSteps = new List<string>
                    {
                        "1. Verificar se a mangueira de admissão de ar não está rasgada ou solta após o sensor MAF.",
                        "2. Medir continuidade do fio de sinal do MAF até o conector da ECU, verificando se há curto para o terra.",
                        "3. Com scanner, monitorar fluxo de ar em g/s: valor típico em lenta varia de 2.0 g/s a 4.5 g/s conforme cilindrada."
                    },
                    SuggestedTools = new List<string> { "Scanner Automotivo", "Multímetro Digital" },
                    SuggestedStockParts = new List<string> { "Sensor MAF", "Mangueira de Admissão de Ar" },
                    ReferenceStandard = "Fluxo de Ar Típico: 2.0 a 4.0 g/s em marcha lenta a quente.",
                    ClarifyingQuestions = new List<string> { "Verificou se há mangueira rasgada na admissão?" },
                    InteractiveReplyChips = new List<string> { "Mangueira sem vazamento", "Sinal travado em 0V" }
                },
                new DiagnosticEntry
                {
                    Code = "P0106",
                    Title = "Sensor de Pressão Absoluta do Coletor (MAP) - Faixa/Rendimento",
                    System = "Injeção Eletrônica & Pressão de Coletor",
                    Symptoms = "Motor falha ao acelerar, cheiro forte de combustível, marcha lenta acelerada (> 1200 RPM) ou instável.",
                    ProbableCauses = new List<string> { "Entrada falsa de ar no coletor de admissão (junta queimada ou mangueira de vácuo solta)", "Sensor MAP com orifício entupido por borra de óleo", "Fiação de sinal com alta resistência", "Sensor MAP defeituoso" },
                    GuidedSteps = new List<string>
                    {
                        "1. Ligar a ignição com motor desligado: o sensor MAP deve marcar a pressão atmosférica local (~980 a 1013 mbar ou ~4.0V a 4.8V).",
                        "2. Ligar o motor em marcha lenta: o vácuo gerado deve fazer a leitura cair para ~300 a 400 mbar (~1.0V a 1.5V).",
                        "3. Se a leitura ficar acima de 500 mbar em lenta, há entrada falsa de ar ou motor fora de sincronismo (ponto da correia dentada atrasado)!"
                    },
                    SuggestedTools = new List<string> { "Bomba de Vácuo Manual (Mityvac)", "Multímetro Digital", "Scanner Automotivo" },
                    SuggestedStockParts = new List<string> { "Sensor MAP", "Junta do Coletor de Admissão", "Mangueiras de Vácuo" },
                    ReferenceStandard = "Motor Desligado: ~1000 mbar (~4.5V) | Marcha Lenta: 300 a 400 mbar (~1.2V DC)",
                    ClarifyingQuestions = new List<string> { "Qual a leitura de mbar exibida no scanner em marcha lenta?" },
                    InteractiveReplyChips = new List<string> { "Marca 350 mbar normal", "Marca acima de 600 mbar", "Conferi o ponto do motor" }
                },
                new DiagnosticEntry
                {
                    Code = "P0115",
                    Title = "Sensor de Temperatura do Líquido de Arrefecimento (ECT)",
                    System = "Gerenciamento Térmico & Injeção Eletrônica",
                    Symptoms = "Dificuldade de partida a frio, eletroventilador ligado direto na velocidade máxima, consumo alto de combustível.",
                    ProbableCauses = new List<string> { "Sensor de temperatura NTC queimado ou com resistência alterada", "Falta de alimentação de referência de 5V da central", "Fio terra do sensor rompido", "Termostato travado aberto mantendo motor frio" },
                    GuidedSteps = new List<string>
                    {
                        "1. Desconectar o plugue do sensor e medir tensão nos 2 pinos com chave ligada: deve existir 5.0V em um pino e terra 0V no outro.",
                        "2. Medir resistência ôhmica do sensor NTC: a frio (~20°C) deve dar ~2500Ω a 3000Ω. A quente (~90°C) deve cair para ~200Ω a 300Ω.",
                        "3. Se a leitura estiver fixa em -40°C no scanner: circuito aberto. Se fixa em +140°C: curto-circuito para o terra."
                    },
                    SuggestedTools = new List<string> { "Multímetro Digital", "Scanner OBD-II", "Termômetro Infravermelho" },
                    SuggestedStockParts = new List<string> { "Sensor de Temperatura ECT", "Conector Chicote Sensor", "Válvula Termostática" },
                    ReferenceStandard = "Resistência a 20°C: ~2500Ω a 3000Ω | Resistência a 90°C: ~200Ω a 300Ω | Alimentação: 5.0V DC",
                    ClarifyingQuestions = new List<string> { "Qual a temperatura indicada no scanner?" },
                    InteractiveReplyChips = new List<string> { "Marca -40 graus", "Marca +140 graus", "Ventoinha ligada direto" }
                },
                new DiagnosticEntry
                {
                    Code = "P0120",
                    Title = "Sensor de Posição da Borboleta / Pedal do Acelerador (TPS Circuito A)",
                    System = "Corpo de Borboleta & Acelerador Eletrônico",
                    Symptoms = "Carro entra em modo de emergência (limp home), não passa de 2000 RPM, pedal não responde, luz EPC acesa no painel.",
                    ProbableCauses = new List<string> { "Pistas resistivas internas do corpo de borboleta desgastadas", "Conector do corpo de borboleta oxidado ou pinos frouxos", "Falta de alimentação de 5V ou terra da ECU", "Pistas do pedal do acelerador com ruído" },
                    GuidedSteps = new List<string>
                    {
                        "1. O corpo eletrônico possui duas pistas redundantes (Pista 1 e Pista 2) com leituras inversas cuja soma sempre dá 5.0V.",
                        "2. Conectar osciloscópio ou multímetro nos pinos de sinal e acionar o pedal lentamente: a subida deve ser linear e sem falhas de contato (quedas a 0V).",
                        "3. Limpar conector com limpa-contato e inspecionar terminais quanto a folga."
                    },
                    SuggestedTools = new List<string> { "Osciloscópio Automotivo", "Multímetro Digital", "Scanner OBD-II" },
                    SuggestedStockParts = new List<string> { "Corpo de Borboleta Eletrônico", "Pedal do Acelerador Eletrônico", "Conector Chicote TBI" },
                    ReferenceStandard = "Pista 1: 0.5V a 4.5V | Pista 2: 4.5V a 0.5V | Soma das Pistas = 5.0V constante.",
                    ClarifyingQuestions = new List<string> { "A luz EPC está acesa no painel?" },
                    InteractiveReplyChips = new List<string> { "Luz EPC acesa", "Pedal não responde", "Soma das pistas dá 5V" }
                },
                new DiagnosticEntry
                {
                    Code = "P0130",
                    Title = "Sensor de Oxigênio (Sonda Lambda Banco 1 Sensor 1) - Circuito",
                    System = "Injeção Eletrônica & Controle de Emissões",
                    Symptoms = "Consumo excessivo de combustível, falhas de aceleração, marcha lenta pesada, cheiro forte de gasolina crua.",
                    ProbableCauses = new List<string> { "Sonda lambda envenenada por combustível adulterado ou óleo do motor", "Resistência de aquecimento queimada", "Chicote encostando no escapamento quente e derretido", "Falta de sinal oscilante de 0.1V a 0.9V" },
                    GuidedSteps = new List<string>
                    {
                        "1. Com motor quente e em ciclo fechado (closed loop), monitorar o sinal no osciloscópio ou scanner.",
                        "2. Padrão obrigatório da sonda planar/zircônia: oscilar continuamente entre mistura pobre (~0.1V a 0.2V) e mistura rica (~0.8V a 0.9V) pelo menos 2 a 3 vezes por segundo.",
                        "3. Se o sinal estiver travado em ~0.45V (tensão de referência da ECU), a sonda está aberta ou sem aquecimento."
                    },
                    SuggestedTools = new List<string> { "Osciloscópio Automotivo", "Scanner OBD-II", "Chave Soquete Especial de Sonda Lambda" },
                    SuggestedStockParts = new List<string> { "Sonda Lambda Pré-Catalisador 4 Fios", "Chicote da Sonda" },
                    ReferenceStandard = "Oscilação em Lenta: 0.1V a 0.9V DC com frequência de 1Hz a 3Hz após motor aquecido.",
                    ClarifyingQuestions = new List<string> { "A sonda está oscilando ou travada em uma voltagem fixa?" },
                    InteractiveReplyChips = new List<string> { "Sinal travado em 0.45V", "Sinal oscila normal", "Resistência do aquecedor aberta" }
                },
                new DiagnosticEntry
                {
                    Code = "P0135",
                    Title = "Aquecedor da Sonda Lambda (Banco 1 Sensor 1)",
                    System = "Injeção Eletrônica & Emissões",
                    Symptoms = "Luz de injeção acende logo na primeira partida da manhã, demora para o carro responder bem a frio.",
                    ProbableCauses = new List<string> { "Resistor de aquecimento interno da sonda queimado/rompido", "Fusível do circuito de aquecimento da sonda rompido", "Relé principal da injeção com contato deficiente", "Chicote rompido" },
                    GuidedSteps = new List<string>
                    {
                        "1. Desconectar o conector da sonda de 4 fios (geralmente 2 fios brancos = aquecedor, 1 cinza = terra, 1 preto = sinal).",
                        "2. Medir a resistência ôhmica entre os 2 fios do aquecedor: deve marcar entre 4.0Ω e 15.0Ω a 20°C. Se der circuito aberto (infinito), a sonda está queimada.",
                        "3. No conector do chicote do carro, verificar se chegam 12V e aterramento pulsado por PWM nos terminais do aquecedor com a chave ligada."
                    },
                    SuggestedTools = new List<string> { "Multímetro Digital True RMS", "Scanner OBD-II" },
                    SuggestedStockParts = new List<string> { "Sonda Lambda 4 Fios", "Fusível 15A da Sonda" },
                    ReferenceStandard = "Resistência do Aquecedor: 4Ω a 15Ω nominais | Tensão de Alimentação: 12V linha 15/87",
                    ClarifyingQuestions = new List<string> { "Qual a resistência medida nos fios brancos do aquecedor?" },
                    InteractiveReplyChips = new List<string> { "Resistência deu infinita", "Resistência deu 9 Ohms", "Não tem 12V no chicote" }
                },
                new DiagnosticEntry
                {
                    Code = "P0171",
                    Title = "Sistema de Combustível Muito Pobre (System Too Lean - Banco 1)",
                    System = "Alimentação de Ar & Combustível",
                    Symptoms = "Estalos na admissão ao acelerar, perda de força, falha de ignição, motor esquenta mais que o normal.",
                    ProbableCauses = new List<string> { "Entrada falsa de ar após o MAF/TBI (mangueira do servo-freio/hidrovácuo, cânister ou junta do coletor)", "Bomba de combustível com baixa pressão/vazão (refil cansado ou filtro entupido)", "Bicos injetores entupidos", "Sensor MAF marcando menos ar do que realmente entra" },
                    GuidedSteps = new List<string>
                    {
                        "1. Monitorar o Ajuste de Combustível de Curto e Longo Prazo (STFT e LTFT) no scanner: se a soma estiver acima de +20%, a central está injetando o máximo que pode para compensar falta de combustível ou excesso de ar.",
                        "2. Testar pressão e vazão da linha de combustível com manômetro: linha de injeção multiponto requer 3.0 a 4.2 bar constantes.",
                        "3. Aplicar máquina de fumaça (smoke tester) na admissão para encontrar trincas em mangueiras e juntas."
                    },
                    SuggestedTools = new List<string> { "Manômetro de Pressão de Combustível", "Máquina Geradora de Fumaça", "Scanner OBD-II" },
                    SuggestedStockParts = new List<string> { "Bomba de Combustível Refil", "Filtro de Combustível", "Mangueiras de Vácuo", "Kit de Reparo dos Bicos" },
                    ReferenceStandard = "Ajuste de Combustível (LTFT + STFT): entre -10% e +10% | Pressão da Bomba: 3.0 a 4.2 bar",
                    ClarifyingQuestions = new List<string> { "Qual a pressão de combustível medida com manômetro?" },
                    InteractiveReplyChips = new List<string> { "Pressão da bomba abaixo de 2.5 bar", "Pressão está em 3.8 bar normal", "Ajuste LTFT acima de +25%" }
                },
                new DiagnosticEntry
                {
                    Code = "P0172",
                    Title = "Sistema de Combustível Muito Rico (System Too Rich - Banco 1)",
                    System = "Alimentação de Combustível",
                    Symptoms = "Cheiro forte de combustível, fumaça preta no escape, velas carbonizadas de preto fosco, óleo do motor com cheiro de combustível.",
                    ProbableCauses = new List<string> { "Bicos injetores travados abertos ou gotejando", "Pressão de combustível muito alta (regulador de pressão travado fechado ou retorno obstruído)", "Válvula de purga do cânister travada aberta puxando vapor direto", "Sensor MAP ou ECT marcando valor incorreto" },
                    GuidedSteps = new List<string>
                    {
                        "1. No scanner, monitorar STFT e LTFT: se estiver em valores negativos extremos (-20% a -25%), a central está cortando tempo de injeção.",
                        "2. Retirar os bicos injetores e testar estanqueidade e vazão na máquina de teste de bicos em bancada.",
                        "3. Desconectar a mangueira do cânister e tampar: se o valor voltar ao normal, a válvula do cânister está travada aberta."
                    },
                    SuggestedTools = new List<string> { "Máquina de Teste e Limpeza de Bicos", "Manômetro de Combustível", "Scanner OBD-II" },
                    SuggestedStockParts = new List<string> { "Bico Injetor", "Regulador de Pressão de Combustível", "Válvula de Purga do Cânister" },
                    ReferenceStandard = "Estanqueidade dos Bicos: ZERO gotas sob 3.5 bar de pressão por 1 minuto.",
                    ClarifyingQuestions = new List<string> { "Os bicos foram testados na máquina de bancada?" },
                    InteractiveReplyChips = new List<string> { "Bico gotejando no teste", "Válvula do cânister travada", "Pressão acima de 5 bar" }
                },
                new DiagnosticEntry
                {
                    Code = "P0300",
                    Title = "Falha de Ignição Aleatória / Múltipla (Random/Multiple Cylinder Misfire)",
                    System = "Ignição & Combustão",
                    Symptoms = "Motor trepida intensamente, perda severa de potência, estalos no escape, luz de injeção piscando em aceleração.",
                    ProbableCauses = new List<string> { "Velas de ignição muito gastas ou carbonizadas", "Cabos de vela com fuga de alta tensão", "Bobinas de ignição defeituosas", "Queda de tensão na alimentação 12V da bobina (relé principal)" },
                    GuidedSteps = new List<string>
                    {
                        "1. Verificar cabos de vela com multímetro: máximo de 5kΩ a 10kΩ por metro. Cabos com mais de 20kΩ devem ser trocados.",
                        "2. Inspecionar velas: folga dos eletrodos deve estar calibrada (geralmente 0.8mm a 1.0mm).",
                        "3. Utilizar osciloscópio com pinça capacitiva/indutiva para verificar tempo de centelha (1.0ms a 1.8ms)."
                    },
                    SuggestedTools = new List<string> { "Osciloscópio com Pinça HT", "Calibre de Folga de Velas", "Multímetro" },
                    SuggestedStockParts = new List<string> { "Jogo de Velas de Ignição", "Cabos de Ignição de Silicone", "Bobina de Ignição" },
                    ReferenceStandard = "Tempo de Centelha: 1.0ms a 1.8ms | Resistência dos cabos: < 10kΩ/m | Gap vela: 0.8mm a 1.0mm",
                    ClarifyingQuestions = new List<string> { "As velas e cabos foram inspecionados?" },
                    InteractiveReplyChips = new List<string> { "Velas com folga aberta", "Cabo com resistência alta", "Bobina trincada com centelha fugindo" }
                },
                new DiagnosticEntry
                {
                    Code = "P0301",
                    Title = "Falha de Ignição no Cilindro 1 (Cylinder 1 Misfire Detected)",
                    System = "Ignição & Injeção no Cilindro 1",
                    Symptoms = "Motor mancando em 3 cilindros, vibração no volante em marcha lenta, cheiro de combustível no escapamento.",
                    ProbableCauses = new List<string> { "Vela ou cabo de ignição do cilindro 1 com fuga", "Bobina individual do cilindro 1 em falha", "Bico injetor 1 entupido ou sem pulso elétrico", "Falta de compressão mecânica no cilindro 1" },
                    GuidedSteps = new List<string>
                    {
                        "1. TESTE DA TROCA CRUZADA: Troque a vela e a bobina do cilindro 1 para o cilindro 2. Apague a memória e ande com o carro. Se o código mudar para P0302, o defeito acompanhou a peça trocada!",
                        "2. Se o defeito continuar fixo no cilindro 1, teste o sinal elétrico do bico 1 com caneta de polaridade e meça a compressão mecânica com manômetro."
                    },
                    SuggestedTools = new List<string> { "Manômetro de Compressão de Cilindros", "Caneta de Polaridade", "Scanner OBD-II" },
                    SuggestedStockParts = new List<string> { "Bobina de Ignição Individual", "Vela de Ignição", "Bico Injetor" },
                    ReferenceStandard = "Compressão de Cilindro: 150 a 200 PSI com variação máxima de 10% entre cilindros.",
                    ClarifyingQuestions = new List<string> { "Já fez o teste da troca cruzada de bobina ou vela?" },
                    InteractiveReplyChips = new List<string> { "Fiz a troca e o erro mudou", "Erro continuou no cilindro 1", "Compressão mecânica está boa" }
                },
                new DiagnosticEntry
                {
                    Code = "P0335",
                    Title = "Sensor de Posição da Árvore de Manivelas (CKP - Circuito A)",
                    System = "Sincronismo & Injeção Eletrônica",
                    Symptoms = "Motor gira arranque com vigor mas não pega de jeito nenhum, corta do nada em trânsito ao esquentar, conta-giros não mexe durante a partida.",
                    ProbableCauses = new List<string> { "Sensor de rotação indutivo ou Hall defeituoso", "Entreferro fora de especificação (distância da ponta do sensor à roda fônica)", "Roda fônica com dentes quebrados ou chaveta da polia com folga", "Chicote blindado partido" },
                    GuidedSteps = new List<string>
                    {
                        "1. Identificar o tipo: 2 pinos = Indutivo; 3 pinos = Hall (5V/12V, terra e sinal).",
                        "2. Se Indutivo: medir resistência ôhmica (geralmente entre 500Ω e 1200Ω). Se der circuito aberto, o sensor queimou.",
                        "3. Se Hall: verificar se gera onda quadrada de 0 a 5V com o motor girando.",
                        "4. Verificar se a ponta magnética do sensor não acumulou limalha de ferro do motor de partida."
                    },
                    SuggestedTools = new List<string> { "Osciloscópio Automotivo", "Multímetro Digital", "Calibre de Lâminas" },
                    SuggestedStockParts = new List<string> { "Sensor de Rotação CKP", "Conector Chicote Sensor", "Roda Fônica" },
                    ReferenceStandard = "Resistência Indutivo: 500Ω a 1200Ω | Tensão AC de partida: > 1.5V AC | Entreferro: ~1.0mm",
                    ClarifyingQuestions = new List<string> { "O conta-giros do painel se move quando você dá partida?" },
                    InteractiveReplyChips = new List<string> { "Conta-giros não mexe", "Sensor indutivo deu aberto", "Tem limalha na ponta do sensor" }
                },
                new DiagnosticEntry
                {
                    Code = "P0340",
                    Title = "Sensor de Posição do Comando de Válvulas (CMP - Circuito A)",
                    System = "Sincronismo & Comando de Válvulas",
                    Symptoms = "Demora para pegar na partida (precisa girar vários segundos até a ECU adotar modo sequencial duplo), perda de rendimento em alta rotação.",
                    ProbableCauses = new List<string> { "Sensor de fase Hall queimado", "Correia ou corrente dentada fora de sincronismo (ponto pulado)", "Variador de fase de comando (VVT) travado ou com válvula solenoide entupida" },
                    GuidedSteps = new List<string>
                    {
                        "1. Testar alimentação 12V ou 5V no conector do sensor CMP.",
                        "2. Conectar osciloscópio de 2 canais: Canal 1 no sensor de rotação CKP e Canal 2 no sensor de fase CMP.",
                        "3. Conferir se o dente de referência do comando cai exatamente no dente correto da árvore de manivelas (sincronismo virtual de fábrica)."
                    },
                    SuggestedTools = new List<string> { "Osciloscópio Automotivo 2 Canais", "Scanner OBD-II" },
                    SuggestedStockParts = new List<string> { "Sensor de Fase CMP", "Solenoide do Comando VVT", "Kit de Correia Dentada" },
                    ReferenceStandard = "Sincronismo Virtual: Casamento de borda de subida do CMP com dente de referência do CKP.",
                    ClarifyingQuestions = new List<string> { "Foi trocada correia dentada recentemente?" },
                    InteractiveReplyChips = new List<string> { "Correia dentada trocada recente", "Sensor Hall não emite pulso", "Sincronismo no osciloscópio está fora" }
                },
                new DiagnosticEntry
                {
                    Code = "P0420",
                    Title = "Eficiência do Catalisador Abaixo do Limite (Banco 1)",
                    System = "Sistema de Escape & Emissões",
                    Symptoms = "Luz de injeção acesa constante, cheiro de ovo podre no escapamento, perda discreta de potência em subidas.",
                    ProbableCauses = new List<string> { "Catalisador com miolo de cerâmica derretido, quebrado ou esgotado", "Sonda lambda pós-catalisador defeituosa ou com resposta lenta", "Vazamento de escape entre a sonda 1 e a sonda 2" },
                    GuidedSteps = new List<string>
                    {
                        "1. Com motor quente a 2500 RPM, monitorar o sinal da Sonda 1 (pré) e Sonda 2 (pós).",
                        "2. Se o catalisador estiver bom, a Sonda 1 oscila rápido (0.1V a 0.9V) e a Sonda 2 deve ficar praticamente ESTÁVEL em linha reta (~0.5V a 0.7V).",
                        "3. Se a Sonda 2 estiver copiando as oscilações da Sonda 1, a cerâmica do catalisador perdeu a capacidade de armazenamento de oxigênio!"
                    },
                    SuggestedTools = new List<string> { "Scanner Automotivo", "Osciloscópio Automotivo" },
                    SuggestedStockParts = new List<string> { "Catalisador Homologado", "Sonda Lambda Pós-Catalisador" },
                    ReferenceStandard = "Sinal Sonda 2: Estável entre 0.55V e 0.75V DC em regime constante de 2500 RPM.",
                    ClarifyingQuestions = new List<string> { "A sonda pós-catalisador está oscilando igual à primeira?" },
                    InteractiveReplyChips = new List<string> { "Sonda 2 está oscilando igual à 1", "Sonda 2 está estável", "Escape tem vazamento antes da sonda" }
                },
                new DiagnosticEntry
                {
                    Code = "P0500",
                    Title = "Sensor de Velocidade do Veículo (VSS) - Mal Funcionamento",
                    System = "Velocímetro & Módulos de Tração",
                    Symptoms = "Velocímetro do painel não funciona ou oscila, direção elétrica fica muito leve ou dura em velocidade, motor morre ao parar no semáforo.",
                    ProbableCauses = new List<string> { "Sensor de velocidade no câmbio quebrado ou engrenagem plástica roçada", "Em veículos sem VSS, sinal de velocidade que vem da central do ABS interrompido", "Chicote rompido próximo ao câmbio" },
                    GuidedSteps = new List<string>
                    {
                        "1. Verificar se o sinal de velocidade vem de sensor no câmbio ou dos sensores de roda do ABS via rede CAN.",
                        "2. Se for sensor no câmbio: alimentar 12V e girar a roda no elevador com multímetro na saída de sinal (deve gerar pulsos de 0 a 12V).",
                        "3. Remover o sensor e checar o pinhão de engrenamento plástico se não está desgastado."
                    },
                    SuggestedTools = new List<string> { "Multímetro Digital", "Scanner Automotivo" },
                    SuggestedStockParts = new List<string> { "Sensor de Velocidade VSS", "Engrenagem do Pinhão do Câmbio" },
                    ReferenceStandard = "Pulsos de Velocidade: Sinal quadrado de 0V a 12V proporcional à rotação das rodas.",
                    ClarifyingQuestions = new List<string> { "O velocímetro no painel mexe ou fica totalmente parado?" },
                    InteractiveReplyChips = new List<string> { "Velocímetro parado no zero", "Pinhão plástico do câmbio roçado", "Sinal vem da central do ABS" }
                },
                new DiagnosticEntry
                {
                    Code = "P0562",
                    Title = "Tensão do Sistema Elétrico Baixa (System Voltage Low)",
                    System = "Carga & Alimentação (Alternador / Bateria)",
                    Symptoms = "Luz de bateria acesa no painel, direção elétrica dura, falhas de ignição, faróis fracos, rádio reiniciando.",
                    ProbableCauses = new List<string>
                    {
                        "Alternador com regulador de voltagem em falha ou escovas gastas",
                        "Queda de tensão excessiva no cabo positivo (B+ do alternador à bateria)",
                        "Queda de tensão no cabo massa / aterramento entre bloco e carroceria",
                        "Correia do alternador frouxa, patinando ou tensionador travado",
                        "Bateria com curto interno de placa ou sulfatada"
                    },
                    GuidedSteps = new List<string>
                    {
                        "Medir tensão em repouso da bateria: Mínimo 12.4V (abaixo de 12.0V recarregar antes de testar).",
                        "Partir o motor e ligar farol alto + ventoinha interna em carga máxima a 2000 RPM. Medir tensão nos polos da bateria: deve permanecer entre 13.8V e 14.5V.",
                        "Teste de Queda de Tensão Linha Positiva: Ponta vermelha no B+ do alternador e ponta preta no borne positivo da bateria. Valor máximo tolerado: 0.20V DC.",
                        "Teste de Queda de Tensão Linha Negativa (Massa): Ponta vermelha na carcaça do alternador e ponta preta no borne negativo da bateria. Valor máximo tolerado: 0.10V DC.",
                        "Teste de Ripple / Ondulação AC: Multímetro em VAC nos polos da bateria. Máximo tolerado: 0.05V AC (50mV). Se superior, diodo retificador está aberto."
                    },
                    SuggestedTools = new List<string> { "Multímetro Digital True RMS", "Alicate Amperímetro DC", "Testador de Condutância de Bateria" },
                    SuggestedStockParts = new List<string> { "Regulador de Voltagem", "Cabo de Bateria Reforçado", "Bateria Automotiva 60Ah", "Placa de Diodos" },
                    ReferenceStandard = "Tensão de Carga: 13.8V - 14.5V DC | Queda Positiva: < 0.2V | Queda Massa: < 0.1V | Ripple AC: < 0.05V AC",
                    ClarifyingQuestions = new List<string> { "Qual a voltagem medida com motor funcionando a 2000 RPM?" },
                    InteractiveReplyChips = new List<string> { "Tensão em 12.2V ligado", "Tensão normal em 14.1V", "Queda positiva acima de 0.5V" }
                },
                new DiagnosticEntry
                {
                    Code = "P0563",
                    Title = "Tensão do Sistema Elétrico Alta / Sobretensão (System Voltage High)",
                    System = "Carga & Alimentação",
                    Symptoms = "Lâmpadas queimando constantemente, cheiro forte de ácido/enxofre na bateria (ebulição), erros de comunicação nos módulos (U-codes), odômetro piscando.",
                    ProbableCauses = new List<string>
                    {
                        "Regulador de voltagem em curto direto saturado em condução total",
                        "Perda de sinal de referência de sensoriamento do alternador (terminal S desconectado)",
                        "Bateria aberta internamente sem capacidade de amortecimento de picos",
                        "Aterramento deficiente do módulo de injeção ou carcaça do alternador"
                    },
                    GuidedSteps = new List<string>
                    {
                        "ATENÇÃO: Risco de queima de módulos eletrônicos! Não manter o motor ligado acima de 15.5V.",
                        "Medir tensão nos bornes da bateria com motor ligado a 2500 RPM. Se ultrapassar 14.8V, o regulador não está ceifando a excitação.",
                        "Desconectar o chicote de sinal/sensoriamento do alternador e conferir continuidade do pino de referência.",
                        "Substituir o regulador de tensão ou a unidade do alternador antes de resetar a memória de erros."
                    },
                    SuggestedTools = new List<string> { "Multímetro Digital True RMS", "Scanner Automotivo OBD-II" },
                    SuggestedStockParts = new List<string> { "Regulador de Voltagem", "Alternador Completo", "Bateria Automotiva" },
                    ReferenceStandard = "Tensão Máxima Admissível: 14.8V DC sob rotação máxima.",
                    ClarifyingQuestions = new List<string> { "A voltagem está passando de 15 Volts com o carro ligado?" },
                    InteractiveReplyChips = new List<string> { "Passou de 16 Volts", "Bateria fervendo com cheiro", "Troquei o regulador" }
                },
                new DiagnosticEntry
                {
                    Code = "P0620",
                    Title = "Circuito de Controle do Alternador / Excitação (Generator Control Circuit)",
                    System = "Rede & Alternador Pilotado",
                    Symptoms = "Luz de bateria acesa esporadicamente, alternador para de carregar em aceleração forte, falha no sinal PWM da central (LIN / BSS / SIG).",
                    ProbableCauses = new List<string>
                    {
                        "Alternador pilotado por PWM sem comunicação com a central de injeção (ECU)",
                        "Chicote do conector do alternador partido por vibração próximo ao bloco",
                        "Regulador de voltagem não compatível com o protocolo da ECU (ex: Ford SIG vs GM RVC vs Lin-Bus)",
                        "Fusível de alimentação do circuito de ignição do alternador rompido"
                    },
                    GuidedSteps = new List<string>
                    {
                        "Verificar se o alternador é convencional ou pilotado por PWM/LIN.",
                        "Conectar osciloscópio no pino de comando PWM (SIG/LIN) entre ECU e alternador: deve existir trem de pulsos com frequência e ciclo de trabalho variáveis com a carga elétrica.",
                        "Inspecionar conector do alternador quanto a pinos recuados ou oxidados.",
                        "Verificar fusível de linha 15 (pós-chave) que alimenta a excitação inicial."
                    },
                    SuggestedTools = new List<string> { "Osciloscópio Automotivo 2 Canais", "Caneta de Polaridade Inteligente", "Scanner" },
                    SuggestedStockParts = new List<string> { "Regulador Pilotado PWM/LIN", "Conector Chicote Alternador", "Fusível Lâmina 10A" },
                    ReferenceStandard = "Sinal PWM: 12V pico a pico, frequência típica 125Hz - 250Hz conforme montadora.",
                    ClarifyingQuestions = new List<string> { "O alternador instalado é original ou foi trocado recentemente?" },
                    InteractiveReplyChips = new List<string> { "Alternador trocado recentemente", "Sinal PWM presente no osciloscópio", "Fio de sinal quebrado no conector" }
                },

                // =========================================================================
                // 3. CÓDIGOS DE REDE E COMUNICAÇÃO (CAN BUS - U)
                // =========================================================================
                new DiagnosticEntry
                {
                    Code = "U0100",
                    Title = "Perda de Comunicação com a Unidade de Controle do Motor (ECM / ECU)",
                    System = "Rede de Dados CAN Bus",
                    Symptoms = "Painel de instrumentos com luzes de aviso piscando, conta-giros inoperante, motor não dá partida, scanner não acessa motor.",
                    ProbableCauses = new List<string> { "Falta de alimentação 12V constante (linha 30) ou pós-chave (linha 15) na ECU", "Relé principal da injeção eletrônica com bobina queimada", "Linha de aterramento principal da ECU com resistência", "Fios CAN High e CAN Low rompidos ou em curto" },
                    GuidedSteps = new List<string>
                    {
                        "1. Medir resistência nos pinos 6 e 14 do conector OBD-II com chave desligada: deve marcar exatos 60 Ohms.",
                        "2. Verificar os fusíveis de alimentação da ECU e o relé principal de ignição.",
                        "3. Medir tensões de barramento CAN com chave ligada: Pino 6 (~2.7V) e Pino 14 (~2.3V)."
                    },
                    SuggestedTools = new List<string> { "Multímetro Digital True RMS", "Osciloscópio Automotivo", "Caixa de Breakout OBD-II" },
                    SuggestedStockParts = new List<string> { "Relé Principal da Injeção", "Fusíveis Lâmina", "Conector da ECU" },
                    ReferenceStandard = "Resistência de Barramento: 60Ω nominais (55Ω a 65Ω) entre pinos 6 e 14 do OBD-II.",
                    ClarifyingQuestions = new List<string> { "Quantos Ohms o multímetro marca entre os pinos 6 e 14 do OBD?" },
                    InteractiveReplyChips = new List<string> { "Marcou 60 Ohms perfeito", "Marcou 120 Ohms", "Relé principal não atraca", "Scanner não comunica com a injeção" }
                },
                new DiagnosticEntry
                {
                    Code = "U0121",
                    Title = "Perda de Comunicação com o Módulo de Freio Antitravamento (ABS)",
                    System = "Rede CAN & Módulo ABS",
                    Symptoms = "Luz do ABS e do controle de tração acesas no painel, velocímetro parado, direção elétrica dura.",
                    ProbableCauses = new List<string> { "Fusível Maxi de alimentação de potência da bomba do ABS queimado", "Conector de 38 ou 42 pinos do bloco do ABS com infiltração de água e oxidação", "Aterramento principal do módulo ABS frouxo na lataria" },
                    GuidedSteps = new List<string>
                    {
                        "1. Localizar a caixa de fusíveis do motor e testar os 2 fusíveis de alta corrente do ABS (geralmente 30A e 40A/50A).",
                        "2. Desconectar o conector principal do ABS e verificar se há pinos verdes de azinhavre ou água.",
                        "3. Medir continuidade das linhas de CAN do módulo ABS até o barramento central."
                    },
                    SuggestedTools = new List<string> { "Multímetro Digital", "Scanner OBD-II" },
                    SuggestedStockParts = new List<string> { "Maxi Fusível 40A", "Módulo Eletrônico ABS" },
                    ReferenceStandard = "Alimentação do ABS: 12V em duas linhas de potência separadas | Queda no Terra: < 0.1V",
                    ClarifyingQuestions = new List<string> { "Verificou os fusíveis de alta amperagem do ABS no cofre?" },
                    InteractiveReplyChips = new List<string> { "Fusível de 40A estava queimado", "Conector do ABS tem oxidação", "Scanner não acessa o ABS" }
                },

                // =========================================================================
                // 4. CÓDIGOS DE CHASSI (C) E CARROCERIA (B)
                // =========================================================================
                new DiagnosticEntry
                {
                    Code = "C0035",
                    Title = "Sensor de Velocidade de Roda Dianteira Esquerda (ABS)",
                    System = "Sistema de Freios ABS",
                    Symptoms = "Luz do ABS acende logo que o carro passa de 20 km/h, pedal trepida em frenagem leve no asfalto seco.",
                    ProbableCauses = new List<string> { "Sensor de roda sujo com limalha de pastilha", "Chicote do sensor rompido na suspensão pelo movimento da direção", "Rolamento de roda montado ao contrário (sem anel magnético encoder voltado para o sensor)" },
                    GuidedSteps = new List<string>
                    {
                        "1. Levantar o lado esquerdo e monitorar sinal de velocidade da roda via scanner gráfico girando a roda com a mão.",
                        "2. Se for sensor ativo de 2 fios: verificar alimentação de 12V e sinal de corrente em onda quadrada (7mA a 14mA).",
                        "3. Inspecionar visualmente o cabo próximo ao amortecedor: é onde ele quebra internamente por torção."
                    },
                    SuggestedTools = new List<string> { "Osciloscópio Automotivo", "Scanner OBD-II", "Detector de Anel Magnético" },
                    SuggestedStockParts = new List<string> { "Sensor de Roda ABS Dianteiro Esquerdo", "Rolamento de Roda com Anel Magnético" },
                    ReferenceStandard = "Sinal Hall Ativo: Pulsos de corrente com dentes simétricos em giro contínuo.",
                    ClarifyingQuestions = new List<string> { "O rolamento da roda dianteira foi trocado recentemente?" },
                    InteractiveReplyChips = new List<string> { "Rolamento trocado recente", "Fio quebrado no amortecedor", "Sensor sujo de limalha" }
                },
                new DiagnosticEntry
                {
                    Code = "B1000",
                    Title = "Falha do Módulo Eletrônico de Carroceria (BCM / Central de Conforto)",
                    System = "Carroceria & Rede de Conforto",
                    Symptoms = "Travas elétricas travam sozinhas, luz de teto não apaga, limpador de para-brisa liga sozinho, vidros inoperantes.",
                    ProbableCauses = new List<string> { "Infiltração de água pela churrasqueira ou para-brisa atingindo a BCM", "Queda severa de tensão no aterramento da BCM", "Transistor interno de acionamento em curto" },
                    GuidedSteps = new List<string>
                    {
                        "1. Localizar a central BCM (geralmente abaixo da coluna de direção ou atrás do porta-luvas) e checar se há umidade ou cheiro de queimado.",
                        "2. Medir linhas de alimentação permanente 12V e linhas de massa.",
                        "3. Realizar ciclo de reset de bateria (desconectar bornes por 20 minutos com chave desligada)."
                    },
                    SuggestedTools = new List<string> { "Multímetro Digital", "Scanner Automotivo" },
                    SuggestedStockParts = new List<string> { "Módulo BCM", "Chicote de Carroceria" },
                    ReferenceStandard = "Alimentação permanente: > 12.4V constante sem ondulações.",
                    ClarifyingQuestions = new List<string> { "Houve infiltração de água após chuva forte ou lavagem?" },
                    InteractiveReplyChips = new List<string> { "Teve água perto da BCM", "Reset de bateria não resolveu", "Fusíveis da BCM estão bons" }
                },

                new DiagnosticEntry
                {
                    Code = "B0001",
                    Title = "Circuito de Disparo do Airbag Dianteiro do Motorista (Fase 1)",
                    System = "Segurança Suplementar & Airbag (SRS)",
                    Symptoms = "Luz de advertência do Airbag acesa constante no painel, buzina para de funcionar ou comandos de som no volante param.",
                    ProbableCauses = new List<string> { "Cinta do Airbag (Clock Spring / Hard Disc) partida por torção na coluna de direção", "Conector amarelo do airbag desencaixado sob o volante", "Espoleta do módulo do volante aberta" },
                    GuidedSteps = new List<string>
                    {
                        "1. ATENÇÃO DE SEGURANÇA: Desconecte a bateria e aguarde 10 minutos antes de manusear conectores amarelos do Airbag!",
                        "2. NUNCA aplique multímetro direto na bolsa do airbag (a corrente de teste do multímetro pode detonar a bolsa!).",
                        "3. Desconecte a bolsa do airbag e a cinta na base da coluna. Meça a continuidade da pista da cinta Clock Spring girando o volante de batente a batente: se der circuito aberto em qualquer posição, a cinta partiu!"
                    },
                    SuggestedTools = new List<string> { "Scanner Automotivo Especial SRS/Airbag", "Multímetro Digital (apenas na fiação desconectada)", "Resistor Simulador 2.2Ω" },
                    SuggestedStockParts = new List<string> { "Cinta do Airbag Clock Spring", "Conector Chicote SRS Amarelo" },
                    ReferenceStandard = "Resistência Típica da Bolsa com Simulador: 2.0Ω a 2.5Ω nominais.",
                    ClarifyingQuestions = new List<string> { "A buzina também parou de funcionar junto com a luz do airbag?" },
                    InteractiveReplyChips = new List<string> { "Buzina também parou", "Cinta deu circuito aberto", "Conector amarelo desencaixado" }
                },
                new DiagnosticEntry
                {
                    Code = "C1201",
                    Title = "Mau Funcionamento do Sistema de Controle de Estabilidade / Tração (VSC/ESP)",
                    System = "Segurança Dinâmica, Freios & VSC",
                    Symptoms = "Luzes do controle de estabilidade (carrinho derrapando) e freio acesas, carro perde potência repentinamente ao fazer curvas, ABS desabilitado.",
                    ProbableCauses = new List<string> { "Sensor de taxa de guinada / aceleração lateral (Yaw Rate Sensor) sem calibração", "Sensor de ângulo de direção (SAS) com leitura desalinhada", "Subtensão de bateria afetando o módulo hidráulico" },
                    GuidedSteps = new List<string>
                    {
                        "1. Na maioria dos veículos (ex: Toyota/Honda/VW), o código C1201 é gerado no módulo de freios simplesmente porque a ECU do motor registrou um código de injeção!",
                        "2. Verifique se há DTCs ativos na injeção (motor). Se houver (ex: P0300, P0171), resolva primeiro a injeção: ao apagar o erro do motor, o C1201 apaga automaticamente!",
                        "3. Se não houver erro no motor, faça o aprendizado de ponto zero do sensor de aceleração e SAS via scanner com carro em piso nivelado."
                    },
                    SuggestedTools = new List<string> { "Scanner Automotivo OBD-II", "Multímetro Digital" },
                    SuggestedStockParts = new List<string> { "Módulo VSC/ABS", "Sensor de Ângulo de Direção" },
                    ReferenceStandard = "Sensor Yaw Rate em repouso: 0.0 deg/s | Aceleração Longitudinal/Lateral: 0.00 G.",
                    ClarifyingQuestions = new List<string> { "Há código de falha de injeção gravado no motor também?" },
                    InteractiveReplyChips = new List<string> { "Tem código no motor também", "Ponto zero calibrado no scanner", "Sensor SAS marcando torto" }
                },
                new DiagnosticEntry
                {
                    Code = "P0401",
                    Title = "Fluxo Insuficiente Detectado na Recirculação dos Gases de Escape (EGR)",
                    System = "Escape, Emissões & Válvula EGR",
                    Symptoms = "Motor 'grila' ou bate pino sob carga em subidas, aumento na emissão de NOx, consumo de combustível elevado.",
                    ProbableCauses = new List<string> { "Dutos do coletor de admissão e válvula EGR carbonizados com borra preta de fuligem", "Válvula EGR travada fechada por carvão", "Sensor de pressão diferencial (DPFE) com mangueiras de silicone entupidas ou furadas" },
                    GuidedSteps = new List<string>
                    {
                        "1. Desmontar a válvula EGR e inspecionar a passagem de gases: se estiver obstruída por carvão duro, fazer descarbonização química.",
                        "2. Se a EGR for pneumática: aplicar vácuo com bomba manual (Mityvac) e verificar se a haste sobe e segura vácuo.",
                        "3. Se a EGR for elétrica (motor de passo ou solenoide PWM): testar a resistência da bobina e aplicar sinal de acionamento via atuadores do scanner com motor em lenta (o motor deve quase morrer ao abrir a EGR em lenta)."
                    },
                    SuggestedTools = new List<string> { "Bomba de Vácuo Manual Mityvac", "Spray Descarbonizante de Admissão", "Scanner OBD-II" },
                    SuggestedStockParts = new List<string> { "Válvula EGR Completa", "Junta de Vedação da EGR", "Mangueiras Térmicas de Vácuo" },
                    ReferenceStandard = "Abertura da EGR em Lenta: Queda imediata do vácuo do coletor e motor rateando | Resistência da solenoide: 8Ω a 14Ω.",
                    ClarifyingQuestions = new List<string> { "A válvula EGR foi removida para inspeção de carvão?" },
                    InteractiveReplyChips = new List<string> { "EGR cheia de carvão", "Haste da válvula travada", "Mangueira de vácuo furada" }
                },
                new DiagnosticEntry
                {
                    Code = "P0505",
                    Title = "Mau Funcionamento no Sistema de Controle de Marcha Lenta (IAC)",
                    System = "Admissão & Controle de Rotação de Marcha Lenta",
                    Symptoms = "Motor morre ao parar no semáforo ou desengatar a marcha, marcha lenta oscilando (acelera e desacelera sozinha entre 800 e 1800 RPM), ou acelerada fixa acima de 2000 RPM.",
                    ProbableCauses = new List<string> { "Motor de passo / Atuador de Marcha Lenta (IAC) travado por borra de óleo no alojamento", "Entrada falsa de ar após a borboleta", "Bobinas internas do motor de passo queimadas", "Corpo de borboleta eletrônico sujo precisando de reset de aprendizado" },
                    GuidedSteps = new List<string>
                    {
                        "1. Se o veículo usa motor de passo de 4 pinos: medir resistência entre as duas bobinas (pinos A-B e C-D). Normal: 45Ω a 65Ω em cada bobina. Entre bobinas cruzadas deve dar infinito.",
                        "2. Limpar o corpo de borboleta (TBI) e o orifício de desvio de ar com descarbonizante.",
                        "3. Se for acelerador eletrônico (drive-by-wire): realizar o procedimento de 'Aprendizado e Reset de Parâmetros Autoadaptativos da Borboleta' via scanner."
                    },
                    SuggestedTools = new List<string> { "Spray Limpa TBI / Descarbonizante", "Multímetro Digital", "Scanner Automotivo" },
                    SuggestedStockParts = new List<string> { "Atuador de Marcha Lenta IAC (Motor de Passo)", "Junta do Corpo de Borboleta", "Corpo de Borboleta TBI" },
                    ReferenceStandard = "Resistência das Bobinas do Motor de Passo: 45Ω a 65Ω | Rotação Nominal de Lenta: 750 a 850 RPM a quente.",
                    ClarifyingQuestions = new List<string> { "O carro oscila aceleração ou morre quando solta o pé do acelerador?" },
                    InteractiveReplyChips = new List<string> { "Oscila entre 800 e 1800 RPM", "Morre direto nas paradas", "Resistência do motor de passo deu 50 Ohms", "Fiz limpeza do corpo TBI" }
                },
                new DiagnosticEntry
                {
                    Code = "P0606",
                    Title = "Falha no Processador da Unidade de Controle do Motor (ECU/ECM)",
                    System = "Processamento & Módulos Eletrônicos",
                    Symptoms = "Luz de injeção acesa, carro em modo de emergência severo (limp home), acelerador sem resposta, cortes repentinos de ignição.",
                    ProbableCauses = new List<string> { "Queda brusca de tensão durante a partida por bateria esgotada gerando erro de checksum na memória da ECU", "Pico de sobretensão gerado por alternador desregulado (> 16V)", "Aterramento principal da carcaça da ECU deficiente", "Falha de hardware no microcontrolador interno" },
                    GuidedSteps = new List<string>
                    {
                        "1. PROCEDIMENTO DE HARD RESET: Desconecte os cabos positivo e negativo da bateria, encoste os dois cabos do carro um no outro por 30 segundos (SEM A BATERIA!) para descarregar todos os capacitores da ECU, e religue.",
                        "2. TESTE DE ALIMENTAÇÃO DA ECU: Meça com carga se as linhas 30 (positivo direto), linha 15 (pós-chave) e terras principais da ECU têm zero queda de tensão sob carga.",
                        "3. Se o código persistir após reset e alimentação perfeita, o software da ECU precisa de regravação (telecarregamento) ou reparo na bancada de centrais."
                    },
                    SuggestedTools = new List<string> { "Multímetro Digital True RMS", "Fonte Estabilizada de Bancada", "Scanner de Reprogramação" },
                    SuggestedStockParts = new List<string> { "Módulo de Injeção Eletrônica ECU", "Relé Principal", "Bateria Automotiva 60Ah" },
                    ReferenceStandard = "Queda de Tensão na Alimentação da ECU: < 0.1V | Tensão Mínima no Processador: 11.5V DC.",
                    ClarifyingQuestions = new List<string> { "Foi feita chupeta de bateria com polaridade invertida recentemente?" },
                    InteractiveReplyChips = new List<string> { "Fiz hard reset e resolveu", "Alimentação da ECU está perfeita", "Alternador deu pico de sobretensão" }
                },
                new DiagnosticEntry
                {
                    Code = "P0700",
                    Title = "Sistema de Controle da Transmissão Automática (TCM - Solicitação MIL)",
                    System = "Câmbio Automático / Automatizado & TCM",
                    Symptoms = "Câmbio travado em modo de emergência na 3ª marcha (limp mode), trancos fortes ao engatar ré ou drive, luz de injeção acesa constante.",
                    ProbableCauses = new List<string> { "DTC específico gravado no módulo da transmissão automática (TCM)", "Falta de alimentação ou fusível da transmissão queimado", "Solenoide de pressão ou troca de marcha em curto", "Nível de óleo do câmbio automático baixo ou fluido degradado" },
                    GuidedSteps = new List<string>
                    {
                        "1. NOTA IMPORTANTE: O código P0700 na injeção é apenas um aviso informativo da transmissão pedindo para acender a luz da injeção!",
                        "2. Acesse no scanner o módulo 'Transmissão Automática (TCM)' para ler o código real específico da falha (ex: P0730, P0750, P0740 solenoide de TCC).",
                        "3. Verificar nível e estado do fluido de transmissão automática (ATF) conforme procedimento da vareta ou bujão de nível à temperatura de trabalho."
                    },
                    SuggestedTools = new List<string> { "Scanner Automotivo com Diagnóstico de Câmbio TCM", "Termômetro Infravermelho" },
                    SuggestedStockParts = new List<string> { "Fluido para Câmbio Automático ATF", "Filtro de Câmbio", "Jogo de Solenoides de Câmbio" },
                    ReferenceStandard = "Temperatura para Verificação de Nível do Câmbio: entre 40°C e 50°C | Fluido límpido sem cheiro de queimado.",
                    ClarifyingQuestions = new List<string> { "Qual código específico apareceu ao entrar no módulo do câmbio com o scanner?" },
                    InteractiveReplyChips = new List<string> { "Câmbio travou na 3ª marcha", "Deu tranco no engate", "Vou ler o módulo TCM no scanner" }
                },
                new DiagnosticEntry
                {
                    Code = "TESTE_RELE",
                    Title = "Guia Técnico: Pinagem e Teste de Relés Automotivos (DIN 72552)",
                    System = "Acionamento de Cargas Elétricas",
                    Symptoms = "Bomba de combustível não aciona, faróis não ligam, ventoinha não dispara, motor de arranque não dá partida.",
                    ProbableCauses = new List<string>
                    {
                        "Bobina do relé queimada / rompida",
                        "Contatos de potência 30/87 carbonizados com alta resistência de passagem",
                        "Relé colado (soldado pelo arco elétrico sob alta corrente)"
                    },
                    GuidedSteps = new List<string>
                    {
                        "PINAGEM PADRÃO DIN 72552:",
                        " - Pino 30: Entrada de alimentação direta de força (Positivo com fusível de proteção).",
                        " - Pino 85: Comando de acionamento (geralmente Terra / Pulso negativo da central ou botão).",
                        " - Pino 86: Positivo de comando da bobina (Positivo pós-chave linha 15 ou linha 30).",
                        " - Pino 87: Saída de carga normalmente aberta (alimenta farol, bomba, buzina, ventoinha).",
                        " - Pino 87a (em relés de 5 pinos): Saída normalmente fechada quando desenergizado.",
                        "TESTE EM BANCADA:",
                        "1. Medir resistência da bobina entre 85 e 86: valor normal entre 60Ω e 120Ω.",
                        "2. Aplicar 12V entre 85 e 86: deve haver clique sonoro nítido.",
                        "3. Com 12V aplicado na bobina, medir continuidade e resistência entre 30 e 87: deve ser menor que 0.2Ω. Se der acima de 1Ω, os platinados estão carbonizados!"
                    },
                    SuggestedTools = new List<string> { "Multímetro Digital", "Fonte de Alimentação de Bancada 12V", "Cabos com Garras Jacaré" },
                    SuggestedStockParts = new List<string> { "Relé Auxiliar 4 Pinos 40A", "Relé Inversor 5 Pinos 40A", "Relé Temporizador de Vidro" },
                    ReferenceStandard = "Resistência da Bobina: 60Ω a 120Ω | Resistência de Contato Fechado: < 0.2Ω",
                    ClarifyingQuestions = new List<string> { "O relé é de 4 ou 5 pinos?" },
                    InteractiveReplyChips = new List<string> { "Relé de 4 pinos", "Relé de 5 pinos", "Dá o clique mas não passa 12V" }
                },
                new DiagnosticEntry
                {
                    Code = "REDE_CAN",
                    Title = "Diagnóstico Físico de Rede CAN Bus Automotiva (High Speed)",
                    System = "Redes de Comunicação Multiplexada",
                    Symptoms = "Painel não comunica com injeção, código U0100, carro não pega, luzes do ABS e Airbag acesas juntas.",
                    ProbableCauses = new List<string>
                    {
                        "Resistor de terminação de 120Ω aberto dentro do painel ou da ECU",
                        "Curto-circuito entre fios CAN High e CAN Low",
                        "Curto de um dos fios da CAN para o positivo ou para o terra",
                        "Conector OBD-II com fiação rompida ou oxidação por umidade"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. Desligar a ignição e aguardar redes hibernarem.",
                        "2. Localizar os pinos do conector OBD-II: Pino 6 = CAN High e Pino 14 = CAN Low.",
                        "3. Medir a resistência ôhmica entre os pinos 6 e 14 com a chave desligada:",
                        " - PADRÃO EXATO: 60 Ohms (dois resistores de 120Ω em paralelo nas extremidades).",
                        " - Se der 120 Ohms: um resistor terminal está aberto ou um módulo principal desconectado.",
                        " - Se der 0 Ohms: os fios CAN High e CAN Low estão em curto um com o outro.",
                        " - Se der circuito aberto (> 1000Ω): barramento rompido.",
                        "4. Ligar a ignição e medir tensões contínuas para o terra (Pino 4/5):",
                        " - Pino 6 (CAN High): repouso ~2.5V, transmitindo varia entre 2.5V e 3.5V.",
                        " - Pino 14 (CAN Low): repouso ~2.5V, transmitindo varia entre 1.5V e 2.5V."
                    },
                    SuggestedTools = new List<string> { "Multímetro Digital True RMS", "Osciloscópio Automotivo 2 Canais", "Caixa de Breakout OBD-II" },
                    SuggestedStockParts = new List<string> { "Conector OBD-II Fêmea", "Fiação Trançada de Rede CAN" },
                    ReferenceStandard = "Resistência de Barramento: 60Ω nominais (55Ω a 65Ω) | Tensão Média CAN H: 2.7V | Tensão Média CAN L: 2.3V",
                    ClarifyingQuestions = new List<string> { "Qual a resistência em Ohms medida entre os pinos 6 e 14?" },
                    InteractiveReplyChips = new List<string> { "Mede 60 Ohms normal", "Mede 120 Ohms", "Mede 0 Ohms em curto" }
                },
                new DiagnosticEntry
                {
                    Code = "ABS_CONTROLE_TRACAO_VELOCIMETRO",
                    Title = "Diagnóstico: Luz do ABS/Controle de Tração Acesa e Velocímetro Inoperante (Chevrolet Onix / Prisma)",
                    System = "Freios ABS e Segurança Ativa",
                    Symptoms = "Luz do ABS e estabilidade acesa no painel, controle de tração desativado, velocímetro parou de marcar na rodovia ou oscila.",
                    ProbableCauses = new List<string>
                    {
                        "Rolamento de roda com anel fônico / encoder magnético montado invertido",
                        "Limalha de ferro de pastilha de freio acumulada na pista magnética do rolamento",
                        "Chicote do sensor de velocidade de roda (WSS) dianteiro esmagado na caixa de roda",
                        "Sensor de efeito Hall da roda dianteira avariado"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. Conectar scanner automotivo no módulo de ABS e ler os códigos DTC (ex: C0035 roda dianteira esquerda ou C0040 dianteira direita).",
                        "2. No Onix/Prisma, o sinal de velocidade do painel é calculado pela média dos sensores de ABS dianteiros.",
                        "3. Colocar o carro no elevador e entrar em 'Leitura de Parâmetros' no scanner: girar cada roda com a mão e verificar se todas marcam a velocidade proporcional.",
                        "4. Se uma roda marcar 0 km/h: inspecione o conector do sensor no montante da suspensão contra oxidação.",
                        "5. Meça a tensão de alimentação no plugue do sensor com a chave ligada (deve chegar 12V na linha do sensor Hall ativo).",
                        "6. Inspecione o rolamento de roda: se foi trocado recentemente, verifique com cartão detector magnético se a face do encoder está virada para dentro (para o sensor)."
                    },
                    SuggestedTools = new List<string> { "Scanner Automotivo com Linha de Dados de ABS", "Multímetro Digital", "Cartão Testador de Anel Magnético" },
                    SuggestedStockParts = new List<string> { "Sensor de ABS Dianteiro Onix/Prisma", "Rolamento de Roda com Anel Fônico Magnético" },
                    ReferenceStandard = "Tensão de Alimentação do Sensor: 11.5V a 12.8V | Sinal do Sensor Hall Ativo: variação de corrente 7mA a 14mA por dente magnético",
                    ClarifyingQuestions = new List<string> { "Foi trocado rolamento de roda recentemente no carro?", "O velocímetro parou totalmente ou só oscila?" },
                    InteractiveReplyChips = new List<string> { "Trocado rolamento recentemente", "Velocímetro oscila na pista", "Luz acendeu após frear forte" }
                },
                new DiagnosticEntry
                {
                    Code = "ALTERNADOR_SMART_CHARGE_FORD",
                    Title = "Diagnóstico: Alternador Pilotado Ford Smart Charge / Luz de Bateria Acesa (Ford Ka / Fiesta / EcoSport)",
                    System = "Sistema de Carga Pilotada Inteligente",
                    Symptoms = "Luz da bateria acesa no painel, mesmo com alternador novo gerando 14V; código DTC P0620 ou P0622.",
                    ProbableCauses = new List<string>
                    {
                        "Regulador de voltagem paralelo sem suporte ao protocolo PWM Ford Smart Charge",
                        "Rompimento do fio RC (Request Charge - comando da ECU) ou LI (Load Indicator - retorno)",
                        "Fusível de monitoramento de carga de 3A/10A queimado no cofre do motor",
                        "Oxidação nos terminais do chicote de 3 vias do alternador próximo ao radiador"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. Entenda o sistema Ford: a luz da bateria é comandada exclusivamente pela ECU via software, e não pela lâmpada piloto D+ convencional.",
                        "2. Identifique os pinos do conector de 3 vias do alternador Ford:",
                        " - Pino 1 (AS / Sense): positivo direto linha 30 para monitoramento da tensão da bateria (deve ter 12V com motor desligado).",
                        " - Pino 2 (RC): comando PWM enviado da ECU para o regulador controlar a geração.",
                        " - Pino 3 (LI): sinal de resposta de carga enviado pelo regulador para a ECU.",
                        "3. Teste do fusível Sense: meça se chegam 12V no pino AS. Se estiver 0V, procure o fusível de 3A ou 10A queimado na caixa do cofre.",
                        "4. Se o alternador foi trocado recentemente: certifique-se de que o regulador de voltagem é padrão Ford Smart Charge original (ex: Magneti Marelli ou Bosch com protocolo compatível). Reguladores genéricos comuns de 1 via acendem a luz da bateria direto!"
                    },
                    SuggestedTools = new List<string> { "Multímetro Digital", "Osciloscópio Automotivo", "Scanner Automotivo" },
                    SuggestedStockParts = new List<string> { "Regulador de Voltagem Ford Smart Charge", "Chicote do Alternador 3 Vias" },
                    ReferenceStandard = "Pino AS: 12V constantes | Frequência PWM linha RC: ~125 Hz | Tensão de Carga Gerenciada: 13.4V a 14.8V conforme carga térmica",
                    ClarifyingQuestions = new List<string> { "O alternador foi trocado recentemente?", "Chegam 12V no pino de monitoramento AS do conector?" },
                    InteractiveReplyChips = new List<string> { "Alternador foi trocado agora", "Chega 12V no pino Sense", "Luz apaga só em alta rotação" }
                },
                new DiagnosticEntry
                {
                    Code = "VIDRO_ELETRICO_ANTI_ESMAGAMENTO",
                    Title = "Diagnóstico: Vidro Elétrico Sobe e Volta Sozinho / Calibração Anti-Esmagamento (VW Fox / Gol / Polo)",
                    System = "Módulo de Conforto e Vidros Elétricos",
                    Symptoms = "Ao acionar o vidro para subir em um toque, ao atingir o meio ou o topo ele desce cerca de 10 cm sozinho; botão one-touch não responde.",
                    ProbableCauses = new List<string>
                    {
                        "Perda da programação do batente de fim de curso superior na memória do módulo conforto",
                        "Atrito mecânico excessivo e canaletas de borracha ressecadas simulando um obstáculo/braço",
                        "Bateria desconectada ou descarregada recentemente desprogramando o módulo",
                        "Máquina de vidro desalinhada ou com cabo de aço desfiando"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. O sistema anti-esmagamento monitora o consumo em Amperes do motor: se o vidro encontra peso para subir, o módulo inverte o giro por segurança.",
                        "2. Procedimento de Reset / Calibração Rápida:",
                        " - Ligue a ignição (sem dar partida).",
                        " - Puxe o botão para fechar o vidro até o topo e MANTENHA O BOTÃO PUXADO POR 5 SEGUNDOS após bater no batente.",
                        " - Em seguida, aperte o botão para descer até o fim e MANTENHA PRESSIONADO POR 5 SEGUNDOS no batente inferior.",
                        " - Solte o botão e teste o fechamento em um toque (one-touch).",
                        "3. Se o vidro continuar voltando:",
                        " - Limpe a canaleta de borracha da porta e aplique silicone spray ou grafite neutro.",
                        " - Inspecione se o trilho da máquina não está torto forçando o motor elétrico."
                    },
                    SuggestedTools = new List<string> { "Silicone Spray Automotivo", "Amperímetro de Bancada", "Chave Philips e Torx T20" },
                    SuggestedStockParts = new List<string> { "Canaleta de Vidro da Porta", "Máquina de Vidro Elétrico", "Módulo de Conforto" },
                    ReferenceStandard = "Corrente de Operação Livre: 4A a 7A | Corrente de Bloqueio/Anti-Esmagamento: > 12A dispara recuo automático",
                    ClarifyingQuestions = new List<string> { "O carro ficou sem bateria recentemente?", "O vidro sobe pesado fazendo barulho?" },
                    InteractiveReplyChips = new List<string> { "Bateria foi desligada ontem", "Vidro sobe pesado e rangendo", "Fiz o reset e voltou normal" }
                },
                new DiagnosticEntry
                {
                    Code = "MARCADOR_COMBUSTIVEL_BOIA",
                    Title = "Diagnóstico: Marcador de Combustível Oscilando ou Travado na Reserva (Renault Sandero / Logan / Duster)",
                    System = "Instrumentação e Medição de Nível",
                    Symptoms = "Marcador de combustível digital no painel marca reserva com tanque cheio, oscila repentinamente ou apaga barras.",
                    ProbableCauses = new List<string>
                    {
                        "Desgaste das trilhas cerâmicas do reostato do sensor de nível (boia)",
                        "Oxidação e aquecimento no terminal de terra do chicote da tampa do tanque",
                        "Haste metálica da boia enroscando na carcaça do copo da bomba de combustível",
                        "Painel digital necessitando de auto-teste e reset de memória de combustível"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. Teste de Bancada do Sensor de Nível:",
                        " - Levantar o assento do banco traseiro e soltar a tampa plástica de proteção.",
                        " - Desconectar o plugue elétrico da tampa da bomba de combustível.",
                        " - Conectar as pontas do multímetro na escala de Ohms nos 2 terminais do sensor de nível.",
                        " - Valores de Referência Renault:",
                        "   • Tanque Vazio: 300Ω a 350Ω",
                        "   • Meio Tanque: 150Ω a 180Ω",
                        "   • Tanque Cheio: 20Ω a 30Ω",
                        "2. Movimentar a haste da boia devagar de ponta a ponta: a resistência deve variar de forma contínua e suave sem dar 'OL' ou saltos bruscos.",
                        "3. Se a resistência estiver perfeita no sensor: inspecione os pinos do conector fêmea contra derretimento (muito comum em Renault por corrente alta da bomba compartilhando massa).",
                        "4. Procedimento de Reset do Painel Renault: segurar o botão da alavanca do limpador (computador de bordo) pressionado e ligar a ignição para entrar em modo de auto-teste dos ponteiros."
                    },
                    SuggestedTools = new List<string> { "Multímetro Digital na escala 2000Ω", "Chave para Tampa de Tanque", "Bancada de Teste de Boias" },
                    SuggestedStockParts = new List<string> { "Sensor de Nível de Combustível Sandero/Logan", "Chicote de Reparo da Tampa da Bomba" },
                    ReferenceStandard = "Tanque Vazio: 320Ω ± 15Ω | Meio Tanque: 165Ω ± 10Ω | Tanque Cheio: 25Ω ± 5Ω",
                    ClarifyingQuestions = new List<string> { "O marcador apaga tudo ou fica travado?", "Ao movimentar a boia no tanque o valor muda?" },
                    InteractiveReplyChips = new List<string> { "Mede resistência aberta no multímetro", "Pino do chicote está esquentado", "Painel travou na reserva" }
                },
                new DiagnosticEntry
                {
                    Code = "START_STOP_BATERIA_IBS",
                    Title = "Diagnóstico: Sistema Start-Stop Indisponível e Sensor IBS de Bateria (Jeep Renegade / Fiat Toro)",
                    System = "Gerenciamento de Energia e Bateria Inteligente",
                    Symptoms = "Aviso no painel 'Verificar Sistema Start-Stop', luz de injeção acesa, motor não desliga nos semáforos.",
                    ProbableCauses = new List<string>
                    {
                        "Estado de Carga da Bateria (SoC - State of Charge) inferior a 75%",
                        "Instalação de bateria comum convencional no lugar de bateria especial EFB ou AGM",
                        "Conector do sensor inteligente IBS no borne negativo desconectado ou rompido",
                        "Falta de calibração/registro da nova bateria no módulo BCM com scanner"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. Entenda a regra do Start-Stop: a BCM desativa o sistema preventivamente se qualquer condição de segurança da bateria não for atingida.",
                        "2. Teste da Bateria:",
                        " - Utilizar analisador eletrônico de condutância de bateria (CCA).",
                        " - Se a saúde (SoH) ou o estado de carga (SoC) estiver abaixo de 75-80%, a central bloqueia o Start-Stop.",
                        "3. Tipo da Bateria: Em veículos com Start-Stop é OBRIGATÓRIO o uso de bateria homologada EFB ou AGM. Baterias comuns convencionais não suportam os ciclos rápidos e causam erro em semanas!",
                        "4. Inspecione o sensor IBS (pequeno módulo preso no terminal negativo da bateria): verifique se o conector de 2 vias LIN Bus não está quebrado.",
                        "5. Procedimento com Scanner: após substituir a bateria, entre em 'Painel/BCM' e execute 'Reset do Sensor de Monitoramento de Bateria (IBS)'."
                    },
                    SuggestedTools = new List<string> { "Analisador Digital de Bateria CCA", "Scanner Automotivo com Protocolo FCA/Stellantis", "Multímetro" },
                    SuggestedStockParts = new List<string> { "Bateria EFB 72Ah", "Bateria AGM 70Ah", "Sensor de Bateria IBS Negativo" },
                    ReferenceStandard = "Tensão de Repouso Mínima para Start-Stop: 12.60V (SoC > 75%) | Rede LIN do Sensor IBS: 9V a 12V com pulsos digitais",
                    ClarifyingQuestions = new List<string> { "A bateria instalada no carro é EFB/AGM ou comum?", "Qual o teste de CCA acusou no analisador?" },
                    InteractiveReplyChips = new List<string> { "Bateria é nova porém é comum", "Sensor IBS está conectado normal", "Acusa carga abaixo de 70%" }
                },
                new DiagnosticEntry
                {
                    Code = "CHAVE_SETA_FAROL_ALTO",
                    Title = "Diagnóstico: Chave de Seta com Mau Contato / Farol Alto Aceso Direto (Hyundai HB20 / Creta)",
                    System = "Comandos de Coluna e Iluminação",
                    Symptoms = "Farol alto fica ligado direto, ao dar seta para a esquerda a luz alta pisca junto, ou farol baixo apaga sozinho ao esterçar.",
                    ProbableCauses = new List<string>
                    {
                        "Desgaste mecânico e folga nas lâminas de contato internas da alavanca da chave de seta",
                        "Lubrificante interno original endurecido com poeira provocando fuga de contato",
                        "Mola de retenção da alavanca sem pressão mantendo o relampejador acionado",
                        "Chicote da coluna de direção sob tensão"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. Teste Rápido de Diagnóstico na Oficina:",
                        " - Com o motor ligado e farol aceso, dê leves toques com o dedo na ponta da alavanca de seta.",
                        " - Se a luz alta piscar ou apagar com a vibração mecânica: defeito 100% interno na chave de seta!",
                        "2. Teste Elétrico no Conector:",
                        " - Retirar as duas capas plásticas da coluna de direção (2 parafusos frontais e 1 inferior).",
                        " - Desconectar o chicote traseiro da chave de seta.",
                        " - Com multímetro na escala de continuidade (bip), testar se há continuidade indevida entre o pino de farol alto e o pino de alimentação sem puxar a alavanca.",
                        "3. Solução recomendada: substituir o conjunto da chave de seta multifunção."
                    },
                    SuggestedTools = new List<string> { "Chave Phillips PH2", "Multímetro Digital com Sinal Sonoro", "Espátulas Plásticas de Acabamento" },
                    SuggestedStockParts = new List<string> { "Chave de Seta Completa HB20", "Chave de Seta com Farol de Neblina" },
                    ReferenceStandard = "Continuidade no Relampejador: < 0.2Ω apenas quando puxada | Circuito Aberto (OL) em repouso absoluto",
                    ClarifyingQuestions = new List<string> { "Ao mexer levemente na alavanca o farol alto pisca?", "O farol baixo também apaga?" },
                    InteractiveReplyChips = new List<string> { "Pisca só de encostar na alavanca", "Farol alto não desliga mais", "Já testei os relés e estão bons" }
                },
                new DiagnosticEntry
                {
                    Code = "COMANDOS_VOLANTE_CINTA_AIRBAG",
                    Title = "Diagnóstico: Buzina e Comandos de Som Inoperantes / Cinta do Airbag Clock Spring (Toyota Corolla / Etios)",
                    System = "Coluna de Direção, Airbag e Controles de Volante",
                    Symptoms = "Buzina parou de funcionar de repente, controles de áudio no volante não respondem e luz do Airbag acendeu no painel.",
                    ProbableCauses = new List<string>
                    {
                        "Rompimento das fitas flexíveis condutoras internas da cinta do airbag (Clock Spring / Spiral Cable)",
                        "Cinta montada fora de centro após reparo na caixa de direção rompendo no batente de esterçamento",
                        "Conector amarelo do volante com trava de segurança solta",
                        "Aterramento da coluna de direção deficiente"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. PROCEDIMENTO DE SEGURANÇA OBRIGATÓRIO: Desconectar o polo negativo da bateria e aguardar 10 minutos para descarregar os capacitores do módulo do Airbag antes de mexer!",
                        "2. Diagnóstico Técnico de Sintoma Combinado:",
                        " - Quando buzina e botões de multimídia do volante param simultaneamente, o defeito NÃO É relé nem fusível: eles compartilham as trilhas flexíveis da cinta do volante.",
                        "3. Desmontagem e Teste de Continuidade:",
                        " - Soltar as travas laterais da bolsa do airbag no volante.",
                        " - Desconectar os plugues amarelos de segurança com chave fina.",
                        " - Com multímetro em continuidade, testar cada via da cinta espiral desde o conector de entrada até o topo.",
                        " - Se uma ou mais vias acusarem 'OL' (aberto), a cinta está rompida.",
                        "4. DICA DE MONTAGEM DA NOVA CINTA: Gire a nova cinta totalmente para um lado com cuidado, conte o total de voltas (geralmente 5 voltas) e retorne exatamente a metade (2,5 voltas) para deixá-la 100% centralizada com as rodas retas antes de encaixar o volante!"
                    },
                    SuggestedTools = new List<string> { "Multímetro Digital", "Chaves Torx T30 com Furo Guia", "Extrator de Volante (se necessário)" },
                    SuggestedStockParts = new List<string> { "Cinta de Airbag Clock Spring Corolla/Etios", "Hard Disk Toyota" },
                    ReferenceStandard = "Resistência por Trilha da Cinta: < 0.3Ω contínuos em qualquer ângulo de giro do volante",
                    ClarifyingQuestions = new List<string> { "Foi feito alinhamento ou mexido na caixa de direção recentemente?", "A luz do Airbag também acendeu?" },
                    InteractiveReplyChips = new List<string> { "Luz do Airbag acendeu junto", "Mexeram na caixa de direção", "Buzina funciona só com volante virado" }
                },
                new DiagnosticEntry
                {
                    Code = "P0135",
                    Title = "Mau Funcionamento no Circuito do Aquecedor da Sonda Lambda (Banco 1, Sensor 1)",
                    System = "Injeção Eletrônica e Controle de Emissões",
                    Symptoms = "Luz de injeção acesa constante, consumo elevado na fase fria, odor de combustível no escapamento.",
                    ProbableCauses = new List<string>
                    {
                        "Resistência de aquecimento da sonda lambda queimada internamente (circuito aberto)",
                        "Fusível de proteção do aquecedor da sonda rompido na caixa de fusíveis",
                        "Falta de alimentação positiva 12V pós-chave no plugue da sonda",
                        "Chicote do escapamento encostado no coletor com isolamento derretido"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. Desconectar o conector de 4 vias da sonda lambda pré-catalisador.",
                        "2. Identificar os 2 fios da mesma cor (geralmente brancos ou pretos) correspondentes à resistência do aquecedor.",
                        "3. Medir com multímetro na escala de Ohms (200Ω) a resistência entre os 2 terminais do aquecedor na sonda:",
                        " - PADRÃO NORMAL: 3.5Ω a 14.0Ω.",
                        " - Se der 'OL' (circuito aberto / infinito): o aquecedor queimou, substituir a sonda lambda.",
                        "4. No chicote do veículo, com chave ligada, medir se chegam 12V de alimentação e sinal de massa PWM da central."
                    },
                    SuggestedTools = new List<string> { "Multímetro Digital na escala 200Ω", "Chave Especial para Sonda Lambda 22mm", "Lâmpada de Teste" },
                    SuggestedStockParts = new List<string> { "Sonda Lambda Pré-Catalisador 4 Fios", "Fusível 10A/15A" },
                    ReferenceStandard = "Resistência do Aquecedor: 3.5Ω a 14.0Ω a 20°C | Tensão de Alimentação: 12.0V a 14.2V",
                    ClarifyingQuestions = new List<string> { "A resistência entre os fios do aquecedor deu quantos Ohms?", "Chegam 12V no chicote com a chave ligada?" },
                    InteractiveReplyChips = new List<string> { "Resistência deu aberta OL", "Chegam 12V no chicote", "Fusível está queimado" }
                },
                new DiagnosticEntry
                {
                    Code = "P0171",
                    Title = "Sistema de Combustível Muito Pobre (Banco 1)",
                    System = "Alimentação de Combustível e Admissão de Ar",
                    Symptoms = "Luz de injeção acesa, motor engasga em retomadas, oscilação de marcha lenta, estouros na admissão.",
                    ProbableCauses = new List<string>
                    {
                        "Entrada falsa de ar após o corpo de borboleta (junta do coletor ou mangueira de vácuo)",
                        "Baixa pressão na linha de combustível (bomba de combustível cansada < 3.0 bar)",
                        "Sensor de fluxo de ar MAF com filamento carbonizado",
                        "Bicos injetores entupidos ou com vazão reduzida"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. Teste de Entrada Falsa de Ar: inspecione mangueiras de vácuo, mangueira do servofreio (hidrovácuo) e junta do coletor de admissão com máquina de fumaça.",
                        "2. Teste da Linha de Combustível: instalar manômetro de pressão na flauta. Pressão deve se manter estável entre 3.8 e 4.2 bar (sistemas sem retorno) ou 3.0 bar (com regulador no vácuo).",
                        "3. Leitura do Parâmetro STFT e LTFT no scanner: se o Short Term Fuel Trim e Long Term Fuel Trim estiverem acima de +20%, a injeção está tentando compensar falta de combustível ou excesso de ar."
                    },
                    SuggestedTools = new List<string> { "Máquina Geradora de Fumaça para Vácuo", "Manômetro de Pressão de Combustível", "Scanner Automotivo" },
                    SuggestedStockParts = new List<string> { "Kit de Juntas do Coletor", "Refil da Bomba de Combustível", "Sensor MAF" },
                    ReferenceStandard = "Ajuste de Combustível a Longo Prazo (LTFT): entre -10% e +10% | Pressão de Combustível: 3.5 a 4.2 bar",
                    ClarifyingQuestions = new List<string> { "A pressão da bomba de combustível deu quanto no manômetro?", "O ajuste de combustível LTFT está acima de +20%?" },
                    InteractiveReplyChips = new List<string> { "Pressão deu abaixo de 2.5 bar", "Entrada falsa de ar na admissão", "Sensor MAF limpo" }
                },
                new DiagnosticEntry
                {
                    Code = "P0335",
                    Title = "Mau Funcionamento no Circuito do Sensor de Posição da Árvore de Manivelas (CKP / Rotação)",
                    System = "Gerenciamento do Motor e Sincronismo",
                    Symptoms = "Motor vira forte no arranque mas não pega; apaga repentinamente quando atinge temperatura de trabalho.",
                    ProbableCauses = new List<string>
                    {
                        "Bobina interna do sensor de rotação indutivo abrindo circuito com o aquecimento",
                        "Entre-ferro fora de especificação entre a ponta do sensor e a roda fônica",
                        "Fiação do sensor esfolada encostando no escapamento ou bloco",
                        "Roda fônica com dentes danificados ou solta"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. Teste de Resistência (Sensor Indutivo): desconectar o plugue e medir a resistência nos 2 pinos de sinal (faixa típica: 500Ω a 1200Ω). Se der infinito quando o motor está quente: bobina interna do sensor abre com calor!",
                        "2. Teste com Osciloscópio / Scanner: ao dar partida, o scanner deve registrar rotação subindo para 200 a 300 RPM. Se marcar 0 RPM contínuos durante o giro do motor de partida: sinal ausente!",
                        "3. Inspecione o conector e o chicote próximo à polia do virabrequim ou flange traseira do volante."
                    },
                    SuggestedTools = new List<string> { "Osciloscópio Automotivo", "Multímetro Digital", "Scanner Automotivo" },
                    SuggestedStockParts = new List<string> { "Sensor de Rotação CKP", "Roda Fônica" },
                    ReferenceStandard = "Resistência Sensor Indutivo: 600Ω a 1100Ω | RPM no Arranque: 200 a 300 RPM | Tensão de Pico a Pico no Osciloscópio: > 2.0V AC no giro",
                    ClarifyingQuestions = new List<string> { "O scanner acusa rotação (RPM) enquanto você dá a partida?", "O carro apaga quente e volta a pegar frio?" },
                    InteractiveReplyChips = new List<string> { "Marca 0 RPM no arranque", "Sensor deu resistência aberta", "Apaga só quando esquenta" }
                },
                new DiagnosticEntry
                {
                    Code = "P0420",
                    Title = "Eficiência do Sistema de Catalisador Abaixo do Limite (Banco 1)",
                    System = "Controle de Emissões e Pós-Tratamento",
                    Symptoms = "Luz de injeção acesa no painel, sem perda notável de potência em percursos urbanos.",
                    ProbableCauses = new List<string>
                    {
                        "Cerâmica interna do catalisador desgastada, trincada ou sem metais nobres",
                        "Vazamento no escapamento entre a sonda pré e a sonda pós",
                        "Sonda lambda pós-catalisador lenta ou avariada",
                        "Contaminação por óleo do motor ou combustível adulterado"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. Analisar gráfico de tensão das duas sondas no scanner com motor bem aquecido a 2500 RPM:",
                        " - Sonda 1 (Pré): deve oscilar ciclicamente e rapidamente entre 0.1V (pobre) e 0.9V (rico).",
                        " - Sonda 2 (Pós): deve permanecer quase reta e estável em torno de 0.5V a 0.7V.",
                        "2. Se a sonda pós-catalisador começar a oscilar e copiar o sinal da sonda pré: a cerâmica do catalisador perdeu a capacidade de retenção de oxigênio!",
                        "3. Inspecione visualmente e bata levemente no catalisador com martelo de borracha: se ouvir barulho de chocalho, a cerâmica está solta/esfarelada por dentro."
                    },
                    SuggestedTools = new List<string> { "Scanner com Gráfico de Sondas em Tempo Real", "Termômetro Infravermelho Laser", "Elevador" },
                    SuggestedStockParts = new List<string> { "Catalisador Homologado", "Sonda Lambda Pós-Catalisador" },
                    ReferenceStandard = "Tensão Sonda Pós em Catalisador Eficiente: 0.55V a 0.70V constante sem oscilações bruscas",
                    ClarifyingQuestions = new List<string> { "A sonda pós está oscilando igual à sonda pré?", "O escapamento tem vazamento ou trinca?" },
                    InteractiveReplyChips = new List<string> { "Sonda pós copia a pré", "Catalisador faz barulho por dentro", "Escapamento não tem vazamento" }
                },
                new DiagnosticEntry
                {
                    Code = "P0500",
                    Title = "Mau Funcionamento do Sensor de Velocidade do Veículo (VSS)",
                    System = "Trem de Força e Velocímetro",
                    Symptoms = "Ponteiro do velocímetro inoperante ou caindo para zero, hodômetro não incrementa km, direção elétrica endurece.",
                    ProbableCauses = new List<string>
                    {
                        "Pinhão plástico do sensor de velocidade gasto ou desdentado na caixa de câmbio",
                        "Falta de alimentação 12V pós-chave no chicote do sensor VSS",
                        "Fio de sinal de velocidade rompido entre o câmbio e o painel de instrumentos",
                        "Sensor de velocidade de efeito Hall queimado internamente"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. Localizar o sensor de velocidade acoplado no diferencial da caixa de câmbio.",
                        "2. Retirar o sensor e inspecionar o pinhão plástico dentado: se os dentes estiverem gastos, substitua o pinhão.",
                        "3. Teste elétrico no conector de 3 vias do sensor VSS:",
                        " - Pino de Alimentação: 12V pós-chave (linha 15).",
                        " - Pino de Massa: 0V constante aterrado na lata/bloco.",
                        " - Pino de Sinal: ao girar o eixo do sensor com a mão com chave ligada, a tensão deve alternar entre 0V e 5V (ou 12V conforme montadora).",
                        "4. Se o sensor gerar sinal mas o painel não marcar: confira continuidade do fio de sinal até o painel de instrumentos."
                    },
                    SuggestedTools = new List<string> { "Multímetro Digital", "Scanner Automotivo", "Elevador" },
                    SuggestedStockParts = new List<string> { "Sensor de Velocidade VSS", "Pinhão do Sensor de Velocidade", "Conector Chicote 3 Vias" },
                    ReferenceStandard = "Sinal de Velocidade: onda quadrada de 0V a 5V/12V com frequência proporcional à velocidade",
                    ClarifyingQuestions = new List<string> { "O sensor é mecânico no câmbio ou a velocidade vem do módulo ABS?", "O pinhão plástico está desgastado?" },
                    InteractiveReplyChips = new List<string> { "Pinhão plástico está liso", "Chegam 12V e terra no plugue", "Velocidade vem do ABS" }
                },
                new DiagnosticEntry
                {
                    Code = "U0121",
                    Title = "Perda de Comunicação com o Módulo de Freio ABS / ESP",
                    System = "Rede de Comunicação CAN Bus / Segurança Ativa",
                    Symptoms = "Luzes do ABS, Controle de Tração e Freio de Mão acesas simultaneamente, aviso no computador de bordo, velocímetro parado.",
                    ProbableCauses = new List<string>
                    {
                        "Falta de alimentação positiva no maxi-fusível do módulo de freio ABS (30A a 50A)",
                        "Ponto de aterramento principal da unidade hidráulica do ABS solto ou oxidado",
                        "Linha de comunicação CAN High ou CAN Low rompida até o módulo ABS",
                        "Queima da placa eletrônica do bloco de válvulas ABS"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. Teste de Alimentação do Módulo ABS:",
                        " - Desconectar o chicote de múltiplos pinos da unidade hidráulica do ABS no cofre.",
                        " - Inspecione os fusíveis de alta amperagem (Maxi-Fusíveis de 30A, 40A ou 50A) na caixa do cofre.",
                        " - Meça com lâmpada de teste se chegam os positivos diretos (linha 30) nos pinos de potência mais grossos do conector.",
                        "2. Teste do Aterramento: meça a continuidade dos pinos de massa para a lataria (resistência < 0.2Ω).",
                        "3. Teste da Rede CAN no conector do ABS: meça as tensões de barramento CAN High (~2.6V) e CAN Low (~2.4V) com ignição ligada.",
                        "4. Se todas as alimentações e rede estiverem perfeitas e o scanner não entra no ABS: módulo eletrônico do ABS inoperante."
                    },
                    SuggestedTools = new List<string> { "Multímetro Digital", "Lâmpada de Teste 21W", "Scanner Automotivo", "Diagrama Elétrico do Conector ABS" },
                    SuggestedStockParts = new List<string> { "Módulo Eletrônico de ABS", "Maxi-Fusível 40A", "Chicote do ABS" },
                    ReferenceStandard = "Tensão de Alimentação Contínua: 12.0V a 14.5V com lâmpada de teste sob carga | Resistência da Massa: < 0.2Ω",
                    ClarifyingQuestions = new List<string> { "Você testou os maxi-fusíveis de alta amperagem do ABS?", "Chega 12V sob carga no conector do ABS?" },
                    InteractiveReplyChips = new List<string> { "Maxi-fusível está queimado", "Chega 12V forte no plugue", "Massa está com resistência alta" }
                },
                new DiagnosticEntry
                {
                    Code = "FAROL_ACESO_DIRETO",
                    Title = "Farol Alto ou Baixo Não Apaga / Fica Aceso Direto com Chave Desligada",
                    System = "Iluminação Externa / Circuito de Força e Relés",
                    Symptoms = "Farol alto ou baixo fica ligado direto sem comando, descarrega a bateria com carro desligado, só apaga ao remover relé ou fusível.",
                    ProbableCauses = new List<string>
                    {
                        "Contatos internos do relé de farol (terminais 30 e 87) soldados/colados por arco elétrico",
                        "Lâmpadas fora de especificação (100W Super Branca / Rally) causando sobrecorrente e derretimento de contatos",
                        "Alavanca da chave de seta com lâmina de contato travada internamente",
                        "Curto-circuito no chicote entre a linha 30 positiva e a linha dos faróis",
                        "Driver de potência (transistor high-side) em curto no módulo de carroceria BCM/UPC"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. TESTE RÁPIDO DO RELÉ: Localize o relé do farol na caixa de fusíveis e retire-o do soquete. Se o farol apagar na hora, o curto está no relé ou no comando dele!",
                        "2. TESTE DE CONTINUIDADE DO RELÉ: Com o relé fora e desenergizado, meça com o multímetro (escala de continuidade/bip) entre os pinos 30 e 87. Se bipar com 0.0Ω, o relé está colado mecanicamente! Substitua o relé imediatamente.",
                        "3. VERIFICAÇÃO OBRIGATÓRIA DAS LÂMPADAS: Retire as lâmpadas do farol e confira a potência gravada na carcaça. Se forem de 100W, substitua pelas originais de 55/60W para não queimar o novo relé nem derreter o chicote!",
                        "4. SE NÃO APAGOU AO TIRAR O RELÉ: O circuito possui curto direto entre cabos no chicote dianteiro ou o veículo usa módulo BCM com saída em curto."
                    },
                    SuggestedTools = new List<string> { "Multímetro Digital", "Relé Auxiliar 12V 40A", "Alicate de Bico" },
                    SuggestedStockParts = new List<string> { "Relé Auxiliar 4 Pinos 12V 40A", "Lâmpada H4 12V 55/60W", "Chave de Seta" },
                    ReferenceStandard = "Relé Desenergizado: circuito aberto (OL) entre pinos 30 e 87 | Potência original de lâmpada: 55W/60W (máx 5A por lâmpada)",
                    ClarifyingQuestions = new List<string> { "Qual é a marca, modelo e ano do carro?", "Você já retirou o relé do farol na caixa de fusíveis para ver se ele apaga?", "As lâmpadas instaladas são originais de 55W ou são de 100W?" },
                    InteractiveReplyChips = new List<string> { "Tirei o relé e o farol apagou", "O relé testado na bancada está bipando", "As lâmpadas são de 100W", "Farol não apagou ao tirar relé" }
                },
                new DiagnosticEntry
                {
                    Code = "CHICOTE_TAMPA_TRASEIRA",
                    Title = "Falha Conjunta na Tampa Traseira (Desembaçador, Brake Light e Limpador)",
                    System = "Carroceria / Chicote Elétrico Flexível de Articulação",
                    Symptoms = "Desembaçador do vidro traseiro, terceira luz de freio (brake light) e limpador traseiro pararam ao mesmo tempo ou falham ao abrir a tampa.",
                    ProbableCauses = new List<string>
                    {
                        "Fios de cobre partidos por fadiga mecânica dentro da borracha sanfonada da articulação da tampa do porta-malas (defeito nº 1)",
                        "Fio terra (massa) principal da tampa traseira rompido na articulação",
                        "Fusível de proteção rompido após curto-circuito entre fios desencapados na borracha sanfonada",
                        "Falha nas trilhas térmicas do vidro traseiro"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. INSPEÇÃO DA BORRACHA SANFONADA: Solte com cuidado as bordas da coifa sanfonada de borracha entre a coluna do teto e a tampa traseira.",
                        "2. LOCALIZAÇÃO DOS FIOS ROMPIDOS: Puxe levemente cada fio por dentro da borracha. Em 95% dos casos de defeito múltiplo na tampa traseira, múltiplos fios de cobre estão partidos ou com o isolamento quebrado se encostando!",
                        "3. REPARO PROFISSIONAL: Emende os fios partidos utilizando solda estanho e espaguete termorretrátil de qualidade, ou substitua o trecho por cabos automotivos flexíveis novos para suportar o movimento da tampa.",
                        "4. FUSÍVEIS E ATERRAMENTO: Após refazer o chicote, inspecione a caixa de fusíveis (pois os fios em curto costumam queimar os fusíveis do desembaçador e freio) e teste o terra da tampa."
                    },
                    SuggestedTools = new List<string> { "Ferro de Solda e Estanho", "Espaguete Termorretrátil", "Multímetro Digital", "Fita Isolante de Tecido" },
                    SuggestedStockParts = new List<string> { "Fio Automotivo Flexível", "Espaguete Termorretrátil", "Fusível 15A / 20A" },
                    ReferenceStandard = "Continuidade dos condutores da coluna até a tampa: < 0.2Ω | Isolamento entre fios adjacentes: circuito aberto (OL)",
                    ClarifyingQuestions = new List<string> { "Qual o modelo e ano do carro?", "Você já inspecionou a borracha sanfonada da tampa do porta-malas?", "O limpador traseiro também parou junto com o desembaçador e a luz de freio?" },
                    InteractiveReplyChips = new List<string> { "Fios estão partidos na borracha sanfonada", "O limpador traseiro também parou", "Fusível do desembaçador estava queimado", "Fiz a emenda e funcionou" }
                },
                new DiagnosticEntry
                {
                    Code = "BOBINA_ONIX_PRISMA",
                    Title = "Defeito Crônico na Bobina de Ignição 4 Saídas (GM Onix / Prisma 1.0 e 1.4 SPE/4)",
                    System = "Ignição Eletrônica & Injeção Delco E83",
                    Symptoms = "Motor falhando sob carga (3ª e 4ª marcha em subida), luz de injeção piscando, DTC P0300, P0301 ou P0304 gravado, engasgos ao retomar velocidade.",
                    ProbableCauses = new List<string>
                    {
                        "Trinca microscópica no isolamento epóxi inferior da bobina de 4 torres com fuga de centelha direta para o cabeçote ou parafusos",
                        "Velas de ignição desgastadas com abertura de gap excessiva (> 1.0mm) forçando a bobina até furar o isolamento",
                        "Queima dos transistores de disparo na ECU Delco E83 se o cliente rodar muito tempo com a bobina falhando",
                        "Resistência excessiva dos cabos de vela supressores"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. INSPEÇÃO VISUAL DA BOBINA: Retire os 2 parafusos Torx da bobina de 4 saídas. Olhe atentamente a parte inferior de plástico preto. Em 90% dos casos, há linhas esbranquiçadas ou pontinhos de queimado onde a centelha estava furando o corpo da bobina!",
                        "2. PINAGEM DO CONECTOR DE 4 VIAS: Pino A (+12V Linha 15 pós-chave do fusível), Pino B (Terra de Potência Linha 31 - queda de tensão < 0.05V), Pino C (Pulso de disparo Cilindros 1 e 4 vindo da ECU E83), Pino D (Pulso de disparo Cilindros 2 e 3).",
                        "3. TESTE COM CENTELHADOR AJUSTÁVEL: Com o centelhador ajustado em 20kV, acione a partida. A faísca deve ser forte e azulada em todas as 4 saídas. Se em alguma saída a centelha falhar ou pular para a carcaça da bobina, ela está em curto interno.",
                        "4. REGRA DE OURO DA OFICINA: NUNCA troque apenas a bobina! Substitua SEMPRE o jogo de 4 velas originais (NGK BPR6EY ou equivalente) com gap estritamente calibrado em 0.8mm. Vela velha fura a bobina nova em menos de 30 dias!"
                    },
                    SuggestedTools = new List<string> { "Centelhador Ajustável", "Chave Torx T30", "Calibrador de Folga de Vela", "Multímetro Digital" },
                    SuggestedStockParts = new List<string> { "Bobina de Ignição 4 Saídas Onix/Prisma", "Jogo de Velas NGK BPR6EY", "Jogo de Cabos de Vela 1.0/1.4" },
                    ReferenceStandard = "Resistência primária: ~0.8Ω | Secundária: ~5.5kΩ a 6.5kΩ | Gap de vela calibrado: 0.8mm | Queda terra pino B: < 0.05V",
                    ClarifyingQuestions = new List<string> { "A falha acontece em marcha lenta ou somente quando você pisa fundo na subida?", "As velas já foram retiradas para checar o desgaste dos eletrodos?", "Você já inspecionou a parte de baixo da bobina procurando trincas ou manchas brancas?" },
                    InteractiveReplyChips = new List<string> { "Falha mais quando pisa fundo", "Vi uma trinca branca embaixo da bobina", "Velas estão com eletrodo gasto", "Já troquei velas e continuou" }
                },
                new DiagnosticEntry
                {
                    Code = "FORD_KA_VVT_SINCRONISMO",
                    Title = "Falha de Sincronismo CMP x CKP e Comando Variável Ti-VCT (Ford Ka 1.0 3 Cilindros)",
                    System = "Distribuição & Injeção Eletrônica Bosch MED 17.0.7 / EMS2211",
                    Symptoms = "Motor perde potência, luz de injeção acesa, DTC P0016 / P0017 (desalinhamento comando x virabrequim) ou P0011 / P0012 (avanço do comando lento), motor apaga em marcha lenta.",
                    ProbableCauses = new List<string>
                    {
                        "Válvula solenóide do variador VVT (admissão ou escape) travada ou entupida por borra de óleo",
                        "Correia dentada banhada a óleo esfarelando e entupindo a tela do pescador de óleo (pressão hidráulica insuficiente nos variadores)",
                        "Sensor de fase (CMP) ou sensor de rotação (CKP) com defeito de leitura ou chicote rompido",
                        "Pulo de dente na correia dentada ou folga mecânica nas polias variadoras"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. TESTE ELÉTRICO DA SOLENOIDE VVT: Desconecte a solenoide do comando na tampa de válvulas. Meça a resistência interna com multímetro (padrão: 7.0Ω a 12.0Ω a 20°C). Com a chave ligada, confira +12V no pino 1 e pulso PWM negativo no pino 2.",
                        "2. TESTE DE PRESSÃO DE ÓLEO: Instale manômetro na galeria de óleo. Em marcha lenta a 90°C, a pressão DEVE ser de no mínimo 1.2 bar. Se estiver abaixo de 0.8 bar, os variadores VVT não avançam e a central grava erro de sincronismo!",
                        "3. INSPEÇÃO DA CORREIA BANHADA A ÓLEO: Abra a tampa de óleo do motor e observe a correia dentada. Se apresentar trincas, desfiamento ou inchaço, remova o cárter imediatamente para desentupir o pescador de óleo.",
                        "4. SINCRONISMO COM OSCILOSCÓPIO: Conecte Canal 1 no sensor de rotação (CKP) e Canal 2 no sensor de fase (CMP). O dente de referência da fase deve coincidir exatamente no ponto de fasagem do virabrequim."
                    },
                    SuggestedTools = new List<string> { "Manômetro de Pressão de Óleo", "Osciloscópio Automotivo 2 Canais", "Ferramenta de Fasagem Ford 1.0 3 Cil", "Multímetro" },
                    SuggestedStockParts = new List<string> { "Válvula Solenóide VVT Ford Ka", "Kit Correia Banhada a Óleo", "Óleo 5W20 Sintético WSS-M2C948-B", "Sensor de Fase CMP" },
                    ReferenceStandard = "Resistência solenoide VVT: 7.0Ω a 12.0Ω | Pressão de óleo quente em lenta: > 1.2 bar | Tensão solenoide: +12V Linha 15",
                    ClarifyingQuestions = new List<string> { "Qual código DTC de falha está gravado no scanner (ex: P0016, P0011)?", "O óleo do motor e a correia banhada a óleo foram trocados recentemente?", "A pressão da bomba de óleo já foi medida com manômetro?" },
                    InteractiveReplyChips = new List<string> { "Scanner acusou DTC P0016", "Solenoide mediu 8 Ohms", "A correia está desfiando na tampa", "Pressão de óleo deu baixa" }
                },
                new DiagnosticEntry
                {
                    Code = "RENAULT_ALTERNADOR_IBS",
                    Title = "Alternador Pilotado (Linha LIN) & Sensor de Bateria IBS (Renault Sandero / Logan / Duster SCe)",
                    System = "Sistema de Carga Inteligente (Energy Smart Management - ESM)",
                    Symptoms = "Lâmpada da bateria piscando ou acesa no painel, bateria descarrega com o carro em uso ou após 2 dias, tensão no multímetro oscila entre 12.2V e 15.0V e eletricista acha que o alternador está ruim.",
                    ProbableCauses = new List<string>
                    {
                        "Desconhecimento da lógica ESM: o alternador Renault NÃO gera 14.4V contínuos (ele reduz para 12.6V em aceleração e sobe até 15.0V em desaceleração)",
                        "Mau contato ou rompimento no conector de sinal LIN do regulador de voltagem do alternador Valeo",
                        "Sensor IBS no polo negativo da bateria desconectado, solto ou cabo terra deficiente",
                        "Instalação de bateria convencional de chumbo em vez de bateria EFB específica para ESM",
                        "Falta de reset do sensor de envelhecimento da bateria no scanner após a troca"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. ENTENDA A LÓGICA ESM RENAULT: Em marcha lenta com bateria carregada, a tensão fica entre 12.6V e 13.2V. Ligue farol alto, desembaçador e ar condicionado: a ECU deve comandar via linha LIN e a tensão deve subir para 14.0V a 14.7V!",
                        "2. TESTE DA LINHA DE DADOS LIN NO REGULADOR: Pino único do regulador do alternador. Meça com multímetro na escala DC: deve indicar tensão oscilante entre 8V e 11V. Se marcar 0V ou 12V fixos sem variação, o cabo LIN está partido ou a ECU não está comunicando.",
                        "3. TESTE DO SENSOR IBS NO POLO NEGATIVO: Inspecione o sensor de corrente fixado no borne negativo da bateria. Verifique se o conector de 2 vias não está quebrado e se o chicote não está tracionado.",
                        "4. RESET OBRIGATÓRIO NO SCANNER: Ao trocar a bateria (usar sempre EFB), entre no módulo de injeção/carroceria e realize a 'Reinicialização do Sensor de Bateria' para que a central recalibre o estado de carga (SoC)."
                    },
                    SuggestedTools = new List<string> { "Multímetro Digital Automotivo", "Scanner Diagnóstico com Função Reset IBS", "Testador Eletrônico de Bateria CCA" },
                    SuggestedStockParts = new List<string> { "Bateria EFB 60Ah Renault", "Regulador de Voltagem Pilotado LIN Valeo", "Sensor de Bateria IBS" },
                    ReferenceStandard = "Tensão ESM em repouso: 12.6V a 13.2V | Tensão sob carga plena: 14.0V a 14.8V | Sinal LIN: 8V a 11V DC oscilante",
                    ClarifyingQuestions = new List<string> { "Qual a tensão medida no multímetro com farol e ar condicionado ligados?", "A bateria instalada no carro é EFB ou convencional?", "O conector do pino LIN do alternador está bem encaixado?" },
                    InteractiveReplyChips = new List<string> { "Com farol ligado sobe para 14.2V", "A bateria instalada é comum", "Tensão fica fixa em 12.3V", "Conector LIN está conectado normal" }
                },
                new DiagnosticEntry
                {
                    Code = "SENSOR_INDUTIVO_VS_HALL",
                    Title = "Guia Técnico Comparativo de Bancada: Sensor Indutivo vs Sensor de Efeito Hall",
                    System = "Sensores de Sincronismo, Rotação e Velocidade (CKP / CMP / VSS)",
                    Symptoms = "Dúvida de bancada sobre como diferenciar, identificar a pinagem e realizar o teste correto com multímetro e osciloscópio sem queimar o sensor.",
                    ProbableCauses = new List<string>
                    {
                        "Medição errada com multímetro (medir resistência em sensor Hall pode danificar o circuito integrado interno)",
                        "Confusão entre sensor de 2 fios (sempre indutivo) e sensor de 3 fios (pode ser Hall ou Indutivo com malha blindada)",
                        "Sensor indutivo que abre a bobina interna apenas quando esquenta",
                        "Sensor Hall sem alimentação de 5V ou 12V da ECU"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. SENSOR INDUTIVO (Gera sua própria energia - Não usa alimentação externa):",
                        "   • Identificação: 2 pinos de sinal (ou 3 pinos sendo 1 malha terra de blindagem).",
                        "   • Teste com Multímetro (Ohms): Meça a resistência da bobina interna. Faixa correta: 500Ω a 1200Ω (conforme montadora: VW AP ~850Ω, Fiat Fire ~600Ω). Se der infinito (OL), está aberto!",
                        "   • Teste em Tensão AC: Na partida, meça na escala de Volts AC. Deve gerar entre 1.0V AC e 2.5V AC.",
                        "   • Osciloscópio: Onda senoidal alternada que cruza a linha de zero com amplitude crescente conforme o RPM sobe.",
                        "2. SENSOR DE EFEITO HALL (Semicondutor ativo - Exige alimentação externa):",
                        "   • Identificação: 3 pinos: Pino 1 (Alimentação +5V ou +12V da ECU), Pino 2 (Terra 0V), Pino 3 (Sinal digital PWM).",
                        "   • AVISO: NUNCA meça resistência com multímetro em sensor Hall!",
                        "   • Teste com Multímetro (Volts DC): Chave ligada, confira +5V ou +12V no pino de alimentação e 0V no terra. No pino de sinal, gire a roda fônica bem devagar: a tensão deve comutar entre 0V e 5V (ou 0V e 12V).",
                        "   • Osciloscópio: Sinal digital quadrado perfeito (onda quadrada de 0V a 5V) com nível alto e baixo bem definidos."
                    },
                    SuggestedTools = new List<string> { "Multímetro Digital Automotivo", "Osciloscópio 2 Canais", "Soprador Térmico (teste a quente)" },
                    SuggestedStockParts = new List<string> { "Sensor de Rotação Indutivo", "Sensor de Rotação Hall", "Chicote Reparo de Rotação" },
                    ReferenceStandard = "Indutivo: 500Ω a 1200Ω / > 1.0V AC na partida | Hall: Alimentação +5V/+12V DC / Sinal comuta 0V a 5V onda quadrada",
                    ClarifyingQuestions = new List<string> { "O sensor que você está testando tem quantos fios no conector (2 ou 3)?", "Você tem osciloscópio ou está usando multímetro?", "O sensor é de rotação do virabrequim (CKP) ou de fase do comando (CMP)?" },
                    InteractiveReplyChips = new List<string> { "O sensor tem 2 fios", "O sensor tem 3 fios", "Estou com multímetro", "Estou com osciloscópio" }
                },
                new DiagnosticEntry
                {
                    Code = "REDE_CAN_VS_LIN",
                    Title = "Medição Física de Redes Multiplexadas: Barramento CAN Bus vs Linha LIN no Conector OBD-II",
                    System = "Redes de Comunicação Multiplexada Automotiva (SAE J1939 / ISO 11898 / LIN)",
                    Symptoms = "Vários módulos sem comunicação no scanner, luzes de injeção/ABS/airbag acesas juntas, carro não dá partida por imobilizador, dúvidas de medição na tomada de diagnóstico.",
                    ProbableCauses = new List<string>
                    {
                        "Resistor de terminação de 120Ω aberto ou em curto na rede CAN",
                        "Curto-circuito entre CAN High e CAN Low ou curto de um dos fios para a massa/positivo",
                        "Módulo eletrônico travando o barramento (transceiver CAN danificado)",
                        "Linha LIN (mono-fio) em curto com a massa derrubando alternador, sensor de chuva ou ar condicionado"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. TESTE DE RESISTÊNCIA DA REDE CAN (Chave DESLIGADA e Bateria Desconectada):",
                        "   • No conector OBD-II de 16 pinos: meça a resistência entre o Pino 6 (CAN High) e o Pino 14 (CAN Low).",
                        "   • VALOR CORRETO: 60 Ohms exatos (são dois resistores de terminação de 120Ω em paralelo, um na ECU e outro no Painel/BCM).",
                        "   • Se medir 120 Ohms: Um dos resistores de terminação está rompido ou o módulo correspondente está desconectado/sem terra!",
                        "   • Se medir 0 Ohms: Os fios CAN High e CAN Low estão em curto um com o outro!",
                        "2. TESTE DE TENSÃO DA REDE CAN (Chave LIGADA e Módulos Ativos):",
                        "   • Multímetro na escala Volts DC com ponta preta no Pino 4 (Terra do Chassi).",
                        "   • Pino 6 (CAN High): Deve medir entre 2.5V e 3.5V (tensão média em repouso ~2.7V).",
                        "   • Pino 14 (CAN Low): Deve medir entre 1.5V e 2.5V (tensão média em repouso ~2.3V).",
                        "   • REGRA DE OURO: A soma de CAN-High + CAN-Low deve ser aproximadamente 5.0V!",
                        "3. REDE LIN (Comunicação Monofio Local):",
                        "   • Não vai ao conector OBD principal (liga módulos escravos como alternador, BCM, sensor IBS).",
                        "   • Medição no multímetro: Tensão oscila entre 9V e 11V DC. No osciloscópio: onda quadrada de 0V a 12V dominante/recessivo a 19.2 kbps."
                    },
                    SuggestedTools = new List<string> { "Multímetro Digital Automotivo", "Osciloscópio 2 Canais", "Caixa de Ruptura OBD Breakout Box" },
                    SuggestedStockParts = new List<string> { "Conector OBD-II Fêmea", "Módulo de Carroceria BCM", "Central de Injeção ECU" },
                    ReferenceStandard = "Resistência CAN 6-14: 60Ω (±3Ω) | Tensão CAN-H: 2.5V a 3.5V | Tensão CAN-L: 1.5V a 2.5V | Linha LIN: 9V a 11V DC",
                    ClarifyingQuestions = new List<string> { "Qual valor em Ohms você mediu entre os pinos 6 e 14 do OBD com a chave desligada?", "Qual a tensão medida no pino 6 e no pino 14 com a chave ligada?", "O scanner consegue comunicar com algum módulo ou dá falha geral?" },
                    InteractiveReplyChips = new List<string> { "Mede 60 Ohms normal", "Mede 120 Ohms", "Mede 0 Ohms em curto", "Nenhum módulo comunica" }
                },
                new DiagnosticEntry
                {
                    Code = "BATERIA_START_STOP_AGM_EFB",
                    Title = "Compatibilidade de Baterias em Veículos com Start-Stop: AGM vs EFB vs Convencional (SLI)",
                    System = "Sistema de Partida, Bateria e Gestão de Energia",
                    Symptoms = "Dúvida se pode instalar bateria convencional de chumbo-ácido em veículo com Start-Stop, bateria convencional que descarregou em 2 meses, mensagem 'Start-Stop Indisponível' no painel.",
                    ProbableCauses = new List<string>
                    {
                        "Instalação indevida de bateria convencional SLI em veículo com Start-Stop (não suporta ciclos contínuos de descarga)",
                        "Bateria convencional ferve por não suportar as correntes elevadas de recarga regenerativa (até 15.2V)",
                        "Troca da bateria sem realizar o reset de calibração do sensor IBS no scanner automotivo",
                        "Aplicação de bateria EFB em veículo de alto consumo elétrico que exige bateria AGM de fábrica"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. POSSO COLOCAR BATERIA COMUM EM CARRO COM START-STOP? NUNCA!",
                        "   • Uma bateria convencional suporta cerca de 30.000 partidas ao longo da vida e descargas superficiais.",
                        "   • Um carro com Start-Stop realiza de 300.000 a 500.000 partidas e sofre descargas profundas no trânsito.",
                        "   • Resultado da bateria comum: Ela sulfata e morre em 60 a 90 dias, o eletrólito ferve e a central BCM desativa o Start-Stop preventivamente!",
                        "2. DIFERENÇA ENTRE EFB E AGM:",
                        "   • EFB (Enhanced Flooded Battery): Bateria de eletrólito líquido com placas reforçadas com manta de poliéster. Indicada para veículos nacionais compactos e médios com Start-Stop simples (ex: Argo, Cronos, Renegade 1.8, Polo, Onix Plus).",
                        "   • AGM (Absorbent Glass Mat): O eletrólito é 100% absorvido em mantas de microfibra de vidro. Suporta correntes brutais de carga e descarga e frenagem regenerativa pesada. Indicada para veículos premium, SUVs e motores turbo com Start-Stop avançado (ex: Compass Diesel, BMW, Audi, Mercedes, Tiguan).",
                        "3. PROCEDIMENTO OBRIGATÓRIO DE INSTALAÇÃO:",
                        "   • Instale bateria de mesma tecnologia (se era AGM, coloque AGM; se era EFB, coloque EFB ou AGM).",
                        "   • Conecte o scanner automotivo e entre no módulo BCM / Injeção.",
                        "   • Execute o procedimento de 'Reset / Aprendizado de Troca de Bateria' para zerar o mapa de envelhecimento do sensor IBS."
                    },
                    SuggestedTools = new List<string> { "Testador Eletrônico de Bateria Condutância CCA", "Scanner com Função Reset de Bateria / IBS", "Chave 10mm" },
                    SuggestedStockParts = new List<string> { "Bateria Moura EFB 60Ah / 72Ah", "Bateria Heliar AGM 70Ah / 80Ah", "Sensor de Bateria IBS" },
                    ReferenceStandard = "Start-Stop Simples: Mínimo EFB | Regeneração Avançada: Obrigatório AGM | Tensão de repouso nova: > 12.60V",
                    ClarifyingQuestions = new List<string> { "Qual é o veículo e modelo exato?", "A bateria original do carro era EFB ou AGM?", "Você tem scanner para fazer o reset da bateria no sensor IBS?" },
                    InteractiveReplyChips = new List<string> { "O carro é um Renegade / Toro", "A bateria original era EFB", "A bateria original era AGM", "Tenho scanner para resetar" }
                }
            };
        }

        private static List<SystemManualEntry> CarregarBaseManuaisSistema()
        {
            return new List<SystemManualEntry>
            {
                new SystemManualEntry
                {
                    Key = "ORDEM_SERVICO",
                    Title = "Manual: Como Abrir, Preencher e Finalizar Ordens de Serviço (OS)",
                    ModuleNavigationTarget = "OrdensServico",
                    Summary = "Controle de serviços da oficina: cadastro da queixa do cliente, adição de peças e mão de obra, técnico responsável e emissão de comprovante.",
                    Instructions = new List<string>
                    {
                        "1. Acesse o menu lateral 'Ordens de Serviço' ou utilize o atalho de busca rápida Ctrl+K e digite 'OS'.",
                        "2. Clique no botão '+ Nova OS' no cabeçalho da página.",
                        "3. Selecione o Cliente e o Veículo atendido (ou cadastre-os instantaneamente clicando no botão ao lado).",
                        "4. Digite a 'Queixa do Cliente / Sintoma Relatado' (ex: 'Alternador não carrega, bateria arriada').",
                        "5. Na aba 'Peças', selecione os itens do estoque usados no conserto com quantidade e valor.",
                        "6. Na aba 'Serviços', adicione as horas ou tipos de mão de obra elétrica.",
                        "7. Selecione o Técnico/Eletricista responsável pelo serviço e defina o prazo de entrega.",
                        "8. Altere o status conforme o trabalho avança: 'Em Execução' -> 'Testes' -> 'Concluída'.",
                        "9. Clique em 'Salvar' e use o botão 'Imprimir OS' para emitir o termo de garantia para o cliente."
                    },
                    Tips = new List<string>
                    {
                        "Você também pode acompanhar e mover o status das suas Ordens de Serviço diretamente pela tela 'Oficina Kanban' arrastando os cartões de serviço!"
                    },
                    Keywords = new List<string> { "ordem de serviço", "ordem de servico", "os", "abrir os", "criar os", "nova os", "imprimir os", "fechar os", "servico" }
                },
                new SystemManualEntry
                {
                    Key = "ORCAMENTOS",
                    Title = "Manual: Como Criar e Converter Orçamentos em OS",
                    ModuleNavigationTarget = "Orcamentos",
                    Summary = "Elaboração de propostas comerciais de peças e mão de obra com conversão automática em Ordem de Serviço com 1 clique.",
                    Instructions = new List<string>
                    {
                        "1. Clique em 'Orçamentos' no menu lateral esquerdo.",
                        "2. Clique no botão '+ Novo Orçamento'.",
                        "3. Escolha o cliente, veículo e liste todas as peças e serviços técnicos necessários.",
                        "4. O PRIMOX calcula o valor bruto, permite aplicar descontos percentuais ou em reais e exibe o valor líquido.",
                        "5. Imprima o orçamento ou envie para aprovação do cliente via WhatsApp.",
                        "6. Quando o cliente aprovar: clique no botão 'Converter em OS'! O sistema criará a Ordem de Serviço automaticamente, sem que você precise redigitar nada."
                    },
                    Tips = new List<string>
                    {
                        "Defina o prazo de validade do orçamento para proteger a oficina de oscilações no custo de peças de reposição."
                    },
                    Keywords = new List<string> { "orcamento", "orçamento", "criar orcamento", "fazer orcamento", "converter orcamento", "proposta", "preco" }
                },
                new SystemManualEntry
                {
                    Key = "OFICINA_KANBAN",
                    Title = "Manual: Como Usar o Quadro Visual Oficina Kanban",
                    ModuleNavigationTarget = "OficinaKanban",
                    Summary = "Acompanhamento visual do fluxo de trabalho na oficina em tempo real inspirado na metodologia ágil.",
                    Instructions = new List<string>
                    {
                        "1. Clique em 'Oficina Kanban' no menu lateral.",
                        "2. Visualize as colunas de fluxo: 'Aguardando Diagnóstico', 'Aguardando Peças', 'Em Execução', 'Testes Finais' e 'Pronto'.",
                        "3. Cada cartão exibe o veículo, placa, cliente, responsável e tempo de permanência.",
                        "4. Para atualizar a etapa de um veículo, basta arrastar o cartão para a coluna seguinte!",
                        "5. Clique duas vezes em qualquer cartão para abrir a edição completa da OS."
                    },
                    Tips = new List<string>
                    {
                        "Mantenha essa tela aberta em uma TV ou monitor na recepção ou na bancada para que toda a oficina saiba o status das prioridades."
                    },
                    Keywords = new List<string> { "kanban", "quadro", "cartao", "colunas", "fluxo de trabalho", "arrastar", "andamento" }
                },
                new SystemManualEntry
                {
                    Key = "PDV_CAIXA",
                    Title = "Manual: Frente de Caixa (PDV) e Fechamento Diário",
                    ModuleNavigationTarget = "PDV",
                    Summary = "Venda ágil de autopeças no balcão, recebimento de pagamentos (Pix, Cartão, Dinheiro) e controle de caixa.",
                    Instructions = new List<string>
                    {
                        "1. Acesse o módulo 'PDV' no menu lateral.",
                        "2. Digite o código de barras, código interno ou nome da peça e pressione Enter para adicionar ao carrinho.",
                        "3. Pressione a tecla de atalho F2 para finalizar a venda.",
                        "4. Escolha a forma de pagamento: Dinheiro, Pix, Cartão de Débito, Cartão de Crédito ou A Prazo.",
                        "5. Informe o valor recebido e confirme. O sistema calcula o troco e emite o comprovante térmico (80mm/58mm).",
                        "6. No início do dia utilize 'Abertura de Caixa' com o valor do fundo de troco.",
                        "7. No fim do expediente, clique em 'Fechar Caixa' para conciliar as receitas do dia."
                    },
                    Tips = new List<string>
                    {
                        "Todas as vendas efetuadas no PDV baixam o estoque automaticamente e alimentam o fluxo de caixa no Financeiro."
                    },
                    Keywords = new List<string> { "pdv", "caixa", "frente de caixa", "venda", "fechar caixa", "abrir caixa", "troco", "balcao", "pix", "dinheiro" }
                },
                new SystemManualEntry
                {
                    Key = "ESTOQUE",
                    Title = "Manual: Controle de Estoque de Autopeças e Saldo Mínimo",
                    ModuleNavigationTarget = "Estoque",
                    Summary = "Cadastro de peças, código original, controle de saldo, localização em prateleiras, custos e margem de lucro.",
                    Instructions = new List<string>
                    {
                        "1. Clique em 'Estoque' no menu lateral.",
                        "2. Clique em '+ Novo Produto' para cadastrar uma nova peça.",
                        "3. Preencha Nome, Código Fabricante / Código Original, Categoria (Alternador, Arranque, Baterias, Lâmpadas, etc.).",
                        "4. Informe o Preço de Custo, Margem de Lucro (%) e o sistema calcula o Preço de Venda.",
                        "5. Configure o 'Estoque Mínimo' para que o sistema avise automaticamente quando for hora de repor.",
                        "6. Defina a 'Localização' (ex: Gaveta B-04) para agilizar o atendimento no balcão."
                    },
                    Tips = new List<string>
                    {
                        "Você pode importar notas fiscais de fornecedores em XML na tela 'Importar NF-e' para alimentar o estoque automaticamente sem digitação manual!"
                    },
                    Keywords = new List<string> { "estoque", "produto", "peca", "cadastrar peca", "saldo", "custo", "margem", "prateleira", "reposicao" }
                },
                new SystemManualEntry
                {
                    Key = "FERRAMENTARIA",
                    Title = "Manual: Controle de Ferramental Especializado e Empréstimos",
                    ModuleNavigationTarget = "Ferramentas",
                    Summary = "Rastreamento patrimonial de scanners, osciloscópios, alicates e empréstimo com devolução para técnicos.",
                    Instructions = new List<string>
                    {
                        "1. Acesse o menu 'Ferramentaria' na barra lateral.",
                        "2. Visualize a lista de equipamentos especializados, código patrimonial e status (Disponível, Em Uso, Manutenção).",
                        "3. Para registrar um empréstimo: selecione a ferramenta, clique no botão 'Emprestar / Devolver', selecione o funcionário e confirme.",
                        "4. A ferramenta passa para o status 'Em Uso', registrando data e hora exatas do empréstimo.",
                        "5. Para devolver: abra o diálogo novamente e registre a devolução com um clique.",
                        "6. Você pode perguntar para mim no Copilot a qualquer momento: 'Quem está com ferramentas em uso?'."
                    },
                    Tips = new List<string>
                    {
                        "Evite perdas e extravios de equipamentos caros registrando cada saída da bancada técnica."
                    },
                    Keywords = new List<string> { "ferramenta", "ferramentas", "ferramentaria", "scanner", "osciloscopio", "emprestar", "devolver", "em uso", "patrimonio" }
                },
                new SystemManualEntry
                {
                    Key = "IMPORTAR_NFE",
                    Title = "Manual: Importação de Notas Fiscais (NF-e) via Arquivo XML",
                    ModuleNavigationTarget = "ImportarNFe",
                    Summary = "Entrada rápida de mercadorias no estoque e financeiro a partir dos arquivos XML fornecidos pelos distribuidores.",
                    Instructions = new List<string>
                    {
                        "1. Clique em 'Importar NF-e' no menu lateral.",
                        "2. Clique no botão 'Selecionar Arquivo XML' ou arraste o arquivo .xml da nota fiscal recebida.",
                        "3. O sistema analisa os itens da nota, códigos de barras, NCM, fornecedor e valores tributários.",
                        "4. Faça a conciliação: vincule cada item da nota com um produto já existente no seu estoque ou clique para criar novo.",
                        "5. Clique em 'Concluir Importação'.",
                        "6. O saldo de estoque e as contas a pagar no Financeiro são atualizados automaticamente!"
                    },
                    Tips = new List<string>
                    {
                        "Economiza horas de digitação e evita erros em códigos e custos de reposição."
                    },
                    Keywords = new List<string> { "importar nfe", "nfe", "xml", "nota fiscal", "distribuidora", "entrada de nota", "fornecedor" }
                },
                new SystemManualEntry
                {
                    Key = "COMPRAS_FALTA",
                    Title = "Manual: Gestão de Compras e Anti-Ruptura de Estoque",
                    ModuleNavigationTarget = "ComprasNecessidade",
                    Summary = "Geração de lista de pedidos de compras com base no consumo real e produtos abaixo do estoque mínimo.",
                    Instructions = new List<string>
                    {
                        "1. Acesse 'Compras & Falta' no menu lateral.",
                        "2. O painel lista todas as peças que atingiram o limite mínimo ou estão zeradas.",
                        "3. O sistema sugere a quantidade ideal de compra para os próximos 15 a 30 dias.",
                        "4. Você pode agrupar os pedidos por Fornecedor e exportar a lista para cotação."
                    },
                    Tips = new List<string>
                    {
                        "Pergunte ao Copilot: 'Quais produtos estão em falta?' para uma resposta instantânea!"
                    },
                    Keywords = new List<string> { "compras", "falta", "anti ruptura", "comprar", "pedido de compra", "repor pecas" }
                },
                new SystemManualEntry
                {
                    Key = "CLIENTES_VEICULOS",
                    Title = "Manual: Cadastro de Clientes e Prontuário de Veículos",
                    ModuleNavigationTarget = "Clientes",
                    Summary = "Histórico completo de clientes com WhatsApp, CPF/CNPJ e associação com veículos por placa e chassi.",
                    Instructions = new List<string>
                    {
                        "1. Acesse 'Clientes' ou 'Veículos' no menu lateral.",
                        "2. Clique em '+ Novo' para adicionar um registro.",
                        "3. No cliente: informe Nome Completo, WhatsApp (para envio de orçamentos e aviso de OS pronta) e CPF.",
                        "4. No veículo: cadastre Placa, Marca, Modelo, Ano, Motorização e Chassi.",
                        "5. Todo o histórico de serviços, manutenções e peças já aplicadas naquele veículo fica salvo permanentemente no Prontuário!"
                    },
                    Tips = new List<string>
                    {
                        "Cadastre o WhatsApp correto para que os relatórios e orçamentos possam ser compartilhados facilmente."
                    },
                    Keywords = new List<string> { "cliente", "veiculo", "cadastrar cliente", "placa", "chassi", "whatsapp", "prontuario" }
                },
                new SystemManualEntry
                {
                    Key = "CONFIGURACOES",
                    Title = "Manual: Configurações Gerais, Chave de IA e Preferências",
                    ModuleNavigationTarget = "Configuracoes",
                    Summary = "Personalização da oficina, dados cadastrais, logotipo para impressões e configuração da Inteligência Artificial.",
                    Instructions = new List<string>
                    {
                        "1. Acesse 'Configurações' no menu lateral (ícone de engrenagem).",
                        "2. Aba 'Oficina / Empresa': defina Nome Fantasia, CNPJ, Endereço, Telefones e faça upload do Logo.",
                        "3. Aba 'Inteligência Artificial': escolha entre o motor local 'Offline Técnico' (gratuito) ou insira sua chave gratuita do 'Google Gemini'.",
                        "4. Para usar a IA generativa do Gemini de graça: acesse aistudio.google.com, gere sua chave gratuita (sem cartão) e cole no campo API Key.",
                        "5. Clique em 'Testar Conexão' e depois em 'Salvar'."
                    },
                    Tips = new List<string>
                    {
                        "O PRIMOX nunca para: mesmo sem internet ou sem chave de API, o motor especialista local offline responde a todas as suas dúvidas técnicas e operacionais."
                    },
                    Keywords = new List<string> { "configuracoes", "configuracao", "empresa", "cnpj", "logo", "chave de ia", "api key", "gemini", "tema" }
                },
                new SystemManualEntry
                {
                    Key = "AUTO_ELETRICA_TECNICA",
                    Title = "Manual: Módulo Auto Elétrica Técnica e Prontuário",
                    ModuleNavigationTarget = "AutoEletricaTecnica",
                    Summary = "Prontuário elétrico veicular, diagnósticos guiados, roteiros de testes e biblioteca técnica.",
                    Instructions = new List<string>
                    {
                        "1. Clique em 'Auto Elétrica Técnica' no menu lateral.",
                        "2. Consulte o 'Prontuário Elétrico do Veículo' para ver especificações do alternador, bateria, relés e chicote.",
                        "3. Na coluna central, selecione um 'Roteiro de Diagnóstico Guiado' (ex: Queda de Tensão, Teste de Relé, Alternador Pilotado).",
                        "4. O sistema exibe o passo a passo dos testes e os valores nominais esperados em Volts e Amperes.",
                        "5. Clique em 'Gerar Orçamento' para levar as peças e serviços sugeridos direto para a área comercial."
                    },
                    Tips = new List<string>
                    {
                        "Você pode abrir o assistente Copilot IA diretamente a partir deste módulo clicando no botão '✨ Copilot IA' no topo!"
                    },
                    Keywords = new List<string> { "tecnica", "auto eletrica tecnica", "prontuario", "roteiro", "diagnostico guiado", "esquema" }
                },
                new SystemManualEntry
                {
                    Key = "FINANCEIRO_FLUXO_CAIXA",
                    Title = "Manual: Gestão Financeira, Contas a Pagar/Receber e Fluxo de Caixa",
                    ModuleNavigationTarget = "Financeiro",
                    Summary = "Controle das finanças da oficina: contas a pagar, contas a receber, conciliação bancária, despesas fixas e DRE.",
                    Instructions = new List<string>
                    {
                        "1. Acesse o menu lateral 'Financeiro'.",
                        "2. Na aba 'Contas a Receber': acompanhe os recebimentos de OS faturadas, vendas no PDV, Pix e cartões.",
                        "3. Na aba 'Contas a Pagar': cadastre boletos de distribuidores de autopeças, contas de energia, aluguel e salários.",
                        "4. Clique em '+ Nova Despesa' para lançar pagamentos com data de vencimento, categoria e fornecedor.",
                        "5. Ao quitar uma conta: selecione a linha e clique em 'Baixar / Liquidar'.",
                        "6. Na aba 'Fluxo de Caixa': visualize o gráfico diário e mensal de entradas versus saídas com saldo projetado."
                    },
                    Tips = new List<string>
                    {
                        "Mantenha os custos de peças conciliados para que a DRE mostre a lucratividade real de cada serviço prestado."
                    },
                    Keywords = new List<string> { "financeiro", "contas a pagar", "contas a receber", "fluxo de caixa", "dre", "despesas", "receitas", "boleto", "pagamento" }
                },
                new SystemManualEntry
                {
                    Key = "RELATORIOS_INDICADORES",
                    Title = "Manual: Relatórios Gerenciais, Faturamento e Produtividade",
                    ModuleNavigationTarget = "Relatorios",
                    Summary = "Análise estratégica da oficina: faturamento por período, peças mais vendidas, ticket médio e produtividade por eletricista.",
                    Instructions = new List<string>
                    {
                        "1. Clique em 'Relatórios' no menu lateral.",
                        "2. Selecione o tipo de relatório desejado: 'Faturamento por Período', 'Produtividade de Mecânicos/Eletricistas', 'Curva ABC de Peças' ou 'Serviços Mais Realizados'.",
                        "3. Defina o período de datas inicial e final no filtro superior.",
                        "4. O sistema gera a visualização em grade e gráficos de barras/pizza interativos.",
                        "5. Clique em 'Exportar PDF' ou 'Exportar Excel' para compartilhar os relatórios com os sócios ou contador."
                    },
                    Tips = new List<string>
                    {
                        "Acompanhe a produtividade por técnico para calcular premiações e comissões sobre a mão de obra aplicada."
                    },
                    Keywords = new List<string> { "relatorio", "relatorios", "faturamento", "produtividade", "comissao", "curva abc", "indicadores", "ticket medio", "exportar pdf" }
                },
                new SystemManualEntry
                {
                    Key = "CENTRAL_FISCAL",
                    Title = "Manual: Central Fiscal - Emissão de NFS-e (Serviços) e NFC-e (Peças)",
                    ModuleNavigationTarget = "FiscalOperacoes",
                    Summary = "Emissão eletrônica de documentos fiscais integrados com certificado digital A1: NFS-e para a prefeitura e NFC-e/NF-e para a SEFAZ.",
                    Instructions = new List<string>
                    {
                        "1. Acesse 'Central Fiscal' no menu lateral (ou configure em 'Configurações → Parâmetros Fiscais').",
                        "2. Certificado Digital: carregue seu arquivo A1 (.pfx) com a senha para validação jurídica.",
                        "3. Para emitir NFS-e de Mão de Obra: ao concluir a OS, selecione 'Emitir NFS-e'. O PRIMOX assina o XML e transmite à Prefeitura automaticamente.",
                        "4. Para emitir NFC-e de Peças: no fechamento da OS ou no balcão PDV, selecione 'Emitir Cupom Fiscal (NFC-e)'. O cupom com QR Code é impresso na impressora de 80mm/58mm.",
                        "5. Em caso de queda de internet: o sistema entra em 'Contingência Offline' e retransmite as notas pendentes assim que a conexão retornar."
                    },
                    Tips = new List<string>
                    {
                        "A divisão automática entre NFS-e (mão de obra - tributação municipal ISS) e NFC-e (peças - ICMS estadual) garante economia tributária legal para a oficina."
                    },
                    Keywords = new List<string> { "fiscal", "central fiscal", "nfse", "nfc-e", "nfe", "nota fiscal", "certificado digital", "a1", "sefaz", "iss", "cupom fiscal" }
                },
                new SystemManualEntry
                {
                    Key = "GESTAO_FROTAS",
                    Title = "Manual: Gestão de Frotas B2B e Faturamento Periódico",
                    ModuleNavigationTarget = "GestaoFrotas",
                    Summary = "Contratos corporativos com locadoras e transportadoras, telemetria de KM por hodômetro, tabela de desconto corporativo e faturamento quinzenal/mensal.",
                    Instructions = new List<string>
                    {
                        "1. Acesse 'Gestão de Frotas' no menu lateral.",
                        "2. Aba 'Contratos': cadastre a empresa cliente, defina % de desconto em peças e mão de obra, limite de crédito e ciclo de fechamento (quinzenal ou mensal).",
                        "3. Vincule as placas dos utilitários pertencentes àquela empresa e seus motoristas.",
                        "4. No atendimento da oficina: ao selecionar a placa de frota na OS, o desconto do contrato é aplicado automaticamente e a digitação da quilometragem (KM) é obrigatória.",
                        "5. Fechamento Periódico: acesse 'Faturamento Periódico', filtre o mês e clique em 'Gerar Faturamento Consolidado'.",
                        "6. O PRIMOX gera os títulos a receber no Financeiro e emite o 'Romaneio Analítico de Frota' em PDF com detalhamento por placa."
                    },
                    Tips = new List<string>
                    {
                        "O Romaneio Analítico em PDF pode ser anexado diretamente no e-mail de cobrança para aprovação imediata do departamento de frotas da empresa parceira."
                    },
                    Keywords = new List<string> { "frota", "frotas", "b2b", "contrato corporativo", "transportadora", "locadora", "faturamento agrupado", "romaneio", "km", "desconto corporativo" }
                },
                new SystemManualEntry
                {
                    Key = "MULTI_FILIAIS",
                    Title = "Manual: Multi-Filiais Corporativo e Transferências de Estoque Inter-Lojas",
                    ModuleNavigationTarget = "MultiFiliais",
                    Summary = "Operação de rede de oficinas com Matriz e Filiais, consolidação em tempo real e transferências seguras de peças entre unidades.",
                    Instructions = new List<string>
                    {
                        "1. Acesse 'Configurações → Multi-Filiais' para cadastrar Matriz e unidades Filiais com seus respectivos CNPJs e endereços.",
                        "2. Para alternar de unidade: clique no seletor de filial no topo da tela do sistema. O estoque, OS e caixa mudam instantaneamente para a unidade ativa.",
                        "3. Solicitar Transferência: acesse 'Transferências → Nova Transferência', escolha Origem e Destino, adicione as peças e clique em 'Despachar Transferência'.",
                        "4. As peças saem do saldo disponível e ficam com status 'Em Trânsito'.",
                        "5. Recebimento na Filial: na filial de destino, acesse 'Transferências Pendentes', confira as quantidades físicas e clique em 'Confirmar Recebimento'.",
                        "6. O estoque de destino é creditado com consistência transacional atômica (ACID)."
                    },
                    Tips = new List<string>
                    {
                        "Gerentes e administradores gerais podem visualizar o faturamento e estoque consolidado de todas as unidades pelo Dashboard Geral."
                    },
                    Keywords = new List<string> { "filial", "filiais", "multi filiais", "matriz", "transferencia de pecas", "inter lojas", "em transito", "rede de oficinas" }
                },
                new SystemManualEntry
                {
                    Key = "ETIQUETAS_TERMICAS",
                    Title = "Manual: Impressão Térmica de Etiquetas ZPL II (Zebra, Argox, Elgin)",
                    ModuleNavigationTarget = "EtiquetasTermicas",
                    Summary = "Impressão industrial de etiquetas para chaves de veículos na recepção, gôndolas de autopeças e patrimônio de ferramentas.",
                    Instructions = new List<string>
                    {
                        "1. Configuração da Impressora: acesse 'Configurações → Impressoras Térmicas' e configure o modelo (Zebra ZD220/GK420t, Argox OS-214 ou Elgin L42 Pro) via USB ou IP de Rede.",
                        "2. Etiqueta de Chave (Recepção): ao abrir a OS ou Orçamento, clique em 'Imprimir Etiqueta de Chave'. Ela sai com Placa, Cliente, Nº da OS e Código de Barras para pendurar no chaveiro.",
                        "3. Etiqueta de Prateleira (Estoque): no módulo Estoque, selecione o produto e clique em 'Imprimir Etiqueta de Gôndola' com localização e código de barras Code 128 resistente a óleo.",
                        "4. Etiqueta de Ativo (Ferramentaria): identifique scanners e ferramentas caras com QR Code patrimonial.",
                        "5. A impressão é ultra-rápida (menos de 1 segundo) via comandos nativos ZPL II puros."
                    },
                    Tips = new List<string>
                    {
                        "A etiqueta de chave do veículo evita confusão de chaves no pátio e agiliza a localização do carro pelos técnicos."
                    },
                    Keywords = new List<string> { "etiqueta", "etiquetas", "zpl", "zebra", "argox", "elgin", "impressora termica", "chave", "etiqueta de chave", "gondola", "prateleira", "codigo de barras" }
                },
                new SystemManualEntry
                {
                    Key = "RECIBO_TERMO_GARANTIA",
                    Title = "Manual: Emissão de Recibo, Comprovante e Termo de Garantia da Oficina",
                    ModuleNavigationTarget = "OrdensServico",
                    Summary = "Como emitir comprovantes de pagamento e termos de garantia legal (90 dias - Art. 26 CDC) e estendida para serviços elétricos e peças trocadas.",
                    Instructions = new List<string>
                    {
                        "1. GARANTIA LEGAL DE 90 DIAS (CDC): Todo serviço prestado e peças instaladas possuem garantia legal obrigatória de 90 dias conforme Artigo 26 do Código de Defesa do Consumidor.",
                        "2. EMISSÃO DO TERMO: Acesse 'Ordens de Serviço' no menu lateral e localize a OS com status 'Finalizada' ou 'Concluída'.",
                        "3. Na barra de ações superior da OS, clique em 'Imprimir Comprovante / Termo de Garantia'.",
                        "4. O PRIMOX gera automaticamente o documento padronizado contendo dados do veículo, placa, quilometragem na entrega, peças instaladas com prazo de garantia individual (3 a 12 meses) e mão de obra executada com termo de responsabilidade.",
                        "5. Clique em 'Imprimir (A4 ou Térmica 80mm)' ou use 'Enviar via WhatsApp' em formato PDF assinado digitalmente.",
                        "6. RETORNO DE GARANTIA: Se o cliente retornar dentro do prazo de 90 dias com o mesmo defeito, clique em '+ Retorno em Garantia' vinculado à OS original. O sistema não cobrará o cliente novamente e registrará o custo interno para a oficina."
                    },
                    Tips = new List<string>
                    {
                        "Entregar o termo de garantia impresso com o prazo de 90 dias transmite máxima credibilidade e protege juridicamente a oficina contra cobranças indevidas de serviços não realizados."
                    },
                    Keywords = new List<string> { "recibo", "garantia", "garantia legal", "90 dias", "cdc", "garantia de servico", "termo de garantia", "comprovante", "entrega do veiculo", "imprimir garantia", "retorno em garantia" }
                },
                new SystemManualEntry
                {
                    Key = "COMISSOES_ELETRICISTAS",
                    Title = "Manual: Gestão e Relatório de Comissões dos Eletricistas e Mecânicos",
                    ModuleNavigationTarget = "Relatorios",
                    Summary = "Como configurar percentuais de comissão por técnico e emitir relatórios de fechamento de pagamento de serviços.",
                    Instructions = new List<string>
                    {
                        "1. CONFIGURAÇÃO DE PORCENTAGEM: Acesse 'Configurações → Usuários e Técnicos' e edite o cadastro do eletricista/mecânico.",
                        "2. No campo 'Comissão de Serviços (%)', informe o percentual acordado (ex: 30%, 40% ou 50% sobre o valor da mão de obra líquida).",
                        "3. VÍNCULO NA ORDEM DE SERVIÇO: Ao abrir ou finalizar a OS, selecione o técnico responsável por cada serviço executado.",
                        "4. CONSULTA E FECHAMENTO: Acesse o menu lateral 'Relatórios → Produtividade & Comissões' ou utilize o atalho Ctrl+K e digite 'Comissões'.",
                        "5. Selecione o período desejado (ex: quinzenal ou mensal) e filtre por técnico.",
                        "6. O sistema exibe o espelho completo com o número de cada OS concluída, serviços realizados, valor total de mão de obra e a comissão líquida a pagar.",
                        "7. Clique em 'Imprimir Espelho de Comissão' ou exporte para PDF para conferência e assinatura do colaborador."
                    },
                    Tips = new List<string>
                    {
                        "As comissões são contabilizadas apenas em Ordens de Serviço com status 'Concluída' ou 'Finalizada' para garantir que serviços em aberto não sejam pagos antecipadamente."
                    },
                    Keywords = new List<string> { "comissao", "comissoes", "comissao de eletricista", "produtividade", "pagar mecanico", "relatorio de comissao", "comissao tecnico", "fechamento de comissao" }
                },
                new SystemManualEntry
                {
                    Key = "HISTORICO_VEICULO_PLACA",
                    Title = "Manual: Consulta de Histórico Completo de Manutenções por Placa do Veículo",
                    ModuleNavigationTarget = "Veiculos",
                    Summary = "Como rastrear todo o histórico de manutenções, quilometragens, peças substituídas e serviços anteriores de qualquer carro pela placa.",
                    Instructions = new List<string>
                    {
                        "1. BUSCA RÁPIDA: Pressione o atalho global Ctrl+K e digite a placa do carro (ex: BRA2E19 ou ABC-1234) ou acesse o menu lateral 'Veículos'.",
                        "2. Na listagem de veículos, localize o automóvel e clique em 'Ver Ficha Completa' ou duplo-clique sobre a linha.",
                        "3. Clique na aba 'Histórico de Manutenções / OS Anteriores'.",
                        "4. O PRIMOX exibirá a linha do tempo cronológica com todas as visitas do veículo à oficina.",
                        "5. Em cada visita você pode conferir: Data do atendimento, Quilometragem registrada, Sintoma relatado pelo cliente, Peças trocadas com número de série/garantia e Técnico que realizou o reparo.",
                        "6. Clique sobre qualquer OS da lista para visualizar os detalhes completos ou reimprimir a segunda via da Ordem de Serviço."
                    },
                    Tips = new List<string>
                    {
                        "Consultar o histórico pela placa antes de iniciar um novo diagnóstico ajuda a verificar se o defeito atual tem relação com serviços realizados anteriormente ou se peças ainda estão na garantia!"
                    },
                    Keywords = new List<string> { "historico", "historico do veiculo", "historico pela placa", "consultar placa", "passado do carro", "os anteriores", "manutencoes anteriores", "placa", "busca por placa" }
                },
                new SystemManualEntry
                {
                    Key = "CADASTRO_CLIENTE_VEICULO",
                    Title = "Manual: Cadastro de Clientes e Veículos (Placa Mercosul e Tradicional)",
                    ModuleNavigationTarget = "Clientes",
                    Summary = "Cadastro ágil de clientes e frotistas com vínculo aos seus respectivos veículos para abertura instantânea de ordens de serviço.",
                    Instructions = new List<string>
                    {
                        "1. Acesse o menu lateral 'Clientes' ou pressione Ctrl+K e digite 'Clientes'.",
                        "2. Clique em '+ Novo Cliente' e preencha Nome/Razão Social, CPF/CNPJ e Telefone WhatsApp (essencial para envio de orçamentos e avisos).",
                        "3. Na aba 'Veículos do Cliente', clique em '+ Adicionar Veículo'.",
                        "4. Digite a Placa (formato Mercosul ABC1D23 ou tradicional ABC-1234), Marca, Modelo, Ano e Cor.",
                        "5. Clique em 'Salvar'. O veículo ficará permanentemente vinculado ao histórico daquele cliente para qualquer OS futura."
                    },
                    Tips = new List<string>
                    {
                        "O número de telefone com DDD permite enviar mensagens com 1 clique direto pelo sistema."
                    },
                    Keywords = new List<string> { "cadastrar cliente", "novo cliente", "cadastrar veiculo", "placa", "mercosul", "adicionar carro", "proprietario" }
                },
                new SystemManualEntry
                {
                    Key = "BACKUP_SISTEMA_DADOS",
                    Title = "Manual: Backup Automático e Segurança dos Dados da Oficina",
                    ModuleNavigationTarget = "Configuracoes",
                    Summary = "Procedimento de proteção, exportação e restauração do banco de dados da oficina para evitar perda de informações.",
                    Instructions = new List<string>
                    {
                        "1. Acesse o menu 'Configurações → Banco de Dados e Backups'.",
                        "2. O sistema realiza backups automáticos diários em segundo plano criptografados.",
                        "3. Para gerar um backup imediato: clique no botão 'Gerar Cópia de Segurança Agora (.backup)'.",
                        "4. Selecione uma pasta segura (ex: Pendrive, HD Externo ou pasta sincronizada no Google Drive/OneDrive).",
                        "5. Para restaurar: use o botão 'Restaurar Backup' e aponte para o arquivo desejado após confirmar a senha de administrador."
                    },
                    Tips = new List<string>
                    {
                        "Recomenda-se manter sempre uma cópia do backup em um pendrive fora da oficina para proteção contra danos físicos ou raios."
                    },
                    Keywords = new List<string> { "backup", "fazer backup", "restaurar", "copia de seguranca", "salvar banco", "seguranca de dados" }
                },
                new SystemManualEntry
                {
                    Key = "CONFIGURACOES_OFICINA_LOGO",
                    Title = "Manual: Configuração dos Dados da Oficina e Logotipo nos Documentos",
                    ModuleNavigationTarget = "Configuracoes",
                    Summary = "Personalização de cabeçalhos de ordens de serviço, orçamentos e recibos com logotipo, CNPJ, endereço e redes sociais da oficina.",
                    Instructions = new List<string>
                    {
                        "1. Clique em 'Configurações' no menu lateral e acesse a aba 'Dados da Empresa'.",
                        "2. Preencha Nome Fantasia, Razão Social, CNPJ, Inscrição Estadual, Endereço Completo e Telefones de Contato.",
                        "3. No campo 'Logotipo da Oficina', clique em 'Selecionar Imagem' e escolha um arquivo PNG ou JPEG.",
                        "4. Clique em 'Salvar Configurações'.",
                        "5. Todos os orçamentos, ordens de serviço impressas e notas fiscais geradas passarão a exibir o seu logotipo oficial no cabeçalho."
                    },
                    Tips = new List<string>
                    {
                        "Utilize uma imagem com fundo transparente em alta resolução para uma apresentação visual profissional nos orçamentos."
                    },
                    Keywords = new List<string> { "configurar oficina", "logotipo", "logo", "dados da empresa", "cabecalho", "personalizar", "cnpj oficina" }
                }
            };
        }
    }
}
