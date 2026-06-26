using ISO11820.Core.Enums;

namespace ISO11820.App.UI.Common;

/// <summary>
/// Applies <see cref="ButtonStateMatrix"/> logic to actual WinForms <see cref="Button"/> controls.
/// Centralised entry point for all button enable/disable updates —
/// keeps the matrix out of individual click handlers.
/// </summary>
public sealed class ButtonStateManager
{
    private readonly Dictionary<string, Button> _buttons;
    private readonly ButtonStateMatrix _matrix = new();

    public ButtonStateManager(
        Button newTest,
        Button startHeating,
        Button stopHeating,
        Button startRecording,
        Button stopRecording,
        Button parameterSettings)
    {
        _buttons = new Dictionary<string, Button>
        {
            ["NewTest"] = newTest,
            ["StartHeating"] = startHeating,
            ["StopHeating"] = stopHeating,
            ["StartRecording"] = startRecording,
            ["StopRecording"] = stopRecording,
            ["ParameterSettings"] = parameterSettings,
        };
    }

    public void Update(TestState state)
    {
        var enabledStates = _matrix.GetEnabledStates(state);

        foreach (var (key, enabled) in enabledStates)
        {
            if (_buttons.TryGetValue(key, out var button))
            {
                button.Enabled = enabled;
            }
        }
    }
}
