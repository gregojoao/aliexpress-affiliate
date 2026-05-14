using AliExpress.Affiliate.OpenPlatform;

namespace AliExpress.Affiliate.Infrastructure.OpenPlatform;

internal interface IAliExpressOpenPlatformGateway
{
    Task<string> SendAsync(
        AliExpressOpenPlatformRequest request,
        CancellationToken cancellationToken);
}
