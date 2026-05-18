using AliExpress.Affiliate.Exceptions;
using AliExpress.Affiliate.Reports.Application;
using AliExpress.Affiliate.Reports.Application.Requests;
using AliExpress.Affiliate.Reports.Clients;
using AliExpress.Affiliate.Reports.Configuration;
using AliExpress.Affiliate.Reports.Domain;
using AliExpress.Affiliate.Reports.Exceptions;
using AliExpress.Affiliate.Reports.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Web;

namespace AliExpress.Affiliate.Tests.Reports;

public class AliExpressAffiliateReportsClientTests
{
    private static readonly DateTimeOffset Clock =
        DateTimeOffset.FromUnixTimeMilliseconds(1778688000000); // 2026-05-13 00:00:00 UTC

    [Fact]
    public async Task ListConversionsAsync_ShouldMapSingleConversion_FromOrderListResponse()
    {
        var handler = new QueueingHandler(SinglePageJson);
        var client = CreateClient(handler);

        var page = await client.ListConversionsAsync(new ListConversionsRequest(
            From: new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            To: new DateTimeOffset(2026, 5, 2, 0, 0, 0, TimeSpan.Zero),
            Status: ConversionStatusFilter.Paid));

        page.Items.Should().HaveCount(1);
        page.Page.Should().Be(1);
        page.TotalCount.Should().Be(1);
        page.HasMore.Should().BeFalse();

        var conversion = page.Items[0];
        conversion.Should().BeEquivalentTo(new AliExpressConversion(
            ConversionId: "222222",
            OrderId: "3333333",
            Status: OrderStatus.Paid,
            ProductId: "1005006356702381",
            ProductTitle: "Fifine microfone dinamico usb/xlr",
            ProductImageUrl: "https://ae-pic-a1.aliexpress-media.com/kf/product.jpg",
            ProductUrl: "https://pt.aliexpress.com/item/1005006356702381.html",
            Quantity: 2,
            ItemPrice: new Money(99.95m, "BRL"),
            TotalSale: new Money(199.90m, "BRL"),
            Commission: new Money(13.99m, "BRL"),
            CommissionRate: 0.07m,
            SubId1: "telegram_a",
            SubId2: "campaign_x",
            SubId3: null,
            SubId4: null,
            SubId5: null,
            ClickTime: new DateTimeOffset(2026, 4, 30, 23, 0, 0, TimeSpan.FromHours(8)),
            PurchaseTime: new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.FromHours(8)),
            PaidTime: new DateTimeOffset(2026, 5, 1, 10, 5, 0, TimeSpan.FromHours(8)),
            FinishTime: null,
            Currency: "BRL",
            RawJson: conversion.RawJson));
    }

    [Fact]
    public async Task ListConversionsAsync_WhenWindowEmpty_ShouldReturnZeroItems_NotThrow()
    {
        // AliExpress signals "no records in this window" with resp_code=405 instead of
        // an empty array. The SDK must translate that into an empty page.
        var handler = new QueueingHandler(EmptyResultErrorJson);
        var client = CreateClient(handler);

        var page = await client.ListConversionsAsync(new ListConversionsRequest(
            From: DateTimeOffset.UtcNow.AddDays(-1),
            To: DateTimeOffset.UtcNow));

        page.Items.Should().BeEmpty();
        page.TotalCount.Should().Be(0);
        page.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task GetSalesSummaryAsync_WhenWindowEmpty_ShouldReturnZeroSummary_NotThrow()
    {
        var handler = new QueueingHandler(EmptyResultErrorJson);
        var client = CreateClient(handler);

        var summary = await client.GetSalesSummaryAsync(new SalesSummaryRequest(
            From: DateTimeOffset.UtcNow.AddDays(-1),
            To: DateTimeOffset.UtcNow));

        summary.Conversions.Should().Be(0);
        summary.GrossRevenue.Amount.Should().Be(0m);
        summary.Commission.Amount.Should().Be(0m);
        summary.Supported.Should().BeTrue();
    }

    [Fact]
    public async Task ListConversionsAsync_ShouldDecodeIntegerMonetaryFieldsAsCents()
    {
        // Mirrors the shape returned by aliexpress.affiliate.order.list:
        // monetary values come as integer JSON numbers in the smallest currency unit.
        var handler = new QueueingHandler(IntegerCentsPageJson);
        var client = CreateClient(handler);

        var page = await client.ListConversionsAsync(new ListConversionsRequest(
            From: DateTimeOffset.UtcNow.AddDays(-1),
            To: DateTimeOffset.UtcNow));

        var conversion = page.Items.Single();
        conversion.TotalSale.Should().Be(new Money(11.13m, "USD"));
        conversion.Commission.Should().Be(new Money(0.77m, "USD"));
        conversion.CommissionRate.Should().Be(0.07m);
        conversion.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task ListConversionsAsync_ShouldHonorPagination_AndPropagateHasMore()
    {
        var handler = new QueueingHandler(PageOneJson, PageTwoJson);
        var client = CreateClient(handler);
        var window = new ListConversionsRequest(
            From: new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            To: new DateTimeOffset(2026, 5, 2, 0, 0, 0, TimeSpan.Zero),
            Page: 1,
            PageSize: 2);

        var first = await client.ListConversionsAsync(window);
        var second = await client.ListConversionsAsync(window with { Page = 2 });

        first.Items.Should().HaveCount(2);
        first.HasMore.Should().BeTrue();
        first.TotalCount.Should().Be(3);
        second.Items.Should().HaveCount(1);
        second.HasMore.Should().BeFalse();

        handler.Requests[0].FormBody.Should().Contain("page_no=1");
        handler.Requests[1].FormBody.Should().Contain("page_no=2");
    }

    [Fact]
    public async Task ListConversionsAsync_ShouldConvertWindowToGmtPlus8AndSign()
    {
        var handler = new QueueingHandler(EmptyPageJson);
        var client = CreateClient(handler);

        await client.ListConversionsAsync(new ListConversionsRequest(
            From: new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            To: new DateTimeOffset(2026, 5, 2, 0, 0, 0, TimeSpan.Zero)));

        var body = ParseForm(handler.Requests[0].FormBody);
        body["method"].Should().Be("aliexpress.affiliate.order.list");
        body["start_time"].Should().Be("2026-05-01 08:00:00");
        body["end_time"].Should().Be("2026-05-02 08:00:00");
        body["sign_method"].Should().Be("sha256");
        body["v"].Should().Be("2.0");
        body["page_size"].Should().Be("50");
        body.Should().ContainKey("sign");
        body["sign"].Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(null, "Payment Completed")]
    [InlineData(ConversionStatusFilter.All, "Payment Completed")]
    [InlineData(ConversionStatusFilter.Paid, "Payment Completed")]
    [InlineData(ConversionStatusFilter.Pending, "Payment Pending")]
    [InlineData(ConversionStatusFilter.Confirmed, "Buyer Confirmed Receipt")]
    [InlineData(ConversionStatusFilter.Cancelled, "Cancelled Order")]
    [InlineData(ConversionStatusFilter.Invalid, "Invalid Order")]
    public async Task ListConversionsAsync_ShouldAlwaysSendMandatoryStatus(
        ConversionStatusFilter? filter,
        string expectedStatusValue)
    {
        var handler = new QueueingHandler(EmptyPageJson);
        var client = CreateClient(handler);

        await client.ListConversionsAsync(new ListConversionsRequest(
            From: DateTimeOffset.UtcNow,
            To: DateTimeOffset.UtcNow,
            Status: filter));

        ParseForm(handler.Requests[0].FormBody)["status"].Should().Be(expectedStatusValue);
    }

    [Fact]
    public async Task ListConversionsAsync_ShouldClampPageSizeAt50()
    {
        var handler = new QueueingHandler(EmptyPageJson);
        var client = CreateClient(handler);

        await client.ListConversionsAsync(new ListConversionsRequest(
            From: DateTimeOffset.UtcNow,
            To: DateTimeOffset.UtcNow,
            PageSize: 999));

        ParseForm(handler.Requests[0].FormBody)["page_size"].Should().Be("50");
    }

    [Fact]
    public async Task ListConversionsAsync_ShouldMapAuthError_FromTopRespCode()
    {
        var handler = new QueueingHandler(AuthErrorJson);
        var client = CreateClient(handler);

        var act = async () => await client.ListConversionsAsync(new ListConversionsRequest(
            From: DateTimeOffset.UtcNow,
            To: DateTimeOffset.UtcNow));

        var exception = await act.Should().ThrowAsync<AliExpressAffiliateAuthException>();
        exception.Which.Code.Should().Be("isv.invalid-signature");
    }

    [Fact]
    public async Task ListConversionsAsync_ShouldMapRateLimitError_FromTopErrorResponse()
    {
        var handler = new QueueingHandler(RateLimitJson);
        var client = CreateClient(handler);

        var act = async () => await client.ListConversionsAsync(new ListConversionsRequest(
            From: DateTimeOffset.UtcNow,
            To: DateTimeOffset.UtcNow));

        var exception = await act.Should().ThrowAsync<AliExpressAffiliateRateLimitException>();
        exception.Which.Code.Should().Be("isv.api-flow-limit");
        exception.Which.RequestId.Should().Be("req-rl-1");
    }

    [Fact]
    public async Task ListConversionsAsync_ShouldMapUnsupportedEndpoint_AsUnsupportedException()
    {
        var handler = new QueueingHandler(UnsupportedJson);
        var client = CreateClient(handler);

        var act = async () => await client.ListConversionsAsync(new ListConversionsRequest(
            From: DateTimeOffset.UtcNow,
            To: DateTimeOffset.UtcNow));

        await act.Should().ThrowAsync<AliExpressAffiliateUnsupportedException>();
    }

    [Fact]
    public async Task ListConversionsAsync_ShouldFallBackToApiException_ForUnknownErrorCodes()
    {
        var handler = new QueueingHandler(UnknownBusinessErrorJson);
        var client = CreateClient(handler);

        var act = async () => await client.ListConversionsAsync(new ListConversionsRequest(
            From: DateTimeOffset.UtcNow,
            To: DateTimeOffset.UtcNow));

        var exception = await act.Should().ThrowAsync<AliExpressAffiliateApiException>();
        exception.Which.Code.Should().Be("isv.unknown-business-error");
        exception.Which.RequestId.Should().Be("req-unknown-1");
    }

    [Fact]
    public async Task ListConversionsAsync_ShouldRetryOnceOn5xx_AndSucceedOnSecondAttempt()
    {
        var handler = new SequencedStatusHandler(
            (HttpStatusCode.BadGateway, "upstream down"),
            (HttpStatusCode.OK, EmptyPageJson));
        var client = CreateClient(handler);

        var page = await client.ListConversionsAsync(new ListConversionsRequest(
            From: DateTimeOffset.UtcNow,
            To: DateTimeOffset.UtcNow));

        page.Items.Should().BeEmpty();
        handler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListConversionsAsync_ShouldNotRetryOn4xx_AndShouldThrowAuthOn401()
    {
        var handler = new SequencedStatusHandler(
            (HttpStatusCode.Unauthorized, "no"));
        var client = CreateClient(handler);

        var act = async () => await client.ListConversionsAsync(new ListConversionsRequest(
            From: DateTimeOffset.UtcNow,
            To: DateTimeOffset.UtcNow));

        await act.Should().ThrowAsync<AliExpressAffiliateAuthException>();
        handler.Requests.Should().HaveCount(1);
    }

    [Fact]
    public async Task ListConversionsAsync_ShouldThrowRateLimit_On429HttpStatus()
    {
        var handler = new SequencedStatusHandler(
            (HttpStatusCode.TooManyRequests, "slow down"));
        var client = CreateClient(handler);

        var act = async () => await client.ListConversionsAsync(new ListConversionsRequest(
            From: DateTimeOffset.UtcNow,
            To: DateTimeOffset.UtcNow));

        await act.Should().ThrowAsync<AliExpressAffiliateRateLimitException>();
    }

    [Fact]
    public async Task GetConversionAsync_ShouldParseOrderDetailWithLines()
    {
        var handler = new QueueingHandler(OrderDetailJson);
        var client = CreateClient(handler);

        var detail = await client.GetConversionAsync("3333333");

        detail.OrderId.Should().Be("3333333");
        detail.Status.Should().Be(OrderStatus.Paid);
        detail.Lines.Should().HaveCount(2);
        detail.Lines[0].ProductId.Should().Be("1005006356702381");
        detail.Lines[0].Quantity.Should().Be(2);
        detail.Lines[0].Commission.Should().Be(new Money(13.99m, "BRL"));
        detail.Lines[0].CommissionRate.Should().Be(0.07m);
        detail.Lines[1].ProductId.Should().Be("1005006860981590");
        detail.Lines[1].Quantity.Should().Be(1);

        var requestBody = ParseForm(handler.Requests[0].FormBody);
        requestBody["method"].Should().Be("aliexpress.affiliate.order.get");
        requestBody["order_ids"].Should().Be("3333333");
    }

    [Fact]
    public async Task GetConversionAsync_WhenOrderMissing_ShouldThrowNotFound()
    {
        var handler = new QueueingHandler("""
        {
          "aliexpress_affiliate_order_get_response": {
            "resp_result": { "resp_code": 200, "result": {} }
          }
        }
        """);
        var client = CreateClient(handler);

        var act = async () => await client.GetConversionAsync("missing-id");
        await act.Should().ThrowAsync<AliExpressAffiliateNotFoundException>();
    }

    [Fact]
    public async Task GetSalesSummaryAsync_ShouldAggregateAcrossPages()
    {
        var handler = new QueueingHandler(SummaryPageOneJson, SummaryPageTwoJson);
        var client = CreateClient(handler);

        var summary = await client.GetSalesSummaryAsync(new SalesSummaryRequest(
            From: new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            To: new DateTimeOffset(2026, 5, 2, 0, 0, 0, TimeSpan.Zero)));

        summary.Conversions.Should().Be(3);
        summary.Clicks.Should().BeNull();
        summary.ConversionRate.Should().BeNull();
        summary.GrossRevenue.Should().Be(new Money(360m, "BRL"));
        summary.Commission.Should().Be(new Money(25.20m, "BRL"));
        summary.AvgCommissionRate.Should().Be(0.07m);
        summary.ByStatus[OrderStatus.Paid].Should().Be(2);
        summary.ByStatus[OrderStatus.Confirmed].Should().Be(1);
        summary.TopProducts.Should().HaveCountGreaterThan(0);
        summary.TopSubIds.Should().HaveCountGreaterThan(0);
        summary.Supported.Should().BeTrue();
        handler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetClickStatsAsync_WithHourGranularity_ShouldReportUnsupported_AndNotHitNetwork()
    {
        var handler = new QueueingHandler();
        var client = CreateClient(handler);

        var stats = await client.GetClickStatsAsync(new ClickStatsRequest(
            From: DateTimeOffset.UtcNow,
            To: DateTimeOffset.UtcNow,
            Granularity: ReportGranularity.Hour));

        stats.Granularity.Should().Be(ReportGranularity.Hour);
        stats.Supported.Should().BeFalse();
        stats.UnsupportedReason.Should().NotBeNullOrWhiteSpace();
        stats.Points.Should().BeEmpty();
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task GetClickStatsAsync_WithDayGranularity_ShouldEchoGranularity()
    {
        var handler = new QueueingHandler();
        var client = CreateClient(handler);

        var stats = await client.GetClickStatsAsync(new ClickStatsRequest(
            From: DateTimeOffset.UtcNow,
            To: DateTimeOffset.UtcNow,
            Granularity: ReportGranularity.Day));

        stats.Granularity.Should().Be(ReportGranularity.Day);
        stats.Supported.Should().BeFalse();
    }

    [Fact]
    public async Task GetGeneratedLinkUsageAsync_ShouldReportUnsupported_AndZeroCounts()
    {
        var handler = new QueueingHandler();
        var client = CreateClient(handler);

        var usage = await client.GetGeneratedLinkUsageAsync(new LinkUsageRequest(
            From: DateTimeOffset.UtcNow,
            To: DateTimeOffset.UtcNow));

        usage.Supported.Should().BeFalse();
        usage.LinksGenerated.Should().Be(0);
        usage.ClicksAttributed.Should().Be(0);
        usage.ConversionsAttributed.Should().Be(0);
        usage.CommissionAttributed.Amount.Should().Be(0m);
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task ListConversionsAsync_ShouldNeverLogAppSecretOrAccessToken()
    {
        var handler = new QueueingHandler(EmptyPageJson);
        var capturingLogger = new CapturingLogger();
        var options = CreateOptions();
        options.AccessToken = "SECRET_ACCESS_TOKEN_TEST";
        var client = new AliExpressAffiliateReportsClient(
            new HttpClient(handler),
            options,
            clock: () => Clock,
            logger: capturingLogger);

        await client.ListConversionsAsync(new ListConversionsRequest(
            From: new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            To: new DateTimeOffset(2026, 5, 2, 0, 0, 0, TimeSpan.Zero)));

        capturingLogger.AllMessages.Should().NotContain(message => message.Contains(options.AppSecret));
        capturingLogger.AllMessages.Should().NotContain(message => message.Contains(options.AccessToken!));
        capturingLogger.AllMessages.Should().NotContain(message => message.Contains("app-secret-do-not-log"));
    }

    [Fact]
    public void MaskCredential_ShouldRedactMiddleCharacters()
    {
        ReportsGateway.MaskCredential("123456789").Should().Be("1***9");
        ReportsGateway.MaskCredential("abcd").Should().Be("****");
        ReportsGateway.MaskCredential(string.Empty).Should().BeEmpty();
        ReportsGateway.MaskCredential(null).Should().BeEmpty();
    }

    private static AliExpressAffiliateReportsClient CreateClient(HttpMessageHandler handler)
    {
        return new AliExpressAffiliateReportsClient(
            new HttpClient(handler),
            CreateOptions(),
            clock: () => Clock,
            logger: null);
    }

    private static AliExpressAffiliateReportsOptions CreateOptions()
    {
        return new AliExpressAffiliateReportsOptions
        {
            AppKey = "534190",
            AppSecret = "app-secret-do-not-log",
            TrackingId = "telegram_greco",
            Endpoint = AliExpressAffiliateReportsOptions.DefaultEndpoint,
            SignMethod = AliExpressAffiliateReportsOptions.DefaultSignMethod,
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    private static Dictionary<string, string> ParseForm(string body)
    {
        var parsed = HttpUtility.ParseQueryString(body);
        return parsed.AllKeys
            .Where(k => k is not null)
            .ToDictionary(k => k!, k => parsed[k] ?? string.Empty);
    }

    // Real shape returned by AliExpress when a window has zero conversions — note the
    // resp_code=405 carried as a successful HTTP 200 response.
    private const string EmptyResultErrorJson = """
    {
      "aliexpress_affiliate_order_list_response": {
        "resp_result": {
          "resp_code": 405,
          "resp_msg": "The result is empty"
        }
      }
    }
    """;

    // Mirrors the real shape observed against aliexpress.affiliate.order.list: monetary
    // values come as integer JSON numbers (cents) and commission_rate as a percentage string.
    private const string IntegerCentsPageJson = """
    {
      "aliexpress_affiliate_order_list_response": {
        "resp_result": {
          "resp_code": 200,
          "result": {
            "current_page_no": 1,
            "current_record_count": 1,
            "total_page_no": 1,
            "total_record_count": 1,
            "orders": {
              "order": [
                {
                  "order_id": 8211114279848963,
                  "sub_order_id": 8211114279858963,
                  "order_status": "Payment Completed",
                  "product_id": 1005011822522159,
                  "product_title": "CURREN 8421 Men's Watch",
                  "settled_currency": "USD",
                  "paid_amount": 1113,
                  "estimated_paid_commission": 77,
                  "commission_rate": "7.00%",
                  "product_count": 1,
                  "tracking_id": "telegram_greco",
                  "created_time": "2026-05-17 22:52:01",
                  "paid_time": "2026-05-17 22:54:02"
                }
              ]
            }
          }
        }
      }
    }
    """;

    private const string SinglePageJson = """
    {
      "aliexpress_affiliate_order_list_response": {
        "resp_result": {
          "resp_code": 200,
          "result": {
            "current_page_no": 1,
            "current_record_count": 1,
            "total_page_no": 1,
            "total_record_count": 1,
            "orders": {
              "order": [
                {
                  "order_id": 3333333,
                  "sub_order_id": 222222,
                  "order_number": 222222,
                  "order_status": "Payment Completed",
                  "tracking_id": "telegram_greco",
                  "product_id": 1005006356702381,
                  "product_title": "Fifine microfone dinamico usb/xlr",
                  "product_main_image_url": "https://ae-pic-a1.aliexpress-media.com/kf/product.jpg",
                  "product_detail_url": "https://pt.aliexpress.com/item/1005006356702381.html",
                  "item_count": 2,
                  "item_price": "99.95",
                  "item_price_currency": "BRL",
                  "paid_amount": "199.90",
                  "paid_amount_currency": "BRL",
                  "commission_rate": "7%",
                  "estimated_paid_commission": "13.99",
                  "estimated_paid_commission_currency": "BRL",
                  "order_currency": "BRL",
                  "sub_id1": "telegram_a",
                  "sub_id2": "campaign_x",
                  "click_time": "2026-04-30 23:00:00",
                  "created_time": "2026-05-01 10:00:00",
                  "paid_time": "2026-05-01 10:05:00"
                }
              ]
            }
          }
        }
      }
    }
    """;

    private const string PageOneJson = """
    {
      "aliexpress_affiliate_order_list_response": {
        "resp_result": {
          "resp_code": 200,
          "result": {
            "current_page_no": 1,
            "current_record_count": 2,
            "total_page_no": 2,
            "total_record_count": 3,
            "orders": {
              "order": [
                {
                  "order_id": 1,
                  "sub_order_id": 11,
                  "order_status": "Payment Completed",
                  "product_id": "100",
                  "product_title": "A",
                  "item_count": 1,
                  "item_price": "50",
                  "paid_amount": "50",
                  "paid_amount_currency": "BRL",
                  "commission_rate": "7%",
                  "estimated_paid_commission": "3.50",
                  "order_currency": "BRL",
                  "created_time": "2026-05-01 10:00:00"
                },
                {
                  "order_id": 2,
                  "sub_order_id": 22,
                  "order_status": "Payment Completed",
                  "product_id": "200",
                  "product_title": "B",
                  "item_count": 1,
                  "item_price": "60",
                  "paid_amount": "60",
                  "paid_amount_currency": "BRL",
                  "commission_rate": "7%",
                  "estimated_paid_commission": "4.20",
                  "order_currency": "BRL",
                  "created_time": "2026-05-01 11:00:00"
                }
              ]
            }
          }
        }
      }
    }
    """;

    private const string PageTwoJson = """
    {
      "aliexpress_affiliate_order_list_response": {
        "resp_result": {
          "resp_code": 200,
          "result": {
            "current_page_no": 2,
            "current_record_count": 1,
            "total_page_no": 2,
            "total_record_count": 3,
            "orders": {
              "order": [
                {
                  "order_id": 3,
                  "sub_order_id": 33,
                  "order_status": "Payment Completed",
                  "product_id": "300",
                  "product_title": "C",
                  "item_count": 1,
                  "item_price": "70",
                  "paid_amount": "70",
                  "paid_amount_currency": "BRL",
                  "commission_rate": "7%",
                  "estimated_paid_commission": "4.90",
                  "order_currency": "BRL",
                  "created_time": "2026-05-01 12:00:00"
                }
              ]
            }
          }
        }
      }
    }
    """;

    private const string EmptyPageJson = """
    {
      "aliexpress_affiliate_order_list_response": {
        "resp_result": {
          "resp_code": 200,
          "result": {
            "current_page_no": 1,
            "current_record_count": 0,
            "total_page_no": 0,
            "total_record_count": 0
          }
        }
      }
    }
    """;

    private const string SummaryPageOneJson = """
    {
      "aliexpress_affiliate_order_list_response": {
        "resp_result": {
          "resp_code": 200,
          "result": {
            "current_page_no": 1,
            "current_record_count": 2,
            "total_page_no": 2,
            "total_record_count": 3,
            "orders": {
              "order": [
                {
                  "order_id": 1,
                  "sub_order_id": 11,
                  "order_status": "Payment Completed",
                  "product_id": "100",
                  "product_title": "Product A",
                  "item_count": 1,
                  "item_price": "100",
                  "paid_amount": "100",
                  "paid_amount_currency": "BRL",
                  "commission_rate": "7%",
                  "estimated_paid_commission": "7.00",
                  "order_currency": "BRL",
                  "sub_id1": "telegram_a",
                  "created_time": "2026-05-01 10:00:00"
                },
                {
                  "order_id": 2,
                  "sub_order_id": 22,
                  "order_status": "Buyer Confirmed Receipt",
                  "product_id": "200",
                  "product_title": "Product B",
                  "item_count": 1,
                  "item_price": "120",
                  "paid_amount": "120",
                  "paid_amount_currency": "BRL",
                  "commission_rate": "7%",
                  "estimated_paid_commission": "8.40",
                  "order_currency": "BRL",
                  "sub_id1": "telegram_a",
                  "created_time": "2026-05-01 11:00:00"
                }
              ]
            }
          }
        }
      }
    }
    """;

    private const string SummaryPageTwoJson = """
    {
      "aliexpress_affiliate_order_list_response": {
        "resp_result": {
          "resp_code": 200,
          "result": {
            "current_page_no": 2,
            "current_record_count": 1,
            "total_page_no": 2,
            "total_record_count": 3,
            "orders": {
              "order": [
                {
                  "order_id": 3,
                  "sub_order_id": 33,
                  "order_status": "Payment Completed",
                  "product_id": "100",
                  "product_title": "Product A",
                  "item_count": 1,
                  "item_price": "140",
                  "paid_amount": "140",
                  "paid_amount_currency": "BRL",
                  "commission_rate": "7%",
                  "estimated_paid_commission": "9.80",
                  "order_currency": "BRL",
                  "sub_id1": "telegram_b",
                  "created_time": "2026-05-01 12:00:00"
                }
              ]
            }
          }
        }
      }
    }
    """;

    private const string OrderDetailJson = """
    {
      "aliexpress_affiliate_order_get_response": {
        "resp_result": {
          "resp_code": 200,
          "result": {
            "orders": {
              "order": [
                {
                  "order_id": 3333333,
                  "sub_order_id": 222222,
                  "order_status": "Payment Completed",
                  "product_id": "1005006356702381",
                  "product_title": "Fifine microfone dinamico usb/xlr",
                  "item_count": 2,
                  "item_price": "99.95",
                  "paid_amount": "199.90",
                  "paid_amount_currency": "BRL",
                  "commission_rate": "7%",
                  "estimated_paid_commission": "13.99",
                  "order_currency": "BRL",
                  "created_time": "2026-05-01 10:00:00"
                },
                {
                  "order_id": 3333333,
                  "sub_order_id": 333333,
                  "order_status": "Payment Completed",
                  "product_id": "1005006860981590",
                  "product_title": "Cabo XLR",
                  "item_count": 1,
                  "item_price": "30.00",
                  "paid_amount": "30.00",
                  "paid_amount_currency": "BRL",
                  "commission_rate": "5%",
                  "estimated_paid_commission": "1.50",
                  "order_currency": "BRL",
                  "created_time": "2026-05-01 10:00:00"
                }
              ]
            }
          }
        }
      }
    }
    """;

    private const string AuthErrorJson = """
    {
      "aliexpress_affiliate_order_list_response": {
        "resp_result": {
          "resp_code": "isv.invalid-signature",
          "resp_msg": "Invalid signature"
        }
      }
    }
    """;

    private const string RateLimitJson = """
    {
      "error_response": {
        "code": "isv.api-flow-limit",
        "msg": "Too many requests",
        "request_id": "req-rl-1"
      }
    }
    """;

    private const string UnsupportedJson = """
    {
      "error_response": {
        "code": "isv.permission-api-package-gateway-no-auth",
        "msg": "This API is not granted to your app",
        "request_id": "req-unsup-1"
      }
    }
    """;

    private const string UnknownBusinessErrorJson = """
    {
      "error_response": {
        "code": "isv.unknown-business-error",
        "msg": "Something unspecified happened",
        "request_id": "req-unknown-1"
      }
    }
    """;

    internal sealed class QueueingHandler : HttpMessageHandler
    {
        private readonly Queue<string> _responseBodies;

        public QueueingHandler(params string[] responseBodies)
        {
            _responseBodies = new Queue<string>(responseBodies);
        }

        public List<CapturedRequest> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(new CapturedRequest(
                request.RequestUri,
                request.Content == null
                    ? string.Empty
                    : await request.Content.ReadAsStringAsync(cancellationToken)));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseBodies.Dequeue(), Encoding.UTF8, "application/json")
            };
        }
    }

    internal sealed class SequencedStatusHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode Status, string Body)> _responses;

        public SequencedStatusHandler(params (HttpStatusCode Status, string Body)[] responses)
        {
            _responses = new Queue<(HttpStatusCode Status, string Body)>(responses);
        }

        public List<CapturedRequest> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var (status, body) = _responses.Dequeue();
            Requests.Add(new CapturedRequest(
                request.RequestUri,
                request.Content == null
                    ? string.Empty
                    : await request.Content.ReadAsStringAsync(cancellationToken)));

            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }
    }

    internal sealed record CapturedRequest(Uri? RequestUri, string FormBody);

    private sealed class CapturingLogger : ILogger<AliExpressAffiliateReportsClient>
    {
        public List<string> AllMessages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => new NoopScope();

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            AllMessages.Add(formatter(state, exception));
        }

        private sealed class NoopScope : IDisposable
        {
            public void Dispose() { }
        }
    }
}
