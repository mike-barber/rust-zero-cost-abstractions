using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace CsharpBench
{
    class Program
    {
        static void Main(string[] args)
        {
            FunctionsBench.SelfTest();

            var job = Job.Default
                .WithEnvironmentVariables(new EnvironmentVariable[] {
                    // enable Dynamic Full PGO for .net 6.0
                    // references:
                    // - https://devblogs.microsoft.com/dotnet/announcing-net-6/#dynamic-pgo
                    // - https://devblogs.microsoft.com/dotnet/announcing-net-6/#ryujit
                    new EnvironmentVariable("DOTNET_TieredPGO", "1"),
                    new EnvironmentVariable("DOTNET_ReadyToRun", "0"),
                    new EnvironmentVariable("DOTNET_TC_QuickJitForLoops", "1")
                })
                .WithMaxIterationCount(30);

            var config = DefaultConfig.Instance
                .AddJob(job);

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
        }
    }
}
