# AliExpress.Affiliate

Minimal .NET client for the AliExpress Open Platform affiliate APIs.

`AliExpress.Affiliate` helps generate affiliate links, fetch product details, sign Open Platform requests, and handle the common case where AliExpress returns only `source_value` instead of a promotion link.

## Features

- Generate affiliate links with `aliexpress.affiliate.link.generate`
- Fetch product details with `aliexpress.affiliate.productdetail.get`
- Sign TOP-style requests with `md5`, `hmac-md5`, or `hmac-sha256`
- Normalize AliExpress product URLs by trimming everything after `.html`
- Parse title, current price, original price, image URL, product URL, and promotion link
- Detect non-affiliate products with a dedicated exception
- Load options from dictionaries or configuration-style environment values

## Requirements

- .NET 10 SDK or newer
- AliExpress Open Platform credentials
- An affiliate tracking ID

## Installation

After the package is published to NuGet:

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

## Product Details

Use `GetProductDetailsAsync` when you only need product metadata:

```csharp
var details = await client.GetProductDetailsAsync(
    "1005006356702381",
    options);

Console.WriteLine(details?.ProductTitle);
Console.WriteLine(details?.ProductPrice);
Console.WriteLine(details?.ProductOriginalPrice);
```

The method accepts either a product ID or an AliExpress product URL.

## Configuration

You can build options directly:

```csharp
var options = new AliExpressAffiliateOptions
{
    AppKey = "<your-app-key>",
    AppSecret = "<your-app-secret>",
    TrackingId = "<your-tracking-id>",
    TargetCurrency = "BRL",
    TargetLanguage = "PT",
    ShipToCountry = "BR"
};
```

Or load them from dictionary/configuration values:

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

Supported configuration keys include:

| Option | Keys |
| --- | --- |
| Endpoint | `ALIEXPRESS_AFFILIATE_API_ENDPOINT`, `ALIEXPRESS_ENDPOINT` |
| App key | `ALIEXPRESS_AFFILIATE_APP_KEY`, `ALIEXPRESS_OPEN_API_APP_KEY`, `ALIEXPRESS_APP_KEY` |
| App secret | `ALIEXPRESS_AFFILIATE_APP_SECRET`, `ALIEXPRESS_OPEN_API_APP_SECRET`, `ALIEXPRESS_APP_SECRET` |
| Tracking ID | `ALIEXPRESS_TRACKING_ID`, `ALIEXPRESS_AFFILIATE_TRACKING_ID` |
| Sign method | `ALIEXPRESS_AFFILIATE_API_SIGN_METHOD`, `ALIEXPRESS_SIGN_METHOD` |
| Product details | `ALIEXPRESS_AFFILIATE_PRODUCT_DETAIL_ENABLED`, `ALIEXPRESS_PRODUCT_DETAIL_ENABLED` |
| Timeout | `ALIEXPRESS_AFFILIATE_API_TIMEOUT_MS` |

## Error Handling

Some AliExpress products cannot generate affiliate links. When the API succeeds but does not return `promotion_link`, the client throws:

```csharp
AliExpressAffiliateLinkUnavailableException
```

This is different from HTTP, signature, or configuration failures, so callers can route the item to a dedicated retry or review flow.

## Project Structure

The library follows a lightweight DDD-style organization:

```text
src/AliExpress.Affiliate/
  Application/      Use cases and application ports
  Domain/           Product URL, product ID, price, and result rules
  Infrastructure/   AliExpress Open Platform HTTP, signing, requests, and parsing
```

## Development

Restore, build, test, and pack locally:

```bash
dotnet restore
dotnet build
dotnet test
dotnet pack src/AliExpress.Affiliate/AliExpress.Affiliate.csproj -c Release
```

The repository includes a GitHub Actions workflow that builds, tests, packs, and uploads the generated NuGet package as a workflow artifact.

## Contributing

Issues and pull requests are welcome. Please keep changes focused, add or update tests when behavior changes, and run `dotnet test` before opening a pull request.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
