using System;
using BenchmarkDotNet.Attributes;

namespace CsharpBench
{
    [DisassemblyDiagnoser]
    public class FunctionsBench
    {
        TestSet testSet = new TestSet(100, 20_000);
        Random rng = new Random();

        public FunctionsBench()
        {
        }

        // quick self test will throw if you've got something wrong
        public static void SelfTest()
        {
            var self = new FunctionsBench();
            self.BenchDirect();
            self.BenchDirectBranchless();
        }

        public long CalculateDirect(int[] va, int[] vb)
        {
            long sum = 0;
            for (var i = 0; i < va.Length; ++i)
            {
                var a = va[i];
                var b = vb[i];
                if (a > 2)
                {
                    sum += a * b;
                }
            }
            return sum;
        }

        public long CalculateDirectBranchless(int[] va, int[] vb)
        {
            long sum = 0;
            for (var i = 0; i < va.Length; ++i)
            {
                var a = va[i];
                var b = vb[i];

                // mask is 0xFFFF when flag is true
                bool includeFlag = a > 2;
                int includeMask = -System.Runtime.CompilerServices.Unsafe.As<bool, int>(ref includeFlag);

                // mutliply apply mask
                int mult = a*b;
                int gated = includeMask & mult;

                //Console.WriteLine($"{a} {b} gated {gated} include mask {includeMask:x}");
                sum += gated;
            }
            return sum;
        }

        [Benchmark]
        public long BenchDirect()
        {
            var (va, vb) = testSet.Sample(rng);
            var res = CalculateDirect(va, vb);
            return res;
        }

        [Benchmark]
        public long BenchDirectBranchless()
        {
            var (va, vb) = testSet.Sample(rng);
            var res = CalculateDirectBranchless(va, vb);
            return res;
        }


    }

}
