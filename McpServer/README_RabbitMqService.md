# RabbitMqService - Documentación

## Resumen de cambios realizados

La implementación original del `RabbitMqService` tenía varios problemas críticos que han sido corregidos:

### Problemas identificados y solucionados:

1. **Error de asignación asíncrona**: 
   - ❌ **Antes**: `_connection = factory.CreateConnectionAsync(endpoints);`
   - ✅ **Ahora**: `_connection = await factory.CreateConnectionAsync(endpoints);`

2. **Manejo de recursos**:
   - ❌ **Antes**: No implementaba `IDisposable`
   - ✅ **Ahora**: Implementa `IDisposable` correctamente

3. **Métodos asíncronos**:
   - ❌ **Antes**: Mezclaba métodos síncronos y asíncronos incorrectamente
   - ✅ **Ahora**: Usa completamente métodos asíncronos de RabbitMQ.Client v7+

4. **Manejo de errores**:
   - ❌ **Antes**: Sin manejo de excepciones
   - ✅ **Ahora**: Manejo completo de errores con mensajes descriptivos

5. **Inyección de dependencias**:
   - ❌ **Antes**: Registrado como clase concreta
   - ✅ **Ahora**: Interfaz `IRabbitMqService` para mejor testabilidad

## Uso correcto del servicio

### 1. Inicialización en Program.cs
```csharp
// Registrar el servicio
builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();

var app = builder.Build();

// Inicializar la conexión al arrancar
using (var scope = app.Services.CreateScope())
{
    var rabbitService = scope.ServiceProvider.GetRequiredService<IRabbitMqService>();
    await rabbitService.InitializeAsync();
}
```

### 2. Uso en controladores/endpoints
```csharp
app.MapPost("/mcp", async (McpRequest request, IRabbitMqService rabbitService) =>
{
    try
    {
        await rabbitService.SendMessageAsync("mi-cola", "mi-mensaje");
        return Results.Ok(new { status = "success" });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});
```

### 3. Configuración de RabbitMQ

El servicio se conecta por defecto a:
- **Primera opción**: `localhost` (para desarrollo local)
- **Fallback**: `hostname` (configurable según tu entorno)

Para cambiar la configuración, modifica los endpoints en el método `InitializeAsync()`.

## Requisitos

- **RabbitMQ.Client**: v7.1.2 (ya incluido en el proyecto)
- **RabbitMQ Server**: Debe estar ejecutándose en localhost o en el hostname configurado
- **.NET 9.0**: Según la configuración del proyecto

## Comando para ejecutar RabbitMQ localmente

```bash
# Con Docker
docker run -d --hostname my-rabbit --name some-rabbit -p 5672:5672 -p 15672:15672 rabbitmq:3-management

# Interfaz de administración disponible en: http://localhost:15672
# Usuario: guest, Contraseña: guest
```

## Notas importantes

1. **Inicialización obligatoria**: Debes llamar a `InitializeAsync()` antes de usar `SendMessageAsync()`
2. **Disposición automática**: El servicio se limpia automáticamente al finalizar la aplicación
3. **Thread-safe**: El servicio es seguro para uso concurrente
4. **Manejo de errores**: Todas las excepciones incluyen mensajes descriptivos en español
5. **Compatibilidad RabbitMQ.Client v7+**: Se eliminó el método `Close()` deprecated, solo se usa `Dispose()` 