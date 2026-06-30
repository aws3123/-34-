using ISO11820.Core.Enums;

namespace ISO11820.App.UI.Common;

/// <summary>
/// Pure logic mapping <see cref="TestState"/> to per-button enabled/disabled states.
/// No WinForms dependency — safe to unit-test from any test host.
/// </summary>
public sealed class ButtonStateMatrix
{
    public IReadOnlyDictionary<string, bool> GetEnabledStates(TestState state)
    {
        return state switch
        {
            TestState.Idle => Create(
                newTest: true,
                startHeating: true,
                stopHeating: false,
                startRecording: false,
                stopRecording: false,
                parameterSettings: true,
                testRecord: true),

            TestState.Preparing => Create(
                newTest: false,
                startHeating: false,
                stopHeating: true,
                startRecording: false,
                stopRecording: false,
                parameterSettings: false,
                testRecord: false),

            TestState.Ready => Create(
                newTest: false,
                startHeating: false,
                stopHeating: true,
                startRecording: true,
                stopRecording: false,
                parameterSettings: false,
                testRecord: false),

            TestState.Recording => Create(
                newTest: false,
                startHeating: false,
                stopHeating: false,
                startRecording: false,
                stopRecording: true,
                parameterSettings: false,
                testRecord: false),

            TestState.Complete => Create(
                newTest: true,
                startHeating: false,
                stopHeating: false,
                startRecording: false,
                stopRecording: false,
                parameterSettings: true,
                testRecord: true),

            _ => CreateAllFalse(),
        };
    }

    private static Dictionary<string, bool> Create(
        bool newTest,
        bool startHeating,
        bool stopHeating,
        bool startRecording,
        bool stopRecording,
        bool parameterSettings,
        bool testRecord)
    {
        return new Dictionary<string, bool>
        {
            ["NewTest"] = newTest,
            ["StartHeating"] = startHeating,
            ["StopHeating"] = stopHeating,
            ["StartRecording"] = startRecording,
            ["StopRecording"] = stopRecording,
            ["ParameterSettings"] = parameterSettings,
            ["TestRecord"] = testRecord,
        };
    }

    private static Dictionary<string, bool> CreateAllFalse()
    {
        return Create(false, false, false, false, false, false, false);
    }
}
