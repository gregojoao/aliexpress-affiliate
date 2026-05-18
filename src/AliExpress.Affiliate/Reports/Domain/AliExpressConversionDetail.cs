namespace AliExpress.Affiliate.Reports.Domain;

/// <summary>
/// Conversion enriched with the per-line breakdown returned by
/// <c>aliexpress.affiliate.order.get</c>. Inherits every field from
/// <see cref="AliExpressConversion"/> and adds <see cref="Lines"/>.
/// </summary>
public sealed record AliExpressConversionDetail(
    string ConversionId,
    string OrderId,
    OrderStatus Status,
    string? ProductId,
    string? ProductTitle,
    string? ProductImageUrl,
    string? ProductUrl,
    int Quantity,
    Money ItemPrice,
    Money TotalSale,
    Money Commission,
    decimal CommissionRate,
    string? SubId1,
    string? SubId2,
    string? SubId3,
    string? SubId4,
    string? SubId5,
    DateTimeOffset? ClickTime,
    DateTimeOffset PurchaseTime,
    DateTimeOffset? PaidTime,
    DateTimeOffset? FinishTime,
    string? Currency,
    string? RawJson,
    IReadOnlyList<AliExpressOrderLine> Lines)
    : AliExpressConversion(
        ConversionId,
        OrderId,
        Status,
        ProductId,
        ProductTitle,
        ProductImageUrl,
        ProductUrl,
        Quantity,
        ItemPrice,
        TotalSale,
        Commission,
        CommissionRate,
        SubId1,
        SubId2,
        SubId3,
        SubId4,
        SubId5,
        ClickTime,
        PurchaseTime,
        PaidTime,
        FinishTime,
        Currency,
        RawJson);
