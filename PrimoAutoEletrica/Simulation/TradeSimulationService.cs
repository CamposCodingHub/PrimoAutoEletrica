using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace PrimoAutoEletrica.Simulation
{
    /// <summary>
    /// Simula a estratégia de negociação descrita:
    /// 4 contratos entram, 4 alvos fixos e 1 stop fixo.
    /// O terceiro contrato move o stop para breakeven; o quarto tem risco zero.
    /// </summary>
    public class TradeSimulationService
    {
        private readonly ILogger<TradeSimulationService> _logger;

        public TradeSimulationService(ILogger<TradeSimulationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IReadOnlyList<ContractResult> Simulate()
        {
            var results = new List<ContractResult>();

            // Definir alvos fixos (% de lucro)
            var targets = new[] { 0.50m, 1.00m, 1.618m, 2.00m };
            // Stop fixo inicial (colocado na entrada)
            var stop = 0.0m; // risco zero depois do stop move

            // Simular cada contrato
            for (int i = 0; i < 4; i++)
            {
                var contract = new ContractResult
                {
                    Index = i + 1,
                    Target = targets[i],
                    EntryPrice = 1m // preço de referência (qualquer unidade)
                };

                // Caso especial do contrato 3 (index 2) - move stop para breakeven ao atingir alvo 3
                if (i == 2)
                {
                    // Quando atinge alvo 3, stop passa a ser entrada (breakeven)
                    contract.StopMovedToBreakeven = true;
                    // O stop já está em breakeven, portanto risco zero a partir daqui
                    contract.Stop = 0m;
                }
                else if (i == 3) // contrato 4 com risco zero e alvo extra
                {
                    // Stop já está no preço de entrada (risco zero)
                    contract.Stop = 0m;
                    contract.HasExtraTarget = true;
                }
                else
                {
                    // Para contratos 1 e 2, stop permanece no preço de entrada (não usado)
                    contract.Stop = 0m;
                }

                results.Add(contract);
            }

            _logger.LogInformation("Simulação de contratos concluída com {Count} contratos.", results.Count);
            return results;
        }
    }

    public class ContractResult
    {
        public int Index { get; set; }
        public decimal EntryPrice { get; set; }
        public decimal Target { get; set; }
        public decimal Stop { get; set; }
        public bool StopMovedToBreakeven { get; set; }
        public bool HasExtraTarget { get; set; }
    }
}
