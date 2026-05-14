namespace AliExpress.Affiliate.Application.Requests;

public sealed record AliExpressOrderListByIndexQuery
{
    public DateTimeOffset? StartTime { get; init; }
    public DateTimeOffset? EndTime { get; init; }
    public string Status { get; init; } = string.Empty;
    public string TimeType { get; init; } = string.Empty;
    public string StartQueryIndexId { get; init; } = string.Empty;
    public string Locale { get; init; } = string.Empty;
    public int PageSize { get; init; } = 50;
    public string Fields { get; init; } = string.Empty;
}
