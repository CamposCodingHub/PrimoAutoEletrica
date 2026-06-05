using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimoAutoEletrica.Repositories
{
    public sealed class ClienteRepository : IClienteRepository
    {
        private readonly Func<SqliteConnection> _connectionFactory;
        private readonly Func<Guid, List<HistoricoServico>> _historicoLoader;
        private readonly LoggerService _logger;

        private const string ClienteColumns = @"
            Id,
            Nome,
            CPF,
            RG,
            DataNascimento,
            Telefone,
            WhatsApp,
            Email,
            CEP,
            Rua,
            Numero,
            Bairro,
            Cidade,
            Estado,
            Ativo,
            ClienteVip,
            TotalGasto,
            TotalServicos,
            PontosFidelidade,
            ConsentimentoLGPD,
            DataConsentimentoLGPD,
            OrigemConsentimentoLGPD,
            AutorizaContatoWhatsApp,
            Observacoes,
            CaminhoDocumento,
            CaminhoAssinatura,
            DataCadastro,
            UltimaVisita,
            ImagemUrl";

        private const string VeiculoColumns = @"
            Id,
            ClienteId,
            Marca,
            Modelo,
            Ano,
            Cor,
            Placa,
            Chassi,
            Renavam,
            ImagemUrl,
            DocumentoImagemUrl,
            TipoVeiculo,
            SistemaEletrico,
            Motor,
            Combustivel,
            BateriaPrincipal,
            BateriaAuxiliar,
            Alternador,
            MotorPartida,
            Quilometragem,
            HistoricoTecnico,
            ObservacoesEletricasRecorrentes,
            ProblemaRecorrente,
            ObservacaoImportanteTecnico,
            RetornoRecomendadoEm,
            GarantiaValidaAte,
            ProximaRevisaoEm,
            Observacoes";

        public ClienteRepository(
            Func<SqliteConnection> connectionFactory,
            Func<Guid, List<HistoricoServico>> historicoLoader,
            LoggerService logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _historicoLoader = historicoLoader ?? throw new ArgumentNullException(nameof(historicoLoader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<Cliente> ObterTodos()
        {
            var clientes = new List<Cliente>();

            try
            {
                using var connection = _connectionFactory();
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = $@"
                    SELECT {ClienteColumns}
                    FROM Clientes
                    ORDER BY DataCadastro DESC;";

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    try
                    {
                        clientes.Add(MaterializarCliente(reader));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Erro ao materializar cliente na listagem.", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao obter clientes no repositorio.", ex);
            }

            CarregarRelacionamentos(clientes);
            return clientes;
        }

        public Cliente? ObterPorId(Guid id)
        {
            using var connection = _connectionFactory();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT {ClienteColumns}
                FROM Clientes
                WHERE Id = @Id
                LIMIT 1;";
            command.Parameters.AddWithValue("@Id", id.ToString());

            using var reader = command.ExecuteReader();
            if (!reader.Read())
                return null;

            var cliente = MaterializarCliente(reader);
            cliente.Veiculos = ObterVeiculosPorClienteId(cliente.Id);
            cliente.HistoricoServicos = CarregarHistoricoSeguro(cliente.Id);
            return cliente;
        }

        public void Inserir(Cliente cliente)
        {
            if (cliente == null)
            {
                throw new ArgumentNullException(nameof(cliente));
            }

            cliente.Id = cliente.Id == Guid.Empty ? Guid.NewGuid() : cliente.Id;
            cliente.DataCadastro = cliente.DataCadastro == default ? DateTime.Now : cliente.DataCadastro;

            using var connection = _connectionFactory();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                PrepararEValidarCliente(cliente, connection, transaction);

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = $@"
                    INSERT INTO Clientes
                    (
                        {ClienteColumns}
                    )
                    VALUES
                    (
                        @Id,
                        @Nome,
                        @CPF,
                        @RG,
                        @DataNascimento,
                        @Telefone,
                        @WhatsApp,
                        @Email,
                        @CEP,
                        @Rua,
                        @Numero,
                        @Bairro,
                        @Cidade,
                        @Estado,
                        @Ativo,
                        @ClienteVip,
                        @TotalGasto,
                        @TotalServicos,
                        @PontosFidelidade,
                        @ConsentimentoLGPD,
                        @DataConsentimentoLGPD,
                        @OrigemConsentimentoLGPD,
                        @AutorizaContatoWhatsApp,
                        @Observacoes,
                        @CaminhoDocumento,
                        @CaminhoAssinatura,
                        @DataCadastro,
                        @UltimaVisita,
                        @ImagemUrl
                    );";

                AddClienteParameters(command, cliente);
                command.ExecuteNonQuery();

                SalvarVeiculosDoCliente(cliente.Id, cliente.Veiculos, connection, transaction);
                AtualizarResumoCliente(connection, transaction, cliente.Id);
                transaction.Commit();

                RegistrarAuditoria("ClienteCriado", cliente, null, CriarSnapshot(cliente));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError($"Falha ao inserir cliente '{cliente.Nome}'.", ex);
                throw;
            }
        }

        public void Atualizar(Cliente cliente)
        {
            if (cliente == null)
            {
                throw new ArgumentNullException(nameof(cliente));
            }

            var anterior = ObterPorId(cliente.Id);

            using var connection = _connectionFactory();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                PrepararEValidarCliente(cliente, connection, transaction);

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    UPDATE Clientes
                    SET
                        Nome = @Nome,
                        CPF = @CPF,
                        RG = @RG,
                        DataNascimento = @DataNascimento,
                        Telefone = @Telefone,
                        WhatsApp = @WhatsApp,
                        Email = @Email,
                        CEP = @CEP,
                        Rua = @Rua,
                        Numero = @Numero,
                        Bairro = @Bairro,
                        Cidade = @Cidade,
                        Estado = @Estado,
                        Ativo = @Ativo,
                        ClienteVip = @ClienteVip,
                        TotalGasto = @TotalGasto,
                        TotalServicos = @TotalServicos,
                        PontosFidelidade = @PontosFidelidade,
                        ConsentimentoLGPD = @ConsentimentoLGPD,
                        DataConsentimentoLGPD = @DataConsentimentoLGPD,
                        OrigemConsentimentoLGPD = @OrigemConsentimentoLGPD,
                        AutorizaContatoWhatsApp = @AutorizaContatoWhatsApp,
                        Observacoes = @Observacoes,
                        CaminhoDocumento = @CaminhoDocumento,
                        CaminhoAssinatura = @CaminhoAssinatura,
                        UltimaVisita = @UltimaVisita,
                        ImagemUrl = @ImagemUrl,
                        RowVersion = COALESCE(RowVersion, 0) + 1,
                        DataUltimaAlteracao = @DataUltimaAlteracao
                    WHERE Id = @Id;";

                AddClienteParameters(command, cliente);
                command.Parameters.AddWithValue("@DataUltimaAlteracao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.ExecuteNonQuery();

                SalvarVeiculosDoCliente(cliente.Id, cliente.Veiculos, connection, transaction);
                AtualizarResumoCliente(connection, transaction, cliente.Id);
                transaction.Commit();

                RegistrarAuditoria("ClienteAtualizado", cliente, CriarSnapshot(anterior), CriarSnapshot(cliente));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError($"Falha ao atualizar cliente '{cliente.Nome}'.", ex);
                throw;
            }
        }

        public void Excluir(Guid id)
        {
            var anterior = ObterPorId(id);

            using var connection = _connectionFactory();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                if (ClientePossuiOrdensServico(connection, transaction, id))
                    throw new InvalidOperationException("O cliente possui ordens de servico cadastradas. Inative o cadastro em vez de excluir.");

                ExcluirVeiculosDoCliente(id, connection, transaction);

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM Clientes WHERE Id = @Id;";
                command.Parameters.AddWithValue("@Id", id.ToString());
                command.ExecuteNonQuery();

                transaction.Commit();
                RegistrarAuditoria("ClienteExcluido", anterior, CriarSnapshot(anterior), null, id);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError($"Falha ao excluir cliente '{id}'.", ex);
                throw;
            }
        }

        public List<Veiculo> ObterTodosVeiculos()
        {
            var veiculos = new List<Veiculo>();

            using var connection = _connectionFactory();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT {VeiculoColumns}
                FROM Veiculos
                ORDER BY Marca, Modelo, Placa;";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                veiculos.Add(MaterializarVeiculo(reader));
            }

            return veiculos;
        }

        public List<Veiculo> ObterVeiculosPorClienteId(Guid clienteId)
        {
            using var connection = _connectionFactory();
            connection.Open();
            return ObterVeiculosPorClienteId(clienteId, connection, null);
        }

        public void SalvarVeiculo(Veiculo veiculo)
        {
            if (veiculo == null)
            {
                throw new ArgumentNullException(nameof(veiculo));
            }

            veiculo.Id = veiculo.Id == Guid.Empty ? Guid.NewGuid() : veiculo.Id;

            using var connection = _connectionFactory();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                PrepararEValidarVeiculo(veiculo, connection, transaction);
                SalvarVeiculo(veiculo, connection, transaction, replace: true);
                transaction.Commit();
                RegistrarAuditoriaVeiculo("VeiculoSalvo", veiculo, CriarSnapshotVeiculo(veiculo));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError($"Falha ao salvar veiculo '{veiculo.Id}'.", ex);
                throw;
            }
        }

        public void ExcluirVeiculo(Guid veiculoId)
        {
            using var connection = _connectionFactory();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM Veiculos WHERE Id = @Id;";
                command.Parameters.AddWithValue("@Id", veiculoId.ToString());
                command.ExecuteNonQuery();
                transaction.Commit();

                RegistrarAuditoriaVeiculo("VeiculoExcluido", null, null, veiculoId);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError($"Falha ao excluir veiculo '{veiculoId}'.", ex);
                throw;
            }
        }

        public void SalvarVeiculosDoCliente(Guid clienteId, IEnumerable<Veiculo> veiculos)
        {
            using var connection = _connectionFactory();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            var listaVeiculos = (veiculos ?? Enumerable.Empty<Veiculo>()).ToList();

            try
            {
                foreach (var veiculo in listaVeiculos)
                {
                    veiculo.ClienteId = clienteId;
                    PrepararEValidarVeiculo(veiculo, connection, transaction);
                }

                ValidarPlacasDuplicadasNaColecao(listaVeiculos);
                SalvarVeiculosDoCliente(clienteId, listaVeiculos, connection, transaction);
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError($"Falha ao salvar veiculos do cliente '{clienteId}'.", ex);
                throw;
            }
        }

        private void CarregarRelacionamentos(List<Cliente> clientes)
        {
            var veiculosPorCliente = ObterTodosVeiculos()
                .Where(v => v.ClienteId.HasValue)
                .GroupBy(v => v.ClienteId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var cliente in clientes)
            {
                cliente.Veiculos = veiculosPorCliente.TryGetValue(cliente.Id, out var veiculos)
                    ? veiculos
                    : new List<Veiculo>();

                cliente.HistoricoServicos = CarregarHistoricoSeguro(cliente.Id);
            }
        }

        private void PrepararEValidarCliente(Cliente cliente, SqliteConnection connection, SqliteTransaction? transaction)
        {
            cliente.Nome = cliente.Nome?.Trim() ?? string.Empty;
            cliente.CPF = CadastroValidationHelper.NormalizarDocumento(cliente.CPF);
            cliente.RG = cliente.RG?.Trim() ?? string.Empty;
            cliente.Telefone = CadastroValidationHelper.NormalizarTelefone(cliente.Telefone);
            cliente.WhatsApp = CadastroValidationHelper.NormalizarTelefone(cliente.WhatsApp);
            cliente.Email = CadastroValidationHelper.NormalizarEmail(cliente.Email);
            cliente.CEP = cliente.CEP?.Trim() ?? string.Empty;
            cliente.Rua = cliente.Rua?.Trim() ?? string.Empty;
            cliente.Numero = cliente.Numero?.Trim() ?? string.Empty;
            cliente.Bairro = cliente.Bairro?.Trim() ?? string.Empty;
            cliente.Cidade = cliente.Cidade?.Trim() ?? string.Empty;
            cliente.Estado = cliente.Estado?.Trim() ?? string.Empty;
            cliente.Observacoes = cliente.Observacoes?.Trim() ?? string.Empty;
            cliente.OrigemConsentimentoLGPD = cliente.OrigemConsentimentoLGPD?.Trim() ?? string.Empty;
            if (!cliente.ConsentimentoLGPD)
            {
                cliente.DataConsentimentoLGPD = null;
                cliente.OrigemConsentimentoLGPD = string.Empty;
                cliente.AutorizaContatoWhatsApp = false;
            }
            else
            {
                cliente.DataConsentimentoLGPD ??= DateTime.Now;
                if (string.IsNullOrWhiteSpace(cliente.OrigemConsentimentoLGPD))
                {
                    cliente.OrigemConsentimentoLGPD = "Cadastro";
                }
            }
            cliente.PontosFidelidade = Math.Max(0, cliente.PontosFidelidade);
            cliente.Veiculos ??= new List<Veiculo>();

            if (string.IsNullOrWhiteSpace(cliente.Nome))
            {
                throw new InvalidOperationException("Informe o nome do cliente.");
            }

            var erroDocumento = CadastroValidationHelper.ValidarCpfOuCnpj(cliente.CPF, obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroDocumento))
            {
                throw new InvalidOperationException(erroDocumento);
            }

            var erroTelefone = CadastroValidationHelper.ValidarTelefone(cliente.Telefone, obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroTelefone))
            {
                throw new InvalidOperationException(erroTelefone);
            }

            var erroWhatsApp = CadastroValidationHelper.ValidarTelefone(cliente.WhatsApp, "WhatsApp", obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroWhatsApp))
            {
                throw new InvalidOperationException(erroWhatsApp);
            }

            var erroEmail = CadastroValidationHelper.ValidarEmail(cliente.Email, obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroEmail))
            {
                throw new InvalidOperationException(erroEmail);
            }

            ValidarDuplicidadeCliente(cliente, connection, transaction);
            ValidarPlacasDuplicadasNaColecao(cliente.Veiculos);

            foreach (var veiculo in cliente.Veiculos)
            {
                veiculo.ClienteId = cliente.Id;
                PrepararEValidarVeiculo(veiculo, connection, transaction);
            }
        }

        private static void ValidarPlacasDuplicadasNaColecao(IEnumerable<Veiculo> veiculos)
        {
            var placasDuplicadas = (veiculos ?? Enumerable.Empty<Veiculo>())
                .Where(v => !string.IsNullOrWhiteSpace(v.Placa))
                .Select(v => CadastroValidationHelper.NormalizarPlaca(v.Placa))
                .Where(placa => !string.IsNullOrWhiteSpace(placa))
                .GroupBy(placa => placa)
                .Where(grupo => grupo.Count() > 1)
                .Select(grupo => grupo.Key)
                .ToList();

            if (placasDuplicadas.Count > 0)
            {
                throw new InvalidOperationException($"Existem placas duplicadas no cadastro do cliente: {string.Join(", ", placasDuplicadas)}.");
            }
        }

        private void PrepararEValidarVeiculo(Veiculo veiculo, SqliteConnection connection, SqliteTransaction? transaction)
        {
            veiculo.Marca = veiculo.Marca?.Trim() ?? string.Empty;
            veiculo.Modelo = veiculo.Modelo?.Trim() ?? string.Empty;
            veiculo.Ano = veiculo.Ano?.Trim() ?? string.Empty;
            veiculo.Cor = veiculo.Cor?.Trim() ?? string.Empty;
            veiculo.Placa = CadastroValidationHelper.NormalizarPlaca(veiculo.Placa);
            veiculo.Chassi = veiculo.Chassi?.Trim().ToUpperInvariant() ?? string.Empty;
            veiculo.Renavam = veiculo.Renavam?.Trim() ?? string.Empty;
            veiculo.ImagemUrl = veiculo.ImagemUrl?.Trim() ?? string.Empty;
            veiculo.DocumentoImagemUrl = veiculo.DocumentoImagemUrl?.Trim() ?? string.Empty;
            veiculo.TipoVeiculo = VeiculoProfileService.InferirTipoVeiculo(veiculo.Marca, veiculo.TipoVeiculo);
            veiculo.SistemaEletrico = VeiculoProfileService.InferirSistemaEletrico(veiculo.TipoVeiculo, veiculo.SistemaEletrico);
            veiculo.Motor = veiculo.Motor?.Trim() ?? string.Empty;
            veiculo.Combustivel = veiculo.Combustivel?.Trim() ?? string.Empty;
            veiculo.BateriaPrincipal = veiculo.BateriaPrincipal?.Trim() ?? string.Empty;
            veiculo.BateriaAuxiliar = veiculo.BateriaAuxiliar?.Trim() ?? string.Empty;
            veiculo.Alternador = veiculo.Alternador?.Trim() ?? string.Empty;
            veiculo.MotorPartida = veiculo.MotorPartida?.Trim() ?? string.Empty;
            veiculo.HistoricoTecnico = veiculo.HistoricoTecnico?.Trim() ?? string.Empty;
            veiculo.ObservacoesEletricasRecorrentes = veiculo.ObservacoesEletricasRecorrentes?.Trim() ?? string.Empty;
            veiculo.ProblemaRecorrente = veiculo.ProblemaRecorrente?.Trim() ?? string.Empty;
            veiculo.ObservacaoImportanteTecnico = veiculo.ObservacaoImportanteTecnico?.Trim() ?? string.Empty;
            veiculo.Observacoes = veiculo.Observacoes?.Trim() ?? string.Empty;
            veiculo.Quilometragem = Math.Max(0, veiculo.Quilometragem);

            if (string.IsNullOrWhiteSpace(veiculo.Marca))
            {
                throw new InvalidOperationException("Informe a marca do veiculo.");
            }

            if (string.IsNullOrWhiteSpace(veiculo.Modelo))
            {
                throw new InvalidOperationException("Informe o modelo do veiculo.");
            }

            var erroPlaca = CadastroValidationHelper.ValidarPlaca(veiculo.Placa, obrigatorio: true);
            if (!string.IsNullOrWhiteSpace(erroPlaca))
            {
                throw new InvalidOperationException(erroPlaca);
            }

            var erroQuilometragem = CadastroValidationHelper.ValidarInteiro(veiculo.Quilometragem, "a quilometragem");
            if (!string.IsNullOrWhiteSpace(erroQuilometragem))
            {
                throw new InvalidOperationException(erroQuilometragem);
            }

            if (ExistePlacaEmOutroVeiculo(veiculo, connection, transaction))
            {
                throw new InvalidOperationException("Esta placa ja esta cadastrada em outro veiculo.");
            }
        }

        private void ValidarDuplicidadeCliente(Cliente cliente, SqliteConnection connection, SqliteTransaction? transaction)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT
                    Id,
                    Nome,
                    CPF,
                    Telefone,
                    WhatsApp,
                    Email
                FROM Clientes
                WHERE Ativo = 1
                  AND Id <> @Id;";
            command.Parameters.AddWithValue("@Id", cliente.Id.ToString());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var cpf = CadastroValidationHelper.NormalizarDocumento(ReadString(reader, 2));
                if (!string.IsNullOrWhiteSpace(cliente.CPF) &&
                    string.Equals(cpf, cliente.CPF, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Ja existe um cliente ativo com este documento.");
                }

                var nome = CadastroValidationHelper.NormalizarTextoComparacao(ReadString(reader, 1));
                var telefone = CadastroValidationHelper.NormalizarTelefone(ReadString(reader, 3));
                var whatsApp = CadastroValidationHelper.NormalizarTelefone(ReadString(reader, 4));
                var email = CadastroValidationHelper.NormalizarEmail(ReadString(reader, 5));

                var mesmoNome = string.Equals(nome, CadastroValidationHelper.NormalizarTextoComparacao(cliente.Nome), StringComparison.Ordinal);
                var contatoCoincide =
                    (!string.IsNullOrWhiteSpace(cliente.Telefone) && string.Equals(telefone, cliente.Telefone, StringComparison.Ordinal)) ||
                    (!string.IsNullOrWhiteSpace(cliente.WhatsApp) && string.Equals(whatsApp, cliente.WhatsApp, StringComparison.Ordinal)) ||
                    (!string.IsNullOrWhiteSpace(cliente.Email) && string.Equals(email, cliente.Email, StringComparison.Ordinal));

                if (mesmoNome && contatoCoincide)
                {
                    throw new InvalidOperationException("Ja existe um cliente ativo com o mesmo nome e contato principal.");
                }
            }
        }

        private static bool ExistePlacaEmOutroVeiculo(Veiculo veiculo, SqliteConnection connection, SqliteTransaction? transaction)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT Placa
                FROM Veiculos
                WHERE Id <> @Id;";
            command.Parameters.AddWithValue("@Id", veiculo.Id.ToString());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(CadastroValidationHelper.NormalizarPlaca(ReadString(reader, 0)), veiculo.Placa, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private List<HistoricoServico> CarregarHistoricoSeguro(Guid clienteId)
        {
            try
            {
                return _historicoLoader(clienteId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro ao carregar historico do cliente {clienteId}.", ex);
                return new List<HistoricoServico>();
            }
        }

        private static Cliente MaterializarCliente(SqliteDataReader reader)
        {
            return new Cliente
            {
                Id = ReadGuid(reader, 0),
                Nome = ReadString(reader, 1),
                CPF = ReadString(reader, 2),
                RG = ReadString(reader, 3),
                DataNascimento = ReadNullableDate(reader, 4),
                Telefone = ReadString(reader, 5),
                WhatsApp = ReadString(reader, 6),
                Email = ReadString(reader, 7),
                CEP = ReadString(reader, 8),
                Rua = ReadString(reader, 9),
                Numero = ReadString(reader, 10),
                Bairro = ReadString(reader, 11),
                Cidade = ReadString(reader, 12),
                Estado = ReadString(reader, 13),
                Ativo = ReadBool(reader, 14),
                ClienteVip = ReadBool(reader, 15),
                TotalGasto = ReadDecimal(reader, 16),
                TotalServicos = ReadInt(reader, 17),
                PontosFidelidade = ReadInt(reader, 18),
                ConsentimentoLGPD = ReadBool(reader, 19),
                DataConsentimentoLGPD = ReadNullableDate(reader, 20),
                OrigemConsentimentoLGPD = ReadString(reader, 21),
                AutorizaContatoWhatsApp = ReadBool(reader, 22),
                Observacoes = ReadString(reader, 23),
                CaminhoDocumento = ReadString(reader, 24),
                CaminhoAssinatura = ReadString(reader, 25),
                DataCadastro = ReadDate(reader, 26, DateTime.Now),
                UltimaVisita = ReadNullableDate(reader, 27),
                ImagemUrl = ReadString(reader, 28)
            };
        }

        private static Veiculo MaterializarVeiculo(SqliteDataReader reader)
        {
            return new Veiculo
            {
                Id = ReadGuid(reader, 0),
                ClienteId = ReadNullableGuid(reader, 1),
                Marca = ReadString(reader, 2),
                Modelo = ReadString(reader, 3),
                Ano = ReadString(reader, 4),
                Cor = ReadString(reader, 5),
                Placa = ReadString(reader, 6),
                Chassi = ReadString(reader, 7),
                Renavam = ReadString(reader, 8),
                ImagemUrl = ReadString(reader, 9),
                DocumentoImagemUrl = ReadString(reader, 10),
                TipoVeiculo = ReadString(reader, 11),
                SistemaEletrico = ReadString(reader, 12),
                Motor = ReadString(reader, 13),
                Combustivel = ReadString(reader, 14),
                BateriaPrincipal = ReadString(reader, 15),
                BateriaAuxiliar = ReadString(reader, 16),
                Alternador = ReadString(reader, 17),
                MotorPartida = ReadString(reader, 18),
                Quilometragem = ReadInt(reader, 19),
                HistoricoTecnico = ReadString(reader, 20),
                ObservacoesEletricasRecorrentes = ReadString(reader, 21),
                ProblemaRecorrente = ReadString(reader, 22),
                ObservacaoImportanteTecnico = ReadString(reader, 23),
                RetornoRecomendadoEm = ReadNullableDate(reader, 24),
                GarantiaValidaAte = ReadNullableDate(reader, 25),
                ProximaRevisaoEm = ReadNullableDate(reader, 26),
                Observacoes = ReadString(reader, 27)
            };
        }

        private static void AddClienteParameters(SqliteCommand command, Cliente cliente)
        {
            command.Parameters.AddWithValue("@Id", cliente.Id.ToString());
            command.Parameters.AddWithValue("@Nome", ToDbNullableString(cliente.Nome));
            command.Parameters.AddWithValue("@CPF", ToDbNullableString(cliente.CPF));
            command.Parameters.AddWithValue("@RG", ToDbNullableString(cliente.RG));
            command.Parameters.AddWithValue("@DataNascimento", ToDbNullableDate(cliente.DataNascimento, "yyyy-MM-dd"));
            command.Parameters.AddWithValue("@Telefone", ToDbNullableString(cliente.Telefone));
            command.Parameters.AddWithValue("@WhatsApp", ToDbNullableString(cliente.WhatsApp));
            command.Parameters.AddWithValue("@Email", ToDbNullableString(cliente.Email));
            command.Parameters.AddWithValue("@CEP", ToDbNullableString(cliente.CEP));
            command.Parameters.AddWithValue("@Rua", ToDbNullableString(cliente.Rua));
            command.Parameters.AddWithValue("@Numero", ToDbNullableString(cliente.Numero));
            command.Parameters.AddWithValue("@Bairro", ToDbNullableString(cliente.Bairro));
            command.Parameters.AddWithValue("@Cidade", ToDbNullableString(cliente.Cidade));
            command.Parameters.AddWithValue("@Estado", ToDbNullableString(cliente.Estado));
            command.Parameters.AddWithValue("@Ativo", cliente.Ativo ? 1 : 0);
            command.Parameters.AddWithValue("@ClienteVip", cliente.ClienteVip ? 1 : 0);
            command.Parameters.AddWithValue("@TotalGasto", cliente.TotalGasto);
            command.Parameters.AddWithValue("@TotalServicos", cliente.TotalServicos);
            command.Parameters.AddWithValue("@PontosFidelidade", cliente.PontosFidelidade);
            command.Parameters.AddWithValue("@ConsentimentoLGPD", cliente.ConsentimentoLGPD ? 1 : 0);
            command.Parameters.AddWithValue("@DataConsentimentoLGPD", ToDbNullableDate(cliente.DataConsentimentoLGPD, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@OrigemConsentimentoLGPD", ToDbNullableString(cliente.OrigemConsentimentoLGPD));
            command.Parameters.AddWithValue("@AutorizaContatoWhatsApp", cliente.AutorizaContatoWhatsApp ? 1 : 0);
            command.Parameters.AddWithValue("@Observacoes", ToDbNullableString(cliente.Observacoes));
            command.Parameters.AddWithValue("@CaminhoDocumento", ToDbNullableString(cliente.CaminhoDocumento));
            command.Parameters.AddWithValue("@CaminhoAssinatura", ToDbNullableString(cliente.CaminhoAssinatura));
            command.Parameters.AddWithValue("@DataCadastro", cliente.DataCadastro.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@UltimaVisita", ToDbNullableDate(cliente.UltimaVisita, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@ImagemUrl", ToDbNullableString(cliente.ImagemUrl));
        }

        private static void AddVeiculoParameters(SqliteCommand command, Veiculo veiculo)
        {
            command.Parameters.AddWithValue("@Id", veiculo.Id.ToString());
            command.Parameters.AddWithValue("@ClienteId", ToDbNullableString(veiculo.ClienteId?.ToString()));
            command.Parameters.AddWithValue("@Marca", ToDbNullableString(veiculo.Marca));
            command.Parameters.AddWithValue("@Modelo", ToDbNullableString(veiculo.Modelo));
            command.Parameters.AddWithValue("@Ano", ToDbNullableString(veiculo.Ano));
            command.Parameters.AddWithValue("@Cor", ToDbNullableString(veiculo.Cor));
            command.Parameters.AddWithValue("@Placa", ToDbNullableString(veiculo.Placa));
            command.Parameters.AddWithValue("@Chassi", ToDbNullableString(veiculo.Chassi));
            command.Parameters.AddWithValue("@Renavam", ToDbNullableString(veiculo.Renavam));
            command.Parameters.AddWithValue("@ImagemUrl", ToDbNullableString(veiculo.ImagemUrl));
            command.Parameters.AddWithValue("@DocumentoImagemUrl", ToDbNullableString(veiculo.DocumentoImagemUrl));
            command.Parameters.AddWithValue("@TipoVeiculo", ToDbNullableString(veiculo.TipoVeiculo));
            command.Parameters.AddWithValue("@SistemaEletrico", ToDbNullableString(veiculo.SistemaEletrico));
            command.Parameters.AddWithValue("@Motor", ToDbNullableString(veiculo.Motor));
            command.Parameters.AddWithValue("@Combustivel", ToDbNullableString(veiculo.Combustivel));
            command.Parameters.AddWithValue("@BateriaPrincipal", ToDbNullableString(veiculo.BateriaPrincipal));
            command.Parameters.AddWithValue("@BateriaAuxiliar", ToDbNullableString(veiculo.BateriaAuxiliar));
            command.Parameters.AddWithValue("@Alternador", ToDbNullableString(veiculo.Alternador));
            command.Parameters.AddWithValue("@MotorPartida", ToDbNullableString(veiculo.MotorPartida));
            command.Parameters.AddWithValue("@Quilometragem", Math.Max(0, veiculo.Quilometragem));
            command.Parameters.AddWithValue("@HistoricoTecnico", ToDbNullableString(veiculo.HistoricoTecnico));
            command.Parameters.AddWithValue("@ObservacoesEletricasRecorrentes", ToDbNullableString(veiculo.ObservacoesEletricasRecorrentes));
            command.Parameters.AddWithValue("@ProblemaRecorrente", ToDbNullableString(veiculo.ProblemaRecorrente));
            command.Parameters.AddWithValue("@ObservacaoImportanteTecnico", ToDbNullableString(veiculo.ObservacaoImportanteTecnico));
            command.Parameters.AddWithValue("@RetornoRecomendadoEm", ToDbNullableDate(veiculo.RetornoRecomendadoEm, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@GarantiaValidaAte", ToDbNullableDate(veiculo.GarantiaValidaAte, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@ProximaRevisaoEm", ToDbNullableDate(veiculo.ProximaRevisaoEm, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@Observacoes", ToDbNullableString(veiculo.Observacoes));
        }

        private List<Veiculo> ObterVeiculosPorClienteId(
            Guid clienteId,
            SqliteConnection connection,
            SqliteTransaction? transaction)
        {
            var veiculos = new List<Veiculo>();

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $@"
                SELECT {VeiculoColumns}
                FROM Veiculos
                WHERE ClienteId = @ClienteId
                ORDER BY Marca, Modelo, Placa;";
            command.Parameters.AddWithValue("@ClienteId", clienteId.ToString());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                veiculos.Add(MaterializarVeiculo(reader));
            }

            return veiculos;
        }

        private void SalvarVeiculosDoCliente(
            Guid clienteId,
            IEnumerable<Veiculo> veiculos,
            SqliteConnection connection,
            SqliteTransaction? transaction)
        {
            ExcluirVeiculosDoCliente(clienteId, connection, transaction);

            foreach (var veiculo in veiculos ?? Enumerable.Empty<Veiculo>())
            {
                if (veiculo == null)
                    continue;

                veiculo.Id = veiculo.Id == Guid.Empty ? Guid.NewGuid() : veiculo.Id;
                veiculo.ClienteId = clienteId;
                SalvarVeiculo(veiculo, connection, transaction, replace: false);
            }
        }

        private static void SalvarVeiculo(
            Veiculo veiculo,
            SqliteConnection connection,
            SqliteTransaction? transaction,
            bool replace)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $@"
                INSERT {(replace ? "OR REPLACE " : string.Empty)}INTO Veiculos
                (
                    {VeiculoColumns}
                )
                VALUES
                (
                    @Id,
                    @ClienteId,
                    @Marca,
                    @Modelo,
                    @Ano,
                    @Cor,
                    @Placa,
                    @Chassi,
                    @Renavam,
                    @ImagemUrl,
                    @DocumentoImagemUrl,
                    @TipoVeiculo,
                    @SistemaEletrico,
                    @Motor,
                    @Combustivel,
                    @BateriaPrincipal,
                    @BateriaAuxiliar,
                    @Alternador,
                    @MotorPartida,
                    @Quilometragem,
                    @HistoricoTecnico,
                    @ObservacoesEletricasRecorrentes,
                    @ProblemaRecorrente,
                    @ObservacaoImportanteTecnico,
                    @RetornoRecomendadoEm,
                    @GarantiaValidaAte,
                    @ProximaRevisaoEm,
                    @Observacoes
                );";

            AddVeiculoParameters(command, veiculo);
            command.ExecuteNonQuery();
        }

        private static void ExcluirVeiculosDoCliente(
            Guid clienteId,
            SqliteConnection connection,
            SqliteTransaction? transaction)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM Veiculos WHERE ClienteId = @ClienteId;";
            command.Parameters.AddWithValue("@ClienteId", clienteId.ToString());
            command.ExecuteNonQuery();
        }

        private static bool ClientePossuiOrdensServico(
            SqliteConnection connection,
            SqliteTransaction? transaction,
            Guid clienteId)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT COUNT(*) FROM OrdensServico WHERE ClienteId = @ClienteId;";
            command.Parameters.AddWithValue("@ClienteId", clienteId.ToString());
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        private static void AtualizarResumoCliente(
            SqliteConnection connection,
            SqliteTransaction? transaction,
            Guid clienteId)
        {
            decimal totalGasto = 0;
            var totalServicos = 0;
            DateTime? ultimaVisita = null;

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
                    SELECT
                        COALESCE(SUM(
                            COALESCE(
                                (
                                    SELECT SUM(Quantidade * ValorUnitario)
                                    FROM OrdemServicoItens itens
                                    WHERE itens.OrdemServicoId = os.Id
                                ),
                                0
                            ) - COALESCE(os.Desconto, 0)
                        ), 0) AS TotalGasto,
                        COUNT(*) AS TotalServicos,
                        MAX(COALESCE(os.DataEntrega, os.DataConclusao, os.DataAbertura)) AS UltimaVisita
                    FROM OrdensServico os
                    WHERE os.ClienteId = @ClienteId
                      AND os.Ativo = 1
                      AND (os.Status = 'Entregue' OR os.DataEntrega IS NOT NULL);";
                command.Parameters.AddWithValue("@ClienteId", clienteId.ToString());

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    totalGasto = ReadDecimal(reader, 0);
                    totalServicos = ReadInt(reader, 1);
                    ultimaVisita = ReadNullableDate(reader, 2);
                }
            }

            using var updateCommand = connection.CreateCommand();
            updateCommand.Transaction = transaction;
            updateCommand.CommandText = @"
                UPDATE Clientes
                SET
                    TotalGasto = @TotalGasto,
                    TotalServicos = @TotalServicos,
                    PontosFidelidade = @PontosFidelidade,
                    UltimaVisita = @UltimaVisita,
                    RowVersion = COALESCE(RowVersion, 0) + 1,
                    DataUltimaAlteracao = @DataUltimaAlteracao
                WHERE Id = @Id;";
            updateCommand.Parameters.AddWithValue("@TotalGasto", totalGasto);
            updateCommand.Parameters.AddWithValue("@TotalServicos", totalServicos);
            updateCommand.Parameters.AddWithValue("@PontosFidelidade", Math.Max(0, (int)Math.Floor(totalGasto / 10m)));
            updateCommand.Parameters.AddWithValue("@UltimaVisita", ToDbNullableDate(ultimaVisita, "yyyy-MM-dd HH:mm:ss"));
            updateCommand.Parameters.AddWithValue("@DataUltimaAlteracao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            updateCommand.Parameters.AddWithValue("@Id", clienteId.ToString());
            updateCommand.ExecuteNonQuery();
        }

        private static object ToDbNullableString(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
        }

        private static object ToDbNullableDate(DateTime? value, string format)
        {
            return value.HasValue ? value.Value.ToString(format) : DBNull.Value;
        }

        private static string ReadString(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? string.Empty : Convert.ToString(reader.GetValue(index)) ?? string.Empty;
        }

        private static int ReadInt(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0 : Convert.ToInt32(reader.GetValue(index));
        }

        private static bool ReadBool(SqliteDataReader reader, int index)
        {
            return !reader.IsDBNull(index) && Convert.ToInt32(reader.GetValue(index)) == 1;
        }

        private static decimal ReadDecimal(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0 : Convert.ToDecimal(reader.GetValue(index));
        }

        private static Guid ReadGuid(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) || !Guid.TryParse(Convert.ToString(reader.GetValue(index)), out var value)
                ? Guid.NewGuid()
                : value;
        }

        private static Guid? ReadNullableGuid(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) || !Guid.TryParse(Convert.ToString(reader.GetValue(index)), out var value)
                ? null
                : value;
        }

        private static DateTime ReadDate(SqliteDataReader reader, int index, DateTime fallback)
        {
            return reader.IsDBNull(index) || !DateTime.TryParse(Convert.ToString(reader.GetValue(index)), out var value)
                ? fallback
                : value;
        }

        private static DateTime? ReadNullableDate(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) || !DateTime.TryParse(Convert.ToString(reader.GetValue(index)), out var value)
                ? null
                : value;
        }

        private static string? CriarSnapshot(Cliente? cliente)
        {
            return cliente == null
                ? null
                : $"Nome={cliente.Nome}; Documento={cliente.Documento}; Telefone={cliente.Telefone}; Ativo={cliente.Ativo}; Vip={cliente.ClienteVip}; LGPD={cliente.ConsentimentoLGPD}; WhatsApp={cliente.AutorizaContatoWhatsApp}; Veiculos={cliente.Veiculos.Count}";
        }

        private static string? CriarSnapshotVeiculo(Veiculo? veiculo)
        {
            return veiculo == null
                ? null
                : $"Marca={veiculo.Marca}; Modelo={veiculo.Modelo}; Placa={veiculo.Placa}; ClienteId={veiculo.ClienteId}";
        }

        private static void RegistrarAuditoria(string acao, Cliente? cliente, string? anterior, string? novo, Guid? idOverride = null)
        {
            global::PrimoAutoEletrica.App.Audit.Registrar(
                categoria: "Clientes",
                acao: acao,
                entidade: "Cliente",
                entidadeId: (cliente?.Id ?? idOverride ?? Guid.Empty).ToString(),
                detalhes: cliente == null ? "Cliente nao localizado." : $"Nome={cliente.Nome}; Documento={cliente.Documento}",
                valorAnterior: anterior,
                valorNovo: novo);
        }

        private static void RegistrarAuditoriaVeiculo(string acao, Veiculo? veiculo, string? detalhes, Guid? idOverride = null)
        {
            global::PrimoAutoEletrica.App.Audit.Registrar(
                categoria: "Clientes",
                acao: acao,
                entidade: "Veiculo",
                entidadeId: (veiculo?.Id ?? idOverride ?? Guid.Empty).ToString(),
                detalhes: detalhes ?? "Veiculo nao localizado.");
        }
    }
}
