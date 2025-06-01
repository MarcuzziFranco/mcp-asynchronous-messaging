using System.Text.Json;
using System.Text;
using System.Net.Http;

namespace McpClient.Services;

public class McpService
{
    private readonly HttpClient _httpClient;
    private readonly IMcpServerManager _serverManager;

    public McpService(IMcpServerManager serverManager)
    {
        _httpClient = new HttpClient();
        _serverManager = serverManager;
    }

    public McpService() : this(new McpServerManager())
    {
    }

    public async Task<string> SendCommandAsync(string json)
    {
        try
        {
            var activeServer = await _serverManager.GetActiveServerAsync();
            var fullUrl = $"{activeServer.BaseUrl}{activeServer.Endpoint}";
            
            Console.WriteLine("-------------------------------- SendCommandAsync --------------------------------");
            Console.WriteLine($"Sending JSON to MCP Server ({activeServer.Name}): {json}");
            Console.WriteLine($"URL: {fullUrl}");
            Console.WriteLine("----------------------------------------------------------------------------------");

            // Crear el contenido HTTP con el JSON
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            // Agregar headers adicionales si los hay
            foreach (var header in activeServer.Headers)
            {
                content.Headers.Add(header.Key, header.Value);
            }
            
            // Enviar la petición POST al servidor MCP
            var response = await _httpClient.PostAsync(fullUrl, content);
            
            // Leer la respuesta
            var responseBody = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine("-------------------------------- MCP Server Response --------------------------------");
            Console.WriteLine($"Server: {activeServer.Name}");
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Response: {responseBody}");
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

    private string GenerateClarificationFallback(string failedAction, string errorDetail)
    {
        return $$"""
        {
            "errorContext": {
                "failedAction": "{{failedAction}}",
                "reason": "{{errorDetail}}"
            }
        }
        """;
    }

    private string MockReadQueue(JsonDocument doc)
    {
        Console.WriteLine("Action MockReadQueue");
        var queue = doc.RootElement.GetProperty("parameters").GetProperty("queue").GetString();
        return $$"""
        {
          "status": "success",
          "queue": "{{queue}}",
          "messages": [
            { "id": "msg-1", "body": "Mensaje 1 simulado" },
            { "id": "msg-2", "body": "Mensaje 2 simulado" }
          ]
        }
        """;
    }

    private string MockSendMessageToQueue(JsonDocument doc)
    {
        Console.WriteLine("Action MockSendMessageToQueue");
        var queue = doc.RootElement.GetProperty("parameters").GetProperty("queue").GetString();
        var message = doc.RootElement.GetProperty("parameters").GetProperty("message").GetString();

        return $$"""
        {
          "status": "success",
          "sentTo": "{{queue}}",
          "message": "{{message}}"
        }
        """;
    }

    private string MockSendToTopic(JsonDocument doc)
    {
        Console.WriteLine("Action MockSendToTopic");
        var topic = doc.RootElement.GetProperty("parameters").GetProperty("topic").GetString();
        var message = doc.RootElement.GetProperty("parameters").GetProperty("message").GetString();

        return $$"""
        {
            "status": "success",
            "sentTo": {
                "topic": "{{topic}}",
                "message": "{{message}}"
            },
        }
        """;
    }

    private string MockListTopics()
    {
        Console.WriteLine("Action MockListTopics");
        return """
        {
          "status": "success",
          "topics": [
            "crm-notificaciones",
            "altas-pendientes",
            "errores-sistema"
          ]
        }
        """;
    }
}
