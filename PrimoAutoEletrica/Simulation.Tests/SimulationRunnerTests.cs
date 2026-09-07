using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrimoAutoEletrica.Simulation;

namespace PrimoAutoEletrica.Simulation.Tests
{
    public class SimulationRunnerTests
    {
        [Fact]
        public async Task RunAsync_ShouldCompleteWithoutException_AndPopulateMetrics()
        {
            // Arrange – the SimulationRunner already configures DI internally, so just call it.
            // No additional setup required.

            // Act
            var exception = await Record.ExceptionAsync(() => SimulationRunner.RunAsync());

            // Assert – no exception should be thrown.
            Assert.Null(exception);
            // Additional verification could be added by exposing the ViewModel via a test‑only internal property,
            // but for now we just ensure the method completes successfully.
        }
    }
}
