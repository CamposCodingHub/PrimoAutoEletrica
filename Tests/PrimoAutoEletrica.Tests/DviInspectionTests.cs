using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Models.Dvi;
using PrimoAutoEletrica.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace PrimoAutoEletrica.Tests
{
    public class DviInspectionTests : IDisposable
    {
        private readonly string _testDir;
        private readonly DatabaseService _databaseService;
        private readonly DviInspectionService _dviService;

        public DviInspectionTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "primox_dvi_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);

            _databaseService = new DatabaseService(_testDir);
            _dviService = new DviInspectionService(_databaseService);
        }

        public void Dispose()
        {
            try
            {
                SqliteConnection.ClearAllPools();
                if (Directory.Exists(_testDir))
                {
                    Directory.Delete(_testDir, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup lock in temp dir
            }
        }

        [Fact]
        public async Task CriarEBuscarInspecao_DevePersistirComSucesso()
        {
            // Arrange
            var inspecao = new InspecaoDvi
            {
                PlacaVeiculo = "ABC1D23",
                ModeloVeiculo = "Chevrolet Onix 1.4 2018",
                ClienteNome = "Carlos Alberto Eletricista",
                ClienteTelefone = "(11) 98765-4321",
                ResponsavelTecnico = "Técnico Lucas",
                ObservacoesGerais = "Veículo apresentava corte de ignição intermitente."
            };

            var checklist = _dviService.GerarChecklistPadraoAutoEletrica(0);
            checklist[0].Severidade = DviStatusSeveridade.Critico;
            checklist[0].ObservacaoTecnica = "Bateria com 11.9V em repouso e queda para 8.5V na partida.";
            checklist[0].ValorEstimadoReparo = 450.00m;

            checklist[1].Severidade = DviStatusSeveridade.Atencao;
            checklist[1].ObservacaoTecnica = "Alternador gerando 13.5V com carga total.";
            checklist[1].ValorEstimadoReparo = 180.00m;

            inspecao.Itens = checklist;

            // Act
            var salva = await _dviService.SalvarInspecaoAsync(inspecao);
            var recuperada = await _dviService.ObterPorIdAsync(salva.Id);
            var porPlaca = await _dviService.ListarPorPlacaAsync("ABC1D23");

            // Assert
            Assert.True(salva.Id > 0);
            Assert.NotNull(recuperada);
            Assert.Equal("ABC1D23", recuperada!.PlacaVeiculo);
            Assert.Equal("Carlos Alberto Eletricista", recuperada.ClienteNome);
            Assert.Equal(630.00m, recuperada.ValorTotalEstimado);
            Assert.Equal(1, recuperada.TotalItensCriticos);
            Assert.Equal(1, recuperada.TotalItensAtencao);
            Assert.NotEmpty(porPlaca);
            Assert.Equal(salva.Id, porPlaca[0].Id);
        }

        [Fact]
        public void GerarChecklistPadrao_DeveConterCategoriasEletricasEssenciais()
        {
            // Act
            var checklist = _dviService.GerarChecklistPadraoAutoEletrica(0);

            // Assert
            Assert.NotNull(checklist);
            Assert.True(checklist.Count >= 16);
            Assert.Contains(checklist, i => i.Categoria.Contains("Elétrica & Bateria"));
            Assert.Contains(checklist, i => i.Categoria.Contains("Iluminação & Sinalização"));
            Assert.Contains(checklist, i => i.Categoria.Contains("Chicotes & Fusíveis"));
            Assert.Contains(checklist, i => i.Categoria.Contains("Injeção & Arrefecimento"));
            Assert.Contains(checklist, i => i.Categoria.Contains("Carroceria & Entrada"));
        }

        [Fact]
        public void MontarResumoTextoLaudoEWhatsApp_DeveFormatarCorretamente()
        {
            // Arrange
            var inspecao = new InspecaoDvi
            {
                PlacaVeiculo = "KRT-8840",
                ModeloVeiculo = "Fiat Uno Mille Fire",
                ClienteNome = "Mariana Ramos",
                ClienteTelefone = "11977778888",
                ResponsavelTecnico = "Marcos",
                OrdemServicoId = "1055"
            };

            inspecao.Itens.Add(new InspecaoDviItem
            {
                NomeItem = "Relé da Ventoinha T02",
                Severidade = DviStatusSeveridade.Critico,
                ObservacaoTecnica = "Relé com terminais 87 e 30 derretidos por sobreaquecimento.",
                ValorEstimadoReparo = 85.00m
            });

            inspecao.Itens.Add(new InspecaoDviItem
            {
                NomeItem = "Bateria 60Ah",
                Severidade = DviStatusSeveridade.Atencao,
                ObservacaoTecnica = "Carga em 45%, sugerida recarga lenta.",
                ValorEstimadoReparo = 50.00m
            });

            inspecao.Itens.Add(new InspecaoDviItem
            {
                NomeItem = "Faróis",
                Severidade = DviStatusSeveridade.Ok
            });

            // Act
            var texto = _dviService.MontarResumoTextoLaudo(inspecao);
            var linkWa = _dviService.GerarLinkWhatsApp(inspecao);

            // Assert
            Assert.Contains("LAUDO DE INSPEÇÃO DIGITAL VEICULAR (DVI 2.0)", texto);
            Assert.Contains("KRT-8840", texto);
            Assert.Contains("ITENS CRÍTICOS / URGENTES (1)", texto);
            Assert.Contains("Relé da Ventoinha T02", texto);
            Assert.Contains("ITENS PREVENTIVOS / ATENÇÃO (1)", texto);
            Assert.Contains("ITENS CONFORMES / APROVADOS: 1", texto);
            Assert.Contains("https://wa.me/5511977778888?text=", linkWa);
        }

        [Fact]
        public async Task ConverterItensParaOrdemServico_DeveGravarItensNaTabelaOS()
        {
            // Arrange: Cria registro prévio de OS no SQLite
            using (var conn = _databaseService.GetConnection())
            {
                conn.Open();
                using var cmdOs = conn.CreateCommand();
                cmdOs.CommandText = @"
                    CREATE TABLE IF NOT EXISTS OrdensServico (
                        Id TEXT PRIMARY KEY,
                        Numero TEXT NOT NULL,
                        ClienteId TEXT NOT NULL,
                        Status TEXT NOT NULL,
                        Prioridade TEXT NOT NULL,
                        DataAbertura TEXT NOT NULL
                    );
                    INSERT INTO OrdensServico (Id, Numero, ClienteId, Status, Prioridade, DataAbertura)
                    VALUES ('OS-TESTE-99', '2026/0099', 'CLI-01', 'Aberta', 'Normal', '2026-10-04T12:00:00');
                ";
                cmdOs.ExecuteNonQuery();
            }

            var inspecao = new InspecaoDvi
            {
                PlacaVeiculo = "TEST-1234",
                ModeloVeiculo = "VW Gol 1.6",
                OrdemServicoId = "OS-TESTE-99"
            };

            inspecao.Itens.Add(new InspecaoDviItem
            {
                NomeItem = "Alternador 90A",
                Severidade = DviStatusSeveridade.Critico,
                ObservacaoTecnica = "Placa de diodos em curto.",
                ValorEstimadoReparo = 320.00m
            });

            var salva = await _dviService.SalvarInspecaoAsync(inspecao);

            // Act
            var inseridos = await _dviService.ConverterItensParaOrdemServicoAsync(salva.Id, "OS-TESTE-99");

            // Assert
            Assert.Equal(1, inseridos);

            using (var conn = _databaseService.GetConnection())
            {
                conn.Open();
                using var cmdQuery = conn.CreateCommand();
                cmdQuery.CommandText = "SELECT COUNT(*), Descricao, ValorUnitario FROM OrdemServicoItens WHERE OrdemServicoId = 'OS-TESTE-99';";
                using var reader = cmdQuery.ExecuteReader();
                Assert.True(reader.Read());
                Assert.Equal(1, reader.GetInt32(0));
                Assert.Contains("[DVI] Alternador 90A", reader.GetString(1));
                Assert.Equal(320.0, reader.GetDouble(2));
            }
        }

        [Fact]
        public async Task ConverterItensParaOrdemServico_EmOsEntregue_DeveLancarExcecao()
        {
            // Arrange
            using (var conn = _databaseService.GetConnection())
            {
                conn.Open();
                using var cmdOs = conn.CreateCommand();
                cmdOs.CommandText = @"
                    CREATE TABLE IF NOT EXISTS OrdensServico (
                        Id TEXT PRIMARY KEY,
                        Numero TEXT NOT NULL,
                        ClienteId TEXT NOT NULL,
                        Status TEXT NOT NULL,
                        Prioridade TEXT NOT NULL,
                        DataAbertura TEXT NOT NULL
                    );
                    INSERT INTO OrdensServico (Id, Numero, ClienteId, Status, Prioridade, DataAbertura)
                    VALUES ('OS-FECHADA-01', '2026/0100', 'CLI-01', 'Entregue', 'Normal', '2026-10-04T12:00:00');
                ";
                cmdOs.ExecuteNonQuery();
            }

            var inspecao = new InspecaoDvi
            {
                PlacaVeiculo = "BLOCKED-01",
                ModeloVeiculo = "Toyota Corolla"
            };
            inspecao.Itens.Add(new InspecaoDviItem
            {
                NomeItem = "Bateria",
                Severidade = DviStatusSeveridade.Critico
            });
            var salva = await _dviService.SalvarInspecaoAsync(inspecao);

            // Act & Assert: Não deve permitir inserir em OS Entregue
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _dviService.ConverterItensParaOrdemServicoAsync(salva.Id, "OS-FECHADA-01");
            });
        }

        [Fact]
        public async Task SalvarFotoComCompressao_DeveGerarArquivoJpegERegistro()
        {
            // Arrange: cria uma imagem bitmap em memória via ImageSharp
            byte[] imageBytes;
            using (var image = new Image<Rgba32>(1600, 1200))
            {
                using var ms = new MemoryStream();
                image.SaveAsPng(ms);
                imageBytes = ms.ToArray();
            }

            var inspecao = new InspecaoDvi
            {
                PlacaVeiculo = "FOTO-9999",
                ModeloVeiculo = "Ford Ka 1.0"
            };
            inspecao.Itens.Add(new InspecaoDviItem
            {
                NomeItem = "Chicote do Bico 1",
                Severidade = DviStatusSeveridade.Critico
            });
            var salva = await _dviService.SalvarInspecaoAsync(inspecao);
            var itemSalvo = salva.Itens[0];

            // Act: salva e comprime
            var foto = await _dviService.SalvarFotoAsync(itemSalvo.Id, imageBytes, "chicote_avariado.png", "Terminal oxidado");

            // Assert
            Assert.True(foto.Id > 0);
            Assert.True(File.Exists(foto.CaminhoArquivo));

            // Valida dimensões comprimidas (max 1280px)
            var info = Image.Identify(foto.CaminhoArquivo);
            Assert.True(info.Width <= 1280);
            Assert.True(info.Height <= 960);

            // Act: Excluir foto
            var deletado = await _dviService.ExcluirFotoAsync(foto.Id);
            Assert.True(deletado);
            Assert.False(File.Exists(foto.CaminhoArquivo));
        }
    }
}
