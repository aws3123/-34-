using ISO11820.App.Infrastructure.FileStorage;
using ISO11820.App.Shared.Models.Records;

namespace ISO11820.App.Features.TestRecord;

/// <summary>
/// 试验记录协调器
/// 负责试验完成后的记录保存闭环
/// 统一组织接收输入、调用持久化、置状态标记
/// </summary>
public sealed class TestRecordCoordinator
{
    private readonly CsvSampleWriter _csvWriter;
    private readonly SaveStateFlag _saveStateFlag;
    private readonly List<TestRecordInput> _pendingRecords = new();

    public TestRecordCoordinator(CsvSampleWriter csvWriter)
    {
        _csvWriter = csvWriter ?? throw new ArgumentNullException(nameof(csvWriter));
        _saveStateFlag = new SaveStateFlag();
    }

    /// <summary>
    /// 接收试验记录输入
    /// 由 TestRecordDialog 在保存按钮点击时调用
    /// </summary>
    /// <param name="input">试验记录输入数据</param>
    /// <returns>接收结果</returns>
    public RecordAcceptResult AcceptRecord(TestRecordInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TestId);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.ProductId);

        // 检查是否已保存，避免覆盖
        if (_saveStateFlag.IsSaved(input.TestId))
        {
            return RecordAcceptResult.AlreadySaved(input.TestId,
                "该试验记录已保存，请勿重复提交。如需修改，请先取消后重新录入。");
        }

        // 暂存记录等待保存
        lock (_pendingRecords)
        {
            // 移除同试验的旧记录（如果有）
            _pendingRecords.RemoveAll(r => r.TestId == input.TestId);
            _pendingRecords.Add(input);
        }

        return RecordAcceptResult.CreateAccepted(input.TestId);
    }

    /// <summary>
    /// 执行保存闭环
    /// 调用持久化层完成保存，成功后置保存标记
    /// </summary>
    /// <param name="testId">试验编号</param>
    /// <param name="persistenceAction">持久化操作委托（由持久化层提供）</param>
    /// <returns>保存结果</returns>
    public TestRecordResult ExecuteSave(string testId, Func<TestRecordInput, bool> persistenceAction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(testId);
        ArgumentNullException.ThrowIfNull(persistenceAction);

        // 检查是否已保存，防止重复保存
        if (_saveStateFlag.IsSaved(testId))
        {
            return TestRecordResult.AlreadySaved(testId);
        }

        // 获取待保存记录
        TestRecordInput? record;
        lock (_pendingRecords)
        {
            record = _pendingRecords.FirstOrDefault(r => r.TestId == testId);
        }

        if (record == null)
        {
            return TestRecordResult.Failed(testId, "未找到待保存的试验记录，请先录入记录信息");
        }

        try
        {
            // 调用持久化层保存
            var saveSuccess = persistenceAction(record);

            if (!saveSuccess)
            {
                return TestRecordResult.Failed(testId, "持久化层返回保存失败");
            }

            // 标记为已保存
            _saveStateFlag.MarkAsSaved(testId);

            // 从待保存列表移除
            lock (_pendingRecords)
            {
                _pendingRecords.RemoveAll(r => r.TestId == testId);
            }

            return TestRecordResult.Saved(testId);
        }
        catch (Exception ex)
        {
            return TestRecordResult.Failed(testId, $"保存异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 检查试验是否已保存
    /// 用于运行时判断"已完成未保存"状态
    /// </summary>
    /// <param name="testId">试验编号</param>
    /// <returns>是否已保存</returns>
    public bool IsRecordSaved(string testId)
    {
        return _saveStateFlag.IsSaved(testId);
    }

    /// <summary>
    /// 获取待保存的记录
    /// 用于在对话框中预填充数据
    /// </summary>
    /// <param name="testId">试验编号</param>
    /// <returns>记录输入数据，如不存在则返回 null</returns>
    public TestRecordInput? GetPendingRecord(string testId)
    {
        lock (_pendingRecords)
        {
            return _pendingRecords.FirstOrDefault(r => r.TestId == testId);
        }
    }

    /// <summary>
    /// 清除保存标记
    /// 用于取消保存或重新试验时
    /// </summary>
    /// <param name="testId">试验编号</param>
    public void ClearSaveFlag(string testId)
    {
        _saveStateFlag.Clear(testId);
        lock (_pendingRecords)
        {
            _pendingRecords.RemoveAll(r => r.TestId == testId);
        }
    }

    /// <summary>
    /// 验证试验记录是否完整
    /// 在保存前调用，确保必填项已填写
    /// </summary>
    public ValidationResult ValidateRecord(TestRecordInput input)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(input.TestId))
            errors.Add("试验编号不能为空");

        if (string.IsNullOrWhiteSpace(input.ProductId))
            errors.Add("产品编号不能为空");

        if (string.IsNullOrWhiteSpace(input.Operator))
            errors.Add("操作员不能为空");

        if (string.IsNullOrWhiteSpace(input.Phenomenon))
            errors.Add("试验现象不能为空");

        if (input.Quality == TestQuality.NotEvaluated)
            errors.Add("请选择试验后质量评估");

        return errors.Count == 0
            ? ValidationResult.Valid()
            : ValidationResult.Invalid(errors.ToArray());
    }
}

/// <summary>
/// 记录接收结果
/// </summary>
public sealed record RecordAcceptResult
{
    public bool Accepted { get; init; }
    public string TestId { get; init; } = string.Empty;
    public bool IsAlreadySaved { get; init; }
    public string? Message { get; init; }

    public static RecordAcceptResult CreateAccepted(string testId)
        => new() { Accepted = true, TestId = testId };

    public static RecordAcceptResult AlreadySaved(string testId, string message)
        => new() { Accepted = false, TestId = testId, IsAlreadySaved = true, Message = message };
}

/// <summary>
/// 验证结果
/// </summary>
public sealed record ValidationResult
{
    public bool IsValid { get; init; }
    public string[] Errors { get; init; } = Array.Empty<string>();

    public static ValidationResult Valid()
        => new() { IsValid = true };

    public static ValidationResult Invalid(string[] errors)
        => new() { IsValid = false, Errors = errors };
}
