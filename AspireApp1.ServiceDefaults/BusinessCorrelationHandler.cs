using Microsoft.AspNetCore.Http;

namespace AspireApp1.ServiceDefaults;

/// <summary>Forwards an existing business correlation ID separately from W3C trace context.</summary>
public sealed class BusinessCorrelationHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains("X-Correlation-Id")
            && accessor.HttpContext?.Items["correlation_id"] is string correlationId)
        {
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
