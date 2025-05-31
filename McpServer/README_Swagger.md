# Documentación de Swagger - MCP Server API

## ¿Qué es Swagger?

Swagger es una herramienta de documentación interactiva para APIs REST que permite:
- Ver toda la documentación de la API en una interfaz web
- Probar los endpoints directamente desde el navegador
- Ver los modelos de datos y sus propiedades
- Generar código cliente automáticamente

## Cómo acceder a Swagger

Una vez que el servidor esté ejecutándose, puedes acceder a Swagger a través de:

```
http://localhost:5000/
```

O si estás usando HTTPS:

```
https://localhost:5001/
```

## Características implementadas

### 🎯 Endpoint documentado: `/mcp`

- **Método**: POST
- **Descripción**: Procesa una solicitud MCP para enviar un mensaje a una cola específica de RabbitMQ
- **Tag**: Messaging
- **Modelos tipados**: Incluye modelos para solicitudes y respuestas

### 📋 Modelos de datos

1. **McpRequest**
   - `Tool` (string, requerido): Herramienta a ejecutar
   - `Parameters` (object, requerido): Parámetros para la herramienta

2. **ApiResponse<T>**
   - `Status` (string): Estado de la respuesta
   - `Data` (T): Datos de la respuesta

3. **MessageResponse**
   - `Message` (string): Mensaje de respuesta

## Ejemplo de uso desde Swagger

1. Abre la interfaz de Swagger en tu navegador
2. Haz clic en el endpoint `/mcp`
3. Haz clic en "Try it out"
4. Introduce el siguiente JSON de ejemplo:

```json
{
  "tool": "messaging.sendToRabbitMQ",
  "parameters": {
    "queue": "test-queue",
    "message": "Hola desde Swagger!"
  }
}
```

5. Haz clic en "Execute"
6. Ve la respuesta del servidor

## Configuración

La configuración de Swagger está en `Program.cs`:
- Solo está habilitado en modo Development
- La interfaz está disponible en la raíz del sitio (`/`)
- Incluye metadatos completos del API

## Beneficios

✅ **Documentación automática**: Se genera automáticamente desde el código  
✅ **Pruebas interactivas**: Puedes probar la API sin herramientas externas  
✅ **Validación de esquemas**: Verifica que los datos cumplan el formato esperado  
✅ **Fácil integración**: Los desarrolladores pueden entender rápidamente cómo usar la API  