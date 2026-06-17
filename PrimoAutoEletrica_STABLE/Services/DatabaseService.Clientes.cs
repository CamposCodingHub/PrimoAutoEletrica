using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace PrimoAutoEletrica.Services
{
    public partial class DatabaseService
    {
        // CRUD de clientes mantido em partial para preservar a API atual.

        // =====================================================
        // CLIENTES - CRUD
        // =====================================================
        public List<Cliente> ObterTodosClientes()
        {
            if (UsarClienteRepositoryBridge())
                return global::PrimoAutoEletrica.App.Repositories.Clientes.ObterTodos();

            var clientes = new List<Cliente>();

            try
            {
                using var connection = GetConnection();
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT
                        Id,
                        Nome,
                        TipoPessoa,
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
                        Observacoes,
                        CaminhoDocumento,
                        CaminhoAssinatura,
                        DataCadastro,
                        UltimaVisita,
                        ImagemUrl
                    FROM Clientes
                    ORDER BY DataCadastro DESC
                ";

                using var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    clientes.Add(new Cliente
                    {
                        Id = reader.GetGuid(0),
                        Nome = reader.GetString(1),
                        TipoPessoa = NormalizarTipoPessoaCliente(reader.IsDBNull(2) ? "" : reader.GetString(2), reader.IsDBNull(3) ? "" : reader.GetString(3)),
                        CPF = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        RG = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        DataNascimento = reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5)),
                        Telefone = reader.IsDBNull(6) ? "" : reader.GetString(6),
                        WhatsApp = reader.IsDBNull(7) ? "" : reader.GetString(7),
                        Email = reader.IsDBNull(8) ? "" : reader.GetString(8),
                        CEP = reader.IsDBNull(9) ? "" : reader.GetString(9),
                        Rua = reader.IsDBNull(10) ? "" : reader.GetString(10),
                        Numero = reader.IsDBNull(11) ? "" : reader.GetString(11),
                        Bairro = reader.IsDBNull(12) ? "" : reader.GetString(12),
                        Cidade = reader.IsDBNull(13) ? "" : reader.GetString(13),
                        Estado = reader.IsDBNull(14) ? "" : reader.GetString(14),
                        Ativo = reader.GetBoolean(15),
                        ClienteVip = reader.GetBoolean(16),
                        TotalGasto = reader.IsDBNull(17) ? 0 : Convert.ToDecimal(reader.GetDouble(17)),
                        TotalServicos = reader.IsDBNull(18) ? 0 : reader.GetInt32(18),
                        PontosFidelidade = reader.IsDBNull(19) ? 0 : reader.GetInt32(19),
                        Observacoes = reader.IsDBNull(20) ? "" : reader.GetString(20),
                        CaminhoDocumento = reader.IsDBNull(21) ? "" : reader.GetString(21),
                        CaminhoAssinatura = reader.IsDBNull(22) ? "" : reader.GetString(22),
                        DataCadastro = DateTime.Parse(reader.GetString(23)),
                        UltimaVisita = reader.IsDBNull(24) ? null : DateTime.Parse(reader.GetString(24)),
                        ImagemUrl = reader.IsDBNull(25) ? "" : reader.GetString(25)
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Erro ao obter clientes no banco de dados.", ex);
                throw;
            }

            foreach (var cliente in clientes)
            {
                cliente.Veiculos = ObterVeiculosPorClienteId(cliente.Id);
                cliente.HistoricoServicos = ObterHistoricoServicosPorClienteId(cliente.Id);
            }

            return clientes;
        }

        public Cliente? ObterClientePorId(Guid id)
        {
            if (UsarClienteRepositoryBridge())
                return global::PrimoAutoEletrica.App.Repositories.Clientes.ObterPorId(id);

            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    Id,
                    Nome,
                    TipoPessoa,
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
                    Observacoes,
                    CaminhoDocumento,
                    CaminhoAssinatura,
                    DataCadastro,
                    UltimaVisita,
                    ImagemUrl
                FROM Clientes
                WHERE Id = @Id
                LIMIT 1
            ";

            command.Parameters.AddWithValue("@Id", id.ToString());

            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            var cliente = new Cliente
            {
                Id = reader.GetGuid(0),
                Nome = reader.GetString(1),
                TipoPessoa = NormalizarTipoPessoaCliente(reader.IsDBNull(2) ? "" : reader.GetString(2), reader.IsDBNull(3) ? "" : reader.GetString(3)),
                CPF = reader.IsDBNull(3) ? "" : reader.GetString(3),
                RG = reader.IsDBNull(4) ? "" : reader.GetString(4),
                DataNascimento = reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5)),
                Telefone = reader.IsDBNull(6) ? "" : reader.GetString(6),
                WhatsApp = reader.IsDBNull(7) ? "" : reader.GetString(7),
                Email = reader.IsDBNull(8) ? "" : reader.GetString(8),
                CEP = reader.IsDBNull(9) ? "" : reader.GetString(9),
                Rua = reader.IsDBNull(10) ? "" : reader.GetString(10),
                Numero = reader.IsDBNull(11) ? "" : reader.GetString(11),
                Bairro = reader.IsDBNull(12) ? "" : reader.GetString(12),
                Cidade = reader.IsDBNull(13) ? "" : reader.GetString(13),
                Estado = reader.IsDBNull(14) ? "" : reader.GetString(14),
                Ativo = reader.GetBoolean(15),
                ClienteVip = reader.GetBoolean(16),
                TotalGasto = reader.IsDBNull(17) ? 0 : Convert.ToDecimal(reader.GetDouble(17)),
                TotalServicos = reader.IsDBNull(18) ? 0 : reader.GetInt32(18),
                PontosFidelidade = reader.IsDBNull(19) ? 0 : reader.GetInt32(19),
                Observacoes = reader.IsDBNull(20) ? "" : reader.GetString(20),
                CaminhoDocumento = reader.IsDBNull(21) ? "" : reader.GetString(21),
                CaminhoAssinatura = reader.IsDBNull(22) ? "" : reader.GetString(22),
                DataCadastro = DateTime.Parse(reader.GetString(23)),
                UltimaVisita = reader.IsDBNull(24) ? null : DateTime.Parse(reader.GetString(24)),
                ImagemUrl = reader.IsDBNull(25) ? "" : reader.GetString(25)
            };

            cliente.Veiculos = ObterVeiculosPorClienteId(cliente.Id);
            cliente.HistoricoServicos = ObterHistoricoServicosPorClienteId(cliente.Id);

            return cliente;
        }

        public void InserirCliente(Cliente cliente)
        {
            if (UsarClienteRepositoryBridge())
            {
                global::PrimoAutoEletrica.App.Repositories.Clientes.Inserir(cliente);
                return;
            }

            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            using var command = connection.CreateCommand();
            command.Transaction = transaction;

            command.CommandText = @"
        INSERT INTO Clientes
        (
            Id,
            Nome,
            TipoPessoa,
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
            Observacoes,
            CaminhoDocumento,
            CaminhoAssinatura,
            DataCadastro,
            UltimaVisita,
            ImagemUrl
        )
        VALUES
        (
            @Id,
            @Nome,
            @TipoPessoa,
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
            @Observacoes,
            @CaminhoDocumento,
            @CaminhoAssinatura,
            @DataCadastro,
            @UltimaVisita,
            @ImagemUrl
        )
    ";

            // =========================
            // PARÂMETROS
            // =========================

            command.Parameters.AddWithValue(
                "@Id",
                cliente.Id.ToString()
            );

            command.Parameters.AddWithValue(
                "@Nome",
                string.IsNullOrWhiteSpace(cliente.Nome)
                    ? DBNull.Value
                    : cliente.Nome
            );

            command.Parameters.AddWithValue(
                "@TipoPessoa",
                NormalizarTipoPessoaCliente(cliente.TipoPessoa, cliente.CPF)
            );

            command.Parameters.AddWithValue(
                "@CPF",
                string.IsNullOrWhiteSpace(cliente.CPF)
                    ? DBNull.Value
                    : cliente.CPF
            );

            command.Parameters.AddWithValue(
                "@RG",
                string.IsNullOrWhiteSpace(cliente.RG)
                    ? DBNull.Value
                    : cliente.RG
            );

            command.Parameters.AddWithValue(
                "@DataNascimento",
                cliente.DataNascimento.HasValue
                    ? cliente.DataNascimento.Value.ToString("yyyy-MM-dd")
                    : DBNull.Value
            );

            command.Parameters.AddWithValue(
                "@Telefone",
                string.IsNullOrWhiteSpace(cliente.Telefone)
                    ? DBNull.Value
                    : cliente.Telefone
            );

            command.Parameters.AddWithValue(
                "@WhatsApp",
                string.IsNullOrWhiteSpace(cliente.WhatsApp)
                    ? DBNull.Value
                    : cliente.WhatsApp
            );

            command.Parameters.AddWithValue(
                "@Email",
                string.IsNullOrWhiteSpace(cliente.Email)
                    ? DBNull.Value
                    : cliente.Email
            );

            command.Parameters.AddWithValue(
                "@CEP",
                string.IsNullOrWhiteSpace(cliente.CEP)
                    ? DBNull.Value
                    : cliente.CEP
            );

            command.Parameters.AddWithValue(
                "@Rua",
                string.IsNullOrWhiteSpace(cliente.Rua)
                    ? DBNull.Value
                    : cliente.Rua
            );

            command.Parameters.AddWithValue(
                "@Numero",
                string.IsNullOrWhiteSpace(cliente.Numero)
                    ? DBNull.Value
                    : cliente.Numero
            );

            command.Parameters.AddWithValue(
                "@Bairro",
                string.IsNullOrWhiteSpace(cliente.Bairro)
                    ? DBNull.Value
                    : cliente.Bairro
            );

            command.Parameters.AddWithValue(
                "@Cidade",
                string.IsNullOrWhiteSpace(cliente.Cidade)
                    ? DBNull.Value
                    : cliente.Cidade
            );

            command.Parameters.AddWithValue(
                "@Estado",
                string.IsNullOrWhiteSpace(cliente.Estado)
                    ? DBNull.Value
                    : cliente.Estado
            );

            command.Parameters.AddWithValue(
                "@Ativo",
                cliente.Ativo ? 1 : 0
            );

            command.Parameters.AddWithValue(
                "@ClienteVip",
                cliente.ClienteVip ? 1 : 0
            );

            command.Parameters.AddWithValue(
                "@TotalGasto",
                cliente.TotalGasto
            );

            command.Parameters.AddWithValue(
                "@TotalServicos",
                cliente.TotalServicos
            );

            command.Parameters.AddWithValue(
                "@PontosFidelidade",
                cliente.PontosFidelidade
            );

            command.Parameters.AddWithValue(
                "@Observacoes",
                string.IsNullOrWhiteSpace(cliente.Observacoes)
                    ? DBNull.Value
                    : cliente.Observacoes
            );

            command.Parameters.AddWithValue(
                "@CaminhoDocumento",
                string.IsNullOrWhiteSpace(cliente.CaminhoDocumento)
                    ? DBNull.Value
                    : cliente.CaminhoDocumento
            );

            command.Parameters.AddWithValue(
                "@CaminhoAssinatura",
                string.IsNullOrWhiteSpace(cliente.CaminhoAssinatura)
                    ? DBNull.Value
                    : cliente.CaminhoAssinatura
            );

            command.Parameters.AddWithValue(
                "@DataCadastro",
                cliente.DataCadastro.ToString("yyyy-MM-dd HH:mm:ss")
            );

            command.Parameters.AddWithValue(
                "@UltimaVisita",
                cliente.UltimaVisita.HasValue
                    ? cliente.UltimaVisita.Value.ToString("yyyy-MM-dd HH:mm:ss")
                    : DBNull.Value
            );

            command.Parameters.AddWithValue(
                "@ImagemUrl",
                string.IsNullOrWhiteSpace(cliente.ImagemUrl)
                    ? DBNull.Value
                    : cliente.ImagemUrl
            );

            // =========================
            // EXECUTA
            // =========================

            command.ExecuteNonQuery();
            SalvarVeiculosDoCliente(cliente.Id, cliente.Veiculos, connection, transaction);
            AtualizarResumoCliente(connection, transaction, cliente.Id);
            transaction.Commit();
        }
        public void AtualizarCliente(Cliente cliente)
        {
            if (UsarClienteRepositoryBridge())
            {
                global::PrimoAutoEletrica.App.Repositories.Clientes.Atualizar(cliente);
                return;
            }

            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                UPDATE Clientes
                SET
                    Nome = @Nome,
                    TipoPessoa = @TipoPessoa,
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
                    Observacoes = @Observacoes,
                    CaminhoDocumento = @CaminhoDocumento,
                    CaminhoAssinatura = @CaminhoAssinatura,
                    UltimaVisita = @UltimaVisita,
                    ImagemUrl = @ImagemUrl
                WHERE Id = @Id
            ";

            command.Parameters.AddWithValue("@Nome", ToDbNullableString(cliente.Nome));
            command.Parameters.AddWithValue("@TipoPessoa", NormalizarTipoPessoaCliente(cliente.TipoPessoa, cliente.CPF));
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
            command.Parameters.AddWithValue("@Observacoes", ToDbNullableString(cliente.Observacoes));
            command.Parameters.AddWithValue("@CaminhoDocumento", ToDbNullableString(cliente.CaminhoDocumento));
            command.Parameters.AddWithValue("@CaminhoAssinatura", ToDbNullableString(cliente.CaminhoAssinatura));
            command.Parameters.AddWithValue("@UltimaVisita", ToDbNullableDate(cliente.UltimaVisita, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@ImagemUrl", ToDbNullableString(cliente.ImagemUrl));
            command.Parameters.AddWithValue("@Id", cliente.Id.ToString());

            command.ExecuteNonQuery();
            SalvarVeiculosDoCliente(cliente.Id, cliente.Veiculos, connection, transaction);
            AtualizarResumoCliente(connection, transaction, cliente.Id);
            transaction.Commit();
        }

        public void ExcluirCliente(Guid id)
        {
            if (UsarClienteRepositoryBridge())
            {
                global::PrimoAutoEletrica.App.Repositories.Clientes.Excluir(id);
                return;
            }

            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            if (ClientePossuiOrdensServico(connection, transaction, id))
                throw new InvalidOperationException("O cliente possui ordens de servico cadastradas. Inative o cadastro em vez de excluir.");

            ExcluirVeiculosDoCliente(id, connection, transaction);

            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                DELETE FROM Clientes
                WHERE Id = @Id
            ";

            command.Parameters.AddWithValue("@Id", id.ToString());

            command.ExecuteNonQuery();
            transaction.Commit();
        }

        private static string NormalizarTipoPessoaCliente(string? tipoPessoa, string? documento)
        {
            var valor = tipoPessoa?.Trim() ?? string.Empty;
            if (valor.Equals("Juridica", StringComparison.OrdinalIgnoreCase) ||
                valor.Equals("Pessoa juridica", StringComparison.OrdinalIgnoreCase) ||
                valor.Equals("PJ", StringComparison.OrdinalIgnoreCase))
            {
                return "Juridica";
            }

            if (valor.Equals("Fisica", StringComparison.OrdinalIgnoreCase) ||
                valor.Equals("Pessoa fisica", StringComparison.OrdinalIgnoreCase) ||
                valor.Equals("PF", StringComparison.OrdinalIgnoreCase))
            {
                return "Fisica";
            }

            var digitos = new string((documento ?? string.Empty).Where(char.IsDigit).ToArray());
            return digitos.Length > 11 ? "Juridica" : "Fisica";
        }

    }
}
