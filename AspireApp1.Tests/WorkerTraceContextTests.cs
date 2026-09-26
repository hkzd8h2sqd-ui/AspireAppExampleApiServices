using System.Diagnostics;
using System.Text.Json;
using AspireApp1.WorkerService1;

namespace AspireApp1.Tests;

[TestClass]
public class WorkerTraceContextTests
{
    [TestMethod]
    public void TryParse_WithValidTraceParent_ReturnsTrue()
    {
        using var activity = new Activity("test");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();

        var result = WorkerTraceContext.TryParse(activity.Id, activity.TraceStateString, out var context);

        Assert.IsTrue(result);
        Assert.AreEqual(activity.TraceId, context.TraceId);
    }

    [TestMethod]
    public void TryParse_WithTraceState_RetainsRemoteParentAcrossRetries()
    {
        const string traceParent = "00-4bf92f3577b34da6a3ce929d0e0e4736-1111111111111111-01";
        Assert.IsTrue(WorkerTraceContext.TryParse(traceParent, "vendor=value", out var parent));
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "AspireApp1.WorkerService1",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);
        using var source = new ActivitySource("AspireApp1.WorkerService1");

        using var first = source.StartActivity("Worker.ProcessJob", ActivityKind.Consumer, parent);
        Assert.IsNotNull(first);
        Assert.AreEqual(parent.TraceId, first.TraceId);
        Assert.AreEqual(parent.SpanId, first.ParentSpanId);
        Assert.AreEqual("vendor=value", first.TraceStateString);
        first.Stop();

        using var retry = source.StartActivity("Worker.ProcessJob", ActivityKind.Consumer, parent);
        Assert.IsNotNull(retry);
        Assert.AreEqual(parent.TraceId, retry.TraceId);
        Assert.AreEqual(parent.SpanId, retry.ParentSpanId);
        Assert.AreNotEqual(first.SpanId, retry.SpanId);
    }

    [TestMethod]
    public void TryParse_WithMissingTraceParent_ReturnsFalse()
    {
        var result = WorkerTraceContext.TryParse(null, null, out _);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryParse_WithInvalidTraceParent_ReturnsFalse()
    {
        var result = WorkerTraceContext.TryParse("invalid-traceparent", null, out _);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void JobMessage_WithoutVersionDefaultsToV1_AndPreservesTraceContext()
    {
        const string json = """
            {"jobId":"job-1","traceParent":"00-4bf92f3577b34da6a3ce929d0e0e4736-1111111111111111-01","traceState":"vendor=value","correlationId":"order-1"}
            """;
        var job = JsonSerializer.Deserialize<WorkerJobMessage>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.IsNotNull(job);
        Assert.AreEqual(1, job.Version);
        Assert.AreEqual("order-1", job.CorrelationId);
        Assert.IsTrue(WorkerTraceContext.TryParse(job.TraceParent, job.TraceState, out var context));
        Assert.AreEqual("vendor=value", context.TraceState);
    }
}
