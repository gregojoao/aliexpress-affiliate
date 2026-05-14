namespace AliExpress.Affiliate.Infrastructure.OpenPlatform;

internal static class OpenPlatformText
{
    public static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }
}
