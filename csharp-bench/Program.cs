using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;

namespace CsharpBench
{
    class Program
    {
        static void Main(string[] args)
        {
            FunctionsBench.SelfTest();

            // benchmarking
            var job = Job.Default
                .WithGcServer(true);

            // limited iterations
            job = job
                  .WithWarmupCount(5)
                  .WithIterationCount(10)
                  .WithIterationTime(TimeInterval.FromMilliseconds(500))
                  // c.f. https://devblogs.microsoft.com/dotnet/performance_improvements_in_net_7/#jit
                  // enable for loops (not default, but now viable with on-stack replacement)
                  .WithEnvironmentVariable("DOTNET_TC_QuickJitForLoops", "1") 
                  // ensure tiered PGO is enabled
                  .WithEnvironmentVariable("DOTNET_TieredPGO", "1") 
                  // disable loading RTR code for standard libraries, ensuring they get fully optimised too
                  // (this doesn't make much of a difference for this benchmark)
                  .WithEnvironmentVariable("DOTNET_ReadyToRun", "0")
                  ;

            var config = DefaultConfig.Instance
                .AddJob(job);

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
        }
    }
}
