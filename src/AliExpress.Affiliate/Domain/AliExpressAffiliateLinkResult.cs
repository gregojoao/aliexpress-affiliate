namespace AliExpress.Affiliate;

public sealed record AliExpressAffiliateLinkResult(
    string AffiliateUrl,
    string SourceUrl,
    string ProductUrl,
    string FinalProductUrl,
    string ProductTitle,
    string ProductPrice,
    string ProductOriginalPrice,
    string ProductImageUrl,
    AliExpressProductDetails? ProductDetails);
