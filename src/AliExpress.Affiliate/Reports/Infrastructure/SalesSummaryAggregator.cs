using AliExpress.Affiliate.Reports.Domain;

namespace AliExpress.Affiliate.Reports.Infrastructure;

/// <summary>
/// Builds an <see cref="AliExpressSalesSummary"/> from the conversion stream returned by
/// <c>aliexpress.affiliate.order.list</c>. AliExpress does not expose click counts to
/// affiliates via TOP, so <see cref="AliExpressSalesSummary.Clicks"/> and
/// <see cref="AliExpressSalesSummary.ConversionRate"/> are reported as <c>null</c>.
/// </summary>
internal static class SalesSummaryAggregator
{
    public static AliExpressSalesSummary Aggregate(
        IReadOnlyCollection<AliExpressConversion> conversions,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd)
    {
        if (conversions.Count == 0)
        {
            return new AliExpressSalesSummary(
                PeriodStart: periodStart,
                PeriodEnd: periodEnd,
                Conversions: 0,
                Clicks: null,
                GrossRevenue: Money.Zero(),
                Commission: Money.Zero(),
                AvgCommissionRate: 0m,
                ConversionRate: null,
                ByStatus: new Dictionary<OrderStatus, int>(),
                TopProducts: Array.Empty<AliExpressTopProduct>(),
                TopSubIds: Array.Empty<AliExpressTopSubId>(),
                Supported: true,
                UnsupportedReason: null);
        }

        var currency = conversions
            .Select(c => c.Commission.Currency)
            .FirstOrDefault(currency => !string.IsNullOrWhiteSpace(currency))
            ?? "USD";

        var grossRevenue = conversions.Sum(c => c.TotalSale.Amount);
        var commission = conversions.Sum(c => c.Commission.Amount);
        var avgRate = conversions.Average(c => c.CommissionRate);

        var byStatus = conversions
            .GroupBy(c => c.Status)
            .ToDictionary(group => group.Key, group => group.Count());

        var topProducts = conversions
            .Where(c => !string.IsNullOrWhiteSpace(c.ProductId))
            .GroupBy(c => c.ProductId!)
            .Select(group => new AliExpressTopProduct(
                ProductId: group.Key,
                ProductTitle: group.Select(c => c.ProductTitle).FirstOrDefault(t => !string.IsNullOrWhiteSpace(t)),
                Conversions: group.Count(),
                Commission: new Money(group.Sum(c => c.Commission.Amount), currency)))
            .OrderByDescending(p => p.Commission.Amount)
            .ThenByDescending(p => p.Conversions)
            .Take(10)
            .ToArray();

        var topSubIds = conversions
            .Select(c => new
            {
                SubId = FirstNonEmpty(c.SubId1, c.SubId2, c.SubId3, c.SubId4, c.SubId5),
                Conversion = c
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.SubId))
            .GroupBy(item => item.SubId!)
            .Select(group => new AliExpressTopSubId(
                SubId: group.Key,
                Conversions: group.Count(),
                Commission: new Money(group.Sum(item => item.Conversion.Commission.Amount), currency)))
            .OrderByDescending(s => s.Commission.Amount)
            .ThenByDescending(s => s.Conversions)
            .Take(10)
            .ToArray();

        return new AliExpressSalesSummary(
            PeriodStart: periodStart,
            PeriodEnd: periodEnd,
            Conversions: conversions.Count,
            Clicks: null,
            GrossRevenue: new Money(grossRevenue, currency),
            Commission: new Money(commission, currency),
            AvgCommissionRate: avgRate,
            ConversionRate: null,
            ByStatus: byStatus,
            TopProducts: topProducts,
            TopSubIds: topSubIds,
            Supported: true,
            UnsupportedReason: null);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
