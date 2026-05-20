using ILNumerics;
using static ILNumerics.ILMath;
using static ILNumerics.Globals;
using System.Diagnostics;
using ILNumerics.Drawing.Plotting;
using ILNumerics.Drawing;
using System.Drawing.Imaging;
using System.Drawing;

Array<double> A = counter<double>(1.0, 1.0, 1500, 1500);
Array<double> CS = empty<double>(1, 1500);
Array<double> CV = empty<double>(1, 1500);
Array<double> D = empty<double>(1, 1500);
Array<double> ES = empty<double>(1, 1500);
Array<double> EV = empty<double>(1, 1500);

const string PartName = "Part 2b";

//ILN(enabled = false)
void workload_ILN_Seq() {
    for (int i = 0; i < A.S[1]; i++) {
        CS[r(0,i)] = sum(abs(sin(A[full, i])), dim: 0);
    }
}
void workload_ParFor() {

    Parallel.For(0, A.S[1], i => {
        D[r(0, i)] = sum(abs(sin(A[full, i])), dim: 0);
    });
}
//ILN(enabled = true)
void workload_ILN_Seq_VP() {
    for (int i = 0; i < A.S[1]; i++) {
        ES[r(0, i)] = sum(abs(sin(A[full, i])), dim: 0);
    }
}
//ILN(enabled = false)
double run(Action func, int rep = 10, Func<BaseArray> awaitable = null) {

    var sw = Stopwatch.StartNew();
    for (int i = 0; i < rep; i++) {
        func();
    }
    if (awaitable != null) awaitable().Finish();
    return sw.ElapsedTicks * 1000.0 / Stopwatch.Frequency / rep;
}

// run repetitions, display + file times & check result
System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
if (!File.Exists("values.csv")) {

    for (var start = DateTime.Now; DateTime.Now < start + TimeSpan.FromSeconds(10);) {
        var spent_ILN_seq = run(workload_ILN_Seq);
        var spent_ParFor = run(workload_ParFor);
        var spent_ILN_Seq_VP = run(workload_ILN_Seq_VP, awaitable: () => ES);

        Console.Write($"{PartName} ILNseq: {spent_ILN_seq} ms,");
        Console.Write($"{PartName} Parallel.For: {spent_ParFor} ms"); check((double)maxall(abs(D - CS)));
        Console.Write($"{PartName} VP Seq: {spent_ILN_Seq_VP} ms"); check((double)maxall(abs(ES - CS)));

        File.AppendAllText("values.csv", $"{(DateTime.Now - start).TotalMilliseconds}, {spent_ILN_seq}, {spent_ParFor}, {spent_ILN_Seq_VP}\r\n");
        Console.WriteLine();
    }

}
draw();
void check(double err) {
    if (err < 3 * eps * A.S[0] * maxall(A)) {
        Console.Write("(correct: True) ");
    } else {
        Console.Write("(correct: ");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"False [err: {err}]");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(") ");
    }
}

void draw() {
    Array<double> times = csvread<double>(File.ReadAllText("values.csv"));

    var allLabelFonts = new Font("Linux Libertine", 18.0f);
    Label.DefaultFont = allLabelFonts;

    var scene = new Scene {
        new PlotCube(twoDMode: true) {
            new LinePlot(tosingle(times[full,"0,1"]).T, tag: "ILNumerics  for  loop", lineWidth: 3, markerStyle: MarkerStyle.TriangleDown, lineColor: Color.Black, lineStyle: DashStyle.Dotted),
            new LinePlot(tosingle(times[full,"0,3"]).T, tag: "ILNumerics  for  loop,  VP ", lineWidth: 3, markerStyle: MarkerStyle.Dot, lineColor: Color.Black, lineStyle: DashStyle.Solid),
            new LinePlot(tosingle(times[full,"0,2"]).T, tag: "Parallel.For \\color{red}(invalid)", markerStyle: MarkerStyle.Cross, markerColor: Color.Blue, lineColor: Color.Blue, lineStyle: DashStyle.PointDash),
            new Legend() {
                Location = new PointF(.87f, .3f), Anchor = new PointF(1, .5f)
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

    var minSec = (int)(minall(times[full, 0]) / 1000);
    var maxSec = (int)(maxall(times[full, 0]) / 1000);
    pc.Axes.XAxis.Ticks.Mode = TickMode.Manual;
    for (var i = minSec + 1; i <= maxSec; i++) {
        var ltext = (i < 5 || i > 6) ? i.ToString() : "";
        pc.Axes.XAxis.Ticks.Add(i * 1000, ltext);
    }

    // configure Y axis
    pc.ScaleModes.YAxisScale = AxisScale.Logarithmic;
    pc.Axes.YAxis.Label.Text = "Exec time [ms]";
    pc.Axes.YAxis.LabelPosition = new Vector3(.73, .3, 0);
    pc.Axes.YAxis.LabelAnchor = new PointF(.5f, 1f);
    pc.Axes.YAxis.LabelRotation = pif / 2f * 3;
    //pc.Axes.YAxis.Ticks.Mode = TickMode.Manual;
    //pc.Axes.YAxis.Ticks.Add(100, "10^2");
    //pc.Axes.YAxis.Ticks.Add(1000, "10^3");

    // increase markers size
    foreach (var lineplot in pc.Find<LinePlot>()) {
        if (lineplot.Marker.Style == MarkerStyle.TriangleUp || lineplot.Marker.Style == MarkerStyle.TriangleDown) {
            lineplot.Marker.Size = 18;
        } else {
            lineplot.Marker.Size = 13;
        }
    }

    var sysInfo = @$"\fontsize{{-3}}System: {Settings.WMIInfo.RamInstalledBytes / (1 << 30)} GB RAM, {Settings.WMIInfo.Processors[0].NumberOfCores}  cores
{Settings.WMIInfo.Processors[0].Name}";

    var title = pc.Add(new Title(
@$"\bf\itDependent\reset\bf Loop Iterations\reset
Parallel.For vers. ILNumerics VP
\reset
B[r(0,i)] = sum(abs(sin(A[full,  i])), dim: 0)
" + sysInfo));
    title.Location = new PointF(.2f, .3f);
    title.Anchor = new PointF(0, 0.5f);

    pc.Limits.Update(new Vector3(0, 2.1f, 0), 2);

    var pbILabel = scene.First<Label>(predicate: l => l.Text.Length > 20);
    pbILabel.Position = new Vector3(0.88, 0.87, 0);

    scene.Configure();
    var gdi = new GDIDriver(1500, 1000);
    gdi.Scene = scene;
    gdi.Render();
    gdi.BackBuffer.Bitmap.Save($"{PartName}.jpg", ImageFormat.Jpeg);

    using var outStream = new FileStream($"{PartName}.svg", FileMode.Create);
    new SVGDriver(outStream, 1500, 1000, scene: scene).Render();

}