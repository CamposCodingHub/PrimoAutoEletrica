using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Repositories
{
    public sealed class FornecedorRepository : IFornecedorRepository
    {
        private readonly Func<SqliteConnection> _connectionFactory;
        private readonly LoggerService _logger;

        private const string FornecedorColumns = @"
            Id,
            RazaoSocial,
            NomeFantasia,
            CNPJ,
            InscricaoEstadual,
            Telefone,
            Celular,
            WhatsAppVendedor,
            Email,
            Site,
            CEP,
            Rua,
            Numero,
            Complemento,
            Bairro,
            Cidade,
            Estado,
            FormaPagamento,
            PrazoPagamento,
            PrazoMedioEntregaDias,
            PedidoMinimo,
            Categoria,
            CategoriaPreferencial,
            Ativo,
            Nota,
            Observacoes,
            DataCadastro,
            UltimaCompra,
            TotalCompras";

        public FornecedorRepository(Func<SqliteConnection> connectionFactory, LoggerService logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<Fornecedor> ObterTodos()
        {
            var fornecedores = new List<Fornecedor>();

            try
            {
                using var connection = _connectionFactory();
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"
                        SELECT {FornecedorColumns}
                        FROM Fornecedores
                        ORDER BY NomeFantasia;";

                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        try
                        {
                            fornecedores.Add(MaterializarFornecedor(reader));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("Erro ao materializar fornecedor na listagem.", ex);
                        }
                    }
                }

                CarregarContatos(connection, fornecedores);
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro ao obter fornecedores no repositorio.", ex);
            }

            return fornecedores;
        }

        public Fornecedor? ObterPorId(Guid id)
        {
            return ObterUnico("Id = @Valor", id.ToString());
        }

        public Fornecedor? ObterPorCnpj(string cnpj)
        {
            var cnpjNormalizado = CadastroValidationHelper.NormalizarDocumento(cnpj);
            if (string.IsNullOrWhiteSpace(cnpjNormalizado))
            {
                return null;
            }

            using var connection = _connectionFactory();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT {FornecedorColumns}
                FROM Fornecedores
                WHERE CNPJ IS NOT NULL;";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var fornecedor = MaterializarFornecedor(reader);
                if (!string.Equals(CadastroValidationHelper.NormalizarDocumento(fornecedor.CNPJ), cnpjNormalizado, StringComparison.Ordinal))
                {
                    continue;
                }

                fornecedor.Contatos = ObterContatos(connection, fornecedor.Id);
                return fornecedor;
            }

            return null;
        }

        public Fornecedor? ObterPorNomeFantasia(string nomeFantasia)
        {
            if (string.IsNullOrWhiteSpace(nomeFantasia))
            {
                return null;
            }

            return ObterUnico("NomeFantasia = @Valor", nomeFantasia.Trim());
        }

        public void Inserir(Fornecedor fornecedor)
        {
            if (fornecedor == null)
            {
                throw new ArgumentNullException(nameof(fornecedor));
            }

            fornecedor.Id = fornecedor.Id == Guid.Empty ? Guid.NewGuid() : fornecedor.Id;
            fornecedor.DataCadastro = fornecedor.DataCadastro == default ? DateTime.Now : fornecedor.DataCadastro;

            using var connection = _connectionFactory();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                PrepararEValidarFornecedor(fornecedor, connection, transaction);

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = $@"
                    INSERT INTO Fornecedores
                    (
                        {FornecedorColumns}
                    )
                    VALUES
                    (
                        @Id,
                        @RazaoSocial,
                        @NomeFantasia,
                        @CNPJ,
                        @InscricaoEstadual,
                        @Telefone,
                        @Celular,
                        @WhatsAppVendedor,
                        @Email,
                        @Site,
                        @CEP,
                        @Rua,
                        @Numero,
                        @Complemento,
                        @Bairro,
                        @Cidade,
                        @Estado,
                        @FormaPagamento,
                        @PrazoPagamento,
                        @PrazoMedioEntregaDias,
                        @PedidoMinimo,
                        @Categoria,
                        @CategoriaPreferencial,
                        @Ativo,
                        @Nota,
                        @Observacoes,
                        @DataCadastro,
                        @UltimaCompra,
                        @TotalCompras
                    );";

                AddFornecedorParameters(command, fornecedor);
                command.ExecuteNonQuery();

                SalvarContatos(connection, transaction, fornecedor);
                VincularProdutosRelacionados(connection, transaction, fornecedor);
                transaction.Commit();

                RegistrarAuditoria("FornecedorCriado", fornecedor, null, CriarSnapshot(fornecedor));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError($"Falha ao inserir fornecedor '{fornecedor.RazaoSocial}'.", ex);
                throw;
            }
        }

        public void Atualizar(Fornecedor fornecedor)
        {
            if (fornecedor == null)
            {
                throw new ArgumentNullException(nameof(fornecedor));
            }

            var anterior = ObterPorId(fornecedor.Id);

            using var connection = _connectionFactory();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                PrepararEValidarFornecedor(fornecedor, connection, transaction);

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    UPDATE Fornecedores
                    SET
                        RazaoSocial = @RazaoSocial,
                        NomeFantasia = @NomeFantasia,
                        CNPJ = @CNPJ,
                        InscricaoEstadual = @InscricaoEstadual,
                        Telefone = @Telefone,
                        Celular = @Celular,
                        WhatsAppVendedor = @WhatsAppVendedor,
                        Email = @Email,
                        Site = @Site,
                        CEP = @CEP,
                        Rua = @Rua,
                        Numero = @Numero,
                        Complemento = @Complemento,
                        Bairro = @Bairro,
                        Cidade = @Cidade,
                        Estado = @Estado,
                        FormaPagamento = @FormaPagamento,
                        PrazoPagamento = @PrazoPagamento,
                        PrazoMedioEntregaDias = @PrazoMedioEntregaDias,
                        PedidoMinimo = @PedidoMinimo,
                        Categoria = @Categoria,
                        CategoriaPreferencial = @CategoriaPreferencial,
                        Ativo = @Ativo,
                        Nota = @Nota,
                        Observacoes = @Observacoes,
                        UltimaCompra = @UltimaCompra,
                        TotalCompras = @TotalCompras,
                        RowVersion = COALESCE(RowVersion, 0) + 1,
                        DataUltimaAlteracao = @DataUltimaAlteracao
                    WHERE Id = @Id;";

                AddFornecedorParameters(command, fornecedor);
                command.Parameters.AddWithValue("@DataUltimaAlteracao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.ExecuteNonQuery();

                ExcluirContatos(connection, transaction, fornecedor.Id);
                SalvarContatos(connection, transaction, fornecedor);
                VincularProdutosRelacionados(connection, transaction, fornecedor);
                transaction.Commit();

                RegistrarAuditoria("FornecedorAtualizado", fornecedor, CriarSnapshot(anterior), CriarSnapshot(fornecedor));
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError($"Falha ao atualizar fornecedor '{fornecedor.RazaoSocial}'.", ex);
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
                ExcluirContatos(connection, transaction, id);
                ExcluirProdutoFornecedores(connection, transaction, id);
                LimparVinculosProdutos(connection, transaction, id);

                using var deleteCommand = connection.CreateCommand();
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = "DELETE FROM Fornecedores WHERE Id = @Id;";
                deleteCommand.Parameters.AddWithValue("@Id", id.ToString());
                deleteCommand.ExecuteNonQuery();

                transaction.Commit();

                RegistrarAuditoria("FornecedorExcluido", anterior, CriarSnapshot(anterior), null, id);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError($"Falha ao excluir fornecedor '{id}'.", ex);
                throw;
            }
        }

        private static void LimparVinculosProdutos(SqliteConnection connection, SqliteTransaction transaction, Guid fornecedorId)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                UPDATE Produtos
                SET
                    FornecedorId = NULL,
                    Fornecedor = NULL,
                    CNPJFornecedor = NULL,
                    ContatoFornecedor = NULL,
                    TelefoneFornecedor = NULL
                WHERE FornecedorId = @FornecedorId;";
            command.Parameters.AddWithValue("@FornecedorId", fornecedorId.ToString());
            command.ExecuteNonQuery();
        }

        private Fornecedor? ObterUnico(string whereClause, string value)
        {
            using var connection = _connectionFactory();
            connection.Open();

            Fornecedor? fornecedor = null;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $@"
                    SELECT {FornecedorColumns}
                    FROM Fornecedores
                    WHERE {whereClause}
                    LIMIT 1;";
                command.Parameters.AddWithValue("@Valor", value);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    fornecedor = MaterializarFornecedor(reader);
                }
            }

            if (fornecedor != null)
            {
                fornecedor.Contatos = ObterContatos(connection, fornecedor.Id);
            }

            return fornecedor;
        }

        private void PrepararEValidarFornecedor(Fornecedor fornecedor, SqliteConnection connection, SqliteTransaction? transaction)
        {
            fornecedor.RazaoSocial = fornecedor.RazaoSocial?.Trim() ?? string.Empty;
            fornecedor.NomeFantasia = fornecedor.NomeFantasia?.Trim() ?? string.Empty;
            fornecedor.CNPJ = CadastroValidationHelper.NormalizarDocumento(fornecedor.CNPJ);
            fornecedor.InscricaoEstadual = fornecedor.InscricaoEstadual?.Trim() ?? string.Empty;
            fornecedor.Telefone = CadastroValidationHelper.NormalizarTelefone(fornecedor.Telefone);
            fornecedor.Celular = CadastroValidationHelper.NormalizarTelefone(fornecedor.Celular);
            fornecedor.WhatsAppVendedor = CadastroValidationHelper.NormalizarTelefone(fornecedor.WhatsAppVendedor);
            fornecedor.Email = CadastroValidationHelper.NormalizarEmail(fornecedor.Email);
            fornecedor.Site = fornecedor.Site?.Trim() ?? string.Empty;
            fornecedor.CEP = fornecedor.CEP?.Trim() ?? string.Empty;
            fornecedor.Rua = fornecedor.Rua?.Trim() ?? string.Empty;
            fornecedor.Numero = fornecedor.Numero?.Trim() ?? string.Empty;
            fornecedor.Complemento = fornecedor.Complemento?.Trim() ?? string.Empty;
            fornecedor.Bairro = fornecedor.Bairro?.Trim() ?? string.Empty;
            fornecedor.Cidade = fornecedor.Cidade?.Trim() ?? string.Empty;
            fornecedor.Estado = fornecedor.Estado?.Trim() ?? string.Empty;
            fornecedor.FormaPagamento = fornecedor.FormaPagamento?.Trim() ?? string.Empty;
            fornecedor.PrazoPagamento = fornecedor.PrazoPagamento?.Trim() ?? string.Empty;
            fornecedor.PrazoMedioEntregaDias = Math.Max(0, fornecedor.PrazoMedioEntregaDias);
            fornecedor.Categoria = string.IsNullOrWhiteSpace(fornecedor.Categoria) ? "Pecas" : fornecedor.Categoria.Trim();
            fornecedor.CategoriaPreferencial = string.IsNullOrWhiteSpace(fornecedor.CategoriaPreferencial)
                ? fornecedor.Categoria
                : fornecedor.CategoriaPreferencial.Trim();
            fornecedor.Observacoes = fornecedor.Observacoes?.Trim() ?? string.Empty;
            fornecedor.PedidoMinimo = Math.Max(0, fornecedor.PedidoMinimo);

            if (string.IsNullOrWhiteSpace(fornecedor.RazaoSocial))
            {
                throw new InvalidOperationException("Informe a razao social do fornecedor.");
            }

            if (string.IsNullOrWhiteSpace(fornecedor.NomeFantasia))
            {
                throw new InvalidOperationException("Informe o nome fantasia do fornecedor.");
            }

            var erroCnpj = CadastroValidationHelper.ValidarCnpj(fornecedor.CNPJ, obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroCnpj))
            {
                throw new InvalidOperationException(erroCnpj);
            }

            var erroTelefone = CadastroValidationHelper.ValidarTelefone(fornecedor.Telefone, obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroTelefone))
            {
                throw new InvalidOperationException(erroTelefone);
            }

            var erroCelular = CadastroValidationHelper.ValidarTelefone(fornecedor.Celular, "celular", obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroCelular))
            {
                throw new InvalidOperationException(erroCelular);
            }

            var erroWhatsApp = CadastroValidationHelper.ValidarTelefone(fornecedor.WhatsAppVendedor, "celular", obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroWhatsApp))
            {
                throw new InvalidOperationException(erroWhatsApp);
            }

            var erroEmail = CadastroValidationHelper.ValidarEmail(fornecedor.Email, obrigatorio: false);
            if (!string.IsNullOrWhiteSpace(erroEmail))
            {
                throw new InvalidOperationException(erroEmail);
            }

            var erroPedidoMinimo = CadastroValidationHelper.ValidarDecimal(fornecedor.PedidoMinimo, "um pedido minimo");
            if (!string.IsNullOrWhiteSpace(erroPedidoMinimo))
            {
                throw new InvalidOperationException(erroPedidoMinimo);
            }

            ValidarDuplicidadeFornecedor(fornecedor, connection, transaction);
        }

        private void ValidarDuplicidadeFornecedor(Fornecedor fornecedor, SqliteConnection connection, SqliteTransaction? transaction)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT
                    Id,
                    NomeFantasia,
                    CNPJ,
                    Telefone,
                    Celular,
                    Email,
                    Ativo
                FROM Fornecedores
                WHERE Id <> @Id
                  AND Ativo = 1;";
            command.Parameters.AddWithValue("@Id", fornecedor.Id.ToString());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var cnpj = CadastroValidationHelper.NormalizarDocumento(ReadString(reader, 2));
                if (!string.IsNullOrWhiteSpace(fornecedor.CNPJ) &&
                    string.Equals(cnpj, fornecedor.CNPJ, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Ja existe um fornecedor ativo com este CNPJ.");
                }

                var nome = CadastroValidationHelper.NormalizarTextoComparacao(ReadString(reader, 1));
                var telefone = CadastroValidationHelper.NormalizarTelefone(ReadString(reader, 3));
                var celular = CadastroValidationHelper.NormalizarTelefone(ReadString(reader, 4));
                var email = CadastroValidationHelper.NormalizarEmail(ReadString(reader, 5));

                var mesmoNome = string.Equals(nome, CadastroValidationHelper.NormalizarTextoComparacao(fornecedor.NomeFantasia), StringComparison.Ordinal);
                var contatoCoincide =
                    (!string.IsNullOrWhiteSpace(fornecedor.Telefone) && string.Equals(telefone, fornecedor.Telefone, StringComparison.Ordinal)) ||
                    (!string.IsNullOrWhiteSpace(fornecedor.Celular) && string.Equals(celular, fornecedor.Celular, StringComparison.Ordinal)) ||
                    (!string.IsNullOrWhiteSpace(fornecedor.Email) && string.Equals(email, fornecedor.Email, StringComparison.Ordinal));

                if (mesmoNome && contatoCoincide)
                {
                    throw new InvalidOperationException("Ja existe um fornecedor ativo com o mesmo nome fantasia e contato principal.");
                }
            }
        }

        private static void AddFornecedorParameters(SqliteCommand command, Fornecedor fornecedor)
        {
            command.Parameters.AddWithValue("@Id", fornecedor.Id.ToString());
            command.Parameters.AddWithValue("@RazaoSocial", fornecedor.RazaoSocial.Trim());
            command.Parameters.AddWithValue("@NomeFantasia", fornecedor.NomeFantasia.Trim());
            command.Parameters.AddWithValue("@CNPJ", ToDbNullableString(fornecedor.CNPJ));
            command.Parameters.AddWithValue("@InscricaoEstadual", ToDbNullableString(fornecedor.InscricaoEstadual));
            command.Parameters.AddWithValue("@Telefone", ToDbNullableString(fornecedor.Telefone));
            command.Parameters.AddWithValue("@Celular", ToDbNullableString(fornecedor.Celular));
            command.Parameters.AddWithValue("@WhatsAppVendedor", ToDbNullableString(fornecedor.WhatsAppVendedor));
            command.Parameters.AddWithValue("@Email", ToDbNullableString(fornecedor.Email));
            command.Parameters.AddWithValue("@Site", ToDbNullableString(fornecedor.Site));
            command.Parameters.AddWithValue("@CEP", ToDbNullableString(fornecedor.CEP));
            command.Parameters.AddWithValue("@Rua", ToDbNullableString(fornecedor.Rua));
            command.Parameters.AddWithValue("@Numero", ToDbNullableString(fornecedor.Numero));
            command.Parameters.AddWithValue("@Complemento", ToDbNullableString(fornecedor.Complemento));
            command.Parameters.AddWithValue("@Bairro", ToDbNullableString(fornecedor.Bairro));
            command.Parameters.AddWithValue("@Cidade", ToDbNullableString(fornecedor.Cidade));
            command.Parameters.AddWithValue("@Estado", ToDbNullableString(fornecedor.Estado));
            command.Parameters.AddWithValue("@FormaPagamento", ToDbNullableString(fornecedor.FormaPagamento));
            command.Parameters.AddWithValue("@PrazoPagamento", ToDbNullableString(fornecedor.PrazoPagamento));
            command.Parameters.AddWithValue("@PrazoMedioEntregaDias", fornecedor.PrazoMedioEntregaDias);
            command.Parameters.AddWithValue("@PedidoMinimo", fornecedor.PedidoMinimo);
            command.Parameters.AddWithValue("@Categoria", string.IsNullOrWhiteSpace(fornecedor.Categoria) ? "Pecas" : fornecedor.Categoria.Trim());
            command.Parameters.AddWithValue("@CategoriaPreferencial", ToDbNullableString(fornecedor.CategoriaPreferencial));
            command.Parameters.AddWithValue("@Ativo", fornecedor.Ativo ? 1 : 0);
            command.Parameters.AddWithValue("@Nota", fornecedor.Nota);
            command.Parameters.AddWithValue("@Observacoes", ToDbNullableString(fornecedor.Observacoes));
            command.Parameters.AddWithValue("@DataCadastro", fornecedor.DataCadastro.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@UltimaCompra", ToDbNullableDate(fornecedor.UltimaCompra, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@TotalCompras", fornecedor.TotalCompras);
        }

        private static Fornecedor MaterializarFornecedor(SqliteDataReader reader)
        {
            return new Fornecedor
            {
                Id = ReadGuid(reader, 0),
                RazaoSocial = ReadString(reader, 1),
                NomeFantasia = ReadString(reader, 2),
                CNPJ = ReadString(reader, 3),
                InscricaoEstadual = ReadString(reader, 4),
                Telefone = ReadString(reader, 5),
                Celular = ReadString(reader, 6),
                WhatsAppVendedor = ReadString(reader, 7),
                Email = ReadString(reader, 8),
                Site = ReadString(reader, 9),
                CEP = ReadString(reader, 10),
                Rua = ReadString(reader, 11),
                Numero = ReadString(reader, 12),
                Complemento = ReadString(reader, 13),
                Bairro = ReadString(reader, 14),
                Cidade = ReadString(reader, 15),
                Estado = ReadString(reader, 16),
                FormaPagamento = ReadString(reader, 17),
                PrazoPagamento = ReadString(reader, 18),
                PrazoMedioEntregaDias = ReadInt(reader, 19),
                PedidoMinimo = ReadDecimal(reader, 20),
                Categoria = ReadString(reader, 21),
                CategoriaPreferencial = ReadString(reader, 22),
                Ativo = ReadBool(reader, 23),
                Nota = ReadInt(reader, 24),
                Observacoes = ReadString(reader, 25),
                DataCadastro = ReadDate(reader, 26, DateTime.Now),
                UltimaCompra = ReadNullableDate(reader, 27),
                TotalCompras = ReadDecimal(reader, 28)
            };
        }

        private static void SalvarContatos(SqliteConnection connection, SqliteTransaction transaction, Fornecedor fornecedor)
        {
            foreach (var contato in fornecedor.Contatos ?? new List<ContatoFornecedor>())
            {
                contato.Id = contato.Id == Guid.Empty ? Guid.NewGuid() : contato.Id;
                contato.FornecedorId = fornecedor.Id;

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    INSERT INTO ContatosFornecedor
                    (
                        Id,
                        FornecedorId,
                        Nome,
                        Cargo,
                        Telefone,
                        Email,
                        Principal
                    )
                    VALUES
                    (
                        @Id,
                        @FornecedorId,
                        @Nome,
                        @Cargo,
                        @Telefone,
                        @Email,
                        @Principal
                    );";

                command.Parameters.AddWithValue("@Id", contato.Id.ToString());
                command.Parameters.AddWithValue("@FornecedorId", fornecedor.Id.ToString());
                command.Parameters.AddWithValue("@Nome", contato.Nome.Trim());
                command.Parameters.AddWithValue("@Cargo", ToDbNullableString(contato.Cargo));
                command.Parameters.AddWithValue("@Telefone", ToDbNullableString(contato.Telefone));
                command.Parameters.AddWithValue("@Email", ToDbNullableString(contato.Email));
                command.Parameters.AddWithValue("@Principal", contato.Principal ? 1 : 0);
                command.ExecuteNonQuery();
            }
        }

        private static void ExcluirContatos(SqliteConnection connection, SqliteTransaction transaction, Guid fornecedorId)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM ContatosFornecedor WHERE FornecedorId = @FornecedorId;";
            command.Parameters.AddWithValue("@FornecedorId", fornecedorId.ToString());
            command.ExecuteNonQuery();
        }

        private static void ExcluirProdutoFornecedores(SqliteConnection connection, SqliteTransaction transaction, Guid fornecedorId)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM ProdutoFornecedores WHERE FornecedorId = @FornecedorId;";
            command.Parameters.AddWithValue("@FornecedorId", fornecedorId.ToString());
            command.ExecuteNonQuery();
        }

        private static void CarregarContatos(SqliteConnection connection, IEnumerable<Fornecedor> fornecedores)
        {
            foreach (var fornecedor in fornecedores)
            {
                fornecedor.Contatos = ObterContatos(connection, fornecedor.Id);
            }
        }

        private static List<ContatoFornecedor> ObterContatos(SqliteConnection connection, Guid fornecedorId)
        {
            var contatos = new List<ContatoFornecedor>();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    Id,
                    FornecedorId,
                    Nome,
                    Cargo,
                    Telefone,
                    Email,
                    Principal
                FROM ContatosFornecedor
                WHERE FornecedorId = @FornecedorId
                ORDER BY Principal DESC, Nome;";
            command.Parameters.AddWithValue("@FornecedorId", fornecedorId.ToString());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                contatos.Add(new ContatoFornecedor
                {
                    Id = ReadGuid(reader, 0),
                    FornecedorId = ReadGuid(reader, 1),
                    Nome = ReadString(reader, 2),
                    Cargo = ReadString(reader, 3),
                    Telefone = ReadString(reader, 4),
                    Email = ReadString(reader, 5),
                    Principal = ReadBool(reader, 6)
                });
            }

            return contatos;
        }

        private static void VincularProdutosRelacionados(SqliteConnection connection, SqliteTransaction transaction, Fornecedor fornecedor)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                UPDATE Produtos
                SET
                    FornecedorId = @FornecedorId,
                    Fornecedor = CASE
                        WHEN trim(COALESCE(Fornecedor, '')) = '' THEN @NomeFantasia
                        ELSE Fornecedor
                    END,
                    CNPJFornecedor = CASE
                        WHEN trim(COALESCE(CNPJFornecedor, '')) = '' THEN @Cnpj
                        ELSE CNPJFornecedor
                    END
                WHERE (
                        @CnpjNormalizado <> ''
                    AND replace(replace(replace(lower(COALESCE(CNPJFornecedor, '')), '.', ''), '/', ''), '-', '') = @CnpjNormalizado
                    )
                   OR (
                        @NomeFantasiaNormalizado <> ''
                    AND (
                           lower(trim(COALESCE(Fornecedor, ''))) = @NomeFantasiaNormalizado
                        OR lower(trim(COALESCE(Fornecedor, ''))) = @RazaoSocialNormalizada
                    )
                   );";
            command.Parameters.AddWithValue("@FornecedorId", fornecedor.Id.ToString());
            command.Parameters.AddWithValue("@NomeFantasia", fornecedor.NomeFantasia);
            command.Parameters.AddWithValue("@Cnpj", ToDbNullableString(fornecedor.CNPJ));
            command.Parameters.AddWithValue("@CnpjNormalizado", NormalizarDocumento(fornecedor.CNPJ));
            command.Parameters.AddWithValue("@NomeFantasiaNormalizado", (fornecedor.NomeFantasia ?? string.Empty).Trim().ToLowerInvariant());
            command.Parameters.AddWithValue("@RazaoSocialNormalizada", (fornecedor.RazaoSocial ?? string.Empty).Trim().ToLowerInvariant());
            command.ExecuteNonQuery();
        }

        private static object ToDbNullableString(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
        }

        private static string NormalizarDocumento(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim()
                .Replace(".", string.Empty, StringComparison.Ordinal)
                .Replace("/", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .ToLowerInvariant();
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

        private static string? CriarSnapshot(Fornecedor? fornecedor)
        {
            return fornecedor == null
                ? null
                : $"RazaoSocial={fornecedor.RazaoSocial}; NomeFantasia={fornecedor.NomeFantasia}; CNPJ={fornecedor.CNPJ}; WhatsApp={fornecedor.WhatsAppVendedor}; CategoriaPreferencial={fornecedor.CategoriaPreferencial}; Ativo={fornecedor.Ativo}; Nota={fornecedor.Nota}; Contatos={fornecedor.Contatos.Count}";
        }

        private static void RegistrarAuditoria(string acao, Fornecedor? fornecedor, string? anterior, string? novo, Guid? idOverride = null)
        {
            global::PrimoAutoEletrica.App.Audit.Registrar(
                categoria: "Fornecedores",
                acao: acao,
                entidade: "Fornecedor",
                entidadeId: (fornecedor?.Id ?? idOverride ?? Guid.Empty).ToString(),
                detalhes: fornecedor == null ? "Fornecedor nao localizado." : $"RazaoSocial={fornecedor.RazaoSocial}; NomeFantasia={fornecedor.NomeFantasia}",
                valorAnterior: anterior,
                valorNovo: novo);
        }
    }
}
