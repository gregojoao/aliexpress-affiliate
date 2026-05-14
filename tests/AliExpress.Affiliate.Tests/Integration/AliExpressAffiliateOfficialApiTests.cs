using AliExpress.Affiliate.Application.Requests;
using AliExpress.Affiliate.Clients;
using AliExpress.Affiliate.Configuration;
using FluentAssertions;
using Xunit.Abstractions;

namespace AliExpress.Affiliate.Tests.Integration;

public class AliExpressAffiliateOfficialApiTests
{
    private readonly ITestOutputHelper _output;

    public AliExpressAffiliateOfficialApiTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetProductDetailsAsync_WithOfficialApi_ShouldReturnProductDetails()
    {
        if (!TryCreateOfficialOptions(out var options) ||
            !TryGetValue(
            "ALIEXPRESS_AFFILIATE_TEST_PRODUCT_ID_OR_URL",
            "Set ALIEXPRESS_AFFILIATE_TEST_PRODUCT_ID_OR_URL to a product ID or URL that is valid for your AliExpress account.",
            out var productIdOrUrl))
        {
            return;
        }

        using var httpClient = new HttpClient();
        var client = new AliExpressAffiliateClient(httpClient);

        var details = await client.GetProductDetailsAsync(productIdOrUrl, options);

        details.Should().NotBeNull();
        details!.ProductTitle.Should().NotBeNullOrWhiteSpace();
        details.ProductUrl.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GenerateAffiliateLinkAsync_WithOfficialApi_ShouldReturnPromotionLink()
    {
        if (!TryCreateOfficialOptions(out var options, includeProductDetails: true) ||
            !TryGetValue(
            "ALIEXPRESS_AFFILIATE_TEST_PRODUCT_URL",
            "Set ALIEXPRESS_AFFILIATE_TEST_PRODUCT_URL to a product URL that can generate an affiliate link for your account.",
            out var productUrl))
        {
            return;
        }

        using var httpClient = new HttpClient();
        var client = new AliExpressAffiliateClient(httpClient);

        var result = await client.GenerateAffiliateLinkAsync(
            new AliExpressAffiliateLinkRequest
            {
                ProductUrl = productUrl,
                IncludeProductDetails = true
            },
            options);

        result.Should().NotBeNull();
        result!.AffiliateUrl.Should().NotBeNullOrWhiteSpace();
        result.AffiliateUrl.Should().StartWith("http");
        result.SourceUrl.Should().Contain(".html");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ProductDiscoveryMethods_WithOfficialApi_ShouldReturnWithoutApiErrors()
    {
        if (!TryCreateOfficialOptions(out var options))
        {
            return;
        }

        using var httpClient = new HttpClient();
        var client = new AliExpressAffiliateClient(httpClient);

        var categories = await client.GetCategoriesAsync(options);
        var products = await client.SearchProductsAsync(
            new AliExpressProductQuery
            {
                Keywords = "microfone",
                PageNumber = 1,
                PageSize = 5
            },
            options);
        var hotProducts = await client.GetHotProductsAsync(
            new AliExpressProductQuery
            {
                Keywords = "fone bluetooth",
                PageNumber = 1,
                PageSize = 5
            },
            options);

        categories.Items.Should().NotBeNull();
        products.Items.Should().NotBeNull();
        hotProducts.Items.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PromotionMethods_WithOfficialApi_ShouldReturnWithoutApiErrors()
    {
        if (!TryCreateOfficialOptions(out var options))
        {
            return;
        }

        using var httpClient = new HttpClient();
        var client = new AliExpressAffiliateClient(httpClient);

        var promos = await client.GetFeaturedPromosAsync(options);
        var hotDownload = await client.GetHotProductDownloadAsync(
            new AliExpressHotProductDownloadQuery
            {
                CategoryId = "7",
                PageNumber = 1,
                PageSize = 5
            },
            options);

        promos.Items.Should().NotBeNull();
        hotDownload.Items.Should().NotBeNull();
    }

    private bool TryCreateOfficialOptions(
        out AliExpressAffiliateOptions officialOptions,
        bool includeProductDetails = false)
    {
        var options = AliExpressAffiliateEnvironment.FromDictionary(
            Environment.GetEnvironmentVariables()
                .Cast<System.Collections.DictionaryEntry>()
                .ToDictionary(
                    entry => entry.Key?.ToString() ?? string.Empty,
                    entry => entry.Value?.ToString() ?? string.Empty));

        if (string.IsNullOrWhiteSpace(options.AppKey) ||
            string.IsNullOrWhiteSpace(options.AppSecret) ||
            string.IsNullOrWhiteSpace(options.DefaultTrackingId))
        {
            _output.WriteLine(
                "Official AliExpress API tests require ALIEXPRESS_AFFILIATE_APP_KEY, " +
                "ALIEXPRESS_AFFILIATE_APP_SECRET and ALIEXPRESS_TRACKING_ID.");
            officialOptions = options;
            return false;
        }

        officialOptions = new AliExpressAffiliateOptions
        {
            ApiEndpoint = options.ApiEndpoint,
            AppKey = options.AppKey,
            AppSecret = options.AppSecret,
            DefaultTrackingId = options.DefaultTrackingId,
            AppSignature = options.AppSignature,
            SignMethod = options.SignMethod,
            DefaultPromotionLinkType = options.DefaultPromotionLinkType,
            DefaultShipToCountry = options.DefaultShipToCountry,
            DefaultTargetCurrency = options.DefaultTargetCurrency,
            DefaultTargetLanguage = options.DefaultTargetLanguage,
            TimeoutMilliseconds = options.TimeoutMilliseconds
        };

        return true;
    }

    private bool TryGetValue(
        string key,
        string skipReason,
        out string value)
    {
        value = Environment.GetEnvironmentVariable(key)?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            _output.WriteLine(skipReason);
            return false;
        }

        return true;
    }
}
