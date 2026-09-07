using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.ViewModelTests
{
    public class LoginViewModelTests
    {
        [Fact]
        public void Login_InvalidCredentials_ShowsError()
        {
            var db = new DatabaseService();
            var logger = new LoggerService();
            var userSession = new UserSessionService(db, logger, App.Session);
            var vm = new LoginViewModel(db, logger, userSession);

            vm.Email = "invalid@example.com";
            vm.Password = "wrong";

            var result = vm.Login();

            Assert.False(result);
            Assert.True(vm.HasError);
            Assert.False(string.IsNullOrWhiteSpace(vm.ErrorMessage));
        }
    }
}
