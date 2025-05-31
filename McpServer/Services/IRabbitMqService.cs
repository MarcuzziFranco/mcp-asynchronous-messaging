using ServerMCP.Models;

namespace ServerMCP.Services;

public interface IRabbitMqService : IDisposable
{
    Task InitializeAsync();
    Task SendMessageAsync(string queue, string message);
    Task SendMessageToTopicAsync(string topic, string message);
    Task<List<QueueMessage>> ReadQueueAsync(string queue, int maxMessages = 10);
    Task<List<string>> ListTopicsAsync();
    Task<string> ConnectServiceAsync(string serviceName);
    Task<bool> IsConnectedAsync();
} 