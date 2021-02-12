
## C#

```
BenchmarkDotNet=v0.12.1, OS=ubuntu 18.04
Intel Xeon Platinum 8124M CPU 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET Core SDK=5.0.103
  [Host]     : .NET Core 5.0.3 (CoreCLR 5.0.321.7203, CoreFX 5.0.321.7203), X64 RyuJIT
  DefaultJob : .NET Core 5.0.3 (CoreCLR 5.0.321.7203, CoreFX 5.0.321.7203), X64 RyuJIT
```

|           Method |          Mean |        Error |       StdDev | Gen 0 | Gen 1 | Gen 2 | Allocated | Code Size |
|----------------- |--------------:|-------------:|-------------:|------:|------:|------:|----------:|----------:|
|           Direct |  63,813.81 ns |   165.540 ns |   138.234 ns |     - |     - |     - |         - |     272 B |
| DirectBranchless |  24,358.65 ns |    22.175 ns |    18.517 ns |     - |     - |     - |         - |     317 B |
|   DirectUnrolled |  22,458.46 ns |    67.313 ns |    59.671 ns |     - |     - |     - |         - |     573 B |
|     DirectUnsafe |  21,844.39 ns |    63.542 ns |    59.437 ns |     - |     - |     - |         - |     419 B |
|  DirectUnsafeAvx |   5,278.74 ns |   102.595 ns |   159.728 ns |     - |     - |     - |         - |     560 B |
|         Iterator | 555,791.22 ns | 2,549.665 ns | 2,260.212 ns |     - |     - |     - |     264 B |    1936 B |
|  IteratorSimpler | 563,697.39 ns | 2,462.605 ns | 2,303.522 ns |     - |     - |     - |     200 B |    1307 B |
|   SelectBaseline |      27.81 ns |     0.126 ns |     0.118 ns |     - |     - |     - |         - |     134 B |

## Java

```
Benchmark                                   Mode  Cnt        Score      Error  Units
FunctionsBenchmark.benchmarkBaselineSelect  avgt    5       28.322 ±    0.153  ns/op
FunctionsBenchmark.benchmarkDirect          avgt    5    62992.706 ±  169.410  ns/op
FunctionsBenchmark.benchmarkIterator        avgt    5   320732.428 ± 1290.976  ns/op
FunctionsBenchmark.benchmarkIteratorGuava   avgt    5  1026823.686 ± 8433.354  ns/op
```

## Rust

Refer to the central column. 
```
rng select baseline     time:   [19.238 ns 19.249 ns 19.262 ns]                                 
calculate_direct_index  time:   [10.721 us 10.744 us 10.775 us]                                    
calculate_direct        time:   [6.7978 us 6.8134 us 6.8290 us]                              
calculate_iter          time:   [5.8937 us 5.9042 us 5.9195 us]                            
calculate_fold          time:   [5.8410 us 5.8474 us 5.8539 us]                            
```
