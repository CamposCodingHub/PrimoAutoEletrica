using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Security
{
    public class LicenseScaffoldHonestyTests
    {
        [Fact]
        public void LicenseService_IsCommercialScaffoldOnly_True()
        {
            Assert.True(LicenseService.IsCommercialScaffoldOnly);
        }
    }
}
