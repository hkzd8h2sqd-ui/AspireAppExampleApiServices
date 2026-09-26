using AspireApp1.ServiceDefaults;
using Microsoft.AspNetCore.Http;

namespace AspireApp1.Tests;

[TestClass]
public class BusinessCorrelationHandlerTests
{
    [TestMethod]
    public async Task SendAsync_ForwardsBusinessIdWithoutChangingTraceHeaders()
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        accessor.HttpContext.Items["correlation_id"] = "order-1";
        var recorder = new HeaderRecorder();
        using var client = new HttpClient(new BusinessCorrelationHandler(accessor) { InnerHandler = recorder });
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/forecast");
        request.Headers.TryAddWithoutValidation("traceparent",
            "00-4bf92f3577b34da6a3ce929d0e0e4736-1111111111111111-01");

        using var response = await client.SendAsync(request);

        Assert.AreEqual("order-1", recorder.CorrelationId);
        Assert.AreEqual("00-4bf92f3577b34da6a3ce929d0e0e4736-1111111111111111-01", recorder.TraceParent);
    }

    [TestMethod]
    public async Task SendAsync_WithoutBusinessId_DoesNotInventOne()
    {
        var recorder = new HeaderRecorder();
        using var client = new HttpClient(new BusinessCorrelationHandler(new HttpContextAccessor())
        {
            InnerHandler = recorder
        });

        using var response = await client.GetAsync("http://localhost/forecast");

        Assert.IsNull(recorder.CorrelationId);
    }

    private sealed class HeaderRecorder : HttpMessageHandler
    {
        public string? CorrelationId { get; private set; }
        public string? TraceParent { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CorrelationId = request.Headers.TryGetValues("X-Correlation-Id", out var values) ? values.Single() : null;
            TraceParent = request.Headers.TryGetValues("traceparent", out var traceValues) ? traceValues.Single() : null;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
