using AliExpress.Affiliate.Exceptions;
using AliExpress.Affiliate.Reports.Application;
using AliExpress.Affiliate.Reports.Application.Requests;
using AliExpress.Affiliate.Reports.Configuration;
using AliExpress.Affiliate.Reports.Domain;
using AliExpress.Affiliate.Reports.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AliExpress.Affiliate.Reports.Clients;

/// <summary>
/// Default implementation of <see cref="IAliExpressAffiliateReportsClient"/>.
/// Consumes the AliExpress Open Platform (TOP) gateway over an injected
/// <see cref="HttpClient"/>; the HttpClient lifecycle is owned by the host.
/// </summary>
public sealed class AliExpressAffiliateReportsClient : IAliExpressAffiliateReportsClient
{
    // Pagination cap for the summary aggregator: 40 pages × 50 items = up to 2000 conversions
    // per window. Beyond that, callers should narrow the window or paginate ListConversionsAsync
    // themselves. Serial paging is deliberate — AliExpress TOP enforces per-app QPS limits.
    private const int SummaryMaxPages = 40;

    private readonly ReportsGateway _gateway;
    private readonly Func<DateTimeOffset> _clock;
    private readonly AliExpressAffiliateReportsOptions? _defaultOptions;

    /// <summary>
    /// DI-friendly constructor. <paramref name="options"/> must contain a valid app key
    /// and app secret; validation is deferred to the first call.
    /// </summary>
    public AliExpressAffiliateReportsClient(
        HttpClient httpClient,
        IOptions<AliExpressAffiliateReportsOptions> options,
        ILogger<AliExpressAffiliateReportsClient>? logger = null)
        : this(httpClient, options?.Value, clock: null, logger)
    {
    }

    /// <summary>
    /// Primary constructor. <paramref name="defaultOptions"/> is required — pass <c>null</c>
    /// only when the caller plans to feed options via configuration before the first call.
    /// </summary>
    /// <param name="httpClient">Reused HttpClient. The SDK applies its own per-call timeout via cancellation.</param>
    /// <param name="defaultOptions">Reports options applied to every method call.</param>
    /// <param name="clock">Wall-clock provider used to stamp <c>timestamp</c>. Defaults to <see cref="DateTimeOffset.UtcNow"/>.</param>
    /// <param name="logger">Optional logger. Credential values are never logged.</param>
    public AliExpressAffiliateReportsClient(
        HttpClient httpClient,
        AliExpressAffiliateReportsOptions? defaultOptions,
        Func<DateTimeOffset>? clock = null,
        ILogger<AliExpressAffiliateReportsClient>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _gateway = new ReportsGateway(httpClient, logger);
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _defaultOptions = defaultOptions;
    }

    /// <inheritdoc />
    public async Task<AliExpressConversionPage> ListConversionsAsync(
        ListConversionsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var options = GetOptions();
        var openPlatformRequest = ReportsRequestFactory.BuildOrderListRequest(request, options, _clock());
        var body = await _gateway.SendAsync(openPlatformRequest, options, cancellationToken).ConfigureAwait(false);
        return ReportsResponseParser.ParseConversionPage(body, request.Page, ReportsRequestFactory.ClampPageSize(request.PageSize));
    }

    /// <inheritdoc />
    public async Task<AliExpressConversionDetail> GetConversionAsync(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            throw new ArgumentException("Order id is required.", nameof(orderId));
        }

        var options = GetOptions();
        var openPlatformRequest = ReportsRequestFactory.BuildOrderGetRequest(orderId, options, _clock());
        var body = await _gateway.SendAsync(openPlatformRequest, options, cancellationToken).ConfigureAwait(false);
        return ReportsResponseParser.ParseConversionDetail(body);
    }

    /// <inheritdoc />
    public async Task<AliExpressSalesSummary> GetSalesSummaryAsync(
        SalesSummaryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var collected = new List<AliExpressConversion>();

        for (var page = 1; page <= SummaryMaxPages; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var current = await ListConversionsAsync(
                new ListConversionsRequest(
                    From: request.From,
                    To: request.To,
                    Status: ConversionStatusFilter.All,
                    Page: page,
                    PageSize: ReportsRequestFactory.MaxPageSize,
                    TrackingId: request.TrackingId),
                cancellationToken).ConfigureAwait(false);

            collected.AddRange(current.Items);
            if (!current.HasMore || current.Items.Count == 0)
            {
                break;
            }
        }

        return SalesSummaryAggregator.Aggregate(collected, request.From, request.To);
    }

    /// <inheritdoc />
    public Task<AliExpressClickStats> GetClickStatsAsync(
        ClickStatsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Task.FromResult(new AliExpressClickStats(
            Granularity: request.Granularity,
            Points: Array.Empty<AliExpressClickPoint>(),
            Supported: false,
            UnsupportedReason: "AliExpress TOP does not expose click statistics to affiliates. Use the affiliate portal CSV export."));
    }

    /// <inheritdoc />
    public Task<AliExpressLinkUsage> GetGeneratedLinkUsageAsync(
        LinkUsageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Task.FromResult(new AliExpressLinkUsage(
            LinksGenerated: 0,
            ClicksAttributed: 0,
            ConversionsAttributed: 0,
            CommissionAttributed: Money.Zero(),
            Supported: false,
            UnsupportedReason: "AliExpress TOP does not expose generated-link usage to affiliates."));
    }

    private AliExpressAffiliateReportsOptions GetOptions()
    {
        return _defaultOptions
            ?? throw new AliExpressAffiliateValidationException(
                "No default AliExpressAffiliateReportsOptions were configured. Register the client with AddAliExpressAffiliateReports.");
    }
}
