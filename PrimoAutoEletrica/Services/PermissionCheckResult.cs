using System;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Resultado explícito de verificação de permissão.
    /// Unavailable = falha de infraestrutura (DB/IO); operações críticas devem falhar fechado.
    /// </summary>
    public enum PermissionCheckStatus
    {
        Allowed = 0,
        Denied = 1,
        Unavailable = 2
    }

    public readonly struct PermissionCheckResult
    {
        public PermissionCheckStatus Status { get; }
        public string? Detail { get; }

        public bool IsAllowed => Status == PermissionCheckStatus.Allowed;
        public bool IsDenied => Status == PermissionCheckStatus.Denied;
        public bool IsUnavailable => Status == PermissionCheckStatus.Unavailable;

        private PermissionCheckResult(PermissionCheckStatus status, string? detail)
        {
            Status = status;
            Detail = detail;
        }

        public static PermissionCheckResult Allowed(string? detail = null) =>
            new(PermissionCheckStatus.Allowed, detail);

        public static PermissionCheckResult Denied(string? detail = null) =>
            new(PermissionCheckStatus.Denied, detail);

        public static PermissionCheckResult Unavailable(string? detail = null) =>
            new(PermissionCheckStatus.Unavailable, detail);
    }
}
