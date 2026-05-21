# Artifact 2 - Parallel.For vers. [ILNumerics Accelerator](https://ilnumerics.net/ilnumerics-accelerator-compiler.html)

This benchmark investigates the execution speed of various ways to parallelize: 

`sum(abs(sin(A)));`

for moderately sized A: `<double>[1500, 1500]` 

by running above expression ... 
* in vectorized / high-level form: `sum(abs(sin(A)))`, and 
* by manual iteration over the columns of A: 
  ```
  for (int i = 0; i < A.S[1]; i++) {
      CS[i] = sum(abs(sin(A[full, i])), dim: 0);
  }
  ```

Both ways are executed, using 
1) plain ILNumerics Computing Engine and 
2) ILNumerics Accelerator.  

Results are checked for correctness and compared with the manual iteration version using `Parallel.For`. 
![Part2a_1500x1500_rep10.svg](Part2a_1500x1500_rep10.svg) 

## Benchmark Structure
All benchmarks are handled from `ILNumerics\Part2a.csproj`. At runtime the project starts the 5 benchmarks, measures execution times and creates a plot (bmp, svg) using measured results. 

## Clone the repository (all benchmarks)

```
git clone https://github.com/hokb/decentralized-array-execution-artifacts2026 
```
Navigate into directory: `Appendix/Part 2a Loop Parallelization`.

## Running the Benchmark from Code
Make sure to have the latest .NET SDK installed. Find instructions in [here](/System%20Setup.txt).

Navigate into the `ILNumerics` subdirectory and start the project `Part2a.csproj`

```bash
dotnet run -c Release
```

## Results
The benchmark generates the following results and places them into the projects **output** folder: 

`\Appendix\Part 2a Loop Parallelization\bin\Release\Net8.0\`:   
`Part2a.svg`,`Part2a.bmp`, and `values.csv`.

## Notes
The size of A (1500 x 1500 double elements) is commonly large enough to justify manual parallelization (i.e.: it pays off to split the array and distribute to multiple cores). Such size was chosen, to make the comparison fair. ILNumerics Accelerator, however, does not so much depend on large-enough array data. Try to modify the benchmark to use much smaller data and see, how the advantage of splitting the data for manuel parallelization diminishes completely, while the speed-up by ILNumerics Accelerator on the abstract high-level expression is retained: 
![Part2a_200x200_rep500.svg](Part2a_200x200_rep500.svg)

## Feedback
Please let us know about your findings! Did you observe similar results ? Get in touch and have us take a look: [benchmarks@ilnumerics.net](benchmarks@ilnumerics.net)

## More 
[ILNumerics Website](https://ilnumerics.net)  
[Benchmark 1: Low Level Expressions](../Part1%20Low%20Level%20Expressions/Readme.md)  
[All benchmarks](/Readme.md)