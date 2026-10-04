using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimoAutoEletrica.Models.Circuitos
{
    /// <summary>
    /// Grafo de circuito elétrico automotivo vetorial com suporte a algoritmo BFS
    /// para identificação instantânea de fios contíguos (Trace Wire) e medições de bancada.
    /// </summary>
    public sealed class CircuitGraph
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string SystemCategory { get; set; } = "Geral";
        public string ApplicableVehicles { get; set; } = "Universal";
        public string TechnicalTip { get; set; } = string.Empty;

        public Dictionary<string, CircuitNode> Nodes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<CircuitWire> Wires { get; } = new();
        public List<CircuitProbePoint> Probes { get; } = new();

        private Dictionary<string, List<CircuitWire>> _nodeAdjacency = new(StringComparer.OrdinalIgnoreCase);
        private bool _isAdjacencyBuilt;

        public void AddNode(CircuitNode node)
        {
            Nodes[node.Id] = node;
            _isAdjacencyBuilt = false;
        }

        public void AddWire(CircuitWire wire)
        {
            Wires.Add(wire);
            _isAdjacencyBuilt = false;
        }

        public void AddProbe(CircuitProbePoint probe)
        {
            Probes.Add(probe);
        }

        public void RebuildAdjacency()
        {
            _nodeAdjacency.Clear();

            foreach (var node in Nodes.Values)
            {
                _nodeAdjacency[node.Id] = new List<CircuitWire>();
            }

            foreach (var wire in Wires)
            {
                if (!_nodeAdjacency.TryGetValue(wire.FromNodeId, out var fromList))
                {
                    fromList = new List<CircuitWire>();
                    _nodeAdjacency[wire.FromNodeId] = fromList;
                }
                fromList.Add(wire);

                if (!_nodeAdjacency.TryGetValue(wire.ToNodeId, out var toList))
                {
                    toList = new List<CircuitWire>();
                    _nodeAdjacency[wire.ToNodeId] = toList;
                }
                toList.Add(wire);
            }

            _isAdjacencyBuilt = true;
        }

        /// <summary>
        /// Executa Busca em Largura (BFS) sobre o grafo elétrico a partir de um fio específico,
        /// descobrindo todos os segmentos da mesma malha elétrica contígua (mesma Linha DIN ou continuidade).
        /// Executa em tempo submilissegundo sem impacto na UI thread.
        /// </summary>
        public (HashSet<string> HighlightedWireIds, HashSet<string> HighlightedNodeIds) TraceWireBfs(string wireId)
        {
            if (!_isAdjacencyBuilt)
            {
                RebuildAdjacency();
            }

            var wire = Wires.FirstOrDefault(w => string.Equals(w.Id, wireId, StringComparison.OrdinalIgnoreCase));
            if (wire == null)
            {
                return (new HashSet<string>(), new HashSet<string>());
            }

            var visitedWires = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { wire.Id };
            var visitedNodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var queue = new Queue<string>();
            queue.Enqueue(wire.FromNodeId);
            queue.Enqueue(wire.ToNodeId);

            visitedNodes.Add(wire.FromNodeId);
            visitedNodes.Add(wire.ToNodeId);

            string lineType = wire.CircuitLineType;

            while (queue.Count > 0)
            {
                var currentNodeId = queue.Dequeue();

                if (_nodeAdjacency.TryGetValue(currentNodeId, out var incidentWires))
                {
                    foreach (var incidentWire in incidentWires)
                    {
                        // Continuidade na mesma linha elétrica (ex: toda Linha 30 conectada através de fusível ou emenda)
                        bool sameLine = string.Equals(incidentWire.CircuitLineType, lineType, StringComparison.OrdinalIgnoreCase);

                        if (sameLine && visitedWires.Add(incidentWire.Id))
                        {
                            var otherNodeId = string.Equals(incidentWire.FromNodeId, currentNodeId, StringComparison.OrdinalIgnoreCase)
                                ? incidentWire.ToNodeId
                                : incidentWire.FromNodeId;

                            if (visitedNodes.Add(otherNodeId))
                            {
                                queue.Enqueue(otherNodeId);
                            }
                        }
                    }
                }
            }

            return (visitedWires, visitedNodes);
        }

        /// <summary>
        /// Localiza todos os fios que pertencem a uma linha específica (ex: "Linha 30", "Linha 15", "Linha 87")
        /// </summary>
        public HashSet<string> ObterFiosPorLinha(string tipoLinha)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var wire in Wires)
            {
                if (wire.CircuitLineType.Contains(tipoLinha, StringComparison.OrdinalIgnoreCase))
                {
                    set.Add(wire.Id);
                }
            }
            return set;
        }

        // =========================================================================
        // BANCO DE CIRCUITOS INTERATIVOS OFICIAIS DA OFICINA PRIMOX
        // =========================================================================

        public static CircuitGraph CriarCircuitoArrefecimento()
        {
            var g = new CircuitGraph
            {
                Id = "CIRCUITO_ARREFECIMENTO",
                Title = "Circuito do Eletroventilador & Arrefecimento (Fiat Fire / Universal)",
                SystemCategory = "Arrefecimento do Motor",
                ApplicableVehicles = "Fiat Palio, Uno Fire, Punto, Grand Siena e Veículos Linha Leve",
                TechnicalTip = "Desconecte o plugue do sensor ECT no motor: por segurança fail-safe, a ECU deve ligar a ventoinha em velocidade máxima imediatamente!"
            };

            // Nós
            g.AddNode(new CircuitNode { Id = "N_BAT", Label = "BATERIA 12V", ComponentName = "Bateria Chumbo-Ácido", NodeType = CircuitNodeType.PowerSource, X = 40, Y = 80, Width = 110, Height = 80, TerminalCode = "30" });
            g.AddNode(new CircuitNode { Id = "N_FUSE", Label = "FUSÍVEL F01 (40A)", ComponentName = "Maxi-Fuse 40A", NodeType = CircuitNodeType.Fuse, X = 240, Y = 95, Width = 100, Height = 50, TerminalCode = "30" });
            g.AddNode(new CircuitNode { Id = "N_RELE", Label = "RELÉ R04\n30: Bat | 87: Motor\n85: ECU | 86: Chave", ComponentName = "Relé Auxiliar 40A 4 Pinos", NodeType = CircuitNodeType.RelayTerminal, X = 420, Y = 70, Width = 130, Height = 110, TerminalCode = "DIN" });
            g.AddNode(new CircuitNode { Id = "N_MOTOR", Label = "VENTOINHA\nMotor DC", ComponentName = "Eletroventilador Radiador", NodeType = CircuitNodeType.Actuator, X = 620, Y = 80, Width = 80, Height = 80, TerminalCode = "87" });
            g.AddNode(new CircuitNode { Id = "N_GND_VENT", Label = "TERRA CHASSI", ComponentName = "Aterramento Longarina Dianteira", NodeType = CircuitNodeType.Ground, X = 640, Y = 230, Width = 50, Height = 30, TerminalCode = "31" });
            g.AddNode(new CircuitNode { Id = "N_ECU", Label = "CENTRAL ECU\nSinal Negativo (GND)", ComponentName = "Módulo de Injeção Eletrônica", NodeType = CircuitNodeType.ModuleEcu, X = 430, Y = 270, Width = 110, Height = 60, TerminalCode = "85" });

            // Fios
            g.AddWire(new CircuitWire { Id = "W_BAT_FUSE", Label = "Alimentação Bateria ➔ Fusível F01", FromNodeId = "N_BAT", ToNodeId = "N_FUSE", WireColor = "Vermelho", WireGauge = "4.0 mm²", CircuitLineType = "Linha 30", FuseProtection = "F01 40A", NeonHighlightColorHex = "#FF1493", X1 = 150, Y1 = 120, X2 = 240, Y2 = 120, Thickness = 4.0 });
            g.AddWire(new CircuitWire { Id = "W_FUSE_RELE", Label = "Alimentação Fusível F01 ➔ Relé Pino 30", FromNodeId = "N_FUSE", ToNodeId = "N_RELE", WireColor = "Vermelho", WireGauge = "4.0 mm²", CircuitLineType = "Linha 30", FuseProtection = "F01 40A", NeonHighlightColorHex = "#FF1493", X1 = 340, Y1 = 120, X2 = 420, Y2 = 120, Thickness = 4.0 });
            g.AddWire(new CircuitWire { Id = "W_RELE_MOTOR", Label = "Potência Relé Pino 87 ➔ Motor Ventoinha", FromNodeId = "N_RELE", ToNodeId = "N_MOTOR", WireColor = "Vermelho/Preto", WireGauge = "2.5 mm²", CircuitLineType = "Linha 87", FuseProtection = "F01 40A", NeonHighlightColorHex = "#00F5FF", X1 = 550, Y1 = 120, X2 = 620, Y2 = 120, Thickness = 3.5 });
            g.AddWire(new CircuitWire { Id = "W_MOTOR_GND", Label = "Retorno Terra Ventoinha ➔ Longarina", FromNodeId = "N_MOTOR", ToNodeId = "N_GND_VENT", WireColor = "Marrom/Preto", WireGauge = "2.5 mm²", CircuitLineType = "Linha 31", NeonHighlightColorHex = "#E0E6ED", X1 = 660, Y1 = 160, X2 = 660, Y2 = 230, Thickness = 3.0 });
            g.AddWire(new CircuitWire { Id = "W_ECU_RELE", Label = "Comando Bobina ECU ➔ Relé Pino 85", FromNodeId = "N_ECU", ToNodeId = "N_RELE", WireColor = "Azul/Branco", WireGauge = "0.75 mm²", CircuitLineType = "Linha 85", NeonHighlightColorHex = "#BA55D3", X1 = 485, Y1 = 270, X2 = 485, Y2 = 180, Thickness = 2.0, IsDashed = true });

            // Pontos de Teste (Probes)
            g.AddProbe(new CircuitProbePoint
            {
                Id = "A",
                Title = "Pólo Positivo da Bateria",
                TargetComponent = "Bateria Chumbo-Ácido",
                X = 155,
                Y = 120,
                MultimeterScale = "20V DC",
                KeyOffVoltage = "12.4V a 12.7V",
                KeyOnVoltage = "12.2V a 12.5V",
                EngineRunningVoltage = "13.8V a 14.4V",
                DiagnosticInstructions = "Meça entre o terminal positivo e a carcaça de terra.",
                FaultConditionInterpretation = "Se menor que 12.0V, bateria descarregada ou elemento sulfatado."
            });

            g.AddProbe(new CircuitProbePoint
            {
                Id = "B",
                Title = "Saída do Maxi-Fuse 40A",
                TargetComponent = "Fusível Geral F01",
                X = 345,
                Y = 120,
                MultimeterScale = "20V DC",
                KeyOffVoltage = "12.4V a 12.7V",
                KeyOnVoltage = "12.2V a 12.5V",
                EngineRunningVoltage = "13.8V a 14.4V",
                DiagnosticInstructions = "Conecte a ponta vermelha no pino de saída do fusível e ponta preta ao chassi.",
                FaultConditionInterpretation = "Se der 0V com 12V na entrada, o fusível de alta corrente está rompido."
            });

            g.AddProbe(new CircuitProbePoint
            {
                Id = "C",
                Title = "Pino 30 do Relé de Potência",
                TargetComponent = "Terminal 30 do Relé R04",
                X = 420,
                Y = 90,
                MultimeterScale = "20V DC",
                KeyOffVoltage = "12.4V a 12.7V",
                KeyOnVoltage = "12.2V a 12.5V",
                EngineRunningVoltage = "13.8V a 14.4V",
                DiagnosticInstructions = "Verifique a presença de +12V permanentes no soquete do relé.",
                FaultConditionInterpretation = "Se 0V, terminal oxidado ou chicote aberto entre caixa de fusíveis e relé."
            });

            g.AddProbe(new CircuitProbePoint
            {
                Id = "D",
                Title = "Pino 87 do Relé (Saída para Ventoinha)",
                TargetComponent = "Terminal 87 do Relé R04",
                X = 555,
                Y = 120,
                MultimeterScale = "20V DC",
                KeyOffVoltage = "0.0V",
                KeyOnVoltage = "0.0V (12V quando acionado)",
                EngineRunningVoltage = "13.8V a 14.2V (com ventoinha ligada)",
                DiagnosticInstructions = "Faça ponte com jumper entre terminais 30 e 87 com o relé sacado.",
                FaultConditionInterpretation = "Se a ponte acionar o ventilador mas o relé não ligar na temperatura de disparo, o relé ou o comando da ECU falhou."
            });

            g.AddProbe(new CircuitProbePoint
            {
                Id = "E",
                Title = "Pino 85 (Comando Negativo da ECU)",
                TargetComponent = "Terminal 85 do Relé R04 / Saída Driver ECU",
                X = 485,
                Y = 230,
                MultimeterScale = "20V DC / Frequencímetro",
                KeyOffVoltage = "12.0V (Flutuante)",
                KeyOnVoltage = "12.0V em repouso / 0.2V a 0.5V quando acionado",
                EngineRunningVoltage = "0.2V (Sinal aterrado pela ECU para armar)",
                OscilloscopeWaveform = "Degrau de 12V descendo para 0V ao atingir 97°C (ou sinal PWM 100Hz em carros modernos)",
                OscilloscopeTimebase = "50 ms/div",
                OscilloscopeVoltageScale = "2V / div",
                DiagnosticInstructions = "Monitore a tensão no pino 85 enquanto o scanner indica temperatura subindo.",
                FaultConditionInterpretation = "Se o scanner marcar 100°C e o pino 85 continuar em 12V, o driver interno da ECU está aberto ou o chicote está partido."
            });

            g.RebuildAdjacency();
            return g;
        }

        public static CircuitGraph CriarCircuitoPartidaLinha50()
        {
            var g = new CircuitGraph
            {
                Id = "CIRCUITO_PARTIDA",
                Title = "Motor de Partida, Solenóide & Linha 50 (Diagnóstico Tec-Tec)",
                SystemCategory = "Sistema de Partida & Carga",
                ApplicableVehicles = "Universal (Veículos com motor de arranque com solenóide bendix)",
                TechnicalTip = "O som 'tec-tec' prova que o sinal Linha 50 chegou ao solenóide! O defeito geralmente é queda de tensão de aterramento ou escovas do induzido gastas."
            };

            g.AddNode(new CircuitNode { Id = "N_BAT", Label = "BATERIA 12V\nCabo 25mm²", ComponentName = "Bateria de Arranque", NodeType = CircuitNodeType.PowerSource, X = 40, Y = 80, Width = 100, Height = 80, TerminalCode = "30" });
            g.AddNode(new CircuitNode { Id = "N_COMUTADOR", Label = "COMUTADOR\nLinha 50 (Chave)", ComponentName = "Comutador Miolo Ignição", NodeType = CircuitNodeType.Switch, X = 180, Y = 220, Width = 120, Height = 60, TerminalCode = "50" });
            g.AddNode(new CircuitNode { Id = "N_SOLENOIDE", Label = "AUTOMÁTICO\n• Borne 30 (Bat)\n• Borne 50 (Sinal)\n• Borne Saída (M)", ComponentName = "Solenóide / Automático", NodeType = CircuitNodeType.Actuator, X = 480, Y = 100, Width = 130, Height = 170, TerminalCode = "30/50" });
            g.AddNode(new CircuitNode { Id = "N_MOTOR_ARRANQUE", Label = "MOTOR DE\nPARTIDA\nInduzido e\nEscovas", ComponentName = "Motor de Partida DC", NodeType = CircuitNodeType.Actuator, X = 630, Y = 110, Width = 80, Height = 100, TerminalCode = "M" });
            g.AddNode(new CircuitNode { Id = "N_GND_MOTOR", Label = "TERRA BLOCO", ComponentName = "Malha de Aterramento do Motor", NodeType = CircuitNodeType.Ground, X = 670, Y = 280, Width = 50, Height = 30, TerminalCode = "31" });

            g.AddWire(new CircuitWire { Id = "W_BAT_SOLENOIDE", Label = "Cabo Principal Linha 30 (25mm²)", FromNodeId = "N_BAT", ToNodeId = "N_SOLENOIDE", WireColor = "Vermelho", WireGauge = "25.0 mm²", CircuitLineType = "Linha 30", NeonHighlightColorHex = "#FF1493", X1 = 140, Y1 = 120, X2 = 480, Y2 = 120, Thickness = 5.0 });
            g.AddWire(new CircuitWire { Id = "W_COMUT_SOLENOIDE", Label = "Sinal de Partida Linha 50", FromNodeId = "N_COMUTADOR", ToNodeId = "N_SOLENOIDE", WireColor = "Amarelo/Vermelho", WireGauge = "2.5 mm²", CircuitLineType = "Linha 50", NeonHighlightColorHex = "#FF8C00", X1 = 300, Y1 = 250, X2 = 480, Y2 = 250, Thickness = 3.0 });
            g.AddWire(new CircuitWire { Id = "W_SOLENOIDE_MOTOR", Label = "Ponte de Cobre Interna do Automático", FromNodeId = "N_SOLENOIDE", ToNodeId = "N_MOTOR_ARRANQUE", WireColor = "Cobre Nu", WireGauge = "25.0 mm²", CircuitLineType = "Linha 87", NeonHighlightColorHex = "#00F5FF", X1 = 610, Y1 = 160, X2 = 630, Y2 = 160, Thickness = 4.0 });
            g.AddWire(new CircuitWire { Id = "W_MOTOR_TERRA", Label = "Aterramento Carcaça ➔ Chassi (Linha 31)", FromNodeId = "N_MOTOR_ARRANQUE", ToNodeId = "N_GND_MOTOR", WireColor = "Cobre/Preto", WireGauge = "25.0 mm²", CircuitLineType = "Linha 31", NeonHighlightColorHex = "#E0E6ED", X1 = 670, Y1 = 210, X2 = 670, Y2 = 280, Thickness = 4.0 });

            g.AddProbe(new CircuitProbePoint
            {
                Id = "A",
                Title = "Bateria durante a Partida",
                TargetComponent = "Polos da Bateria",
                X = 145,
                Y = 120,
                MultimeterScale = "20V DC / Min-Max",
                KeyOffVoltage = "12.5V a 12.7V",
                KeyOnVoltage = "12.3V",
                CrankingVoltage = ">= 9.6V (Tensão Crítica Mínima)",
                DiagnosticInstructions = "Gire o motor de arranque e leia a tensão mínima registrada.",
                FaultConditionInterpretation = "Se a tensão cair abaixo de 9.6V (ex: 7.5V), a bateria está sem carga ou o motor de arranque está travado mecanicamente."
            });

            g.AddProbe(new CircuitProbePoint
            {
                Id = "B",
                Title = "Sinal Linha 50 no Comutador",
                TargetComponent = "Borne 50 do Comutador de Ignição",
                X = 305,
                Y = 250,
                MultimeterScale = "20V DC",
                KeyOffVoltage = "0.0V",
                KeyOnVoltage = "0.0V",
                CrankingVoltage = "11.5V a 12.0V",
                DiagnosticInstructions = "Meça no conector traseiro do miolo de ignição com a chave virada até o fim de curso.",
                FaultConditionInterpretation = "Se der 0V, o contato metálico do comutador de partida está desgastado."
            });

            g.AddProbe(new CircuitProbePoint
            {
                Id = "C",
                Title = "Terminal 50 no Solenóide",
                TargetComponent = "Borne 50 do Automático de Partida",
                X = 480,
                Y = 250,
                MultimeterScale = "20V DC",
                KeyOffVoltage = "0.0V",
                KeyOnVoltage = "0.0V",
                CrankingVoltage = ">= 10.5V",
                DiagnosticInstructions = "Meça no terminal parafusado do solenóide durante o giro.",
                FaultConditionInterpretation = "Se indicar 12V e ouvir o 'tec-tec' sem o motor girar, o contato de cobre interno do automático queimou ou as escovas do induzido estão no limite."
            });

            g.AddProbe(new CircuitProbePoint
            {
                Id = "D",
                Title = "Queda de Tensão no Aterramento (Linha 31)",
                TargetComponent = "Malha de Aterramento do Bloco",
                X = 670,
                Y = 250,
                MultimeterScale = "2000mV DC (2V DC)",
                KeyOffVoltage = "0.00V",
                KeyOnVoltage = "0.02V",
                CrankingVoltage = "<= 0.20V (200mV máximo permitido)",
                DiagnosticInstructions = "Ponta preta no pólo negativo da bateria; ponta vermelha na carcaça metálica do motor de arranque.",
                FaultConditionInterpretation = "Se medir mais de 0.20V durante a partida (ex: 0.8V a 1.5V), a malha de aterramento está frouxa ou oxidada gerando o tec-tec."
            });

            g.RebuildAdjacency();
            return g;
        }

        public static CircuitGraph CriarCircuitoFaroisDIN()
        {
            var g = new CircuitGraph
            {
                Id = "CIRCUITO_FAROIS",
                Title = "Relé Auxiliar de Faróis (Padrão DIN 4 e 5 Pinos // Caso Crônico Farol Aceso)",
                SystemCategory = "Iluminação & Sinalização",
                ApplicableVehicles = "Universal DIN 72552 (Todos os veículos com relés auxiliares de 4 ou 5 pinos)",
                TechnicalTip = "Quando são instaladas lâmpadas de 100W, a corrente atinge 17A no contato. O arco elétrico solda os pinos 30 e 87 do relé, mantendo o farol aceso direto mesmo com a chave desligada!"
            };

            g.AddNode(new CircuitNode { Id = "N_BAT", Label = "BATERIA 12V\nLinha 30", ComponentName = "Bateria", NodeType = CircuitNodeType.PowerSource, X = 40, Y = 80, Width = 100, Height = 70, TerminalCode = "30" });
            g.AddNode(new CircuitNode { Id = "N_FUSE", Label = "FUSÍVEL\n20A", ComponentName = "Fusível Farol 20A", NodeType = CircuitNodeType.Fuse, X = 200, Y = 90, Width = 80, Height = 50, TerminalCode = "30" });
            g.AddNode(new CircuitNode { Id = "N_RELE", Label = "RELÉ DIN 4 PINOS\n• 30: +12V Bat\n• 87: Saída Farol\n• 86: Chave Seta\n• 85: Terra 31", ComponentName = "Relé DIN 40A", NodeType = CircuitNodeType.RelayTerminal, X = 360, Y = 70, Width = 140, Height = 130, TerminalCode = "DIN" });
            g.AddNode(new CircuitNode { Id = "N_CHAVE_SETA", Label = "CHAVE DE SETA\nComando Farol", ComponentName = "Comutador de Farol Alto/Baixo", NodeType = CircuitNodeType.Switch, X = 230, Y = 215, Width = 130, Height = 50, TerminalCode = "86" });
            g.AddNode(new CircuitNode { Id = "N_FAROL_ESQ", Label = "FAROL ESQ.\nH4 / H7", ComponentName = "Lâmpada Farol Esquerdo", NodeType = CircuitNodeType.Actuator, X = 600, Y = 75, Width = 90, Height = 45, TerminalCode = "87" });
            g.AddNode(new CircuitNode { Id = "N_FAROL_DIR", Label = "FAROL DIR.\nH4 / H7", ComponentName = "Lâmpada Farol Direito", NodeType = CircuitNodeType.Actuator, X = 600, Y = 150, Width = 90, Height = 45, TerminalCode = "87" });
            g.AddNode(new CircuitNode { Id = "N_GND_FAROL", Label = "TERRA", ComponentName = "Aterramento Iluminação", NodeType = CircuitNodeType.Ground, X = 710, Y = 95, Width = 40, Height = 30, TerminalCode = "31" });

            g.AddWire(new CircuitWire { Id = "W_BAT_FUSE", Label = "Bateria Linha 30 ➔ Fusível", FromNodeId = "N_BAT", ToNodeId = "N_FUSE", WireColor = "Vermelho", WireGauge = "2.5 mm²", CircuitLineType = "Linha 30", FuseProtection = "20A", NeonHighlightColorHex = "#FF1493", X1 = 140, Y1 = 115, X2 = 200, Y2 = 115, Thickness = 3.0 });
            g.AddWire(new CircuitWire { Id = "W_FUSE_RELE", Label = "Fusível ➔ Relé Pino 30", FromNodeId = "N_FUSE", ToNodeId = "N_RELE", WireColor = "Vermelho", WireGauge = "2.5 mm²", CircuitLineType = "Linha 30", FuseProtection = "20A", NeonHighlightColorHex = "#FF1493", X1 = 280, Y1 = 115, X2 = 360, Y2 = 115, Thickness = 3.0 });
            g.AddWire(new CircuitWire { Id = "W_RELE_FAROIS", Label = "Relé Pino 87 ➔ Faróis", FromNodeId = "N_RELE", ToNodeId = "N_FAROL_ESQ", WireColor = "Amarelo", WireGauge = "2.5 mm²", CircuitLineType = "Linha 87", FuseProtection = "20A", NeonHighlightColorHex = "#00F5FF", X1 = 500, Y1 = 115, X2 = 600, Y2 = 115, Thickness = 3.0 });
            g.AddWire(new CircuitWire { Id = "W_CHAVE_RELE", Label = "Comando Chave de Seta ➔ Relé Pino 86", FromNodeId = "N_CHAVE_SETA", ToNodeId = "N_RELE", WireColor = "Branco", WireGauge = "1.0 mm²", CircuitLineType = "Linha 86", NeonHighlightColorHex = "#BA55D3", X1 = 360, Y1 = 240, X2 = 430, Y2 = 200, Thickness = 2.0 });

            g.AddProbe(new CircuitProbePoint
            {
                Id = "A",
                Title = "Pino 30 do Relé de Farol",
                TargetComponent = "Entrada Linha 30",
                X = 360,
                Y = 115,
                MultimeterScale = "20V DC",
                KeyOffVoltage = "12.5V",
                KeyOnVoltage = "12.3V",
                DiagnosticInstructions = "Meça a presença constante de +12V protegidos.",
                FaultConditionInterpretation = "Se 0V, fusível de 20A rompido."
            });

            g.AddProbe(new CircuitProbePoint
            {
                Id = "B",
                Title = "Pino 87 do Relé (Saída para Faróis)",
                TargetComponent = "Saída Linha 87",
                X = 505,
                Y = 115,
                MultimeterScale = "20V DC",
                KeyOffVoltage = "0.0V (ou 12V em caso de defeito colado)",
                KeyOnVoltage = "12.2V (com farol ligado)",
                DiagnosticInstructions = "Desligue a chave de seta no painel e meça este terminal.",
                FaultConditionInterpretation = "SE INDICAR +12V COM A CHAVE DE SETA DESLIGADA, os contatos 30 e 87 estão magneticamente soldados! Substitua o relé e descarte lâmpadas de 100W."
            });

            g.AddProbe(new CircuitProbePoint
            {
                Id = "C",
                Title = "Pino 86 (Comando da Bobina)",
                TargetComponent = "Entrada Linha 86",
                X = 430,
                Y = 215,
                MultimeterScale = "20V DC",
                KeyOffVoltage = "0.0V",
                KeyOnVoltage = "12.0V (quando farol acionado)",
                DiagnosticInstructions = "Verifique se a chave de seta interrompe a alimentação ao ser desligada.",
                FaultConditionInterpretation = "Se pino 86 vai a 0V e o farol não apaga, o defeito é 100% no relé e NUNCA na chave de seta."
            });

            g.RebuildAdjacency();
            return g;
        }

        public static CircuitGraph CriarCircuitoRedeCanObd2()
        {
            var g = new CircuitGraph
            {
                Id = "CIRCUITO_CAN_OBD2",
                Title = "Rede CAN & Conector de Diagnóstico OBD-II (Resistência de Terminação 60Ω)",
                SystemCategory = "Redes de Comunicação & Telemetria",
                ApplicableVehicles = "SAE J1962 / Padrão Universal CAN-BUS Automotivo",
                TechnicalTip = "Com a bateria desconectada, meça resistência entre pinos 6 e 14 no conector OBD: o valor exato deve ser 60 Ohms! Se medir 120 Ohms, um dos módulos de terminação está desconectado."
            };

            g.AddNode(new CircuitNode { Id = "N_OBD", Label = "TOMADA OBD-II\n• P4: Chassi\n• P5: Signal GND\n• P6: CAN-H\n• P14: CAN-L\n• P16: +12V", ComponentName = "Conector Diagnóstico SAE J1962", NodeType = CircuitNodeType.ConnectorTerminal, X = 40, Y = 80, Width = 130, Height = 150, TerminalCode = "OBD" });
            g.AddNode(new CircuitNode { Id = "N_ECU", Label = "MÓDULO ECU\nResistor 120Ω\nde Terminação", ComponentName = "Módulo de Injeção Eletrônica", NodeType = CircuitNodeType.ModuleEcu, X = 480, Y = 80, Width = 120, Height = 100, TerminalCode = "ECU" });
            g.AddNode(new CircuitNode { Id = "N_PAINEL", Label = "PAINEL (CLUSTER)\nResistor 120Ω\nde Terminação", ComponentName = "Quadro de Instrumentos", NodeType = CircuitNodeType.ModuleEcu, X = 480, Y = 240, Width = 120, Height = 80, TerminalCode = "IC" });

            g.AddWire(new CircuitWire { Id = "W_CAN_H", Label = "Barramento CAN-High", FromNodeId = "N_OBD", ToNodeId = "N_ECU", WireColor = "Azul", WireGauge = "0.5 mm² Trançado", CircuitLineType = "CAN-H", NeonHighlightColorHex = "#00BFFF", X1 = 170, Y1 = 110, X2 = 480, Y2 = 110, Thickness = 3.0 });
            g.AddWire(new CircuitWire { Id = "W_CAN_L", Label = "Barramento CAN-Low", FromNodeId = "N_OBD", ToNodeId = "N_ECU", WireColor = "Verde", WireGauge = "0.5 mm² Trançado", CircuitLineType = "CAN-L", NeonHighlightColorHex = "#00FF7F", X1 = 170, Y1 = 150, X2 = 480, Y2 = 150, Thickness = 3.0 });
            g.AddWire(new CircuitWire { Id = "W_CAN_RAMAL", Label = "Ramal CAN Painel", FromNodeId = "N_ECU", ToNodeId = "N_PAINEL", WireColor = "Azul/Verde Trançado", WireGauge = "0.5 mm²", CircuitLineType = "CAN-BUS", NeonHighlightColorHex = "#00BFFF", X1 = 540, Y1 = 180, X2 = 540, Y2 = 240, Thickness = 2.5 });

            g.AddProbe(new CircuitProbePoint
            {
                Id = "A",
                Title = "Pino 6 - Linha CAN-High",
                TargetComponent = "Conector OBD-II Pino 6",
                X = 175,
                Y = 110,
                MultimeterScale = "20V DC / Osciloscópio",
                KeyOffVoltage = "0.0V",
                KeyOnVoltage = "2.5V (Repouso) a 3.5V (Transmitindo)",
                OscilloscopeWaveform = "Onda quadrada diferencial 500 kbps, sinal dominante sobe para 3.5V",
                OscilloscopeTimebase = "2 µs/div",
                OscilloscopeVoltageScale = "1V / div",
                DiagnosticInstructions = "Meça o sinal em relação ao pino 4/5 (terra).",
                FaultConditionInterpretation = "Se travado em 0V ou 12V, a linha está em curto com massa ou positivo."
            });

            g.AddProbe(new CircuitProbePoint
            {
                Id = "B",
                Title = "Pino 14 - Linha CAN-Low",
                TargetComponent = "Conector OBD-II Pino 14",
                X = 175,
                Y = 150,
                MultimeterScale = "20V DC / Osciloscópio",
                KeyOffVoltage = "0.0V",
                KeyOnVoltage = "2.5V (Repouso) a 1.5V (Transmitindo)",
                OscilloscopeWaveform = "Onda quadrada espelho da CAN-High, sinal dominante desce para 1.5V",
                OscilloscopeTimebase = "2 µs/div",
                OscilloscopeVoltageScale = "1V / div",
                DiagnosticInstructions = "Verifique o espelhamento perfeito com a CAN-High no osciloscópio de 2 canais.",
                FaultConditionInterpretation = "Se a soma CAN-H + CAN-L não for ~5.0V constante, há ruído elétrico na rede."
            });

            g.AddProbe(new CircuitProbePoint
            {
                Id = "C",
                Title = "Resistência de Terminação (Pino 6 ao 14)",
                TargetComponent = "Barramento Completo",
                X = 540,
                Y = 130,
                MultimeterScale = "200 Ohms (Bateria Desconectada)",
                KeyOffVoltage = "60.0 Ohms exatos (±3Ω)",
                DiagnosticInstructions = "DESCONECTE O POLO DA BATERIA antes de medir resistência. Conecte as pontas entre pino 6 e 14.",
                FaultConditionInterpretation = "Se medir 120 Ohms, um dos módulos terminadores (ECU ou Painel) está desconectado. Se medir 0 Ohms, os fios CAN-H e CAN-L estão em curto."
            });

            g.RebuildAdjacency();
            return g;
        }

        public static CircuitGraph CriarCircuitoFugaParasita()
        {
            var g = new CircuitGraph
            {
                Id = "CIRCUITO_FUGA_PARASITA",
                Title = "Teste de Consumo Parasita da Bateria (Multímetro em Série no Terra)",
                SystemCategory = "Diagnóstico de Bateria & Drenagem",
                ApplicableVehicles = "Universal (Veículos onde a bateria descarrega durante a noite)",
                TechnicalTip = "Multimídias não originais com fio amarelo (Bateria direta) invertido com o vermelho (Pós-chave) são responsáveis por 70% das fugas de carga!"
            };

            g.AddNode(new CircuitNode { Id = "N_BAT", Label = "BATERIA 12V\n(Negativo Desconectado)", ComponentName = "Bateria 12V", NodeType = CircuitNodeType.PowerSource, X = 40, Y = 100, Width = 110, Height = 90, TerminalCode = "30" });
            g.AddNode(new CircuitNode { Id = "N_MULTI", Label = "MULTÍMETRO\nEscala: 10A DC\nEm Série no Terra", ComponentName = "Multímetro Automotivo", NodeType = CircuitNodeType.ConnectorTerminal, X = 230, Y = 95, Width = 130, Height = 100, TerminalCode = "A" });
            g.AddNode(new CircuitNode { Id = "N_CENTRAL_FUSE", Label = "CENTRAL FUSÍVEIS\n(BCM / Módulos)\n• F01 Rádio | F02 Alarme", ComponentName = "Caixa de Fusíveis Principal", NodeType = CircuitNodeType.Fuse, X = 460, Y = 85, Width = 140, Height = 120, TerminalCode = "FUSE" });
            g.AddNode(new CircuitNode { Id = "N_GND_CHASSI", Label = "CHASSI VEÍCULO", ComponentName = "Aterramento Carroceria", NodeType = CircuitNodeType.Ground, X = 460, Y = 240, Width = 50, Height = 30, TerminalCode = "31" });

            g.AddWire(new CircuitWire { Id = "W_BAT_MULTI", Label = "Pólo (-) Bateria ➔ Ponta Vermelha Multímetro", FromNodeId = "N_BAT", ToNodeId = "N_MULTI", WireColor = "Preto/Vermelho", WireGauge = "4.0 mm²", CircuitLineType = "Linha 31", NeonHighlightColorHex = "#39FF14", X1 = 150, Y1 = 145, X2 = 230, Y2 = 145, Thickness = 3.5 });
            g.AddWire(new CircuitWire { Id = "W_MULTI_FUSE", Label = "Ponta Preta Multímetro ➔ Cabo Chassi", FromNodeId = "N_MULTI", ToNodeId = "N_CENTRAL_FUSE", WireColor = "Preto", WireGauge = "4.0 mm²", CircuitLineType = "Linha 31", NeonHighlightColorHex = "#39FF14", X1 = 360, Y1 = 145, X2 = 460, Y2 = 145, Thickness = 3.5 });
            g.AddWire(new CircuitWire { Id = "W_FUSE_GND", Label = "Malha de Terra Central ➔ Chassi", FromNodeId = "N_CENTRAL_FUSE", ToNodeId = "N_GND_CHASSI", WireColor = "Marrom", WireGauge = "4.0 mm²", CircuitLineType = "Linha 31", NeonHighlightColorHex = "#E0E6ED", X1 = 460, Y1 = 205, X2 = 460, Y2 = 240, Thickness = 3.0 });

            g.AddProbe(new CircuitProbePoint
            {
                Id = "A",
                Title = "Medição no Multímetro (Corrente Parasita)",
                TargetComponent = "Multímetro em Série",
                X = 365,
                Y = 145,
                MultimeterScale = "10A DC / 200mA DC",
                KeyOffVoltage = "<= 0.050A (50mA máximo em Sleep Mode)",
                KeyOnVoltage = "N/A (Não ligar a chave com multímetro na escala de corrente!)",
                EngineRunningVoltage = "N/A",
                DiagnosticInstructions = "Aguarde 20 minutos com capô travado para os módulos entrarem em repouso (Sleep Mode).",
                FaultConditionInterpretation = "Se marcar mais de 0.050A (ex: 0.35A a 0.80A), há fuga de corrente drenando a bateria durante a noite."
            });

            g.AddProbe(new CircuitProbePoint
            {
                Id = "B",
                Title = "Procedimento de Isolamento Fusível por Fusível",
                TargetComponent = "Caixa de Fusíveis",
                X = 605,
                Y = 145,
                MultimeterScale = "10A DC",
                KeyOffVoltage = "Observar queda brusca de corrente",
                DiagnosticInstructions = "Remova um fusível por vez na central. Quando a corrente cair de 350mA para 30mA, aquele fusível alimenta o circuito com defeito.",
                FaultConditionInterpretation = "Comum em módulos de alarme instalados incorretamente, amplificadores de som ou multimídias com chicote pós-chave invertido."
            });

            g.RebuildAdjacency();
            return g;
        }

        public static CircuitGraph CriarCircuitoChicoteTraseiro()
        {
            var g = new CircuitGraph
            {
                Id = "CIRCUITO_CHICOTE_TRASEIRO",
                Title = "Chicote da Tampa Traseira & Lanternas (Falha Crônica de Fadiga por Flexão)",
                SystemCategory = "Chicote & Carroceria",
                ApplicableVehicles = "Hatchbacks, SUVs e Sedans (HB20, Gol, Onix, Fox, Sandero)",
                TechnicalTip = "Não solde o fio exatamente na dobra da sanfona (a solda é rígida e quebra rápido). Substitua o trecho inteiro por fio com isolação em silicone super flexível!"
            };

            g.AddNode(new CircuitNode { Id = "N_CHICOTE_PRINCIPAL", Label = "CHICOTE PRINCIPAL\n(Carroceria)\n• 12V Lanterna | Placa", ComponentName = "Chicote da Coluna C", NodeType = CircuitNodeType.ConnectorTerminal, X = 40, Y = 90, Width = 120, Height = 110, TerminalCode = "CARROCERIA" });
            g.AddNode(new CircuitNode { Id = "N_BORRACHA_SANFONA", Label = "BORRACHA SANFONADA\n(Articulação da Tampa)\n⚠️ Ponto Crítico de Fadiga!", ComponentName = "Passagem Articulada da Mala", NodeType = CircuitNodeType.ConnectorTerminal, X = 260, Y = 80, Width = 150, Height = 130, TerminalCode = "FLEXAO" });
            g.AddNode(new CircuitNode { Id = "N_TAMPA_TRASEIRA", Label = "TAMPA TRASEIRA\n• Luz de Placa\n• Luz de Freio / Break\n• Trava Mala", ComponentName = "Lanternas da Tampa", NodeType = CircuitNodeType.Actuator, X = 500, Y = 85, Width = 150, Height = 120, TerminalCode = "TAMPA" });

            g.AddWire(new CircuitWire { Id = "W_LANTERNA_IN", Label = "Linha 58 Lanterna Esquerda", FromNodeId = "N_CHICOTE_PRINCIPAL", ToNodeId = "N_BORRACHA_SANFONA", WireColor = "Cinza/Vermelho", WireGauge = "1.0 mm²", CircuitLineType = "Linha 58", NeonHighlightColorHex = "#FF1493", X1 = 160, Y1 = 120, X2 = 260, Y2 = 120, Thickness = 3.0 });
            g.AddWire(new CircuitWire { Id = "W_PLACA_IN", Label = "Alimentação Luz de Placa", FromNodeId = "N_CHICOTE_PRINCIPAL", ToNodeId = "N_BORRACHA_SANFONA", WireColor = "Cinza/Preto", WireGauge = "0.75 mm²", CircuitLineType = "Linha 58", NeonHighlightColorHex = "#FFAA00", X1 = 160, Y1 = 145, X2 = 260, Y2 = 145, Thickness = 2.5 });
            g.AddWire(new CircuitWire { Id = "W_TERRA_IN", Label = "Linha 31 Terra da Tampa", FromNodeId = "N_CHICOTE_PRINCIPAL", ToNodeId = "N_BORRACHA_SANFONA", WireColor = "Marrom", WireGauge = "1.5 mm²", CircuitLineType = "Linha 31", NeonHighlightColorHex = "#E0E6ED", X1 = 160, Y1 = 170, X2 = 260, Y2 = 170, Thickness = 3.0 });

            g.AddWire(new CircuitWire { Id = "W_LANTERNA_OUT", Label = "Lanterna para Soquetes", FromNodeId = "N_BORRACHA_SANFONA", ToNodeId = "N_TAMPA_TRASEIRA", WireColor = "Cinza/Vermelho", WireGauge = "1.0 mm²", CircuitLineType = "Linha 58", NeonHighlightColorHex = "#FF1493", X1 = 410, Y1 = 120, X2 = 500, Y2 = 120, Thickness = 3.0 });
            g.AddWire(new CircuitWire { Id = "W_PLACA_OUT", Label = "Luz de Placa para Soquetes", FromNodeId = "N_BORRACHA_SANFONA", ToNodeId = "N_TAMPA_TRASEIRA", WireColor = "Cinza/Preto", WireGauge = "0.75 mm²", CircuitLineType = "Linha 58", NeonHighlightColorHex = "#FFAA00", X1 = 410, Y1 = 145, X2 = 500, Y2 = 145, Thickness = 2.5 });
            g.AddWire(new CircuitWire { Id = "W_TERRA_OUT", Label = "Terra para Soquetes", FromNodeId = "N_BORRACHA_SANFONA", ToNodeId = "N_TAMPA_TRASEIRA", WireColor = "Marrom", WireGauge = "1.5 mm²", CircuitLineType = "Linha 31", NeonHighlightColorHex = "#E0E6ED", X1 = 410, Y1 = 170, X2 = 500, Y2 = 170, Thickness = 3.0 });

            g.AddProbe(new CircuitProbePoint
            {
                Id = "A",
                Title = "Fios na Borracha Sanfonada",
                TargetComponent = "Feixe Flexível da Articulação",
                X = 335,
                Y = 145,
                MultimeterScale = "Continuidade / 20V DC",
                KeyOffVoltage = "Teste de continuidade sonora",
                KeyOnVoltage = "12.0V com lanterna ligada",
                DiagnosticInstructions = "Puxe suavemente cada fio de cobre dentro da borracha sanfonada com uma pinça.",
                FaultConditionInterpretation = "Fios de cobre partem internamente mantendo a capa de PVC quase intacta (fio elástico/rompido). Apaga luz de placa e lanternas simultaneamente."
            });

            g.AddProbe(new CircuitProbePoint
            {
                Id = "B",
                Title = "Soquete da Lâmpada (Retorno de Terra)",
                TargetComponent = "Soquetes na Tampa",
                X = 655,
                Y = 145,
                MultimeterScale = "20V DC / Resistência",
                KeyOffVoltage = "0 Ohms para o chassi",
                KeyOnVoltage = "12.0V no positivo / 0.0V no terra",
                DiagnosticInstructions = "Meça se há retorno de terra com a luz de ré ou freio acionada.",
                FaultConditionInterpretation = "Se o terra da tampa estiver rompido, ao pisar no freio a corrente retorna pela lâmpada de ré ou placa, acendendo todas as lâmpadas fracas juntas."
            });

            g.RebuildAdjacency();
            return g;
        }

        public static CircuitGraph ObterPorIndice(int index)
        {
            return index switch
            {
                0 => CriarCircuitoArrefecimento(),
                1 => CriarCircuitoPartidaLinha50(),
                2 => CriarCircuitoFaroisDIN(),
                3 => CriarCircuitoFugaParasita(),
                4 => CriarCircuitoChicoteTraseiro(),
                5 => CriarCircuitoRedeCanObd2(),
                _ => CriarCircuitoArrefecimento()
            };
        }
    }
}
