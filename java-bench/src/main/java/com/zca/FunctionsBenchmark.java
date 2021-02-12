
package com.zca;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.Iterator;
import java.util.List;
import java.util.Random;
import java.util.concurrent.TimeUnit;
import java.util.stream.IntStream;
import java.util.stream.Stream;
import java.util.stream.StreamSupport;

import org.openjdk.jmh.annotations.Benchmark;
import org.openjdk.jmh.annotations.BenchmarkMode;
import org.openjdk.jmh.annotations.Fork;
import org.openjdk.jmh.annotations.Mode;
import org.openjdk.jmh.annotations.OutputTimeUnit;
import org.openjdk.jmh.annotations.Scope;
import org.openjdk.jmh.annotations.Setup;
import org.openjdk.jmh.annotations.State;

import scala.Tuple2;

@State(Scope.Benchmark)
public class FunctionsBenchmark {

    static int numVecs = 100;
    static int vecLength = 20000;
    private List<int[]> vectors;

    // thread-local state 
    @State(Scope.Thread)
    public static class ThreadState {
        public static Random rng = new Random();
    }

    // setup dataset
    @Setup
    public void setup() {
        var rng = new Random();
        vectors = new ArrayList<int[]>();
        for (int i=0; i<numVecs; ++i) {
            var vec = new int[vecLength];
            for (int j=0; j<vecLength; ++j) {
                vec[j] = rng.nextInt(10);
            }
            vectors.add(vec);
        }
    }

    private int[] Sample(Random rng) {
        var idx = rng.nextInt(vectors.size());
        return vectors.get(idx);
    }

    @Benchmark
    @BenchmarkMode(Mode.AverageTime)
    @OutputTimeUnit(TimeUnit.NANOSECONDS)
    @Fork(value = 1, warmups = 1)
    public int benchmarkBaselineSelect() {
        var va = Sample(ThreadState.rng);
        var vb = Sample(ThreadState.rng);
        return va.length + vb.length;
    }

    @Benchmark
    @BenchmarkMode(Mode.AverageTime)
    @OutputTimeUnit(TimeUnit.NANOSECONDS)
    @Fork(value = 1, warmups = 1)
    public long benchmarkDirect() {
        long sum = 0;
        var va = Sample(ThreadState.rng);
        var vb = Sample(ThreadState.rng);
        for (int i=0; i<va.length; ++i) {
            var a = va[i];
            var b = vb[i];
            if (a > 2) {
                sum += a * b;
            }
        }
        return sum;
    }

    @Benchmark
    @BenchmarkMode(Mode.AverageTime)
    @OutputTimeUnit(TimeUnit.NANOSECONDS)
    @Fork(value = 1, warmups = 1)
    public long benchmarkIterator() {
        var va = Sample(ThreadState.rng);
        var vb = Sample(ThreadState.rng);

        long sum = zip(Arrays.stream(va), Arrays.stream(vb))
            .filter(t -> t._1 > 2)
            .map(t -> (long)(t._1 * t._2))
            .reduce(0l, Long::sum);
                   
        return sum;
    }

    // pretty much have to write this; no built-in zip iteration
    // just returning Scala Tuple2
    public Stream<Tuple2<Integer,Integer>> zip(IntStream sa, IntStream sb) {
        final var ia = sa.iterator();
        final var ib = sb.iterator();
        Iterator<Tuple2<Integer,Integer>> iter = new Iterator<Tuple2<Integer,Integer>>() {
            @Override
            public boolean hasNext() {
                return ia.hasNext() && ib.hasNext();
            }
            @Override
            public Tuple2<Integer,Integer> next() {
                return Tuple2.apply(ia.next(), ib.next());
            }
        };
        Iterable<Tuple2<Integer,Integer>> iterable = () -> iter;
        return StreamSupport.stream(iterable.spliterator(),false);
    }
}
