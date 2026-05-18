namespace AliExpress.Affiliate.Reports.Domain;

/// <summary>
/// Monetary amount with the currency exactly as returned by AliExpress. The SDK never
/// converts currencies; if the API returns USD, this record carries USD.
/// </summary>
/// <remarks>
/// <para>
/// AliExpress emits monetary values in two shapes — both are normalised to the
/// major unit (e.g. dollars, not cents) before reaching <see cref="Amount"/>:
/// </para>
/// <list type="bullet">
///   <item><description>Integer JSON numbers carry the <b>smallest currency unit</b>
///   (centavos / cents). <c>1113</c> becomes <c>11.13m</c>.</description></item>
///   <item><description>Decimal numbers and strings (<c>11.13</c>, <c>"11.13"</c>) are
///   already in major units and pass through verbatim.</description></item>
/// </list>
/// <para>
/// Assumes a 2-decimal-place currency. AliExpress affiliate accounts settle in
/// BRL / USD / EUR / CNY today, all of which match. Currencies like JPY (0 decimals)
/// or BHD (3 decimals) would need different scaling, which the SDK does not perform.
/// </para>
/// </remarks>
/// <param name="Amount">Decimal amount in the major unit of <paramref name="Currency"/>.</param>
/// <param name="Currency">ISO 4217 currency code as returned by AliExpress (typically
/// <c>settled_currency</c>). Defaults to <c>USD</c>.</param>
public sealed record Money(decimal Amount, string Currency = "USD")
{
    /// <summary>Zero amount in the supplied currency.</summary>
    public static Money Zero(string currency = "USD") => new(0m, currency);
}
