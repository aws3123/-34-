using ISO11820.App.UI.Common;
using ISO11820.Core.Enums;

namespace ISO11820.Tests.Features;

/// <summary>
/// Tests for <see cref="ButtonStateMatrix"/> — the pure logic layer
/// that maps <see cref="TestState"/> to per-button enabled/disabled states.
/// </summary>
public sealed class ButtonStateMatrixTests
{
    private readonly ButtonStateMatrix _matrix = new();

    [Fact]
    public void Idle_Allows_NewTest_And_StartHeating()
    {
        var states = _matrix.GetEnabledStates(TestState.Idle);

        Assert.True(states["NewTest"]);
        Assert.True(states["StartHeating"]);
        Assert.False(states["StopHeating"]);
        Assert.False(states["StartRecording"]);
        Assert.False(states["StopRecording"]);
        Assert.True(states["ParameterSettings"]);
    }

    [Fact]
    public void Preparing_Only_Allows_StopHeating()
    {
        var states = _matrix.GetEnabledStates(TestState.Preparing);

        Assert.False(states["NewTest"]);
        Assert.False(states["StartHeating"]);
        Assert.True(states["StopHeating"]);
        Assert.False(states["StartRecording"]);
        Assert.False(states["StopRecording"]);
        Assert.False(states["ParameterSettings"]);
    }

    [Fact]
    public void Ready_Allows_StopHeating_And_StartRecording()
    {
        var states = _matrix.GetEnabledStates(TestState.Ready);

        Assert.False(states["NewTest"]);
        Assert.False(states["StartHeating"]);
        Assert.True(states["StopHeating"]);
        Assert.True(states["StartRecording"]);
        Assert.False(states["StopRecording"]);
        Assert.False(states["ParameterSettings"]);
    }

    [Fact]
    public void Recording_Only_Allows_StopRecording()
    {
        var states = _matrix.GetEnabledStates(TestState.Recording);

        Assert.False(states["NewTest"]);
        Assert.False(states["StartHeating"]);
        Assert.False(states["StopHeating"]);
        Assert.False(states["StartRecording"]);
        Assert.True(states["StopRecording"]);
        Assert.False(states["ParameterSettings"]);
    }

    [Fact]
    public void Complete_Allows_NewTest_And_ParameterSettings()
    {
        var states = _matrix.GetEnabledStates(TestState.Complete);

        Assert.True(states["NewTest"]);
        Assert.False(states["StartHeating"]);
        Assert.False(states["StopHeating"]);
        Assert.False(states["StartRecording"]);
        Assert.False(states["StopRecording"]);
        Assert.True(states["ParameterSettings"]);
    }

    [Fact]
    public void All_Buttons_Are_Covered_In_Every_State()
    {
        string[] expectedKeys = { "NewTest", "StartHeating", "StopHeating", "StartRecording", "StopRecording", "ParameterSettings" };

        foreach (TestState state in Enum.GetValues<TestState>())
        {
            var states = _matrix.GetEnabledStates(state);

            foreach (var key in expectedKeys)
            {
                Assert.True(states.ContainsKey(key),
                    $"State {state} is missing button key '{key}'");
            }
        }
    }

    [Fact]
    public void At_Least_One_Action_Button_Enabled_In_Each_Active_State()
    {
        var actionKeys = new[] { "StartHeating", "StopHeating", "StartRecording", "StopRecording" };

        var preparing = _matrix.GetEnabledStates(TestState.Preparing);
        Assert.Contains(preparing, pair => actionKeys.Contains(pair.Key) && pair.Value);

        var ready = _matrix.GetEnabledStates(TestState.Ready);
        Assert.Contains(ready, pair => actionKeys.Contains(pair.Key) && pair.Value);

        var recording = _matrix.GetEnabledStates(TestState.Recording);
        Assert.Contains(recording, pair => actionKeys.Contains(pair.Key) && pair.Value);
    }

    [Fact]
    public void Ready_Allows_Both_StopHeating_And_StartRecording()
    {
        var states = _matrix.GetEnabledStates(TestState.Ready);

        Assert.True(states["StopHeating"]);
        Assert.True(states["StartRecording"]);
    }
}
