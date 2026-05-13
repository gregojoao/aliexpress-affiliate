using System.Net;
using System.Text;
using FluentAssertions;
using Xunit;

namespace AliExpress.Affiliate.Tests;

public class AliExpressAffiliateClientTests
{
    [Fact]
    public void CreateTopSignature_WithMd5Parameters_ShouldUseUppercaseTopSignature()
    {
        var parameters = new Dictionary<string, string>
        {
            ["app_key"] = "123456",
            ["format"] = "json",
            ["method"] = "aliexpress.affiliate.link.generate",
            ["promotion_link_type"] = "0",
            ["sign_method"] = "md5",
            ["source_values"] = "https://pt.aliexpress.com/item/1005006860981590.html",
            ["timestamp"] = "1778688000000",
            ["tracking_id"] = "telegram",
            ["v"] = "2.0"
        };

        var signature = AliExpressAffiliateClient.CreateTopSignature(
            parameters,
            "secret",
            "md5");

        signature.Should().Be("641E9282AAE8DD8DFA5A063D5E111BB7");
    }

    [Fact]
    public void BuildLinkGenerateRequest_ShouldUseAliExpressOpenPlatformShape()
    {
        var request = AliExpressAffiliateClient.BuildLinkGenerateRequest(
            "https://pt.aliexpress.com/item/1005006860981590.html",
            CreateOptions(),
            DateTimeOffset.FromUnixTimeMilliseconds(1778688000000));

        request.RequestUri.AbsoluteUri.Should().Be("https://api-sg.aliexpress.com/sync");
        request.BodyParameters.Should().Contain("method", "aliexpress.affiliate.link.generate");
        request.BodyParameters.Should().Contain("app_key", "534190");
        request.BodyParameters.Should().Contain("sign_method", "md5");
        request.BodyParameters.Should().Contain("v", "2.0");
        request.BodyParameters.Should().Contain("source_values", "https://pt.aliexpress.com/item/1005006860981590.html");
        request.BodyParameters.Should().Contain("tracking_id", "telegram_greco");
        request.BodyParameters.Should().Contain("promotion_link_type", "0");
        request.BodyParameters.Should().ContainKey("sign");
        request.BodyParameters.Should().NotContainKey("ship_to_country");
    }

    [Fact]
    public void BuildProductDetailRequest_ShouldUseProductDetailParameters()
    {
        var request = AliExpressAffiliateClient.BuildProductDetailRequest(
            "1005010592960109",
            CreateOptions(),
            DateTimeOffset.FromUnixTimeMilliseconds(1778688000000));

        request.RequestUri.AbsoluteUri.Should().Be("https://api-sg.aliexpress.com/sync");
        request.BodyParameters.Should().Contain("method", "aliexpress.affiliate.productdetail.get");
        request.BodyParameters.Should().Contain("product_ids", "1005010592960109");
        request.BodyParameters.Should().Contain("tracking_id", "telegram_greco");
        request.BodyParameters.Should().Contain("ship_to_country", "BR");
        request.BodyParameters.Should().Contain("country", "BR");
        request.BodyParameters.Should().Contain("target_currency", "BRL");
        request.BodyParameters.Should().Contain("target_language", "PT");
    }

    [Fact]
    public async Task GenerateAffiliateLinkAsync_ShouldReadPromotionLinkAndMergeProductDetails()
    {
        var handler = new QueueingHandler(
            """
            {
              "resp_result": {
                "result": {
                  "promotion_links": [
                    {
                      "promotion_link": "https://s.click.aliexpress.com/e/_generated"
                    }
                  ]
                },
                "resp_code": 200
              }
            }
            """,
            """
            {
              "resp_result": {
                "result": {
                  "products": {
                    "product": [
                      {
                        "product_title": "Fifine microfone dinamico usb/xlr",
                        "target_sale_price": "264.51",
                        "target_sale_price_currency": "BRL",
                        "target_original_price": "499.05",
                        "target_original_price_currency": "BRL",
                        "product_main_image_url": "https://ae-pic-a1.aliexpress-media.com/kf/product.jpg",
                        "product_detail_url": "https://pt.aliexpress.com/item/1005006356702381.html",
                        "promotion_link": "https://s.click.aliexpress.com/s/detail"
                      }
                    ]
                  }
                },
                "resp_code": 200
              }
            }
            """);
        var client = new AliExpressAffiliateClient(
            new HttpClient(handler),
            () => DateTimeOffset.FromUnixTimeMilliseconds(1778688000000));

        var result = await client.GenerateAffiliateLinkAsync(
            "https://pt.aliexpress.com/item/1005006356702381.html?spm=abc",
            CreateOptions(includeProductDetails: true));

        result.Should().NotBeNull();
        result!.AffiliateUrl.Should().Be("https://s.click.aliexpress.com/e/_generated");
        result.SourceUrl.Should().Be("https://pt.aliexpress.com/item/1005006356702381.html");
        result.ProductTitle.Should().Be("Fifine microfone dinamico usb/xlr");
        result.ProductPrice.Should().Be("R$ 264,51");
        result.ProductOriginalPrice.Should().Be("R$ 499,05");
        result.ProductImageUrl.Should().Be("https://ae-pic-a1.aliexpress-media.com/kf/product.jpg");
        handler.Requests.Should().HaveCount(2);
        handler.Requests[0].RequestBody.Should().Contain("method=aliexpress.affiliate.link.generate");
        handler.Requests[0].RequestBody.Should().Contain("promotion_link_type=0");
        handler.Requests[0].RequestBody.Should().NotContain("promotion_link_type=2");
        handler.Requests[1].RequestBody.Should().Contain("method=aliexpress.affiliate.productdetail.get");
        handler.Requests[1].RequestBody.Should().Contain("product_ids=1005006356702381");
    }

    [Fact]
    public async Task GenerateAffiliateLinkAsync_WithOnlySourceValue_ShouldThrowUnavailable()
    {
        var handler = new QueueingHandler("""
        {
          "resp_result": {
            "result": {
              "promotion_links": [
                {
                  "source_value": "https://pt.aliexpress.com/item/1005008652606983.html"
                }
              ]
            },
            "resp_code": 200,
            "resp_msg": "Call succeeds"
          }
        }
        """);
        var client = new AliExpressAffiliateClient(
            new HttpClient(handler),
            () => DateTimeOffset.FromUnixTimeMilliseconds(1778688000000));

        Func<Task> act = async () => await client.GenerateAffiliateLinkAsync(
            "https://pt.aliexpress.com/item/1005008652606983.html?spm=abc",
            CreateOptions());

        await act.Should()
            .ThrowAsync<AliExpressAffiliateLinkUnavailableException>()
            .Where(ex =>
                ex.ProductUrl == "https://pt.aliexpress.com/item/1005008652606983.html" &&
                ex.ResponseSummary.Contains("promotion_link=missing", StringComparison.OrdinalIgnoreCase));
        handler.Requests.Should().HaveCount(1);
    }

    [Fact]
    public void NormalizeAliExpressUrl_WithAnyAliExpressHtmlUrl_ShouldTrimAfterHtml()
    {
        var normalized = AliExpressAffiliateClient.NormalizeAliExpressUrl(
            "https://campaign.aliexpress.com/wow/gcp/some-page.html?spm=abc#section");

        normalized.Should().Be("https://campaign.aliexpress.com/wow/gcp/some-page.html");
    }

    [Fact]
    public void ExtractAffiliateUrl_WithOnlySourceValue_ShouldReturnEmpty()
    {
        const string responseBody = """
        {
          "resp_result": {
            "result": {
              "promotion_links": [
                {
                  "source_value": "https://pt.aliexpress.com/item/1005008652606983.html"
                }
              ]
            },
            "resp_code": 200
          }
        }
        """;

        var affiliateUrl = AliExpressAffiliateClient.ExtractAffiliateUrl(responseBody);

        affiliateUrl.Should().BeEmpty();
    }

    [Theory]
    [InlineData("https://pt.aliexpress.com/item/1005010592960109.html?spm=abc", "1005010592960109")]
    [InlineData("https://pt.aliexpress.com/item/foo.html?x_object_id=1005006356702381", "1005006356702381")]
    public void TryExtractProductId_ShouldReadKnownAliExpressProductIds(
        string url,
        string expectedProductId)
    {
        var extracted = AliExpressAffiliateClient.TryExtractProductId(url, out var productId);

        extracted.Should().BeTrue();
        productId.Should().Be(expectedProductId);
    }

    private static AliExpressAffiliateOptions CreateOptions(bool includeProductDetails = false)
    {
        return new AliExpressAffiliateOptions
        {
            Endpoint = "https://api-sg.aliexpress.com/sync",
            AppKey = "534190",
            AppSecret = "app-secret",
            TrackingId = "telegram_greco",
            SignMethod = "md5",
            PromotionLinkType = "0",
            ShipToCountry = "BR",
            TargetCurrency = "BRL",
            TargetLanguage = "PT",
            IncludeProductDetails = includeProductDetails
        };
    }

    private sealed class QueueingHandler : HttpMessageHandler
    {
        private readonly Queue<string> _responseBodies;

        public QueueingHandler(params string[] responseBodies)
        {
            _responseBodies = new Queue<string>(responseBodies);
        }

        public List<CapturedRequest> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(new CapturedRequest(
                request.RequestUri,
                request.Content == null
                    ? string.Empty
                    : await request.Content.ReadAsStringAsync(cancellationToken)));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseBodies.Dequeue(), Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed record CapturedRequest(Uri? RequestUri, string RequestBody);
}
