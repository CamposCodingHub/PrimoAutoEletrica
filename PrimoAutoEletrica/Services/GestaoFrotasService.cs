using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public interface IGestaoFrotasService
    {
        Task<ContratoFrota> CriarContratoAsync(ContratoFrota contrato);
        Task AtualizarContratoAsync(ContratoFrota contrato);
        Task<ContratoFrota?> ObterContratoPorIdAsync(Guid id);
        Task<ContratoFrota?> ObterContratoPorNumeroAsync(string numeroContrato);
        Task<List<ContratoFrota>> ListarContratosAsync(bool apenasAtivos = false);

        Task<VeiculoFrota> VincularVeiculoFrotaAsync(VeiculoFrota veiculo);
        Task AtualizarVeiculoFrotaAsync(VeiculoFrota veiculo);
        Task AtualizarTelemetriaKmAsync(Guid veiculoFrotaId, int kmAtual, int? horimetroAtual = null);
        Task<List<VeiculoFrota>> ListarVeiculosPorContratoAsync(Guid contratoFrotaId);
        Task<VeiculoFrota?> ObterVeiculoFrotaPorPlacaAsync(string placa);

        Task<List<AlertaManutencaoFrota>> VerificarAlertasManutencaoPreventivaAsync();
        Task<FaturaFrota> FecharFaturaPeriodicaAsync(Guid contratoId, DateTime dataInicio, DateTime dataFim, string usuarioResponsavel = "Sistema");
        Task<List<FaturaFrota>> ListarFaturasPorContratoAsync(Guid contratoId);
        Task<FaturaFrota?> ObterFaturaPorIdAsync(Guid faturaId);
        Task<bool> AtualizarStatusFaturaAsync(Guid faturaId, StatusFaturaFrota novoStatus);

        string GerarTokenAprovacaoRemota(string ordemServicoId, string placa, decimal valor, int validadeHoras = 48);
        (bool Valido, string OrdemServicoId, string Placa, decimal Valor) ValidarTokenAprovacaoRemota(string token);
    }

    public sealed class GestaoFrotasService : IGestaoFrotasService
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggerService? _logger;
        private static readonly byte[] TokenSigningKey = Encoding.UTF8.GetBytes("PRIMOX-FLEET-B2B-SECURE-HMAC-2026-KEY!#987");

        public GestaoFrotasService(DatabaseService databaseService, LoggerService? logger = null)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger;
        }

        private DbConnection ObterConexaoAberta()
        {
            var conn = _databaseService.GetConnection();
            conn.Open();
            return conn;
        }

        public async Task<ContratoFrota> CriarContratoAsync(ContratoFrota contrato)
        {
            if (contrato == null) throw new ArgumentNullException(nameof(contrato));
            if (string.IsNullOrWhiteSpace(contrato.ClienteNome))
                throw new ArgumentException("Cliente é obrigatório.", nameof(contrato));

            if (string.IsNullOrWhiteSpace(contrato.NumeroContrato))
            {
                contrato.NumeroContrato = $"CTR-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpperInvariant()}";
            }

            return await Task.Run(() =>
            {
                using var conn = ObterConexaoAberta();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO ContratosFrotas
                    (
                        Id, ClienteId, ClienteNome, NumeroContrato, Descricao,
                        DataInicio, DataVencimento, DescontoPecasPercentual, DescontoServicosPercentual,
                        ValorHoraTecnicaNegociada, DiaFechamentoFatura, DiasVencimentoBoleto,
                        LimiteCreditoMensal, ExigeAutorizacaoPrevia, Status, Observacoes, DataCadastro
                    )
                    VALUES
                    (
                        @Id, @ClienteId, @ClienteNome, @NumeroContrato, @Descricao,
                        @DataInicio, @DataVencimento, @DescontoPecasPercentual, @DescontoServicosPercentual,
                        @ValorHoraTecnicaNegociada, @DiaFechamentoFatura, @DiasVencimentoBoleto,
                        @LimiteCreditoMensal, @ExigeAutorizacaoPrevia, @Status, @Observacoes, @DataCadastro
                    );";

                cmd.Parameters.AddWithValue("@Id", contrato.Id.ToString());
                cmd.Parameters.AddWithValue("@ClienteId", contrato.ClienteId);
                cmd.Parameters.AddWithValue("@ClienteNome", contrato.ClienteNome);
                cmd.Parameters.AddWithValue("@NumeroContrato", contrato.NumeroContrato);
                cmd.Parameters.AddWithValue("@Descricao", (object?)contrato.Descricao ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DataInicio", contrato.DataInicio.ToString("o"));
                cmd.Parameters.AddWithValue("@DataVencimento", contrato.DataVencimento.ToString("o"));
                cmd.Parameters.AddWithValue("@DescontoPecasPercentual", Convert.ToDouble(contrato.DescontoPecasPercentual));
                cmd.Parameters.AddWithValue("@DescontoServicosPercentual", Convert.ToDouble(contrato.DescontoServicosPercentual));
                cmd.Parameters.AddWithValue("@ValorHoraTecnicaNegociada", Convert.ToDouble(contrato.ValorHoraTecnicaNegociada));
                cmd.Parameters.AddWithValue("@DiaFechamentoFatura", contrato.DiaFechamentoFatura);
                cmd.Parameters.AddWithValue("@DiasVencimentoBoleto", contrato.DiasVencimentoBoleto);
                cmd.Parameters.AddWithValue("@LimiteCreditoMensal", Convert.ToDouble(contrato.LimiteCreditoMensal));
                cmd.Parameters.AddWithValue("@ExigeAutorizacaoPrevia", contrato.ExigeAutorizacaoPrevia ? 1 : 0);
                cmd.Parameters.AddWithValue("@Status", (int)contrato.Status);
                cmd.Parameters.AddWithValue("@Observacoes", (object?)contrato.Observacoes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DataCadastro", contrato.DataCadastro.ToString("o"));

                cmd.ExecuteNonQuery();
                _logger?.LogInfo($"[GestaoFrotas] Contrato {contrato.NumeroContrato} criado com sucesso para o cliente {contrato.ClienteNome}.");
                return contrato;
            });
        }

        public async Task AtualizarContratoAsync(ContratoFrota contrato)
        {
            if (contrato == null) throw new ArgumentNullException(nameof(contrato));

            await Task.Run(() =>
            {
                using var conn = ObterConexaoAberta();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE ContratosFrotas SET
                        ClienteId = @ClienteId,
                        ClienteNome = @ClienteNome,
                        Descricao = @Descricao,
                        DataInicio = @DataInicio,
                        DataVencimento = @DataVencimento,
                        DescontoPecasPercentual = @DescontoPecasPercentual,
                        DescontoServicosPercentual = @DescontoServicosPercentual,
                        ValorHoraTecnicaNegociada = @ValorHoraTecnicaNegociada,
                        DiaFechamentoFatura = @DiaFechamentoFatura,
                        DiasVencimentoBoleto = @DiasVencimentoBoleto,
                        LimiteCreditoMensal = @LimiteCreditoMensal,
                        ExigeAutorizacaoPrevia = @ExigeAutorizacaoPrevia,
                        Status = @Status,
                        Observacoes = @Observacoes
                    WHERE Id = @Id;";

                cmd.Parameters.AddWithValue("@Id", contrato.Id.ToString());
                cmd.Parameters.AddWithValue("@ClienteId", contrato.ClienteId);
                cmd.Parameters.AddWithValue("@ClienteNome", contrato.ClienteNome);
                cmd.Parameters.AddWithValue("@Descricao", (object?)contrato.Descricao ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DataInicio", contrato.DataInicio.ToString("o"));
                cmd.Parameters.AddWithValue("@DataVencimento", contrato.DataVencimento.ToString("o"));
                cmd.Parameters.AddWithValue("@DescontoPecasPercentual", Convert.ToDouble(contrato.DescontoPecasPercentual));
                cmd.Parameters.AddWithValue("@DescontoServicosPercentual", Convert.ToDouble(contrato.DescontoServicosPercentual));
                cmd.Parameters.AddWithValue("@ValorHoraTecnicaNegociada", Convert.ToDouble(contrato.ValorHoraTecnicaNegociada));
                cmd.Parameters.AddWithValue("@DiaFechamentoFatura", contrato.DiaFechamentoFatura);
                cmd.Parameters.AddWithValue("@DiasVencimentoBoleto", contrato.DiasVencimentoBoleto);
                cmd.Parameters.AddWithValue("@LimiteCreditoMensal", Convert.ToDouble(contrato.LimiteCreditoMensal));
                cmd.Parameters.AddWithValue("@ExigeAutorizacaoPrevia", contrato.ExigeAutorizacaoPrevia ? 1 : 0);
                cmd.Parameters.AddWithValue("@Status", (int)contrato.Status);
                cmd.Parameters.AddWithValue("@Observacoes", (object?)contrato.Observacoes ?? DBNull.Value);

                cmd.ExecuteNonQuery();
            });
        }

        public async Task<ContratoFrota?> ObterContratoPorIdAsync(Guid id)
        {
            return await Task.Run(() =>
            {
                using var conn = ObterConexaoAberta();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM ContratosFrotas WHERE Id = @Id LIMIT 1;";
                cmd.Parameters.AddWithValue("@Id", id.ToString());

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return MapearContrato(reader);
                }
                return null;
            });
        }

        public async Task<ContratoFrota?> ObterContratoPorNumeroAsync(string numeroContrato)
        {
            return await Task.Run(() =>
            {
                using var conn = ObterConexaoAberta();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM ContratosFrotas WHERE NumeroContrato = @Numero LIMIT 1;";
                cmd.Parameters.AddWithValue("@Numero", numeroContrato);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return MapearContrato(reader);
                }
                return null;
            });
        }

        public async Task<List<ContratoFrota>> ListarContratosAsync(bool apenasAtivos = false)
        {
            return await Task.Run(() =>
            {
                var lista = new List<ContratoFrota>();
                using var conn = ObterConexaoAberta();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = apenasAtivos
                    ? "SELECT * FROM ContratosFrotas WHERE Status = 1 ORDER BY ClienteNome ASC;"
                    : "SELECT * FROM ContratosFrotas ORDER BY ClienteNome ASC;";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    lista.Add(MapearContrato(reader));
                }
                return lista;
            });
        }

        public async Task<VeiculoFrota> VincularVeiculoFrotaAsync(VeiculoFrota veiculo)
        {
            if (veiculo == null) throw new ArgumentNullException(nameof(veiculo));

            return await Task.Run(() =>
            {
                using var conn = ObterConexaoAberta();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO VeiculosFrotas
                    (
                        Id, VeiculoId, ContratoFrotaId, PrefixoFrota, Placa,
                        MarcaModelo, MotoristaResponsavel, CentroCusto, KmAtual,
                        HorimetroAtual, UltimaRevisaoKm, IntervaloRevisaoKm, Ativo
                    )
                    VALUES
                    (
                        @Id, @VeiculoId, @ContratoFrotaId, @PrefixoFrota, @Placa,
                        @MarcaModelo, @MotoristaResponsavel, @CentroCusto, @KmAtual,
                        @HorimetroAtual, @UltimaRevisaoKm, @IntervaloRevisaoKm, @Ativo
                    );";

                cmd.Parameters.AddWithValue("@Id", veiculo.Id.ToString());
                cmd.Parameters.AddWithValue("@VeiculoId", veiculo.VeiculoId);
                cmd.Parameters.AddWithValue("@ContratoFrotaId", veiculo.ContratoFrotaId.ToString());
                cmd.Parameters.AddWithValue("@PrefixoFrota", veiculo.PrefixoFrota);
                cmd.Parameters.AddWithValue("@Placa", veiculo.Placa.ToUpperInvariant());
                cmd.Parameters.AddWithValue("@MarcaModelo", (object?)veiculo.MarcaModelo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MotoristaResponsavel", (object?)veiculo.MotoristaResponsavel ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CentroCusto", (object?)veiculo.CentroCusto ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@KmAtual", veiculo.KmAtual);
                cmd.Parameters.AddWithValue("@HorimetroAtual", veiculo.HorimetroAtual);
                cmd.Parameters.AddWithValue("@UltimaRevisaoKm", veiculo.UltimaRevisaoKm);
                cmd.Parameters.AddWithValue("@IntervaloRevisaoKm", veiculo.IntervaloRevisaoKm);
                cmd.Parameters.AddWithValue("@Ativo", veiculo.Ativo ? 1 : 0);

                cmd.ExecuteNonQuery();
                return veiculo;
            });
        }

        public async Task AtualizarVeiculoFrotaAsync(VeiculoFrota veiculo)
        {
            if (veiculo == null) throw new ArgumentNullException(nameof(veiculo));

            await Task.Run(() =>
            {
                using var conn = ObterConexaoAberta();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE VeiculosFrotas SET
                        PrefixoFrota = @PrefixoFrota,
                        Placa = @Placa,
                        MarcaModelo = @MarcaModelo,
                        MotoristaResponsavel = @MotoristaResponsavel,
                        CentroCusto = @CentroCusto,
                        KmAtual = @KmAtual,
                        HorimetroAtual = @HorimetroAtual,
                        UltimaRevisaoKm = @UltimaRevisaoKm,
                        IntervaloRevisaoKm = @IntervaloRevisaoKm,
                        Ativo = @Ativo
                    WHERE Id = @Id;";

                cmd.Parameters.AddWithValue("@Id", veiculo.Id.ToString());
                cmd.Parameters.AddWithValue("@PrefixoFrota", veiculo.PrefixoFrota);
                cmd.Parameters.AddWithValue("@Placa", veiculo.Placa.ToUpperInvariant());
                cmd.Parameters.AddWithValue("@MarcaModelo", (object?)veiculo.MarcaModelo ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MotoristaResponsavel", (object?)veiculo.MotoristaResponsavel ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@CentroCusto", (object?)veiculo.CentroCusto ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@KmAtual", veiculo.KmAtual);
                cmd.Parameters.AddWithValue("@HorimetroAtual", veiculo.HorimetroAtual);
                cmd.Parameters.AddWithValue("@UltimaRevisaoKm", veiculo.UltimaRevisaoKm);
                cmd.Parameters.AddWithValue("@IntervaloRevisaoKm", veiculo.IntervaloRevisaoKm);
                cmd.Parameters.AddWithValue("@Ativo", veiculo.Ativo ? 1 : 0);

                cmd.ExecuteNonQuery();
            });
        }

        public async Task AtualizarTelemetriaKmAsync(Guid veiculoFrotaId, int kmAtual, int? horimetroAtual = null)
        {
            await Task.Run(() =>
            {
                using var conn = ObterConexaoAberta();
                using var cmd = conn.CreateCommand();
                if (horimetroAtual.HasValue)
                {
                    cmd.CommandText = "UPDATE VeiculosFrotas SET KmAtual = @Km, HorimetroAtual = @Horimetro WHERE Id = @Id;";
                    cmd.Parameters.AddWithValue("@Horimetro", horimetroAtual.Value);
                }
                else
                {
                    cmd.CommandText = "UPDATE VeiculosFrotas SET KmAtual = @Km WHERE Id = @Id;";
                }
                cmd.Parameters.AddWithValue("@Km", kmAtual);
                cmd.Parameters.AddWithValue("@Id", veiculoFrotaId.ToString());
                cmd.ExecuteNonQuery();
            });
        }

        public async Task<List<VeiculoFrota>> ListarVeiculosPorContratoAsync(Guid contratoFrotaId)
        {
            return await Task.Run(() =>
            {
                var lista = new List<VeiculoFrota>();
                using var conn = ObterConexaoAberta();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM VeiculosFrotas WHERE ContratoFrotaId = @ContratoId ORDER BY PrefixoFrota ASC;";
                cmd.Parameters.AddWithValue("@ContratoId", contratoFrotaId.ToString());

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    lista.Add(MapearVeiculoFrota(reader));
                }
                return lista;
            });
        }

        public async Task<VeiculoFrota?> ObterVeiculoFrotaPorPlacaAsync(string placa)
        {
            if (string.IsNullOrWhiteSpace(placa)) return null;

            return await Task.Run(() =>
            {
                using var conn = ObterConexaoAberta();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM VeiculosFrotas WHERE upper(Placa) = @Placa LIMIT 1;";
                cmd.Parameters.AddWithValue("@Placa", placa.Trim().ToUpperInvariant());

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return MapearVeiculoFrota(reader);
                }
                return null;
            });
        }

        public async Task<List<AlertaManutencaoFrota>> VerificarAlertasManutencaoPreventivaAsync()
        {
            return await Task.Run(() =>
            {
                var alertas = new List<AlertaManutencaoFrota>();
                using var conn = ObterConexaoAberta();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT vf.Id, vf.Placa, vf.PrefixoFrota, cf.ClienteNome, vf.KmAtual, 
                           vf.UltimaRevisaoKm, vf.IntervaloRevisaoKm
                    FROM VeiculosFrotas vf
                    INNER JOIN ContratosFrotas cf ON vf.ContratoFrotaId = cf.Id
                    WHERE vf.Ativo = 1 AND cf.Status = 1;";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var id = Guid.Parse(reader.GetString(0));
                    var placa = reader.GetString(1);
                    var prefixo = reader.GetString(2);
                    var clienteNome = reader.GetString(3);
                    var kmAtual = reader.GetInt32(4);
                    var ultimaRevisaoKm = reader.GetInt32(5);
                    var intervaloRevisao = reader.GetInt32(6);
                    var programado = ultimaRevisaoKm + intervaloRevisao;

                    CriticidadeRevisao criticidade;
                    if (kmAtual >= programado)
                    {
                        criticidade = CriticidadeRevisao.Vencida;
                    }
                    else if ((programado - kmAtual) <= 1000)
                    {
                        criticidade = CriticidadeRevisao.Proxima;
                    }
                    else
                    {
                        continue; // Manutenção em dia, não gera alerta ativo
                    }

                    alertas.Add(new AlertaManutencaoFrota
                    {
                        VeiculoFrotaId = id,
                        Placa = placa,
                        Prefixo = prefixo,
                        ClienteNome = clienteNome,
                        KmAtual = kmAtual,
                        KmProgramado = programado,
                        Criticidade = criticidade
                    });
                }

                return alertas;
            });
        }

        public async Task<FaturaFrota> FecharFaturaPeriodicaAsync(Guid contratoId, DateTime dataInicio, DateTime dataFim, string usuarioResponsavel = "Sistema")
        {
            var contrato = await ObterContratoPorIdAsync(contratoId)
                ?? throw new InvalidOperationException($"Contrato de frota não encontrado para o ID: {contratoId}");

            return await Task.Run(() =>
            {
                using var conn = ObterConexaoAberta();
                using var tx = conn.BeginTransaction();

                try
                {
                    // 1. Obter veículos associados ao contrato
                    var veiculos = new Dictionary<string, VeiculoFrota>(StringComparer.OrdinalIgnoreCase);
                    using (var cmdV = conn.CreateCommand())
                    {
                        cmdV.Transaction = tx;
                        cmdV.CommandText = "SELECT * FROM VeiculosFrotas WHERE ContratoFrotaId = @ContratoId;";
                        cmdV.Parameters.AddWithValue("@ContratoId", contratoId.ToString());
                        using var readerV = cmdV.ExecuteReader();
                        while (readerV.Read())
                        {
                            var v = MapearVeiculoFrota(readerV);
                            veiculos[v.Placa] = v;
                        }
                    }

                    // 2. Buscar Ordens de Serviço finalizadas ou faturáveis no período
                    var itensFatura = new List<FaturaFrotaItemOS>();
                    decimal totalBruto = 0;
                    decimal totalDescontos = 0;

                    using (var cmdOS = conn.CreateCommand())
                    {
                        cmdOS.Transaction = tx;
                        cmdOS.CommandText = @"
                            SELECT Id, Numero, PlacaSnapshot, DataAbertura, DataConclusao, ValorMaoObra, Desconto
                            FROM OrdensServico
                            WHERE (ClienteId = @ClienteIdStr OR ClienteId = @ClienteIdInt)
                              AND Status IN ('Concluida', 'Finalizada', 'Entregue', 'Fechada')
                              AND DataAbertura >= @DataInicio AND DataAbertura <= @DataFim;";

                        cmdOS.Parameters.AddWithValue("@ClienteIdStr", contrato.ClienteId.ToString());
                        cmdOS.Parameters.AddWithValue("@ClienteIdInt", contrato.ClienteId);
                        cmdOS.Parameters.AddWithValue("@DataInicio", dataInicio.ToString("yyyy-MM-dd 00:00:00"));
                        cmdOS.Parameters.AddWithValue("@DataFim", dataFim.ToString("yyyy-MM-dd 23:59:59"));

                        using var readerOS = cmdOS.ExecuteReader();
                        while (readerOS.Read())
                        {
                            var osIdStr = readerOS.GetString(0);
                            var numeroOS = readerOS.GetString(1);
                            var placa = readerOS.IsDBNull(2) ? "" : readerOS.GetString(2).Trim().ToUpperInvariant();
                            var dataStr = readerOS.IsDBNull(3) ? DateTime.Now.ToString("o") : readerOS.GetString(3);
                            DateTime.TryParse(dataStr, out var dataOS);

                            // Verificar se veículo pertence à frota
                            string prefixo = "";
                            if (veiculos.TryGetValue(placa, out var vf))
                            {
                                prefixo = vf.PrefixoFrota;
                            }

                            int osIdNumeric = 0;
                            int.TryParse(osIdStr, out osIdNumeric);

                            // Ler itens da OS para cálculo preciso de peças e mão de obra
                            decimal valorPecas = 0;
                            decimal valorServicos = 0;

                            using (var cmdItens = conn.CreateCommand())
                            {
                                cmdItens.Transaction = tx;
                                cmdItens.CommandText = "SELECT Tipo, Quantidade, ValorUnitario FROM OrdemServicoItens WHERE OrdemServicoId = @OsId;";
                                cmdItens.Parameters.AddWithValue("@OsId", osIdStr);
                                using var readerItens = cmdItens.ExecuteReader();
                                while (readerItens.Read())
                                {
                                    var tipo = readerItens.IsDBNull(0) ? "" : readerItens.GetString(0);
                                    var qtd = Convert.ToDecimal(readerItens.GetDouble(1));
                                    var unit = Convert.ToDecimal(readerItens.GetDouble(2));
                                    var totalItem = qtd * unit;

                                    if (tipo.Equals("Peca", StringComparison.OrdinalIgnoreCase) || tipo.Equals("Produto", StringComparison.OrdinalIgnoreCase))
                                    {
                                        valorPecas += totalItem;
                                    }
                                    else
                                    {
                                        valorServicos += totalItem;
                                    }
                                }
                            }

                            // Fallback caso OS não tenha itens na tabela separada
                            if (valorPecas == 0 && valorServicos == 0)
                            {
                                valorServicos = Convert.ToDecimal(readerOS.GetDouble(5)); // ValorMaoObra
                            }

                            var valorBrutoOS = valorPecas + valorServicos;
                            if (valorBrutoOS <= 0) continue;

                            // Aplicar descontos contratuais negociados
                            var descPecas = valorPecas * (contrato.DescontoPecasPercentual / 100m);
                            var descServ = valorServicos * (contrato.DescontoServicosPercentual / 100m);
                            var descontoTotalItem = descPecas + descServ;
                            var valorComDesconto = valorBrutoOS - descontoTotalItem;

                            totalBruto += valorBrutoOS;
                            totalDescontos += descontoTotalItem;

                            itensFatura.Add(new FaturaFrotaItemOS
                            {
                                Id = Guid.NewGuid(),
                                OrdemServicoId = osIdNumeric,
                                NumeroOS = numeroOS,
                                PlacaVeiculo = placa,
                                PrefixoVeiculo = prefixo,
                                DataOS = dataOS,
                                ValorTotalOriginal = valorBrutoOS,
                                ValorComDescontoContrato = valorComDesconto
                            });
                        }
                    }

                    // 3. Criar Fatura de Frota
                    var agora = DateTime.Now;
                    var numeroFatura = $"FAT-{agora:yyyyMM}-{new Random().Next(1000, 9999)}";
                    var fatura = new FaturaFrota
                    {
                        Id = Guid.NewGuid(),
                        ContratoFrotaId = contratoId,
                        ClienteId = contrato.ClienteId,
                        ClienteNome = contrato.ClienteNome,
                        NumeroFatura = numeroFatura,
                        PeriodoInicio = dataInicio,
                        PeriodoFim = dataFim,
                        DataEmissao = agora,
                        DataVencimento = agora.AddDays(contrato.DiasVencimentoBoleto > 0 ? contrato.DiasVencimentoBoleto : 15),
                        ValorBruto = totalBruto,
                        ValorDescontosContratuais = totalDescontos,
                        Status = StatusFaturaFrota.Aberta,
                        Observacoes = $"Fatura periódica consolidada por {usuarioResponsavel} em {agora:dd/MM/yyyy HH:mm}. Total de OSs agrupadas: {itensFatura.Count}.",
                        ItensOS = itensFatura
                    };

                    using (var cmdInsFat = conn.CreateCommand())
                    {
                        cmdInsFat.Transaction = tx;
                        cmdInsFat.CommandText = @"
                            INSERT INTO FaturasFrotas
                            (
                                Id, ContratoFrotaId, ClienteId, ClienteNome, NumeroFatura,
                                PeriodoInicio, PeriodoFim, DataEmissao, DataVencimento,
                                ValorBruto, ValorDescontosContratuais, Status, Observacoes
                            )
                            VALUES
                            (
                                @Id, @ContratoFrotaId, @ClienteId, @ClienteNome, @NumeroFatura,
                                @PeriodoInicio, @PeriodoFim, @DataEmissao, @DataVencimento,
                                @ValorBruto, @ValorDescontosContratuais, @Status, @Observacoes
                            );";

                        cmdInsFat.Parameters.AddWithValue("@Id", fatura.Id.ToString());
                        cmdInsFat.Parameters.AddWithValue("@ContratoFrotaId", fatura.ContratoFrotaId.ToString());
                        cmdInsFat.Parameters.AddWithValue("@ClienteId", fatura.ClienteId);
                        cmdInsFat.Parameters.AddWithValue("@ClienteNome", fatura.ClienteNome);
                        cmdInsFat.Parameters.AddWithValue("@NumeroFatura", fatura.NumeroFatura);
                        cmdInsFat.Parameters.AddWithValue("@PeriodoInicio", fatura.PeriodoInicio.ToString("o"));
                        cmdInsFat.Parameters.AddWithValue("@PeriodoFim", fatura.PeriodoFim.ToString("o"));
                        cmdInsFat.Parameters.AddWithValue("@DataEmissao", fatura.DataEmissao.ToString("o"));
                        cmdInsFat.Parameters.AddWithValue("@DataVencimento", fatura.DataVencimento.ToString("o"));
                        cmdInsFat.Parameters.AddWithValue("@ValorBruto", Convert.ToDouble(fatura.ValorBruto));
                        cmdInsFat.Parameters.AddWithValue("@ValorDescontosContratuais", Convert.ToDouble(fatura.ValorDescontosContratuais));
                        cmdInsFat.Parameters.AddWithValue("@Status", (int)fatura.Status);
                        cmdInsFat.Parameters.AddWithValue("@Observacoes", (object?)fatura.Observacoes ?? DBNull.Value);

                        cmdInsFat.ExecuteNonQuery();
                    }

                    // 4. Inserir itens da fatura
                    foreach (var item in itensFatura)
                    {
                        using var cmdItem = conn.CreateCommand();
                        cmdItem.Transaction = tx;
                        cmdItem.CommandText = @"
                            INSERT INTO FaturasFrotasItens
                            (
                                Id, FaturaFrotaId, OrdemServicoId, NumeroOS,
                                PlacaVeiculo, PrefixoVeiculo, DataOS, ValorTotalOriginal,
                                ValorComDescontoContrato
                            )
                            VALUES
                            (
                                @Id, @FaturaFrotaId, @OrdemServicoId, @NumeroOS,
                                @PlacaVeiculo, @PrefixoVeiculo, @DataOS, @ValorTotalOriginal,
                                @ValorComDescontoContrato
                            );";

                        cmdItem.Parameters.AddWithValue("@Id", item.Id.ToString());
                        cmdItem.Parameters.AddWithValue("@FaturaFrotaId", fatura.Id.ToString());
                        cmdItem.Parameters.AddWithValue("@OrdemServicoId", item.OrdemServicoId);
                        cmdItem.Parameters.AddWithValue("@NumeroOS", item.NumeroOS);
                        cmdItem.Parameters.AddWithValue("@PlacaVeiculo", item.PlacaVeiculo);
                        cmdItem.Parameters.AddWithValue("@PrefixoVeiculo", (object?)item.PrefixoVeiculo ?? DBNull.Value);
                        cmdItem.Parameters.AddWithValue("@DataOS", item.DataOS.ToString("o"));
                        cmdItem.Parameters.AddWithValue("@ValorTotalOriginal", Convert.ToDouble(item.ValorTotalOriginal));
                        cmdItem.Parameters.AddWithValue("@ValorComDescontoContrato", Convert.ToDouble(item.ValorComDescontoContrato));

                        cmdItem.ExecuteNonQuery();
                    }

                    tx.Commit();
                    _logger?.LogInfo($"[GestaoFrotas] Fatura periódica {fatura.NumeroFatura} gerada com sucesso. Valor Líquido: {fatura.ValorLiquido:C2}");
                    return fatura;
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    _logger?.LogError($"[GestaoFrotas] Falha ao fechar fatura periódica: {ex.Message}");
                    throw;
                }
            });
        }

        public async Task<List<FaturaFrota>> ListarFaturasPorContratoAsync(Guid contratoId)
        {
            return await Task.Run(() =>
            {
                var faturas = new List<FaturaFrota>();
                using var conn = ObterConexaoAberta();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT * FROM FaturasFrotas WHERE ContratoFrotaId = @ContratoId ORDER BY DataEmissao DESC;";
                cmd.Parameters.AddWithValue("@ContratoId", contratoId.ToString());

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    faturas.Add(MapearFatura(reader));
                }

                return faturas;
            });
        }

        public async Task<FaturaFrota?> ObterFaturaPorIdAsync(Guid faturaId)
        {
            return await Task.Run(() =>
            {
                using var conn = ObterConexaoAberta();
                FaturaFrota? fatura = null;

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM FaturasFrotas WHERE Id = @Id LIMIT 1;";
                    cmd.Parameters.AddWithValue("@Id", faturaId.ToString());

                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        fatura = MapearFatura(reader);
                    }
                }

                if (fatura != null)
                {
                    using var cmdItens = conn.CreateCommand();
                    cmdItens.CommandText = "SELECT * FROM FaturasFrotasItens WHERE FaturaFrotaId = @FaturaId ORDER BY DataOS ASC;";
                    cmdItens.Parameters.AddWithValue("@FaturaId", faturaId.ToString());

                    using var readerItens = cmdItens.ExecuteReader();
                    while (readerItens.Read())
                    {
                        fatura.ItensOS.Add(new FaturaFrotaItemOS
                        {
                            Id = Guid.Parse(readerItens.GetString(0)),
                            FaturaFrotaId = faturaId,
                            OrdemServicoId = readerItens.GetInt32(2),
                            NumeroOS = readerItens.GetString(3),
                            PlacaVeiculo = readerItens.GetString(4),
                            PrefixoVeiculo = readerItens.IsDBNull(5) ? "" : readerItens.GetString(5),
                            DataOS = DateTime.Parse(readerItens.GetString(6)),
                            ValorTotalOriginal = Convert.ToDecimal(readerItens.GetDouble(7)),
                            ValorComDescontoContrato = Convert.ToDecimal(readerItens.GetDouble(8))
                        });
                    }
                }

                return fatura;
            });
        }

        public async Task<bool> AtualizarStatusFaturaAsync(Guid faturaId, StatusFaturaFrota novoStatus)
        {
            return await Task.Run(() =>
            {
                using var conn = ObterConexaoAberta();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE FaturasFrotas SET Status = @Status WHERE Id = @Id;";
                cmd.Parameters.AddWithValue("@Status", (int)novoStatus);
                cmd.Parameters.AddWithValue("@Id", faturaId.ToString());

                return cmd.ExecuteNonQuery() > 0;
            });
        }

        public string GerarTokenAprovacaoRemota(string ordemServicoId, string placa, decimal valor, int validadeHoras = 48)
        {
            var expiraEmTicks = DateTime.UtcNow.AddHours(validadeHoras).Ticks;
            var payload = $"{ordemServicoId}|{placa.ToUpperInvariant()}|{valor.ToString("F2", CultureInfo.InvariantCulture)}|{expiraEmTicks}";

            using var hmac = new HMACSHA256(TokenSigningKey);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var assinatura = Convert.ToBase64String(hash);

            var dadosCompletos = $"{payload}|{assinatura}";
            var tokenBytes = Encoding.UTF8.GetBytes(dadosCompletos);
            return Convert.ToBase64String(tokenBytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        public (bool Valido, string OrdemServicoId, string Placa, decimal Valor) ValidarTokenAprovacaoRemota(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                    return (false, "", "", 0);

                var base64 = token.Replace('-', '+').Replace('_', '/');
                switch (base64.Length % 4)
                {
                    case 2: base64 += "=="; break;
                    case 3: base64 += "="; break;
                }

                var dadosCompletos = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                var partes = dadosCompletos.Split('|');
                if (partes.Length != 5)
                    return (false, "", "", 0);

                var osId = partes[0];
                var placa = partes[1];
                var valorStr = partes[2];
                var expiraTicksStr = partes[3];
                var assinatura = partes[4];

                var payload = $"{osId}|{placa}|{valorStr}|{expiraTicksStr}";

                using var hmac = new HMACSHA256(TokenSigningKey);
                var esperadoHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var esperadaAssinatura = Convert.ToBase64String(esperadoHash);

                if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(assinatura), Encoding.UTF8.GetBytes(esperadaAssinatura)))
                {
                    return (false, "", "", 0);
                }

                if (!long.TryParse(expiraTicksStr, out var expiraTicks) || DateTime.UtcNow.Ticks > expiraTicks)
                {
                    return (false, "", "", 0); // Token expirado
                }

                decimal.TryParse(valorStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var valor);
                return (true, osId, placa, valor);
            }
            catch
            {
                return (false, "", "", 0);
            }
        }

        private static ContratoFrota MapearContrato(DbDataReader reader)
        {
            return new ContratoFrota
            {
                Id = Guid.Parse(reader.GetString(0)),
                ClienteId = reader.GetInt32(1),
                ClienteNome = reader.GetString(2),
                NumeroContrato = reader.GetString(3),
                Descricao = reader.IsDBNull(4) ? "" : reader.GetString(4),
                DataInicio = DateTime.Parse(reader.GetString(5)),
                DataVencimento = DateTime.Parse(reader.GetString(6)),
                DescontoPecasPercentual = Convert.ToDecimal(reader.GetDouble(7)),
                DescontoServicosPercentual = Convert.ToDecimal(reader.GetDouble(8)),
                ValorHoraTecnicaNegociada = Convert.ToDecimal(reader.GetDouble(9)),
                DiaFechamentoFatura = reader.GetInt32(10),
                DiasVencimentoBoleto = reader.GetInt32(11),
                LimiteCreditoMensal = Convert.ToDecimal(reader.GetDouble(12)),
                ExigeAutorizacaoPrevia = reader.GetInt32(13) == 1,
                Status = (StatusContratoFrota)reader.GetInt32(14),
                Observacoes = reader.IsDBNull(15) ? "" : reader.GetString(15),
                DataCadastro = DateTime.Parse(reader.GetString(16))
            };
        }

        private static VeiculoFrota MapearVeiculoFrota(DbDataReader reader)
        {
            return new VeiculoFrota
            {
                Id = Guid.Parse(reader.GetString(0)),
                VeiculoId = reader.GetInt32(1),
                ContratoFrotaId = Guid.Parse(reader.GetString(2)),
                PrefixoFrota = reader.GetString(3),
                Placa = reader.GetString(4),
                MarcaModelo = reader.IsDBNull(5) ? "" : reader.GetString(5),
                MotoristaResponsavel = reader.IsDBNull(6) ? "" : reader.GetString(6),
                CentroCusto = reader.IsDBNull(7) ? "" : reader.GetString(7),
                KmAtual = reader.GetInt32(8),
                HorimetroAtual = reader.GetInt32(9),
                UltimaRevisaoKm = reader.GetInt32(10),
                IntervaloRevisaoKm = reader.GetInt32(11),
                Ativo = reader.GetInt32(12) == 1
            };
        }

        private static FaturaFrota MapearFatura(DbDataReader reader)
        {
            return new FaturaFrota
            {
                Id = Guid.Parse(reader.GetString(0)),
                ContratoFrotaId = Guid.Parse(reader.GetString(1)),
                ClienteId = reader.GetInt32(2),
                ClienteNome = reader.GetString(3),
                NumeroFatura = reader.GetString(4),
                PeriodoInicio = DateTime.Parse(reader.GetString(5)),
                PeriodoFim = DateTime.Parse(reader.GetString(6)),
                DataEmissao = DateTime.Parse(reader.GetString(7)),
                DataVencimento = DateTime.Parse(reader.GetString(8)),
                ValorBruto = Convert.ToDecimal(reader.GetDouble(9)),
                ValorDescontosContratuais = Convert.ToDecimal(reader.GetDouble(10)),
                Status = (StatusFaturaFrota)reader.GetInt32(11),
                LinhaDigitavelBoleto = reader.IsDBNull(12) ? null : reader.GetString(12),
                PixCopiaECola = reader.IsDBNull(13) ? null : reader.GetString(13),
                Observacoes = reader.IsDBNull(14) ? "" : reader.GetString(14)
            };
        }
    }
}
