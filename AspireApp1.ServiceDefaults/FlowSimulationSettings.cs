namespace AspireApp1.ServiceDefaults;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Configurable simulation settings for retry-demo process flows.
/// </summary>
public sealed class FlowSimulationSettings
{
    public const string SectionName = "FlowSimulation";

    /// <summary>
    /// Enables simulated delay/failure behavior for retry-demo flow steps.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Number of attempts per step. Allowed range is 1-3.
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Minimum delay for normal calls in milliseconds.
    /// </summary>
    public int NormalMinDelayMs { get; set; } = 10;

    /// <summary>
    /// Maximum delay for normal calls in milliseconds.
    /// </summary>
    public int NormalMaxDelayMs { get; set; } = 500;

    /// <summary>
    /// Minimum delay for intermittent slow calls in milliseconds.
    /// </summary>
    public int SlowMinDelayMs { get; set; } = 5000;

    /// <summary>
    /// Maximum delay for intermittent slow calls in milliseconds.
    /// </summary>
    public int SlowMaxDelayMs { get; set; } = 30000;

    /// <summary>
    /// Probability (0-100) that a call becomes slow.
    /// </summary>
    public double SlowCallProbabilityPercent { get; set; } = 20;

    /// <summary>
    /// Probability (0-100) that a call simulates HTTP 500.
    /// </summary>
    public double Http500ProbabilityPercent { get; set; } = 15;

    /// <summary>
    /// Delay between retry attempts in milliseconds.
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Seed used for deterministic, reproducible simulation decisions.
    /// </summary>
    public int DeterministicSeed { get; set; } = 2026;
}

/// <summary>
/// Provides deterministic simulation decisions for each flow step attempt.
/// </summary>
public static class FlowSimulationPlanner
{
    /// <summary>
    /// Normalizes and clamps retry and range values to safe bounds.
    /// </summary>
    public static FlowSimulationSettings Normalize(FlowSimulationSettings source)
    {
        var normalMin = Math.Max(0, Math.Min(source.NormalMinDelayMs, source.NormalMaxDelayMs));
        var normalMax = Math.Max(normalMin, Math.Max(source.NormalMinDelayMs, source.NormalMaxDelayMs));
        var slowMin = Math.Max(0, Math.Min(source.SlowMinDelayMs, source.SlowMaxDelayMs));
        var slowMax = Math.Max(slowMin, Math.Max(source.SlowMinDelayMs, source.SlowMaxDelayMs));

        return new FlowSimulationSettings
        {
            Enabled = source.Enabled,
            RetryAttempts = Math.Clamp(source.RetryAttempts, 1, 3),
            NormalMinDelayMs = normalMin,
            NormalMaxDelayMs = normalMax,
            SlowMinDelayMs = slowMin,
            SlowMaxDelayMs = slowMax,
            SlowCallProbabilityPercent = Math.Clamp(source.SlowCallProbabilityPercent, 0, 100),
            Http500ProbabilityPercent = Math.Clamp(source.Http500ProbabilityPercent, 0, 100),
            RetryDelayMs = Math.Max(0, source.RetryDelayMs),
            DeterministicSeed = source.DeterministicSeed
        };
    }

    /// <summary>
    /// Builds a deterministic simulation plan for a given step attempt.
    /// </summary>
    public static FlowSimulationAttemptPlan CreateAttemptPlan(
        FlowSimulationSettings settings,
        string flowRunId,
        string stepName,
        int attempt)
    {
        if (!settings.Enabled)
        {
            return new FlowSimulationAttemptPlan(false, 0, false);
        }

        var slowProbability = settings.SlowCallProbabilityPercent / 100.0;
        var errorProbability = settings.Http500ProbabilityPercent / 100.0;

        var isSlow = NextUnitInterval(settings.DeterministicSeed, flowRunId, stepName, attempt, "slow") < slowProbability;
        var (minDelay, maxDelay) = isSlow
            ? (settings.SlowMinDelayMs, settings.SlowMaxDelayMs)
            : (settings.NormalMinDelayMs, settings.NormalMaxDelayMs);

        var delaySpread = Math.Max(0, maxDelay - minDelay);
        var delayScale = NextUnitInterval(settings.DeterministicSeed, flowRunId, stepName, attempt, "delay");
        var delayMs = minDelay + (int)Math.Round(delayScale * delaySpread, MidpointRounding.AwayFromZero);

        var shouldFailWithHttp500 = NextUnitInterval(settings.DeterministicSeed, flowRunId, stepName, attempt, "error") < errorProbability;
        return new FlowSimulationAttemptPlan(true, delayMs, shouldFailWithHttp500);
    }

    private static double NextUnitInterval(int seed, string flowRunId, string stepName, int attempt, string discriminator)
    {
        var input = $"{seed}|{flowRunId}|{stepName}|{attempt}|{discriminator}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var value = BitConverter.ToUInt64(hash, 0);
        return value / (double)ulong.MaxValue;
    }
}

/// <summary>
/// Deterministic simulation decision for one attempt.
/// </summary>
/// <param name="IsSimulationEnabled">True when simulation is enabled.</param>
/// <param name="DelayMs">Delay to apply before execution.</param>
/// <param name="ShouldFailWithHttp500">True when attempt should simulate HTTP 500.</param>
public sealed record FlowSimulationAttemptPlan(
    bool IsSimulationEnabled,
    int DelayMs,
    bool ShouldFailWithHttp500);

/// <summary>
/// Per-flow default simulation settings loaded from appsettings.
/// </summary>
public sealed class FlowSimulationProfiles
{
    public const string SectionName = "FlowSimulationProfiles";

    public FlowSimulationSettings RetryDemo { get; set; } = new()
    {
        Enabled = true,
        RetryAttempts = 3,
        RetryDelayMs = 10000,
        NormalMinDelayMs = 10,
        NormalMaxDelayMs = 500,
        SlowMinDelayMs = 5000,
        SlowMaxDelayMs = 30000,
        SlowCallProbabilityPercent = 0,
        Http500ProbabilityPercent = 0,
        DeterministicSeed = 2026
    };

    public FlowSimulationSettings IntermittentDemo { get; set; } = new()
    {
        Enabled = true,
        RetryAttempts = 3,
        RetryDelayMs = 1000,
        NormalMinDelayMs = 10,
        NormalMaxDelayMs = 500,
        SlowMinDelayMs = 5000,
        SlowMaxDelayMs = 30000,
        SlowCallProbabilityPercent = 20,
        Http500ProbabilityPercent = 15,
        DeterministicSeed = 2026
    };
}

/// <summary>
/// Request body used when starting a flow with optional simulation overrides.
/// </summary>
/// <param name="SimulationSettings">Runtime simulation settings for this flow run.</param>
public sealed record FlowStartSimulationRequest(
    FlowSimulationSettings? SimulationSettings);

/// <summary>
/// Response with default simulation profiles for Retry and Intermittent flows.
/// </summary>
/// <param name="RetryDemo">Default settings for retry-demo flow.</param>
/// <param name="IntermittentDemo">Default settings for intermittent-demo flow.</param>
public sealed record FlowSimulationProfilesResponse(
    FlowSimulationSettings RetryDemo,
    FlowSimulationSettings IntermittentDemo);
