using ISO11820.App.Runtime.Controller;
using ISO11820.Core.Enums;

namespace ISO11820.App.Features.TestExecution;

/// <summary>
/// Coordinates the "create new test" workflow between UI and runtime.
/// Current implementation resets the controller state — actual persistence
/// integration will be added by the record/save owner later.
/// </summary>
public sealed class TestExecutionCoordinator
{
    /// <summary>
    /// Prepares the runtime for a new test session.
    /// If the controller is in <see cref="TestState.Complete"/> or any non-Idle state,
    /// it is first reset to <see cref="TestState.Idle"/>.
    /// </summary>
    public void PrepareNewTest(TestController controller)
    {
        if (controller.CurrentState != TestState.Idle)
        {
            controller.ResetToIdle();
        }
    }
}
