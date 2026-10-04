using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Models.Common
{
    /// <summary>
    /// Estrutura universal de resposta paginada para o PRIMOX.
    /// Impede sobrecarga de memória carregando dados em lotes finitos a partir do banco.
    /// </summary>
    /// <typeparam name="T">Tipo do item contido na página</typeparam>
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; }
        public int PageNumber { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public PagedResult(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
        {
            Items = items ?? Array.Empty<T>();
            TotalCount = Math.Max(0, totalCount);
            PageNumber = Math.Max(1, pageNumber);
            PageSize = Math.Max(1, pageSize);
        }

        public static PagedResult<T> Empty(int pageNumber = 1, int pageSize = 25) =>
            new(Array.Empty<T>(), 0, pageNumber, pageSize);
    }

    /// <summary>
    /// Parâmetros padronizados de solicitação de página e busca textual.
    /// </summary>
    public record PaginationParams(
        int PageNumber = 1,
        int PageSize = 25,
        string? SearchTerm = null,
        string? SortBy = null,
        bool Ascending = true)
    {
        public int Offset => Math.Max(0, (Math.Max(1, PageNumber) - 1) * Math.Max(1, PageSize));
        public int Limit => Math.Max(1, PageSize);
    }
}
