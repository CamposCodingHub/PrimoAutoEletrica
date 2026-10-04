using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Models.Dvi;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services
{
    public sealed class DviInspectionService : IDviInspectionService
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggerService? _logger;

        public DviInspectionService(DatabaseService databaseService, LoggerService? logger = null)
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

        private static string ObterDiretorioFotosDvi()
        {
            var appData = string.IsNullOrWhiteSpace(App.RuntimeAppDataPath)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PrimoAutoEletrica")
                : App.RuntimeAppDataPath;

            var dir = Path.Combine(appData, "Media", "DVI");
            Directory.CreateDirectory(dir);
            return dir;
        }

        public async Task<InspecaoDvi> SalvarInspecaoAsync(InspecaoDvi inspecao)
        {
            if (inspecao == null) throw new ArgumentNullException(nameof(inspecao));
            if (string.IsNullOrWhiteSpace(inspecao.PlacaVeiculo))
                throw new ArgumentException("Placa do veículo é obrigatória para a inspeção.", nameof(inspecao));

            using var conn = ObterConexaoAberta();
            using var trans = conn.BeginTransaction();

            try
            {
                // Recalcula valor total estimado baseado nos itens
                decimal total = 0;
                if (inspecao.Itens != null)
                {
                    foreach (var item in inspecao.Itens)
                    {
                        if (item.ValorEstimadoReparo.HasValue)
                        {
                            total += item.ValorEstimadoReparo.Value;
                        }
                    }
                }
                inspecao.ValorTotalEstimado = total;

                if (inspecao.Id <= 0)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.Transaction = trans;
                    cmd.CommandText = @"
                        INSERT INTO InspecoesDvi (
                            OrdemServicoId, VeiculoId, ClienteId, PlacaVeiculo, ModeloVeiculo,
                            ClienteNome, ClienteTelefone, DataInspecao, ResponsavelTecnico,
                            StatusAprovacao, ObservacoesGerais, TokenAprovacaoRemota, DataAprovacao, ValorTotalEstimado
                        ) VALUES (
                            @OSId, @VeicId, @CliId, @Placa, @Modelo,
                            @CliNome, @CliTel, @Data, @Resp,
                            @Status, @Obs, @Token, @DataApr, @ValorTotal
                        );
                        SELECT last_insert_rowid();";

                    cmd.Parameters.AddWithValue("@OSId", (object?)inspecao.OrdemServicoId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@VeicId", (object?)inspecao.VeiculoId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CliId", (object?)inspecao.ClienteId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Placa", inspecao.PlacaVeiculo.ToUpperInvariant().Trim());
                    cmd.Parameters.AddWithValue("@Modelo", inspecao.ModeloVeiculo ?? string.Empty);
                    cmd.Parameters.AddWithValue("@CliNome", inspecao.ClienteNome ?? string.Empty);
                    cmd.Parameters.AddWithValue("@CliTel", (object?)inspecao.ClienteTelefone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Data", inspecao.DataInspecao.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                    cmd.Parameters.AddWithValue("@Resp", inspecao.ResponsavelTecnico ?? "Eletricista");
                    cmd.Parameters.AddWithValue("@Status", (int)inspecao.StatusAprovacao);
                    cmd.Parameters.AddWithValue("@Obs", (object?)inspecao.ObservacoesGerais ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Token", (object?)inspecao.TokenAprovacaoRemota ?? Guid.NewGuid().ToString("N"));
                    cmd.Parameters.AddWithValue("@DataApr", inspecao.DataAprovacao.HasValue ? inspecao.DataAprovacao.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ValorTotal", (double)inspecao.ValorTotalEstimado);

                    var result = await cmd.ExecuteScalarAsync();
                    inspecao.Id = Convert.ToInt32(result);
                }
                else
                {
                    using var cmd = conn.CreateCommand();
                    cmd.Transaction = trans;
                    cmd.CommandText = @"
                        UPDATE InspecoesDvi SET
                            OrdemServicoId = @OSId,
                            VeiculoId = @VeicId,
                            ClienteId = @CliId,
                            PlacaVeiculo = @Placa,
                            ModeloVeiculo = @Modelo,
                            ClienteNome = @CliNome,
                            ClienteTelefone = @CliTel,
                            ResponsavelTecnico = @Resp,
                            StatusAprovacao = @Status,
                            ObservacoesGerais = @Obs,
                            DataAprovacao = @DataApr,
                            ValorTotalEstimado = @ValorTotal
                        WHERE Id = @Id;";

                    cmd.Parameters.AddWithValue("@Id", inspecao.Id);
                    cmd.Parameters.AddWithValue("@OSId", (object?)inspecao.OrdemServicoId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@VeicId", (object?)inspecao.VeiculoId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CliId", (object?)inspecao.ClienteId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Placa", inspecao.PlacaVeiculo.ToUpperInvariant().Trim());
                    cmd.Parameters.AddWithValue("@Modelo", inspecao.ModeloVeiculo ?? string.Empty);
                    cmd.Parameters.AddWithValue("@CliNome", inspecao.ClienteNome ?? string.Empty);
                    cmd.Parameters.AddWithValue("@CliTel", (object?)inspecao.ClienteTelefone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Resp", inspecao.ResponsavelTecnico ?? "Eletricista");
                    cmd.Parameters.AddWithValue("@Status", (int)inspecao.StatusAprovacao);
                    cmd.Parameters.AddWithValue("@Obs", (object?)inspecao.ObservacoesGerais ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DataApr", inspecao.DataAprovacao.HasValue ? inspecao.DataAprovacao.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ValorTotal", (double)inspecao.ValorTotalEstimado);

                    await cmd.ExecuteNonQueryAsync();
                }

                // Salva ou atualiza itens
                if (inspecao.Itens != null && inspecao.Itens.Count > 0)
                {
                    foreach (var item in inspecao.Itens)
                    {
                        item.InspecaoDviId = inspecao.Id;
                        if (item.Id <= 0)
                        {
                            using var cmdItem = conn.CreateCommand();
                            cmdItem.Transaction = trans;
                            cmdItem.CommandText = @"
                                INSERT INTO InspecoesDviItens (
                                    InspecaoDviId, Categoria, NomeItem, Severidade,
                                    ObservacaoTecnica, ValorEstimadoReparo, AprovadoPeloCliente
                                ) VALUES (
                                    @InspId, @Cat, @Nome, @Sev, @Obs, @Val, @Apr
                                );
                                SELECT last_insert_rowid();";

                            cmdItem.Parameters.AddWithValue("@InspId", item.InspecaoDviId);
                            cmdItem.Parameters.AddWithValue("@Cat", item.Categoria ?? "Geral");
                            cmdItem.Parameters.AddWithValue("@Nome", item.NomeItem ?? string.Empty);
                            cmdItem.Parameters.AddWithValue("@Sev", (int)item.Severidade);
                            cmdItem.Parameters.AddWithValue("@Obs", (object?)item.ObservacaoTecnica ?? DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@Val", item.ValorEstimadoReparo.HasValue ? (double)item.ValorEstimadoReparo.Value : (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@Apr", item.AprovadoPeloCliente ? 1 : 0);

                            var itemRes = await cmdItem.ExecuteScalarAsync();
                            item.Id = Convert.ToInt32(itemRes);
                        }
                        else
                        {
                            using var cmdItem = conn.CreateCommand();
                            cmdItem.Transaction = trans;
                            cmdItem.CommandText = @"
                                UPDATE InspecoesDviItens SET
                                    Categoria = @Cat,
                                    NomeItem = @Nome,
                                    Severidade = @Sev,
                                    ObservacaoTecnica = @Obs,
                                    ValorEstimadoReparo = @Val,
                                    AprovadoPeloCliente = @Apr
                                WHERE Id = @Id;";

                            cmdItem.Parameters.AddWithValue("@Id", item.Id);
                            cmdItem.Parameters.AddWithValue("@Cat", item.Categoria ?? "Geral");
                            cmdItem.Parameters.AddWithValue("@Nome", item.NomeItem ?? string.Empty);
                            cmdItem.Parameters.AddWithValue("@Sev", (int)item.Severidade);
                            cmdItem.Parameters.AddWithValue("@Obs", (object?)item.ObservacaoTecnica ?? DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@Val", item.ValorEstimadoReparo.HasValue ? (double)item.ValorEstimadoReparo.Value : (object)DBNull.Value);
                            cmdItem.Parameters.AddWithValue("@Apr", item.AprovadoPeloCliente ? 1 : 0);

                            await cmdItem.ExecuteNonQueryAsync();
                        }
                    }
                }

                trans.Commit();
                _logger?.LogInfo($"Inspeção DVI #{inspecao.Id} para {inspecao.PlacaVeiculo} salva com sucesso.");
                return inspecao;
            }
            catch (Exception ex)
            {
                trans.Rollback();
                _logger?.LogError($"Erro ao salvar inspeção DVI #{inspecao.Id}: {ex.Message}");
                throw;
            }
        }

        public async Task<InspecaoDvi?> ObterPorIdAsync(int id)
        {
            using var conn = ObterConexaoAberta();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM InspecoesDvi WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            var inspecao = MapearInspecao(reader);
            reader.Close();

            await CarregarItensEFotosAsync(conn, inspecao);
            return inspecao;
        }

        public async Task<InspecaoDvi?> ObterPorOrdemServicoIdAsync(string ordemServicoId)
        {
            using var conn = ObterConexaoAberta();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM InspecoesDvi WHERE OrdemServicoId = @OSId ORDER BY Id DESC LIMIT 1;";
            cmd.Parameters.AddWithValue("@OSId", ordemServicoId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            var inspecao = MapearInspecao(reader);
            reader.Close();

            await CarregarItensEFotosAsync(conn, inspecao);
            return inspecao;
        }

        public async Task<List<InspecaoDvi>> ListarRecentesAsync(int limite = 50)
        {
            var lista = new List<InspecaoDvi>();
            using var conn = ObterConexaoAberta();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM InspecoesDvi ORDER BY Id DESC LIMIT @Limit;";
            cmd.Parameters.AddWithValue("@Limit", limite);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(MapearInspecao(reader));
            }
            reader.Close();

            foreach (var insp in lista)
            {
                await CarregarItensEFotosAsync(conn, insp);
            }

            return lista;
        }

        public async Task<List<InspecaoDvi>> ListarPorPlacaAsync(string placa)
        {
            var lista = new List<InspecaoDvi>();
            using var conn = ObterConexaoAberta();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM InspecoesDvi WHERE PlacaVeiculo = @Placa ORDER BY Id DESC;";
            cmd.Parameters.AddWithValue("@Placa", placa.Trim().ToUpperInvariant());

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lista.Add(MapearInspecao(reader));
            }
            reader.Close();

            foreach (var insp in lista)
            {
                await CarregarItensEFotosAsync(conn, insp);
            }

            return lista;
        }

        public async Task<InspecaoDviItem> SalvarItemAsync(InspecaoDviItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            using var conn = ObterConexaoAberta();
            if (item.Id <= 0)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO InspecoesDviItens (
                        InspecaoDviId, Categoria, NomeItem, Severidade,
                        ObservacaoTecnica, ValorEstimadoReparo, AprovadoPeloCliente
                    ) VALUES (
                        @InspId, @Cat, @Nome, @Sev, @Obs, @Val, @Apr
                    );
                    SELECT last_insert_rowid();";

                cmd.Parameters.AddWithValue("@InspId", item.InspecaoDviId);
                cmd.Parameters.AddWithValue("@Cat", item.Categoria ?? "Geral");
                cmd.Parameters.AddWithValue("@Nome", item.NomeItem ?? string.Empty);
                cmd.Parameters.AddWithValue("@Sev", (int)item.Severidade);
                cmd.Parameters.AddWithValue("@Obs", (object?)item.ObservacaoTecnica ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Val", item.ValorEstimadoReparo.HasValue ? (double)item.ValorEstimadoReparo.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Apr", item.AprovadoPeloCliente ? 1 : 0);

                var res = await cmd.ExecuteScalarAsync();
                item.Id = Convert.ToInt32(res);
            }
            else
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE InspecoesDviItens SET
                        Categoria = @Cat,
                        NomeItem = @Nome,
                        Severidade = @Sev,
                        ObservacaoTecnica = @Obs,
                        ValorEstimadoReparo = @Val,
                        AprovadoPeloCliente = @Apr
                    WHERE Id = @Id;";

                cmd.Parameters.AddWithValue("@Id", item.Id);
                cmd.Parameters.AddWithValue("@Cat", item.Categoria ?? "Geral");
                cmd.Parameters.AddWithValue("@Nome", item.NomeItem ?? string.Empty);
                cmd.Parameters.AddWithValue("@Sev", (int)item.Severidade);
                cmd.Parameters.AddWithValue("@Obs", (object?)item.ObservacaoTecnica ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Val", item.ValorEstimadoReparo.HasValue ? (double)item.ValorEstimadoReparo.Value : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Apr", item.AprovadoPeloCliente ? 1 : 0);

                await cmd.ExecuteNonQueryAsync();
            }

            return item;
        }

        public async Task<InspecaoDviFoto> SalvarFotoAsync(int itemId, byte[] imagemBytes, string nomeOriginal, string? descricao = null)
        {
            if (imagemBytes == null || imagemBytes.Length == 0)
                throw new ArgumentException("Dados da imagem inválidos.", nameof(imagemBytes));

            var dir = ObterDiretorioFotosDvi();
            var nomeArquivo = $"{Guid.NewGuid():N}.jpg";
            var caminhoCompleto = Path.Combine(dir, nomeArquivo);

            // Compressão e Redimensionamento com ImageSharp (Max 1280x960px, JPEG 80% qualidade)
            await Task.Run(() =>
            {
                using var image = Image.Load(imagemBytes);
                if (image.Width > 1280 || image.Height > 960)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(1280, 960)
                    }));
                }

                var encoder = new JpegEncoder
                {
                    Quality = 80
                };

                image.Save(caminhoCompleto, encoder);
            });

            var foto = new InspecaoDviFoto
            {
                InspecaoDviItemId = itemId,
                CaminhoArquivo = caminhoCompleto,
                Descricao = descricao ?? nomeOriginal,
                CriadoEm = DateTime.Now
            };

            using var conn = ObterConexaoAberta();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO InspecoesDviFotos (
                    InspecaoDviItemId, CaminhoArquivo, Descricao, AnotacoesJson, CriadoEm
                ) VALUES (
                    @ItemId, @Caminho, @Desc, @Anot, @Criado
                );
                SELECT last_insert_rowid();";

            cmd.Parameters.AddWithValue("@ItemId", foto.InspecaoDviItemId);
            cmd.Parameters.AddWithValue("@Caminho", foto.CaminhoArquivo);
            cmd.Parameters.AddWithValue("@Desc", (object?)foto.Descricao ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Anot", (object?)foto.AnotacoesJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Criado", foto.CriadoEm.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));

            var res = await cmd.ExecuteScalarAsync();
            foto.Id = Convert.ToInt32(res);

            _logger?.LogInfo($"Foto #{foto.Id} salva para o item DVI #{itemId}. Arquivo: {nomeArquivo}");
            return foto;
        }

        public async Task<bool> ExcluirFotoAsync(int fotoId)
        {
            using var conn = ObterConexaoAberta();
            string? caminhoArquivo = null;

            using (var cmdSelect = conn.CreateCommand())
            {
                cmdSelect.CommandText = "SELECT CaminhoArquivo FROM InspecoesDviFotos WHERE Id = @Id;";
                cmdSelect.Parameters.AddWithValue("@Id", fotoId);
                caminhoArquivo = (await cmdSelect.ExecuteScalarAsync())?.ToString();
            }

            using (var cmdDelete = conn.CreateCommand())
            {
                cmdDelete.CommandText = "DELETE FROM InspecoesDviFotos WHERE Id = @Id;";
                cmdDelete.Parameters.AddWithValue("@Id", fotoId);
                var rows = await cmdDelete.ExecuteNonQueryAsync();

                if (rows > 0 && !string.IsNullOrWhiteSpace(caminhoArquivo) && File.Exists(caminhoArquivo))
                {
                    try { File.Delete(caminhoArquivo); } catch { /* Ignore */ }
                }

                return rows > 0;
            }
        }

        public async Task<bool> ExcluirInspecaoAsync(int inspecaoId)
        {
            var inspecao = await ObterPorIdAsync(inspecaoId);
            if (inspecao == null) return false;

            // Remove arquivos físicos de fotos
            if (inspecao.Itens != null)
            {
                foreach (var item in inspecao.Itens)
                {
                    if (item.Fotos != null)
                    {
                        foreach (var foto in item.Fotos)
                        {
                            if (!string.IsNullOrWhiteSpace(foto.CaminhoArquivo) && File.Exists(foto.CaminhoArquivo))
                            {
                                try { File.Delete(foto.CaminhoArquivo); } catch { /* Ignore */ }
                            }
                        }
                    }
                }
            }

            using var conn = ObterConexaoAberta();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM InspecoesDvi WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", inspecaoId);
            var affected = await cmd.ExecuteNonQueryAsync();
            return affected > 0;
        }

        public List<InspecaoDviItem> GerarChecklistPadraoAutoEletrica(int inspecaoId)
        {
            return new List<InspecaoDviItem>
            {
                // Elétrica & Bateria
                new InspecaoDviItem { InspecaoDviId = inspecaoId, Categoria = "⚡ Elétrica & Bateria", NomeItem = "Bateria: Tensão em Repouso & Nível de Carga", Severidade = DviStatusSeveridade.Ok },
                new InspecaoDviItem { InspecaoDviId = inspecaoId, Categoria = "⚡ Elétrica & Bateria", NomeItem = "Bateria: Queda de Tensão na Partida (CCA)", Severidade = DviStatusSeveridade.Ok },
                new InspecaoDviItem { InspecaoDviId = inspecaoId, Categoria = "⚡ Elétrica & Bateria", NomeItem = "Alternador: Tensão de Carga sob Carga Máxima (13.8V-14.4V)", Severidade = DviStatusSeveridade.Ok },
                new InspecaoDviItem { InspecaoDviId = inspecaoId, Categoria = "⚡ Elétrica & Bateria", NomeItem = "Aterramento Principal: Queda de Tensão Bloco/Chassi (< 0.2V)", Severidade = DviStatusSeveridade.Ok },

                // Iluminação & Sinalização
                new InspecaoDviItem { InspecaoDviId = inspecaoId, Categoria = "💡 Iluminação & Sinalização", NomeItem = "Faróis Dianteiros: Foco Alto / Baixo e Regulagem", Severidade = DviStatusSeveridade.Ok },
                new InspecaoDviItem { InspecaoDviId = inspecaoId, Categoria = "💡 Iluminação & Sinalização", NomeItem = "Lanternas Traseiras, Luz de Freio e Brake-Light", Severidade = DviStatusSeveridade.Ok },
                new InspecaoDviItem { InspecaoDviId = inspecaoId, Categoria = "💡 Iluminação & Sinalização", NomeItem = "Setas de Direção e Pisca-Alerta (Diant/Tras/Laterais)", Severidade = DviStatusSeveridade.Ok },
                new InspecaoDviItem { InspecaoDviId = inspecaoId, Categoria = "💡 Iluminação & Sinalização", NomeItem = "Luz de Ré e Iluminação da Placa Traseira", Severidade = DviStatusSeveridade.Ok },

                // Chicotes, Relés & Fusíveis
                new InspecaoDviItem { InspecaoDviId = inspecaoId, Categoria = "⚙️ Chicotes & Fusíveis", NomeItem = "Central Elétrica do Vão do Motor (Fusíveis e Relés)", Severidade = DviStatusSeveridade.Ok },
                new InspecaoDviItem { InspecaoDviId = inspecaoId, Categoria = "⚙️ Chicotes & Fusíveis", NomeItem = "Chicote do Motor de Partida e Alternador (Isolação/Fixação)", Severidade = DviStatusSeveridade.Ok },
                new InspecaoDviItem { InspecaoDviId = inspecaoId, Categoria = "⚙️ Chicotes & Fusíveis", NomeItem = "Caixa de Fusíveis Interna do Painel / Habitáculo", Severidade = DviStatusSeveridade.Ok },

                // Injeção, Ignição & Arrefecimento
                new InspecaoDviItem { InspecaoDviId = inspecaoId, Categoria = "🔌 Injeção & Arrefecimento", NomeItem = "Eletroventilador: Acionamento da 1ª e 2ª Velocidade", Severidade = DviStatusSeveridade.Ok },
                new InspecaoDviItem { InspecaoDviId = inspecaoId, Categoria = "🔌 Injeção & Arrefecimento", NomeItem = "Cabos de Ignição, Bobinas e Conectores dos Bicos", Severidade = DviStatusSeveridade.Ok },
                new InspecaoDviItem { InspecaoDviId = inspecaoId, Categoria = "🔌 Injeção & Arrefecimento", NomeItem = "Sensor de Temperatura da Água e Chicote do Termostato", Severidade = DviStatusSeveridade.Ok },

                // Carroceria & Avarias de Entrada
                new InspecaoDviItem { InspecaoDviId = inspecaoId, Categoria = "🚗 Carroceria & Entrada", NomeItem = "Pára-choque Dianteiro e Grades (Riscos / Avarias)", Severidade = DviStatusSeveridade.Ok },
                new InspecaoDviItem { InspecaoDviId = inspecaoId, Categoria = "🚗 Carroceria & Entrada", NomeItem = "Laterais Esquerda / Direita e Retrovisores", Severidade = DviStatusSeveridade.Ok },
                new InspecaoDviItem { InspecaoDviId = inspecaoId, Categoria = "🚗 Carroceria & Entrada", NomeItem = "Pára-choque Traseiro e Tampa do Porta-Malas", Severidade = DviStatusSeveridade.Ok },
                new InspecaoDviItem { InspecaoDviId = inspecaoId, Categoria = "🚗 Carroceria & Entrada", NomeItem = "Travas Elétricas, Vidros e Alarme Automotivo", Severidade = DviStatusSeveridade.Ok }
            };
        }

        public string MontarResumoTextoLaudo(InspecaoDvi inspecao)
        {
            if (inspecao == null) return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("⚡ *LAUDO DE INSPEÇÃO DIGITAL VEICULAR (DVI 2.0)* ⚡");
            sb.AppendLine("*PRIMO AUTO ELÉTRICA & DIAGNÓSTICO PROFISSIONAL*");
            sb.AppendLine("────────────────────────────────");
            sb.AppendLine($"🚗 *Veículo:* {inspecao.ModeloVeiculo} | *Placa:* {inspecao.PlacaVeiculo}");
            if (!string.IsNullOrWhiteSpace(inspecao.ClienteNome))
                sb.AppendLine($"👤 *Cliente:* {inspecao.ClienteNome}");
            sb.AppendLine($"👨‍🔧 *Técnico:* {inspecao.ResponsavelTecnico}");
            sb.AppendLine($"📅 *Data:* {inspecao.DataInspecao:dd/MM/yyyy HH:mm}");
            if (!string.IsNullOrWhiteSpace(inspecao.OrdemServicoId))
                sb.AppendLine($"📋 *Ordem de Serviço Vinculada:* #{inspecao.OrdemServicoId}");
            sb.AppendLine("────────────────────────────────");

            var criticos = inspecao.Itens?.FindAll(i => i.Severidade == DviStatusSeveridade.Critico) ?? new();
            var atencao = inspecao.Itens?.FindAll(i => i.Severidade == DviStatusSeveridade.Atencao) ?? new();
            var ok = inspecao.Itens?.FindAll(i => i.Severidade == DviStatusSeveridade.Ok) ?? new();

            if (criticos.Count > 0)
            {
                sb.AppendLine($"🔴 *ITENS CRÍTICOS / URGENTES ({criticos.Count}):*");
                foreach (var item in criticos)
                {
                    var val = item.ValorEstimadoReparo.HasValue ? $" - R$ {item.ValorEstimadoReparo.Value:N2}" : string.Empty;
                    var obs = !string.IsNullOrWhiteSpace(item.ObservacaoTecnica) ? $"\n   ↳ _{item.ObservacaoTecnica}_" : string.Empty;
                    sb.AppendLine($"• {item.NomeItem}{val}{obs}");
                }
                sb.AppendLine();
            }

            if (atencao.Count > 0)
            {
                sb.AppendLine($"🟡 *ITENS PREVENTIVOS / ATENÇÃO ({atencao.Count}):*");
                foreach (var item in atencao)
                {
                    var val = item.ValorEstimadoReparo.HasValue ? $" - R$ {item.ValorEstimadoReparo.Value:N2}" : string.Empty;
                    var obs = !string.IsNullOrWhiteSpace(item.ObservacaoTecnica) ? $"\n   ↳ _{item.ObservacaoTecnica}_" : string.Empty;
                    sb.AppendLine($"• {item.NomeItem}{val}{obs}");
                }
                sb.AppendLine();
            }

            sb.AppendLine($"🟢 *ITENS CONFORMES / APROVADOS:* {ok.Count} itens inspecionados sem avarias.");

            if (inspecao.ValorTotalEstimado > 0)
            {
                sb.AppendLine("────────────────────────────────");
                sb.AppendLine($"💰 *Investimento Total Sugerido:* R$ {inspecao.ValorTotalEstimado:N2}");
            }

            if (!string.IsNullOrWhiteSpace(inspecao.ObservacoesGerais))
            {
                sb.AppendLine($"\n📝 *Observações Técnicas:* {inspecao.ObservacoesGerais}");
            }

            sb.AppendLine("\n💬 *Para autorizar os serviços ou tirar dúvidas com nossa equipe, responda a esta mensagem!*");

            return sb.ToString().TrimEnd();
        }

        public string GerarLinkWhatsApp(InspecaoDvi inspecao)
        {
            if (inspecao == null) return string.Empty;

            var rawTel = inspecao.ClienteTelefone ?? string.Empty;
            var telDigits = new StringBuilder();
            foreach (var ch in rawTel)
            {
                if (char.IsDigit(ch)) telDigits.Append(ch);
            }

            var tel = telDigits.ToString();
            if (tel.Length == 10 || tel.Length == 11)
            {
                tel = "55" + tel;
            }

            var texto = MontarResumoTextoLaudo(inspecao);
            return $"https://wa.me/{tel}?text={Uri.EscapeDataString(texto)}";
        }

        public async Task<int> ConverterItensParaOrdemServicoAsync(int inspecaoId, string ordemServicoId)
        {
            var inspecao = await ObterPorIdAsync(inspecaoId);
            if (inspecao == null) return 0;

            var itensParaInserir = inspecao.Itens?.FindAll(i => i.Severidade != DviStatusSeveridade.Ok) ?? new();
            if (itensParaInserir.Count == 0) return 0;

            using var conn = ObterConexaoAberta();

            // Validação de Integridade: Não permitir inserção em OS com status encerrado
            using (var cmdCheckStatus = conn.CreateCommand())
            {
                cmdCheckStatus.CommandText = "SELECT Status FROM OrdensServico WHERE Id = @Id LIMIT 1;";
                cmdCheckStatus.Parameters.AddWithValue("@Id", ordemServicoId);
                var statusObj = await cmdCheckStatus.ExecuteScalarAsync();
                if (statusObj != null)
                {
                    var status = statusObj.ToString()?.Trim() ?? string.Empty;
                    if (string.Equals(status, "Entregue", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(status, "Cancelada", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(status, "Faturada", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Não é permitido adicionar novos itens à Ordem de Serviço #{ordemServicoId} com status encerrado ('{status}').");
                    }
                }
            }

            using var trans = conn.BeginTransaction();
            int inseridos = 0;

            try
            {
                foreach (var item in itensParaInserir)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.Transaction = trans;
                    cmd.CommandText = @"
                        INSERT INTO OrdemServicoItens (
                            Id, OrdemServicoId, Tipo, Descricao, Quantidade, ValorUnitario, CustoUnitario, EstoqueMovimentado
                        ) VALUES (
                            @Id, @OSId, @Tipo, @Desc, @Qtd, @Preco, 0, 0
                        );";

                    cmd.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                    cmd.Parameters.AddWithValue("@OSId", ordemServicoId);
                    cmd.Parameters.AddWithValue("@Tipo", "Servico");
                    cmd.Parameters.AddWithValue("@Desc", $"[DVI] {item.NomeItem}" + (!string.IsNullOrWhiteSpace(item.ObservacaoTecnica) ? $" ({item.ObservacaoTecnica})" : ""));
                    cmd.Parameters.AddWithValue("@Qtd", 1.0);
                    var preco = item.ValorEstimadoReparo ?? 0;
                    cmd.Parameters.AddWithValue("@Preco", (double)preco);

                    await cmd.ExecuteNonQueryAsync();
                    inseridos++;
                }

                // Atualiza o vínculo da inspeção com a OS se ainda não estiver vinculada
                if (inspecao.OrdemServicoId != ordemServicoId)
                {
                    using var cmdVinc = conn.CreateCommand();
                    cmdVinc.Transaction = trans;
                    cmdVinc.CommandText = "UPDATE InspecoesDvi SET OrdemServicoId = @OSId WHERE Id = @Id;";
                    cmdVinc.Parameters.AddWithValue("@OSId", ordemServicoId);
                    cmdVinc.Parameters.AddWithValue("@Id", inspecaoId);
                    await cmdVinc.ExecuteNonQueryAsync();
                }

                trans.Commit();
                _logger?.LogInfo($"{inseridos} itens da inspeção DVI #{inspecaoId} inseridos com sucesso na OS #{ordemServicoId}.");
                return inseridos;
            }
            catch (Exception ex)
            {
                trans.Rollback();
                _logger?.LogError($"Erro ao converter itens da inspeção DVI #{inspecaoId} para OS #{ordemServicoId}: {ex.Message}");
                throw;
            }
        }

        private static InspecaoDvi MapearInspecao(DbDataReader reader)
        {
            return new InspecaoDvi
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                OrdemServicoId = reader.IsDBNull(reader.GetOrdinal("OrdemServicoId")) ? null : reader.GetString(reader.GetOrdinal("OrdemServicoId")),
                VeiculoId = reader.IsDBNull(reader.GetOrdinal("VeiculoId")) ? null : reader.GetInt32(reader.GetOrdinal("VeiculoId")),
                ClienteId = reader.IsDBNull(reader.GetOrdinal("ClienteId")) ? null : reader.GetInt32(reader.GetOrdinal("ClienteId")),
                PlacaVeiculo = reader.GetString(reader.GetOrdinal("PlacaVeiculo")),
                ModeloVeiculo = reader.IsDBNull(reader.GetOrdinal("ModeloVeiculo")) ? string.Empty : reader.GetString(reader.GetOrdinal("ModeloVeiculo")),
                ClienteNome = reader.IsDBNull(reader.GetOrdinal("ClienteNome")) ? string.Empty : reader.GetString(reader.GetOrdinal("ClienteNome")),
                ClienteTelefone = reader.IsDBNull(reader.GetOrdinal("ClienteTelefone")) ? string.Empty : reader.GetString(reader.GetOrdinal("ClienteTelefone")),
                DataInspecao = DateTime.TryParse(reader.GetString(reader.GetOrdinal("DataInspecao")), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : DateTime.Now,
                ResponsavelTecnico = reader.IsDBNull(reader.GetOrdinal("ResponsavelTecnico")) ? "Eletricista" : reader.GetString(reader.GetOrdinal("ResponsavelTecnico")),
                StatusAprovacao = (DviStatusAprovacao)reader.GetInt32(reader.GetOrdinal("StatusAprovacao")),
                ObservacoesGerais = reader.IsDBNull(reader.GetOrdinal("ObservacoesGerais")) ? null : reader.GetString(reader.GetOrdinal("ObservacoesGerais")),
                TokenAprovacaoRemota = reader.IsDBNull(reader.GetOrdinal("TokenAprovacaoRemota")) ? null : reader.GetString(reader.GetOrdinal("TokenAprovacaoRemota")),
                DataAprovacao = reader.IsDBNull(reader.GetOrdinal("DataAprovacao")) ? null : (DateTime.TryParse(reader.GetString(reader.GetOrdinal("DataAprovacao")), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dta) ? dta : null),
                ValorTotalEstimado = (decimal)reader.GetDouble(reader.GetOrdinal("ValorTotalEstimado"))
            };
        }

        private static async Task CarregarItensEFotosAsync(DbConnection conn, InspecaoDvi inspecao)
        {
            var itens = new List<InspecaoDviItem>();
            using (var cmdItens = conn.CreateCommand())
            {
                cmdItens.CommandText = "SELECT * FROM InspecoesDviItens WHERE InspecaoDviId = @InspId ORDER BY Id ASC;";
                cmdItens.Parameters.AddWithValue("@InspId", inspecao.Id);

                using var readerItens = await cmdItens.ExecuteReaderAsync();
                while (await readerItens.ReadAsync())
                {
                    itens.Add(new InspecaoDviItem
                    {
                        Id = readerItens.GetInt32(readerItens.GetOrdinal("Id")),
                        InspecaoDviId = readerItens.GetInt32(readerItens.GetOrdinal("InspecaoDviId")),
                        Categoria = readerItens.GetString(readerItens.GetOrdinal("Categoria")),
                        NomeItem = readerItens.GetString(readerItens.GetOrdinal("NomeItem")),
                        Severidade = (DviStatusSeveridade)readerItens.GetInt32(readerItens.GetOrdinal("Severidade")),
                        ObservacaoTecnica = readerItens.IsDBNull(readerItens.GetOrdinal("ObservacaoTecnica")) ? null : readerItens.GetString(readerItens.GetOrdinal("ObservacaoTecnica")),
                        ValorEstimadoReparo = readerItens.IsDBNull(readerItens.GetOrdinal("ValorEstimadoReparo")) ? null : (decimal)readerItens.GetDouble(readerItens.GetOrdinal("ValorEstimadoReparo")),
                        AprovadoPeloCliente = readerItens.GetInt32(readerItens.GetOrdinal("AprovadoPeloCliente")) == 1
                    });
                }
            }

            foreach (var item in itens)
            {
                using var cmdFotos = conn.CreateCommand();
                cmdFotos.CommandText = "SELECT * FROM InspecoesDviFotos WHERE InspecaoDviItemId = @ItemId ORDER BY Id ASC;";
                cmdFotos.Parameters.AddWithValue("@ItemId", item.Id);

                using var readerFotos = await cmdFotos.ExecuteReaderAsync();
                while (await readerFotos.ReadAsync())
                {
                    item.Fotos.Add(new InspecaoDviFoto
                    {
                        Id = readerFotos.GetInt32(readerFotos.GetOrdinal("Id")),
                        InspecaoDviItemId = readerFotos.GetInt32(readerFotos.GetOrdinal("InspecaoDviItemId")),
                        CaminhoArquivo = readerFotos.GetString(readerFotos.GetOrdinal("CaminhoArquivo")),
                        Descricao = readerFotos.IsDBNull(readerFotos.GetOrdinal("Descricao")) ? null : readerFotos.GetString(readerFotos.GetOrdinal("Descricao")),
                        AnotacoesJson = readerFotos.IsDBNull(readerFotos.GetOrdinal("AnotacoesJson")) ? null : readerFotos.GetString(readerFotos.GetOrdinal("AnotacoesJson")),
                        CriadoEm = DateTime.TryParse(readerFotos.GetString(readerFotos.GetOrdinal("CriadoEm")), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : DateTime.Now
                    });
                }
            }

            inspecao.Itens = itens;
        }
    }
}
