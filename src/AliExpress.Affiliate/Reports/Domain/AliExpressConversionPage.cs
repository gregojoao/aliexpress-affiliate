namespace AliExpress.Affiliate.Reports.Domain;

/// <summary>
/// Single page of conversions returned by <c>aliexpress.affiliate.order.list</c>.
/// </summary>
/// <param name="Items">Conversions on this page.</param>
/// <param name="Page">Page number echoed back by the API (1-based).</param>
/// <param name="PageSize">Page size requested.</param>
/// <param name="TotalCount">Total record count reported by AliExpress, when available.</param>
/// <param name="HasMore">True when at least one more page can be fetched.</param>
public sealed record AliExpressConversionPage(
    IReadOnlyList<AliExpressConversion> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasMore);
