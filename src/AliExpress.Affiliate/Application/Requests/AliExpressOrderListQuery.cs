namespace AliExpress.Affiliate.Application.Requests;

public sealed record AliExpressOrderListQuery
{
    public DateTimeOffset? StartTime { get; init; }
    public DateTimeOffset? EndTime { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Locale { get; init; } = string.Empty;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public string Fields { get; init; } = string.Empty;
}
