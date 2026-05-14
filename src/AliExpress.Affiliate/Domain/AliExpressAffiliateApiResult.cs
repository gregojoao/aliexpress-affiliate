namespace AliExpress.Affiliate;

public sealed record AliExpressAffiliateApiResult<T>(
    IReadOnlyList<T> Items,
    int CurrentPageNo,
    int CurrentRecordCount,
    int TotalPageNo,
    int TotalRecordCount,
    bool IsFinished,
    string RawJson);
