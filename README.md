# ILNumerics Accelerator (.NET) - Benchmarks and Artifacts 2026
This repository collects performance benchmarks for the [ILNumerics Accelerator JIT compiler](https://ilnumerics.net/ilnumerics-accelerator-compiler.html) for high-performance numerical array algorithm codes on .NET. 
In the current state the benchmarks represent artifacts for the 2026' arxiv paper published by [ILNumerics GmbH](https://ilnumerics.net). It will subsequently be extended with more and updated benchmarks as ILNumerics Accelerator evolves. 

Each benchmark can be downloaded and reproduced locally. Docker images / build scripts are provided where applicable. 

## Content
<table style="border:solid black 0px">
  <tr>
    <td width="55%" valign="top">

### [Part 1 - Low Level Expressions](https://github.com/hokb/decentralized-array-execution-artifacts2026/tree/main/Appendix/Part%201%20Low%20Level%20Expressions)
Performance comparison of executing the array expression `sum((m0 & (A << shift)) | (~m0 & B), dim: 1)` with moderate array sizes on NumPy, FORTRAN and [ILNumerics Accelerator (.NET)](https://ilnumerics.net/ilnumerics-accelerator-compiler.html). 
</td>
    <td width="45%" valign="top" style="border:dashed brown 2px">
<img src="https://raw.githubusercontent.com/hokb/decentralized-array-execution-artifacts2026/refs/heads/main/Appendix/Part%201%20Low%20Level%20Expressions/Part1.svg" alt="Array expression performance on NumPy, FORTRAN and ILNumerics" width="100%">
</td>
  </tr>
  <tr>
    <td width="55%" valign="top">

### [Part 2a - `Parallel.For` vers. ILNumerics Accelerator](https://github.com/hokb/decentralized-array-execution-artifacts2026/tree/main/Appendix/Part%202a%20Loop%20Parallelization)
Compares the performance of automatic parallelization by [ILNumerics Accelerator (.NET)](https://ilnumerics.net/ilnumerics-accelerator-compiler.html) with the manual `Parallel.For` attempt on the array expression `sum(abs(sin(A)))`. 
</td>
    <td width="45%" valign="top" style="border:dashed brown 2px">
<img src="https://raw.githubusercontent.com/hokb/decentralized-array-execution-artifacts2026/refs/heads/main/Appendix/Part%202a%20Loop%20Parallelization/ILNumerics/Part2a_1500x1500_rep10.svg" alt="Array expression performance on NumPy, FORTRAN and ILNumerics" width="100%">
</td>
  </tr>
  <tr>
    <td width="55%" valign="top">

### [Part 2b - ILNumerics Accelerator vers. *Invalid* `Parallel.For`](https://github.com/hokb/decentralized-array-execution-artifacts2026/tree/main/Appendix/Part%202b%20Loop%20Parallelization)
Demonstrates strong-scaling capabilities by [ILNumerics Accelerator (.NET)](https://ilnumerics.net/ilnumerics-accelerator-compiler.html) where `Parallel.For` fails: an order dependent loop is parallelized. 
</td>
    <td width="45%" valign="top" style="border:dashed brown 2px">
<img src="https://raw.githubusercontent.com/hokb/decentralized-array-execution-artifacts2026/refs/heads/main/Appendix/Part%202b%20Loop%20Parallelization/ILNumerics/Part2b_1500x1500.svg" alt="Array expression performance on NumPy, FORTRAN and ILNumerics" width="100%">
</td>
  </tr>
  <tr>
    <td width="55%" valign="top">

### [Part 3 - ILNumerics Accelerator vers. *Invalid* `Parallel.For`](https://github.com/hokb/decentralized-array-execution-artifacts2026/tree/main/Appendix/Part%203%20KMeans)
ILNumerics Accelerator performs strong scaling on the iterative K-Means algorithm.  
</td>
    <td width="45%" valign="top" style="border:dashed brown 2px">
<img src="https://raw.githubusercontent.com/hokb/decentralized-array-execution-artifacts2026/refs/heads/main/Appendix/Part%203%20KMeans/Part3.svg" alt="Array expression performance on NumPy, FORTRAN and ILNumerics" width="100%">
</td>
  </tr>
</table>


|| |



