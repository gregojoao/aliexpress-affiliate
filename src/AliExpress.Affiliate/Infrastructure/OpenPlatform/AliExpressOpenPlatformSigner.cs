using System.Security.Cryptography;
using System.Text;

namespace AliExpress.Affiliate.Infrastructure.OpenPlatform;

internal static class AliExpressOpenPlatformSigner
{
    public static string CreateTopSignature(
        IReadOnlyDictionary<string, string> parameters,
        string appSecret,
        string signMethod)
    {
        if (string.IsNullOrWhiteSpace(appSecret))
        {
            throw new ArgumentException("App secret is required.", nameof(appSecret));
        }

        var baseString = BuildSignatureSourceString(parameters);

        return NormalizeSignMethod(signMethod) switch
        {
            "md5" => Convert.ToHexString(
                MD5.HashData(Encoding.UTF8.GetBytes(appSecret + baseString + appSecret))),
            "hmac" => ComputeHmacMd5(baseString, appSecret),
            "sha256" => ComputeHmacSha256(baseString, appSecret),
            _ => throw new ArgumentOutOfRangeException(nameof(signMethod), signMethod, "Invalid AliExpress sign method.")
        };
    }

    public static string BuildSignatureSourceString(
        IEnumerable<KeyValuePair<string, string>> parameters)
    {
        return string.Concat(
            parameters
                .Where(parameter =>
                    !string.Equals(parameter.Key, "sign", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(parameter.Value))
                .OrderBy(parameter => parameter.Key, StringComparer.Ordinal)
                .Select(parameter => parameter.Key + parameter.Value));
    }

    public static string NormalizeSignMethod(string signMethod)
    {
        var normalized = string.IsNullOrWhiteSpace(signMethod)
            ? AliExpressAffiliateOptions.DefaultSignMethod
            : signMethod.Trim().ToLowerInvariant();

        return normalized switch
        {
            "md5" => "md5",
            "hmac" => "hmac",
            "hmac-md5" => "hmac",
            "hmacsha256" => "sha256",
            "hmac-sha256" => "sha256",
            "sha256" => "sha256",
            _ => throw new InvalidOperationException("Invalid AliExpress sign method. Use hmac, md5 or sha256.")
        };
    }

    private static string ComputeHmacSha256(
        string value,
        string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    private static string ComputeHmacMd5(
        string value,
        string secret)
    {
        using var hmac = new HMACMD5(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }
}
