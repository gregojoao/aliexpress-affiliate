using System.Text.RegularExpressions;

namespace AliExpress.Affiliate.Domain;

internal static class AliExpressProductUrl
{
    public static string Normalize(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return url;
        }

        var trimmed = url.Trim();

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) &&
            HostMatches(uri.Host, "aliexpress.com"))
        {
            return TrimAfterHtml(trimmed);
        }

        return trimmed;
    }

    public static bool TryExtractProductId(
        string productUrl,
        out string productId)
    {
        productId = string.Empty;

        if (string.IsNullOrWhiteSpace(productUrl))
        {
            return false;
        }

        var match = Regex.Match(
            productUrl,
            @"/item/(?<id>\d+)\.html",
            RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            match = Regex.Match(
                Uri.UnescapeDataString(productUrl),
                @"(?:orig_item_id|orig_sl_item_id|x_object_id|productIds)[^\d]*(?<id>\d{10,})",
                RegexOptions.IgnoreCase);
        }

        productId = match.Success
            ? match.Groups["id"].Value
            : string.Empty;

        return !string.IsNullOrWhiteSpace(productId);
    }

    private static bool HostMatches(
        string candidateHost,
        string configuredHost)
    {
        return candidateHost.Equals(configuredHost, StringComparison.OrdinalIgnoreCase) ||
               candidateHost.EndsWith($".{configuredHost}", StringComparison.OrdinalIgnoreCase);
    }

    private static string TrimAfterHtml(string value)
    {
        var htmlIndex = value.IndexOf(".html", StringComparison.OrdinalIgnoreCase);

        return htmlIndex >= 0
            ? value[..(htmlIndex + ".html".Length)]
            : value;
    }
}
