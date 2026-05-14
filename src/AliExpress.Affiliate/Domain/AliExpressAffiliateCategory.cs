namespace AliExpress.Affiliate.Domain;

public sealed record AliExpressAffiliateCategory(
    string CategoryId,
    string CategoryName,
    string ParentCategoryId,
    string RawJson);
