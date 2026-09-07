using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PrimoAutoEletrica.Helpers
{
    public static class SqlIdentifierGuard
    {
        private static readonly Regex SafeIdentifierRegex =
            new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly HashSet<string> AllowedTables = new(StringComparer.OrdinalIgnoreCase)
        {
            "Clientes", "Veiculos", "Produtos", "Orcamentos", "OrcamentoItens",
            "OrdensServico", "OrdemServicoItens", "Vendas", "VendaItens",
            "Fornecedores", "Funcionarios", "Agendamentos", "AgendamentoProdutos",
            "ImportacoesNFe", "ContasPagar", "ContasReceber", "Auditoria",
            "LoginTentativasSeguranca", "SchemaMigrations", "Perfis", "Permissoes", "PerfilPermissoes"
        };

        private static readonly HashSet<string> AllowedColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "Id", "ProdutoId", "ClienteId", "VeiculoId", "FornecedorId", "FuncionarioId",
            "Ativo", "IsDeleted", "ExcluidoEm", "ExcluidoPor", "Nome", "Status", "Email"
        };

        public static string EnsureAllowedTable(string? tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName) || !SafeIdentifierRegex.IsMatch(tableName) || !AllowedTables.Contains(tableName))
                throw new ArgumentException($"Tabela nao permitida: '{tableName}'.", nameof(tableName));
            return tableName;
        }

        public static string EnsureAllowedColumn(string? columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName) || !SafeIdentifierRegex.IsMatch(columnName) || !AllowedColumns.Contains(columnName))
                throw new ArgumentException($"Coluna nao permitida: '{columnName}'.", nameof(columnName));
            return columnName;
        }
    }

    public sealed class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalItems { get; init; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
    }

    public static class PagingHelper
    {
        public static PagedResult<T> Page<T>(IEnumerable<T> source, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 500);
            var list = source as IList<T> ?? source.ToList();
            return new PagedResult<T>
            {
                Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = list.Count
            };
        }
    }
}
