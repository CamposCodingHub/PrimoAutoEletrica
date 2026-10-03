using PrimoAutoEletrica.Models.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services.AI
{
    public sealed class DeterministicFallbackAIService : IAIService
    {
        private readonly AIToolRegistry _toolRegistry;
        private readonly AutomotiveDiagnosticRAGService _ragService;

        public string ProviderName => "PRIMOX Offline Expert System";
        public bool IsOnlineAvailable => true; // Sempre disponível localmente

        public DeterministicFallbackAIService(AIToolRegistry toolRegistry, AutomotiveDiagnosticRAGService? ragService = null)
        {
            _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
            _ragService = ragService ?? new AutomotiveDiagnosticRAGService();
        }

        public async Task<AIChatResponse> ProcessarMensagemAsync(AIChatRequest request, CancellationToken cancellationToken = default)
        {
            var ultimaMensagem = request.Messages.LastOrDefault(m => m.Role == AIRole.User)?.Content ?? string.Empty;
            var textoLimpo = ultimaMensagem.Trim().ToLowerInvariant();

            var response = new AIChatResponse
            {
                ProviderUsed = ProviderName,
                Success = true
            };

            // 1. Verificacao de DTC OBD-II
            var dtcMatch = Regex.Match(ultimaMensagem.ToUpperInvariant(), @"\b([PBUS]\d{4})\b");
            if (dtcMatch.Success)
            {
                var dtcCode = dtcMatch.Value;
                var dtc = _ragService.BuscarPorCodigoDTC(dtcCode);
                if (dtc != null)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"⚡ **Diagnóstico Técnico Guiado: DTC {dtc.Code}**");
                    sb.AppendLine($"**Sistema:** {dtc.System}");
                    sb.AppendLine($"**Definição:** {dtc.Title}\n");
                    sb.AppendLine($"🔍 **Sintomas Reportados:**\n{dtc.Symptoms}\n");
                    sb.AppendLine("⚠️ **Causas Mais Frequentes:**");
                    foreach (var c in dtc.ProbableCauses) sb.AppendLine($"• {c}");
                    sb.AppendLine("\n📋 **Roteiro de Testes na Bancada / Veículo:**");
                    for (int i = 0; i < dtc.GuidedSteps.Count; i++)
                    {
                        sb.AppendLine($"{i + 1}. {dtc.GuidedSteps[i]}");
                    }
                    sb.AppendLine($"\n📏 **Padrão de Referência:** {dtc.ReferenceStandard}");
                    sb.AppendLine($"🧰 **Instrumentos:** {string.Join(", ", dtc.SuggestedTools)}");

                    response.Message = sb.ToString();

                    response.SuggestedActions.Add(new AISuggestedAction
                    {
                        Label = "Ir para Módulo Técnico",
                        ActionType = "Navigate",
                        Parameter = "AutoEletricaTecnica"
                    });

                    if (dtc.SuggestedStockParts.Count > 0)
                    {
                        var peca = dtc.SuggestedStockParts.First();
                        response.SuggestedActions.Add(new AISuggestedAction
                        {
                            Label = $"Ver Peças ({peca})",
                            ActionType = "SearchStock",
                            Parameter = peca
                        });
                    }

                    return response;
                }
            }

            // 2. Procedimentos Elétricos Especializados
            if (textoLimpo.Contains("fuga") || textoLimpo.Contains("parasita") || textoLimpo.Contains("descarrega") || textoLimpo.Contains("sleep"))
            {
                var diag = _ragService.BuscarPorCodigoDTC("CONSUMO_PARASITA") ?? _ragService.BuscarPorSintomaOuTermo("parasita").FirstOrDefault();
                if (diag != null)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("🔋 **Procedimento de Teste: Fuga de Corrente / Consumo Parasita**\n");
                    for (int i = 0; i < diag.GuidedSteps.Count; i++) sb.AppendLine($"{diag.GuidedSteps[i]}");
                    sb.AppendLine($"\n📏 **Padrão Aceitável:** {diag.ReferenceStandard}");
                    sb.AppendLine($"🧰 **Ferramentas:** {string.Join(", ", diag.SuggestedTools)}");

                    response.Message = sb.ToString();
                    response.SuggestedActions.Add(new AISuggestedAction
                    {
                        Label = "Consultar Alicate Amperímetro",
                        ActionType = "Navigate",
                        Parameter = "Ferramentas"
                    });
                    return response;
                }
            }

            if (textoLimpo.Contains("relé") || textoLimpo.Contains("rele") || textoLimpo.Contains("pinagem") || textoLimpo.Contains("din 72552"))
            {
                var diag = _ragService.BuscarPorCodigoDTC("TESTE_RELE");
                if (diag != null)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("🔌 **Pinagem & Teste de Relés Automotivos (Norma DIN 72552)**\n");
                    for (int i = 0; i < diag.GuidedSteps.Count; i++) sb.AppendLine($"{diag.GuidedSteps[i]}");
                    sb.AppendLine($"\n📏 **Padrão:** {diag.ReferenceStandard}");

                    response.Message = sb.ToString();
                    response.SuggestedActions.Add(new AISuggestedAction
                    {
                        Label = "Ver Relés no Estoque",
                        ActionType = "SearchStock",
                        Parameter = "Rele"
                    });
                    return response;
                }
            }

            if (textoLimpo.Contains("can") || textoLimpo.Contains("rede") || textoLimpo.Contains("u0100") || textoLimpo.Contains("60 ohm") || textoLimpo.Contains("multiplexad"))
            {
                var diag = _ragService.BuscarPorCodigoDTC("REDE_CAN");
                if (diag != null)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("🌐 **Diagnóstico Físico de Rede CAN Bus Automotiva**\n");
                    for (int i = 0; i < diag.GuidedSteps.Count; i++) sb.AppendLine($"{diag.GuidedSteps[i]}");
                    sb.AppendLine($"\n📏 **Padrão:** {diag.ReferenceStandard}");

                    response.Message = sb.ToString();
                    response.SuggestedActions.Add(new AISuggestedAction
                    {
                        Label = "Ir para Módulo Técnico",
                        ActionType = "Navigate",
                        Parameter = "AutoEletricaTecnica"
                    });
                    return response;
                }
            }

            // 3. Comandos Operacionais ERP: Navegacao
            if (textoLimpo.StartsWith("abre ") || textoLimpo.StartsWith("abrir ") || textoLimpo.StartsWith("ir para ") || textoLimpo.StartsWith("navegar "))
            {
                string? destino = null;
                if (textoLimpo.Contains("estoque")) destino = "Estoque";
                else if (textoLimpo.Contains("compra") || textoLimpo.Contains("falta")) destino = "ComprasNecessidade";
                else if (textoLimpo.Contains("ferramenta")) destino = "Ferramentas";
                else if (textoLimpo.Contains("ordem") || textoLimpo.Contains("os")) destino = "OrdensServico";
                else if (textoLimpo.Contains("kanban")) destino = "OficinaKanban";
                else if (textoLimpo.Contains("pdv") || textoLimpo.Contains("caixa")) destino = "PDV";
                else if (textoLimpo.Contains("cliente")) destino = "Clientes";
                else if (textoLimpo.Contains("veiculo")) destino = "Veiculos";
                else if (textoLimpo.Contains("tecnic") || textoLimpo.Contains("auto eletrica")) destino = "AutoEletricaTecnica";
                else if (textoLimpo.Contains("financeiro")) destino = "Financeiro";
                else if (textoLimpo.Contains("fornecedor")) destino = "Fornecedores";
                else if (textoLimpo.Contains("dashboard")) destino = "Dashboard";

                if (destino != null)
                {
                    var res = await _toolRegistry.ExecutarFerramentaAsync("NavegarParaModulo", new() { ["modulo"] = destino });
                    response.Message = $"✅ {res} Abrindo a tela solicitada para você.";
                    response.SuggestedActions.Add(new AISuggestedAction
                    {
                        Label = $"Acessar {destino}",
                        ActionType = "Navigate",
                        Parameter = destino
                    });
                    return response;
                }
            }

            // 4. Consulta de Ferramentas da Oficina
            if (textoLimpo.Contains("ferramenta") || textoLimpo.Contains("scanner") || textoLimpo.Contains("osciloscopio") || textoLimpo.Contains("multimetro"))
            {
                if (textoLimpo.Contains("uso") || textoLimpo.Contains("quem") || textoLimpo.Contains("onde") || textoLimpo.Contains("emprest") || textoLimpo.Contains("status"))
                {
                    var res = await _toolRegistry.ExecutarFerramentaAsync("ConsultarFerramentasEmUso", new());
                    response.Message = $"🧰 **Situação da Ferramentaria:**\n\n{res}";
                    response.SuggestedActions.Add(new AISuggestedAction
                    {
                        Label = "Abrir Ferramentaria",
                        ActionType = "Navigate",
                        Parameter = "Ferramentas"
                    });
                    return response;
                }
            }

            // 5. Consulta de Produtos em Falta / Compras
            if (textoLimpo.Contains("falta") || (textoLimpo.Contains("pedido") && textoLimpo.Contains("compra")) || textoLimpo.Contains("comprar") || textoLimpo.Contains("ruptura"))
            {
                var res = await _toolRegistry.ExecutarFerramentaAsync("ConsultarProdutosEmFalta", new());
                response.Message = $"📦 **Gestão de Compras & Anti-Ruptura:**\n\n{res}";
                response.SuggestedActions.Add(new AISuggestedAction
                {
                    Label = "Abrir Compras & Falta",
                    ActionType = "Navigate",
                    Parameter = "ComprasNecessidade"
                });
                return response;
            }

            // 6. Consulta de Estoque de Produtos
            if (textoLimpo.Contains("estoque") || textoLimpo.Contains("preco") || textoLimpo.Contains("tem ") || textoLimpo.Contains("saldo"))
            {
                var termo = Regex.Replace(textoLimpo, @"\b(tem|saldo|estoque|preco|de|do|da|no|na|peca|produto|consultar)\b", " ").Trim();
                if (termo.Length >= 2)
                {
                    var res = await _toolRegistry.ExecutarFerramentaAsync("ConsultarEstoque", new() { ["termo"] = termo });
                    response.Message = $"📦 **Consulta de Estoque:**\n\n{res}";
                    response.SuggestedActions.Add(new AISuggestedAction
                    {
                        Label = "Ver no Estoque Completo",
                        ActionType = "Navigate",
                        Parameter = "Estoque"
                    });
                    return response;
                }
            }

            // 7. Busca de Cliente / Veiculo
            if (textoLimpo.Contains("cliente") || textoLimpo.Contains("veiculo") || textoLimpo.Contains("placa"))
            {
                var termo = Regex.Replace(textoLimpo, @"\b(cliente|veiculo|placa|buscar|procurar|consultar|onde|esta)\b", " ").Trim();
                if (termo.Length >= 2)
                {
                    var res = await _toolRegistry.ExecutarFerramentaAsync("BuscarClienteVeiculo", new() { ["termo"] = termo });
                    response.Message = $"👤🚗 **Localização no Cadastro:**\n\n{res}";
                    return response;
                }
            }

            // Resposta Padrao Orientativa com Sugestoes
            var sbPadrao = new StringBuilder();
            sbPadrao.AppendLine("👋 Olá! Sou o **PRIMOX Copilot**, seu copiloto de inteligência para Auto Elétrica e gestão da oficina.");
            sbPadrao.AppendLine("Posso ajudá-lo com diagnósticos técnicos automotivos e automação operacional no sistema:\n");
            sbPadrao.AppendLine("⚡ **Diagnóstico Técnico & Elétrico:**");
            sbPadrao.AppendLine("• Digite um código de anomalia (ex: *'Diagnosticar P0562'* ou *'O que causa P0335?'*)");
            sbPadrao.AppendLine("• Teste de fuga de carga (*'Como medir corrente parasita na bateria?'*)");
            sbPadrao.AppendLine("• Tabela de queda de tensão (*'Quais os limites de voltage drop no alternador?'*)");
            sbPadrao.AppendLine("• Pinagens de relés e rede CAN (*'Pinagem padrão de relé 5 pinos'*)\n");
            sbPadrao.AppendLine("🏢 **Operação & Gestão da Oficina:**");
            sbPadrao.AppendLine("• Gestão de compras (*'Quais produtos estão em falta?'*)");
            sbPadrao.AppendLine("• Rastreamento de ferramentas (*'Quem está com ferramentas da oficina?'*)");
            sbPadrao.AppendLine("• Consulta de estoque (*'Saldo de Bateria 60Ah'* ou *'Preço de relé'*)\n");
            sbPadrao.AppendLine("Como posso auxiliá-lo agora?");

            response.Message = sbPadrao.ToString();
            response.SuggestedActions.Add(new AISuggestedAction { Label = "⚡ Diagnóstico P0562", ActionType = "ExecuteQuery", Parameter = "DTC P0562" });
            response.SuggestedActions.Add(new AISuggestedAction { Label = "🔋 Consumo Parasita", ActionType = "ExecuteQuery", Parameter = "Como testar consumo parasita?" });
            response.SuggestedActions.Add(new AISuggestedAction { Label = "📦 Produtos em Falta", ActionType = "ExecuteQuery", Parameter = "Quais produtos estão em falta?" });
            response.SuggestedActions.Add(new AISuggestedAction { Label = "🧰 Ferramentas em Uso", ActionType = "ExecuteQuery", Parameter = "Quem está com ferramentas em uso?" });

            return response;
        }
    }
}
