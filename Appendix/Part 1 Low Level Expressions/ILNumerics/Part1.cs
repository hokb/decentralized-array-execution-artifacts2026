using ILNumerics;
using ILNumerics.Drawing;
using ILNumerics.Drawing.Plotting;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using static ILNumerics.Globals;
using static ILNumerics.ILMath;
//ILN(enabled = false)          // disable the VP Accelerator for now. It will be selectively enabled below.

string measureDir = Path.Combine("..","..","..","..", "result");
var measurements = new Measurement[] { 
    new Measurement{ Name = "NumPy NoOpt", Source =  Path.Combine("..", "..", "..", "..", "numpy","Part1_noopt.py"), TimesPath = "times_numpy_noopt.txt", ResultsPath= "results_numpy_noopt.bin", Separator = "," },
    new Measurement{ Name = "NumPy Opt", Source =  Path.Combine("..", "..", "..", "..", "numpy","Part1_opt.py"), TimesPath = "times_numpy_opt.txt", ResultsPath= "results_numpy_opt.bin", Separator = "," },
    new Measurement{ Name = "FORTRAN NoOpt", Source =  Path.Combine("..", "..", "..", "..","FORTRAN", "Part1_F90_nooptim"), TimesPath = "times_fortran_noopt.txt", ResultsPath= "results_fortran_noopt.bin", Separator = "      " },
    new Measurement{ Name = "FORTRAN Opt", Source =  Path.Combine("..", "..", "..", "..","FORTRAN", "Part1_F90_optim"), TimesPath = "times_fortran_opt.txt", ResultsPath= "results_fortran_opt.bin", Separator = "      " },
    new Measurement{ Name = "ILNumerics NoOpt", Source =  "", TimesPath = "timess_iln_noopt.txt",ResultsPath= "results_iln_noopt.bin", Separator = ","},
    new Measurement{ Name = "ILNumerics Opt", Source =  "", TimesPath = "times_iln_opt.txt", ResultsPath= "results_iln_opt.bin", Separator = ","},
    new Measurement{ Name = "ILNumerics OptExp", Source =  "", TimesPath = "times_iln_optexp.txt", ResultsPath= "results_iln_optexp.bin", Separator = "," }
};

const uint m0 = 1142251548;      // some variables to be used in our expression measurements
const int shift = 17; 
int elemCount = 507 * 5 * 17;

// a cell array holds our measured times and results per experiment
Cell samples = cell(size(2, measurements.Length));
// ensure invariant number format (floating point char: '.') 
System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture; 
// a global variable will be used for checking all experiments calculation results for identity.
Array<uint> result = null; 

if (!readMeasurements(samples)) {
    performMeasurements();
    readMeasurements(samples); 
}
Console.WriteLine($"Current Directory: " + Environment.CurrentDirectory);
Console.WriteLine($"Measure Directory: " + Path.GetFullPath(measureDir));

draw(samples);
 unsafe bool readMeasurements(OutCell all_samples) {

    for (var i = 0; i < measurements.Length; i++) {

        var m = measurements[i];
        var timesPath = Path.Combine(measureDir, m.TimesPath); 
        var resultsPath = Path.Combine(measureDir, m.ResultsPath);

        if (File.Exists(timesPath) && File.Exists(resultsPath)) {
            // read measurement times from CSV
            all_samples[0, i] = csvread<double>(File.ReadAllText(timesPath), elementSeparator: m.Separator);

            #region read and check result(s) 
            // all results are serialized into the results file
            using (var file = File.OpenRead(resultsPath)) {

                Array<uint> res = zeros<uint>(507, 1, 5, 17); 
                while (file.Position < file.Length) {

                    var order = m.Name.ToLower().StartsWith("numpy") ? StorageOrders.RowMajor : StorageOrders.ColumnMajor; 

                    file.Read(new Span<byte>((void*)res.GetHostPointerForWrite(order), elemCount * sizeof(uint))); 

                    if (isnull(result)) {
                        // first value: just assign
                        result = res; 
                    } else {
                        if (result.S != res.S || (bool)anyall(result != res)) {
                            throw new ArgumentException($"Measurement {m.Name} yields invalid result values. The result returned differs from other measurement results."); 
                        }
                    }
                }
                all_samples[1, i] = res; 
            }
            #endregion

        } else { 
            return false;
        }
    }
    return true; 
}
void performMeasurements() {

    // measure numpy + FORTRAN ( overwriting existing measurement result files )
    Settings.ArrayStyle = ArrayStyles.numpy;
    // create output measurement dir, if not exists
    if (!Directory.Exists(measureDir)) {
        Directory.CreateDirectory(measureDir); 
    }

    foreach (var m in measurements) {

        var timesPath = Path.Combine(measureDir, m.TimesPath);
        var resultsPath = Path.Combine(measureDir, m.ResultsPath);

        if (m.Name.ToLower().StartsWith("numpy")) {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
                Process.Start("python3", m.Source + " " + timesPath + " " + resultsPath).WaitForExit();
            } else {
                Process.Start("/opt/venv/bin/python3", m.Source + " " + timesPath + " " + resultsPath).WaitForExit();
            }

        } else if (m.Name.ToLower().StartsWith("fortran")) {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
                Process.Start(m.Source + ".exe", "\"" + timesPath + "\" \"" + resultsPath + "\"").WaitForExit();
            } else {
                Process.Start(m.Source, "\"" + timesPath + "\" \"" + resultsPath + "\"").WaitForExit();
            }
        
        } else {

            measureILN(m); 

        }
    }
    Console.WriteLine("Segment invocations count: " + Segment.RunCount);
    Console.WriteLine("ILNumerics Version: " + FileVersionInfo.GetVersionInfo(typeof(Array<>).Assembly.Location).FileVersion?.ToString());
    
}
static string getSystemInfo() {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
        return @$"{RuntimeInformation.OSDescription}: {Settings.WMIInfo.RamInstalledBytes / (1 << 30)} GB RAM, {Settings.WMIInfo.Processors[0].NumberOfCores}  cores
{Settings.WMIInfo.Processors[0].Name}";
    } else {
        return @$"{RuntimeInformation.OSDescription}: {GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1 << 30)} GB RAM, {Environment.ProcessorCount}  cores
{GetLinuxProcessorName()}";
    }
}
static string GetLinuxProcessorName() {
    return File.ReadLines("/proc/cpuinfo")
        .FirstOrDefault(l => l.StartsWith("model name", StringComparison.OrdinalIgnoreCase))?
        .Split(':', 2)[1]
        .Trim()
        ?? "Unknown CPU";
}

unsafe void measureILN(Measurement m) {

    if (m.Name.EndsWith(" OptExp")) {
        Segment.Default.SpecializeFlags = ILNumerics.Core.Segments.SpecializeFlags.BSDsAll; // <- experimental optimization feature
    } else {
        Segment.Default.SpecializeFlags = ILNumerics.Core.Segments.SpecializeFlags.None; // default for "standard" optimization
    }

    Settings.ArrayStyle = ArrayStyles.numpy;
    Array<uint> A = counter<uint>(1507, 200, 507, 10, 5, 17);    // creates [507, 10, 5, 17] array. 
    Array<uint> B = counter<uint>(1507, 1, 1, 1, 5, 17);  // creates [1, 1, 5, 17] array.  

    Array<uint> result = 1;
    using var resFile = File.OpenWrite(Path.Combine(measureDir, m.ResultsPath)); 
    using var timeFile = File.OpenWrite(Path.Combine(measureDir, m.TimesPath));
    using var tWriter = new StreamWriter(timeFile); 

    for (var startTime = DateTime.Now; DateTime.Now < startTime.AddSeconds(10);) {
        // System.Console.WriteLine($"Test ({m.Name}): {(A << shift).GetValue(0)}, {(m0 & (A << shift)).GetValue(0)}, {(~m0 & B).GetValue(0)}, {((m0 & (A << shift)) | (~m0 & B)).GetValue(0)}  ms. result[0]: " + result.GetValue(0));

        double elapsed, tpoint; 
        var start = Stopwatch.StartNew();

        if (m.Name.EndsWith(" NoOpt")) {
            //ILN(enabled = false)          // redundant with above file scope setting, though
            for (int i = 0; i < 1000; ++i) {
                result.a = sum((m0 & (A << shift)) | (~m0 & B), dim: 1);    // expression will not be accelerated
            }

        } else if (m.Name.EndsWith(" Opt")) {
            //ILN(enabled = true)           // enables the ILNumerics VP compiler: optimize subsequent array expressions.
            for (int i = 0; i < 1000; ++i) {
                result.a = sum((m0 & (A << shift)) | (~m0 & B), dim: 1);    // expression will be accelerated aggressively
            }
            result.Finish();                                                // for fairness: waits for ALL results to finish

        } else if (m.Name.EndsWith(" OptExp")) {
            //ILN(enabled = true)           // enables the ILNumerics VP compiler: optimize subsequent array expressions.
            for (int i = 0; i < 1000; ++i) {
                result.a = sum((m0 & (A << shift)) | (~m0 & B), dim: 1);    // expression will be optimized more aggressively
            }
            result.Finish();                                                // for fairness: waits for ALL results to finish
            //ILN(enabled = false)                                          // disables the VP for the rest of this file

        }
        elapsed = (double)start.ElapsedTicks / Stopwatch.Frequency;   // gives ms
        tpoint = (DateTime.Now - startTime).TotalMilliseconds;
        // write the result's values (serialized) 
        resFile.Write(new ReadOnlySpan<byte>((void*)result.GetHostPointerForRead(StorageOrders.ColumnMajor), (int)result.S.NumberOfElements * sizeof(uint)));
        // write current time [ms] and elapsed time [ms] into csv 
        tWriter.WriteLine(tpoint.ToString("F8") + ", " + elapsed.ToString("F8")); 
        System.Console.WriteLine($"Avg time msec ({m.Name}): {elapsed:F8} ms. result[0]: " + result.GetValue(0));
    }
}
void draw(InCell samples) {

    System.Console.WriteLine($"Size of samples cell: {samples.S.ToString()}"); 
    System.Console.WriteLine($"Samples cell: {samples.ToString()}"); 

    Array<double> Numpy = samples.GetArray<double>(0, 0);
    Array<double> Numpy_numba = samples.GetArray<double>(0, 1);
    Array<double> fortran = samples.GetArray<double>(0, 2);
    Array<double> fortran_opt = samples.GetArray<double>(0, 3);
    Array<double> ILN_noAcc = samples.GetArray<double>(0, 4); 
    Array<double> ILN_Acc = samples.GetArray<double>(0, 5);
    Array<double> ILN_AccBSDAll = samples.GetArray<double>(0, 6);

    var allLabelFonts = new ILNumerics.Drawing.Compat.Font("Linux Libertine", 14.0f);
    Label.DefaultFont = allLabelFonts;
    
    var scene = new Scene {
        new PlotCube(twoDMode: true) {
            new LinePlot(tosingle(Numpy).T, tag: "Numpy 2.0.2", markerStyle: MarkerStyle.Dot, lineColor: Color.Green, lineStyle: DashStyle.Dotted),
            new LinePlot(tosingle(fortran).T, tag: "FORTRAN gfort /Od",  markerStyle: MarkerStyle.TriangleUp, lineColor: Color.Blue, lineStyle: DashStyle.Dotted, markerColor: Color.Red),
            new LinePlot(tosingle(Numpy_numba).T, tag: "Numpy 2.0.2, numba 0.60.0, ", markerStyle: MarkerStyle.Dot, lineColor: Color.Green, lineStyle: DashStyle.Solid),
            new LinePlot(tosingle(ILN_noAcc).T, tag: "ILNumerics, no optimization", markerStyle: MarkerStyle.Square, lineColor: Color.Black, lineStyle: DashStyle.Dotted),
            new LinePlot(tosingle(fortran_opt).T[full,r(0,2,end)], tag: "FORTRAN gfort /fast", markerStyle: MarkerStyle.TriangleUp, lineColor: Color.Blue, lineStyle: DashStyle.Solid, markerColor: Color.Red),
            new LinePlot(tosingle(ILN_Acc).T[full,r(0,2,end)], tag: "ILNumerics, optimized", markerStyle: MarkerStyle.Square, lineColor: Color.Red, lineStyle: DashStyle.Solid),
            new LinePlot(tosingle(ILN_AccBSDAll).T, tag: "ILNumerics, V7, optimized exp.", markerStyle: MarkerStyle.None, lineColor: Color.Black, lineStyle: DashStyle.Solid),
            new Group(scale: new Vector3(0.8, 0.8, 1)) {
                new Legend() { Location = new PointF(.463f, .67f), Anchor = new PointF(1, .5f), Alpha = 0.9f }   // was: 0.9, .32
            }
        }
    };
    var pc = scene.First<PlotCube>();
    
    // increase markers size
    foreach (var lineplot in pc.Find<LinePlot>()) {
        lineplot.Marker.Size = 13;
        lineplot.Line.Width = 3; 
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

    var title = new Title(
@$"\bfLow  Level  Expressions\reset  (CPU)
Data:  [507, 10, 5, 17] uint32
\fontsize{{-3}}System: {getSystemInfo()}");
    title.Anchor = new PointF(1, 0.5f); 
    title.Location = new PointF(.9f, .43f);
    pc.Add(title); 
    //title.Label.Font = new Font(title.Label.Font.FontFamily, 17f);
     
    var pbILabel = scene.First<Label>(predicate: l => l.Text.Length > 20);
    pbILabel.Position = new Vector3(0.9, 0.88, 0);

    //scene = new Scene() { Screen = { title } }; 
    scene.Configure();

    // this will only work on Windows. It has been disabled to allow reproduction on a docker image (based on Debian Linux). 
    var gdi = new GDIDriver(1500, 1000);
    gdi.Scene = scene;
    gdi.Render();
    gdi.BackBuffer.SaveBitmap(Path.Combine(measureDir, "Part1.bmp"));

    using var outStream = new FileStream(Path.Combine(measureDir, "Part1.svg"), FileMode.Create); 
    new SVGDriver(outStream, 1500, 1000, scene: scene).Render(); 

}

struct Measurement {
    public string Name;
    public string Source;
    public string TimesPath;
    public string ResultsPath;
    public string Separator;
    public Action<Measurement> Method;
}
