namespace Quraan.Application.Common;

public sealed record PageRequest(int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;

    public int Skip => (Math.Max(1, Page) - 1) * Math.Clamp(PageSize, 1, MaxPageSize);
    public int Take => Math.Clamp(PageSize, 1, MaxPageSize);
}
