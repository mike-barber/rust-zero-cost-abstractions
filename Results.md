
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
|           Direct |  63,685.72 ns |   108.506 ns |    90.607 ns |     - |     - |     - |         - |     272 B |
| DirectBranchless |  24,463.49 ns |   215.987 ns |   180.359 ns |     - |     - |     - |         - |     317 B |
|   DirectUnrolled |  22,166.03 ns |    78.709 ns |    69.773 ns |     - |     - |     - |         - |     573 B |
|     DirectUnsafe |  17,731.75 ns |    11.628 ns |     9.710 ns |     - |     - |     - |         - |     670 B |
|  DirectUnsafeAvx |   5,362.40 ns |   104.761 ns |   153.557 ns |     - |     - |     - |         - |     560 B |
|         Iterator | 554,312.97 ns | 1,788.428 ns | 1,585.395 ns |     - |     - |     - |     265 B |    1936 B |
|  IteratorSimpler | 549,125.86 ns | 1,559.249 ns | 1,382.233 ns |     - |     - |     - |     200 B |    1307 B |
|   SelectBaseline |      26.79 ns |     0.102 ns |     0.090 ns |     - |     - |     - |         - |     134 B |

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
