using System;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Mantem o contexto global da sessao autenticada na aplicacao desktop.
    /// </summary>
    public class AppSessionService
    {
        public Guid SessionId { get; private set; }
        public DateTime? LoginAt { get; private set; }
        public DateTime? LastActivityAt { get; private set; }
        public Funcionario? CurrentUser { get; private set; }

        public bool IsAuthenticated => CurrentUser != null;
        public int? UserId => CurrentUser?.Id;
        public string UserName => CurrentUser?.Nome ?? "Sistema";
        public string UserEmail => CurrentUser?.Email ?? string.Empty;
        public string AccessProfile => CurrentUser?.PerfilAcesso ?? "Sem perfil";

        public void StartSession(Funcionario funcionario)
        {
            CurrentUser = funcionario ?? throw new ArgumentNullException(nameof(funcionario));
            SessionId = Guid.NewGuid();
            LoginAt = DateTime.Now;
            LastActivityAt = LoginAt;
        }

        public void TouchActivity()
        {
            if (!IsAuthenticated)
            {
                return;
            }

            LastActivityAt = DateTime.Now;
        }

        public void EndSession()
        {
            CurrentUser = null;
            SessionId = Guid.Empty;
            LoginAt = null;
            LastActivityAt = null;
        }
    }
}
