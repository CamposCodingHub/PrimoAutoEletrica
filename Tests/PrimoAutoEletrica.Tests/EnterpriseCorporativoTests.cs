using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.Fiscal;
using Xunit;

namespace PrimoAutoEletrica.Tests
{
    public class EnterpriseCorporativoTests
    {
        [Fact]
        public async Task MultiFilial_AdicionarEListar_DeveGerenciarMatrizEFiliaisCorretamente()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "primox-test-multifilial-" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            try
            {
                var logger = new LoggerService(tempDir);
                var db = new DatabaseService(tempDir, logger: logger);
                var filialService = new FilialService(db, logger);

                // Inicialmente cria matriz automaticamente
                var filiaisIniciais = await filialService.CarregarFiliaisAsync();
                Assert.NotEmpty(filiaisIniciais);
                Assert.False(filialService.DeveExibirSelecaoFilial()); // Apenas 1 filial, não precisa exibir seletor

                // Adiciona Filial Secundária (Ex: Filial Zona Norte)
                var filialNorte = new Filial
                {
                    Nome = "PRIMOX Unidade Zona Norte",
                    Codigo = "ZN-02",
                    Cnpj = "12.345.678/0002-99",
                    Endereco = "Av. das Nações, 1500",
                    Telefone = "(11) 98888-0002",
                    Cidade = "São Paulo",
                    Estado = "SP",
                    Ativa = true,
                    IsMatriz = false
                };

                await filialService.AdicionarFilialAsync(filialNorte);

                var filiaisAtualizadas = await filialService.CarregarFiliaisAsync();
                Assert.Equal(2, filiaisAtualizadas.Count);
                Assert.True(filialService.DeveExibirSelecaoFilial()); // Agora possui 2+, seletor ativo

                var obtida = filialService.ObterFilialPorId(filialNorte.Id);
                Assert.NotNull(obtida);
                Assert.Equal("PRIMOX Unidade Zona Norte", obtida.Nome);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task TransferenciaEstoque_CicloCompleto_DeveGarantirConsistenciaDeEstoque()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "primox-test-transferencia-" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            try
            {
                var logger = new LoggerService(tempDir);
                var db = new DatabaseService(tempDir, logger: logger);
                var transferenciaService = new TransferenciaEstoqueService(db, logger);

                // Cadastra produto de teste para transferência
                var prodId = 0;
                using (var conn = db.GetConnection())
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO Produtos (Codigo, Nome, PrecoVenda, PrecoCusto, EstoqueAtual, EstoqueMinimo, Categoria, Ativo)
                        VALUES ('ALT-BOSCH-120', 'Alternador Bosch 120A', 850.00, 520.00, 15, 2, 'Alternadores', 1);
                        SELECT last_insert_rowid();";
                    prodId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                var matrizId = Guid.NewGuid();
                var filial2Id = Guid.NewGuid();

                var itens = new List<TransferenciaEstoqueItem>
                {
                    new TransferenciaEstoqueItem
                    {
                        ProdutoId = prodId,
                        Codigo = "ALT-BOSCH-120",
                        Descricao = "Alternador Bosch 120A",
                        QuantidadeEnviada = 5
                    }
                };

                // 1. Solicitar transferência
                var trf = await transferenciaService.SolicitarTransferenciaAsync(
                    matrizId, "Matriz Central",
                    filial2Id, "Filial Express 02",
                    itens, "Supervisor Carlos", "Reposição de alternadores urgentes");

                Assert.Equal(StatusTransferenciaEstoque.Solicitada, trf.Status);
                Assert.Single(trf.Itens);

                // 2. Despachar transferência (deve deduzir estoque)
                var despachado = await transferenciaService.DespacharTransferenciaAsync(trf.Id, "Expedição Almoxarifado");
                Assert.True(despachado);

                var trfDespachada = await transferenciaService.ObterPorIdAsync(trf.Id);
                Assert.NotNull(trfDespachada);
                Assert.Equal(StatusTransferenciaEstoque.EmTransito, trfDespachada.Status);

                // Verifica se abateu 5 unidades (15 - 5 = 10)
                using (var conn = db.GetConnection())
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT EstoqueAtual FROM Produtos WHERE Id = @Id;";
                    var p = cmd.CreateParameter();
                    p.ParameterName = "@Id";
                    p.Value = prodId;
                    cmd.Parameters.Add(p);
                    var estoqueAposDespacho = Convert.ToInt32(cmd.ExecuteScalar());
                    Assert.Equal(10, estoqueAposDespacho);
                }

                // 3. Confirmar recebimento com conferência física
                var conferencias = new Dictionary<int, int> { [prodId] = 5 };
                var recebido = await transferenciaService.ConfirmarRecebimentoAsync(trf.Id, "Almoxarife Destino", conferencias);
                Assert.True(recebido);

                var trfFinal = await transferenciaService.ObterPorIdAsync(trf.Id);
                Assert.NotNull(trfFinal);
                Assert.Equal(StatusTransferenciaEstoque.Recebida, trfFinal.Status);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task GestaoFrotas_ContratoEAlertasPreventivos_DeveOperarComPrecisao()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "primox-test-frotas-" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            try
            {
                var logger = new LoggerService(tempDir);
                var db = new DatabaseService(tempDir, logger: logger);
                var frotasService = new GestaoFrotasService(db, logger);

                // 1. Criar Contrato B2B com Transportadora
                var contrato = new ContratoFrota
                {
                    ClienteId = 101,
                    ClienteNome = "TransLogística Brasil S/A",
                    NumeroContrato = "CTR-FROT-2026-001",
                    Descricao = "Manutenção Elétrica de Frota Pesada",
                    DescontoPecasPercentual = 15.0m,
                    DescontoServicosPercentual = 20.0m,
                    ValorHoraTecnicaNegociada = 140.00m,
                    DiaFechamentoFatura = 28,
                    DiasVencimentoBoleto = 15,
                    Status = StatusContratoFrota.Ativo
                };

                await frotasService.CriarContratoAsync(contrato);
                var salvo = await frotasService.ObterContratoPorIdAsync(contrato.Id);
                Assert.NotNull(salvo);
                Assert.Equal("TransLogística Brasil S/A", salvo.ClienteNome);

                // 2. Vincular veículos da frota
                // Veículo 1: Manutenção em dia (KM 42.000, próx: 50.000)
                var v1 = new VeiculoFrota
                {
                    VeiculoId = 501,
                    ContratoFrotaId = contrato.Id,
                    PrefixoFrota = "CAM-01",
                    Placa = "BRA2E19",
                    KmAtual = 42000,
                    UltimaRevisaoKm = 40000,
                    IntervaloRevisaoKm = 10000,
                    Ativo = true
                };
                await frotasService.VincularVeiculoFrotaAsync(v1);

                // Veículo 2: Manutenção vencida (KM 61.500, próx: 60.000)
                var v2 = new VeiculoFrota
                {
                    VeiculoId = 502,
                    ContratoFrotaId = contrato.Id,
                    PrefixoFrota = "CARRETA-88",
                    Placa = "RST9H44",
                    KmAtual = 61500,
                    UltimaRevisaoKm = 50000,
                    IntervaloRevisaoKm = 10000,
                    Ativo = true
                };
                await frotasService.VincularVeiculoFrotaAsync(v2);

                // 3. Verificar Alertas Preventivos
                var alertas = await frotasService.VerificarAlertasManutencaoPreventivaAsync();
                Assert.Single(alertas);
                Assert.Equal("RST9H44", alertas[0].Placa);
                Assert.Equal(CriticidadeRevisao.Vencida, alertas[0].Criticidade);
                Assert.Contains("ALERTA CRÍTICO", alertas[0].MensagemAlerta);

                // 4. Teste de Token de Aprovação Remota
                var token = frotasService.GerarTokenAprovacaoRemota("OS-2026-904", "RST9H44", 2450.00m, validadeHoras: 24);
                Assert.NotNull(token);

                var validacaoToken = frotasService.ValidarTokenAprovacaoRemota(token);
                Assert.True(validacaoToken.Valido);
                Assert.Equal("OS-2026-904", validacaoToken.OrdemServicoId);
                Assert.Equal("RST9H44", validacaoToken.Placa);
                Assert.Equal(2450.00m, validacaoToken.Valor);

                // Token adulterado deve ser rejeitado imediatamente
                var tokenAdulterado = token.Substring(0, token.Length - 4) + "AAAA";
                var validacaoInvalida = frotasService.ValidarTokenAprovacaoRemota(tokenAdulterado);
                Assert.False(validacaoInvalida.Valido);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void LicenseService_CriptografiaEAntiTampering_DeveBloquearAdulteracao()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "primox-test-license-" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            try
            {
                var logger = new LoggerService(tempDir);
                var licenseService = new LicenseService(tempDir, logger);

                // HWID deve ser consistente
                var hwid1 = licenseService.GenerateHardwareId();
                var hwid2 = licenseService.GenerateHardwareId();
                Assert.Equal(hwid1, hwid2);
                Assert.False(string.IsNullOrWhiteSpace(hwid1));

                // Validação inicial deve fornecer licença Trial assinada
                var validacao = licenseService.ValidateLicense();
                Assert.True(validacao.IsValid);
                Assert.NotNull(validacao.License);
                Assert.Equal(LicenseType.Trial, validacao.License.Type);
                Assert.True(licenseService.ValidarAssinaturaLicenca(validacao.License));

                // Simulação de tentativa de fraude / adulteração por usuário malicioso
                var trialPath = Path.Combine(tempDir, "Config", "license_trial.json");
                Assert.True(File.Exists(trialPath));

                // Altera arquivo para estender validade sem a chave mestra HMAC
                var jsonOriginal = File.ReadAllText(trialPath);
                var jsonFraudado = jsonOriginal.Replace("\"Type\": 0", "\"Type\": 3"); // Muda Trial para Enterprise
                File.WriteAllText(trialPath, jsonFraudado);

                // Validação deve detectar adulteração imediatamente
                var validacaoFraude = licenseService.ValidateLicense(validacao.License.LicenseKey);
                Assert.False(validacaoFraude.IsValid);
                Assert.Contains("adulterado", validacaoFraude.Message);

                // Ativação legítima por Token Criptografado
                var novaLicencaEnterprise = new LicenseInfo
                {
                    LicenseKey = "PRMX-ENT-2026-9999",
                    CompanyName = "Rede de Oficinas AutoElétrica Brasil",
                    Cnpj = "11.222.333/0001-44",
                    PurchaseDate = DateTime.Now,
                    ExpirationDate = DateTime.Now.AddYears(2),
                    Type = LicenseType.Enterprise,
                    MaxUsers = 100,
                    MaxComputers = 50,
                    MaxFiliais = 10,
                    IsActive = true,
                    HardwareId = hwid1
                };

                var tokenAtivacao = licenseService.GerarTokenAtivacaoCompacto(novaLicencaEnterprise);
                Assert.StartsWith("PRMX-", tokenAtivacao);

                var resultadoAtivacao = licenseService.AtivarComToken(tokenAtivacao);
                Assert.True(resultadoAtivacao.IsValid);
                Assert.Equal(LicenseType.Enterprise, resultadoAtivacao.License?.Type);
                Assert.Equal(10, resultadoAtivacao.License?.MaxFiliais);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task GatewayFiscal_NfseENfceECupomTermico_DeveGerarDocumentosFiscaisCorretos()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "primox-test-fiscal-" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            try
            {
                var logger = new LoggerService(tempDir);
                var configService = new FiscalConfigurationService(tempDir, logger);
                var gateway = new GatewayFiscalCorporativoService(configService, logger);

                // 1. Emissão de NFS-e Municipal para Mão de Obra
                var reqNfse = new NfseRequisicao
                {
                    NumeroOS = "OS-2026-440",
                    OrdemServicoId = 440,
                    CnpjPrestador = "11.222.333/0001-44",
                    InscricaoMunicipalPrestador = "987654-1",
                    TomadorNome = "Transportes Rápido Ltda",
                    TomadorDocumento = "44.555.666/0001-77",
                    ValorServicos = 750.00m,
                    AliquotaIss = 3.0m,
                    DiscriminacaoServicos = "Revisão e reparo de alternador e chicote elétrico de caminhão Volvo FH."
                };

                var respNfse = await gateway.EmitirNfseMaoDeObraAsync(reqNfse);
                Assert.True(respNfse.Sucesso);
                Assert.Equal(FiscalDocumentStatus.Authorized, respNfse.Status);
                Assert.False(string.IsNullOrWhiteSpace(respNfse.NumeroNfse));
                Assert.False(string.IsNullOrWhiteSpace(respNfse.CodigoVerificacao));

                // 2. Emissão de NFC-e Balcão com Itens
                var reqNfce = new NfceRequisicao
                {
                    FormaPagamento = "PIX",
                    ValorTotal = 320.00m,
                    Itens = new List<NfceItemRequisicao>
                    {
                        new NfceItemRequisicao
                        {
                            NumeroItem = 1,
                            CodigoProduto = "BAT-MOURA-60",
                            Descricao = "Bateria Moura 60Ah",
                            Quantidade = 1,
                            ValorUnitario = 320.00m
                        }
                    }
                };

                var respNfce = await gateway.EmitirNfceVendaBalcaoAsync(reqNfce);
                Assert.True(respNfce.Sucesso);
                Assert.Equal(FiscalDocumentStatus.Authorized, respNfce.Status);
                Assert.NotNull(respNfce.ChaveAcesso);
                Assert.Equal(44, respNfce.ChaveAcesso?.Length);
                Assert.NotNull(respNfce.UrlQrCode);

                // 3. Formatação do Cupom Térmico NFC-e
                var cupom = gateway.FormatarCupomTermicoNfce(
                    reqNfce, respNfce,
                    "PRIMOX AUTO ELÉTRICA LTDA",
                    "11.222.333/0001-44",
                    "Av. Central das Oficinas, 120");

                Assert.Contains("DANFE NFC-e", cupom);
                Assert.Contains("Bateria Moura", cupom);
                Assert.Contains("PIX", cupom);
                Assert.Contains("CHAVE DE ACESSO", cupom);
                Assert.Contains("PRIMOX AUTO ELÉTRICA ENTERPRISE", cupom);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public async Task EtiquetaTermicaZpl_GeracaoFormatosIndustriais_DeveProduzirComandosValidos()
        {
            var logger = new LoggerService(Path.GetTempPath());
            var etiquetaService = new EtiquetaTermicaZplService(logger);

            // 1. Etiqueta de Chave de Veículo da OS
            var zplChave = etiquetaService.GerarZplChaveVeiculo(
                numeroOs: "OS-8821",
                placa: "KXZ-9B41",
                cliente: "Roberto Silva",
                modeloVeiculo: "VW Constellation 24-280",
                tecnico: "Marcio Eletricista",
                dataEntrada: new DateTime(2026, 10, 3, 14, 30, 0));

            Assert.StartsWith("^XA", zplChave);
            Assert.Contains("KXZ-9B41", zplChave);
            Assert.Contains("OS-8821", zplChave);
            Assert.Contains("^BCN", zplChave); // Código de barras Code 128
            Assert.EndsWith("^XZ\n", zplChave.Replace("\r", ""));

            // 2. Etiqueta de Peça / Almoxarifado
            var zplPeca = etiquetaService.GerarZplPecaAlmoxarifado(
                codigo: "REG-VALEO-14V",
                descricao: "Regulador de Voltagem Valeo 14V",
                precoVenda: 185.50m,
                codigoBarras: "7891234567890",
                localizacao: "GAVETA-B12",
                marca: "VALEO");

            Assert.Contains("REG-VALEO-14V", zplPeca);
            Assert.Contains("VALEO", zplPeca);
            Assert.Contains("GAVETA-B12", zplPeca);
            Assert.Contains("185,50", zplPeca);
            Assert.Contains("7891234567890", zplPeca);

            // 3. Etiqueta de Ferramenta / Ativo
            var zplFerramenta = etiquetaService.GerarZplFerramentaAtivo(
                codigoPatrimonio: "FER-2026-0042",
                nomeFerramenta: "Osciloscópio Automotivo 8CH",
                categoria: "Diagnóstico",
                localizacao: "Armário A - Prateleira 2");

            Assert.Contains("FER-2026-0042", zplFerramenta);
            Assert.Contains("Osciloscópio Automotivo", zplFerramenta);
            Assert.Contains("^BQN", zplFerramenta); // QR Code industrial ZPL

            // 4. Salvar arquivo ZPL
            var caminhoSalvo = await etiquetaService.SalvarArquivoZplAsync(zplChave, "etiqueta_chave_teste");
            Assert.True(File.Exists(caminhoSalvo));
            if (File.Exists(caminhoSalvo))
            {
                File.Delete(caminhoSalvo);
            }
        }
    }
}
