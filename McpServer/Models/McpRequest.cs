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
