using AliExpress.Affiliate.Configuration;
using System.Globalization;

namespace AliExpress.Affiliate.Infrastructure.OpenPlatform;

internal sealed class OpenPlatformParameterBuilder
{
    private readonly Dictionary<string, string> _parameters = new();

    public OpenPlatformParameterBuilder Add(string key, string value)
    {
        _parameters[key] = value;
        return this;
    }

    public OpenPlatformParameterBuilder Add(string key, int value)
    {
        _parameters[key] = value.ToString(CultureInfo.InvariantCulture);
        return this;
    }

    public OpenPlatformParameterBuilder Add(string key, DateTimeOffset? value)
    {
        _parameters[key] = value.HasValue
            ? value.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            : string.Empty;
        return this;
    }

    public OpenPlatformParameterBuilder AddTargeting(
        string targetCurrency,
        string optionTargetCurrency,
        string targetLanguage,
        string optionTargetLanguage)
    {
        return Add("target_currency", OpenPlatformText.FirstNonEmpty(targetCurrency, optionTargetCurrency))
            .Add("target_language", OpenPlatformText.FirstNonEmpty(targetLanguage, optionTargetLanguage));
    }

    public OpenPlatformParameterBuilder AddTracking(
        string trackingId,
        AliExpressAffiliateOptions options)
    {
        return Add("tracking_id", OpenPlatformText.FirstNonEmpty(trackingId, options.DefaultTrackingId));
    }

    public OpenPlatformParameterBuilder AddCountry(
        string key,
        string country,
        AliExpressAffiliateOptions options)
    {
        return Add(key, OpenPlatformText.FirstNonEmpty(country, options.DefaultShipToCountry));
    }

    public Dictionary<string, string> Build()
    {
        return _parameters;
    }
}
