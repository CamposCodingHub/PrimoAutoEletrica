using System;
using System.IO;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    /// <summary>
    /// Regressao source-level do BUG-007: ExerciseHostedElementButtons deve rotear
    /// ClientesControl para o harness dedicado. PASS funcional real = UiSmoke
    /// Interacao:Modulo:ClientesControl no App EXE (nao fabricar PASS aqui).
    /// </summary>
    public class ClientesHarnessRoutingRegressionTests
    {
        [Fact]
        public void Helpers_RoteiaClientesControlParaHarnessDedicado()
        {
            var src = Locate("Services", "UiSmokeTestService.Helpers.cs");
            var text = File.ReadAllText(src);

            Assert.Contains("private void ExerciseClientesControlButtons", text, StringComparison.Ordinal);

            var hostedIdx = text.IndexOf("private void ExerciseHostedElementButtons", StringComparison.Ordinal);
            Assert.True(hostedIdx >= 0, "ExerciseHostedElementButtons nao encontrado");

            var clientesMethodIdx = text.IndexOf("private void ExerciseClientesControlButtons", StringComparison.Ordinal);
            Assert.True(clientesMethodIdx > hostedIdx, "metodo dedicado deve existir apos o roteador");

            var routerBlock = text.Substring(hostedIdx, clientesMethodIdx - hostedIdx);
            Assert.Contains("typeof(ClientesControl)", routerBlock, StringComparison.Ordinal);
            Assert.Contains("ExerciseClientesControlButtons()", routerBlock, StringComparison.Ordinal);

            var clientesBranch = routerBlock.IndexOf("typeof(ClientesControl)", StringComparison.Ordinal);
            var fallbackIdx = routerBlock.IndexOf("ExerciseInteractionSurface(", StringComparison.Ordinal);
            Assert.True(fallbackIdx > clientesBranch, "fallback generico deve vir depois do ramo Clientes");
        }

        private static string Locate(string folder, string fileName)
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "PrimoAutoEletrica", folder, fileName);
                if (File.Exists(candidate)) return candidate;
                candidate = Path.Combine(dir.FullName, folder, fileName);
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            throw new FileNotFoundException($"Nao encontrou {folder}/{fileName}");
        }
    }
}