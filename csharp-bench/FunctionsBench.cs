using System;
using System.Linq;
using System.Runtime.CompilerServices;
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
            self.DirectUnrolled();
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
            Assert.Equal(expected, CalculateDirectUnrolled(va,vb));
            Assert.Equal(expected, CalculateDirectUnsafe(va,vb));
            Assert.Equal(expected, CalculateIterator(va,vb));
            Assert.Equal(expected, CalculateIteratorSimpler(va,vb));

            Console.WriteLine("SelfTest checks succeeded");
            Console.WriteLine();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                int gated = includeMask & (a * b);
                sum += gated;
            }
            return sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long CalculateDirectUnrolled(int[] va, int[] vb)
        {
            long sum = 0;

            var len = va.Length;
            var spana = va.AsSpan(0, len);
            var spanb = vb.AsSpan(0, len);
            var chunkEnd = (len >> 2) << 2; 

            var i = 0;
            while (i < chunkEnd) 
            {
                var sa = spana.Slice(i, 4);
                var sb = spanb.Slice(i, 4);

                var f0 = sa[0] > 2;
                var f1 = sa[1] > 2;
                var f2 = sa[2] > 2;
                var f3 = sa[2] > 3;
                var m0 = -Unsafe.As<bool, int>(ref f0);
                var m1 = -Unsafe.As<bool, int>(ref f1);
                var m2 = -Unsafe.As<bool, int>(ref f2);
                var m3 = -Unsafe.As<bool, int>(ref f3);

                var v0 = m0 & (sa[0] * sb[0]);
                var v1 = m1 & (sa[1] * sb[1]);
                var v2 = m2 & (sa[2] * sb[2]);
                var v3 = m3 & (sa[3] * sb[3]);

                var v01 = v0 + v1;
                var v23 = v2 + v3;
                sum += v01;
                sum += v23;

                i += 4;
            }
            while (i < len) {
                var a = spana[i];
                var b = spanb[i];
                
                var f = a > 2;
                var m = -Unsafe.As<bool, int>(ref f);

                var v = m & (a*b);
                sum += v;

                ++i;
            }
            return sum;
        }

        // TODO: create an unrolled or SIMD version 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long CalculateIterator(int[] va, int[] vb)
        {
            var res = va.Zip(vb)
                .Where(pair => pair.First > 2)
                .Select(pair => (long)(pair.First * pair.Second))
                .Sum();
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        public long DirectUnrolled()
        {
            var (va, vb) = testSet.Sample(rng);
            var res = CalculateDirectUnrolled(va, vb);
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
