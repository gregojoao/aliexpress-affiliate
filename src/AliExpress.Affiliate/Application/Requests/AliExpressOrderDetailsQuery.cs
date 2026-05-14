namespace AliExpress.Affiliate;

public sealed record AliExpressOrderDetailsQuery
{
    public string OrderIds { get; init; } = string.Empty;
    public string Fields { get; init; } = string.Empty;
}
