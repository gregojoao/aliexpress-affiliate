namespace AliExpress.Affiliate.Reports.Domain;

/// <summary>
/// Single attributed order returned by <c>aliexpress.affiliate.order.list</c>.
/// </summary>
/// <param name="ConversionId">Conversion id assigned by AliExpress (typically the sub-order id).</param>
/// <param name="OrderId">Top-level order id reported by AliExpress.</param>
/// <param name="Status">Canonical status (see <see cref="OrderStatus"/>).</param>
/// <param name="ProductId">Product id of the converting item, if reported.</param>
/// <param name="ProductTitle">Product title at the time of the order.</param>
/// <param name="ProductImageUrl">Main product image URL, when present in the response.</param>
/// <param name="ProductUrl">Product detail URL (without affiliate parameters).</param>
/// <param name="Quantity">Quantity ordered for this conversion.</param>
/// <param name="ItemPrice">Unit price recorded for the item.</param>
/// <param name="TotalSale">Total sale amount (item price × quantity, or as reported).</param>
/// <param name="Commission">Commission attributed to the affiliate.</param>
/// <param name="CommissionRate">Commission rate as a fraction (e.g. <c>0.07m</c> for 7%).</param>
/// <param name="SubId1">First sub-id slot reported by AliExpress.</param>
/// <param name="SubId2">Second sub-id slot.</param>
/// <param name="SubId3">Third sub-id slot.</param>
/// <param name="SubId4">Fourth sub-id slot.</param>
/// <param name="SubId5">Fifth sub-id slot.</param>
/// <param name="ClickTime">Click timestamp, when present.</param>
/// <param name="PurchaseTime">Order creation / purchase timestamp.</param>
/// <param name="PaidTime">Payment timestamp, when present.</param>
/// <param name="FinishTime">Settlement / finished timestamp, when present.</param>
/// <param name="Currency">Currency reported by AliExpress in <c>settled_currency</c>
/// (typically <c>USD</c>, even for non-US accounts — AliExpress settles affiliate
/// payouts in USD by default).</param>
/// <param name="RawJson">Raw JSON payload of the single order, useful for debugging.</param>
public record AliExpressConversion(
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
    string? RawJson);
