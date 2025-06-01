using System.Text.Json;
using System.Text;
using System.Net.Http;

namespace McpClient.Services;

public class McpService
{
    private readonly HttpClient _httpClient;
    private readonly string _mcpServerUrl;

    public McpService()
    {
        _httpClient = new HttpClient();
        _mcpServerUrl = "http://localhost:5221/mcp";
    }

    public async Task<string> SendCommandAsync(string json)
    {
        try
        {
            Console.WriteLine("-------------------------------- SendCommandAsync --------------------------------");
            Console.WriteLine("Sending JSON to MCP Server: " + json);
            Console.WriteLine("----------------------------------------------------------------------------------");

            // Crear el contenido HTTP con el JSON
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            // Enviar la petición POST al servidor MCP
            var response = await _httpClient.PostAsync(_mcpServerUrl, content);
            
            // Leer la respuesta
            var responseBody = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine("-------------------------------- MCP Server Response --------------------------------");
            Console.WriteLine("Status Code: " + response.StatusCode);
            Console.WriteLine("Response: " + responseBody);
            Console.WriteLine("---------------------------------------------------------------------------");
            
            return responseBody;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error sending command to MCP Server");
            Console.WriteLine($"Error: {ex.Message}");
            return GenerateClarificationFallback(json, ex.Message);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

}
