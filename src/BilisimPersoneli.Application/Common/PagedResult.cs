namespace BilisimPersoneli.Application.Common;

public static class PaginationDefaults
{
    public static readonly int[] PageSizeOptions = { 5, 10, 20, 30, 50, 100 };
    public const int DefaultPageSize = 10;
    public const int DefaultPage = 1;
}

public interface IPagedResult
{
    int Page { get; }
    int PageSize { get; }
    int TotalCount { get; }
    int TotalPages { get; }
    bool HasPrevious { get; }
    bool HasNext { get; }
}

public class PagedResult<T> : IPagedResult
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        var (normalizedPage, normalizedSize) = Pagination.Normalize(page, pageSize);
        return new PagedResult<T>
        {
            Items = items,
            Page = normalizedPage,
            PageSize = normalizedSize,
            TotalCount = totalCount
        };
    }
}

public static class Pagination
{
    public static (int Page, int PageSize) Normalize(int page, int pageSize)
    {
        if (page < 1)
            page = PaginationDefaults.DefaultPage;

        if (!PaginationDefaults.PageSizeOptions.Contains(pageSize))
            pageSize = PaginationDefaults.DefaultPageSize;

        return (page, pageSize);
    }
}
