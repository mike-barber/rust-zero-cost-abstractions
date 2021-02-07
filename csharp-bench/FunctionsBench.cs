using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Xunit;

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
            self.Direct();
            self.DirectBranchless();
            self.DirectUnsafe();
            self.Iterator();
            self.IteratorSimpler();
            self.SelectBaseline();

            // check valid 
            var va = new[] { 1, 2, 3, 4, 5 };
            var vb = new[] { 5, 6, 7, 8, 9 };
            var expected = 3*7 + 4*8 + 5*9;

            Assert.Equal(expected, CalculateDirect(va,vb));
            Assert.Equal(expected, CalculateDirectBranchless(va,vb));
            Assert.Equal(expected, CalculateDirectUnsafe(va,vb));
            Assert.Equal(expected, CalculateIterator(va,vb));
            Assert.Equal(expected, CalculateIteratorSimpler(va,vb));

            Console.WriteLine("SelfTest checks succeeded");
            Console.WriteLine();
        }

        public static long CalculateDirect(int[] va, int[] vb)
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

        public static long CalculateDirectBranchless(int[] va, int[] vb)
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
                int mult = a * b;
                int gated = includeMask & mult;

                //Console.WriteLine($"{a} {b} gated {gated} include mask {includeMask:x}");
                sum += gated;
            }
            return sum;
        }

        // TODO: create an unrolled or SIMD version 
        public static long CalculateDirectUnsafe(int[] va, int[] vb)
        {
            long sum = 0;
            unsafe
            {
                fixed (int* ptra = va, ptrb = vb)
                {
                    var pa = ptra;
                    var pb = ptrb;
                    var paEnd = ptra + va.Length;

                    while (pa < paEnd)
                    {
                        var a = *pa;
                        var b = *pb;

                        if (a > 2)
                        {
                            sum += a * b;
                        }

                        pa += 1;
                        pb += 1;
                    }
                }
            }
            return sum;
        }

        public static long CalculateIterator(int[] va, int[] vb)
        {
            var res = va.Zip(vb)
                .Where(pair => pair.First > 2)
                .Select(pair => (long)(pair.First * pair.Second))
                .Sum();
            return res;
        }

        public static long CalculateIteratorSimpler(int[] va, int[] vb)
        {
            var res = va.Zip(vb)
                .Select(pair =>
                {
                    if (pair.First > 2)
                        return (long)(pair.First * pair.Second);
                    else
                        return 0;
                })
                .Sum();
            return res;
        }

        [Benchmark]
        public long Direct()
        {
            var (va, vb) = testSet.Sample(rng);
            var res = CalculateDirect(va, vb);
            return res;
        }

        [Benchmark]
        public long DirectBranchless()
        {
            var (va, vb) = testSet.Sample(rng);
            var res = CalculateDirectBranchless(va, vb);
            return res;
        }

        [Benchmark]
        public long DirectUnsafe()
        {
            var (va, vb) = testSet.Sample(rng);
            var res = CalculateDirectUnsafe(va, vb);
            return res;
        }

        [Benchmark]
        public long Iterator()
        {
            var (va, vb) = testSet.Sample(rng);
            var res = CalculateIterator(va, vb);
            return res;
        }

        [Benchmark]
        public long IteratorSimpler()
        {
            var (va, vb) = testSet.Sample(rng);
            var res = CalculateIteratorSimpler(va, vb);
            return res;
        }

        [Benchmark]
        public int SelectBaseline()
        {
            var (va, vb) = testSet.Sample(rng);
            return va.Length;
        }


    }

}
