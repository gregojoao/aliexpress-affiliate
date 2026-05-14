namespace AliExpress.Affiliate;

public sealed record AliExpressOrderListByIndexQuery
{
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string TimeType { get; init; } = string.Empty;
    public string StartQueryIndexId { get; init; } = string.Empty;
    public string LocaleSite { get; init; } = string.Empty;
    public int PageSize { get; init; } = 50;
    public string Fields { get; init; } = string.Empty;
}
