using Microsoft.Extensions.DependencyInjection;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Models.AI;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PrimoAutoEletrica.Services.AI
{
    public sealed class AIToolDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, string> Parameters { get; set; } = new();
    }

    public sealed class AIToolRegistry
    {
        private readonly INavigationService? _navigationService;
        private readonly IProdutoRepository? _produtoRepository;
        private readonly IFerramentaService? _ferramentaService;
        private readonly IGestaoComprasService? _comprasService;
        private readonly IClienteRepository? _clienteRepository;
        private readonly AutomotiveDiagnosticRAGService _ragService;

        public AIToolRegistry(
            INavigationService? navigationService = null,
            IProdutoRepository? produtoRepository = null,
            IFerramentaService? ferramentaService = null,
            IGestaoComprasService? comprasService = null,
            IClienteRepository? clienteRepository = null,
            AutomotiveDiagnosticRAGService? ragService = null)
        {
            _navigationService = navigationService ?? App.Services?.GetService<INavigationService>();
            _produtoRepository = produtoRepository ?? App.Repositories?.Produtos;
            _ferramentaService = ferramentaService ?? App.Services?.GetService<IFerramentaService>();
            _comprasService = comprasService ?? App.Services?.GetService<IGestaoComprasService>();
            _clienteRepository = clienteRepository ?? App.Repositories?.Clientes;
            _ragService = ragService ?? new AutomotiveDiagnosticRAGService();
        }

        public List<AIToolDefinition> ObterFerramentasDisponiveis()
        {
            return new List<AIToolDefinition>
            {
                new AIToolDefinition
                {
                    Name = "NavegarParaModulo",
                    Description = "Abre qualquer tela ou módulo do sistema PRIMOX (Estoque, Compras, Ferramentaria, OrdensServico, Clientes, Veiculos, AutoEletricaTecnica, Financeiro, Dashboard, etc.).",
                    Parameters = new() { ["modulo"] = "Nome exato do módulo (ex: Estoque, ComprasNecessidade, Ferramentas, OrdensServico, AutoEletricaTecnica, Clientes, Veiculos)" }
                },
                new AIToolDefinition
                {
                    Name = "ConsultarEstoque",
                    Description = "Verifica a quantidade disponível em estoque, preço de venda e localização de um produto ou peça.",
                    Parameters = new() { ["termo"] = "Código ou nome da peça (ex: Bateria 60Ah, Relé 40A, Lâmpada H7, Fusível 10A)" }
                },
                new AIToolDefinition
                {
                    Name = "ConsultarProdutosEmFalta",
                    Description = "Lista os produtos e peças com estoque crítico, zerado ou que atingiram o ponto de reposição/pedido.",
                    Parameters = new()
                },
                new AIToolDefinition
                {
                    Name = "ConsultarFerramentasEmUso",
                    Description = "Verifica quais ferramentas e instrumentos da oficina estão atualmente emprestados, com qual técnico e em qual OS.",
                    Parameters = new() { ["categoria"] = "Filtro opcional por tipo (ex: Diagnostico, Medicao)" }
                },
                new AIToolDefinition
                {
                    Name = "BuscarClienteVeiculo",
                    Description = "Localiza clientes ou veículos cadastrados por nome, telefone ou placa.",
                    Parameters = new() { ["termo"] = "Placa do veículo, nome do cliente ou telefone" }
                },
                new AIToolDefinition
                {
                    Name = "ConsultarDiagnosticoEletrico",
                    Description = "Consulta procedimento técnico guiado, valores de teste com multímetro/osciloscópio e causas de um código DTC ou sintoma elétrico.",
                    Parameters = new() { ["codigoOuSintoma"] = "Código de falha DTC (ex: P0562, P0335) ou sintoma (fuga de corrente, teste de relé)" }
                }
            };
        }

        public async Task<string> ExecutarFerramentaAsync(string nomeFerramenta, Dictionary<string, string> argumentos)
        {
            try
            {
                switch (nomeFerramenta.Trim())
                {
                    case "NavegarParaModulo":
                    {
                        var modulo = argumentos.GetValueOrDefault("modulo") ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(modulo)) return "Nome do módulo não informado.";

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            _navigationService?.Navigate(modulo);
                        });

                        return $"Navegado com sucesso para a tela '{modulo}'.";
                    }

                    case "ConsultarEstoque":
                    {
                        var termo = argumentos.GetValueOrDefault("termo") ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(termo)) return "Termo de busca de estoque não informado.";

                        var produtos = (_produtoRepository?.ObterTodos() ?? new List<Produto>())
                            .Where(p => (!string.IsNullOrEmpty(p.Nome) && p.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase)) ||
                                        (!string.IsNullOrEmpty(p.Codigo) && p.Codigo.Contains(termo, StringComparison.OrdinalIgnoreCase)) ||
                                        (!string.IsNullOrEmpty(p.Marca) && p.Marca.Contains(termo, StringComparison.OrdinalIgnoreCase)))
                            .ToList();

                        if (produtos.Count == 0) return $"Nenhuma peça encontrada no estoque para '{termo}'.";

                        var sb = new StringBuilder();
                        sb.AppendLine($"Resultados para '{termo}':");
                        foreach (var p in produtos.Take(5))
                        {
                            var saldoAlerta = p.QuantidadeEstoque <= p.QuantidadeMinima ? " [⚠️ ESTOQUE BAIXO]" : "";
                            sb.AppendLine($"• [{p.Codigo}] {p.Nome} - Saldo: {p.QuantidadeEstoque} un. (Mín: {p.QuantidadeMinima}){saldoAlerta} | Preço: R$ {p.PrecoVenda:N2} | Local: {p.Localizacao}");
                        }
                        return sb.ToString();
                    }

                    case "ConsultarProdutosEmFalta":
                    {
                        if (_comprasService == null) return "Serviço de compras não disponível.";
                        var itens = await _comprasService.ObterNecessidadesReposicaoAsync();
                        if (itens.Count == 0) return "Nenhum produto em falta ou com ruptura no momento. O estoque está equilibrado!";

                        var sb = new StringBuilder();
                        sb.AppendLine($"Itens que necessitam de pedido ({itens.Count} no total):");
                        foreach (var item in itens.Take(6))
                        {
                            sb.AppendLine($"• [{item.Codigo}] {item.Nome} - Saldo Atual: {item.QuantidadeEstoque} un. | Sugestão Compra: {item.QuantidadeSugeridaCompra} un. | Urgência: {item.Urgencia} | Custo Est.: R$ {item.ValorTotalEstimado:N2}");
                        }
                        return sb.ToString();
                    }

                    case "ConsultarFerramentasEmUso":
                    {
                        if (_ferramentaService == null) return "Serviço de ferramentaria não disponível.";
                        var ferramentas = await _ferramentaService.ListarFerramentasAsync(null, null, StatusFerramenta.EmUso);
                        if (ferramentas.Count == 0) return "Todas as ferramentas e instrumentos estão atualmente guardados nos armários da oficina.";

                        var sb = new StringBuilder();
                        sb.AppendLine($"Ferramentas atualmente em uso nas bancadas ({ferramentas.Count}):");
                        foreach (var f in ferramentas)
                        {
                            sb.AppendLine($"• [{f.CodigoPatrimonio}] {f.Nome} | Com: {f.FuncionarioPosseAtualNome ?? "Não informado"} | OS: {f.NumeroOSAtual ?? "Sem OS"} | Desde: {f.DataHoraRetiradaAtual:dd/MM HH:mm}");
                        }
                        return sb.ToString();
                    }

                    case "BuscarClienteVeiculo":
                    {
                        var termo = argumentos.GetValueOrDefault("termo") ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(termo)) return "Termo de busca não informado.";

                        var clientes = (_clienteRepository?.ObterTodos() ?? new List<Cliente>())
                            .Where(c => (!string.IsNullOrEmpty(c.Nome) && c.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase)) ||
                                        (!string.IsNullOrEmpty(c.Telefone) && c.Telefone.Contains(termo, StringComparison.OrdinalIgnoreCase)) ||
                                        (!string.IsNullOrEmpty(c.CPF) && c.CPF.Contains(termo, StringComparison.OrdinalIgnoreCase)))
                            .ToList();

                        var veiculos = (_clienteRepository?.ObterTodosVeiculos() ?? new List<Veiculo>())
                            .Where(v => (!string.IsNullOrEmpty(v.Placa) && v.Placa.Contains(termo, StringComparison.OrdinalIgnoreCase)) ||
                                        (!string.IsNullOrEmpty(v.Modelo) && v.Modelo.Contains(termo, StringComparison.OrdinalIgnoreCase)) ||
                                        (!string.IsNullOrEmpty(v.Marca) && v.Marca.Contains(termo, StringComparison.OrdinalIgnoreCase)))
                            .ToList();

                        var sb = new StringBuilder();
                        if (clientes.Count > 0)
                        {
                            sb.AppendLine("Clientes localizados:");
                            foreach (var c in clientes.Take(3)) sb.AppendLine($"• {c.Nome} | Tel: {c.Telefone} | CPF: {c.CPF}");
                        }

                        if (veiculos.Count > 0)
                        {
                            sb.AppendLine("Veículos localizados:");
                            foreach (var v in veiculos.Take(3)) sb.AppendLine($"• Placa: {v.Placa} | {v.Modelo} ({v.Marca}) | Ano: {v.Ano} | Cor: {v.Cor}");
                        }

                        if (clientes.Count == 0 && veiculos.Count == 0) return $"Nenhum cliente ou veículo localizado para '{termo}'.";

                        return sb.ToString();
                    }

                    case "ConsultarDiagnosticoEletrico":
                    {
                        var termo = argumentos.GetValueOrDefault("codigoOuSintoma") ?? string.Empty;
                        var contexto = _ragService.MontarContextoTecnico(termo);
                        if (string.IsNullOrWhiteSpace(contexto)) return $"Nenhum procedimento específico encontrado na base para '{termo}'.";
                        return contexto;
                    }

                    default:
                        return $"Ferramenta '{nomeFerramenta}' não reconhecida pelo sistema.";
                }
            }
            catch (Exception ex)
            {
                return $"Erro ao executar ferramenta {nomeFerramenta}: {ex.Message}";
            }
        }
    }
}
