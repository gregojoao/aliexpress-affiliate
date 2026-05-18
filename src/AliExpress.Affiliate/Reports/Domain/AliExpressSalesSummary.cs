namespace AliExpress.Affiliate.Reports.Domain;

/// <summary>
/// Aggregate sales summary computed by the SDK from the conversion stream returned by
/// <c>aliexpress.affiliate.order.list</c>. Clicks are not exposed by the official TOP
/// gateway and are therefore <c>null</c>; <see cref="Supported"/> remains <c>true</c>
/// since the conversion-level fields are always populated.
/// </summary>
public sealed record AliExpressSalesSummary(
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    int Conversions,
    int? Clicks,
    Money GrossRevenue,
    Money Commission,
    decimal AvgCommissionRate,
    decimal? ConversionRate,
    IReadOnlyDictionary<OrderStatus, int> ByStatus,
    IReadOnlyList<AliExpressTopProduct> TopProducts,
    IReadOnlyList<AliExpressTopSubId> TopSubIds,
    bool Supported,
    string? UnsupportedReason);
