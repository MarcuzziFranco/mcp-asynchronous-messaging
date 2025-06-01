using McpClient.Services;
using McpClient.Helper;

Console.WriteLine("🧠 MCP Client Console");
var llmService = new LlmService("http://localhost:11434", "phi4-mcp:latest");
var mcpService = new McpService(); 

while (true)
{
    Console.Write("\n> ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input == "exit") break;

    var finalMessage = await ExecuteFinalWithFormatting(input);
    Console.WriteLine("\nLLM → Mensaje final para usuario:");
    finalMessage = SanitizeJson.Sanitize(finalMessage);
    Console.WriteLine(finalMessage);
    
}

async Task<string> ExecuteFinalWithFormatting(string userInput)
{
    // Paso 1: Enviar el mensaje al LLM para que genere la acción
    var llmAction = await llmService.SendPromptAsync(userInput);
    Console.WriteLine("\nLLM → Acción:");
    Console.WriteLine(llmAction);
    var sanitizedResponse = SanitizeJson.Sanitize(llmAction);

    // Paso 2: Ejecutar la acción en el MCP Server
    var mcpResult = await mcpService.SendCommandAsync(sanitizedResponse);
    Console.WriteLine("\nMCP → Resultado:");
    Console.WriteLine(mcpResult);

    // Paso 3: Reenviar el resultado al LLM para que genere respuesta de usuario
    var finalMessage = await llmService.SendPromptAsync(mcpResult);
    return finalMessage;
}
