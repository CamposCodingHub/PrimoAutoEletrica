using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Models.Circuitos
{
    public enum CircuitNodeType
    {
        PowerSource = 1,       // Bateria 12V / Alternador
        Fuse = 2,              // Fusível / Maxi-Fuse
        Switch = 3,            // Comutador de ignição / Botão
        RelayTerminal = 4,     // Terminal de Relé DIN (30, 85, 86, 87, 87a)
        Sensor = 5,            // Sensor de temperatura, rotação, etc.
        Actuator = 6,          // Motor ventoinha, motor de partida, bico injetor, lâmpada
        Ground = 7,            // Ponto de aterramento (Linha 31 / Chassi)
        ConnectorTerminal = 8, // Conector chicote / Pino OBD-II
        ModuleEcu = 9          // Módulo eletrônico (ECU / BCM / Painel)
    }

    /// <summary>
    /// Representa um nó ou componente dentro de um diagrama elétrico automotivo
    /// </summary>
    public sealed class CircuitNode
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string ComponentName { get; set; } = string.Empty;
        public CircuitNodeType NodeType { get; set; } = CircuitNodeType.ConnectorTerminal;
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; } = 90;
        public double Height { get; set; } = 50;
        public string? Description { get; set; }
        public string? TerminalCode { get; set; } // "30", "85", "86", "87", "31", "50", "CAN-H", etc.

        public override string ToString() => $"{ComponentName} [{Id}] ({NodeType})";
    }
}
