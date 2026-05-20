using ILNumerics;
using static ILNumerics.Globals;
using static ILNumerics.ILMath;
using System.Diagnostics;
//ILN(enabled=true)     // <-- remove this line or set 'enabled=true' for acceleration in Release mode 

// Just some arbitrary data: [1203 x 1203] 
Array<double> A = todouble(repmat(SpecialData.terrain, 3, 3)) * sin(arange<double>(1, 401 * 3) * 0.1);
var sw = new Stopwatch();
for (int i = 0; i < 15; i++) {

    sw.Restart();
    Array<long> classes = 1;
    Array<double> centers = kmeans(A, k: 50, classes, out int iterations);
    System.Console.WriteLine("Finished: " + iterations + " iterations in: " + sw.ElapsedMilliseconds + "ms");

}

static RetArray<double> kmeans(InArray<double> A, int k, OutArray<long> classes, out int iterations) {

    using var _ = Scope.Enter();

    Array<double> centers = A[full, linspace(0, A.S[1] - 2, k)];
    classes.a = empty<long>(A.S[1]);
    Array<long> oldClasses = 0, C = 1;
    iterations = 0;
    // main iterations loop. Repeats both k-means phases: 
    do {
        oldClasses.a = classes;   // exit condition              
        classes[full] = -1;

        // phase 1) assign samples to clusters
        for (int i = 0; i < A.S[1]; i++) {

            min(sqrt(sum(squared(A[full, i] - centers), 0)), dim: 1, I: C);
            classes[i] = C;

        }
        // phase 2) recompute cluster centers
        for (int i = 0; i < centers.S[1]; i++) {

            Array<long> found = find(classes == i);
            if (found.Length > 0) {
                centers[full, i] = mean(A[full, found], dim: 1);
            }
        }
        iterations++;

    } while (!classes.Equals(oldClasses));      // loop until classes don't change anymore
    return centers;
}
