namespace AliExpress.Affiliate.Reports.Application;

/// <summary>
/// Bucket size requested for time-series reports.
/// </summary>
public enum ReportGranularity
{
    /// <summary>One bucket per hour.</summary>
    Hour = 0,

    /// <summary>One bucket per day.</summary>
    Day
}
