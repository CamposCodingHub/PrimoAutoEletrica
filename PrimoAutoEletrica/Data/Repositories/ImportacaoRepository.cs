using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Data.Repositories
{
    public class ImportacaoRepository
    {
        private readonly DatabaseService _databaseService;

        public ImportacaoRepository(DatabaseService? databaseService = null)
        {
            _databaseService = databaseService ?? global::PrimoAutoEletrica.App.Database;

            using var connection = GetConnection();
            connection.Open();
            InicializarTabelas(connection);
        }

        private SqliteConnection GetConnection()
        {
            return _databaseService.GetConnection();
        }

        private static void InicializarTabelas(SqliteConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS ImportacoesNFe
                (
                    Id TEXT PRIMARY KEY,
                    ChaveAcesso TEXT,
                    Numero TEXT,
                    Serie TEXT,
                    DataEmissao TEXT,
                    DataEntrada TEXT,
                    ValorTotal REAL NOT NULL DEFAULT 0,
                    ValorProdutos REAL NOT NULL DEFAULT 0,
                    Modelo TEXT,
                    FornecedorNome TEXT,
                    FornecedorCNPJ TEXT,
                    Status TEXT NOT NULL,
                    CaminhoArquivo TEXT,
                    Erro TEXT,
                    DataImportacao TEXT NOT NULL,
                    UsuarioId TEXT,
                    UsuarioNome TEXT
                );";
            command.ExecuteNonQuery();

            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS ImportacoesItens
                (
                    Id TEXT PRIMARY KEY,
                    ImportacaoId TEXT NOT NULL,
                    Codigo TEXT,
                    Nome TEXT,
                    NCM TEXT,
                    CFOP TEXT,
                    Quantidade REAL NOT NULL DEFAULT 0,
                    ValorUnitario REAL NOT NULL DEFAULT 0,
                    ValorTotal REAL NOT NULL DEFAULT 0,
                    UnidadeMedida TEXT,
                    Status TEXT NOT NULL,
                    ProdutoExistenteId TEXT,
                    MotivoIgnorado TEXT,
                    SelecionadoParaImportacao INTEGER NOT NULL DEFAULT 1,
                    AcaoPlanejada TEXT,
                    CategoriaSugerida TEXT,
                    MargemAplicada REAL NOT NULL DEFAULT 0,
                    PrecoVendaSugerido REAL NOT NULL DEFAULT 0,
                    ProdutoVinculadoReferencia TEXT,
                    CodigoBarras TEXT,
                    ObservacaoConferencia TEXT,
                    ProdutoSnapshotAnterior TEXT,
                    ProdutoSnapshotPosterior TEXT,
                    FOREIGN KEY (ImportacaoId) REFERENCES ImportacoesNFe(Id)
                );";
            command.ExecuteNonQuery();

            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS ImportacoesNFeExclusoes
                (
                    Id TEXT PRIMARY KEY,
                    ImportacaoId TEXT NOT NULL,
                    ChaveAcesso TEXT,
                    Numero TEXT,
                    Serie TEXT,
                    FornecedorNome TEXT,
                    FornecedorCNPJ TEXT,
                    QuantidadeProdutos INTEGER NOT NULL DEFAULT 0,
                    ProdutosNovos INTEGER NOT NULL DEFAULT 0,
                    ProdutosAtualizados INTEGER NOT NULL DEFAULT 0,
                    ValorTotal REAL NOT NULL DEFAULT 0,
                    StatusOriginal TEXT,
                    UsuarioImportacao TEXT,
                    DataImportacaoOriginal TEXT,
                    DataExclusao TEXT NOT NULL,
                    UsuarioExclusao TEXT,
                    Motivo TEXT
                );";
            command.ExecuteNonQuery();

            command.CommandText = @"
                CREATE INDEX IF NOT EXISTS IX_ImportacoesNFeExclusoes_DataExclusao
                ON ImportacoesNFeExclusoes (DataExclusao DESC);";
            command.ExecuteNonQuery();

            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS ImportacoesNFeRollbacks
                (
                    Id TEXT PRIMARY KEY,
                    ImportacaoId TEXT NOT NULL,
                    ChaveAcesso TEXT,
                    Numero TEXT,
                    Serie TEXT,
                    FornecedorNome TEXT,
                    FornecedorCNPJ TEXT,
                    DataRollback TEXT NOT NULL,
                    UsuarioRollback TEXT,
                    Motivo TEXT,
                    TotalItens INTEGER NOT NULL DEFAULT 0,
                    ProdutosRemovidos INTEGER NOT NULL DEFAULT 0,
                    ProdutosBloqueados INTEGER NOT NULL DEFAULT 0,
                    ProdutosIgnorados INTEGER NOT NULL DEFAULT 0,
                    AtualizacoesRevertidas INTEGER NOT NULL DEFAULT 0,
                    AtualizacoesIgnoradas INTEGER NOT NULL DEFAULT 0,
                    Detalhes TEXT
                );";
            command.ExecuteNonQuery();

            command.CommandText = @"
                CREATE INDEX IF NOT EXISTS IX_ImportacoesNFeRollbacks_DataRollback
                ON ImportacoesNFeRollbacks (DataRollback DESC);";
            command.ExecuteNonQuery();

            EnsureColumnExists(connection, "ImportacoesItens", "SelecionadoParaImportacao", "ALTER TABLE ImportacoesItens ADD COLUMN SelecionadoParaImportacao INTEGER NOT NULL DEFAULT 1;");
            EnsureColumnExists(connection, "ImportacoesItens", "AcaoPlanejada", "ALTER TABLE ImportacoesItens ADD COLUMN AcaoPlanejada TEXT;");
            EnsureColumnExists(connection, "ImportacoesItens", "CategoriaSugerida", "ALTER TABLE ImportacoesItens ADD COLUMN CategoriaSugerida TEXT;");
            EnsureColumnExists(connection, "ImportacoesItens", "MargemAplicada", "ALTER TABLE ImportacoesItens ADD COLUMN MargemAplicada REAL NOT NULL DEFAULT 0;");
            EnsureColumnExists(connection, "ImportacoesItens", "PrecoVendaSugerido", "ALTER TABLE ImportacoesItens ADD COLUMN PrecoVendaSugerido REAL NOT NULL DEFAULT 0;");
            EnsureColumnExists(connection, "ImportacoesItens", "ProdutoVinculadoReferencia", "ALTER TABLE ImportacoesItens ADD COLUMN ProdutoVinculadoReferencia TEXT;");
            EnsureColumnExists(connection, "ImportacoesItens", "CodigoBarras", "ALTER TABLE ImportacoesItens ADD COLUMN CodigoBarras TEXT;");
            EnsureColumnExists(connection, "ImportacoesItens", "ObservacaoConferencia", "ALTER TABLE ImportacoesItens ADD COLUMN ObservacaoConferencia TEXT;");
            EnsureColumnExists(connection, "ImportacoesItens", "ProdutoSnapshotAnterior", "ALTER TABLE ImportacoesItens ADD COLUMN ProdutoSnapshotAnterior TEXT;");
            EnsureColumnExists(connection, "ImportacoesItens", "ProdutoSnapshotPosterior", "ALTER TABLE ImportacoesItens ADD COLUMN ProdutoSnapshotPosterior TEXT;");
            EnsureColumnExists(connection, "ImportacoesNFeRollbacks", "AtualizacoesRevertidas", "ALTER TABLE ImportacoesNFeRollbacks ADD COLUMN AtualizacoesRevertidas INTEGER NOT NULL DEFAULT 0;");
        }

        public void SalvarImportacao(NotaFiscalImportada nota)
        {
            using var connection = GetConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // Salvar nota fiscal
                var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    INSERT OR REPLACE INTO ImportacoesNFe 
                    (Id, ChaveAcesso, Numero, Serie, DataEmissao, DataEntrada, ValorTotal, ValorProdutos, 
                     Modelo, FornecedorNome, FornecedorCNPJ, Status, CaminhoArquivo, Erro, DataImportacao, UsuarioId, UsuarioNome)
                    VALUES 
                    (@Id, @ChaveAcesso, @Numero, @Serie, @DataEmissao, @DataEntrada, @ValorTotal, @ValorProdutos,
                     @Modelo, @FornecedorNome, @FornecedorCNPJ, @Status, @CaminhoArquivo, @Erro, @DataImportacao, @UsuarioId, @UsuarioNome)
                ";

                command.Parameters.AddWithValue("@Id", nota.Id.ToString());
                command.Parameters.AddWithValue("@ChaveAcesso", nota.ChaveAcesso);
                command.Parameters.AddWithValue("@Numero", nota.Numero);
                command.Parameters.AddWithValue("@Serie", nota.Serie);
                command.Parameters.AddWithValue("@DataEmissao", nota.DataEmissao.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@DataEntrada", nota.DataEntrada.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@ValorTotal", nota.ValorTotal);
                command.Parameters.AddWithValue("@ValorProdutos", nota.ValorProdutos);
                command.Parameters.AddWithValue("@Modelo", nota.Modelo);
                command.Parameters.AddWithValue("@FornecedorNome", nota.Fornecedor.Nome);
                command.Parameters.AddWithValue("@FornecedorCNPJ", nota.Fornecedor.CNPJ);
                command.Parameters.AddWithValue("@Status", nota.Status.ToString());
                command.Parameters.AddWithValue("@CaminhoArquivo", nota.CaminhoArquivo);
                command.Parameters.AddWithValue("@Erro", string.IsNullOrWhiteSpace(nota.Erro) ? (object)DBNull.Value : nota.Erro);
                command.Parameters.AddWithValue("@DataImportacao", nota.DataImportacao.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@UsuarioId", nota.UsuarioId.ToString());
                command.Parameters.AddWithValue("@UsuarioNome", nota.UsuarioNome);

                command.ExecuteNonQuery();

                var deleteItemsCommand = connection.CreateCommand();
                deleteItemsCommand.Transaction = transaction;
                deleteItemsCommand.CommandText = "DELETE FROM ImportacoesItens WHERE ImportacaoId = @ImportacaoId";
                deleteItemsCommand.Parameters.AddWithValue("@ImportacaoId", nota.Id.ToString());
                deleteItemsCommand.ExecuteNonQuery();

                // Salvar itens da nota
                foreach (var produto in nota.Produtos)
                {
                    var itemCommand = connection.CreateCommand();
                    itemCommand.Transaction = transaction;
                    itemCommand.CommandText = @"
                        INSERT INTO ImportacoesItens 
                        (Id, ImportacaoId, Codigo, Nome, NCM, CFOP, Quantidade, ValorUnitario, ValorTotal, 
                         UnidadeMedida, Status, ProdutoExistenteId, MotivoIgnorado, SelecionadoParaImportacao,
                         AcaoPlanejada, CategoriaSugerida, MargemAplicada, PrecoVendaSugerido,
                         ProdutoVinculadoReferencia, CodigoBarras, ObservacaoConferencia,
                         ProdutoSnapshotAnterior, ProdutoSnapshotPosterior)
                        VALUES 
                        (@Id, @ImportacaoId, @Codigo, @Nome, @NCM, @CFOP, @Quantidade, @ValorUnitario, @ValorTotal,
                         @UnidadeMedida, @Status, @ProdutoExistenteId, @MotivoIgnorado, @SelecionadoParaImportacao,
                         @AcaoPlanejada, @CategoriaSugerida, @MargemAplicada, @PrecoVendaSugerido,
                         @ProdutoVinculadoReferencia, @CodigoBarras, @ObservacaoConferencia,
                         @ProdutoSnapshotAnterior, @ProdutoSnapshotPosterior)
                    ";

                    itemCommand.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                    itemCommand.Parameters.AddWithValue("@ImportacaoId", nota.Id.ToString());
                    itemCommand.Parameters.AddWithValue("@Codigo", produto.Codigo);
                    itemCommand.Parameters.AddWithValue("@Nome", produto.Nome);
                    itemCommand.Parameters.AddWithValue("@NCM", produto.NCM);
                    itemCommand.Parameters.AddWithValue("@CFOP", produto.CFOP);
                    itemCommand.Parameters.AddWithValue("@Quantidade", produto.Quantidade);
                    itemCommand.Parameters.AddWithValue("@ValorUnitario", produto.ValorUnitario);
                    itemCommand.Parameters.AddWithValue("@ValorTotal", produto.ValorTotal);
                    itemCommand.Parameters.AddWithValue("@UnidadeMedida", produto.UnidadeMedida);
                    itemCommand.Parameters.AddWithValue("@Status", produto.Status.ToString());
                    itemCommand.Parameters.AddWithValue("@ProdutoExistenteId", produto.ProdutoExistenteId?.ToString() ?? (object)DBNull.Value);
                    itemCommand.Parameters.AddWithValue("@MotivoIgnorado", string.IsNullOrWhiteSpace(produto.MotivoIgnorado) ? (object)DBNull.Value : produto.MotivoIgnorado);
                    itemCommand.Parameters.AddWithValue("@SelecionadoParaImportacao", produto.SelecionadoParaImportacao ? 1 : 0);
                    itemCommand.Parameters.AddWithValue("@AcaoPlanejada", string.IsNullOrWhiteSpace(produto.AcaoPlanejada) ? (object)DBNull.Value : produto.AcaoPlanejada);
                    itemCommand.Parameters.AddWithValue("@CategoriaSugerida", string.IsNullOrWhiteSpace(produto.CategoriaSugerida) ? (object)DBNull.Value : produto.CategoriaSugerida);
                    itemCommand.Parameters.AddWithValue("@MargemAplicada", produto.MargemAplicada);
                    itemCommand.Parameters.AddWithValue("@PrecoVendaSugerido", produto.PrecoVendaSugerido);
                    itemCommand.Parameters.AddWithValue("@ProdutoVinculadoReferencia", string.IsNullOrWhiteSpace(produto.ProdutoVinculadoReferencia) ? (object)DBNull.Value : produto.ProdutoVinculadoReferencia);
                    itemCommand.Parameters.AddWithValue("@CodigoBarras", string.IsNullOrWhiteSpace(produto.CodigoBarras) ? (object)DBNull.Value : produto.CodigoBarras);
                    itemCommand.Parameters.AddWithValue("@ObservacaoConferencia", string.IsNullOrWhiteSpace(produto.ObservacaoConferencia) ? (object)DBNull.Value : produto.ObservacaoConferencia);
                    itemCommand.Parameters.AddWithValue("@ProdutoSnapshotAnterior", string.IsNullOrWhiteSpace(produto.ProdutoSnapshotAnterior) ? (object)DBNull.Value : produto.ProdutoSnapshotAnterior);
                    itemCommand.Parameters.AddWithValue("@ProdutoSnapshotPosterior", string.IsNullOrWhiteSpace(produto.ProdutoSnapshotPosterior) ? (object)DBNull.Value : produto.ProdutoSnapshotPosterior);

                    itemCommand.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Erro ao salvar importação: {ex.Message}", ex);
            }
        }

        public List<NotaFiscalImportada> ObterHistoricoImportacoes(int limite = 50)
        {
            var importacoes = new List<NotaFiscalImportada>();

            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, ChaveAcesso, Numero, Serie, DataEmissao, DataEntrada, ValorTotal, ValorProdutos,
                       Modelo, FornecedorNome, FornecedorCNPJ, Status, CaminhoArquivo, Erro, DataImportacao, UsuarioId, UsuarioNome
                FROM ImportacoesNFe
                ORDER BY DataImportacao DESC
                LIMIT @Limite
            ";

            command.Parameters.AddWithValue("@Limite", limite);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var nota = new NotaFiscalImportada
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    ChaveAcesso = reader.GetString(1),
                    Numero = reader.GetString(2),
                    Serie = reader.GetString(3),
                    DataEmissao = DateTime.Parse(reader.GetString(4)),
                    DataEntrada = DateTime.Parse(reader.GetString(5)),
                    ValorTotal = Convert.ToDecimal(reader.GetDouble(6)),
                    ValorProdutos = Convert.ToDecimal(reader.GetDouble(7)),
                    Modelo = reader.GetString(8),
                    Fornecedor = new FornecedorNota
                    {
                        Nome = reader.GetString(9),
                        CNPJ = reader.GetString(10)
                    },
                    Status = Enum.Parse<StatusImportacaoNota>(reader.GetString(11)),
                    CaminhoArquivo = reader.GetString(12),
                    Erro = reader.IsDBNull(13) ? "" : reader.GetString(13),
                    DataImportacao = DateTime.Parse(reader.GetString(14)),
                    UsuarioId = reader.IsDBNull(15) || !Guid.TryParse(reader.GetString(15), out var usuarioIdHistorico) ? Guid.Empty : usuarioIdHistorico,
                    UsuarioNome = reader.IsDBNull(16) ? string.Empty : reader.GetString(16)
                };

                importacoes.Add(nota);
            }

            return importacoes;
        }

        public NotaFiscalImportada? ObterImportacaoPorId(Guid id)
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, ChaveAcesso, Numero, Serie, DataEmissao, DataEntrada, ValorTotal, ValorProdutos,
                       Modelo, FornecedorNome, FornecedorCNPJ, Status, CaminhoArquivo, Erro, DataImportacao, UsuarioId, UsuarioNome
                FROM ImportacoesNFe
                WHERE Id = @Id
            ";

            command.Parameters.AddWithValue("@Id", id.ToString());

            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                var nota = new NotaFiscalImportada
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    ChaveAcesso = reader.GetString(1),
                    Numero = reader.GetString(2),
                    Serie = reader.GetString(3),
                    DataEmissao = DateTime.Parse(reader.GetString(4)),
                    DataEntrada = DateTime.Parse(reader.GetString(5)),
                    ValorTotal = Convert.ToDecimal(reader.GetDouble(6)),
                    ValorProdutos = Convert.ToDecimal(reader.GetDouble(7)),
                    Modelo = reader.GetString(8),
                    Fornecedor = new FornecedorNota
                    {
                        Nome = reader.GetString(9),
                        CNPJ = reader.GetString(10)
                    },
                    Status = Enum.Parse<StatusImportacaoNota>(reader.GetString(11)),
                    CaminhoArquivo = reader.GetString(12),
                    Erro = reader.IsDBNull(13) ? "" : reader.GetString(13),
                    DataImportacao = DateTime.Parse(reader.GetString(14)),
                    UsuarioId = reader.IsDBNull(15) || !Guid.TryParse(reader.GetString(15), out var usuarioId) ? Guid.Empty : usuarioId,
                    UsuarioNome = reader.IsDBNull(16) ? string.Empty : reader.GetString(16)
                };

                // Carregar itens
                nota.Produtos = ObterItensImportacao(nota.Id, connection);

                return nota;
            }

            return null;
        }

        private List<ProdutoImportado> ObterItensImportacao(Guid importacaoId, SqliteConnection connection)
        {
            var itens = new List<ProdutoImportado>();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Codigo, Nome, NCM, CFOP, Quantidade, ValorUnitario, ValorTotal,
                       UnidadeMedida, Status, ProdutoExistenteId, MotivoIgnorado,
                       SelecionadoParaImportacao, AcaoPlanejada, CategoriaSugerida, MargemAplicada,
                       PrecoVendaSugerido, ProdutoVinculadoReferencia, CodigoBarras, ObservacaoConferencia,
                       ProdutoSnapshotAnterior, ProdutoSnapshotPosterior
                FROM ImportacoesItens
                WHERE ImportacaoId = @ImportacaoId
            ";

            command.Parameters.AddWithValue("@ImportacaoId", importacaoId.ToString());

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var item = new ProdutoImportado
                {
                    Codigo = reader.GetString(0),
                    Nome = reader.GetString(1),
                    NCM = reader.GetString(2),
                    CFOP = reader.GetString(3),
                    Quantidade = Convert.ToDecimal(reader.GetDouble(4)),
                    ValorUnitario = Convert.ToDecimal(reader.GetDouble(5)),
                    ValorTotal = Convert.ToDecimal(reader.GetDouble(6)),
                    UnidadeMedida = reader.GetString(7),
                    Status = Enum.Parse<StatusImportacao>(reader.GetString(8)),
                    ProdutoExistenteId = reader.IsDBNull(9) ? null : Guid.Parse(reader.GetString(9)),
                    MotivoIgnorado = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    SelecionadoParaImportacao = reader.IsDBNull(11) || Convert.ToInt32(reader.GetValue(11)) == 1,
                    AcaoPlanejada = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                    CategoriaSugerida = reader.IsDBNull(13) ? string.Empty : reader.GetString(13),
                    MargemAplicada = reader.IsDBNull(14) ? 0 : Convert.ToDecimal(reader.GetDouble(14)),
                    PrecoVendaSugerido = reader.IsDBNull(15) ? 0 : Convert.ToDecimal(reader.GetDouble(15)),
                    ProdutoVinculadoReferencia = reader.IsDBNull(16) ? string.Empty : reader.GetString(16),
                    CodigoBarras = reader.IsDBNull(17) ? string.Empty : reader.GetString(17),
                    ObservacaoConferencia = reader.IsDBNull(18) ? string.Empty : reader.GetString(18),
                    ProdutoSnapshotAnterior = reader.IsDBNull(19) ? string.Empty : reader.GetString(19),
                    ProdutoSnapshotPosterior = reader.IsDBNull(20) ? string.Empty : reader.GetString(20)
                };

                itens.Add(item);
            }

            return itens;
        }

        public bool VerificarNotaDuplicada(string chaveAcesso)
        {
            if (string.IsNullOrWhiteSpace(chaveAcesso))
            {
                return false;
            }

            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT COUNT(*)
                FROM ImportacoesNFe
                WHERE ChaveAcesso = @ChaveAcesso
            ";

            command.Parameters.AddWithValue("@ChaveAcesso", chaveAcesso);

            var count = Convert.ToInt32(command.ExecuteScalar());

            return count > 0;
        }

        public int ObterTotalExclusoesAuditadas()
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM ImportacoesNFeExclusoes;";

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public int ObterTotalRollbacksAuditados()
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM ImportacoesNFeRollbacks;";

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public void ExcluirImportacao(Guid id, string? usuarioExclusao = null, string? motivo = null)
        {
            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var auditoriaCommand = connection.CreateCommand();
                auditoriaCommand.Transaction = transaction;
                auditoriaCommand.CommandText = @"
                    INSERT INTO ImportacoesNFeExclusoes
                    (
                        Id,
                        ImportacaoId,
                        ChaveAcesso,
                        Numero,
                        Serie,
                        FornecedorNome,
                        FornecedorCNPJ,
                        QuantidadeProdutos,
                        ProdutosNovos,
                        ProdutosAtualizados,
                        ValorTotal,
                        StatusOriginal,
                        UsuarioImportacao,
                        DataImportacaoOriginal,
                        DataExclusao,
                        UsuarioExclusao,
                        Motivo
                    )
                    SELECT
                        @ExclusaoId,
                        n.Id,
                        n.ChaveAcesso,
                        n.Numero,
                        n.Serie,
                        n.FornecedorNome,
                        n.FornecedorCNPJ,
                        COALESCE((SELECT COUNT(*) FROM ImportacoesItens i WHERE i.ImportacaoId = n.Id), 0),
                        COALESCE((SELECT COUNT(*) FROM ImportacoesItens i WHERE i.ImportacaoId = n.Id AND i.Status = 'Novo'), 0),
                        COALESCE((SELECT COUNT(*) FROM ImportacoesItens i WHERE i.ImportacaoId = n.Id AND i.Status = 'Atualizado'), 0),
                        n.ValorTotal,
                        n.Status,
                        n.UsuarioNome,
                        n.DataImportacao,
                        @DataExclusao,
                        @UsuarioExclusao,
                        @Motivo
                    FROM ImportacoesNFe n
                    WHERE n.Id = @Id;";
                auditoriaCommand.Parameters.AddWithValue("@ExclusaoId", Guid.NewGuid().ToString());
                auditoriaCommand.Parameters.AddWithValue("@DataExclusao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                auditoriaCommand.Parameters.AddWithValue("@UsuarioExclusao", string.IsNullOrWhiteSpace(usuarioExclusao) ? "Sistema" : usuarioExclusao.Trim());
                auditoriaCommand.Parameters.AddWithValue("@Motivo", string.IsNullOrWhiteSpace(motivo) ? "Exclusao manual de importacao NF-e" : motivo.Trim());
                auditoriaCommand.Parameters.AddWithValue("@Id", id.ToString());

                var auditoriaCriada = auditoriaCommand.ExecuteNonQuery();
                if (auditoriaCriada == 0)
                {
                    throw new InvalidOperationException("Importacao de NF-e nao localizada para exclusao.");
                }

                var deleteItemsCommand = connection.CreateCommand();
                deleteItemsCommand.Transaction = transaction;
                deleteItemsCommand.CommandText = "DELETE FROM ImportacoesItens WHERE ImportacaoId = @ImportacaoId;";
                deleteItemsCommand.Parameters.AddWithValue("@ImportacaoId", id.ToString());
                deleteItemsCommand.ExecuteNonQuery();

                var deleteImportacaoCommand = connection.CreateCommand();
                deleteImportacaoCommand.Transaction = transaction;
                deleteImportacaoCommand.CommandText = "DELETE FROM ImportacoesNFe WHERE Id = @Id;";
                deleteImportacaoCommand.Parameters.AddWithValue("@Id", id.ToString());
                var linhasAfetadas = deleteImportacaoCommand.ExecuteNonQuery();

                if (linhasAfetadas == 0)
                {
                    throw new InvalidOperationException("Importacao de NF-e nao localizada para exclusao.");
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Erro ao excluir importacao de NF-e: {ex.Message}", ex);
            }
        }

        public ImportacaoRollbackResult DesfazerProdutosDaImportacao(Guid id, string? usuarioRollback = null, string? motivo = null)
        {
            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var cabecalho = ObterCabecalhoRollback(connection, transaction, id)
                    ?? throw new InvalidOperationException("Importacao de NF-e nao localizada para rollback.");
                var itens = ObterItensRollback(connection, transaction, id);

                var resultado = new ImportacaoRollbackResult
                {
                    ImportacaoId = id,
                    NumeroNota = cabecalho.Numero,
                    Serie = cabecalho.Serie,
                    Fornecedor = cabecalho.FornecedorNome,
                    TotalItens = itens.Count
                };

                foreach (var item in itens)
                {
                    ProcessarItemRollback(connection, transaction, cabecalho, item, resultado);
                }

                RegistrarRollback(connection, transaction, cabecalho, resultado, usuarioRollback, motivo);

                transaction.Commit();
                return resultado;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Erro ao desfazer produtos da importacao de NF-e: {ex.Message}", ex);
            }
        }

        private static void ProcessarItemRollback(
            SqliteConnection connection,
            SqliteTransaction transaction,
            ImportacaoRollbackCabecalho cabecalho,
            ImportacaoRollbackItem item,
            ImportacaoRollbackResult resultado)
        {
            if (string.Equals(item.Status, StatusImportacao.Atualizado.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                ProcessarAtualizacaoRollback(connection, transaction, item, resultado);
                return;
            }

            if (!string.Equals(item.Status, StatusImportacao.Novo.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                AdicionarResultado(resultado, item, item.ProdutoId, "Ignorado", "Item nao criou produto novo no estoque.");
                return;
            }

            var candidato = ResolverProdutoCriado(connection, transaction, cabecalho, item);
            if (candidato == null)
            {
                AdicionarResultado(resultado, item, item.ProdutoId, "Ignorado", "Produto criado pela importacao nao foi localizado ou ja foi removido.");
                return;
            }

            if (candidato.Ambiguo)
            {
                AdicionarResultado(resultado, item, candidato.Id, "Bloqueado", "Mais de um produto combina com este item; rollback automatico evitado.");
                return;
            }

            var bloqueios = ObterBloqueiosProduto(connection, transaction, cabecalho, item, candidato);
            if (bloqueios.Count > 0)
            {
                AdicionarResultado(resultado, item, candidato.Id, "Bloqueado", string.Join("; ", bloqueios));
                return;
            }

            using var deleteCommand = connection.CreateCommand();
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM Produtos WHERE Id = @ProdutoId;";
            deleteCommand.Parameters.AddWithValue("@ProdutoId", candidato.Id.ToString());
            deleteCommand.ExecuteNonQuery();

            AdicionarResultado(resultado, item, candidato.Id, "Removido", "Produto criado pela NF-e removido com travas de seguranca.");
        }

        private static void ProcessarAtualizacaoRollback(
            SqliteConnection connection,
            SqliteTransaction transaction,
            ImportacaoRollbackItem item,
            ImportacaoRollbackResult resultado)
        {
            if (!item.ProdutoId.HasValue)
            {
                resultado.AtualizacoesIgnoradas++;
                AdicionarResultado(resultado, item, null, "Ignorado", "Produto atualizado nao possui vinculo de ID no historico.");
                return;
            }

            var snapshotAnterior = ProdutoImportacaoSnapshot.FromJson(item.ProdutoSnapshotAnterior);
            var snapshotPosterior = ProdutoImportacaoSnapshot.FromJson(item.ProdutoSnapshotPosterior);
            if (snapshotAnterior == null || snapshotPosterior == null)
            {
                resultado.AtualizacoesIgnoradas++;
                AdicionarResultado(resultado, item, item.ProdutoId, "Ignorado", "Produto existente atualizado nao possui snapshot anterior/posterior seguro para reversao automatica.");
                return;
            }

            var snapshotAtual = ObterSnapshotProdutoAtual(connection, transaction, item.ProdutoId.Value);
            if (snapshotAtual == null)
            {
                resultado.AtualizacoesIgnoradas++;
                AdicionarResultado(resultado, item, item.ProdutoId, "Ignorado", "Produto atualizado nao foi localizado no estoque.");
                return;
            }

            if (!snapshotAtual.EquivaleA(snapshotPosterior))
            {
                resultado.AtualizacoesIgnoradas++;
                AdicionarResultado(resultado, item, item.ProdutoId, "Bloqueado", "Produto teve alteracao posterior ou ja foi revertido; snapshot posterior nao confere com o estado atual.");
                return;
            }

            RestaurarProdutoAtualizado(connection, transaction, snapshotAnterior);
            AdicionarResultado(resultado, item, item.ProdutoId, "Restaurado", "Produto atualizado pela NF-e restaurado para o snapshot anterior.");
        }

        private static ImportacaoRollbackCabecalho? ObterCabecalhoRollback(SqliteConnection connection, SqliteTransaction transaction, Guid id)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT Id, ChaveAcesso, Numero, Serie, FornecedorNome, FornecedorCNPJ, DataImportacao
                FROM ImportacoesNFe
                WHERE Id = @Id
                LIMIT 1;";
            command.Parameters.AddWithValue("@Id", id.ToString());

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return new ImportacaoRollbackCabecalho
            {
                Id = Guid.Parse(reader.GetString(0)),
                ChaveAcesso = ReadString(reader, 1),
                Numero = ReadString(reader, 2),
                Serie = ReadString(reader, 3),
                FornecedorNome = ReadString(reader, 4),
                FornecedorCNPJ = ReadString(reader, 5),
                DataImportacao = ReadDate(reader, 6)
            };
        }

        private static List<ImportacaoRollbackItem> ObterItensRollback(SqliteConnection connection, SqliteTransaction transaction, Guid importacaoId)
        {
            var itens = new List<ImportacaoRollbackItem>();

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT ProdutoExistenteId, Codigo, Nome, CodigoBarras, Quantidade, Status, AcaoPlanejada,
                       ProdutoSnapshotAnterior, ProdutoSnapshotPosterior
                FROM ImportacoesItens
                WHERE ImportacaoId = @ImportacaoId;";
            command.Parameters.AddWithValue("@ImportacaoId", importacaoId.ToString());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                itens.Add(new ImportacaoRollbackItem
                {
                    ProdutoId = ReadNullableGuid(reader, 0),
                    Codigo = ReadString(reader, 1),
                    Nome = ReadString(reader, 2),
                    CodigoBarras = ReadString(reader, 3),
                    Quantidade = ReadDecimal(reader, 4),
                    Status = ReadString(reader, 5),
                    AcaoPlanejada = ReadString(reader, 6),
                    ProdutoSnapshotAnterior = ReadString(reader, 7),
                    ProdutoSnapshotPosterior = ReadString(reader, 8)
                });
            }

            return itens;
        }

        private static ProdutoRollbackCandidate? ResolverProdutoCriado(
            SqliteConnection connection,
            SqliteTransaction transaction,
            ImportacaoRollbackCabecalho cabecalho,
            ImportacaoRollbackItem item)
        {
            if (item.ProdutoId.HasValue)
            {
                var porId = ObterProdutoRollback(connection, transaction, "Id = @ProdutoId", command =>
                {
                    command.Parameters.AddWithValue("@ProdutoId", item.ProdutoId.Value.ToString());
                });

                if (porId != null)
                {
                    return porId;
                }
            }

            if (string.IsNullOrWhiteSpace(cabecalho.Numero))
            {
                return null;
            }

            var candidatos = ObterProdutosRollback(connection, transaction, @"
                (
                    (@Codigo <> '' AND lower(trim(COALESCE(Codigo, ''))) = lower(trim(@Codigo)))
                    OR (@CodigoBarras <> '' AND lower(trim(COALESCE(CodigoBarras, ''))) = lower(trim(@CodigoBarras)))
                    OR (@Nome <> '' AND lower(trim(COALESCE(Nome, ''))) = lower(trim(@Nome)))
                )
                AND (
                    COALESCE(Descricao, '') LIKE @DescricaoMarker
                    OR COALESCE(Observacoes, '') LIKE @ObservacaoMarker
                )",
                command =>
                {
                    command.Parameters.AddWithValue("@Codigo", item.Codigo);
                    command.Parameters.AddWithValue("@CodigoBarras", item.CodigoBarras);
                    command.Parameters.AddWithValue("@Nome", item.Nome);
                    command.Parameters.AddWithValue("@DescricaoMarker", $"%Importado da NF-e {cabecalho.Numero}%");
                    command.Parameters.AddWithValue("@ObservacaoMarker", $"%Importado automaticamente da NF-e {cabecalho.Numero}%");
                },
                2);

            if (candidatos.Count == 0)
            {
                return null;
            }

            if (candidatos.Count > 1)
            {
                candidatos[0].Ambiguo = true;
            }

            return candidatos[0];
        }

        private static ProdutoRollbackCandidate? ObterProdutoRollback(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string whereClause,
            Action<SqliteCommand> addParameters)
        {
            return ObterProdutosRollback(connection, transaction, whereClause, addParameters, 1).FirstOrDefault();
        }

        private static ProdutoImportacaoSnapshot? ObterSnapshotProdutoAtual(
            SqliteConnection connection,
            SqliteTransaction transaction,
            Guid produtoId)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT
                    Id,
                    Categoria,
                    QuantidadeEstoque,
                    PrecoCompra,
                    PrecoVenda,
                    MargemLucro,
                    ValorTotalEstoque,
                    UnidadeMedida,
                    CodigoBarras,
                    NCMS,
                    CFOP,
                    DataUltimaCompra,
                    DataUltimaAtualizacao
                FROM Produtos
                WHERE Id = @ProdutoId
                LIMIT 1;";
            command.Parameters.AddWithValue("@ProdutoId", produtoId.ToString());

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return null;
            }

            return new ProdutoImportacaoSnapshot
            {
                ProdutoId = Guid.Parse(reader.GetString(0)),
                Categoria = ReadString(reader, 1),
                QuantidadeEstoque = ReadInt(reader, 2),
                PrecoCompra = ReadDecimal(reader, 3),
                PrecoVenda = ReadDecimal(reader, 4),
                MargemLucro = ReadDecimal(reader, 5),
                ValorTotalEstoque = ReadDecimal(reader, 6),
                UnidadeMedida = ReadString(reader, 7),
                CodigoBarras = ReadString(reader, 8),
                NCMS = ReadString(reader, 9),
                CFOP = ReadString(reader, 10),
                DataUltimaCompra = ReadNullableDate(reader, 11),
                DataUltimaAtualizacao = ReadNullableDate(reader, 12)
            };
        }

        private static void RestaurarProdutoAtualizado(
            SqliteConnection connection,
            SqliteTransaction transaction,
            ProdutoImportacaoSnapshot snapshot)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                UPDATE Produtos
                SET
                    Categoria = @Categoria,
                    QuantidadeEstoque = @QuantidadeEstoque,
                    PrecoCompra = @PrecoCompra,
                    PrecoVenda = @PrecoVenda,
                    MargemLucro = @MargemLucro,
                    ValorTotalEstoque = @ValorTotalEstoque,
                    UnidadeMedida = @UnidadeMedida,
                    CodigoBarras = @CodigoBarras,
                    NCMS = @NCMS,
                    CFOP = @CFOP,
                    DataUltimaCompra = @DataUltimaCompra,
                    DataUltimaAtualizacao = @DataUltimaAtualizacao,
                    RowVersion = COALESCE(RowVersion, 0) + 1,
                    DataUltimaAlteracao = @DataUltimaAlteracao
                WHERE Id = @ProdutoId;";
            command.Parameters.AddWithValue("@ProdutoId", snapshot.ProdutoId.ToString());
            command.Parameters.AddWithValue("@Categoria", ToDbNullableString(snapshot.Categoria));
            command.Parameters.AddWithValue("@QuantidadeEstoque", snapshot.QuantidadeEstoque);
            command.Parameters.AddWithValue("@PrecoCompra", snapshot.PrecoCompra);
            command.Parameters.AddWithValue("@PrecoVenda", snapshot.PrecoVenda);
            command.Parameters.AddWithValue("@MargemLucro", snapshot.MargemLucro);
            command.Parameters.AddWithValue("@ValorTotalEstoque", snapshot.ValorTotalEstoque);
            command.Parameters.AddWithValue("@UnidadeMedida", ToDbNullableString(snapshot.UnidadeMedida));
            command.Parameters.AddWithValue("@CodigoBarras", ToDbNullableString(snapshot.CodigoBarras));
            command.Parameters.AddWithValue("@NCMS", ToDbNullableString(snapshot.NCMS));
            command.Parameters.AddWithValue("@CFOP", ToDbNullableString(snapshot.CFOP));
            command.Parameters.AddWithValue("@DataUltimaCompra", ToDbNullableDate(snapshot.DataUltimaCompra));
            command.Parameters.AddWithValue("@DataUltimaAtualizacao", ToDbNullableDate(snapshot.DataUltimaAtualizacao));
            command.Parameters.AddWithValue("@DataUltimaAlteracao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));

            if (command.ExecuteNonQuery() == 0)
            {
                throw new InvalidOperationException("Produto atualizado nao foi localizado para restauracao.");
            }
        }

        private static List<ProdutoRollbackCandidate> ObterProdutosRollback(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string whereClause,
            Action<SqliteCommand> addParameters,
            int limit)
        {
            var produtos = new List<ProdutoRollbackCandidate>();

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $@"
                SELECT
                    Id,
                    Codigo,
                    Nome,
                    Descricao,
                    Fornecedor,
                    QuantidadeEstoque,
                    TotalVendas,
                    DataUltimaVenda,
                    Observacoes,
                    COALESCE(RowVersion, 0)
                FROM Produtos
                WHERE {whereClause}
                LIMIT @Limit;";
            command.Parameters.AddWithValue("@Limit", limit);
            addParameters(command);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                produtos.Add(new ProdutoRollbackCandidate
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    Codigo = ReadString(reader, 1),
                    Nome = ReadString(reader, 2),
                    Descricao = ReadString(reader, 3),
                    Fornecedor = ReadString(reader, 4),
                    QuantidadeEstoque = ReadInt(reader, 5),
                    TotalVendas = ReadInt(reader, 6),
                    DataUltimaVenda = ReadNullableDate(reader, 7),
                    Observacoes = ReadString(reader, 8),
                    RowVersion = ReadInt(reader, 9)
                });
            }

            return produtos;
        }

        private static List<string> ObterBloqueiosProduto(
            SqliteConnection connection,
            SqliteTransaction transaction,
            ImportacaoRollbackCabecalho cabecalho,
            ImportacaoRollbackItem item,
            ProdutoRollbackCandidate produto)
        {
            var bloqueios = new List<string>();

            if (!ProdutoTemAssinaturaDaImportacao(cabecalho, produto))
            {
                bloqueios.Add("produto nao tem assinatura clara desta NF-e");
            }

            var quantidadeImportada = ConverterQuantidade(item.Quantidade);
            if (produto.QuantidadeEstoque != quantidadeImportada)
            {
                bloqueios.Add($"estoque atual ({produto.QuantidadeEstoque}) difere da quantidade importada ({quantidadeImportada})");
            }

            if (produto.RowVersion > 0)
            {
                bloqueios.Add("produto teve alteracao posterior");
            }

            if (produto.TotalVendas > 0 || produto.DataUltimaVenda.HasValue)
            {
                bloqueios.Add("produto possui venda registrada");
            }

            var vendas = ContarReferenciasProduto(connection, transaction, "VendaItens", "ProdutoId", produto.Id);
            if (vendas > 0)
            {
                bloqueios.Add($"vinculado a {vendas} item(ns) de venda");
            }

            var ordens = ContarReferenciasProduto(connection, transaction, "OrdemServicoItens", "ProdutoId", produto.Id);
            if (ordens > 0)
            {
                bloqueios.Add($"vinculado a {ordens} item(ns) de OS");
            }

            var orcamentos = ContarReferenciasProduto(connection, transaction, "OrcamentoItens", "ProdutoId", produto.Id);
            if (orcamentos > 0)
            {
                bloqueios.Add($"vinculado a {orcamentos} item(ns) de orcamento");
            }

            var agendamentos = ContarReferenciasProduto(connection, transaction, "AgendamentoProdutos", "ProdutoId", produto.Id);
            if (agendamentos > 0)
            {
                bloqueios.Add($"vinculado a {agendamentos} item(ns) de agendamento");
            }

            var outrasImportacoes = ContarOutrasImportacoesDoProduto(connection, transaction, cabecalho.Id, produto.Id);
            if (outrasImportacoes > 0)
            {
                bloqueios.Add($"vinculado a {outrasImportacoes} outra(s) importacao(oes)");
            }

            var auditoriasPosteriores = ContarAuditoriasPosterioresDoProduto(connection, transaction, produto.Id);
            if (auditoriasPosteriores > 0)
            {
                bloqueios.Add("produto possui auditoria operacional posterior");
            }

            return bloqueios;
        }

        private static bool ProdutoTemAssinaturaDaImportacao(ImportacaoRollbackCabecalho cabecalho, ProdutoRollbackCandidate produto)
        {
            if (string.IsNullOrWhiteSpace(cabecalho.Numero))
            {
                return false;
            }

            var temMarcadorNota =
                Contem(produto.Descricao, $"Importado da NF-e {cabecalho.Numero}") ||
                Contem(produto.Observacoes, $"Importado automaticamente da NF-e {cabecalho.Numero}");

            var fornecedorCompativel =
                string.IsNullOrWhiteSpace(cabecalho.FornecedorNome) ||
                Contem(produto.Fornecedor, cabecalho.FornecedorNome) ||
                Contem(produto.Descricao, cabecalho.FornecedorNome);

            return temMarcadorNota && fornecedorCompativel;
        }

        private static int ContarReferenciasProduto(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string tabela,
            string coluna,
            Guid produtoId)
        {
            if (!TabelaExiste(connection, transaction, tabela))
            {
                return 0;
            }

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"SELECT COUNT(*) FROM {tabela} WHERE {coluna} = @ProdutoId;";
            command.Parameters.AddWithValue("@ProdutoId", produtoId.ToString());

            return Convert.ToInt32(command.ExecuteScalar());
        }

        private static int ContarOutrasImportacoesDoProduto(
            SqliteConnection connection,
            SqliteTransaction transaction,
            Guid importacaoAtualId,
            Guid produtoId)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT COUNT(*)
                FROM ImportacoesItens
                WHERE ProdutoExistenteId = @ProdutoId
                  AND ImportacaoId <> @ImportacaoId;";
            command.Parameters.AddWithValue("@ProdutoId", produtoId.ToString());
            command.Parameters.AddWithValue("@ImportacaoId", importacaoAtualId.ToString());

            return Convert.ToInt32(command.ExecuteScalar());
        }

        private static int ContarAuditoriasPosterioresDoProduto(
            SqliteConnection connection,
            SqliteTransaction transaction,
            Guid produtoId)
        {
            if (!TabelaExiste(connection, transaction, "AuditLogs"))
            {
                return 0;
            }

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT COUNT(*)
                FROM AuditLogs
                WHERE Entidade = 'Produto'
                  AND EntidadeId = @ProdutoId
                  AND Acao <> 'ProdutoCriado';";
            command.Parameters.AddWithValue("@ProdutoId", produtoId.ToString());

            return Convert.ToInt32(command.ExecuteScalar());
        }

        private static void RegistrarRollback(
            SqliteConnection connection,
            SqliteTransaction transaction,
            ImportacaoRollbackCabecalho cabecalho,
            ImportacaoRollbackResult resultado,
            string? usuarioRollback,
            string? motivo)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                INSERT INTO ImportacoesNFeRollbacks
                (
                    Id,
                    ImportacaoId,
                    ChaveAcesso,
                    Numero,
                    Serie,
                    FornecedorNome,
                    FornecedorCNPJ,
                    DataRollback,
                    UsuarioRollback,
                    Motivo,
                    TotalItens,
                    ProdutosRemovidos,
                    ProdutosBloqueados,
                    ProdutosIgnorados,
                    AtualizacoesRevertidas,
                    AtualizacoesIgnoradas,
                    Detalhes
                )
                VALUES
                (
                    @Id,
                    @ImportacaoId,
                    @ChaveAcesso,
                    @Numero,
                    @Serie,
                    @FornecedorNome,
                    @FornecedorCNPJ,
                    @DataRollback,
                    @UsuarioRollback,
                    @Motivo,
                    @TotalItens,
                    @ProdutosRemovidos,
                    @ProdutosBloqueados,
                    @ProdutosIgnorados,
                    @AtualizacoesRevertidas,
                    @AtualizacoesIgnoradas,
                    @Detalhes
                );";
            command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("@ImportacaoId", cabecalho.Id.ToString());
            command.Parameters.AddWithValue("@ChaveAcesso", cabecalho.ChaveAcesso);
            command.Parameters.AddWithValue("@Numero", cabecalho.Numero);
            command.Parameters.AddWithValue("@Serie", cabecalho.Serie);
            command.Parameters.AddWithValue("@FornecedorNome", cabecalho.FornecedorNome);
            command.Parameters.AddWithValue("@FornecedorCNPJ", cabecalho.FornecedorCNPJ);
            command.Parameters.AddWithValue("@DataRollback", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("@UsuarioRollback", string.IsNullOrWhiteSpace(usuarioRollback) ? "Sistema" : usuarioRollback.Trim());
            command.Parameters.AddWithValue("@Motivo", string.IsNullOrWhiteSpace(motivo) ? "Rollback manual de produtos criados por NF-e" : motivo.Trim());
            command.Parameters.AddWithValue("@TotalItens", resultado.TotalItens);
            command.Parameters.AddWithValue("@ProdutosRemovidos", resultado.ProdutosRemovidos);
            command.Parameters.AddWithValue("@ProdutosBloqueados", resultado.ProdutosBloqueados);
            command.Parameters.AddWithValue("@ProdutosIgnorados", resultado.ProdutosIgnorados);
            command.Parameters.AddWithValue("@AtualizacoesRevertidas", resultado.AtualizacoesRevertidas);
            command.Parameters.AddWithValue("@AtualizacoesIgnoradas", resultado.AtualizacoesIgnoradas);
            command.Parameters.AddWithValue("@Detalhes", CriarDetalhesRollback(resultado));
            command.ExecuteNonQuery();
        }

        private static string CriarDetalhesRollback(ImportacaoRollbackResult resultado)
        {
            var detalhes = new StringBuilder();
            foreach (var item in resultado.Itens)
            {
                detalhes.Append(item.Resultado)
                    .Append(" | ProdutoId=")
                    .Append(item.ProdutoId?.ToString() ?? "-")
                    .Append(" | Nome=")
                    .Append(item.ProdutoNome)
                    .Append(" | Motivo=")
                    .AppendLine(item.Motivo);
            }

            return detalhes.ToString();
        }

        private static void AdicionarResultado(
            ImportacaoRollbackResult resultado,
            ImportacaoRollbackItem item,
            Guid? produtoId,
            string status,
            string motivo)
        {
            resultado.Itens.Add(new ImportacaoRollbackItemResult
            {
                ProdutoId = produtoId,
                ProdutoNome = TextoOuPadrao(item.Nome),
                Resultado = status,
                Motivo = motivo
            });

            if (string.Equals(status, "Removido", StringComparison.OrdinalIgnoreCase))
            {
                resultado.ProdutosRemovidos++;
            }
            else if (string.Equals(status, "Restaurado", StringComparison.OrdinalIgnoreCase))
            {
                resultado.AtualizacoesRevertidas++;
            }
            else if (string.Equals(status, "Bloqueado", StringComparison.OrdinalIgnoreCase))
            {
                resultado.ProdutosBloqueados++;
            }
            else
            {
                resultado.ProdutosIgnorados++;
            }
        }

        private static bool TabelaExiste(SqliteConnection connection, SqliteTransaction transaction, string tabela)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT 1
                FROM sqlite_master
                WHERE type = 'table'
                  AND name = @Tabela
                LIMIT 1;";
            command.Parameters.AddWithValue("@Tabela", tabela);

            return command.ExecuteScalar() != null;
        }

        private static bool Contem(string texto, string busca)
        {
            return !string.IsNullOrWhiteSpace(texto) &&
                   !string.IsNullOrWhiteSpace(busca) &&
                   texto.Contains(busca, StringComparison.OrdinalIgnoreCase);
        }

        private static int ConverterQuantidade(decimal quantidade)
        {
            return quantidade <= 0
                ? 0
                : decimal.ToInt32(decimal.Round(quantidade, 0, MidpointRounding.AwayFromZero));
        }

        private static string TextoOuPadrao(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? "-" : valor.Trim();
        }

        private static object ToDbNullableString(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? DBNull.Value : valor.Trim();
        }

        private static object ToDbNullableDate(DateTime? valor)
        {
            return valor.HasValue
                ? valor.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                : DBNull.Value;
        }

        private static string ReadString(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? string.Empty : reader.GetString(index);
        }

        private static int ReadInt(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0 : Convert.ToInt32(reader.GetValue(index));
        }

        private static decimal ReadDecimal(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0 : Convert.ToDecimal(reader.GetValue(index), CultureInfo.InvariantCulture);
        }

        private static DateTime ReadDate(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) || !DateTime.TryParse(reader.GetString(index), CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
                ? DateTime.MinValue
                : value;
        }

        private static DateTime? ReadNullableDate(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) || !DateTime.TryParse(Convert.ToString(reader.GetValue(index), CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
                ? null
                : value;
        }

        private static Guid? ReadNullableGuid(SqliteDataReader reader, int index)
        {
            return reader.IsDBNull(index) || !Guid.TryParse(reader.GetString(index), out var value)
                ? null
                : value;
        }

        private sealed class ImportacaoRollbackCabecalho
        {
            public Guid Id { get; init; }
            public string ChaveAcesso { get; init; } = string.Empty;
            public string Numero { get; init; } = string.Empty;
            public string Serie { get; init; } = string.Empty;
            public string FornecedorNome { get; init; } = string.Empty;
            public string FornecedorCNPJ { get; init; } = string.Empty;
            public DateTime DataImportacao { get; init; }
        }

        private sealed class ImportacaoRollbackItem
        {
            public Guid? ProdutoId { get; init; }
            public string Codigo { get; init; } = string.Empty;
            public string Nome { get; init; } = string.Empty;
            public string CodigoBarras { get; init; } = string.Empty;
            public decimal Quantidade { get; init; }
            public string Status { get; init; } = string.Empty;
            public string AcaoPlanejada { get; init; } = string.Empty;
            public string ProdutoSnapshotAnterior { get; init; } = string.Empty;
            public string ProdutoSnapshotPosterior { get; init; } = string.Empty;
        }

        private sealed class ProdutoRollbackCandidate
        {
            public Guid Id { get; init; }
            public string Codigo { get; init; } = string.Empty;
            public string Nome { get; init; } = string.Empty;
            public string Descricao { get; init; } = string.Empty;
            public string Fornecedor { get; init; } = string.Empty;
            public int QuantidadeEstoque { get; init; }
            public int TotalVendas { get; init; }
            public DateTime? DataUltimaVenda { get; init; }
            public string Observacoes { get; init; } = string.Empty;
            public int RowVersion { get; init; }
            public bool Ambiguo { get; set; }
        }

        private static void EnsureColumnExists(SqliteConnection connection, string tableName, string columnName, string alterSql)
        {
            using var pragma = connection.CreateCommand();
            pragma.CommandText = $"PRAGMA table_info({tableName});";

            using var reader = pragma.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = alterSql;
            alterCommand.ExecuteNonQuery();
        }
    }
}
