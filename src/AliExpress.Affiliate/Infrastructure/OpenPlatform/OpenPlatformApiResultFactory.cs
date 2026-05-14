using AliExpress.Affiliate.Domain;
using System.Text.Json;

namespace AliExpress.Affiliate.Infrastructure.OpenPlatform;

internal static class OpenPlatformApiResultFactory
{
    public static AliExpressAffiliateApiResult<T> Create<T>(
        string responseBody,
        JsonElement result,
        IReadOnlyList<T> items)
    {
        return new AliExpressAffiliateApiResult<T>(
            Items: items,
            CurrentPageNumber: OpenPlatformJsonReader.GetPropertyInt(result, "current_page_no"),
            CurrentRecordCount: OpenPlatformJsonReader.GetPropertyInt(result, "current_record_count"),
            TotalPageCount: OpenPlatformJsonReader.GetPropertyInt(result, "total_page_no"),
            TotalRecordCount: FirstNonZero(
                OpenPlatformJsonReader.GetPropertyInt(result, "total_record_count"),
                OpenPlatformJsonReader.GetPropertyInt(result, "total_result_count")),
            IsFinished: OpenPlatformJsonReader.GetPropertyBool(result, "is_finished"),
            RawJson: responseBody);
    }

    private static int FirstNonZero(params int[] values)
    {
        return values.FirstOrDefault(value => value != 0);
    }
}
