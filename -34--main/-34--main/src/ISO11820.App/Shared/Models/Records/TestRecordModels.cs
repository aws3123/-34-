namespace ISO11820.App.Shared.Models.Records;

/// <summary>
/// 试验记录输入模型
/// 用于接收试验完成后的记录数据
/// </summary>
public sealed record TestRecordInput
{
    /// <summary>试验唯一标识</summary>
    public required string TestId { get; init; }

    /// <summary>产品编号</summary>
    public required string ProductId { get; init; }

    /// <summary>操作员</summary>
    public string Operator { get; init; } = string.Empty;

    /// <summary>试验现象描述</summary>
    public string Phenomenon { get; init; } = string.Empty;

    /// <summary>试验后质量</summary>
    public TestQuality Quality { get; init; } = TestQuality.NotEvaluated;

    /// <summary>备注</summary>
    public string Remarks { get; init; } = string.Empty;

    /// <summary>记录时间</summary>
    public DateTime RecordedAt { get; init; } = DateTime.Now;

    /// <summary>试验持续时间（秒）</summary>
    public int DurationSeconds { get; init; }

    /// <summary>是否已保存到数据库</summary>
    public bool IsSaved { get; init; } = false;
}

/// <summary>
/// 试验后质量评估
/// </summary>
public enum TestQuality
{
    /// <summary>未评估</summary>
    NotEvaluated = 0,

    /// <summary>合格</summary>
    Pass = 1,

    /// <summary>不合格</summary>
    Fail = 2,

    /// <summary>需复检</summary>
    Retest = 3
}

/// <summary>
/// 试验记录保存结果
/// </summary>
public sealed record TestRecordResult
{
    public bool Success { get; init; }
    public string TestId { get; init; } = string.Empty;
    public bool IsAlreadySaved { get; init; }
    public string? ErrorMessage { get; init; }

    public static TestRecordResult Saved(string testId)
        => new() { Success = true, TestId = testId };

    public static TestRecordResult AlreadySaved(string testId)
        => new() { Success = true, TestId = testId, IsAlreadySaved = true };

    public static TestRecordResult Failed(string testId, string error)
        => new() { Success = false, TestId = testId, ErrorMessage = error };
}

/// <summary>
/// 保存状态标记
/// 用于防止"已完成未保存"的数据被覆盖
/// </summary>
public sealed class SaveStateFlag
{
    private readonly HashSet<string> _savedTestIds = new();
    private readonly ReaderWriterLockSlim _lock = new();

    /// <summary>
    /// 标记指定试验为已保存
    /// </summary>
    public void MarkAsSaved(string testId)
    {
        _lock.EnterWriteLock();
        try
        {
            _savedTestIds.Add(testId);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 检查指定试验是否已保存
    /// </summary>
    public bool IsSaved(string testId)
    {
        _lock.EnterReadLock();
        try
        {
            return _savedTestIds.Contains(testId);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 清除指定试验的保存标记
    /// </summary>
    public void Clear(string testId)
    {
        _lock.EnterWriteLock();
        try
        {
            _savedTestIds.Remove(testId);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}
