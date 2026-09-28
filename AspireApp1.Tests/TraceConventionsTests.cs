using AspireApp1.ServiceDefaults;

namespace AspireApp1.Tests;

[TestClass]
public class TraceConventionsTests
{
    [TestMethod]
    [DataRow("/health", true)]
    [DataRow("/health/ready", true)]
    [DataRow("/alive", true)]
    [DataRow("/_blazor/negotiate", true)]
    [DataRow("/_framework/blazor.web.js", true)]
    [DataRow("/_content/app.css", true)]
    [DataRow("/styles/SITE.CSS", true)]
    [DataRow("/healthcare", false)]
    [DataRow("/forecast", false)]
    [DataRow("/flow/start", false)]
    public void IsNoisePath_OnlyFiltersRoutineRequests(string path, bool expected)
    {
        Assert.AreEqual(expected, TraceConventions.IsNoisePath(path));
    }
}
