using BilisimPersoneli.Application.Common;

namespace BilisimPersoneli.Web.Models;

public class PaginationUiModel
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPrevious { get; init; }
    public bool HasNext { get; init; }
    public string PageParam { get; init; } = "page";
    public string PageSizeParam { get; init; } = "pageSize";
    public IReadOnlyDictionary<string, string?> ExtraQuery { get; init; } =
        new Dictionary<string, string?>();

    public static PaginationUiModel From(
        IPagedResult result,
        string pageParam = "page",
        string pageSizeParam = "pageSize",
        IReadOnlyDictionary<string, string?>? extraQuery = null)
    {
        return new PaginationUiModel
        {
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages,
            HasPrevious = result.HasPrevious,
            HasNext = result.HasNext,
            PageParam = pageParam,
            PageSizeParam = pageSizeParam,
            ExtraQuery = extraQuery ?? new Dictionary<string, string?>()
        };
    }
}
