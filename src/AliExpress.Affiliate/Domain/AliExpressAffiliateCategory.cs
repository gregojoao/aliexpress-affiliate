namespace AliExpress.Affiliate;

public sealed record AliExpressAffiliateCategory(
    string CategoryId,
    string CategoryName,
    string ParentCategoryId,
    string RawJson);
