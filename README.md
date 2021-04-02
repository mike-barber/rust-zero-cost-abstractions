# Cost of Abstractions

This repo contains some comparative benchmarks in
- C#, 
- Java, and
- Rust. 

Please note that it is *not* intended to compare the languages directly. Specifically, I'm not knocking Java or C# here: both are good high-level languages. The benchmarking framework is different for each, so a direct comparison on such a short benchmark is not valuable. What we are demonstrating here is the *relative* cost of using a functional-iterator pattern vs a simple loop in each language. I'm C# developer, so don't expect the Java benchmark to be as carefully checked.

Having said that, Rust does an impressive job of **zero cost abstractions** in certain cases, in much the same way that C++ does (the term is originally from the C++ world). There are always ways to break it, but I've presented this case as an example of where it works really well. I've presented this at work to encourage developers to think about these things:

* How expensive your abstraction is
* Where your hot code is; typically it's only a small part of your code 
  * Consider avoiding doing expensive things on hot code paths
  * If it's a really hot path, consider some aggressive optimisation
* Do benchmarking and profiling if you're not sure what's going on 
* Learn some Rust; it's really fun!

## Benchmarking

It's the following that we're benchmarking: 

- We create a `TestSet` from which we sample pairs of vectors for each iteration.
  - The test set contains 100 vectors
  - Vectors are linear vectors, int32, 20k elements, _pre-initialized_ random values in `[0;10)`
  - Around 8MB dataset to pick vectors from
  - This is all set up before running benchmarks
- Then we have various functions that calculate a number based on a pair of vectors,
  according to these rules for vectors `va` and `vb`:
  - start with `sum = 0`
  - for every pair `a`, `b` in aligned vectors `va`, `vb`
     - if `a > 2`, then `sum += a * b`
  - return `sum`

Pretty simple stuff, but interesting enough to demonstrate some performance characteristics. The `a>2` is enough to mess up any compiler that decides to use branch instructions, which of course then happens in the Java/C# case. But that's not the focus of this -- we're looking at the cost of iterators primarily.

Comparing the C#, Java and Rust benchmarks, we find:

- C# and Java iterators (Linq or Streams) are stupidly expensive compared to classic loops
  - iterators are in the 300-800 microsecond range
  - direct loop is around 40-50 microseconds in both languages
  - i.e. the simple loop is around 10X faster, so you need to pay attention to where you're using iterators
- C# and Java classic loops themselves also aren't very fast, and can be improved using the classic techniques:
  - loop unrolling
  - branchless programming
  - SIMD intrinsics
  - only tested this in C#; I believe Java doesn't support intrinsics just yet
- Iterators in Java might be a bit unfair -- the lack of built-in tuples or a `zip` function is a hint
  - On that note, I'd love to see how Scala or Go does, but I haven't had time to look at these.
- Rust does an outstanding job with the iterators
  - around 3 microseconds
  - almost as fast as a hand-coded AVX2 implementation
  - iterators are actually faster than simple loops in this case, but they're usually in the same ballpark

Of course, this is with `target-cpu=native` for Rust to enable AVX2 instructions on my machine. This is enabled in the [.cargo/config](.cargo/config) file. I have Windows and Linux targets set there -- if you're keen to give this a go on a Mac, you'll need to add the config there. All benchmarks were run on my AMD Ryzen 3900X system, under WSL2. 

In all cases, I'm also measuring the time it takes to randomly pick two vectors, so that I can be sure this won't bias the results. It's irrelevant - around 20 nanoseconds for all languages.

Rust is fast for a few reasons, including:

- iterators are monomorphic and statically dispatched with no heap allocation
- the actual iterator code is optimised away
- the Rust compiler and LLVM backend are *very* good at the classic optimisations, and
- it will happily use SIMD instructions automatically

You'll notice you pay for these benefits at compile time -- it takes a while for the compiler to do all this work.

## Summary

Times are in **nanoseconds**

| Language                | C#            | Java          | Rust           |
|-------------------------|--------------:|--------------:|---------------:|
| Iterator                | 404,825       | 264,864       | 3,641          |
| Simple loop             | 47,022        | 44,744        | 6,578          |
| Unrolled, branchless    | 15,892        |               |                |
| AVX2 intrinsics         | 2,071         |               | 1,954          |

The table has some blanks. I can't use intrinsics in Java yet, and there's no point unrolling a loop in Rust by hand. 

## C#

We're using [BenchmarkDotNet](https://benchmarkdotnet.org/articles/overview.html)

There are several implementations in addition to the traditional ones: 
- normal loop
- branchless loop, with a safe-ish branchless/mask function to replace `if (x>2)`, implemented using SSE intrinsics
- unrolled loop (also branchless)
- unsafe, unrolled loop (also branchless) using pointer arithmetic
- unsafe AVX2 loop

C# responds well to all of these classic optimisation techniques, because the JIT compiler doesn't have the spare time available to do this stuff for you.

```
BenchmarkDotNet=v0.12.1, OS=ubuntu 20.04
AMD Ryzen 9 3900X, 1 CPU, 24 logical and 12 physical cores
.NET Core SDK=5.0.201
  [Host]     : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
  DefaultJob : .NET Core 5.0.4 (CoreCLR 5.0.421.11614, CoreFX 5.0.421.11614), X64 RyuJIT
```

|           Method |          Mean |        Error |       StdDev |
|----------------- |--------------:|-------------:|-------------:|
|           Direct |  47,022.29 ns |   293.281 ns |   259.986 ns |
| DirectBranchless |  17,932.28 ns |    40.106 ns |    37.515 ns |
|   DirectUnrolled |  15,891.53 ns |    50.564 ns |    44.824 ns |
|     DirectUnsafe |  12,046.63 ns |    88.918 ns |    83.174 ns |
|  DirectUnsafeAvx |   2,071.47 ns |    14.052 ns |    13.144 ns |
|         Iterator | 404,825.24 ns |   790.227 ns |   739.179 ns |
|  IteratorSimpler | 405,514.29 ns | 6,774.750 ns | 6,337.105 ns |
|   SelectBaseline |      20.00 ns |     0.024 ns |     0.020 ns |

## Java

We're using [JMH](https://github.com/openjdk/jmh). Hopefully correctly. I'm haven't coded in anger in Java in long time, but the numbers appear spot on for the direct loop, so I'm relatively confident it's right.

```
# JMH version: 1.27
# VM version: JDK 11.0.10, OpenJDK 64-Bit Server VM, 11.0.10+9-Ubuntu-0ubuntu1.20.04
# VM invoker: /usr/lib/jvm/java-11-openjdk-amd64/bin/java

Benchmark                                   Mode  Cnt       Score       Error  Units
FunctionsBenchmark.benchmarkBaselineSelect  avgt    5      13.835 ±     0.175  ns/op
FunctionsBenchmark.benchmarkDirect          avgt    5   44743.546 ±   149.830  ns/op
FunctionsBenchmark.benchmarkIterator        avgt    5  264864.180 ± 10846.190  ns/op
FunctionsBenchmark.benchmarkIteratorGuava   avgt    5  913337.174 ±  5627.959  ns/op
```

## Rust

We're using [Criterion](https://crates.io/crates/criterion)

Refer to the central column for the average. We're using Rust 1.50.
```
rng select baseline     time:   [14.172 ns 14.193 ns 14.218 ns] * note: nanoseconds in this row

calculate_direct_index  time:   [6.5779 us 6.5995 us 6.6310 us] * note: microseconds for these
calculate_direct        time:   [6.6033 us 6.6187 us 6.6371 us]                              
calculate_iter          time:   [3.6406 us 3.6609 us 3.6901 us]                            
calculate_fold          time:   [3.6491 us 3.6575 us 3.6675 us]                            
calculate_avx           time:   [1.9541 us 1.9586 us 1.9640 us]                           
```

Rust is almost **boring**, because it produces such fast code: there's often no point in writing a manually-unrolled loop. Sometimes it's worth writing AVX2 code or even inline assembly, but it has to be a really hot path for this to make sense. With Rust, the AVX2 code is only 1.8X faster than the iterator, and a lot less maintainable. Compare this with the over 23X improvement in the C# case.


