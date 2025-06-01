using ServerMCP.Models;

namespace ServerMCP.Services;

public interface IToolInfoService
{
    List<string> GetAvailableTools();
    object? GetToolDefinition(string toolName);
    bool IsToolSupported(string toolName);
} 