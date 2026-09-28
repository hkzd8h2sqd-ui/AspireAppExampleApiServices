namespace AspireApp1.WorkerService3;

public sealed record WorkerJobMessage(
    string JobId,
    string TraceParent,
    string? TraceState,
    string CorrelationId,
    int Version = 1);
