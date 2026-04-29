namespace Quraan.Application.Common;

public sealed record PageResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
