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
            Console.WriteLine($"Error: {ex.Message}");
            return GenerateClarificationFallback(json, ex.Message);
        }
    }

    private string GenerateClarificationFallback(string failedAction, string errorDetail)
    {
        return $$"""
        {
            "action": "requestClarification",
            "parameters": {},
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
        var queue = doc.RootElement.GetProperty("parameters").GetProperty("queueName").GetString();
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
        var topic = doc.RootElement.GetProperty("parameters").GetProperty("topic").GetString();
        var subscription = doc.RootElement.GetProperty("parameters").GetProperty("subscription").GetString();
        var message = doc.RootElement.GetProperty("parameters").GetProperty("message").GetString();

        return $$"""
        {
            "status": "success",
            "sentTo": {
                "topic": "{{topic}}",
                "subscription": "{{subscription}}"
            },
            "message": "{{message}}"
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
                "name": "readQueue",
                "description": "Reads the latest messages from a given queue.",
                "parameters": {
                    "queue": "string (required) - The name of the queue to read from."
                }
            },
            {
                "name": "sendMessageToQueue",
                "description": "Sends a text message to a specific queue.",
                "parameters": {
                    "queue": "string (required) - Target queue name.",
                    "message": "string (required) - Content of the message."
                }
            },
            {
                "name": "sendMessageToTopic",
                "description": "Sends a text message to a specific topic.",
                "parameters": {
                    "topic": "string (required) - Target topic name.",
                    "message": "string (required) - Content of the message."
                }
            },
                {
                "name": "listTopics",
                "description": "Lists all available messaging topics in the system.",
                "parameters": {}
                },
            {
                "name": "connectService",
                "description": "Connects the MCP to an external service by name.",
                "parameters": {
                    "service": "string (required) - Name of the external service."
                }
            }
        ],
        "documentation": "http://localhost:5000/docs"
        }
        """;
    }

}
