using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AliExpress.Affiliate.Tests;

public class AliExpressAffiliateClientTests
{
    [Fact]
    public void CreateTopSignature_WithMd5Parameters_ShouldUseUppercaseTopSignature()
    {
        var parameters = new Dictionary<string, string>
        {
            ["app_key"] = "123456",
            ["format"] = "json",
            ["method"] = "aliexpress.affiliate.link.generate",
            ["promotion_link_type"] = "0",
            ["sign_method"] = "md5",
            ["source_values"] = "https://pt.aliexpress.com/item/1005006860981590.html",
            ["timestamp"] = "1778688000000",
            ["tracking_id"] = "telegram",
            ["v"] = "2.0"
        };

        var signature = AliExpressAffiliateClient.CreateTopSignature(
            parameters,
            "secret",
            "md5");

        signature.Should().Be("641E9282AAE8DD8DFA5A063D5E111BB7");
    }

    [Fact]
    public void BuildLinkGenerateRequest_ShouldUseAliExpressOpenPlatformShape()
    {
        var request = AliExpressAffiliateClient.BuildLinkGenerateRequest(
            "https://pt.aliexpress.com/item/1005006860981590.html",
            CreateOptions(),
            DateTimeOffset.FromUnixTimeMilliseconds(1778688000000));

        request.RequestUri.AbsoluteUri.Should().Be("https://api-sg.aliexpress.com/sync");
        request.BodyParameters.Should().Contain("method", "aliexpress.affiliate.link.generate");
        request.BodyParameters.Should().Contain("app_key", "534190");
        request.BodyParameters.Should().Contain("sign_method", "md5");
        request.BodyParameters.Should().Contain("v", "2.0");
        request.BodyParameters.Should().Contain("source_values", "https://pt.aliexpress.com/item/1005006860981590.html");
        request.BodyParameters.Should().Contain("tracking_id", "telegram_greco");
        request.BodyParameters.Should().Contain("promotion_link_type", "0");
        request.BodyParameters.Should().ContainKey("sign");
        request.BodyParameters.Should().NotContainKey("ship_to_country");
    }

    [Fact]
    public void BuildProductDetailRequest_ShouldUseProductDetailParameters()
    {
        var request = AliExpressAffiliateClient.BuildProductDetailRequest(
            "1005010592960109",
            CreateOptions(),
            DateTimeOffset.FromUnixTimeMilliseconds(1778688000000));

        request.RequestUri.AbsoluteUri.Should().Be("https://api-sg.aliexpress.com/sync");
        request.BodyParameters.Should().Contain("method", "aliexpress.affiliate.productdetail.get");
        request.BodyParameters.Should().Contain("product_ids", "1005010592960109");
        request.BodyParameters.Should().Contain("tracking_id", "telegram_greco");
        request.BodyParameters.Should().Contain("ship_to_country", "BR");
        request.BodyParameters.Should().Contain("country", "BR");
        request.BodyParameters.Should().Contain("target_currency", "BRL");
        request.BodyParameters.Should().Contain("target_language", "PT");
    }

    [Fact]
    public void BuildApiRequest_ShouldSignGenericAffiliateMethods()
    {
        var request = AliExpressAffiliateClient.BuildApiRequest(
            "aliexpress.affiliate.product.query",
            CreateOptions(),
            DateTimeOffset.FromUnixTimeMilliseconds(1778688000000),
            new Dictionary<string, string>
            {
                ["keywords"] = "microfone",
                ["page_no"] = "2",
                ["page_size"] = "20",
                ["target_currency"] = "BRL",
                ["target_language"] = "PT",
                ["ship_to_country"] = "BR",
                ["tracking_id"] = "telegram_greco"
            });

        request.BodyParameters.Should().Contain("method", "aliexpress.affiliate.product.query");
        request.BodyParameters.Should().Contain("keywords", "microfone");
        request.BodyParameters.Should().Contain("page_no", "2");
        request.BodyParameters.Should().Contain("page_size", "20");
        request.BodyParameters.Should().Contain("target_currency", "BRL");
        request.BodyParameters.Should().ContainKey("sign");
    }

    [Fact]
    public async Task GenerateAffiliateLinkAsync_ShouldReadPromotionLinkAndMergeProductDetails()
    {
        var handler = new QueueingHandler(
            """
            {
              "resp_result": {
                "result": {
                  "promotion_links": [
                    {
                      "promotion_link": "https://s.click.aliexpress.com/e/_generated"
                    }
                  ]
                },
                "resp_code": 200
              }
            }
            """,
            """
            {
              "resp_result": {
                "result": {
                  "products": {
                    "product": [
                      {
                        "product_title": "Fifine microfone dinamico usb/xlr",
                        "target_sale_price": "264.51",
                        "target_sale_price_currency": "BRL",
                        "target_original_price": "499.05",
                        "target_original_price_currency": "BRL",
                        "product_main_image_url": "https://ae-pic-a1.aliexpress-media.com/kf/product.jpg",
                        "product_detail_url": "https://pt.aliexpress.com/item/1005006356702381.html",
                        "promotion_link": "https://s.click.aliexpress.com/s/detail"
                      }
                    ]
                  }
                },
                "resp_code": 200
              }
            }
            """);
        var client = new AliExpressAffiliateClient(
            new HttpClient(handler),
            () => DateTimeOffset.FromUnixTimeMilliseconds(1778688000000));

        var result = await client.GenerateAffiliateLinkAsync(
            "https://pt.aliexpress.com/item/1005006356702381.html?spm=abc",
            CreateOptions(includeProductDetails: true));

        result.Should().NotBeNull();
        result!.AffiliateUrl.Should().Be("https://s.click.aliexpress.com/e/_generated");
        result.SourceUrl.Should().Be("https://pt.aliexpress.com/item/1005006356702381.html");
        result.ProductTitle.Should().Be("Fifine microfone dinamico usb/xlr");
        result.ProductPrice.Should().Be("R$ 264,51");
        result.ProductOriginalPrice.Should().Be("R$ 499,05");
        result.ProductImageUrl.Should().Be("https://ae-pic-a1.aliexpress-media.com/kf/product.jpg");
        handler.Requests.Should().HaveCount(2);
        handler.Requests[0].RequestBody.Should().Contain("method=aliexpress.affiliate.link.generate");
        handler.Requests[0].RequestBody.Should().Contain("promotion_link_type=0");
        handler.Requests[0].RequestBody.Should().NotContain("promotion_link_type=2");
        handler.Requests[1].RequestBody.Should().Contain("method=aliexpress.affiliate.productdetail.get");
        handler.Requests[1].RequestBody.Should().Contain("product_ids=1005006356702381");
    }

    [Fact]
    public async Task GenerateAffiliateLinkAsync_WithDefaultOptionsFromConstructor_ShouldUseConfiguredOptions()
    {
        var handler = new QueueingHandler("""
        {
          "resp_result": {
            "result": {
              "promotion_links": [
                {
                  "promotion_link": "https://s.click.aliexpress.com/e/_configured"
                }
              ]
            },
            "resp_code": 200
          }
        }
        """);
        var client = new AliExpressAffiliateClient(
            new HttpClient(handler),
            CreateOptions(),
            () => DateTimeOffset.FromUnixTimeMilliseconds(1778688000000));

        var result = await client.GenerateAffiliateLinkAsync(
            "https://pt.aliexpress.com/item/1005006356702381.html");

        result!.AffiliateUrl.Should().Be("https://s.click.aliexpress.com/e/_configured");
        handler.Requests[0].RequestBody.Should().Contain("tracking_id=telegram_greco");
    }

    [Fact]
    public async Task GenerateAffiliateLinkAsync_WithoutDefaultOptions_ShouldAskCallerToPassOptions()
    {
        var client = new AliExpressAffiliateClient(new HttpClient(new QueueingHandler()));

        Func<Task> act = async () => await client.GenerateAffiliateLinkAsync(
            "https://pt.aliexpress.com/item/1005006356702381.html");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Pass options to this method*");
    }

    [Fact]
    public async Task AddAliExpressAffiliate_WithConfiguration_ShouldRegisterClientWithDefaultOptions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AliExpress:Affiliate:Endpoint"] = "https://api-sg.aliexpress.com/sync",
                ["AliExpress:Affiliate:AppKey"] = "534190",
                ["AliExpress:Affiliate:AppSecret"] = "app-secret",
                ["AliExpress:Affiliate:TrackingId"] = "telegram_greco",
                ["AliExpress:Affiliate:SignMethod"] = "md5",
                ["AliExpress:Affiliate:PromotionLinkType"] = "0",
                ["AliExpress:Affiliate:ShipToCountry"] = "BR",
                ["AliExpress:Affiliate:TargetCurrency"] = "BRL",
                ["AliExpress:Affiliate:TargetLanguage"] = "PT"
            })
            .Build();
        var handler = new QueueingHandler("""
        {
          "resp_result": {
            "result": {
              "promotion_links": [
                {
                  "promotion_link": "https://s.click.aliexpress.com/e/_di"
                }
              ]
            },
            "resp_code": 200
          }
        }
        """);
        var services = new ServiceCollection()
            .AddAliExpressAffiliate(configuration);
        services.AddSingleton<HttpMessageHandler>(handler);
        services.ConfigureHttpClientDefaults(builder =>
            builder.ConfigurePrimaryHttpMessageHandler(sp => sp.GetRequiredService<HttpMessageHandler>()));

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<AliExpressAffiliateClient>();

        var result = await client.GenerateAffiliateLinkAsync(
            "https://pt.aliexpress.com/item/1005006356702381.html");

        result!.AffiliateUrl.Should().Be("https://s.click.aliexpress.com/e/_di");
        handler.Requests[0].RequestBody.Should().Contain("tracking_id=telegram_greco");
    }

    [Fact]
    public async Task AddAliExpressAffiliate_WithConfigureAction_ShouldRegisterClientWithDefaultOptions()
    {
        var handler = new QueueingHandler("""
        {
          "resp_result": {
            "result": {
              "promotion_links": [
                {
                  "promotion_link": "https://s.click.aliexpress.com/e/_action"
                }
              ]
            },
            "resp_code": 200
          }
        }
        """);
        var services = new ServiceCollection()
            .AddAliExpressAffiliate(options =>
            {
                options.AppKey = "534190";
                options.AppSecret = "app-secret";
                options.TrackingId = "telegram_greco";
                options.SignMethod = "md5";
                options.PromotionLinkType = "0";
            });
        services.AddSingleton<HttpMessageHandler>(handler);
        services.ConfigureHttpClientDefaults(builder =>
            builder.ConfigurePrimaryHttpMessageHandler(sp => sp.GetRequiredService<HttpMessageHandler>()));

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<AliExpressAffiliateClient>();

        var result = await client.GenerateAffiliateLinkAsync(
            "https://pt.aliexpress.com/item/1005006356702381.html");

        result!.AffiliateUrl.Should().Be("https://s.click.aliexpress.com/e/_action");
    }

    [Fact]
    public async Task GenerateAffiliateLinksAsync_ShouldGenerateLinksInBatch()
    {
        var handler = new QueueingHandler("""
        {
          "resp_result": {
            "result": {
              "promotion_links": {
                "promotion_link": [
                  {
                    "source_value": "https://pt.aliexpress.com/item/1005006356702381.html",
                    "promotion_link": "https://s.click.aliexpress.com/e/_one"
                  },
                  {
                    "source_value": "https://pt.aliexpress.com/item/1005006860981590.html",
                    "promotion_link": "https://s.click.aliexpress.com/e/_two"
                  }
                ]
              }
            },
            "resp_code": 200
          }
        }
        """);
        var client = new AliExpressAffiliateClient(
            new HttpClient(handler),
            () => DateTimeOffset.FromUnixTimeMilliseconds(1778688000000));

        var result = await client.GenerateAffiliateLinksAsync(
            new[]
            {
                "https://pt.aliexpress.com/item/1005006356702381.html?spm=abc",
                "https://pt.aliexpress.com/item/1005006860981590.html"
            },
            CreateOptions());

        result.Should().HaveCount(2);
        result[0].PromotionLink.Should().Be("https://s.click.aliexpress.com/e/_one");
        handler.Requests.Should().HaveCount(1);
        handler.Requests[0].RequestBody.Should().Contain("method=aliexpress.affiliate.link.generate");
        handler.Requests[0].RequestBody.Should().Contain("source_values=https%3A%2F%2Fpt.aliexpress.com%2Fitem%2F1005006356702381.html%2Chttps%3A%2F%2Fpt.aliexpress.com%2Fitem%2F1005006860981590.html");
    }

    [Theory]
    [InlineData("SearchProductsAsync", "aliexpress.affiliate.product.query")]
    [InlineData("GetHotProductsAsync", "aliexpress.affiliate.hotproduct.query")]
    public async Task ProductQueries_ShouldSendExpectedMethodAndParseProducts(
        string operation,
        string expectedMethod)
    {
        var handler = new QueueingHandler(ProductPageJson);
        var client = new AliExpressAffiliateClient(
            new HttpClient(handler),
            () => DateTimeOffset.FromUnixTimeMilliseconds(1778688000000));
        var query = new AliExpressProductQuery
        {
            Keywords = "microfone",
            PageNo = 2,
            PageSize = 20,
            Sort = "SALE_PRICE_ASC"
        };

        var result = operation == "SearchProductsAsync"
            ? await client.SearchProductsAsync(query, CreateOptions())
            : await client.GetHotProductsAsync(query, CreateOptions());

        result.CurrentPageNo.Should().Be(2);
        result.TotalRecordCount.Should().Be(123);
        result.Items.Should().HaveCount(1);
        result.Items[0].ProductId.Should().Be("1005006356702381");
        result.Items[0].ProductPrice.Should().Be("R$ 264,51");
        handler.Requests[0].RequestBody.Should().Contain($"method={expectedMethod}");
        handler.Requests[0].RequestBody.Should().Contain("keywords=microfone");
        handler.Requests[0].RequestBody.Should().Contain("page_no=2");
        handler.Requests[0].RequestBody.Should().Contain("page_size=20");
        handler.Requests[0].RequestBody.Should().Contain("target_currency=BRL");
        handler.Requests[0].RequestBody.Should().Contain("tracking_id=telegram_greco");
    }

    [Fact]
    public async Task GetHotProductDownloadAsync_ShouldSendDownloadParameters()
    {
        var handler = new QueueingHandler(ProductPageJson);
        var client = new AliExpressAffiliateClient(
            new HttpClient(handler),
            () => DateTimeOffset.FromUnixTimeMilliseconds(1778688000000));

        var result = await client.GetHotProductDownloadAsync(
            new AliExpressHotProductDownloadQuery
            {
                CategoryId = "111",
                LocaleSite = "global",
                PageNo = 1,
                PageSize = 10
            },
            CreateOptions());

        result.Items.Should().ContainSingle();
        handler.Requests[0].RequestBody.Should().Contain("method=aliexpress.affiliate.hotproduct.download");
        handler.Requests[0].RequestBody.Should().Contain("category_id=111");
        handler.Requests[0].RequestBody.Should().Contain("locale_site=global");
        handler.Requests[0].RequestBody.Should().Contain("country=BR");
    }

    [Fact]
    public async Task GetCategoriesAsync_ShouldParseCategories()
    {
        var handler = new QueueingHandler("""
        {
          "aliexpress_affiliate_category_get_response": {
            "resp_result": {
              "resp_code": 200,
              "result": {
                "categories": {
                  "category": [
                    {
                      "category_id": 111,
                      "category_name": "Audio",
                      "parent_category_id": 0
                    }
                  ]
                },
                "total_result_count": 1
              }
            }
          }
        }
        """);
        var client = new AliExpressAffiliateClient(
            new HttpClient(handler),
            () => DateTimeOffset.FromUnixTimeMilliseconds(1778688000000));

        var result = await client.GetCategoriesAsync(CreateOptions());

        result.Items.Should().ContainSingle();
        result.Items[0].CategoryName.Should().Be("Audio");
        result.TotalRecordCount.Should().Be(1);
        handler.Requests[0].RequestBody.Should().Contain("method=aliexpress.affiliate.category.get");
    }

    [Fact]
    public async Task GetFeaturedPromosAsync_ShouldParsePromos()
    {
        var handler = new QueueingHandler("""
        {
          "resp_result": {
            "resp_code": 200,
            "result": {
              "current_record_count": 1,
              "promos": {
                "promo": [
                  {
                    "promo_name": "Hot Product",
                    "promo_desc": "High commission products",
                    "product_num": 100
                  }
                ]
              }
            }
          }
        }
        """);
        var client = new AliExpressAffiliateClient(
            new HttpClient(handler),
            () => DateTimeOffset.FromUnixTimeMilliseconds(1778688000000));

        var result = await client.GetFeaturedPromosAsync(CreateOptions());

        result.Items.Should().ContainSingle();
        result.Items[0].PromoName.Should().Be("Hot Product");
        result.CurrentRecordCount.Should().Be(1);
        handler.Requests[0].RequestBody.Should().Contain("method=aliexpress.affiliate.featuredpromo.get");
    }

    [Fact]
    public async Task GetFeaturedPromoProductsAsync_ShouldSendPromoParameters()
    {
        var handler = new QueueingHandler(ProductPageJson);
        var client = new AliExpressAffiliateClient(
            new HttpClient(handler),
            () => DateTimeOffset.FromUnixTimeMilliseconds(1778688000000));

        var result = await client.GetFeaturedPromoProductsAsync(
            new AliExpressFeaturedPromoProductsQuery
            {
                PromotionName = "Hot Product",
                Sort = "commissionDesc",
                PageSize = 5
            },
            CreateOptions());

        result.Items.Should().ContainSingle();
        handler.Requests[0].RequestBody.Should().Contain("method=aliexpress.affiliate.featuredpromo.products.get");
        handler.Requests[0].RequestBody.Should().Contain("promotion_name=Hot+Product");
        handler.Requests[0].RequestBody.Should().Contain("sort=commissionDesc");
    }

    [Fact]
    public async Task GetSmartMatchProductsAsync_ShouldRequireDeviceIdAndSendParameters()
    {
        var handler = new QueueingHandler(ProductPageJson);
        var client = new AliExpressAffiliateClient(
            new HttpClient(handler),
            () => DateTimeOffset.FromUnixTimeMilliseconds(1778688000000));

        var result = await client.GetSmartMatchProductsAsync(
            new AliExpressSmartMatchQuery
            {
                DeviceId = "device-123",
                Keywords = "microfone",
                PageNo = 1
            },
            CreateOptions());

        result.Items.Should().ContainSingle();
        handler.Requests[0].RequestBody.Should().Contain("method=aliexpress.affiliate.product.smartmatch");
        handler.Requests[0].RequestBody.Should().Contain("device_id=device-123");
        handler.Requests[0].RequestBody.Should().Contain("keywords=microfone");

        Func<Task> act = async () => await client.GetSmartMatchProductsAsync(new AliExpressSmartMatchQuery(), CreateOptions());
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task OrderMethods_ShouldSendExpectedParametersAndParseOrders()
    {
        var handler = new QueueingHandler(OrderPageJson, OrderPageJson, OrderPageJson);
        var client = new AliExpressAffiliateClient(
            new HttpClient(handler),
            () => DateTimeOffset.FromUnixTimeMilliseconds(1778688000000));

        var orders = await client.GetOrdersAsync(
            new AliExpressOrderListQuery
            {
                StartTime = "2026-05-01 00:00:00",
                EndTime = "2026-05-02 00:00:00",
                Status = "Payment Completed",
                PageNo = 1,
                PageSize = 20
            },
            CreateOptions());
        var details = await client.GetOrderDetailsAsync(
            new AliExpressOrderDetailsQuery { OrderIds = "222222" },
            CreateOptions());
        var byIndex = await client.GetOrdersByIndexAsync(
            new AliExpressOrderListByIndexQuery
            {
                StartTime = "2026-05-01 00:00:00",
                EndTime = "2026-05-02 00:00:00",
                Status = "Payment Completed",
                TimeType = "Payment Completed Time",
                StartQueryIndexId = "1000"
            },
            CreateOptions());

        orders.Items[0].SubOrderId.Should().Be("222222");
        details.Items[0].OrderStatus.Should().Be("Payment Completed");
        byIndex.Items[0].TrackingId.Should().Be("telegram_greco");
        handler.Requests[0].RequestBody.Should().Contain("method=aliexpress.affiliate.order.list");
        handler.Requests[0].RequestBody.Should().Contain("status=Payment+Completed");
        handler.Requests[1].RequestBody.Should().Contain("method=aliexpress.affiliate.order.get");
        handler.Requests[1].RequestBody.Should().Contain("order_ids=222222");
        handler.Requests[2].RequestBody.Should().Contain("method=aliexpress.affiliate.order.listbyindex");
        handler.Requests[2].RequestBody.Should().Contain("start_query_index_id=1000");
    }

    [Fact]
    public async Task GenerateAffiliateLinkAsync_WithOnlySourceValue_ShouldThrowUnavailable()
    {
        var handler = new QueueingHandler("""
        {
          "resp_result": {
            "result": {
              "promotion_links": [
                {
                  "source_value": "https://pt.aliexpress.com/item/1005008652606983.html"
                }
              ]
            },
            "resp_code": 200,
            "resp_msg": "Call succeeds"
          }
        }
        """);
        var client = new AliExpressAffiliateClient(
            new HttpClient(handler),
            () => DateTimeOffset.FromUnixTimeMilliseconds(1778688000000));

        Func<Task> act = async () => await client.GenerateAffiliateLinkAsync(
            "https://pt.aliexpress.com/item/1005008652606983.html?spm=abc",
            CreateOptions());

        await act.Should()
            .ThrowAsync<AliExpressAffiliateLinkUnavailableException>()
            .Where(ex =>
                ex.ProductUrl == "https://pt.aliexpress.com/item/1005008652606983.html" &&
                ex.ResponseSummary.Contains("promotion_link=missing", StringComparison.OrdinalIgnoreCase));
        handler.Requests.Should().HaveCount(1);
    }

    [Fact]
    public void NormalizeAliExpressUrl_WithAnyAliExpressHtmlUrl_ShouldTrimAfterHtml()
    {
        var normalized = AliExpressAffiliateClient.NormalizeAliExpressUrl(
            "https://campaign.aliexpress.com/wow/gcp/some-page.html?spm=abc#section");

        normalized.Should().Be("https://campaign.aliexpress.com/wow/gcp/some-page.html");
    }

    [Fact]
    public void ExtractAffiliateUrl_WithOnlySourceValue_ShouldReturnEmpty()
    {
        const string responseBody = """
        {
          "resp_result": {
            "result": {
              "promotion_links": [
                {
                  "source_value": "https://pt.aliexpress.com/item/1005008652606983.html"
                }
              ]
            },
            "resp_code": 200
          }
        }
        """;

        var affiliateUrl = AliExpressAffiliateClient.ExtractAffiliateUrl(responseBody);

        affiliateUrl.Should().BeEmpty();
    }

    [Theory]
    [InlineData("https://pt.aliexpress.com/item/1005010592960109.html?spm=abc", "1005010592960109")]
    [InlineData("https://pt.aliexpress.com/item/foo.html?x_object_id=1005006356702381", "1005006356702381")]
    public void TryExtractProductId_ShouldReadKnownAliExpressProductIds(
        string url,
        string expectedProductId)
    {
        var extracted = AliExpressAffiliateClient.TryExtractProductId(url, out var productId);

        extracted.Should().BeTrue();
        productId.Should().Be(expectedProductId);
    }

    private static AliExpressAffiliateOptions CreateOptions(bool includeProductDetails = false)
    {
        return new AliExpressAffiliateOptions
        {
            Endpoint = "https://api-sg.aliexpress.com/sync",
            AppKey = "534190",
            AppSecret = "app-secret",
            TrackingId = "telegram_greco",
            SignMethod = "md5",
            PromotionLinkType = "0",
            ShipToCountry = "BR",
            TargetCurrency = "BRL",
            TargetLanguage = "PT",
            IncludeProductDetails = includeProductDetails
        };
    }

    private const string ProductPageJson = """
    {
      "resp_result": {
        "resp_code": 200,
        "result": {
          "current_page_no": 2,
          "current_record_count": 1,
          "total_page_no": 10,
          "total_record_count": 123,
          "products": {
            "product": [
              {
                "product_id": 1005006356702381,
                "product_title": "Fifine microfone dinamico usb/xlr",
                "target_sale_price": "264.51",
                "target_sale_price_currency": "BRL",
                "target_original_price": "499.05",
                "target_original_price_currency": "BRL",
                "product_main_image_url": "https://ae-pic-a1.aliexpress-media.com/kf/product.jpg",
                "product_detail_url": "https://pt.aliexpress.com/item/1005006356702381.html",
                "promotion_link": "https://s.click.aliexpress.com/e/_product",
                "commission_rate": "3.5%",
                "hot_product_commission_rate": "60%",
                "discount": "47%",
                "evaluate_rate": "96%",
                "lastest_volume": 300,
                "first_level_category_id": 44,
                "first_level_category_name": "Consumer Electronics",
                "second_level_category_id": 55,
                "second_level_category_name": "Microphones",
                "shop_id": 123,
                "shop_url": "https://www.aliexpress.com/store/123",
                "platform_product_type": "ALL"
              }
            ]
          }
        }
      }
    }
    """;

    private const string OrderPageJson = """
    {
      "resp_result": {
        "resp_code": 200,
        "result": {
          "current_page_no": 1,
          "current_record_count": 1,
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
                "product_detail_url": "https://pt.aliexpress.com/item/1005006356702381.html",
                "commission_rate": "3.5%",
                "estimated_commission": "1.20",
                "paid_commission": "0.00",
                "created_time": "2026-05-01 10:00:00"
              }
            ]
          }
        }
      }
    }
    """;

    private sealed class QueueingHandler : HttpMessageHandler
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

    private sealed record CapturedRequest(Uri? RequestUri, string RequestBody);
}
