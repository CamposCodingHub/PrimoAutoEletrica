using System;
using System.Collections.Generic;
using System.Linq;
using PRIMOX.Domain.Enums;
using PRIMOX.Domain.Exceptions;
using PRIMOX.Domain.Interfaces;
using PRIMOX.Domain.ValueObjects;

namespace PRIMOX.Domain.Entities
{
    /// <summary>
    /// Agregado Raiz central do ecossistema PRIMOX 3.0.
    /// Orquestra todo o ciclo operacional, diagnóstico, orçamentário e de execução da oficina.
    /// Protege rigidamente suas invariantes de negócio e a máquina de estados.
    /// </summary>
    public class OrdemServico
    {
        private readonly List<ItemPecaOS> _itensPeca = new();
        private readonly List<ItemServicoOS> _itensServico = new();
        private readonly List<HistoricoStatusOS> _historico = new();

        public Guid Id { get; private set; }
        public string Numero { get; private set; }
        public Guid TenantId { get; private set; }
        public Guid FilialId { get; private set; }
        public Guid ClienteId { get; private set; }
        public Guid VeiculoId { get; private set; }

        public string ClienteNomeSnapshot { get; private set; }
        public string VeiculoPlacaSnapshot { get; private set; }
        public string VeiculoModeloSnapshot { get; private set; }

        public StatusOrdemServico Status { get; private set; }
        public PrioridadeOS Prioridade { get; private set; }

        public string QueixaCliente { get; private set; }
        public string DiagnosticoTecnico { get; private set; }
        public string ObservacoesInternas { get; private set; }

        public DateTimeOffset DataAbertura { get; private set; }
        public DateTimeOffset? DataPrevisaoConclusao { get; private set; }
        public DateTimeOffset? DataConclusao { get; private set; }

        public Money Desconto { get; private set; }

        /// <summary>
        /// Espaço arquitetural preparado para posterior acoplamento do Diagnostic Evidence Engine.
        /// </summary>
        public Guid? SessaoDiagnosticoId { get; private set; }

        public IReadOnlyCollection<ItemPecaOS> ItensPeca => _itensPeca.AsReadOnly();
        public IReadOnlyCollection<ItemServicoOS> ItensServico => _itensServico.AsReadOnly();
        public IReadOnlyCollection<HistoricoStatusOS> Historico => _historico.AsReadOnly();

        public Money TotalPecas => _itensPeca.Aggregate(
            Money.Zero(Desconto.Currency),
            (acc, p) => acc + p.Subtotal);

        public Money TotalServicos => _itensServico.Aggregate(
            Money.Zero(Desconto.Currency),
            (acc, s) => acc + s.Subtotal);

        public Money TotalBruto => TotalPecas + TotalServicos;

        public Money TotalLiquido
        {
            get
            {
                var total = TotalBruto - Desconto;
                return total.IsNegative ? Money.Zero(Desconto.Currency) : total;
            }
        }

        private OrdemServico()
        {
            Numero = string.Empty;
            ClienteNomeSnapshot = string.Empty;
            VeiculoPlacaSnapshot = string.Empty;
            VeiculoModeloSnapshot = string.Empty;
            QueixaCliente = string.Empty;
            DiagnosticoTecnico = string.Empty;
            ObservacoesInternas = string.Empty;
            Desconto = Money.Zero();
        }

        public OrdemServico(
            Guid id,
            string numero,
            Guid clienteId,
            Guid veiculoId,
            string queixaCliente,
            ITimeProvider timeProvider,
            PrioridadeOS prioridade = PrioridadeOS.Normal,
            string clienteNomeSnapshot = "",
            string veiculoPlacaSnapshot = "",
            string veiculoModeloSnapshot = "",
            Guid? tenantId = null,
            Guid? filialId = null)
        {
            if (id == Guid.Empty) throw new RegraNegocioException("Id da Ordem de Serviço não pode ser vazio.");
            if (string.IsNullOrWhiteSpace(numero)) throw new RegraNegocioException("Número da Ordem de Serviço é obrigatório.");
            if (clienteId == Guid.Empty) throw new RegraNegocioException("ClienteId é obrigatório para abertura da OS.");
            if (veiculoId == Guid.Empty) throw new RegraNegocioException("VeiculoId é obrigatório para abertura da OS.");
            if (string.IsNullOrWhiteSpace(queixaCliente)) throw new RegraNegocioException("Queixa principal do cliente é obrigatória.");
            if (timeProvider == null) throw new ArgumentNullException(nameof(timeProvider));

            Id = id;
            Numero = numero.Trim();
            ClienteId = clienteId;
            VeiculoId = veiculoId;
            QueixaCliente = queixaCliente.Trim();
            Prioridade = prioridade;
            ClienteNomeSnapshot = clienteNomeSnapshot?.Trim() ?? string.Empty;
            VeiculoPlacaSnapshot = veiculoPlacaSnapshot?.Trim() ?? string.Empty;
            VeiculoModeloSnapshot = veiculoModeloSnapshot?.Trim() ?? string.Empty;

            TenantId = tenantId ?? Guid.Empty;
            FilialId = filialId ?? Guid.Empty;

            Status = StatusOrdemServico.Aberta;
            DiagnosticoTecnico = string.Empty;
            ObservacoesInternas = string.Empty;
            Desconto = Money.Zero();

            DataAbertura = timeProvider.GetUtcNow();

            _historico.Add(new HistoricoStatusOS(
                Guid.NewGuid(),
                Id,
                StatusOrdemServico.Aberta,
                StatusOrdemServico.Aberta,
                "Abertura da Ordem de Serviço",
                "Recepção",
                DataAbertura));
        }

        /// <summary>
        /// Reconstrói o Agregado Raiz a partir da camada de persistência sem disparar eventos de negócio.
        /// </summary>
        public static OrdemServico Reconstituir(
            Guid id,
            string numero,
            Guid clienteId,
            Guid veiculoId,
            string clienteNomeSnapshot,
            string veiculoPlacaSnapshot,
            string veiculoModeloSnapshot,
            StatusOrdemServico status,
            PrioridadeOS prioridade,
            string queixaCliente,
            string diagnosticoTecnico,
            string observacoesInternas,
            DateTimeOffset dataAbertura,
            DateTimeOffset? dataPrevisaoConclusao,
            DateTimeOffset? dataConclusao,
            Money desconto,
            Guid? sessaoDiagnosticoId,
            Guid tenantId,
            Guid filialId,
            IEnumerable<ItemPecaOS> itensPeca,
            IEnumerable<ItemServicoOS> itensServico,
            IEnumerable<HistoricoStatusOS> historico)
        {
            var os = new OrdemServico
            {
                Id = id,
                Numero = numero,
                ClienteId = clienteId,
                VeiculoId = veiculoId,
                ClienteNomeSnapshot = clienteNomeSnapshot ?? string.Empty,
                VeiculoPlacaSnapshot = veiculoPlacaSnapshot ?? string.Empty,
                VeiculoModeloSnapshot = veiculoModeloSnapshot ?? string.Empty,
                Status = status,
                Prioridade = prioridade,
                QueixaCliente = queixaCliente ?? string.Empty,
                DiagnosticoTecnico = diagnosticoTecnico ?? string.Empty,
                ObservacoesInternas = observacoesInternas ?? string.Empty,
                DataAbertura = dataAbertura,
                DataPrevisaoConclusao = dataPrevisaoConclusao,
                DataConclusao = dataConclusao,
                Desconto = desconto,
                SessaoDiagnosticoId = sessaoDiagnosticoId,
                TenantId = tenantId,
                FilialId = filialId
            };

            if (itensPeca != null)
            {
                os._itensPeca.AddRange(itensPeca);
            }

            if (itensServico != null)
            {
                os._itensServico.AddRange(itensServico);
            }

            if (historico != null)
            {
                os._historico.AddRange(historico);
            }

            return os;
        }

        public void AdicionarPeca(
            string descricao,
            decimal quantidade,
            Money valorUnitario,
            string codigo = "",
            Money? custoUnitario = null,
            Guid? produtoId = null)
        {
            GarantirModificacaoPermitida();

            var item = new ItemPecaOS(
                Guid.NewGuid(),
                Id,
                descricao,
                quantidade,
                valorUnitario,
                codigo,
                custoUnitario,
                produtoId);

            _itensPeca.Add(item);
        }

        public void AdicionarServico(
            string descricao,
            decimal quantidadeHoras,
            Money valorHora,
            string? tecnicoResponsavel = null,
            Guid? servicoId = null)
        {
            GarantirModificacaoPermitida();

            var item = new ItemServicoOS(
                Guid.NewGuid(),
                Id,
                descricao,
                quantidadeHoras,
                valorHora,
                tecnicoResponsavel,
                servicoId);

            _itensServico.Add(item);
        }

        public void RemoverItemPeca(Guid itemId)
        {
            GarantirModificacaoPermitida();
            var item = _itensPeca.FirstOrDefault(p => p.Id == itemId);
            if (item != null)
            {
                _itensPeca.Remove(item);
            }
        }

        public void RemoverItemServico(Guid itemId)
        {
            GarantirModificacaoPermitida();
            var item = _itensServico.FirstOrDefault(s => s.Id == itemId);
            if (item != null)
            {
                _itensServico.Remove(item);
            }
        }

        public void AplicarDesconto(Money desconto)
        {
            GarantirModificacaoPermitida();
            if (desconto.IsNegative)
                throw new RegraNegocioException("Desconto não pode ser negativo.");

            if (desconto > TotalBruto)
                throw new RegraNegocioException($"Desconto ({desconto}) não pode exceder o total bruto da OS ({TotalBruto}).");

            Desconto = desconto;
        }

        public void AtualizarDiagnostico(string diagnostico)
        {
            GarantirModificacaoPermitida();
            DiagnosticoTecnico = diagnostico?.Trim() ?? string.Empty;
        }

        public void AtualizarObservacoesInternas(string observacoes)
        {
            ObservacoesInternas = observacoes?.Trim() ?? string.Empty;
        }

        public void DefinirPrevisaoConclusao(DateTimeOffset previsao)
        {
            if (previsao < DataAbertura)
                throw new RegraNegocioException("Data prevista não pode ser anterior à data de abertura.");

            DataPrevisaoConclusao = previsao;
        }

        public void VincularSessaoDiagnostico(Guid sessaoId)
        {
            if (sessaoId == Guid.Empty)
                throw new RegraNegocioException("SessaoDiagnosticoId não pode ser vazio.");

            SessaoDiagnosticoId = sessaoId;
        }

        public void AlterarStatus(
            StatusOrdemServico novoStatus,
            string motivo,
            string responsavel,
            ITimeProvider timeProvider)
        {
            if (timeProvider == null) throw new ArgumentNullException(nameof(timeProvider));

            if (Status == novoStatus)
                return; // Sem alteração

            ValidarTransicaoStatus(Status, novoStatus);

            var statusAnterior = Status;
            Status = novoStatus;

            var agora = timeProvider.GetUtcNow();

            if (novoStatus == StatusOrdemServico.Finalizada)
            {
                DataConclusao = agora;
            }

            _historico.Add(new HistoricoStatusOS(
                Guid.NewGuid(),
                Id,
                statusAnterior,
                novoStatus,
                motivo,
                responsavel,
                agora));
        }

        private void ValidarTransicaoStatus(StatusOrdemServico atual, StatusOrdemServico destino)
        {
            if (atual == StatusOrdemServico.Cancelada)
                throw new TransicaoStatusInvalidaException(atual.ToString(), destino.ToString(), "A Ordem de Serviço já está cancelada (estado terminal).");

            if (atual == StatusOrdemServico.Rejeitada)
                throw new TransicaoStatusInvalidaException(atual.ToString(), destino.ToString(), "A Ordem de Serviço foi rejeitada pelo cliente (estado terminal).");

            // Cancelamento é permitido de quase qualquer estado intermediário antes de finalizada
            if (destino == StatusOrdemServico.Cancelada)
            {
                if (atual == StatusOrdemServico.Finalizada)
                    throw new TransicaoStatusInvalidaException(atual.ToString(), destino.ToString(), "Não é permitido cancelar uma OS já finalizada e entregue.");
                return;
            }

            var transicaoValida = (atual, destino) switch
            {
                (StatusOrdemServico.Aberta, StatusOrdemServico.EmTriagemDvi) => true,
                (StatusOrdemServico.Aberta, StatusOrdemServico.EmDiagnostico) => true,
                (StatusOrdemServico.Aberta, StatusOrdemServico.OrcamentoGerado) => true,

                (StatusOrdemServico.EmTriagemDvi, StatusOrdemServico.EmDiagnostico) => true,
                (StatusOrdemServico.EmTriagemDvi, StatusOrdemServico.OrcamentoGerado) => true,

                (StatusOrdemServico.EmDiagnostico, StatusOrdemServico.OrcamentoGerado) => true,
                (StatusOrdemServico.EmDiagnostico, StatusOrdemServico.EmTriagemDvi) => true,

                (StatusOrdemServico.OrcamentoGerado, StatusOrdemServico.Aprovada) => true,
                (StatusOrdemServico.OrcamentoGerado, StatusOrdemServico.Rejeitada) => true,
                (StatusOrdemServico.OrcamentoGerado, StatusOrdemServico.EmDiagnostico) => true,

                (StatusOrdemServico.Aprovada, StatusOrdemServico.AguardandoPeca) => true,
                (StatusOrdemServico.Aprovada, StatusOrdemServico.EmExecucao) => true,

                (StatusOrdemServico.AguardandoPeca, StatusOrdemServico.EmExecucao) => true,

                (StatusOrdemServico.EmExecucao, StatusOrdemServico.ControleQualidade) => true,
                (StatusOrdemServico.EmExecucao, StatusOrdemServico.AguardandoPeca) => true,

                (StatusOrdemServico.ControleQualidade, StatusOrdemServico.ProntaParaEntrega) => true,
                (StatusOrdemServico.ControleQualidade, StatusOrdemServico.EmExecucao) => true, // Reprovação interna / reteste

                (StatusOrdemServico.ProntaParaEntrega, StatusOrdemServico.Finalizada) => true,
                (StatusOrdemServico.ProntaParaEntrega, StatusOrdemServico.ControleQualidade) => true,

                (StatusOrdemServico.Finalizada, StatusOrdemServico.RetornoGarantia) => true,

                (StatusOrdemServico.RetornoGarantia, StatusOrdemServico.EmDiagnostico) => true,
                (StatusOrdemServico.RetornoGarantia, StatusOrdemServico.EmExecucao) => true,

                _ => false
            };

            if (!transicaoValida)
            {
                throw new TransicaoStatusInvalidaException(
                    atual.ToString(),
                    destino.ToString(),
                    $"Não é permitida a transição direta de '{atual}' para '{destino}'.");
            }
        }

        private void GarantirModificacaoPermitida()
        {
            if (Status == StatusOrdemServico.Finalizada)
                throw new RegraNegocioException("Não é permitido modificar itens de uma Ordem de Serviço já finalizada.");

            if (Status == StatusOrdemServico.Cancelada)
                throw new RegraNegocioException("Não é permitido modificar itens de uma Ordem de Serviço cancelada.");

            if (Status == StatusOrdemServico.Rejeitada)
                throw new RegraNegocioException("Não é permitido modificar itens de uma Ordem de Serviço rejeitada.");
        }
    }
}
