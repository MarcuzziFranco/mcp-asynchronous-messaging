using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace McpClient.Services;

public class LlmService
{
    private readonly HttpClient _httpClient;
    private readonly string _url;
    private readonly string _model;

    public LlmService(string baseUrl, string model)
    {
        _httpClient = new HttpClient();
        _url = $"{baseUrl}/api/generate";
        _model = model;
    }

    public async Task<string> SendPromptAsync(string prompt)
    {
        var payload = new
        {
            model = _model,
            prompt = prompt,
            stream = false
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(_url, content);
        var result = await response.Content.ReadAsStringAsync();

        using var jsonDoc = JsonDocument.Parse(result);
        return jsonDoc.RootElement.GetProperty("response").GetString() ?? "{}";
    }
}
