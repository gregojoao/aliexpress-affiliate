namespace AliExpress.Affiliate.Domain;

public sealed record AliExpressAffiliateOrder(
    string OrderId,
    string SubOrderId,
    string OrderNumber,
    string OrderStatus,
    string TrackingId,
    string ProductId,
    string ProductTitle,
    string ProductDetailUrl,
    string CommissionRate,
    string EstimatedCommission,
    string PaidCommission,
    string CreatedTime,
    string FinishedTime,
    string RawJson);
