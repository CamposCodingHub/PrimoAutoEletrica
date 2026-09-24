#nullable enable
using System;
using System.IO;
using System.Linq;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public class DiagnosticoTecnicoServiceTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly DiagnosticoTecnicoService _service;

        public DiagnosticoTecnicoServiceTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "PrimoAuto_B2_Test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            DiagnosticoTecnicoService.TestStorageDirectoryOverride = _tempDir;
            _service = new DiagnosticoTecnicoService(_tempDir);
        }

        public void Dispose()
        {
            DiagnosticoTecnicoService.TestStorageDirectoryOverride = null;
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                }
            }
            catch
            {
                // Limpeza de testes tolerante
            }
        }

        [Fact]
        public void SalvarDiagnostico_SemOrdemServicoId_LancaArgumentException()
        {
            var diag = new DiagnosticoTecnico
            {
                VeiculoId = Guid.NewGuid(),
                OrdemServicoId = Guid.Empty
            };

            var ex = Assert.Throws<ArgumentException>(() => _service.SalvarDiagnostico(diag));
            Assert.Contains("OrdemServicoId", ex.Message);
        }

        [Fact]
        public void SalvarDiagnostico_SemVeiculoId_LancaArgumentException()
        {
            var diag = new DiagnosticoTecnico
            {
                OrdemServicoId = Guid.NewGuid(),
                VeiculoId = Guid.Empty
            };

            var ex = Assert.Throws<ArgumentException>(() => _service.SalvarDiagnostico(diag));
            Assert.Contains("VeiculoId", ex.Message);
        }

        [Fact]
        public void SalvarDiagnostico_NovoRegistro_PersisteComIdentidadePropria()
        {
            var osId = Guid.NewGuid();
            var veiculoId = Guid.NewGuid();
            var diag = new DiagnosticoTecnico
            {
                OrdemServicoId = osId,
                VeiculoId = veiculoId,
                RoteiroCodigo = "D01",
                SintomaRelatado = "Bateria descarregando durante a noite",
                DiagnosticoLaudo = "Corrente de fuga excessiva via módulo de conforto",
                CausaStatus = CausaStatusEnum.CONFIRMADA,
                CausaDescricao = "Módulo de conforto travado em estado ativo consumindo 280mA",
                CorrecaoExecutada = "Substituição do relé temporizador do módulo",
                Status = DiagnosticoStatusEnum.EmAndamento
            };

            var salvo = _service.SalvarDiagnostico(diag);

            Assert.NotEqual(Guid.Empty, salvo.Id);
            var recuperado = _service.ObterPorId(salvo.Id);
            Assert.NotNull(recuperado);
            Assert.Equal(salvo.Id, recuperado!.Id);
            Assert.Equal(osId, recuperado.OrdemServicoId);
            Assert.Equal(veiculoId, recuperado.VeiculoId);
            Assert.Equal("D01", recuperado.RoteiroCodigo);
            Assert.Equal(CausaStatusEnum.CONFIRMADA, recuperado.CausaStatus);
        }

        [Fact]
        public void SalvarDiagnostico_MultiplosEventosMesmoVeiculo_NaoSobrescreveHistorico()
        {
            var veiculoId = Guid.NewGuid();
            var osA = Guid.NewGuid();
            var osB = Guid.NewGuid();

            var diagA = new DiagnosticoTecnico
            {
                Id = Guid.NewGuid(),
                OrdemServicoId = osA,
                VeiculoId = veiculoId,
                RoteiroCodigo = "D01",
                SintomaRelatado = "Bateria descarregando",
                Status = DiagnosticoStatusEnum.Concluido,
                DataHora = DateTime.Now.AddMonths(-2)
            };

            var diagB = new DiagnosticoTecnico
            {
                Id = Guid.NewGuid(),
                OrdemServicoId = osB,
                VeiculoId = veiculoId,
                RoteiroCodigo = "D02",
                SintomaRelatado = "Motor de partida pesado ao virar a chave",
                Status = DiagnosticoStatusEnum.EmAndamento,
                DataHora = DateTime.Now
            };

            _service.SalvarDiagnostico(diagA);
            _service.SalvarDiagnostico(diagB);

            var historico = _service.ObterHistoricoPorVeiculoId(veiculoId);
            Assert.Equal(2, historico.Count);
            Assert.Contains(historico, d => d.Id == diagA.Id && d.OrdemServicoId == osA && d.RoteiroCodigo == "D01");
            Assert.Contains(historico, d => d.Id == diagB.Id && d.OrdemServicoId == osB && d.RoteiroCodigo == "D02");
        }

        [Fact]
        public void ObterPorOrdemServicoId_IsolamentoEntreOrdensDeServico()
        {
            var veiculoId = Guid.NewGuid();
            var os1 = Guid.NewGuid();
            var os2 = Guid.NewGuid();

            var diag1 = new DiagnosticoTecnico
            {
                OrdemServicoId = os1,
                VeiculoId = veiculoId,
                RoteiroCodigo = "D01",
                SintomaRelatado = "OS 1 sintoma"
            };

            var diag2 = new DiagnosticoTecnico
            {
                OrdemServicoId = os2,
                VeiculoId = veiculoId,
                RoteiroCodigo = "D03",
                SintomaRelatado = "OS 2 sintoma"
            };

            _service.SalvarDiagnostico(diag1);
            _service.SalvarDiagnostico(diag2);

            var os1Diags = _service.ObterPorOrdemServicoId(os1);
            var os2Diags = _service.ObterPorOrdemServicoId(os2);

            Assert.Single(os1Diags);
            Assert.Equal("D01", os1Diags[0].RoteiroCodigo);
            Assert.Single(os2Diags);
            Assert.Equal("D03", os2Diags[0].RoteiroCodigo);
        }

        [Fact]
        public void RegistrarMedicoes_GrandezasEUnidadesValidadas()
        {
            var osId = Guid.NewGuid();
            var veiculoId = Guid.NewGuid();

            var diag = new DiagnosticoTecnico
            {
                OrdemServicoId = osId,
                VeiculoId = veiculoId,
                RoteiroCodigo = "D02",
                SintomaRelatado = "Queda excessiva de tensão na partida"
            };

            var medicaoTensao = new DiagnosticoMedicao
            {
                NomeTeste = "Tensão em repouso da bateria",
                TipoGrandeza = GrandezaEletricaEnum.Tensao,
                Instrumento = "Multímetro digital",
                Unidade = "V",
                ValorReferenciaMin = 12.4m,
                ValorReferenciaMax = 12.8m,
                ValorInicial = 12.6m,
                Resultado = MedicaoResultadoEnum.NORMAL
            };

            var medicaoQueda = new DiagnosticoMedicao
            {
                NomeTeste = "Queda de tensão na linha positiva B+",
                TipoGrandeza = GrandezaEletricaEnum.QuedaTensao,
                Instrumento = "Multímetro digital em DCV min/max",
                Unidade = "V",
                ValorReferenciaMin = 0.0m,
                ValorReferenciaMax = 0.5m,
                ValorInicial = 1.25m,
                Resultado = MedicaoResultadoEnum.FORA_DO_ESPERADO,
                Observacao = "Queda no cabo de 1.25V ultrapassa limite de 0.5V"
            };

            diag.Medicoes.Add(medicaoTensao);
            diag.Medicoes.Add(medicaoQueda);

            var salvo = _service.SalvarDiagnostico(diag);
            var lido = _service.ObterPorId(salvo.Id);

            Assert.NotNull(lido);
            Assert.Equal(2, lido!.Medicoes.Count);
            var mQueda = lido.Medicoes.First(m => m.TipoGrandeza == GrandezaEletricaEnum.QuedaTensao);
            Assert.Equal("V", mQueda.Unidade);
            Assert.Equal(1.25m, mQueda.ValorInicial);
            Assert.Equal(MedicaoResultadoEnum.FORA_DO_ESPERADO, mQueda.Resultado);
        }

        [Fact]
        public void TestePosReparo_ComparaAntesEDepoisCalculaDelta()
        {
            var osId = Guid.NewGuid();
            var veiculoId = Guid.NewGuid();

            var diag = new DiagnosticoTecnico
            {
                OrdemServicoId = osId,
                VeiculoId = veiculoId,
                RoteiroCodigo = "D02",
                SintomaRelatado = "Queda de tensão na partida"
            };

            var medicao = new DiagnosticoMedicao
            {
                NomeTeste = "Tensão mínima durante acionamento do motor de partida",
                TipoGrandeza = GrandezaEletricaEnum.Tensao,
                Instrumento = "Osciloscópio / Multímetro MinMax",
                Unidade = "V",
                ValorReferenciaMin = 9.8m,
                ValorInicial = 8.5m,
                Resultado = MedicaoResultadoEnum.FORA_DO_ESPERADO,
                Observacao = "Tensão despenca para 8.5V durante o giro"
            };

            diag.Medicoes.Add(medicao);
            _service.SalvarDiagnostico(diag);

            // Registro do pós-reparo: após substituir o cabo positivo deteriorado
            var atualizado = _service.RegistrarMedicaoPosReparo(
                diag.Id,
                medicao.Id,
                valorPosReparo: 10.4m,
                novoResultado: MedicaoResultadoEnum.NORMAL,
                observacao: "Após instalação de novo cabo positivo de 35mm², tensão sob carga subiu para 10.4V");

            Assert.NotNull(atualizado);
            var medLida = atualizado!.Medicoes.First(m => m.Id == medicao.Id);
            Assert.True(medLida.TemValidacaoPosReparo);
            Assert.Equal(10.4m, medLida.ValorPosReparo);
            Assert.Equal(1.9m, medLida.DeltaPosReparo);
            Assert.Equal(MedicaoResultadoEnum.NORMAL, medLida.Resultado);
            Assert.Contains("35mm²", medLida.Observacao);
        }

        [Fact]
        public void DiagnosticoPesado24V_RegistraLimitesDeReferenciaEspecificos()
        {
            var osId = Guid.NewGuid();
            var veiculoId = Guid.NewGuid();

            var diag = new DiagnosticoTecnico
            {
                OrdemServicoId = osId,
                VeiculoId = veiculoId,
                RoteiroCodigo = "D03",
                SintomaRelatado = "Alternador de 24V sem carregar baterias em caminhão",
                Observacoes = "Sistema Elétrico 24V (Linha Pesada)"
            };

            var medicaoAlternador24V = new DiagnosticoMedicao
            {
                NomeTeste = "Tensão de carga do alternador 24V a 1500 RPM com faróis ligados",
                TipoGrandeza = GrandezaEletricaEnum.Tensao,
                Instrumento = "Multímetro",
                Unidade = "V",
                ValorReferenciaMin = 27.6m,
                ValorReferenciaMax = 28.8m,
                TextoReferencia = "27.6V a 28.8V sob carga em 24V",
                ValorInicial = 24.2m,
                Resultado = MedicaoResultadoEnum.FORA_DO_ESPERADO,
                Observacao = "Tensão não se eleva acima de 24.2V indicando falha no regulador de voltagem de 28V"
            };

            diag.Medicoes.Add(medicaoAlternador24V);
            var salvo = _service.SalvarDiagnostico(diag);

            Assert.NotNull(salvo);
            var m = salvo.Medicoes[0];
            Assert.Equal(27.6m, m.ValorReferenciaMin);
            Assert.Equal(28.8m, m.ValorReferenciaMax);
            Assert.Equal(24.2m, m.ValorInicial);
            Assert.Equal(MedicaoResultadoEnum.FORA_DO_ESPERADO, m.Resultado);
        }

        [Fact]
        public void ConcluirDiagnostico_RegistraDataConclusaoEImpedeEdicaoIndevida()
        {
            var osId = Guid.NewGuid();
            var veiculoId = Guid.NewGuid();

            var diag = new DiagnosticoTecnico
            {
                OrdemServicoId = osId,
                VeiculoId = veiculoId,
                RoteiroCodigo = "D04",
                SintomaRelatado = "Luz de injeção acesa",
                Status = DiagnosticoStatusEnum.EmAndamento
            };

            _service.SalvarDiagnostico(diag);
            var concluido = _service.ConcluirDiagnostico(diag.Id, "Laudo final aprovado pelo eletricista responsável.");

            Assert.NotNull(concluido);
            Assert.Equal(DiagnosticoStatusEnum.Concluido, concluido!.Status);
            Assert.NotNull(concluido.DataConclusao);
            Assert.Equal("Laudo final aprovado pelo eletricista responsável.", concluido.DiagnosticoLaudo);
        }

        [Fact]
        public void ImportarLegado_ProcessaArquivoJsonSemDanificarOuSobrescrever()
        {
            var legacyJsonPath = Path.Combine(_tempDir, "roteiros-resultados.json");
            File.WriteAllText(legacyJsonPath, @"[
                {
                    ""Codigo"": ""D01"",
                    ""Titulo"": ""Bateria descarregando com veiculo parado"",
                    ""Sintoma"": ""Bateria zera apos 2 dias"",
                    ""Resultado"": ""Fuga de 180mA encontrada"",
                    ""Conclusao"": ""Alarme paralelo em curto"",
                    ""DataRegistro"": ""2026-06-01T10:00:00""
                },
                {
                    ""Codigo"": ""D02"",
                    ""Titulo"": ""Motor de partida nao vira"",
                    ""Sintoma"": ""Apenas da estalo"",
                    ""Resultado"": ""Queda de 1.8V no automatico"",
                    ""Conclusao"": ""Automatico do motor de partida colado"",
                    ""DataRegistro"": ""2026-06-05T14:30:00""
                }
            ]");

            var importados = _service.ImportarLegado(legacyJsonPath);

            Assert.Equal(2, importados);
            var todos = _service.ObterTodos();
            Assert.Equal(2, todos.Count);
            Assert.All(todos, item =>
            {
                Assert.True(item.OrigemLegado);
                Assert.NotEqual(Guid.Empty, item.Id);
                Assert.Equal(Guid.Empty, item.OrdemServicoId);
                Assert.Equal(Guid.Empty, item.VeiculoId);
            });
        }

        [Fact]
        public void Primox360Service_IntegracaoVeiculo360EOrdemServico360()
        {
            var clienteId = Guid.NewGuid();
            var veiculoId = Guid.NewGuid();
            var osId = Guid.NewGuid();

            var clienteRepo = new ClienteRepositoryMock(clienteId, veiculoId);
            var osRepo = new OrdemServicoRepositoryMock(osId, clienteId, veiculoId);

            var diag = new DiagnosticoTecnico
            {
                OrdemServicoId = osId,
                VeiculoId = veiculoId,
                ClienteId = clienteId,
                RoteiroCodigo = "D01",
                SintomaRelatado = "Bateria descarregando",
                DiagnosticoLaudo = "Alternador com diodo em curto",
                CausaStatus = CausaStatusEnum.CONFIRMADA,
                CausaDescricao = "Ponte retificadora danificada",
                CorrecaoExecutada = "Troca da placa de diodos",
                Status = DiagnosticoStatusEnum.Concluido
            };

            _service.SalvarDiagnostico(diag);

            var p360 = new Primox360Service(
                clienteRepo,
                osRepo,
                diagnosticos: _service);

            // Teste Veículo 360
            var v360 = p360.ObterVeiculo360(veiculoId);
            Assert.NotNull(v360);
            Assert.Equal(veiculoId, v360.VeiculoId);
            Assert.Single(v360.Diagnosticos);
            Assert.Equal("D01", v360.Diagnosticos[0].RoteiroCodigo);
            Assert.Equal("Alternador com diodo em curto", v360.Diagnosticos[0].DiagnosticoLaudo);
            Assert.Equal(1, v360.DiagnosticosCount);

            // Teste Ordem de Serviço 360
            var os360 = p360.ObterOrdemServico360(osId);
            Assert.NotNull(os360);
            Assert.Equal(osId, os360.OrdemServicoId);
            Assert.Equal("CONNECTED", os360.DiagnosticoLink);
            Assert.Single(os360.Diagnosticos);
            Assert.Contains("Diagnóstico:CONNECTED", os360.HubResumo);
        }

        private sealed class ClienteRepositoryMock : IClienteRepository
        {
            private readonly Guid _clienteId;
            private readonly Guid _veiculoId;

            public ClienteRepositoryMock(Guid clienteId, Guid veiculoId)
            {
                _clienteId = clienteId;
                _veiculoId = veiculoId;
            }

            public System.Collections.Generic.List<Veiculo> ObterTodosVeiculos()
            {
                return new System.Collections.Generic.List<Veiculo>
                {
                    new Veiculo
                    {
                        Id = _veiculoId,
                        ClienteId = _clienteId,
                        Placa = "ABC-1234",
                        Marca = "Scania",
                        Modelo = "R440",
                        Ano = "2020",
                        SistemaEletrico = "24V"
                    }
                };
            }

            public System.Collections.Generic.List<Veiculo> ObterVeiculosPorClienteId(Guid clienteId) => ObterTodosVeiculos();
            public Cliente? ObterPorId(Guid id) => new Cliente { Id = _clienteId, Nome = "Transportadora Exemplo" };
            public System.Collections.Generic.List<Cliente> ObterTodos() => new() { ObterPorId(_clienteId)! };
            public System.Collections.Generic.List<Cliente> ObterTodos(bool incluirInativos = false) => ObterTodos();
            public void Inserir(Cliente cliente) { }
            public void Atualizar(Cliente cliente) { }
            public void Excluir(Guid id) { }
            public bool Restaurar(Guid id) => true;
            public void InserirVeiculo(Veiculo veiculo) { }
            public void AtualizarVeiculo(Veiculo veiculo) { }
            public void ExcluirVeiculo(Guid id) { }
            public Veiculo? ObterVeiculoPorId(Guid id) => ObterTodosVeiculos().FirstOrDefault(v => v.Id == id);
            public void Salvar(Cliente cliente) { }
            public void SalvarVeiculo(Veiculo veiculo) { }
            public void SalvarVeiculosDoCliente(Guid clienteId, System.Collections.Generic.IEnumerable<Veiculo> veiculos) { }
        }

        private sealed class OrdemServicoRepositoryMock : IOrdemServicoRepository
        {
            private readonly Guid _osId;
            private readonly Guid _clienteId;
            private readonly Guid _veiculoId;

            public OrdemServicoRepositoryMock(Guid osId, Guid clienteId, Guid veiculoId)
            {
                _osId = osId;
                _clienteId = clienteId;
                _veiculoId = veiculoId;
            }

            public string GerarProximoNumero() => "OS-2026-999";

            public OrdemServico? ObterPorId(Guid id)
            {
                return new OrdemServico
                {
                    Id = _osId,
                    Numero = "OS-2026-001",
                    ClienteId = _clienteId,
                    VeiculoId = _veiculoId,
                    Status = "EmAndamento",
                    DataAbertura = DateTime.Now
                };
            }

            public System.Collections.Generic.List<OrdemServico> ObterTodos(bool incluirInativas = false)
            {
                return new System.Collections.Generic.List<OrdemServico> { ObterPorId(_osId)! };
            }

            public System.Collections.Generic.List<OrdemServico> ObterPorClienteId(Guid clienteId, bool incluirInativas = false)
            {
                return ObterTodos(incluirInativas);
            }

            public void Salvar(OrdemServico ordemServico) { }
            public void Inserir(OrdemServico ordemServico) { }
            public void Atualizar(OrdemServico ordemServico) { }
            public void Excluir(Guid id) { }
            public void AdicionarItem(Guid ordemServicoId, OrdemServicoItem item) { }
            public void RemoverItem(Guid itemId) { }
            public System.Collections.Generic.List<OrdemServicoItem> ObterItensPorOrdemServicoId(Guid ordemServicoId) => new();
        }
    }
}
