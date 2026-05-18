using AliExpress.Affiliate.Exceptions;
using AliExpress.Affiliate.Infrastructure.OpenPlatform;
using AliExpress.Affiliate.Reports.Exceptions;
using System.Net;
using System.Text.Json;

namespace AliExpress.Affiliate.Reports.Infrastructure;

/// <summary>
/// Inspects a TOP error payload and routes it to the most specific Reports exception.
/// </summary>
internal static class ReportsErrorClassifier
{
    public static void ThrowForHttpStatus(HttpStatusCode statusCode, string responseBody, string? requestId)
    {
        if (statusCode == HttpStatusCode.TooManyRequests)
        {
            throw new AliExpressAffiliateRateLimitException(
                $"AliExpress affiliate report request was rate-limited (HTTP 429). {Trim(responseBody)}",
                code: ((int)statusCode).ToString(),
                requestId: requestId);
        }

        if (statusCode == HttpStatusCode.Unauthorized || statusCode == HttpStatusCode.Forbidden)
        {
            throw new AliExpressAffiliateAuthException(
                $"AliExpress affiliate report request was rejected for credential reasons (HTTP {(int)statusCode}). {Trim(responseBody)}",
                code: ((int)statusCode).ToString(),
                requestId: requestId);
        }

        throw new AliExpressAffiliateHttpException(statusCode, responseBody);
    }

    public static void ThrowForTopError(JsonElement root, string responseBody)
    {
        if (!TryReadError(root, out var code, out var subCode, out var message, out var requestId))
        {
            return;
        }

        var fullMessage = ComposeMessage(code, subCode, message, responseBody);

        if (IsAuthCode(code, subCode))
        {
            throw new AliExpressAffiliateAuthException(fullMessage, code, subCode, requestId);
        }

        if (IsRateLimitCode(code, subCode))
        {
            throw new AliExpressAffiliateRateLimitException(fullMessage, code, subCode, requestId);
        }

        if (IsNotFoundCode(code, subCode))
        {
            throw new AliExpressAffiliateNotFoundException(fullMessage, code, subCode, requestId);
        }

        if (IsUnsupportedCode(code, subCode))
        {
            throw new AliExpressAffiliateUnsupportedException(fullMessage, code, subCode, requestId);
        }

        throw new AliExpressAffiliateApiException(fullMessage, code, subCode, requestId);
    }

    private static bool TryReadError(
        JsonElement root,
        out string? code,
        out string? subCode,
        out string? message,
        out string? requestId)
    {
        code = null;
        subCode = null;
        message = null;
        requestId = null;

        if (root.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (OpenPlatformJsonReader.TryGetProperty(root, "error_response", out var errorResponse))
        {
            ReadCommon(errorResponse, out code, out subCode, out message, out requestId);
            return true;
        }

        var response = FindResponseElement(root);
        var responseResult = OpenPlatformJsonReader.TryGetProperty(response, "resp_result", out var responseResultElement)
            ? responseResultElement
            : response;

        if (OpenPlatformJsonReader.TryGetProperty(responseResult, "resp_code", out var respCodeElement))
        {
            var respCode = OpenPlatformJsonReader.GetScalarString(respCodeElement);
            if (!string.IsNullOrWhiteSpace(respCode) && !respCode.Equals("200", StringComparison.OrdinalIgnoreCase))
            {
                code = respCode;
                message = OpenPlatformJsonReader.GetPropertyString(responseResult, "resp_msg");
                subCode = OpenPlatformJsonReader.GetPropertyString(responseResult, "sub_code");
                requestId = OpenPlatformJsonReader.GetPropertyString(root, "request_id");
                return true;
            }
        }

        return false;
    }

    private static void ReadCommon(
        JsonElement element,
        out string? code,
        out string? subCode,
        out string? message,
        out string? requestId)
    {
        code = NullIfEmpty(OpenPlatformJsonReader.GetPropertyString(element, "code"));
        subCode = NullIfEmpty(OpenPlatformJsonReader.GetPropertyString(element, "sub_code"));
        message = NullIfEmpty(OpenPlatformText.FirstNonEmpty(
            OpenPlatformJsonReader.GetPropertyString(element, "msg"),
            OpenPlatformJsonReader.GetPropertyString(element, "sub_msg")));
        requestId = NullIfEmpty(OpenPlatformJsonReader.GetPropertyString(element, "request_id"));
    }

    private static JsonElement FindResponseElement(JsonElement root)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (property.Name.EndsWith("_response", StringComparison.OrdinalIgnoreCase) &&
                !property.Name.Equals("error_response", StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        return root;
    }

    private static bool IsAuthCode(string? code, string? subCode)
    {
        return MatchesAny(code, subCode,
            "isv.invalid-signature",
            "isv.invalid-appkey",
            "isv.invalid-app-key",
            "isv.invalid-session",
            "isv.session-expired",
            "isv.access-token-expired",
            "isv.access-token-invalid",
            "AuthFailure",
            "AccessDenied")
            || EqualsAny(code, "401", "403");
    }

    private static bool IsRateLimitCode(string? code, string? subCode)
    {
        return MatchesAny(code, subCode,
            "isv.api-flow-limit",
            "isv.flow-limit-exceeded",
            "HTTP_INVOKE_LIMITED",
            "isp.flow-limit")
            || EqualsAny(code, "429");
    }

    private static bool IsNotFoundCode(string? code, string? subCode)
    {
        return MatchesAny(code, subCode,
            "isv.invalid-parameter:order-not-found",
            "isv.order-not-found",
            "isv.no-order-found")
            || EqualsAny(code, "404");
    }

    private static bool IsUnsupportedCode(string? code, string? subCode)
    {
        return MatchesAny(code, subCode,
            "isv.permission-api-package-gateway-no-auth",
            "isv.api-not-found",
            "isv.permission-denied",
            "isp.service-unknown");
    }

    private static bool MatchesAny(string? code, string? subCode, params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (string.Equals(code, candidate, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(subCode, candidate, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool EqualsAny(string? value, params string[] candidates)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (var candidate in candidates)
        {
            if (string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string ComposeMessage(string? code, string? subCode, string? message, string responseBody)
    {
        var parts = new List<string>(capacity: 3);
        if (!string.IsNullOrWhiteSpace(code))
        {
            parts.Add(code!);
        }

        if (!string.IsNullOrWhiteSpace(subCode) && !string.Equals(subCode, code, StringComparison.OrdinalIgnoreCase))
        {
            parts.Add(subCode!);
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            parts.Add(message!);
        }

        if (parts.Count == 0)
        {
            return $"AliExpress affiliate report request failed. {Trim(responseBody)}";
        }

        return $"AliExpress affiliate report request failed: {string.Join(" - ", parts)}";
    }

    private static string Trim(string responseBody)
    {
        if (string.IsNullOrEmpty(responseBody))
        {
            return string.Empty;
        }

        const int maxLength = 512;
        return responseBody.Length <= maxLength
            ? responseBody
            : responseBody[..maxLength] + "…";
    }

    private static string? NullIfEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
