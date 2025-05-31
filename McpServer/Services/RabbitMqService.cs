using RabbitMQ.Client;
using System.Text;

namespace ServerMCP.Services;

public class RabbitMqService : IRabbitMqService, IDisposable
{
    private IConnection? _connection;
    private bool _disposed = false;
    
    public async Task InitializeAsync()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                UserName = "admin",
                Password = "admin",
                VirtualHost = "/",
                Port = 5672

            };

            var endpoints = new List<AmqpTcpEndpoint>
            {
                new AmqpTcpEndpoint("localhost"), // Primero localhost
                new AmqpTcpEndpoint("hostname")   // Fallback
            };
            
            _connection = await factory.CreateConnectionAsync(endpoints);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al conectar con RabbitMQ: {ex.Message}", ex);
        }
    }

    public async Task SendMessageAsync(string queue, string message)
    {
        if (_connection == null)
            throw new InvalidOperationException("La conexión no ha sido inicializada. Llama a InitializeAsync() primero.");

        try
        {
            using var channel = await _connection.CreateChannelAsync();
            
            await channel.QueueDeclareAsync(
                queue: queue,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var body = Encoding.UTF8.GetBytes(message);
            
            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queue,
                body: body);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al enviar mensaje a la cola '{queue}': {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _connection?.Dispose();
            _connection = null;
            _disposed = true;
        }
    }
}
