from numpy import *
import time as tm
import sys
import os 

def kmeans(X,k):
    n = size(X,1)
    maxit = 20
    it = 0
    centers = X[:,0:k].copy()
    classes = zeros((1,n))
    oldCenters = centers.copy()
    while it < maxit:
        for i in range(n):
            dist = sum(abs(centers - X[:,i,newaxis]), axis=0)
            classes[0,i] = dist.argmin()
             
        for i in range(k):
            inClass = X[:,nonzero(classes == i)[1]]
            if inClass.size == 0:
                centers[:,i] = nan
            else:
                centers[:,i] = inClass.mean(axis=1)
 
        if all(oldCenters == centers):
            break
        else:
            oldCenters = centers.copy()
    return classes

def kmeans_noopt():
    if len(sys.argv) < 4:
        raise ValueError("Invalid arguments. Required: working_dir K measID append.csv ")

    array = loadtxt(os.path.join(sys.argv[1], 'input.csv'), delimiter=',')
    k = int(sys.argv[2])
    measID = int(sys.argv[3])
    with open(sys.argv[4], "w") as outfile:
        wall = tm.perf_counter()
        while tm.perf_counter() - wall < 10: 
            start = tm.perf_counter()
            classes = kmeans(array, k)
            now = tm.perf_counter()
            outfile.write("%i, %f, %f\r\n" % (measID, (now - wall) , now - start))
            print("Avg time: %f ms. result: %i" % (now - start, classes[...,-1]))

    # TODO: end measure, write out to append.csv
    savetxt(sys.argv[4], classes, delimiter=',')
    return classes

kmeans_noopt()
