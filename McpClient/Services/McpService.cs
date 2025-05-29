using System.Text.Json;

namespace McpClient.Services;

public class McpService
{
    public async Task<string> SendCommandAsync(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var action = doc.RootElement.GetProperty("action").GetString();
            Console.WriteLine("-------------------------------- SendCommandAsync --------------------------------");
            Console.WriteLine("Action: " + action);
            Console.WriteLine("----------------------------------------------------------------------------------");
            return action switch
            {
                "sendMessageToQueue" => MockSendMessageToQueue(doc),
                "sendMessageToTopic" => MockSendToTopic(doc),
                "readQueue" => MockReadQueue(doc),
                "listTopics" => MockListTopics(),
                "connectService" => MockConnectService(doc),
                "requestClarification" => MockRequestClarification(),
                _ => GenerateClarificationFallback(action, "Invalid action")
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error sending command to MCP");
            //Console.WriteLine($"Error: {ex.Message}");
            return GenerateClarificationFallback(json, ex.Message);
        }
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
