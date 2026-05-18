using AliExpress.Affiliate.Reports.Application;
using AliExpress.Affiliate.Reports.Application.Requests;
using AliExpress.Affiliate.Reports.Clients;
using AliExpress.Affiliate.Reports.Configuration;
using FluentAssertions;
using System.Globalization;
using Xunit.Abstractions;

namespace AliExpress.Affiliate.Tests.Integration;

/// <summary>
/// Smoke tests against the real AliExpress Open Platform reports API.
/// Skipped automatically when the required environment variables are missing.
/// Required:
///   ALIEXPRESS_AFFILIATE_APP_KEY
///   ALIEXPRESS_AFFILIATE_APP_SECRET
/// Optional:
///   ALIEXPRESS_TRACKING_ID              (filters by PID; recommended)
///   ALIEXPRESS_AFFILIATE_ACCESS_TOKEN   (only if your TOP scope requires OAuth)
///   ALIEXPRESS_AFFILIATE_REPORTS_FROM   (ISO 8601 UTC; defaults to UtcNow - 7d)
///   ALIEXPRESS_AFFILIATE_REPORTS_TO     (ISO 8601 UTC; defaults to UtcNow)
///   ALIEXPRESS_AFFILIATE_REPORTS_ORDER_ID (enables the GetConversionAsync test)
/// </summary>
public class AliExpressAffiliateReportsOfficialApiTests
{
    private readonly ITestOutputHelper _output;

    public AliExpressAffiliateReportsOfficialApiTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ListConversionsAsync_WithOfficialApi_ShouldReturnPageWithoutErrors()
    {
        if (!TryCreateOptions(out var options))
        {
            return;
        }

        var (from, to) = GetWindow();
        _output.WriteLine($"Window: {from:O} -> {to:O}");
        _output.WriteLine($"AppKey: {Mask(options.AppKey)}  TrackingId: {options.TrackingId ?? "<none>"}");

        var client = BuildClient(options);
        var page = await client.ListConversionsAsync(new ListConversionsRequest(
            From: from,
            To: to,
            Status: ConversionStatusFilter.All,
            Page: 1,
            PageSize: 20));

        _output.WriteLine($"Page={page.Page}  Items={page.Items.Count}  TotalCount={page.TotalCount}  HasMore={page.HasMore}");
        foreach (var conversion in page.Items.Take(5))
        {
            _output.WriteLine(
                $"  - {conversion.OrderId} [{conversion.Status}] {conversion.ProductTitle} " +
                $"qty={conversion.Quantity} sale={conversion.TotalSale} commission={conversion.Commission} " +
                $"purchased={conversion.PurchaseTime:O}");
        }

        if (page.Items.Count > 0)
        {
            _output.WriteLine("--- Per-item parent/sub/order ids (for cart grouping diagnostics) ---");
            foreach (var conversion in page.Items)
            {
                using var doc = System.Text.Json.JsonDocument.Parse(conversion.RawJson ?? "{}");
                var root = doc.RootElement;
                var parent = root.TryGetProperty("parent_order_number", out var p) ? p.GetRawText() : "?";
                var order = root.TryGetProperty("order_id", out var o) ? o.GetRawText() : "?";
                var sub = root.TryGetProperty("sub_order_id", out var s) ? s.GetRawText() : "?";
                var title = conversion.ProductTitle?.Substring(0, Math.Min(40, conversion.ProductTitle.Length));
                _output.WriteLine($"  parent={parent}  order={order}  sub={sub}  product={title}");
            }
            _output.WriteLine("--- End ---");
        }

        page.Items.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetSalesSummaryAsync_WithOfficialApi_ShouldReturnAggregateWithoutErrors()
    {
        if (!TryCreateOptions(out var options))
        {
            return;
        }

        var (from, to) = GetWindow();
        _output.WriteLine($"Summary window: {from:O} -> {to:O}");

        var client = BuildClient(options);
        var summary = await client.GetSalesSummaryAsync(new SalesSummaryRequest(from, to));

        _output.WriteLine(
            $"Conversions={summary.Conversions}  GrossRevenue={summary.GrossRevenue}  " +
            $"Commission={summary.Commission}  AvgRate={summary.AvgCommissionRate:P2}  " +
            $"Supported={summary.Supported}");
        foreach (var bucket in summary.ByStatus)
        {
            _output.WriteLine($"  status[{bucket.Key}] = {bucket.Value}");
        }
        foreach (var product in summary.TopProducts.Take(3))
        {
            _output.WriteLine($"  top product: {product.ProductId} {product.ProductTitle} commission={product.Commission} conversions={product.Conversions}");
        }
        foreach (var subId in summary.TopSubIds.Take(3))
        {
            _output.WriteLine($"  top sub-id: {subId.SubId} commission={subId.Commission} conversions={subId.Conversions}");
        }

        summary.Should().NotBeNull();
        summary.Supported.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetConversionAsync_WithOfficialApi_WhenOrderIdProvided_ShouldReturnDetail()
    {
        if (!TryCreateOptions(out var options))
        {
            return;
        }

        var orderId = Environment.GetEnvironmentVariable("ALIEXPRESS_AFFILIATE_REPORTS_ORDER_ID")?.Trim();
        if (string.IsNullOrWhiteSpace(orderId))
        {
            _output.WriteLine("Set ALIEXPRESS_AFFILIATE_REPORTS_ORDER_ID to enable GetConversionAsync integration test.");
            return;
        }

        var client = BuildClient(options);
        var detail = await client.GetConversionAsync(orderId);

        _output.WriteLine(
            $"Order {detail.OrderId} status={detail.Status} commission={detail.Commission} " +
            $"sale={detail.TotalSale} lines={detail.Lines.Count}");
        foreach (var line in detail.Lines)
        {
            _output.WriteLine($"  line: {line.ProductId} qty={line.Quantity} sale={line.TotalSale} commission={line.Commission}");
        }

        detail.OrderId.Should().NotBeNullOrWhiteSpace();
    }

    private bool TryCreateOptions(out AliExpressAffiliateReportsOptions options)
    {
        var appKey = Environment.GetEnvironmentVariable("ALIEXPRESS_AFFILIATE_APP_KEY")?.Trim();
        var appSecret = Environment.GetEnvironmentVariable("ALIEXPRESS_AFFILIATE_APP_SECRET")?.Trim();
        var trackingId = Environment.GetEnvironmentVariable("ALIEXPRESS_TRACKING_ID")?.Trim();
        var accessToken = Environment.GetEnvironmentVariable("ALIEXPRESS_AFFILIATE_ACCESS_TOKEN")?.Trim();

        if (string.IsNullOrWhiteSpace(appKey) || string.IsNullOrWhiteSpace(appSecret))
        {
            _output.WriteLine(
                "Set ALIEXPRESS_AFFILIATE_APP_KEY and ALIEXPRESS_AFFILIATE_APP_SECRET to enable AliExpress Affiliate Reports integration tests.");
            options = null!;
            return false;
        }

        options = new AliExpressAffiliateReportsOptions
        {
            AppKey = appKey,
            AppSecret = appSecret,
            TrackingId = string.IsNullOrWhiteSpace(trackingId) ? null : trackingId,
            AccessToken = string.IsNullOrWhiteSpace(accessToken) ? null : accessToken,
            Endpoint = AliExpressAffiliateReportsOptions.DefaultEndpoint,
            SignMethod = AliExpressAffiliateReportsOptions.DefaultSignMethod,
            Timeout = TimeSpan.FromSeconds(30)
        };
        return true;
    }

    private (DateTimeOffset From, DateTimeOffset To) GetWindow()
    {
        var fromRaw = Environment.GetEnvironmentVariable("ALIEXPRESS_AFFILIATE_REPORTS_FROM");
        var toRaw = Environment.GetEnvironmentVariable("ALIEXPRESS_AFFILIATE_REPORTS_TO");
        var to = TryParse(toRaw) ?? DateTimeOffset.UtcNow;
        var from = TryParse(fromRaw) ?? to.AddDays(-7);
        return (from, to);
    }

    private static DateTimeOffset? TryParse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(
                raw,
                CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal,
                out var parsed))
        {
            return parsed.ToUniversalTime();
        }

        return null;
    }

    private static AliExpressAffiliateReportsClient BuildClient(AliExpressAffiliateReportsOptions options)
    {
        return new AliExpressAffiliateReportsClient(
            new HttpClient(),
            options,
            clock: () => DateTimeOffset.UtcNow,
            logger: null);
    }

    private static string Mask(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "<empty>";
        }

        if (value.Length <= 4)
        {
            return new string('*', value.Length);
        }

        return $"{value[0]}***{value[^1]} (len={value.Length})";
    }
}
