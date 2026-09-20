using System;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests
{
    public sealed class Primox360ServiceTests : IDisposable
    {
        private readonly string _testDir;
        private readonly DatabaseService _database;
        private readonly LoggerService _logger;
        private readonly RepositoryRegistry _repos;
        private readonly Primox360Service _svc;

        public Primox360ServiceTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"Primox360_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDir);
            _logger = new LoggerService(_testDir);
            _database = new DatabaseService(_testDir, logger: _logger);
            _repos = new RepositoryRegistry(_database, _logger);
            _svc = new Primox360Service(
                _repos.Clientes,
                _repos.OrdensServico,
                () => new OrcamentoDatabaseService(_database),
                () => new FinanceiroDatabaseService(_database),
                () => new VendaRepository(_database),
                produtos: _repos.Produtos);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testDir))
                {
                    Directory.Delete(_testDir, recursive: true);
                }
            }
            catch
            {
                // ignore cleanup
            }
        }

        [Fact]
        public void Case01_Cliente_UmVeiculo_UmaOs_ReceitaPorId()
        {
            var (cliente, veiculo, ordem) = CriarClienteVeiculoOs("CASE01", 250m);

            var snap = _svc.ObterCliente360(cliente.Id);
            Assert.Equal(1, snap.VeiculosCount);
            Assert.Equal(1, snap.OsCount);
            Assert.Equal(250m, snap.ReceitaOsTotal);
            Assert.Equal(250m, snap.TicketMedioOs);
            Assert.Contains(ordem.Id, snap.OrdemServicoIds);
            Assert.Contains(veiculo.Id, snap.VeiculoIds);

            var v360 = _svc.ObterVeiculo360(veiculo.Id);
            Assert.Equal(1, v360.OsCountPorVeiculoId);
            Assert.Equal(250m, v360.ReceitaAcumuladaPorVeiculoId);
            Assert.False(v360.UsaFallbackPlaca);

            var os360 = _svc.ObterOrdemServico360(ordem.Id);
            Assert.Equal("CONNECTED", os360.ClienteLink);
            Assert.Equal("CONNECTED", os360.VeiculoLink);
        }

        [Fact]
        public void Case02_DoisVeiculos_OsNaoCruzam()
        {
            var cliente = CriarCliente("CASE02-A");
            var vA = CriarVeiculo(cliente.Id, "AAA1A11");
            var vB = CriarVeiculo(cliente.Id, "BBB2B22");
            var osA = CriarOs(cliente, vA, 100m);
            var osB = CriarOs(cliente, vB, 200m);

            var snapA = _svc.ObterVeiculo360(vA.Id);
            var snapB = _svc.ObterVeiculo360(vB.Id);

            Assert.Equal(100m, snapA.ReceitaAcumuladaPorVeiculoId);
            Assert.Equal(200m, snapB.ReceitaAcumuladaPorVeiculoId);
            Assert.DoesNotContain(osB.Id, snapA.OrdemServicoIds);
            Assert.DoesNotContain(osA.Id, snapB.OrdemServicoIds);

            var c360 = _svc.ObterCliente360(cliente.Id);
            Assert.Equal(2, c360.VeiculosCount);
            Assert.Equal(2, c360.OsCount);
            Assert.Equal(300m, c360.ReceitaOsTotal);
        }

        [Fact]
        public void Case03_ClienteB_NaoRecebeOsDeClienteA()
        {
            var a = CriarCliente("CASE03-A");
            var b = CriarCliente("CASE03-B");
            var va = CriarVeiculo(a.Id, "CCC3C33");
            var osA = CriarOs(a, va, 80m);

            var snapB = _svc.ObterCliente360(b.Id);
            Assert.Equal(0, snapB.OsCount);
            Assert.DoesNotContain(osA.Id, snapB.OrdemServicoIds);
            Assert.Equal(0m, snapB.ReceitaOsTotal);
        }

        [Fact]
        public void Case05_FinanceiroVinculadoPorOrigem_NaoPorNome()
        {
            var (cliente, veiculo, ordem) = CriarClienteVeiculoOs("CASE05", 150m);
            InserirContaReceberIntegrada(ordem.Id, "OrdemServicoContaReceber", cliente.Nome, 150m, "Pendente");
            // Conta com mesmo nome mas sem vínculo ID — NÃO deve entrar
            InserirContaReceberManual(cliente.Nome, 999m, "Pendente");

            var vinculados = _svc.ObterContasReceberVinculadasAoCliente(cliente.Id);
            Assert.Single(vinculados);
            Assert.Equal(150m, vinculados[0].Valor);

            var snap = _svc.ObterCliente360(cliente.Id);
            Assert.Equal(150m, snap.DividaVinculadaPorId);
            Assert.Equal(1, snap.ContasReceberVinculadasPendentes);
            Assert.Contains("150", snap.DividaTotalDisplay);
            Assert.Contains("ClienteId", snap.DividaTotalDisplay, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("999", snap.DividaTotalDisplay);
        }

        [Fact]
        public void Case06_ClienteSemFinanceiro_DividaZeroVinculada()
        {
            var cliente = CriarCliente("CASE06");
            var snap = _svc.ObterCliente360(cliente.Id);
            Assert.Equal(0m, snap.DividaVinculadaPorId);
            Assert.Equal(0, snap.ContasReceberVinculadasPendentes);
        }

        [Fact]
        public void Case07_VeiculoSemOs()
        {
            var cliente = CriarCliente("CASE07");
            var veiculo = CriarVeiculo(cliente.Id, "DDD4D44");
            var snap = _svc.ObterVeiculo360(veiculo.Id);
            Assert.Equal(0, snap.OsCountPorVeiculoId);
            Assert.Null(snap.DiasDesdeUltimoServico);
            Assert.Equal(0m, snap.ReceitaAcumuladaPorVeiculoId);
        }

        [Fact]
        public void Case08_OsSemOrcamento_LinkMissing()
        {
            var (cliente, veiculo, ordem) = CriarClienteVeiculoOs("CASE08", 50m);
            var os360 = _svc.ObterOrdemServico360(ordem.Id);
            Assert.Equal("MISSING", os360.OrcamentoLink);
            Assert.Equal("MISSING", os360.FiscalLink);
            Assert.Equal("MISSING", os360.PosVendaLink);
        }

        [Fact]
        public void Case04_OrcamentoAprovadoERecusado_Metricas()
        {
            var cliente = CriarCliente("CASE04");
            var veiculo = CriarVeiculo(cliente.Id, "EEE5E55");
            SalvarOrcamento(cliente.Id, veiculo.Id, "ORC-A", "Aprovado", 100m);
            SalvarOrcamento(cliente.Id, veiculo.Id, "ORC-R", "Recusado", 40m);
            SalvarOrcamento(cliente.Id, veiculo.Id, "ORC-X", "Rejeitado", 10m);

            var snap = _svc.ObterCliente360(cliente.Id);
            Assert.Equal(3, snap.OrcamentosCount);
            Assert.Equal(1, snap.OrcamentosAprovados);
            Assert.Equal(2, snap.OrcamentosRecusados);
            Assert.Equal(50m, snap.ValorPerdidoOrcamentos);

            var v360 = _svc.ObterVeiculo360(veiculo.Id);
            Assert.Equal(3, v360.OrcamentosPorVeiculoId);
        }

        [Fact]
        public void StatusHelpers_AprovadoRecusado()
        {
            Assert.True(Primox360Service.EhOrcamentoAprovado("Aprovado"));
            Assert.True(Primox360Service.EhOrcamentoAprovado("Convertido em OS"));
            Assert.True(Primox360Service.EhOrcamentoRecusado("Recusado"));
            Assert.True(Primox360Service.EhOrcamentoRecusado("Rejeitado"));
            Assert.False(Primox360Service.EhOrcamentoAprovado("Rascunho"));
        }

        [Fact]
        public void CaseProduto360_UsaSomenteProdutoIdNasOs()
        {
            var (cliente, veiculo, _) = CriarClienteVeiculoOs("ABC1D23", 50m);
            var produto = new Produto
            {
                Id = Guid.NewGuid(),
                Codigo = "P360-1",
                Nome = "Bobina teste",
                QuantidadeEstoque = 3,
                QuantidadeMinima = 5,
                PrecoCompra = 10m,
                PrecoVenda = 25m,
                Fornecedor = "Fornecedor X",
                Ativo = true
            };
            _repos.Produtos.Inserir(produto);

            var ordem = new OrdemServico
            {
                Id = Guid.NewGuid(),
                Numero = $"OS-P-{Guid.NewGuid():N}"[..12],
                ClienteId = cliente.Id,
                VeiculoId = veiculo.Id,
                ClienteNomeSnapshot = cliente.Nome,
                VeiculoDescricaoSnapshot = $"{veiculo.Marca} {veiculo.Modelo}",
                PlacaSnapshot = veiculo.Placa,
                Status = "EmAndamento",
                DataAbertura = DateTime.Today,
                Ativo = true,
                Itens =
                {
                    new OrdemServicoItem
                    {
                        Id = Guid.NewGuid(),
                        Tipo = "Peca",
                        Descricao = produto.Nome,
                        ProdutoId = produto.Id,
                        Quantidade = 2,
                        ValorUnitario = 25m
                    }
                }
            };
            _repos.OrdensServico.Inserir(ordem);

            var snap = _svc.ObterProduto360(produto.Id);
            Assert.Equal(produto.Id, snap.ProdutoId);
            Assert.Equal(1, snap.OsComUsoCount);
            Assert.Equal(2m, snap.QuantidadeUsadaEmOs);
            Assert.Equal(50m, snap.ValorUsadoEmOs);
            Assert.True(snap.EstoqueCritico);
            Assert.Contains(ordem.Id, snap.OrdemServicoIds);
            Assert.Contains("CRÍTICO", snap.HubResumo);
            Assert.NotEmpty(snap.OrdemServicoResumos);
            Assert.Contains(ordem.Numero, snap.OrdemServicoResumos[0]);
        }

        private (Cliente cliente, Veiculo veiculo, OrdemServico ordem) CriarClienteVeiculoOs(string tag, decimal valor)
        {
            var cliente = CriarCliente(tag);
            var veiculo = CriarVeiculo(cliente.Id, tag.Length >= 7 ? tag[..7].ToUpperInvariant() : "ZZZ9Z99");
            var ordem = CriarOs(cliente, veiculo, valor);
            return (cliente, veiculo, ordem);
        }

        private Cliente CriarCliente(string nome)
        {
            var cliente = new Cliente
            {
                Id = Guid.NewGuid(),
                Nome = nome,
                Telefone = "11999990000",
                Ativo = true,
                DataCadastro = DateTime.Today.AddDays(-30),
                ConsentimentoLGPD = true,
                DataConsentimentoLGPD = DateTime.Today,
                OrigemConsentimentoLGPD = "Teste"
            };
            _repos.Clientes.Inserir(cliente);
            return cliente;
        }

        private Veiculo CriarVeiculo(Guid clienteId, string placa)
        {
            var veiculo = new Veiculo
            {
                Id = Guid.NewGuid(),
                ClienteId = clienteId,
                Placa = placa,
                Marca = "Test",
                Modelo = "Model",
                Ano = "2020",
                Quilometragem = 10000
            };
            _repos.Clientes.SalvarVeiculo(veiculo);
            return veiculo;
        }

        private OrdemServico CriarOs(Cliente cliente, Veiculo veiculo, decimal valor)
        {
            var ordem = new OrdemServico
            {
                Id = Guid.NewGuid(),
                Numero = $"OS-{Guid.NewGuid():N}"[..12],
                ClienteId = cliente.Id,
                VeiculoId = veiculo.Id,
                ClienteNomeSnapshot = cliente.Nome,
                VeiculoDescricaoSnapshot = $"{veiculo.Marca} {veiculo.Modelo}",
                PlacaSnapshot = veiculo.Placa,
                Status = "Finalizada",
                DataAbertura = DateTime.Today.AddDays(-2),
                Ativo = true,
                Itens =
                {
                    new OrdemServicoItem
                    {
                        Id = Guid.NewGuid(),
                        Tipo = "Servico",
                        Descricao = "Servico teste",
                        Quantidade = 1,
                        ValorUnitario = valor
                    }
                }
            };
            _repos.OrdensServico.Inserir(ordem);
            return ordem;
        }

        private void SalvarOrcamento(Guid clienteId, Guid veiculoId, string numero, string status, decimal total)
        {
            var id = Guid.NewGuid();
            using var connection = _database.GetConnection();
            connection.Open();
            _ = new OrcamentoDatabaseService(_database);
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Orcamentos
                (Id, ClienteId, VeiculoId, Numero, Status, DataCriacao, Subtotal, Desconto, DescontoTipo, DescontoPercentual, Acrescimo, Total, MargemLucro, LucroEstimado, ComissaoVendedor, ImpostosEstimados, Observacoes, Diagnostico, CondicoesPagamento, PrazoEntrega)
                VALUES
                (@id, @clienteId, @veiculoId, @numero, @status, @data, @total, 0, 'Valor', 0, 0, @total, 0, 0, 0, 0, '', '', '', '');";
            AddParam(cmd, "@id", id.ToString());
            AddParam(cmd, "@clienteId", clienteId.ToString());
            AddParam(cmd, "@veiculoId", veiculoId.ToString());
            AddParam(cmd, "@numero", numero);
            AddParam(cmd, "@status", status);
            AddParam(cmd, "@data", DateTime.Today.ToString("O"));
            AddParam(cmd, "@total", total);
            cmd.ExecuteNonQuery();
        }

        private static void AddParam(System.Data.Common.DbCommand cmd, string name, object? value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        private void InserirContaReceberIntegrada(Guid referenciaId, string origem, string clienteNome, decimal valor, string status)
        {
            _ = new FinanceiroDatabaseService(_database);
            using var connection = _database.GetConnection();
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ContasReceber
                (Cliente, Descricao, Valor, DataVencimento, DataPagamento, Status, FormaPagamento, Observacoes, DataCriacao, Origem, ReferenciaExterna)
                VALUES
                (@cliente, @desc, @valor, @venc, NULL, @status, '', '', @criacao, @origem, @ref);";
            AddParam(cmd, "@cliente", clienteNome);
            AddParam(cmd, "@desc", "OS integrada");
            AddParam(cmd, "@valor", valor);
            AddParam(cmd, "@venc", DateTime.Today.AddDays(7).ToString("O"));
            AddParam(cmd, "@status", status);
            AddParam(cmd, "@criacao", DateTime.UtcNow.ToString("O"));
            AddParam(cmd, "@origem", origem);
            AddParam(cmd, "@ref", referenciaId.ToString());
            cmd.ExecuteNonQuery();
        }

        private void InserirContaReceberManual(string clienteNome, decimal valor, string status)
        {
            _ = new FinanceiroDatabaseService(_database);
            using var connection = _database.GetConnection();
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ContasReceber
                (Cliente, Descricao, Valor, DataVencimento, DataPagamento, Status, FormaPagamento, Observacoes, DataCriacao, Origem, ReferenciaExterna)
                VALUES
                (@cliente, @desc, @valor, @venc, NULL, @status, '', '', @criacao, NULL, NULL);";
            AddParam(cmd, "@cliente", clienteNome);
            AddParam(cmd, "@desc", "Manual sem vinculo");
            AddParam(cmd, "@valor", valor);
            AddParam(cmd, "@venc", DateTime.Today.AddDays(7).ToString("O"));
            AddParam(cmd, "@status", status);
            AddParam(cmd, "@criacao", DateTime.UtcNow.ToString("O"));
            cmd.ExecuteNonQuery();
        }
    }
}
