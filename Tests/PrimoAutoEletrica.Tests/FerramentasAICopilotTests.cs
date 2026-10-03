using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Models.AI;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PrimoAutoEletrica.Tests
{
    public class FerramentasAICopilotTests
    {
        [Fact]
        public void Ferramenta_CalibracaoVencida_DeveDetectarCorretamente()
        {
            var f = new Ferramenta
            {
                Nome = "Osciloscópio Hantek",
                RequerCalibracaoPeriodica = true,
                IntervaloCalibracaoDias = 365,
                UltimaCalibracao = DateTime.Today.AddDays(-400),
                ProximaCalibracao = DateTime.Today.AddDays(-35)
            };

            Assert.True(f.CalibracaoVencida);

            f.ProximaCalibracao = DateTime.Today.AddDays(30);
            Assert.False(f.CalibracaoVencida);
        }

        [Fact]
        public void Ferramenta_DescricoesStatusECategoria_DevemSerConsistentes()
        {
            var f = new Ferramenta
            {
                Status = StatusFerramenta.EmUso,
                Categoria = CategoriaFerramenta.DiagnosticoEletronico
            };

            Assert.Equal("Em Uso", f.StatusDescricao);
            Assert.Equal("Diagnóstico Eletrônico", f.CategoriaDescricao);
        }

        [Fact]
        public void RAGService_BuscarDTC_P0562_DeveRetornarProcedimentoEValoresNominais()
        {
            var rag = new AutomotiveDiagnosticRAGService();
            var dtc = rag.BuscarPorCodigoDTC("P0562");

            Assert.NotNull(dtc);
            Assert.Equal("P0562", dtc!.Code);
            Assert.Contains("Tensão do Sistema", dtc.Title);
            Assert.Contains(dtc.GuidedSteps, step => step.Contains("Queda de Tensão") || step.Contains("13.8V"));
            Assert.Contains("Multímetro Digital True RMS", dtc.SuggestedTools);
            Assert.True(dtc.ProbableCauses.Count >= 3);
        }

        [Fact]
        public void RAGService_BuscarConsumoParasita_DeveRetornarPassoAPassoFugaCorrente()
        {
            var rag = new AutomotiveDiagnosticRAGService();
            var diags = rag.BuscarPorSintomaOuTermo("consumo parasita fuga bateria");

            Assert.NotEmpty(diags);
            var item = diags.First();
            Assert.Contains("Fuga de Corrente", item.Title);
            Assert.Contains(item.GuidedSteps, step => step.Contains("50mA") || step.Contains("fusíveis") || step.Contains("sleep"));
        }

        [Fact]
        public void RAGService_BuscarRele_DeveRetornarPinagemPadraoDIN72552()
        {
            var rag = new AutomotiveDiagnosticRAGService();
            var diag = rag.BuscarPorCodigoDTC("TESTE_RELE");

            Assert.NotNull(diag);
            Assert.Contains("DIN 72552", diag!.Title);
            Assert.Contains(diag.GuidedSteps, step => step.Contains("Pino 30") && step.Contains("Pino 87"));
        }

        [Fact]
        public void RAGService_BuscarRedeCAN_DeveRetornar60OhmsTerminacao()
        {
            var rag = new AutomotiveDiagnosticRAGService();
            var diag = rag.BuscarPorCodigoDTC("REDE_CAN");

            Assert.NotNull(diag);
            Assert.Contains("CAN Bus", diag!.Title);
            Assert.Contains(diag.GuidedSteps, step => step.Contains("60 Ohms") || step.Contains("Pino 6"));
        }

        [Fact]
        public async Task DeterministicFallbackAIService_PerguntaDTC_DeveFormatarDiagnostico()
        {
            var toolRegistry = new AIToolRegistry(ragService: new AutomotiveDiagnosticRAGService());
            var engine = new DeterministicFallbackAIService(toolRegistry);

            var req = new AIChatRequest
            {
                Messages = new List<AIChatMessage>
                {
                    new AIChatMessage { Role = AIRole.User, Content = "Qual o diagnóstico para o código P0562?" }
                }
            };

            var res = await engine.ProcessarMensagemAsync(req);

            Assert.True(res.Success);
            Assert.Contains("P0562", res.Message);
            Assert.Contains("Diagnóstico Técnico", res.Message);
            Assert.Contains("Queda de Tensão", res.Message);
            Assert.NotEmpty(res.SuggestedActions);
        }

        [Fact]
        public async Task DeterministicFallbackAIService_PerguntaConsumoParasita_DeveRetornarProcedimento()
        {
            var toolRegistry = new AIToolRegistry(ragService: new AutomotiveDiagnosticRAGService());
            var engine = new DeterministicFallbackAIService(toolRegistry);

            var req = new AIChatRequest
            {
                Messages = new List<AIChatMessage>
                {
                    new AIChatMessage { Role = AIRole.User, Content = "A bateria do carro descarrega à noite, como testar corrente parasita?" }
                }
            };

            var res = await engine.ProcessarMensagemAsync(req);

            Assert.True(res.Success);
            Assert.Contains("Consumo Parasita", res.Message);
            Assert.Contains("50mA", res.Message);
        }
    }
}
