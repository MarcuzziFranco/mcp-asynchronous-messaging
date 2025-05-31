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
    if (request.Tool == "messaging.sendToRabbitMQ")
    {
        try
        {
            await rabbitService.SendMessageAsync(
                request.Parameters["queue"]!.ToString()!,
                request.Parameters["message"]!.ToString()!
            );

            return Results.Ok(new ApiResponse<MessageResponse>
            {
                Status = "success",
                Data = new MessageResponse { Message = "Sent to RabbitMQ" }
            });
        }
        catch (Exception ex)
        {
            return Results.Ok(new ApiResponse<MessageResponse>
            {
                Status = "error",
                Data = new MessageResponse { Message = ex.Message }
            });
        }
    }

    return Results.Ok(new ApiResponse<MessageResponse>
    {
        Status = "error",
        Data = new MessageResponse { Message = "Tool not supported" }
    });
})
.WithName("SendMessageToRabbitMQ")
.WithTags("Messaging")
.WithSummary("Envía un mensaje a RabbitMQ")
.WithDescription("Procesa una solicitud MCP para enviar un mensaje a una cola específica de RabbitMQ")
.WithOpenApi();

app.Run();
