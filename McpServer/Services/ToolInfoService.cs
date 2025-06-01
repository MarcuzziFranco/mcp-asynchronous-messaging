using ServerMCP.Models;

namespace ServerMCP.Services;

public class ToolInfoService : IToolInfoService
{
    private readonly List<string> _availableTools = new()
    {
        "sendMessageToQueue",
        "sendMessageToTopic", 
        "readQueue",
        "listTopics",
        "listTools",
        "describeTool"
    };

    private readonly Dictionary<string, object> _toolDefinitions;

    public ToolInfoService()
    {
        _toolDefinitions = InitializeToolDefinitions();
    }

    public List<string> GetAvailableTools()
    {
        return new List<string>(_availableTools);
    }

    public object? GetToolDefinition(string toolName)
    {
        return _toolDefinitions.TryGetValue(toolName, out var definition) ? definition : null;
    }

    public bool IsToolSupported(string toolName)
    {
        return _availableTools.Contains(toolName);
    }

    private Dictionary<string, object> InitializeToolDefinitions()
    {
        return new Dictionary<string, object>
        {
            ["sendMessageToQueue"] = new
            {
                tool = "sendMessageToQueue",
                description = "Sends a string message to a queue.",
                parameters = new Dictionary<string, object>
                {
                    ["queue"] = new { type = "string", required = true, description = "The name of the queue to send the message to" },
                    ["message"] = new { type = "string", required = true, description = "The message content to send" }
                },
                responseExample = new
                {
                    status = "success",
                    data = new { 
                        sentTo = "test.colas", 
                        message = "Hello World" 
                    }
                }
            },
            ["sendMessageToTopic"] = new
            {
                tool = "sendMessageToTopic",
                description = "Sends a string message to a topic.",
                parameters = new Dictionary<string, object>
                {
                    ["topic"] = new { type = "string", required = true, description = "The name of the topic to send the message to" },
                    ["message"] = new { type = "string", required = true, description = "The message content to send" }
                },
                responseExample = new
                {
                    status = "success",
                    data = new { 
                        sentTo = new { topic = "crm-notificaciones", message = "Hello Topic" }
                    }
                }
            },
            ["readQueue"] = new
            {
                tool = "readQueue",
                description = "Reads messages from a queue.",
                parameters = new Dictionary<string, object>
                {
                    ["queue"] = new { type = "string", required = true, description = "The name of the queue to read messages from" }
                },
                responseExample = new
                {
                    status = "success",
                    data = new { 
                        queue = "test.colas",
                        messages = new[] { 
                            new { id = "msg-1", body = "Message 1" },
                            new { id = "msg-2", body = "Message 2" }
                        }
                    }
                }
            },
            ["listTopics"] = new
            {
                tool = "listTopics",
                description = "Lists all available topics.",
                parameters = new Dictionary<string, object>(),
                responseExample = new
                {
                    status = "success",
                    data = new { 
                        topics = new[] { "crm-notificaciones", "altas-pendientes", "errores-sistema" }
                    }
                }
            },
            ["listTools"] = new
            {
                tool = "listTools",
                description = "Lists all available tools in the MCP server.",
                parameters = new Dictionary<string, object>(),
                responseExample = new
                {
                    status = "success",
                    data = new { 
                        tools = new[] { "sendMessageToQueue", "sendMessageToTopic", "readQueue", "listTopics" }
                    }
                }
            },
            ["describeTool"] = new
            {
                tool = "describeTool",
                description = "Describes a specific tool with its parameters and response format.",
                parameters = new Dictionary<string, object>
                {
                    ["toolName"] = new { type = "string", required = true, description = "The name of the tool to describe" }
                },
                responseExample = new
                {
                    status = "success",
                    data = new { 
                        tool = "sendMessageToQueue",
                        description = "Sends a string message to a queue.",
                        parameters = new { },
                        responseExample = new { }
                    }
                }
            }
        };
    }
} 