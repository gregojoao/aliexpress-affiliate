namespace AliExpress.Affiliate.Reports.Domain;

/// <summary>
/// Aggregated metric per product in a sales summary.
/// </summary>
public sealed record AliExpressTopProduct(
    string? ProductId,
    string? ProductTitle,
    int Conversions,
    Money Commission);
