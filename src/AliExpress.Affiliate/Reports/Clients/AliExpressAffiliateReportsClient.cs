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
    private const int SummaryPageSize = 50;
    private const int SummaryMaxPages = 40;

    private readonly ReportsGateway _gateway;
    private readonly Func<DateTimeOffset> _clock;
    private readonly AliExpressAffiliateReportsOptions? _defaultOptions;

    /// <summary>
    /// Primary constructor matching the link-generation client's shape.
    /// </summary>
    /// <param name="httpClient">Reused HttpClient. The SDK applies its own per-call timeout via cancellation.</param>
    /// <param name="clock">Wall-clock provider used to stamp <c>timestamp</c>. Defaults to <see cref="DateTimeOffset.UtcNow"/>.</param>
    public AliExpressAffiliateReportsClient(
        HttpClient httpClient,
        Func<DateTimeOffset>? clock = null)
        : this(httpClient, defaultOptions: null, clock, logger: null)
    {
    }

    /// <summary>
    /// Constructor with default options (used by the DI extension).
    /// </summary>
    public AliExpressAffiliateReportsClient(
        HttpClient httpClient,
        IOptions<AliExpressAffiliateReportsOptions> options,
        ILogger<AliExpressAffiliateReportsClient>? logger = null)
        : this(httpClient, options?.Value, clock: null, logger)
    {
    }

    /// <summary>
    /// Full constructor — defaults can be overridden per call, logger is optional.
    /// </summary>
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
        return ReportsResponseParser.ParseConversionPage(body, request.Page, ClampPageSize(request.PageSize));
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
                    PageSize: SummaryPageSize,
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

    private static int ClampPageSize(int requested)
    {
        if (requested <= 0)
        {
            return 50;
        }

        return requested > 50 ? 50 : requested;
    }
}
