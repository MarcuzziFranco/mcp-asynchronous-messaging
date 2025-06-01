namespace McpClient.Services;

public class McpServerConfiguration
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "/mcp";
    public bool IsActive { get; set; } = true;
    public Dictionary<string, string> Headers { get; set; } = new();
    public List<string> SupportedTools { get; set; } = new();
    public DateTime LastDiscovery { get; set; } = DateTime.MinValue;
}

public interface IMcpServerManager
{
    Task<McpServerConfiguration> GetActiveServerAsync();
    Task<List<McpServerConfiguration>> GetAllServersAsync();
    Task<bool> AddServerAsync(McpServerConfiguration server);
    Task<bool> SetActiveServerAsync(string serverId);
    Task RefreshServerToolsAsync(string serverId);
}

public class McpServerManager : IMcpServerManager
{
    private readonly List<McpServerConfiguration> _servers = new();
    private string _activeServerId = string.Empty;

    public McpServerManager()
    {
        // Servidor por defecto
        var defaultServer = new McpServerConfiguration
        {
            Id = "default",
            Name = "Local MCP Server",
            BaseUrl = "http://localhost:5221",
            Endpoint = "/mcp",
            IsActive = true
        };
        
        _servers.Add(defaultServer);
        _activeServerId = defaultServer.Id;
    }

    public async Task<McpServerConfiguration> GetActiveServerAsync()
    {
        await Task.Delay(1); // Para hacer el método asíncrono
        
        var activeServer = _servers.FirstOrDefault(s => s.Id == _activeServerId);
        if (activeServer == null)
        {
            // Si no hay servidor activo, usar el primero disponible
            activeServer = _servers.FirstOrDefault(s => s.IsActive);
            if (activeServer != null)
            {
                _activeServerId = activeServer.Id;
            }
        }
        
        return activeServer ?? throw new InvalidOperationException("No hay servidores MCP disponibles");
    }

    public async Task<List<McpServerConfiguration>> GetAllServersAsync()
    {
        await Task.Delay(1); // Para hacer el método asíncrono
        return new List<McpServerConfiguration>(_servers);
    }

    public async Task<bool> AddServerAsync(McpServerConfiguration server)
    {
        await Task.Delay(1); // Para hacer el método asíncrono
        
        if (_servers.Any(s => s.Id == server.Id))
        {
            return false; // Ya existe un servidor con ese ID
        }
        
        _servers.Add(server);
        
        // Si es el primer servidor, hacerlo activo
        if (string.IsNullOrEmpty(_activeServerId))
        {
            _activeServerId = server.Id;
        }
        
        return true;
    }

    public async Task<bool> SetActiveServerAsync(string serverId)
    {
        await Task.Delay(1); // Para hacer el método asíncrono
        
        var server = _servers.FirstOrDefault(s => s.Id == serverId);
        if (server != null && server.IsActive)
        {
            _activeServerId = serverId;
            return true;
        }
        
        return false;
    }

    public async Task RefreshServerToolsAsync(string serverId)
    {
        await Task.Delay(1); // Para hacer el método asíncrono
        
        var server = _servers.FirstOrDefault(s => s.Id == serverId);
        if (server != null)
        {
            server.LastDiscovery = DateTime.UtcNow;
            // Aquí se podría implementar lógica para actualizar las herramientas
        }
    }
} 