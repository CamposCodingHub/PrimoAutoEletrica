using System;
using System.Collections.Concurrent;
using System.Linq;
using PrimoAutoEletrica.Helpers;

namespace PrimoAutoEletrica.Services
{
    public sealed class AppCacheService
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
        private readonly TimeSpan _defaultTtl;
        public AppCacheService(TimeSpan? defaultTtl = null) => _defaultTtl = defaultTtl ?? TimeSpan.FromMinutes(5);

        public T GetOrAdd<T>(string key, Func<T> factory, TimeSpan? ttl = null)
        {
            if (TryGet<T>(key, out var cached) && cached is not null) return cached;
            var value = factory();
            _entries[key] = new CacheEntry(value!, DateTime.UtcNow.Add(ttl ?? _defaultTtl));
            return value;
        }

        public bool TryGet<T>(string key, out T? value)
        {
            value = default;
            if (!_entries.TryGetValue(key, out var entry)) return false;
            if (entry.ExpiresAtUtc <= DateTime.UtcNow) { _entries.TryRemove(key, out _); return false; }
            if (entry.Value is T typed) { value = typed; return true; }
            return false;
        }

        public void InvalidatePrefix(string prefix)
        {
            foreach (var key in _entries.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                _entries.TryRemove(key, out _);
        }

        public void Clear() => _entries.Clear();
        private sealed record CacheEntry(object Value, DateTime ExpiresAtUtc);
    }

    public sealed class SoftDeleteService
    {
        private readonly DatabaseService _db;
        private readonly LoggerService? _logger;
        private readonly AuditLogService? _audit;

        public SoftDeleteService(DatabaseService db, LoggerService? logger = null, AuditLogService? audit = null)
        {
            _db = db; _logger = logger; _audit = audit;
        }

        public bool SoftDelete(string tableName, object id, string? por = null)
        {
            var table = SqlIdentifierGuard.EnsureAllowedTable(tableName);
            var usuario = string.IsNullOrWhiteSpace(por) ? (App.Session?.UserName ?? "sistema") : por.Trim();
            using var connection = _db.GetConnection();
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $@"UPDATE {table} SET IsDeleted = 1, ExcluidoEm = @Em, ExcluidoPor = @Por
                                 WHERE Id = @Id AND COALESCE(IsDeleted, 0) = 0;";
            cmd.Parameters.AddWithValue("@Id", id?.ToString() ?? "");
            cmd.Parameters.AddWithValue("@Em", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@Por", usuario);
            var rows = cmd.ExecuteNonQuery();
            if (rows > 0)
            {
                _logger?.LogInfo($"Soft delete {table}/{id} por {usuario}");
                try { _audit?.RegistrarAcaoCritica("LGPD", "SoftDelete", table, id?.ToString() ?? "", usuario); } catch { }
            }
            return rows > 0;
        }

        public bool Restore(string tableName, object id, string? por = null)
        {
            var table = SqlIdentifierGuard.EnsureAllowedTable(tableName);
            var usuario = string.IsNullOrWhiteSpace(por) ? (App.Session?.UserName ?? "sistema") : por.Trim();
            using var connection = _db.GetConnection();
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $@"UPDATE {table} SET IsDeleted = 0, ExcluidoEm = NULL, ExcluidoPor = NULL, Ativo = 1
                                 WHERE Id = @Id AND COALESCE(IsDeleted, 0) = 1;";
            cmd.Parameters.AddWithValue("@Id", id?.ToString() ?? "");
            var rows = cmd.ExecuteNonQuery();
            if (rows > 0)
            {
                _logger?.LogInfo($"Restore {table}/{id} por {usuario}");
                try { _audit?.RegistrarAcaoCritica("LGPD", "Restore", table, id?.ToString() ?? "", usuario); } catch { }
            }
            return rows > 0;
        }
    }

    public sealed class RBACService
    {
        private readonly PermissionService _permissions;
        private readonly AppCacheService _cache;
        private readonly TimeSpan _ttl = TimeSpan.FromMinutes(2);

        public RBACService(PermissionService permissions, AppCacheService cache)
        {
            _permissions = permissions;
            _cache = cache;
        }

        public static RBACService CriarParaSessaoAtual() =>
            new(PermissionService.CriarParaSessaoAtual(), new AppCacheService());

        public bool CanExecute(string code) =>
            _cache.GetOrAdd($"rbac:{_permissions.ObterPerfil()}:{code}", () => _permissions.TemPermissaoCodigo(code), _ttl);

        public bool CanAccessModule(string module) =>
            _cache.GetOrAdd($"rbac-mod:{_permissions.ObterPerfil()}:{module}", () => _permissions.TemPermissao(module), _ttl);

        public void InvalidateCache() => _cache.InvalidatePrefix("rbac");
    }
}
