using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimoAutoEletrica.Services
{
    public sealed class CaixaService
    {
        private readonly DatabaseService _databaseService;

        public CaixaService(DatabaseService? databaseService = null)
        {
            _databaseService = databaseService ?? global::PrimoAutoEletrica.App.Database;

            // Garante que o financeiro esteja pronto antes das integracoes de caixa.
            _ = new FinanceiroDatabaseService();
        }

        public CaixaSessaoOperacional? ObterSessaoAbertaAtual()
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();
            return ObterSessaoAbertaAtual(connection, transaction: null);
        }

        public CaixaSessaoOperacional? ObterSessaoPorId(Guid sessaoId)
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();
            return ObterSessaoPorId(connection, transaction: null, sessaoId);
        }

        public List<CaixaMovimentacaoOperacional> ObterMovimentacoesSessao(Guid sessaoId)
        {
            var movimentacoes = new List<CaixaMovimentacaoOperacional>();

            using var connection = _databaseService.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, CaixaSessaoId, Data, Tipo, ValorMovimento, ValorInicial, ValorFinal,
                       Sangrias, Suprimentos, Diferenca, COALESCE(Operador, ''), COALESCE(FormaPagamento, ''),
                       COALESCE(ReferenciaId, ''), COALESCE(Observacoes, '')
                FROM MovimentacoesCaixa
                WHERE CaixaSessaoId = @caixaSessaoId
                ORDER BY Data DESC;";
            command.Parameters.AddWithValue("@caixaSessaoId", sessaoId.ToString());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                movimentacoes.Add(MapearMovimentacao(reader));
            }

            return movimentacoes;
        }

        public CaixaSessaoOperacional AbrirCaixa(decimal valorAbertura, string observacoes = "", string numeroCaixa = "01")
        {
            ComercialValidationHelper.GarantirValorMaiorOuIgualZero(valorAbertura, "o valor de abertura do caixa");
            var operador = ObterOperadorAtual();

            using var connection = _databaseService.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var sessaoExistente = ObterSessaoAbertaAtual(connection, transaction);
            if (sessaoExistente != null)
            {
                throw new InvalidOperationException("Ja existe um caixa aberto para este operador.");
            }

            var sessao = new CaixaSessaoOperacional
            {
                Id = Guid.NewGuid(),
                NumeroCaixa = string.IsNullOrWhiteSpace(numeroCaixa) ? "01" : numeroCaixa.Trim(),
                DataAbertura = DateTime.Now,
                OperadorId = operador.id,
                OperadorNome = operador.nome,
                PerfilOperador = operador.perfil,
                ValorAbertura = valorAbertura,
                ValorEsperado = valorAbertura,
                TotalVendas = 0m,
                TotalSangrias = 0m,
                TotalSuprimentos = 0m,
                QuantidadeVendas = 0,
                Status = "Aberto",
                Observacoes = observacoes?.Trim() ?? string.Empty,
                DataCriacao = DateTime.Now,
                DataUltimaMovimentacao = DateTime.Now
            };

            InserirSessao(connection, transaction, sessao);
            InserirMovimentacaoCaixa(
                connection,
                transaction,
                sessao.Id,
                "Abertura",
                valorAbertura,
                0m,
                valorAbertura,
                0m,
                valorAbertura,
                valorAbertura,
                operador.nome,
                "Dinheiro",
                string.Empty,
                string.IsNullOrWhiteSpace(observacoes) ? "Abertura operacional do caixa." : observacoes);

            transaction.Commit();

            global::PrimoAutoEletrica.App.Audit.Registrar(
                categoria: "Caixa",
                acao: "AberturaCaixa",
                entidade: "CaixaSessao",
                entidadeId: sessao.Id.ToString(),
                detalhes: $"Numero={sessao.NumeroCaixa}; Operador={operador.nome}; ValorAbertura={valorAbertura:C}",
                valorNovo: valorAbertura.ToString("F2"));

            return sessao;
        }

        public CaixaSessaoOperacional RegistrarSuprimento(decimal valor, string observacoes = "", string formaPagamento = "Dinheiro")
        {
            ComercialValidationHelper.GarantirValorMaiorQueZero(valor, "o valor do suprimento");
            using var connection = _databaseService.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var sessao = ObterSessaoAbertaObrigatoria(connection, transaction);
            var saldoAntes = sessao.ValorEsperado;
            var saldoDepois = saldoAntes + valor;
            var operador = ObterOperadorAtual();

            AtualizarSessao(
                connection,
                transaction,
                sessao.Id,
                saldoDepois,
                sessao.TotalVendas,
                sessao.TotalSangrias,
                sessao.TotalSuprimentos + valor,
                sessao.QuantidadeVendas,
                "Aberto",
                sessao.Observacoes,
                null,
                null);

            InserirMovimentacaoCaixa(
                connection,
                transaction,
                sessao.Id,
                "Suprimento",
                valor,
                saldoAntes,
                saldoDepois,
                0m,
                valor,
                valor,
                operador.nome,
                formaPagamento,
                string.Empty,
                observacoes);

            InserirMovimentacaoFinanceiraSeNaoExistir(
                connection,
                transaction,
                "Entrada",
                "Suprimento de caixa",
                valor,
                DateTime.Now,
                "Caixa",
                formaPagamento,
                observacoes,
                "CaixaSuprimento",
                $"{sessao.Id}:suprimento:{DateTime.Now:yyyyMMddHHmmssfff}");

            transaction.Commit();

            global::PrimoAutoEletrica.App.Audit.Registrar(
                categoria: "Caixa",
                acao: "SuprimentoCaixa",
                entidade: "CaixaSessao",
                entidadeId: sessao.Id.ToString(),
                detalhes: $"Valor={valor:C}; SaldoAntes={saldoAntes:C}; SaldoDepois={saldoDepois:C}; Observacoes={observacoes}",
                valorAnterior: saldoAntes.ToString("F2"),
                valorNovo: saldoDepois.ToString("F2"));

            return ObterSessaoPorId(sessao.Id) ?? sessao;
        }

        public CaixaSessaoOperacional RegistrarSangria(decimal valor, string observacoes = "", string formaPagamento = "Dinheiro")
        {
            ComercialValidationHelper.GarantirValorMaiorQueZero(valor, "o valor da sangria");
            using var connection = _databaseService.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var sessao = ObterSessaoAbertaObrigatoria(connection, transaction);
            if (valor > sessao.ValorEsperado)
            {
                throw new InvalidOperationException("Nao e possivel registrar uma sangria maior que o saldo esperado do caixa.");
            }

            var saldoAntes = sessao.ValorEsperado;
            var saldoDepois = saldoAntes - valor;
            var operador = ObterOperadorAtual();

            AtualizarSessao(
                connection,
                transaction,
                sessao.Id,
                saldoDepois,
                sessao.TotalVendas,
                sessao.TotalSangrias + valor,
                sessao.TotalSuprimentos,
                sessao.QuantidadeVendas,
                "Aberto",
                sessao.Observacoes,
                null,
                null);

            InserirMovimentacaoCaixa(
                connection,
                transaction,
                sessao.Id,
                "Sangria",
                valor,
                saldoAntes,
                saldoDepois,
                valor,
                0m,
                -valor,
                operador.nome,
                formaPagamento,
                string.Empty,
                observacoes);

            InserirMovimentacaoFinanceiraSeNaoExistir(
                connection,
                transaction,
                "Saida",
                "Sangria de caixa",
                valor,
                DateTime.Now,
                "Caixa",
                formaPagamento,
                observacoes,
                "CaixaSangria",
                $"{sessao.Id}:sangria:{DateTime.Now:yyyyMMddHHmmssfff}");

            transaction.Commit();

            global::PrimoAutoEletrica.App.Audit.Registrar(
                categoria: "Caixa",
                acao: "SangriaCaixa",
                entidade: "CaixaSessao",
                entidadeId: sessao.Id.ToString(),
                detalhes: $"Valor={valor:C}; SaldoAntes={saldoAntes:C}; SaldoDepois={saldoDepois:C}; Observacoes={observacoes}",
                valorAnterior: saldoAntes.ToString("F2"),
                valorNovo: saldoDepois.ToString("F2"));

            return ObterSessaoPorId(sessao.Id) ?? sessao;
        }

        public CaixaSessaoOperacional FecharCaixa(decimal valorInformado, string observacoes = "")
        {
            ComercialValidationHelper.GarantirValorMaiorOuIgualZero(valorInformado, "o valor informado no fechamento do caixa");
            using var connection = _databaseService.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var sessao = ObterSessaoAbertaObrigatoria(connection, transaction);
            var operador = ObterOperadorAtual();
            var saldoEsperado = sessao.ValorEsperado;
            var diferenca = valorInformado - saldoEsperado;

            AtualizarSessao(
                connection,
                transaction,
                sessao.Id,
                saldoEsperado,
                sessao.TotalVendas,
                sessao.TotalSangrias,
                sessao.TotalSuprimentos,
                sessao.QuantidadeVendas,
                "Fechado",
                string.IsNullOrWhiteSpace(observacoes) ? sessao.Observacoes : observacoes.Trim(),
                DateTime.Now,
                valorInformado);

            InserirMovimentacaoCaixa(
                connection,
                transaction,
                sessao.Id,
                "Fechamento",
                valorInformado,
                saldoEsperado,
                valorInformado,
                sessao.TotalSangrias,
                sessao.TotalSuprimentos,
                diferenca,
                operador.nome,
                "Dinheiro",
                string.Empty,
                observacoes);

            transaction.Commit();

            global::PrimoAutoEletrica.App.Audit.Registrar(
                categoria: "Caixa",
                acao: "FechamentoCaixa",
                entidade: "CaixaSessao",
                entidadeId: sessao.Id.ToString(),
                detalhes: $"ValorEsperado={saldoEsperado:C}; ValorInformado={valorInformado:C}; Diferenca={diferenca:C}; Operador={operador.nome}; Observacoes={observacoes}",
                valorAnterior: saldoEsperado.ToString("F2"),
                valorNovo: valorInformado.ToString("F2"));

            return ObterSessaoPorId(sessao.Id) ?? sessao;
        }

        internal void RegistrarVendaNaSessao(SqliteConnection connection, SqliteTransaction transaction, Venda venda)
        {
            if (venda.CaixaSessaoId == null || venda.CaixaSessaoId == Guid.Empty)
            {
                return;
            }

            var sessao = ObterSessaoPorId(connection, transaction, venda.CaixaSessaoId.Value)
                ?? throw new InvalidOperationException("A sessao de caixa vinculada a venda nao foi localizada.");

            if (!sessao.Aberto)
            {
                throw new InvalidOperationException("Nao e possivel registrar venda em um caixa fechado.");
            }

            var saldoAntes = sessao.ValorEsperado;
            var saldoDepois = saldoAntes + venda.Total;

            AtualizarSessao(
                connection,
                transaction,
                sessao.Id,
                saldoDepois,
                sessao.TotalVendas + venda.Total,
                sessao.TotalSangrias,
                sessao.TotalSuprimentos,
                sessao.QuantidadeVendas + 1,
                "Aberto",
                sessao.Observacoes,
                null,
                null);

            InserirMovimentacaoCaixa(
                connection,
                transaction,
                sessao.Id,
                "Venda",
                venda.Total,
                saldoAntes,
                saldoDepois,
                0m,
                0m,
                venda.Total,
                venda.Usuario,
                venda.FormaPagamento,
                venda.Id.ToString(),
                $"Venda PDV registrada para {venda.Cliente?.Nome ?? "consumidor final"}.");

            InserirMovimentacaoFinanceiraSeNaoExistir(
                connection,
                transaction,
                "Entrada",
                $"Venda PDV {venda.Id.ToString()[..8]}",
                venda.Total,
                venda.Data,
                "PDV",
                venda.FormaPagamento,
                $"Cliente={venda.Cliente?.Nome ?? "Nao informado"}; SessaoCaixa={sessao.NumeroCaixa}",
                "PDVMovimentacao",
                venda.Id.ToString());
        }

        internal void CancelarVendaNaSessao(SqliteConnection connection, SqliteTransaction transaction, Venda venda, string motivo)
        {
            if (venda.CaixaSessaoId == null || venda.CaixaSessaoId == Guid.Empty)
            {
                return;
            }

            var sessao = ObterSessaoPorId(connection, transaction, venda.CaixaSessaoId.Value)
                ?? throw new InvalidOperationException("A sessao de caixa vinculada a venda nao foi localizada para cancelamento.");

            if (!sessao.Aberto)
            {
                throw new InvalidOperationException("Nao e possivel cancelar uma venda vinculada a um caixa ja fechado.");
            }

            if (venda.Total > sessao.ValorEsperado)
            {
                throw new InvalidOperationException("O caixa nao possui saldo suficiente para estornar esta venda.");
            }

            var saldoAntes = sessao.ValorEsperado;
            var saldoDepois = saldoAntes - venda.Total;

            AtualizarSessao(
                connection,
                transaction,
                sessao.Id,
                saldoDepois,
                Math.Max(0m, sessao.TotalVendas - venda.Total),
                sessao.TotalSangrias,
                sessao.TotalSuprimentos,
                Math.Max(0, sessao.QuantidadeVendas - 1),
                "Aberto",
                sessao.Observacoes,
                null,
                null);

            InserirMovimentacaoCaixa(
                connection,
                transaction,
                sessao.Id,
                "CancelamentoVenda",
                venda.Total,
                saldoAntes,
                saldoDepois,
                0m,
                0m,
                -venda.Total,
                ObterOperadorAtual().nome,
                venda.FormaPagamento,
                venda.Id.ToString(),
                string.IsNullOrWhiteSpace(motivo) ? "Cancelamento de venda registrado no PDV." : motivo);

            InserirMovimentacaoFinanceiraSeNaoExistir(
                connection,
                transaction,
                "Saida",
                $"Estorno venda PDV {venda.Id.ToString()[..8]}",
                venda.Total,
                DateTime.Now,
                "PDV",
                venda.FormaPagamento,
                motivo,
                "PDVCancelamento",
                venda.Id.ToString());
        }

        private CaixaSessaoOperacional ObterSessaoAbertaObrigatoria(SqliteConnection connection, SqliteTransaction transaction)
        {
            return ObterSessaoAbertaAtual(connection, transaction)
                ?? throw new InvalidOperationException("Nenhum caixa aberto foi encontrado para o operador atual.");
        }

        private CaixaSessaoOperacional? ObterSessaoAbertaAtual(SqliteConnection connection, SqliteTransaction? transaction)
        {
            var operador = ObterOperadorAtual();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT Id, NumeroCaixa, DataAbertura, DataFechamento, OperadorId, OperadorNome, PerfilOperador,
                       ValorAbertura, ValorEsperado, ValorInformadoFechamento, TotalVendas, TotalSangrias,
                       TotalSuprimentos, QuantidadeVendas, Status, COALESCE(Observacoes, ''),
                       DataCriacao, DataUltimaMovimentacao
                FROM CaixaSessoes
                WHERE Status = 'Aberto'
                  AND (
                        (@operadorId IS NOT NULL AND OperadorId = @operadorId)
                        OR (@operadorId IS NULL AND OperadorNome = @operadorNome)
                      )
                ORDER BY DataAbertura DESC
                LIMIT 1;";
            command.Parameters.AddWithValue("@operadorId", operador.id.HasValue ? operador.id.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@operadorNome", operador.nome);

            using var reader = command.ExecuteReader();
            return reader.Read() ? MapearSessao(reader) : null;
        }

        private static CaixaSessaoOperacional? ObterSessaoPorId(SqliteConnection connection, SqliteTransaction? transaction, Guid sessaoId)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT Id, NumeroCaixa, DataAbertura, DataFechamento, OperadorId, OperadorNome, PerfilOperador,
                       ValorAbertura, ValorEsperado, ValorInformadoFechamento, TotalVendas, TotalSangrias,
                       TotalSuprimentos, QuantidadeVendas, Status, COALESCE(Observacoes, ''),
                       DataCriacao, DataUltimaMovimentacao
                FROM CaixaSessoes
                WHERE Id = @id
                LIMIT 1;";
            command.Parameters.AddWithValue("@id", sessaoId.ToString());

            using var reader = command.ExecuteReader();
            return reader.Read() ? MapearSessao(reader) : null;
        }

        private static void InserirSessao(SqliteConnection connection, SqliteTransaction transaction, CaixaSessaoOperacional sessao)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                INSERT INTO CaixaSessoes
                (
                    Id, NumeroCaixa, DataAbertura, DataFechamento, OperadorId, OperadorNome, PerfilOperador,
                    ValorAbertura, ValorEsperado, ValorInformadoFechamento, TotalVendas, TotalSangrias,
                    TotalSuprimentos, QuantidadeVendas, Status, Observacoes, DataCriacao, DataUltimaMovimentacao
                )
                VALUES
                (
                    @id, @numeroCaixa, @dataAbertura, @dataFechamento, @operadorId, @operadorNome, @perfilOperador,
                    @valorAbertura, @valorEsperado, @valorInformadoFechamento, @totalVendas, @totalSangrias,
                    @totalSuprimentos, @quantidadeVendas, @status, @observacoes, @dataCriacao, @dataUltimaMovimentacao
                );";
            command.Parameters.AddWithValue("@id", sessao.Id.ToString());
            command.Parameters.AddWithValue("@numeroCaixa", sessao.NumeroCaixa);
            command.Parameters.AddWithValue("@dataAbertura", sessao.DataAbertura.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@dataFechamento", sessao.DataFechamento?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@operadorId", sessao.OperadorId.HasValue ? sessao.OperadorId.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@operadorNome", sessao.OperadorNome);
            command.Parameters.AddWithValue("@perfilOperador", ToDbNullableString(sessao.PerfilOperador));
            command.Parameters.AddWithValue("@valorAbertura", sessao.ValorAbertura);
            command.Parameters.AddWithValue("@valorEsperado", sessao.ValorEsperado);
            command.Parameters.AddWithValue("@valorInformadoFechamento", sessao.ValorInformadoFechamento.HasValue ? sessao.ValorInformadoFechamento.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@totalVendas", sessao.TotalVendas);
            command.Parameters.AddWithValue("@totalSangrias", sessao.TotalSangrias);
            command.Parameters.AddWithValue("@totalSuprimentos", sessao.TotalSuprimentos);
            command.Parameters.AddWithValue("@quantidadeVendas", sessao.QuantidadeVendas);
            command.Parameters.AddWithValue("@status", sessao.Status);
            command.Parameters.AddWithValue("@observacoes", ToDbNullableString(sessao.Observacoes));
            command.Parameters.AddWithValue("@dataCriacao", sessao.DataCriacao.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@dataUltimaMovimentacao", sessao.DataUltimaMovimentacao?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.ExecuteNonQuery();
        }

        private static void AtualizarSessao(
            SqliteConnection connection,
            SqliteTransaction transaction,
            Guid sessaoId,
            decimal valorEsperado,
            decimal totalVendas,
            decimal totalSangrias,
            decimal totalSuprimentos,
            int quantidadeVendas,
            string status,
            string observacoes,
            DateTime? dataFechamento,
            decimal? valorInformadoFechamento)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                UPDATE CaixaSessoes
                SET ValorEsperado = @valorEsperado,
                    TotalVendas = @totalVendas,
                    TotalSangrias = @totalSangrias,
                    TotalSuprimentos = @totalSuprimentos,
                    QuantidadeVendas = @quantidadeVendas,
                    Status = @status,
                    Observacoes = @observacoes,
                    DataFechamento = @dataFechamento,
                    ValorInformadoFechamento = @valorInformadoFechamento,
                    DataUltimaMovimentacao = @dataUltimaMovimentacao
                WHERE Id = @id;";
            command.Parameters.AddWithValue("@id", sessaoId.ToString());
            command.Parameters.AddWithValue("@valorEsperado", valorEsperado);
            command.Parameters.AddWithValue("@totalVendas", totalVendas);
            command.Parameters.AddWithValue("@totalSangrias", totalSangrias);
            command.Parameters.AddWithValue("@totalSuprimentos", totalSuprimentos);
            command.Parameters.AddWithValue("@quantidadeVendas", quantidadeVendas);
            command.Parameters.AddWithValue("@status", status);
            command.Parameters.AddWithValue("@observacoes", ToDbNullableString(observacoes));
            command.Parameters.AddWithValue("@dataFechamento", dataFechamento?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@valorInformadoFechamento", valorInformadoFechamento.HasValue ? valorInformadoFechamento.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@dataUltimaMovimentacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.ExecuteNonQuery();
        }

        private static void InserirMovimentacaoCaixa(
            SqliteConnection connection,
            SqliteTransaction transaction,
            Guid caixaSessaoId,
            string tipo,
            decimal valorMovimento,
            decimal valorInicial,
            decimal valorFinal,
            decimal sangrias,
            decimal suprimentos,
            decimal diferenca,
            string operador,
            string formaPagamento,
            string referenciaId,
            string observacoes)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                INSERT INTO MovimentacoesCaixa
                (
                    Id, CaixaSessaoId, Data, Tipo, ValorMovimento, ValorInicial, ValorFinal,
                    Sangrias, Suprimentos, Diferenca, Operador, FormaPagamento, ReferenciaId, Observacoes
                )
                VALUES
                (
                    @id, @caixaSessaoId, @data, @tipo, @valorMovimento, @valorInicial, @valorFinal,
                    @sangrias, @suprimentos, @diferenca, @operador, @formaPagamento, @referenciaId, @observacoes
                );";
            command.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("@caixaSessaoId", caixaSessaoId.ToString());
            command.Parameters.AddWithValue("@data", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@tipo", tipo);
            command.Parameters.AddWithValue("@valorMovimento", valorMovimento);
            command.Parameters.AddWithValue("@valorInicial", valorInicial);
            command.Parameters.AddWithValue("@valorFinal", valorFinal);
            command.Parameters.AddWithValue("@sangrias", sangrias);
            command.Parameters.AddWithValue("@suprimentos", suprimentos);
            command.Parameters.AddWithValue("@diferenca", diferenca);
            command.Parameters.AddWithValue("@operador", ToDbNullableString(operador));
            command.Parameters.AddWithValue("@formaPagamento", ToDbNullableString(formaPagamento));
            command.Parameters.AddWithValue("@referenciaId", ToDbNullableString(referenciaId));
            command.Parameters.AddWithValue("@observacoes", ToDbNullableString(observacoes));
            command.ExecuteNonQuery();
        }

        private static void InserirMovimentacaoFinanceiraSeNaoExistir(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string tipo,
            string descricao,
            decimal valor,
            DateTime data,
            string categoria,
            string formaPagamento,
            string observacoes,
            string origem,
            string referenciaExterna)
        {
            if (ExisteMovimentacaoFinanceira(connection, transaction, origem, referenciaExterna))
            {
                return;
            }

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                INSERT INTO MovimentacoesFinanceiras
                (
                    Tipo,
                    Descricao,
                    Valor,
                    Data,
                    Categoria,
                    FormaPagamento,
                    ReferenciaId,
                    Observacoes,
                    DataCriacao,
                    Origem,
                    ReferenciaExterna
                )
                VALUES
                (
                    @tipo,
                    @descricao,
                    @valor,
                    @data,
                    @categoria,
                    @formaPagamento,
                    NULL,
                    @observacoes,
                    @dataCriacao,
                    @origem,
                    @referenciaExterna
                );";
            command.Parameters.AddWithValue("@tipo", tipo);
            command.Parameters.AddWithValue("@descricao", descricao);
            command.Parameters.AddWithValue("@valor", valor);
            command.Parameters.AddWithValue("@data", data.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@categoria", ToDbNullableString(categoria));
            command.Parameters.AddWithValue("@formaPagamento", ToDbNullableString(formaPagamento));
            command.Parameters.AddWithValue("@observacoes", ToDbNullableString(observacoes));
            command.Parameters.AddWithValue("@dataCriacao", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@origem", ToDbNullableString(origem));
            command.Parameters.AddWithValue("@referenciaExterna", ToDbNullableString(referenciaExterna));
            command.ExecuteNonQuery();
        }

        private static bool ExisteMovimentacaoFinanceira(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string origem,
            string referenciaExterna)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"
                SELECT 1
                FROM MovimentacoesFinanceiras
                WHERE Origem = @origem
                  AND ReferenciaExterna = @referenciaExterna
                LIMIT 1;";
            command.Parameters.AddWithValue("@origem", origem);
            command.Parameters.AddWithValue("@referenciaExterna", referenciaExterna);
            return command.ExecuteScalar() != null;
        }

        private static CaixaSessaoOperacional MapearSessao(SqliteDataReader reader)
        {
            return new CaixaSessaoOperacional
            {
                Id = Guid.Parse(reader.GetString(0)),
                NumeroCaixa = reader.IsDBNull(1) ? "01" : reader.GetString(1),
                DataAbertura = LerData(reader, 2),
                DataFechamento = LerDataNullable(reader, 3),
                OperadorId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                OperadorNome = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                PerfilOperador = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                ValorAbertura = LerDecimal(reader, 7),
                ValorEsperado = LerDecimal(reader, 8),
                ValorInformadoFechamento = reader.IsDBNull(9) ? null : LerDecimal(reader, 9),
                TotalVendas = LerDecimal(reader, 10),
                TotalSangrias = LerDecimal(reader, 11),
                TotalSuprimentos = LerDecimal(reader, 12),
                QuantidadeVendas = reader.IsDBNull(13) ? 0 : reader.GetInt32(13),
                Status = reader.IsDBNull(14) ? "Fechado" : reader.GetString(14),
                Observacoes = reader.IsDBNull(15) ? string.Empty : reader.GetString(15),
                DataCriacao = LerData(reader, 16),
                DataUltimaMovimentacao = LerDataNullable(reader, 17)
            };
        }

        private static CaixaMovimentacaoOperacional MapearMovimentacao(SqliteDataReader reader)
        {
            return new CaixaMovimentacaoOperacional
            {
                Id = Guid.Parse(reader.GetString(0)),
                CaixaSessaoId = Guid.Parse(reader.GetString(1)),
                Data = LerData(reader, 2),
                Tipo = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                ValorMovimento = LerDecimal(reader, 4),
                ValorInicial = LerDecimal(reader, 5),
                ValorFinal = LerDecimal(reader, 6),
                Sangrias = LerDecimal(reader, 7),
                Suprimentos = LerDecimal(reader, 8),
                Diferenca = LerDecimal(reader, 9),
                Operador = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                FormaPagamento = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                ReferenciaId = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                Observacoes = reader.IsDBNull(13) ? string.Empty : reader.GetString(13)
            };
        }

        private static DateTime LerData(SqliteDataReader reader, int ordinal)
        {
            return DateTime.TryParse(reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal), out var data)
                ? data
                : DateTime.Now;
        }

        private static DateTime? LerDataNullable(SqliteDataReader reader, int ordinal)
        {
            return DateTime.TryParse(reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal), out var data)
                ? data
                : null;
        }

        private static decimal LerDecimal(SqliteDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return 0m;
            }

            var value = reader.GetValue(ordinal);
            return value switch
            {
                decimal decimalValue => decimalValue,
                double doubleValue => Convert.ToDecimal(doubleValue),
                float floatValue => Convert.ToDecimal(floatValue),
                long longValue => longValue,
                int intValue => intValue,
                _ => Convert.ToDecimal(value)
            };
        }

        private static (int? id, string nome, string perfil) ObterOperadorAtual()
        {
            return (
                global::PrimoAutoEletrica.App.Session.UserId,
                string.IsNullOrWhiteSpace(global::PrimoAutoEletrica.App.Session.UserName) ? "Sistema" : global::PrimoAutoEletrica.App.Session.UserName,
                global::PrimoAutoEletrica.App.Session.AccessProfile);
        }

        private static object ToDbNullableString(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
        }
    }
}
