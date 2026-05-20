import time as tm
from numpy import *

A = 1507 + arange(507 * 10 * 5 * 17, dtype=uint32).reshape(507, 10, 5, 17, order='F') * 200
B = 1507 + arange(1 * 1 * 5 * 17, dtype=uint32).reshape(1, 1, 5, 17, order='F')

m0 = array([1142251548])
shift = 17
result = 1
rep = 1000
with open("values_numpy_noopt.txt", "w") as outfile:
    wall = tm.perf_counter()
    while tm.perf_counter() - wall < 10:

        start = tm.perf_counter()
        for i in range(rep):
            result = sum(astype((m0 & (A << shift)) | (~m0 & B), 'uint32'), axis=1, )

        now = tm.perf_counter()
        outfile.write("%f, %f\r\n" % ((now - wall) * 1000, now - start))
        print("%f result: %i" % (now - start, result[0,0,0]))
