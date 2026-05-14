namespace AliExpress.Affiliate.OpenPlatform;

public sealed record AliExpressOpenPlatformRequest(
    Uri RequestUri,
    IReadOnlyDictionary<string, string> CommonParameters,
    IReadOnlyDictionary<string, string> BodyParameters);
