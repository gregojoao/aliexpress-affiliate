using AliExpress.Affiliate.Reports.Infrastructure.Signing;
using FluentAssertions;

namespace AliExpress.Affiliate.Tests.Reports;

public class TopSignatureBuilderTests
{
    [Fact]
    public void Build_WithMd5_ShouldMatchKnownVector()
    {
        var parameters = new Dictionary<string, string>
        {
            ["app_key"] = "123456",
            ["format"] = "json",
            ["method"] = "aliexpress.affiliate.order.list",
            ["sign_method"] = "md5",
            ["timestamp"] = "1778688000000",
            ["v"] = "2.0",
            ["start_time"] = "2026-05-01 00:00:00",
            ["end_time"] = "2026-05-02 00:00:00",
            ["page_no"] = "1",
            ["page_size"] = "50"
        };

        var signature = TopSignatureBuilder.Build(parameters, "secret", "md5");

        signature.Should().Be("BE5339F9B6DDFAB626235DFB0FD7AA07");
    }

    [Fact]
    public void Build_WithSha256_ShouldMatchKnownVector()
    {
        var parameters = new Dictionary<string, string>
        {
            ["app_key"] = "123456",
            ["format"] = "json",
            ["method"] = "aliexpress.affiliate.order.list",
            ["sign_method"] = "sha256",
            ["timestamp"] = "1778688000000",
            ["v"] = "2.0",
            ["start_time"] = "2026-05-01 00:00:00",
            ["end_time"] = "2026-05-02 00:00:00",
            ["page_no"] = "1",
            ["page_size"] = "50"
        };

        var signature = TopSignatureBuilder.Build(parameters, "secret", "sha256");

        signature.Should().Be("4E9DDDE56747030DE1860813EF290D51207C7C9A6B52B09FB672689AEFD9423A");
    }

    [Fact]
    public void Build_ShouldIgnoreSignParameterDuringSigning()
    {
        var baseParameters = new Dictionary<string, string>
        {
            ["app_key"] = "123456",
            ["method"] = "aliexpress.affiliate.order.list",
            ["timestamp"] = "1778688000000"
        };
        var withSign = new Dictionary<string, string>(baseParameters) { ["sign"] = "junk" };

        var withoutSign = TopSignatureBuilder.Build(baseParameters, "secret", "sha256");
        var withSignature = TopSignatureBuilder.Build(withSign, "secret", "sha256");

        withoutSign.Should().Be(withSignature);
    }

    [Fact]
    public void Build_ShouldSortParametersAlphabetically()
    {
        var ordered = new Dictionary<string, string>
        {
            ["a"] = "1",
            ["b"] = "2",
            ["c"] = "3"
        };
        var unordered = new Dictionary<string, string>
        {
            ["c"] = "3",
            ["a"] = "1",
            ["b"] = "2"
        };

        TopSignatureBuilder.Build(ordered, "secret", "sha256")
            .Should().Be(TopSignatureBuilder.Build(unordered, "secret", "sha256"));
    }

    [Theory]
    [InlineData("sha256", "sha256")]
    [InlineData("hmac-sha256", "sha256")]
    [InlineData("hmacsha256", "sha256")]
    [InlineData("md5", "md5")]
    [InlineData("hmac-md5", "hmac")]
    [InlineData("hmac", "hmac")]
    public void Normalize_ShouldMapAcceptedAliases(string input, string expected)
    {
        TopSignatureBuilder.Normalize(input).Should().Be(expected);
    }
}
