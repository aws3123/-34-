using ISO11820.App.Infrastructure.Persistence;
using ISO11820.App.Infrastructure.Persistence.Models;
using Microsoft.Data.Sqlite;
using OfficeOpenXml;

namespace ISO11820.App.Features.History;

public sealed class HistoryCoordinator
{
    private readonly DbHelper _dbHelper;

    public HistoryCoordinator(DbHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }

    public List<Operator> QueryOperators()
    {
        using var connection = _dbHelper.CreateConnection();
        var sql = "SELECT id, username, pwd, role, created_at FROM operators ORDER BY id";

        using var command = new SqliteCommand(sql, connection);
        using var reader = command.ExecuteReader();

        var result = new List<Operator>();
        while (reader.Read())
        {
            result.Add(new Operator
            {
                Id = reader.GetInt64(0),
                Username = reader.GetString(1),
                Pwd = reader.GetString(2),
                Role = reader.GetString(3),
                CreatedAt = reader.GetString(4)
            });
        }

        return result;
    }

    public List<ProductMaster> QueryProducts(string? productCode = null)
    {
        using var connection = _dbHelper.CreateConnection();
        var sql = productCode is null
            ? "SELECT id, product_code, test_id, product_name, specification, height_mm, diameter_mm, created_at FROM productmaster ORDER BY id"
            : "SELECT id, product_code, test_id, product_name, specification, height_mm, diameter_mm, created_at FROM productmaster WHERE product_code = @code ORDER BY id";

        using var command = new SqliteCommand(sql, connection);
        if (productCode is not null)
        {
            command.Parameters.AddWithValue("@code", productCode);
        }

        using var reader = command.ExecuteReader();

        var result = new List<ProductMaster>();
        while (reader.Read())
        {
            result.Add(new ProductMaster
            {
                Id = reader.GetInt64(0),
                ProductCode = reader.GetString(1),
                TestId = reader.IsDBNull(2) ? null : reader.GetString(2),
                ProductName = reader.GetString(3),
                Specification = reader.IsDBNull(4) ? null : reader.GetString(4),
                HeightMm = reader.IsDBNull(5) ? null : reader.GetDouble(5),
                DiameterMm = reader.IsDBNull(6) ? null : reader.GetDouble(6),
                CreatedAt = reader.GetString(7)
            });
        }

        return result;
    }

    public List<TestMaster> QueryTestTypes(string? productId = null)
    {
        using var connection = _dbHelper.CreateConnection();
        var sql = productId is null
            ? "SELECT productid, testid, testdate, operator, sample_name, specification, height_mm, diameter_mm, preweight, postweight, lostweight_per, deltatf, totaltesttime, flame_time, flame_duration, has_flame, env_temp, env_humidity, notes, flag, created_at FROM testmaster ORDER BY productid, testid"
            : "SELECT productid, testid, testdate, operator, sample_name, specification, height_mm, diameter_mm, preweight, postweight, lostweight_per, deltatf, totaltesttime, flame_time, flame_duration, has_flame, env_temp, env_humidity, notes, flag, created_at FROM testmaster WHERE productid = @productid ORDER BY productid, testid";

        using var command = new SqliteCommand(sql, connection);
        if (productId is not null)
        {
            command.Parameters.AddWithValue("@productid", productId);
        }

        using var reader = command.ExecuteReader();

        var result = new List<TestMaster>();
        while (reader.Read())
        {
            result.Add(MapTestMaster(reader));
        }

        return result;
    }

    /// <summary>
    /// 组合条件查询试验记录，支持按样品编号模糊匹配、操作员、日期范围筛选。
    /// 所有参数可选，未提供的条件不参与筛选。
    /// </summary>
    public List<TestMaster> QueryTests(
        string? productIdLike = null,
        string? operatorName = null,
        string? dateFrom = null,
        string? dateTo = null)
    {
        using var connection = _dbHelper.CreateConnection();
        var conditions = new List<string>();

        if (!string.IsNullOrWhiteSpace(productIdLike))
            conditions.Add("productid LIKE @productid");

        if (!string.IsNullOrWhiteSpace(operatorName))
            conditions.Add("operator = @operator");

        if (!string.IsNullOrWhiteSpace(dateFrom))
            conditions.Add("testdate >= @dateFrom");

        if (!string.IsNullOrWhiteSpace(dateTo))
            conditions.Add("testdate <= @dateTo");

        var whereClause = conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : "";

        var sql = "SELECT productid, testid, testdate, operator, sample_name, specification, " +
                  "height_mm, diameter_mm, preweight, postweight, lostweight_per, deltatf, " +
                  "totaltesttime, flame_time, flame_duration, has_flame, env_temp, env_humidity, " +
                  "notes, flag, created_at FROM testmaster " +
                  whereClause + " ORDER BY productid, testid";

        using var command = new SqliteCommand(sql, connection);

        if (!string.IsNullOrWhiteSpace(productIdLike))
            command.Parameters.AddWithValue("@productid", $"%{productIdLike}%");

        if (!string.IsNullOrWhiteSpace(operatorName))
            command.Parameters.AddWithValue("@operator", operatorName);

        if (!string.IsNullOrWhiteSpace(dateFrom))
            command.Parameters.AddWithValue("@dateFrom", dateFrom);

        if (!string.IsNullOrWhiteSpace(dateTo))
            command.Parameters.AddWithValue("@dateTo", dateTo);

        using var reader = command.ExecuteReader();

        var result = new List<TestMaster>();
        while (reader.Read())
        {
            result.Add(MapTestMaster(reader));
        }

        return result;
    }

    /// <summary>
    /// 将查询结果导出为 Excel 文件。
    /// </summary>
    public void ExportToExcel(List<TestMaster> records, string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("查询结果");

        // 表头
        var headers = new[]
        {
            "样品编号", "试验标识", "试验日期", "操作员", "样品名称",
            "规格", "高度(mm)", "直径(mm)", "试验前重量(g)", "试验后重量(g)",
            "质量损失率(%)", "ΔTf(°C)", "试验总时间(s)", "火焰出现时间(s)",
            "火焰持续时间(s)", "有火焰", "环境温度(°C)", "环境湿度(%)", "备注"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            sheet.Cells[1, i + 1].Value = headers[i];
            sheet.Cells[1, i + 1].Style.Font.Bold = true;
        }

        // 数据行
        for (int i = 0; i < records.Count; i++)
        {
            var r = records[i];
            var row = i + 2;
            sheet.Cells[row, 1].Value = r.ProductId;
            sheet.Cells[row, 2].Value = r.TestId;
            sheet.Cells[row, 3].Value = r.TestDate;
            sheet.Cells[row, 4].Value = r.Operator ?? "";
            sheet.Cells[row, 5].Value = r.SampleName ?? "";
            sheet.Cells[row, 6].Value = r.Specification ?? "";
            sheet.Cells[row, 7].Value = r.HeightMm;
            sheet.Cells[row, 8].Value = r.DiameterMm;
            sheet.Cells[row, 9].Value = r.PreWeight;
            sheet.Cells[row, 10].Value = r.PostWeight;
            sheet.Cells[row, 11].Value = r.LostWeightPer;
            sheet.Cells[row, 12].Value = r.DeltaTf;
            sheet.Cells[row, 13].Value = r.TotalTestTime;
            sheet.Cells[row, 14].Value = r.FlameTime;
            sheet.Cells[row, 15].Value = r.FlameDuration;
            sheet.Cells[row, 16].Value = r.HasFlame == 1 ? "是" : "否";
            sheet.Cells[row, 17].Value = r.EnvTemp;
            sheet.Cells[row, 18].Value = r.EnvHumidity;
            sheet.Cells[row, 19].Value = r.Notes ?? "";
        }

        sheet.Cells.AutoFitColumns();
        package.SaveAs(new FileInfo(filePath));
    }

    private static TestMaster MapTestMaster(SqliteDataReader reader)
    {
        return new TestMaster
        {
            ProductId = reader.GetString(0),
            TestId = reader.GetString(1),
            TestDate = reader.GetString(2),
            Operator = reader.IsDBNull(3) ? null : reader.GetString(3),
            SampleName = reader.IsDBNull(4) ? null : reader.GetString(4),
            Specification = reader.IsDBNull(5) ? null : reader.GetString(5),
            HeightMm = reader.IsDBNull(6) ? null : reader.GetDouble(6),
            DiameterMm = reader.IsDBNull(7) ? null : reader.GetDouble(7),
            PreWeight = reader.IsDBNull(8) ? null : reader.GetDouble(8),
            PostWeight = reader.IsDBNull(9) ? null : reader.GetDouble(9),
            LostWeightPer = reader.IsDBNull(10) ? null : reader.GetDouble(10),
            DeltaTf = reader.IsDBNull(11) ? null : reader.GetDouble(11),
            TotalTestTime = reader.IsDBNull(12) ? null : reader.GetInt32(12),
            FlameTime = reader.IsDBNull(13) ? null : reader.GetInt32(13),
            FlameDuration = reader.IsDBNull(14) ? null : reader.GetInt32(14),
            HasFlame = reader.GetInt32(15),
            EnvTemp = reader.IsDBNull(16) ? null : reader.GetDouble(16),
            EnvHumidity = reader.IsDBNull(17) ? null : reader.GetDouble(17),
            Notes = reader.IsDBNull(18) ? null : reader.GetString(18),
            Flag = reader.IsDBNull(19) ? null : reader.GetString(19),
            CreatedAt = reader.GetString(20)
        };
    }
}