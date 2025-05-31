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

app.MapPost("/mcp", async (McpRequest request, IRabbitMqService rabbitService) =>
{
    try
    {
        return request.Tool switch
        {
            "sendMessageToQueue" => await HandleSendMessageToQueue(request, rabbitService),
            "sendMessageToTopic" => await HandleSendMessageToTopic(request, rabbitService),
            "readQueue" => await HandleReadQueue(request, rabbitService),
            "listTopics" => await HandleListTopics(request, rabbitService),
            "connectService" => await HandleConnectService(request, rabbitService),
            "requestClarification" => HandleRequestClarification(),
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
                },
                nextAction = "requestClarification"
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

async Task<IResult> HandleConnectService(McpRequest request, IRabbitMqService rabbitService)
{
    Console.WriteLine("HandleConnectService");
    var serviceName = request.Parameters["service"]!.ToString()!;
    await rabbitService.ConnectServiceAsync(serviceName);

    return Results.Ok(new ApiResponse<object>
    {
        Status = "connected",
        Data = new { service = serviceName }
    });
}

IResult HandleRequestClarification()
{
    Console.WriteLine("HandleRequestClarification");
    return Results.Ok(new ApiResponse<object>
    {
        Status = "clarification-needed",
        Data = new {
            message = "The request could not be resolved to a specific action. Below are the supported MCP actions and their descriptions.",
            supportedActions = new[]
            {
                new { action = "sendMessageToQueue", description = "Send a message to a queue" },
                new { action = "sendMessageToTopic", description = "Send a message to a topic" },
                new { action = "readQueue", description = "Read a message from a queue" },
                new { action = "listTopics", description = "List all topics" },
                new { action = "connectService", description = "Connect to a service" }
            }
        }
    });
}

IResult HandleUnsupportedTool(string tool)
{
    Console.WriteLine("HandleUnsupportedTool");
    return Results.Ok(new ApiResponse<object>
    {
        Status = "error",
        Data = new { 
            errorContext = new { 
                failedAction = tool, 
                reason = "Invalid action" 
            },
            nextAction = "requestClarification"
        }
    });
}

app.Run();
