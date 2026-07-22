namespace CareConnect.Application.Common.Models;

/// <summary>
/// Shared paging inputs. Page and PageSize clamp themselves, so a caller cannot ask for
/// page 0 or pull the whole table down in one request.
/// </summary>
public abstract class PagedQueryParameters
{
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 10;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }

    public int Skip => (Page - 1) * PageSize;
}
