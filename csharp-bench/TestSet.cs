using System;
using System.Collections.Generic;
using System.Linq;

namespace CsharpBench
{
    public class TestSet
    {
        List<int[]> vectors = new List<int[]>();

        public TestSet(int numVectors, int vectorLength)
        {
            var rng = new Random();
            for (var i = 0; i < numVectors; ++i)
            {
                var v = Enumerable.Range(0, vectorLength).Select(_ => rng.Next(0, 10)).ToArray();
                vectors.Add(v);
            }
        }

        public (int[], int[]) Sample(Random rng)
        {
            var numVecs = vectors.Count;
            return (
                vectors[rng.Next(0, numVecs)],
                vectors[rng.Next(0, numVecs)]
            );
        }
    }
}