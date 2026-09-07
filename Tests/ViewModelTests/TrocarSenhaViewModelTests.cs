using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Models;
using Xunit;

namespace PrimoAutoEletrica.Tests.ViewModelTests
{
    public class TrocarSenhaViewModelTests
    {
        [Fact]
        public void Save_InvalidPassword_ShowsError()
        {
            var db = new DatabaseService();
            var logger = new LoggerService();
            var funcionario = new Funcionario { Id = 1, Email = "u@ex" };
            var vm = new TrocarSenhaObrigatoriaViewModel(funcionario, "temp1234", db, logger);

            vm.NovaSenha = "short";
            vm.ConfirmarSenha = "short";

            var ok = vm.Save();

            Assert.False(ok);
            Assert.True(vm.HasError);
        }
    }
}
