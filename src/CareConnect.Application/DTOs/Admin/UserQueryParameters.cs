namespace CareConnect.Application.DTOs.Admin;

public class UserQueryParameters
{
    private const int MaxPageSize = 100;

    private int _page = 1;
    private int _pageSize = 10;

    /// <summary>Matches against full name or email, case-insensitively.</summary>
    public string? Search { get; set; }

    public string? Role { get; set; }

    public bool? IsActive { get; set; }

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
            < 1 => 10,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }
}

public class ToggleUserStatusResponse
{
    public string UserId { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}
