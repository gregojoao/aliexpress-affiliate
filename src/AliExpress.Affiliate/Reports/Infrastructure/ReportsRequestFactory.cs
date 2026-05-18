using AliExpress.Affiliate.OpenPlatform;
using AliExpress.Affiliate.Reports.Application;
using AliExpress.Affiliate.Reports.Application.Requests;
using AliExpress.Affiliate.Reports.Configuration;
using AliExpress.Affiliate.Reports.Infrastructure.Signing;
using System.Globalization;

namespace AliExpress.Affiliate.Reports.Infrastructure;

/// <summary>
/// Builds signed TOP requests for the reporting endpoints.
/// </summary>
internal static class ReportsRequestFactory
{
    public const string OrderListMethod = "aliexpress.affiliate.order.list";
    public const string OrderGetMethod = "aliexpress.affiliate.order.get";
    public const string JsonFormat = "json";

    public static AliExpressOpenPlatformRequest BuildOrderListRequest(
        ListConversionsRequest request,
        AliExpressAffiliateReportsOptions options,
        DateTimeOffset timestamp)
    {
        options.Validate();

        var parameters = BuildCommonParameters(OrderListMethod, options, timestamp);
        parameters["start_time"] = GmtPlus8Time.Format(request.From);
        parameters["end_time"] = GmtPlus8Time.Format(request.To);
        parameters["page_no"] = request.Page.ToString(CultureInfo.InvariantCulture);
        parameters["page_size"] = ClampPageSize(request.PageSize).ToString(CultureInfo.InvariantCulture);

        // AliExpress TOP requires `status` for aliexpress.affiliate.order.list — it has no
        // "all statuses" wildcard. ConversionStatusFilter.All / null falls back to
        // "Payment Completed" (paid conversions), which is the typical dashboard signal.
        parameters["status"] = MapStatusFilter(request.Status);

        AddIfNotEmpty(parameters, "order_ids", request.OrderId);
        AddIfNotEmpty(parameters, "tracking_id", request.TrackingId ?? options.TrackingId);

        return Finalize(parameters, options);
    }

    public static AliExpressOpenPlatformRequest BuildOrderGetRequest(
        string orderId,
        AliExpressAffiliateReportsOptions options,
        DateTimeOffset timestamp)
    {
        options.Validate();

        if (string.IsNullOrWhiteSpace(orderId))
        {
            throw new ArgumentException("Order id is required.", nameof(orderId));
        }

        var parameters = BuildCommonParameters(OrderGetMethod, options, timestamp);
        parameters["order_ids"] = orderId.Trim();
        AddIfNotEmpty(parameters, "tracking_id", options.TrackingId);

        return Finalize(parameters, options);
    }

    private static SortedDictionary<string, string> BuildCommonParameters(
        string method,
        AliExpressAffiliateReportsOptions options,
        DateTimeOffset timestamp)
    {
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["method"] = method,
            ["app_key"] = options.AppKey,
            ["timestamp"] = timestamp.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
            ["format"] = JsonFormat,
            ["sign_method"] = TopSignatureBuilder.Normalize(options.SignMethod),
            ["v"] = string.IsNullOrWhiteSpace(options.ApiVersion)
                ? AliExpressAffiliateReportsOptions.DefaultApiVersion
                : options.ApiVersion
        };

        if (!string.IsNullOrWhiteSpace(options.AccessToken))
        {
            parameters["session"] = options.AccessToken!;
        }

        return parameters;
    }

    private static AliExpressOpenPlatformRequest Finalize(
        SortedDictionary<string, string> parameters,
        AliExpressAffiliateReportsOptions options)
    {
        parameters["sign"] = TopSignatureBuilder.Build(parameters, options.AppSecret, parameters["sign_method"]);

        var endpoint = string.IsNullOrWhiteSpace(options.Endpoint)
            ? AliExpressAffiliateReportsOptions.DefaultEndpoint
            : options.Endpoint;
        var requestUri = new UriBuilder(new Uri(endpoint, UriKind.Absolute))
        {
            Query = string.Empty
        }.Uri;

        return new AliExpressOpenPlatformRequest(requestUri, parameters, parameters);
    }

    private static int ClampPageSize(int requested)
    {
        if (requested <= 0)
        {
            return 50;
        }

        return requested > 50 ? 50 : requested;
    }

    private static void AddIfNotEmpty(IDictionary<string, string> parameters, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parameters[key] = value!.Trim();
        }
    }

    private static string MapStatusFilter(ConversionStatusFilter? filter)
    {
        return (filter ?? ConversionStatusFilter.All) switch
        {
            ConversionStatusFilter.All => "Payment Completed",
            ConversionStatusFilter.Pending => "Payment Pending",
            ConversionStatusFilter.Paid => "Payment Completed",
            ConversionStatusFilter.Confirmed => "Buyer Confirmed Receipt",
            ConversionStatusFilter.Cancelled => "Cancelled Order",
            ConversionStatusFilter.Invalid => "Invalid Order",
            _ => "Payment Completed"
        };
    }
}
