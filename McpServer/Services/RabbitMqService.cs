using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using ServerMCP.Models;

namespace ServerMCP.Services;

public class RabbitMqService : IRabbitMqService, IDisposable
{
    private IConnection? _connection;
    private bool _disposed = false;
    private readonly List<string> _connectedServices = new();
    private readonly List<string> _topics = new() 
    { 
        "crm-notificaciones", 
        "altas-pendientes", 
        "errores-sistema" 
    };
    
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
            Console.WriteLine("RabbitMQ connection established successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al conectar con RabbitMQ: {ex.Message}");
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
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var messageBody = new
            {
                id = Guid.NewGuid().ToString(),
                body = message,
                timestamp = DateTime.UtcNow,
                queue = queue
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(messageBody));
            
            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queue,
                body: body);

            Console.WriteLine($"Message sent to queue '{queue}': {message}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al enviar mensaje a la cola '{queue}': {ex.Message}", ex);
        }
    }

    public async Task SendMessageToTopicAsync(string topic, string message)
    {
        if (_connection == null)
            throw new InvalidOperationException("La conexión no ha sido inicializada. Llama a InitializeAsync() primero.");

        try
        {
            using var channel = await _connection.CreateChannelAsync();
            
            // Declarar el exchange para el topic
            await channel.ExchangeDeclareAsync(
                exchange: topic,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                arguments: null);

            var messageBody = new
            {
                id = Guid.NewGuid().ToString(),
                body = message,
                timestamp = DateTime.UtcNow,
                topic = topic
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(messageBody));
            
            await channel.BasicPublishAsync(
                exchange: topic,
                routingKey: "general",
                body: body);

            Console.WriteLine($"Message sent to topic '{topic}': {message}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al enviar mensaje al topic '{topic}': {ex.Message}", ex);
        }
    }

    public async Task<List<QueueMessage>> ReadQueueAsync(string queue, int maxMessages = 10)
    {
        if (_connection == null)
            throw new InvalidOperationException("La conexión no ha sido inicializada. Llama a InitializeAsync() primero.");

        var messages = new List<QueueMessage>();

        try
        {
            using var channel = await _connection.CreateChannelAsync();
            
            await channel.QueueDeclareAsync(
                queue: queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Leer mensajes de la cola
            for (int i = 0; i < maxMessages; i++)
            {
                var result = await channel.BasicGetAsync(queue, autoAck: true);
                if (result == null) break;

                var body = Encoding.UTF8.GetString(result.Body.ToArray());
                
                try
                {
                    var messageData = JsonSerializer.Deserialize<JsonElement>(body);
                    var queueMessage = new QueueMessage
                    {
                        Id = messageData.TryGetProperty("id", out var id) ? id.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString(),
                        Body = messageData.TryGetProperty("body", out var bodyProp) ? bodyProp.GetString() ?? body : body,
                        Timestamp = messageData.TryGetProperty("timestamp", out var timestamp) ? timestamp.GetDateTime() : DateTime.UtcNow
                    };
                    messages.Add(queueMessage);
                }
                catch
                {
                    // Si no es JSON válido, usar el body como mensaje directo
                    messages.Add(new QueueMessage
                    {
                        Id = Guid.NewGuid().ToString(),
                        Body = body,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }

            Console.WriteLine($"Read {messages.Count} messages from queue '{queue}'");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al leer mensajes de la cola '{queue}': {ex.Message}", ex);
        }

        return messages;
    }

    public async Task<List<string>> ListTopicsAsync()
    {
        // En una implementación real, esto podría consultar los exchanges de RabbitMQ
        // Por ahora retornamos una lista predefinida como en el mock
        await Task.Delay(50); // Simular operación asíncrona
        Console.WriteLine("Listing available topics");
        return new List<string>(_topics);
    }

    public async Task<string> ConnectServiceAsync(string serviceName)
    {
        if (_connection == null)
            throw new InvalidOperationException("La conexión no ha sido inicializada. Llama a InitializeAsync() primero.");

        try
        {
            // Simular conexión a un servicio específico
            if (!_connectedServices.Contains(serviceName))
            {
                _connectedServices.Add(serviceName);
            }

            await Task.Delay(100); // Simular tiempo de conexión
            Console.WriteLine($"Connected to service: {serviceName}");
            return serviceName;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al conectar con el servicio '{serviceName}': {ex.Message}", ex);
        }
    }

    public async Task<bool> IsConnectedAsync()
    {
        await Task.Delay(10); // Operación asíncrona mínima
        return _connection?.IsOpen ?? false;
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
