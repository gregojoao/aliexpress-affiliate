# AliExpress.Affiliate

> A lightweight, strongly typed .NET SDK for AliExpress Open Platform affiliate APIs.

[![CI](https://github.com/gregojoao/aliexpress-affiliate/actions/workflows/ci.yml/badge.svg)](https://github.com/gregojoao/aliexpress-affiliate/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/AliExpress.Affiliate.svg)](https://www.nuget.org/packages/AliExpress.Affiliate)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8%20%7C%2010-512BD4.svg)](https://dotnet.microsoft.com/)

`AliExpress.Affiliate` helps .NET applications generate affiliate links, discover products, fetch product details, read promotions, query orders, and handle AliExpress affiliate API responses with typed contracts.

It also handles a common edge case from the affiliate API: successful responses that return only `source_value` instead of a usable `promotion_link`.

## Highlights

- Generate single or batch affiliate links with explicit request objects
- Fetch product details, product discovery results, categories, featured promotions, and orders
- Register and inject `IAliExpressAffiliateClient`
- Keep credentials/defaults in `AliExpressAffiliateOptions`
- Override tracking, country, language, currency, and product-detail behavior per request
- Use dedicated SDK exceptions for HTTP, API, and unavailable affiliate-link failures
- Keep Open Platform signing/parsing helpers outside the main client via diagnostics APIs

## Installation

```bash
dotnet add package AliExpress.Affiliate
```

Or pack locally:

```bash
dotnet pack src/AliExpress.Affiliate/AliExpress.Affiliate.csproj -c Release -o artifacts
```

## Namespaces

Most applications use these namespaces:

```csharp
using AliExpress.Affiliate;
using AliExpress.Affiliate.Application.Requests;
using AliExpress.Affiliate.Clients;
using AliExpress.Affiliate.Configuration;
using AliExpress.Affiliate.Domain;
```

Dependency injection extensions are in their own namespace:

```csharp
using AliExpress.Affiliate.DependencyInjection;
```

SDK exceptions are grouped under:

```csharp
using AliExpress.Affiliate.Exceptions;
```

Open Platform diagnostics live separately:

```csharp
using AliExpress.Affiliate.OpenPlatform;
```

## Quick Start

```csharp
using AliExpress.Affiliate;
using AliExpress.Affiliate.Application.Requests;
using AliExpress.Affiliate.Clients;
using AliExpress.Affiliate.Configuration;

var options = new AliExpressAffiliateOptions
{
    AppKey = "<your-app-key>",
    AppSecret = "<your-app-secret>",
    DefaultTrackingId = "<your-tracking-id>",
    SignMethod = "md5",
    DefaultPromotionLinkType = "0",
    DefaultTargetCurrency = "BRL",
    DefaultTargetLanguage = "PT",
    DefaultShipToCountry = "BR"
};

using var httpClient = new HttpClient();
var client = new AliExpressAffiliateClient(httpClient);

var result = await client.GenerateAffiliateLinkAsync(
    new AliExpressAffiliateLinkRequest
    {
        ProductUrl = "https://pt.aliexpress.com/item/1005006356702381.html?spm=abc",
        IncludeProductDetails = true
    },
    options);

Console.WriteLine(result?.AffiliateUrl);
Console.WriteLine(result?.ProductTitle);
Console.WriteLine(result?.ProductPrice);
```

## Dependency Injection

Register with `appsettings.json`:

```json
{
  "AliExpress": {
    "Affiliate": {
      "AppKey": "<your-app-key>",
      "AppSecret": "<your-app-secret>",
      "DefaultTrackingId": "<your-tracking-id>",
      "SignMethod": "md5",
      "DefaultPromotionLinkType": "0",
      "DefaultTargetCurrency": "BRL",
      "DefaultTargetLanguage": "PT",
      "DefaultShipToCountry": "BR"
    }
  }
}
```

```csharp
builder.Services.AddAliExpressAffiliate(builder.Configuration);
```

Or configure in code:

```csharp
builder.Services.AddAliExpressAffiliate(options =>
{
    options.AppKey = "<your-app-key>";
    options.AppSecret = "<your-app-secret>";
    options.DefaultTrackingId = "<your-tracking-id>";
    options.DefaultTargetCurrency = "BRL";
    options.DefaultTargetLanguage = "PT";
    options.DefaultShipToCountry = "BR";
});
```

Then inject the interface:

```csharp
public sealed class ProductLinkService
{
    private readonly IAliExpressAffiliateClient _client;

    public ProductLinkService(IAliExpressAffiliateClient client)
    {
        _client = client;
    }

    public Task<AliExpressAffiliateLinkResult?> CreateLinkAsync(
        string productUrl,
        CancellationToken cancellationToken = default)
    {
        return _client.GenerateAffiliateLinkAsync(
            new AliExpressAffiliateLinkRequest
            {
                ProductUrl = productUrl,
                IncludeProductDetails = true
            },
            cancellationToken);
    }
}
```

## API Examples

### Batch Links

```csharp
var links = await client.GenerateAffiliateLinksAsync(
    new AliExpressAffiliateLinksRequest
    {
        SourceUrls = new[]
        {
            "https://pt.aliexpress.com/item/1005006356702381.html",
            "https://pt.aliexpress.com/item/1005006860981590.html"
        }
    },
    options);
```

### Product Details

```csharp
var details = await client.GetProductDetailsAsync(
    "1005006356702381",
    options);
```

### Product Discovery

```csharp
var products = await client.SearchProductsAsync(
    new AliExpressProductQuery
    {
        Keywords = "microfone",
        PageNumber = 1,
        PageSize = 20,
        Sort = "SALE_PRICE_ASC"
    },
    options);

var hotProducts = await client.GetHotProductsAsync(
    new AliExpressProductQuery
    {
        Keywords = "fone bluetooth",
        PageSize = 20
    },
    options);
```

### Promotions

```csharp
var promos = await client.GetFeaturedPromosAsync(options);

var promoProducts = await client.GetFeaturedPromoProductsAsync(
    new AliExpressFeaturedPromoProductsQuery
    {
        PromotionName = "Hot Product",
        PageSize = 20
    },
    options);
```

### Orders

```csharp
var orders = await client.GetOrdersAsync(
    new AliExpressOrderListQuery
    {
        StartTime = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
        EndTime = new DateTimeOffset(2026, 5, 2, 0, 0, 0, TimeSpan.Zero),
        PageNumber = 1,
        PageSize = 20
    },
    options);
```

Order details accept a list of order IDs and the SDK formats the AliExpress CSV parameter internally:

```csharp
var details = await client.GetOrderDetailsAsync(
    new AliExpressOrderDetailsQuery
    {
        OrderIds = new[] { "222222" }
    },
    options);
```

## Configuration

`AliExpressAffiliateOptions` stores credentials and default request values:

| Option | Meaning |
| --- | --- |
| `ApiEndpoint` | AliExpress Open Platform endpoint |
| `AppKey` | Open Platform app key |
| `AppSecret` | Open Platform app secret |
| `DefaultTrackingId` | Default affiliate tracking ID |
| `AppSignature` | Optional app signature |
| `SignMethod` | `md5`, `hmac-md5`, or `hmac-sha256` |
| `DefaultPromotionLinkType` | Default promotion link type |
| `DefaultShipToCountry` | Default ship-to country code |
| `DefaultTargetCurrency` | Default target currency |
| `DefaultTargetLanguage` | Default target language |
| `TimeoutMilliseconds` | Request timeout |

Per-call behavior such as `IncludeProductDetails` belongs to `AliExpressAffiliateLinkRequest`, not global options.

You can load options from environment-style dictionaries:

```csharp
var options = AliExpressAffiliateEnvironment.FromDictionary(new Dictionary<string, string>
{
    ["ALIEXPRESS_AFFILIATE_APP_KEY"] = "<your-app-key>",
    ["ALIEXPRESS_AFFILIATE_APP_SECRET"] = "<your-app-secret>",
    ["ALIEXPRESS_TRACKING_ID"] = "<your-tracking-id>",
    ["ALIEXPRESS_TARGET_CURRENCY"] = "BRL",
    ["ALIEXPRESS_TARGET_LANGUAGE"] = "PT",
    ["ALIEXPRESS_SHIP_TO_COUNTRY"] = "BR"
});
```

## Diagnostics

The main client exposes only SDK operations. Lower-level Open Platform helpers are available through `AliExpressOpenPlatformDiagnostics`:

```csharp
var signature = AliExpressOpenPlatformDiagnostics.CreateTopSignature(
    parameters,
    appSecret: "<secret>",
    signMethod: "md5");

var normalizedUrl = AliExpressOpenPlatformDiagnostics.NormalizeAliExpressUrl(productUrl);
```

## Error Handling

All SDK-specific exceptions derive from `AliExpressAffiliateException`.

| Exception | When it is thrown |
| --- | --- |
| `AliExpressAffiliateHttpException` | AliExpress returns a non-success HTTP status |
| `AliExpressAffiliateApiException` | AliExpress returns an API/business error payload |
| `AliExpressAffiliateValidationException` | Required SDK options are missing |
| `AliExpressAffiliateLinkUnavailableException` | Link generation succeeds but no `promotion_link` is returned |

```csharp
try
{
    var result = await client.GenerateAffiliateLinkAsync(request, options);
}
catch (AliExpressAffiliateLinkUnavailableException ex)
{
    Console.WriteLine(ex.ResponseSummary);
}
catch (AliExpressAffiliateException ex)
{
    Console.WriteLine(ex.Message);
}
```

## Project Structure

```text
src/AliExpress.Affiliate/
  Application/      Use cases, request models, and application ports
  Clients/          Public client interface and implementation
  Configuration/    Options and environment/configuration helpers
  DependencyInjection/
                    Microsoft.Extensions.DependencyInjection registration
  Domain/           Product URL, product ID, price, result, and order models
  Exceptions/       SDK exception hierarchy
  Infrastructure/   AliExpress Open Platform HTTP, signing, request, and parsing code
  OpenPlatform/     Public diagnostic helpers for signing, parsing, and request inspection
```

## Development

```bash
dotnet restore
dotnet build
dotnet test
dotnet pack src/AliExpress.Affiliate/AliExpress.Affiliate.csproj -c Release -o artifacts
```

## Official API Smoke Tests

Integration tests are skipped unless credentials and test products are configured:

```bash
ALIEXPRESS_AFFILIATE_APP_KEY=<your-app-key>
ALIEXPRESS_AFFILIATE_APP_SECRET=<your-app-secret>
ALIEXPRESS_TRACKING_ID=<your-tracking-id>
ALIEXPRESS_AFFILIATE_TEST_PRODUCT_ID_OR_URL=<product-id-or-url>
ALIEXPRESS_AFFILIATE_TEST_PRODUCT_URL=<affiliate-enabled-product-url>
```

Run them with:

```bash
dotnet test --filter Category=Integration
```

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
