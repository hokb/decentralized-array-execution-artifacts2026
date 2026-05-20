using ILNumerics;
using static ILNumerics.ILMath;
using static ILNumerics.Globals;
using System.Diagnostics;
using ILNumerics.Drawing.Plotting;
using ILNumerics.Drawing;
using ILNumerics.IO.HDF5; 
using System.Drawing.Imaging;
using System.Drawing;
using ILNumerics.SC25.Appendix;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.JavaScript.JSType;



/** 
 * This experiment compares the execution speed of three technologies running equivalent code (kmeans).
 * It performs and manages all measurements and plotting from within a single file (this file). 
 * The experiment first runs all measurements, one after another. Therefore, it expects that 
 * all dependencies and related software is installed on the execution computer. Consult the file 
 * 'SystemSetup.txt' for instructions on prerequisites. 
 * 
 **/ 

const string H5ValuesFileName = "part3_all_measurements.h5"; 
const string ValuesFolderName = "values";
const string H5ILN_noopt_name = "H5ILN_noopt"; 
const string H5ILN_opt_name = "H5ILN_opt";
const string H5numpy_noopt_name = "H5numpy_noopt";
const string H5numpy_numba_name = "H5numpy_numba";
const string H5numpy_skl_name = "H5numpy_sciekit";
const string H5FORTRAN_opt_name = "H5FORTRAN_opt";

// run measurement, display + save times & check result
System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

//// some helpful data types: a measurement defines all relevant information required to run a measurement experiment on a certain technology with certain parameters
//class Measurement {

//    public string Name { get; set; }
//    public string OutputFilePath { get; set; }
//    public string MyProperty { get; set; }

//}

RetArray<double> createData() {

    // Just some arbitrary data: [1203 x 1203]. We scale realistic terrain data with a sine function
    // to ensure input values, leading to a certain number of iterations reliably. Otherwise, we could 
    // not safely compare an iterative algorithm like kmeans, since intermediate values might differ
    // due to optimization. This could lead to different number of kmeans iterations until convergence
    // which would make the performance comparison much less useful. 
    return todouble(repmat(SpecialData.terrain, 3, 3)) * sin(arange<double>(1, 401 * 3) * 0.1);

}

RetCell getMeasurementResults() {
    var filename = Path.Combine(ValuesFolderName, H5ValuesFileName); 
    try {
        using (var file = new H5File(filename)) {
            Cell ret = cell(size(6, 1), [
                file.Get<H5Dataset>(H5ILN_noopt_name).Get<double>(),
                file.Get<H5Dataset>(H5ILN_opt_name).Get<double>(),
                file.Get<H5Dataset>(H5numpy_noopt_name).Get<double>(),
                file.Get<H5Dataset>(H5numpy_numba_name).Get<double>(),
                file.Get<H5Dataset>(H5numpy_skl_name).Get<double>(),
                file.Get<H5Dataset>(H5FORTRAN_opt_name).Get<double>()
            ]);  
            return ret;
        }
    } catch {
        // no file or no/invalid content: measure!
        if (File.Exists(filename)) File.Delete(filename); 
    }
    using (var file = new H5File(H5ValuesFileName)) {
        Array<double> data = 
        return data;
    }

}

if (!File.Exists(Path.Combine(ValuesFolderName, H5ValuesFileName)) {

    Array<double> A = createData(); 
    Array<long> classes_noOpt = 1;
    Array<long> classes_opt = 1;

    int iterations_opt = 0, iterations_noOpt = 0;
    const int rep = 1; 

    var sw = new Stopwatch();
    for (var start = DateTime.Now; DateTime.Now < start + TimeSpan.FromSeconds(20);) {
        sw.Restart();
        for (int i = 0; i < rep; i++) {
            Part3_noopt.kmeans(A, k: 50, classes_noOpt, out iterations_noOpt);
        }
        sw.Stop();
        Console.WriteLine($"Part 3 no opt: " + sw.ElapsedMilliseconds);
        File.AppendAllText("values.csv", $"0, {(DateTime.Now - start).TotalMilliseconds}, {sw.ElapsedMilliseconds / (double)rep}\r\n");
    }
    for (var start = DateTime.Now; DateTime.Now < start + TimeSpan.FromSeconds(20);) {
        sw.Restart();
        for (int i = 0; i < rep; i++) {
            Part3_optimized.kmeans(A, k: 50, classes_opt, out iterations_opt);
        }
        sw.Stop();
        Console.WriteLine($"Part 3 optimized: " + sw.ElapsedMilliseconds);
        File.AppendAllText("values.csv", $"1, {(DateTime.Now - start).TotalMilliseconds}, {sw.ElapsedMilliseconds / (double)rep}\r\n");
    }
    if ((bool)anyall(classes_opt != classes_noOpt) || iterations_noOpt != iterations_opt) {
        throw new InvalidProgramException("Optimized version differs from non-optimized version!"); 
    }
}
Array<double> times = csvread<double>(File.ReadAllText("values.csv"));

ILNumerics.Drawing.Label.DefaultFont = new Font("Roboto", 14);
var scene = new Scene {
    new PlotCube(twoDMode: true) {
        new LinePlot(tosingle(times[times[full, 0] == 0, "1,2"]).T, tag: "KMeans  no optimization ", lineWidth: 3, markerStyle: MarkerStyle.TriangleUp, lineColor: Color.Black, lineStyle: DashStyle.Solid),
        new LinePlot(tosingle(times[times[full, 0] == 1, "1,2"]).T, tag: "KMeans  VP  optimization ", lineWidth: 3, markerStyle: MarkerStyle.Dot, lineColor: Color.Black, lineStyle: DashStyle.Solid),
        new Legend() {
            Location = new PointF(.9f, .35f), Anchor = new PointF(1, .5f)
        }
    }
};
var pc = scene.First<PlotCube>();
pc.ScaleModes.YAxisScale = AxisScale.Logarithmic;
pc.Axes.XAxis.Label.Text = "Time  since  app start [s]";
pc.Axes.YAxis.Label.Text = "Execution  time  per  kmeans  invocation  [ms]";
pc.Axes.XAxis.Ticks.Clear();
pc.Axes.XAxis.Ticks.DefaultLabel.Anchor = new PointF(.5f, 0f);

for (var i = 1; i < 21; i++) 
pc.Axes.XAxis.Ticks.Add(i * 1000, i.ToString());
pc.Axes.XAxis.Ticks.Mode = TickMode.Manual;

var title = pc.Add(new Title(
@$"\bfPart 3:  Comparing  the  speed  of  sequential  kmeans  
with  ILNumerics  VP optimizations
\reset

\bfSystem:\reset {Settings.WMIInfo.Processors[0].Name},  {Settings.WMIInfo.Processors[0].MaxClockSpeed / 1000.0} GHz, {Settings.WMIInfo.Processors[0].NumberOfCores}  cores
{Settings.WMIInfo.RamInstalledBytes / (1 << 30)} GB RAM,  {Environment.OSVersion},  {RuntimeInformation.FrameworkDescription}
A: {A.S.ToString()}  double"));  
title.Location = new PointF(.16f, .35f);
title.Anchor = new PointF(0, 0.5f);
title.Label.Font = new Font(title.Label.Font.FontFamily, 14f);

var pbILabel = scene.First<Label>(predicate: l => l.Text.Length > 20);
pbILabel.Position = new Vector3(0.9, 0.9, 0);

scene.Configure();
var gdi = new GDIDriver(1500, 1000);
gdi.Scene = scene;
gdi.Render();
gdi.BackBuffer.Bitmap.Save($"Part3.jpg", ImageFormat.Jpeg);

using var outStream = new FileStream($"Part3.svg", FileMode.Create);
new SVGDriver(outStream, 1500, 1000, scene: scene).Render();

