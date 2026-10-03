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
    }

    public sealed class AutomotiveDiagnosticRAGService
    {
        private readonly List<DiagnosticEntry> _technicalBase;

        public AutomotiveDiagnosticRAGService()
        {
            _technicalBase = CarregarBaseConhecimentoTecnico();
        }

        public DiagnosticEntry? BuscarPorCodigoDTC(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return null;

            var match = Regex.Match(codigo.Trim().ToUpperInvariant(), @"[PBUS]\d{4}");
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

            var termoNorm = termo.Trim().ToLowerInvariant();
            var tokens = termoNorm.Split(new[] { ' ', ',', ';', '-', '/' }, StringSplitOptions.RemoveEmptyEntries);

            var resultados = _technicalBase
                .Select(d => new
                {
                    Item = d,
                    Score = CalcularRelevancia(d, termoNorm, tokens)
                })
                .Where(r => r.Score > 0)
                .OrderByDescending(r => r.Score)
                .Take(limite)
                .Select(r => r.Item)
                .ToList();

            return resultados;
        }

        private static int CalcularRelevancia(DiagnosticEntry d, string termoNorm, string[] tokens)
        {
            int score = 0;
            var fullText = $"{d.Code} {d.Title} {d.System} {d.Symptoms} {string.Join(" ", d.ProbableCauses)}".ToLowerInvariant();

            if (fullText.Contains(termoNorm)) score += 30;

            foreach (var t in tokens)
            {
                if (t.Length < 3) continue;
                if (d.Code.ToLowerInvariant().Contains(t)) score += 25;
                if (d.Title.ToLowerInvariant().Contains(t)) score += 15;
                if (d.System.ToLowerInvariant().Contains(t)) score += 10;
                if (fullText.Contains(t)) score += 5;
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
                    ReferenceStandard = "Tensão de Carga: 13.8V - 14.5V DC | Queda Positiva: < 0.2V | Queda Massa: < 0.1V | Ripple AC: < 0.05V AC"
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
                    ReferenceStandard = "Tensão Máxima Admissível: 14.8V DC sob rotação máxima."
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
                    ReferenceStandard = "Sinal PWM: 12V pico a pico, frequência típica 125Hz - 250Hz conforme montadora."
                },
                new DiagnosticEntry
                {
                    Code = "P0335",
                    Title = "Sensor de Posição da Árvore de Manivelas (CKP - Circuito A)",
                    System = "Ignição & Injeção Eletrônica",
                    Symptoms = "Motor gira no arranque mas não entra em funcionamento, corte súbito do motor em trânsito ao esquentar, conta-giros não mexe na partida.",
                    ProbableCauses = new List<string>
                    {
                        "Sensor de rotação indutivo ou Hall defeituoso (aberto quando aquece)",
                        "Distância entre a ponta do sensor e a roda fônica incorreta (entreferro fora da especificação)",
                        "Roda fônica empenada, dentes quebrados ou chaveta da polia com folga",
                        "Blindagem do cabo do sensor rompida, captando ruído da bobina ou motor de arranque"
                    },
                    GuidedSteps = new List<string>
                    {
                        "Identificar o tipo de sensor: 2 pinos = Indutivo; 3 pinos = Hall (alimentação 5V ou 12V, terra e sinal).",
                        "Se Indutivo: medir resistência ôhmica com multímetro (geralmente entre 500Ω e 1200Ω). Se aberto ou em curto: sensor queimado.",
                        "Conectar osciloscópio no sinal de rotação durante a partida: deve gerar onda senoidal simétrica (indutivo) ou onda quadrada 0-5V (Hall) com falha visível dos 2 dentes de sincronismo.",
                        "Conferir entreferro: normalmente entre 0.8mm e 1.2mm com calibre de lâminas."
                    },
                    SuggestedTools = new List<string> { "Osciloscópio Automotivo", "Multímetro Digital", "Calibre de Lâminas" },
                    SuggestedStockParts = new List<string> { "Sensor de Rotação CKP", "Roda Fônica", "Conector Chicote Sensor" },
                    ReferenceStandard = "Resistência Indutivo: 500Ω - 1200Ω | Tensão AC de partida indutivo: > 1.5V AC | Entreferro: ~1.0mm"
                },
                new DiagnosticEntry
                {
                    Code = "P0300",
                    Title = "Falha de Ignição Múltipla / Aleatória (Random Cylinder Misfire)",
                    System = "Sistema de Ignição & Alta Tensão",
                    Symptoms = "Motor tremendo em marcha lenta, estalos no escape, perda severa de potência, luz de injeção piscando em aceleração.",
                    ProbableCauses = new List<string>
                    {
                        "Bobinas de ignição com fuga de alta tensão na carcaça ou queima de enrolamento secundário",
                        "Cabos de vela partidos, com resistência excessiva ou conectores soltos",
                        "Velas de ignição carbonizadas, gastas com gap excessivo (> 1.1mm)",
                        "Queda de tensão na alimentação positiva das bobinas (linha 15 pós-chave através de relé com contato queimado)"
                    },
                    GuidedSteps = new List<string>
                    {
                        "Medir resistência dos cabos de ignição: padrão máximo de 5kΩ a 10kΩ por metro. Cabos com > 20kΩ devem ser trocados.",
                        "Inspecionar visualmente as velas: verificar folga dos eletrodos com calibre de folga.",
                        "Conectar pinça indutiva/capacitiva de alta tensão no osciloscópio para analisar tempo de queima da centelha (normal entre 1.0ms e 1.8ms) e tensão de disparo.",
                        "Verificar queda de tensão na alimentação de 12V da bobina sob aceleração súbita."
                    },
                    SuggestedTools = new List<string> { "Osciloscópio com Pinça de Ignição Secundária (HT)", "Calibre de Folga de Velas", "Multímetro" },
                    SuggestedStockParts = new List<string> { "Jogo de Velas de Ignição", "Cabos de Ignição de Silicone", "Bobina de Ignição", "Relé Principal da Injeção" },
                    ReferenceStandard = "Tempo de centelha no osciloscópio: 1.0ms a 1.8ms | Resistência dos cabos: < 10kΩ/m | Gap vela: 0.8mm a 1.0mm"
                },
                new DiagnosticEntry
                {
                    Code = "CONSUMO_PARASITA",
                    Title = "Procedimento Especializado: Diagnóstico de Fuga de Corrente / Consumo Parasita",
                    System = "Alimentação & Redes em Repouso",
                    Symptoms = "Bateria descarrega de um dia para o outro ou no fim de semana mesmo com alternador carregando perfeitamente.",
                    ProbableCauses = new List<string>
                    {
                        "Módulos que não entram em modo Sleep / Hibernação (BCM, Painel, Multimídia, Trava)",
                        "Rastreador GPS clandestino ou mal instalado com alimentação direta",
                        "Lâmpada do porta-malas ou porta-luvas permanentemente acesa por interruptor quebrado",
                        "Diodo de retorno do alternador com micro-fuga para a carcaça",
                        "Alarme pós-venda, módulo de subida de vidro com relé travado"
                    },
                    GuidedSteps = new List<string>
                    {
                        "1. Abrir o capô e simular o fechamento da fechadura do capô com uma chave de fenda para desarmar o sensor de alarme.",
                        "2. Fechar todas as portas do veículo e travar o alarme pelo controle remoto.",
                        "3. AGUARDAR O VEÍCULO ENTRAR EM SLEEP: de 15 a 45 minutos dependendo da marca (veículos modernos mantêm a rede CAN acordada por até 30 min).",
                        "4. Instalar o alicate amperímetro com resolução de mA no cabo negativo da bateria, ou multímetro em escala de 10A DC em série.",
                        "5. VALOR NOMINAL ACEITÁVEL: abaixo de 0.050A (50mA). Em veículos com rastreador, aceita-se no máximo 0.070A (70mA).",
                        "6. Se estiver acima de 0.080A (ex: 250mA, 500mA): abrir a caixa de fusíveis interna e retirar um a um enquanto monitora o amperímetro.",
                        "7. Quando a corrente cair repentinamente para o nível normal (< 50mA), o fusível removido aponta exatamente o circuito com falha!"
                    },
                    SuggestedTools = new List<string> { "Alicate Amperímetro DC de Precisão (escala mA)", "Multímetro Digital True RMS", "Extrator de Fusíveis" },
                    SuggestedStockParts = new List<string> { "Relé Auxiliar 40A", "Módulo de Vidro", "Fusíveis Lâmina Variados", "Interruptor de Cortesia" },
                    ReferenceStandard = "Consumo em Repouso Tolerado: 20mA a 50mA (0.02A - 0.05A) após 30 minutos de trancamento."
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
                    ReferenceStandard = "Resistência da Bobina: 60Ω a 120Ω | Resistência de Contato Fechado: < 0.2Ω"
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
                    ReferenceStandard = "Resistência de Barramento: 60Ω nominais (55Ω a 65Ω) | Tensão Média CAN H: 2.7V | Tensão Média CAN L: 2.3V"
                }
            };
        }
    }
}
