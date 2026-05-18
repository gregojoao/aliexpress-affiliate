using AliExpress.Affiliate.Infrastructure.OpenPlatform;
using AliExpress.Affiliate.Reports.Domain;
using AliExpress.Affiliate.Reports.Exceptions;
using System.Globalization;
using System.Text.Json;

namespace AliExpress.Affiliate.Reports.Infrastructure;

/// <summary>
/// Maps AliExpress TOP responses to the Reports domain records.
/// </summary>
internal static class ReportsResponseParser
{
    private static readonly string[] DateFormats =
    {
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-dd HH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm:sszzz",
        "yyyy-MM-dd"
    };

    public static AliExpressConversionPage ParseConversionPage(string responseBody, int page, int pageSize)
    {
        using var document = JsonDocument.Parse(responseBody);
        ReportsErrorClassifier.ThrowForTopError(document.RootElement, responseBody);

        var result = OpenPlatformResponseEnvelope.ExtractResult(document.RootElement);
        var orders = OpenPlatformJsonReader.TryGetProperty(result, "orders", out var ordersElement)
            ? OpenPlatformJsonReader.EnumerateItems(ordersElement, "order")
                .Select(MapConversion)
                .ToArray()
            : Array.Empty<AliExpressConversion>();

        var totalCount = FirstNonZero(
            OpenPlatformJsonReader.GetPropertyInt(result, "total_record_count"),
            OpenPlatformJsonReader.GetPropertyInt(result, "total_result_count"));
        var totalPages = OpenPlatformJsonReader.GetPropertyInt(result, "total_page_no");
        var currentPage = OpenPlatformJsonReader.GetPropertyInt(result, "current_page_no");
        var currentPageOrFallback = currentPage > 0 ? currentPage : page;
        var hasMore = totalPages > 0
            ? currentPageOrFallback < totalPages
            : totalCount > currentPageOrFallback * pageSize;

        return new AliExpressConversionPage(orders, currentPageOrFallback, pageSize, totalCount, hasMore);
    }

    public static AliExpressConversionDetail ParseConversionDetail(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        ReportsErrorClassifier.ThrowForTopError(document.RootElement, responseBody);

        var result = OpenPlatformResponseEnvelope.ExtractResult(document.RootElement);
        if (!OpenPlatformJsonReader.TryGetProperty(result, "orders", out var ordersElement))
        {
            ThrowOrderNotFound();
        }

        var orderElements = OpenPlatformJsonReader.EnumerateItems(ordersElement, "order").ToArray();
        if (orderElements.Length == 0)
        {
            ThrowOrderNotFound();
        }

        var first = orderElements[0];
        var conversion = MapConversion(first);
        var lines = orderElements.Select(MapLine).ToArray();

        return new AliExpressConversionDetail(
            conversion.ConversionId,
            conversion.OrderId,
            conversion.Status,
            conversion.ProductId,
            conversion.ProductTitle,
            conversion.ProductImageUrl,
            conversion.ProductUrl,
            conversion.Quantity,
            conversion.ItemPrice,
            conversion.TotalSale,
            conversion.Commission,
            conversion.CommissionRate,
            conversion.SubId1,
            conversion.SubId2,
            conversion.SubId3,
            conversion.SubId4,
            conversion.SubId5,
            conversion.ClickTime,
            conversion.PurchaseTime,
            conversion.PaidTime,
            conversion.FinishTime,
            conversion.Currency,
            conversion.RawJson,
            lines);
    }

    private static AliExpressConversion MapConversion(JsonElement order)
    {
        var currency = ReadFirstNonEmpty(order,
            "order_currency",
            "settle_currency",
            "paid_amount_currency",
            "estimated_paid_commission_currency",
            "commission_currency");
        var fallbackCurrency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency!;

        var itemPrice = ReadMoney(order, fallbackCurrency, "item_price", "product_price", "paid_amount");
        var totalSale = ReadMoney(order, fallbackCurrency, "paid_amount", "total_paid_amount", "order_amount");
        var commission = ReadMoney(order, fallbackCurrency,
            "estimated_paid_commission",
            "estimated_commission",
            "paid_commission",
            "finished_commission");
        var commissionRate = ReadPercentAsFraction(order, "commission_rate", "estimated_commission_rate");
        var quantity = ReadInt(order, "item_count", "product_count", "quantity");
        if (quantity <= 0)
        {
            quantity = 1;
        }

        return new AliExpressConversion(
            ConversionId: ReadFirstNonEmpty(order, "sub_order_id", "order_id", "order_number") ?? string.Empty,
            OrderId: ReadFirstNonEmpty(order, "order_id", "order_number", "sub_order_id") ?? string.Empty,
            Status: MapStatus(OpenPlatformJsonReader.GetPropertyString(order, "order_status")),
            ProductId: ReadFirstNonEmpty(order, "product_id", "item_id"),
            ProductTitle: ReadFirstNonEmpty(order, "product_title", "item_title"),
            ProductImageUrl: ReadFirstNonEmpty(order, "product_main_image_url", "product_image_url", "image_url"),
            ProductUrl: ReadFirstNonEmpty(order, "product_detail_url"),
            Quantity: quantity,
            ItemPrice: itemPrice,
            TotalSale: totalSale,
            Commission: commission,
            CommissionRate: commissionRate,
            SubId1: ReadFirstNonEmpty(order, "sub_id1", "sub_id"),
            SubId2: ReadFirstNonEmpty(order, "sub_id2"),
            SubId3: ReadFirstNonEmpty(order, "sub_id3"),
            SubId4: ReadFirstNonEmpty(order, "sub_id4"),
            SubId5: ReadFirstNonEmpty(order, "sub_id5"),
            ClickTime: ReadTimestamp(order, "click_time"),
            PurchaseTime: ReadTimestamp(order, "created_time", "order_time", "paid_time")
                ?? DateTimeOffset.MinValue,
            PaidTime: ReadTimestamp(order, "paid_time", "estimated_paid_time"),
            FinishTime: ReadTimestamp(order, "finished_time", "settled_time"),
            Currency: currency,
            RawJson: order.GetRawText());
    }

    private static AliExpressOrderLine MapLine(JsonElement order)
    {
        var currency = ReadFirstNonEmpty(order, "order_currency", "paid_amount_currency", "commission_currency");
        var fallbackCurrency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency!;
        var quantity = ReadInt(order, "item_count", "quantity");
        if (quantity <= 0)
        {
            quantity = 1;
        }

        return new AliExpressOrderLine(
            ProductId: ReadFirstNonEmpty(order, "product_id"),
            ProductTitle: ReadFirstNonEmpty(order, "product_title"),
            Quantity: quantity,
            ItemPrice: ReadMoney(order, fallbackCurrency, "item_price", "product_price"),
            TotalSale: ReadMoney(order, fallbackCurrency, "paid_amount", "total_paid_amount"),
            Commission: ReadMoney(order, fallbackCurrency,
                "estimated_paid_commission",
                "estimated_commission",
                "paid_commission"),
            CommissionRate: ReadPercentAsFraction(order, "commission_rate"));
    }

    private static OrderStatus MapStatus(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return OrderStatus.Unknown;
        }

        var normalized = raw.Trim().ToLowerInvariant();
        return normalized switch
        {
            "payment pending" or "wait payment" or "pending" => OrderStatus.Pending,
            "payment completed" or "paid" => OrderStatus.Paid,
            "buyer confirmed receipt" or "confirmed receipt" or "confirmed" or "settled" => OrderStatus.Confirmed,
            "cancelled order" or "cancelled" or "canceled" => OrderStatus.Cancelled,
            "invalid order" or "invalid" => OrderStatus.Invalid,
            _ => OrderStatus.Unknown
        };
    }

    private static Money ReadMoney(JsonElement order, string fallbackCurrency, params string[] amountFieldNames)
    {
        foreach (var name in amountFieldNames)
        {
            if (!OpenPlatformJsonReader.TryGetProperty(order, name, out var element))
            {
                continue;
            }

            var amount = ParseDecimal(element);
            if (amount is null)
            {
                continue;
            }

            var currency = OpenPlatformText.FirstNonEmpty(
                OpenPlatformJsonReader.GetPropertyString(order, name + "_currency"),
                fallbackCurrency);
            return new Money(amount.Value, string.IsNullOrWhiteSpace(currency) ? fallbackCurrency : currency);
        }

        return Money.Zero(fallbackCurrency);
    }

    private static decimal? ParseDecimal(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var asDecimal))
        {
            return asDecimal;
        }

        var raw = OpenPlatformJsonReader.GetScalarString(element);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        raw = raw.Trim().Replace("%", string.Empty, StringComparison.Ordinal);
        if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static decimal ReadPercentAsFraction(JsonElement order, params string[] fieldNames)
    {
        foreach (var name in fieldNames)
        {
            if (!OpenPlatformJsonReader.TryGetProperty(order, name, out var element))
            {
                continue;
            }

            var raw = OpenPlatformJsonReader.GetScalarString(element);
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var hasPercent = raw.Contains('%', StringComparison.Ordinal);
            var parsed = ParseDecimal(element);
            if (parsed is null)
            {
                continue;
            }

            var value = parsed.Value;
            if (hasPercent || value > 1m)
            {
                value /= 100m;
            }

            return value;
        }

        return 0m;
    }

    private static int ReadInt(JsonElement order, params string[] fieldNames)
    {
        foreach (var name in fieldNames)
        {
            var raw = OpenPlatformJsonReader.GetPropertyString(order, name);
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
        }

        return 0;
    }

    private static DateTimeOffset? ReadTimestamp(JsonElement order, params string[] fieldNames)
    {
        foreach (var name in fieldNames)
        {
            var raw = OpenPlatformJsonReader.GetPropertyString(order, name);
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            if (DateTimeOffset.TryParseExact(
                    raw,
                    DateFormats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out var parsedExact))
            {
                return InterpretTimestamp(parsedExact, raw);
            }

            if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            {
                return InterpretTimestamp(parsed, raw);
            }
        }

        return null;
    }

    private static DateTimeOffset InterpretTimestamp(DateTimeOffset parsed, string raw)
    {
        // AliExpress emits timestamps in GMT+8 without a zone designator. If the raw string carries
        // no offset, re-anchor it to GMT+8 so callers receive an unambiguous UTC instant.
        var hasExplicitOffset = raw.Contains('+', StringComparison.Ordinal)
            || raw.Contains('Z', StringComparison.OrdinalIgnoreCase)
            || raw.Count(c => c == '-') > 2;
        if (hasExplicitOffset)
        {
            return parsed;
        }

        return new DateTimeOffset(parsed.DateTime, GmtPlus8Time.Offset);
    }

    private static string? ReadFirstNonEmpty(JsonElement element, params string[] fieldNames)
    {
        foreach (var name in fieldNames)
        {
            var value = OpenPlatformJsonReader.GetPropertyString(element, name);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static int FirstNonZero(params int[] values)
    {
        foreach (var value in values)
        {
            if (value != 0)
            {
                return value;
            }
        }

        return 0;
    }

    private static void ThrowOrderNotFound()
    {
        throw new AliExpressAffiliateNotFoundException(
            "AliExpress affiliate order not found.",
            code: "isv.order-not-found");
    }
}
