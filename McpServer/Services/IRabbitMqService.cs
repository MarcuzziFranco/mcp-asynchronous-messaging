namespace ServerMCP.Services;

public interface IRabbitMqService : IDisposable
{
    Task InitializeAsync();
    Task SendMessageAsync(string queue, string message);
} 