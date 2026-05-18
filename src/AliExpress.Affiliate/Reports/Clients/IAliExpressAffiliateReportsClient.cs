using AliExpress.Affiliate.Reports.Application.Requests;
using AliExpress.Affiliate.Reports.Domain;

namespace AliExpress.Affiliate.Reports.Clients;

/// <summary>
/// AliExpress affiliate reporting client. Reads conversion / sales data via the official
/// AliExpress Open Platform (TOP) gateway. Clicks and link-usage attribution are not
/// exposed by the public TOP API today; the SDK reports those endpoints as
/// <c>Supported=false</c> so callers can fall back to manual portal exports.
/// </summary>
/// <remarks>
/// Endpoints in use:
/// <list type="bullet">
///   <item><description><c>aliexpress.affiliate.order.list</c> — drives <see cref="ListConversionsAsync"/> and <see cref="GetSalesSummaryAsync"/>.</description></item>
///   <item><description><c>aliexpress.affiliate.order.get</c> — drives <see cref="GetConversionAsync"/>.</description></item>
/// </list>
/// </remarks>
public interface IAliExpressAffiliateReportsClient
{
    /// <summary>
    /// Lists conversions for the configured affiliate account in the requested window.
    /// Backed by <c>aliexpress.affiliate.order.list</c>.
    /// </summary>
    Task<AliExpressConversionPage> ListConversionsAsync(
        ListConversionsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches a single conversion (and its line items) by order id. Backed by
    /// <c>aliexpress.affiliate.order.get</c>.
    /// </summary>
    Task<AliExpressConversionDetail> GetConversionAsync(
        string orderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Aggregates the conversion stream into a sales summary for the requested window.
    /// Reads from <c>aliexpress.affiliate.order.list</c>; clicks remain <c>null</c>
    /// because AliExpress does not expose them via TOP.
    /// </summary>
    Task<AliExpressSalesSummary> GetSalesSummaryAsync(
        SalesSummaryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns time-series click statistics. The AliExpress TOP gateway does not expose
    /// click metrics to affiliates; the returned object has <c>Supported=false</c>.
    /// </summary>
    Task<AliExpressClickStats> GetClickStatsAsync(
        ClickStatsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns generated-link usage statistics. The AliExpress TOP gateway does not
    /// expose link / click attribution to affiliates; the returned object has
    /// <c>Supported=false</c>.
    /// </summary>
    Task<AliExpressLinkUsage> GetGeneratedLinkUsageAsync(
        LinkUsageRequest request,
        CancellationToken cancellationToken = default);
}
