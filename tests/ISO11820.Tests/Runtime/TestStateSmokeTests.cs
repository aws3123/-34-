using ISO11820.Core.Enums;

namespace ISO11820.Tests.Runtime;

public sealed class TestStateSmokeTests
{
    [Fact]
    public void Idle_Should_Be_Default_Enum_Value()
    {
        Assert.Equal(0, (int)TestState.Idle);
    }
}
