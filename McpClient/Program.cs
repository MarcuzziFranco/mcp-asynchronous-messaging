using McpClient.Services;
using McpClient.Helper;

Console.WriteLine("�� MCP Client Console - Agnóstico al Servidor");

// Configurar servicios
var llmService = new LlmService("http://localhost:11434", "phi4-mcp:latest");
var serverManager = new McpServerManager();
var mcpService = new McpService(serverManager);
var flowOrchestrator = new FlowOrchestrator(llmService, mcpService);

// Mostrar información del servidor activo
var activeServer = await serverManager.GetActiveServerAsync();
Console.WriteLine($"📡 Servidor MCP activo: {activeServer.Name} ({activeServer.BaseUrl})");

while (true)
{
    Console.Write("\n> ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input == "exit") break;

    try
    {
        var finalMessage = await flowOrchestrator.ProcessUserInputAsync(input);
        Console.WriteLine("\n🎯 Respuesta final para el usuario:");
        finalMessage = SanitizeJson.Sanitize(finalMessage);
        Console.WriteLine(finalMessage);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n❌ Error procesando la solicitud: {ex.Message}");
    }
}
