using AliExpress.Affiliate.Application.Requests;
using AliExpress.Affiliate.Clients;
using AliExpress.Affiliate.Configuration;
using AliExpress.Affiliate.Exceptions;
using FluentAssertions;
using System.Net;
using System.Text;

namespace AliExpress.Affiliate.Tests.Clients;

public class AliExpressAffiliateErrorHandlingTests
{
    [Fact]
    public async Task GenerateAffiliateLinkAsync_WithHttpError_ShouldThrowHttpException()
    {
        var client = new AliExpressAffiliateClient(
            new HttpClient(new FixedStatusHandler(HttpStatusCode.BadGateway, "upstream down")));

        var act = async () => await client.GenerateAffiliateLinkAsync(
            new AliExpressAffiliateLinkRequest
            {
                ProductUrl = "https://pt.aliexpress.com/item/1005006356702381.html"
            },
            CreateOptions());

        var exception = await act.Should().ThrowAsync<AliExpressAffiliateHttpException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        exception.Which.ResponseBody.Should().Be("upstream down");
    }

    [Fact]
    public async Task GenerateAffiliateLinkAsync_WithBusinessError_ShouldThrowApiException()
    {
        var client = new AliExpressAffiliateClient(
            new HttpClient(new FixedStatusHandler(
                HttpStatusCode.OK,
                """
                {
                  "resp_result": {
                    "resp_code": 401,
                    "resp_msg": "Invalid signature"
                  }
                }
                """)));

        var act = async () => await client.GenerateAffiliateLinkAsync(
            new AliExpressAffiliateLinkRequest
            {
                ProductUrl = "https://pt.aliexpress.com/item/1005006356702381.html"
            },
            CreateOptions());

        await act.Should().ThrowAsync<AliExpressAffiliateApiException>()
            .WithMessage("*401*Invalid signature*");
    }

    private static AliExpressAffiliateOptions CreateOptions()
    {
        return new AliExpressAffiliateOptions
        {
            AppKey = "534190",
            AppSecret = "app-secret",
            DefaultTrackingId = "tracking"
        };
    }

    private sealed class FixedStatusHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseBody;

        public FixedStatusHandler(HttpStatusCode statusCode, string responseBody)
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            });
        }
    }
}
