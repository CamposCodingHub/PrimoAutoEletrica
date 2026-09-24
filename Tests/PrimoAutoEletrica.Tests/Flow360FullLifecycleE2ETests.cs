using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests
{
    public sealed class Flow360FullLifecycleE2ETests : IDisposable
    {
        private readonly string _testDir;

        public Flow360FullLifecycleE2ETests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "primox_flow360_e2e_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
            DiagnosticoTecnicoService.TestStorageDirectoryOverride = Path.Combine(_testDir, "diagnosticos");
            PosVendaService.TestStorageDirectoryOverride = Path.Combine(_testDir, "posvenda");
        }

        public void Dispose()
        {
            DiagnosticoTecnicoService.TestStorageDirectoryOverride = null;
            PosVendaService.TestStorageDirectoryOverride = null;
            if (Directory.Exists(_testDir))
            {
                try { Directory.Delete(_testDir, true); } catch { }
            }
        }

        [Fact]
        public void FluxoCompleto360_DoClienteAoPosVenda_PreservaIdsSemPerdaDeDados()
        {
            // 1. Cliente
            var clienteId = Guid.NewGuid();
            var cliente = new Cliente
            {
                Id = clienteId,
                Nome = "Transportadora Rota Segura",
                CPF = "12.345.678/0001-90",
                Telefone = "11988887777",
                Email = "frota@rotasegura.com.br",
                Ativo = true
            };
            Assert.NotEqual(Guid.Empty, cliente.Id);

            // 2. Veículo (Heavy Diesel)
            var veiculoId = Guid.NewGuid();
            var veiculo = new Veiculo
            {
                Id = veiculoId,
                ClienteId = clienteId,
                Placa = "MLB9J14",
                Modelo = "Volvo FH 540 Globetrotter 6x4",
                Marca = "Volvo",
                Ano = "2024",
                Combustivel = "Diesel S10",
                SistemaEletrico = "24V",
                BateriaPrincipal = "2x 12V 170Ah Heliar Frota",
                Alternador = "Bosch 28V 120A Long Life",
                MotorPartida = "Prestolite 24V 5.5kW"
            };
            Assert.Equal(clienteId, veiculo.ClienteId);

            // 3. Orçamento com Peça e Serviço
            var orcamentoId = Guid.NewGuid();
            var produtoId = Guid.NewGuid();
            var orcamento = new Orcamento
            {
                Id = orcamentoId,
                Numero = "ORC-2026-9001",
                ClienteId = clienteId,
                VeiculoId = veiculoId,
                Status = "Rascunho",
                DataCriacao = DateTime.Now,
                Subtotal = 1700.00m,
                Total = 1700.00m,
                Itens = new List<OrcamentoItem>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        OrcamentoId = orcamentoId,
                        ProdutoId = produtoId,
                        Tipo = "Produto",
                        ProdutoNome = "Regulador de Voltagem 28V Heavy Duty",
                        Quantidade = 1,
                        PrecoUnitario = 1250.00m,
                        Subtotal = 1250.00m
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        OrcamentoId = orcamentoId,
                        Tipo = "Servico",
                        ProdutoNome = "Revisao de Alternador e Teste de Queda de Tensao 24V",
                        Quantidade = 1,
                        PrecoUnitario = 450.00m,
                        Subtotal = 450.00m
                    }
                }
            };

            // 4. Aprovação do Orçamento
            orcamento.Status = "Aprovado";
            orcamento.DataAprovacao = DateTime.Now;

            // 5. Conversão em OS (Lossless, zero redigitação)
            var osId = Guid.NewGuid();
            var ordemServico = new OrdemServico
            {
                Id = osId,
                Numero = "OS-2026-9001",
                OrcamentoId = orcamento.Id,
                ClienteId = orcamento.ClienteId.Value,
                VeiculoId = orcamento.VeiculoId,
                ClienteNomeSnapshot = cliente.Nome,
                VeiculoDescricaoSnapshot = veiculo.Modelo,
                PlacaSnapshot = veiculo.Placa,
                TelefoneClienteSnapshot = cliente.Telefone,
                Status = "EmAndamento",
                DataAbertura = DateTime.Now,
                ValorMaoObra = 450.00m,
                Itens = orcamento.Itens.Select(i => new OrdemServicoItem
                {
                    Id = Guid.NewGuid(),
                    OrdemServicoId = osId,
                    ProdutoId = i.ProdutoId,
                    Tipo = i.Tipo,
                    Descricao = i.ProdutoNome,
                    Quantidade = i.Quantidade,
                    ValorUnitario = i.PrecoUnitario
                }).ToList()
            };

            Assert.Equal(clienteId, ordemServico.ClienteId);
            Assert.Equal(veiculoId, ordemServico.VeiculoId);
            Assert.Equal(orcamentoId, ordemServico.OrcamentoId);
            Assert.Equal(2, ordemServico.Itens.Count);

            // 6. Diagnóstico Técnico Autoelétrica Heavy (Roteiro D01 - Carga 24V)
            var diagService = new DiagnosticoTecnicoService(Path.Combine(_testDir, "diagnosticos"));
            var diagnosticoId = Guid.NewGuid();
            var medicaoId = Guid.NewGuid();

            var diagnostico = new DiagnosticoTecnico
            {
                Id = diagnosticoId,
                OrdemServicoId = osId,
                VeiculoId = veiculoId,
                ClienteId = clienteId,
                RoteiroCodigo = "D01",
                SintomaRelatado = "Baterias descarregando em viagem noturna sob chuva",
                SintomaCategoria = "Sistema de Carga 24V",
                CausaStatus = CausaStatusEnum.PROVAVEL,
                CausaDescricao = "Regulador de voltagem com fuga térmica sob rotação",
                Status = DiagnosticoStatusEnum.EmAndamento,
                Medicoes = new List<DiagnosticoMedicao>
                {
                    new()
                    {
                        Id = medicaoId,
                        DiagnosticoId = diagnosticoId,
                        NomeTeste = "Tensao Alternador sob Carga 24V",
                        TipoGrandeza = GrandezaEletricaEnum.Tensao,
                        Instrumento = "Alicate Amperimetro / Multimetro Fluke",
                        Unidade = "V",
                        Momento = "Marcha Lenta com Farois e Ar Ligados",
                        Condicao = "24V Diesel Pesado",
                        ValorReferenciaMin = 27.8m,
                        ValorReferenciaMax = 28.8m,
                        ValorInicial = 26.20m, // Anormal antes do reparo
                        Resultado = MedicaoResultadoEnum.FORA_DO_ESPERADO
                    }
                }
            };
            diagService.SalvarDiagnostico(diagnostico);

            // 7. Correção e Teste Pós-Reparo (Validação Objetiva com Delta)
            var diagAtualizado = diagService.RegistrarMedicaoPosReparo(
                diagnosticoId,
                medicaoId,
                valorPosReparo: 28.35m, // Normal pós-reparo
                novoResultado: MedicaoResultadoEnum.NORMAL,
                observacao: "Regulador 28V substituido e conexoes de aterramento limpas."
            );

            Assert.NotNull(diagAtualizado);
            var medicaoFinal = diagAtualizado!.Medicoes.First(m => m.Id == medicaoId);
            Assert.True(medicaoFinal.TemValidacaoPosReparo);
            Assert.Equal(28.35m, medicaoFinal.ValorPosReparo);
            Assert.Equal(2.15m, medicaoFinal.DeltaPosReparo); // 28.35 - 26.20 = +2.15V
            Assert.Equal(MedicaoResultadoEnum.NORMAL, medicaoFinal.Resultado);

            // 8. Conclusão da OS com Garantia de 90 dias
            ordemServico.Status = "Concluido";
            ordemServico.DataConclusao = DateTime.Now;
            ordemServico.GarantiaValidaAte = DateTime.Today.AddDays(90);

            // 9. Pós-Venda Estruturado (Revisão Preventiva & Follow-up)
            var posVendaService = new PosVendaService(Path.Combine(_testDir, "posvenda"));
            var posVenda = posVendaService.CriarDeOrdemServico(
                ordemServico,
                PosVendaTipoEnum.FollowUpPosServico,
                dias: 7,
                responsavel: "Consultor Carlos"
            );

            Assert.NotNull(posVenda);
            Assert.Equal(clienteId, posVenda.ClienteId);
            Assert.Equal(veiculoId, posVenda.VeiculoId);
            Assert.Equal(osId, posVenda.OrdemServicoId);
            Assert.Equal(PosVendaStatusEnum.Pendente, posVenda.Status);
            Assert.Equal("Consultor Carlos", posVenda.Responsavel);
            Assert.Equal(DateTime.Today.AddDays(90), posVenda.GarantiaValidaAte);

            // 10. Follow-up do Pós-venda Concluído
            var contatoSucesso = posVendaService.RegistrarContato(
                posVenda.Id,
                "Cliente relatou que veiculo rodou 1.200km sem nenhuma falha no alternador. Carga 100% estavel.",
                "Consultor Carlos",
                resolvido: true,
                novoStatus: PosVendaStatusEnum.Concluido
            );
            Assert.True(contatoSucesso);

            var itemPosVendaFinal = posVendaService.ObterPorId(posVenda.Id);
            Assert.NotNull(itemPosVendaFinal);
            Assert.Equal(PosVendaStatusEnum.Concluido, itemPosVendaFinal!.Status);
            Assert.True(itemPosVendaFinal.Resolvido);
            Assert.True(itemPosVendaFinal.ContatoRealizado);

            // 11. Validação de Isolamento e Rastreabilidade 360 por ID
            var historicoClientePosVenda = posVendaService.ListarPorClienteId(clienteId);
            var historicoVeiculoPosVenda = posVendaService.ListarPorVeiculoId(veiculoId);
            var historicoOsPosVenda = posVendaService.ListarPorOrdemServicoId(osId);

            Assert.Single(historicoClientePosVenda);
            Assert.Single(historicoVeiculoPosVenda);
            Assert.Single(historicoOsPosVenda);
        }
    }
}
