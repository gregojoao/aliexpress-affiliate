using System.Text.Json;

namespace AliExpress.Affiliate.Infrastructure.OpenPlatform;

internal static class OpenPlatformJsonReader
{
    public static bool TryGetProperty(
        JsonElement element,
        string propertyName,
        out JsonElement propertyValue)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out propertyValue))
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    propertyValue = property.Value;
                    return true;
                }
            }
        }

        propertyValue = default;
        return false;
    }

    public static string GetPropertyString(
        JsonElement element,
        string propertyName)
    {
        return TryGetProperty(element, propertyName, out var propertyValue)
            ? GetScalarString(propertyValue)
            : string.Empty;
    }

    public static int GetPropertyInt(
        JsonElement element,
        string propertyName)
    {
        var value = GetPropertyString(element, propertyName);

        return int.TryParse(value, out var parsed)
            ? parsed
            : 0;
    }

    public static bool GetPropertyBool(
        JsonElement element,
        string propertyName)
    {
        if (!TryGetProperty(element, propertyName, out var propertyValue))
        {
            return false;
        }

        return propertyValue.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => propertyValue.GetString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true,
            _ => false
        };
    }

    public static string GetScalarString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => string.Empty
        };
    }

    public static IEnumerable<JsonElement> EnumerateItems(
        JsonElement element,
        string itemPropertyName)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    yield return item;
                }
            }

            yield break;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        if (TryGetProperty(element, itemPropertyName, out var items))
        {
            foreach (var item in EnumerateItems(items, itemPropertyName))
            {
                yield return item;
            }

            yield break;
        }

        yield return element;
    }
}
