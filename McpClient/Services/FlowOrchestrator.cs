using System.Text.Json;
using McpClient.Helper;

namespace McpClient.Services;

public class FlowOrchestrator : IFlowOrchestrator
{
    private readonly LlmService _llmService;
    private readonly McpService _mcpService;

    public FlowOrchestrator(LlmService llmService, McpService mcpService)
    {
        _llmService = llmService;
        _mcpService = mcpService;
    }

    public async Task<string> ProcessUserInputAsync(string userInput)
    {
        var conversationHistory = new List<string>();
        var maxIterations = 10; // Prevenir bucles infinitos
        var iteration = 0;

        //Console.WriteLine($"\n🔄 Iniciando procesamiento de: '{userInput}'");

        // Paso 1: Enviar input del usuario al LLM
        var llmResponse = await _llmService.SendPromptAsync(userInput);
        Console.WriteLine($"\n🧠 LLM Respuesta (Iteración {++iteration}):");
        Console.WriteLine(llmResponse);

        var sanitizedLlmResponse = SanitizeJson.Sanitize(llmResponse);
        conversationHistory.Add($"LLM: {sanitizedLlmResponse}");

        while (iteration < maxIterations)
        {
            // Verificar si la respuesta del LLM es una acción MCP válida
            var validation = ValidateMcpAction(sanitizedLlmResponse);
            
            if (validation.IsValid)
            {
                Console.WriteLine($"\n✅ Acción MCP válida detectada: {validation.Tool}");
                
                // Enviar al servidor MCP
                var mcpResponse = await _mcpService.SendCommandAsync(sanitizedLlmResponse);
                Console.WriteLine($"\n🔧 MCP Server Respuesta:");
                Console.WriteLine(mcpResponse);
                
                conversationHistory.Add($"MCP: {mcpResponse}");

                // Verificar si el MCP devolvió un error y necesitamos redirigir al LLM
                if (ShouldRedirectToLlm(mcpResponse))
                {
                    Console.WriteLine($"\n🔄 Redirigiendo error al LLM para corrección...");
                    var contextualPrompt = BuildContextualPrompt(mcpResponse, conversationHistory);
                    llmResponse = await _llmService.SendPromptAsync(contextualPrompt);
                    sanitizedLlmResponse = SanitizeJson.Sanitize(llmResponse);
                    
                    Console.WriteLine($"\n🧠 LLM Corrección (Iteración {++iteration}):");
                    Console.WriteLine(llmResponse);
                    
                    conversationHistory.Add($"LLM_CORRECTION: {sanitizedLlmResponse}");
                    continue;
                }
                else
                {
                    // Éxito: Enviar resultado al LLM para formatear respuesta final
                    Console.WriteLine($"\n✅ Éxito! Formateando respuesta final...");
                    var finalPrompt = $"Basado en este resultado exitoso del sistema: {mcpResponse}\n\nGenera una respuesta amigable para el usuario explicando qué se hizo y el resultado obtenido.";
                    var finalResponse = await _llmService.SendPromptAsync(finalPrompt);
                    
                    Console.WriteLine($"\n🎯 Respuesta final generada");
                    return finalResponse;
                }
            }
            else
            {
                // No es una acción MCP válida, asumir que es respuesta final del LLM
                Console.WriteLine($"\n💬 Respuesta final del LLM detectada");
                return llmResponse;
            }
        }

        Console.WriteLine($"\n⚠️ Máximo de iteraciones alcanzado. Devolviendo última respuesta.");
        return llmResponse;
    }

    public bool IsValidMcpAction(string llmResponse)
    {
        return ValidateMcpAction(llmResponse).IsValid;
    }

    public bool ShouldRedirectToLlm(string mcpResponse)
    {
        try
        {
            using var doc = JsonDocument.Parse(mcpResponse);
            var root = doc.RootElement;

            // Si el status es error, redirigir al LLM
            if (root.TryGetProperty("Status", out var status) && 
                status.GetString() == "error")
            {
                return true;
            }

            // Si hay sugerencias en la respuesta, redirigir al LLM
            if (root.TryGetProperty("Data", out var data) && 
                data.TryGetProperty("suggestion", out var suggestion))
            {
                return true;
            }

            return false;
        }
        catch
        {
            // Si no se puede parsear como JSON, asumir que no es error MCP
            return false;
        }
    }

    public async Task<string> HandleMcpErrorAsync(string mcpErrorResponse)
    {
        var contextualPrompt = $"""
        El servidor MCP devolvió el siguiente error:
        {mcpErrorResponse}

        Por favor, analiza el error y genera una nueva acción MCP válida o una respuesta explicativa para el usuario.
        Si hay una sugerencia en el error, síguelo.
        """;

        return await _llmService.SendPromptAsync(contextualPrompt);
    }

    private McpActionValidation ValidateMcpAction(string response)
    {
        try
        {
            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            if (!root.TryGetProperty("tool", out var toolElement))
            {
                return new McpActionValidation 
                { 
                    IsValid = false, 
                    ValidationError = "No 'tool' property found" 
                };
            }

            var tool = toolElement.GetString();
            if (string.IsNullOrEmpty(tool))
            {
                return new McpActionValidation 
                { 
                    IsValid = false, 
                    Tool = tool ?? "", 
                    ValidationError = "Tool name is empty" 
                };
            }

            var parameters = new Dictionary<string, object>();
            if (root.TryGetProperty("parameters", out var paramsElement))
            {
                foreach (var param in paramsElement.EnumerateObject())
                {
                    parameters[param.Name] = param.Value.ToString();
                }
            }

            return new McpActionValidation
            {
                IsValid = true,
                Tool = tool,
                Parameters = parameters
            };
        }
        catch (JsonException ex)
        {
            return new McpActionValidation 
            { 
                IsValid = false, 
                ValidationError = $"Invalid JSON: {ex.Message}" 
            };
        }
    }

    private string BuildContextualPrompt(string mcpResponse, List<string> conversationHistory)
    {
        var context = string.Join("\n", conversationHistory.TakeLast(3));
        
        return $"""
        Contexto de la conversación:
        {context}

        El servidor MCP respondió con:
        {mcpResponse}

        Basado en este contexto y la respuesta del servidor, por favor:
        1. Si hay un error, analízalo y corrige la acción MCP
        2. Si hay una sugerencia, síguelo
        3. Si es exitoso pero necesitas más información, genera la próxima acción apropiada
        4. Si es un resultado final exitoso, genera una respuesta amigable para el usuario

        Nota: Si necesitas conocer las herramientas disponibles, puedes usar 'listTools' o 'describeTool'.
        """;
    }
} 