using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AzureFunctionIgga.Middlewares;

public class ExceptionHandlingMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado en función: {FunctionName}", context.FunctionDefinition.Name);

            // Crear respuesta de error personalizada
            var errorResponse = new
            {
                Error = new
                {
                    Message = "Se produjo un error interno del servidor",
                    TraceId = context.TraceContext.TraceId,
                    Timestamp = DateTime.UtcNow
                }
            };

            // Intentar establecer la respuesta HTTP si es una función HTTP
            if (context.GetHttpRequestData() != null)
            {
                var httpRequestData = context.GetHttpRequestData();
                var response = httpRequestData!.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                response.Headers.Add("Content-Type", "application/json");
                await response.WriteStringAsync(JsonSerializer.Serialize(errorResponse));
                
                context.GetInvocationResult().Value = response;
            }

            // Re-lanzar la excepción para que el runtime de Functions la maneje
            throw;
        }
    }
}

public class AuthenticationMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<AuthenticationMiddleware> _logger;

    public AuthenticationMiddleware(ILogger<AuthenticationMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var functionName = context.FunctionDefinition.Name;
        _logger.LogInformation("Verificando autenticación para función: {FunctionName}", functionName);

        // Obtener request HTTP si existe
        var httpRequestData = context.GetHttpRequestData();
        
        if (httpRequestData != null)
        {
            // Verificar si la función requiere autenticación (skip para health check y funciones anónimas)
            var skipAuth = functionName.Contains("HealthCheck") || 
                          context.FunctionDefinition.InputBindings.Any(b => 
                              b.Value.Type == "httpTrigger" && 
                              b.Value.Properties.TryGetValue("authLevel", out var authLevel) && 
                              authLevel.ToString() == "Anonymous");

            if (!skipAuth)
            {
                var authHeader = httpRequestData.Headers.GetValues("Authorization").FirstOrDefault();
                
                if (string.IsNullOrEmpty(authHeader))
                {
                    _logger.LogWarning("Intento de acceso sin token de autorización a función: {FunctionName}", functionName);
                    
                    var unauthorizedResponse = httpRequestData.CreateResponse(System.Net.HttpStatusCode.Unauthorized);
                    unauthorizedResponse.Headers.Add("Content-Type", "application/json");
                    await unauthorizedResponse.WriteStringAsync(JsonSerializer.Serialize(new
                    {
                        Error = new
                        {
                            Message = "Token de autorización requerido",
                            Code = "UNAUTHORIZED"
                        }
                    }));
                    
                    context.GetInvocationResult().Value = unauthorizedResponse;
                    return;
                }

                // Aquí puedes agregar validación de JWT o API Key
                var isValidToken = await ValidateTokenAsync(authHeader);
                
                if (!isValidToken)
                {
                    _logger.LogWarning("Token de autorización inválido para función: {FunctionName}", functionName);
                    
                    var forbiddenResponse = httpRequestData.CreateResponse(System.Net.HttpStatusCode.Forbidden);
                    forbiddenResponse.Headers.Add("Content-Type", "application/json");
                    await forbiddenResponse.WriteStringAsync(JsonSerializer.Serialize(new
                    {
                        Error = new
                        {
                            Message = "Token de autorización inválido",
                            Code = "FORBIDDEN"
                        }
                    }));
                    
                    context.GetInvocationResult().Value = forbiddenResponse;
                    return;
                }

                // Agregar información del usuario autenticado al contexto
                var userId = ExtractUserIdFromToken(authHeader);
                context.Items["UserId"] = userId;
                context.Items["IsAuthenticated"] = true;
            }
        }

        await next(context);
    }

    private async Task<bool> ValidateTokenAsync(string authHeader)
    {
        try
        {
            // Implementar validación de token (JWT, API Key, etc.)
            // Por ahora, validación básica de API Key
            if (authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                
                // Validar JWT aquí si usas JWT
                return await ValidateJwtTokenAsync(token);
            }
            else if (authHeader.StartsWith("ApiKey "))
            {
                var apiKey = authHeader.Substring("ApiKey ".Length);
                
                // Validar API Key
                var expectedApiKey = Environment.GetEnvironmentVariable("ApiKey");
                return !string.IsNullOrEmpty(expectedApiKey) && apiKey == expectedApiKey;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar token de autorización");
            return false;
        }
    }

    private async Task<bool> ValidateJwtTokenAsync(string token)
    {
        // Implementar validación JWT aquí
        // Por ahora, retorna true para tokens no vacíos
        await Task.CompletedTask;
        return !string.IsNullOrEmpty(token);
    }

    private string? ExtractUserIdFromToken(string authHeader)
    {
        try
        {
            // Implementar extracción de User ID del token
            // Por ahora, retorna un ID de prueba
            return "1";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al extraer User ID del token");
            return null;
        }
    }
}

public class LoggingMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var functionName = context.FunctionDefinition.Name;
        var traceId = context.TraceContext.TraceId;
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("Iniciando ejecución de función: {FunctionName} | TraceId: {TraceId}", 
            functionName, traceId);

        try
        {
            await next(context);
            
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Función ejecutada exitosamente: {FunctionName} | Duración: {Duration}ms | TraceId: {TraceId}", 
                functionName, duration.TotalMilliseconds, traceId);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Error en función: {FunctionName} | Duración: {Duration}ms | TraceId: {TraceId}", 
                functionName, duration.TotalMilliseconds, traceId);
            throw;
        }
    }
}

public class CorsMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<CorsMiddleware> _logger;

    public CorsMiddleware(ILogger<CorsMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpRequestData = context.GetHttpRequestData();
        
        if (httpRequestData != null)
        {
            // Manejar preflight requests
            if (httpRequestData.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Manejando preflight CORS request");
                
                var response = httpRequestData.CreateResponse(System.Net.HttpStatusCode.OK);
                AddCorsHeaders(response);
                
                context.GetInvocationResult().Value = response;
                return;
            }
        }

        await next(context);

        // Agregar headers CORS a la respuesta
        if (httpRequestData != null && context.GetInvocationResult().Value is Microsoft.Azure.Functions.Worker.Http.HttpResponseData responseData)
        {
            AddCorsHeaders(responseData);
        }
    }

    private static void AddCorsHeaders(Microsoft.Azure.Functions.Worker.Http.HttpResponseData response)
    {
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With");
        response.Headers.Add("Access-Control-Max-Age", "86400");
    }
}

public class RateLimitingMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private static readonly Dictionary<string, List<DateTime>> _requests = new();
    private static readonly object _lock = new();

    public RateLimitingMiddleware(ILogger<RateLimitingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpRequestData = context.GetHttpRequestData();
        
        if (httpRequestData != null)
        {
            var clientId = GetClientIdentifier(httpRequestData);
            var isAllowed = CheckRateLimit(clientId);

            if (!isAllowed)
            {
                _logger.LogWarning("Rate limit excedido para cliente: {ClientId}", clientId);
                
                var response = httpRequestData.CreateResponse(System.Net.HttpStatusCode.TooManyRequests);
                response.Headers.Add("Content-Type", "application/json");
                await response.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    Error = new
                    {
                        Message = "Demasiadas solicitudes. Intente más tarde.",
                        Code = "RATE_LIMIT_EXCEEDED"
                    }
                }));
                
                context.GetInvocationResult().Value = response;
                return;
            }
        }

        await next(context);
    }

    private string GetClientIdentifier(Microsoft.Azure.Functions.Worker.Http.HttpRequestData request)
    {
        // Obtener identificador del cliente (IP, API Key, User ID, etc.)
        var authHeader = request.Headers.GetValues("Authorization").FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader))
        {
            return authHeader.GetHashCode().ToString();
        }

        // Fallback a IP address (en producción usar X-Forwarded-For)
        return request.Headers.GetValues("X-Client-IP").FirstOrDefault() ?? "unknown";
    }

    private bool CheckRateLimit(string clientId, int maxRequests = 100, TimeSpan window = default)
    {
        if (window == default)
            window = TimeSpan.FromMinutes(1);

        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var windowStart = now - window;

            if (!_requests.ContainsKey(clientId))
            {
                _requests[clientId] = new List<DateTime>();
            }

            var clientRequests = _requests[clientId];
            
            // Remover requests fuera de la ventana
            clientRequests.RemoveAll(r => r < windowStart);

            if (clientRequests.Count >= maxRequests)
            {
                return false;
            }

            clientRequests.Add(now);
            return true;
        }
    }
}