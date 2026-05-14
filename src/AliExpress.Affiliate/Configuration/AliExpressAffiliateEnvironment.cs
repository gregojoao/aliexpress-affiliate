namespace AliExpress.Affiliate.Configuration;

public static class AliExpressAffiliateEnvironment
{
    public static AliExpressAffiliateOptions FromDictionary(
        IReadOnlyDictionary<string, string>? values)
    {
        return new AliExpressAffiliateOptions
        {
            ApiEndpoint = FirstNonEmpty(
                GetValue(values, "ALIEXPRESS_AFFILIATE_API_ENDPOINT"),
                GetValue(values, "ALIEXPRESS_ENDPOINT"),
                AliExpressAffiliateOptions.DefaultEndpoint),
            AppKey = FirstNonEmpty(
                GetValue(values, "ALIEXPRESS_AFFILIATE_APP_KEY"),
                GetValue(values, "ALIEXPRESS_OPEN_API_APP_KEY"),
                GetValue(values, "ALIEXPRESS_APP_KEY")),
            AppSecret = FirstNonEmpty(
                GetValue(values, "ALIEXPRESS_AFFILIATE_APP_SECRET"),
                GetValue(values, "ALIEXPRESS_OPEN_API_APP_SECRET"),
                GetValue(values, "ALIEXPRESS_APP_SECRET")),
            DefaultTrackingId = FirstNonEmpty(
                GetValue(values, "ALIEXPRESS_TRACKING_ID"),
                GetValue(values, "ALIEXPRESS_AFFILIATE_TRACKING_ID")),
            AppSignature = GetValue(values, "ALIEXPRESS_APP_SIGNATURE"),
            SignMethod = FirstNonEmpty(
                GetValue(values, "ALIEXPRESS_AFFILIATE_API_SIGN_METHOD"),
                GetValue(values, "ALIEXPRESS_SIGN_METHOD"),
                AliExpressAffiliateOptions.DefaultSignMethod),
            DefaultPromotionLinkType = FirstNonEmpty(
                GetValue(values, "ALIEXPRESS_PROMOTION_LINK_TYPE"),
                AliExpressAffiliateOptions.FallbackPromotionLinkType),
            DefaultShipToCountry = GetValue(values, "ALIEXPRESS_SHIP_TO_COUNTRY"),
            DefaultTargetCurrency = GetValue(values, "ALIEXPRESS_TARGET_CURRENCY"),
            DefaultTargetLanguage = GetValue(values, "ALIEXPRESS_TARGET_LANGUAGE"),
            TimeoutMilliseconds = GetPositiveInt(
                values,
                "ALIEXPRESS_AFFILIATE_API_TIMEOUT_MS",
                AliExpressAffiliateOptions.DefaultTimeoutMilliseconds)
        };
    }

    private static string GetValue(
        IReadOnlyDictionary<string, string>? values,
        string key)
    {
        if (values == null)
        {
            return string.Empty;
        }

        if (values.TryGetValue(key, out var value))
        {
            return value?.Trim() ?? string.Empty;
        }

        foreach (var pair in values)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Value?.Trim() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static bool GetBoolean(
        IReadOnlyDictionary<string, string>? values,
        string key)
    {
        var value = GetValue(values, key);

        return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    private static int GetPositiveInt(
        IReadOnlyDictionary<string, string>? values,
        string key,
        int defaultValue)
    {
        var value = GetValue(values, key);

        return int.TryParse(value, out var parsed) && parsed > 0
            ? parsed
            : defaultValue;
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }
}
