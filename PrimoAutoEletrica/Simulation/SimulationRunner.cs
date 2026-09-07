using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PrimoAutoEletrica.Simulation
{
    public static class SimulationRunner
    {
        public static async Task RunAsync()
        {
            // Configura DI mínima para o TradeSimulationService
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddTransient<TradeSimulationService>();
            var provider = services.BuildServiceProvider();

            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Simulation");
            
            // Use TradeSimulationService to run the contract simulation
            var tradeService = provider.GetRequiredService<TradeSimulationService>();
            var results = tradeService.Simulate();
            foreach (var contract in results)
            {
                logger.LogInformation($"Contrato {contract.Index}: Target={contract.Target:P}, StopMovedToBreakeven={contract.StopMovedToBreakeven}, HasExtraTarget={contract.HasExtraTarget}");
            }
        }
    }
}
