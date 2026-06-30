using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using ISO11820.Core.Models;

namespace ISO11820.App.UI.Chart;

/// <summary>
/// OxyPlot 温度曲线图面板，显示 4 条实时温度曲线。
/// 滚动窗口 600 秒（10 分钟），最多 750 个数据点。
/// </summary>
public sealed class TemperatureChartPanel : IDisposable
{
    private const int MaxPoints = 750;
    private const int WindowSeconds = 600;

    private readonly PlotView _view;
    private readonly PlotModel _model;
    private readonly LineSeries _furnace1Series;
    private readonly LineSeries _furnace2Series;
    private readonly LineSeries _surfaceSeries;
    private readonly LineSeries _centerSeries;
    private readonly LinearAxis _xAxis;
    private readonly LinearAxis _yAxis;
    private int _paintCount;

    public PlotView View => _view;

    public TemperatureChartPanel()
    {
        _model = new PlotModel
        {
            Title = "温度曲线",
            TitleColor = OxyColor.FromRgb(50, 50, 50),
            PlotAreaBorderColor = OxyColor.FromRgb(180, 180, 180),
            Background = OxyColors.White,
        };

        _xAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "时间 (秒)",
            Minimum = 0,
            Maximum = WindowSeconds,
            MajorStep = 60,
            MinorStep = 10,
        };
        _model.Axes.Add(_xAxis);

        _yAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "温度 (°C)",
            Minimum = 0,
            Maximum = 800,
            MajorStep = 100,
            MinorStep = 50,
        };
        _model.Axes.Add(_yAxis);

        _furnace1Series = CreateSeries("炉温1", OxyColors.Red);
        _furnace2Series = CreateSeries("炉温2", OxyColors.Orange);
        _surfaceSeries = CreateSeries("表面温", OxyColors.Blue);
        _centerSeries = CreateSeries("中心温", OxyColors.Green);

        _model.Series.Add(_furnace1Series);
        _model.Series.Add(_furnace2Series);
        _model.Series.Add(_surfaceSeries);
        _model.Series.Add(_centerSeries);

        _view = new PlotView
        {
            Dock = DockStyle.Fill,
            Model = _model,
            BackColor = Color.White,
        };

        // 诊断：监听 Paint 事件，确认 OnPaint 是否被调用
        _view.Paint += OnViewPaint;

        // 当尺寸从 0x0 变为有效尺寸时，强制重绘图表
        _view.SizeChanged += OnViewSizeChanged;
    }

    private void OnViewPaint(object? sender, PaintEventArgs e)
    {
        _paintCount++;
        try
        {
            var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iso11820_chart_debug.txt");
            System.IO.File.AppendAllText(logPath,
                $"[{DateTime.Now:HH:mm:ss.fff}] PAINT #{_paintCount}: clip={e.ClipRectangle}, series[0].Points={_furnace1Series.Points.Count}\n");
        }
        catch { }
    }

    private void OnViewSizeChanged(object? sender, EventArgs e)
    {
        if (_view.Width > 0 && _view.Height > 0 && _furnace1Series.Points.Count > 0)
        {
            _view.InvalidatePlot(true);
            _view.Refresh();
        }
    }

    private static LineSeries CreateSeries(string title, OxyColor color)
    {
        return new LineSeries
        {
            Title = title,
            Color = color,
            StrokeThickness = 1.5,
            MarkerType = MarkerType.None,
            CanTrackerInterpolatePoints = false,
        };
    }

    /// <summary>
    /// 添加一个新的温度采样点到图表
    /// </summary>
    public void AppendSample(int elapsedSeconds, TemperatureSnapshot temps)
    {
        double x = elapsedSeconds;

        _furnace1Series.Points.Add(new DataPoint(x, temps.Furnace1));
        _furnace2Series.Points.Add(new DataPoint(x, temps.Furnace2));
        _surfaceSeries.Points.Add(new DataPoint(x, temps.Surface));
        _centerSeries.Points.Add(new DataPoint(x, temps.Center));

        // 诊断日志
        try
        {
            var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iso11820_chart_debug.txt");
            System.IO.File.AppendAllText(logPath,
                $"[{DateTime.Now:HH:mm:ss.fff}] AppendSample: x={x}, F1={temps.Furnace1:F1}, F2={temps.Furnace2:F1}, S={temps.Surface:F1}, C={temps.Center:F1}, totalPoints={_furnace1Series.Points.Count}\n");
        }
        catch { }

        // 裁剪超出最大点数的旧数据
        TrimPoints(_furnace1Series);
        TrimPoints(_furnace2Series);
        TrimPoints(_surfaceSeries);
        TrimPoints(_centerSeries);

        // 滚动 X 轴窗口
        if (elapsedSeconds > WindowSeconds)
        {
            _xAxis.Minimum = elapsedSeconds - WindowSeconds;
            _xAxis.Maximum = elapsedSeconds;
        }

        _view.InvalidatePlot(true);
        try
        {
            // 诊断：始终记录 PlotView 的实际状态
            var logPath3 = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iso11820_chart_debug.txt");
            System.IO.File.AppendAllText(logPath3,
                $"[{DateTime.Now:HH:mm:ss.fff}] ViewState: handle={_view.IsHandleCreated}, visible={_view.Visible}, size={_view.Width}x{_view.Height}, paintCount={_paintCount}\n");

            if (_view.IsHandleCreated && _view.Visible && _view.Width > 0 && _view.Height > 0)
            {
                _view.Refresh();
                // 双重保险：如果 Refresh 后 Paint 事件没触发，强制重设 Model
                System.IO.File.AppendAllText(logPath3,
                    $"[{DateTime.Now:HH:mm:ss.fff}] Refresh OK, post-refresh paintCount={_paintCount}\n");
            }
            else
            {
                var parent = _view.Parent;
                var parentSize = parent != null ? $"{parent.Width}x{parent.Height}" : "null";
                var grandParent = parent?.Parent;
                var grandParentSize = grandParent != null ? $"{grandParent.Width}x{grandParent.Height}" : "null";
                System.IO.File.AppendAllText(logPath3,
                    $"[{DateTime.Now:HH:mm:ss.fff}] Refresh SKIPPED: handle={_view.IsHandleCreated}, visible={_view.Visible}, size={_view.Width}x{_view.Height}, parent={parent?.GetType().Name}:{parentSize}, grandParent={grandParent?.GetType().Name}:{grandParentSize}\n");
            }
        }
        catch (Exception ex)
        {
            var logPath2 = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iso11820_chart_debug.txt");
            System.IO.File.AppendAllText(logPath2,
                $"[{DateTime.Now:HH:mm:ss.fff}] Refresh ERROR: {ex.GetType().Name}: {ex.Message}\n");
        }

        // 诊断：每 10 个点导出一次图表图片
        if (_furnace1Series.Points.Count > 0 && _furnace1Series.Points.Count % 10 == 0)
        {
            try
            {
                var imgPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iso11820_chart_export.png");
                var exporter = new PngExporter { Width = 800, Height = 500 };
                exporter.ExportToFile(_model, imgPath);
                var fi = new System.IO.FileInfo(imgPath);
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iso11820_chart_debug.txt"),
                    $"[{DateTime.Now:HH:mm:ss.fff}] Chart exported: {fi.Length} bytes, points={_furnace1Series.Points.Count}\n");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iso11820_chart_debug.txt"),
                    $"[{DateTime.Now:HH:mm:ss.fff}] Chart export ERROR: {ex.Message}\n");
            }
        }
    }

    private static void TrimPoints(LineSeries series)
    {
        while (series.Points.Count > MaxPoints)
        {
            series.Points.RemoveAt(0);
        }
    }

    /// <summary>
    /// 清空图表数据（新建试验时调用）
    /// </summary>
    public void Clear()
    {
        _furnace1Series.Points.Clear();
        _furnace2Series.Points.Clear();
        _surfaceSeries.Points.Clear();
        _centerSeries.Points.Clear();

        _xAxis.Minimum = 0;
        _xAxis.Maximum = WindowSeconds;

        _view.InvalidatePlot(true);
        try
        {
            // 诊断：始终记录 PlotView 的实际状态
            var logPath3 = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iso11820_chart_debug.txt");
            System.IO.File.AppendAllText(logPath3,
                $"[{DateTime.Now:HH:mm:ss.fff}] ViewState: handle={_view.IsHandleCreated}, visible={_view.Visible}, size={_view.Width}x{_view.Height}, paintCount={_paintCount}\n");

            if (_view.IsHandleCreated && _view.Visible && _view.Width > 0 && _view.Height > 0)
            {
                _view.Refresh();
                // 双重保险：如果 Refresh 后 Paint 事件没触发，强制重设 Model
                System.IO.File.AppendAllText(logPath3,
                    $"[{DateTime.Now:HH:mm:ss.fff}] Refresh OK, post-refresh paintCount={_paintCount}\n");
            }
            else
            {
                var parent = _view.Parent;
                var parentSize = parent != null ? $"{parent.Width}x{parent.Height}" : "null";
                var grandParent = parent?.Parent;
                var grandParentSize = grandParent != null ? $"{grandParent.Width}x{grandParent.Height}" : "null";
                System.IO.File.AppendAllText(logPath3,
                    $"[{DateTime.Now:HH:mm:ss.fff}] Refresh SKIPPED: handle={_view.IsHandleCreated}, visible={_view.Visible}, size={_view.Width}x{_view.Height}, parent={parent?.GetType().Name}:{parentSize}, grandParent={grandParent?.GetType().Name}:{grandParentSize}\n");
            }
        }
        catch (Exception ex)
        {
            var logPath2 = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iso11820_chart_debug.txt");
            System.IO.File.AppendAllText(logPath2,
                $"[{DateTime.Now:HH:mm:ss.fff}] Refresh ERROR: {ex.GetType().Name}: {ex.Message}\n");
        }

        // 诊断：每 10 个点导出一次图表图片
        if (_furnace1Series.Points.Count > 0 && _furnace1Series.Points.Count % 10 == 0)
        {
            try
            {
                var imgPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iso11820_chart_export.png");
                var exporter = new PngExporter { Width = 800, Height = 500 };
                exporter.ExportToFile(_model, imgPath);
                var fi = new System.IO.FileInfo(imgPath);
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iso11820_chart_debug.txt"),
                    $"[{DateTime.Now:HH:mm:ss.fff}] Chart exported: {fi.Length} bytes, points={_furnace1Series.Points.Count}\n");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(System.IO.Path.GetTempPath(), "iso11820_chart_debug.txt"),
                    $"[{DateTime.Now:HH:mm:ss.fff}] Chart export ERROR: {ex.Message}\n");
            }
        }
    }

    /// <summary>
    /// 导出图表为 Bitmap 图片，用于 Excel/PDF 嵌入
    /// </summary>
    public System.Drawing.Image? ExportImage(int width = 800, int height = 500)
    {
        try
        {
            var exporter = new PngExporter { Width = width, Height = height };
            var bitmap = exporter.ExportToBitmap(_model);
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        _view.Dispose();
    }
}
