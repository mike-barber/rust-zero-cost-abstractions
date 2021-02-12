using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
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
            self.DirectUnsafeAvx();
            self.Iterator();
            self.IteratorSimpler();
            self.SelectBaseline();

            // check valid 
            var va = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var vb = new[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
            var expected = 900;

            Assert.Equal(expected, CalculateDirect(va, vb));
            Assert.Equal(expected, CalculateDirectBranchless(va, vb));
            Assert.Equal(expected, CalculateDirectUnrolled(va, vb));
            Assert.Equal(expected, CalculateDirectUnsafe(va, vb));
            Assert.Equal(expected, CalculateDirectUnsafeAvx(va, vb));
            Assert.Equal(expected, CalculateIterator(va, vb));
            Assert.Equal(expected, CalculateIteratorSimpler(va, vb));

            // check the hand-coded risky ones agree for long vectors
            var first = self.testSet.Get(0);
            var second = self.testSet.Get(1);
            Assert.Equal(CalculateDirect(first, second), CalculateDirectUnrolled(first, second));
            Assert.Equal(CalculateDirect(first, second), CalculateDirectUnsafe(first, second));
            Assert.Equal(CalculateDirect(first, second), CalculateDirectUnsafeAvx(first, second));

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

        public static int MaskGreaterThan(int value, int greaterThan)
        {
            // slightly faster with SSE on Intel, given it's just a scalar anyway
            return Sse2.CompareGreaterThan(Vector128.CreateScalar(value), Vector128.CreateScalar(greaterThan)).GetElement(0);
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
                // bool includeFlag = a > 2;
                // int includeMask = -System.Runtime.CompilerServices.Unsafe.As<bool, int>(ref includeFlag);
                int includeMask = MaskGreaterThan(a, 2);

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

                var m0 = MaskGreaterThan(sa[0], 2);
                var m1 = MaskGreaterThan(sa[1], 2);
                var m2 = MaskGreaterThan(sa[2], 2);
                var m3 = MaskGreaterThan(sa[3], 2);

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
            while (i < len)
            {
                var a = spana[i];
                var b = spanb[i];

                var m = MaskGreaterThan(a,2);

                var v = m & (a * b);
                sum += v;

                ++i;
            }
            return sum;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long CalculateDirectUnsafe(int[] va, int[] vb)
        {
            if (va.Length != vb.Length) throw new ArgumentException("length mismatch");

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

                        // create mask
                        int mask = MaskGreaterThan(a, 2);

                        // mutliply through, masking result
                        sum += mask & (a * b);

                        pa += 1;
                        pb += 1;
                    }
                }
            }
            return sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long CalculateDirectUnsafeAvx(int[] vecta, int[] vectb)
        {
            if (vecta.Length != vectb.Length) throw new ArgumentException("length mismatch");

            long sum = 0;
            var len = vecta.Length;
            var chunkEndIndex = (len >> 3) << 3;
            unsafe
            {
                fixed (int* ptra = vecta, ptrb = vectb)
                {
                    var chunkEnd = ptra + chunkEndIndex;
                    var allEnd = ptra + len;
                    var pa = ptra;
                    var pb = ptrb;

                    var value2 = Vector256.Create(2);
                    var value0 = Vector256.Create(0);

                    // two accumulators, each 4 x int64, for total 8 x int64
                    var acc1 = Vector256.Create(0L);
                    var acc2 = Vector256.Create(0L);

                    // main loop -- 8 elements at a time
                    while (pa < chunkEnd)
                    {
                        var a = Avx.LoadVector256(pa);
                        var b = Avx.LoadVector256(pb);

                        var mask = Avx2.CompareGreaterThan(a, value2);
                        a = Avx2.And(a, mask);

                        // odd numbered elements (i32 * i32 -> i64)
                        var m1 = Avx2.Multiply(a, b);
                        acc1 = Avx2.Add(acc1, m1);

                        // shuffle adjacent and multiply again
                        a = Avx2.Shuffle(a, 0b10_11_00_01);
                        b = Avx2.Shuffle(b, 0b10_11_00_01);
                        var m2 = Avx2.Multiply(a, b);
                        acc2 = Avx2.Add(acc2, m2);

                        pa += 8;
                        pb += 8;
                    }
                    // tail loop -- single elements
                    while (pa < allEnd)
                    {
                        // could do something smart like a masked load here, but 
                        // this will do for demonstration purposes.
                        var a = Vector256.CreateScalar(*pa);
                        var b = Vector256.CreateScalar(*pb);

                        // as above, accumulating into acc1 
                        var mask = Avx2.CompareGreaterThan(a, value2);
                        a = Avx2.And(a, mask);
                        var m1 = Avx2.Multiply(a, b);
                        acc1 = Avx2.Add(acc1, m1);

                        pa++;
                        pb++;
                    }

                    // sum all the elements
                    acc1 = Avx2.Add(acc1, acc2);
                    sum = acc1.GetElement(0) + acc1.GetElement(1) + acc1.GetElement(2) + acc1.GetElement(3);
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
        public long DirectUnsafeAvx()
        {
            var (va, vb) = testSet.Sample(rng);
            var res = CalculateDirectUnsafeAvx(va, vb);
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
