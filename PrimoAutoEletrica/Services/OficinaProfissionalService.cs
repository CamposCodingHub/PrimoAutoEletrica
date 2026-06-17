using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PrimoAutoEletrica.Services
{
    public sealed class OficinaProfissionalService
    {
        private static readonly string[] StatusKanban =
        {
            "Agendado",
            "Recebido",
            "Em diagnostico",
            "Aguardando aprovacao",
            "Aguardando peca",
            "Em execucao",
            "Finalizado",
            "Aguardando pagamento",
            "Entregue",
            "Cancelado"
        };

        private readonly IOrdemServicoRepository _ordemServicoRepository;
        private readonly OrcamentoDatabaseService _orcamentoService;
        private readonly VendaService _vendaService;

        public OficinaProfissionalService(
            IOrdemServicoRepository? ordemServicoRepository = null,
            OrcamentoDatabaseService? orcamentoService = null,
            VendaService? vendaService = null)
        {
            _ordemServicoRepository = ordemServicoRepository ?? App.Repositories.OrdensServico;
            _orcamentoService = orcamentoService ?? new OrcamentoDatabaseService();
            _vendaService = vendaService ?? new VendaService(App.Database);
        }

        public OficinaProfissionalSnapshot CriarSnapshot(Guid? clienteId = null, Guid? veiculoId = null)
        {
            var kanban = ObterKanban();
            var primeiroCard = kanban.SelectMany(coluna => coluna.Cards).FirstOrDefault();

            return new OficinaProfissionalSnapshot
            {
                ChecklistPadrao = CriarChecklistPadrao(),
                Kanban = kanban,
                Timeline = ObterTimeline(clienteId ?? primeiroCard?.ClienteId, veiculoId ?? primeiroCard?.VeiculoId),
                Mensagens = CriarModelosMensagem(primeiroCard),
                Resumo = $"Kanban={kanban.Sum(c => c.Quantidade)} OS; Checklist={CriarChecklistPadrao().Count} itens; Mensagens={CriarModelosMensagem(primeiroCard).Count} modelos."
            };
        }

        public IReadOnlyList<ChecklistVisualItem> CriarChecklistPadrao()
        {
            return new[]
            {
                Item("Carroceria", "Estado geral do veiculo"),
                Item("Interior", "Painel"),
                Item("Eletrica externa", "Luzes"),
                Item("Eletrica externa", "Farois"),
                Item("Eletrica externa", "Lanternas"),
                Item("Eletrica externa", "Setas"),
                Item("Eletrica externa", "Luz de freio"),
                Item("Eletrica externa", "Luz de re"),
                Item("Conforto", "Limpador"),
                Item("Conforto", "Buzina"),
                Item("Sistema eletrico", "Bateria"),
                Item("Sistema eletrico", "Alternador"),
                Item("Sistema eletrico", "Motor de partida"),
                Item("Sistema eletrico", "Fusiveis"),
                Item("Sistema eletrico", "Reles"),
                Item("Sistema eletrico", "Chicotes aparentes"),
                Item("Sistema eletrico", "Aterramentos visiveis"),
                Item("Acessorios", "Acessorios instalados"),
                Item("Carroceria", "Avarias"),
                Item("Pertences", "Objetos deixados no veiculo")
            };

            static ChecklistVisualItem Item(string categoria, string nome) => new()
            {
                Categoria = categoria,
                Nome = nome,
                Observacoes = "Registrar foto antes/depois quando houver divergencia."
            };
        }

        public IReadOnlyList<OficinaKanbanColumn> ObterKanban()
        {
            var funcionarios = App.Repositories.Funcionarios.ObterTodos(false)
                .ToDictionary(funcionario => funcionario.Id, funcionario => funcionario);

            var ordens = _ordemServicoRepository.ObterTodos()
                .OrderByDescending(o => PesoPrioridade(o.Prioridade))
                .ThenBy(o => o.DataPrevisao ?? DateTime.MaxValue)
                .ThenByDescending(o => o.DataAbertura)
                .ToList();

            return StatusKanban
                .Select(status => new OficinaKanbanColumn
                {
                    Titulo = status,
                    Descricao = CriarDescricaoColuna(status),
                    Cards = new ObservableCollection<OficinaKanbanCard>(
                        ordens
                            .Where(ordem => PertenceAColuna(ordem, status))
                            .Select(ordem => CriarCard(ordem, funcionarios)))
                })
                .ToList();
        }

        public IReadOnlyList<OficinaTimelineItem> ObterTimeline(Guid? clienteId, Guid? veiculoId)
        {
            var timeline = new List<OficinaTimelineItem>();

            if (clienteId.HasValue)
            {
                var cliente = App.Repositories.Clientes.ObterPorId(clienteId.Value);
                if (cliente != null)
                {
                    timeline.Add(new OficinaTimelineItem
                    {
                        Data = cliente.DataCadastro,
                        Tipo = "Cliente",
                        Titulo = "Cadastro do cliente",
                        Descricao = cliente.Nome,
                        Referencia = cliente.Documento
                    });
                }
            }

            foreach (var ordem in _ordemServicoRepository.ObterTodos())
            {
                if (!Relaciona(ordem.ClienteId, ordem.VeiculoId, clienteId, veiculoId))
                {
                    continue;
                }

                var valor = CalcularTotalOrdem(ordem);
                timeline.Add(new OficinaTimelineItem
                {
                    Data = ordem.DataAbertura,
                    Tipo = "OS",
                    Titulo = $"OS {ordem.Numero}",
                    Descricao = $"{Texto(ordem.Status, "Rascunho")} | {Texto(ordem.ProblemaRelatado, ordem.VeiculoDescricaoSnapshot)}",
                    Referencia = ordem.PlacaSnapshot,
                    Valor = valor
                });

                foreach (var evento in ordem.Eventos)
                {
                    timeline.Add(new OficinaTimelineItem
                    {
                        Data = evento.DataEvento,
                        Tipo = "Evento OS",
                        Titulo = evento.Titulo,
                        Descricao = evento.Descricao,
                        Referencia = ordem.Numero
                    });
                }
            }

            foreach (var orcamento in _orcamentoService.ObterTodosOrcamentos())
            {
                if (!Relaciona(orcamento.ClienteId, orcamento.VeiculoId, clienteId, veiculoId))
                {
                    continue;
                }

                timeline.Add(new OficinaTimelineItem
                {
                    Data = orcamento.DataCriacao,
                    Tipo = "Orcamento",
                    Titulo = $"Orcamento {orcamento.Numero}",
                    Descricao = $"{Texto(orcamento.Status, "Rascunho")} | Validade {orcamento.DataValidade?.ToString("dd/MM/yyyy") ?? "nao definida"}",
                    Referencia = orcamento.Veiculo?.Placa ?? string.Empty,
                    Valor = orcamento.Total
                });

                if (orcamento.DataAprovacao.HasValue)
                {
                    timeline.Add(new OficinaTimelineItem
                    {
                        Data = orcamento.DataAprovacao.Value,
                        Tipo = "Aprovacao",
                        Titulo = $"Orcamento {orcamento.Numero} aprovado",
                        Descricao = "Aprovacao registrada com data/hora.",
                        Referencia = orcamento.Numero,
                        Valor = orcamento.Total
                    });
                }
            }

            if (clienteId.HasValue)
            {
                foreach (var venda in _vendaService.ObterVendas(DateTime.Today.AddYears(-3), DateTime.Today.AddDays(1)))
                {
                    if (venda.Cliente?.Id != clienteId.Value)
                    {
                        continue;
                    }

                    timeline.Add(new OficinaTimelineItem
                    {
                        Data = venda.Data,
                        Tipo = "Venda",
                        Titulo = $"Venda {venda.FormaPagamento}",
                        Descricao = venda.Status,
                        Referencia = venda.Id.ToString()[..8],
                        Valor = venda.Total
                    });
                }
            }

            return timeline
                .OrderByDescending(item => item.Data)
                .Take(80)
                .ToList();
        }

        public IReadOnlyList<ModeloMensagemCliente> CriarModelosMensagem(OficinaKanbanCard? card)
        {
            var cliente = card?.Cliente ?? "{cliente}";
            var veiculo = card?.Veiculo ?? "{veiculo}";
            var os = card?.Numero ?? "{OS}";
            var valor = card?.ValorEstimadoFormatado ?? "{valor}";

            return new[]
            {
                Mensagem("ORCAMENTO_ENVIADO", "Orcamento enviado", $"Ola {cliente}, segue o resumo do orcamento da Primo Auto Eletrica para o veiculo {veiculo}. Total previsto: {valor}. A proposta fica sujeita a validade e disponibilidade das pecas."),
                Mensagem("ORCAMENTO_APROVADO", "Orcamento aprovado", $"Obrigado, {cliente}. Registramos a aprovacao do orcamento e vamos seguir com a OS {os}."),
                Mensagem("DIAGNOSTICO", "Veiculo em diagnostico", $"Ola {cliente}, seu veiculo {veiculo} esta em diagnostico eletrico. Avisaremos assim que houver conclusao tecnica."),
                Mensagem("AGUARDANDO_PECA", "Aguardando peca", $"Ola {cliente}, a OS {os} esta pausada aguardando peca/componente. Manteremos voce atualizado."),
                Mensagem("FINALIZADO", "Servico finalizado", $"Ola {cliente}, o servico do veiculo {veiculo} foi finalizado. Total previsto: {valor}."),
                Mensagem("PRONTO_RETIRADA", "Pronto para retirada", $"Ola {cliente}, seu veiculo {veiculo} esta pronto para retirada na Primo Auto Eletrica."),
                Mensagem("COBRANCA_AMIGAVEL", "Cobranca amigavel", $"Ola {cliente}, identificamos pendencia financeira vinculada a OS {os}. Podemos combinar a melhor forma de regularizar?"),
                Mensagem("LEMBRETE_RETORNO", "Lembrete de retorno", $"Ola {cliente}, passando para lembrar do retorno/revisao eletrica recomendado para o veiculo {veiculo}.")
            };

            static ModeloMensagemCliente Mensagem(string codigo, string titulo, string texto) => new()
            {
                Codigo = codigo,
                Titulo = titulo,
                Contexto = "WhatsApp / atendimento",
                Mensagem = texto
            };
        }

        public static string CriarMensagemOrcamentoWhatsApp(Orcamento orcamento)
        {
            ArgumentNullException.ThrowIfNull(orcamento);

            var cliente = orcamento.Cliente?.Nome ?? "cliente";
            var veiculo = orcamento.Veiculo == null
                ? "veiculo informado no atendimento"
                : $"{orcamento.Veiculo.Marca} {orcamento.Veiculo.Modelo} {orcamento.Veiculo.Placa}".Trim();
            var servicos = orcamento.Itens.Count == 0
                ? "Itens detalhados no PDF/anexo do orcamento."
                : string.Join("; ", orcamento.Itens.Take(5).Select(item => $"{item.ProdutoNome} ({item.Subtotal:C})"));

            return string.Join(Environment.NewLine, new[]
            {
                "Primo Auto Eletrica",
                $"Cliente: {cliente}",
                $"Veiculo: {veiculo}",
                $"Resumo dos servicos/pecas: {servicos}",
                $"Valor total: {orcamento.Total:C}",
                $"Validade: {orcamento.DataValidade?.ToString("dd/MM/yyyy") ?? "nao definida"}",
                string.IsNullOrWhiteSpace(orcamento.Observacoes) ? "Observacoes: sem observacoes adicionais." : $"Observacoes: {orcamento.Observacoes}",
                "Para aprovar ou recusar, responda esta mensagem para registrarmos no sistema."
            });
        }

        public void RegistrarEnvioOrcamentoWhatsApp(Guid orcamentoId)
        {
            var orcamento = _orcamentoService.ObterOrcamentoPorId(orcamentoId)
                ?? throw new InvalidOperationException("Orcamento nao encontrado para registrar envio.");

            orcamento.Status = UiTextSanitizer.EqualsNormalized(orcamento.Status, "Rascunho")
                ? "Enviado"
                : orcamento.Status;
            orcamento.Observacoes = AnexarRastreio(orcamento.Observacoes, "Enviado por WhatsApp");
            _orcamentoService.AtualizarOrcamento(orcamento);
        }

        public void MarcarOrcamentoAprovado(Guid orcamentoId)
        {
            var orcamento = _orcamentoService.ObterOrcamentoPorId(orcamentoId)
                ?? throw new InvalidOperationException("Orcamento nao encontrado para aprovar.");

            orcamento.Status = "Aprovado";
            orcamento.DataAprovacao = DateTime.Now;
            orcamento.Observacoes = AnexarRastreio(orcamento.Observacoes, "Aprovado pelo cliente");
            _orcamentoService.AtualizarOrcamento(orcamento);
        }

        public void MarcarOrcamentoRecusado(Guid orcamentoId)
        {
            var orcamento = _orcamentoService.ObterOrcamentoPorId(orcamentoId)
                ?? throw new InvalidOperationException("Orcamento nao encontrado para recusar.");

            orcamento.Status = "Recusado";
            orcamento.Observacoes = AnexarRastreio(orcamento.Observacoes, "Recusado pelo cliente");
            _orcamentoService.AtualizarOrcamento(orcamento);
        }

        public void AlterarStatusOrdem(Guid ordemId, string novoStatus)
        {
            if (string.IsNullOrWhiteSpace(novoStatus))
            {
                throw new ArgumentException("Novo status invalido.", nameof(novoStatus));
            }

            var ordem = _ordemServicoRepository.ObterPorId(ordemId)
                ?? throw new InvalidOperationException("OS nao encontrada para alterar status.");

            var statusAnterior = ordem.Status;
            ordem.Status = novoStatus.Trim();
            AplicarDatasDoStatus(ordem);
            ordem.Eventos.Insert(0, CriarEvento(ordem.Id, "Status alterado no Kanban", $"Status alterado de '{statusAnterior}' para '{ordem.Status}'.", "Kanban"));
            _ordemServicoRepository.Atualizar(ordem);
        }

        public void AvancarStatusOrdem(Guid ordemId)
        {
            var ordem = _ordemServicoRepository.ObterPorId(ordemId)
                ?? throw new InvalidOperationException("OS nao encontrada para avancar status.");
            var proximo = ObterProximoStatus(ordem.Status)
                ?? throw new InvalidOperationException("Esta OS nao possui proximo status automatico.");
            AlterarStatusOrdem(ordemId, proximo);
        }

        public static string? ObterProximoStatus(string? statusAtual)
        {
            if (UiTextSanitizer.EqualsNormalized(statusAtual, "Agendado") ||
                UiTextSanitizer.EqualsNormalized(statusAtual, "Rascunho") ||
                UiTextSanitizer.EqualsNormalized(statusAtual, "Aberta"))
                return "Em diagnostico";

            if (UiTextSanitizer.EqualsNormalized(statusAtual, "Aguardando aprovacao"))
                return "Aprovada";

            if (UiTextSanitizer.EqualsNormalized(statusAtual, "Aprovada"))
                return "Em diagnostico";

            if (UiTextSanitizer.EqualsNormalized(statusAtual, "Em diagnostico"))
                return "Em execucao";

            if (UiTextSanitizer.EqualsNormalized(statusAtual, "Aguardando peca"))
                return "Em execucao";

            if (UiTextSanitizer.EqualsNormalized(statusAtual, "Em execucao"))
                return "Finalizada";

            if (UiTextSanitizer.EqualsNormalized(statusAtual, "Finalizada"))
                return "Aguardando pagamento";

            if (UiTextSanitizer.EqualsNormalized(statusAtual, "Aguardando pagamento"))
                return "Pronta para entrega";

            if (UiTextSanitizer.EqualsNormalized(statusAtual, "Pronta para entrega"))
                return "Entregue";

            return null;
        }

        public static string CriarWhatsAppUrl(string telefone, string mensagem)
        {
            if (!CadastroValidationHelper.TryObterTelefoneWhatsApp(telefone, out var telefoneNormalizado))
            {
                throw new InvalidOperationException("Telefone/WhatsApp invalido.");
            }

            return $"https://wa.me/{telefoneNormalizado}?text={Uri.EscapeDataString(mensagem)}";
        }

        private static OficinaKanbanCard CriarCard(OrdemServico ordem, IReadOnlyDictionary<int, Funcionario> funcionarios)
        {
            var servicoPrincipal = ordem.Itens
                .OrderBy(i => i.OrdemExibicao)
                .FirstOrDefault(i => string.Equals(i.Tipo, "Servico", StringComparison.OrdinalIgnoreCase))?.Descricao
                ?? ordem.Itens.OrderBy(i => i.OrdemExibicao).FirstOrDefault()?.Descricao
                ?? Texto(ordem.ProblemaRelatado, "Servico nao detalhado");

            var tecnico = ordem.TecnicoId.HasValue && funcionarios.TryGetValue(ordem.TecnicoId.Value, out var funcionario)
                ? funcionario.Nome
                : "Nao atribuido";

            return new OficinaKanbanCard
            {
                Id = ordem.Id,
                ClienteId = ordem.ClienteId,
                VeiculoId = ordem.VeiculoId,
                Numero = ordem.Numero,
                Cliente = Texto(ordem.ClienteNomeSnapshot, "Cliente nao informado"),
                Veiculo = Texto(ordem.VeiculoDescricaoSnapshot, "Veiculo nao informado"),
                Placa = Texto(ordem.PlacaSnapshot, "Sem placa"),
                ServicoPrincipal = servicoPrincipal,
                Tecnico = tecnico,
                Prazo = ordem.DataPrevisao?.ToString("dd/MM/yyyy HH:mm") ?? "Sem previsao",
                DataPrevisao = ordem.DataPrevisao,
                Status = Texto(ordem.Status, "Rascunho"),
                Prioridade = Texto(ordem.Prioridade, "Normal"),
                Telefone = ordem.TelefoneClienteSnapshot,
                ValorEstimado = CalcularTotalOrdem(ordem)
            };
        }

        private static decimal CalcularTotalOrdem(OrdemServico ordem)
        {
            var total = ordem.Itens.Sum(item => item.Total);
            return Math.Max(0m, total - ordem.Desconto);
        }

        private static bool PertenceAColuna(OrdemServico ordem, string coluna)
        {
            var status = Texto(ordem.Status, "Rascunho");

            if (UiTextSanitizer.EqualsNormalized(coluna, "Agendado"))
            {
                return UiTextSanitizer.EqualsNormalized(status, "Agendado") ||
                       (UiTextSanitizer.EqualsNormalized(ordem.Origem, "Agendamento") &&
                        (UiTextSanitizer.EqualsNormalized(status, "Rascunho") || UiTextSanitizer.EqualsNormalized(status, "Aberta")));
            }

            if (UiTextSanitizer.EqualsNormalized(coluna, "Recebido"))
            {
                return UiTextSanitizer.EqualsNormalized(status, "Rascunho") ||
                       UiTextSanitizer.EqualsNormalized(status, "Aberta") ||
                       UiTextSanitizer.EqualsNormalized(status, "Recebido");
            }

            if (UiTextSanitizer.EqualsNormalized(coluna, "Finalizado"))
            {
                return UiTextSanitizer.EqualsNormalized(status, "Finalizada") ||
                       UiTextSanitizer.EqualsNormalized(status, "Finalizado");
            }

            if (UiTextSanitizer.EqualsNormalized(coluna, "Cancelado"))
            {
                return UiTextSanitizer.EqualsNormalized(status, "Cancelada") ||
                       UiTextSanitizer.EqualsNormalized(status, "Cancelado");
            }

            return UiTextSanitizer.EqualsNormalized(status, coluna);
        }

        private static bool Relaciona(Guid? registroClienteId, Guid? registroVeiculoId, Guid? clienteId, Guid? veiculoId)
        {
            if (clienteId.HasValue && registroClienteId == clienteId.Value)
            {
                return true;
            }

            if (veiculoId.HasValue && registroVeiculoId == veiculoId.Value)
            {
                return true;
            }

            return !clienteId.HasValue && !veiculoId.HasValue;
        }

        private static int PesoPrioridade(string? prioridade)
        {
            if (string.Equals(prioridade, "Alta", StringComparison.OrdinalIgnoreCase))
                return 3;
            if (string.Equals(prioridade, "Normal", StringComparison.OrdinalIgnoreCase))
                return 2;
            return 1;
        }

        private static string CriarDescricaoColuna(string status)
        {
            return status switch
            {
                "Agendado" => "Previstos para entrada na oficina.",
                "Recebido" => "Veiculos ja recebidos ou OS abertas.",
                "Em diagnostico" => "Analise tecnica em andamento.",
                "Aguardando aprovacao" => "Dependem de retorno do cliente.",
                "Aguardando peca" => "Pausadas por componente.",
                "Em execucao" => "Servicos em bancada/oficina.",
                "Finalizado" => "Servico concluido tecnicamente.",
                "Aguardando pagamento" => "Pendencia financeira antes da retirada.",
                "Entregue" => "Veiculo entregue ao cliente.",
                _ => "Fluxo encerrado ou cancelado."
            };
        }

        private static void AplicarDatasDoStatus(OrdemServico ordem)
        {
            if (UiTextSanitizer.EqualsNormalized(ordem.Status, "Aprovada"))
            {
                ordem.AprovadaCliente = true;
                ordem.DataAprovacao ??= DateTime.Now;
                ordem.MetodoAprovacao = Texto(ordem.MetodoAprovacao, "Atendimento");
            }

            if (UiTextSanitizer.EqualsNormalized(ordem.Status, "Em diagnostico") ||
                UiTextSanitizer.EqualsNormalized(ordem.Status, "Em execucao"))
            {
                ordem.DataInicio ??= DateTime.Now;
            }

            if (UiTextSanitizer.EqualsNormalized(ordem.Status, "Finalizada") ||
                UiTextSanitizer.EqualsNormalized(ordem.Status, "Finalizado") ||
                UiTextSanitizer.EqualsNormalized(ordem.Status, "Pronta para entrega"))
            {
                ordem.DataConclusao ??= DateTime.Now;
            }

            if (UiTextSanitizer.EqualsNormalized(ordem.Status, "Entregue"))
            {
                ordem.DataConclusao ??= DateTime.Now;
                ordem.DataEntrega ??= DateTime.Now;
            }
        }

        private static OrdemServicoEvento CriarEvento(Guid ordemId, string titulo, string descricao, string tipo)
        {
            return new OrdemServicoEvento
            {
                Id = Guid.NewGuid(),
                OrdemServicoId = ordemId,
                DataEvento = DateTime.Now,
                Titulo = titulo,
                Descricao = descricao,
                Tipo = tipo,
                Usuario = string.IsNullOrWhiteSpace(App.Session.UserName) ? "Sistema" : App.Session.UserName
            };
        }

        private static string AnexarRastreio(string observacoes, string evento)
        {
            var linha = $"[{DateTime.Now:dd/MM/yyyy HH:mm}] {evento}.";
            return string.IsNullOrWhiteSpace(observacoes)
                ? linha
                : $"{observacoes.Trim()}{Environment.NewLine}{linha}";
        }

        private static string Texto(string? valor, string fallback)
        {
            return string.IsNullOrWhiteSpace(valor) ? fallback : valor.Trim();
        }
    }
}
