using System.Globalization;

namespace AliExpress.Affiliate.Domain;

internal static class ProductPriceFormatter
{
    public static string FormatMoney(
        string value,
        string currency)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            return value;
        }

        return currency.Equals("BRL", StringComparison.OrdinalIgnoreCase)
            ? string.Format(CultureInfo.GetCultureInfo("pt-BR"), "R$ {0:N2}", amount)
            : string.IsNullOrWhiteSpace(currency)
                ? amount.ToString("N2", CultureInfo.InvariantCulture)
                : string.Format(CultureInfo.InvariantCulture, "{0} {1:N2}", currency, amount);
    }
}
