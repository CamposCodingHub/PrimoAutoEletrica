using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public class MoneyCentsTests
    {
        [Theory]
        [InlineData(10.00, 1000)]
        [InlineData(10.005, 1001)] // AwayFromZero
        [InlineData(10.004, 1000)]
        [InlineData(0.01, 1)]
        [InlineData(-1.50, -150)]
        public void FromDecimal_RoundsAwayFromZero(decimal amount, long expectedCents)
        {
            Assert.Equal(expectedCents, MoneyCents.FromDecimal(amount).Cents);
        }

        [Fact]
        public void ApplyPercentDiscount_TenPercent()
        {
            var gross = MoneyCents.FromDecimal(100m);
            var net = MoneyCents.ApplyPercentDiscount(gross, 10m);
            Assert.Equal(9000, net.Cents);
        }

        [Fact]
        public void Sum_And_Diff_PreserveCents()
        {
            var a = MoneyCents.FromDecimal(0.10m);
            var b = MoneyCents.FromDecimal(0.20m);
            Assert.Equal(30, (a + b).Cents);
            Assert.Equal(10, (b - a).Cents);
        }

        [Fact]
        public void Source_Documents_Migration_Not_Done()
        {
            var src = System.IO.File.ReadAllText(
                Locate("Services", "MoneyCents.cs"));
            Assert.Contains("NOT DONE", src);
            Assert.Contains("integer centavos", src, System.StringComparison.OrdinalIgnoreCase);
        }

        private static string Locate(string folder, string fileName)
        {
            var dir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = System.IO.Path.Combine(dir.FullName, "PrimoAutoEletrica", folder, fileName);
                if (System.IO.File.Exists(candidate)) return candidate;
                candidate = System.IO.Path.Combine(dir.FullName, folder, fileName);
                if (System.IO.File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            throw new System.IO.FileNotFoundException($"Nao encontrou {folder}/{fileName}");
        }
    }
}
