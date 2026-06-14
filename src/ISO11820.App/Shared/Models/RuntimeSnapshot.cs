using ISO11820.Core.Enums;
using ISO11820.Core.Models;

namespace ISO11820.App.Shared.Models;

public sealed record RuntimeSnapshot(
    TestState State,
    TemperatureSnapshot Temperatures,
    IReadOnlyList<SystemMessage> Messages);
