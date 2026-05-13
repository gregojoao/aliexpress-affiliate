# AliExpress.Affiliate

Minimal .NET 10 client for the AliExpress Open Platform affiliate APIs.

It supports:

- Generating affiliate links with `aliexpress.affiliate.link.generate`
- Fetching product details with `aliexpress.affiliate.productdetail.get`
- TOP-style request signing with `md5`, `hmac-md5`, or `hmac-sha256`
- AliExpress product URL normalization by trimming everything after `.html`
- Parsing product title, current price, original price, image, product URL, and promotion link
- Detecting responses where AliExpress returns only `source_value` and no `promotion_link`

## Install

After publishing to NuGet:

```bash
dotnet add package AliExpress.Affiliate
```

For local development:

```bash
dotnet pack src/AliExpress.Affiliate/AliExpress.Affiliate.csproj -c Release
```

## Quick Start

```csharp
using AliExpress.Affiliate;

var options = new AliExpressAffiliateOptions
{
    AppKey = "<your-app-key>",
    AppSecret = "<your-app-secret>",
    TrackingId = "<your-tracking-id>",
    SignMethod = "md5",
    PromotionLinkType = "0",
    TargetCurrency = "BRL",
    TargetLanguage = "PT",
    ShipToCountry = "BR",
    IncludeProductDetails = true
};

using var httpClient = new HttpClient();
var client = new AliExpressAffiliateClient(httpClient);

var result = await client.GenerateAffiliateLinkAsync(
    "https://pt.aliexpress.com/item/1005006356702381.html?spm=abc",
    options);

Console.WriteLine(result?.AffiliateUrl);
Console.WriteLine(result?.ProductTitle);
Console.WriteLine(result?.ProductPrice);
Console.WriteLine(result?.ProductOriginalPrice);
```

## Product Details Only

```csharp
var details = await client.GetProductDetailsAsync(
    "1005006356702381",
    options);

Console.WriteLine(details?.ProductTitle);
Console.WriteLine(details?.ProductPrice);
Console.WriteLine(details?.ProductOriginalPrice);
```

## Environment Variable Helper

If you already keep credentials in a dictionary or configuration section, you can use:

```csharp
var options = AliExpressAffiliateEnvironment.FromDictionary(new Dictionary<string, string>
{
    ["ALIEXPRESS_AFFILIATE_APP_KEY"] = "<your-app-key>",
    ["ALIEXPRESS_AFFILIATE_APP_SECRET"] = "<your-app-secret>",
    ["ALIEXPRESS_TRACKING_ID"] = "<your-tracking-id>",
    ["ALIEXPRESS_AFFILIATE_API_SIGN_METHOD"] = "md5",
    ["ALIEXPRESS_TARGET_CURRENCY"] = "BRL",
    ["ALIEXPRESS_TARGET_LANGUAGE"] = "PT",
    ["ALIEXPRESS_SHIP_TO_COUNTRY"] = "BR",
    ["ALIEXPRESS_AFFILIATE_PRODUCT_DETAIL_ENABLED"] = "true"
});
```

## Non-Affiliate Products

Some AliExpress products cannot generate affiliate links. When the API succeeds but returns no `promotion_link`, the client throws:

```csharp
AliExpressAffiliateLinkUnavailableException
```

This is different from an HTTP/signature/configuration failure and is useful for sending the item to a dedicated error queue.

## Publishing

```bash
dotnet test
dotnet pack src/AliExpress.Affiliate/AliExpress.Affiliate.csproj -c Release
dotnet nuget push src/AliExpress.Affiliate/bin/Release/AliExpress.Affiliate.0.1.0.nupkg \
  --api-key <NUGET_API_KEY> \
  --source https://api.nuget.org/v3/index.json
```

## License

MIT
