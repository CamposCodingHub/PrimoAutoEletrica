using System;
using System.Linq;
using PRIMOX.Domain.Entities;
using PRIMOX.Domain.Enums;
using PRIMOX.Domain.Exceptions;
using PRIMOX.Domain.Tests.Fakes;
using PRIMOX.Domain.ValueObjects;
using Xunit;

namespace PRIMOX.Domain.Tests
{
    public class OrdemServicoTests
    {
        private readonly FakeTimeProvider _timeProvider;
        private readonly Guid _clienteId = Guid.NewGuid();
        private readonly Guid _veiculoId = Guid.NewGuid();

        public OrdemServicoTests()
        {
            _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 10, 4, 10, 0, 0, TimeSpan.Zero));
        }

        private OrdemServico CriarOSValida()
        {
            return new OrdemServico(
                Guid.NewGuid(),
                "OS-2026-0001",
                _clienteId,
                _veiculoId,
                "Veículo não dá partida e luz da bateria acesa no painel",
                _timeProvider,
                PrioridadeOS.Alta,
                clienteNomeSnapshot: "Oficina Piloto",
                veiculoPlacaSnapshot: "BRA2E19",
                veiculoModeloSnapshot: "HB20 1.6 2014");
        }

        [Fact]
        public void Abertura_De_OS_Deve_Inicializar_Com_Status_Aberta_E_Historico()
        {
            var os = CriarOSValida();

            Assert.Equal(StatusOrdemServico.Aberta, os.Status);
            Assert.Equal(PrioridadeOS.Alta, os.Prioridade);
            Assert.Equal("OS-2026-0001", os.Numero);
            Assert.Equal(_timeProvider.GetUtcNow(), os.DataAbertura);
            Assert.Single(os.Historico);
            Assert.Equal(StatusOrdemServico.Aberta, os.Historico.First().NovoStatus);
        }

        [Fact]
        public void Abertura_Sem_Cliente_Deve_Lancar_RegraNegocioException()
        {
            Assert.Throws<RegraNegocioException>(() =>
                new OrdemServico(
                    Guid.NewGuid(),
                    "OS-01",
                    Guid.Empty,
                    _veiculoId,
                    "Queixa teste",
                    _timeProvider));
        }

        [Fact]
        public void Abertura_Sem_Veiculo_Deve_Lancar_RegraNegocioException()
        {
            Assert.Throws<RegraNegocioException>(() =>
                new OrdemServico(
                    Guid.NewGuid(),
                    "OS-01",
                    _clienteId,
                    Guid.Empty,
                    "Queixa teste",
                    _timeProvider));
        }

        [Fact]
        public void Abertura_Sem_Queixa_Deve_Lancar_RegraNegocioException()
        {
            Assert.Throws<RegraNegocioException>(() =>
                new OrdemServico(
                    Guid.NewGuid(),
                    "OS-01",
                    _clienteId,
                    _veiculoId,
                    "   ",
                    _timeProvider));
        }

        [Fact]
        public void Adicionar_Peca_E_Servico_Deve_Calcular_Totais_Corretamente()
        {
            var os = CriarOSValida();

            os.AdicionarPeca(
                descricao: "Alternador Remanufaturado Valeo 90A",
                quantidade: 1,
                valorUnitario: Money.FromBRL(650.00m),
                codigo: "ALT-VAL-90A");

            os.AdicionarServico(
                descricao: "Substituição de Alternador e Teste de Carga",
                quantidadeHoras: 2.5m,
                valorHora: Money.FromBRL(120.00m),
                tecnicoResponsavel: "Marcos Eletricista");

            Assert.Equal(650.00m, os.TotalPecas.Amount);
            Assert.Equal(300.00m, os.TotalServicos.Amount); // 2.5 * 120 = 300
            Assert.Equal(950.00m, os.TotalBruto.Amount);
            Assert.Equal(950.00m, os.TotalLiquido.Amount);
        }

        [Fact]
        public void Aplicar_Desconto_Deve_Deduzir_Do_TotalLiquido()
        {
            var os = CriarOSValida();
            os.AdicionarPeca("Correia Poli-V", 1, Money.FromBRL(100.00m));

            os.AplicarDesconto(Money.FromBRL(15.00m));

            Assert.Equal(100.00m, os.TotalBruto.Amount);
            Assert.Equal(85.00m, os.TotalLiquido.Amount);
            Assert.Equal(15.00m, os.Desconto.Amount);
        }

        [Fact]
        public void Desconto_Maior_Que_TotalBruto_Deve_Lancar_RegraNegocioException()
        {
            var os = CriarOSValida();
            os.AdicionarPeca("Fusível 15A", 1, Money.FromBRL(5.00m));

            Assert.Throws<RegraNegocioException>(() =>
                os.AplicarDesconto(Money.FromBRL(50.00m)));
        }

        [Fact]
        public void Adicionar_Item_Com_Quantidade_Zero_Ou_Negativa_Deve_Lancar_RegraNegocioException()
        {
            var os = CriarOSValida();

            Assert.Throws<RegraNegocioException>(() =>
                os.AdicionarPeca("Lâmpada H7", 0, Money.FromBRL(35.00m)));

            Assert.Throws<RegraNegocioException>(() =>
                os.AdicionarServico("Troca de Lâmpada", -1.0m, Money.FromBRL(50.00m)));
        }

        [Fact]
        public void Maquina_De_Estados_Transicoes_Validas_Devem_Funcionar()
        {
            var os = CriarOSValida();

            // Aberta -> EmTriagemDvi
            os.AlterarStatus(StatusOrdemServico.EmTriagemDvi, "Iniciando checklist fotográfico de entrada", "Técnico DVI", _timeProvider);
            Assert.Equal(StatusOrdemServico.EmTriagemDvi, os.Status);

            // EmTriagemDvi -> EmDiagnostico
            os.AlterarStatus(StatusOrdemServico.EmDiagnostico, "Conferência elétrica com multímetro", "Eletricista", _timeProvider);
            Assert.Equal(StatusOrdemServico.EmDiagnostico, os.Status);

            // EmDiagnostico -> OrcamentoGerado
            os.AlterarStatus(StatusOrdemServico.OrcamentoGerado, "Diagnóstico concluído: diodo queimado", "Orçamentista", _timeProvider);
            Assert.Equal(StatusOrdemServico.OrcamentoGerado, os.Status);

            // OrcamentoGerado -> Aprovada
            os.AlterarStatus(StatusOrdemServico.Aprovada, "Cliente aprovou orçamento via WhatsApp", "Recepção", _timeProvider);
            Assert.Equal(StatusOrdemServico.Aprovada, os.Status);

            // Aprovada -> EmExecucao
            os.AlterarStatus(StatusOrdemServico.EmExecucao, "Veículo alocado no Box 02", "Chefe de Oficina", _timeProvider);
            Assert.Equal(StatusOrdemServico.EmExecucao, os.Status);

            // EmExecucao -> ControleQualidade
            os.AlterarStatus(StatusOrdemServico.ControleQualidade, "Alternador montado, testando carga sob pico de consumo", "Inspetor QA", _timeProvider);
            Assert.Equal(StatusOrdemServico.ControleQualidade, os.Status);

            // ControleQualidade -> ProntaParaEntrega
            os.AlterarStatus(StatusOrdemServico.ProntaParaEntrega, "14.2V estável com ar condicionado e faróis ligados. Aprovado.", "Inspetor QA", _timeProvider);
            Assert.Equal(StatusOrdemServico.ProntaParaEntrega, os.Status);

            // ProntaParaEntrega -> Finalizada
            os.AlterarStatus(StatusOrdemServico.Finalizada, "Faturamento concluído e veículo retirado pelo cliente", "Financeiro", _timeProvider);
            Assert.Equal(StatusOrdemServico.Finalizada, os.Status);
            Assert.NotNull(os.DataConclusao);

            // Histórico deve conter 9 eventos (1 de abertura + 8 transições)
            Assert.Equal(9, os.Historico.Count);
        }

        [Fact]
        public void Transicao_Invalida_Direta_Deve_Lancar_TransicaoStatusInvalidaException()
        {
            var os = CriarOSValida();

            // Pular direto de Aberta para Finalizada é estritamente proibido
            var ex = Assert.Throws<TransicaoStatusInvalidaException>(() =>
                os.AlterarStatus(StatusOrdemServico.Finalizada, "Tentativa de pulo", "Admin", _timeProvider));

            Assert.Equal("Aberta", ex.StatusOrigem);
            Assert.Equal("Finalizada", ex.StatusDestino);
        }

        [Fact]
        public void Nao_Permitir_Alterar_Status_De_OS_Cancelada()
        {
            var os = CriarOSValida();
            os.AlterarStatus(StatusOrdemServico.Cancelada, "Cliente desistiu antes da triagem", "Recepção", _timeProvider);

            Assert.Throws<TransicaoStatusInvalidaException>(() =>
                os.AlterarStatus(StatusOrdemServico.EmExecucao, "Tentando reabrir sem autorização", "Técnico", _timeProvider));
        }

        [Fact]
        public void Nao_Permitir_Adicionar_Itens_Em_OS_Finalizada()
        {
            var os = CriarOSValida();
            os.AlterarStatus(StatusOrdemServico.EmTriagemDvi, "Triagem", "Técnico", _timeProvider);
            os.AlterarStatus(StatusOrdemServico.OrcamentoGerado, "Orcamento", "Técnico", _timeProvider);
            os.AlterarStatus(StatusOrdemServico.Aprovada, "Aprovada", "Recepção", _timeProvider);
            os.AlterarStatus(StatusOrdemServico.EmExecucao, "Executando", "Técnico", _timeProvider);
            os.AlterarStatus(StatusOrdemServico.ControleQualidade, "QA", "Inspetor", _timeProvider);
            os.AlterarStatus(StatusOrdemServico.ProntaParaEntrega, "Pronta", "Inspetor", _timeProvider);
            os.AlterarStatus(StatusOrdemServico.Finalizada, "Finalizada", "Financeiro", _timeProvider);

            Assert.Throws<RegraNegocioException>(() =>
                os.AdicionarPeca("Item Extra Não Faturado", 1, Money.FromBRL(10m)));
        }

        [Fact]
        public void Retorno_De_Garantia_A_Partir_De_OS_Finalizada()
        {
            var os = CriarOSValida();
            os.AlterarStatus(StatusOrdemServico.EmTriagemDvi, "Triagem", "Técnico", _timeProvider);
            os.AlterarStatus(StatusOrdemServico.OrcamentoGerado, "Orcamento", "Técnico", _timeProvider);
            os.AlterarStatus(StatusOrdemServico.Aprovada, "Aprovada", "Recepção", _timeProvider);
            os.AlterarStatus(StatusOrdemServico.EmExecucao, "Execução", "Técnico", _timeProvider);
            os.AlterarStatus(StatusOrdemServico.ControleQualidade, "QA", "Inspetor", _timeProvider);
            os.AlterarStatus(StatusOrdemServico.ProntaParaEntrega, "Pronta", "Inspetor", _timeProvider);
            os.AlterarStatus(StatusOrdemServico.Finalizada, "Finalizada", "Financeiro", _timeProvider);

            os.AlterarStatus(StatusOrdemServico.RetornoGarantia, "Luz de bateria voltou a piscar em alta rotação", "Recepção", _timeProvider);
            Assert.Equal(StatusOrdemServico.RetornoGarantia, os.Status);
        }
    }
}
