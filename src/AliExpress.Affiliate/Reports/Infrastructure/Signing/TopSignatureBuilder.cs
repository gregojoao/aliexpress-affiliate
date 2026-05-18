using AliExpress.Affiliate.Infrastructure.OpenPlatform;

namespace AliExpress.Affiliate.Reports.Infrastructure.Signing;

/// <summary>
/// Builds the TOP gateway signature for an outgoing request. Provided as a thin,
/// testable wrapper over <see cref="AliExpressOpenPlatformSigner"/> so the report client
/// can be exercised with a known signature vector without depending on the link-generation
/// path. Supports <c>sha256</c> (preferred) and <c>md5</c>.
/// </summary>
internal static class TopSignatureBuilder
{
    /// <summary>
    /// Computes the TOP signature for the given parameter set, alphabetically sorted as
    /// required by the AliExpress Open Platform.
    /// </summary>
    /// <param name="parameters">Parameter dictionary. The <c>sign</c> entry is ignored.</param>
    /// <param name="appSecret">App secret used as the HMAC key (sha256) or MD5 envelope.</param>
    /// <param name="signMethod">Signing algorithm, normalised by <see cref="AliExpressOpenPlatformSigner.NormalizeSignMethod"/>.</param>
    public static string Build(
        IReadOnlyDictionary<string, string> parameters,
        string appSecret,
        string signMethod)
    {
        return AliExpressOpenPlatformSigner.CreateTopSignature(parameters, appSecret, signMethod);
    }

    /// <summary>
    /// Normalises a sign-method label to one of the canonical AliExpress identifiers
    /// (<c>sha256</c>, <c>md5</c>, <c>hmac</c>).
    /// </summary>
    public static string Normalize(string signMethod)
    {
        return AliExpressOpenPlatformSigner.NormalizeSignMethod(signMethod);
    }

    /// <summary>
    /// Concatenates the parameter set in the order used to sign — exposed for diagnostic
    /// purposes.
    /// </summary>
    public static string BuildSignatureSourceString(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        return AliExpressOpenPlatformSigner.BuildSignatureSourceString(parameters);
    }
}
