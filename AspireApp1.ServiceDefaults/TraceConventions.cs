namespace AspireApp1.ServiceDefaults;

/// <summary>Shared filtering for routine HTTP traffic that is not part of a business flow.</summary>
public static class TraceConventions
{
    private static readonly string[] StaticAssetExtensions =
    [
        ".css", ".js", ".map", ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico", ".woff", ".woff2"
    ];

    /// <summary>Returns true for health probes and framework/static-asset requests.</summary>
    public static bool IsNoisePath(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        return IsSegment(path, "/health") || IsSegment(path, "/alive")
            || IsSegment(path, "/_blazor") || IsSegment(path, "/_framework")
            || IsSegment(path, "/_content")
            || StaticAssetExtensions.Any(extension => path.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSegment(string path, string segment) =>
        path.Equals(segment, StringComparison.OrdinalIgnoreCase)
        || path.StartsWith(segment + "/", StringComparison.OrdinalIgnoreCase);
}
