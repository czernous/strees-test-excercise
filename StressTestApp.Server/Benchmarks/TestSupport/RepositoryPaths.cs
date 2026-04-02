namespace StressTestApp.Server.Benchmarks.TestSupport;

internal static class RepositoryPaths
{
    public static string FindServerDataCsvDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "Data", "Csv");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the original Data/Csv directory.");
    }
}
