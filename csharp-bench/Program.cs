using System;
using BenchmarkDotNet.Running;

namespace CsharpBench
{
    class Program
    {
        static void Main(string[] args)
        {
            FunctionsBench.SelfTest();
            BenchmarkRunner.Run<FunctionsBench>();
        }
    }
}
