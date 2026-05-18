# AliExpress.Affiliate

> A lightweight, strongly typed .NET SDK for AliExpress Open Platform affiliate APIs.

[![CI](https://github.com/gregojoao/aliexpress-affiliate/actions/workflows/ci.yml/badge.svg)](https://github.com/gregojoao/aliexpress-affiliate/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/AliExpress.Affiliate.svg)](https://www.nuget.org/packages/AliExpress.Affiliate)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8%20%7C%2010-512BD4.svg)](https://dotnet.microsoft.com/)

Small .NET SDK for AliExpress Affiliate Open API workflows.

Maintained by **Greco Labs**.

`AliExpress.Affiliate` helps .NET applications generate affiliate links, discover products, fetch product details, read promotions, query orders, and handle AliExpress affiliate API responses with typed contracts.

It also handles a common edge case from the affiliate API: successful responses that return only `source_value` instead of a usable `promotion_link`.

## Highlights

- Generate single or batch affiliate links with explicit request objects
- Fetch product details, product discovery results, categories, featured promotions, and orders
- Read affiliate **reports** (conversions, sales summary) through `IAliExpressAffiliateReportsClient`
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

## Affiliate Reports

Sales/conversion data is exposed by a separate, dashboard-friendly client:
`AliExpressAffiliateReportsClient` (interface `IAliExpressAffiliateReportsClient`).
It only consumes the **official AliExpress Open Platform (TOP) gateway** — no portal scraping.

Endpoints consumed today:

| SDK method | TOP method |
| --- | --- |
| `ListConversionsAsync` / `GetSalesSummaryAsync` | `aliexpress.affiliate.order.list` |
| `GetConversionAsync` | `aliexpress.affiliate.order.get` |

`GetClickStatsAsync` and `GetGeneratedLinkUsageAsync` are included for parity with
non-AliExpress affiliate dashboards. AliExpress does not expose click metrics or
generated-link attribution through TOP, so both return `Supported = false` with a
human-readable `UnsupportedReason`. Treat that as a "manual import available"
signal in your dashboard.

### Registration

The reports client is wire-compatible with the existing link-generation pattern.
Register it once and resolve `IAliExpressAffiliateReportsClient`:

```csharp
using AliExpress.Affiliate.Reports.Clients;
using AliExpress.Affiliate.Reports.DependencyInjection;

services.AddAliExpressAffiliateReports(builder.Configuration); // binds AliExpress:Affiliate:Reports
```

```json
{
  "AliExpress": {
    "Affiliate": {
      "Reports": {
        "AppKey": "<your-app-key>",
        "AppSecret": "<your-app-secret>",
        "TrackingId": "<your-tracking-id>",
        "Endpoint": "https://api-sg.aliexpress.com/sync",
        "SignMethod": "sha256",
        "Timeout": "00:00:30"
      }
    }
  }
}
```

If you prefer manual wiring (matching the existing `IHttpClientFactory` setup):

```csharp
services.AddHttpClient("AliExpressAffiliateReportsSdk", c => c.Timeout = Timeout.InfiniteTimeSpan);
services.AddSingleton(sp =>
    new AliExpressAffiliateReportsClient(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient("AliExpressAffiliateReportsSdk"),
        new AliExpressAffiliateReportsOptions
        {
            AppKey = "<key>",
            AppSecret = "<secret>",
            TrackingId = "<tracking>"
        },
        clock: () => DateTimeOffset.UtcNow));
```

### Usage

```csharp
using AliExpress.Affiliate.Reports.Application;
using AliExpress.Affiliate.Reports.Application.Requests;

// Last 7 days (rolling)
var page = await reports.ListConversionsAsync(new ListConversionsRequest(
    From: DateTimeOffset.UtcNow.AddDays(-7),
    To:   DateTimeOffset.UtcNow,
    Status: ConversionStatusFilter.Paid,
    Page: 1,
    PageSize: 50));

foreach (var conversion in page.Items)
{
    Console.WriteLine($"{conversion.OrderId} {conversion.Status} {conversion.Commission}");
}

// Fixed monthly window — useful for reports
var summary = await reports.GetSalesSummaryAsync(new SalesSummaryRequest(
    From: new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
    To:   new DateTimeOffset(2026, 5, 31, 23, 59, 59, TimeSpan.Zero)));

Console.WriteLine($"Conversions: {summary.Conversions}");
Console.WriteLine($"Gross: {summary.GrossRevenue} | Commission: {summary.Commission}");

// Empty window — returns Conversions=0, no exception
var emptySummary = await reports.GetSalesSummaryAsync(new SalesSummaryRequest(
    From: new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
    To:   new DateTimeOffset(2020, 1, 2, 0, 0, 0, TimeSpan.Zero)));
```

`From` / `To` accept any `DateTimeOffset` — UTC, local, or with an explicit offset. See [Time windows](#time-windows) below for the practical caps.

### Methods and exceptions

| Method | TOP endpoint | Throws |
| --- | --- | --- |
| `ListConversionsAsync` | `aliexpress.affiliate.order.list` | `AliExpressAffiliateAuthException`, `AliExpressAffiliateRateLimitException`, `AliExpressAffiliateUnsupportedException`, `AliExpressAffiliateApiException` (with `Code` / `SubCode` / `RequestId`) |
| `GetConversionAsync` | `aliexpress.affiliate.order.get` | Above + `AliExpressAffiliateNotFoundException` |
| `GetSalesSummaryAsync` | aggregates `aliexpress.affiliate.order.list` | Same as `ListConversionsAsync` |
| `GetClickStatsAsync` | — (returns `Supported=false`) | — |
| `GetGeneratedLinkUsageAsync` | — (returns `Supported=false`) | — |

Transport behavior:

- One automatic retry for HTTP 5xx and timeouts. 4xx and TOP business errors are surfaced immediately.
- Per-call timeout (default 30 s) layered on top of the injected `HttpClient`; the
  HttpClient itself can keep `Timeout.InfiniteTimeSpan`.
- `AppSecret` and `AccessToken` are never logged. App key is masked (`5***0`) in
  diagnostic logs.

### Time windows

`From` / `To` accept any `DateTimeOffset`. The SDK converts them to **GMT+8** (the timezone the AliExpress TOP gateway evaluates on) before signing the request, so callers can keep working in UTC. There is no fixed maximum window — AliExpress paginates server-side — but practical limits are:

- Page size capped at 50 records per `aliexpress.affiliate.order.list` call. Requests above 50 are clamped.
- `GetSalesSummaryAsync` walks up to 40 pages × 50 = 2.000 conversions per window before stopping. For larger windows, paginate `ListConversionsAsync` yourself or shrink the window.
- Very large windows hit the per-app QPS limit faster (each request burns one token).

### Known AliExpress quirks

- The TOP gateway enforces per-app QPS limits. Rate-limit responses surface as `AliExpressAffiliateRateLimitException` with the original `Code` and `RequestId` for support tickets.
- `aliexpress.affiliate.order.list` requires a non-empty `status` parameter — there is no wildcard. `ConversionStatusFilter.All` (and `null`) maps to `"Payment Completed"`, which is the most useful single signal for dashboards. To see other statuses, pass them explicitly (`Pending`, `Confirmed`, `Cancelled`, `Invalid`).
- AliExpress signals "no conversions in this window" with `resp_code = 405` ("The result is empty") instead of an empty array. The SDK translates that into a zero-item `AliExpressConversionPage` / zero-valued `AliExpressSalesSummary` — callers never see an exception for an empty window.
- Monetary fields (`paid_amount`, `estimated_paid_commission`, etc.) are emitted as integer JSON numbers in the **smallest currency unit** (e.g. `1113` for `R$11,13`). The SDK divides integer numerics by 100; decimal numerics and strings (`"11.13"`) are taken as-is. Assumes a 2-decimal-place currency — applies to all currencies AliExpress affiliate accounts settle in today (BRL / USD / EUR / CNY).
- The `Currency` field on every `AliExpressConversion` reflects whatever AliExpress reported in `settled_currency`. AliExpress typically settles affiliate commissions in **USD**, even for Brazilian accounts whose buyers paid in BRL — the BRL amount is visible in the embedded `product_detail_url` query string but isn't part of the settlement record. The SDK never converts currencies.

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
| `AliExpressAffiliateApiException` | AliExpress returns an API/business error payload (exposes `Code`, `SubCode`, `RequestId`) |
| `AliExpressAffiliateValidationException` | Required SDK options are missing |
| `AliExpressAffiliateLinkUnavailableException` | Link generation succeeds but no `promotion_link` is returned |
| `AliExpressAffiliateAuthException` *(Reports)* | TOP rejected the call for signature/credential reasons |
| `AliExpressAffiliateRateLimitException` *(Reports)* | TOP signaled rate-limiting (HTTP 429 or `isv.api-flow-limit`) |
| `AliExpressAffiliateNotFoundException` *(Reports)* | TOP could not find the requested resource (e.g. order id) |
| `AliExpressAffiliateUnsupportedException` *(Reports)* | App is not granted the requested TOP endpoint |

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
  Reports/          Reporting client (conversions, sales summaries) — Application/Clients/
                    Configuration/DependencyInjection/Domain/Exceptions/Infrastructure
```

## Development

```bash
dotnet restore
dotnet build
dotnet test
dotnet pack src/AliExpress.Affiliate/AliExpress.Affiliate.csproj -c Release -o artifacts
```

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
