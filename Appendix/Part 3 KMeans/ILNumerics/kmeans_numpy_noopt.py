from numpy import *
from numpy._core.defchararray import center
 
def kmeans_numpy_noopt(X,k):
    n = size(X,1)
    maxit = 20
    centers = X[:,0:k].copy()
    classes = zeros((1.,n))
    oldCenters = centers.copy()
    for it in range(maxit):
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
    return centers