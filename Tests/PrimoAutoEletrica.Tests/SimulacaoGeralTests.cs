using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable CS8632 // nullable annotations

namespace PrimoAutoEletrica.Tests
{
    /// <summary>
    /// Simulação geral abrangente que testa TODAS as operações CRUD do sistema:
    /// Cliente → Veículo → Produto → Orçamento → OS → Venda → Funcionário → Fornecedor
    /// </summary>
    public class SimulacaoGeralTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly string _testDir;
        private readonly DatabaseService _databaseService;
        private readonly LoggerService _loggerService;
        private readonly RepositoryRegistry _repositories;
        private readonly List<string> _erros = new();
        private readonly List<string> _ok = new();
        private readonly Random _rng = new();

        public SimulacaoGeralTests(ITestOutputHelper output)
        {
            _output = output;
            _testDir = Path.Combine(Path.GetTempPath(), $"PrimoSim_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDir);
            _loggerService = new LoggerService(_testDir);
            _databaseService = new DatabaseService(_testDir, logger: _loggerService);
            _repositories = new RepositoryRegistry(_databaseService, _loggerService);
        }

        public void Dispose()
        {
            try { if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true); } catch { }
        }

        [Fact]
        public void Simulacao_CicloCompleto_CRUD_TodosModulos()
        {
            _output.WriteLine("═══════════════════════════════════════════════════");
            _output.WriteLine("  SIMULAÇÃO GERAL - PRIMO AUTO ELÉTRICA");
            _output.WriteLine($"  {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            _output.WriteLine("═══════════════════════════════════════════════════\n");

            var cliente = TestarClientes();
            var veiculo = TestarVeiculos(cliente);
            var produto = TestarProdutos();
            TestarOrcamentos(cliente, veiculo);
            TestarOrdensServico(cliente, veiculo);
            TestarVendas();
            TestarFuncionarios();
            TestarFornecedores();
            TestarIntegridade(cliente, veiculo);
            TestarSeguranca();

            // RELATÓRIO
            _output.WriteLine("\n═══════════════════════════════════════════════════");
            _output.WriteLine("  RELATÓRIO FINAL");
            _output.WriteLine("═══════════════════════════════════════════════════");
            _output.WriteLine($"\n✅ Sucesso: {_ok.Count}");
            foreach (var o in _ok) _output.WriteLine($"   ✅ {o}");
            _output.WriteLine($"\n❌ Erros: {_erros.Count}");
            foreach (var e in _erros) _output.WriteLine($"   ❌ {e}");
            var total = _ok.Count + _erros.Count;
            _output.WriteLine($"\n📊 {_ok.Count}/{total} ({(_ok.Count * 100.0 / Math.Max(1, total)):F1}%)");

            Assert.True(_erros.Count == 0,
                $"❌ {_erros.Count} erro(s):\n" + string.Join("\n", _erros.Select(e => $"  - {e}")));
        }

        // ── CLIENTES ───────────────────────────────────────────────

        private Cliente TestarClientes()
        {
            _output.WriteLine("\n── FASE 1: CLIENTES ──────────────────────────────\n");
            var c = new Cliente
            {
                Id = Guid.NewGuid(),
                Nome = "João da Silva Teste",
                TipoPessoa = "Fisica",
                CPF = "52998224725",
                Telefone = "11987654321",
                Email = "joao.teste@email.com",
                CEP = "01310930",
                Rua = "Rua Augusta",
                Numero = "1000",
                Bairro = "Consolação",
                Cidade = "São Paulo",
                Estado = "SP",
                Ativo = true,
                DataCadastro = DateTime.Now,
                ConsentimentoLGPD = true,
                DataConsentimentoLGPD = DateTime.Now,
                OrigemConsentimentoLGPD = "Simulação"
            };

            Run("Cliente.Inserir", () => _repositories.Clientes.Inserir(c));

            Run("Cliente.ObterPorId", () =>
            {
                var lido = _repositories.Clientes.ObterPorId(c.Id);
                Assert.NotNull(lido);
                Assert.Equal(c.Nome, lido.Nome);
            });

            Run("Cliente.ObterTodos", () =>
            {
                var todos = _repositories.Clientes.ObterTodos();
                Assert.Contains(todos, x => x.Id == c.Id);
            });

            Run("Cliente.Atualizar", () =>
            {
                c.Nome = "João da Silva Atualizado";
                c.Telefone = "11999887766";
                _repositories.Clientes.Atualizar(c);
                var a = _repositories.Clientes.ObterPorId(c.Id);
                Assert.Equal("João da Silva Atualizado", a!.Nome);
            });

            Run("Cliente.InserirPJ", () =>
            {
                var pj = new Cliente
                {
                    Id = Guid.NewGuid(),
                    Nome = "Maria Santos PJ",
                    TipoPessoa = "Juridica",
                    CPF = "34713402036",
                    Telefone = "1133334444",
                    Ativo = true,
                    DataCadastro = DateTime.Now,
                    ConsentimentoLGPD = true,
                    DataConsentimentoLGPD = DateTime.Now,
                    OrigemConsentimentoLGPD = "Simulação"
                };
                _repositories.Clientes.Inserir(pj);
                Assert.NotNull(_repositories.Clientes.ObterPorId(pj.Id));
            });

            return c;
        }

        // ── VEÍCULOS ───────────────────────────────────────────────

        private Veiculo TestarVeiculos(Cliente cliente)
        {
            _output.WriteLine("\n── FASE 2: VEÍCULOS ──────────────────────────────\n");
            var v = new Veiculo
            {
                Id = Guid.NewGuid(),
                ClienteId = cliente.Id,
                Marca = "Toyota",
                Modelo = "Corolla",
                Ano = "2024",
                Cor = "Prata",
                Placa = $"TST{_rng.Next(1000, 9999)}",
                Chassi = $"9BD{_rng.Next(10000000, 99999999)}901234",
                Renavam = "12345678901",
                TipoVeiculo = "Passeio",
                SistemaEletrico = "12V Convencional",
                Motor = "2.0 Flex",
                Combustivel = "Flex",
                BateriaPrincipal = "60Ah",
                Quilometragem = 25000,
                Observacoes = "Veículo teste"
            };

            Run("Veiculo.SalvarNovo", () => _repositories.Clientes.SalvarVeiculo(v));

            Run("Veiculo.ObterTodos", () =>
            {
                var veiculos = _repositories.Clientes.ObterTodosVeiculos();
                Assert.Contains(veiculos, x => x.Id == v.Id);
            });

            Run("Veiculo.FiltrarPorCliente", () =>
            {
                var vc = _repositories.Clientes.ObterTodosVeiculos()
                    .Where(x => x.ClienteId == cliente.Id).ToList();
                Assert.Contains(vc, x => x.Id == v.Id);
            });

            Run("Veiculo.Atualizar", () =>
            {
                v.Quilometragem = 30000;
                v.BateriaAuxiliar = "Não possui";
                _repositories.Clientes.SalvarVeiculo(v);
                var a = _repositories.Clientes.ObterTodosVeiculos().First(x => x.Id == v.Id);
                Assert.Equal(30000, a.Quilometragem);
            });

            Run("Veiculo.SalvarCaminhao", () =>
            {
                _repositories.Clientes.SalvarVeiculo(new Veiculo
                {
                    Id = Guid.NewGuid(),
                    ClienteId = cliente.Id,
                    Marca = "Scania",
                    Modelo = "R-Series",
                    Ano = "2022",
                    Placa = $"CAM{_rng.Next(1000, 9999)}",
                    TipoVeiculo = "Caminhão",
                    SistemaEletrico = "24V",
                    Quilometragem = 150000
                });
            });

            Run("Veiculo.SalvarMinimo", () =>
            {
                _repositories.Clientes.SalvarVeiculo(new Veiculo
                {
                    Id = Guid.NewGuid(),
                    ClienteId = cliente.Id,
                    Marca = "Fiat",
                    Modelo = "Uno",
                    Ano = "2020",
                    Placa = $"MIN{_rng.Next(1000, 9999)}",
                    Quilometragem = 0
                });
            });

            Run("Veiculo.SalvarPlacaMercosul", () =>
            {
                _repositories.Clientes.SalvarVeiculo(new Veiculo
                {
                    Id = Guid.NewGuid(),
                    ClienteId = cliente.Id,
                    Marca = "Honda",
                    Modelo = "Civic",
                    Ano = "2023",
                    Placa = $"BRA{_rng.Next(0, 9)}E{_rng.Next(10, 99)}",
                    Quilometragem = 15000
                });
            });

            Run("Veiculo.SalvarCompleto", () =>
            {
                var vc = new Veiculo
                {
                    Id = Guid.NewGuid(),
                    ClienteId = cliente.Id,
                    Marca = "Volkswagen",
                    Modelo = "Gol",
                    Ano = "2019",
                    Cor = "Branco",
                    Placa = $"CMP{_rng.Next(1000, 9999)}",
                    Chassi = $"9BW{_rng.Next(10000000, 99999999)}123456",
                    Renavam = "98765432100",
                    TipoVeiculo = "Passeio",
                    SistemaEletrico = "12V Convencional",
                    Motor = "1.0 MPI",
                    Combustivel = "Flex",
                    BateriaPrincipal = "45Ah",
                    BateriaInstalada = "Moura M45FD",
                    BateriaMarca = "Moura",
                    BateriaAmperagem = "45",
                    BateriaDataInstalacao = DateTime.Today.AddMonths(-6),
                    Alternador = "90A Original",
                    MotorPartida = "Bosch 12V",
                    Quilometragem = 85000,
                    TesteTensaoRepouso = "12.6V",
                    TesteTensaoPartida = "10.5V",
                    TesteCargaAlternador = "14.2V",
                    CorrenteFuga = "0.03A",
                    EstadoAterramentos = "Bom estado",
                    ChicotesReparados = "Nenhum",
                    FusiveisSubstituidos = "F15 20A",
                    RelesSubstituidos = "Nenhum",
                    LampadasSubstituidas = "Farol esquerdo H4",
                    AcessoriosInstalados = "Alarme, Trava elétrica",
                    ObservacoesTecnicasEletricas = "OK",
                    HistoricoTecnico = "Revisão em 01/2024",
                    ObservacoesEletricasRecorrentes = "Nenhuma",
                    RetornoRecomendadoEm = DateTime.Today.AddMonths(6),
                    GarantiaValidaAte = DateTime.Today.AddYears(1),
                    ProximaRevisaoEm = DateTime.Today.AddMonths(3),
                    Observacoes = "Todos os campos preenchidos"
                };
                _repositories.Clientes.SalvarVeiculo(vc);
                var lido = _repositories.Clientes.ObterTodosVeiculos().First(x => x.Id == vc.Id);
                Assert.Equal("Volkswagen", lido.Marca);
                Assert.Equal("Moura", lido.BateriaMarca);
                Assert.Equal(85000, lido.Quilometragem);
            });

            return v;
        }

        // ── PRODUTOS ───────────────────────────────────────────────

        private Produto TestarProdutos()
        {
            _output.WriteLine("\n── FASE 3: PRODUTOS ──────────────────────────────\n");
            var p = new Produto
            {
                Id = Guid.NewGuid(),
                Nome = "Bateria Moura 60Ah",
                Codigo = $"BAT-{_rng.Next(10000, 99999)}",
                CodigoBarras = $"789{_rng.Next(100000000, 999999999)}",
                Descricao = "Bateria automotiva 60Ah 12V",
                Categoria = "Baterias",
                Marca = "Moura",
                UnidadeMedida = "UN",
                PrecoCompra = 320.00m,
                PrecoVenda = 480.00m,
                MargemLucro = 50.0m,
                QuantidadeEstoque = 15,
                QuantidadeMinima = 3,
                Ativo = true,
                DataCadastro = DateTime.Now
            };

            Run("Produto.Inserir", () => _repositories.Produtos.Inserir(p));

            Run("Produto.ObterPorId", () =>
            {
                var lido = _repositories.Produtos.ObterPorId(p.Id);
                Assert.NotNull(lido);
                Assert.Equal(p.Nome, lido!.Nome);
                Assert.Equal(p.PrecoVenda, lido.PrecoVenda);
            });

            Run("Produto.ObterTodos", () =>
            {
                var todos = _repositories.Produtos.ObterTodos();
                Assert.Contains(todos, x => x.Id == p.Id);
            });

            Run("Produto.Atualizar", () =>
            {
                p.PrecoVenda = 520.00m;
                p.QuantidadeEstoque = 10;
                _repositories.Produtos.Atualizar(p);
                var a = _repositories.Produtos.ObterPorId(p.Id);
                Assert.Equal(520.00m, a!.PrecoVenda);
            });

            Run("Produto.InserirServico", () =>
            {
                _repositories.Produtos.Inserir(new Produto
                {
                    Id = Guid.NewGuid(),
                    Nome = "MO Troca de Bateria",
                    Codigo = $"SRV-{_rng.Next(1000, 9999)}",
                    Categoria = "Serviços",
                    PrecoVenda = 80.00m,
                    QuantidadeEstoque = 999,
                    Ativo = true,
                    DataCadastro = DateTime.Now
                });
            });

            Run("Produto.Excluir", () =>
            {
                var pe = new Produto
                {
                    Id = Guid.NewGuid(),
                    Nome = "Para Excluir",
                    Codigo = $"DEL-{_rng.Next(1000, 9999)}",
                    PrecoVenda = 10.00m,
                    Ativo = true,
                    DataCadastro = DateTime.Now
                };
                _repositories.Produtos.Inserir(pe);
                _repositories.Produtos.Excluir(pe.Id);
            });

            return p;
        }

        // ── ORÇAMENTOS ─────────────────────────────────────────────

        private void TestarOrcamentos(Cliente cliente, Veiculo veiculo)
        {
            _output.WriteLine("\n── FASE 4: ORÇAMENTOS ────────────────────────────\n");

            Orcamento orc = null!;

            // Criar produto para usar nos itens do orçamento
            var produtoOrc = new Produto
            {
                Id = Guid.NewGuid(),
                Nome = "Bateria Orçamento 60Ah",
                Codigo = $"ORC-{_rng.Next(10000, 99999)}",
                Categoria = "Baterias",
                PrecoVenda = 480.00m,
                QuantidadeEstoque = 10,
                Ativo = true,
                DataCadastro = DateTime.Now
            };
            _repositories.Produtos.Inserir(produtoOrc);

            Run("Orcamento.Criar", () =>
            {
                var db = new OrcamentoDatabaseService();
                orc = new Orcamento
                {
                    Id = Guid.NewGuid(),
                    ClienteId = cliente.Id,
                    VeiculoId = veiculo.Id,
                    Status = "Pendente",
                    DataCriacao = DateTime.Now,
                    DataValidade = DateTime.Now.AddDays(30),
                    Observacoes = "Teste simulação",
                    Itens = new List<OrcamentoItem>
                    {
                        new OrcamentoItem
                        {
                            Id = Guid.NewGuid(),
                            ProdutoId = produtoOrc.Id,
                            ProdutoNome = produtoOrc.Nome,
                            ProdutoCodigo = produtoOrc.Codigo,
                            Quantidade = 1,
                            PrecoUnitario = 480.00m,
                            Tipo = "Produto"
                        }
                    }
                };
                db.AdicionarOrcamento(orc);
            });

            Run("Orcamento.Ler", () =>
            {
                var db = new OrcamentoDatabaseService();
                var lido = db.ObterOrcamentoPorId(orc.Id);
                Assert.NotNull(lido);
                Assert.Equal(cliente.Id, lido!.ClienteId);
            });

            Run("Orcamento.ListarTodos", () =>
            {
                var db = new OrcamentoDatabaseService();
                Assert.True(db.ObterTodosOrcamentos().Count >= 1);
            });

            Run("Orcamento.Atualizar", () =>
            {
                var db = new OrcamentoDatabaseService();
                orc.Status = "Enviado";
                // Ensure items list is populated for update validation
                if (orc.Itens == null || orc.Itens.Count == 0)
                {
                    orc.Itens = new List<OrcamentoItem>
                    {
                        new OrcamentoItem
                        {
                            Id = Guid.NewGuid(),
                            OrcamentoId = orc.Id,
                            ProdutoId = produtoOrc.Id,
                            ProdutoNome = produtoOrc.Nome,
                            ProdutoCodigo = produtoOrc.Codigo,
                            Quantidade = 1,
                            PrecoUnitario = 480.00m,
                            Tipo = "Produto"
                        }
                    };
                }
                db.AtualizarOrcamento(orc);
                Assert.Equal("Enviado", db.ObterOrcamentoPorId(orc.Id)!.Status);
            });

            Run("Orcamento.AdicionarItem", () =>
            {
                var db = new OrcamentoDatabaseService();
                db.AdicionarOrcamentoItem(new OrcamentoItem
                {
                    Id = Guid.NewGuid(),
                    OrcamentoId = orc.Id,
                    ProdutoId = produtoOrc.Id,
                    ProdutoNome = produtoOrc.Nome,
                    ProdutoCodigo = produtoOrc.Codigo,
                    Quantidade = 2,
                    PrecoUnitario = 480.00m,
                    Tipo = "Produto"
                });
            });

            Run("Orcamento.LerItens", () =>
            {
                var db = new OrcamentoDatabaseService();
                Assert.True(db.ObterItensDoOrcamento(orc.Id).Count >= 1);
            });

            Run("Orcamento.Excluir", () =>
            {
                var db = new OrcamentoDatabaseService();
                var oe = new Orcamento
                {
                    Id = Guid.NewGuid(),
                    ClienteId = cliente.Id,
                    Status = "Pendente",
                    DataCriacao = DateTime.Now,
                    Itens = new List<OrcamentoItem>
                    {
                        new OrcamentoItem
                        {
                            Id = Guid.NewGuid(),
                            ProdutoId = produtoOrc.Id,
                            ProdutoNome = produtoOrc.Nome,
                            Quantidade = 1,
                            PrecoUnitario = 100.00m,
                            Tipo = "Produto"
                        }
                    }
                };
                db.AdicionarOrcamento(oe);
                db.ExcluirOrcamento(oe.Id);
            });
        }

        // ── ORDENS DE SERVIÇO ──────────────────────────────────────

        private void TestarOrdensServico(Cliente cliente, Veiculo veiculo)
        {
            _output.WriteLine("\n── FASE 5: ORDENS DE SERVIÇO ─────────────────────\n");

            OrdemServico os = null!;

            Run("OS.Criar", () =>
            {
                os = new OrdemServico
                {
                    Id = Guid.NewGuid(),
                    ClienteId = cliente.Id,
                    ClienteNomeSnapshot = cliente.Nome,
                    VeiculoId = veiculo.Id,
                    VeiculoDescricaoSnapshot = $"{veiculo.Marca} {veiculo.Modelo}",
                    PlacaSnapshot = veiculo.Placa,
                    Status = "Aberta",
                    DataAbertura = DateTime.Now,
                    ProblemaRelatado = "Bateria descarregando",
                    Ativo = true
                };
                _repositories.OrdensServico.Inserir(os);
            });

            Run("OS.LerPorId", () =>
            {
                var lida = _repositories.OrdensServico.ObterPorId(os.Id);
                Assert.NotNull(lida);
                Assert.Equal("Aberta", lida!.Status);
            });

            Run("OS.ListarTodos", () =>
            {
                Assert.True(_repositories.OrdensServico.ObterTodos().Count >= 1);
            });

            Run("OS.ListarPorCliente", () =>
            {
                var lista = _repositories.OrdensServico.ObterPorClienteId(cliente.Id);
                Assert.Contains(lista, x => x.Id == os.Id);
            });

            Run("OS.Atualizar", () =>
            {
                os.Status = "Em andamento";
                _repositories.OrdensServico.Atualizar(os);
                Assert.Equal("Em andamento", _repositories.OrdensServico.ObterPorId(os.Id)!.Status);
            });

            Run("OS.Finalizar", () =>
            {
                os.Status = "Finalizada";
                _repositories.OrdensServico.Atualizar(os);
                Assert.Equal("Finalizada", _repositories.OrdensServico.ObterPorId(os.Id)!.Status);
            });

            Run("OS.Excluir", () =>
            {
                var oe = new OrdemServico
                {
                    Id = Guid.NewGuid(),
                    ClienteId = cliente.Id,
                    ClienteNomeSnapshot = cliente.Nome,
                    VeiculoId = veiculo.Id,
                    Status = "Aberta",
                    DataAbertura = DateTime.Now,
                    Ativo = true
                };
                _repositories.OrdensServico.Inserir(oe);
                _repositories.OrdensServico.Excluir(oe.Id);
            });
        }

        // ── VENDAS / PDV ───────────────────────────────────────────

        private void TestarVendas()
        {
            _output.WriteLine("\n── FASE 6: VENDAS / PDV ──────────────────────────\n");

            Run("Venda.Inserir", () =>
            {
                var repo = new VendaRepository(_databaseService);
                using var conn = _databaseService.GetConnection();
                conn.Open();
                using var tx = conn.BeginTransaction();

                var venda = new Venda
                {
                    Id = Guid.NewGuid(),
                    Data = DateTime.Now,
                    Total = 560.00m,
                    FormaPagamento = "Dinheiro",
                    Status = "Concluida",
                    Usuario = "admin"
                };
                repo.InserirVenda(conn, tx, venda);
                tx.Commit();
            });

            Run("Venda.Listar", () =>
            {
                var repo = new VendaRepository(_databaseService);
                Assert.NotNull(repo.ObterVendas(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1)));
            });

            Run("Venda.HistoricoOperacional", () =>
            {
                var repo = new VendaRepository(_databaseService);
                Assert.NotNull(repo.ObterHistoricoOperacional(50));
            });
        }

        // ── FUNCIONÁRIOS ───────────────────────────────────────────

        private void TestarFuncionarios()
        {
            _output.WriteLine("\n── FASE 7: FUNCIONÁRIOS ──────────────────────────\n");

            Run("Funcionario.ObterTodos", () =>
            {
                Assert.NotNull(_repositories.Funcionarios.ObterTodos(somenteAtivos: false));
            });
        }

        // ── FORNECEDORES ───────────────────────────────────────────

        private void TestarFornecedores()
        {
            _output.WriteLine("\n── FASE 8: FORNECEDORES ──────────────────────────\n");

            Run("Fornecedor.Inserir", () =>
            {
                _repositories.Fornecedores.Inserir(new Fornecedor
                {
                    Id = Guid.NewGuid(),
                    RazaoSocial = "Moura Baterias LTDA",
                    NomeFantasia = "Moura Baterias",
                    CNPJ = "11222333000181",
                    Telefone = "81999998888",
                    Email = "contato@moura.com.br",
                    Ativo = true,
                    DataCadastro = DateTime.Now
                });
            });

            Run("Fornecedor.ObterTodos", () =>
            {
                Assert.True(_repositories.Fornecedores.ObterTodos().Count >= 1);
            });

            Run("Fornecedor.ObterPorId", () =>
            {
                var todos = _repositories.Fornecedores.ObterTodos();
                if (todos.Count > 0)
                    Assert.NotNull(_repositories.Fornecedores.ObterPorId(todos[0].Id));
            });

            Run("Fornecedor.Excluir", () =>
            {
                var fe = new Fornecedor
                {
                    Id = Guid.NewGuid(),
                    RazaoSocial = "Para Excluir LTDA",
                    NomeFantasia = "Excluir",
                    CNPJ = "33000167000101",
                    Ativo = true,
                    DataCadastro = DateTime.Now
                };
                _repositories.Fornecedores.Inserir(fe);
                _repositories.Fornecedores.Excluir(fe.Id);
            });
        }

        // ── INTEGRIDADE ────────────────────────────────────────────

        private void TestarIntegridade(Cliente cliente, Veiculo veiculo)
        {
            _output.WriteLine("\n── FASE 9: INTEGRIDADE ───────────────────────────\n");

            Run("Integridade.VeiculoCliente", () =>
            {
                var vc = _repositories.Clientes.ObterTodosVeiculos()
                    .Where(x => x.ClienteId == cliente.Id).ToList();
                Assert.True(vc.Count >= 1);
            });

            Run("Integridade.ExcluirVeiculo", () =>
            {
                var vt = new Veiculo
                {
                    Id = Guid.NewGuid(),
                    ClienteId = cliente.Id,
                    Marca = "Honda", Modelo = "Civic", Ano = "2023",
                    Placa = $"DEL{_rng.Next(1000, 9999)}",
                    Quilometragem = 5000
                };
                _repositories.Clientes.SalvarVeiculo(vt);
                _repositories.Clientes.ExcluirVeiculo(vt.Id);
                Assert.Null(_repositories.Clientes.ObterTodosVeiculos().FirstOrDefault(x => x.Id == vt.Id));
            });

            Run("Integridade.PlacaDuplicada", () =>
            {
                Assert.Throws<InvalidOperationException>(() =>
                    _repositories.Clientes.SalvarVeiculo(new Veiculo
                    {
                        Id = Guid.NewGuid(),
                        ClienteId = cliente.Id,
                        Marca = "Ford", Modelo = "Ka", Ano = "2021",
                        Placa = veiculo.Placa,
                        Quilometragem = 10000
                    }));
            });

            Run("Integridade.PRAGMA", () =>
            {
                using var conn = _databaseService.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA integrity_check;";
                Assert.Equal("ok", cmd.ExecuteScalar()?.ToString());
            });

            Run("Integridade.Tabelas", () =>
            {
                using var conn = _databaseService.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
                var tabelas = new List<string>();
                using var reader = cmd.ExecuteReader();
                while (reader.Read()) tabelas.Add(reader.GetString(0));
                _output.WriteLine($"   📋 Tabelas ({tabelas.Count}): {string.Join(", ", tabelas)}");
                Assert.True(tabelas.Count >= 5);
            });
        }

        // ── SEGURANÇA ──────────────────────────────────────────────

        private void TestarSeguranca()
        {
            _output.WriteLine("\n── FASE 10: SEGURANÇA ────────────────────────────\n");

            Run("Seguranca.HashSenha", () =>
            {
                var hash = PasswordHasherService.HashPassword("MinhaS3nh@!");
                Assert.NotEqual("MinhaS3nh@!", hash);
                Assert.True(hash.Length > 50);
            });

            Run("Seguranca.VerificarCorreta", () =>
            {
                var hash = PasswordHasherService.HashPassword("Teste@123");
                Assert.True(PasswordHasherService.VerifyPassword("Teste@123", hash));
            });

            Run("Seguranca.VerificarErrada", () =>
            {
                var hash = PasswordHasherService.HashPassword("Teste@123");
                Assert.False(PasswordHasherService.VerifyPassword("Errada", hash));
            });

            Run("Seguranca.NeedsRehash_Plano", () =>
            {
                Assert.True(PasswordHasherService.NeedsRehash("textoPlano"));
            });

            Run("Seguranca.NeedsRehash_Hash", () =>
            {
                Assert.False(PasswordHasherService.NeedsRehash(PasswordHasherService.HashPassword("x")));
            });
        }

        // ── HELPER ─────────────────────────────────────────────────

        private void Run(string nome, Action acao)
        {
            try
            {
                acao();
                _ok.Add(nome);
                _output.WriteLine($"   ✅ {nome}");
            }
            catch (Exception ex)
            {
                var msg = $"{nome}: {ex.GetType().Name} - {ex.Message}";
                _erros.Add(msg);
                _output.WriteLine($"   ❌ {msg}");
                if (ex.InnerException != null)
                    _output.WriteLine($"      Inner: {ex.InnerException.Message}");
            }
        }
    }
}
