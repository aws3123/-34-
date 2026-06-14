using ISO11820.Core.Enums;

namespace ISO11820.Tests.Runtime;

public sealed class TestStateSmokeTests
{
    [Fact]
    public void Idle_Should_Be_Default_Enum_Value()
    {
        Assert.Equal(0, (int)TestState.Idle);
    }

    [Fact]
    public void All_States_Have_Unique_Values()
    {
        var values = Enum.GetValues<TestState>();
        var distinct = values.Distinct().Count();
        Assert.Equal(values.Length, distinct);
    }

    [Fact]
    public void State_Order_Is_Idle_Preparing_Ready_Recording_Complete()
    {
        Assert.Equal(0, (int)TestState.Idle);
        Assert.Equal(1, (int)TestState.Preparing);
        Assert.Equal(2, (int)TestState.Ready);
        Assert.Equal(3, (int)TestState.Recording);
        Assert.Equal(4, (int)TestState.Complete);
    }
}
