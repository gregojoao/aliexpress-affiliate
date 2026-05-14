namespace AliExpress.Affiliate.Domain;

public sealed record AliExpressAffiliateLink(
    string SourceValue,
    string PromotionLink,
    string RawJson);
