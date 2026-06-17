using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PrimoAutoEletrica.Services
{
    public partial class DatabaseService
    {
        private void InitializeVeiculosSchema(DbConnection connection)
        {
            if (VeiculosSchemaPrecisaRecriar(connection))
            {
                var backupName = $"Veiculos_Legado_{DateTime.Now:yyyyMMddHHmmss}";
                using var renameCommand = connection.CreateCommand();
                renameCommand.CommandText = $"ALTER TABLE Veiculos RENAME TO {backupName};";
                renameCommand.ExecuteNonQuery();
            }

            using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Veiculos
                (
                    Id TEXT PRIMARY KEY,
                    ClienteId TEXT NOT NULL,
                    Marca TEXT,
                    Modelo TEXT,
                    Ano TEXT,
                    Cor TEXT,
                    Placa TEXT,
                    Chassi TEXT,
                    Renavam TEXT,
                    ImagemUrl TEXT,
                    DocumentoImagemUrl TEXT,
                    TipoVeiculo TEXT,
                    SistemaEletrico TEXT,
                    Motor TEXT,
                    Combustivel TEXT,
                    BateriaPrincipal TEXT,
                    BateriaAuxiliar TEXT,
                    BateriaInstalada TEXT,
                    BateriaMarca TEXT,
                    BateriaAmperagem TEXT,
                    BateriaDataInstalacao TEXT,
                    Alternador TEXT,
                    MotorPartida TEXT,
                    Quilometragem INTEGER NOT NULL DEFAULT 0,
                    TesteTensaoRepouso TEXT,
                    TesteTensaoPartida TEXT,
                    TesteCargaAlternador TEXT,
                    CorrenteFuga TEXT,
                    EstadoAterramentos TEXT,
                    ChicotesReparados TEXT,
                    FusiveisSubstituidos TEXT,
                    RelesSubstituidos TEXT,
                    LampadasSubstituidas TEXT,
                    AcessoriosInstalados TEXT,
                    ObservacoesTecnicasEletricas TEXT,
                    FotosTecnicas TEXT,
                    HistoricoTecnico TEXT,
                    ObservacoesEletricasRecorrentes TEXT,
                    ProblemaRecorrente TEXT,
                    ObservacaoImportanteTecnico TEXT,
                    RetornoRecomendadoEm TEXT,
                    GarantiaValidaAte TEXT,
                    ProximaRevisaoEm TEXT,
                    Observacoes TEXT
                );
            ";
            command.ExecuteNonQuery();

            EnsureColumnExists(connection, "Veiculos", "ClienteId", "ALTER TABLE Veiculos ADD COLUMN ClienteId TEXT;");
            EnsureColumnExists(connection, "Veiculos", "Marca", "ALTER TABLE Veiculos ADD COLUMN Marca TEXT;");
            EnsureColumnExists(connection, "Veiculos", "Modelo", "ALTER TABLE Veiculos ADD COLUMN Modelo TEXT;");
            EnsureColumnExists(connection, "Veiculos", "Ano", "ALTER TABLE Veiculos ADD COLUMN Ano TEXT;");
            EnsureColumnExists(connection, "Veiculos", "Cor", "ALTER TABLE Veiculos ADD COLUMN Cor TEXT;");
            EnsureColumnExists(connection, "Veiculos", "Placa", "ALTER TABLE Veiculos ADD COLUMN Placa TEXT;");
            EnsureColumnExists(connection, "Veiculos", "Chassi", "ALTER TABLE Veiculos ADD COLUMN Chassi TEXT;");
            EnsureColumnExists(connection, "Veiculos", "Renavam", "ALTER TABLE Veiculos ADD COLUMN Renavam TEXT;");
            EnsureColumnExists(connection, "Veiculos", "ImagemUrl", "ALTER TABLE Veiculos ADD COLUMN ImagemUrl TEXT;");
            EnsureColumnExists(connection, "Veiculos", "DocumentoImagemUrl", "ALTER TABLE Veiculos ADD COLUMN DocumentoImagemUrl TEXT;");
            EnsureColumnExists(connection, "Veiculos", "TipoVeiculo", "ALTER TABLE Veiculos ADD COLUMN TipoVeiculo TEXT;");
            EnsureColumnExists(connection, "Veiculos", "SistemaEletrico", "ALTER TABLE Veiculos ADD COLUMN SistemaEletrico TEXT;");
            EnsureColumnExists(connection, "Veiculos", "Motor", "ALTER TABLE Veiculos ADD COLUMN Motor TEXT;");
            EnsureColumnExists(connection, "Veiculos", "Combustivel", "ALTER TABLE Veiculos ADD COLUMN Combustivel TEXT;");
            EnsureColumnExists(connection, "Veiculos", "BateriaPrincipal", "ALTER TABLE Veiculos ADD COLUMN BateriaPrincipal TEXT;");
            EnsureColumnExists(connection, "Veiculos", "BateriaAuxiliar", "ALTER TABLE Veiculos ADD COLUMN BateriaAuxiliar TEXT;");
            EnsureColumnExists(connection, "Veiculos", "BateriaInstalada", "ALTER TABLE Veiculos ADD COLUMN BateriaInstalada TEXT;");
            EnsureColumnExists(connection, "Veiculos", "BateriaMarca", "ALTER TABLE Veiculos ADD COLUMN BateriaMarca TEXT;");
            EnsureColumnExists(connection, "Veiculos", "BateriaAmperagem", "ALTER TABLE Veiculos ADD COLUMN BateriaAmperagem TEXT;");
            EnsureColumnExists(connection, "Veiculos", "BateriaDataInstalacao", "ALTER TABLE Veiculos ADD COLUMN BateriaDataInstalacao TEXT;");
            EnsureColumnExists(connection, "Veiculos", "Alternador", "ALTER TABLE Veiculos ADD COLUMN Alternador TEXT;");
            EnsureColumnExists(connection, "Veiculos", "MotorPartida", "ALTER TABLE Veiculos ADD COLUMN MotorPartida TEXT;");
            EnsureColumnExists(connection, "Veiculos", "Quilometragem", "ALTER TABLE Veiculos ADD COLUMN Quilometragem INTEGER NOT NULL DEFAULT 0;");
            EnsureColumnExists(connection, "Veiculos", "TesteTensaoRepouso", "ALTER TABLE Veiculos ADD COLUMN TesteTensaoRepouso TEXT;");
            EnsureColumnExists(connection, "Veiculos", "TesteTensaoPartida", "ALTER TABLE Veiculos ADD COLUMN TesteTensaoPartida TEXT;");
            EnsureColumnExists(connection, "Veiculos", "TesteCargaAlternador", "ALTER TABLE Veiculos ADD COLUMN TesteCargaAlternador TEXT;");
            EnsureColumnExists(connection, "Veiculos", "CorrenteFuga", "ALTER TABLE Veiculos ADD COLUMN CorrenteFuga TEXT;");
            EnsureColumnExists(connection, "Veiculos", "EstadoAterramentos", "ALTER TABLE Veiculos ADD COLUMN EstadoAterramentos TEXT;");
            EnsureColumnExists(connection, "Veiculos", "ChicotesReparados", "ALTER TABLE Veiculos ADD COLUMN ChicotesReparados TEXT;");
            EnsureColumnExists(connection, "Veiculos", "FusiveisSubstituidos", "ALTER TABLE Veiculos ADD COLUMN FusiveisSubstituidos TEXT;");
            EnsureColumnExists(connection, "Veiculos", "RelesSubstituidos", "ALTER TABLE Veiculos ADD COLUMN RelesSubstituidos TEXT;");
            EnsureColumnExists(connection, "Veiculos", "LampadasSubstituidas", "ALTER TABLE Veiculos ADD COLUMN LampadasSubstituidas TEXT;");
            EnsureColumnExists(connection, "Veiculos", "AcessoriosInstalados", "ALTER TABLE Veiculos ADD COLUMN AcessoriosInstalados TEXT;");
            EnsureColumnExists(connection, "Veiculos", "ObservacoesTecnicasEletricas", "ALTER TABLE Veiculos ADD COLUMN ObservacoesTecnicasEletricas TEXT;");
            EnsureColumnExists(connection, "Veiculos", "FotosTecnicas", "ALTER TABLE Veiculos ADD COLUMN FotosTecnicas TEXT;");
            EnsureColumnExists(connection, "Veiculos", "HistoricoTecnico", "ALTER TABLE Veiculos ADD COLUMN HistoricoTecnico TEXT;");
            EnsureColumnExists(connection, "Veiculos", "ObservacoesEletricasRecorrentes", "ALTER TABLE Veiculos ADD COLUMN ObservacoesEletricasRecorrentes TEXT;");
            EnsureColumnExists(connection, "Veiculos", "ProblemaRecorrente", "ALTER TABLE Veiculos ADD COLUMN ProblemaRecorrente TEXT;");
            EnsureColumnExists(connection, "Veiculos", "ObservacaoImportanteTecnico", "ALTER TABLE Veiculos ADD COLUMN ObservacaoImportanteTecnico TEXT;");
            EnsureColumnExists(connection, "Veiculos", "RetornoRecomendadoEm", "ALTER TABLE Veiculos ADD COLUMN RetornoRecomendadoEm TEXT;");
            EnsureColumnExists(connection, "Veiculos", "GarantiaValidaAte", "ALTER TABLE Veiculos ADD COLUMN GarantiaValidaAte TEXT;");
            EnsureColumnExists(connection, "Veiculos", "ProximaRevisaoEm", "ALTER TABLE Veiculos ADD COLUMN ProximaRevisaoEm TEXT;");
            EnsureColumnExists(connection, "Veiculos", "Observacoes", "ALTER TABLE Veiculos ADD COLUMN Observacoes TEXT;");
        }

        private static bool VeiculosSchemaPrecisaRecriar(DbConnection connection)
        {
            using var pragma = connection.CreateCommand();
            pragma.CommandText = "PRAGMA table_info(Veiculos);";

            var colunas = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using var reader = pragma.ExecuteReader();
            while (reader.Read())
                colunas[reader.GetString(1)] = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);

            if (colunas.Count == 0)
                return false;

            return !colunas.TryGetValue("ClienteId", out _) ||
                   !colunas.TryGetValue("Id", out var tipoId) ||
                   !string.Equals(tipoId, "TEXT", StringComparison.OrdinalIgnoreCase);
        }

        private void InitializeOrdensServicoSchema(DbConnection connection)
        {
            using var ordensCommand = connection.CreateCommand();
            ordensCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS OrdensServico
                (
                    Id TEXT PRIMARY KEY,
                    Numero TEXT NOT NULL UNIQUE,
                    ClienteId TEXT NOT NULL,
                    VeiculoId TEXT,
                    TecnicoId INTEGER,
                    AgendamentoId TEXT,
                    ClienteNomeSnapshot TEXT,
                    TelefoneClienteSnapshot TEXT,
                    VeiculoDescricaoSnapshot TEXT,
                    PlacaSnapshot TEXT,
                    Status TEXT NOT NULL,
                    Prioridade TEXT NOT NULL,
                    Origem TEXT,
                    ProblemaRelatado TEXT,
                    Diagnostico TEXT,
                    DiagnosticoInicial TEXT,
                    DiagnosticoFinal TEXT,
                    ObservacoesInternas TEXT,
                    ObservacoesCliente TEXT,
                    ChecklistEntrada TEXT,
                    ChecklistEntrega TEXT,
                    ChecklistSaida TEXT,
                    FotosAntes TEXT,
                    FotosDepois TEXT,
                    GarantiaObservacoes TEXT,
                    TermoAutorizacao TEXT,
                    AssinaturaClienteUrl TEXT,
                    AprovadaCliente INTEGER NOT NULL DEFAULT 0,
                    MetodoAprovacao TEXT,
                    DataAbertura TEXT NOT NULL,
                    DataPrevisao TEXT,
                    DataAprovacao TEXT,
                    DataInicio TEXT,
                    DataConclusao TEXT,
                    DataEntrega TEXT,
                    GarantiaValidaAte TEXT,
                    TempoPrevistoMinutos INTEGER NOT NULL DEFAULT 0,
                    TempoRealMinutos INTEGER NOT NULL DEFAULT 0,
                    OrcamentoId TEXT,
                    ValorMaoObra REAL NOT NULL DEFAULT 0,
                    Desconto REAL NOT NULL DEFAULT 0,
                    Ativo INTEGER NOT NULL DEFAULT 1
                );
            ";
            ordensCommand.ExecuteNonQuery();

            EnsureColumnExists(connection, "OrdensServico", "AgendamentoId", "ALTER TABLE OrdensServico ADD COLUMN AgendamentoId TEXT;");
            EnsureColumnExists(connection, "OrdensServico", "ChecklistEntrada", "ALTER TABLE OrdensServico ADD COLUMN ChecklistEntrada TEXT;");
            EnsureColumnExists(connection, "OrdensServico", "ChecklistEntrega", "ALTER TABLE OrdensServico ADD COLUMN ChecklistEntrega TEXT;");
            EnsureColumnExists(connection, "OrdensServico", "ChecklistSaida", "ALTER TABLE OrdensServico ADD COLUMN ChecklistSaida TEXT;");
            EnsureColumnExists(connection, "OrdensServico", "FotosAntes", "ALTER TABLE OrdensServico ADD COLUMN FotosAntes TEXT;");
            EnsureColumnExists(connection, "OrdensServico", "FotosDepois", "ALTER TABLE OrdensServico ADD COLUMN FotosDepois TEXT;");
            EnsureColumnExists(connection, "OrdensServico", "DiagnosticoInicial", "ALTER TABLE OrdensServico ADD COLUMN DiagnosticoInicial TEXT;");
            EnsureColumnExists(connection, "OrdensServico", "DiagnosticoFinal", "ALTER TABLE OrdensServico ADD COLUMN DiagnosticoFinal TEXT;");
            EnsureColumnExists(connection, "OrdensServico", "GarantiaObservacoes", "ALTER TABLE OrdensServico ADD COLUMN GarantiaObservacoes TEXT;");
            EnsureColumnExists(connection, "OrdensServico", "TermoAutorizacao", "ALTER TABLE OrdensServico ADD COLUMN TermoAutorizacao TEXT;");
            EnsureColumnExists(connection, "OrdensServico", "GarantiaValidaAte", "ALTER TABLE OrdensServico ADD COLUMN GarantiaValidaAte TEXT;");
            EnsureColumnExists(connection, "OrdensServico", "AssinaturaClienteUrl", "ALTER TABLE OrdensServico ADD COLUMN AssinaturaClienteUrl TEXT;");
            EnsureColumnExists(connection, "OrdensServico", "TempoPrevistoMinutos", "ALTER TABLE OrdensServico ADD COLUMN TempoPrevistoMinutos INTEGER NOT NULL DEFAULT 0;");
            EnsureColumnExists(connection, "OrdensServico", "TempoRealMinutos", "ALTER TABLE OrdensServico ADD COLUMN TempoRealMinutos INTEGER NOT NULL DEFAULT 0;");
            EnsureColumnExists(connection, "OrdensServico", "OrcamentoId", "ALTER TABLE OrdensServico ADD COLUMN OrcamentoId TEXT;");

            using var itensCommand = connection.CreateCommand();
            itensCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS OrdemServicoItens
                (
                    Id TEXT PRIMARY KEY,
                    OrdemServicoId TEXT NOT NULL,
                    ProdutoId TEXT,
                    Tipo TEXT NOT NULL,
                    Descricao TEXT NOT NULL,
                    Quantidade REAL NOT NULL DEFAULT 1,
                    ValorUnitario REAL NOT NULL DEFAULT 0,
                    CustoUnitario REAL NOT NULL DEFAULT 0,
                    Observacoes TEXT,
                    OrdemExibicao INTEGER NOT NULL DEFAULT 0,
                    EstoqueMovimentado INTEGER NOT NULL DEFAULT 0
                );
            ";
            itensCommand.ExecuteNonQuery();

            using var eventosCommand = connection.CreateCommand();
            eventosCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS OrdemServicoEventos
                (
                    Id TEXT PRIMARY KEY,
                    OrdemServicoId TEXT NOT NULL,
                    DataEvento TEXT NOT NULL,
                    Titulo TEXT NOT NULL,
                    Descricao TEXT,
                    Tipo TEXT,
                    Usuario TEXT
                );
            ";
            eventosCommand.ExecuteNonQuery();

            EnsureColumnExists(
                connection,
                "OrdemServicoItens",
                "EstoqueMovimentado",
                "ALTER TABLE OrdemServicoItens ADD COLUMN EstoqueMovimentado INTEGER NOT NULL DEFAULT 0;");
        }

        private static void EnsureColumnExists(
            DbConnection connection,
            string tableName,
            string columnName,
            string alterSql)
        {
            using var pragma = connection.CreateCommand();
            pragma.CommandText = $"PRAGMA table_info({tableName});";

            using var reader = pragma.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = alterSql;
            alterCommand.ExecuteNonQuery();
        }

        public List<Funcionario> ObterFuncionarios(bool somenteAtivos = true)
        {
            return global::PrimoAutoEletrica.App.Repositories.Funcionarios.ObterTodos(somenteAtivos);
        }

        public List<Funcionario> ObterTodosFuncionarios()
        {
            return global::PrimoAutoEletrica.App.Repositories.Funcionarios.ObterTodos(false);
        }

        public List<Veiculo> ObterVeiculosPorClienteId(Guid clienteId)
        {
            if (UsarClienteRepositoryBridge())
                return global::PrimoAutoEletrica.App.Repositories.Clientes.ObterVeiculosPorClienteId(clienteId);

            using var connection = GetConnection();
            connection.Open();
            return ObterVeiculosPorClienteId(clienteId, connection, null);
        }

        public List<Veiculo> ObterTodosVeiculos()
        {
            if (UsarClienteRepositoryBridge())
                return global::PrimoAutoEletrica.App.Repositories.Clientes.ObterTodosVeiculos();

            using var connection = GetConnection();
            connection.Open();
            
            var veiculos = new List<Veiculo>();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
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
                    Observacoes,
                    BateriaInstalada,
                    BateriaMarca,
                    BateriaAmperagem,
                    BateriaDataInstalacao,
                    TesteTensaoRepouso,
                    TesteTensaoPartida,
                    TesteCargaAlternador,
                    CorrenteFuga,
                    EstadoAterramentos,
                    ChicotesReparados,
                    FusiveisSubstituidos,
                    RelesSubstituidos,
                    LampadasSubstituidas,
                    AcessoriosInstalados,
                    ObservacoesTecnicasEletricas,
                    FotosTecnicas
                FROM Veiculos
                ORDER BY Marca, Modelo, Placa;
            ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                veiculos.Add(new Veiculo
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
                    Quilometragem = ReadInt32(reader, 19),
                    HistoricoTecnico = ReadString(reader, 20),
                    ObservacoesEletricasRecorrentes = ReadString(reader, 21),
                    ProblemaRecorrente = ReadString(reader, 22),
                    ObservacaoImportanteTecnico = ReadString(reader, 23),
                    RetornoRecomendadoEm = ReadDateTime(reader, 24),
                    GarantiaValidaAte = ReadDateTime(reader, 25),
                    ProximaRevisaoEm = ReadDateTime(reader, 26),
                    Observacoes = ReadString(reader, 27),
                    BateriaInstalada = ReadString(reader, 28),
                    BateriaMarca = ReadString(reader, 29),
                    BateriaAmperagem = ReadString(reader, 30),
                    BateriaDataInstalacao = ReadDateTime(reader, 31),
                    TesteTensaoRepouso = ReadString(reader, 32),
                    TesteTensaoPartida = ReadString(reader, 33),
                    TesteCargaAlternador = ReadString(reader, 34),
                    CorrenteFuga = ReadString(reader, 35),
                    EstadoAterramentos = ReadString(reader, 36),
                    ChicotesReparados = ReadString(reader, 37),
                    FusiveisSubstituidos = ReadString(reader, 38),
                    RelesSubstituidos = ReadString(reader, 39),
                    LampadasSubstituidas = ReadString(reader, 40),
                    AcessoriosInstalados = ReadString(reader, 41),
                    ObservacoesTecnicasEletricas = ReadString(reader, 42),
                    FotosTecnicas = ReadString(reader, 43)
                });
            }

            return veiculos;
        }

        public void ExcluirVeiculo(Guid veiculoId)
        {
            if (UsarClienteRepositoryBridge())
            {
                global::PrimoAutoEletrica.App.Repositories.Clientes.ExcluirVeiculo(veiculoId);
                return;
            }

            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM Veiculos WHERE Id = @Id;";
            command.Parameters.AddWithValue("@Id", veiculoId.ToString());
            command.ExecuteNonQuery();
            
            transaction.Commit();
        }

        public void SalvarVeiculo(Veiculo veiculo)
        {
            if (UsarClienteRepositoryBridge())
            {
                global::PrimoAutoEletrica.App.Repositories.Clientes.SalvarVeiculo(veiculo);
                return;
            }

            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            
            if (veiculo.Id == Guid.Empty)
                veiculo.Id = Guid.NewGuid();
            
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                INSERT OR REPLACE INTO Veiculos
                (
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
                    Observacoes,
                    BateriaInstalada,
                    BateriaMarca,
                    BateriaAmperagem,
                    BateriaDataInstalacao,
                    TesteTensaoRepouso,
                    TesteTensaoPartida,
                    TesteCargaAlternador,
                    CorrenteFuga,
                    EstadoAterramentos,
                    ChicotesReparados,
                    FusiveisSubstituidos,
                    RelesSubstituidos,
                    LampadasSubstituidas,
                    AcessoriosInstalados,
                    ObservacoesTecnicasEletricas,
                    FotosTecnicas
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
                    @Observacoes,
                    @BateriaInstalada,
                    @BateriaMarca,
                    @BateriaAmperagem,
                    @BateriaDataInstalacao,
                    @TesteTensaoRepouso,
                    @TesteTensaoPartida,
                    @TesteCargaAlternador,
                    @CorrenteFuga,
                    @EstadoAterramentos,
                    @ChicotesReparados,
                    @FusiveisSubstituidos,
                    @RelesSubstituidos,
                    @LampadasSubstituidas,
                    @AcessoriosInstalados,
                    @ObservacoesTecnicasEletricas,
                    @FotosTecnicas
                );
            ";
            
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
            command.Parameters.AddWithValue("@BateriaInstalada", ToDbNullableString(veiculo.BateriaInstalada));
            command.Parameters.AddWithValue("@BateriaMarca", ToDbNullableString(veiculo.BateriaMarca));
            command.Parameters.AddWithValue("@BateriaAmperagem", ToDbNullableString(veiculo.BateriaAmperagem));
            command.Parameters.AddWithValue("@BateriaDataInstalacao", ToDbNullableDate(veiculo.BateriaDataInstalacao, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@TesteTensaoRepouso", ToDbNullableString(veiculo.TesteTensaoRepouso));
            command.Parameters.AddWithValue("@TesteTensaoPartida", ToDbNullableString(veiculo.TesteTensaoPartida));
            command.Parameters.AddWithValue("@TesteCargaAlternador", ToDbNullableString(veiculo.TesteCargaAlternador));
            command.Parameters.AddWithValue("@CorrenteFuga", ToDbNullableString(veiculo.CorrenteFuga));
            command.Parameters.AddWithValue("@EstadoAterramentos", ToDbNullableString(veiculo.EstadoAterramentos));
            command.Parameters.AddWithValue("@ChicotesReparados", ToDbNullableString(veiculo.ChicotesReparados));
            command.Parameters.AddWithValue("@FusiveisSubstituidos", ToDbNullableString(veiculo.FusiveisSubstituidos));
            command.Parameters.AddWithValue("@RelesSubstituidos", ToDbNullableString(veiculo.RelesSubstituidos));
            command.Parameters.AddWithValue("@LampadasSubstituidas", ToDbNullableString(veiculo.LampadasSubstituidas));
            command.Parameters.AddWithValue("@AcessoriosInstalados", ToDbNullableString(veiculo.AcessoriosInstalados));
            command.Parameters.AddWithValue("@ObservacoesTecnicasEletricas", ToDbNullableString(veiculo.ObservacoesTecnicasEletricas));
            command.Parameters.AddWithValue("@FotosTecnicas", ToDbNullableString(veiculo.FotosTecnicas));
            command.Parameters.AddWithValue("@HistoricoTecnico", ToDbNullableString(veiculo.HistoricoTecnico));
            command.Parameters.AddWithValue("@ObservacoesEletricasRecorrentes", ToDbNullableString(veiculo.ObservacoesEletricasRecorrentes));
            command.Parameters.AddWithValue("@ProblemaRecorrente", ToDbNullableString(veiculo.ProblemaRecorrente));
            command.Parameters.AddWithValue("@ObservacaoImportanteTecnico", ToDbNullableString(veiculo.ObservacaoImportanteTecnico));
            command.Parameters.AddWithValue("@RetornoRecomendadoEm", ToDbNullableDate(veiculo.RetornoRecomendadoEm, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@GarantiaValidaAte", ToDbNullableDate(veiculo.GarantiaValidaAte, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@ProximaRevisaoEm", ToDbNullableDate(veiculo.ProximaRevisaoEm, "yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@Observacoes", ToDbNullableString(veiculo.Observacoes));
            
            command.ExecuteNonQuery();
            transaction.Commit();
        }

        private List<Veiculo> ObterVeiculosPorClienteId(
            Guid clienteId,
            DbConnection connection,
            DbTransaction? transaction)
        {
            var veiculos = new List<Veiculo>();

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT
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
                    Observacoes,
                    BateriaInstalada,
                    BateriaMarca,
                    BateriaAmperagem,
                    BateriaDataInstalacao,
                    TesteTensaoRepouso,
                    TesteTensaoPartida,
                    TesteCargaAlternador,
                    CorrenteFuga,
                    EstadoAterramentos,
                    ChicotesReparados,
                    FusiveisSubstituidos,
                    RelesSubstituidos,
                    LampadasSubstituidas,
                    AcessoriosInstalados,
                    ObservacoesTecnicasEletricas,
                    FotosTecnicas
                FROM Veiculos
                WHERE ClienteId = @ClienteId
                ORDER BY Marca, Modelo, Placa;
            ";
            command.Parameters.AddWithValue("@ClienteId", clienteId.ToString());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                veiculos.Add(new Veiculo
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
                    Quilometragem = ReadInt32(reader, 19),
                    HistoricoTecnico = ReadString(reader, 20),
                    ObservacoesEletricasRecorrentes = ReadString(reader, 21),
                    ProblemaRecorrente = ReadString(reader, 22),
                    ObservacaoImportanteTecnico = ReadString(reader, 23),
                    RetornoRecomendadoEm = ReadDateTime(reader, 24),
                    GarantiaValidaAte = ReadDateTime(reader, 25),
                    ProximaRevisaoEm = ReadDateTime(reader, 26),
                    Observacoes = ReadString(reader, 27),
                    BateriaInstalada = ReadString(reader, 28),
                    BateriaMarca = ReadString(reader, 29),
                    BateriaAmperagem = ReadString(reader, 30),
                    BateriaDataInstalacao = ReadDateTime(reader, 31),
                    TesteTensaoRepouso = ReadString(reader, 32),
                    TesteTensaoPartida = ReadString(reader, 33),
                    TesteCargaAlternador = ReadString(reader, 34),
                    CorrenteFuga = ReadString(reader, 35),
                    EstadoAterramentos = ReadString(reader, 36),
                    ChicotesReparados = ReadString(reader, 37),
                    FusiveisSubstituidos = ReadString(reader, 38),
                    RelesSubstituidos = ReadString(reader, 39),
                    LampadasSubstituidas = ReadString(reader, 40),
                    AcessoriosInstalados = ReadString(reader, 41),
                    ObservacoesTecnicasEletricas = ReadString(reader, 42),
                    FotosTecnicas = ReadString(reader, 43)
                });
            }

            return veiculos;
        }

        public void SalvarVeiculosDoCliente(Guid clienteId, IEnumerable<Veiculo> veiculos)
        {
            if (UsarClienteRepositoryBridge())
            {
                global::PrimoAutoEletrica.App.Repositories.Clientes.SalvarVeiculosDoCliente(clienteId, veiculos);
                return;
            }

            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();
            SalvarVeiculosDoCliente(clienteId, veiculos, connection, transaction);
            transaction.Commit();
        }

        private void SalvarVeiculosDoCliente(
            Guid clienteId,
            IEnumerable<Veiculo> veiculos,
            DbConnection connection,
            DbTransaction? transaction)
        {
            ExcluirVeiculosDoCliente(clienteId, connection, transaction);

            foreach (var veiculo in veiculos ?? Enumerable.Empty<Veiculo>())
            {
                if (veiculo == null)
                    continue;

                if (veiculo.Id == Guid.Empty)
                    veiculo.Id = Guid.NewGuid();

                veiculo.ClienteId = clienteId;

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    INSERT INTO Veiculos
                    (
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
                        Observacoes,
                        BateriaInstalada,
                        BateriaMarca,
                        BateriaAmperagem,
                        BateriaDataInstalacao,
                        TesteTensaoRepouso,
                        TesteTensaoPartida,
                        TesteCargaAlternador,
                        CorrenteFuga,
                        EstadoAterramentos,
                        ChicotesReparados,
                        FusiveisSubstituidos,
                        RelesSubstituidos,
                        LampadasSubstituidas,
                        AcessoriosInstalados,
                        ObservacoesTecnicasEletricas,
                        FotosTecnicas
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
                        @Observacoes,
                        @BateriaInstalada,
                        @BateriaMarca,
                        @BateriaAmperagem,
                        @BateriaDataInstalacao,
                        @TesteTensaoRepouso,
                        @TesteTensaoPartida,
                        @TesteCargaAlternador,
                        @CorrenteFuga,
                        @EstadoAterramentos,
                        @ChicotesReparados,
                        @FusiveisSubstituidos,
                        @RelesSubstituidos,
                        @LampadasSubstituidas,
                        @AcessoriosInstalados,
                        @ObservacoesTecnicasEletricas,
                        @FotosTecnicas
                    );
                ";

                command.Parameters.AddWithValue("@Id", veiculo.Id.ToString());
                command.Parameters.AddWithValue("@ClienteId", clienteId.ToString());
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
                command.Parameters.AddWithValue("@BateriaInstalada", ToDbNullableString(veiculo.BateriaInstalada));
                command.Parameters.AddWithValue("@BateriaMarca", ToDbNullableString(veiculo.BateriaMarca));
                command.Parameters.AddWithValue("@BateriaAmperagem", ToDbNullableString(veiculo.BateriaAmperagem));
                command.Parameters.AddWithValue("@BateriaDataInstalacao", ToDbNullableDate(veiculo.BateriaDataInstalacao, "yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@TesteTensaoRepouso", ToDbNullableString(veiculo.TesteTensaoRepouso));
                command.Parameters.AddWithValue("@TesteTensaoPartida", ToDbNullableString(veiculo.TesteTensaoPartida));
                command.Parameters.AddWithValue("@TesteCargaAlternador", ToDbNullableString(veiculo.TesteCargaAlternador));
                command.Parameters.AddWithValue("@CorrenteFuga", ToDbNullableString(veiculo.CorrenteFuga));
                command.Parameters.AddWithValue("@EstadoAterramentos", ToDbNullableString(veiculo.EstadoAterramentos));
                command.Parameters.AddWithValue("@ChicotesReparados", ToDbNullableString(veiculo.ChicotesReparados));
                command.Parameters.AddWithValue("@FusiveisSubstituidos", ToDbNullableString(veiculo.FusiveisSubstituidos));
                command.Parameters.AddWithValue("@RelesSubstituidos", ToDbNullableString(veiculo.RelesSubstituidos));
                command.Parameters.AddWithValue("@LampadasSubstituidas", ToDbNullableString(veiculo.LampadasSubstituidas));
                command.Parameters.AddWithValue("@AcessoriosInstalados", ToDbNullableString(veiculo.AcessoriosInstalados));
                command.Parameters.AddWithValue("@ObservacoesTecnicasEletricas", ToDbNullableString(veiculo.ObservacoesTecnicasEletricas));
                command.Parameters.AddWithValue("@FotosTecnicas", ToDbNullableString(veiculo.FotosTecnicas));
                command.Parameters.AddWithValue("@HistoricoTecnico", ToDbNullableString(veiculo.HistoricoTecnico));
                command.Parameters.AddWithValue("@ObservacoesEletricasRecorrentes", ToDbNullableString(veiculo.ObservacoesEletricasRecorrentes));
                command.Parameters.AddWithValue("@ProblemaRecorrente", ToDbNullableString(veiculo.ProblemaRecorrente));
                command.Parameters.AddWithValue("@ObservacaoImportanteTecnico", ToDbNullableString(veiculo.ObservacaoImportanteTecnico));
                command.Parameters.AddWithValue("@RetornoRecomendadoEm", ToDbNullableDate(veiculo.RetornoRecomendadoEm, "yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@GarantiaValidaAte", ToDbNullableDate(veiculo.GarantiaValidaAte, "yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@ProximaRevisaoEm", ToDbNullableDate(veiculo.ProximaRevisaoEm, "yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@Observacoes", ToDbNullableString(veiculo.Observacoes));

                command.ExecuteNonQuery();
            }
        }

        private void ExcluirVeiculosDoCliente(
            Guid clienteId,
            DbConnection connection,
            DbTransaction? transaction)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM Veiculos WHERE ClienteId = @ClienteId;";
            command.Parameters.AddWithValue("@ClienteId", clienteId.ToString());
            command.ExecuteNonQuery();
        }

        private bool ClientePossuiOrdensServico(
            DbConnection connection,
            DbTransaction? transaction,
            Guid clienteId)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT COUNT(*) FROM OrdensServico WHERE ClienteId = @ClienteId;";
            command.Parameters.AddWithValue("@ClienteId", clienteId.ToString());
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
        }

        public string GerarProximoNumeroOrdemServico()
        {
            if (UsarOrdemServicoRepositoryBridge())
                return global::PrimoAutoEletrica.App.Repositories.OrdensServico.GerarProximoNumero();

            using var connection = GetConnection();
            connection.Open();

            var policy = SystemConfigurationService.ResolveOrdemServicoNumberingPolicy(connection, DateTime.Now);
            var prefixo = policy.Prefix;

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Numero
                FROM OrdensServico
                WHERE Numero LIKE @Prefixo || '%'
                ORDER BY Numero DESC
                LIMIT 1;
            ";
            command.Parameters.AddWithValue("@Prefixo", prefixo);

            var ultimoNumero = command.ExecuteScalar() as string;
            if (string.IsNullOrWhiteSpace(ultimoNumero))
                return $"{prefixo}{policy.NextNumber:0000}";

            var sufixo = ultimoNumero.Substring(prefixo.Length);
            if (!int.TryParse(sufixo, out var sequencia))
                sequencia = 0;

            return $"{prefixo}{Math.Max(sequencia + 1, policy.NextNumber):0000}";
        }

        public List<OrdemServico> ObterOrdensServico(bool incluirInativas = false)
        {
            if (UsarOrdemServicoRepositoryBridge())
                return global::PrimoAutoEletrica.App.Repositories.OrdensServico.ObterTodos(incluirInativas);

            using var connection = GetConnection();
            connection.Open();
            return ObterOrdensServico(connection, null, null, incluirInativas);
        }

        public List<OrdemServico> ObterOrdensServicoPorClienteId(Guid clienteId, bool incluirInativas = false)
        {
            if (UsarOrdemServicoRepositoryBridge())
                return global::PrimoAutoEletrica.App.Repositories.OrdensServico.ObterPorClienteId(clienteId, incluirInativas);

            using var connection = GetConnection();
            connection.Open();
            return ObterOrdensServico(connection, null, clienteId, incluirInativas);
        }

        public OrdemServico? ObterOrdemServicoPorId(Guid id)
        {
            if (UsarOrdemServicoRepositoryBridge())
                return global::PrimoAutoEletrica.App.Repositories.OrdensServico.ObterPorId(id);

            using var connection = GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    Id,
                    Numero,
                    ClienteId,
                    VeiculoId,
                    TecnicoId,
                    ClienteNomeSnapshot,
                    TelefoneClienteSnapshot,
                    VeiculoDescricaoSnapshot,
                    PlacaSnapshot,
                    Status,
                    Prioridade,
                    Origem,
                    ProblemaRelatado,
                    Diagnostico,
                    ObservacoesInternas,
                    ObservacoesCliente,
                    TermoAutorizacao,
                    AprovadaCliente,
                    MetodoAprovacao,
                    DataAbertura,
                    DataPrevisao,
                    DataAprovacao,
                    DataInicio,
                    DataConclusao,
                    DataEntrega,
                    ValorMaoObra,
                    Desconto,
                    Ativo
                FROM OrdensServico
                WHERE Id = @Id
                LIMIT 1;
            ";
            command.Parameters.AddWithValue("@Id", id.ToString());

            using var reader = command.ExecuteReader();
            if (!reader.Read())
                return null;

            var ordem = MapOrdemServico(reader);
            ordem.Itens = ObterItensDaOrdem(ordem.Id, connection, null);
            ordem.Eventos = ObterEventosDaOrdem(ordem.Id, connection, null);
            return ordem;
        }

        public void InserirOrdemServico(OrdemServico ordem)
        {
            if (UsarOrdemServicoRepositoryBridge())
            {
                global::PrimoAutoEletrica.App.Repositories.OrdensServico.Inserir(ordem);
                return;
            }

            SalvarOrdemServico(ordem, true);
        }

        public void AtualizarOrdemServico(OrdemServico ordem)
        {
            if (UsarOrdemServicoRepositoryBridge())
            {
                global::PrimoAutoEletrica.App.Repositories.OrdensServico.Atualizar(ordem);
                return;
            }

            SalvarOrdemServico(ordem, false);
        }

        public void ExcluirOrdemServico(Guid id)
        {
            if (UsarOrdemServicoRepositoryBridge())
            {
                global::PrimoAutoEletrica.App.Repositories.OrdensServico.Excluir(id);
                return;
            }

            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            Guid? clienteId = null;

            using (var selectCommand = connection.CreateCommand())
            {
                selectCommand.Transaction = transaction;
                selectCommand.CommandText = "SELECT ClienteId FROM OrdensServico WHERE Id = @Id LIMIT 1;";
                selectCommand.Parameters.AddWithValue("@Id", id.ToString());

                var value = selectCommand.ExecuteScalar() as string;
                if (!string.IsNullOrWhiteSpace(value))
                    clienteId = Guid.Parse(value);
            }

            var itensMovimentados = new List<OrdemServicoItem>();

            using (var itensCommand = connection.CreateCommand())
            {
                itensCommand.Transaction = transaction;
                itensCommand.CommandText = @"
                    SELECT
                        Id,
                        OrdemServicoId,
                        ProdutoId,
                        Tipo,
                        Descricao,
                        Quantidade,
                        ValorUnitario,
                        CustoUnitario,
                        Observacoes,
                        OrdemExibicao,
                        EstoqueMovimentado
                    FROM OrdemServicoItens
                    WHERE OrdemServicoId = @Id;
                ";
                itensCommand.Parameters.AddWithValue("@Id", id.ToString());

                using var reader = itensCommand.ExecuteReader();
                while (reader.Read())
                {
                    itensMovimentados.Add(new OrdemServicoItem
                    {
                        Id = ReadGuid(reader, 0),
                        OrdemServicoId = ReadGuid(reader, 1),
                        ProdutoId = ReadNullableGuid(reader, 2),
                        Tipo = ReadString(reader, 3),
                        Descricao = ReadString(reader, 4),
                        Quantidade = ReadDecimal(reader, 5),
                        ValorUnitario = ReadDecimal(reader, 6),
                        CustoUnitario = ReadDecimal(reader, 7),
                        Observacoes = ReadString(reader, 8),
                        OrdemExibicao = ReadInt32(reader, 9),
                        EstoqueMovimentado = ReadBool(reader, 10)
                    });
                }
            }

            RestaurarEstoqueDaOrdemExcluida(itensMovimentados, connection, transaction);

            using (var eventosCommand = connection.CreateCommand())
            {
                eventosCommand.Transaction = transaction;
                eventosCommand.CommandText = "DELETE FROM OrdemServicoEventos WHERE OrdemServicoId = @Id;";
                eventosCommand.Parameters.AddWithValue("@Id", id.ToString());
                eventosCommand.ExecuteNonQuery();
            }

            using (var itensCommand = connection.CreateCommand())
            {
                itensCommand.Transaction = transaction;
                itensCommand.CommandText = "DELETE FROM OrdemServicoItens WHERE OrdemServicoId = @Id;";
                itensCommand.Parameters.AddWithValue("@Id", id.ToString());
                itensCommand.ExecuteNonQuery();
            }

            using (var ordemCommand = connection.CreateCommand())
            {
                ordemCommand.Transaction = transaction;
                ordemCommand.CommandText = "DELETE FROM OrdensServico WHERE Id = @Id;";
                ordemCommand.Parameters.AddWithValue("@Id", id.ToString());
                ordemCommand.ExecuteNonQuery();
            }

            if (clienteId.HasValue)
                AtualizarResumoCliente(connection, transaction, clienteId.Value);

            transaction.Commit();
        }

        public List<HistoricoServico> ObterHistoricoServicosPorClienteId(Guid clienteId)
        {
            var historico = new List<HistoricoServico>();
            var funcionarios = ObterFuncionarios(false).ToDictionary(f => f.Id, f => f.Nome);
            var ordens = ObterOrdensServicoPorClienteId(clienteId, true)
                .Where(o => string.Equals(o.Status, "Entregue", StringComparison.OrdinalIgnoreCase) || o.DataEntrega.HasValue)
                .OrderByDescending(o => o.DataEntrega ?? o.DataConclusao ?? o.DataAbertura)
                .ToList();

            foreach (var ordem in ordens)
            {
                var servicos = ordem.Itens
                    .Where(i => string.Equals(i.Tipo, "Servico", StringComparison.OrdinalIgnoreCase))
                    .Select(i => i.Descricao)
                    .Where(d => !string.IsNullOrWhiteSpace(d))
                    .ToList();

                var descricao = servicos.Count > 0
                    ? string.Join(" | ", servicos)
                    : (string.IsNullOrWhiteSpace(ordem.ProblemaRelatado) ? ordem.Numero : ordem.ProblemaRelatado);

                historico.Add(new HistoricoServico
                {
                    Id = ordem.Id,
                    DataServico = ordem.DataEntrega ?? ordem.DataConclusao ?? ordem.DataAbertura,
                    Descricao = descricao,
                    Valor = CalcularTotalOrdem(ordem),
                    TecnicoResponsavel = ordem.TecnicoId.HasValue && funcionarios.TryGetValue(ordem.TecnicoId.Value, out var nomeTecnico)
                        ? nomeTecnico
                        : "Equipe interna",
                    Observacoes = string.IsNullOrWhiteSpace(ordem.Diagnostico)
                        ? ordem.ObservacoesCliente
                        : ordem.Diagnostico
                });
            }

            return historico;
        }

        private List<OrdemServico> ObterOrdensServico(
            DbConnection connection,
            DbTransaction? transaction,
            Guid? clienteId,
            bool incluirInativas)
        {
            var ordens = new List<OrdemServico>();

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT
                    Id,
                    Numero,
                    ClienteId,
                    VeiculoId,
                    TecnicoId,
                    ClienteNomeSnapshot,
                    TelefoneClienteSnapshot,
                    VeiculoDescricaoSnapshot,
                    PlacaSnapshot,
                    Status,
                    Prioridade,
                    Origem,
                    ProblemaRelatado,
                    Diagnostico,
                    ObservacoesInternas,
                    ObservacoesCliente,
                    TermoAutorizacao,
                    AprovadaCliente,
                    MetodoAprovacao,
                    DataAbertura,
                    DataPrevisao,
                    DataAprovacao,
                    DataInicio,
                    DataConclusao,
                    DataEntrega,
                    ValorMaoObra,
                    Desconto,
                    Ativo
                FROM OrdensServico
                WHERE (@ClienteId IS NULL OR ClienteId = @ClienteId)
                  AND (@IncluirInativas = 1 OR Ativo = 1)
                ORDER BY DataAbertura DESC, Numero DESC;
            ";
            command.Parameters.AddWithValue("@ClienteId", clienteId?.ToString() ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@IncluirInativas", incluirInativas ? 1 : 0);

            using var reader = command.ExecuteReader();
            while (reader.Read())
                ordens.Add(MapOrdemServico(reader));

            foreach (var ordem in ordens)
            {
                ordem.Itens = ObterItensDaOrdem(ordem.Id, connection, transaction);
                ordem.Eventos = ObterEventosDaOrdem(ordem.Id, connection, transaction);
            }

            return ordens;
        }

        private OrdemServico MapOrdemServico(DbDataReader reader)
        {
            return new OrdemServico
            {
                Id = ReadGuid(reader, 0),
                Numero = ReadString(reader, 1),
                ClienteId = ReadGuid(reader, 2),
                VeiculoId = ReadNullableGuid(reader, 3),
                TecnicoId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                ClienteNomeSnapshot = ReadString(reader, 5),
                TelefoneClienteSnapshot = ReadString(reader, 6),
                VeiculoDescricaoSnapshot = ReadString(reader, 7),
                PlacaSnapshot = ReadString(reader, 8),
                Status = ReadString(reader, 9),
                Prioridade = ReadString(reader, 10),
                Origem = ReadString(reader, 11),
                ProblemaRelatado = ReadString(reader, 12),
                Diagnostico = ReadString(reader, 13),
                ObservacoesInternas = ReadString(reader, 14),
                ObservacoesCliente = ReadString(reader, 15),
                TermoAutorizacao = ReadString(reader, 16),
                AprovadaCliente = ReadBool(reader, 17),
                MetodoAprovacao = ReadString(reader, 18),
                DataAbertura = ReadDateTime(reader, 19) ?? DateTime.Now,
                DataPrevisao = ReadDateTime(reader, 20),
                DataAprovacao = ReadDateTime(reader, 21),
                DataInicio = ReadDateTime(reader, 22),
                DataConclusao = ReadDateTime(reader, 23),
                DataEntrega = ReadDateTime(reader, 24),
                ValorMaoObra = ReadDecimal(reader, 25),
                Desconto = ReadDecimal(reader, 26),
                Ativo = ReadBool(reader, 27)
            };
        }

        private List<OrdemServicoItem> ObterItensDaOrdem(
            Guid ordemId,
            DbConnection connection,
            DbTransaction? transaction)
        {
            var itens = new List<OrdemServicoItem>();

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT
                    Id,
                    OrdemServicoId,
                    ProdutoId,
                    Tipo,
                    Descricao,
                    Quantidade,
                    ValorUnitario,
                    CustoUnitario,
                    Observacoes,
                    OrdemExibicao,
                    EstoqueMovimentado
                FROM OrdemServicoItens
                WHERE OrdemServicoId = @OrdemServicoId
                ORDER BY OrdemExibicao, Descricao;
            ";
            command.Parameters.AddWithValue("@OrdemServicoId", ordemId.ToString());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                itens.Add(new OrdemServicoItem
                {
                    Id = ReadGuid(reader, 0),
                    OrdemServicoId = ReadGuid(reader, 1),
                    ProdutoId = ReadNullableGuid(reader, 2),
                    Tipo = ReadString(reader, 3),
                    Descricao = ReadString(reader, 4),
                    Quantidade = ReadDecimal(reader, 5),
                    ValorUnitario = ReadDecimal(reader, 6),
                    CustoUnitario = ReadDecimal(reader, 7),
                    Observacoes = ReadString(reader, 8),
                    OrdemExibicao = ReadInt32(reader, 9),
                    EstoqueMovimentado = ReadBool(reader, 10)
                });
            }

            return itens;
        }

        private List<OrdemServicoEvento> ObterEventosDaOrdem(
            Guid ordemId,
            DbConnection connection,
            DbTransaction? transaction)
        {
            var eventos = new List<OrdemServicoEvento>();

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT
                    Id,
                    OrdemServicoId,
                    DataEvento,
                    Titulo,
                    Descricao,
                    Tipo,
                    Usuario
                FROM OrdemServicoEventos
                WHERE OrdemServicoId = @OrdemServicoId
                ORDER BY DataEvento DESC;
            ";
            command.Parameters.AddWithValue("@OrdemServicoId", ordemId.ToString());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                eventos.Add(new OrdemServicoEvento
                {
                    Id = ReadGuid(reader, 0),
                    OrdemServicoId = ReadGuid(reader, 1),
                    DataEvento = ReadDateTime(reader, 2) ?? DateTime.Now,
                    Titulo = ReadString(reader, 3),
                    Descricao = ReadString(reader, 4),
                    Tipo = ReadString(reader, 5),
                    Usuario = ReadString(reader, 6)
                });
            }

            return eventos;
        }

        private void SalvarOrdemServico(OrdemServico ordem, bool inserir)
        {
            if (ordem == null)
                throw new ArgumentNullException(nameof(ordem));

            NormalizarOrdemServico(ordem, inserir);

            using var connection = GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = inserir
                    ? @"
                        INSERT INTO OrdensServico
                        (
                            Id,
                            Numero,
                            ClienteId,
                            VeiculoId,
                            TecnicoId,
                            ClienteNomeSnapshot,
                            TelefoneClienteSnapshot,
                            VeiculoDescricaoSnapshot,
                            PlacaSnapshot,
                            Status,
                            Prioridade,
                            Origem,
                            ProblemaRelatado,
                            Diagnostico,
                            ObservacoesInternas,
                            ObservacoesCliente,
                            TermoAutorizacao,
                            AprovadaCliente,
                            MetodoAprovacao,
                            DataAbertura,
                            DataPrevisao,
                            DataAprovacao,
                            DataInicio,
                            DataConclusao,
                            DataEntrega,
                            ValorMaoObra,
                            Desconto,
                            Ativo
                        )
                        VALUES
                        (
                            @Id,
                            @Numero,
                            @ClienteId,
                            @VeiculoId,
                            @TecnicoId,
                            @ClienteNomeSnapshot,
                            @TelefoneClienteSnapshot,
                            @VeiculoDescricaoSnapshot,
                            @PlacaSnapshot,
                            @Status,
                            @Prioridade,
                            @Origem,
                            @ProblemaRelatado,
                            @Diagnostico,
                            @ObservacoesInternas,
                            @ObservacoesCliente,
                            @TermoAutorizacao,
                            @AprovadaCliente,
                            @MetodoAprovacao,
                            @DataAbertura,
                            @DataPrevisao,
                            @DataAprovacao,
                            @DataInicio,
                            @DataConclusao,
                            @DataEntrega,
                            @ValorMaoObra,
                            @Desconto,
                            @Ativo
                        );"
                    : @"
                        UPDATE OrdensServico
                        SET
                            Numero = @Numero,
                            ClienteId = @ClienteId,
                            VeiculoId = @VeiculoId,
                            TecnicoId = @TecnicoId,
                            ClienteNomeSnapshot = @ClienteNomeSnapshot,
                            TelefoneClienteSnapshot = @TelefoneClienteSnapshot,
                            VeiculoDescricaoSnapshot = @VeiculoDescricaoSnapshot,
                            PlacaSnapshot = @PlacaSnapshot,
                            Status = @Status,
                            Prioridade = @Prioridade,
                            Origem = @Origem,
                            ProblemaRelatado = @ProblemaRelatado,
                            Diagnostico = @Diagnostico,
                            ObservacoesInternas = @ObservacoesInternas,
                            ObservacoesCliente = @ObservacoesCliente,
                            TermoAutorizacao = @TermoAutorizacao,
                            AprovadaCliente = @AprovadaCliente,
                            MetodoAprovacao = @MetodoAprovacao,
                            DataAbertura = @DataAbertura,
                            DataPrevisao = @DataPrevisao,
                            DataAprovacao = @DataAprovacao,
                            DataInicio = @DataInicio,
                            DataConclusao = @DataConclusao,
                            DataEntrega = @DataEntrega,
                            ValorMaoObra = @ValorMaoObra,
                            Desconto = @Desconto,
                            Ativo = @Ativo
                        WHERE Id = @Id;";

                command.Parameters.AddWithValue("@Id", ordem.Id.ToString());
                command.Parameters.AddWithValue("@Numero", ordem.Numero);
                command.Parameters.AddWithValue("@ClienteId", ordem.ClienteId.ToString());
                command.Parameters.AddWithValue("@VeiculoId", ordem.VeiculoId?.ToString() ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@TecnicoId", ordem.TecnicoId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ClienteNomeSnapshot", ToDbNullableString(ordem.ClienteNomeSnapshot));
                command.Parameters.AddWithValue("@TelefoneClienteSnapshot", ToDbNullableString(ordem.TelefoneClienteSnapshot));
                command.Parameters.AddWithValue("@VeiculoDescricaoSnapshot", ToDbNullableString(ordem.VeiculoDescricaoSnapshot));
                command.Parameters.AddWithValue("@PlacaSnapshot", ToDbNullableString(ordem.PlacaSnapshot));
                command.Parameters.AddWithValue("@Status", ordem.Status);
                command.Parameters.AddWithValue("@Prioridade", ordem.Prioridade);
                command.Parameters.AddWithValue("@Origem", ToDbNullableString(ordem.Origem));
                command.Parameters.AddWithValue("@ProblemaRelatado", ToDbNullableString(ordem.ProblemaRelatado));
                command.Parameters.AddWithValue("@Diagnostico", ToDbNullableString(ordem.Diagnostico));
                command.Parameters.AddWithValue("@ObservacoesInternas", ToDbNullableString(ordem.ObservacoesInternas));
                command.Parameters.AddWithValue("@ObservacoesCliente", ToDbNullableString(ordem.ObservacoesCliente));
                command.Parameters.AddWithValue("@TermoAutorizacao", ToDbNullableString(ordem.TermoAutorizacao));
                command.Parameters.AddWithValue("@AprovadaCliente", ordem.AprovadaCliente ? 1 : 0);
                command.Parameters.AddWithValue("@MetodoAprovacao", ToDbNullableString(ordem.MetodoAprovacao));
                command.Parameters.AddWithValue("@DataAbertura", ordem.DataAbertura.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("@DataPrevisao", ToDbNullableDate(ordem.DataPrevisao, "yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@DataAprovacao", ToDbNullableDate(ordem.DataAprovacao, "yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@DataInicio", ToDbNullableDate(ordem.DataInicio, "yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@DataConclusao", ToDbNullableDate(ordem.DataConclusao, "yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@DataEntrega", ToDbNullableDate(ordem.DataEntrega, "yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@ValorMaoObra", ordem.ValorMaoObra);
                command.Parameters.AddWithValue("@Desconto", ordem.Desconto);
                command.Parameters.AddWithValue("@Ativo", ordem.Ativo ? 1 : 0);

                command.ExecuteNonQuery();
            }

            SalvarItensDaOrdem(ordem, connection, transaction);
            SalvarEventosDaOrdem(ordem, connection, transaction);

            if (string.Equals(ordem.Status, "Entregue", StringComparison.OrdinalIgnoreCase))
                AplicarBaixaEstoqueSeNecessaria(ordem, connection, transaction);

            AtualizarResumoCliente(connection, transaction, ordem.ClienteId);
            transaction.Commit();
        }

        private void NormalizarOrdemServico(OrdemServico ordem, bool inserir)
        {
            if (ordem.Id == Guid.Empty)
                ordem.Id = Guid.NewGuid();

            if (ordem.ClienteId == Guid.Empty)
                throw new InvalidOperationException("Selecione um cliente para a ordem de servico.");

            ordem.Numero = string.IsNullOrWhiteSpace(ordem.Numero)
                ? GerarProximoNumeroOrdemServico()
                : ordem.Numero.Trim();

            ordem.Status = string.IsNullOrWhiteSpace(ordem.Status) ? "Rascunho" : ordem.Status.Trim();
            ordem.Prioridade = string.IsNullOrWhiteSpace(ordem.Prioridade) ? "Normal" : ordem.Prioridade.Trim();
            ordem.Origem = string.IsNullOrWhiteSpace(ordem.Origem) ? "Balcao" : ordem.Origem.Trim();
            ordem.TermoAutorizacao = ordem.TermoAutorizacao?.Trim() ?? string.Empty;

            ordem.DataAbertura = ordem.DataAbertura == default ? DateTime.Now : ordem.DataAbertura;

            foreach (var item in ordem.Itens)
            {
                if (item.Id == Guid.Empty)
                    item.Id = Guid.NewGuid();

                item.OrdemServicoId = ordem.Id;
                item.Tipo = string.IsNullOrWhiteSpace(item.Tipo) ? "Servico" : item.Tipo.Trim();
                item.Descricao = item.Descricao?.Trim() ?? string.Empty;
                item.Observacoes = item.Observacoes?.Trim() ?? string.Empty;
                item.Quantidade = item.Quantidade <= 0 ? 1 : item.Quantidade;
                item.ValorUnitario = Math.Max(0, item.ValorUnitario);
                item.CustoUnitario = Math.Max(0, item.CustoUnitario);
            }

            ordem.ValorMaoObra = ordem.Itens
                .Where(i => string.Equals(i.Tipo, "Servico", StringComparison.OrdinalIgnoreCase))
                .Sum(i => i.Total);

            ordem.Desconto = Math.Max(0, ordem.Desconto);

            if (ordem.AprovadaCliente && !ordem.DataAprovacao.HasValue)
                ordem.DataAprovacao = DateTime.Now;

            if (StatusImplicaInicio(ordem.Status) && !ordem.DataInicio.HasValue)
                ordem.DataInicio = DateTime.Now;

            if (StatusImplicaConclusao(ordem.Status) && !ordem.DataConclusao.HasValue)
                ordem.DataConclusao = DateTime.Now;

            if (string.Equals(ordem.Status, "Entregue", StringComparison.OrdinalIgnoreCase) && !ordem.DataEntrega.HasValue)
                ordem.DataEntrega = DateTime.Now;

            ordem.Eventos ??= new List<OrdemServicoEvento>();
            ordem.Itens ??= new List<OrdemServicoItem>();

            if (inserir && !ordem.Eventos.Any())
            {
                ordem.Eventos.Add(new OrdemServicoEvento
                {
                    OrdemServicoId = ordem.Id,
                    DataEvento = DateTime.Now,
                    Titulo = "OS criada",
                    Descricao = "Ordem de servico registrada no sistema.",
                    Tipo = "Cadastro",
                    Usuario = "Sistema"
                });
            }
        }

        private void SalvarItensDaOrdem(
            OrdemServico ordem,
            DbConnection connection,
            DbTransaction transaction)
        {
            var estoquePorItemId = new Dictionary<Guid, bool>();

            using (var selectCommand = connection.CreateCommand())
            {
                selectCommand.Transaction = transaction;
                selectCommand.CommandText = @"
                    SELECT Id, EstoqueMovimentado
                    FROM OrdemServicoItens
                    WHERE OrdemServicoId = @OrdemServicoId;
                ";
                selectCommand.Parameters.AddWithValue("@OrdemServicoId", ordem.Id.ToString());

                using var reader = selectCommand.ExecuteReader();
                while (reader.Read())
                    estoquePorItemId[Guid.Parse(reader.GetString(0))] = ReadBool(reader, 1);
            }

            var itemIds = ordem.Itens.Select(i => i.Id.ToString()).ToList();

            using (var deleteCommand = connection.CreateCommand())
            {
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = itemIds.Count == 0
                    ? "DELETE FROM OrdemServicoItens WHERE OrdemServicoId = @OrdemServicoId;"
                    : $@"
                        DELETE FROM OrdemServicoItens
                        WHERE OrdemServicoId = @OrdemServicoId
                          AND Id NOT IN ({string.Join(", ", itemIds.Select((_, index) => $"@ItemId{index}"))});
                    ";
                deleteCommand.Parameters.AddWithValue("@OrdemServicoId", ordem.Id.ToString());

                for (var index = 0; index < itemIds.Count; index++)
                    deleteCommand.Parameters.AddWithValue($"@ItemId{index}", itemIds[index]);

                deleteCommand.ExecuteNonQuery();
            }

            for (var index = 0; index < ordem.Itens.Count; index++)
            {
                var item = ordem.Itens[index];
                item.OrdemExibicao = index + 1;

                if (estoquePorItemId.TryGetValue(item.Id, out var estoqueMovimentado))
                    item.EstoqueMovimentado = item.EstoqueMovimentado || estoqueMovimentado;

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    INSERT INTO OrdemServicoItens
                    (
                        Id,
                        OrdemServicoId,
                        ProdutoId,
                        Tipo,
                        Descricao,
                        Quantidade,
                        ValorUnitario,
                        CustoUnitario,
                        Observacoes,
                        OrdemExibicao,
                        EstoqueMovimentado
                    )
                    VALUES
                    (
                        @Id,
                        @OrdemServicoId,
                        @ProdutoId,
                        @Tipo,
                        @Descricao,
                        @Quantidade,
                        @ValorUnitario,
                        @CustoUnitario,
                        @Observacoes,
                        @OrdemExibicao,
                        @EstoqueMovimentado
                    )
                    ON CONFLICT(Id) DO UPDATE SET
                        OrdemServicoId = excluded.OrdemServicoId,
                        ProdutoId = excluded.ProdutoId,
                        Tipo = excluded.Tipo,
                        Descricao = excluded.Descricao,
                        Quantidade = excluded.Quantidade,
                        ValorUnitario = excluded.ValorUnitario,
                        CustoUnitario = excluded.CustoUnitario,
                        Observacoes = excluded.Observacoes,
                        OrdemExibicao = excluded.OrdemExibicao,
                        EstoqueMovimentado = excluded.EstoqueMovimentado;
                ";

                command.Parameters.AddWithValue("@Id", item.Id.ToString());
                command.Parameters.AddWithValue("@OrdemServicoId", ordem.Id.ToString());
                command.Parameters.AddWithValue("@ProdutoId", item.ProdutoId?.ToString() ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Tipo", item.Tipo);
                command.Parameters.AddWithValue("@Descricao", item.Descricao);
                command.Parameters.AddWithValue("@Quantidade", item.Quantidade);
                command.Parameters.AddWithValue("@ValorUnitario", item.ValorUnitario);
                command.Parameters.AddWithValue("@CustoUnitario", item.CustoUnitario);
                command.Parameters.AddWithValue("@Observacoes", ToDbNullableString(item.Observacoes));
                command.Parameters.AddWithValue("@OrdemExibicao", item.OrdemExibicao);
                command.Parameters.AddWithValue("@EstoqueMovimentado", item.EstoqueMovimentado ? 1 : 0);

                command.ExecuteNonQuery();
            }
        }

        private void SalvarEventosDaOrdem(
            OrdemServico ordem,
            DbConnection connection,
            DbTransaction transaction)
        {
            using (var deleteCommand = connection.CreateCommand())
            {
                deleteCommand.Transaction = transaction;
                deleteCommand.CommandText = "DELETE FROM OrdemServicoEventos WHERE OrdemServicoId = @OrdemServicoId;";
                deleteCommand.Parameters.AddWithValue("@OrdemServicoId", ordem.Id.ToString());
                deleteCommand.ExecuteNonQuery();
            }

            foreach (var evento in ordem.Eventos.OrderBy(e => e.DataEvento))
            {
                if (evento.Id == Guid.Empty)
                    evento.Id = Guid.NewGuid();

                evento.OrdemServicoId = ordem.Id;

                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    INSERT INTO OrdemServicoEventos
                    (
                        Id,
                        OrdemServicoId,
                        DataEvento,
                        Titulo,
                        Descricao,
                        Tipo,
                        Usuario
                    )
                    VALUES
                    (
                        @Id,
                        @OrdemServicoId,
                        @DataEvento,
                        @Titulo,
                        @Descricao,
                        @Tipo,
                        @Usuario
                    );
                ";

                command.Parameters.AddWithValue("@Id", evento.Id.ToString());
                command.Parameters.AddWithValue("@OrdemServicoId", ordem.Id.ToString());
                command.Parameters.AddWithValue("@DataEvento", evento.DataEvento.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("@Titulo", evento.Titulo);
                command.Parameters.AddWithValue("@Descricao", ToDbNullableString(evento.Descricao));
                command.Parameters.AddWithValue("@Tipo", ToDbNullableString(evento.Tipo));
                command.Parameters.AddWithValue("@Usuario", ToDbNullableString(evento.Usuario));

                command.ExecuteNonQuery();
            }
        }

        private void AplicarBaixaEstoqueSeNecessaria(
            OrdemServico ordem,
            DbConnection connection,
            DbTransaction transaction)
        {
            var itensPendentes = ordem.Itens
                .Where(i =>
                    !i.EstoqueMovimentado &&
                    i.ProdutoId.HasValue &&
                    string.Equals(i.Tipo, "Peca", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var item in itensPendentes)
            {
                var quantidade = ValidarQuantidadeInteira(item);

                using (var selectCommand = connection.CreateCommand())
                {
                    selectCommand.Transaction = transaction;
                    selectCommand.CommandText = @"
                        SELECT Nome, QuantidadeEstoque
                        FROM Produtos
                        WHERE Id = @ProdutoId
                        LIMIT 1;
                    ";
                    selectCommand.Parameters.AddWithValue("@ProdutoId", item.ProdutoId!.Value.ToString());

                    using var reader = selectCommand.ExecuteReader();
                    if (!reader.Read())
                        throw new InvalidOperationException($"O produto vinculado ao item '{item.Descricao}' nao foi encontrado.");

                    var nomeProduto = ReadString(reader, 0);
                    var estoqueAtual = ReadInt32(reader, 1);

                    if (estoqueAtual < quantidade)
                    {
                        throw new InvalidOperationException(
                            $"Estoque insuficiente para '{nomeProduto}'. Disponivel: {estoqueAtual}. Necessario: {quantidade}.");
                    }
                }

                using (var updateCommand = connection.CreateCommand())
                {
                    updateCommand.Transaction = transaction;
                    updateCommand.CommandText = @"
                        UPDATE Produtos
                        SET
                            QuantidadeEstoque = QuantidadeEstoque - @Quantidade,
                            ValorTotalEstoque = (QuantidadeEstoque - @Quantidade) * PrecoCompra,
                            DataUltimaAtualizacao = @DataAtualizacao
                        WHERE Id = @ProdutoId;
                    ";
                    updateCommand.Parameters.AddWithValue("@Quantidade", quantidade);
                    updateCommand.Parameters.AddWithValue("@DataAtualizacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                    updateCommand.Parameters.AddWithValue("@ProdutoId", item.ProdutoId!.Value.ToString());
                    updateCommand.ExecuteNonQuery();
                }

                item.EstoqueMovimentado = true;

                using var itemCommand = connection.CreateCommand();
                itemCommand.Transaction = transaction;
                itemCommand.CommandText = @"
                    UPDATE OrdemServicoItens
                    SET EstoqueMovimentado = 1
                    WHERE Id = @Id;
                ";
                itemCommand.Parameters.AddWithValue("@Id", item.Id.ToString());
                itemCommand.ExecuteNonQuery();
            }
        }

        private void RestaurarEstoqueDaOrdemExcluida(
            IEnumerable<OrdemServicoItem> itens,
            DbConnection connection,
            DbTransaction transaction)
        {
            foreach (var item in itens.Where(i =>
                         i.EstoqueMovimentado &&
                         i.ProdutoId.HasValue &&
                         string.Equals(i.Tipo, "Peca", StringComparison.OrdinalIgnoreCase)))
            {
                var quantidade = ValidarQuantidadeInteira(item);

                using var updateCommand = connection.CreateCommand();
                updateCommand.Transaction = transaction;
                updateCommand.CommandText = @"
                    UPDATE Produtos
                    SET
                        QuantidadeEstoque = QuantidadeEstoque + @Quantidade,
                        ValorTotalEstoque = (QuantidadeEstoque + @Quantidade) * PrecoCompra,
                        DataUltimaAtualizacao = @DataAtualizacao
                    WHERE Id = @ProdutoId;
                ";
                updateCommand.Parameters.AddWithValue("@Quantidade", quantidade);
                updateCommand.Parameters.AddWithValue("@DataAtualizacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                updateCommand.Parameters.AddWithValue("@ProdutoId", item.ProdutoId!.Value.ToString());
                updateCommand.ExecuteNonQuery();
            }
        }

        private void AtualizarResumoCliente(
            DbConnection connection,
            DbTransaction? transaction,
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
                      AND (os.Status = 'Entregue' OR os.DataEntrega IS NOT NULL);
                ";
                command.Parameters.AddWithValue("@ClienteId", clienteId.ToString());

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    totalGasto = ReadDecimal(reader, 0);
                    totalServicos = ReadInt32(reader, 1);
                    ultimaVisita = ReadDateTime(reader, 2);
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
                    UltimaVisita = @UltimaVisita
                WHERE Id = @Id;
            ";
            updateCommand.Parameters.AddWithValue("@TotalGasto", totalGasto);
            updateCommand.Parameters.AddWithValue("@TotalServicos", totalServicos);
            updateCommand.Parameters.AddWithValue("@PontosFidelidade", Math.Max(0, (int)Math.Floor(totalGasto / 10m)));
            updateCommand.Parameters.AddWithValue("@UltimaVisita", ToDbNullableDate(ultimaVisita, "yyyy-MM-dd HH:mm:ss"));
            updateCommand.Parameters.AddWithValue("@Id", clienteId.ToString());
            updateCommand.ExecuteNonQuery();
        }

        private static bool StatusImplicaInicio(string status)
        {
            return string.Equals(status, "Aprovada", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Em diagnostico", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Em execucao", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Aguardando peca", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Finalizada", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Aguardando pagamento", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Pronta para entrega", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Entregue", StringComparison.OrdinalIgnoreCase);
        }

        private static bool StatusImplicaConclusao(string status)
        {
            return string.Equals(status, "Pronta para entrega", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Finalizada", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Aguardando pagamento", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "Entregue", StringComparison.OrdinalIgnoreCase);
        }

        private static int ValidarQuantidadeInteira(OrdemServicoItem item)
        {
            if (item.Quantidade <= 0)
                throw new InvalidOperationException($"O item '{item.Descricao}' precisa ter quantidade maior que zero.");

            if (decimal.Truncate(item.Quantidade) != item.Quantidade)
            {
                throw new InvalidOperationException(
                    $"O item '{item.Descricao}' usa produto de estoque e precisa de quantidade inteira.");
            }

            return decimal.ToInt32(item.Quantidade);
        }

        private static decimal CalcularTotalOrdem(OrdemServico ordem)
        {
            var totalItens = ordem.Itens.Sum(i => i.Total);
            return Math.Max(0, totalItens - ordem.Desconto);
        }

        private static Guid ReadGuid(DbDataReader reader, int index)
        {
            return Guid.Parse(reader.GetString(index));
        }

        private static Guid? ReadNullableGuid(DbDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return null;

            var value = reader.GetString(index);
            return string.IsNullOrWhiteSpace(value) ? null : Guid.Parse(value);
        }

        private static string ReadString(DbDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? string.Empty : reader.GetString(index);
        }

        private static bool ReadBool(DbDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return false;

            return reader.GetBoolean(index);
        }

        private static int ReadInt32(DbDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return 0;

            return Convert.ToInt32(reader.GetValue(index), CultureInfo.InvariantCulture);
        }

        private static decimal ReadDecimal(DbDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return 0m;

            return Convert.ToDecimal(reader.GetValue(index), CultureInfo.InvariantCulture);
        }

        private static DateTime? ReadDateTime(DbDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return null;

            var value = reader.GetValue(index)?.ToString();
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
                ? result
                : null;
        }
    }
}
