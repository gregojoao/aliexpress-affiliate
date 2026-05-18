namespace AliExpress.Affiliate.Reports.Application;

/// <summary>
/// Filter passed to <c>aliexpress.affiliate.order.list</c> via the <c>status</c> parameter.
/// </summary>
public enum ConversionStatusFilter
{
    /// <summary>
    /// Default. AliExpress' <c>aliexpress.affiliate.order.list</c> endpoint requires the
    /// <c>status</c> parameter and does not accept a wildcard, so the SDK maps this value
    /// to <c>Payment Completed</c> (paid conversions) — the most useful single signal for
    /// affiliate dashboards. To inspect other statuses, pass them explicitly.
    /// </summary>
    All = 0,

    /// <summary>Order has been placed but the buyer has not paid yet.</summary>
    Pending,

    /// <summary>Buyer has paid; commission is still being held by AliExpress.</summary>
    Paid,

    /// <summary>Order delivered and confirmed by the buyer; commission is locked in.</summary>
    Confirmed,

    /// <summary>Order cancelled by buyer, seller, or the platform.</summary>
    Cancelled,

    /// <summary>Order marked invalid (e.g. fraud, duplicate) — commission was clawed back.</summary>
    Invalid
}
