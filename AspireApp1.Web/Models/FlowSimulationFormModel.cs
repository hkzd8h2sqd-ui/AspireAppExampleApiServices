namespace AspireApp1.Web.Models;

using AspireApp1.ServiceDefaults;

public sealed class FlowSimulationFormModel
{
    public int RetryAttempts { get; set; }
    public int RetryDelayMs { get; set; }
    public int NormalMinDelayMs { get; set; }
    public int NormalMaxDelayMs { get; set; }
    public int SlowMinDelayMs { get; set; }
    public int SlowMaxDelayMs { get; set; }
    public double SlowCallProbabilityPercent { get; set; }
    public double Http500ProbabilityPercent { get; set; }
    public int DeterministicSeed { get; set; }

    public static FlowSimulationFormModel FromSettings(FlowSimulationSettings settings)
    {
        var normalized = FlowSimulationPlanner.Normalize(settings);
        return new FlowSimulationFormModel
        {
            RetryAttempts = normalized.RetryAttempts,
            RetryDelayMs = normalized.RetryDelayMs,
            NormalMinDelayMs = normalized.NormalMinDelayMs,
            NormalMaxDelayMs = normalized.NormalMaxDelayMs,
            SlowMinDelayMs = normalized.SlowMinDelayMs,
            SlowMaxDelayMs = normalized.SlowMaxDelayMs,
            SlowCallProbabilityPercent = normalized.SlowCallProbabilityPercent,
            Http500ProbabilityPercent = normalized.Http500ProbabilityPercent,
            DeterministicSeed = normalized.DeterministicSeed
        };
    }

    public FlowSimulationSettings ToSettings()
    {
        return FlowSimulationPlanner.Normalize(new FlowSimulationSettings
        {
            Enabled = true,
            RetryAttempts = RetryAttempts,
            RetryDelayMs = RetryDelayMs,
            NormalMinDelayMs = NormalMinDelayMs,
            NormalMaxDelayMs = NormalMaxDelayMs,
            SlowMinDelayMs = SlowMinDelayMs,
            SlowMaxDelayMs = SlowMaxDelayMs,
            SlowCallProbabilityPercent = SlowCallProbabilityPercent,
            Http500ProbabilityPercent = Http500ProbabilityPercent,
            DeterministicSeed = DeterministicSeed
        });
    }
}
