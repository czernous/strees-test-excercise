using BenchmarkDotNet.Running;

namespace StressTestApp.Server.Benchmarks;

public static class BenchmarkEntryPoint
{
    public static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(BenchmarkEntryPoint).Assembly).Run(args);
}
