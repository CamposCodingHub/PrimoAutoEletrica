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
        private readonly AutomotiveWebSearchService _webSearchService;
        private readonly IWorkOrderAiBridgeService? _workOrderBridgeService;
        private readonly ISureTrackService? _sureTrackService;
        private readonly IBibliotecaTecnicaService? _bibliotecaTecnicaService;
        private readonly ITroubleshootingFlowService? _troubleshootingService;
        private readonly ICalculadoraQuedaTensaoService? _calculadoraQuedaTensaoService;

        public AIToolRegistry(
            INavigationService? navigationService = null,
            IProdutoRepository? produtoRepository = null,
            IFerramentaService? ferramentaService = null,
            IGestaoComprasService? comprasService = null,
            IClienteRepository? clienteRepository = null,
            AutomotiveDiagnosticRAGService? ragService = null,
            AutomotiveWebSearchService? webSearchService = null,
            IWorkOrderAiBridgeService? workOrderBridgeService = null,
            ISureTrackService? sureTrackService = null,
            IBibliotecaTecnicaService? bibliotecaTecnicaService = null,
            ITroubleshootingFlowService? troubleshootingService = null,
            ICalculadoraQuedaTensaoService? calculadoraQuedaTensaoService = null)
        {
            _navigationService = navigationService ?? App.Services?.GetService<INavigationService>();
            _produtoRepository = produtoRepository ?? App.Repositories?.Produtos;
            _ferramentaService = ferramentaService ?? App.Services?.GetService<IFerramentaService>();
            _comprasService = comprasService ?? App.Services?.GetService<IGestaoComprasService>();
            _clienteRepository = clienteRepository ?? App.Repositories?.Clientes;
            _ragService = ragService ?? new AutomotiveDiagnosticRAGService();
            _webSearchService = webSearchService ?? new AutomotiveWebSearchService();
            _workOrderBridgeService = workOrderBridgeService ?? App.Services?.GetService<IWorkOrderAiBridgeService>();
            _sureTrackService = sureTrackService ?? App.Services?.GetService<ISureTrackService>();
            _bibliotecaTecnicaService = bibliotecaTecnicaService ?? App.Services?.GetService<IBibliotecaTecnicaService>();
            _troubleshootingService = troubleshootingService ?? App.Services?.GetService<ITroubleshootingFlowService>();
            _calculadoraQuedaTensaoService = calculadoraQuedaTensaoService ?? App.Services?.GetService<ICalculadoraQuedaTensaoService>();
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
                },
                new AIToolDefinition
                {
                    Name = "BuscarEsquemaEDefeitoWeb",
                    Description = "Realiza busca técnica em tempo real na internet por diagramas elétricos, manuais de serviço, esquemas e defeitos crônicos de um modelo de carro.",
                    Parameters = new()
                    {
                        ["sintoma"] = "Sintoma ou componente (ex: esguicho parabrisa nao funciona, alavanca sem sinal)",
                        ["veiculo"] = "Modelo, marca e ano do carro (ex: Gol G4 2008, Palio Fire, Onix)"
                    }
                },
                new AIToolDefinition
                {
                    Name = "ListarOrdensServicoAbertas",
                    Description = "Lista as Ordens de Serviço abertas na oficina com placa, cliente, veículo e status para vinculação de peças ou serviços.",
                    Parameters = new() { ["filtro"] = "Filtro opcional por placa, cliente ou número de OS" }
                },
                new AIToolDefinition
                {
                    Name = "InserirItemOrdemServico",
                    Description = "Insere uma peça ou serviço diagnosticado pela IA diretamente em uma Ordem de Serviço aberta na oficina.",
                    Parameters = new()
                    {
                        ["numeroOuIdOS"] = "Número da OS (ex: OS-2026-0001) ou ID GUID",
                        ["descricao"] = "Descrição exata da peça ou serviço a ser adicionado",
                        ["tipo"] = "Tipo do item: 'Peca' ou 'Servico'",
                        ["quantidade"] = "Quantidade do item (padrão 1)",
                        ["preco"] = "Preço unitário em Reais (opcional, busca automático do estoque se omitido)"
                    }
                },
                new AIToolDefinition
                {
                    Name = "ConsultarBaseSureTrack",
                    Description = "Consulta o histórico empírico da oficina de casos reais resolvidos, taxa de probabilidade percentual de causas-raiz e atalhos de teste de 15 minutos (Identifix / SureTrack).",
                    Parameters = new()
                    {
                        ["modelo"] = "Modelo ou montadora do veículo (ex: Onix, Gol, Palio, Corolla, Hilux, Civic)",
                        ["dtc"] = "Código de falha DTC (ex: P0300, P0118, P0562, P0016, P0101)",
                        ["sintoma"] = "Sintoma relatado (ex: falha de ignição, não pega, ventoinha direta, bateria descarregando)"
                    }
                },
                new AIToolDefinition
                {
                    Name = "ConsultarBibliotecaTecnica",
                    Description = "Consulta a biblioteca técnica automotiva nacional e de linha pesada 24V (Doutor-IE / ALLDATA Heavy Duty benchmark). Retorna pinagens de módulos de injeção (Bosch, Delphi, Marelli), módulos 24V (Scania COO7, Mercedes PLD/MR, Volvo LCM), layout e amperagem de centrais de fusíveis e relés, e especificações técnicas de veículos pesados (torque, folga de válvulas, ar-condicionado, alternador 28V).",
                    Parameters = new()
                    {
                        ["termo"] = "Termo de busca, pino, fusível ou módulo (ex: 'ME 7.5.20', 'COO7', 'PLD', 'F14', 'fusivel bomba onix', 'torque scania')",
                        ["montadora"] = "Montadora (opcional: Volkswagen, Chevrolet / GM, Fiat, Scania, Mercedes-Benz, Volvo)",
                        ["tensao"] = "Tensão do sistema (opcional: 12V ou 24V)"
                    }
                },
                new AIToolDefinition
                {
                    Name = "ConsultarFluxogramaDiagnostico",
                    Description = "Consulta árvores de diagnóstico guiado de troubleshooting passo a passo (Bosch ESI[tronic] / Doutor-IE benchmark). Retorna passos sequenciais com pergunta de eliminação lógica, ferramentas indicadas (multímetro, osciloscópio, etc.), valores esperados de referência e botões de avanço.",
                    Parameters = new()
                    {
                        ["codigoOuSintoma"] = "Código do fluxograma (ex: 'FLOW-PARTIDA-PESADA', 'FLOW-ALTERNADOR-CARGA', 'FLOW-FUGA-CORRENTE', 'FLOW-VENTOINHA-TEMP', 'FLOW-FAROIS-QUEDA', 'FLOW-24V-COMUNICACAO-CAN') ou sintoma relatado",
                        ["passo"] = "Número do passo (opcional, padrão 1)"
                    }
                },
                new AIToolDefinition
                {
                    Name = "CalcularQuedaTensao",
                    Description = "Calcula a queda de tensão (Voltage Drop Analysis) em circuitos automotivos 12V ou 24V conforme normas SAE J1128 e DIN 72551. Calcula resistência parasita do circuito em Ohms, potência térmica dissipada em Watts, bitola mínima recomendada de condutor e emite laudo de conformidade.",
                    Parameters = new()
                    {
                        ["tensaoFonte"] = "Tensão na fonte de alimentação ou bornes de bateria em Volts (ex: 12.60 ou 24.00)",
                        ["tensaoCarga"] = "Tensão medida no consumidor ou ponto de carga em Volts (ex: 11.40 ou 23.20)",
                        ["corrente"] = "Corrente elétrica do circuito em Amperes (ex: 10.0 ou 150.0 no arranque)",
                        ["comprimento"] = "Comprimento do chicote em metros (opcional, padrão 2.0)",
                        ["tipoCircuito"] = "Tipo de circuito: 'Potencia', 'Aterramento', 'Sinal' ou '24V' (opcional)"
                    }
                }
            };
        }

        public async Task<string> ExecutarFerramentaAsync(string nomeFerramenta, Dictionary<string, string> argumentos)
        {
            try
            {
                switch (nomeFerramenta.Trim())
                {
                    case "BuscarEsquemaEDefeitoWeb":
                    {
                        var sintoma = argumentos.GetValueOrDefault("sintoma") ?? string.Empty;
                        var veiculo = argumentos.GetValueOrDefault("veiculo");
                        var resWeb = await _webSearchService.PesquisarAsync(sintoma, veiculo);
                        if (resWeb.Sucesso && resWeb.Resultados.Count > 0)
                        {
                            return resWeb.ContextoFormatadoMarkdown;
                        }
                        return "Pesquisa web finalizada: nenhum resultado adicional ou computador sem conexão com a internet.";
                    }
                    case "NavegarParaModulo":
                    {
                        var modulo = argumentos.GetValueOrDefault("modulo") ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(modulo)) return "Nome do módulo não informado.";

                        if (Application.Current?.Dispatcher != null)
                        {
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                _navigationService?.Navigate(modulo);
                            });
                        }
                        else
                        {
                            _navigationService?.Navigate(modulo);
                        }

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

                    case "ListarOrdensServicoAbertas":
                    {
                        if (_workOrderBridgeService == null) return "Serviço de integração com Ordem de Serviço não disponível.";
                        var filtro = argumentos.GetValueOrDefault("filtro");
                        var ordens = await _workOrderBridgeService.ListarOrdensServicoAbertasAsync(filtro);
                        if (ordens.Count == 0) return "Nenhuma Ordem de Serviço aberta encontrada para o filtro informado.";

                        var sb = new StringBuilder();
                        sb.AppendLine($"Ordens de Serviço abertas na oficina ({ordens.Count}):");
                        foreach (var os in ordens.Take(8))
                        {
                            sb.AppendLine($"• OS #{os.Numero} | Placa: {os.PlacaSnapshot ?? "S/P"} | Veículo: {os.VeiculoDescricaoSnapshot ?? "N/D"} | Cliente: {os.ClienteNomeSnapshot ?? "N/D"} | Status: {os.Status} | Abertura: {os.DataAbertura:dd/MM/yyyy}");
                        }
                        return sb.ToString();
                    }

                    case "InserirItemOrdemServico":
                    {
                        if (_workOrderBridgeService == null) return "Serviço de integração com Ordem de Serviço não disponível.";
                        var identificadorOS = argumentos.GetValueOrDefault("numeroOuIdOS") ?? string.Empty;
                        var descricao = argumentos.GetValueOrDefault("descricao") ?? string.Empty;
                        var tipo = argumentos.GetValueOrDefault("tipo") ?? "Peca";
                        var qtdStr = argumentos.GetValueOrDefault("quantidade") ?? "1";
                        var precoStr = argumentos.GetValueOrDefault("preco");

                        if (string.IsNullOrWhiteSpace(identificadorOS) || string.IsNullOrWhiteSpace(descricao))
                            return "Número da OS e descrição do item são obrigatórios.";

                        var ordens = await _workOrderBridgeService.ListarOrdensServicoAbertasAsync(null);
                        var osAlvo = ordens.FirstOrDefault(o =>
                            o.Id.ToString().Equals(identificadorOS, StringComparison.OrdinalIgnoreCase) ||
                            (!string.IsNullOrEmpty(o.Numero) && o.Numero.Equals(identificadorOS, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrEmpty(o.Numero) && o.Numero.Contains(identificadorOS, StringComparison.OrdinalIgnoreCase)));

                        if (osAlvo == null)
                            return $"Ordem de Serviço '{identificadorOS}' não localizada entre as OS abertas no sistema.";

                        decimal.TryParse(qtdStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var qtd);
                        if (qtd <= 0) qtd = 1;

                        decimal preco = 0;
                        if (!string.IsNullOrWhiteSpace(precoStr))
                        {
                            decimal.TryParse(precoStr.Replace("R$", "").Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out preco);
                            if (preco == 0)
                                decimal.TryParse(precoStr.Replace("R$", "").Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out preco);
                        }

                        var item = new AIPartOrServiceProposal
                        {
                            Tipo = tipo.Contains("serv", StringComparison.OrdinalIgnoreCase) ? "Servico" : "Peca",
                            Descricao = descricao,
                            Quantidade = qtd,
                            PrecoSugerido = preco > 0 ? preco : 50.0m,
                            Selecionado = true
                        };

                        var inseridos = await _workOrderBridgeService.InserirItensEmOrdemServicoAsync(osAlvo.Id, new[] { item });
                        return $"✅ Sucesso: Item '{descricao}' adicionado à Ordem de Serviço #{osAlvo.Numero} ({osAlvo.ClienteNomeSnapshot} - {osAlvo.PlacaSnapshot}).";
                    }

                    case "ConsultarBaseSureTrack":
                    {
                        if (_sureTrackService == null) return "Serviço SureTrack não disponível.";
                        var modelo = argumentos.GetValueOrDefault("modelo");
                        var dtc = argumentos.GetValueOrDefault("dtc");
                        var sintoma = argumentos.GetValueOrDefault("sintoma");
                        var resultado = await _sureTrackService.ConsultarEstatisticasAsync(modelo, dtc, sintoma);
                        return resultado.ResumoEstatisticoFormatado;
                    }

                    case "ConsultarBibliotecaTecnica":
                    {
                        if (_bibliotecaTecnicaService == null) return "Serviço Biblioteca Técnica não disponível.";
                        var termo = argumentos.GetValueOrDefault("termo") ?? string.Empty;
                        var montadora = argumentos.GetValueOrDefault("montadora");
                        var tensao = argumentos.GetValueOrDefault("tensao");

                        var sb = new System.Text.StringBuilder();
                        var modulos = await _bibliotecaTecnicaService.ObterModulosAsync(termo, montadora, tensao);
                        if (modulos.Count > 0)
                        {
                            sb.AppendLine("### 🔌 Módulos Eletrônicos & Pinagens Encontrados:");
                            foreach (var m in modulos.Take(2))
                            {
                                sb.AppendLine($"**{m.NomeModulo}** ({m.Montadora} - {m.TensaoOperacao})");
                                sb.AppendLine($"*Aplicação:* {m.ModelosAplicacao}");
                                sb.AppendLine($"*Conectores:* {m.DescricaoConectores}");
                                if (m.Pinos.Count > 0)
                                {
                                    sb.AppendLine("| Conector | Pino | Função / Sinal | Tipo | Cor Fio | Tensão | Observação |");
                                    sb.AppendLine("|---|---|---|---|---|---|---|");
                                    foreach (var p in m.Pinos)
                                    {
                                        sb.AppendLine($"| {p.Conector} | {p.NumeroPino} | {p.FuncaoSinal} | {p.TipoSinal} | {p.CorFio} | {p.TensaoEsperada} | {p.ObservacoesTecnicas} |");
                                    }
                                }
                                sb.AppendLine();
                            }
                        }

                        var centrais = await _bibliotecaTecnicaService.ObterCentraisEletricasAsync(termo, montadora, tensao);
                        if (centrais.Count > 0)
                        {
                            sb.AppendLine("### ⚡ Centrais de Fusíveis e Relés:");
                            foreach (var c in centrais.Take(3))
                            {
                                sb.AppendLine($"\n#### 📌 {c.Titulo} ({c.TensaoNominal})");
                                sb.AppendLine($"*Localização:* {c.Localizacao}");
                                sb.AppendLine($"*Aplicação:* {c.ModelosAplicacao}");
                                if (c.Fusiveis.Count > 0)
                                {
                                    sb.AppendLine("\n**Tabela de Fusíveis:**");
                                    sb.AppendLine("| Posição | Amperagem | Cor Padrão | Circuito Protegido / Função | Relé Associado |");
                                    sb.AppendLine("|---|---|---|---|---|");
                                    foreach (var f in c.Fusiveis)
                                    {
                                        sb.AppendLine($"| {f.Numero} | {f.CapacidadeAmperes}A | {f.CorPadrao} | {f.CircuitoProtegido} | {f.ReleAssociado} |");
                                    }
                                }
                                if (c.Reles.Count > 0)
                                {
                                    sb.AppendLine("\n**Tabela de Relés:**");
                                    sb.AppendLine("| Posição | Relé / Função | Tipo / Pinos | Pinagem & Referência Técnica |");
                                    sb.AppendLine("|---|---|---|---|");
                                    foreach (var r in c.Reles)
                                    {
                                        sb.AppendLine($"| {r.Posicao} | {r.NomeFuncao} | {r.TipoPinos} | {r.PinagemReferencia} |");
                                    }
                                }
                                sb.AppendLine();
                            }
                        }

                        var pesados = await _bibliotecaTecnicaService.ObterEspecificacoesPesadosAsync(termo);
                        if (pesados.Count > 0)
                        {
                            sb.AppendLine("### 🚛 Especificações Técnicas Linha Pesada 24V:");
                            foreach (var p in pesados.Take(2))
                            {
                                sb.AppendLine($"**{p.Modelo}** ({p.TensaoSistema})");
                                sb.AppendLine($"- Alternador: {p.AlternadorEspecificacao}");
                                sb.AppendLine($"- Baterias: {p.BateriasEspecificacao}");
                                sb.AppendLine($"- Standby: {p.ConsumoStandbyMaximo}");
                                sb.AppendLine($"- Torque Cabeçote: {p.TorqueCabecote}");
                                sb.AppendLine($"- Folga Válvulas: {p.FolgaValvulas}");
                                sb.AppendLine($"- A/C: {p.ArCondicionadoGasGramas} | Óleo: {p.ArCondicionadoOleoTipo}");
                                sb.AppendLine($"- Dica de Chassi: {p.DicasEletricasChassi}");
                                sb.AppendLine();
                            }
                        }

                        if (sb.Length == 0)
                        {
                            return $"Nenhum registro técnico encontrado na biblioteca para o termo '{termo}'.";
                        }

                        return sb.ToString().Trim();
                    }

                    case "ConsultarFluxogramaDiagnostico":
                    {
                        if (_troubleshootingService == null) return "Serviço de fluxogramas de diagnóstico não disponível.";
                        var codigoOuSintoma = argumentos.GetValueOrDefault("codigoOuSintoma") ?? string.Empty;
                        var passoStr = argumentos.GetValueOrDefault("passo") ?? "1";
                        int.TryParse(passoStr, out var passoNum);
                        if (passoNum <= 0) passoNum = 1;

                        var fluxo = await _troubleshootingService.BuscarPorSintomaOuDtcAsync(codigoOuSintoma);
                        if (fluxo == null) return $"Nenhum fluxograma de diagnóstico localizado para '{codigoOuSintoma}'.";

                        var passo = await _troubleshootingService.ObterPassoAsync(fluxo.Id, passoNum)
                            ?? (fluxo.Passos.Count > 0 ? fluxo.Passos[0] : null);

                        var sb = new StringBuilder();
                        sb.AppendLine($"🧠 **Árvore de Diagnóstico Guiado:** {fluxo.Titulo} ({fluxo.SistemaVeicular})");
                        sb.AppendLine($"*Sintoma:* {fluxo.DescricaoSintoma}\n");

                        if (passo != null)
                        {
                            sb.AppendLine($"### 📋 Passo {passo.PassoNumero}: {passo.TituloPasso}");
                            sb.AppendLine($"👉 **Instrução de Teste:** {passo.InstrucaoTeste}");
                            sb.AppendLine($"🔧 **Instrumento Recomendado:** {passo.FerramentaRecomendada}");
                            sb.AppendLine($"📍 **Ponto de Medição:** {passo.PontoMedicao}");
                            sb.AppendLine($"⚡ **Valor Esperado:** {passo.ValorEsperado}");
                            if (!string.IsNullOrWhiteSpace(passo.ObservacaoSeguranca))
                            {
                                sb.AppendLine($"⚠️ *Atenção:* {passo.ObservacaoSeguranca}");
                            }
                            sb.AppendLine();

                            if (passo.Opcoes.Count > 0)
                            {
                                sb.AppendLine("**Opções de Avaliação / Próximos Ramos:**");
                                for (int i = 0; i < passo.Opcoes.Count; i++)
                                {
                                    var op = passo.Opcoes[i];
                                    if (op.EhConclusao)
                                    {
                                        sb.AppendLine($"- [Opção {i + 1}] **{op.TextoBotao}** ➔ 🎯 *Conclusão:* {op.ConclusaoDiagnostica} | *Ação:* {op.AcaoRecomendada}");
                                    }
                                    else
                                    {
                                        sb.AppendLine($"- [Opção {i + 1}] **{op.TextoBotao}** ➔ *Avançar para Passo {op.ProximoPassoNumero}*");
                                    }
                                }
                            }
                        }

                        return sb.ToString().Trim();
                    }

                    case "CalcularQuedaTensao":
                    {
                        if (_calculadoraQuedaTensaoService == null) return "Serviço da calculadora de queda de tensão não disponível.";

                        double.TryParse((argumentos.GetValueOrDefault("tensaoFonte") ?? "12.60").Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var vFonte);
                        double.TryParse((argumentos.GetValueOrDefault("tensaoCarga") ?? "12.40").Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var vCarga);
                        double.TryParse((argumentos.GetValueOrDefault("corrente") ?? "10.0").Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var corrente);
                        double.TryParse((argumentos.GetValueOrDefault("comprimento") ?? "2.0").Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var comp);
                        var tipoCircuito = argumentos.GetValueOrDefault("tipoCircuito") ?? "Potência";

                        var param = new PrimoAutoEletrica.Models.DiagnosticoGuiado.ParametrosCalculoQuedaTensao
                        {
                            TensaoFonteVolts = vFonte > 0 ? vFonte : 12.60,
                            TensaoCargaVolts = vCarga > 0 ? vCarga : 12.40,
                            CorrenteAmperes = corrente > 0 ? corrente : 10.0,
                            ComprimentoCaboMetros = comp > 0 ? comp : 2.0,
                            TipoCircuito = tipoCircuito
                        };

                        var res = _calculadoraQuedaTensaoService.CalcularQuedaTensao(param);
                        var sb = new StringBuilder();
                        sb.AppendLine("⚡ **Laudo Técnico de Queda de Tensão (SAE J1128 / DIN 72551):**");
                        sb.AppendLine($"- Tensão na Fonte: **{res.TensaoFonteVolts:F2}V** | Tensão na Carga: **{res.TensaoCargaVolts:F2}V** | Corrente: **{res.CorrenteAmperes:F1}A**");
                        sb.AppendLine($"- Queda Efetiva (ΔV): **{res.QuedaTensaoVolts:F2}V** ({res.PercentualQueda:F1}%)");
                        sb.AppendLine($"- Limite Máximo Normativo: **{res.LimiteMaximoToleradoVolts:F2}V**");
                        sb.AppendLine($"- Resistência Parasita: **{res.ResistenciaParasitaOhms:F4} Ω**");
                        sb.AppendLine($"- Calor Dissipado no Ponto: **{res.PotenciaDissipadaWatts:F2} Watts**");
                        sb.AppendLine($"- Bitola Mínima Sugerida: **{res.SecaoMinimaRecomendadaMm2:F2} mm²**");
                        sb.AppendLine($"- Classificação: **{res.StatusConformidade}**");
                        sb.AppendLine($"\n📋 **Diagnóstico:** {res.DiagnosticoTecnico}");
                        sb.AppendLine($"🛠️ **Ação Recomendada:** {res.AcaoRecomendada}");

                        return sb.ToString().Trim();
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
