using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CsharpBench
{
    public class TestSet
    {
        List<int[]> _vectors = new List<int[]>();

        public TestSet(int numVectors, int vectorLength)
        {
            var rng = new Random();
            for (var i = 0; i < numVectors; ++i)
            {
                var v = Enumerable.Range(0, vectorLength).Select(_ => rng.Next(0, 10)).ToArray();
                _vectors.Add(v);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (int[], int[]) Sample(Random rng)
        {
            var numVecs = _vectors.Count;
            return (
                _vectors[rng.Next(0, numVecs)],
                _vectors[rng.Next(0, numVecs)]
            );
        }

        public int[] Get(int idx) => _vectors[idx];
    }
}