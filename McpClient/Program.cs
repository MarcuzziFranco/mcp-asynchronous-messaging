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

    // Step 1: Send the prompt to the LLM
    var llmResponse = await llmService.SendPromptAsync(input);
    Console.WriteLine("\n🔁 LLM Response:");
    Console.WriteLine(llmResponse);

    // Sanitize the response to remove special characters
    var sanitizedResponse = SanitizeJson.Sanitize(llmResponse);

    // Step 2: Send the response to the MCP
    var mcpResponse = await mcpService.SendCommandAsync(sanitizedResponse);
    Console.WriteLine("\nMCP Service Response:");
    Console.WriteLine(mcpResponse);

    // Step 3: If the LLM requested clarification, resend the MCP response to the LLM
    if (mcpResponse.Contains("\"action\": \"requestClarification\"")) //Actulizar la clave action por nextAction y armar el flujo correspondiente.
    {
        Console.WriteLine("\nReenviando información al LLM para reevaluar...");

        var refinedResponse = await llmService.SendPromptAsync(mcpResponse);
        Console.WriteLine("\nNueva respuesta del LLM:");
        Console.WriteLine(refinedResponse);

        var finalMcpResponse = await mcpService.SendCommandAsync(refinedResponse);
        Console.WriteLine("\nMCP Final Response:");
        Console.WriteLine(finalMcpResponse);
    }
    else {
        Console.WriteLine("Ready inference completed");
    }
}
