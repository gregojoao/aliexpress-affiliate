namespace AliExpress.Affiliate.Reports.Domain;

/// <summary>
/// Monetary amount with the currency exactly as returned by AliExpress. The SDK never
/// converts currencies; if the API returns USD, this record carries USD.
/// </summary>
/// <param name="Amount">Decimal amount, parsed from the AliExpress response.</param>
/// <param name="Currency">ISO 4217 currency code as returned by AliExpress. Defaults to <c>USD</c>.</param>
public sealed record Money(decimal Amount, string Currency = "USD")
{
    /// <summary>Zero amount in the supplied currency.</summary>
    public static Money Zero(string currency = "USD") => new(0m, currency);
}
