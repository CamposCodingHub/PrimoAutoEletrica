using System;

namespace PrimoAutoEletrica.Models.Circuitos
{
    /// <summary>
    /// Representa um segmento de condutor/fio elétrico no circuito com metadados técnicos de oficina
    /// (Bitola, Cor do Fio, Norma DIN e Proteção por Fusível)
    /// </summary>
    public sealed class CircuitWire
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string FromNodeId { get; set; } = string.Empty;
        public string ToNodeId { get; set; } = string.Empty;
        public string WireColor { get; set; } = "Vermelho";
        public string WireGauge { get; set; } = "1.5 mm²";
        public string CircuitLineType { get; set; } = "Linha 30"; // "Linha 30", "Linha 15", "Linha 31", "Linha 87", "Linha 85", "Linha 86", "Linha 50", "CAN-H", "CAN-L"
        public string? FuseProtection { get; set; }
        public string? DefaultColorHex { get; set; }
        public string NeonHighlightColorHex { get; set; } = "#00F5FF";

        // Coordenadas para renderização no Canvas
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
        public double Thickness { get; set; } = 3.0;
        public bool IsDashed { get; set; }

        public string ObterResumoTecnico()
        {
            var fusivel = !string.IsNullOrWhiteSpace(FuseProtection) ? $" | Fusível: {FuseProtection}" : "";
            return $"{CircuitLineType} ({WireColor}, {WireGauge}){fusivel} [{FromNodeId} ➔ {ToNodeId}]";
        }

        public static string ObterCorNeonPadrao(string linha)
        {
            return linha.ToUpperInvariant() switch
            {
                var l when l.Contains("30") || l.Contains("BAT") => "#FF1493",    // Deep Pink / Neon Red (+12V Permanente)
                var l when l.Contains("15") || l.Contains("IGN") => "#39FF14",    // Neon Lime (+12V Pós-Chave)
                var l when l.Contains("87") || l.Contains("SAIDA") => "#00F5FF",  // Neon Cyan (Potência Atuada)
                var l when l.Contains("50") || l.Contains("PARTIDA") => "#FF8C00",// Neon Amber (Linha 50 Partida)
                var l when l.Contains("31") || l.Contains("TERRA") || l.Contains("GND") => "#E0E6ED", // Clean Platinum (Massa / Terra)
                var l when l.Contains("CAN-H") => "#00BFFF",                       // Deep Sky Blue (CAN High)
                var l when l.Contains("CAN-L") => "#00FF7F",                       // Spring Green (CAN Low)
                var l when l.Contains("85") || l.Contains("86") => "#BA55D3",     // Medium Orchid (Bobina de Controle)
                _ => "#00E5FF"                                                    // Electric Blue Default
            };
        }
    }
}
