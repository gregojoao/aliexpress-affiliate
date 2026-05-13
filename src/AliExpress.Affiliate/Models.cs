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

public sealed record AliExpressProductDetails(
    string ProductTitle,
    string ProductPrice,
    string ProductOriginalPrice,
    string ProductImageUrl,
    string ProductUrl,
    string PromotionLink);

public sealed record AliExpressOpenPlatformRequest(
    Uri RequestUri,
    IReadOnlyDictionary<string, string> CommonParameters,
    IReadOnlyDictionary<string, string> BodyParameters);
