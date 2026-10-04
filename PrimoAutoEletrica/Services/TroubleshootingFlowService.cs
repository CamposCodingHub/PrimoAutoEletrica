using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models.DiagnosticoGuiado;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services
{
    public class TroubleshootingFlowService : ITroubleshootingFlowService
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggerService? _logger;
        private static bool _cargaInicialVerificada = false;
        private static readonly object _syncLock = new();

        public TroubleshootingFlowService(DatabaseService databaseService, LoggerService? logger = null)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger;
        }

        public async Task<bool> GarantirCargaInicialAsync()
        {
            if (_cargaInicialVerificada) return true;

            return await Task.Run(() =>
            {
                lock (_syncLock)
                {
                    if (_cargaInicialVerificada) return true;

                    try
                    {
                        using var conn = _databaseService.GetConnection();
                        conn.Open();

                        using var cmdCheck = conn.CreateCommand();
                        cmdCheck.CommandText = "SELECT COUNT(*) FROM FluxogramasDiagnostico;";
                        var count = Convert.ToInt32(cmdCheck.ExecuteScalar() ?? 0);

                        if (count == 0)
                        {
                            InserirCargaInicial(conn);
                        }

                        _cargaInicialVerificada = true;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError("Falha ao garantir carga inicial dos fluxogramas de diagnóstico.", ex);
                        return false;
                    }
                }
            });
        }

        public async Task<List<FluxogramaDiagnostico>> ObterFluxogramasAsync(string? categoria = null, string? termo = null)
        {
            await GarantirCargaInicialAsync();

            return await Task.Run(() =>
            {
                var lista = new List<FluxogramaDiagnostico>();
                try
                {
                    using var conn = _databaseService.GetConnection();
                    conn.Open();

                    using var cmd = conn.CreateCommand();
                    var sql = "SELECT Id, Codigo, Titulo, Categoria, DescricaoSintoma, SistemaVeicular FROM FluxogramasDiagnostico WHERE 1=1";

                    if (!string.IsNullOrWhiteSpace(categoria) && categoria != "Todas")
                    {
                        sql += " AND Categoria = @Categoria";
                        cmd.Parameters.AddWithValue("@Categoria", categoria);
                    }

                    if (!string.IsNullOrWhiteSpace(termo))
                    {
                        sql += " AND (Titulo LIKE @Termo OR Codigo LIKE @Termo OR DescricaoSintoma LIKE @Termo)";
                        cmd.Parameters.AddWithValue("@Termo", $"%{termo}%");
                    }

                    sql += " ORDER BY Titulo ASC;";
                    cmd.CommandText = sql;

                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        lista.Add(new FluxogramaDiagnostico
                        {
                            Id = reader.GetInt32(0),
                            Codigo = reader.GetString(1),
                            Titulo = reader.GetString(2),
                            Categoria = reader.GetString(3),
                            DescricaoSintoma = reader.GetString(4),
                            SistemaVeicular = reader.GetString(5)
                        });
                    }

                    foreach (var f in lista)
                    {
                        CarregarPassosFluxograma(conn, f);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError("Erro ao consultar fluxogramas de diagnóstico.", ex);
                }

                return lista;
            });
        }

        public async Task<FluxogramaDiagnostico?> ObterFluxogramaPorCodigoAsync(string codigo)
        {
            await GarantirCargaInicialAsync();

            return await Task.Run(() =>
            {
                try
                {
                    using var conn = _databaseService.GetConnection();
                    conn.Open();

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        SELECT Id, Codigo, Titulo, Categoria, DescricaoSintoma, SistemaVeicular 
                        FROM FluxogramasDiagnostico 
                        WHERE Codigo = @Codigo OR Id = @CodigoId LIMIT 1;";
                    cmd.Parameters.AddWithValue("@Codigo", codigo);
                    cmd.Parameters.AddWithValue("@CodigoId", int.TryParse(codigo, out var id) ? id : -1);

                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        var fluxograma = new FluxogramaDiagnostico
                        {
                            Id = reader.GetInt32(0),
                            Codigo = reader.GetString(1),
                            Titulo = reader.GetString(2),
                            Categoria = reader.GetString(3),
                            DescricaoSintoma = reader.GetString(4),
                            SistemaVeicular = reader.GetString(5)
                        };

                        reader.Close();
                        CarregarPassosFluxograma(conn, fluxograma);
                        return fluxograma;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Erro ao obter fluxograma por código '{codigo}'.", ex);
                }

                return null;
            });
        }

        public async Task<FluxogramaPasso?> ObterPassoAsync(int fluxogramaId, int passoNumero)
        {
            await GarantirCargaInicialAsync();

            return await Task.Run(() =>
            {
                try
                {
                    using var conn = _databaseService.GetConnection();
                    conn.Open();

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        SELECT Id, FluxogramaId, PassoNumero, TituloPasso, InstrucaoTeste, FerramentaRecomendada, PontoMedicao, ValorEsperado, ObservacaoSeguranca, OpcoesJson
                        FROM FluxogramasPassos
                        WHERE FluxogramaId = @FluxoId AND PassoNumero = @PassoNum
                        LIMIT 1;";
                    cmd.Parameters.AddWithValue("@FluxoId", fluxogramaId);
                    cmd.Parameters.AddWithValue("@PassoNum", passoNumero);

                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        return MapearPasso(reader);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Erro ao obter passo {passoNumero} do fluxograma {fluxogramaId}.", ex);
                }

                return null;
            });
        }

        public async Task<FluxogramaDiagnostico?> BuscarPorSintomaOuDtcAsync(string sintomaOuDtc)
        {
            if (string.IsNullOrWhiteSpace(sintomaOuDtc)) return null;
            var fluxos = await ObterFluxogramasAsync();

            var termo = sintomaOuDtc.ToLowerInvariant();
            foreach (var f in fluxos)
            {
                if (f.Titulo.ToLowerInvariant().Contains(termo) ||
                    f.DescricaoSintoma.ToLowerInvariant().Contains(termo) ||
                    f.Codigo.ToLowerInvariant().Contains(termo))
                {
                    return f;
                }
            }

            if (termo.Contains("partida") || termo.Contains("arranque") || termo.Contains("nao vira") || termo.Contains("pesad"))
                return fluxos.Find(f => f.Codigo == "FLOW-PARTIDA-PESADA");

            if (termo.Contains("alternador") || termo.Contains("carrega") || termo.Contains("luz bateria") || termo.Contains("sobretensao"))
                return fluxos.Find(f => f.Codigo == "FLOW-ALTERNADOR-CARGA");

            if (termo.Contains("fuga") || termo.Contains("parasita") || termo.Contains("descarrega") || termo.Contains("dreno"))
                return fluxos.Find(f => f.Codigo == "FLOW-FUGA-CORRENTE");

            if (termo.Contains("ventoinha") || termo.Contains("arrefecimento") || termo.Contains("ferve") || termo.Contains("radiador"))
                return fluxos.Find(f => f.Codigo == "FLOW-VENTOINHA-TEMP");

            if (termo.Contains("farol") || termo.Contains("farois") || termo.Contains("luz fraca") || termo.Contains("amarela"))
                return fluxos.Find(f => f.Codigo == "FLOW-FAROIS-QUEDA");

            if (termo.Contains("can") || termo.Contains("rede") || termo.Contains("24v") || termo.Contains("scania") || termo.Contains("linha k"))
                return fluxos.Find(f => f.Codigo == "FLOW-24V-COMUNICACAO-CAN");

            return fluxos.Count > 0 ? fluxos[0] : null;
        }

        private static void CarregarPassosFluxograma(System.Data.Common.DbConnection conn, FluxogramaDiagnostico fluxograma)
        {
            fluxograma.Passos.Clear();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, FluxogramaId, PassoNumero, TituloPasso, InstrucaoTeste, FerramentaRecomendada, PontoMedicao, ValorEsperado, ObservacaoSeguranca, OpcoesJson
                FROM FluxogramasPassos
                WHERE FluxogramaId = @FluxoId
                ORDER BY PassoNumero ASC;";
            cmd.Parameters.AddWithValue("@FluxoId", fluxograma.Id);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                fluxograma.Passos.Add(MapearPasso(reader));
            }
        }

        private static FluxogramaPasso MapearPasso(System.Data.Common.DbDataReader reader)
        {
            var p = new FluxogramaPasso
            {
                Id = reader.GetInt32(0),
                FluxogramaId = reader.GetInt32(1),
                PassoNumero = reader.GetInt32(2),
                TituloPasso = reader.GetString(3),
                InstrucaoTeste = reader.GetString(4),
                FerramentaRecomendada = reader.GetString(5),
                PontoMedicao = reader.GetString(6),
                ValorEsperado = reader.GetString(7),
                ObservacaoSeguranca = reader.IsDBNull(8) ? null : reader.GetString(8)
            };

            var json = reader.IsDBNull(9) ? "[]" : reader.GetString(9);
            try
            {
                p.Opcoes = JsonSerializer.Deserialize<List<FluxogramaOpcaoResposta>>(json) ?? new();
            }
            catch
            {
                p.Opcoes = new();
            }

            return p;
        }

        private static void InserirCargaInicial(System.Data.Common.DbConnection conn)
        {
            using var tx = conn.BeginTransaction();

            // 1. FLOW-PARTIDA-PESADA
            InserirFluxogramaComPassos(conn, tx,
                "FLOW-PARTIDA-PESADA",
                "Motor de Partida Pesado / Não Gira",
                "Partida & Carga",
                "Veículo com arranque lento, estalando ou sem girar durante a tentativa de partida.",
                "12V Leve / 24V Pesado",
                new List<FluxogramaPasso>
                {
                    new FluxogramaPasso
                    {
                        PassoNumero = 1,
                        TituloPasso = "1. Teste de Tensão de Cranking na Bateria",
                        InstrucaoTeste = "Conecte o multímetro em V DC nos bornes de chumbo da bateria e dê a partida por 5 segundos. Qual a leitura de tensão durante a tentativa de giro?",
                        FerramentaRecomendada = "Multímetro Digital (DCV)",
                        PontoMedicao = "Bornes de chumbo da bateria (não na presilha plástica)",
                        ValorEsperado = ">= 9.60V (Sistemas 12V) ou >= 19.20V (Sistemas 24V)",
                        ObservacaoSeguranca = "Cuidado com partes móveis e correias durante a partida.",
                        Opcoes = new()
                        {
                            new FluxogramaOpcaoResposta { TextoBotao = "Abaixo de 9.6V (Tensão desaba)", ProximoPassoNumero = 2 },
                            new FluxogramaOpcaoResposta { TextoBotao = "Estável acima de 10.5V mas arranque não vira", ProximoPassoNumero = 3 },
                            new FluxogramaOpcaoResposta { TextoBotao = "Permanece 12.6V sem nenhuma queda e sem clique", ProximoPassoNumero = 4 }
                        }
                    },
                    new FluxogramaPasso
                    {
                        PassoNumero = 2,
                        TituloPasso = "2. Avaliação de Carga / Bateria em Curto vs Travamento Mecânico",
                        InstrucaoTeste = "Faça o teste de condutância (CCA) com analisador de baterias ou teste com bateria auxiliar carregada de alta amperagem. Qual o resultado?",
                        FerramentaRecomendada = "Analisador Digital de Baterias (CCA) / Multímetro",
                        PontoMedicao = "Bornes da bateria",
                        ValorEsperado = "SOH (Saúde) > 70% e SOC (Carga) > 80%",
                        Opcoes = new()
                        {
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Bateria esgotada / Não recupera CCA",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Bateria com placas sulfatadas ou vaso em curto, incapaz de suprir a corrente de arranque.",
                                AcaoRecomendada = "Substituir a bateria por nova de especificação recomendada e revisar tensão de carga do alternador.",
                                PecaSugerida = "Bateria Automotiva 60Ah Selada",
                                ServicoSugerido = "Troca de Bateria e Teste de Alternador",
                                GravidadeAvaria = "Critica"
                            },
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Bateria está boa mas motor consome > 400A e esquenta cabo",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Motor de arranque com induzido colado, buchas de apoio ovalizadas ou motor com travamento mecânico.",
                                AcaoRecomendada = "Remover motor de partida para revisão na bancada (troca de buchas, porta-escovas e induzido).",
                                PecaSugerida = "Jogo de Buchas e Induzido de Partida",
                                ServicoSugerido = "Revisão Geral do Motor de Partida",
                                GravidadeAvaria = "Critica"
                            }
                        }
                    },
                    new FluxogramaPasso
                    {
                        PassoNumero = 3,
                        TituloPasso = "3. Teste de Queda de Tensão no Circuito de Força (Cabo B+ e Malha Terra)",
                        InstrucaoTeste = "Coloque a ponta vermelha no polo (+) da bateria e a preta no terminal B+ (parafuso 13mm) do automático durante a partida. Depois meça entre carcaça do arranque e (-) da bateria.",
                        FerramentaRecomendada = "Multímetro Digital na escala 2V / 20V DC",
                        PontoMedicao = "Polo bateria -> Terminal B+ do automático | Carcaça arranque -> Polo (-) bateria",
                        ValorEsperado = "Queda máxima permitida no positivo <= 0.20V | Queda no terra <= 0.10V",
                        Opcoes = new()
                        {
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Queda positiva > 0.5V (Perda no cabo positivo)",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Resistência parasita crítica no cabo de força positivo ou terminais de bateria oxidados.",
                                AcaoRecomendada = "Limpar/substituir os terminais de bateria e reprensar cabo de 25mm² ou 35mm².",
                                PecaSugerida = "Terminal de Bateria Forjado Reforçado",
                                ServicoSugerido = "Reparo e Limpeza do Chicote de Força",
                                GravidadeAvaria = "Critica"
                            },
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Queda no aterramento > 0.3V (Falta de terra no motor)",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Malha de aterramento do motor oxidada ou frouxa, impedindo o retorno da corrente de partida.",
                                AcaoRecomendada = "Limpar os pontos de contato da malha terra ou instalar cabo de aterramento adicional de 25mm².",
                                PecaSugerida = "Malha de Aterramento Trançada de Cobre",
                                ServicoSugerido = "Reforço de Aterramento do Bloco do Motor",
                                GravidadeAvaria = "Critica"
                            },
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Ambas as quedas normais (< 0.2V) e arranque não atraca",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Solenoide / automático de partida com pastilha interna carbonizada ou escovas isoladas.",
                                AcaoRecomendada = "Substituir automático de partida e porta-escovas.",
                                PecaSugerida = "Automático de Partida (Solenoide)",
                                ServicoSugerido = "Troca de Automático de Partida",
                                GravidadeAvaria = "Critica"
                            }
                        }
                    },
                    new FluxogramaPasso
                    {
                        PassoNumero = 4,
                        TituloPasso = "4. Teste de Sinal da Linha 50 (Comando da Chave de Ignição)",
                        InstrucaoTeste = "Conecte a lâmpada de teste ou multímetro no terminal fino (linha 50) do automático de partida e gire a chave na posição partida. Chegam 12V?",
                        FerramentaRecomendada = "Lâmpada de Teste 12V / Multímetro Digital",
                        PontoMedicao = "Terminal de encaixe linha 50 no automático",
                        ValorEsperado = "12V estável durante o acionamento da chave",
                        Opcoes = new()
                        {
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Chegam 12V no fio 50 mas solenoide não atraca",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Bobina de chamada do solenoide aberta ou escovas negativas sem contato com o coletor.",
                                AcaoRecomendada = "Substituir solenoide de partida ou revisar escovas do motor de arranque.",
                                PecaSugerida = "Automático de Partida 12V",
                                ServicoSugerido = "Substituição do Automático de Partida",
                                GravidadeAvaria = "Critica"
                            },
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Zero Volts no fio 50 ao girar a chave",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Comutador da chave de ignição com contato 50 queimado, relé inibidor/partida defeituoso ou fusível rompido.",
                                AcaoRecomendada = "Testar saída do comutador de ignição sob o volante e relé de partida na caixa de fusíveis.",
                                PecaSugerida = "Comutador de Ignição Elétrico",
                                ServicoSugerido = "Troca de Comutador ou Relé de Partida",
                                GravidadeAvaria = "Moderada"
                            }
                        }
                    }
                });

            // 2. FLOW-ALTERNADOR-CARGA
            InserirFluxogramaComPassos(conn, tx,
                "FLOW-ALTERNADOR-CARGA",
                "Alternador Não Carrega ou Sobretensão",
                "Partida & Carga",
                "Luz de bateria acesa no painel, bateria descarregando com motor ligado ou lâmpadas queimando por excesso de voltagem.",
                "12V Leve / 24V Pesado",
                new List<FluxogramaPasso>
                {
                    new FluxogramaPasso
                    {
                        PassoNumero = 1,
                        TituloPasso = "1. Teste de Tensão de Carga nos Polos da Bateria",
                        InstrucaoTeste = "Dê a partida no veículo, mantenha em marcha lenta com consumidores desligados e meça a tensão nos bornes da bateria.",
                        FerramentaRecomendada = "Multímetro Digital na escala 20V DC",
                        PontoMedicao = "Bornes de chumbo da bateria",
                        ValorEsperado = "13.80V a 14.40V (Linha 12V) ou 27.60V a 28.50V (Linha 24V)",
                        Opcoes = new()
                        {
                            new FluxogramaOpcaoResposta { TextoBotao = "Tensão entre 12.0V e 12.6V (Alternador não gera)", ProximoPassoNumero = 2 },
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Tensão acima de 15.0V (Sobretensão / Excesso)",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Regulador de voltagem em curto circuito permitindo excitação descontrolada do rotor.",
                                AcaoRecomendada = "Desligar o veículo imediatamente para evitar queima de módulos eletrônicos e substituir o regulador de voltagem.",
                                PecaSugerida = "Regulador de Voltagem do Alternador",
                                ServicoSugerido = "Substituição do Regulador e Teste de Bancada",
                                GravidadeAvaria = "Critica"
                            },
                            new FluxogramaOpcaoResposta { TextoBotao = "Carrega em repouso (14.2V), mas cai abaixo de 13.0V com farol e ar ligados", ProximoPassoNumero = 3 }
                        }
                    },
                    new FluxogramaPasso
                    {
                        PassoNumero = 2,
                        TituloPasso = "2. Teste Direto no Terminal B+ do Alternador vs Carcaça",
                        InstrucaoTeste = "Coloque a ponta vermelha no parafuso B+ do alternador e a ponta preta na carcaça de alumínio do próprio alternador com o motor ligado.",
                        FerramentaRecomendada = "Multímetro Digital na escala 20V DC",
                        PontoMedicao = "Parafuso B+ do alternador <-> Carcaça do alternador",
                        ValorEsperado = "14.0V a 14.50V",
                        Opcoes = new()
                        {
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "No alternador dá 14.2V mas na bateria só chega 12.4V",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Queda de tensão severa no cabo principal de carga B+, mega-fusível oxidado ou terminal frouxo.",
                                AcaoRecomendada = "Inspecionar mega-fusível na caixa de força e substituir o terminal olhal do cabo de carga.",
                                PecaSugerida = "Mega Fusível de Lâmina 100A / 150A",
                                ServicoSugerido = "Reparo do Cabo de Carga B+ e Terminais",
                                GravidadeAvaria = "Critica"
                            },
                            new FluxogramaOpcaoResposta { TextoBotao = "No alternador também dá 12.2V (Sem geração interna)", ProximoPassoNumero = 4 }
                        }
                    },
                    new FluxogramaPasso
                    {
                        PassoNumero = 3,
                        TituloPasso = "3. Teste de Diodos Retificadores (AC Ripple) e Correia Poly-V",
                        InstrucaoTeste = "Ligue farol alto, desembaçador e ar-condicionado. Mude o multímetro para tensão ALTERNADA (AC) e meça nos polos da bateria. Observe também se a correia patina.",
                        FerramentaRecomendada = "Multímetro Digital na escala ACV (Tensão Alternada)",
                        PontoMedicao = "Bornes da bateria em escala AC",
                        ValorEsperado = "<= 0.20V AC (Ripple de corrente contínua limpa)",
                        Opcoes = new()
                        {
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Multímetro acusa tensão AC > 0.50V (Ripple alto)",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Um ou mais diodos da placa retificadora estão queimados ou abertos, reduzindo drasticamente a capacidade de corrente sob carga.",
                                AcaoRecomendada = "Substituir a placa retificadora de diodos completa e verificar isolamento do estator.",
                                PecaSugerida = "Placa Retificadora de Diodos do Alternador",
                                ServicoSugerido = "Revisão Geral do Alternador na Bancada",
                                GravidadeAvaria = "Critica"
                            },
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Ripple AC normal (< 0.2V) mas correia canta ou polia patina",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Tensionador da correia com perda de carga elástica ou polia roda-livre (OAP) travada.",
                                AcaoRecomendada = "Substituir correia Poly-V e conjunto do tensor automático.",
                                PecaSugerida = "Correia de Acessórios Poly-V e Tensor",
                                ServicoSugerido = "Troca de Correia e Tensionador de Acessórios",
                                GravidadeAvaria = "Moderada"
                            }
                        }
                    },
                    new FluxogramaPasso
                    {
                        PassoNumero = 4,
                        TituloPasso = "4. Teste de Excitação / Linha D+ e Escovas do Regulador",
                        InstrucaoTeste = "Com a ignição ligada e motor desligado, desconecte o plugue de excitação (D+ / L) e aterre o fio na carcaça. A luz de bateria acende no painel?",
                        FerramentaRecomendada = "Fio auxiliar com lâmpada de teste",
                        PontoMedicao = "Pino D+ / L do chicote do alternador",
                        ValorEsperado = "Luz de bateria acende no painel ao aterrar o fio",
                        Opcoes = new()
                        {
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Luz de bateria não acendeu (Chicote de excitação aberto)",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Circuito de excitação inicial interrompido (lâmpada piloto queimada ou fio partido no chicote). Sem excitação inicial, o alternador não começa a gerar.",
                                AcaoRecomendada = "Reparar linha da lâmpada de excitação no chicote do painel.",
                                ServicoSugerido = "Reparo Elétrico do Circuito de Excitação D+",
                                GravidadeAvaria = "Moderada"
                            },
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Luz acendeu normal mas alternador não magnetiza",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Escovas do regulador gastas no limite ou anéis coletores do rotor desgastados em degrau profundo.",
                                AcaoRecomendada = "Desmontar alternador, substituir regulador de voltagem e retificar anéis coletores do rotor.",
                                PecaSugerida = "Regulador de Voltagem / Coletor do Rotor",
                                ServicoSugerido = "Revisão e Troca de Escovas do Alternador",
                                GravidadeAvaria = "Critica"
                            }
                        }
                    }
                });

            // 3. FLOW-FUGA-CORRENTE
            InserirFluxogramaComPassos(conn, tx,
                "FLOW-FUGA-CORRENTE",
                "Fuga de Corrente Parasita (Bateria Drenando em Standby)",
                "Iluminação & Chicotes",
                "Bateria descarrega de um dia para o outro ou após veículo ficar 2 dias parado na garagem.",
                "Universal",
                new List<FluxogramaPasso>
                {
                    new FluxogramaPasso
                    {
                        PassoNumero = 1,
                        TituloPasso = "1. Teste de Consumo Standby com Amperímetro em Modo Sleep",
                        InstrucaoTeste = "Tranque todas as portas, simule o travamento da fechadura do capô, aguarde 20 minutos para que os módulos entrem em Sleep Mode e meça a corrente em série com o polo negativo.",
                        FerramentaRecomendada = "Multímetro Digital na escala 10A DC ou Garra Amperimétrica DC",
                        PontoMedicao = "Em série entre o polo negativo da bateria e o cabo negativo solto",
                        ValorEsperado = "<= 0.050A (50 mA máximo tolerado pela norma SAE)",
                        Opcoes = new()
                        {
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Consumo <= 0.05A (50 mA) - Standby Normal",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Consumo parasita em repouso perfeitamente normal. A bateria está descarregando por vida útil esgotada (sulfatação interna) ou falta de rodagem.",
                                AcaoRecomendada = "Realizar teste de saúde da bateria (CCA) e substituir se reprovada.",
                                PecaSugerida = "Bateria Automotiva Nova",
                                ServicoSugerido = "Diagnóstico e Substituição de Bateria",
                                GravidadeAvaria = "Normal"
                            },
                            new FluxogramaOpcaoResposta { TextoBotao = "Consumo entre 0.10A e 0.35A (100 a 350 mA)", ProximoPassoNumero = 2 },
                            new FluxogramaOpcaoResposta { TextoBotao = "Consumo elevado > 0.80A (800 mA até vários Amperes)", ProximoPassoNumero = 3 }
                        }
                    },
                    new FluxogramaPasso
                    {
                        PassoNumero = 2,
                        TituloPasso = "2. Isolamento de Acessórios Pós-Venda (Som, Rastreador, Alarme)",
                        InstrucaoTeste = "Desconecte temporariamente centrais não originais: Multimídia pós-venda, módulo amplificador, rastreador com bateria viciada ou alarme. O consumo caiu?",
                        FerramentaRecomendada = "Amperímetro monitorando em tempo real",
                        PontoMedicao = "Cabo negativo da bateria",
                        ValorEsperado = "Cair imediatamente para < 50 mA",
                        Opcoes = new()
                        {
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Ao desligar multimídia / som o consumo caiu para 25mA",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Aparelho de som com os fios positivo constante (+12V bateria) e pós-chave (ACC) ligados juntos.",
                                AcaoRecomendada = "Adequar o chicote do som ligando o fio vermelho no pós-chave e amarelo no constante.",
                                ServicoSugerido = "Adequação do Chicote de Som e Acessórios",
                                GravidadeAvaria = "Moderada"
                            },
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Ao desligar rastreador o consumo normalizou",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Bateria de backup interna do rastreador viciada drenando corrente ininterrupta do carro.",
                                AcaoRecomendada = "Substituir bateria de lítio interna do rastreador ou equipamento defeituoso.",
                                PecaSugerida = "Bateria Recarregável de Rastreador",
                                ServicoSugerido = "Revisão Elétrica de Rastreador",
                                GravidadeAvaria = "Moderada"
                            },
                            new FluxogramaOpcaoResposta { TextoBotao = "Acessórios não mudaram o consumo (Dreno original)", ProximoPassoNumero = 4 }
                        }
                    },
                    new FluxogramaPasso
                    {
                        PassoNumero = 3,
                        TituloPasso = "3. Teste de Diodo do Alternador e Luzes de Cortesia Ocultas",
                        InstrucaoTeste = "1) Solte a porca do cabo B+ do alternador. O consumo cai? 2) Inspecione se a luz do porta-luvas ou porta-malas permanece acesa com a tampa fechada.",
                        FerramentaRecomendada = "Chave 13mm isolada e amperímetro",
                        PontoMedicao = "Terminal B+ do alternador | Interruptores de tampa",
                        ValorEsperado = "Consumo zerar ao desligar componente causador",
                        Opcoes = new()
                        {
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Ao soltar cabo B+ do alternador o consumo desabou",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Ponte de diodos do alternador com diodo em fuga/curto para a carcaça drenando corrente reversa.",
                                AcaoRecomendada = "Substituir a placa retificadora de diodos do alternador.",
                                PecaSugerida = "Placa de Diodos do Alternador",
                                ServicoSugerido = "Reforma de Alternador",
                                GravidadeAvaria = "Critica"
                            },
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Luz de porta-luvas ou porta-malas fica acesa fechada",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Interruptor mecânico de cortesia quebrado ou desalinhado mantendo lâmpada de 5W/10W acesa direto (drena 0.4A a 0.8A).",
                                AcaoRecomendada = "Ajustar ou substituir o interruptor de cortesia de tampa.",
                                PecaSugerida = "Interruptor de Cortesia de Porta / Mala",
                                ServicoSugerido = "Troca e Regulagem do Interruptor",
                                GravidadeAvaria = "Leve"
                            }
                        }
                    },
                    new FluxogramaPasso
                    {
                        PassoNumero = 4,
                        TituloPasso = "4. Varredura por Queda de Milivolts nos Fusíveis (Técnica Doutor-IE)",
                        InstrucaoTeste = "Coloque o multímetro em mV DC nos 2 furinhos de teste de cada fusível (sem retirá-los para não acordar os módulos). Qual fusível acusa mV?",
                        FerramentaRecomendada = "Multímetro Digital na escala 200 mV DC",
                        PontoMedicao = "Pontos de prova no topo dos fusíveis de lâmina",
                        ValorEsperado = "0.0 mV em todos os fusíveis adormecidos",
                        Opcoes = new()
                        {
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Fusível do módulo de carroceria / vidros acusou milivolts",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Central de vidro ou módulo de carroceria não entra em sleep mode devido a sensor de fechadura com defeito.",
                                AcaoRecomendada = "Verificar micro-switch da fechadura da porta do motorista e fiação da coluna sanfonada.",
                                PecaSugerida = "Microswitch / Fechadura Elétrica de Porta",
                                ServicoSugerido = "Reparo do Módulo de Conforto / Fechadura",
                                GravidadeAvaria = "Moderada"
                            }
                        }
                    }
                });

            // 4. FLOW-VENTOINHA-TEMP
            InserirFluxogramaComPassos(conn, tx,
                "FLOW-VENTOINHA-TEMP",
                "Eletroventilador / Ventoinha Inoperante ou Direta",
                "Injeção & Arrefecimento",
                "Motor esquenta / ferve no trânsito porque ventoinha não liga, ou ventoinha fica ligada direto na velocidade máxima.",
                "Universal",
                new List<FluxogramaPasso>
                {
                    new FluxogramaPasso
                    {
                        PassoNumero = 1,
                        TituloPasso = "1. Identificação do Modo de Falha do Eletroventilador",
                        InstrucaoTeste = "Identifique o comportamento anômalo da ventoinha do radiador com o veículo em temperatura de trabalho.",
                        FerramentaRecomendada = "Scanner Automotivo / Termômetro Infravermelho",
                        PontoMedicao = "Painel / Scanner no parâmetro de temperatura ECT",
                        ValorEsperado = "Ventoinha deve armar na 1ª velocidade entre 95°C e 98°C e desarmar aos 92°C",
                        Opcoes = new()
                        {
                            new FluxogramaOpcaoResposta { TextoBotao = "Temperatura passa de 102°C e ventoinha NÃO liga", ProximoPassoNumero = 2 },
                            new FluxogramaOpcaoResposta { TextoBotao = "Ventoinha liga no máximo assim que liga a chave (Modo Direto)", ProximoPassoNumero = 3 }
                        }
                    },
                    new FluxogramaPasso
                    {
                        PassoNumero = 2,
                        TituloPasso = "2. Teste de Força Direta no Motor do Eletroventilador",
                        InstrucaoTeste = "Desconecte o plugue do motor da ventoinha e injete 12V e aterramento direto com cabo auxiliar de teste fusivelado. O motor girou forte?",
                        FerramentaRecomendada = "Cabo auxiliar com fusível de 30A / Multímetro",
                        PontoMedicao = "Terminais do conector do motor da ventoinha",
                        ValorEsperado = "Motor deve girar imediatamente com fluxo de ar vigoroso",
                        Opcoes = new()
                        {
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Motor não gira ou gira fraco faiscando escovas",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Motor do eletroventilador queimado com escovas gastas no limite ou induzido travado.",
                                AcaoRecomendada = "Substituir o motor ou conjunto do eletroventilador do radiador.",
                                PecaSugerida = "Motor do Eletroventilador do Radiador",
                                ServicoSugerido = "Troca do Eletroventilador e Teste de Arrefecimento",
                                GravidadeAvaria = "Critica"
                            },
                            new FluxogramaOpcaoResposta { TextoBotao = "Motor gira forte no teste direto (Problema no comando/relé)", ProximoPassoNumero = 4 }
                        }
                    },
                    new FluxogramaPasso
                    {
                        PassoNumero = 3,
                        TituloPasso = "3. Investigação de Ventoinha Armada Direto (Estratégia de Emergência)",
                        InstrucaoTeste = "Verifique com scanner os códigos DTC e retire o relé da ventoinha da central elétrica.",
                        FerramentaRecomendada = "Scanner Automotivo OBD-II",
                        PontoMedicao = "Central de injeção / Caixa de relés",
                        ValorEsperado = "Sem falha de sensor ECT e relé desarmado com motor frio",
                        Opcoes = new()
                        {
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Scanner acusa falha P0117 / P0118 (Sensor de Temperatura)",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Sensor de temperatura ECT com circuito aberto ou desconectado. A ECU adota estratégia de emergência ligando a ventoinha no máximo para evitar queima do cabeçote.",
                                AcaoRecomendada = "Substituir o sensor de temperatura da água e revisar fiação do conector.",
                                PecaSugerida = "Sensor de Temperatura da Água (ECT)",
                                ServicoSugerido = "Substituição do Sensor ECT e Sangria do Radiador",
                                GravidadeAvaria = "Critica"
                            },
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Ao retirar o relé a ventoinha desliga (Contatos colados)",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Contatos internos de força (pinos 30 e 87) do relé de potência colados / soldados por arco elétrico.",
                                AcaoRecomendada = "Substituir o relé de potência do eletroventilador.",
                                PecaSugerida = "Relé de Potência 40A / 50A",
                                ServicoSugerido = "Troca de Relé Auxiliar",
                                GravidadeAvaria = "Critica"
                            }
                        }
                    },
                    new FluxogramaPasso
                    {
                        PassoNumero = 4,
                        TituloPasso = "4. Teste de Relé e Resistência de Pré-Carga da 1ª Velocidade",
                        InstrucaoTeste = "Retire o relé de baixa velocidade e faça um jumper entre os pinos 30 e 87. A ventoinha ligou na velocidade fraca?",
                        FerramentaRecomendada = "Jumper com fio de 2.5mm² e fusível",
                        PontoMedicao = "Base do soquete do relé na central de fusíveis",
                        ValorEsperado = "Ventoinha ligar ao fechar jumper",
                        Opcoes = new()
                        {
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Não liga no jumper mas a resistência está aberta/queimada",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Resistência de pré-carga da 1ª velocidade montada no defletor com fusível térmico rompido.",
                                AcaoRecomendada = "Substituir o resistor de velocidade do eletroventilador.",
                                PecaSugerida = "Resistência da Ventoinha do Radiador",
                                ServicoSugerido = "Troca da Resistência do Defletor",
                                GravidadeAvaria = "Moderada"
                            },
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Liga no jumper mas o relé original não atraca pela ECU",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Relé com bobina aberta ou falta de comando de pulso negativo vindo do pino correspondente da ECU.",
                                AcaoRecomendada = "Substituir relé e conferir continuidade da linha de disparo negativo até a ECU.",
                                PecaSugerida = "Relé Auxiliar 4 Pinos",
                                ServicoSugerido = "Troca de Relé e Teste de Continuidade de Chicote",
                                GravidadeAvaria = "Moderada"
                            }
                        }
                    }
                });

            // 5. FLOW-FAROIS-QUEDA
            InserirFluxogramaComPassos(conn, tx,
                "FLOW-FAROIS-QUEDA",
                "Faróis Fracos / Queda de Tensão no Chicote",
                "Iluminação & Chicotes",
                "Faróis com iluminação amarelada e baixa luminosidade mesmo com lâmpadas novas instaladas.",
                "Universal",
                new List<FluxogramaPasso>
                {
                    new FluxogramaPasso
                    {
                        PassoNumero = 1,
                        TituloPasso = "1. Teste de Tensão Direta nos Terminais da Lâmpada (Soquete)",
                        InstrucaoTeste = "Ligue o motor em marcha lenta, acenda os faróis altos e meça com o multímetro a tensão real entregue nos terminais traseiros da lâmpada conectada.",
                        FerramentaRecomendada = "Multímetro Digital na escala 20V DC",
                        PontoMedicao = "Pinos positivo e negativo do soquete H4 / H7 com lâmpada acesa sob carga",
                        ValorEsperado = ">= 13.20V (Ideal 13.5V a 14.0V com alternador carregando)",
                        Opcoes = new()
                        {
                            new FluxogramaOpcaoResposta { TextoBotao = "Tensão no soquete inferior a 11.8V (Queda severa)", ProximoPassoNumero = 2 },
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Tensão no soquete normal (> 13.5V) mas luz fraca",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Lente de policarbonato do farol amarelada/opaca ou espelho refletor interno descascado/queimado.",
                                AcaoRecomendada = "Realizar polimento e cristalização dos faróis ou substituir o bloco óptico.",
                                PecaSugerida = "Lâmpadas H4 Super Branca / Farol Novo",
                                ServicoSugerido = "Polimento Técnico de Faróis",
                                GravidadeAvaria = "Leve"
                            }
                        }
                    },
                    new FluxogramaPasso
                    {
                        PassoNumero = 2,
                        TituloPasso = "2. Identificação da Queda: Circuito Positivo vs Aterramento Negativo",
                        InstrucaoTeste = "Teste A: Ponta vermelha no polo (+) da bateria e preta no pino (+) da lâmpada. Teste B: Ponta vermelha no pino (-) da lâmpada e preta no polo (-) da bateria. Onde está a maior perda?",
                        FerramentaRecomendada = "Multímetro Digital na escala 2V / 20V DC",
                        PontoMedicao = "Circuito de alimentação positiva e circuito de massa negativa sob carga",
                        ValorEsperado = "Queda positiva <= 0.20V | Queda negativa <= 0.10V",
                        Opcoes = new()
                        {
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Queda no aterramento > 0.40V (Falta de terra no soquete)",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Ponto de aterramento dianteiro (terminal olhal na longarina) com oxidação ou soquete com pino terra derretido.",
                                AcaoRecomendada = "Limpar terminal olhal de lataria e substituir soquete cerâmico do farol.",
                                PecaSugerida = "Soquete Cerâmico para Lâmpada H4 / H7",
                                ServicoSugerido = "Substituição de Soquete e Limpeza de Aterramento",
                                GravidadeAvaria = "Moderada"
                            },
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Queda no positivo > 1.50V (Perda na chave de luz/fiação)",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Chicote original fino com alta resistência ou comutador da chave de luz/seta com contatos oxidados.",
                                AcaoRecomendada = "Instalar kit de relé auxiliar duplo para faróis (alimentando lâmpadas direto da bateria via fusíveis).",
                                PecaSugerida = "Kit Relé Duplo de Farol com Porta-Fusível",
                                ServicoSugerido = "Instalação de Relés Auxiliares para Faróis",
                                GravidadeAvaria = "Moderada"
                            }
                        }
                    }
                });

            // 6. FLOW-24V-COMUNICACAO-CAN
            InserirFluxogramaComPassos(conn, tx,
                "FLOW-24V-COMUNICACAO-CAN",
                "Linha Pesada 24V: Falha de Rede CAN / Linha K",
                "Linha Pesada 24V",
                "Caminhão ou ônibus não pega, painel acusa falha de freio EBS/coordenador ou scanner não conecta na ECU.",
                "24V Diesel Pesado",
                new List<FluxogramaPasso>
                {
                    new FluxogramaPasso
                    {
                        PassoNumero = 1,
                        TituloPasso = "1. Teste de Resistência Ôhmica de Terminação da Rede CAN",
                        InstrucaoTeste = "Com a ignição desligada e baterias 24V conectadas, meça a resistência com multímetro entre os pinos CAN-High e CAN-Low (Pinos 6 e 14 do conector de diagnóstico OBD / Redondo).",
                        FerramentaRecomendada = "Multímetro Digital na escala 200 Ω",
                        PontoMedicao = "Pinos CAN-H e CAN-L da rede do chassi / powertrain",
                        ValorEsperado = "60 Ω exatos (+/- 3 Ω, resultado de 2 resistores de 120 Ω em paralelo)",
                        Opcoes = new()
                        {
                            new FluxogramaOpcaoResposta { TextoBotao = "Resistência de ~60 Ω normal (Entre 58 e 62 Ω)", ProximoPassoNumero = 2 },
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Resistência de ~120 Ω (Resistor aberto)",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Um dos resistores de terminação de 120 Ω está aberto ou módulo terminal (ex: Painel ou ECU do motor) está desconectado.",
                                AcaoRecomendada = "Verificar chicote até os módulos extremos e conector de terminação da rede CAN.",
                                ServicoSugerido = "Localização de Quebra no Par Trançado CAN",
                                GravidadeAvaria = "Critica"
                            },
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Resistência < 10 Ω ou 0 Ω (Curto entre os fios CAN)",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Curto-circuito direto entre os fios CAN-High e CAN-Low na passagem do chassi para cabine.",
                                AcaoRecomendada = "Inspecionar pontos de atrito do chicote articulado sob a cabine basculante.",
                                ServicoSugerido = "Reparo de Curto-Circuito em Chicote Articulado",
                                GravidadeAvaria = "Critica"
                            }
                        }
                    },
                    new FluxogramaPasso
                    {
                        PassoNumero = 2,
                        TituloPasso = "2. Teste de Tensões DC das Vias CAN-High e CAN-Low contra Chassi",
                        InstrucaoTeste = "Ligue a chave de contato (24V). Meça a tensão DC da via CAN-High para a massa e depois da via CAN-Low para a massa.",
                        FerramentaRecomendada = "Multímetro Digital na escala 20V DC / Osciloscópio Automotivo",
                        PontoMedicao = "CAN-High <-> Massa do Chassi | CAN-Low <-> Massa do Chassi",
                        ValorEsperado = "CAN-High: 2.6V a 3.0V | CAN-Low: 2.0V a 2.4V (Soma média ~5.0V)",
                        Opcoes = new()
                        {
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "CAN-High ou CAN-Low acusando 24V direto",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Curto-circuito do par trançado CAN com a linha 30 (+24V). Risco iminente de queima dos transceivers de todos os módulos da rede!",
                                AcaoRecomendada = "Desligar alimentação imediatamente e reparar chicote esmagado no chassi.",
                                ServicoSugerido = "Reparo Emergencial de Linha CAN 24V",
                                GravidadeAvaria = "Critica"
                            },
                            new FluxogramaOpcaoResposta
                            {
                                TextoBotao = "Tensões normais (~2.7V e ~2.3V) mas ECU não responde",
                                ProximoPassoNumero = null,
                                ConclusaoDiagnostica = "Alimentações de força (Linha 30 / Linha 15) ou massas principais da ECU do motor ausentes.",
                                AcaoRecomendada = "Verificar fusíveis de alta amperagem na central de força e relé principal de cabine.",
                                PecaSugerida = "Relé Principal de Linha 24V / Maxi Fusível 30A",
                                ServicoSugerido = "Diagnóstico de Alimentação do Módulo Powertrain",
                                GravidadeAvaria = "Critica"
                            }
                        }
                    }
                });

            tx.Commit();
        }

        private static void InserirFluxogramaComPassos(
            System.Data.Common.DbConnection conn,
            System.Data.Common.DbTransaction tx,
            string codigo,
            string titulo,
            string categoria,
            string descricao,
            string sistema,
            List<FluxogramaPasso> passos)
        {
            using var cmdFluxo = conn.CreateCommand();
            cmdFluxo.Transaction = tx;
            cmdFluxo.CommandText = @"
                INSERT OR IGNORE INTO FluxogramasDiagnostico (Codigo, Titulo, Categoria, DescricaoSintoma, SistemaVeicular)
                VALUES (@Codigo, @Titulo, @Categoria, @Descricao, @Sistema);
                SELECT Id FROM FluxogramasDiagnostico WHERE Codigo = @Codigo;";
            cmdFluxo.Parameters.AddWithValue("@Codigo", codigo);
            cmdFluxo.Parameters.AddWithValue("@Titulo", titulo);
            cmdFluxo.Parameters.AddWithValue("@Categoria", categoria);
            cmdFluxo.Parameters.AddWithValue("@Descricao", descricao);
            cmdFluxo.Parameters.AddWithValue("@Sistema", sistema);

            var idObj = cmdFluxo.ExecuteScalar();
            var fluxoId = Convert.ToInt32(idObj);

            foreach (var passo in passos)
            {
                using var cmdPasso = conn.CreateCommand();
                cmdPasso.Transaction = tx;
                cmdPasso.CommandText = @"
                    INSERT OR REPLACE INTO FluxogramasPassos 
                    (FluxogramaId, PassoNumero, TituloPasso, InstrucaoTeste, FerramentaRecomendada, PontoMedicao, ValorEsperado, ObservacaoSeguranca, OpcoesJson)
                    VALUES (@FluxoId, @PassoNum, @Titulo, @Instrucao, @Ferramenta, @Ponto, @Esperado, @Obs, @OpcoesJson);";

                cmdPasso.Parameters.AddWithValue("@FluxoId", fluxoId);
                cmdPasso.Parameters.AddWithValue("@PassoNum", passo.PassoNumero);
                cmdPasso.Parameters.AddWithValue("@Titulo", passo.TituloPasso);
                cmdPasso.Parameters.AddWithValue("@Instrucao", passo.InstrucaoTeste);
                cmdPasso.Parameters.AddWithValue("@Ferramenta", passo.FerramentaRecomendada);
                cmdPasso.Parameters.AddWithValue("@Ponto", passo.PontoMedicao);
                cmdPasso.Parameters.AddWithValue("@Esperado", passo.ValorEsperado);
                cmdPasso.Parameters.AddWithValue("@Obs", (object?)passo.ObservacaoSeguranca ?? DBNull.Value);
                cmdPasso.Parameters.AddWithValue("@OpcoesJson", JsonSerializer.Serialize(passo.Opcoes));

                cmdPasso.ExecuteNonQuery();
            }
        }
    }
}
