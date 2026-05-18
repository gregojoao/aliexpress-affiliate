namespace AliExpress.Affiliate.Reports.Domain;

/// <summary>
/// Line item belonging to a single order returned by <c>aliexpress.affiliate.order.get</c>.
/// </summary>
/// <param name="ProductId">Product id for this line.</param>
/// <param name="ProductTitle">Product title for this line.</param>
/// <param name="Quantity">Quantity ordered.</param>
/// <param name="ItemPrice">Unit price as reported.</param>
/// <param name="TotalSale">Total sale amount for the line.</param>
/// <param name="Commission">Commission attributed to the line.</param>
/// <param name="CommissionRate">Commission rate for the line as a fraction.</param>
public sealed record AliExpressOrderLine(
    string? ProductId,
    string? ProductTitle,
    int Quantity,
    Money ItemPrice,
    Money TotalSale,
    Money Commission,
    decimal CommissionRate);
