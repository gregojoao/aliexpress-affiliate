using AliExpress.Affiliate.Exceptions;
using AliExpress.Affiliate.OpenPlatform;
using System.Net.Http.Headers;

namespace AliExpress.Affiliate.Infrastructure.OpenPlatform;

internal sealed class AliExpressOpenPlatformGateway : IAliExpressOpenPlatformGateway
{
    private readonly HttpClient _httpClient;

    public AliExpressOpenPlatformGateway(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<string> SendAsync(
        AliExpressOpenPlatformRequest request,
        CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(request.BodyParameters);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded")
        {
            CharSet = "utf-8"
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, request.RequestUri)
        {
            Content = content
        };

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new AliExpressAffiliateHttpException(response.StatusCode, responseBody);
        }

        return responseBody;
    }
}
