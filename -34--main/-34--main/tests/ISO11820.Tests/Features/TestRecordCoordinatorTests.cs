using ISO11820.App.Features.TestRecord;
using ISO11820.App.Shared.Models.Records;

namespace ISO11820.Tests.Features;

/// <summary>
/// 试验记录协调器测试
/// 验证保存闭环、参数完整性、重复保存阻塞规则
/// </summary>
public sealed class TestRecordCoordinatorTests
{
    private readonly TestRecordCoordinator _coordinator;

    public TestRecordCoordinatorTests()
    {
        // 使用临时目录创建 CsvSampleWriter
        var tempDir = Path.GetTempPath();
        var csvWriter = new ISO11820.App.Infrastructure.FileStorage.CsvSampleWriter(tempDir);
        _coordinator = new TestRecordCoordinator(csvWriter);
    }

    [Fact]
    public void ValidateRecord_Should_Return_Valid_For_Complete_Input()
    {
        var input = CreateValidInput();

        var result = _coordinator.ValidateRecord(input);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("", "P001", "测试员", "正常升温", nameof(TestRecordInput.TestId))]
    [InlineData("T001", "", "测试员", "正常升温", nameof(TestRecordInput.ProductId))]
    [InlineData("T001", "P001", "", "正常升温", nameof(TestRecordInput.Operator))]
    [InlineData("T001", "P001", "测试员", "", nameof(TestRecordInput.Phenomenon))]
    public void ValidateRecord_Should_Return_Invalid_For_Missing_Required_Fields(
        string testId, string productId, string @operator, string phenomenon, string expectedErrorField)
    {
        var input = new TestRecordInput
        {
            TestId = testId,
            ProductId = productId,
            Operator = @operator,
            Phenomenon = phenomenon,
            Quality = TestQuality.Pass
        };

        var result = _coordinator.ValidateRecord(input);

        Assert.False(result.IsValid);
        Assert.All(result.Errors, e => Assert.NotNull(e));
    }

    [Fact]
    public void ValidateRecord_Should_Require_Quality_Selection()
    {
        var input = CreateValidInput();
        input = input with { Quality = TestQuality.NotEvaluated };

        var result = _coordinator.ValidateRecord(input);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("质量"));
    }

    [Fact]
    public void AcceptRecord_Should_Succeed_For_Valid_Input()
    {
        var input = CreateValidInput();

        var result = _coordinator.AcceptRecord(input);

        Assert.True(result.Accepted);
        Assert.Equal(input.TestId, result.TestId);
        Assert.False(result.IsAlreadySaved);
    }

    [Fact]
    public void AcceptRecord_Should_Store_Pending_Record()
    {
        var input = CreateValidInput();

        _coordinator.AcceptRecord(input);

        var pending = _coordinator.GetPendingRecord(input.TestId);
        Assert.NotNull(pending);
        Assert.Equal(input.TestId, pending.TestId);
        Assert.Equal(input.ProductId, pending.ProductId);
    }

    [Fact]
    public void AcceptRecord_Should_Block_If_Already_Saved()
    {
        var input = CreateValidInput();
        _coordinator.AcceptRecord(input);
        _coordinator.ExecuteSave(input.TestId, _ => true);

        // 尝试再次接收
        var result = _coordinator.AcceptRecord(input);

        Assert.False(result.Accepted);
        Assert.True(result.IsAlreadySaved);
    }

    [Fact]
    public void ExecuteSave_Should_Succeed_For_Valid_Input()
    {
        var input = CreateValidInput();
        _coordinator.AcceptRecord(input);

        var result = _coordinator.ExecuteSave(input.TestId, _ => true);

        Assert.True(result.Success);
        Assert.Equal(input.TestId, result.TestId);
        Assert.False(result.IsAlreadySaved);
    }

    [Fact]
    public void ExecuteSave_Should_Call_Persistence_Action()
    {
        var input = CreateValidInput();
        _coordinator.AcceptRecord(input);
        var persistenceCalled = false;

        _coordinator.ExecuteSave(input.TestId, record =>
        {
            persistenceCalled = true;
            Assert.Equal(input.TestId, record.TestId);
            Assert.Equal(input.ProductId, record.ProductId);
            return true;
        });

        Assert.True(persistenceCalled);
    }

    [Fact]
    public void ExecuteSave_Should_Block_Duplicate_Save()
    {
        var input = CreateValidInput();
        _coordinator.AcceptRecord(input);
        _coordinator.ExecuteSave(input.TestId, _ => true);

        // 尝试重复保存
        var result = _coordinator.ExecuteSave(input.TestId, _ => true);

        Assert.True(result.Success);
        Assert.True(result.IsAlreadySaved);
    }

    [Fact]
    public void ExecuteSave_Should_Return_Failed_If_Persistence_Fails()
    {
        var input = CreateValidInput();
        _coordinator.AcceptRecord(input);

        var result = _coordinator.ExecuteSave(input.TestId, _ => false);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void ExecuteSave_Should_Return_Failed_For_Unknown_TestId()
    {
        var result = _coordinator.ExecuteSave("UNKNOWN", _ => true);

        Assert.False(result.Success);
        Assert.Contains("未找到", result.ErrorMessage);
    }

    [Fact]
    public void IsRecordSaved_Should_Return_True_After_Save()
    {
        var input = CreateValidInput();
        _coordinator.AcceptRecord(input);

        Assert.False(_coordinator.IsRecordSaved(input.TestId));

        _coordinator.ExecuteSave(input.TestId, _ => true);

        Assert.True(_coordinator.IsRecordSaved(input.TestId));
    }

    [Fact]
    public void IsRecordSaved_Should_Return_False_For_New_Test()
    {
        var result = _coordinator.IsRecordSaved("NEW-TEST-001");

        Assert.False(result);
    }

    [Fact]
    public void ClearSaveFlag_Should_Allow_Re_Save()
    {
        var input = CreateValidInput();
        _coordinator.AcceptRecord(input);
        _coordinator.ExecuteSave(input.TestId, _ => true);

        _coordinator.ClearSaveFlag(input.TestId);

        // 重新接收和保存
        var newInput = input with { Phenomenon = "修改后的现象" };
        var acceptResult = _coordinator.AcceptRecord(newInput);

        Assert.True(acceptResult.Accepted);
        Assert.False(acceptResult.IsAlreadySaved);
    }

    [Fact]
    public void GetPendingRecord_Should_Return_Null_For_Unknown_Test()
    {
        var pending = _coordinator.GetPendingRecord("UNKNOWN");

        Assert.Null(pending);
    }

    [Fact]
    public void GetPendingRecord_Should_Return_Updated_Record_On_Multiple_Accepts()
    {
        var input1 = CreateValidInput();
        _coordinator.AcceptRecord(input1);

        var input2 = input1 with { Phenomenon = "更新后的现象" };
        _coordinator.AcceptRecord(input2);

        var pending = _coordinator.GetPendingRecord(input1.TestId);

        Assert.NotNull(pending);
        Assert.Equal("更新后的现象", pending.Phenomenon);
    }

    [Fact]
    public void SaveState_Should_Be_Preserved_For_Multiple_Tests()
    {
        var input1 = CreateValidInput();
        var input2 = CreateValidInput();
        input2 = input2 with { TestId = "T002", ProductId = "P002" };

        _coordinator.AcceptRecord(input1);
        _coordinator.AcceptRecord(input2);

        _coordinator.ExecuteSave(input1.TestId, _ => true);

        Assert.True(_coordinator.IsRecordSaved(input1.TestId));
        Assert.False(_coordinator.IsRecordSaved(input2.TestId));
    }

    [Fact]
    public void AcceptRecord_Should_Throw_For_Null_Input()
    {
        Assert.Throws<ArgumentNullException>(() => _coordinator.AcceptRecord(null!));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("T001", false)]
    public void ExecuteSave_Should_Throw_For_Invalid_Arguments(string? testId, bool isActionNull)
    {
        if (testId == null)
        {
            Assert.Throws<ArgumentNullException>(() => _coordinator.ExecuteSave(null!, _ => true));
        }
        if (isActionNull)
        {
            Assert.Throws<ArgumentNullException>(() => _coordinator.ExecuteSave("T001", null!));
        }
    }

    private static TestRecordInput CreateValidInput()
    {
        return new TestRecordInput
        {
            TestId = "T001",
            ProductId = "P001",
            Operator = "测试员张三",
            Phenomenon = "试验过程正常，温度平稳上升",
            Quality = TestQuality.Pass,
            Remarks = "无异常",
            RecordedAt = DateTime.Now,
            DurationSeconds = 300
        };
    }
}
