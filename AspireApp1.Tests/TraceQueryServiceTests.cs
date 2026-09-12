using AspireApp1.StateStore;
using AspireApp1.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AspireApp1.Tests;

[TestClass]
public class TraceQueryServiceTests
{
    [TestMethod]
    public async Task GetByTraceIdAsync_WhenInputIsAspireTraceDetailUrl_FindsTrace()
    {
        const string traceId = "c2e459604c1fb3f710ce788062e93418";
        var factory = CreateFactory();

        await using (var db = factory.CreateDbContext())
        {
            db.SpanRecords.Add(new SpanRecord
            {
                TraceId = traceId.ToUpperInvariant(),
                SpanId = "1111111111111111",
                ServiceName = "AspireApp1.ApiService",
                OperationName = "ApiService.CallApiServiceForecast",
                StartTime = DateTimeOffset.UtcNow.AddSeconds(-2),
                EndTime = DateTimeOffset.UtcNow,
                Status = SpanRecordStatus.OK,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var sut = new TraceQueryService(factory, NullLogger<TraceQueryService>.Instance);
        var result = await sut.GetByTraceIdAsync($"https://aspireapp1.dev.localhost:17127/traces/detail/{traceId}");

        Assert.IsNotNull(result);
        Assert.AreEqual(traceId, result.TraceId);
        Assert.AreEqual(1, result.Spans.Count);
    }

    [TestMethod]
    public async Task GetByTraceIdAsync_WhenFlowFailed_MapsStopStepAsStepXOfN()
    {
        const string traceId = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        const string flowRunId = "flow-run-01";
        var factory = CreateFactory();

        await using (var db = factory.CreateDbContext())
        {
            db.FlowRunRecords.Add(new FlowRunRecord
            {
                FlowRunId = flowRunId,
                FlowName = "LongFlow",
                CorrelationId = "corr-1",
                TraceId = traceId,
                StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                Status = FlowRunStatus.Failed,
                ErrorMessage = "One or more steps failed"
            });

            db.FlowStepRecords.AddRange(
                new FlowStepRecord
                {
                    FlowRunId = flowRunId,
                    StepOrder = 1,
                    StepName = "Step1",
                    ServiceName = "AspireApp1.WorkerService1",
                    Status = FlowStepStatus.Completed,
                    TraceId = traceId
                },
                new FlowStepRecord
                {
                    FlowRunId = flowRunId,
                    StepOrder = 2,
                    StepName = "Step2",
                    ServiceName = "AspireApp1.WorkerService2",
                    Status = FlowStepStatus.Failed,
                    ErrorMessage = "Timeout in WorkerService2",
                    TraceId = traceId
                },
                new FlowStepRecord
                {
                    FlowRunId = flowRunId,
                    StepOrder = 3,
                    StepName = "Step3",
                    ServiceName = "AspireApp1.WorkerService3",
                    Status = FlowStepStatus.Pending
                });

            await db.SaveChangesAsync();
        }

        var sut = new TraceQueryService(factory, NullLogger<TraceQueryService>.Instance);
        var result = await sut.GetByTraceIdAsync(traceId);

        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.FlowRuns.Count);
        var flow = result.FlowRuns[0];
        Assert.AreEqual("Fel", flow.Status);
        Assert.AreEqual(2, flow.CurrentStep);
        Assert.AreEqual(3, flow.TotalSteps);
        Assert.AreEqual(1, flow.LastSuccessStep);
        Assert.AreEqual(2, flow.ErrorStep);
        Assert.AreEqual("AspireApp1.WorkerService2", flow.CurrentService);
        Assert.AreEqual("Timeout in WorkerService2", flow.ErrorMessage);
    }

    [TestMethod]
    public async Task GetByTraceIdAsync_IncludesPendingStepsWithoutTraceIdForMatchedFlowRun()
    {
        const string traceId = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        const string flowRunId = "flow-run-02";
        var factory = CreateFactory();

        await using (var db = factory.CreateDbContext())
        {
            db.FlowRunRecords.Add(new FlowRunRecord
            {
                FlowRunId = flowRunId,
                FlowName = "LongFlow",
                CorrelationId = "corr-2",
                TraceId = traceId,
                StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                Status = FlowRunStatus.Running
            });

            db.FlowStepRecords.AddRange(
                new FlowStepRecord
                {
                    FlowRunId = flowRunId,
                    StepOrder = 1,
                    StepName = "Step1",
                    ServiceName = "AspireApp1.WorkerService1",
                    Status = FlowStepStatus.Completed,
                    TraceId = traceId
                },
                new FlowStepRecord
                {
                    FlowRunId = flowRunId,
                    StepOrder = 2,
                    StepName = "Step2",
                    ServiceName = "AspireApp1.WorkerService2",
                    Status = FlowStepStatus.Pending
                },
                new FlowStepRecord
                {
                    FlowRunId = flowRunId,
                    StepOrder = 3,
                    StepName = "Step3",
                    ServiceName = "AspireApp1.WorkerService3",
                    Status = FlowStepStatus.Pending
                });

            await db.SaveChangesAsync();
        }

        var sut = new TraceQueryService(factory, NullLogger<TraceQueryService>.Instance);
        var result = await sut.GetByTraceIdAsync(traceId);

        Assert.IsNotNull(result);
        var flow = result.FlowRuns.Single();
        Assert.AreEqual(3, flow.TotalSteps);
        Assert.AreEqual(1, flow.CurrentStep);
        Assert.AreEqual("Pågår", flow.Status);
        Assert.AreEqual("AspireApp1.WorkerService1", flow.CurrentService);
    }

    private static IDbContextFactory<StateStoreDbContext> CreateFactory()
    {
        var options = new DbContextOptionsBuilder<StateStoreDbContext>()
            .UseInMemoryDatabase($"TraceQueryServiceTests_{Guid.NewGuid():N}")
            .Options;
        return new TestDbContextFactory(options);
    }

    private sealed class TestDbContextFactory(DbContextOptions<StateStoreDbContext> options) : IDbContextFactory<StateStoreDbContext>
    {
        public StateStoreDbContext CreateDbContext() => new(options);
    }
}
