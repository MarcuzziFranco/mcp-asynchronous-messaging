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

    private string GenerateClarificationFallback(string failedAction, string errorDetail)
    {
        return $$"""
        {
            "errorContext": {
                "failedAction": "{{failedAction}}",
                "reason": "{{errorDetail}}"
            },
            "nextAction": "requestClarification"
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

    private string MockConnectService(JsonDocument doc)
    {
        Console.WriteLine("Action MockConnectService");
        var name = doc.RootElement.GetProperty("parameters").GetProperty("service").GetString();

        return $$"""
        {
          "status": "connected",
          "service": "{{name}}"
        }
        """;
    }

    private string MockRequestClarification()
    {
        return """
        {
        "status": "clarification-needed",
        "message": "The request could not be resolved to a specific action. Below are the supported MCP actions and their descriptions.",
        "supportedActions": [
            {
                "action": "sendMessageToQueue",
                "description": "Send a message to a queue"
            },
            {
                "action": "sendMessageToTopic",
                "description": "Send a message to a topic"
            },
            {
                "action": "readQueue",
                "description": "Read a message from a queue"
            },
            {
                "action": "listTopics",
                "description": "List all topics"
            },
            {
                "action": "connectService",
                "description": "Connect to a service"
            }
        ]
        }
        """;
    }
}
