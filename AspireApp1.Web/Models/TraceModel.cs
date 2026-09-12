namespace AspireApp1.Web.Models;

public enum SpanStatus
{
    OK,
    Warning,
    Error,
    InProgress,
    Unknown
}

public class LogEntryModel
{
    public DateTimeOffset Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string> Attributes { get; set; } = [];
}

public class SpanModel
{
    public string SpanId { get; set; } = string.Empty;
    public string? ParentSpanId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string OperationName { get; set; } = string.Empty;
    public DateTimeOffset StartTime { get; set; }
    public TimeSpan? Duration { get; set; }
    public SpanStatus Status { get; set; } = SpanStatus.Unknown;
    public string? ErrorMessage { get; set; }
    public int? HttpStatusCode { get; set; }
    public List<LogEntryModel> LogEntries { get; set; } = [];
}

public class TraceModel
{
    public string TraceId { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public SpanStatus OverallStatus { get; set; } = SpanStatus.Unknown;
    public List<SpanModel> Spans { get; set; } = [];
    public List<FlowRunStateModel> FlowRuns { get; set; } = [];
    public DateTimeOffset StartTime { get; set; }
}

public class FlowRunStateModel
{
    public string FlowRunId { get; set; } = string.Empty;
    public string FlowName { get; set; } = string.Empty;
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public string? CurrentService { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? LastSuccessStep { get; set; }
    public int? ErrorStep { get; set; }
    public string? ErrorMessage { get; set; }
}
