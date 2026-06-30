using System.Globalization;

namespace ISO11820.App.Features.Export;

/// <summary>
/// CSV 数据行（用于 Excel/PDF 导出）
/// </summary>
public sealed record CsvRow
{
    public int ElapsedSeconds { get; init; }
    public double Furnace1 { get; init; }
    public double Furnace2 { get; init; }
    public double Surface { get; init; }
    public double Center { get; init; }
}

/// <summary>
/// 读取 sensor_data.csv 并解析为 CsvRow 列表
/// </summary>
public sealed class CsvDataReader
{
    /// <summary>
    /// 读取 CSV 文件并返回数据行列表
    /// </summary>
    public List<CsvRow> ReadAll(string csvFilePath)
    {
        if (!File.Exists(csvFilePath))
        {
            return new List<CsvRow>();
        }

        var rows = new List<CsvRow>();
        var lines = File.ReadAllLines(csvFilePath);

        if (lines.Length < 2)
        {
            return rows; // 只有标题或空文件
        }

        // 跳过标题行，从第 1 行开始解析
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            var parts = line.Split(',');
            if (parts.Length < 5)
                continue;

            // parts[0] = Timestamp, parts[1] = Channel1 (Furnace1), parts[2] = Channel2 (Furnace2),
            // parts[3] = Channel3 (Surface), parts[4] = Channel4 (Center)
            if (double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var f1) &&
                double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var f2) &&
                double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var surf) &&
                double.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var center))
            {
                rows.Add(new CsvRow
                {
                    ElapsedSeconds = (i - 1), // 每行 1 秒（假设 800ms 间隔取整）
                    Furnace1 = f1,
                    Furnace2 = f2,
                    Surface = surf,
                    Center = center,
                });
            }
        }

        return rows;
    }
}
