using System.Text.Json.Nodes;
using System.ComponentModel.DataAnnotations;

namespace ServerMCP.Models;

public class McpRequest
{
    [Required]
    public string Tool { get; set; } = string.Empty;
    
    [Required]
    public Dictionary<string, JsonNode> Parameters { get; set; } = new();
}

public class ApiResponse<T>
{
    public string Status { get; set; } = string.Empty;
    public T? Data { get; set; }
}

public class MessageResponse
{
    public string Message { get; set; } = string.Empty;
}

public class QueueMessage
{
    public string Id { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Properties { get; set; } = new();
}

public class TopicResponse
{
    public string Status { get; set; } = string.Empty;
    public List<string> Topics { get; set; } = new();
}

public class QueueReadResponse
{
    public string Status { get; set; } = string.Empty;
    public string Queue { get; set; } = string.Empty;
    public List<QueueMessage> Messages { get; set; } = new();
}

public class ConnectionResponse
{
    public string Status { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
}
