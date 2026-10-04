using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Models.AI;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services.DatabaseProviders;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services.AI
{
    public class WorkOrderAiBridgeService : IWorkOrderAiBridgeService
    {
        private readonly DatabaseService _databaseService;
        private readonly IProdutoRepository? _produtoRepository;
        private readonly IOrdemServicoRepository? _ordemServicoRepository;
        private readonly IGestaoComprasService? _comprasService;
        private readonly LoggerService? _logger;

        public WorkOrderAiBridgeService(
            DatabaseService databaseService,
            IProdutoRepository? produtoRepository = null,
            IOrdemServicoRepository? ordemServicoRepository = null,
            IGestaoComprasService? comprasService = null,
            LoggerService? logger = null)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _produtoRepository = produtoRepository;
            _ordemServicoRepository = ordemServicoRepository;
            _comprasService = comprasService;
            _logger = logger;
        }

        private DbConnection ObterConexaoAberta()
        {
            var conn = _databaseService.GetConnection();
            if (conn.State != System.Data.ConnectionState.Open)
            {
                conn.Open();
            }
            return conn;
        }

        public async Task<List<AIPartOrServiceProposal>> AnalisarTextoEProporItensAsync(string textoDiagnostico, string? modeloVeiculo = null)
        {
            if (string.IsNullOrWhiteSpace(textoDiagnostico))
                return new List<AIPartOrServiceProposal>();

            var propostas = new List<AIPartOrServiceProposal>();
            var lower = textoDiagnostico.ToLowerInvariant();

            // Regras especializadas de auto elétrica para identificação de peças e mão de obra
            var regras = new List<(string RegexPattern, string NomePeca, string Codigo, decimal PrecoPadrao, string NomeMaoObra, decimal PrecoMaoObra, int Minutos)>
            {
                ("bobina|misfire|p0300|p0301|p0302|p0303|p0304|centelha|dis",
                 "Bobina de Ignição Eletrônica", "BI-0402", 285.00m,
                 "Mão de Obra: Teste com Osciloscópio & Substituição de Bobina", 120.00m, 45),

                ("vela|velas|eletrodo|gap|carboniz",
                 "Jogo de Velas de Ignição Green Plug", "BKR6E", 98.00m,
                 "Mão de Obra: Calibração de Folga e Troca de Velas", 70.00m, 30),

                ("cabo de vela|cabos de igni",
                 "Jogo de Cabos de Ignição Resistivos", "ST-V25", 135.00m,
                 "Mão de Obra: Troca e Isolação de Cabos de Vela", 50.00m, 20),

                ("bateria|cca|tensão em repouso|descarreg|11\\.9v|12\\.0v",
                 "Bateria Automotiva 60Ah Selada Livre de Manutenção", "BAT-60AH", 440.00m,
                 "Mão de Obra: Instalação, Teste de Condutância e Queda de Carga", 60.00m, 30),

                ("alternador|regulador|diodo|placa de diodo|12\\.2v|13\\.2v|p0562",
                 "Regulador de Voltagem do Alternador", "RT-14V", 185.00m,
                 "Mão de Obra: Remoção do Alternador e Teste em Bancada", 160.00m, 90),

                ("rele|relé|rele auxiliar|dni|40a",
                 "Relé Auxiliar Universal 40A 4 Pinos Linha 30/87", "DNI-0101", 28.00m,
                 "Mão de Obra: Teste de Queda de Tensão no Soquete e Substituição", 40.00m, 20),

                ("ventoinha|eletroventilador|arrefecimento|fervendo|termostato",
                 "Sensor de Temperatura da Água (ECT)", "MTE-4050", 68.00m,
                 "Mão de Obra: Troca de Sensor e Sangria do Sistema de Arrefecimento", 90.00m, 40),

                ("arranque|motor de partida|tectic|tec-tec|solenoide|comutador",
                 "Automático de Partida (Solenoide)", "ZM-401", 175.00m,
                 "Mão de Obra: Revisão do Motor de Partida e Troca de Escovas", 180.00m, 90),

                ("sensor de rotacao|sensor ckp|roda fonica|flange|p0335",
                 "Sensor de Rotação e Posição da Árvore de Manivelas (CKP)", "CKP-902", 145.00m,
                 "Mão de Obra: Instalação de Sensor e Conferência de Sinal", 110.00m, 45),

                ("fusivel|fusível|curto|queimando direto",
                 "Kit de Fusíveis Lâmina Variados (10A a 30A)", "FUS-KIT", 18.00m,
                 "Mão de Obra: Rastreamento de Curto-Circuito com Lâmpada de Carga", 150.00m, 60),

                ("farol|lampada|h7|h4|meia luz|soquete",
                 "Lâmpada Halógena H7 12V 55W Standard", "H7-55W", 32.00m,
                 "Mão de Obra: Substituição de Lâmpada e Alinhamento de Foco", 35.00m, 15),

                ("bomba de combustivel|pressao da bomba|manometro|combustivel",
                 "Refil da Bomba de Combustível 3.0 Bar Flex", "MAM-210", 195.00m,
                 "Mão de Obra: Troca de Refil e Teste de Vazão/Pressão no Tanque", 130.00m, 60)
            };

            var pecasAdicionadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var r in regras)
            {
                if (Regex.IsMatch(lower, r.RegexPattern, RegexOptions.IgnoreCase))
                {
                    if (pecasAdicionadas.Add(r.NomePeca))
                    {
                        // 1. Proposta da Peça
                        var propPeca = new AIPartOrServiceProposal
                        {
                            Tipo = "Peca",
                            Descricao = r.NomePeca,
                            CodigoFabricante = r.Codigo,
                            Quantidade = 1,
                            PrecoSugerido = r.PrecoPadrao,
                            Selecionado = true
                        };

                        // Tenta cruzar com o estoque da oficina
                        await CruzarComEstoqueAsync(propPeca);
                        propostas.Add(propPeca);

                        // 2. Proposta do Serviço correspondente
                        var propServico = new AIPartOrServiceProposal
                        {
                            Tipo = "Servico",
                            Descricao = r.NomeMaoObra,
                            Quantidade = 1,
                            PrecoSugerido = r.PrecoMaoObra,
                            TempoEstimadoMinutos = r.Minutos,
                            Selecionado = true
                        };
                        propostas.Add(propServico);
                    }
                }
            }

            return propostas;
        }

        private async Task CruzarComEstoqueAsync(AIPartOrServiceProposal prop)
        {
            try
            {
                // Busca no repositório de produtos se disponível
                if (_produtoRepository != null)
                {
                    var todos = _produtoRepository.ObterTodos();
                    var encontrado = todos.FirstOrDefault(p =>
                        (!string.IsNullOrWhiteSpace(p.Codigo) && p.Codigo.Equals(prop.CodigoFabricante, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(p.Nome) && p.Nome.Contains(prop.Descricao, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(prop.Descricao) && prop.Descricao.Contains(p.Nome, StringComparison.OrdinalIgnoreCase)));

                    if (encontrado != null)
                    {
                        prop.ProdutoId = encontrado.Id;
                        prop.Descricao = encontrado.Nome;
                        prop.CodigoFabricante = encontrado.Codigo;
                        prop.PrecoSugerido = encontrado.PrecoVenda > 0 ? encontrado.PrecoVenda : prop.PrecoSugerido;
                        prop.SaldoEstoque = encontrado.QuantidadeEstoque;
                        return;
                    }
                }

                // Fallback: Busca direta via SQLite
                using var conn = ObterConexaoAberta();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT Id, Nome, Codigo, PrecoVenda, QuantidadeEstoque
                    FROM Produtos
                    WHERE (Codigo = @Cod OR Nome LIKE @Nome) AND Ativo = 1
                    LIMIT 1;";
                cmd.Parameters.AddWithValue("@Cod", prop.CodigoFabricante ?? string.Empty);
                cmd.Parameters.AddWithValue("@Nome", $"%{prop.Descricao}%");

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var idStr = reader.GetString(0);
                    if (Guid.TryParse(idStr, out var pId)) prop.ProdutoId = pId;
                    prop.Descricao = reader.GetString(1);
                    prop.CodigoFabricante = reader.IsDBNull(2) ? prop.CodigoFabricante : reader.GetString(2);
                    var preco = reader.GetDecimal(3);
                    if (preco > 0) prop.PrecoSugerido = preco;
                    prop.SaldoEstoque = reader.GetDecimal(4);
                }
                else
                {
                    // Item não cadastrado no estoque local da oficina
                    prop.SaldoEstoque = 0;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Erro ao cruzar peça com estoque: {ex.Message}");
                prop.SaldoEstoque = 0;
            }
        }

        public async Task<List<OrdemServico>> ListarOrdensServicoAbertasAsync(string? filtro = null)
        {
            var ordens = new List<OrdemServico>();

            using var conn = ObterConexaoAberta();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    Id, Numero, ClienteId, VeiculoId, TecnicoId,
                    ClienteNomeSnapshot, TelefoneClienteSnapshot, VeiculoDescricaoSnapshot, PlacaSnapshot,
                    Status, Prioridade, Origem, ProblemaRelatado, DataAbertura, DataPrevisao,
                    ValorMaoObra, Desconto
                FROM OrdensServico
                WHERE Status NOT IN ('Entregue', 'Cancelada', 'Faturada')
                  AND Ativo = 1
                ORDER BY DataAbertura DESC
                LIMIT 50;";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var os = new OrdemServico
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    Numero = reader.GetString(1),
                    ClienteId = Guid.Parse(reader.GetString(2)),
                    VeiculoId = reader.IsDBNull(3) ? null : Guid.Parse(reader.GetString(3)),
                    TecnicoId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    ClienteNomeSnapshot = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    TelefoneClienteSnapshot = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    VeiculoDescricaoSnapshot = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    PlacaSnapshot = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                    Status = reader.GetString(9),
                    Prioridade = reader.GetString(10),
                    Origem = reader.IsDBNull(11) ? "Balcao" : reader.GetString(11),
                    ProblemaRelatado = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                    DataAbertura = DateTime.TryParse(reader.GetString(13), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : DateTime.Now
                };
                ordens.Add(os);
            }

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                var f = filtro.Trim().ToUpperInvariant();
                ordens = ordens.Where(o =>
                    (!string.IsNullOrEmpty(o.PlacaSnapshot) && o.PlacaSnapshot.ToUpperInvariant().Contains(f)) ||
                    (!string.IsNullOrEmpty(o.ClienteNomeSnapshot) && o.ClienteNomeSnapshot.ToUpperInvariant().Contains(f)) ||
                    (!string.IsNullOrEmpty(o.Numero) && o.Numero.ToUpperInvariant().Contains(f)) ||
                    (!string.IsNullOrEmpty(o.VeiculoDescricaoSnapshot) && o.VeiculoDescricaoSnapshot.ToUpperInvariant().Contains(f))
                ).ToList();
            }

            return ordens;
        }

        public async Task<int> InserirItensEmOrdemServicoAsync(Guid ordemServicoId, IEnumerable<AIPartOrServiceProposal> itensPropostos)
        {
            if (itensPropostos == null) return 0;
            var listaItens = itensPropostos.Where(i => i.Selecionado).ToList();
            if (listaItens.Count == 0) return 0;

            using var conn = ObterConexaoAberta();

            // Validação de Integridade: Não permitir inserção em OS Encerrada
            using (var cmdCheck = conn.CreateCommand())
            {
                cmdCheck.CommandText = "SELECT Status FROM OrdensServico WHERE Id = @Id LIMIT 1;";
                cmdCheck.Parameters.AddWithValue("@Id", ordemServicoId.ToString());
                var statusObj = await cmdCheck.ExecuteScalarAsync();
                if (statusObj == null)
                {
                    throw new InvalidOperationException($"Ordem de Serviço #{ordemServicoId} não foi encontrada.");
                }

                var status = statusObj.ToString()?.Trim() ?? string.Empty;
                if (string.Equals(status, "Entregue", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(status, "Cancelada", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(status, "Faturada", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Não é permitido adicionar novos itens à Ordem de Serviço #{ordemServicoId} com status '{status}'.");
                }
            }

            using var trans = conn.BeginTransaction();
            int inseridos = 0;

            try
            {
                foreach (var item in listaItens)
                {
                    using var cmdItem = conn.CreateCommand();
                    cmdItem.Transaction = trans;
                    cmdItem.CommandText = @"
                        INSERT INTO OrdemServicoItens
                        (
                            Id, OrdemServicoId, ProdutoId, Tipo, Descricao,
                            Quantidade, ValorUnitario, CustoUnitario, EstoqueMovimentado
                        )
                        VALUES
                        (
                            @Id, @OSId, @ProdutoId, @Tipo, @Desc,
                            @Qtd, @Preco, 0, 0
                        );";

                    cmdItem.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                    cmdItem.Parameters.AddWithValue("@OSId", ordemServicoId.ToString());
                    cmdItem.Parameters.AddWithValue("@ProdutoId", item.ProdutoId.HasValue ? item.ProdutoId.Value.ToString() : (object)DBNull.Value);
                    cmdItem.Parameters.AddWithValue("@Tipo", item.Tipo);
                    cmdItem.Parameters.AddWithValue("@Desc", item.Descricao + (!string.IsNullOrWhiteSpace(item.CodigoFabricante) ? $" [{item.CodigoFabricante}]" : ""));
                    cmdItem.Parameters.AddWithValue("@Qtd", (double)item.Quantidade);
                    cmdItem.Parameters.AddWithValue("@Preco", (double)item.PrecoSugerido);

                    await cmdItem.ExecuteNonQueryAsync();
                    inseridos++;
                }

                // Registra evento de auditoria na OS
                using (var cmdEvento = conn.CreateCommand())
                {
                    cmdEvento.Transaction = trans;
                    cmdEvento.CommandText = @"
                        INSERT INTO OrdemServicoEventos
                        (
                            Id, OrdemServicoId, DataEvento, Titulo, Descricao, Tipo, Usuario
                        )
                        VALUES
                        (
                            @Id, @OSId, @Data, @Titulo, @Desc, @Tipo, @User
                        );";

                    cmdEvento.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                    cmdEvento.Parameters.AddWithValue("@OSId", ordemServicoId.ToString());
                    cmdEvento.Parameters.AddWithValue("@Data", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                    cmdEvento.Parameters.AddWithValue("@Titulo", "Itens Copilot IA Adicionados");
                    cmdEvento.Parameters.AddWithValue("@Desc", $"{inseridos} item(ns) identificados no diagnóstico elétrico foram adicionados à OS.");
                    cmdEvento.Parameters.AddWithValue("@Tipo", "DiagnosticoIA");
                    cmdEvento.Parameters.AddWithValue("@User", "PRIMOX Copilot");

                    await cmdEvento.ExecuteNonQueryAsync();
                }

                trans.Commit();
                _logger?.LogInfo($"Ponte IA ➔ OS: {inseridos} itens inseridos na OS #{ordemServicoId}.");
                return inseridos;
            }
            catch (Exception ex)
            {
                trans.Rollback();
                _logger?.LogError($"Erro ao inserir itens da IA na OS #{ordemServicoId}: {ex.Message}");
                throw;
            }
        }

        public async Task<Guid?> GerarRequisicaoCompraAsync(AIPartOrServiceProposal itemSemEstoque, string? observacoes = null)
        {
            if (itemSemEstoque == null) return null;

            try
            {
                using var conn = ObterConexaoAberta();
                using var cmd = conn.CreateCommand();

                // Cria tabela de Solicitações/Requisições de Compra se não existir
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS SolicitacoesCompra
                    (
                        Id TEXT PRIMARY KEY,
                        ProdutoId TEXT,
                        Descricao TEXT NOT NULL,
                        CodigoFabricante TEXT,
                        Quantidade REAL NOT NULL DEFAULT 1,
                        CustoEstimado REAL NOT NULL DEFAULT 0,
                        DataSolicitacao TEXT NOT NULL,
                        Status TEXT NOT NULL DEFAULT 'Pendente',
                        Origem TEXT NOT NULL DEFAULT 'CopilotIA',
                        Observacoes TEXT
                    );
                    INSERT INTO SolicitacoesCompra
                    (
                        Id, ProdutoId, Descricao, CodigoFabricante, Quantidade,
                        CustoEstimado, DataSolicitacao, Status, Origem, Observacoes
                    )
                    VALUES
                    (
                        @Id, @ProdId, @Desc, @Cod, @Qtd,
                        @Custo, @Data, 'Pendente', 'CopilotIA', @Obs
                    );";

                var reqId = Guid.NewGuid();
                cmd.Parameters.AddWithValue("@Id", reqId.ToString());
                cmd.Parameters.AddWithValue("@ProdId", itemSemEstoque.ProdutoId.HasValue ? itemSemEstoque.ProdutoId.Value.ToString() : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Desc", itemSemEstoque.Descricao);
                cmd.Parameters.AddWithValue("@Cod", (object?)itemSemEstoque.CodigoFabricante ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Qtd", (double)itemSemEstoque.Quantidade);
                cmd.Parameters.AddWithValue("@Custo", (double)itemSemEstoque.PrecoSugerido);
                cmd.Parameters.AddWithValue("@Data", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                cmd.Parameters.AddWithValue("@Obs", observacoes ?? "Requisição gerada pelo diagnóstico do PRIMOX Copilot IA.");

                await cmd.ExecuteNonQueryAsync();
                _logger?.LogInfo($"Requisição de compra #{reqId} gerada para {itemSemEstoque.Descricao}.");
                return reqId;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Erro ao gerar requisição de compra: {ex.Message}");
                return null;
            }
        }
    }
}
