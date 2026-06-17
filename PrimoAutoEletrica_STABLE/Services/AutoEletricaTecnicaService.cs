using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PrimoAutoEletrica.Services
{
    public sealed class AutoEletricaTecnicaService
    {
        private readonly OrcamentoDatabaseService _orcamentoService;

        public AutoEletricaTecnicaService(OrcamentoDatabaseService? orcamentoService = null)
        {
            _orcamentoService = orcamentoService ?? new OrcamentoDatabaseService();
        }

        public AutoEletricaTecnicaSnapshot CriarSnapshot(Guid? veiculoId = null)
        {
            var veiculo = veiculoId.HasValue
                ? App.Repositories.Clientes.ObterTodosVeiculos().FirstOrDefault(item => item.Id == veiculoId.Value)
                : App.Repositories.Clientes.ObterTodosVeiculos().FirstOrDefault();

            return new AutoEletricaTecnicaSnapshot
            {
                RoteirosDiagnostico = ObterRoteirosDiagnostico(),
                BibliotecaTecnica = ObterBibliotecaTecnica(),
                DefeitosRecorrentes = ObterDefeitosRecorrentes(),
                SugestoesPecas = ObterSugestoesPecas(),
                ServicosTecnicos = ObterServicosTecnicos(),
                Prontuario = veiculo == null ? null : CriarProntuario(veiculo)
            };
        }

        public ProntuarioEletricoVeiculo CriarProntuario(Veiculo veiculo)
        {
            ArgumentNullException.ThrowIfNull(veiculo);

            var prontuario = new ProntuarioEletricoVeiculo
            {
                VeiculoId = veiculo.Id,
                Veiculo = VeiculoProfileService.MontarDescricao(veiculo.Marca, veiculo.Modelo, veiculo.Ano),
                Placa = veiculo.Placa,
                SistemaEletrico = veiculo.SistemaEletrico,
                FotosTecnicas = SepararLista(veiculo.FotosTecnicas)
            };

            Campo("Sistema 12V/24V", veiculo.SistemaEletrico, "Sistema");
            Campo("Bateria instalada", PrimeiroValor(veiculo.BateriaInstalada, veiculo.BateriaPrincipal), "Bateria");
            Campo("Marca da bateria", veiculo.BateriaMarca, "Bateria");
            Campo("Amperagem", veiculo.BateriaAmperagem, "Bateria");
            Campo("Data de instalacao", veiculo.BateriaDataInstalacao?.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR")) ?? string.Empty, "Bateria");
            Campo("Teste de tensao em repouso", veiculo.TesteTensaoRepouso, "Testes", critico: ValorAparentaBaixo(veiculo.TesteTensaoRepouso, 12.2m));
            Campo("Teste de tensao na partida", veiculo.TesteTensaoPartida, "Testes", critico: ValorAparentaBaixo(veiculo.TesteTensaoPartida, 9.6m));
            Campo("Teste de carga do alternador", veiculo.TesteCargaAlternador, "Testes", critico: ValorAparentaBaixo(veiculo.TesteCargaAlternador, 13.5m));
            Campo("Corrente de fuga", veiculo.CorrenteFuga, "Testes", critico: ValorAparentaAlto(veiculo.CorrenteFuga, 0.08m));
            Campo("Estado dos aterramentos", veiculo.EstadoAterramentos, "Circuito");
            Campo("Chicotes reparados", veiculo.ChicotesReparados, "Circuito");
            Campo("Fusiveis substituidos", veiculo.FusiveisSubstituidos, "Circuito");
            Campo("Reles substituidos", veiculo.RelesSubstituidos, "Circuito");
            Campo("Lampadas substituidas", veiculo.LampadasSubstituidas, "Circuito");
            Campo("Acessorios instalados", veiculo.AcessoriosInstalados, "Acessorios");
            Campo("Observacoes tecnicas", PrimeiroValor(veiculo.ObservacoesTecnicasEletricas, veiculo.HistoricoTecnico), "Observacoes");
            Campo("Fotos tecnicas", veiculo.FotosTecnicas, "Anexos");

            return prontuario;

            void Campo(string nome, string? valor, string grupo, bool critico = false)
            {
                if (string.IsNullOrWhiteSpace(valor))
                {
                    return;
                }

                prontuario.Campos.Add(new ProntuarioEletricoCampo
                {
                    Nome = nome,
                    Valor = valor.Trim(),
                    Grupo = grupo,
                    Critico = critico
                });
            }
        }

        public List<DiagnosticoGuiadoRoteiro> ObterRoteirosDiagnostico()
        {
            return new List<DiagnosticoGuiadoRoteiro>
            {
                Roteiro("D01", "Veiculo nao da partida", "Ao girar a chave ou acionar start, o motor nao entra em funcionamento.",
                    new[] { "Bateria descarregada", "Cabo negativo oxidado", "Comutador/ignicao sem sinal", "Motor de partida travado", "Relé de partida sem acionamento" },
                    new[] { "Multimetro", "Alicate amperimetro", "Caneta de polaridade", "Scanner quando aplicavel" },
                    new[] { "Medir tensao da bateria em repouso", "Medir queda de tensao durante a partida", "Conferir sinal no automatico do motor de partida", "Testar aterramento motor/chassi", "Isolar alarme/bloqueador quando existir" },
                    new[] { "Repouso acima de 12,4V", "Partida acima de 9,6V", "Queda em cabo principal menor que 0,5V", "Sinal de partida presente no automatico" },
                    new[] { "Diagnostico eletrico", "Revisao motor partida" },
                    new[] { "Cabo bateria", "Terminal bateria", "Relé auxiliar", "Motor de partida" }),

                Roteiro("D02", "Bateria descarregando", "Bateria perde carga parada ou retorna descarregada apos poucos dias.",
                    new[] { "Corrente de fuga acima do normal", "Alternador com diodo em fuga", "Acessorio ligado direto", "Bateria sulfatada", "Modulo nao entra em sleep" },
                    new[] { "Multimetro", "Alicate amperimetro DC", "Carregador/testador de bateria" },
                    new[] { "Carregar e testar bateria", "Medir consumo em repouso apos sleep", "Remover fusiveis por setor", "Testar alternador com cabo B+ isolado", "Mapear acessorios pos-venda" },
                    new[] { "Fuga abaixo de 50mA em veiculos simples", "Bateria aprovada em teste de CCA", "Alternador sem retorno de corrente" },
                    new[] { "Fuga corrente", "Diagnostico eletrico" },
                    new[] { "Bateria", "Diodo alternador", "Porta fusivel", "Terminal olhal" }),

                Roteiro("D03", "Alternador nao carrega", "Luz de bateria acesa ou tensao nao sobe com motor ligado.",
                    new[] { "Correia frouxa", "Regulador queimado", "Diodo aberto", "Escovas gastas", "Cabo B+ rompido", "Fusivel principal aberto" },
                    new[] { "Multimetro", "Alicate amperimetro", "Osciloscopio opcional" },
                    new[] { "Conferir correia e polia", "Medir tensao na bateria em marcha lenta", "Medir tensao direto no B+ do alternador", "Testar excitacao/lampada", "Aplicar carga eletrica e medir corrente" },
                    new[] { "13,8V a 14,6V em sistema 12V", "27,6V a 28,8V em sistema 24V", "Ripple baixo e sem queda relevante no B+" },
                    new[] { "Revisao alternador" },
                    new[] { "Regulador alternador", "Escova alternador", "Diodo alternador", "Rolamento alternador" }),

                Roteiro("D04", "Motor de partida pesado", "Partida arrastada mesmo com bateria aparentemente carregada.",
                    new[] { "Bateria com CCA baixo", "Cabo positivo aquecendo", "Mau aterramento", "Buchas gastas", "Induzido em curto", "Motor mecanico pesado" },
                    new[] { "Alicate amperimetro", "Multimetro", "Testador de bateria" },
                    new[] { "Testar CCA da bateria", "Medir corrente de partida", "Medir queda no cabo positivo", "Medir queda no terra", "Testar motor de partida em bancada se necessario" },
                    new[] { "Queda positivo menor que 0,5V", "Queda negativo menor que 0,3V", "Corrente compativel com aplicacao" },
                    new[] { "Revisao motor partida", "Aterramento" },
                    new[] { "Escova motor partida", "Bucha motor partida", "Automatico partida", "Cabo negativo" }),

                Roteiro("D05", "Fusivel queimando", "Fusivel abre repetidamente ao acionar um circuito.",
                    new[] { "Curto no chicote", "Componente travado", "Fusivel abaixo da especificacao", "Relé colado", "Instalacao acessoria incorreta" },
                    new[] { "Multimetro", "Lampada serie", "Diagrama eletrico", "Alicate amperimetro" },
                    new[] { "Conferir amperagem correta do fusivel", "Isolar cargas do circuito", "Medir curto para massa", "Inspecionar chicote em dobras e passagens", "Reenergizar com lampada serie" },
                    new[] { "Resistencia nao deve indicar curto franco", "Consumo abaixo da corrente nominal do fusivel", "Fusivel correto conforme manual" },
                    new[] { "Reparo chicote", "Diagnostico eletrico" },
                    new[] { "Fusivel lamina", "Porta fusivel", "Fita tecido", "Conector reparo" }),

                Roteiro("D06", "Farol fraco", "Farol acende com baixa intensidade ou oscila.",
                    new[] { "Mau aterramento", "Queda de tensao no positivo", "Lampada incorreta", "Conector derretido", "Refletor opaco" },
                    new[] { "Multimetro", "Caneta de polaridade", "Lampada teste" },
                    new[] { "Medir tensao no conector com farol ligado", "Medir queda entre negativo da lampada e bateria", "Inspecionar soquete e terminal", "Comparar lampada esquerda/direita" },
                    new[] { "Queda total menor que 0,5V", "Tensao proxima da bateria", "Aterramento firme e sem aquecimento" },
                    new[] { "Instalacao farol", "Aterramento" },
                    new[] { "Lampada farol", "Soquete farol", "Relé auxiliar", "Terminal faston" }),

                Roteiro("D07", "Luz de re nao acende", "Luz de re permanece apagada com marcha re engatada.",
                    new[] { "Interruptor de re", "Lampada queimada", "Fusivel aberto", "Aterramento ruim", "Chicote da tampa rompido" },
                    new[] { "Multimetro", "Caneta de polaridade" },
                    new[] { "Testar lampada e soquete", "Verificar fusivel", "Medir sinal no interruptor de re", "Testar continuidade ate lanterna", "Conferir aterramento traseiro" },
                    new[] { "12V no soquete com re acionada", "Continuidade do interruptor quando engatada", "Aterramento abaixo de 0,2V de queda" },
                    new[] { "Instalacao lanterna", "Reparo chicote" },
                    new[] { "Interruptor de re", "Lampada re", "Soquete lanterna", "Fusivel" }),

                Roteiro("D08", "Lanterna nao acende", "Lanterna traseira ou dianteira nao acende.",
                    new[] { "Lampada queimada", "Soquete oxidado", "Fusivel aberto", "Chicote rompido", "Comando de luz sem saida" },
                    new[] { "Multimetro", "Caneta de polaridade", "Diagrama" },
                    new[] { "Testar lampada", "Medir tensao no soquete", "Testar aterramento", "Conferir fusivel e comando", "Inspecionar chicote em tampa traseira" },
                    new[] { "12V no positivo da lanterna", "Queda de terra menor que 0,2V", "Fusivel com continuidade" },
                    new[] { "Instalacao lanterna", "Reparo chicote" },
                    new[] { "Lampada lanterna", "Soquete lanterna", "Conector reparo", "Fusivel" }),

                Roteiro("D09", "Limpador nao funciona", "Motor do limpador nao aciona em nenhuma velocidade.",
                    new[] { "Fusivel aberto", "Motor travado", "Chave de seta/comando", "Relé temporizador", "Aterramento ruim" },
                    new[] { "Multimetro", "Caneta de polaridade", "Alicate amperimetro" },
                    new[] { "Conferir fusivel", "Medir alimentacao no motor", "Testar aterramento", "Acionar direto com protecao", "Verificar comando e relé" },
                    new[] { "12V no motor quando acionado", "Consumo sem pico excessivo", "Retorno automatico com sinal presente" },
                    new[] { "Diagnostico eletrico", "Reparo chicote" },
                    new[] { "Motor limpador", "Relé temporizador", "Fusivel", "Chave comando" }),

                Roteiro("D10", "Limpador so funciona uma velocidade", "Limpador aciona apenas lento, rapido ou temporizado.",
                    new[] { "Resistencia/trilha interna do motor", "Comando com contato gasto", "Relé temporizador", "Fio de velocidade rompido" },
                    new[] { "Multimetro", "Diagrama eletrico" },
                    new[] { "Identificar fios de velocidades", "Medir saidas do comando", "Testar motor direto por velocidade", "Conferir temporizador/intermitente" },
                    new[] { "Cada velocidade deve receber sinal proprio", "Motor deve responder a alimentacao direta protegida" },
                    new[] { "Diagnostico eletrico", "Reparo chicote" },
                    new[] { "Motor limpador", "Chave comando", "Relé temporizador", "Conector reparo" }),

                Roteiro("D11", "Vidro eletrico nao funciona", "Vidro nao sobe/desce em uma ou mais portas.",
                    new[] { "Botao interruptor", "Modulo/conforto", "Motor maquina vidro", "Chicote da porta rompido", "Fusivel ou rele" },
                    new[] { "Multimetro", "Caneta de polaridade", "Fonte 12V protegida" },
                    new[] { "Conferir fusivel", "Testar botao mestre e local", "Medir alimentacao na porta", "Inspecionar passagem da porta", "Testar motor com inversao de polaridade" },
                    new[] { "12V e terra no conjunto", "Mudanca de polaridade no motor", "Continuidade no chicote da porta" },
                    new[] { "Diagnostico eletrico", "Reparo chicote" },
                    new[] { "Botao vidro", "Maquina vidro", "Motor vidro", "Fio flexivel" }),

                Roteiro("D12", "Trava eletrica nao funciona", "Trava nao aciona uma porta ou o conjunto inteiro.",
                    new[] { "Atuador queimado", "Modulo de trava", "Chicote porta", "Controle/alarme", "Fusivel aberto" },
                    new[] { "Multimetro", "Caneta de polaridade", "Scanner quando aplicavel" },
                    new[] { "Testar comando no controle e botao", "Medir pulso no atuador", "Testar atuador direto", "Inspecionar chicote de porta", "Verificar modulo alarme/conforto" },
                    new[] { "Pulso 12V ou reversao conforme sistema", "Atuador deve acionar direto", "Fusivel integro" },
                    new[] { "Trava eletrica", "Reparo chicote" },
                    new[] { "Atuador trava", "Modulo trava", "Fusivel", "Conector porta" }),

                Roteiro("D13", "Seta nao funciona", "Seta ou pisca alerta falha em um lado ou em todos.",
                    new[] { "Lampada queimada", "Relé pisca", "Comando de seta", "Aterramento lanterna", "Modulo BCM" },
                    new[] { "Multimetro", "Caneta de polaridade", "Scanner quando aplicavel" },
                    new[] { "Conferir lampadas e soquetes", "Testar pisca alerta", "Medir saida do comando", "Testar relé ou BCM", "Conferir aterramento do conjunto" },
                    new[] { "12V pulsante no circuito", "Frequencia regular", "Sem queda alta no aterramento" },
                    new[] { "Diagnostico eletrico", "Instalacao lanterna" },
                    new[] { "Lampada seta", "Relé pisca", "Soquete", "Comando seta" }),

                Roteiro("D14", "Painel marcando errado", "Marcador, luz ou indicador do painel mostra leitura incorreta.",
                    new[] { "Sensor defeituoso", "Aterramento painel", "Mau contato conector", "Rede CAN com falha", "Modulo painel" },
                    new[] { "Scanner", "Multimetro", "Osciloscopio quando possivel" },
                    new[] { "Ler avarias por scanner", "Comparar valor real e valor do painel", "Medir alimentacao e terra do sensor", "Inspecionar conector do painel", "Checar rede CAN se houver falhas U" },
                    new[] { "5V ou 12V conforme sensor", "Resistencia dentro da tabela do fabricante", "CAN com comunicacao estavel" },
                    new[] { "Diagnostico eletrico" },
                    new[] { "Sensor nivel", "Sensor temperatura", "Conector painel", "Terminal reparo" }),

                Roteiro("D15", "Curto intermitente", "Defeito aparece com vibracao, chuva, curva ou aquecimento.",
                    new[] { "Chicote raspando", "Conector oxidado", "Componente aquecendo", "Acessorio mal instalado", "Umidade em modulo" },
                    new[] { "Multimetro", "Lampada serie", "Scanner", "Spray limpa contato", "Diagrama" },
                    new[] { "Reproduzir condicao do defeito", "Monitorar corrente do circuito", "Movimentar chicotes por setor", "Inspecionar pontos de atrito/umidade", "Registrar foto do ponto encontrado" },
                    new[] { "Consumo estavel", "Sem queda/curto ao movimentar chicote", "Conector seco e sem zinabre" },
                    new[] { "Reparo chicote", "Diagnostico eletrico" },
                    new[] { "Fita tecido", "Termo retratil", "Conector reparo", "Porta fusivel" }),

                Roteiro("D16", "Relé nao aciona", "Relé nao arma ou nao entrega alimentacao ao circuito.",
                    new[] { "Bobina aberta", "Sem positivo/negativo de comando", "Comando ECU ausente", "Contato interno queimado", "Relé errado" },
                    new[] { "Multimetro", "Fonte 12V", "Caneta de polaridade" },
                    new[] { "Identificar terminais 30/85/86/87", "Medir positivo permanente", "Medir comando da bobina", "Testar relé em bancada", "Medir saida 87 com carga" },
                    new[] { "Bobina com resistencia compativel", "Clique ao energizar 85/86", "Continuidade 30-87 quando acionado" },
                    new[] { "Relé auxiliar", "Diagnostico eletrico" },
                    new[] { "Relé 4 pinos", "Relé 5 pinos", "Soquete relé", "Terminal faston" }),

                Roteiro("D17", "Mau aterramento", "Circuitos oscilam, luzes fracas ou multiplas falhas sem causa aparente.",
                    new[] { "Cabo terra oxidado", "Ponto de massa solto", "Pintura isolando contato", "Terminal aquecido", "Chicote terra rompido" },
                    new[] { "Multimetro", "Alicate amperimetro", "Escova/limpa contato" },
                    new[] { "Inspecionar pontos de massa", "Medir queda de tensao sob carga", "Comparar terra motor/chassi/bateria", "Limpar e reapertar conexoes", "Aplicar carga e repetir medicao" },
                    new[] { "Queda de terra menor que 0,2V em cargas leves", "Menor que 0,5V em cargas altas", "Sem aquecimento em terminal" },
                    new[] { "Aterramento", "Diagnostico eletrico" },
                    new[] { "Cabo negativo", "Terminal olhal", "Parafuso massa", "Malha aterramento" })
            };
        }

        public List<BibliotecaTecnicaItem> ObterBibliotecaTecnica()
        {
            return new List<BibliotecaTecnicaItem>
            {
                Biblioteca("Como testar rele 4 pinos", "Rele", "Valida bobina 85/86 e contato 30/87.", new[] { "Multimetro", "Fonte 12V" }, new[] { "Identifique 85, 86, 30 e 87", "Energize bobina com protecao", "Confirme clique e continuidade 30-87" }, new[] { "Bobina geralmente entre 60 e 120 ohms", "Continuidade baixa no contato fechado" }),
                Biblioteca("Como testar rele 5 pinos", "Rele", "Valida contato reversivel 30/87/87a.", new[] { "Multimetro", "Fonte 12V" }, new[] { "Sem bobina, 30 deve comunicar com 87a", "Com bobina energizada, 30 deve comunicar com 87", "Teste aquecimento e folga dos terminais" }, new[] { "87a fechado em repouso", "87 fechado acionado" }),
                Biblioteca("Como testar fusivel", "Protecao", "Confere continuidade e queda de tensao real.", new[] { "Multimetro", "Caneta de polaridade" }, new[] { "Teste continuidade fora do circuito", "Com carga ligada, meca tensao nos dois lados", "Investigue se a troca volta a queimar" }, new[] { "0V de queda ideal", "Mesmo potencial nos dois lados" }),
                Biblioteca("Como testar aterramento", "Circuito", "Mede queda de tensao com circuito carregado.", new[] { "Multimetro" }, new[] { "Ponta positiva no negativo da carga", "Ponta negativa no negativo da bateria", "Acione o consumidor e leia a queda" }, new[] { "Ate 0,2V em carga leve", "Ate 0,5V em motor de partida" }),
                Biblioteca("Como testar queda de tensao", "Circuito", "Metodo para achar resistencia em cabos e conexoes.", new[] { "Multimetro" }, new[] { "Teste sempre com carga ligada", "Meca positivo e negativo separadamente", "Divida o circuito por trechos ate localizar perda" }, new[] { "Queda total abaixo de 0,5V para maioria dos circuitos 12V" }),
                Biblioteca("Como testar bateria", "Bateria", "Verifica repouso, CCA e recuperacao apos carga.", new[] { "Testador CCA", "Carregador", "Multimetro" }, new[] { "Carregue se abaixo de 12,4V", "Teste CCA informado no rotulo", "Observe recuperacao apos partida" }, new[] { "12,6V repouso carregada", "Acima de 9,6V durante partida" }),
                Biblioteca("Como testar alternador", "Carga", "Confere tensao, corrente e ripple.", new[] { "Multimetro", "Alicate amperimetro" }, new[] { "Meca em marcha lenta", "Ligue farol/ventilador/desembacador", "Compare B+ alternador e bateria" }, new[] { "13,8V a 14,6V em 12V", "27,6V a 28,8V em 24V" }),
                Biblioteca("Como testar motor de partida", "Partida", "Separa defeito de bateria, cabo e motor.", new[] { "Alicate amperimetro", "Multimetro" }, new[] { "Teste CCA", "Meca queda no positivo", "Meca queda no terra", "Compare corrente com aplicacao" }, new[] { "Partida acima de 9,6V", "Queda cabo positivo menor que 0,5V" }),
                Biblioteca("Como definir bitola de fios", "Instalacao", "Escolhe cabo por corrente, distancia e queda aceitavel.", new[] { "Tabela bitola", "Alicate amperimetro" }, new[] { "Calcule corrente do consumidor", "Considere ida e volta do circuito", "Use fusivel proximo a fonte" }, new[] { "Queda abaixo de 3% para iluminacao", "Fusivel abaixo da capacidade do cabo" }),
                Biblioteca("Cuidados em 12V e 24V", "Sistema", "Evita componentes errados em frota mista.", new[] { "Multimetro" }, new[] { "Confirme tensao antes de energizar", "Valide relés, lampadas e acessorios", "Identifique conversores 24/12V" }, new[] { "12V nominal: 13,8 a 14,6V carregando", "24V nominal: 27,6 a 28,8V carregando" }),
                Biblioteca("Como crimpar terminais", "Conectores", "Evita mau contato em reparos.", new[] { "Alicate de crimpagem", "Termo retratil" }, new[] { "Escolha terminal pela bitola", "Crimpe sem cortar veios", "Aplique isolacao e alivio de tracao" }, new[] { "Terminal firme sem aquecimento", "Sem cobre exposto fora da crimpagem" }),
                Biblioteca("Como recuperar conectores", "Conectores", "Reparo tecnico de plugs oxidado ou derretido.", new[] { "Extrator de terminal", "Limpa contato" }, new[] { "Fotografe posicao dos fios", "Remova terminal danificado", "Substitua por terminal equivalente", "Teste travamento" }, new[] { "Terminal sem folga", "Sem zinabre ou derretimento" }),
                Biblioteca("Cores de fios automotivos", "Chicote", "Organiza rastreio por funcao e fabricante.", new[] { "Diagrama", "Caneta marcadora" }, new[] { "Nao confie somente na cor", "Confirme por continuidade e diagrama", "Marque emenda feita na OS" }, new[] { "Continuidade confirmada antes de cortar", "Identificacao registrada no prontuario" }),
                Biblioteca("Cuidados com rede CAN", "Eletronica", "Diagnostico sem danificar modulo.", new[] { "Scanner", "Osciloscopio", "Multimetro" }, new[] { "Nunca alimente rede CAN diretamente", "Teste resistencia com bateria desconectada", "Procure curto para positivo/massa" }, new[] { "Aproximadamente 60 ohms entre CAN H e CAN L", "Sinal diferencial estavel" }),
                Biblioteca("Cuidados com modulos eletronicos", "Eletronica", "Previne troca indevida de modulo.", new[] { "Scanner", "Fonte estabilizada" }, new[] { "Verifique alimentacoes e terras", "Leia avarias e dados ao vivo", "Confirme rede antes de condenar modulo" }, new[] { "Alimentacao nominal presente", "Aterramentos sem queda", "Comunicacao de rede ok" })
            };
        }

        public List<SugestaoPecasServico> ObterSugestoesPecas()
        {
            return new List<SugestaoPecasServico>
            {
                Sugestao("Lanterna", new[] { "Lampada lanterna", "Soquete lanterna", "Conector reparo", "Fusivel lamina", "Fita isolante automotiva" }, "Conferir aterramento traseiro antes de trocar conjunto."),
                Sugestao("Alternador", new[] { "Regulador alternador", "Escova alternador", "Diodo alternador", "Rolamento alternador", "Correia auxiliar" }, "Medir carga com consumidores ligados e avaliar ripple."),
                Sugestao("Motor partida", new[] { "Escova motor partida", "Bucha motor partida", "Automatico partida", "Cabo negativo", "Terminal bateria" }, "Sempre registrar queda de tensao antes de condenar o motor."),
                Sugestao("Chicote farol", new[] { "Soquete farol", "Relé auxiliar", "Porta fusivel", "Terminal faston", "Fio automotivo 2,5mm" }, "Dimensionar bitola e fusivel pelo consumo real."),
                Sugestao("Fuga corrente", new[] { "Porta fusivel", "Conector reparo", "Diodo alternador", "Terminal olhal" }, "Aguardar sleep dos modulos antes da medicao final."),
                Sugestao("Aterramento", new[] { "Cabo negativo", "Malha aterramento", "Terminal olhal", "Parafuso massa", "Limpa contato" }, "Limpar contato metal-metal e proteger contra oxidacao."),
                Sugestao("Vidro eletrico", new[] { "Botao vidro", "Motor vidro", "Maquina vidro", "Fio flexivel porta" }, "Testar inversao de polaridade no motor."),
                Sugestao("Trava eletrica", new[] { "Atuador trava", "Modulo trava", "Conector porta", "Fusivel" }, "Conferir pulso e alarme antes de trocar atuador."),
                Sugestao("Relé auxiliar", new[] { "Relé 4 pinos", "Soquete rele", "Porta fusivel", "Terminal faston" }, "Usar alimentacao protegida proxima a bateria.")
            };
        }

        public SugestaoPecasServico? ObterSugestaoPecas(string servico)
        {
            if (string.IsNullOrWhiteSpace(servico))
            {
                return null;
            }

            return ObterSugestoesPecas()
                .FirstOrDefault(item => item.Servico.Contains(servico, StringComparison.OrdinalIgnoreCase) ||
                                        servico.Contains(item.Servico, StringComparison.OrdinalIgnoreCase));
        }

        public List<ServicoTecnicoAutoEletrica> ObterServicosTecnicos()
        {
            return new List<ServicoTecnicoAutoEletrica>
            {
                Servico("Diagnostico eletrico", "Rastreamento tecnico com medições e conclusao registrada.", 180m, 90, 30, new[] { "Fusivel", "Terminal", "Conector reparo" }),
                Servico("Revisao alternador", "Desmontagem, teste de bancada e substituicao de componentes de carga.", 280m, 180, 90, new[] { "Regulador", "Escova", "Diodo", "Rolamento" }),
                Servico("Revisao motor partida", "Revisao completa de motor de partida com teste de corrente.", 260m, 180, 90, new[] { "Escova", "Bucha", "Automatico", "Cabo negativo" }),
                Servico("Instalacao farol", "Instalacao, ajuste e teste de queda de tensao em farol.", 140m, 75, 30, new[] { "Lampada", "Soquete", "Relé auxiliar" }),
                Servico("Lanterna", "Reparo ou substituicao de lanterna e soquetes.", 120m, 60, 30, new[] { "Lampada", "Soquete", "Conector reparo" }),
                Servico("LED", "Instalacao de LED com protecao e orientacao tecnica.", 160m, 90, 30, new[] { "Kit LED", "Canceller", "Porta fusivel" }),
                Servico("Reparo chicote", "Reparo de chicote com emenda tecnica, isolamento e teste sob carga.", 220m, 150, 60, new[] { "Fio automotivo", "Termo retratil", "Fita tecido", "Conector" }),
                Servico("Relé auxiliar", "Instalacao de rele auxiliar com circuito protegido.", 150m, 80, 60, new[] { "Relé 4 pinos", "Soquete rele", "Porta fusivel", "Terminal" }),
                Servico("Tomada carreta", "Instalacao e teste de tomada de carreta.", 220m, 140, 60, new[] { "Tomada carreta", "Modulo reboque", "Fio automotivo" }),
                Servico("Alarme", "Instalacao ou revisao de alarme automotivo.", 280m, 180, 90, new[] { "Central alarme", "Sirene", "Sensor ultrassom", "Relé bloqueio" }),
                Servico("Trava eletrica", "Instalacao ou reparo de travas eletricas.", 240m, 150, 90, new[] { "Atuador trava", "Modulo trava", "Haste", "Conector" }),
                Servico("Som/acessorio", "Instalacao de acessorio com alimentacao protegida.", 180m, 120, 30, new[] { "Porta fusivel", "Cabo alimentacao", "Terminal olhal", "Conector" }),
                Servico("Fuga corrente", "Rastreamento de consumo parasita por circuito.", 220m, 150, 30, new[] { "Fusivel", "Diodo alternador", "Conector reparo" }),
                Servico("Aterramento", "Revisao e reforco de aterramentos motor/chassi/bateria.", 140m, 75, 60, new[] { "Cabo negativo", "Malha aterramento", "Terminal olhal", "Parafuso massa" })
            };
        }

        public List<DefeitoRecorrenteResumo> ObterDefeitosRecorrentes()
        {
            var ordens = App.Repositories.OrdensServico.ObterTodos(incluirInativas: true)
                .Where(ordem => ordem.Ativo)
                .ToList();
            var veiculos = App.Repositories.Clientes.ObterTodosVeiculos().ToDictionary(v => v.Id, v => v);
            var resultados = new List<DefeitoRecorrenteResumo>();

            resultados.AddRange(ordens
                .Select(ordem => new { Ordem = ordem, Defeito = NormalizarDefeito(ordem) })
                .Where(item => !string.IsNullOrWhiteSpace(item.Defeito))
                .GroupBy(item => item.Defeito)
                .Where(grupo => grupo.Count() >= 1)
                .OrderByDescending(grupo => grupo.Count())
                .Take(5)
                .Select(grupo => CriarResumo("Mesmo defeito", grupo.Key, grupo.Select(item => item.Ordem), veiculos)));

            resultados.AddRange(ordens
                .SelectMany(ordem => ordem.Itens
                    .Where(item => !string.IsNullOrWhiteSpace(item.Descricao))
                    .Select(item => new { Ordem = ordem, Peca = item.Descricao.Trim() }))
                .GroupBy(item => item.Peca, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(grupo => grupo.Count())
                .Take(5)
                .Select(grupo => CriarResumo("Peca que mais falha", grupo.Key, grupo.Select(item => item.Ordem), veiculos)));

            resultados.AddRange(ordens
                .Where(ordem => ordem.GarantiaValidaAte.HasValue && ordem.GarantiaValidaAte.Value.Date >= DateTime.Today)
                .GroupBy(ordem => string.IsNullOrWhiteSpace(ordem.GarantiaObservacoes) ? "Servico em garantia" : ordem.GarantiaObservacoes.Trim())
                .OrderByDescending(grupo => grupo.Count())
                .Take(5)
                .Select(grupo => CriarResumo("Garantia ativa", grupo.Key, grupo, veiculos, emGarantia: true)));

            resultados.AddRange(ordens
                .Where(ordem => ordem.VeiculoId.HasValue && veiculos.ContainsKey(ordem.VeiculoId.Value))
                .Select(ordem => new
                {
                    Ordem = ordem,
                    Veiculo = veiculos[ordem.VeiculoId!.Value],
                    Defeito = NormalizarDefeito(ordem)
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Defeito))
                .GroupBy(item => $"{item.Veiculo.Marca} {item.Veiculo.Modelo}: {item.Defeito}")
                .OrderByDescending(grupo => grupo.Count())
                .Take(5)
                .Select(grupo => CriarResumo("Defeito por modelo", grupo.Key, grupo.Select(item => item.Ordem), veiculos)));

            resultados.AddRange(ordens
                .Where(ordem => ObterTempoResolucaoMinutos(ordem) > 0)
                .Select(ordem => new { Ordem = ordem, Defeito = NormalizarDefeito(ordem) })
                .Where(item => !string.IsNullOrWhiteSpace(item.Defeito))
                .GroupBy(item => item.Defeito)
                .OrderByDescending(grupo => grupo.Average(item => ObterTempoResolucaoMinutos(item.Ordem)))
                .Take(5)
                .Select(grupo => CriarResumo("Tempo medio de resolucao", grupo.Key, grupo.Select(item => item.Ordem), veiculos)));

            return resultados
                .OrderByDescending(item => item.Quantidade)
                .ThenBy(item => item.Tipo)
                .ToList();
        }

        public Orcamento CriarOrcamentoAPartirDiagnostico(OrcamentoDiagnosticoDraft draft)
        {
            ArgumentNullException.ThrowIfNull(draft);

            var cliente = App.Repositories.Clientes.ObterPorId(draft.ClienteId)
                ?? throw new InvalidOperationException("Cliente do diagnostico nao foi localizado.");
            var veiculo = draft.VeiculoId.HasValue
                ? App.Repositories.Clientes.ObterTodosVeiculos().FirstOrDefault(item => item.Id == draft.VeiculoId.Value)
                : null;

            var valorMaoObra = draft.ValorMaoObra <= 0 ? 180m : draft.ValorMaoObra;
            var orcamento = new Orcamento
            {
                ClienteId = cliente.Id,
                VeiculoId = veiculo?.Id,
                Cliente = cliente,
                Veiculo = veiculo,
                Status = "Rascunho",
                DataCriacao = DateTime.Now,
                DataValidade = DateTime.Today.AddDays(7),
                Diagnostico = $"{draft.Roteiro}\nResultado: {draft.Resultado}\nConclusao: {draft.Conclusao}".Trim(),
                Observacoes = "Orcamento gerado pelo diagnostico guiado de auto eletrica.",
                CondicoesPagamento = "A combinar",
                PrazoEntrega = "Apos aprovacao e disponibilidade das pecas"
            };

            orcamento.Itens.Add(new OrcamentoItem
            {
                OrcamentoId = orcamento.Id,
                Tipo = "Servico",
                ProdutoNome = $"Diagnostico/servico tecnico - {draft.Roteiro}",
                ProdutoCodigo = "SERVICO",
                ProdutoCategoria = "Auto Eletrica",
                Quantidade = 1,
                PrecoUnitario = valorMaoObra,
                PrecoCusto = 0m,
                Subtotal = valorMaoObra,
                Observacoes = draft.Conclusao
            });

            foreach (var peca in draft.Pecas.Where(peca => !string.IsNullOrWhiteSpace(peca)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                orcamento.Itens.Add(new OrcamentoItem
                {
                    OrcamentoId = orcamento.Id,
                    Tipo = "Servico",
                    ProdutoNome = $"Peca sugerida: {peca.Trim()}",
                    ProdutoCodigo = "SUGESTAO",
                    ProdutoCategoria = "Peca sugerida",
                    Quantidade = 1,
                    PrecoUnitario = 0m,
                    PrecoCusto = 0m,
                    Subtotal = 0m,
                    Observacoes = "Peca sugerida pelo roteiro tecnico; confirmar codigo e aplicacao antes de aprovar."
                });
            }

            _orcamentoService.AdicionarOrcamento(orcamento);
            return orcamento;
        }

        private static DiagnosticoGuiadoRoteiro Roteiro(
            string codigo,
            string titulo,
            string sintoma,
            IEnumerable<string> causas,
            IEnumerable<string> ferramentas,
            IEnumerable<string> testes,
            IEnumerable<string> valores,
            IEnumerable<string> servicos,
            IEnumerable<string> pecas)
        {
            return new DiagnosticoGuiadoRoteiro
            {
                Codigo = codigo,
                Titulo = titulo,
                Sintoma = sintoma,
                PossiveisCausas = causas.ToList(),
                Ferramentas = ferramentas.ToList(),
                SequenciaTestes = testes.ToList(),
                ValoresEsperados = valores.ToList(),
                Resultado = "Aguardando medicao do tecnico.",
                Conclusao = "Preencher conclusao tecnica apos a sequencia de testes.",
                FotoAnexa = "Anexar foto do ponto testado ou componente condenado.",
                ServicosSugeridos = servicos.ToList(),
                PecasSugeridas = pecas.ToList()
            };
        }

        private static BibliotecaTecnicaItem Biblioteca(
            string titulo,
            string categoria,
            string resumo,
            IEnumerable<string> ferramentas,
            IEnumerable<string> passos,
            IEnumerable<string> valores)
        {
            return new BibliotecaTecnicaItem
            {
                Titulo = titulo,
                Categoria = categoria,
                Resumo = resumo,
                Ferramentas = ferramentas.ToList(),
                Passos = passos.ToList(),
                ValoresReferencia = valores.ToList(),
                Cuidados = new List<string>
                {
                    "Usar circuito protegido quando energizar componentes fora do veiculo.",
                    "Registrar resultado e foto quando houver componente condenado."
                }
            };
        }

        private static SugestaoPecasServico Sugestao(string servico, IEnumerable<string> pecas, string observacao)
        {
            return new SugestaoPecasServico
            {
                Servico = servico,
                Pecas = pecas.ToList(),
                Observacao = observacao
            };
        }

        private static ServicoTecnicoAutoEletrica Servico(
            string nome,
            string descricao,
            decimal valor,
            int tempo,
            int garantia,
            IEnumerable<string> pecas)
        {
            return new ServicoTecnicoAutoEletrica
            {
                Nome = nome,
                Descricao = descricao,
                ValorPadrao = valor,
                TempoMedioMinutos = tempo,
                GarantiaPadraoDias = garantia,
                PecasSugeridas = pecas.ToList()
            };
        }

        private static DefeitoRecorrenteResumo CriarResumo(
            string tipo,
            string descricao,
            IEnumerable<OrdemServico> ordens,
            IReadOnlyDictionary<Guid, Veiculo> veiculos,
            bool emGarantia = false)
        {
            var lista = ordens.DistinctBy(ordem => ordem.Id).ToList();
            var modelos = lista
                .Select(ordem => ordem.VeiculoId.HasValue && veiculos.TryGetValue(ordem.VeiculoId.Value, out var veiculo)
                    ? $"{veiculo.Marca} {veiculo.Modelo}".Trim()
                    : ordem.VeiculoDescricaoSnapshot)
                .Where(modelo => !string.IsNullOrWhiteSpace(modelo))
                .GroupBy(modelo => modelo, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(grupo => grupo.Count())
                .Select(grupo => grupo.Key)
                .FirstOrDefault() ?? "Modelo nao informado";

            var tempos = lista
                .Select(ObterTempoResolucaoMinutos)
                .Where(tempo => tempo > 0)
                .ToList();

            return new DefeitoRecorrenteResumo
            {
                Tipo = tipo,
                Descricao = descricao,
                Modelo = modelos,
                Quantidade = lista.Count,
                TempoMedioMinutos = tempos.Count == 0 ? 0 : (int)Math.Round(tempos.Average()),
                ValorTotal = lista.Sum(ordem => ordem.Itens.Sum(item => item.Total) + ordem.ValorMaoObra - ordem.Desconto),
                UltimaOcorrencia = lista.Count == 0 ? null : lista.Max(ordem => ordem.DataAbertura),
                EmGarantia = emGarantia || lista.Any(ordem => ordem.GarantiaValidaAte.HasValue && ordem.GarantiaValidaAte.Value.Date >= DateTime.Today)
            };
        }

        private static string NormalizarDefeito(OrdemServico ordem)
        {
            var texto = PrimeiroValor(ordem.ProblemaRelatado, ordem.Diagnostico, ordem.DiagnosticoInicial, ordem.DiagnosticoFinal);
            if (string.IsNullOrWhiteSpace(texto))
            {
                return string.Empty;
            }

            texto = CadastroValidationHelper.NormalizarTextoComparacao(texto);
            return texto.Length <= 80 ? texto : texto[..80];
        }

        private static int ObterTempoResolucaoMinutos(OrdemServico ordem)
        {
            if (ordem.TempoRealMinutos > 0)
            {
                return ordem.TempoRealMinutos;
            }

            if (ordem.DataConclusao.HasValue)
            {
                return Math.Max(0, (int)(ordem.DataConclusao.Value - ordem.DataAbertura).TotalMinutes);
            }

            if (ordem.DataEntrega.HasValue)
            {
                return Math.Max(0, (int)(ordem.DataEntrega.Value - ordem.DataAbertura).TotalMinutes);
            }

            return 0;
        }

        private static string PrimeiroValor(params string?[] valores)
        {
            return valores.FirstOrDefault(valor => !string.IsNullOrWhiteSpace(valor))?.Trim() ?? string.Empty;
        }

        private static List<string> SepararLista(string? valor)
        {
            return (valor ?? string.Empty)
                .Split(new[] { ';', '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToList();
        }

        private static bool ValorAparentaBaixo(string valor, decimal minimo)
        {
            return TryExtrairDecimal(valor, out var numero) && numero > 0 && numero < minimo;
        }

        private static bool ValorAparentaAlto(string valor, decimal maximo)
        {
            return TryExtrairDecimal(valor, out var numero) && numero > maximo;
        }

        private static bool TryExtrairDecimal(string? valor, out decimal numero)
        {
            numero = 0m;
            if (string.IsNullOrWhiteSpace(valor))
            {
                return false;
            }

            var normalizado = new string(valor
                .Where(c => char.IsDigit(c) || c is ',' or '.' or '-')
                .ToArray())
                .Replace(',', '.');

            return decimal.TryParse(normalizado, NumberStyles.Number, CultureInfo.InvariantCulture, out numero);
        }
    }
}
