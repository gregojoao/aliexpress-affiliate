namespace AliExpress.Affiliate.Application.Ports;

internal sealed record AffiliateLinkLookup(
    string AffiliateUrl,
    string MissingLinkSummary);
