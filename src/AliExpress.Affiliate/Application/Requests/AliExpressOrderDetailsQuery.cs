namespace AliExpress.Affiliate.Application.Requests;

public sealed record AliExpressOrderDetailsQuery
{
    public IReadOnlyList<string> OrderIds { get; init; } = Array.Empty<string>();
    public string Fields { get; init; } = string.Empty;
}
