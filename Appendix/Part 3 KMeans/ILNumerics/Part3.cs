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

/** 
 * This experiment compares the execution speed of three technologies running equivalent code (kmeans).
 * It performs and manages all measurements and plotting from within a single file (this file). 
 * The experiment first runs all measurements, one after another. Therefore, it expects that 
 * all dependencies and related software is installed on the execution computer. Consult the file 
 * 'SystemSetup.txt' for instructions on prerequisites. 
 * 
 **/ 
//ILN(enabled=false)

const string OutputFolderName = "out";
const int MeasureTimeSpanSeconds = 15;
const int MeasureRepetitionsAvg = 3;
const string MainMeasurementTimesFileName = "values.csv";  // format: MeasurementID (int), SampleReadTime (double), SampleValue (double)

[DllImport("PART3_KMEANS", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SKMEANS")]
static unsafe extern void SKMEANS(double* X, ref long M, ref long N, ref int IT, ref int K, double* classes); 


string A_size = "[1203 x 1203]"; 

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

unsafe RetArray<double> getMeasurementResults() {
    var filename = r2aPQ(Path.Combine(OutputFolderName, MainMeasurementTimesFileName), quote: false);
    try {

        var valuesStr = File.ReadAllText(filename);
        Array<double> allVals = csvread<double>(valuesStr);
        return allVals; 

    } catch { }

    // no file or no/invalid content -> measure!
    if (File.Exists(filename)) File.Delete(filename);
    Directory.CreateDirectory(OutputFolderName); 
    using (File.Create(filename)) { } 

    Array<double> A = createData();
    A_size = A.S.ToString(); 

    int iterations = -1, checkIterations = -1;
    Array<long> classes = -1, checkClasses = -1;

    measureILN(filename, () => Part3_noopt.kmeans(A, k: 50, classes, out iterations), 0.0, ref iterations, rep: MeasureRepetitionsAvg);
    
    measureILN(filename, () => Part3_optimized.kmeans(A, k: 50, checkClasses, out checkIterations), 1.0, ref checkIterations, rep: MeasureRepetitionsAvg);
    if ((bool)anyall(classes != checkClasses) || iterations != checkIterations) throw new ArgumentException("Measurement error: ILN opt yields invalid values.");

    // measure numpy noopt
    var sourcefile = r2aPQ(@"..\..\..\..\numpy\Part3_noopt\Part3_noopt.py", true);  // scriptfile
    var cmdargs = r2aPQ(OutputFolderName, true);                         // working dir (for reading inputs & writing classes values)
    cmdargs += $"50 2 ";                                              // k  measurementID
    cmdargs += $"{MeasureRepetitionsAvg} {MeasureTimeSpanSeconds} ";  // rep maxsec
    cmdargs += r2aPQ(filename, true);                                 // filename to write measured times (out/ append)

    checkClasses.a = -1; checkIterations = -1;
    measureCmd(2.0, A, "python", sourcefile + " " + cmdargs, checkClasses, out checkIterations);
    if ((bool)anyall(classes != checkClasses) || iterations != checkIterations) throw new ArgumentException("Measurement error: numpy noopt yields invalid values.");

    //// measure numpy opt (numba) --DISABLED: too many issues for compiling with NUMBA
    //sourcefile = r2aPQ(@"..\..\..\..\numpy\Part3_opt\Part3_opt.py", true);  // scriptfile
    //checkClasses.a = -1; checkIterations = -1;
    //measureCmd(2.0, A, "python", sourcefile + " " + cmdargs, checkClasses, out checkIterations, rep: MeasureRepetitionsAvg);
    //if ((bool)anyall(classes != checkClasses) || iterations != checkIterations) throw new ArgumentException("Measurement error: numpy opt yields invalid values.");

    long m = A.S[0], n = A.S[1]; 
    int k = 50;
    checkIterations = -1; 
    var fort_action = () => SKMEANS(
                            (double*)A.GetHostPointerForRead(order: StorageOrders.ColumnMajor),
                            ref m, ref n, ref checkIterations, ref k, 
                            (double*)checkClasses.GetHostPointerForWrite(order: StorageOrders.ColumnMajor));
    ensureFORTRANLibsInWorkingDir(); 
    measureILN(filename, fort_action, 3.0, ref checkIterations, rep: MeasureRepetitionsAvg);
    if ((bool)anyall(classes != checkClasses - 1)) throw new ArgumentException($"Measurement error: FORTRAN opt yields invalid classes: {checkClasses}.");
    if (iterations != checkIterations) throw new ArgumentException($"Measurement error: FORTRAN opt yields invalid iteration count: {checkIterations}.");
    
    return getMeasurementResults();
}

// makes sure that all pre-compiled FORTRAN libraries / assoc. files are available in the apps execution dir
void ensureFORTRANLibsInWorkingDir() {
#if DEBUG
    var configuration = "Debug"; 
#else 
    var configuration = "Release";
#endif
    var rel = $@"..\..\..\..\FORTRAN\Part3_KMeans\x64\{configuration}\";
    var files = Directory.GetFiles(rel, "Part3_KMeans.*"); 
    foreach (var file in files) {
        var dest = Path.GetFileName(file);
        if (!File.Exists(dest) || File.GetLastWriteTime(dest) < File.GetLastWriteTime(file)) {
            File.Copy(file, dest, true);
            Console.WriteLine($"FORTRAN file copied: {file}"); 
        }
    }
}

string r2aPQ(string rel, bool quote = false) {
    var ret = Path.GetFullPath(rel);
    if (quote) return $"\"{ret}\" ";
    return ret;  
}

void measureCmd(double measureID, InArray<double> X, string cmd, string cmd_args, OutArray<long> classes, out int iterations) {

    using var _ = Scope.Enter();
    
    // ensure input data
    csvwrite(X, Path.Combine(OutputFolderName, "input.csv"));
    iterations = -1; 

    var proc = Process.Start(cmd, cmd_args);
    proc.WaitForExit(); 
    iterations = proc.ExitCode; 

    classes.a = csvread<long>(File.ReadAllText(Path.Combine(OutputFolderName, "classes.csv")));

}

void measureILN(string filename, Action action, double measureID, ref int iterations, int rep = 5) {
    var sw = new Stopwatch();

    for (var start = DateTime.Now; DateTime.Now < start + TimeSpan.FromSeconds(MeasureTimeSpanSeconds);) {
        sw.Restart();
        for (int i = 0; i < rep; i++) {
            action();
        }
        sw.Stop();
        var now = (DateTime.Now - start).TotalMilliseconds; 
        Console.WriteLine($"Part3. ID: {measureID}, {now}, {sw.ElapsedMilliseconds / (double)rep}  iterations: {iterations}");
        File.AppendAllText(filename, $"{measureID}, {now}, {sw.ElapsedMilliseconds / (double)rep}\r\n");
    }
}

void measureFORTRAN(string filename, Action action, double measureID, int rep = 5) {
    var sw = new Stopwatch();

    for (var start = DateTime.Now; DateTime.Now < start + TimeSpan.FromSeconds(MeasureTimeSpanSeconds);) {
        sw.Restart();
        for (int i = 0; i < rep; i++) {
            action(); 
        }
        sw.Stop();
        var now = (DateTime.Now - start).TotalMilliseconds;
        Console.WriteLine($"Part3. ID: {measureID}, {now}, {sw.ElapsedMilliseconds / (double)rep}");
        File.AppendAllText(filename, $"{measureID}, {now}, {sw.ElapsedMilliseconds / (double)rep}\r\n");
    }
}

Array<double> times = getMeasurementResults();
// increase the font size for plotting
var allLabelFonts = new Font("Linux Libertine", 13.0f);
Label.DefaultFont = allLabelFonts;

var scene = new Scene {
    new PlotCube(twoDMode: true) {
        new LinePlot(tosingle(times[times[full, 0] == 0, "1,2"]).T, tag: "ILNumerics  no  optimization ", lineWidth: 3, markerStyle: MarkerStyle.Dot, lineColor: Color.Black, lineStyle: DashStyle.Dashed),
        new LinePlot(tosingle(times[times[full, 0] == 2, "1,2"]).T, tag: "numpy  no  optimization ", lineWidth: 3, markerStyle: MarkerStyle.TriangleUp, lineColor: Color.Green, lineStyle: DashStyle.Solid),
        new LinePlot(tosingle(times[times[full, 0] == 3, "1,2"]).T, tag: "FORTRAN /fast optimization ", lineWidth: 3, markerStyle: MarkerStyle.Square, lineColor: Color.Blue, lineStyle: DashStyle.Solid),
        new LinePlot(tosingle(times[times[full, 0] == 1, "1,2"]).T, tag: "ILNumerics  VP  optimization ", lineWidth: 3, markerStyle: MarkerStyle.Dot, lineColor: Color.Red, lineStyle: DashStyle.Solid),
        new Legend() {
            Location = new PointF(.9f, .46f), Anchor = new PointF(1, .5f)
        }
    }
};
var pc = scene.First<PlotCube>();

// set all fonts
pc.Axes.XAxis.Label.Font = allLabelFonts;
pc.Axes.XAxis.Ticks.DefaultLabel.Font = allLabelFonts;
pc.Axes.YAxis.Label.Font = allLabelFonts;
pc.Axes.YAxis.Ticks.DefaultLabel.Font = allLabelFonts;

// configure X axis
pc.Axes.XAxis.Label.Text = "Elapsed time [s]";
pc.Axes.XAxis.LabelPosition = new Vector3(.5, .9, 0);
pc.Axes.XAxis.LabelAnchor = new PointF(.5f, 1f);
pc.Axes.XAxis.Ticks.Clear();
pc.Axes.XAxis.Ticks.DefaultLabel.Anchor = new PointF(.5f, 0f);

var minSec = (int)(minall(times[full, 1]) / 1000);
var maxSec = (int)(maxall(times[full, 1]) / 1000);
pc.Axes.XAxis.Ticks.Mode = TickMode.Manual;
for (var i = minSec; i <= maxSec + 1; i++) {
    var ltext = (i < 7 || i > 10) ? i.ToString() : "";
    pc.Axes.XAxis.Ticks.Add(i * 1000, ltext);
}

// configure Y axis
pc.ScaleModes.YAxisScale = AxisScale.Logarithmic;
pc.Axes.YAxis.Label.Text = "Exec time [ms]";
pc.Axes.YAxis.LabelPosition = new Vector3(.63, .3, 0);
pc.Axes.YAxis.LabelAnchor = new PointF(.5f, 1f);
pc.Axes.YAxis.LabelRotation = pif / 2f * 3;
//pc.Axes.YAxis.Ticks.Mode = TickMode.Manual;
//pc.Axes.YAxis.Ticks.Add(100, "10^2");
//pc.Axes.YAxis.Ticks.Add(1000, "10^3");

// increase markers size
foreach (var lineplot in pc.Find<LinePlot>()) {
    lineplot.Marker.Size = 13;  
}
var sysInfo = @$"\fontsize{{-3}}System: {Settings.WMIInfo.RamInstalledBytes / (1 << 30)} GB RAM, {Settings.WMIInfo.Processors[0].NumberOfCores}  cores
{Settings.WMIInfo.Processors[0].Name}"; 

var title = pc.Add(new Title(
@$"\bfExecution speed: KMeans clustering\reset
Data:  {A_size} double
" + sysInfo));
title.Location = new PointF(.13f, .46f);
title.Anchor = new PointF(0, 0.5f);
//title.Label.Font = new Font(title.Label.Font.FontFamily, 14f);
pc.Limits.Update(new Vector3(0, 3.1f, 0), 2);

scene.Configure();
var gdi = new GDIDriver(1500, 1000);
gdi.Scene = scene;
gdi.Render();
gdi.BackBuffer.SaveBitmap(Path.Combine(OutputFolderName, $"Part3.bmp"));

using var outStream = new FileStream(Path.Combine(OutputFolderName, $"Part3.svg"), FileMode.Create);
new SVGDriver(outStream, 1500, 1000, scene: scene).Render();

