using AliExpress.Affiliate.Application.Requests;
using AliExpress.Affiliate.Configuration;
using AliExpress.Affiliate.Exceptions;
using AliExpress.Affiliate.OpenPlatform;
using FluentAssertions;

namespace AliExpress.Affiliate.Tests.OpenPlatform;

public class AliExpressOpenPlatformDiagnosticsTests
{
    [Fact]
    public void BuildLinkGenerateRequest_WithPerRequestOverrides_ShouldPreferRequestValues()
    {
        var request = AliExpressOpenPlatformDiagnostics.BuildLinkGenerateRequest(
            "https://pt.aliexpress.com/item/1005006860981590.html",
            new AliExpressAffiliateLinkRequest
            {
                TrackingId = "request-tracking",
                PromotionLinkType = "2"
            },
            CreateOptions(),
            DateTimeOffset.FromUnixTimeMilliseconds(1778688000000));

        request.BodyParameters.Should().Contain("tracking_id", "request-tracking");
        request.BodyParameters.Should().Contain("promotion_link_type", "2");
    }

    [Theory]
    [InlineData("md5")]
    [InlineData("hmac-md5")]
    [InlineData("hmac-sha256")]
    public void CreateTopSignature_WithSupportedMethods_ShouldReturnUppercaseSignature(string signMethod)
    {
        var signature = AliExpressOpenPlatformDiagnostics.CreateTopSignature(
            new Dictionary<string, string>
            {
                ["app_key"] = "123456",
                ["method"] = "aliexpress.affiliate.link.generate",
                ["timestamp"] = "1778688000000"
            },
            "secret",
            signMethod);

        signature.Should().NotBeNullOrWhiteSpace();
        signature.Should().Be(signature.ToUpperInvariant());
    }

    [Fact]
    public void ExtractAffiliateUrl_WithTopLevelApiError_ShouldThrowApiException()
    {
        const string responseBody = """
        {
          "error_response": {
            "code": 15,
            "msg": "Remote service error"
          }
        }
        """;

        var act = () => AliExpressOpenPlatformDiagnostics.ExtractAffiliateUrl(responseBody);

        act.Should().Throw<AliExpressAffiliateApiException>()
            .WithMessage("*15*Remote service error*");
    }

    [Fact]
    public void ExtractAffiliateUrl_WithBusinessError_ShouldThrowApiException()
    {
        const string responseBody = """
        {
          "resp_result": {
            "resp_code": 400,
            "resp_msg": "Invalid tracking id"
          }
        }
        """;

        var act = () => AliExpressOpenPlatformDiagnostics.ExtractAffiliateUrl(responseBody);

        act.Should().Throw<AliExpressAffiliateApiException>()
            .WithMessage("*400*Invalid tracking id*");
    }

    private static AliExpressAffiliateOptions CreateOptions()
    {
        return new AliExpressAffiliateOptions
        {
            ApiEndpoint = "https://api-sg.aliexpress.com/sync",
            AppKey = "534190",
            AppSecret = "app-secret",
            DefaultTrackingId = "default-tracking",
            DefaultPromotionLinkType = "0"
        };
    }
}
