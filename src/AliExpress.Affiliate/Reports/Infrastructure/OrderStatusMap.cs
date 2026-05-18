using AliExpress.Affiliate.Reports.Application;
using AliExpress.Affiliate.Reports.Domain;

namespace AliExpress.Affiliate.Reports.Infrastructure;

/// <summary>
/// Single source of truth for the bidirectional mapping between the SDK's
/// <see cref="OrderStatus"/> / <see cref="ConversionStatusFilter"/> enums and the literal
/// status strings expected by the AliExpress TOP gateway (e.g. <c>"Payment Completed"</c>).
/// </summary>
internal static class OrderStatusMap
{
    private const string PaymentPending = "Payment Pending";
    private const string PaymentCompleted = "Payment Completed";
    private const string BuyerConfirmedReceipt = "Buyer Confirmed Receipt";
    private const string CancelledOrder = "Cancelled Order";
    private const string InvalidOrder = "Invalid Order";

    private static readonly IReadOnlyDictionary<OrderStatus, string> OrderStatusToTop = new Dictionary<OrderStatus, string>
    {
        [OrderStatus.Pending] = PaymentPending,
        [OrderStatus.Paid] = PaymentCompleted,
        [OrderStatus.Confirmed] = BuyerConfirmedReceipt,
        [OrderStatus.Cancelled] = CancelledOrder,
        [OrderStatus.Invalid] = InvalidOrder
    };

    private static readonly IReadOnlyDictionary<string, OrderStatus> TopToOrderStatus = new Dictionary<string, OrderStatus>(StringComparer.OrdinalIgnoreCase)
    {
        [PaymentPending] = OrderStatus.Pending,
        ["Wait Payment"] = OrderStatus.Pending,
        ["Pending"] = OrderStatus.Pending,
        [PaymentCompleted] = OrderStatus.Paid,
        ["Paid"] = OrderStatus.Paid,
        [BuyerConfirmedReceipt] = OrderStatus.Confirmed,
        ["Confirmed Receipt"] = OrderStatus.Confirmed,
        ["Confirmed"] = OrderStatus.Confirmed,
        ["Settled"] = OrderStatus.Confirmed,
        [CancelledOrder] = OrderStatus.Cancelled,
        ["Cancelled"] = OrderStatus.Cancelled,
        ["Canceled"] = OrderStatus.Cancelled,
        [InvalidOrder] = OrderStatus.Invalid,
        ["Invalid"] = OrderStatus.Invalid
    };

    /// <summary>
    /// Maps a <see cref="ConversionStatusFilter"/> (or <c>null</c>) to the literal status
    /// string sent in the <c>status</c> parameter. AliExpress requires this field and does
    /// not accept a wildcard, so <see cref="ConversionStatusFilter.All"/> falls back to
    /// <c>"Payment Completed"</c> — the most useful single signal for dashboards.
    /// </summary>
    public static string ToTopStatusString(ConversionStatusFilter? filter)
    {
        return filter switch
        {
            ConversionStatusFilter.Pending => PaymentPending,
            ConversionStatusFilter.Paid => PaymentCompleted,
            ConversionStatusFilter.Confirmed => BuyerConfirmedReceipt,
            ConversionStatusFilter.Cancelled => CancelledOrder,
            ConversionStatusFilter.Invalid => InvalidOrder,
            _ => PaymentCompleted
        };
    }

    /// <summary>
    /// Maps a raw status string returned by AliExpress to the canonical
    /// <see cref="OrderStatus"/>. Returns <see cref="OrderStatus.Unknown"/> for empty or
    /// unrecognised values.
    /// </summary>
    public static OrderStatus FromTopStatusString(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return OrderStatus.Unknown;
        }

        return TopToOrderStatus.TryGetValue(raw.Trim(), out var status)
            ? status
            : OrderStatus.Unknown;
    }
}
