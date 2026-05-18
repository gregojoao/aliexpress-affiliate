namespace AliExpress.Affiliate.Reports.Domain;

/// <summary>
/// Generated-link usage summary. AliExpress' public TOP gateway does not expose link /
/// click attribution to affiliates; <see cref="Supported"/> is <c>false</c> by default
/// and counts are <c>0</c>. Treat as a manual-import signal.
/// </summary>
public sealed record AliExpressLinkUsage(
    int LinksGenerated,
    int ClicksAttributed,
    int ConversionsAttributed,
    Money CommissionAttributed,
    bool Supported,
    string? UnsupportedReason);
