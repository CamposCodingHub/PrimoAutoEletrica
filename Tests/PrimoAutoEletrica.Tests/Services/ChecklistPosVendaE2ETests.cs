#nullable enable
using System;
using System.IO;
using System.Linq;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public class ChecklistPosVendaE2ETests : IDisposable
    {
        private readonly string _tempDir;
        private readonly ChecklistTecnicoService _checklistService;
        private readonly PosVendaService _posVendaService;
        private readonly DiagnosticoTecnicoService _diagnosticoService;

        public ChecklistPosVendaE2ETests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "PrimoAuto_E2ETest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);

            _checklistService = new ChecklistTecnicoService(Path.Combine(_tempDir, "checklists"));
            _posVendaService = new PosVendaService(Path.Combine(_tempDir, "posvenda"));
            _diagnosticoService = new DiagnosticoTecnicoService(Path.Combine(_tempDir, "diagnosticos"));
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                }
            }
            catch
            {
                // Limpeza segura
            }
        }

        [Fact]
        public void FluxoCompleto_E2E_Cliente_Veiculo_OS_Checklist_Diagnostico_PosVenda_PreservaTodosIDs()
        {
            // 1. Cliente & Veículo
            var clienteId = Guid.NewGuid();
            var veiculoId = Guid.NewGuid();

            // 2. OS Inicial
            var osId = Guid.NewGuid();

            // 3. Checklist Multiponto
            var checklist = _checklistService.ObterOuCriarPadrao(osId, veiculoId, clienteId, isLinhaPesada: false);
            Assert.Equal(osId, checklist.OrdemServicoId);
            Assert.Equal(veiculoId, checklist.VeiculoId);
            Assert.Equal(clienteId, checklist.ClienteId);

            // 4. Medição Antes no Checklist (Bateria)
            var itemBateria = checklist.Itens.First(i => i.Secao == "BATERIA");
            itemBateria.Status = ChecklistStatusEnum.Critico;
            itemBateria.ValorMedido = 11.80m;
            itemBateria.Unidade = "V";
            itemBateria.Momento = "AntesReparo";
            _checklistService.Salvar(checklist);

            // 5. Diagnóstico Roteiro D01 (Bateria descarregando)
            var diag = new DiagnosticoTecnico
            {
                OrdemServicoId = osId,
                VeiculoId = veiculoId,
                ClienteId = clienteId,
                RoteiroCodigo = "D01",
                SintomaRelatado = "Bateria descarregando com veículo parado",
                SintomaCategoria = "Partida e Bateria",
                Status = DiagnosticoStatusEnum.EmAndamento
            };
            diag.Medicoes.Add(new DiagnosticoMedicao
            {
                NomeTeste = "Fuga de corrente no polo negativo",
                TipoGrandeza = GrandezaEletricaEnum.FugaCorrente,
                Instrumento = "Alicate amperímetro DC",
                Unidade = "mA",
                ValorInicial = 450.0m,
                ValorReferenciaMax = 50.0m,
                Resultado = MedicaoResultadoEnum.FORA_DO_ESPERADO,
                Momento = "AntesReparo"
            });
            _diagnosticoService.SalvarDiagnostico(diag);

            // 6. Correção e Medição Depois no Diagnóstico
            _diagnosticoService.RegistrarMedicaoPosReparo(
                diag.Id,
                diag.Medicoes[0].Id,
                valorPosReparo: 28.0m,
                novoResultado: MedicaoResultadoEnum.NORMAL,
                observacao: "Identificado relé do farol de milha colado; substituído");

            _diagnosticoService.ConcluirDiagnostico(
                diag.Id,
                laudoFinal: "Fuga de corrente eliminada com sucesso.",
                correcao: "Substituição de relé auxiliar",
                statusCausa: CausaStatusEnum.CONFIRMADA);

            // 7. Medição Depois e Delta no Checklist Multiponto
            itemBateria.ValorPosReparo = 12.60m;
            itemBateria.Status = ChecklistStatusEnum.OK;
            Assert.Equal(0.80m, itemBateria.DeltaPosReparo);

            _checklistService.Concluir(checklist.Id, "Técnico Especialista Ricardo", "Todos os circuitos normalizados.");

            // 8. Pós-venda Inicial
            var posVendaInicial = new PosVendaItem
            {
                ClienteId = clienteId,
                VeiculoId = veiculoId,
                OrdemServicoId = osId,
                Tipo = PosVendaTipoEnum.FollowUpPosServico,
                Status = PosVendaStatusEnum.Concluido,
                DataPrevistaContato = DateTime.Today.AddDays(7),
                DataContatoRealizado = DateTime.Today.AddDays(7),
                Responsavel = "Consultoria",
                Resultado = "Cliente confirmou que bateria permaneceu carregada perfeitamente.",
                Resolvido = true
            };
            _posVendaService.Salvar(posVendaInicial);

            // 9. Pós-venda de Retorno Preventivo na Mesma OS
            var posVendaRetorno = new PosVendaItem
            {
                ClienteId = clienteId,
                VeiculoId = veiculoId,
                OrdemServicoId = osId,
                Tipo = PosVendaTipoEnum.RevisaoPreventiva,
                Status = PosVendaStatusEnum.Pendente,
                DataPrevistaContato = DateTime.Today.AddDays(90),
                Responsavel = "Consultoria",
                Observacoes = "Revisão de 90 dias do sistema de carga."
            };
            _posVendaService.Salvar(posVendaRetorno);

            // 10. Validação de Integridade Relacional e Não-perda de IDs
            var checklistsRecarregados = _checklistService.ListarPorVeiculoId(veiculoId);
            Assert.Single(checklistsRecarregados);
            Assert.Equal(osId, checklistsRecarregados[0].OrdemServicoId);
            Assert.Equal(clienteId, checklistsRecarregados[0].ClienteId);
            Assert.True(checklistsRecarregados[0].Concluido);

            var diagsRecarregados = _diagnosticoService.ObterPorOrdemServicoId(osId);
            Assert.Single(diagsRecarregados);
            Assert.Equal(veiculoId, diagsRecarregados[0].VeiculoId);
            Assert.Equal(CausaStatusEnum.CONFIRMADA, diagsRecarregados[0].CausaStatus);
            Assert.True(diagsRecarregados[0].Medicoes[0].TemValidacaoPosReparo);

            var posVendasRecarregadas = _posVendaService.ListarPorOrdemServicoId(osId);
            Assert.Equal(2, posVendasRecarregadas.Count);
            Assert.All(posVendasRecarregadas, pv =>
            {
                Assert.Equal(clienteId, pv.ClienteId);
                Assert.Equal(veiculoId, pv.VeiculoId);
                Assert.Equal(osId, pv.OrdemServicoId);
            });
        }

        [Fact]
        public void HistoricoAB_MesmoVeiculoDuasOS_A_NaoIgual_B_SemSobrescrita()
        {
            var clienteId = Guid.NewGuid();
            var veiculoId = Guid.NewGuid();

            // OS A
            var osAId = Guid.NewGuid();
            var checklistA = _checklistService.ObterOuCriarPadrao(osAId, veiculoId, clienteId, isLinhaPesada: false);
            checklistA.ObservacoesGerais = "Checklist OS-A: Reparo do Alternador";
            _checklistService.Salvar(checklistA);

            var diagA = new DiagnosticoTecnico
            {
                OrdemServicoId = osAId,
                VeiculoId = veiculoId,
                RoteiroCodigo = "D02",
                SintomaRelatado = "Alternador não carrega bateria",
                Status = DiagnosticoStatusEnum.Concluido
            };
            _diagnosticoService.SalvarDiagnostico(diagA);

            var posA = new PosVendaItem
            {
                ClienteId = clienteId,
                VeiculoId = veiculoId,
                OrdemServicoId = osAId,
                Tipo = PosVendaTipoEnum.FollowUpPosServico,
                Status = PosVendaStatusEnum.Concluido,
                Observacoes = "Pos-Venda OS-A finalizado com sucesso"
            };
            _posVendaService.Salvar(posA);

            // OS B (Mesmo veículo, período posterior)
            var osBId = Guid.NewGuid();
            var checklistB = _checklistService.ObterOuCriarPadrao(osBId, veiculoId, clienteId, isLinhaPesada: false);
            checklistB.ObservacoesGerais = "Checklist OS-B: Reparo de Faróis e Iluminação";
            _checklistService.Salvar(checklistB);

            var diagB = new DiagnosticoTecnico
            {
                OrdemServicoId = osBId,
                VeiculoId = veiculoId,
                RoteiroCodigo = "D05",
                SintomaRelatado = "Faróis principais sem alimentação",
                Status = DiagnosticoStatusEnum.EmAndamento
            };
            _diagnosticoService.SalvarDiagnostico(diagB);

            var posB = new PosVendaItem
            {
                ClienteId = clienteId,
                VeiculoId = veiculoId,
                OrdemServicoId = osBId,
                Tipo = PosVendaTipoEnum.Garantia,
                Status = PosVendaStatusEnum.Pendente,
                Observacoes = "Pos-Venda OS-B abertura de garantia de lâmpada"
            };
            _posVendaService.Salvar(posB);

            // Asserções Rigorosas: A != B
            Assert.NotEqual(osAId, osBId);
            Assert.NotEqual(checklistA.Id, checklistB.Id);
            Assert.NotEqual(diagA.Id, diagB.Id);
            Assert.NotEqual(posA.Id, posB.Id);

            // Verificação por Veículo (Vehicle360)
            var checklistsVeiculo = _checklistService.ListarPorVeiculoId(veiculoId);
            Assert.Equal(2, checklistsVeiculo.Count);
            Assert.Contains(checklistsVeiculo, c => c.OrdemServicoId == osAId && c.ObservacoesGerais.Contains("OS-A"));
            Assert.Contains(checklistsVeiculo, c => c.OrdemServicoId == osBId && c.ObservacoesGerais.Contains("OS-B"));

            var diagsVeiculo = _diagnosticoService.ObterHistoricoPorVeiculoId(veiculoId);
            Assert.Equal(2, diagsVeiculo.Count);
            Assert.Contains(diagsVeiculo, d => d.OrdemServicoId == osAId && d.RoteiroCodigo == "D02");
            Assert.Contains(diagsVeiculo, d => d.OrdemServicoId == osBId && d.RoteiroCodigo == "D05");

            var posVendasVeiculo = _posVendaService.ListarPorVeiculoId(veiculoId);
            Assert.Equal(2, posVendasVeiculo.Count);
            Assert.Contains(posVendasVeiculo, pv => pv.OrdemServicoId == osAId && pv.Observacoes.Contains("OS-A"));
            Assert.Contains(posVendasVeiculo, pv => pv.OrdemServicoId == osBId && pv.Observacoes.Contains("OS-B"));

            // Verificação por Cliente (Client360)
            var posVendasCliente = _posVendaService.ListarPorClienteId(clienteId);
            Assert.Equal(2, posVendasCliente.Count);
        }
    }
}
