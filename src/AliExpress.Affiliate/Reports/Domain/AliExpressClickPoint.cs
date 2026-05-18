namespace AliExpress.Affiliate.Reports.Domain;

/// <summary>
/// Single time bucket inside <see cref="AliExpressClickStats"/>.
/// </summary>
public sealed record AliExpressClickPoint(
    DateTimeOffset Bucket,
    int Clicks,
    int Conversions,
    Money Commission);
