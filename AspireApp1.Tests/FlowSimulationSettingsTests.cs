using AspireApp1.ServiceDefaults;

namespace AspireApp1.Tests;

[TestClass]
public class FlowSimulationSettingsTests
{
    [TestMethod]
    public void Normalize_ClampsRetryAttemptsToRangeOneToThree()
    {
        var below = FlowSimulationPlanner.Normalize(new FlowSimulationSettings { RetryAttempts = 0 });
        var above = FlowSimulationPlanner.Normalize(new FlowSimulationSettings { RetryAttempts = 9 });

        Assert.AreEqual(1, below.RetryAttempts);
        Assert.AreEqual(3, above.RetryAttempts);
    }

    [TestMethod]
    public void CreateAttemptPlan_WhenEnabledFalse_DoesNotInjectDelayOrFailure()
    {
        var plan = FlowSimulationPlanner.CreateAttemptPlan(
            FlowSimulationPlanner.Normalize(new FlowSimulationSettings { Enabled = false }),
            flowRunId: "flow-a",
            stepName: "RetryStep1.Validate",
            attempt: 1);

        Assert.IsFalse(plan.IsSimulationEnabled);
        Assert.AreEqual(0, plan.DelayMs);
        Assert.IsFalse(plan.ShouldFailWithHttp500);
    }

    [TestMethod]
    public void CreateAttemptPlan_IsDeterministicForSameSeedAndInput()
    {
        var settings = FlowSimulationPlanner.Normalize(new FlowSimulationSettings
        {
            Enabled = true,
            DeterministicSeed = 1337,
            SlowCallProbabilityPercent = 100,
            Http500ProbabilityPercent = 100
        });

        var plan1 = FlowSimulationPlanner.CreateAttemptPlan(settings, "flow-1", "RetryStep2.Process", 2);
        var plan2 = FlowSimulationPlanner.CreateAttemptPlan(settings, "flow-1", "RetryStep2.Process", 2);

        Assert.AreEqual(plan1.DelayMs, plan2.DelayMs);
        Assert.AreEqual(plan1.ShouldFailWithHttp500, plan2.ShouldFailWithHttp500);
        Assert.IsTrue(plan1.ShouldFailWithHttp500);
        Assert.IsTrue(plan1.DelayMs >= settings.SlowMinDelayMs && plan1.DelayMs <= settings.SlowMaxDelayMs);
    }

    [TestMethod]
    public void Normalize_ReordersAndClampsDelayRanges()
    {
        var settings = FlowSimulationPlanner.Normalize(new FlowSimulationSettings
        {
            NormalMinDelayMs = 900,
            NormalMaxDelayMs = 100,
            SlowMinDelayMs = -500,
            SlowMaxDelayMs = -10
        });

        Assert.AreEqual(100, settings.NormalMinDelayMs);
        Assert.AreEqual(900, settings.NormalMaxDelayMs);
        Assert.AreEqual(0, settings.SlowMinDelayMs);
        Assert.AreEqual(0, settings.SlowMaxDelayMs);
    }

    [TestMethod]
    public void FlowSimulationProfiles_HasLegacyRetryDefaults()
    {
        var profiles = new FlowSimulationProfiles();
        var retry = FlowSimulationPlanner.Normalize(profiles.RetryDemo);

        Assert.AreEqual(3, retry.RetryAttempts);
        Assert.AreEqual(10000, retry.RetryDelayMs);
        Assert.AreEqual(0, retry.SlowCallProbabilityPercent);
        Assert.AreEqual(0, retry.Http500ProbabilityPercent);
    }
}
