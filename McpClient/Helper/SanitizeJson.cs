using System.Text.Json;

namespace McpClient.Helper;

public static class SanitizeJson
{
    public static string Sanitize(string json)
    {
        string sanitizedJson = json.Replace("```json", "");
        sanitizedJson = sanitizedJson.Replace("```", "");
        var doc = JsonDocument.Parse(sanitizedJson);
        return doc.RootElement.ToString();
    }
}

