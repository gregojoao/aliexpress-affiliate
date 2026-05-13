namespace AliExpress.Affiliate;

public sealed class AliExpressAffiliateLinkUnavailableException : Exception
{
    public AliExpressAffiliateLinkUnavailableException(
        string productUrl,
        string reason,
        string responseSummary)
        : base(BuildMessage(productUrl, reason, responseSummary))
    {
        ProductUrl = productUrl ?? string.Empty;
        Reason = reason ?? string.Empty;
        ResponseSummary = responseSummary ?? string.Empty;
    }

    public string ProductUrl { get; }
    public string Reason { get; }
    public string ResponseSummary { get; }

    private static string BuildMessage(
        string productUrl,
        string reason,
        string responseSummary)
    {
        var details = string.IsNullOrWhiteSpace(responseSummary)
            ? reason
            : $"{reason} Response: {responseSummary}";

        return $"AliExpress product cannot generate affiliate link: {productUrl}. {details}";
    }
}
