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
    // AliExpress TOP returns timestamps in two shapes: naive "yyyy-MM-dd HH:mm:ss" (interpreted
    // in GMT+8 server-side) and ISO 8601 with an explicit offset. Try offset-bearing formats
    // first; only fall back to anchoring in GMT+8 when no offset is present.
    private static readonly string[] OffsetDateFormats =
    {
        "yyyy-MM-ddTHH:mm:sszzz",
        "yyyy-MM-ddTHH:mm:ss.fffzzz",
        "yyyy-MM-ddTHH:mm:ssZ",
        "yyyy-MM-ddTHH:mm:ss.fffZ"
    };

    private static readonly string[] NaiveDateFormats =
    {
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd HH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.fff",
        "yyyy-MM-dd"
    };

    public static AliExpressConversionPage ParseConversionPage(string responseBody, int page, int pageSize)
    {
        using var document = JsonDocument.Parse(responseBody);

        // AliExpress returns resp_code=405 "The result is empty" instead of an empty array
        // when the window has no conversions. Surface that as a zero-item page, not an error.
        if (ReportsErrorClassifier.IsEmptyResultCode(document.RootElement))
        {
            return new AliExpressConversionPage(
                Items: Array.Empty<AliExpressConversion>(),
                Page: page,
                PageSize: pageSize,
                TotalCount: 0,
                HasMore: false);
        }

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
            "settled_currency",
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
            Status: OrderStatusMap.FromTopStatusString(OpenPlatformJsonReader.GetPropertyString(order, "order_status")),
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
        var currency = ReadFirstNonEmpty(order, "settled_currency", "order_currency", "paid_amount_currency", "commission_currency");
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

    private static Money ReadMoney(JsonElement order, string fallbackCurrency, params string[] amountFieldNames)
    {
        foreach (var name in amountFieldNames)
        {
            if (!OpenPlatformJsonReader.TryGetProperty(order, name, out var element))
            {
                continue;
            }

            var amount = ParseMonetaryAmount(element);
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

    /// <summary>
    /// Parses a monetary amount from the AliExpress affiliate response.
    /// </summary>
    /// <remarks>
    /// AliExpress emits monetary values in two shapes — the SDK accepts both:
    /// <list type="bullet">
    /// <item><description>Integer JSON numbers (e.g. <c>1113</c>) carry the smallest currency unit
    /// (centavos / cents) and are divided by 100 to recover the major-unit decimal
    /// (<c>R$ 11.13</c>). This is the convention observed on
    /// <c>aliexpress.affiliate.order.list</c>.</description></item>
    /// <item><description>Decimal numbers and strings (e.g. <c>11.13</c> or <c>"11.13"</c>) are
    /// already in major units and passed through verbatim.</description></item>
    /// </list>
    /// Assumes a 2-decimal-place currency. Currencies like JPY (0 decimals) or BHD
    /// (3 decimals) would need a different scaling; AliExpress affiliate accounts settle
    /// in BRL / USD / EUR / CNY where this holds.
    /// </remarks>
    private static decimal? ParseMonetaryAmount(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number)
        {
            if (element.TryGetInt64(out var asInteger))
            {
                return asInteger / 100m;
            }

            if (element.TryGetDecimal(out var asDecimal))
            {
                return asDecimal;
            }
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            return ParseDecimalString(element.GetString());
        }

        return null;
    }

    private static decimal? ParseDecimal(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var asDecimal))
        {
            return asDecimal;
        }

        return ParseDecimalString(OpenPlatformJsonReader.GetScalarString(element));
    }

    private static decimal? ParseDecimalString(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim().Replace("%", string.Empty, StringComparison.Ordinal);
        return decimal.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
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
            var parsed = ParseDecimalString(raw);
            if (parsed is null)
            {
                continue;
            }

            var value = parsed.Value;
            if (raw.Contains('%', StringComparison.Ordinal) || value > 1m)
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
                    OffsetDateFormats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var withOffset))
            {
                return withOffset;
            }

            if (DateTime.TryParseExact(
                    raw,
                    NaiveDateFormats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var naive))
            {
                return new DateTimeOffset(naive, GmtPlus8Time.Offset);
            }
        }

        return null;
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
