namespace ISO11820.App.Shared.Models.Records;

public sealed record TestRecordInput
{
    public required string TestId { get; init; }
    public required string ProductId { get; init; }
    public string Operator { get; init; } = string.Empty;
    public string Phenomenon { get; init; } = string.Empty;
    public TestQuality Quality { get; init; } = TestQuality.NotEvaluated;
    public string Remarks { get; init; } = string.Empty;
    public DateTime RecordedAt { get; init; } = DateTime.Now;
    public int DurationSeconds { get; init; }
    public bool IsSaved { get; init; } = false;

    // 火焰信息
    public bool HasFlame { get; init; }
    public int FlameTimeSeconds { get; init; }
    public int FlameDurationSeconds { get; init; }

    // 试验后质量
    public double PostWeightGrams { get; init; }

    // 计算结果
    public double PreWeightGrams { get; init; }
    public double LostWeightGrams { get; init; }
    public double LostWeightPercent { get; init; }
    public double EnvTemperature { get; init; }
    public double FinalFurnace1 { get; init; }
    public double FinalFurnace2 { get; init; }
    public double FinalSurface { get; init; }
    public double FinalCenter { get; init; }
    public double DeltaFurnace1 { get; init; }
    public double DeltaFurnace2 { get; init; }
    public double DeltaSurface { get; init; }
    public double DeltaCenter { get; init; }
}

public enum TestQuality
{
    NotEvaluated = 0,
    Pass = 1,
    Fail = 2,
    Retest = 3
}

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

public sealed class SaveStateFlag
{
    private readonly HashSet<string> _savedTestIds = new();
    private readonly ReaderWriterLockSlim _lock = new();

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
