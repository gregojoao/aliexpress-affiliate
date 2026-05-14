using AliExpress.Affiliate.Configuration;
using AliExpress.Affiliate.Exceptions;
using FluentAssertions;

namespace AliExpress.Affiliate.Tests.Configuration;

public class AliExpressAffiliateOptionsTests
{
    [Fact]
    public void Validate_WithMissingAppKey_ShouldThrowValidationException()
    {
        var options = CreateValidOptions();
        options.AppKey = string.Empty;

        var act = options.Validate;

        act.Should().Throw<AliExpressAffiliateValidationException>()
            .WithMessage("*AppKey*");
    }

    [Fact]
    public void Validate_WithMissingAppSecret_ShouldThrowValidationException()
    {
        var options = CreateValidOptions();
        options.AppSecret = string.Empty;

        var act = options.Validate;

        act.Should().Throw<AliExpressAffiliateValidationException>()
            .WithMessage("*AppSecret*");
    }

    [Fact]
    public void Validate_WithMissingDefaultTrackingId_ShouldThrowValidationException()
    {
        var options = CreateValidOptions();
        options.DefaultTrackingId = string.Empty;

        var act = options.Validate;

        act.Should().Throw<AliExpressAffiliateValidationException>()
            .WithMessage("*DefaultTrackingId*");
    }

    [Fact]
    public void FromDictionary_ShouldReadKnownEnvironmentKeysCaseInsensitively()
    {
        var options = AliExpressAffiliateEnvironment.FromDictionary(
            new Dictionary<string, string>
            {
                ["aliexpress_affiliate_api_endpoint"] = "https://example.test/sync",
                ["ALIEXPRESS_AFFILIATE_APP_KEY"] = "app-key",
                ["ALIEXPRESS_AFFILIATE_APP_SECRET"] = "app-secret",
                ["ALIEXPRESS_TRACKING_ID"] = "tracking",
                ["ALIEXPRESS_SIGN_METHOD"] = "hmac-sha256",
                ["ALIEXPRESS_PROMOTION_LINK_TYPE"] = "2",
                ["ALIEXPRESS_SHIP_TO_COUNTRY"] = "BR",
                ["ALIEXPRESS_TARGET_CURRENCY"] = "BRL",
                ["ALIEXPRESS_TARGET_LANGUAGE"] = "PT",
                ["ALIEXPRESS_AFFILIATE_API_TIMEOUT_MS"] = "1234"
            });

        options.ApiEndpoint.Should().Be("https://example.test/sync");
        options.AppKey.Should().Be("app-key");
        options.AppSecret.Should().Be("app-secret");
        options.DefaultTrackingId.Should().Be("tracking");
        options.SignMethod.Should().Be("hmac-sha256");
        options.DefaultPromotionLinkType.Should().Be("2");
        options.DefaultShipToCountry.Should().Be("BR");
        options.DefaultTargetCurrency.Should().Be("BRL");
        options.DefaultTargetLanguage.Should().Be("PT");
        options.TimeoutMilliseconds.Should().Be(1234);
    }

    [Fact]
    public void FromDictionary_WithInvalidTimeout_ShouldUseDefaultTimeout()
    {
        var options = AliExpressAffiliateEnvironment.FromDictionary(
            new Dictionary<string, string>
            {
                ["ALIEXPRESS_AFFILIATE_API_TIMEOUT_MS"] = "-1"
            });

        options.TimeoutMilliseconds.Should().Be(AliExpressAffiliateOptions.DefaultTimeoutMilliseconds);
    }

    private static AliExpressAffiliateOptions CreateValidOptions()
    {
        return new AliExpressAffiliateOptions
        {
            AppKey = "app-key",
            AppSecret = "app-secret",
            DefaultTrackingId = "tracking"
        };
    }
}
