using AspireApp1.StateStore;
using AspireApp1.Web;
using Microsoft.Extensions.Logging.Abstractions;

namespace AspireApp1.Tests;

/// <summary>
/// Runs <see cref="TraceQueryService"/> against SQLite to guard against queries
/// that only work on SQL Server or the EF Core in-memory provider.
/// </summary>
[TestClass]
public class TraceQueryServiceSqliteTests
{
    private const string TraceId = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string CorrelationId = "corr-sqlite";

    [TestMethod]
    public async Task GetByTraceIdAsync_OnSqlite_ReturnsSpansOrderedByStartTime()
    {
        var factory = await SeedAsync();
        var sut = new TraceQueryService(factory, NullLogger<TraceQueryService>.Instance);

        var result = await sut.GetByTraceIdAsync(TraceId);

        Assert.IsNotNull(result);
        var spanIds = result.Spans.Select(s => s.SpanId).ToList();
        var firstIndex = spanIds.IndexOf("1111111111111111");
        var secondIndex = spanIds.IndexOf("2222222222222222");
        Assert.IsTrue(firstIndex >= 0 && secondIndex >= 0, "Both span records should be part of the trace.");
        Assert.IsTrue(firstIndex < secondIndex, "Span records should be ordered by StartTime, not insertion order.");
        Assert.AreEqual(1, result.FlowRuns.Count);
    }

    [TestMethod]
    public async Task GetByCorrelationIdAsync_OnSqlite_FindsTrace()
    {
        var factory = await SeedAsync();
        var sut = new TraceQueryService(factory, NullLogger<TraceQueryService>.Instance);

        var result = await sut.GetByCorrelationIdAsync(CorrelationId);

        Assert.IsNotNull(result);
        Assert.AreEqual(TraceId, result.TraceId);
    }

    [TestMethod]
    public async Task GetBySpanIdAsync_OnSqlite_FindsTrace()
    {
        var factory = await SeedAsync();
        var sut = new TraceQueryService(factory, NullLogger<TraceQueryService>.Instance);

        var result = await sut.GetBySpanIdAsync("2222222222222222");

        Assert.IsNotNull(result);
        Assert.AreEqual(TraceId, result.TraceId);
    }

    private static async Task<Microsoft.EntityFrameworkCore.IDbContextFactory<StateStoreDbContext>> SeedAsync()
    {
        var factory = await SqliteTestDatabase.CreateFactoryAsync();
        var now = DateTimeOffset.UtcNow;

        await using var db = await factory.CreateDbContextAsync();
        db.FlowRunRecords.Add(new FlowRunRecord
        {
            FlowRunId = "flow-sqlite",
            FlowName = "LongFlow",
            CorrelationId = CorrelationId,
            TraceId = TraceId,
            StartedAt = now.AddSeconds(-5),
            Status = FlowRunStatus.Completed
        });
        db.JobStates.Add(new JobStateRecord
        {
            JobId = "job-sqlite",
            ServiceName = "workerservice1",
            Status = JobStatus.Completed,
            TraceId = TraceId,
            CorrelationId = CorrelationId,
            CreatedAt = now.AddSeconds(-5),
            UpdatedAt = now
        });
        // Inserted out of chronological order to verify ordering by StartTime.
        db.SpanRecords.Add(new SpanRecord
        {
            TraceId = TraceId,
            SpanId = "2222222222222222",
            ParentSpanId = "1111111111111111",
            ServiceName = "workerservice1",
            OperationName = "Worker.ProcessJob",
            StartTime = now.AddSeconds(-3),
            EndTime = now.AddSeconds(-1),
            Status = SpanRecordStatus.OK,
            CreatedAt = now
        });
        db.SpanRecords.Add(new SpanRecord
        {
            TraceId = TraceId,
            SpanId = "1111111111111111",
            ServiceName = "apiservice",
            OperationName = "ApiService.Start",
            StartTime = now.AddSeconds(-4),
            EndTime = now,
            Status = SpanRecordStatus.OK,
            CreatedAt = now
        });
        await db.SaveChangesAsync();
        return factory;
    }
}
