using static ILNumerics.Globals;
using static ILNumerics.ILMath;

//ILN(enabled=false)     <- disables VP acceleration in Release mode
namespace ILNumerics.SC25.Appendix;

public class Part3_noopt {
    public static RetArray<double> kmeans(InArray<double> A, int k, OutArray<long> classes, out int iterations) {

        using var _ = Scope.Enter();

        Array<long> pickIds = linspace<long>(0, (int)A.S[1] - 2, k); 
        Array<double> centers = A[full, pickIds];
        classes.a = zeros<long>(1, A.S[1]); 
        Array<long> oldClasses = 0;
        iterations = 0;
        // main iterations loop. Repeats both k-means phases: 
        do {
            oldClasses.a = classes;   // exit condition              

            // phase 1) assign samples to clusters
            for (int i = 0; i < A.S[1]; i++) {

                Array<long> C = -1;
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

        } while (any(classes != oldClasses));      // loop until classes don't change anymore
        return centers;
    }
}
