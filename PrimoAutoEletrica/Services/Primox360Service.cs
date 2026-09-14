using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.ViewModels;

namespace PrimoAutoEletrica.Services
{
    public interface IPrimox360Service
    {
        Cliente360Snapshot ObterCliente360(Guid clienteId);
        Veiculo360Snapshot ObterVeiculo360(Guid veiculoId);
        OrdemServico360Snapshot ObterOrdemServico360(Guid ordemServicoId);
        IReadOnlyList<ContaReceberVinculo> ObterContasReceberVinculadasAoCliente(Guid clienteId);
    }

    /// <summary>
    /// Agrega Cliente/Veículo/OS 360 apenas com relacionamentos por ID (ou Origem+ReferenciaExterna).
    /// Não usa TEXT_MATCH por nome para KPIs financeiros.
    /// </summary>
    public sealed class Primox360Service : IPrimox360Service
    {
        public const string OrigemOsContaReceber = "OrdemServicoContaReceber";
        public const string OrigemOrcamentoContaReceber = "OrcamentoContaReceber";

        private readonly IClienteRepository _clientes;
        private readonly IOrdemServicoRepository _ordens;
        private readonly Func<OrcamentoDatabaseService> _orcamentosFactory;
        private readonly Func<FinanceiroDatabaseService> _financeiroFactory;
        private readonly Func<VendaRepository> _vendasFactory;

        public Primox360Service(
            IClienteRepository clientes,
            IOrdemServicoRepository ordens,
            Func<OrcamentoDatabaseService>? orcamentosFactory = null,
            Func<FinanceiroDatabaseService>? financeiroFactory = null,
            Func<VendaRepository>? vendasFactory = null)
        {
            _clientes = clientes ?? throw new ArgumentNullException(nameof(clientes));
            _ordens = ordens ?? throw new ArgumentNullException(nameof(ordens));
            _orcamentosFactory = orcamentosFactory ?? (() => new OrcamentoDatabaseService());
            _financeiroFactory = financeiroFactory ?? (() => new FinanceiroDatabaseService());
            _vendasFactory = vendasFactory ?? (() => new VendaRepository());
        }

        public Cliente360Snapshot ObterCliente360(Guid clienteId)
        {
            if (clienteId == Guid.Empty)
            {
                throw new ArgumentException("ClienteId inválido.", nameof(clienteId));
            }

            var cliente = _clientes.ObterPorId(clienteId)
                ?? throw new InvalidOperationException($"Cliente {clienteId} não encontrado.");

            var veiculos = _clientes.ObterVeiculosPorClienteId(clienteId);
            var ordens = _ordens.ObterPorClienteId(clienteId, incluirInativas: true)
                .OrderByDescending(o => o.DataAbertura)
                .ToList();

            var orcamentos = _orcamentosFactory()
                .ObterTodosOrcamentos()
                .Where(o => o.ClienteId == clienteId)
                .ToList();

            var vendas = _vendasFactory()
                .ObterVendas()
                .Where(v => v.Cliente?.Id == clienteId)
                .ToList();

            var limite12m = DateTime.Today.AddMonths(-12);
            decimal TotalOs(OrdemServico os) => Math.Max(0m, os.Itens.Sum(i => i.Total) - os.Desconto);

            var receitaOs = ordens.Sum(TotalOs);
            var receitaOs12 = ordens.Where(o => o.DataAbertura >= limite12m).Sum(TotalOs);
            var receitaVendas = vendas.Sum(v => v.Total);
            var receitaVendas12 = vendas.Where(v => v.Data >= limite12m).Sum(v => v.Total);

            var aprovados = orcamentos.Count(o => EhOrcamentoAprovado(o.Status));
            var recusados = orcamentos.Where(o => EhOrcamentoRecusado(o.Status)).ToList();

            DateTime? ultimaVisita = ordens.Count > 0
                ? ordens.Max(o => o.DataAbertura)
                : cliente.UltimaVisita;

            int? diasSemVisita = ultimaVisita.HasValue
                ? (int)(DateTime.Today - ultimaVisita.Value.Date).TotalDays
                : null;

            var contasVinculadas = ObterContasReceberVinculadasAoCliente(clienteId);
            var pendentes = contasVinculadas.Where(c => !c.Pago).ToList();

            var timeline = new List<Cliente360TimelineItem>
            {
                new()
                {
                    Data = cliente.DataCadastro,
                    Tipo = "Cadastro",
                    Titulo = "Cliente cadastrado",
                    Descricao = cliente.Nome,
                    ReferenciaId = cliente.Id
                }
            };

            timeline.AddRange(ordens.Take(20).Select(o => new Cliente360TimelineItem
            {
                Data = o.DataAbertura,
                Tipo = "OrdemServico",
                Titulo = $"OS {o.Numero}",
                Descricao = $"{o.Status} · {TotalOs(o):C}",
                ReferenciaId = o.Id
            }));

            timeline.AddRange(orcamentos.Take(20).Select(o => new Cliente360TimelineItem
            {
                Data = o.DataCriacao,
                Tipo = "Orcamento",
                Titulo = $"Orçamento {o.Numero}",
                Descricao = $"{o.Status} · {o.Total:C}",
                ReferenciaId = o.Id
            }));

            timeline.AddRange(vendas.Take(20).Select(v => new Cliente360TimelineItem
            {
                Data = v.Data,
                Tipo = "Venda",
                Titulo = "Venda",
                Descricao = $"{v.FormaPagamento} · {v.Total:C} · {v.Status}",
                ReferenciaId = v.Id
            }));

            timeline.AddRange(contasVinculadas.Where(c => c.DataPagamento.HasValue).Take(20).Select(c => new Cliente360TimelineItem
            {
                Data = c.DataPagamento!.Value,
                Tipo = "Pagamento",
                Titulo = "Pagamento (vínculo ID)",
                Descricao = $"{c.FormaPagamento} · {c.Valor:C}",
                ReferenciaId = null
            }));

            return new Cliente360Snapshot
            {
                ClienteId = cliente.Id,
                Nome = cliente.Nome,
                VeiculosCount = veiculos.Count,
                OsCount = ordens.Count,
                VisitasCount = ordens.Count,
                ReceitaOsTotal = receitaOs,
                ReceitaVendasTotal = receitaVendas,
                ReceitaOs12Meses = receitaOs12,
                ReceitaVendas12Meses = receitaVendas12,
                TicketMedioOs = ordens.Count > 0 ? receitaOs / ordens.Count : null,
                OrcamentosCount = orcamentos.Count,
                OrcamentosAprovados = aprovados,
                OrcamentosRecusados = recusados.Count,
                ValorPerdidoOrcamentos = recusados.Sum(o => o.Total),
                UltimaVisita = ultimaVisita,
                DiasDesdeUltimaVisita = diasSemVisita,
                TotalGastoCadastro = cliente.TotalGasto,
                DividaVinculadaPorId = pendentes.Sum(c => c.Valor),
                ContasReceberVinculadasPendentes = pendentes.Count,
                DividaTotalDisplay = "N/A / NÃO DISPONÍVEL (sem ClienteId em ContasReceber)",
                Timeline = timeline.OrderByDescending(t => t.Data).Take(40).ToList(),
                VeiculoIds = veiculos.Select(v => v.Id).ToList(),
                OrdemServicoIds = ordens.Select(o => o.Id).ToList()
            };
        }

        public Veiculo360Snapshot ObterVeiculo360(Guid veiculoId)
        {
            if (veiculoId == Guid.Empty)
            {
                throw new ArgumentException("VeiculoId inválido.", nameof(veiculoId));
            }

            var veiculo = _clientes.ObterTodosVeiculos().FirstOrDefault(v => v.Id == veiculoId)
                ?? throw new InvalidOperationException($"Veículo {veiculoId} não encontrado.");

            var todas = _ordens.ObterTodos(incluirInativas: true);
            var porId = todas.Where(os => os.VeiculoId == veiculoId).ToList();

            var placaNorm = CadastroValidationHelper.NormalizarPlaca(veiculo.Placa);
            var porPlacaExtra = todas
                .Where(os => os.VeiculoId != veiculoId &&
                             !string.IsNullOrWhiteSpace(os.PlacaSnapshot) &&
                             string.Equals(
                                 CadastroValidationHelper.NormalizarPlaca(os.PlacaSnapshot),
                                 placaNorm,
                                 StringComparison.Ordinal))
                .ToList();

            decimal TotalOs(OrdemServico os) => Math.Max(0m, os.Itens.Sum(i => i.Total) - os.Desconto);
            var receita = porId.Sum(TotalOs);
            var ultima = porId.OrderByDescending(o => o.DataAbertura).FirstOrDefault();

            var orcamentos = _orcamentosFactory()
                .ObterTodosOrcamentos()
                .Where(o => o.VeiculoId == veiculoId)
                .ToList();

            return new Veiculo360Snapshot
            {
                VeiculoId = veiculo.Id,
                ClienteId = veiculo.ClienteId,
                Placa = veiculo.Placa ?? string.Empty,
                Quilometragem = veiculo.Quilometragem,
                OsCountPorVeiculoId = porId.Count,
                OsCountIncluindoPlacaFraca = porId.Count + porPlacaExtra.Count,
                ReceitaAcumuladaPorVeiculoId = receita,
                UltimoServico = ultima?.DataAbertura,
                DiasDesdeUltimoServico = ultima == null
                    ? null
                    : (int)(DateTime.Today - ultima.DataAbertura.Date).TotalDays,
                OrcamentosPorVeiculoId = orcamentos.Count,
                ProblemaRecorrenteCadastro = veiculo.ProblemaRecorrente ?? string.Empty,
                UsaFallbackPlaca = porPlacaExtra.Count > 0,
                NotaIntegridade = porPlacaExtra.Count > 0
                    ? $"Há {porPlacaExtra.Count} OS com mesma placa sem VeiculoId — não entram na receita por ID."
                    : "OS e orçamentos filtrados por VeiculoId.",
                OrdemServicoIds = porId.Select(o => o.Id).ToList()
            };
        }

        public OrdemServico360Snapshot ObterOrdemServico360(Guid ordemServicoId)
        {
            var ordem = _ordens.ObterPorId(ordemServicoId)
                ?? throw new InvalidOperationException($"OS {ordemServicoId} não encontrada.");

            var clienteLink = ordem.ClienteId != Guid.Empty ? "CONNECTED" : "MISSING";
            var veiculoLink = ordem.VeiculoId.HasValue && ordem.VeiculoId.Value != Guid.Empty
                ? "CONNECTED"
                : "PARTIAL";
            var orcamentoLink = ordem.OrcamentoId.HasValue ? "CONNECTED" : "MISSING";

            var financeiro = _financeiroFactory().ObterContasReceber();
            var refId = ordem.Id.ToString();
            var temFinanceiro = financeiro.Any(c =>
            {
                var origem = Convert.ToString(GetDyn(c, "Origem")) ?? string.Empty;
                var referencia = Convert.ToString(GetDyn(c, "ReferenciaExterna")) ?? string.Empty;
                return string.Equals(origem, OrigemOsContaReceber, StringComparison.OrdinalIgnoreCase)
                       && string.Equals(referencia, refId, StringComparison.OrdinalIgnoreCase);
            });

            var itensServico = ordem.Itens.Count(i => string.Equals(i.Tipo, "Servico", StringComparison.OrdinalIgnoreCase));
            var itensPeca = ordem.Itens.Count(i => !string.Equals(i.Tipo, "Servico", StringComparison.OrdinalIgnoreCase));
            var total = Math.Max(0m, ordem.Itens.Sum(i => i.Total) - ordem.Desconto);

            var hub =
                $"Cliente:{clienteLink} · Veículo:{veiculoLink} · Orçamento:{orcamentoLink} · " +
                $"Financeiro:{(temFinanceiro ? "CONNECTED" : "MISSING")} · Fiscal:MISSING · Pós-venda:MISSING";

            return new OrdemServico360Snapshot
            {
                OrdemServicoId = ordem.Id,
                Numero = ordem.Numero,
                Status = ordem.Status,
                ClienteId = ordem.ClienteId,
                VeiculoId = ordem.VeiculoId,
                OrcamentoId = ordem.OrcamentoId,
                ClienteLink = clienteLink,
                VeiculoLink = veiculoLink,
                OrcamentoLink = orcamentoLink,
                FinanceiroLink = temFinanceiro ? "CONNECTED" : "MISSING",
                FiscalLink = "MISSING",
                PosVendaLink = "MISSING",
                TotalItens = total,
                ItensServico = itensServico,
                ItensPeca = itensPeca,
                TemFotos = !string.IsNullOrWhiteSpace(ordem.FotosAntes) || !string.IsNullOrWhiteSpace(ordem.FotosDepois),
                TemAssinatura = !string.IsNullOrWhiteSpace(ordem.AssinaturaClienteUrl),
                HubResumo = hub
            };
        }

        public IReadOnlyList<ContaReceberVinculo> ObterContasReceberVinculadasAoCliente(Guid clienteId)
        {
            var osIds = _ordens.ObterPorClienteId(clienteId, incluirInativas: true)
                .Select(o => o.Id.ToString())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var orcIds = _orcamentosFactory()
                .ObterTodosOrcamentos()
                .Where(o => o.ClienteId == clienteId)
                .Select(o => o.Id.ToString())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var resultado = new List<ContaReceberVinculo>();
            foreach (var conta in _financeiroFactory().ObterContasReceber())
            {
                var origem = Convert.ToString(GetDyn(conta, "Origem")) ?? string.Empty;
                var referencia = Convert.ToString(GetDyn(conta, "ReferenciaExterna")) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(origem) || string.IsNullOrWhiteSpace(referencia))
                {
                    continue;
                }

                var matchOs = string.Equals(origem, OrigemOsContaReceber, StringComparison.OrdinalIgnoreCase)
                              && osIds.Contains(referencia);
                var matchOrc = string.Equals(origem, OrigemOrcamentoContaReceber, StringComparison.OrdinalIgnoreCase)
                               && orcIds.Contains(referencia);
                if (!matchOs && !matchOrc)
                {
                    continue;
                }

                var status = Convert.ToString(GetDyn(conta, "Status")) ?? "Pendente";
                var dataPagamentoRaw = Convert.ToString(GetDyn(conta, "DataPagamento"));
                DateTime? dataPagamento = null;
                if (!string.IsNullOrWhiteSpace(dataPagamentoRaw))
                {
                    if (DateTime.TryParse(dataPagamentoRaw, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedPagamento))
                    {
                        dataPagamento = parsedPagamento;
                    }
                }

                var vencRaw = Convert.ToString(GetDyn(conta, "DataVencimento")) ?? string.Empty;
                var vencimento = DateTime.Today;
                if (DateTime.TryParse(vencRaw, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedVenc))
                {
                    vencimento = parsedVenc;
                }

                var valorObj = GetDyn(conta, "Valor");
                var valor = valorObj is decimal d ? d : Convert.ToDecimal(valorObj, CultureInfo.InvariantCulture);

                resultado.Add(new ContaReceberVinculo
                {
                    Id = Convert.ToInt32(GetDyn(conta, "Id"), CultureInfo.InvariantCulture),
                    ClienteNomeSnapshot = Convert.ToString(GetDyn(conta, "Cliente")) ?? string.Empty,
                    Descricao = Convert.ToString(GetDyn(conta, "Descricao")) ?? string.Empty,
                    Valor = valor,
                    DataVencimento = vencimento == default ? DateTime.Today : vencimento,
                    DataPagamento = dataPagamento,
                    Status = status,
                    FormaPagamento = Convert.ToString(GetDyn(conta, "FormaPagamento")) ?? string.Empty,
                    Origem = origem,
                    ReferenciaExterna = referencia,
                    Pago = dataPagamento.HasValue ||
                           status.Equals("Pago", StringComparison.OrdinalIgnoreCase)
                });
            }

            return resultado;
        }

        public static bool EhOrcamentoAprovado(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return false;
            }

            return status.Equals("Aprovado", StringComparison.OrdinalIgnoreCase)
                   || status.StartsWith("Convertido", StringComparison.OrdinalIgnoreCase);
        }

        public static bool EhOrcamentoRecusado(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return false;
            }

            var n = Helpers.OrcamentoStatusNormalizer.Normalizar(status);
            return n.Equals("Recusado", StringComparison.OrdinalIgnoreCase)
                   || n.Equals("Cancelado", StringComparison.OrdinalIgnoreCase);
        }

        private static object? GetDyn(object conta, string property)
        {
            var prop = conta.GetType().GetProperty(property);
            return prop?.GetValue(conta);
        }
    }
}
