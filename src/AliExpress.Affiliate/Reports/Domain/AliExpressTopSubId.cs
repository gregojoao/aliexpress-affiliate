namespace AliExpress.Affiliate.Reports.Domain;

/// <summary>
/// Aggregated metric per sub-id (the first non-empty sub-id slot) in a sales summary.
/// </summary>
public sealed record AliExpressTopSubId(
    string SubId,
    int Conversions,
    Money Commission);
