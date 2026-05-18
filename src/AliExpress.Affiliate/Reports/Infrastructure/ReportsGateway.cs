using AliExpress.Affiliate.OpenPlatform;
using AliExpress.Affiliate.Reports.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;

namespace AliExpress.Affiliate.Reports.Infrastructure;

/// <summary>
/// Thin transport over the injected <see cref="HttpClient"/> with:
///   - per-call timeout via a linked <see cref="CancellationTokenSource"/>
///   - one automatic retry for HTTP 5xx and timeouts (4xx and TOP business errors are surfaced immediately)
///   - sensitive-value-aware logging (no secret or access token is ever logged)
/// </summary>
internal sealed class ReportsGateway
{
    private const int MaxRetries = 1;

    private readonly HttpClient _httpClient;
    private readonly ILogger? _logger;

    public ReportsGateway(HttpClient httpClient, ILogger? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger;
    }

    public async Task<string> SendAsync(
        AliExpressOpenPlatformRequest request,
        AliExpressAffiliateReportsOptions options,
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= MaxRetries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var perCallCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (options.Timeout > TimeSpan.Zero)
            {
                perCallCts.CancelAfter(options.Timeout);
            }

            try
            {
                using var content = new FormUrlEncodedContent(request.BodyParameters);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded")
                {
                    CharSet = "utf-8"
                };

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, request.RequestUri)
                {
                    Content = content
                };

                LogAttempt(attempt, request, options);

                using var response = await _httpClient.SendAsync(httpRequest, perCallCts.Token).ConfigureAwait(false);
                var requestId = response.Headers.TryGetValues("x-ca-request-id", out var headerValues)
                    ? string.Join(",", headerValues)
                    : null;
                var responseBody = await response.Content.ReadAsStringAsync(perCallCts.Token).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    return responseBody;
                }

                if (IsTransient(response.StatusCode) && attempt < MaxRetries)
                {
                    LogRetry(attempt, response.StatusCode);
                    attempt++;
                    continue;
                }

                ReportsErrorClassifier.ThrowForHttpStatus(response.StatusCode, responseBody, requestId);
                // ThrowForHttpStatus always throws.
                return responseBody;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = new TimeoutException($"AliExpress affiliate report request timed out after {options.Timeout}.");
                if (attempt < MaxRetries)
                {
                    LogRetry(attempt, "timeout");
                    attempt++;
                    continue;
                }

                throw lastException;
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                if (attempt < MaxRetries)
                {
                    LogRetry(attempt, ex.GetType().Name);
                    attempt++;
                    continue;
                }

                throw;
            }
        }

        // Unreachable: every loop body either returns or throws.
        throw lastException ?? new InvalidOperationException("AliExpress affiliate report request failed for an unknown reason.");
    }

    private static bool IsTransient(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return code >= 500 && code < 600;
    }

    private void LogAttempt(int attempt, AliExpressOpenPlatformRequest request, AliExpressAffiliateReportsOptions options)
    {
        if (_logger is null || !_logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        request.BodyParameters.TryGetValue("method", out var method);
        _logger.LogDebug(
            "AliExpress affiliate report request attempt={Attempt} method={Method} app_key={MaskedAppKey} endpoint={Endpoint}",
            attempt,
            method ?? "?",
            MaskCredential(options.AppKey),
            request.RequestUri);
    }

    private void LogRetry(int attempt, object reason)
    {
        if (_logger is null || !_logger.IsEnabled(LogLevel.Warning))
        {
            return;
        }

        _logger.LogWarning(
            "AliExpress affiliate report request attempt={Attempt} failed transiently ({Reason}); retrying once.",
            attempt,
            reason);
    }

    internal static string MaskCredential(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Length <= 4)
        {
            return new string('*', value.Length);
        }

        return $"{value[0]}***{value[^1]}";
    }
}
