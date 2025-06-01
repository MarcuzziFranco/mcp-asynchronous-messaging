using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServerMCP.Models;
using ServerMCP.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
});

// Registrar el servicio con la interfaz
builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();

// Registrar el servicio de información de herramientas
builder.Services.AddSingleton<IToolInfoService, ToolInfoService>();

// Agregar servicios de Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MCP Server API",
        Version = "v1",
        Description = "API para envío de mensajes a RabbitMQ a través del protocolo MCP"
    });
});

var app = builder.Build();

// Configurar Swagger en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MCP Server API v1");
        c.RoutePrefix = string.Empty; // Para que Swagger esté en la raíz "/"
    });
}

// Inicializar RabbitMQ al arrancar la aplicación
using (var scope = app.Services.CreateScope())
{
    var rabbitService = scope.ServiceProvider.GetRequiredService<IRabbitMqService>();
    await rabbitService.InitializeAsync();
}

app.MapPost("/mcp", async (McpRequest request, IRabbitMqService rabbitService, IToolInfoService toolInfoService) =>
{
    try
    {
        return request.Tool switch
        {
            "sendMessageToQueue" => await HandleSendMessageToQueue(request, rabbitService),
            "sendMessageToTopic" => await HandleSendMessageToTopic(request, rabbitService),
            "readQueue" => await HandleReadQueue(request, rabbitService),
            "listTopics" => await HandleListTopics(request, rabbitService),
            "listTools" => HandleListTools(toolInfoService),
            "describeTool" => HandleDescribeTool(request, toolInfoService),
            _ => HandleUnsupportedTool(request.Tool)
        };
    }
    catch (Exception ex)
    {
        return Results.Ok(new ApiResponse<object>
        {
            Status = "error",
            Data = new { 
                errorContext = new { 
                    failedAction = request.Tool, 
                    reason = ex.Message 
                }
            }
        });
    }
})
.WithName("SendMessageToRabbitMQ")
.WithTags("Messaging")
.WithSummary("Procesa solicitudes MCP para RabbitMQ")
.WithDescription("Endpoint único que maneja todas las operaciones MCP para RabbitMQ: envío a colas, envío a topics, lectura de colas, listado de topics y conexión de servicios")
.WithOpenApi();

// Funciones auxiliares para manejar cada acción
async Task<IResult> HandleSendMessageToQueue(McpRequest request, IRabbitMqService rabbitService)
{
    Console.WriteLine("HandleSendMessageToQueue");
    await rabbitService.SendMessageAsync(
        request.Parameters["queue"]!.ToString()!,
        request.Parameters["message"]!.ToString()!
    );

    return Results.Ok(new ApiResponse<object>
    {
        Status = "success",
        Data = new { 
            sentTo = request.Parameters["queue"]!.ToString()!,
            message = request.Parameters["message"]!.ToString()!
        }
    });
}

async Task<IResult> HandleSendMessageToTopic(McpRequest request, IRabbitMqService rabbitService)
{
    Console.WriteLine("HandleSendMessageToTopic");
    await rabbitService.SendMessageToTopicAsync(
        request.Parameters["topic"]!.ToString()!,
        request.Parameters["message"]!.ToString()!
    );

    return Results.Ok(new ApiResponse<object>
    {
        Status = "success",
        Data = new { 
            sentTo = new {
                topic = request.Parameters["topic"]!.ToString()!,
                message = request.Parameters["message"]!.ToString()!
            }
        }
    });
}

async Task<IResult> HandleReadQueue(McpRequest request, IRabbitMqService rabbitService)
{
    Console.WriteLine("HandleReadQueue");
    var queue = request.Parameters["queue"]!.ToString()!;
    var messages = await rabbitService.ReadQueueAsync(queue);

    return Results.Ok(new ApiResponse<object>
    {
        Status = "success",
        Data = new { 
            queue = queue,
            messages = messages.Select(m => new { id = m.Id, body = m.Body }).ToList()
        }
    });
}

async Task<IResult> HandleListTopics(McpRequest request, IRabbitMqService rabbitService)
{
    Console.WriteLine("HandleListTopics");
    var topics = await rabbitService.ListTopicsAsync();

    return Results.Ok(new ApiResponse<object>
    {
        Status = "success",
        Data = new { topics = topics }
    });
}

IResult HandleListTools(IToolInfoService toolInfoService)
{
    var availableTools = toolInfoService.GetAvailableTools();

    return Results.Ok(new ApiResponse<object>
    {
        Status = "success",
        Data = new { tools = availableTools }
    });
}

IResult HandleDescribeTool(McpRequest request, IToolInfoService toolInfoService)
{
    if (!request.Parameters.ContainsKey("toolName"))
    {
        return Results.Ok(new ApiResponse<object>
        {
            Status = "error",
            Data = new { 
                errorContext = new { 
                    failedAction = "describeTool", 
                    reason = "Missing required parameter 'toolName'" 
                }
            }
        });
    }

    var toolName = request.Parameters["toolName"]!.ToString()!;
    var toolDefinition = toolInfoService.GetToolDefinition(toolName);

    if (toolDefinition == null)
    {
        return Results.Ok(new ApiResponse<object>
        {
            Status = "error",
            Data = new { 
                errorContext = new { 
                    failedAction = "describeTool", 
                    reason = $"Unknown tool: {toolName}" 
                }
            }
        });
    }

    return Results.Ok(new ApiResponse<object>
    {
        Status = "success",
        Data = toolDefinition
    });
}

IResult HandleUnsupportedTool(string tool)
{
    return Results.Ok(new ApiResponse<object>
    {
        Status = "error",
        Data = new { 
            errorContext = new { 
                failedAction = tool, 
                reason = "Invalid action" 
            },
            message = "Tool not supported. Use 'listTools' to see available tools or 'describeTool' with parameter 'toolName' to get details about a specific tool.",
            suggestion = new {
                action = "listTools",
                description = "Call this to see all available tools"
            }
        }
    });
}

app.Run();
