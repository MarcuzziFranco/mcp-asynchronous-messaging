using System.Text.Json;

namespace McpClient.Services;

public interface IFlowOrchestrator
{
    Task<string> ProcessUserInputAsync(string userInput);
    bool IsValidMcpAction(string llmResponse);
    bool ShouldRedirectToLlm(string mcpResponse);
    Task<string> HandleMcpErrorAsync(string mcpErrorResponse);
}

public class FlowDecision
{
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool SendToMcp { get; set; }
    public bool SendToLlm { get; set; }
    public string? AdditionalContext { get; set; }
}

public class McpActionValidation
{
    public bool IsValid { get; set; }
    public string Tool { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? ValidationError { get; set; }
} 