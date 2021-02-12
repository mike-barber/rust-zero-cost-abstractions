# Cost of Abstractions

This repo contains some comparative benchmarks in
- C#, 
- Java, and
- Rust. 

Please note that it is *not* intended to compare the languages directly. I'm not knocking Java or C# here. The benchmarking framework is different for each, so a direct comparison on such a short benchmark is not valuable. What we are demonstrating here is the *relative* cost of using a functional-iterator pattern vs a simple loop in each language. I'm C# developer, so don't expect the Java benchmark to be as carefully checked.

Having said that, Rust does a pretty impressive job of _zero cost abstractions_ in certain cases. There are always ways to break it, but I've presented this case as an example of where it works really well. I've presented this at work to encourage developers to think about these things:

* How expensive your abstraction is
* Where your hot code is; typically it's only a small part of your code 
  * Consider avoiding expensive things on hot code paths
  * If it's really hot, consider some aggressive optimisation
* Do benchmarking and profiling if you're not sure what's going on 
* Learn some Rust; it's really fun

## Benchmarking

It's the following that we're benchmarking: 

- We create a TestSet from which we sample pairs of vectors for each iteration.
  - The test set contains 100 vectors
  - Vectors are linear arrays, int32, 20k elements, _precalculated_ random values in `[0;10)`
  - Around 8MB dataset
  - This is all pre-benchmark setup
- Then we have various functions that calculate a number based on a pair of vectors,
  according to these rules for vectors `va` and `vb`:
  - start with `sum = 0`
  - for every pair `a`, `b` in aligned vectors `va`, `vb`
     - if `a > 2`, then `sum += a * b`
  - return `sum`

Pretty simple stuff, but interesting enough to demonstrate performance. The `a>2` is enough to mess up any compiler that decides to use branch instructions, which of course then happens. But that's not the focus of this -- we're looking at the cost of iterators :)

Comparing the C#, Java and Rust benchmarks, we find:
- C# and Java iterators (Linq or Streams) are stupidly expensive compared to boring loops
  - iterators are in the 300-800 microsecond range
  - direct loop is around 65 microseconds in both languages
- Yeah, I'm probably abusing Java -- maybe the lack of built in tuples and a zip function is a hint.
- Would love to see how Scala does - it must be better? Just haven't had time to put it together.
- Rust does an outstanding job even with the iterators
  - as fast as the C# AVX2 implementation
  - around 5-6 microseconds

Of course, this is with `target-cpu=native` for Rust to enable the AVX2 instructions. Tested on an AWS c5.xlarge (skylake xeon) machine.

In all cases, I'm also measuring the time it takes to randomly pick two vectors, so that I can be sure this won't bias the results. It's irrelevant - around 20 nanoseconds for all languages.

## C#

We're using [BenchmarkDotNet](https://benchmarkdotnet.org/articles/overview.html)

There are several implementations in addition to the linq ones: 
- normal loop
- unrolled loop, with a safe-ish branchless/mask function to replace `if (x>2)`, implemented in SSE
- unsafe, unrolled loop
- unsafe AVX2 


```
BenchmarkDotNet=v0.12.1, OS=ubuntu 18.04
Intel Xeon Platinum 8124M CPU 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET Core SDK=5.0.103
  [Host]     : .NET Core 5.0.3 (CoreCLR 5.0.321.7203, CoreFX 5.0.321.7203), X64 RyuJIT
  DefaultJob : .NET Core 5.0.3 (CoreCLR 5.0.321.7203, CoreFX 5.0.321.7203), X64 RyuJIT
```

|           Method |          Mean |        Error |       StdDev | Gen 0 | Gen 1 | Gen 2 | Allocated | Code Size |
|----------------- |--------------:|-------------:|-------------:|------:|------:|------:|----------:|----------:|
|           Direct |  63,685.72 ns |   108.506 ns |    90.607 ns |     - |     - |     - |         - |     272 B |
| DirectBranchless |  24,463.49 ns |   215.987 ns |   180.359 ns |     - |     - |     - |         - |     317 B |
|   DirectUnrolled |  22,166.03 ns |    78.709 ns |    69.773 ns |     - |     - |     - |         - |     573 B |
|     DirectUnsafe |  17,731.75 ns |    11.628 ns |     9.710 ns |     - |     - |     - |         - |     670 B |
|  DirectUnsafeAvx |   5,362.40 ns |   104.761 ns |   153.557 ns |     - |     - |     - |         - |     560 B |
|         Iterator | 554,312.97 ns | 1,788.428 ns | 1,585.395 ns |     - |     - |     - |     265 B |    1936 B |
|  IteratorSimpler | 549,125.86 ns | 1,559.249 ns | 1,382.233 ns |     - |     - |     - |     200 B |    1307 B |
|   SelectBaseline |      26.79 ns |     0.102 ns |     0.090 ns |     - |     - |     - |         - |     134 B |

## Java

We're using [JMH](https://github.com/openjdk/jmh). Hopefully correctly. I'm haven't coded in anger in Java in long time, but the numbers appear spot on for the direct loop, so I'm relatively confident it's right.

```
Benchmark                                   Mode  Cnt        Score      Error  Units
FunctionsBenchmark.benchmarkBaselineSelect  avgt    5       28.322 ±    0.153  ns/op
FunctionsBenchmark.benchmarkDirect          avgt    5    62992.706 ±  169.410  ns/op
FunctionsBenchmark.benchmarkIterator        avgt    5   320732.428 ± 1290.976  ns/op
FunctionsBenchmark.benchmarkIteratorGuava   avgt    5  1026823.686 ± 8433.354  ns/op
```

## Rust

We're using [Criterion](https://crates.io/crates/criterion)

Refer to the central column for the average
```
rng select baseline     time:   [19.238 ns 19.249 ns 19.262 ns]                                 
calculate_direct_index  time:   [10.721 us 10.744 us 10.775 us]                                    
calculate_direct        time:   [6.7978 us 6.8134 us 6.8290 us]                              
calculate_iter          time:   [5.8937 us 5.9042 us 5.9195 us]                            
calculate_fold          time:   [5.8410 us 5.8474 us 5.8539 us]                            
```
