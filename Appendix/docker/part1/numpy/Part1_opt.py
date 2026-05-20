import time as tm
from numba import njit
import numpy.dtypes
from numpy import *

A = 1507 + arange(507 * 10 * 5 * 17, dtype=uint32).reshape(507, 10, 5, 17, order='F') * 200
B = 1507 + arange(1 * 1 * 5 * 17, dtype=uint32).reshape(1, 1, 5, 17, order='F')

m0 = array([1142251548])
shift = 17
result = 1
rep = 1000
with open("values_numpy_opt.txt", "w") as outfile:
    wall = tm.perf_counter()

    @njit
    def myfunc(r):

        for i in range(r):
            res = sum((m0 & (A << shift)) | (~m0 & B), axis= 1)
        return res

    while tm.perf_counter() - wall < 10:

        start = tm.perf_counter()
        result = myfunc(rep)
        now = tm.perf_counter()
        outfile.write("%f, %f\r\n" % ((now - wall) * 1000, now - start))
        print("Avg time: %f ms. result: %i" % (now - start, result[0,0,0]))
