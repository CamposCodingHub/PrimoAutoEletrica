using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using PdfSharpCore.Pdf.IO;
using PrimoAutoEletrica.Data.Repositories;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.UserControls;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views;
using PrimoAutoEletrica.Views.Clientes;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        // Criacao de fixtures, dados sinteticos e geradores de documentos.

        private string PersistReport(UiSmokeTestRunResult result)
        {
            var baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "smoke-tests");
            Directory.CreateDirectory(baseDirectory);

            var path = Path.Combine(baseDirectory, $"ui-smoke-{DateTime.Now:yyyy-MM-dd-HH-mm-ss-fff}-p{Environment.ProcessId}.txt");
            var builder = new StringBuilder();

            builder.AppendLine("PRIMO AUTO ELETRICA - UI SMOKE TEST");
            builder.AppendLine($"GeneratedAt: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine($"TotalChecks: {result.TotalChecks}");
            builder.AppendLine($"PassedChecks: {result.PassedChecks}");
            builder.AppendLine($"FailedChecks: {result.FailedChecks}");
            builder.AppendLine();

            foreach (var check in result.Checks)
            {
                builder.AppendLine($"[{(check.Success ? "PASS" : "FAIL")}] {check.Name} ({check.DurationMs} ms)");
                builder.AppendLine($"Message: {check.Message}");
            }

            File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
            return path;
        }

        private static Funcionario CreateSyntheticAdministrator()
        {
            return CreateSyntheticUser("Administrador", "Smoke Test");
        }

        private static Funcionario CreateSyntheticUser(string perfilAcesso, string nome)
        {
            return new Funcionario
            {
                Id = -1,
                Nome = nome,
                Email = $"{perfilAcesso.ToLowerInvariant()}@local",
                PerfilAcesso = perfilAcesso,
                Funcao = perfilAcesso,
                Status = "Ativo",
                Observacoes = "Usuario sintetico usado em smoke test.",
                Ativo = true,
                DataCadastro = DateTime.Now
            };
        }

        private static Cliente CreateSampleCliente()
        {
            var clienteId = Guid.NewGuid();
            var veiculo = new Veiculo
            {
                Id = Guid.NewGuid(),
                ClienteId = clienteId,
                Marca = "Fiat",
                Modelo = "Uno",
                Ano = DateTime.Today.Year.ToString(),
                Cor = "Branco",
                Placa = "ABC1D23",
                Chassi = "9BD00000000000001",
                Renavam = "12345678901",
                Motor = "1.0 Fire",
                Combustivel = "Flex",
                Quilometragem = 125000,
                Observacoes = "Veiculo sintetico para smoke test."
            };

            return new Cliente
            {
                Id = clienteId,
                Nome = "Cliente Smoke Test",
                CPF = "12345678900",
                Telefone = "(11) 99999-0000",
                WhatsApp = "(11) 99999-0000",
                Email = "cliente-smoke@local",
                CEP = "01001000",
                Rua = "Rua de Teste",
                Numero = "100",
                Bairro = "Centro",
                Cidade = "Sao Paulo",
                Estado = "SP",
                Observacoes = "Cadastro sintetico usado na validacao automatizada.",
                ClienteVip = true,
                ConsentimentoLGPD = true,
                DataConsentimentoLGPD = DateTime.Now,
                OrigemConsentimentoLGPD = "Smoke test",
                AutorizaContatoWhatsApp = true,
                TotalGasto = 1250.50m,
                TotalServicos = 4,
                PontosFidelidade = 120,
                DataCadastro = DateTime.Today.AddMonths(-6),
                UltimaVisita = DateTime.Today.AddDays(-8),
                Veiculos = new List<Veiculo> { veiculo },
                HistoricoServicos = new List<HistoricoServico>
                {
                    new()
                    {
                        DataServico = DateTime.Today.AddDays(-15),
                        Descricao = "Revisao eletrica",
                        Observacoes = "Troca de fusivel e limpeza de conexoes",
                        TecnicoResponsavel = "Tecnico Smoke",
                        Valor = 280m
                    }
                }
            };
        }

        private static Fornecedor CreateSampleFornecedor()
        {
            return new Fornecedor
            {
                RazaoSocial = "Fornecedor Smoke LTDA",
                NomeFantasia = "Fornecedor Smoke",
                CNPJ = "12345678000199",
                InscricaoEstadual = "123456789",
                Telefone = "(11) 3333-0000",
                Celular = "(11) 98888-0000",
                Email = "fornecedor-smoke@local",
                Site = "https://example.local/fornecedor-smoke",
                CEP = "01001000",
                Rua = "Avenida Exemplo",
                Numero = "500",
                Bairro = "Centro",
                Cidade = "Sao Paulo",
                Estado = "SP",
                Categoria = "Pecas",
                FormaPagamento = "Boleto",
                PrazoPagamento = "28 dias",
                PrazoMedioPagamentoDias = 28,
                PedidoMinimo = 350m,
                Nota = 5,
                Observacoes = "Fornecedor sintetico usado em smoke test.",
                Ativo = true,
                DataCadastro = DateTime.Today.AddMonths(-10),
                UltimaCompra = DateTime.Today.AddDays(-20),
                TotalCompras = 8450.35m
            };
        }

        private static Produto CreateSampleProduto()
        {
            return new Produto
            {
                Codigo = "SMK-001",
                Nome = "Rele Automotivo Smoke",
                Descricao = "Produto sintetico para validacao automatizada.",
                Categoria = "Eletrica",
                Marca = "Teste",
                Modelo = "12V",
                QuantidadeEstoque = 15,
                QuantidadeMinima = 3,
                QuantidadeMaxima = 40,
                Localizacao = "A1",
                Prateleira = "P1",
                Gaveta = "G1",
                PrecoCompra = 12.50m,
                PrecoVenda = 24.90m,
                Fornecedor = "Fornecedor Smoke",
                TelefoneFornecedor = "(11) 3333-0000",
                Observacoes = "Item sintetico para smoke test.",
                DataCadastro = DateTime.Today.AddMonths(-2),
                DataUltimaAtualizacao = DateTime.Today.AddDays(-2),
                Ativo = true
            };
        }

        private static Orcamento CreateSampleOrcamento(Cliente cliente)
        {
            var veiculo = cliente.Veiculos.FirstOrDefault();
            return new Orcamento
            {
                Numero = "ORC-SMOKE-001",
                ClienteId = cliente.Id,
                VeiculoId = veiculo?.Id,
                Cliente = cliente,
                Veiculo = veiculo,
                Status = "Rascunho",
                DataCriacao = DateTime.Today.AddDays(-3),
                DataValidade = DateTime.Today.AddDays(7),
                Observacoes = "Orcamento sintetico usado em smoke test.",
                Diagnostico = "Diagnostico sintetico para validar conversao em OS.",
                CondicoesPagamento = "PIX ou cartao",
                PrazoEntrega = "Imediato",
                Subtotal = 240m,
                Desconto = 10m,
                DescontoTipo = "Valor",
                Total = 230m
            };
        }

        private Funcionario EnsureFuncionario(string email, string nome, string perfil)
        {
            var existente = App.Repositories.Funcionarios.ObterTodos(false)
                .FirstOrDefault(funcionario => string.Equals(funcionario.Email, email, StringComparison.OrdinalIgnoreCase));

            if (existente != null)
            {
                return existente;
            }

            var funcionario = new Funcionario
            {
                Nome = nome,
                Email = email,
                Senha = PasswordHasherService.HashPassword("Workflow@123"),
                Funcao = perfil,
                PerfilAcesso = perfil,
                Telefone = "(11) 98888-0000",
                DataAdmissao = DateTime.Today,
                Salario = 2500m,
                Status = "Ativo",
                Observacoes = "Funcionario sintetico persistido para smoke test.",
                DataCadastro = DateTime.Now,
                Ativo = true
            };

            funcionario.Id = App.Repositories.Funcionarios.Salvar(funcionario);
            return funcionario;
        }

        private UiSmokeFixture EnsureSmokeFixture(Funcionario administrator)
        {
            var fixture = new UiSmokeFixture
            {
                Administrator = administrator,
                Funcionario = EnsureFuncionario("smoke-vendedor@primoauto.com", "Smoke Vendedor", "Vendedor")
            };

            fixture.Fornecedor = CreatePersistedFornecedor();
            fixture.Cliente = CreatePersistedCliente();
            fixture.Veiculo = CreatePersistedVeiculo(fixture.Cliente.Id);
            fixture.Cliente.Veiculos.Add(fixture.Veiculo);
            fixture.Produto = CreatePersistedProduto(fixture.Fornecedor);
            fixture.Orcamento = CreatePersistedOrcamento(fixture.Cliente, fixture.Produto);
            fixture.OrdemServico = CreatePersistedOrdemServico(fixture.Cliente, fixture.Veiculo, fixture.Produto);
            fixture.Agendamento = CreatePersistedAgendamento(fixture.Cliente, fixture.Veiculo, fixture.Produto);
            EnsureFinanceiroSeed(fixture.Cliente, fixture.Fornecedor);
            fixture.Venda = CreateSyntheticVenda(fixture.Cliente, fixture.Produto, administrator.Nome);

            return fixture;
        }

        private static Fornecedor CreatePersistedFornecedor()
        {
            var token = CriarTokenNumericoSmoke();
            var displayToken = token[^14..];
            var fornecedor = new Fornecedor
            {
                RazaoSocial = $"Fornecedor Smoke {displayToken} LTDA",
                NomeFantasia = $"Fornecedor Smoke {displayToken}",
                CNPJ = GerarCnpjValido(token),
                InscricaoEstadual = token[^9..],
                Telefone = "(11) 3333-0000",
                Celular = "(11) 98888-0000",
                Email = $"fornecedor.{displayToken}@primoauto.com",
                Site = "https://smoke.primoauto.local",
                CEP = "01001000",
                Rua = "Rua Smoke Fornecedor",
                Numero = "100",
                Bairro = "Centro",
                Cidade = "Sao Paulo",
                Estado = "SP",
                Categoria = "Pecas",
                FormaPagamento = "Boleto",
                PrazoPagamento = "28 dias",
                PrazoMedioPagamentoDias = 28,
                PrazoMedioEntregaDias = 3,
                PedidoMinimo = 100m,
                Observacoes = "Fornecedor sintetico criado pelo smoke test.",
                DataCadastro = DateTime.Now,
                UltimaCompra = DateTime.Today.AddDays(-2),
                TotalCompras = 500m,
                Ativo = true
            };

            App.Repositories.Fornecedores.Inserir(fornecedor);
            return fornecedor;
        }

        private static Cliente CreatePersistedCliente()
        {
            var token = CriarTokenNumericoSmoke();
            var displayToken = token[^14..];
            var cliente = new Cliente
            {
                Nome = $"Cliente Smoke {displayToken}",
                CPF = GerarCpfValido(token),
                Telefone = "(11) 98888-0001",
                WhatsApp = "(11) 98888-0001",
                Email = $"cliente.{displayToken}@primoauto.com",
                CEP = "01001000",
                Rua = "Rua Smoke Cliente",
                Numero = "101",
                Bairro = "Centro",
                Cidade = "Sao Paulo",
                Estado = "SP",
                ClienteVip = true,
                TotalGasto = 420m,
                TotalServicos = 2,
                PontosFidelidade = 15,
                Observacoes = "Cliente sintetico criado pelo smoke test.",
                ConsentimentoLGPD = true,
                DataConsentimentoLGPD = DateTime.Now,
                OrigemConsentimentoLGPD = "Smoke test",
                AutorizaContatoWhatsApp = true,
                DataCadastro = DateTime.Now,
                UltimaVisita = DateTime.Today.AddDays(-3)
            };

            App.Repositories.Clientes.Inserir(cliente);
            return cliente;
        }

        private static Veiculo CreatePersistedVeiculo(Guid clienteId)
        {
            var token = CriarTokenNumericoSmoke();
            var displayToken = token[^12..];
            var veiculo = new Veiculo
            {
                ClienteId = clienteId,
                Marca = "Volkswagen",
                Modelo = "Gol",
                Ano = "2019",
                Cor = "Prata",
                Placa = GerarPlacaValida(token),
                Chassi = $"9BWZZZ377VT{displayToken.PadLeft(9, '0')[..9]}",
                Renavam = displayToken.PadLeft(11, '0')[..11],
                Motor = "1.6 MSI",
                Combustivel = "Flex",
                Quilometragem = 78000,
                Observacoes = "Veiculo sintetico criado pelo smoke test."
            };

            App.Repositories.Clientes.SalvarVeiculo(veiculo);
            return veiculo;
        }

        private static Produto CreatePersistedProduto(Fornecedor fornecedor)
        {
            var token = CriarTokenNumericoSmoke();
            var displayToken = token[^12..];
            var produto = new Produto
            {
                Codigo = $"SMK-{displayToken}",
                Nome = $"Produto Smoke {displayToken}",
                Descricao = "Produto sintetico para validacao automatizada da UI.",
                Categoria = "Eletrica",
                Marca = "Smoke",
                Modelo = "12V",
                Cor = "Preto",
                Material = "Cobre",
                Fornecedor = fornecedor.NomeFantasia,
                CNPJFornecedor = fornecedor.CNPJ,
                TelefoneFornecedor = fornecedor.Telefone,
                QuantidadeEstoque = 20,
                QuantidadeMinima = 2,
                QuantidadeMaxima = 40,
                Localizacao = "A1",
                Prateleira = "P1",
                Gaveta = "G1",
                PrecoCompra = 10m,
                PrecoVenda = 25m,
                MargemLucro = 60m,
                ValorTotalEstoque = 200m,
                SKU = $"SKU-{displayToken}",
                CodigoBarras = displayToken.PadLeft(12, '0')[..12],
                Observacoes = "Produto sintetico criado pelo smoke test.",
                DataCadastro = DateTime.Now,
                DataUltimaAtualizacao = DateTime.Now,
                Ativo = true
            };

            App.Repositories.Produtos.Inserir(produto);
            return produto;
        }

        private static Produto CreatePersistedProdutoEstoqueSmoke(
            string tipo,
            int quantidadeEstoque,
            int quantidadeMinima,
            int quantidadeMaxima,
            decimal precoCompra,
            bool semCodigoOperacional,
            DateTime? dataUltimaVenda,
            int totalVendas,
            int vendasUltimoMes)
        {
            var token = Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture);
            var produto = new Produto
            {
                Codigo = $"EST-{tipo.ToUpperInvariant()}-{token[..8]}",
                Nome = $"Produto Estoque {tipo} {token[..6]}",
                Descricao = $"Produto sintetico para validar o filtro {tipo}.",
                Categoria = "Estoque Smoke",
                Marca = "Smoke",
                Modelo = tipo,
                Fornecedor = "Fornecedor Smoke Estoque",
                QuantidadeEstoque = quantidadeEstoque,
                QuantidadeMinima = quantidadeMinima,
                QuantidadeMaxima = quantidadeMaxima,
                Localizacao = $"EST-{tipo}",
                Prateleira = "P1",
                Gaveta = "G1",
                PrecoCompra = precoCompra,
                PrecoVenda = precoCompra * 2m,
                MargemLucro = 50m,
                ValorTotalEstoque = quantidadeEstoque * precoCompra,
                UnidadeMedida = "UN",
                CodigoBarras = semCodigoOperacional ? string.Empty : token[..12],
                SKU = semCodigoOperacional ? string.Empty : $"SKU-{token[..12]}",
                NCMS = "85364100",
                CEST = "0100100",
                CFOP = "5102",
                Ativo = true,
                DataCadastro = dataUltimaVenda?.AddDays(-30) ?? DateTime.Now,
                DataUltimaVenda = dataUltimaVenda,
                DataUltimaAtualizacao = DateTime.Now,
                TotalVendas = totalVendas,
                TotalFaturado = totalVendas * precoCompra * 2m,
                VendasUltimoMes = vendasUltimoMes,
                VendasUltimoTrimestre = totalVendas,
                Observacoes = "Produto sintetico criado para validar filtros do estoque."
            };

            App.Repositories.Produtos.Inserir(produto);
            return produto;
        }

        private static Orcamento CreatePersistedOrcamento(Cliente cliente, Produto produto)
        {
            var service = new OrcamentoDatabaseService();
            var orcamentoId = Guid.NewGuid();
            var item = new OrcamentoItem
            {
                Id = Guid.NewGuid(),
                OrcamentoId = orcamentoId,
                ProdutoId = produto.Id,
                ProdutoNome = produto.Nome,
                ProdutoCodigo = produto.Codigo,
                ProdutoCategoria = produto.Categoria,
                ProdutoMarca = produto.Marca,
                ProdutoAplicacao = "Uso sintetico",
                Quantidade = 1,
                PrecoUnitario = produto.PrecoVenda,
                PrecoCusto = produto.PrecoCompra,
                Desconto = 0,
                Subtotal = produto.PrecoVenda,
                LucroEstimado = produto.PrecoVenda - produto.PrecoCompra,
                MargemLucro = produto.MargemLucro,
                EstoqueDisponivel = produto.QuantidadeEstoque,
                Observacoes = "Item sintetico do smoke test."
            };

            var veiculo = cliente.Veiculos.FirstOrDefault();
            var orcamento = new Orcamento
            {
                Id = orcamentoId,
                ClienteId = cliente.Id,
                VeiculoId = veiculo?.Id,
                Cliente = cliente,
                Veiculo = veiculo,
                Numero = service.GerarNumeroOrcamento(),
                Status = "Rascunho",
                DataCriacao = DateTime.Now,
                DataValidade = DateTime.Today.AddDays(7),
                Subtotal = item.Subtotal,
                Desconto = 0,
                DescontoTipo = "Valor",
                Acrescimo = 0,
                Total = item.Subtotal,
                MargemLucro = item.MargemLucro,
                LucroEstimado = item.LucroEstimado,
                Observacoes = "Orcamento sintetico do smoke test.",
                Diagnostico = "Diagnostico sintetico com teste de alternador e aterramento.",
                CondicoesPagamento = "PIX",
                PrazoEntrega = "Imediato",
                Itens = new List<OrcamentoItem> { item }
            };

            service.AdicionarOrcamento(orcamento);
            return orcamento;
        }

        private static OrdemServico CreatePersistedOrdemServico(Cliente cliente, Veiculo veiculo, Produto produto)
        {
            var ordem = new OrdemServico
            {
                Numero = App.Repositories.OrdensServico.GerarProximoNumero(),
                ClienteId = cliente.Id,
                VeiculoId = veiculo.Id,
                ClienteNomeSnapshot = cliente.Nome,
                TelefoneClienteSnapshot = cliente.Telefone,
                VeiculoDescricaoSnapshot = $"{veiculo.Marca} {veiculo.Modelo} {veiculo.Ano}",
                PlacaSnapshot = veiculo.Placa,
                Status = "Aberta",
                Prioridade = "Normal",
                Origem = "SmokeTest",
                ProblemaRelatado = "Falha eletrica sintetica.",
                ObservacoesInternas = "OS criada pelo smoke test.",
                ChecklistEntrada = "Checklist de entrada sintetico.",
                ChecklistEntrega = "Checklist de entrega sintetico.",
                ChecklistSaida = "Checklist de saida sintetico.",
                TermoAutorizacao = "Termo sintetico de autorizacao da OS smoke.",
                GarantiaObservacoes = "Garantia sintetica",
                GarantiaValidaAte = DateTime.Today.AddDays(30),
                DataAbertura = DateTime.Now,
                DataPrevisao = DateTime.Today.AddDays(1),
                Ativo = true,
                Itens = new List<OrdemServicoItem>
                {
                    new()
                    {
                        ProdutoId = produto.Id,
                        Tipo = "Peca",
                        Descricao = produto.Nome,
                        Quantidade = 1,
                        ValorUnitario = produto.PrecoVenda,
                        CustoUnitario = produto.PrecoCompra,
                        Observacoes = "Peca sintetica do smoke test."
                    },
                    new()
                    {
                        Tipo = "Servico",
                        Descricao = "Diagnostico eletrico",
                        Quantidade = 1,
                        ValorUnitario = 80m,
                        CustoUnitario = 0,
                        Observacoes = "Servico sintetico do smoke test."
                    }
                }
            };

            App.Repositories.OrdensServico.Inserir(ordem);
            return ordem;
        }

        private static Agendamento CreatePersistedAgendamento(
            Cliente cliente,
            Veiculo veiculo,
            Produto produto,
            DateTime? dataAgendamento = null,
            string status = "Confirmado",
            string prioridade = "Normal",
            string? numeroPrefixo = null)
        {
            var dataBase = (dataAgendamento ?? DateTime.Today).Date;
            var agendamento = new Agendamento
            {
                Id = Guid.NewGuid(),
                Numero = $"{(string.IsNullOrWhiteSpace(numeroPrefixo) ? "AG-SMK" : numeroPrefixo)}-{DateTime.Now:HHmmssfff}-{Guid.NewGuid().ToString("N")[..4]}",
                DataCriacao = DateTime.Now,
                DataAgendamento = dataBase,
                HoraInicio = dataBase.AddHours(10),
                HoraTermino = dataBase.AddHours(11),
                DuracaoEstimada = TimeSpan.FromHours(1),
                Status = string.IsNullOrWhiteSpace(status) ? "Confirmado" : status,
                Prioridade = string.IsNullOrWhiteSpace(prioridade) ? "Normal" : prioridade,
                TipoServico = "Diagnostico",
                CategoriaServico = "Oficina",
                DescricaoServico = "Agendamento sintetico do smoke test.",
                Observacoes = "Agendamento sintetico criado pelo smoke test.",
                ClienteId = cliente.Id,
                ClienteNome = cliente.Nome,
                ClienteTelefone = cliente.Telefone,
                ClienteEmail = cliente.Email,
                ClienteDocumento = cliente.CPF,
                ClienteVip = cliente.ClienteVip,
                ClienteTotalGasto = cliente.TotalGasto,
                ClienteAtendimentos = cliente.TotalServicos,
                ClienteUltimaVisita = cliente.UltimaVisita,
                VeiculoId = veiculo.Id,
                VeiculoPlaca = veiculo.Placa,
                VeiculoModelo = veiculo.Modelo,
                VeiculoMarca = veiculo.Marca,
                VeiculoAno = veiculo.Ano,
                VeiculoCor = veiculo.Cor,
                VeiculoCombustivel = veiculo.Combustivel,
                VeiculoQuilometragem = veiculo.Quilometragem,
                TecnicoId = Guid.Empty,
                TecnicoNome = "Smoke Tecnico",
                TecnicoEspecialidade = "Eletrica",
                TecnicoAtivo = true,
                ValorEstimado = produto.PrecoVenda + 90m,
                ValorReal = produto.PrecoVenda + 90m,
                ValorProdutos = produto.PrecoVenda,
                ValorServicos = 90m,
                FormaPagamento = "A definir",
                Produtos = new List<AgendamentoProduto>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ProdutoId = produto.Id,
                        ProdutoNome = produto.Nome,
                        ProdutoCodigo = produto.Codigo,
                        Quantidade = 1,
                        PrecoUnitario = produto.PrecoVenda,
                        PrecoTotal = produto.PrecoVenda,
                        Reservado = false
                    }
                },
                Servicos = new List<AgendamentoServico>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Nome = "Diagnostico eletrico",
                        Categoria = "Oficina",
                        Valor = 90m,
                        TempoEstimado = TimeSpan.FromMinutes(45),
                        Status = "Pendente",
                        Observacoes = "Servico sintetico do smoke test."
                    }
                },
                Timeline = new List<AgendamentoTimeline>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        DataHora = DateTime.Now,
                        Usuario = "Smoke",
                        Acao = "Criacao",
                        Detalhes = "Agendamento sintetico do smoke test.",
                        TipoAlteracao = "Cadastro"
                    }
                }
            };

            new AgendamentoDatabaseService().AdicionarAgendamento(agendamento);
            return agendamento;
        }

        private static void EnsureFinanceiroSeed(Cliente cliente, Fornecedor fornecedor)
        {
            var financeiro = new FinanceiroDatabaseService();
            financeiro.AdicionarContaPagar(
                fornecedor.NomeFantasia,
                "Conta sintetica do smoke test",
                120m,
                DateTime.Today.AddDays(7),
                "Estoque",
                "Seed automatizado do smoke test.");

            financeiro.AdicionarContaReceber(
                cliente.Nome,
                "Recebimento sintetico do smoke test",
                180m,
                DateTime.Today.AddDays(5),
                "PIX",
                "Seed automatizado do smoke test.",
                origem: "SmokeTest",
                referenciaExterna: Guid.NewGuid().ToString("N"));
        }

        private static Venda CreateSyntheticVenda(Cliente cliente, Produto produto, string usuario)
        {
            return new Venda
            {
                Id = Guid.NewGuid(),
                Data = DateTime.Now.AddMinutes(-15),
                Cliente = cliente,
                FormaPagamento = "PIX",
                Status = "Concluida",
                Usuario = usuario,
                Desconto = 0,
                Total = produto.PrecoVenda,
                Itens = new List<ItemVenda>
                {
                    new()
                    {
                        Produto = produto,
                        Quantidade = 1,
                        PrecoUnitario = produto.PrecoVenda,
                        Desconto = 0
                    }
                }
            };
        }

        private static string GerarCpfValido(string seed)
        {
            var baseDigits = new string(seed.Where(char.IsDigit).ToArray());
            baseDigits = baseDigits.Length >= 9
                ? baseDigits[^9..]
                : baseDigits.PadLeft(9, '0');

            var digito1 = CalcularDigitoCpf(baseDigits);
            var digito2 = CalcularDigitoCpf(baseDigits + digito1);
            return $"{baseDigits}{digito1}{digito2}";
        }

        private static string CriarTokenNumericoSmoke()
        {
            var guidDigits = new string(Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture).Where(char.IsDigit).ToArray());
            if (guidDigits.Length < 8)
            {
                guidDigits = guidDigits.PadRight(8, '0');
            }

            return $"{DateTime.UtcNow:yyyyMMddHHmmssfff}{Environment.ProcessId}{guidDigits[..8]}";
        }

        private static int CalcularDigitoCpf(string baseDigits)
        {
            var pesoInicial = baseDigits.Length + 1;
            var soma = 0;

            for (var indice = 0; indice < baseDigits.Length; indice++)
            {
                soma += (baseDigits[indice] - '0') * (pesoInicial - indice);
            }

            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }

        private static string GerarCnpjValido(string seed)
        {
            var baseDigits = new string(seed.Where(char.IsDigit).ToArray());
            baseDigits = baseDigits.Length >= 12
                ? baseDigits[^12..]
                : baseDigits.PadLeft(12, '0');

            var digito1 = CalcularDigitoCnpj(baseDigits);
            var digito2 = CalcularDigitoCnpj(baseDigits + digito1);
            return $"{baseDigits}{digito1}{digito2}";
        }

        private static int CalcularDigitoCnpj(string baseDigits)
        {
            var pesos = baseDigits.Length == 12
                ? new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 }
                : new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
            var soma = 0;

            for (var indice = 0; indice < baseDigits.Length; indice++)
            {
                soma += (baseDigits[indice] - '0') * pesos[indice];
            }

            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }

        private static string GerarPlacaValida(string seed)
        {
            var source = (seed + Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture)).ToUpperInvariant();
            var digits = new string(source.Where(char.IsDigit).ToArray()).PadLeft(3, '0');
            const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            char LetterAt(int index)
            {
                var value = source[index % source.Length];
                return letters[value % letters.Length];
            }

            return $"{LetterAt(0)}{LetterAt(3)}{LetterAt(6)}{digits[^3]}{LetterAt(9)}{digits[^2..]}";
        }

    }
}
