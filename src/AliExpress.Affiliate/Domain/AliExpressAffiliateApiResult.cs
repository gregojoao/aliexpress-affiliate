namespace AliExpress.Affiliate.Domain;

public sealed record AliExpressAffiliateApiResult<T>(
    IReadOnlyList<T> Items,
    int CurrentPageNumber,
    int CurrentRecordCount,
    int TotalPageCount,
    int TotalRecordCount,
    bool IsFinished,
    string RawJson);
