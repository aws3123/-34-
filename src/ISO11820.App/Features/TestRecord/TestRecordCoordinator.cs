using ISO11820.App.Infrastructure.FileStorage;
using ISO11820.App.Shared.Models.Records;

namespace ISO11820.App.Features.TestRecord;

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

    public RecordAcceptResult AcceptRecord(TestRecordInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.TestId);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.ProductId);

        if (_saveStateFlag.IsSaved(input.TestId))
        {
            return RecordAcceptResult.AlreadySaved(input.TestId,
                "该试验记录已保存，请勿重复提交。如需修改，请先取消后重新录入。");
        }

        lock (_pendingRecords)
        {
            _pendingRecords.RemoveAll(r => r.TestId == input.TestId);
            _pendingRecords.Add(input);
        }

        return RecordAcceptResult.CreateAccepted(input.TestId);
    }

    public TestRecordResult ExecuteSave(string testId, Func<TestRecordInput, bool> persistenceAction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(testId);
        ArgumentNullException.ThrowIfNull(persistenceAction);

        if (_saveStateFlag.IsSaved(testId))
        {
            return TestRecordResult.AlreadySaved(testId);
        }

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
            var saveSuccess = persistenceAction(record);

            if (!saveSuccess)
            {
                return TestRecordResult.Failed(testId, "持久化层返回保存失败");
            }

            _saveStateFlag.MarkAsSaved(testId);

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

    public bool IsRecordSaved(string testId)
    {
        return _saveStateFlag.IsSaved(testId);
    }

    public TestRecordInput? GetPendingRecord(string testId)
    {
        lock (_pendingRecords)
        {
            return _pendingRecords.FirstOrDefault(r => r.TestId == testId);
        }
    }

    public void ClearSaveFlag(string testId)
    {
        _saveStateFlag.Clear(testId);
        lock (_pendingRecords)
        {
            _pendingRecords.RemoveAll(r => r.TestId == testId);
        }
    }

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

        if (input.PostWeightGrams <= 0)
            errors.Add("请输入有效的试验后质量");

        return errors.Count == 0
            ? ValidationResult.Valid()
            : ValidationResult.Invalid(errors.ToArray());
    }
}

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

public sealed record ValidationResult
{
    public bool IsValid { get; init; }
    public string[] Errors { get; init; } = Array.Empty<string>();

    public static ValidationResult Valid()
        => new() { IsValid = true };

    public static ValidationResult Invalid(string[] errors)
        => new() { IsValid = false, Errors = errors };
}
