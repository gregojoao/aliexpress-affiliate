namespace AliExpress.Affiliate;

public sealed record AliExpressProductDetails(
    string ProductTitle,
    string ProductPrice,
    string ProductOriginalPrice,
    string ProductImageUrl,
    string ProductUrl,
    string PromotionLink);
