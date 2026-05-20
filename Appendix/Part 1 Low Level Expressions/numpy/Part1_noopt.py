import time as tm
from numba import njit
import numpy.dtypes
import sys
import os
from numpy import *

# creates test data. This must be the same on all experiment targets (C#, FORTRAN, numpy)!
A = 1507 + arange(507 * 10 * 5 * 17, dtype=uint32).reshape(507, 10, 5, 17, order='F') * 200
B = 1507 + arange(1 * 1 * 5 * 17, dtype=uint32).reshape(1, 1, 5, 17, order='F')
# some constants
m0 = array([1142251548], dtype=uint32)
shift = uint32(17)
result = 1
rep = 1000
# where to write the measured times and the calculated results to
result_filename = "results_numpy_noopt.txt" if len(sys.argv) <= 2 else sys.argv[2] 
times_filename = "times_numpy_noopt.txt" if len(sys.argv) <= 1 else sys.argv[1] 

with open(times_filename, "w") as timesfile:
    with open(result_filename, "wb") as resultfile:
        # captures the start time 
        wall = tm.perf_counter()

        
        def myfunc(r):
            # repeat over rep iterations
            for i in range(r):
                # the expression to be measured
                res = sum((m0 & (A << shift)) | (~m0 & B), axis= 1, dtype=uint32)
            # return the last calculated result 
            return res 

        # repeat the measurement for minimum 10 sec
        while tm.perf_counter() - wall < 10:
            # print("test numpy_noopt: A << shift = %i |m0 & ... = %i | ~m0 & B = %i | '..| .. ' = %i" % ((A << shift).flat[0], m0 & (A << shift).flat[0], (~m0 & B).flat[0], ((m0 & (A << shift)) | (~m0 & B)).flat[0]))

            # measure all iterations at once (average)
            start = tm.perf_counter()
            result = myfunc(rep)
            now = tm.perf_counter()
            # write measured time to csv file: [wall clock in ms], [avg duration of each invocation run in ms]  
            timesfile.write("%f, %f\r\n" % ((now - wall) * 1000, (now - start) * 1000.0 / rep))
            # serialize the result to binary file. It will be used to compare it with other experiments. 
            resultfile.write(result.tobytes())
            print("Avg time, numpy_noopt: %f ms. result: %i" % (now - start, result[0,0,0]))
