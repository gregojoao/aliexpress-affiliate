# AliExpress.Affiliate

> A lightweight, strongly typed .NET SDK for the AliExpress Open Platform affiliate APIs.

[![CI](https://github.com/gregojoao/aliexpress-affiliate/actions/workflows/ci.yml/badge.svg)](https://github.com/gregojoao/aliexpress-affiliate/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/AliExpress.Affiliate.svg)](https://www.nuget.org/packages/AliExpress.Affiliate)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/)

`AliExpress.Affiliate` helps .NET applications generate affiliate links, discover products, fetch product details, read promotions, query orders, and sign TOP-style AliExpress Open Platform requests.

It also handles a common edge case from the affiliate API: successful responses that return only `source_value` instead of a usable `promotion_link`.

## Highlights

- Generate single or batch affiliate links with `aliexpress.affiliate.link.generate`
- Fetch product details with `aliexpress.affiliate.productdetail.get`
- Search products with `aliexpress.affiliate.product.query`
- Fetch hot products, hot product downloads, categories, featured promotions, and smart match recommendations
- Query affiliate orders by page, by order IDs, or by query index
- Sign Open Platform requests with `md5`, `hmac-md5`, or `hmac-sha256`
- Normalize AliExpress product URLs by trimming tracking parameters after `.html`
- Parse title, current price, original price, image URL, product URL, and promotion link
- Detect non-affiliate products with a dedicated exception
- Load options from dictionaries or configuration-style environment values

## Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Dependency Injection](#dependency-injection)
- [API Examples](#api-examples)
- [Configuration](#configuration)
- [Error Handling](#error-handling)
- [Project Structure](#project-structure)
- [Development](#development)
- [Official API Smoke Tests](#official-api-smoke-tests)
- [Contributing](#contributing)
- [License](#license)

## Requirements

| Requirement | Version / Notes |
| --- | --- |
| .NET SDK | `10.0` or newer |
| AliExpress credentials | Open Platform app key and app secret |
| Affiliate tracking | AliExpress affiliate tracking ID |

## Installation

Install from NuGet:

```bash
dotnet add package AliExpress.Affiliate
```

Or pack the project locally:

```bash
dotnet pack src/AliExpress.Affiliate/AliExpress.Affiliate.csproj -c Release -o artifacts
```

Then install from the local package source as needed:

```bash
dotnet add <consumer-project>.csproj package AliExpress.Affiliate --source ./artifacts
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

## Dependency Injection

DI and `IConfiguration` support are optional. You can keep creating `AliExpressAffiliateClient` manually and pass `AliExpressAffiliateOptions` per call, or register default options once and use shorter method overloads.

Register with `appsettings.json`:

```json
{
  "AliExpress": {
    "Affiliate": {
      "AppKey": "<your-app-key>",
      "AppSecret": "<your-app-secret>",
      "TrackingId": "<your-tracking-id>",
      "SignMethod": "md5",
      "PromotionLinkType": "0",
      "TargetCurrency": "BRL",
      "TargetLanguage": "PT",
      "ShipToCountry": "BR",
      "IncludeProductDetails": true
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
    options.TrackingId = "<your-tracking-id>";
    options.TargetCurrency = "BRL";
    options.TargetLanguage = "PT";
    options.ShipToCountry = "BR";
});
```

Then inject the client:

```csharp
public sealed class ProductLinkService
{
    private readonly AliExpressAffiliateClient _client;

    public ProductLinkService(AliExpressAffiliateClient client)
    {
        _client = client;
    }

    public Task<AliExpressAffiliateLinkResult?> CreateLinkAsync(
        string productUrl,
        CancellationToken cancellationToken = default)
    {
        return _client.GenerateAffiliateLinkAsync(productUrl, cancellationToken);
    }
}
```

If no default options were registered, use the existing overloads that receive `AliExpressAffiliateOptions` explicitly.

## API Examples

### Product Details

Use `GetProductDetailsAsync` when you only need product metadata. The method accepts either a product ID or an AliExpress product URL.

```csharp
var details = await client.GetProductDetailsAsync(
    "1005006356702381",
    options);

Console.WriteLine(details?.ProductTitle);
Console.WriteLine(details?.ProductPrice);
Console.WriteLine(details?.ProductOriginalPrice);
```

### Product Discovery

Search regular products:

```csharp
var products = await client.SearchProductsAsync(
    new AliExpressProductQuery
    {
        Keywords = "microfone",
        PageNo = 1,
        PageSize = 20,
        Sort = "SALE_PRICE_ASC"
    },
    options);
```

Fetch hot products:

```csharp
var hotProducts = await client.GetHotProductsAsync(
    new AliExpressProductQuery
    {
        Keywords = "fone bluetooth",
        PageSize = 20
    },
    options);
```

Fetch affiliate categories:

```csharp
var categories = await client.GetCategoriesAsync(options);
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
        StartTime = "2026-05-01 00:00:00",
        EndTime = "2026-05-02 00:00:00",
        PageNo = 1,
        PageSize = 20
    },
    options);
```

### Main Client Methods

| Area | Methods |
| --- | --- |
| Affiliate links | `GenerateAffiliateLinkAsync`, `GenerateAffiliateLinksAsync` |
| Products | `GetProductDetailsAsync`, `SearchProductsAsync`, `GetHotProductsAsync`, `GetHotProductDownloadAsync`, `GetSmartMatchProductsAsync` |
| Categories | `GetCategoriesAsync` |
| Promotions | `GetFeaturedPromosAsync`, `GetFeaturedPromoProductsAsync` |
| Orders | `GetOrdersAsync`, `GetOrderDetailsAsync`, `GetOrdersByIndexAsync` |

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

### Supported Configuration Keys

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

This is different from HTTP, signature, or configuration failures, so callers can route the item to a dedicated retry, fallback, or review flow.

## Project Structure

The library follows a lightweight DDD-style organization:

```text
src/AliExpress.Affiliate/
  Application/      Use cases, request models, and application ports
  Domain/           Product URL, product ID, price, result, and order rules
  Infrastructure/   AliExpress Open Platform HTTP, signing, request, and parsing code
```

## Development

Restore, build, test, and pack locally:

```bash
dotnet restore
dotnet build
dotnet test
dotnet pack src/AliExpress.Affiliate/AliExpress.Affiliate.csproj -c Release -o artifacts
```

The repository includes a GitHub Actions workflow that builds, tests, packs, and uploads the generated NuGet package as a workflow artifact.

## Official API Smoke Tests

The test project includes opt-in integration tests that call the official AliExpress API. They are skipped unless credentials and test products are configured through environment variables:

```bash
ALIEXPRESS_AFFILIATE_APP_KEY=<your-app-key>
ALIEXPRESS_AFFILIATE_APP_SECRET=<your-app-secret>
ALIEXPRESS_TRACKING_ID=<your-tracking-id>
ALIEXPRESS_AFFILIATE_TEST_PRODUCT_ID_OR_URL=<product-id-or-url>
ALIEXPRESS_AFFILIATE_TEST_PRODUCT_URL=<affiliate-enabled-product-url>
```

Run only the official API tests with:

```bash
dotnet test --filter Category=Integration
```

## Contributing

Issues and pull requests are welcome. Please keep changes focused, add or update tests when behavior changes, and run `dotnet test` before opening a pull request.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
