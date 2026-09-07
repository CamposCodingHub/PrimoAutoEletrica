using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace PrimoAutoEletrica.Tests
{
    public class VeiculoOrcamentoWorkflowTests : IDisposable
    {
        private readonly string _testDbPath;
        private readonly DatabaseService _databaseService;
        private readonly LoggerService _loggerService;
        private readonly RepositoryRegistry _repositories;

        public VeiculoOrcamentoWorkflowTests()
        {
            // Configurar banco de dados isolado para teste
            var testDir = Path.Combine(Path.GetTempPath(), $"PrimoAutoTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(testDir);
            _testDbPath = Path.Combine(testDir, "test.db");

            _loggerService = new LoggerService(testDir);
            _databaseService = new DatabaseService(testDir, logger: _loggerService);
            _repositories = new RepositoryRegistry(_databaseService, _loggerService);
        }

        public void Dispose()
        {
            // Remover banco de teste
            if (File.Exists(_testDbPath))
            {
                File.Delete(_testDbPath);
            }

            // Remover diretório de teste
            var testDir = Path.GetDirectoryName(_testDbPath);
            if (Directory.Exists(testDir))
            {
                try
                {
                    Directory.Delete(testDir, recursive: true);
                }
                catch
                {
                    // Ignorar erros na limpeza
                }
            }
        }

        /*
        [Fact]
        public void Test_CadastroCliente_Veiculo_Orcamento_Completo()
        {
            // 1. CADASTRAR CLIENTE
            var cliente = new Cliente
            {
                Id = Guid.NewGuid(),
                Nome = "Cliente Teste Workflow",
                TipoPessoa = "Física",
                CPF = "52998224725", // CPF válido para teste
                Telefone = "11987654321",
                Email = "cliente@teste.com",
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
                OrigemConsentimentoLGPD = "Teste Automatizado"
            };

            // Inserir cliente
            _repositories.Clientes.Inserir(cliente);

            // Verificar se cliente foi salvo
            var clienteSalvo = _repositories.Clientes.ObterPorId(cliente.Id);
            Assert.NotNull(clienteSalvo);
            Assert.Equal(cliente.Nome, clienteSalvo.Nome);
            Assert.Equal(cliente.CPF, clienteSalvo.CPF);

            // 2. CADASTRAR VEÍCULO VINCULADO AO CLIENTE
            var veiculo = new Veiculo
            {
                Id = Guid.NewGuid(),
                ClienteId = cliente.Id, // CLIENTE OBRIGATÓRIO
                Marca = "Toyota",
                Modelo = "Corolla",
                Ano = "2024",
                Cor = "Prata",
                Placa = "ABC1234",
                Chassi = "9BD12345678901234",
                Renavam = "12345678901",
                TipoVeiculo = "Carro",
                SistemaEletrico = "12V",
                Motor = "2.0 Flex",
                Combustivel = "Flex",
                Quilometragem = 15000,
                Observacoes = "Veículo de teste para workflow"
            };

            // Salvar veículo (deve funcionar com cliente vinculado)
            _repositories.Clientes.SalvarVeiculo(veiculo);

            // Verificar se veículo foi salvo
            var veiculoSalvo = _repositories.Clientes.ObterTodosVeiculos()
                .FirstOrDefault(v => v.Id == veiculo.Id);
            Assert.NotNull(veiculoSalvo);
            Assert.Equal(veiculo.Marca, veiculoSalvo.Marca);
            Assert.Equal(veiculo.Modelo, veiculoSalvo.Modelo);
            Assert.Equal(veiculo.Placa, veiculoSalvo.Placa);
            Assert.Equal(cliente.Id, veiculoSalvo.ClienteId); // Verificar vinculação

            // 3. TESTAR VALIDAÇÃO DE CLIENTE OBRIGATÓRIO
            var veiculoSemCliente = new Veiculo
            {
                Id = Guid.NewGuid(),
                ClienteId = null, // SEM CLIENTE - DEVE FALHAR
                Marca = "Honda",
                Modelo = "Civic",
                Ano = "2024",
                Cor = "Branco",
                Placa = "DEF5678",
                Quilometragem = 20000
            };

            // Tentar salvar veículo sem cliente deve lançar exceção do banco (NOT NULL constraint)
            Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() =>
            {
                _repositories.Clientes.SalvarVeiculo(veiculoSemCliente);
            });

            // 4. VERIFICAR SE VEÍCULO ESTÁ DISPONÍVEL PARA ORÇAMENTO
            var veiculosDoCliente = _repositories.Clientes.ObterVeiculosPorClienteId(cliente.Id);
            Assert.Single(veiculosDoCliente);
            Assert.Equal(veiculo.Id, veiculosDoCliente.First().Id);

            // 5. TESTAR VALIDAÇÃO DE PLACA
            // Placa válida no formato antigo
            Assert.True(CadastroValidationHelper.EhPlacaValida("ABC1234"));
            
            // Placa válida no formato Mercosul
            Assert.True(CadastroValidationHelper.EhPlacaValida("ABC1D23"));
            
            // Placa válida no novo formato (2 letras no final)
            Assert.True(CadastroValidationHelper.EhPlacaValida("ABC12CD"));
            
            // Placa inválida
            Assert.False(CadastroValidationHelper.EhPlacaValida("INVALIDA"));
        }
        */

        [Fact]
        public void Test_ValidacaoPlaca_NovosPadroes()
        {
            // Testar os novos padrões de placa adicionados
            Assert.True(CadastroValidationHelper.EhPlacaValida("ABC1234"), "Placa antiga deve ser válida");
            Assert.True(CadastroValidationHelper.EhPlacaValida("ABC1D23"), "Placa Mercosul deve ser válida");
            Assert.True(CadastroValidationHelper.EhPlacaValida("ABC12CD"), "Placa novo padrão deve ser válida");
            Assert.False(CadastroValidationHelper.EhPlacaValida("AB1234"), "Placa inválida deve ser rejeitada");
            Assert.False(CadastroValidationHelper.EhPlacaValida("ABCD123"), "Placa inválida deve ser rejeitada");
        }

        /*
        [Fact]
        public void Test_Veiculo_ComCliente_Validacao()
        {
            // Criar cliente
            var cliente = new Cliente
            {
                Id = Guid.NewGuid(),
                Nome = "Cliente Validação",
                CPF = "52998224725",
                Telefone = "11912345678",
                Ativo = true,
                DataCadastro = DateTime.Now
            };
            _repositories.Clientes.Inserir(cliente);

            // Criar veículo COM cliente (deve funcionar)
            var veiculoComCliente = new Veiculo
            {
                Id = Guid.NewGuid(),
                ClienteId = cliente.Id,
                Marca = "Ford",
                Modelo = "Fiesta",
                Ano = "2023",
                Placa = "GHI9012",
                Quilometragem = 30000
            };

            var exception = Record.Exception(() => _repositories.Clientes.SalvarVeiculo(veiculoComCliente));
            Assert.Null(exception);

            // Verificar se foi salvo
            var salvo = _repositories.Clientes.ObterTodosVeiculos().FirstOrDefault(v => v.Id == veiculoComCliente.Id);
            Assert.NotNull(salvo);
            Assert.Equal(cliente.Id, salvo.ClienteId);
        }

        [Fact]
        public void Test_Veiculo_SemCliente_Rejeitado()
        {
            // Tentar criar veículo SEM cliente (deve falhar)
            var veiculoSemCliente = new Veiculo
            {
                Id = Guid.NewGuid(),
                ClienteId = null, // Campo obrigatório no banco
                Marca = "Chevrolet",
                Modelo = "Onix",
                Ano = "2024",
                Placa = "JKL3456",
                Quilometragem = 5000
            };

            // O banco de dados retorna SqliteException quando a constraint NOT NULL é violada
            Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(() =>
            {
                _repositories.Clientes.SalvarVeiculo(veiculoSemCliente);
            });
        }
        */
    }
}