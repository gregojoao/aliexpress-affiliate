namespace AliExpress.Affiliate;

public sealed record AliExpressOrderListQuery
{
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string LocaleSite { get; init; } = string.Empty;
    public int PageNo { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public string Fields { get; init; } = string.Empty;
}
