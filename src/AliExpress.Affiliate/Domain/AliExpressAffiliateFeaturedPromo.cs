namespace AliExpress.Affiliate.Domain;

public sealed record AliExpressAffiliateFeaturedPromo(
    string PromoName,
    string PromoDescription,
    string ProductCount,
    string RawJson);
