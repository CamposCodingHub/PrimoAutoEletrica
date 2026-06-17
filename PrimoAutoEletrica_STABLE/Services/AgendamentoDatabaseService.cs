using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PrimoAutoEletrica.Services
{
    public class AgendamentoDatabaseService
    {
        private readonly DatabaseService _databaseService;
        private readonly EstoqueOperationalService _estoqueOperationalService;

        public AgendamentoDatabaseService()
        {
            _databaseService = global::PrimoAutoEletrica.App.Database;
            _estoqueOperationalService = new EstoqueOperationalService(_databaseService, global::PrimoAutoEletrica.App.Logger);
            InicializarTabelas();
        }

        private DbConnection GetConnection()
        {
            return _databaseService.GetConnection();
        }

        private void InicializarTabelas()
        {
            using var connection = GetConnection();
            connection.Open();

            if (IsSqlServerConnection(connection))
            {
                // O SQL Server e provisionado pelo schema mestre.
                return;
            }

            // Tabela de Agendamentos
            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Agendamentos (
                    Id TEXT PRIMARY KEY,
                    Numero TEXT,
                    DataCriacao TEXT,
                    DataAgendamento TEXT,
                    HoraInicio TEXT,
                    HoraTermino TEXT,
                    DuracaoEstimada TEXT,
                    DuracaoReal TEXT,
                    Status TEXT,
                    Prioridade TEXT,
                    TipoServico TEXT,
                    CategoriaServico TEXT,
                    DescricaoServico TEXT,
                    Observacoes TEXT,
                    ClienteId TEXT,
                    ClienteNome TEXT,
                    ClienteTelefone TEXT,
                    ClienteEmail TEXT,
                    ClienteDocumento TEXT,
                    ClienteVip INTEGER,
                    ClienteTotalGasto REAL,
                    ClienteAtendimentos INTEGER,
                    ClienteUltimaVisita TEXT,
                    VeiculoId TEXT,
                    VeiculoPlaca TEXT,
                    VeiculoModelo TEXT,
                    VeiculoMarca TEXT,
                    VeiculoAno TEXT,
                    VeiculoCor TEXT,
                    VeiculoCombustivel TEXT,
                    VeiculoQuilometragem INTEGER,
                    VeiculoObservacoes TEXT,
                    TecnicoId TEXT,
                    TecnicoNome TEXT,
                    TecnicoEspecialidade TEXT,
                    TecnicoAtivo INTEGER,
                    OrdemServicoId TEXT,
                    NumeroOS TEXT,
                    DataInicioOS TEXT,
                    DataConclusaoOS TEXT,
                    ValorEstimado REAL,
                    ValorReal REAL,
                    ValorPago REAL,
                    FormaPagamento TEXT,
                    Pago INTEGER,
                    DataPagamento TEXT,
                    CheckIn TEXT,
                    CheckOut TEXT,
                    CheckInObservacoes TEXT,
                    CheckOutObservacoes TEXT,
                    CheckInFotos TEXT,
                    CheckOutFotos TEXT,
                    ValorProdutos REAL,
                    ValorServicos REAL,
                    Recorrente INTEGER,
                    TipoRecorrencia TEXT,
                    IntervaloRecorrencia INTEGER,
                    ProximaRecorrencia TEXT,
                    LembreteWhatsApp INTEGER,
                    LembreteEmail INTEGER,
                    DataLembrete TEXT,
                    LembreteEnviado INTEGER,
                    AlertaAtraso INTEGER,
                    AlertaPecaFaltando INTEGER,
                    AlertaPronto INTEGER,
                    AvaliacaoCliente INTEGER,
                    AvaliacaoComentario TEXT,
                    DataCancelamento TEXT,
                    MotivoCancelamento TEXT,
                    CanceladoPor TEXT,
                    DataReagendamento TEXT,
                    DataAgendamentoAnterior TEXT,
                    MotivoReagendamento TEXT
                )";
            command.ExecuteNonQuery();

            // Tabela de AgendamentoProdutos
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS AgendamentoProdutos (
                    Id TEXT PRIMARY KEY,
                    AgendamentoId TEXT,
                    ProdutoId TEXT,
                    ProdutoNome TEXT,
                    ProdutoCodigo TEXT,
                    Quantidade INTEGER,
                    PrecoUnitario REAL,
                    PrecoTotal REAL,
                    Reservado INTEGER,
                    DataReserva TEXT,
                    FOREIGN KEY (AgendamentoId) REFERENCES Agendamentos(Id)
                )";
            command.ExecuteNonQuery();

            // Tabela de AgendamentoServicos
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS AgendamentoServicos (
                    Id TEXT PRIMARY KEY,
                    AgendamentoId TEXT,
                    Nome TEXT,
                    Categoria TEXT,
                    Valor REAL,
                    TempoEstimado TEXT,
                    TempoReal TEXT,
                    TecnicoResponsavel TEXT,
                    Status TEXT,
                    Observacoes TEXT,
                    Concluido INTEGER,
                    DataConclusao TEXT,
                    FOREIGN KEY (AgendamentoId) REFERENCES Agendamentos(Id)
                )";
            command.ExecuteNonQuery();

            // Tabela de AgendamentoTimeline
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS AgendamentoTimeline (
                    Id TEXT PRIMARY KEY,
                    AgendamentoId TEXT,
                    DataHora TEXT,
                    Usuario TEXT,
                    Acao TEXT,
                    Detalhes TEXT,
                    TipoAlteracao TEXT,
                    FOREIGN KEY (AgendamentoId) REFERENCES Agendamentos(Id)
                )";
            command.ExecuteNonQuery();

            // Tabela de AlertasAgendamento
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS AlertasAgendamento (
                    Id TEXT PRIMARY KEY,
                    Tipo TEXT,
                    Mensagem TEXT,
                    Severidade TEXT,
                    DataGeracao TEXT,
                    Lido INTEGER,
                    Origem TEXT,
                    AgendamentoId TEXT
                )";
            command.ExecuteNonQuery();

            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS AgendamentoIntegracoes (
                    Id TEXT PRIMARY KEY,
                    AgendamentoId TEXT NOT NULL,
                    Tipo TEXT NOT NULL,
                    Detalhes TEXT,
                    DataCriacao TEXT NOT NULL,
                    UNIQUE(AgendamentoId, Tipo)
                )";
            command.ExecuteNonQuery();
        }

        public void AdicionarAgendamento(Agendamento agendamento)
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Agendamentos (
                    Id, Numero, DataCriacao, DataAgendamento, HoraInicio, HoraTermino,
                    DuracaoEstimada, DuracaoReal, Status, Prioridade, TipoServico, CategoriaServico,
                    DescricaoServico, Observacoes, ClienteId, ClienteNome, ClienteTelefone, ClienteEmail,
                    ClienteDocumento, ClienteVip, ClienteTotalGasto, ClienteAtendimentos, ClienteUltimaVisita,
                    VeiculoId, VeiculoPlaca, VeiculoModelo, VeiculoMarca, VeiculoAno, VeiculoCor,
                    VeiculoCombustivel, VeiculoQuilometragem, VeiculoObservacoes, TecnicoId, TecnicoNome,
                    TecnicoEspecialidade, TecnicoAtivo, OrdemServicoId, NumeroOS, DataInicioOS, DataConclusaoOS,
                    ValorEstimado, ValorReal, ValorPago, FormaPagamento, Pago, DataPagamento, CheckIn, CheckOut,
                    CheckInObservacoes, CheckOutObservacoes, CheckInFotos, CheckOutFotos, ValorProdutos, ValorServicos,
                    Recorrente, TipoRecorrencia, IntervaloRecorrencia, ProximaRecorrencia, LembreteWhatsApp,
                    LembreteEmail, DataLembrete, LembreteEnviado, AlertaAtraso, AlertaPecaFaltando, AlertaPronto,
                    AvaliacaoCliente, AvaliacaoComentario
                )
                VALUES (
                    @Id, @Numero, @DataCriacao, @DataAgendamento, @HoraInicio, @HoraTermino,
                    @DuracaoEstimada, @DuracaoReal, @Status, @Prioridade, @TipoServico, @CategoriaServico,
                    @DescricaoServico, @Observacoes, @ClienteId, @ClienteNome, @ClienteTelefone, @ClienteEmail,
                    @ClienteDocumento, @ClienteVip, @ClienteTotalGasto, @ClienteAtendimentos, @ClienteUltimaVisita,
                    @VeiculoId, @VeiculoPlaca, @VeiculoModelo, @VeiculoMarca, @VeiculoAno, @VeiculoCor,
                    @VeiculoCombustivel, @VeiculoQuilometragem, @VeiculoObservacoes, @TecnicoId, @TecnicoNome,
                    @TecnicoEspecialidade, @TecnicoAtivo, @OrdemServicoId, @NumeroOS, @DataInicioOS, @DataConclusaoOS,
                    @ValorEstimado, @ValorReal, @ValorPago, @FormaPagamento, @Pago, @DataPagamento, @CheckIn, @CheckOut,
                    @CheckInObservacoes, @CheckOutObservacoes, @CheckInFotos, @CheckOutFotos, @ValorProdutos, @ValorServicos,
                    @Recorrente, @TipoRecorrencia, @IntervaloRecorrencia, @ProximaRecorrencia, @LembreteWhatsApp,
                    @LembreteEmail, @DataLembrete, @LembreteEnviado, @AlertaAtraso, @AlertaPecaFaltando, @AlertaPronto,
                    @AvaliacaoCliente, @AvaliacaoComentario
                )";

            command.Parameters.AddWithValue("@Id", agendamento.Id.ToString());
            command.Parameters.AddWithValue("@Numero", agendamento.Numero);
            command.Parameters.AddWithValue("@DataCriacao", agendamento.DataCriacao.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@DataAgendamento", agendamento.DataAgendamento.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@HoraInicio", agendamento.HoraInicio?.ToString("HH:mm") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@HoraTermino", agendamento.HoraTermino?.ToString("HH:mm") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@DuracaoEstimada", agendamento.DuracaoEstimada.ToString());
            command.Parameters.AddWithValue("@DuracaoReal", agendamento.DuracaoReal.ToString());
            command.Parameters.AddWithValue("@Status", agendamento.Status);
            command.Parameters.AddWithValue("@Prioridade", agendamento.Prioridade);
            command.Parameters.AddWithValue("@TipoServico", agendamento.TipoServico);
            command.Parameters.AddWithValue("@CategoriaServico", agendamento.CategoriaServico);
            command.Parameters.AddWithValue("@DescricaoServico", agendamento.DescricaoServico);
            command.Parameters.AddWithValue("@Observacoes", agendamento.Observacoes);
            command.Parameters.AddWithValue("@ClienteId", agendamento.ClienteId.ToString());
            command.Parameters.AddWithValue("@ClienteNome", agendamento.ClienteNome);
            command.Parameters.AddWithValue("@ClienteTelefone", agendamento.ClienteTelefone);
            command.Parameters.AddWithValue("@ClienteEmail", agendamento.ClienteEmail);
            command.Parameters.AddWithValue("@ClienteDocumento", agendamento.ClienteDocumento);
            command.Parameters.AddWithValue("@ClienteVip", agendamento.ClienteVip ? 1 : 0);
            command.Parameters.AddWithValue("@ClienteTotalGasto", agendamento.ClienteTotalGasto);
            command.Parameters.AddWithValue("@ClienteAtendimentos", agendamento.ClienteAtendimentos);
            command.Parameters.AddWithValue("@ClienteUltimaVisita", agendamento.ClienteUltimaVisita?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@VeiculoId", agendamento.VeiculoId.ToString());
            command.Parameters.AddWithValue("@VeiculoPlaca", agendamento.VeiculoPlaca);
            command.Parameters.AddWithValue("@VeiculoModelo", agendamento.VeiculoModelo);
            command.Parameters.AddWithValue("@VeiculoMarca", agendamento.VeiculoMarca);
            command.Parameters.AddWithValue("@VeiculoAno", agendamento.VeiculoAno);
            command.Parameters.AddWithValue("@VeiculoCor", agendamento.VeiculoCor);
            command.Parameters.AddWithValue("@VeiculoCombustivel", agendamento.VeiculoCombustivel);
            command.Parameters.AddWithValue("@VeiculoQuilometragem", agendamento.VeiculoQuilometragem);
            command.Parameters.AddWithValue("@VeiculoObservacoes", agendamento.VeiculoObservacoes);
            command.Parameters.AddWithValue("@TecnicoId", agendamento.TecnicoId.ToString());
            command.Parameters.AddWithValue("@TecnicoNome", agendamento.TecnicoNome);
            command.Parameters.AddWithValue("@TecnicoEspecialidade", agendamento.TecnicoEspecialidade);
            command.Parameters.AddWithValue("@TecnicoAtivo", agendamento.TecnicoAtivo ? 1 : 0);
            command.Parameters.AddWithValue("@OrdemServicoId", agendamento.OrdemServicoId?.ToString() ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@NumeroOS", agendamento.NumeroOS);
            command.Parameters.AddWithValue("@DataInicioOS", agendamento.DataInicioOS?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@DataConclusaoOS", agendamento.DataConclusaoOS?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ValorEstimado", agendamento.ValorEstimado);
            command.Parameters.AddWithValue("@ValorReal", agendamento.ValorReal);
            command.Parameters.AddWithValue("@ValorPago", agendamento.ValorPago);
            command.Parameters.AddWithValue("@FormaPagamento", agendamento.FormaPagamento);
            command.Parameters.AddWithValue("@Pago", agendamento.Pago ? 1 : 0);
            command.Parameters.AddWithValue("@DataPagamento", agendamento.DataPagamento?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CheckIn", agendamento.CheckIn?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CheckOut", agendamento.CheckOut?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CheckInObservacoes", agendamento.CheckInObservacoes);
            command.Parameters.AddWithValue("@CheckOutObservacoes", agendamento.CheckOutObservacoes);
            command.Parameters.AddWithValue("@CheckInFotos", agendamento.CheckInFotos);
            command.Parameters.AddWithValue("@CheckOutFotos", agendamento.CheckOutFotos);
            command.Parameters.AddWithValue("@ValorProdutos", agendamento.ValorProdutos);
            command.Parameters.AddWithValue("@ValorServicos", agendamento.ValorServicos);
            command.Parameters.AddWithValue("@Recorrente", agendamento.Recorrente ? 1 : 0);
            command.Parameters.AddWithValue("@TipoRecorrencia", agendamento.TipoRecorrencia);
            command.Parameters.AddWithValue("@IntervaloRecorrencia", agendamento.IntervaloRecorrencia);
            command.Parameters.AddWithValue("@ProximaRecorrencia", agendamento.ProximaRecorrencia?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@LembreteWhatsApp", agendamento.LembreteWhatsApp ? 1 : 0);
            command.Parameters.AddWithValue("@LembreteEmail", agendamento.LembreteEmail ? 1 : 0);
            command.Parameters.AddWithValue("@DataLembrete", agendamento.DataLembrete?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@LembreteEnviado", agendamento.LembreteEnviado ? 1 : 0);
            command.Parameters.AddWithValue("@AlertaAtraso", agendamento.AlertaAtraso ? 1 : 0);
            command.Parameters.AddWithValue("@AlertaPecaFaltando", agendamento.AlertaPecaFaltando ? 1 : 0);
            command.Parameters.AddWithValue("@AlertaPronto", agendamento.AlertaPronto ? 1 : 0);
            command.Parameters.AddWithValue("@AvaliacaoCliente", agendamento.AvaliacaoCliente);
            command.Parameters.AddWithValue("@AvaliacaoComentario", agendamento.AvaliacaoComentario);

            command.ExecuteNonQuery();

            // Adicionar produtos
            if (agendamento.Produtos != null)
            {
                foreach (var produto in agendamento.Produtos)
                {
                    AdicionarAgendamentoProduto(produto, agendamento.Id);
                }
            }

            // Adicionar serviços
            if (agendamento.Servicos != null)
            {
                foreach (var servico in agendamento.Servicos)
                {
                    AdicionarAgendamentoServico(servico, agendamento.Id);
                }
            }

            // Adicionar timeline
            if (agendamento.Timeline != null)
            {
                foreach (var timeline in agendamento.Timeline)
                {
                    AdicionarAgendamentoTimeline(timeline, agendamento.Id);
                }
            }
        }

        public void AdicionarAgendamentoProduto(AgendamentoProduto produto, Guid agendamentoId)
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO AgendamentoProdutos (
                    Id, AgendamentoId, ProdutoId, ProdutoNome, ProdutoCodigo,
                    Quantidade, PrecoUnitario, PrecoTotal, Reservado, DataReserva
                )
                VALUES (
                    @Id, @AgendamentoId, @ProdutoId, @ProdutoNome, @ProdutoCodigo,
                    @Quantidade, @PrecoUnitario, @PrecoTotal, @Reservado, @DataReserva
                )";

            command.Parameters.AddWithValue("@Id", produto.Id.ToString());
            command.Parameters.AddWithValue("@AgendamentoId", agendamentoId.ToString());
            command.Parameters.AddWithValue("@ProdutoId", produto.ProdutoId.ToString());
            command.Parameters.AddWithValue("@ProdutoNome", produto.ProdutoNome);
            command.Parameters.AddWithValue("@ProdutoCodigo", produto.ProdutoCodigo);
            command.Parameters.AddWithValue("@Quantidade", produto.Quantidade);
            command.Parameters.AddWithValue("@PrecoUnitario", produto.PrecoUnitario);
            command.Parameters.AddWithValue("@PrecoTotal", produto.PrecoTotal);
            command.Parameters.AddWithValue("@Reservado", produto.Reservado ? 1 : 0);
            command.Parameters.AddWithValue("@DataReserva", produto.DataReserva?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);

            command.ExecuteNonQuery();
        }

        public void AdicionarAgendamentoServico(AgendamentoServico servico, Guid agendamentoId)
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO AgendamentoServicos (
                    Id, AgendamentoId, Nome, Categoria, Valor, TempoEstimado,
                    TempoReal, TecnicoResponsavel, Status, Observacoes, Concluido, DataConclusao
                )
                VALUES (
                    @Id, @AgendamentoId, @Nome, @Categoria, @Valor, @TempoEstimado,
                    @TempoReal, @TecnicoResponsavel, @Status, @Observacoes, @Concluido, @DataConclusao
                )";

            command.Parameters.AddWithValue("@Id", servico.Id.ToString());
            command.Parameters.AddWithValue("@AgendamentoId", agendamentoId.ToString());
            command.Parameters.AddWithValue("@Nome", servico.Nome);
            command.Parameters.AddWithValue("@Categoria", servico.Categoria);
            command.Parameters.AddWithValue("@Valor", servico.Valor);
            command.Parameters.AddWithValue("@TempoEstimado", servico.TempoEstimado.ToString());
            command.Parameters.AddWithValue("@TempoReal", servico.TempoReal.ToString());
            command.Parameters.AddWithValue("@TecnicoResponsavel", servico.TecnicoResponsavel);
            command.Parameters.AddWithValue("@Status", servico.Status);
            command.Parameters.AddWithValue("@Observacoes", servico.Observacoes);
            command.Parameters.AddWithValue("@Concluido", servico.Concluido ? 1 : 0);
            command.Parameters.AddWithValue("@DataConclusao", servico.DataConclusao?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);

            command.ExecuteNonQuery();
        }

        public void AdicionarAgendamentoTimeline(AgendamentoTimeline timeline, Guid agendamentoId)
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO AgendamentoTimeline (
                    Id, AgendamentoId, DataHora, Usuario, Acao, Detalhes, TipoAlteracao
                )
                VALUES (
                    @Id, @AgendamentoId, @DataHora, @Usuario, @Acao, @Detalhes, @TipoAlteracao
                )";

            command.Parameters.AddWithValue("@Id", timeline.Id.ToString());
            command.Parameters.AddWithValue("@AgendamentoId", agendamentoId.ToString());
            command.Parameters.AddWithValue("@DataHora", timeline.DataHora.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@Usuario", timeline.Usuario);
            command.Parameters.AddWithValue("@Acao", timeline.Acao);
            command.Parameters.AddWithValue("@Detalhes", timeline.Detalhes);
            command.Parameters.AddWithValue("@TipoAlteracao", timeline.TipoAlteracao);

            command.ExecuteNonQuery();
        }

        public bool IntegracaoExecutada(Guid agendamentoId, string tipo)
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT 1
                FROM AgendamentoIntegracoes
                WHERE AgendamentoId = @AgendamentoId
                  AND Tipo = @Tipo
                " + LimitOne(connection);
            command.Parameters.AddWithValue("@AgendamentoId", agendamentoId.ToString());
            command.Parameters.AddWithValue("@Tipo", tipo);

            return command.ExecuteScalar() != null;
        }

        public void RegistrarIntegracao(Guid agendamentoId, string tipo, string detalhes = "")
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = IsSqlServerConnection(connection)
                ? @"
                    IF NOT EXISTS
                    (
                        SELECT 1
                        FROM AgendamentoIntegracoes
                        WHERE AgendamentoId = @AgendamentoId
                          AND Tipo = @Tipo
                    )
                    BEGIN
                        INSERT INTO AgendamentoIntegracoes
                        (
                            Id,
                            AgendamentoId,
                            Tipo,
                            Detalhes,
                            DataCriacao
                        )
                        VALUES
                        (
                            @Id,
                            @AgendamentoId,
                            @Tipo,
                            @Detalhes,
                            @DataCriacao
                        );
                    END"
                : @"
                    INSERT OR IGNORE INTO AgendamentoIntegracoes
                    (
                        Id,
                        AgendamentoId,
                        Tipo,
                        Detalhes,
                        DataCriacao
                    )
                    VALUES
                    (
                        @Id,
                        @AgendamentoId,
                        @Tipo,
                        @Detalhes,
                        @DataCriacao
                    )";
            command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("@AgendamentoId", agendamentoId.ToString());
            command.Parameters.AddWithValue("@Tipo", tipo);
            command.Parameters.AddWithValue("@Detalhes", string.IsNullOrWhiteSpace(detalhes) ? (object)DBNull.Value : detalhes.Trim());
            command.Parameters.AddWithValue("@DataCriacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.ExecuteNonQuery();
        }

        public List<AgendamentoProduto> ObterProdutosDoAgendamento(Guid agendamentoId)
        {
            using var connection = GetConnection();
            connection.Open();
            return ObterProdutosDoAgendamento(connection, agendamentoId);
        }

        public List<AgendamentoServico> ObterServicosDoAgendamento(Guid agendamentoId)
        {
            using var connection = GetConnection();
            connection.Open();
            return ObterServicosDoAgendamento(connection, agendamentoId);
        }

        public OrdemServico ConverterEmOrdemServico(Agendamento agendamento, string usuario = "Sistema")
        {
            ArgumentNullException.ThrowIfNull(agendamento);

            if (agendamento.ClienteId == Guid.Empty)
            {
                throw new InvalidOperationException("O agendamento precisa ter um cliente valido antes da conversao em OS.");
            }

            var ordemExistente = ObterOrdemServicoVinculada(agendamento);
            if (ordemExistente != null)
            {
                _estoqueOperationalService.ReservarProdutosDoAgendamento(agendamento, usuario);
                VincularAgendamentoComOrdemServico(agendamento, ordemExistente);
                RegistrarIntegracao(agendamento.Id, "OrdemServicoConversao", $"OS {ordemExistente.Numero} ja estava vinculada ao agendamento.");
                return ordemExistente;
            }

            var tinhaReservaAtiva = PossuiReservaAtiva(agendamento);

            try
            {
                _estoqueOperationalService.ReservarProdutosDoAgendamento(agendamento, usuario);

                var ordem = CriarOrdemServicoDoAgendamento(agendamento, usuario);
                global::PrimoAutoEletrica.App.Repositories.OrdensServico.Inserir(ordem);

                var persistida = global::PrimoAutoEletrica.App.Repositories.OrdensServico.ObterPorId(ordem.Id) ?? ordem;
                VincularAgendamentoComOrdemServico(agendamento, persistida);
                RegistrarIntegracao(agendamento.Id, "OrdemServicoConversao", $"OS {persistida.Numero} criada a partir do agendamento.");
                return persistida;
            }
            catch
            {
                if (!tinhaReservaAtiva)
                {
                    try
                    {
                        _estoqueOperationalService.LiberarReservasDoAgendamento(agendamento, "Falha ao converter em OS", usuario);
                    }
                    catch
                    {
                    }
                }

                throw;
            }
        }

        public OrdemServico? FinalizarOrdemServicoVinculada(Agendamento agendamento, string usuario = "Sistema")
        {
            ArgumentNullException.ThrowIfNull(agendamento);

            var ordem = ObterOrdemServicoVinculada(agendamento);
            if (ordem == null)
            {
                return null;
            }

            var referenciaFinalizacao = agendamento.CheckOut ?? DateTime.Now;
            ordem.Status = "Entregue";
            ordem.DataInicio ??= agendamento.CheckIn ?? agendamento.DataInicioOS ?? agendamento.DataAgendamento;
            ordem.DataConclusao = referenciaFinalizacao;
            ordem.DataEntrega = referenciaFinalizacao;
            ordem.ClienteNomeSnapshot = string.IsNullOrWhiteSpace(agendamento.ClienteNome)
                ? ordem.ClienteNomeSnapshot
                : agendamento.ClienteNome.Trim();
            ordem.TelefoneClienteSnapshot = string.IsNullOrWhiteSpace(agendamento.ClienteTelefone)
                ? ordem.TelefoneClienteSnapshot
                : agendamento.ClienteTelefone.Trim();
            ordem.VeiculoDescricaoSnapshot = string.IsNullOrWhiteSpace(agendamento.VeiculoModelo)
                ? ordem.VeiculoDescricaoSnapshot
                : $"{agendamento.VeiculoMarca} {agendamento.VeiculoModelo} {agendamento.VeiculoAno}".Trim();
            ordem.PlacaSnapshot = string.IsNullOrWhiteSpace(agendamento.VeiculoPlaca)
                ? ordem.PlacaSnapshot
                : agendamento.VeiculoPlaca.Trim().ToUpperInvariant();

            if (!string.IsNullOrWhiteSpace(agendamento.CheckOutObservacoes))
            {
                ordem.ObservacoesInternas = string.IsNullOrWhiteSpace(ordem.ObservacoesInternas)
                    ? $"Check-out: {agendamento.CheckOutObservacoes.Trim()}"
                    : $"{ordem.ObservacoesInternas}\nCheck-out: {agendamento.CheckOutObservacoes.Trim()}";
            }

            ordem.Eventos ??= new List<OrdemServicoEvento>();
            ordem.Eventos.Add(new OrdemServicoEvento
            {
                Id = Guid.NewGuid(),
                OrdemServicoId = ordem.Id,
                DataEvento = referenciaFinalizacao,
                Titulo = "Finalizacao via agendamento",
                Descricao = $"Atendimento finalizado pelo fluxo de agendamento {agendamento.Numero}.",
                Tipo = "Fluxo",
                Usuario = string.IsNullOrWhiteSpace(usuario) ? "Sistema" : usuario.Trim()
            });

            global::PrimoAutoEletrica.App.Repositories.OrdensServico.Atualizar(ordem);
            var persistida = global::PrimoAutoEletrica.App.Repositories.OrdensServico.ObterPorId(ordem.Id) ?? ordem;

            agendamento.NumeroOS = persistida.Numero;
            agendamento.OrdemServicoId = persistida.Id;
            agendamento.DataInicioOS = persistida.DataInicio;
            agendamento.DataConclusaoOS = persistida.DataConclusao ?? persistida.DataEntrega;
            RegistrarIntegracao(agendamento.Id, "OrdemServicoFinalizacao", $"OS {persistida.Numero} finalizada pelo checkout do agendamento.");
            return persistida;
        }

        private static List<AgendamentoProduto> ObterProdutosDoAgendamento(DbConnection connection, Guid agendamentoId)
        {
            var produtos = new List<AgendamentoProduto>();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    Id,
                    ProdutoId,
                    ProdutoNome,
                    ProdutoCodigo,
                    Quantidade,
                    PrecoUnitario,
                    PrecoTotal,
                    Reservado,
                    DataReserva
                FROM AgendamentoProdutos
                WHERE AgendamentoId = @AgendamentoId";
            command.Parameters.AddWithValue("@AgendamentoId", agendamentoId.ToString());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                produtos.Add(new AgendamentoProduto
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    ProdutoId = Guid.Parse(reader.GetString(1)),
                    ProdutoNome = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    ProdutoCodigo = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    Quantidade = reader.GetInt32(4),
                    PrecoUnitario = ReadDecimal(reader, 5),
                    PrecoTotal = ReadDecimal(reader, 6),
                    Reservado = reader.GetInt32(7) == 1,
                    DataReserva = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8))
                });
            }

            return produtos;
        }

        private static List<AgendamentoServico> ObterServicosDoAgendamento(DbConnection connection, Guid agendamentoId)
        {
            var servicos = new List<AgendamentoServico>();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    Id,
                    Nome,
                    Categoria,
                    Valor,
                    TempoEstimado,
                    TempoReal,
                    TecnicoResponsavel,
                    Status,
                    Observacoes,
                    Concluido,
                    DataConclusao
                FROM AgendamentoServicos
                WHERE AgendamentoId = @AgendamentoId
                ORDER BY Nome ASC";
            command.Parameters.AddWithValue("@AgendamentoId", agendamentoId.ToString());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                servicos.Add(new AgendamentoServico
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    Nome = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    Categoria = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Valor = ReadDecimal(reader, 3),
                    TempoEstimado = reader.IsDBNull(4) ? TimeSpan.Zero : TimeSpan.Parse(reader.GetString(4)),
                    TempoReal = reader.IsDBNull(5) ? TimeSpan.Zero : TimeSpan.Parse(reader.GetString(5)),
                    TecnicoResponsavel = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    Status = reader.IsDBNull(7) ? "Pendente" : reader.GetString(7),
                    Observacoes = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                    Concluido = reader.GetInt32(9) == 1,
                    DataConclusao = reader.IsDBNull(10) ? null : DateTime.Parse(reader.GetString(10))
                });
            }

            return servicos;
        }

        public List<Agendamento> ObterTodosAgendamentos()
        {
            var agendamentos = new List<Agendamento>();
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT * FROM Agendamentos
                ORDER BY DataAgendamento DESC, HoraInicio DESC";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                agendamentos.Add(LerAgendamento(reader));
            }

            reader.Dispose();
            foreach (var agendamento in agendamentos)
            {
                CarregarDadosRelacionados(connection, agendamento);
            }

            return agendamentos;
        }

        public List<Agendamento> ObterAgendamentosPorData(DateTime data)
        {
            var agendamentos = new List<Agendamento>();
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            var dataExpression = DateOnlyExpression(connection, "DataAgendamento");
            command.CommandText = $@"
                SELECT * FROM Agendamentos
                WHERE {dataExpression} = @data
                ORDER BY HoraInicio ASC";
            command.Parameters.AddWithValue("@data", data.ToString("yyyy-MM-dd"));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                agendamentos.Add(LerAgendamento(reader));
            }

            reader.Dispose();
            foreach (var agendamento in agendamentos)
            {
                CarregarDadosRelacionados(connection, agendamento);
            }

            return agendamentos;
        }

        public List<Agendamento> ObterAgendamentosPorPeriodo(DateTime dataInicio, DateTime dataFim)
        {
            var agendamentos = new List<Agendamento>();
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            var dataExpression = DateOnlyExpression(connection, "DataAgendamento");
            command.CommandText = $@"
                SELECT * FROM Agendamentos
                WHERE {dataExpression} BETWEEN @dataInicio AND @dataFim
                ORDER BY DataAgendamento ASC, HoraInicio ASC";
            command.Parameters.AddWithValue("@dataInicio", dataInicio.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@dataFim", dataFim.ToString("yyyy-MM-dd"));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                agendamentos.Add(LerAgendamento(reader));
            }

            reader.Dispose();
            foreach (var agendamento in agendamentos)
            {
                CarregarDadosRelacionados(connection, agendamento);
            }

            return agendamentos;
        }

        public List<Agendamento> ObterAgendamentosPorTecnico(Guid tecnicoId)
        {
            var agendamentos = new List<Agendamento>();
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT * FROM Agendamentos
                WHERE TecnicoId = @tecnicoId
                ORDER BY DataAgendamento ASC, HoraInicio ASC";
            command.Parameters.AddWithValue("@tecnicoId", tecnicoId.ToString());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                agendamentos.Add(LerAgendamento(reader));
            }

            foreach (var agendamento in agendamentos)
            {
                CarregarDadosRelacionados(connection, agendamento);
            }

            return agendamentos;
        }

        public List<Agendamento> ObterAgendamentosPorCliente(Guid clienteId)
        {
            var agendamentos = new List<Agendamento>();
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT * FROM Agendamentos
                WHERE ClienteId = @clienteId
                ORDER BY DataAgendamento DESC";
            command.Parameters.AddWithValue("@clienteId", clienteId.ToString());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                agendamentos.Add(LerAgendamento(reader));
            }

            foreach (var agendamento in agendamentos)
            {
                CarregarDadosRelacionados(connection, agendamento);
            }

            return agendamentos;
        }

        public Agendamento? ObterAgendamentoPorId(Guid id)
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT * FROM Agendamentos
                WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id.ToString());

            Agendamento? agendamento = null;
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    agendamento = LerAgendamento(reader);
                }
            }

            if (agendamento != null)
            {
                CarregarDadosRelacionados(connection, agendamento);
            }

            return agendamento;
        }

        public void AtualizarAgendamento(Agendamento agendamento)
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Agendamentos SET
                    Status = @Status,
                    Prioridade = @Prioridade,
                    DataAgendamento = @DataAgendamento,
                    HoraInicio = @HoraInicio,
                    HoraTermino = @HoraTermino,
                    DuracaoReal = @DuracaoReal,
                    TipoServico = @TipoServico,
                    CategoriaServico = @CategoriaServico,
                    DescricaoServico = @DescricaoServico,
                    TecnicoId = @TecnicoId,
                    TecnicoNome = @TecnicoNome,
                    Observacoes = @Observacoes,
                    OrdemServicoId = @OrdemServicoId,
                    NumeroOS = @NumeroOS,
                    DataInicioOS = @DataInicioOS,
                    DataConclusaoOS = @DataConclusaoOS,
                    ValorEstimado = @ValorEstimado,
                    ValorReal = @ValorReal,
                    ValorPago = @ValorPago,
                    FormaPagamento = @FormaPagamento,
                    Pago = @Pago,
                    DataPagamento = @DataPagamento,
                    CheckIn = @CheckIn,
                    CheckOut = @CheckOut,
                    CheckInObservacoes = @CheckInObservacoes,
                    CheckOutObservacoes = @CheckOutObservacoes,
                    CheckInFotos = @CheckInFotos,
                    CheckOutFotos = @CheckOutFotos,
                    ValorProdutos = @ValorProdutos,
                    ValorServicos = @ValorServicos,
                    DataCancelamento = @DataCancelamento,
                    MotivoCancelamento = @MotivoCancelamento,
                    DataReagendamento = @DataReagendamento,
                    DataAgendamentoAnterior = @DataAgendamentoAnterior,
                    MotivoReagendamento = @MotivoReagendamento,
                    LembreteWhatsApp = @LembreteWhatsApp,
                    LembreteEmail = @LembreteEmail,
                    DataLembrete = @DataLembrete,
                    LembreteEnviado = @LembreteEnviado,
                    AvaliacaoCliente = @AvaliacaoCliente,
                    AvaliacaoComentario = @AvaliacaoComentario
                WHERE Id = @Id";

            command.Parameters.AddWithValue("@Status", agendamento.Status);
            command.Parameters.AddWithValue("@Prioridade", agendamento.Prioridade);
            command.Parameters.AddWithValue("@DataAgendamento", agendamento.DataAgendamento.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@HoraInicio", agendamento.HoraInicio?.ToString("HH:mm") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@HoraTermino", agendamento.HoraTermino?.ToString("HH:mm") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@DuracaoReal", agendamento.DuracaoReal.ToString());
            command.Parameters.AddWithValue("@TipoServico", agendamento.TipoServico);
            command.Parameters.AddWithValue("@CategoriaServico", agendamento.CategoriaServico);
            command.Parameters.AddWithValue("@DescricaoServico", agendamento.DescricaoServico);
            command.Parameters.AddWithValue("@TecnicoId", agendamento.TecnicoId.ToString());
            command.Parameters.AddWithValue("@TecnicoNome", agendamento.TecnicoNome);
            command.Parameters.AddWithValue("@Observacoes", agendamento.Observacoes);
            command.Parameters.AddWithValue("@OrdemServicoId", agendamento.OrdemServicoId?.ToString() ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@NumeroOS", agendamento.NumeroOS);
            command.Parameters.AddWithValue("@DataInicioOS", agendamento.DataInicioOS?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@DataConclusaoOS", agendamento.DataConclusaoOS?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ValorEstimado", agendamento.ValorEstimado);
            command.Parameters.AddWithValue("@ValorReal", agendamento.ValorReal);
            command.Parameters.AddWithValue("@ValorPago", agendamento.ValorPago);
            command.Parameters.AddWithValue("@FormaPagamento", agendamento.FormaPagamento);
            command.Parameters.AddWithValue("@Pago", agendamento.Pago ? 1 : 0);
            command.Parameters.AddWithValue("@DataPagamento", agendamento.DataPagamento?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CheckIn", agendamento.CheckIn?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CheckOut", agendamento.CheckOut?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CheckInObservacoes", agendamento.CheckInObservacoes);
            command.Parameters.AddWithValue("@CheckOutObservacoes", agendamento.CheckOutObservacoes);
            command.Parameters.AddWithValue("@CheckInFotos", agendamento.CheckInFotos);
            command.Parameters.AddWithValue("@CheckOutFotos", agendamento.CheckOutFotos);
            command.Parameters.AddWithValue("@ValorProdutos", agendamento.ValorProdutos);
            command.Parameters.AddWithValue("@ValorServicos", agendamento.ValorServicos);
            command.Parameters.AddWithValue("@DataCancelamento", agendamento.DataCancelamento?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@MotivoCancelamento", agendamento.MotivoCancelamento);
            command.Parameters.AddWithValue("@DataReagendamento", agendamento.DataReagendamento?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@DataAgendamentoAnterior", agendamento.DataAgendamentoAnterior?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@MotivoReagendamento", agendamento.MotivoReagendamento);
            command.Parameters.AddWithValue("@LembreteWhatsApp", agendamento.LembreteWhatsApp ? 1 : 0);
            command.Parameters.AddWithValue("@LembreteEmail", agendamento.LembreteEmail ? 1 : 0);
            command.Parameters.AddWithValue("@DataLembrete", agendamento.DataLembrete?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@LembreteEnviado", agendamento.LembreteEnviado ? 1 : 0);
            command.Parameters.AddWithValue("@AvaliacaoCliente", agendamento.AvaliacaoCliente);
            command.Parameters.AddWithValue("@AvaliacaoComentario", agendamento.AvaliacaoComentario);
            command.Parameters.AddWithValue("@Id", agendamento.Id.ToString());

            command.ExecuteNonQuery();
        }

        public void ReservarProdutosDoAgendamento(Agendamento agendamento, string usuario = "Sistema")
        {
            ArgumentNullException.ThrowIfNull(agendamento);
            _estoqueOperationalService.ReservarProdutosDoAgendamento(agendamento, usuario);
            RegistrarIntegracao(agendamento.Id, "ReservaEstoqueAgendamento", $"Reserva operacional atualizada pelo usuario {usuario}.");
        }

        public void LiberarReservasDoAgendamento(Agendamento agendamento, string motivo, string usuario = "Sistema")
        {
            ArgumentNullException.ThrowIfNull(agendamento);
            _estoqueOperationalService.LiberarReservasDoAgendamento(agendamento, motivo, usuario);
            RegistrarIntegracao(agendamento.Id, "LiberacaoReservaAgendamento", $"Reservas liberadas. Motivo: {motivo}");
        }

        private void CarregarDadosRelacionados(DbConnection connection, Agendamento agendamento)
        {
            agendamento.Produtos = ObterProdutosDoAgendamento(connection, agendamento.Id);
            agendamento.Servicos = ObterServicosDoAgendamento(connection, agendamento.Id);
            agendamento.Timeline = ObterTimelineDoAgendamento(connection, agendamento.Id);
        }

        private static List<AgendamentoTimeline> ObterTimelineDoAgendamento(DbConnection connection, Guid agendamentoId)
        {
            var timeline = new List<AgendamentoTimeline>();

            if (!TabelaRelacionadaExiste(connection, "AgendamentoTimeline"))
            {
                return timeline;
            }

            using var command = connection.CreateCommand();
            command.CommandText = IsSqlServerConnection(connection)
                ? @"
                    SELECT Id, DataHora, Usuario, Acao, Detalhes, TipoAlteracao
                    FROM AgendamentoTimeline
                    WHERE AgendamentoId = @AgendamentoId
                    ORDER BY DataHora DESC, Id DESC;"
                : @"
                    SELECT Id, DataHora, Usuario, Acao, Detalhes, TipoAlteracao
                    FROM AgendamentoTimeline
                    WHERE AgendamentoId = @AgendamentoId
                    ORDER BY datetime(DataHora) DESC, rowid DESC;";
            command.Parameters.AddWithValue("@AgendamentoId", agendamentoId.ToString());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                timeline.Add(new AgendamentoTimeline
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    DataHora = DateTime.Parse(reader.GetString(1)),
                    Usuario = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Acao = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    Detalhes = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    TipoAlteracao = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
                });
            }

            return timeline;
        }

        private static bool TabelaRelacionadaExiste(DbConnection connection, string tableName)
        {
            using var command = connection.CreateCommand();
            command.CommandText = IsSqlServerConnection(connection)
                ? @"
                    SELECT 1
                    FROM sys.tables
                    WHERE name = @name;"
                : @"
                    SELECT 1
                    FROM sqlite_master
                    WHERE type = 'table'
                      AND name = @name
                    LIMIT 1;";
            command.Parameters.AddWithValue("@name", tableName);
            return command.ExecuteScalar() != null;
        }

        private OrdemServico? ObterOrdemServicoVinculada(Agendamento agendamento)
        {
            if (!agendamento.OrdemServicoId.HasValue || agendamento.OrdemServicoId.Value == Guid.Empty)
            {
                return null;
            }

            return global::PrimoAutoEletrica.App.Repositories.OrdensServico.ObterPorId(agendamento.OrdemServicoId.Value);
        }

        private void VincularAgendamentoComOrdemServico(Agendamento agendamento, OrdemServico ordem)
        {
            agendamento.Status = "Em Andamento";
            agendamento.OrdemServicoId = ordem.Id;
            agendamento.NumeroOS = ordem.Numero;
            agendamento.DataInicioOS = ordem.DataInicio ?? ordem.DataAbertura;
            agendamento.DataConclusaoOS = ordem.DataConclusao ?? ordem.DataEntrega;
            AtualizarAgendamento(agendamento);
        }

        private OrdemServico CriarOrdemServicoDoAgendamento(Agendamento agendamento, string usuario)
        {
            var produtos = ObterProdutosOperacionais(agendamento);
            var servicos = ObterServicosOperacionais(agendamento);
            var itens = new List<OrdemServicoItem>();
            var ordemExibicao = 1;
            var dataAbertura = agendamento.CheckIn ?? DateTime.Now;
            var dataPrevisao = DeterminarDataPrevisaoOrdemServico(agendamento, dataAbertura);

            foreach (var servico in servicos)
            {
                itens.Add(new OrdemServicoItem
                {
                    Id = Guid.NewGuid(),
                    Tipo = "Servico",
                    Descricao = string.IsNullOrWhiteSpace(servico.Nome) ? "Servico tecnico" : servico.Nome.Trim(),
                    Quantidade = 1,
                    ValorUnitario = servico.Valor,
                    CustoUnitario = 0m,
                    Observacoes = string.IsNullOrWhiteSpace(servico.Observacoes)
                        ? servico.Categoria?.Trim() ?? string.Empty
                        : servico.Observacoes.Trim(),
                    OrdemExibicao = ordemExibicao++
                });
            }

            foreach (var produto in produtos)
            {
                itens.Add(new OrdemServicoItem
                {
                    Id = Guid.NewGuid(),
                    ProdutoId = produto.ProdutoId,
                    Tipo = "Peca",
                    Descricao = string.IsNullOrWhiteSpace(produto.ProdutoNome) ? "Peca" : produto.ProdutoNome.Trim(),
                    Quantidade = produto.Quantidade,
                    ValorUnitario = produto.PrecoUnitario,
                    CustoUnitario = ObterCustoUnitarioProduto(produto.ProdutoId),
                    Observacoes = string.IsNullOrWhiteSpace(produto.ProdutoCodigo)
                        ? string.Empty
                        : $"Codigo: {produto.ProdutoCodigo.Trim()}",
                    OrdemExibicao = ordemExibicao++
                });
            }

            if (itens.Count == 0)
            {
                itens.Add(new OrdemServicoItem
                {
                    Id = Guid.NewGuid(),
                    Tipo = "Servico",
                    Descricao = string.IsNullOrWhiteSpace(agendamento.TipoServico)
                        ? "Servico do agendamento"
                        : agendamento.TipoServico.Trim(),
                    Quantidade = 1,
                    ValorUnitario = DeterminarValorServicoFallback(agendamento),
                    CustoUnitario = 0m,
                    Observacoes = agendamento.DescricaoServico?.Trim() ?? string.Empty,
                    OrdemExibicao = ordemExibicao
                });
            }

            return new OrdemServico
            {
                Id = Guid.NewGuid(),
                Numero = global::PrimoAutoEletrica.App.Repositories.OrdensServico.GerarProximoNumero(),
                ClienteId = agendamento.ClienteId,
                VeiculoId = agendamento.VeiculoId == Guid.Empty ? null : agendamento.VeiculoId,
                TecnicoId = ResolverTecnicoResponsavel(agendamento),
                AgendamentoId = agendamento.Id,
                ClienteNomeSnapshot = string.IsNullOrWhiteSpace(agendamento.ClienteNome) ? "Cliente nao informado" : agendamento.ClienteNome.Trim(),
                TelefoneClienteSnapshot = string.IsNullOrWhiteSpace(agendamento.ClienteTelefone) ? string.Empty : agendamento.ClienteTelefone.Trim(),
                VeiculoDescricaoSnapshot = $"{agendamento.VeiculoMarca} {agendamento.VeiculoModelo} {agendamento.VeiculoAno}".Trim(),
                PlacaSnapshot = string.IsNullOrWhiteSpace(agendamento.VeiculoPlaca) ? string.Empty : agendamento.VeiculoPlaca.Trim().ToUpperInvariant(),
                Status = "Em execucao",
                Prioridade = string.IsNullOrWhiteSpace(agendamento.Prioridade) ? "Normal" : agendamento.Prioridade.Trim(),
                Origem = "Agendamento",
                ProblemaRelatado = !string.IsNullOrWhiteSpace(agendamento.DescricaoServico)
                    ? agendamento.DescricaoServico.Trim()
                    : agendamento.TipoServico.Trim(),
                ObservacoesCliente = agendamento.Observacoes?.Trim() ?? string.Empty,
                ObservacoesInternas = $"Gerada automaticamente a partir do agendamento {agendamento.Numero}.",
                AprovadaCliente = true,
                MetodoAprovacao = "Agenda confirmada",
                DataAbertura = dataAbertura,
                DataPrevisao = dataPrevisao,
                DataInicio = agendamento.CheckIn ?? dataAbertura,
                Itens = itens,
                Eventos = new List<OrdemServicoEvento>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        DataEvento = DateTime.Now,
                        Titulo = "OS criada via agendamento",
                        Descricao = $"Conversao do agendamento {agendamento.Numero} para ordem de servico.",
                        Tipo = "Fluxo",
                        Usuario = string.IsNullOrWhiteSpace(usuario) ? "Sistema" : usuario.Trim()
                    }
                }
            };
        }

        private static DateTime DeterminarDataPrevisaoOrdemServico(Agendamento agendamento, DateTime dataAbertura)
        {
            var inicioAgendado = CombinarDataHoraAgendada(agendamento.DataAgendamento, agendamento.HoraInicio);
            var terminoAgendado = CombinarDataHoraAgendada(agendamento.DataAgendamento, agendamento.HoraTermino);
            var duracaoPlanejada = agendamento.DuracaoEstimada > TimeSpan.Zero
                ? agendamento.DuracaoEstimada
                : TimeSpan.FromHours(2);

            var dataPrevisao = terminoAgendado
                ?? inicioAgendado?.Add(duracaoPlanejada)
                ?? agendamento.DataAgendamento.Date.Add(duracaoPlanejada);

            if (dataPrevisao < dataAbertura)
            {
                dataPrevisao = dataAbertura.Add(duracaoPlanejada);
            }

            return dataPrevisao;
        }

        private static DateTime? CombinarDataHoraAgendada(DateTime dataAgendamento, DateTime? horario)
        {
            if (!horario.HasValue)
            {
                return null;
            }

            return dataAgendamento.Date.Add(horario.Value.TimeOfDay);
        }

        private List<AgendamentoProduto> ObterProdutosOperacionais(Agendamento agendamento)
        {
            if (agendamento.Produtos != null && agendamento.Produtos.Count > 0)
            {
                return agendamento.Produtos
                    .Where(p => p.Quantidade > 0)
                    .ToList();
            }

            return ObterProdutosDoAgendamento(agendamento.Id)
                .Where(p => p.Quantidade > 0)
                .ToList();
        }

        private List<AgendamentoServico> ObterServicosOperacionais(Agendamento agendamento)
        {
            if (agendamento.Servicos != null && agendamento.Servicos.Count > 0)
            {
                return agendamento.Servicos
                    .Where(s => !string.IsNullOrWhiteSpace(s.Nome) || s.Valor > 0)
                    .ToList();
            }

            var servicos = ObterServicosDoAgendamento(agendamento.Id)
                .Where(s => !string.IsNullOrWhiteSpace(s.Nome) || s.Valor > 0)
                .ToList();

            if (servicos.Count > 0)
            {
                return servicos;
            }

            if (string.IsNullOrWhiteSpace(agendamento.TipoServico) &&
                string.IsNullOrWhiteSpace(agendamento.DescricaoServico) &&
                agendamento.ValorServicos <= 0 &&
                agendamento.ValorEstimado <= 0 &&
                agendamento.ValorReal <= 0)
            {
                return new List<AgendamentoServico>();
            }

            return new List<AgendamentoServico>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Nome = string.IsNullOrWhiteSpace(agendamento.TipoServico) ? "Servico tecnico" : agendamento.TipoServico.Trim(),
                    Categoria = agendamento.CategoriaServico?.Trim() ?? string.Empty,
                    Valor = DeterminarValorServicoFallback(agendamento),
                    TempoEstimado = agendamento.DuracaoEstimada,
                    TempoReal = agendamento.DuracaoReal,
                    TecnicoResponsavel = agendamento.TecnicoNome?.Trim() ?? string.Empty,
                    Status = "Pendente",
                    Observacoes = agendamento.DescricaoServico?.Trim() ?? string.Empty
                }
            };
        }

        private int? ResolverTecnicoResponsavel(Agendamento agendamento)
        {
            if (string.IsNullOrWhiteSpace(agendamento.TecnicoNome))
            {
                return null;
            }

            var tecnico = global::PrimoAutoEletrica.App.Repositories.Funcionarios
                .ObterTodos()
                .FirstOrDefault(f =>
                    f.Ativo &&
                    string.Equals(f.Nome?.Trim(), agendamento.TecnicoNome.Trim(), StringComparison.OrdinalIgnoreCase));

            return tecnico?.Id;
        }

        private static decimal DeterminarValorServicoFallback(Agendamento agendamento)
        {
            if (agendamento.ValorServicos > 0)
            {
                return agendamento.ValorServicos;
            }

            var totalBase = agendamento.ValorReal > 0 ? agendamento.ValorReal : agendamento.ValorEstimado;
            if (totalBase <= 0)
            {
                return 0m;
            }

            return Math.Max(0m, totalBase - agendamento.ValorProdutos);
        }

        private static decimal ObterCustoUnitarioProduto(Guid produtoId)
        {
            var produto = global::PrimoAutoEletrica.App.Repositories.Produtos.ObterPorId(produtoId);
            return produto?.PrecoCompra ?? 0m;
        }

        public void CancelarAgendamento(Guid id, string motivo)
        {
            var agendamento = ObterAgendamentoPorId(id)
                ?? throw new InvalidOperationException("Agendamento nao encontrado para cancelamento.");

            if (PossuiReservaAtiva(agendamento))
            {
                _estoqueOperationalService.LiberarReservasDoAgendamento(
                    agendamento,
                    motivo,
                    global::PrimoAutoEletrica.App.Session.UserName);
            }

            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                UPDATE Agendamentos SET
                    Status = 'Cancelado',
                    DataCancelamento = @DataCancelamento,
                    MotivoCancelamento = @MotivoCancelamento
                WHERE Id = @Id";

            command.Parameters.AddWithValue("@DataCancelamento", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@MotivoCancelamento", motivo);
            command.Parameters.AddWithValue("@Id", id.ToString());

            command.ExecuteNonQuery();
            RegistrarIntegracao(id, "CancelamentoAgendamento", $"Agendamento cancelado. Motivo: {motivo}");
        }

        private bool PossuiReservaAtiva(Agendamento agendamento)
        {
            var produtos = agendamento.Produtos != null && agendamento.Produtos.Count > 0
                ? agendamento.Produtos
                : ObterProdutosDoAgendamento(agendamento.Id);

            return produtos.Any(produto => produto.Reservado && produto.Quantidade > 0);
        }

        public string GerarNumeroAgendamento()
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = IsSqlServerConnection(connection)
                ? @"
                    SELECT COUNT(*) FROM Agendamentos
                    WHERE CONVERT(date, TRY_CONVERT(datetime, DataCriacao)) = CONVERT(date, GETDATE())"
                : @"
                    SELECT COUNT(*) FROM Agendamentos
                    WHERE date(DataCriacao) = date('now')";

            var count = Convert.ToInt32(command.ExecuteScalar());
            return $"AG{DateTime.Now:yyyyMMdd}{count + 1:D4}";
        }

        private static string LimitOne(DbConnection connection)
        {
            return IsSqlServerConnection(connection) ? string.Empty : "LIMIT 1";
        }

        private static string DateOnlyExpression(DbConnection connection, string expression)
        {
            return IsSqlServerConnection(connection)
                ? $"CONVERT(date, TRY_CONVERT(datetime, {expression}))"
                : $"date({expression})";
        }

        private static bool IsSqlServerConnection(DbConnection connection)
        {
            return connection.GetType().FullName?.Contains("SqlClient", StringComparison.OrdinalIgnoreCase) == true;
        }

        private static Agendamento LerAgendamento(DbDataReader reader)
        {
            return new Agendamento
            {
                Id = Guid.Parse(reader.GetString(0)),
                Numero = reader.GetString(1),
                DataCriacao = DateTime.Parse(reader.GetString(2)),
                DataAgendamento = DateTime.Parse(reader.GetString(3)),
                HoraInicio = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4)),
                HoraTermino = reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5)),
                DuracaoEstimada = TimeSpan.Parse(reader.GetString(6)),
                DuracaoReal = reader.IsDBNull(7) ? TimeSpan.Zero : TimeSpan.Parse(reader.GetString(7)),
                Status = reader.GetString(8),
                Prioridade = reader.GetString(9),
                TipoServico = reader.GetString(10),
                CategoriaServico = reader.GetString(11),
                DescricaoServico = reader.GetString(12),
                Observacoes = reader.GetString(13),
                ClienteId = Guid.Parse(reader.GetString(14)),
                ClienteNome = reader.GetString(15),
                ClienteTelefone = reader.GetString(16),
                ClienteEmail = reader.GetString(17),
                ClienteDocumento = reader.GetString(18),
                ClienteVip = reader.GetInt32(19) == 1,
                ClienteTotalGasto = ReadDecimal(reader, 20),
                ClienteAtendimentos = reader.GetInt32(21),
                ClienteUltimaVisita = reader.IsDBNull(22) ? null : DateTime.Parse(reader.GetString(22)),
                VeiculoId = Guid.Parse(reader.GetString(23)),
                VeiculoPlaca = reader.GetString(24),
                VeiculoModelo = reader.GetString(25),
                VeiculoMarca = reader.GetString(26),
                VeiculoAno = reader.GetString(27),
                VeiculoCor = reader.GetString(28),
                VeiculoCombustivel = reader.GetString(29),
                VeiculoQuilometragem = reader.GetInt32(30),
                VeiculoObservacoes = reader.GetString(31),
                TecnicoId = Guid.Parse(reader.GetString(32)),
                TecnicoNome = reader.GetString(33),
                TecnicoEspecialidade = reader.GetString(34),
                TecnicoAtivo = reader.GetInt32(35) == 1,
                OrdemServicoId = reader.IsDBNull(36) ? null : Guid.Parse(reader.GetString(36)),
                NumeroOS = reader.GetString(37),
                DataInicioOS = reader.IsDBNull(38) ? null : DateTime.Parse(reader.GetString(38)),
                DataConclusaoOS = reader.IsDBNull(39) ? null : DateTime.Parse(reader.GetString(39)),
                ValorEstimado = ReadDecimal(reader, 40),
                ValorReal = ReadDecimal(reader, 41),
                ValorPago = ReadDecimal(reader, 42),
                FormaPagamento = reader.GetString(43),
                Pago = reader.GetInt32(44) == 1,
                DataPagamento = reader.IsDBNull(45) ? null : DateTime.Parse(reader.GetString(45)),
                CheckIn = reader.IsDBNull(46) ? null : DateTime.Parse(reader.GetString(46)),
                CheckOut = reader.IsDBNull(47) ? null : DateTime.Parse(reader.GetString(47)),
                CheckInObservacoes = reader.GetString(48),
                CheckOutObservacoes = reader.GetString(49),
                CheckInFotos = reader.GetString(50),
                CheckOutFotos = reader.GetString(51),
                ValorProdutos = ReadDecimal(reader, 52),
                ValorServicos = ReadDecimal(reader, 53),
                Recorrente = reader.GetInt32(54) == 1,
                TipoRecorrencia = reader.GetString(55),
                IntervaloRecorrencia = reader.GetInt32(56),
                ProximaRecorrencia = reader.IsDBNull(57) ? null : DateTime.Parse(reader.GetString(57)),
                LembreteWhatsApp = reader.GetInt32(58) == 1,
                LembreteEmail = reader.GetInt32(59) == 1,
                DataLembrete = reader.IsDBNull(60) ? null : DateTime.Parse(reader.GetString(60)),
                LembreteEnviado = reader.GetInt32(61) == 1,
                AlertaAtraso = reader.GetInt32(62) == 1,
                AlertaPecaFaltando = reader.GetInt32(63) == 1,
                AlertaPronto = reader.GetInt32(64) == 1,
                AvaliacaoCliente = reader.GetInt32(65),
                AvaliacaoComentario = reader.GetString(66),
                DataCancelamento = reader.IsDBNull(67) ? null : DateTime.Parse(reader.GetString(67)),
                MotivoCancelamento = reader.IsDBNull(68) ? string.Empty : reader.GetString(68),
                CanceladoPor = reader.IsDBNull(69) ? null : Guid.Parse(reader.GetString(69)),
                DataReagendamento = reader.IsDBNull(70) ? null : DateTime.Parse(reader.GetString(70)),
                DataAgendamentoAnterior = reader.IsDBNull(71) ? null : DateTime.Parse(reader.GetString(71)),
                MotivoReagendamento = reader.IsDBNull(72) ? string.Empty : reader.GetString(72)
            };
        }

        private static decimal ReadDecimal(DbDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? 0m : Convert.ToDecimal(reader.GetValue(index));
        }
    }
}

