using ILNumerics;
using ILNumerics.Drawing;
using ILNumerics.Drawing.Plotting;
using ILNumerics.IO.HDF5;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using static ILNumerics.Globals;
using static ILNumerics.ILMath;

const string h5Name = "Part1_all_measurements.h5";
const string numpySourceNoOptPath = @"..\..\..\..\numpy\Part1_noopt.py";
const string numpySourceOptPath = @"..\..\..\..\numpy\Part1_opt.py";
const string FORTRANSource_NoOptPath = @"..\..\..\..\FORTRAN\Part1_F90_nooptim.exe";
const string FORTRANSource_OptPath = @"..\..\..\..\FORTRAN\Part1_F90_fast.exe";
const string numpyValuesNoOptPath = @"values_numpy_noopt.txt";
const string numpyValuesOptPath = @"values_numpy_opt.txt";
const string FORTRANValues_NoOptPath = @"values_fortran_noopt.txt";
const string FORTRANValues_OptPath = @"values_fortran_opt.txt";

const uint m0 = 1142251548;      // some variables to be used in our expression measurements
const int shift = 17;            

if (!File.Exists(h5Name)) {
    performMeasurements();
}

draw(h5Name);

static void performMeasurements() {

    using var file = new H5File(h5Name);

    checkSources();

    // measure numpy + FORTRAN ( overwrites existing measurements)
    MeasureSingle("python", numpySourceNoOptPath, numpyValuesNoOptPath, file, "numpy_noopt");
    MeasureSingle("python", numpySourceOptPath, numpyValuesOptPath, file, "numpy_opt");
    MeasureSingle(FORTRANSource_NoOptPath, "", FORTRANValues_NoOptPath, file, "FORTRAN_noopt", "      ");
    MeasureSingle(FORTRANSource_OptPath, "", FORTRANValues_OptPath, file, "FORTRAN_opt", "      ");

    // measure ILNumerics w/o Accelerator
    Settings.ArrayStyle = ArrayStyles.numpy;
    var times = new List<Tuple<TimeSpan, double>>();

    Array<uint> Result = measureILN_noOpt(times);
    fileIt(times, file, "ILN_noAcc");

    Array<uint> ResultOpt = measureILN_Opt(times);
    if (anyall(ResultOpt != Result)) throw new InvalidProgramException("Optimized program yields invalid results.");
    fileIt(times, file, "ILN_Acc");

    Array<uint> ResultOptExp = measureILN_OptExp(times);
    if (anyall(ResultOptExp != Result)) throw new InvalidProgramException("Optimized program yields invalid results.");
    fileIt(times, file, "ILN_AccBSDAll");

    Console.WriteLine("Segment invocations count: " + Segment.RunCount);

}
static RetArray<uint> measureILN_noOpt(List<Tuple<TimeSpan, double>> times) {

    //ILN(enabled = false)          // this disables the ILNumerics Accelerator (VP compiler)

    Array<uint> A = counter<uint>(1507, 200, 507, 10, 5, 17);    // creates [507, 10, 5, 17] array. 
    Array<uint> B = counter<uint>(1507, 1, 1, 1, 5, 17);  // creates [1, 1, 5, 17] array.  

    Array<uint> result = 1;          // A local variable will be used for testing correctness.

    for (var startTime = DateTime.Now; DateTime.Now < startTime.AddSeconds(10);) {

        var start = Stopwatch.StartNew();

        for (int i = 0; i < 1000; ++i) {
            result.a = sum((m0 & (A << shift)) | (~m0 & B), dim: 1);    // Original expression
        }

        var t = Tuple.Create(DateTime.Now - startTime, (double)start.ElapsedTicks / Stopwatch.Frequency); // gives ms
        times.Add(t);
        System.Console.WriteLine($"Avg time msec (original): {t:F8} ms. Result: " + result.GetValue(0));
    }
    return result;
}
static RetArray<uint> measureILN_Opt(List<Tuple<TimeSpan, double>> times) {

    //ILN(enabled = true)           // enables the ILNumerics Accelerator (VP compiler)
    Settings.ArrayStyle = ArrayStyles.numpy;
    Array<uint> A = counter<uint>(1507, 200, 507, 10, 5, 17);    // creates [507, 10, 5, 17] array. 
    Array<uint> B = counter<uint>(1507, 1, 1, 1, 5, 17);  // creates [1, 1, 5, 17] array.  

    Array<uint> result = 1;
    times.Clear();

    for (var startTime = DateTime.Now; DateTime.Now < startTime.AddSeconds(10);) {

        var start = Stopwatch.StartNew();

        for (int i = 0; i < 1000; ++i) {
            result.a = sum((m0 & (A << shift)) | (~m0 & B), dim: 1);    // expression will be optimized
        }
        result.Finish();            // <-- only required for timing measurements!! This actually slows down
                                    // computations and should not be used in production code, unless required! 

        var t = Tuple.Create(DateTime.Now - startTime, (double)start.ElapsedTicks / Stopwatch.Frequency); // gives ms
        times.Add(t);
        System.Console.WriteLine($"Avg time msec (optimized): {t:F8} ms. result: " + result.GetValue(0));
    }
    return result;
}
static RetArray<uint> measureILN_OptExp(List<Tuple<TimeSpan, double>> times) {

    //ILN(enabled = true)           // enables the ILNumerics Accelerator (VP compiler)
    Segment.Default.SpecializeFlags = ILNumerics.Core.Segments.SpecializeFlags.BSDsAll; // <- experimental optimization feature

    Settings.ArrayStyle = ArrayStyles.numpy;
    Array<uint> A = counter<uint>(1507, 200, 507, 10, 5, 17);    // creates [507, 10, 5, 17] array. 
    Array<uint> B = counter<uint>(1507, 1, 1, 1, 5, 17);  // creates [1, 1, 5, 17] array.  

    Array<uint> result = 1;
    times.Clear();

    for (var startTime = DateTime.Now; DateTime.Now < startTime.AddSeconds(10);) {

        var start = Stopwatch.StartNew();

        for (int i = 0; i < 1000; ++i) {
            result.a = sum((m0 & (A << shift)) | (~m0 & B), dim: 1);    // expression will be optimized more aggressively
        }
        result.Finish();

        var t = Tuple.Create(DateTime.Now - startTime, (double)start.ElapsedTicks / Stopwatch.Frequency); // gives ms
        times.Add(t);
        System.Console.WriteLine($"Avg time msec (optimized++): {t:F8} ms. result: " + result.GetValue(0));
    }
    return result;
}

//check to ensure that all experiment sources / binaries are availaible,throw otherwise
static void checkSources() {
    if (!File.Exists(numpySourceOptPath)) throw new ArgumentException($"NumPy source file '{numpySourceNoOptPath}' not found."); 
    if (!File.Exists(numpySourceNoOptPath)) throw new ArgumentException($"NumPy source file '{numpySourceNoOptPath}' not found.");
    if (!File.Exists(FORTRANSource_NoOptPath)) throw new ArgumentException($"FORTRAN executable '{FORTRANSource_NoOptPath}' not found.");
    if (!File.Exists(FORTRANSource_OptPath)) throw new ArgumentException($"FORTRAN executable '{FORTRANSource_OptPath}' not found.");
}

static void fileIt(List<Tuple<TimeSpan, double>> ongoing, H5File file, string dsname) {
    using var s = Scope.Enter(ArrayStyles.ILNumericsV4);

    Array<double> m = empty<double>(ongoing.Count, 2); 
    m[":", 0] = (Array<double>)ongoing.Select(i => i.Item1.TotalMilliseconds).ToArray();
    m[":", 1] = (Array<double>)ongoing.Select(i => (double)i.Item2).ToArray();
    file.Add(new H5Dataset(dsname, m));
}

void draw(string h5filename) {

    using var h5file = new H5File(h5filename); 
    Array<double> ILN_noAcc = h5file.Get<H5Dataset>("ILN_noAcc").Get<double>(); 
    Array<double> ILN_Acc = h5file.Get<H5Dataset>("ILN_Acc").Get<double>();
    Array<double> ILN_AccBSDAll = h5file.Get<H5Dataset>("ILN_AccBSDAll").Get<double>();
    Array<double> Numpy = h5file.Get<H5Dataset>("numpy_noopt").Get<double>();
    Array<double> Numpy_numba = h5file.Get<H5Dataset>("numpy_opt").Get<double>();
    Array<double> fortran = h5file.Get<H5Dataset>("FORTRAN_noopt").Get<double>(); 
    Array<double> fortran_opt = h5file.Get<H5Dataset>("FORTRAN_opt").Get<double>();

    var allLabelFonts = new Font("Linux Libertine", 18.0f);
    Label.DefaultFont = allLabelFonts;
    
    var scene = new Scene {
        new PlotCube(twoDMode: true) {
            new LinePlot(tosingle(Numpy).T, tag: "Numpy 2.0.2", lineWidth: 3, markerStyle: MarkerStyle.Dot, lineColor: Color.Green, lineStyle: DashStyle.Dotted),
            new LinePlot(tosingle(fortran).T, tag: "FORTRAN ifx /Od", lineWidth: 3, markerStyle: MarkerStyle.TriangleUp, lineColor: Color.Blue, lineStyle: DashStyle.Dotted, markerColor: Color.Red),
            new LinePlot(tosingle(Numpy_numba).T, tag: "Numpy 2.0.2, numba 0.60.0, ", lineWidth: 3, markerStyle: MarkerStyle.Dot, lineColor: Color.Green, lineStyle: DashStyle.Solid),
            new LinePlot(tosingle(ILN_noAcc).T, tag: "ILNumerics, no optimization", lineWidth: 3, markerStyle: MarkerStyle.Square, lineColor: Color.Black, lineStyle: DashStyle.Dotted),
            new LinePlot(tosingle(fortran_opt).T[full,r(0,2,end)], tag: "FORTRAN ifx /fast", lineWidth: 3, markerStyle: MarkerStyle.TriangleUp, lineColor: Color.Blue, lineStyle: DashStyle.Solid, markerColor: Color.Red),
            new LinePlot(tosingle(ILN_Acc).T[full,r(0,2,end)], tag: "ILNumerics, optimized", lineWidth: 3, markerStyle: MarkerStyle.Square, lineColor: Color.Red, lineStyle: DashStyle.Solid),
            new LinePlot(tosingle(ILN_AccBSDAll).T, tag: "ILNumerics, V7, optimized exp.", lineWidth: 3, markerStyle: MarkerStyle.None, lineColor: Color.Black, lineStyle: DashStyle.Solid),
            new Group(scale: new Vector3(0.8, 0.8, 1)) {
                new Legend() { Location = new PointF(.463f, .67f), Anchor = new PointF(1, .5f), Alpha = 0.9f }   // was: 0.9, .32
            }
        }
    };
    var pc = scene.First<PlotCube>();
    
    // increase markers size
    foreach (var lineplot in pc.Find<LinePlot>()) {
        lineplot.Marker.Size = 13;
    }
    pc.ScaleModes.YAxisScale = AxisScale.Logarithmic;
    pc.Axes.XAxis.Label.Text = "Elapsed time [s]";
    pc.Axes.XAxis.LabelPosition = new Vector3(.5, .9, 0);
    pc.Axes.XAxis.LabelAnchor = new PointF(.5f, 1f); 
    //pc.Axes.XAxis.Label.Font = new Font("Linux Libertine", 20.0f);

    pc.Axes.YAxis.Label.Text = "Exec time [ms]";
    pc.Axes.YAxis.LabelPosition = new Vector3(.63, .3, 0);
    pc.Axes.YAxis.LabelAnchor = new PointF(.5f, 1f);
    pc.Axes.YAxis.LabelRotation = pif / 2f * 3;

    pc.Axes.XAxis.Ticks.Clear();
    pc.Axes.XAxis.Ticks.DefaultLabel.Anchor = new PointF(.5f, 0f);
    //pc.Axes.XAxis.Ticks.DefaultLabel.Font = new Font("Arial", 15f, FontStyle.Bold);
    pc.Axes.XAxis.Ticks.Add(0, "0");
    pc.Axes.XAxis.Ticks.Add(2000, "2");
    pc.Axes.XAxis.Ticks.Add(4000, "4");
    pc.Axes.XAxis.Ticks.Add(6000, ""); // room for the axis label
    pc.Axes.XAxis.Ticks.Add(8000, "8");
    pc.Axes.XAxis.Ticks.Add(10000, "10");
    pc.Axes.XAxis.Ticks.Mode = TickMode.Manual;

    var title = pc.Add(new Title(
@$"\bfLow  Level  Expressions\reset  (CPU)
Data:  [507, 10, 5, 17] uint32
\fontsize{{-3}}System: {Settings.WMIInfo.RamInstalledBytes / (1 << 30)} GB RAM, {Settings.WMIInfo.Processors[0].NumberOfCores}  cores
{Settings.WMIInfo.Processors[0].Name}"));
    title.Anchor = new PointF(1, 0.5f); 
    title.Location = new PointF(.9f, .43f);
    //title.Label.Font = new Font(title.Label.Font.FontFamily, 17f);
     
    var pbILabel = scene.First<Label>(predicate: l => l.Text.Length > 20);
    pbILabel.Position = new Vector3(0.9, 0.88, 0); 

    scene.Configure();
    var gdi = new GDIDriver(1500, 1000);
    gdi.Scene = scene;
    gdi.Render();
    gdi.BackBuffer.Bitmap.Save("Part1.jpg", ImageFormat.Jpeg);

    using var outStream = new FileStream("Part1.svg", FileMode.Create); 
    new SVGDriver(outStream, 1500, 1000, scene: scene).Render(); 

}

static void MeasureSingle(string progName, string argument, string values_file, H5File file, string dsname, string separator = ",") {
    // measure
    Process.Start(progName, argument).WaitForExit();

    Array<double> values = csvread<double>(File.ReadAllText(values_file), elementSeparator : separator);
    file.Add(new H5Dataset(dsname, values));
}