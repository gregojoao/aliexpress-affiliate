namespace AliExpress.Affiliate.Reports.Domain;

/// <summary>
/// Canonical order status used by the reporting client. AliExpress returns a variety of
/// raw status strings; the mapping is best-effort and falls back to <see cref="Unknown"/>.
/// </summary>
public enum OrderStatus
{
    /// <summary>Status string returned by the API was empty or did not match a known label.</summary>
    Unknown = 0,

    /// <summary>Order placed, awaiting payment.</summary>
    Pending,

    /// <summary>Payment received.</summary>
    Paid,

    /// <summary>Delivery confirmed by the buyer.</summary>
    Confirmed,

    /// <summary>Order cancelled.</summary>
    Cancelled,

    /// <summary>Order flagged invalid by AliExpress.</summary>
    Invalid
}
