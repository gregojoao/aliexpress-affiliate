# Changelog

All notable changes to **AliExpress.Affiliate** are documented in this file. The
format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and
this project adheres to [Semantic Versioning](https://semver.org/).

## [1.1.0] - 2026-05-17

### Fixed (during pre-release validation)

- Monetary values returned by `aliexpress.affiliate.order.list` arrive as integer JSON
  numbers in the smallest currency unit (e.g. `1113` for `$11.13`). The SDK now
  detects this shape and scales by 100; decimal numbers and strings continue to be
  parsed as major units. Without the fix, every conversion appeared 100× inflated.
- `MapConversion` was looking up `settle_currency` (typo) instead of the actual
  `settled_currency` field returned by the TOP gateway, causing the currency to
  silently fall back to `"USD"` even when the real value matched.
- AliExpress signals "no records in this window" with `resp_code = 405` ("The result
  is empty") on a HTTP 200 response. The SDK now translates that into a zero-item
  `AliExpressConversionPage` / zero-valued `AliExpressSalesSummary` instead of
  raising `AliExpressAffiliateApiException`.

### Added

- New `AliExpress.Affiliate.Reports` area for dashboard / reporting workloads,
  built on top of the official AliExpress Open Platform (TOP) gateway. No portal
  scraping.
- `IAliExpressAffiliateReportsClient` / `AliExpressAffiliateReportsClient` with:
  - `ListConversionsAsync` — paginated conversions backed by
    `aliexpress.affiliate.order.list`.
  - `GetConversionAsync` — single order with line items via
    `aliexpress.affiliate.order.get`.
  - `GetSalesSummaryAsync` — aggregated sales summary (gross revenue, commission,
    by-status, top products, top sub-ids) computed from the conversion stream.
  - `GetClickStatsAsync` / `GetGeneratedLinkUsageAsync` — return
    `Supported = false` because TOP does not expose those metrics; callers can
    surface this as a "manual import" signal.
- `AliExpressAffiliateReportsOptions` with sensible defaults (SHA-256 signing,
  GMT+8 time conversion, 30 s per-call timeout, optional `AccessToken`).
- Dedicated reporting exceptions, each carrying `Code` / `SubCode` / `RequestId`
  for support correlation: `AliExpressAffiliateAuthException`,
  `AliExpressAffiliateRateLimitException`,
  `AliExpressAffiliateNotFoundException`,
  `AliExpressAffiliateUnsupportedException`. Anything else maps to the
  pre-existing `AliExpressAffiliateApiException` (now also exposing the same
  three properties).
- DI extension `AddAliExpressAffiliateReports(...)` covering the same shapes as
  `AddAliExpressAffiliate` (no-arg, action, configuration, configuration +
  section). Binds to `AliExpress:Affiliate:Reports` by default.
- xUnit tests covering signature vectors (MD5 + SHA-256), GMT+8 conversion,
  pagination, error classification, transient retry policy, and credential
  masking in logs.

### Changed

- `AliExpressAffiliateApiException` now also surfaces `Code`, `SubCode` and
  `RequestId` (additive — the existing single-string constructor and behavior
  are preserved).

### Compatibility

- No breaking changes to the v1.0 public surface. Existing
  `AliExpressAffiliateClient` consumers continue to work unchanged.

## [1.0.1] - 2026 — see git history

- Maintenance release: NuGet metadata fixes.

## [1.0.0] - 2026 — see git history

- First stable SDK release.
