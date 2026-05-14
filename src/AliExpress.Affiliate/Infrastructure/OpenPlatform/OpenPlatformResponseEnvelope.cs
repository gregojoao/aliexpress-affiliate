using AliExpress.Affiliate.Exceptions;
using System.Text.Json;

namespace AliExpress.Affiliate.Infrastructure.OpenPlatform;

internal static class OpenPlatformResponseEnvelope
{
    public static JsonElement ExtractResult(JsonElement root)
    {
        ThrowIfTopError(root);

        var response = FindResponseElement(root);
        var responseResult = OpenPlatformJsonReader.TryGetProperty(response, "resp_result", out var responseResultElement)
            ? responseResultElement
            : response;

        ThrowIfBusinessError(responseResult);

        return OpenPlatformJsonReader.TryGetProperty(responseResult, "result", out var resultElement)
            ? resultElement
            : responseResult;
    }

    public static JsonElement ExtractNamedResult(
        JsonElement root,
        string responsePropertyName)
    {
        ThrowIfTopError(root);

        var response = OpenPlatformJsonReader.TryGetProperty(root, responsePropertyName, out var responseElement)
            ? responseElement
            : root;
        var responseResult = OpenPlatformJsonReader.TryGetProperty(response, "resp_result", out var responseResultElement)
            ? responseResultElement
            : response;

        ThrowIfBusinessError(responseResult);

        return OpenPlatformJsonReader.TryGetProperty(responseResult, "result", out var resultElement)
            ? resultElement
            : responseResult;
    }

    private static JsonElement FindResponseElement(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return root;
        }

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

    private static void ThrowIfTopError(JsonElement root)
    {
        if (OpenPlatformJsonReader.TryGetProperty(root, "error_response", out var errorResponse))
        {
            throw new AliExpressAffiliateApiException(BuildErrorMessage("AliExpress API", errorResponse));
        }
    }

    private static void ThrowIfBusinessError(JsonElement responseResult)
    {
        if (!OpenPlatformJsonReader.TryGetProperty(responseResult, "resp_code", out var responseCodeElement))
        {
            return;
        }

        var responseCode = OpenPlatformJsonReader.GetScalarString(responseCodeElement);
        if (string.IsNullOrWhiteSpace(responseCode) ||
            responseCode.Equals("200", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new AliExpressAffiliateApiException(BuildErrorMessage($"AliExpress API resp_code {responseCode}", responseResult));
    }

    private static string BuildErrorMessage(
        string prefix,
        JsonElement element)
    {
        var code = OpenPlatformText.FirstNonEmpty(
            OpenPlatformJsonReader.GetPropertyString(element, "code"),
            OpenPlatformJsonReader.GetPropertyString(element, "sub_code"),
            OpenPlatformJsonReader.GetPropertyString(element, "resp_code"));
        var message = OpenPlatformText.FirstNonEmpty(
            OpenPlatformJsonReader.GetPropertyString(element, "msg"),
            OpenPlatformJsonReader.GetPropertyString(element, "sub_msg"),
            OpenPlatformJsonReader.GetPropertyString(element, "message"),
            OpenPlatformJsonReader.GetPropertyString(element, "resp_msg"));

        if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(message))
        {
            return $"{prefix} returned an error.";
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return $"{prefix}: {message}";
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return $"{prefix}: {code}";
        }

        return $"{prefix}: {code} - {message}";
    }
}
