using AliExpress.Affiliate.Domain;
using System.Text.Json;

namespace AliExpress.Affiliate.Infrastructure.OpenPlatform;

internal static class AliExpressOrderResponseMapper
{
    public static AliExpressAffiliateApiResult<AliExpressAffiliateOrder> ExtractOrders(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var result = OpenPlatformResponseEnvelope.ExtractResult(document.RootElement);

        var orders = OpenPlatformJsonReader.TryGetProperty(result, "orders", out var ordersElement)
            ? OpenPlatformJsonReader.EnumerateItems(ordersElement, "order")
                .Select(order => new AliExpressAffiliateOrder(
                    OrderId: OpenPlatformJsonReader.GetPropertyString(order, "order_id"),
                    SubOrderId: OpenPlatformJsonReader.GetPropertyString(order, "sub_order_id"),
                    OrderNumber: OpenPlatformJsonReader.GetPropertyString(order, "order_number"),
                    OrderStatus: OpenPlatformJsonReader.GetPropertyString(order, "order_status"),
                    TrackingId: OpenPlatformJsonReader.GetPropertyString(order, "tracking_id"),
                    ProductId: OpenPlatformJsonReader.GetPropertyString(order, "product_id"),
                    ProductTitle: OpenPlatformJsonReader.GetPropertyString(order, "product_title"),
                    ProductDetailUrl: OpenPlatformJsonReader.GetPropertyString(order, "product_detail_url"),
                    CommissionRate: OpenPlatformJsonReader.GetPropertyString(order, "commission_rate"),
                    EstimatedCommission: OpenPlatformText.FirstNonEmpty(
                        OpenPlatformJsonReader.GetPropertyString(order, "estimated_commission"),
                        OpenPlatformJsonReader.GetPropertyString(order, "estimated_finished_commission"),
                        OpenPlatformJsonReader.GetPropertyString(order, "estimated_paid_commission")),
                    PaidCommission: OpenPlatformText.FirstNonEmpty(
                        OpenPlatformJsonReader.GetPropertyString(order, "paid_commission"),
                        OpenPlatformJsonReader.GetPropertyString(order, "finished_commission")),
                    CreatedTime: OpenPlatformJsonReader.GetPropertyString(order, "created_time"),
                    FinishedTime: OpenPlatformJsonReader.GetPropertyString(order, "finished_time"),
                    RawJson: order.GetRawText()))
                .ToArray()
            : Array.Empty<AliExpressAffiliateOrder>();

        return OpenPlatformApiResultFactory.Create(responseBody, result, orders);
    }
}
