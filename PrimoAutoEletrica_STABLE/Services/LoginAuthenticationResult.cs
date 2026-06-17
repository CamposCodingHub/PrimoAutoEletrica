using PrimoAutoEletrica.Models;
using System;

namespace PrimoAutoEletrica.Services
{
    public enum LoginAuthenticationStatus
    {
        Success = 1,
        InvalidCredentials = 2,
        Locked = 3
    }

    public sealed class LoginAuthenticationResult
    {
        public LoginAuthenticationStatus Status { get; init; }
        public Funcionario? Funcionario { get; init; }
        public DateTime? BloqueadoAte { get; init; }
        public int TentativasFalhas { get; init; }
        public int TentativasRestantes { get; init; }
        public string MensagemUsuario { get; init; } = string.Empty;

        public bool IsSuccess => Status == LoginAuthenticationStatus.Success && Funcionario != null;
        public bool IsLocked => Status == LoginAuthenticationStatus.Locked;
        public bool RequiresPasswordChange => IsSuccess && Funcionario?.ExigirTrocaSenha == true;

        public static LoginAuthenticationResult Success(Funcionario funcionario)
        {
            return new LoginAuthenticationResult
            {
                Status = LoginAuthenticationStatus.Success,
                Funcionario = funcionario
            };
        }

        public static LoginAuthenticationResult Invalid(string mensagemUsuario, int tentativasFalhas = 0, int tentativasRestantes = 0)
        {
            return new LoginAuthenticationResult
            {
                Status = LoginAuthenticationStatus.InvalidCredentials,
                TentativasFalhas = tentativasFalhas,
                TentativasRestantes = tentativasRestantes,
                MensagemUsuario = mensagemUsuario
            };
        }

        public static LoginAuthenticationResult Locked(DateTime bloqueadoAte, int tentativasFalhas, string mensagemUsuario)
        {
            return new LoginAuthenticationResult
            {
                Status = LoginAuthenticationStatus.Locked,
                BloqueadoAte = bloqueadoAte,
                TentativasFalhas = tentativasFalhas,
                TentativasRestantes = 0,
                MensagemUsuario = mensagemUsuario
            };
        }
    }
}
